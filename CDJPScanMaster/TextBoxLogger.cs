using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CDJPScanMaster
{
    public class TextBoxLogger : IDisposable
    {
        TextBox textBox;
        Timer textBoxUpdateTimer;
        string pendingData = string.Empty;
        public TextBoxLogger(TextBox targetBox)
        {
            this.textBox = targetBox;
            textBoxUpdateTimer = new Timer();
            textBoxUpdateTimer.Interval = 25;
            textBoxUpdateTimer.Tick += TextBoxUpdateTimer_Tick;
            textBoxUpdateTimer.Enabled = true;
        }

        public void Dispose()
        {
            textBoxUpdateTimer.Dispose();
        }

        public void WriteLine(string message)
        {
            lock (pendingData)
            {
                pendingData += message + "\r\n";
            }
        }

        private void TextBoxUpdateTimer_Tick(object sender, EventArgs e)
        {
            lock(pendingData)
            {
                if(!string.IsNullOrEmpty(pendingData))
                {
                    textBox.AppendText(pendingData);
                    pendingData = string.Empty;
                }
            }
        }

        void UpdateThread()
        {

        }
    }
}
