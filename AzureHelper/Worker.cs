using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class Worker : TableServiceEntity
    {
        public Worker(string workerID, string buddyGroupID, int faultDomain)
            : base(workerID, buddyGroupID)
        {
            this.FaultDomain = faultDomain;
        }

        public Worker()
            : base()
        {
        }

        public int FaultDomain { get; set; }
    }
}
