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
                AzureHelper.WaitForMessage(AzureHelper.WorkerRequestQueue, message => true, ProcessNewTask);
            }
        }

        private bool ProcessNewTask(AzureMessage message)
        {
            KMeansTask task = message as KMeansTask;
            
            System.Diagnostics.Trace.TraceInformation("[WorkerRole] ProcessNewTask(jobID={1}, taskID={0})", task.TaskID, task.JobID);

            // Process the task
            KMeansTaskProcessor taskProcessor = new KMeansTaskProcessor(task);
            taskProcessor.Run();

            // Send the result back
            taskProcessor.TaskResult.SavePointsProcessedDataByCentroid();
            AzureHelper.EnqueueMessage(AzureHelper.WorkerResponseQueue, taskProcessor.TaskResult);

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
