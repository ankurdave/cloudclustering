using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    [Serializable]
    public class KMeansTaskResult : KMeansTask
    {
        public int NumPointsChanged { get; set; }

        [NonSerialized]
        private Dictionary<Guid, PointsProcessedData> _pointsProcessedDataByCentroid;
        public Dictionary<Guid, PointsProcessedData> PointsProcessedDataByCentroid { get { return _pointsProcessedDataByCentroid; } set { _pointsProcessedDataByCentroid = value; } }

        public List<KeyValuePair<Guid, PointsProcessedData>> PointsProcessedDataByCentroidList { get; private set; }

        public KMeansTaskResult(KMeansTask task)
            : base(task)
        {
            NumPointsChanged = 0;
            PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
        }

        public void SavePointsProcessedDataByCentroid()
        {
            PointsProcessedDataByCentroidList = new List<KeyValuePair<Guid, PointsProcessedData>>();
            foreach (KeyValuePair<Guid, PointsProcessedData> pair in PointsProcessedDataByCentroid)
            {
                PointsProcessedDataByCentroidList.Add(pair);
            }
        }

        public void RestorePointsProcessedDataByCentroid()
        {
            PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
            foreach (KeyValuePair<Guid, PointsProcessedData> pair in PointsProcessedDataByCentroidList)
            {
                PointsProcessedDataByCentroid[pair.Key] = pair.Value;
            }
        }
    }
}
