using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class KMeansTaskResult : KMeansTask
    {
        public int NumPointsChanged { get; set; }
        public Dictionary<Centroid, PointsProcessedData> PointsProcessedDataByCentroid { get; set; }
    }

    public class PointsProcessedData
    {
        public int NumPointsProcessed { get; set; }
        public Point PartialPointSum { get; set; }

        public static PointsProcessedData operator +(PointsProcessedData d1, PointsProcessedData d2)
        {
            return new PointsProcessedData
            {
                NumPointsProcessed = d1.NumPointsProcessed + d2.NumPointsProcessed,
                PartialPointSum = d1.PartialPointSum + d2.PartialPointSum
            };
        }
    }
}
