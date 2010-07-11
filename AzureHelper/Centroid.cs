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

        public Centroid() : base() { }

        public Centroid(Guid id, float x, float y) : base(x, y)
        {
            this.ID = id;
        }

        public Centroid(Guid id, Point p)
            : base(p)
        {
            this.ID = id;
        }

        public new static int Size
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
        public new static Centroid FromByteArray(byte[] bytes)
        {
            byte[] guidBytes = new byte[16];
            Array.Copy(bytes, 0, guidBytes, 0, guidBytes.Length);

            byte[] pointBytes = new byte[Point.Size];
            Array.Copy(bytes, 16, pointBytes, 0, pointBytes.Length);
            Point p = Point.FromByteArray(pointBytes);

            return new Centroid(new Guid(guidBytes), p);
        }

        public static List<Centroid> ListFromByteStream(Stream stream)
        {
            List<Centroid> centroids = new List<Centroid>();
            
            byte[] centroidBytes = new byte[Centroid.Size];
            while (stream.Position + Centroid.Size <= stream.Length)
            {
                stream.Read(centroidBytes, 0, Centroid.Size);
                Centroid c = Centroid.FromByteArray(centroidBytes);
                centroids.Add(c);
            }

            return centroids;
        }
    }
}
