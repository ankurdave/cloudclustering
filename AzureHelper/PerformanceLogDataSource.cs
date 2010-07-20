using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;

namespace AzureUtils
{
    public class PerformanceLogDataSource
    {
        public PerformanceLogContext ServiceContext { get; private set; }

        public PerformanceLogDataSource()
        {
            ServiceContext = new PerformanceLogContext(AzureHelper.StorageAccount.TableEndpoint.ToString(), AzureHelper.StorageAccount.Credentials);

            AzureHelper.StorageAccount.CreateCloudTableClient().CreateTableIfNotExist(PerformanceLogContext.PerformanceLogTableName);
        }

        public IEnumerable<PerformanceLog> PerformanceLogs
        {
            get
            {
                return (from log in ServiceContext.PerformanceLogTable select log).AsTableServiceQuery<PerformanceLog>().Execute();
            }
        }

        public void Delete(PerformanceLog item)
        {
            ServiceContext.AttachTo(PerformanceLogContext.PerformanceLogTableName, item);
            ServiceContext.DeleteObject(item);
            ServiceContext.SaveChanges();
        }

        public void Insert(PerformanceLog item)
        {
            ServiceContext.AddObject(PerformanceLogContext.PerformanceLogTableName, item);
            ServiceContext.SaveChanges();
        }
    }
}
