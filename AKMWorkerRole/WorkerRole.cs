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
        public override void Run()
        {
            while (true)
            {
                System.Diagnostics.Trace.TraceInformation("[WorkerRole] Waiting for messages...");
                AzureHelper.WaitForMessage(AzureHelper.WorkerRequestQueue, message => true, ProcessNewTask, visibilityTimeoutSeconds:3600);
            }
        }

        private bool ProcessNewTask(AzureMessage message)
        {
            DateTime start = DateTime.UtcNow;

            KMeansTaskData task = message as KMeansTaskData;
            
            System.Diagnostics.Trace.TraceInformation("[WorkerRole] ProcessNewTask(jobID={1}, taskID={0})", task.TaskID, task.JobID);

            // Process the taskData
            KMeansTaskProcessor taskProcessor = new KMeansTaskProcessor(task);
            taskProcessor.Run();

            // Send the result back
            taskProcessor.TaskResult.SavePointsProcessedDataByCentroid();
            AzureHelper.EnqueueMessage(AzureHelper.WorkerResponseQueue, taskProcessor.TaskResult);

            DateTime end = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(task.JobID.ToString(), "ProcessNewTask", start, end);
            log.Points = task.Points.ToString();
            log.Centroids = task.Centroids.ToString();
            log.IterationCount = task.Iteration;
            AzureHelper.PerformanceLogger.Insert(log);

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
