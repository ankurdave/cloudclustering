using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    public class Centroid : Point
    {
        public Guid ID { get; set; }

        public Centroid(Guid id, int x, int y) : base(x, y)
        {
            this.ID = id;
        }

        public static int Size
        {
            get
            {
                return Guid.Empty.ToByteArray().Length + Point.Size;
            }
        }

        public override byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream(Size);
            
            byte[] idBytes = ID.ToByteArray();
            stream.Write(idBytes, 0, idBytes.Length);

            byte[] pointBytes = base.ToByteArray();
            stream.Write(pointBytes, 0, pointBytes.Length);

            return stream.ToArray();
        }
        public static Centroid FromByteArray(byte[] bytes)
        {
            byte[] guidBytes = new byte[16];
            Array.Copy(bytes, 0, guidBytes, 0, 16);

            return new Centroid(
                new Guid(guidBytes),
                BitConverter.ToInt32(bytes, 16),
                BitConverter.ToInt32(bytes, 20));
        }
    }
}
