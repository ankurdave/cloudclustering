using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    [Serializable]
    public class KMeansJobStatus : KMeansJobResult
    {
        public int IterationNumber { get; set; }
        public DateTime IterationStartTime { get; set; }
        public List<TimeBenchmark> BenchmarkData { get; set; }

        public KMeansJobStatus(KMeansJobData jobData, int iterationNumber, DateTime iterationStartTime, Uri points, Uri centroids)
            : base(jobData, points, centroids)
        {
            this.IterationNumber = iterationNumber;
            this.IterationStartTime = iterationStartTime;

            this.BenchmarkData = new List<TimeBenchmark>();
        }

        public void AddTimeBenchmark(string methodName, TimeSpan time)
        {
            BenchmarkData
                .Find(timeBenchmark => timeBenchmark.MethodName == methodName)
                .ExecutionTimes.Add(time);
        }
    }
}
