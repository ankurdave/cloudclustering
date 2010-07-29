using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class KMeansTask
    {
        public KMeansTaskData TaskData { get; set; }
        public bool Running { get; set; }

        public KMeansTask(KMeansTaskData taskData, bool running = true)
        {
            this.TaskData = taskData;
            this.Running = running;
        }
    }
}
