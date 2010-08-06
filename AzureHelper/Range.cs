using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public struct Range<T>
    {
        public T Start { get; set; }
        public T End { get; set; }

        public Range(T start, T end)
            : this()
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Start, End);
        }
    }
}
