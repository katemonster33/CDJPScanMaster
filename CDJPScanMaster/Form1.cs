using ScanMaster.Database;
using ScanMaster.Database.Enums;
using ScanMaster.Database.Objects;
using ScanMaster.Comms;
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

namespace ScanMaster.UI
{
    public partial class Form1 : Form
    {
        Module selectedModule = null;
        System.Windows.Forms.Timer listBoxUpdater = new System.Windows.Forms.Timer();
        VehicleLinkManager vlm;
        public Form1()
        {
            InitializeComponent();
            vlm = new VehicleLinkManager();
            foreach (ModuleType type in vlm.GetDatabase().GetModuleTypes())
            {
                listBox1.Items.Add(type);
            }
            listBox1.Tag = vlm.GetDatabase().GetModuleTypes();
            cmbComPorts.Items.AddRange(SerialPort.GetPortNames());
            listBoxUpdater.Interval = 100;
            listBoxUpdater.Tick += ListBoxUpdater_Tick;
            listBoxUpdater.Enabled = true;
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
            List<Function> modulesFunctions = vlm.GetDatabase().GetModuleFunctionsWithoutTX(selectedModule);
            Dictionary<uint, TXItem> FunctionIDToTXItem = new Dictionary<uint, TXItem>();
            foreach (TXItem item in selectedModule.TXItems.Where(tx => tx.Function != null))
            {
                FunctionIDToTXItem[item.FunctionID] = item;
            }
            listBox1.InvokeIfRequired(() =>
            {
                listBox1.Items.Clear();
                foreach (ModuleMenuItem moduleMenu in vlm.GetDatabase().GetModuleMenuItems())
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

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1 || listBox1.SelectedItem == null || listBox1.SelectedItem.GetType() != typeof(ModuleType)) return;
            ModuleType type = (ModuleType)listBox1.SelectedItem;
            Task.Factory.StartNew(() => TryConnect(type));
        }

        void TryConnect(ModuleType type)
        {
            Module module = vlm.ScanModuleType(type);
            if (module != null)
            {
                LoadModule(module);
            }
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
            lblProgress.InvokeIfRequired(() => lblProgress.Text = "Connecting...");
            progressBar1.InvokeIfRequired(() =>
            {
                progressBar1.Enabled = false;
                progressBar1.Visible = false;
            });
            if (!vlm.OpenComms(comPort))
            {
                lblProgress.InvokeIfRequired(() => lblProgress.Text = "Failed to connect to Arduino.");
            }
            else
            {
                pnlScanMenu.InvokeIfRequired(() => pnlScanMenu.Visible = false);
            }
        }

        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            string[] msgTokens = txtMsgToSend.Text.Split(' ');
            List<byte> messageBytes = new List<byte>();
            foreach(string tok in msgTokens)
            {
                messageBytes.Add(Convert.ToByte(tok, 16));
            }
            //vlm.SendMessage(messageBytes);
        }

        private void btnSetMux_Click(object sender, EventArgs e)
        {
            //vlm.ConnectChannel(chan);
        }
    }
}
