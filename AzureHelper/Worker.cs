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
            : base(workerID, Guid.NewGuid().ToString())
        {
            this.BuddyGroupID = buddyGroupID;
            this.FaultDomain = faultDomain;
        }

        public Worker()
            : base()
        {
        }

        public string BuddyGroupID { get; set; }
        public int FaultDomain { get; set; }
    }
}
