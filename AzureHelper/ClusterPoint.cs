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

        public ClusterPoint(int x, int y, Guid centroidID)
            : base(x, y)
        {
            this.CentroidID = centroidID;
        }

        public static int Size
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
        public static ClusterPoint FromByteArray(byte[] bytes)
        {
            byte[] guidBytes = new byte[16];
            Array.Copy(bytes, 8, guidBytes, 0, 16);
            
            return new ClusterPoint(
                BitConverter.ToInt32(bytes, 0),
                BitConverter.ToInt32(bytes, 4),
                new Guid(guidBytes));
        }
    }
}
