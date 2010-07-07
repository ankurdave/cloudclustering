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
        public int K { get; set; }
        public int M { get; set; }

        public KMeansJobData() { }

        public KMeansJobData(KMeansJobData job)
        {
            JobID = job.JobID;
            N = job.N;
            K = job.K;
            M = job.M;
        }
    }
}
