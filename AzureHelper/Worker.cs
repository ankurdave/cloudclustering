using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class Worker : TableServiceEntity
    {
        public Worker(string workerID)
            : base()
        {
            this.WorkerID = workerID;
        }

        public string WorkerID
        {
            get
            {
                return PartitionKey;
            }
            set
            {
                PartitionKey = value;
            }
        }
    }
}
