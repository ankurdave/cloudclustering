using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using AzureUtils;

namespace AKMServerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private Dictionary<Guid, KMeansJob> jobs = new Dictionary<Guid, KMeansJob>();

        public override void Run()
        {
            while (true)
            {
                AzureHelper.PollForMessage("serverrequest", message => true, ProcessNewJob);
                AzureHelper.PollForMessage("workerresponse", message => true, ProcessWorkerResponse);
            
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Handles a new k-means job. Initializes the Azure storage and enqueues a number of tasks for workers to complete.
        /// </summary>
        /// <param name="message">The new job. Must be of type KMeansJobData.</param>
        private bool ProcessNewJob(AzureMessage message)
        {
            KMeansJobData job = (KMeansJobData)message;

            jobs[job.JobID] = new KMeansJob(job);
            jobs[job.JobID].InitializeStorage();
            jobs[job.JobID].EnqueueTasks();
            
            return true;
        }

        /// <summary>
        /// Handles an individual worker's taskResult from a running k-means job. Adds up the partial sums from the taskResult.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False if the given task result has already been counted, true otherwise.</returns>
        private bool ProcessWorkerResponse(AzureMessage message)
        {
            KMeansTaskResult taskResult = (KMeansTaskResult)message;
            KMeansJob jobWorkspace = jobs[taskResult.JobID];

            // Make sure we're actually still waiting for a result for this task
            // If not, this might be a duplicate queue message
            if (!jobWorkspace.ContainsTaskID(taskResult.TaskID))
                return false;
            jobWorkspace.RemoveTaskID(taskResult.TaskID);

            // Add up the partial sums
            jobWorkspace.AddDataFromTaskResult(taskResult);

            // If this is the last worker to return, this iteration is done
            if (jobWorkspace.NoMoreTaskIDs())
            {
                jobWorkspace.NextIteration();
            }

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
