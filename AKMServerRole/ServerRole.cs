using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using AzureUtils;

namespace AKMServerRole
{
    public class ServerRole : RoleEntryPoint
    {
        private Dictionary<string, Worker> workers = new Dictionary<string, Worker>();
        private Dictionary<Guid, KMeansJob> jobs = new Dictionary<Guid, KMeansJob>();

        public override void Run()
        {
            while (true)
            {
                System.Diagnostics.Trace.TraceInformation("[ServerRole] Waiting for messages...");
                AzureHelper.PollForMessage<ServerControlMessage>(AzureHelper.ServerControlQueue, ProcessNewWorker);
                AzureHelper.PollForMessage<KMeansJobData>(AzureHelper.ServerRequestQueue, ProcessNewJob);
                AzureHelper.PollForMessage<KMeansTaskResult>(AzureHelper.WorkerResponseQueue, ProcessWorkerResponse);
            
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Handles a request for a new k-means job. Sets up a new job and starts it off.
        /// </summary>
        /// <param name="message">The job request. Must be of type KMeansJobData.</param>
        private bool ProcessNewJob(KMeansJobData jobData)
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessNewJob(jobID={0})", jobData.JobID);

            jobs[jobData.JobID] = new KMeansJob(jobData, "server");
            jobs[jobData.JobID].InitializeStorage();
            jobs[jobData.JobID].EnqueueTasks(workers.Values);
            
            return true;
        }

        /// <summary>
        /// Handles a worker response as part of a running k-means job. Looks up the appropriate job and passes the worker's response to it.
        /// </summary>
        /// <param name="message">The worker response. Must be of type KMeansTaskResult.</param>
        private bool ProcessWorkerResponse(KMeansTaskResult taskResult)
        {
            taskResult.RestorePointsProcessedDataByCentroid();

            // Make sure the job belongs to this server
            if (!jobs.ContainsKey(taskResult.JobID))
                return false;

            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessWorkerResponse(jobID={0}, taskID={1}, iterationCount={2})", taskResult.JobID, taskResult.TaskID, jobs[taskResult.JobID].IterationCount);

            return jobs[taskResult.JobID].ProcessWorkerResponse(taskResult, workers.Values);
        }

        private bool ProcessNewWorker(ServerControlMessage controlMessage)
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessNewWorker(machineID={0})", controlMessage.MachineID);

            // Make sure this isn't a duplicate message
            if (workers.ContainsKey(controlMessage.MachineID))
                return true;

            Worker worker = new Worker(controlMessage.MachineID, Guid.Empty.ToString(), controlMessage.FaultDomain);
            AzureHelper.WorkerStatsReporter.Insert(worker);
            
            workers[controlMessage.MachineID] = worker;

            // Need to regroup the workers into new fault domains to accommodate the new worker
            workers = RegroupWorkers(workers.Values, () => Guid.NewGuid().ToString()).ToDictionary(w => w.PartitionKey);
            foreach (Worker w in workers.Select(pair => pair.Value))
                AzureHelper.WorkerStatsReporter.Update(w);

            return true;
        }

        /// <summary>
        /// Assigns the given workers into groups for the purposes of fault tolerance.
        /// 
        /// Currently uses a simple algorithm that assigns at least one worker from each fault domain into each group.
        /// This results in at least as many workers per group as there are fault domains, which is suboptimal from a performance standpoint
        /// because everyone in a group is polling everyone else, resulting in a combinatorial growth of network traffic.
        /// 
        /// The ideal solution would create smaller groups that have only workers from two different fault domains.
        /// </summary>
        /// <param name="buddyGroupIDGenerator">Function that is called once per buddy group and generates a unique buddy group ID on each invocation. (Making this a parameter aids in unit testing.)</param>
        private IEnumerable<Worker> RegroupWorkers(IEnumerable<Worker> workers, Func<string> buddyGroupIDGenerator)
        {
            var workersInFaultDomains = workers.GroupBy(worker => worker.FaultDomain);

            // If there is only one fault domain, group into pairs
            if (workersInFaultDomains.Count() <= 1)
            {
                return workers.SliceMin(2).SelectMany(workersInGroup =>
                {
                    string buddyGroupID = buddyGroupIDGenerator.Invoke();
                    return workersInGroup.Select(worker =>
                    {
                        worker.BuddyGroupID = buddyGroupID;
                        return worker;
                    });
                });
            }

            // Otherwise, assign at least one worker from each fault domain into each group
            int numWorkersInSmallestFaultDomain = workersInFaultDomains.Min(faultDomain => faultDomain.Count());
            return workersInFaultDomains
                .Select(faultDomain => faultDomain.SliceInto(numWorkersInSmallestFaultDomain))
                .ZipN(workersInGroup => {
                    string buddyGroupID = buddyGroupIDGenerator.Invoke();
                    return workersInGroup.SelectMany(ws => ws.Select(worker =>
                    {
                        worker.BuddyGroupID = buddyGroupID;
                        return worker;
                    }));
                }).Flatten1();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            DiagnosticMonitor.Start("DiagnosticsConnectionString");

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            return base.OnStart();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }

        public override void OnStop()
        {
            AzureHelper.ClearQueues();
            AzureHelper.WorkerStatsReporter.Clear();

            base.OnStop();
        }
    }
}
