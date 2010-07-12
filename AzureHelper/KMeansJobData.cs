﻿using System;
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

        public KMeansJobData(Guid jobID, int n, int k, int m)
        {
            this.JobID = jobID;
            this.N = n;
            this.K = k;
            this.M = m;
        }

        public KMeansJobData(KMeansJobData jobData)
        {
            JobID = jobData.JobID;
            N = jobData.N;
            K = jobData.K;
            M = jobData.M;
        }
    }
}
