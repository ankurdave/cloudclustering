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

            var logs = (AzureHelper.PerformanceLogger.PerformanceLogs
                .Where(log => log.PartitionKey == jobID.ToString()) as DataServiceQuery<PerformanceLog>)
                .Execute()
                .Where(log => log.MethodName == "ProcessNewTask")
                .OrderBy(log => log.StartTime)
                .Select(log => string.Format("{0}\t{2}\tExecuting the task {3}{4}{5}...\n"
                    + "{1}\t{2}\tExecution of task {3}{4}{5} is done, it takes {6} mins\n",
                    log.StartTime.ToString("M/d/yyyy H:m"), log.EndTime.ToString("M/d/yyyy H:m"), log.PartitionKey, log.MethodName, log.IterationCount, log.RowKey, (log.EndTime - log.StartTime).TotalMinutes));
            Response.Write(string.Join("", logs));
        }
    }
}