using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;

namespace AzureUtils
{
    public class PerformanceLogContext : TableServiceContext
    {
        public PerformanceLogContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
        }

        public const string PerformanceLogTableName = "PerformanceLogTable";

        public IQueryable<PerformanceLog> PerformanceLogTable
        {
            get
            {
                return this.CreateQuery<PerformanceLog>(PerformanceLogTableName);
            }
        }
    }
}
