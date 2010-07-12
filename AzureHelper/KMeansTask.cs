using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    [Serializable]
    public class KMeansTask : KMeansJobData
    {
        public Guid TaskID { get; set; }
        public Uri Points { get; set; }
        public Uri Centroids { get; set; }

        public KMeansTask(Guid jobID, Guid taskID, int n, int k, int m, Uri points, Uri centroids)
            : base(jobID, n, k, m)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.Centroids = centroids;
        }

        public KMeansTask(KMeansJobData job, Guid taskID, Uri points, Uri centroids)
            : base(job)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.Centroids = centroids;
        }

        public KMeansTask(KMeansTask task)
            : base(task)
        {
            this.TaskID = task.TaskID;
            this.Points = task.Points;
            this.Centroids = task.Centroids;
        }
    }
}
