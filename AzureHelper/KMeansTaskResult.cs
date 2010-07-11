using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class KMeansTaskResult : KMeansTask
    {
        public int NumPointsChanged { get; set; }
        public Dictionary<Guid, PointsProcessedData> PointsProcessedDataByCentroid { get; set; }

        public KMeansTaskResult()
            : base() { }
        public KMeansTaskResult(KMeansTask task)
            : base(task) { }
    }
}
