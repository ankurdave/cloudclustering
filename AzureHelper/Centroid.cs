using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class Centroid : Point
    {
        private Guid _id;
        public Guid ID
        {
            get
            {
                return _id;
            }
        }

        public Centroid(Guid id, int x, int y) : base(x, y)
        {
            _id = id;
        }
    }
}
