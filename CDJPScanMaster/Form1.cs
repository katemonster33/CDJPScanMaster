using DRBDB;
using DRBDB.Enums;
using DRBDB.Objects;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CDJPScanMaster
{
    public partial class Form1 : Form
    {
        Module selectedModule = null;
        Database drbdb = new Database();
        System.Windows.Forms.Timer listBoxUpdater = new System.Windows.Forms.Timer();
        TextBoxLogger serialLogger;
        VehicleLinkManager vlm;
        public Form1()
        {
            InitializeComponent();
            foreach(ModuleType type in drbdb.GetModuleTypes())
            {
                listBox1.Items.Add(type);
            }
            listBox1.Tag = drbdb.GetModuleTypes();
            cmbComPorts.Items.AddRange(SerialPort.GetPortNames());
            listBoxUpdater.Interval = 100;
            listBoxUpdater.Tick += ListBoxUpdater_Tick;
            listBoxUpdater.Enabled = true;
            serialLogger = new TextBoxLogger(txtSerialLog);

            vlm = new VehicleLinkManager(serialLogger);
        }

        void ListBoxUpdater_Tick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lstDataMenuTXs.Items)
            {
                if (item.Tag != null && item.Tag.GetType() == typeof(TXItem))
                {
                    TXItem txitem = (TXItem)item.Tag;
                    if (txitem.DataDisplay.IsRawDataUpdated)
                    {
                        item.SubItems[1].Text = txitem.DataDisplay.FormattedData;
                    }
                }
            }
        }

        void LoadModule(Module module)
        {
            selectedModule = module;
            if(selectedModule == null)
            {
                MessageBox.Show("Attempted to load a bad module.");
                return;
            }
            List<Function> modulesFunctions = drbdb.GetModuleFunctionsWithoutTX(selectedModule);
            Dictionary<uint, TXItem> FunctionIDToTXItem = new Dictionary<uint, TXItem>();
            foreach (TXItem item in selectedModule.TXItems.Where(tx => tx.Function != null))
            {
                FunctionIDToTXItem[item.FunctionID] = item;
            }
            listBox1.InvokeIfRequired(() =>
            {
                listBox1.Items.Clear();
                foreach (ModuleMenuItem moduleMenu in drbdb.GetModuleMenuItems())
                {
                    if (moduleMenu.ID == 1 || moduleMenu.ID == 4)
                    {
                        ListView targetBox = (moduleMenu.ID == 1 ? lstTests : lstActuators);
                        foreach (TXItem item in selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID &&
                            item.Function != null && item.Function.LinkedFunctions.Any()))
                        {
                            TestObject test = new TestObject(item, GetFunctionTXChildren(FunctionIDToTXItem, item.Function).ToList());
                            targetBox.Items.Add(test.ToString()).Tag = test;
                        }
                        foreach (Function modMenuFunc in modulesFunctions.Where(func => func.ModuleMenuID == moduleMenu.ID))
                        {
                            TestObject test = new TestObject(modMenuFunc, GetFunctionTXChildren(FunctionIDToTXItem, modMenuFunc).ToList());
                            targetBox.Items.Add(test.ToString()).Tag = test;
                        }
                    }
                    else if(selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID).Any())
                    {
                        lstDataMenus.Items.Add(moduleMenu);
                    }
                }
            });
        }

        IEnumerable<TXItem> GetFunctionTXChildren(Dictionary<uint, TXItem> FunctionIDToTXItem, Function func)
        {
            TXItem linkedItemTemp = null;
            foreach (Function linkedFunction in func.LinkedFunctions)
            {
                if (FunctionIDToTXItem.TryGetValue(linkedFunction.ID, out linkedItemTemp))
                {
                    yield return linkedItemTemp;
                }
            }
        }

        //private string AutodetectArduinoPort()
        //{
        //    ManagementScope connectionScope = new ManagementScope();
        //    SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
        //    ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

        //    try
        //    {
        //        foreach (ManagementObject item in searcher.Get())
        //        {
        //            string desc = item["Description"].ToString();
        //            string deviceId = item["DeviceID"].ToString();

        //            if (desc.Contains("Arduino"))
        //            {
        //                return deviceId;
        //            }
        //        }
        //    }
        //    catch (ManagementException e)
        //    {
        //        /* Do Nothing */
        //    }

        //    return null;
        //}

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1 || listBox1.SelectedItem == null || listBox1.SelectedItem.GetType() != typeof(ModuleType)) return;
            ModuleType type = (ModuleType)listBox1.SelectedItem;
            Task.Factory.StartNew(() => TryConnect(type));
        }

        void TryConnect(ModuleType type)
        {
            Module module = vlm.ScanModuleType(type);
            LoadModule(module);
        }

        private void lstDataMenus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDataMenus.SelectedItem == null) return;
            ModuleMenuItem moduleMenu = (ModuleMenuItem)lstDataMenus.SelectedItem;
            List<TXItem> visibleTxItems = selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID).ToList();
            lstDataMenuTXs.InvokeIfRequired(() =>
            {
                lstDataMenuTXs.Items.Clear();
                
                foreach (TXItem tx in visibleTxItems)
                {
                    ListViewItem newItem = new ListViewItem(tx.Name.ResourceString) { Tag = tx };
                    newItem.SubItems.Add(tx.DataDisplay.FormattedData);
                    lstDataMenuTXs.Items.Add(newItem);
                }
            });

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            vlm.SetQueriedTXItems(new List<TXItem>());
            lstDataMenuTXs.Items.Clear();
        }

        private void btnConnectComPort_Click(object sender, EventArgs e)
        {
            string comPort = (string)cmbComPorts.SelectedItem;
            Task.Run(() => ConnectThread(comPort));
        }
        
        void ConnectThread(string comPort)
        {
            progressBar1.InvokeIfRequired(() =>
            {
                progressBar1.Enabled = true;
                progressBar1.Visible = true;
            });
            bool connected = false;
            lblProgress.InvokeIfRequired(() => lblProgress.Text = "Connecting...");
            connected = vlm.Open(comPort);
            if (!connected)
            {
                lblProgress.InvokeIfRequired(() => lblProgress.Text = "Failed to connect to Arduino.");
            }
            progressBar1.InvokeIfRequired(() =>
            {
                progressBar1.Enabled = false;
                progressBar1.Visible = false;
            });
            if(connected)
            {
                pnlScanMenu.InvokeIfRequired(() => pnlScanMenu.Visible = false);
            }
        }
    }

    public static class Extensions
    {
        public static uint TestModuleQualifiers(this Module module, string engineSize, string year, string bodyStyle, string ecmStyle)
        {
            if (module.Name == null) return 0;
            string modName = module.Name.ResourceString.
            Replace('(', ' ').
            Replace(')', ' ').
            Replace('/', ' ').
            Replace("SBEC3", "SBECIII").
            Replace("SBEC 3A", "SBECIIIA").
            Replace("Prowler", "PR").
            Replace("Dakota", "AN").
            Replace("Viper", "SR").
            Replace("FOUR-CYL", "2.5");
            uint count = 0;
            List<string> engineSizes = new List<string>();
            List<string> bodyStyles = new List<string>();
            foreach (string token in modName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 3 && token[1] == '.')
                {
                    engineSizes.Add(token);
                }
                else if (token.Length == 2 && char.IsLetter(token[0]) && char.IsLetter(token[1]))
                {
                    bodyStyles.Add(token);
                }
                else if (token.Length == 2 && char.IsDigit(token[0]) && char.IsDigit(token[1]))
                {
                    if (token[0] != '9' && token[0] != '0') //engine size
                    {
                        engineSizes.Add(token[0] + "." + token[1]);
                    }
                    else if (year.Substring(2) == token)
                    {
                        count++;
                    }
                }
                else if (token.Contains("SBEC") || token.Contains("JTEC") || token.Contains("Siemens"))
                {
                    if (ecmStyle == token) count++;
                }
            }
            if (modName.Contains("18PL"))
            {
                engineSizes.Add("1.8");
                bodyStyles.Add("PL");
            }
            if (engineSizes.Count == 0)
            {
                if (modName.Contains("V6") && engineSize.Contains("V6")) count++;
                if (modName.Contains("V10") && engineSize.Contains("V10")) count++;
            }
            if (engineSizes.Any())
            {
                bool anyEnginesMatched = false;
                foreach (string engineSizeComp in engineSizes)
                {
                    if (engineSizeComp == "5.9")
                    {
                        if (modName.Contains("DIESEL"))
                        {
                            if (engineSize == "5.9L I6") anyEnginesMatched = true;
                        }
                        else if (engineSize == "5.9L V8") anyEnginesMatched = true;
                    }
                    else
                    {
                        if (engineSize.StartsWith(engineSizeComp))
                        {
                            if (modName.Contains("V6"))
                            {
                                if (engineSize.Contains("V6")) anyEnginesMatched = true;
                            }
                            else if (modName.Contains("V10"))
                            {
                                if (engineSize.Contains("V10")) anyEnginesMatched = true;
                            }
                            else
                            {
                                anyEnginesMatched = true;
                            }
                        }
                    }
                }
                if (anyEnginesMatched) count++;
                else return 0;
            }
            if (bodyStyles.Any())
            {
                bool anyBodyMatched = false;
                foreach (string bodyStyleComp in bodyStyles)
                {
                    if (bodyStyle == bodyStyleComp.ToUpper()) anyBodyMatched = true;
                }
                if (anyBodyMatched) count++;
                else return 0;
            }
            return count;
        }
    }
}
