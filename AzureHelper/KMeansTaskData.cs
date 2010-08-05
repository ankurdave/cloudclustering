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
        public int PartitionNumber { get; set; }
        public int M { get; set; }
        public Uri Centroids { get; set; }
        public DateTime TaskStartTime { get; set; }
        public int Iteration { get; set; }

        public KMeansTaskData(Guid jobID, Guid taskID, int n, Uri points, int k, int m, int maxIterationCount, int partitionNumber, Uri centroids, DateTime jobStartTime, DateTime taskStartTime, int iteration)
            : base(jobID, n, points, k, maxIterationCount, jobStartTime)
        {
            this.TaskID = taskID;
            this.PartitionNumber = partitionNumber;
            this.M = m;
            this.Centroids = centroids;
            this.TaskStartTime = TaskStartTime;
            this.Iteration = iteration;
        }

        public KMeansTaskData(KMeansJobData job, Guid taskID, int partitionNumber, int m, Uri centroids, DateTime taskStartTime, int iteration)
            : base(job)
        {
            this.TaskID = taskID;
            this.PartitionNumber = partitionNumber;
            this.M = m;
            this.Centroids = centroids;
            this.TaskStartTime = TaskStartTime;
            this.Iteration = iteration;
        }

        public KMeansTaskData(KMeansTaskData task)
            : base(task)
        {
            this.TaskID = task.TaskID;
            this.PartitionNumber = task.PartitionNumber;
            this.M = task.M;
            this.Centroids = task.Centroids;
            this.TaskStartTime = task.TaskStartTime;
            this.Iteration = task.Iteration;
        }
    }
}
