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
        private Dictionary<Guid, KMeansJob> jobs = new Dictionary<Guid, KMeansJob>();

        public override void Run()
        {
            AzureHelper.ClearQueues();

            while (true)
            {
                System.Diagnostics.Trace.TraceInformation("[ServerRole] Waiting for messages...");
                AzureHelper.PollForMessage(AzureHelper.ServerRequestQueue, message => true, ProcessNewJob, visibilityTimeoutSeconds:3600);
                AzureHelper.PollForMessage(AzureHelper.WorkerResponseQueue, message => true, ProcessWorkerResponse, visibilityTimeoutSeconds:3600);
            
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Handles a request for a new k-means job. Sets up a new job and starts it off.
        /// </summary>
        /// <param name="message">The job request. Must be of type KMeansJobData.</param>
        private bool ProcessNewJob(AzureMessage message)
        {
            KMeansJobData jobData = message as KMeansJobData;

            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessNewJob(jobID={0})", jobData.JobID);

            jobs[jobData.JobID] = new KMeansJob(jobData)
            {
                MachineID = "server"
            };
            jobs[jobData.JobID].InitializeStorage();
            jobs[jobData.JobID].EnqueueTasks();
            
            return true;
        }

        /// <summary>
        /// Handles a worker response as part of a running k-means job. Looks up the appropriate job and passes the worker's response to it.
        /// </summary>
        /// <param name="message">The worker response. Must be of type KMeansTaskResult.</param>
        private bool ProcessWorkerResponse(AzureMessage message)
        {
            KMeansTaskResult taskResult = message as KMeansTaskResult;
            taskResult.RestorePointsProcessedDataByCentroid();

            // Make sure the job belongs to this server
            if (!jobs.ContainsKey(taskResult.JobID))
                return false;

            System.Diagnostics.Trace.TraceInformation("[ServerRole] ProcessWorkerResponse(jobID={0}, taskID={1}, iterationCount={2})", taskResult.JobID, taskResult.TaskID, jobs[taskResult.JobID].IterationCount);

            return jobs[taskResult.JobID].ProcessWorkerResponse(taskResult);
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
