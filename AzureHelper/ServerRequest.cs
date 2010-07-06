using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class ServerRequest : IAzureMessage
    {
        public Guid JobID { get; set; }
        public int N { get; set; }
        public int K { get; set; }
        public int M { get; set; }
    }
}
