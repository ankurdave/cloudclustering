using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    [Serializable]
    public class KMeansTaskData : KMeansJobData
    {
        public Guid TaskID { get; set; }
        public Uri Points { get; set; }
        public int PointPartitionNumber { get; set; }
        public Uri Centroids { get; set; }

        public KMeansTaskData(Guid jobID, Guid taskID, int n, int k, int m, Uri points, int pointPartitionNumber, Uri centroids)
            : base(jobID, n, k, m)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.PointPartitionNumber = pointPartitionNumber;
            this.Centroids = centroids;
        }

        public KMeansTaskData(KMeansJobData job, Guid taskID, Uri points, int pointPartitionNumber, Uri centroids)
            : base(job)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.PointPartitionNumber = pointPartitionNumber;
            this.Centroids = centroids;
        }

        public KMeansTaskData(KMeansTaskData task)
            : base(task)
        {
            this.TaskID = task.TaskID;
            this.Points = task.Points;
            this.PointPartitionNumber = task.PointPartitionNumber;
            this.Centroids = task.Centroids;
        }
    }
}
