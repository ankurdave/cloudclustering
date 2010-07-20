using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Data.Services.Client;

namespace AzureUtils
{
    public class PerformanceLogContext : TableServiceContext
    {
        public PerformanceLogContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
        }

        public const string PerformanceLogTableName = "PerformanceLogTable";

        public DataServiceQuery<PerformanceLog> PerformanceLogTable
        {
            get
            {
                return this.CreateQuery<PerformanceLog>(PerformanceLogTableName);
            }
        }
    }
}
