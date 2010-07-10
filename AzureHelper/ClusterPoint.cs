using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    public class ClusterPoint : Point
    {
        public Guid CentroidID { get; set; }

        public ClusterPoint()
            : base()
        { }

        public ClusterPoint(float x, float y, Guid centroidID)
            : base(x, y)
        {
            this.CentroidID = centroidID;
        }

        public ClusterPoint(Point p, Guid centroidID)
            : base(p)
        {
            this.CentroidID = centroidID;
        }

        public new static int Size
        {
            get
            {
                return Point.Size + Guid.Empty.ToByteArray().Length;
            }
        }

        public override byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream(Size);

            byte[] pointBytes = base.ToByteArray();
            stream.Write(pointBytes, 0, pointBytes.Length);

            byte[] centroidIDBytes = CentroidID.ToByteArray();
            stream.Write(centroidIDBytes, 0, centroidIDBytes.Length);

            return stream.ToArray();
        }
        public new static ClusterPoint FromByteArray(byte[] bytes)
        {
            byte[] guidBytes = new byte[16];
            Array.Copy(bytes, Point.Size, guidBytes, 0, guidBytes.Length);

            Point p = Point.FromByteArray(bytes);

            return new ClusterPoint(p, new Guid(guidBytes));
        }
    }
}
