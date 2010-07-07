using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class Point
    {
        private int _x;
        private int _y;

        public int X
        {
            get
            {
                return _x;
            }
        }
        public int Y
        {
            get
            {
                return _y;
            }
        }

        public Point(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(
                p1.X + p2.X,
                p1.Y + p2.Y);
        }
    }
}
