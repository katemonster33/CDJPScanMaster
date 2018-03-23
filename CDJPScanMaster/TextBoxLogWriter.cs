using ScanMaster.Comms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanMaster.UI
{
    internal class TextBoxLogWriter : LogWriter
    {
        TextBox target;

        public TextBoxLogWriter(TextBox target)
        {
            this.target = target;
        }

        public void WriteLine(string args)
        {
            target.BeginInvoke((Action)(() => target.AppendText(args + "\n")));
        }
    }
}
