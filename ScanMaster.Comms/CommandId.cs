using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    public enum CommandID
    {
        None = 0,
        Mux_SetSCIAEngine,
        Mux_SetSCIATrans,
        Mux_SetSCIBEngine,
        Mux_SetSCIBTrans,
        Mux_SetISO9141,
        SCI_SetHiSpeed,
        SCI_SetLoSpeed,
        ISO9141_5BaudInit
    }
}
