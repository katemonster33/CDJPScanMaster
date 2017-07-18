/*
 * DRBDBReader
 * Copyright (C) 2016-2017, Kyle Repinski, Katie McKean
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using DRBDB.Objects;
using System.Diagnostics;
using DRBDB.Enums;

namespace DRBDB
{
	public partial class frmMain : Form
	{
		FileInfo fi = new FileInfo( "database.mem" );
		Database db;
		List<string> cmdHistory = new List<string>();
		int cmdIdx = 0;

		public frmMain()
		{
			InitializeComponent();
			this.cmdHistory.Add( "" );
			this.cmdIdx = 0;
		}

		void checkDB()
		{
			if( this.db == null )
			{
                try
                {
                    Console.WriteLine("Loading database, please wait...");
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    this.db = new Database(new FileInfo("database.mem"));
                    // manually append text to avoid the automatic newline preceeding it
                    Console.WriteLine(" ...done! took " + sw.ElapsedMilliseconds + " ms" + Environment.NewLine);
                    sw.Stop();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("database.mem not found. Please place database.mem in the Binary folder to use DRBDBReader.");
                }
			}
		}

        void OpenDatabase()
        {
            this.checkDB();
            if (db != null)
            {
                List<TreeNode> nodesToAdd = new List<TreeNode>();
                foreach(ModuleType type in db.GetModuleTypes())
                {
                    TreeNode typeNode = new TreeNode(type.TypeID + " | " + type.Name.ResourceString);
                    typeNode.Tag = type;
                    foreach (Module mod in db.GetModules().Where(m => m.ModuleType == type))
                    {
                        List<Function> modulesFunctions = db.GetModuleFunctionsWithoutTX(mod);
                        Dictionary<uint, TXItem> FunctionIDToTXItem = new Dictionary<uint, TXItem>();
                        foreach (TXItem item in mod.TXItems.Where(tx => tx.Function != null))
                        {
                            FunctionIDToTXItem[item.FunctionID] = item;
                        }
                        IEnumerable<string> moduleProtocols = mod.TXItems.Select(tx => tx.DataAcquisitionMethod.Protocol).Distinct().Select(prot => prot.ToString());
                        string moduleName = mod.ID + " | " + mod.Name.ResourceString + " | " + mod.ModuleType.Name.ResourceString + " | " + string.Join(",", moduleProtocols);
                        TreeNode modNode = new TreeNode(moduleName);
                        modNode.Tag = mod;
                        foreach (ModuleMenuItem moduleMenu in db.GetModuleMenusWithChildren(mod))
                        {
                            TreeNode modMenuNode = new TreeNode(moduleMenu.ID + " | " + moduleMenu.Name.ResourceString);
                            modNode.Nodes.Add(modMenuNode);
                            foreach (TXItem item in mod.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID))
                            {
                                TreeNode txNode = new TreeNode(item.ID + " | " + item.Name.ResourceString + " | " + BitConverter.ToString(item.TransmitBytes)) { Tag = item };
                                modMenuNode.Nodes.Add(txNode);
                                if (item.Function != null && item.Function.LinkedFunctions.Any() && (moduleMenu.ID == 1 || moduleMenu.ID == 4))
                                {
                                    AddLinkedTXNodes(txNode, FunctionIDToTXItem, item.Function);
                                }
                            }
                            if (moduleMenu.ID == 1 || moduleMenu.ID == 4)
                            {
                                foreach (Function modMenuFunc in modulesFunctions.Where(func => func.ModuleMenuID == moduleMenu.ID))
                                {
                                    TreeNode funcNode = new TreeNode(modMenuFunc.ID + " | " + modMenuFunc.Name.ResourceString + " | " + modMenuFunc.Order) { Tag = modMenuFunc };
                                    modMenuNode.Nodes.Add(funcNode);
                                    if (moduleMenu.ID == 1 || moduleMenu.ID == 4)
                                    {
                                        AddLinkedTXNodes(funcNode, FunctionIDToTXItem, modMenuFunc);
                                    }
                                }
                            }
                        }
                        foreach (DataMenuItem dataMenu in mod.DataMenuItems)
                        {
                            TreeNode dataMenuNode = new TreeNode(dataMenu.Name.ResourceString);
                            foreach (TXItem item in dataMenu.TXGroups.SelectMany(grp => grp.TXItems).Intersect(mod.TXItems))
                            {
                                TreeNode txNode = new TreeNode(item.ID + " | " + item.Name.ResourceString) { Tag = item };
                                dataMenuNode.Nodes.Add(txNode);
                            }
                            if (dataMenuNode.Nodes.Count > 0)
                            {
                                modNode.Nodes.Add(dataMenuNode);
                            }
                        }
                        typeNode.Nodes.Add(modNode);
                    }
                    nodesToAdd.Add(typeNode);
                }
                tvMain.InvokeIfRequired(() =>
                {
                    tvMain.BeginUpdate();
                    tvMain.Nodes.AddRange(nodesToAdd.ToArray());
                    tvMain.EndUpdate();
                });
                SetDataGridColumns("ID", "XMIT", "Name", "Scaler");
                foreach(TXItem item in db.GetTXItems())
                {
                    string scaling = item.ConversionScalingType.ToString() + " | " + item.ConversionScalingID + " | ";
                    if(item.DataScaler == null)
                    {
                        scaling += "(NULL)";
                    }
                    else
                    {
                        scaling += item.DataScaler.GetType().Name;
                    }
                    dgvIdk.Rows.Add(item.ID, BitConverter.ToString(item.TransmitBytes), item.Name.ResourceString, scaling);
                }
            }
        }

        void AddLinkedTXNodes(TreeNode parentNode, Dictionary<uint, TXItem> FunctionIDToTXItem, Function func)
        {
            TXItem linkedItemTemp = null;
            foreach (Function linkedFunction in func.LinkedFunctions)
            {
                TreeNode linkedNode;
                if (FunctionIDToTXItem.TryGetValue(linkedFunction.ID, out linkedItemTemp))
                {
                    linkedNode = new TreeNode(linkedItemTemp.ID + " | " + linkedItemTemp.Name.ResourceString + " | " + BitConverter.ToString(linkedItemTemp.TransmitBytes))
                    {
                        Tag = linkedItemTemp
                    };
                    parentNode.Nodes.Add(linkedNode);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    this.fi = new FileInfo(ofd.FileName);
                    OpenDatabase();
                }
            }
        }

        void SetDataGridColumns(params string[] names)
        {
            dgvIdk.Columns.Clear();
            foreach(string name in names)
            {
                DataGridViewColumn column = new DataGridViewColumn();
                column.HeaderText = column.ToolTipText = name;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                column.CellTemplate = new DataGridViewTextBoxCell();
                column.Resizable = DataGridViewTriState.True;
                column.SortMode = DataGridViewColumnSortMode.Automatic;
                dgvIdk.Columns.Add(column);
            }
        }

        void tvMain_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag == null) return;
            if (e.Node.Tag.GetType() == typeof(TXItem))
            {
                TXItem nodeTag = (TXItem)e.Node.Tag;
                for (int i = 0; i < dgvIdk.RowCount; i++)
                {
                    if(((uint)dgvIdk[0, i].Value) == nodeTag.ID)
                    {
                        dgvIdk.InvokeIfRequired(() =>
                        {
                            dgvIdk.ClearSelection();
                            dgvIdk.Rows[i].Selected = true;
                            dgvIdk.Rows[i].Visible = true;
                            dgvIdk.FirstDisplayedScrollingRowIndex = i;
                        });
                        return;
                    }
                }
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            OpenDatabase();
        }
    }

    public static class InvokeHelper
    {
        public static void InvokeIfRequired(this Control ctrl, Action act)
        {
            if(ctrl.InvokeRequired)
            {
                ctrl.Invoke(act);
            }
            else
            {
                act();
            }
        }
    }
}
