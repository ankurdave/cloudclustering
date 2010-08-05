using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    [Serializable]
    public class KMeansJobResult : KMeansJobData
    {
        public Uri Centroids { get; set; }

        public KMeansJobResult(Guid jobID, int n, Uri points, int k, int maxIterationCount, Uri centroids, DateTime jobStartTime)
            : base(jobID, n, points, k, maxIterationCount, jobStartTime)
        {
            this.Centroids = centroids;
        }

        public KMeansJobResult(KMeansJobData jobData, Uri centroids)
            : base(jobData)
        {
            this.Centroids = centroids;
        }
    }
}
