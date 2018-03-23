using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    public interface LogWriter
    {
        void WriteLine(string args);
    }
}
