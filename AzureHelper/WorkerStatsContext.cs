using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Data.Services.Client;

namespace AzureUtils
{
    public class WorkerStatsContext : TableServiceContext
    {
        public WorkerStatsContext()
            : base(AzureHelper.StorageAccount.TableEndpoint.ToString(), AzureHelper.StorageAccount.Credentials)
        {
            AzureHelper.StorageAccount.CreateCloudTableClient().CreateTableIfNotExist(WorkerStatsTableName);
        }

        public const string WorkerStatsTableName = "WorkerStats";

        public DataServiceQuery<Worker> WorkerStats
        {
            get
            {
                return this.CreateQuery<Worker>(WorkerStatsTableName);
            }
        }

        public void Delete(Worker item)
        {
            AttachTo(WorkerStatsTableName, item);
            DeleteObject(item);
            SaveChanges();
        }

        public void Insert(Worker item)
        {
            AddObject(WorkerStatsTableName, item);
            SaveChanges();
        }

        public void Update(Worker item)
        {
            try
            {
                AttachTo(WorkerStatsTableName, item);
            }
            catch (InvalidOperationException e) // Context is already tracking the entity
            {
            }

            UpdateObject(item);
            SaveChanges();
        }

        public IQueryable<Worker> WorkersInBuddyGroup(string buddyGroup)
        {
            return WorkerStats.Where(worker => worker.BuddyGroupID == buddyGroup);
        }

        private void DeleteAllRows()
        {
            IEnumerable<Worker> rows = WorkerStats.Execute().ToList();
            foreach (Worker worker in rows)
            {
                try
                {
                    AttachTo(WorkerStatsTableName, worker);
                }
                catch (InvalidOperationException e)
                {
                }
                DeleteObject(worker);
            }
            SaveChanges();
        }

        private void DeleteRecreateTable()
        {
            CloudTableClient client = AzureHelper.StorageAccount.CreateCloudTableClient();
            client.RetryPolicy = RetryPolicies.Retry(int.MaxValue, TimeSpan.FromSeconds(5));
            client.DeleteTable(WorkerStatsTableName);
            client.CreateTable(WorkerStatsTableName);
        }

        public void Clear()
        {
            DeleteAllRows();
        }
    }
}
