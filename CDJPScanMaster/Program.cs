using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanMaster.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!File.Exists("database.mem"))
            {
                MessageBox.Show("Could not locate the file database.mem.\n\nPlease copy this file from an existing installation of DRB Emulator into the directory that contains ScanMaster.exe.\n\nScanMaster will now close.", "Error");
                return;
            }
            Application.Run(new Form1());
        }
    }
}
