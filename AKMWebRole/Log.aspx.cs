using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AzureUtils;
using System.Data.Services.Client;

namespace AKMWebRole
{
    public partial class Log : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string jobID = Request.Params["JobID"];
            if (string.IsNullOrEmpty(jobID))
            {
                Response.StatusCode = 400;
                Response.StatusDescription = "Bad Request";
                return;
            }

            Response.ContentType = "text/plain";

            IEnumerable<PerformanceLog> logs = (AzureHelper.PerformanceLogger.PerformanceLogs
                .Where(log => log.PartitionKey == jobID.ToString()) as DataServiceQuery<PerformanceLog>)
                .Execute().ToList();

            if (!string.IsNullOrEmpty(Request.Params["summary"]))
            {
                PrintSummary(logs);
            }
            else
            {
                PrintLog(logs);
            }
            
        }

        private void PrintSummary(IEnumerable<PerformanceLog> logs)
        {
            int targetIteration = 0;
            if (!string.IsNullOrEmpty(Request.Params["iteration"]))
            {
                targetIteration = int.Parse(Request.Params["iteration"]);
            }

            logs = logs
                    .Where(log => log.IterationCount == targetIteration);

            if (string.IsNullOrEmpty(Request.Params["allRoles"]))
            {
                logs = GenerateMachineIDs(logs).Where(log => log.MachineID != "server");
            }

            DateTime startTime = logs.Min(log => log.StartTime);
            DateTime endTime = logs.Max(log => log.EndTime);

            Response.Write((endTime - startTime).TotalMinutes);
        }

        private void PrintLog(IEnumerable<PerformanceLog> logs)
        {
            if (string.IsNullOrEmpty(Request.Params["all"]))
            {
                logs = logs
                    .Where(log => log.MethodName == "ProcessNewTask");
            }
            else
            {
                logs = logs
                    .Where(log => log.MethodName != "InitializeStorage");
            }

            logs = logs
                .OrderBy(log => log.StartTime);

            if (logs.Any(log => string.IsNullOrEmpty(log.MachineID)))
            {
                logs = GenerateMachineIDs(logs);
            }

            Response.Write(string.Join("", logs
                .Select(log => string.Format("{0}\t{2}\tExecuting the task {3}...\r\n"
                    + "{1}\t{2}\tExecution of task {3} is done, it takes {4} mins\r\n",
                    log.StartTime.Ticks, log.EndTime.Ticks, log.MachineID, Math.Abs((log.MethodName + log.IterationCount + log.RowKey).GetHashCode()), (log.EndTime - log.StartTime).TotalMinutes))));
        }

        /// <summary>
        /// Processes a list of PerformanceLogs and populates their machine IDs with synthetic but consistent values.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private IEnumerable<PerformanceLog> GenerateMachineIDs(IEnumerable<PerformanceLog> logs)
        {
            int currentIteration = -1;
            int currentWorkerID = -1;

            foreach (PerformanceLog log in logs)
            {
                if (serverMethodNames.Contains(log.MethodName))
                {
                    log.MachineID = "server";
                }
                else if (workerMethodNames.Contains(log.MethodName))
                {
                    // Reset the currentWorkerID on every iteration
                    if (log.IterationCount != currentIteration)
                    {
                        currentWorkerID = -1;
                        currentIteration = log.IterationCount;
                    }

                    currentWorkerID++;
                    log.MachineID = string.Format("worker{0}", currentWorkerID);
                }

                yield return log;
            }
        }

        private static List<string> serverMethodNames = new List<string> { "InitializeStorage", "EnqueueTasks", "ProcessWorkerResponse", "RecalculateCentroids" };
        private static List<string> workerMethodNames = new List<string> { "ProcessNewTask" };
    }
}