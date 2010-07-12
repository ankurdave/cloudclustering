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
        public Uri Points { get; set; }
        public Uri Centroids { get; set; }

        public KMeansJobResult(Guid jobID, int n, int k, int m, Uri points, Uri centroids)
            : base(jobID, n, k, m)
        {
            this.Points = points;
            this.Centroids = centroids;
        }

        public KMeansJobResult(KMeansJobData jobData, Uri points, Uri centroids)
            : base(jobData)
        {
            this.Points = points;
            this.Centroids = centroids;
        }
    }
}
