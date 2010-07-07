using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class KMeansTask : KMeansJob
    {
        public Guid TaskID { get; set; }
        public CloudBlob Points { get; set; }
        public CloudBlob Centroids { get; set; }

        public KMeansTask() { }

        public KMeansTask(KMeansJob job) : base(job) { }
    }
}
