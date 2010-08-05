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
            : base(workerID, Guid.NewGuid().ToString())
        {
        }

        public Worker()
            : base()
        {
        }
    }
}
