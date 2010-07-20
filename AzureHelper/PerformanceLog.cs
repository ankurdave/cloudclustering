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
            : base(jobID, Guid.NewGuid().ToString())
        {
            this.MethodName = methodName;
            this.StartTime = startTime;
            this.EndTime = endTime;

            this.IterationCount = 0;
            this.Points = string.Empty;
            this.Centroids = string.Empty;
        }

        public PerformanceLog()
            : base()
        {
            this.MethodName = string.Empty;
            this.IterationCount = 0;
            this.Points = string.Empty;
            this.Centroids = string.Empty;
        }

        public string MethodName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int IterationCount { get; set; }
        public string Points { get; set; }
        public string Centroids { get; set; }
    }
}
