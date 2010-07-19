using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class PerformanceLog : TableServiceEntity
    {
        public PerformanceLog(string jobID, string methodName, DateTime startTime, DateTime endTime)
            : base(jobID, methodName)
        {
            this.JobID = jobID;
            this.MethodName = methodName;
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        public string JobID { get; set; }
        public string MethodName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
