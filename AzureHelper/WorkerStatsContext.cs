﻿using System;
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
                System.Diagnostics.Trace.WriteLine("Can't attach to entity: " + e.ToString());
            }

            UpdateObject(item);
            SaveChanges();
        }

        private void DeleteAllRows()
        {
            foreach (Worker worker in WorkerStats.Execute())
            {
                AttachTo(WorkerStatsTableName, worker);
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
            DeleteRecreateTable();
        }
    }
}