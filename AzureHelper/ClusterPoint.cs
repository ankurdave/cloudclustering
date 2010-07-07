using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class ClusterPoint : Point
    {
        private Guid _centroidID;
        public Guid CentroidID
        {
            get
            {
                return _centroidID;
            }
        }

        public ClusterPoint(int x, int y, Guid centroidID)
            : base(x, y)
        {
            _centroidID = centroidID;
        }
    }
}
