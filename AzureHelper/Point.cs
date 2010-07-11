﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point()
        {
            X = Y = 0;
        }

        public Point(float x, float y)
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

        public static Point operator /(Point p, float a)
        {
            return new Point(
                p.X / a,
                p.Y / a);
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
            stream.Write(BitConverter.GetBytes(X), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(Y), 0, sizeof(float));

            return stream.ToArray();
        }
        public static Point FromByteArray(byte[] bytes)
        {
            return new Point(
                BitConverter.ToSingle(bytes, 0),
                BitConverter.ToSingle(bytes, sizeof(float)));
        }

        public static float Distance(Point p1, Point p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }

    public static class PointExtension
    {
        public static T MinElement<T>(this IEnumerable<T> list, Func<T, float> comparer)
        {
            T minElement = default(T);
            float minValue = float.MaxValue;
            foreach (T element in list)
            {
                float value = comparer.Invoke(element);

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
