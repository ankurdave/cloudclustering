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
            AzureHelper.ClearQueues();
            AzureHelper.WorkerStatsReporter.Clear();

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
            jobs[jobData.JobID].EnqueueTasks(workers);
            
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

            return jobs[taskResult.JobID].ProcessWorkerResponse(taskResult, workers);
        }

        private bool ProcessNewWorker(ServerControlMessage controlMessage)
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessNewWorker(machineID={0})", controlMessage.MachineID);

            // Make sure this isn't a duplicate message
            if (workers.ContainsKey(controlMessage.MachineID))
                return true;

            Worker worker = new Worker(controlMessage.MachineID);
            
            workers[controlMessage.MachineID] = worker;
            AzureHelper.WorkerStatsReporter.Insert(worker);

            return true;
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
    }
}
