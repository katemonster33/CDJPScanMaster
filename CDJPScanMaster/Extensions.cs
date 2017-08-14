using ScanMaster.Database.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanMaster.UI
{

    public static class Extensions
    {
        public static void InvokeIfRequired(this Control ctrl, Action act)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(act);
            else act();
        }
    }
}
