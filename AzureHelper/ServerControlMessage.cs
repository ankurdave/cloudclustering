using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    [Serializable]
    public class ServerControlMessage : AzureMessage
    {
        public string MachineID { get; set; }

        public ServerControlMessage(string machineID)
        {
            this.MachineID = machineID;
        }
    }
}
