using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Database.Enums
{
    public enum Protocol
    {
        Invalid = 0,
        J1850 = 1,
        CCD = 53,
        CCD_2 = 103,
        SCI = 60,
        KWP2000 = 155,
        TPM = 158,
        Multimeter = 159,
        SCI_NGC = 160
    }
}
