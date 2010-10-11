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
        public int FaultDomain { get; set; }

        public ServerControlMessage(string machineID, int faultDomain)
        {
            this.MachineID = machineID;
            this.FaultDomain = faultDomain;
        }
    }
}
