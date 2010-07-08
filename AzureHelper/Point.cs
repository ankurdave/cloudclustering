using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point()
        {
            X = Y = 0;
        }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(
                p1.X + p2.X,
                p1.Y + p2.Y);
        }

        public static int Size
        {
            get
            {
                return sizeof(int) * 2;
            }
        }
        
        public virtual byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream(Size);
            stream.Write(BitConverter.GetBytes(X), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(Y), 0, sizeof(int));

            return stream.ToArray();
        }
        public static Point FromByteArray(byte[] bytes)
        {
            return new Point(
                BitConverter.ToInt32(bytes, 0),
                BitConverter.ToInt32(bytes, 4));
        }
    }
}
