using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    [Serializable]
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point()
        {
            X = Y = 0;
        }

        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point(Point p)
        {
            X = p.X;
            Y = p.Y;
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(
                p1.X + p2.X,
                p1.Y + p2.Y);
        }

        public static Point operator /(Point p, double a)
        {
            return new Point(
                p.X / a,
                p.Y / a);
        }

        public static int Size
        {
            get
            {
                return sizeof(double) * 2;
            }
        }
        
        public virtual byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream(Size);
            stream.Write(BitConverter.GetBytes(X), 0, sizeof(double));
            stream.Write(BitConverter.GetBytes(Y), 0, sizeof(double));

            return stream.ToArray();
        }
        public static Point FromByteArray(byte[] bytes)
        {
            return new Point(
                BitConverter.ToDouble(bytes, 0),
                BitConverter.ToDouble(bytes, sizeof(double)));
        }

        public static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }

    public static class PointExtension
    {
        public static T MinElement<T>(this IEnumerable<T> list, Func<T, double> comparer)
        {
            T minElement = default(T);
            double minValue = double.MaxValue;
            foreach (T element in list)
            {
                double value = comparer.Invoke(element);

                if (value < minValue)
                {
                    minElement = element;
                    minValue = value;
                }
            }

            return minElement;
        }
    }
}
