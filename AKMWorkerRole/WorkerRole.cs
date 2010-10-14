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

namespace AKMWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private string machineID;
        private List<Worker> buddies;
        private const int visibilityTimeoutSeconds = 300;

        public override void Run()
        {
            InitializeToServer();

            while (true)
            {
                System.Diagnostics.Trace.TraceInformation("[WorkerRole] Waiting for messages...");
                AzureHelper.PollForMessage<KMeansTaskData>(AzureHelper.GetWorkerRequestQueue(machineID), ProcessNewTask, visibilityTimeoutSeconds);

                // Note that if the previous call to PollForMessage finds a task to do, it will block until the task is complete.
                // So we will only start checking the buddies *after* we're done with our own task. This gives time for the buddies to complete their work normally.
                // But this shouldn't matter because of the way CheckBuddies works: it only declares a buddy as failed once the task has reappeared on its queue after the visibility timeout,
                // which should be longer than the time it takes to process a task.
                CheckBuddies();
                
                Thread.Sleep(500);
            }
        }

        private bool ProcessNewTask(KMeansTaskData task)
        {
            System.Diagnostics.Trace.TraceInformation("[WorkerRole] ProcessNewTask(jobID={1}, taskID={0})", task.TaskID, task.JobID);

            UpdateBuddyGroup(task);

            AzureHelper.LogPerformance(() =>
            {
                // Process the taskData
                KMeansTaskProcessor taskProcessor = new KMeansTaskProcessor(task);
                taskProcessor.Run();

                // Send the result back
                taskProcessor.TaskResult.SavePointsProcessedDataByCentroid();
                AzureHelper.EnqueueMessage(AzureHelper.WorkerResponseQueue, taskProcessor.TaskResult);
            }, jobID: task.JobID.ToString(), methodName: "ProcessNewTask", iterationCount: task.Iteration, points: task.Points.ToString(), centroids: task.Centroids.ToString(), machineID: machineID);

            return true;
        }

        private void UpdateBuddyGroup(KMeansTaskData task)
        {
            buddies = AzureHelper.WorkerStatsReporter.WorkersInBuddyGroup(task.BuddyGroup).ToList();
        }

        /// <summary>
        /// Polls the queues of all buddies looking for messages that have been on the queue for longer than the standard visibility timeout (and are visible). If such a message is found, processes it as a normal task.
        /// </summary>
        /// 
        /// <remarks>
        /// This handles the following cases appropriately:
        /// a) If the server has just put the message on the buddy's queue and the buddy hasn't had time to look at it yet, then CheckBuddies will not handle it, because it won't have been on the queue for long enough.
        /// b) If the buddy is still processing the message, then CheckBuddies will not handle it, because it simply won't be visible in the queue at all, because of Azure's visibility timeout.
        /// c) If the buddy failed before it could even look at the message, then CheckBuddies will handle it once it has been on the queue for the standard visibility timeout.
        /// d) If the buddy failed while it was processing the message, then CheckBuddies will handle it when it reappears on the queue. By this time it would have been inserted longer ago than the standard visibility timeout, so CheckBuddies will handle it immediately once it reappears.
        /// 
        /// TODO: If a message is processed, report the buddy who was supposed to be responsible for it as failed to the server.
        /// </remarks>
        private void CheckBuddies()
        {
            if (buddies == null)
                return;

            foreach (Worker buddy in buddies)
            {
                AzureHelper.PollForMessageRawCondition<KMeansTaskData>(AzureHelper.GetWorkerRequestQueue(buddy.PartitionKey), ProcessNewTask, visibilityTimeoutSeconds, rawMessage => HasMessageBeenOnQueueForThreshold(rawMessage, visibilityTimeoutSeconds));
            }
        }

        private bool HasMessageBeenOnQueueForThreshold(CloudQueueMessage message, int thresholdSeconds)
        {
            return message.InsertionTime.HasValue && message.InsertionTime.Value.AddSeconds(thresholdSeconds) < DateTime.UtcNow;
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

        private void InitializeToServer()
        {
            // Give ourselves a machine ID
            this.machineID = Guid.NewGuid().ToString();

            // Find our fault domain
            int faultDomain = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.FaultDomain : 1;

            Trace.TraceInformation("[WorkerRole] Machine ID: {0}, Fault Domain: {1}", machineID, faultDomain);

            // Announce ourselves to the server
            AzureHelper.EnqueueMessage(AzureHelper.ServerControlQueue, new ServerControlMessage(machineID, faultDomain));
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
