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
        public Uri Centroids { get; set; }
        public DateTime TaskStartTime { get; set; }
        public int Iteration { get; set; }

        public KMeansTaskData(Guid jobID, Guid taskID, int n, int k, int m, int maxIterationCount, Uri points, Uri centroids, DateTime jobStartTime, DateTime taskStartTime, int iteration)
            : base(jobID, n, k, m, maxIterationCount, jobStartTime)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.Centroids = centroids;
            this.TaskStartTime = TaskStartTime;
            this.Iteration = iteration;
        }

        public KMeansTaskData(KMeansJobData job, Guid taskID, Uri points, Uri centroids, DateTime taskStartTime, int iteration)
            : base(job)
        {
            this.TaskID = taskID;
            this.Points = points;
            this.Centroids = centroids;
            this.TaskStartTime = TaskStartTime;
            this.Iteration = iteration;
        }

        public KMeansTaskData(KMeansTaskData task)
            : base(task)
        {
            this.TaskID = task.TaskID;
            this.Points = task.Points;
            this.Centroids = task.Centroids;
            this.TaskStartTime = task.TaskStartTime;
            this.Iteration = task.Iteration;
        }
    }
}
