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

        public override void Run()
        {
            while (true)
            {
                System.Diagnostics.Trace.TraceInformation("[WorkerRole] Waiting for messages...");
                AzureHelper.PollForMessage<KMeansTaskData>(AzureHelper.GetWorkerRequestQueue(machineID), ProcessNewTask, visibilityTimeoutSeconds:3600);
                Thread.Sleep(500);
            }
        }

        private bool ProcessNewTask(KMeansTaskData task)
        {
            System.Diagnostics.Trace.TraceInformation("[WorkerRole] ProcessNewTask(jobID={1}, taskID={0})", task.TaskID, task.JobID);

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

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            DiagnosticMonitor.Start("DiagnosticsConnectionString");

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            InitializeToServer();

            return base.OnStart();
        }

        private void InitializeToServer()
        {
            // Give ourselves a machine ID
            this.machineID = Guid.NewGuid().ToString();

            // Announce ourselves to the server
            AzureHelper.EnqueueMessage(AzureHelper.ServerControlQueue, new ServerControlMessage(machineID));
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
