using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace AzureUtils
{
    [Serializable]
    public class KMeansJobData : AzureMessage
    {
        public Guid JobID { get; set; }
        public int N { get; set; }
        public Uri Points { get; set; }
        public int K { get; set; }
        public int M { get; set; }
        public int MaxIterationCount { get; set; }
        public DateTime JobStartTime { get; set; }

        public KMeansJobData(Guid jobID, int n, Uri points, int k, int m, int maxIterationCount, DateTime jobStartTime)
        {
            this.JobID = jobID;
            this.N = n;
            this.Points = points;
            this.K = k;
            this.M = m;
            this.MaxIterationCount = maxIterationCount;
            this.JobStartTime = jobStartTime;
        }

        public KMeansJobData(KMeansJobData jobData)
        {
            JobID = jobData.JobID;
            N = jobData.N;
            Points = jobData.Points;
            K = jobData.K;
            M = jobData.M;
            MaxIterationCount = jobData.MaxIterationCount;
            JobStartTime = jobData.JobStartTime;
        }
    }
}
