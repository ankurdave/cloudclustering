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

            var logs = (AzureHelper.PerformanceLogger.PerformanceLogs
                .Where(log => log.PartitionKey == jobID.ToString()) as DataServiceQuery<PerformanceLog>)
                .Execute()
                .OrderBy(log => log.StartTime)
                .Select(log => string.Format("{0}\t{2}\tExecuting the task {3}{4}...\n"
                    + "{1}\t{2}\tExecution of task {3}{4} is done, it takes {5} mins\n",
                    log.StartTime, log.EndTime, log.PartitionKey, log.MethodName, log.IterationCount, (log.EndTime - log.StartTime).TotalMinutes));
            Response.Write(string.Join("", logs));
        }
    }
}