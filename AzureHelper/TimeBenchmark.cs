using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class TimeBenchmark
    {
        public string MethodName { get; set; }
        public List<TimeSpan> ExecutionTimes { get; set; }

        public TimeBenchmark(string methodName)
        {
            this.MethodName = methodName;

            this.ExecutionTimes = new List<TimeSpan>();
        }
    }
}
