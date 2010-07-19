using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class PerformanceLogDataSource
    {
        private PerformanceLogContext serviceContext = null;

        public PerformanceLogDataSource()
        {
            serviceContext = new PerformanceLogContext(AzureHelper.StorageAccount.TableEndpoint.ToString(), AzureHelper.StorageAccount.Credentials);

            AzureHelper.StorageAccount.CreateCloudTableClient().CreateTableIfNotExist(PerformanceLogContext.PerformanceLogTableName);
        }

        public IEnumerable<PerformanceLog> Select()
        {
            return (from c in serviceContext.PerformanceLogTable select c)
                .AsTableServiceQuery<PerformanceLog>()
                .Execute();
        }

        public void Delete(PerformanceLog item)
        {
            serviceContext.AttachTo(PerformanceLogContext.PerformanceLogTableName, item);
            serviceContext.DeleteObject(item);
            serviceContext.SaveChanges();
        }

        public void Insert(PerformanceLog item)
        {
            serviceContext.AddObject(PerformanceLogContext.PerformanceLogTableName, item);
            serviceContext.SaveChanges();
        }
    }
}
