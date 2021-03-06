﻿using System;
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

        public DataServiceQuery<PerformanceLog> PerformanceLogs
        {
            get
            {
                return ServiceContext.PerformanceLogTable;
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

        public void Update(PerformanceLog item)
        {
            try
            {
                ServiceContext.AttachTo(PerformanceLogContext.PerformanceLogTableName, item);
            }
            catch (InvalidOperationException e) // Context is already tracking the entity
            {
            }

            ServiceContext.UpdateObject(item);
            ServiceContext.SaveChanges();
        }
    }
}
