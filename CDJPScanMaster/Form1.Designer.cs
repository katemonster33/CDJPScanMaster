namespace ScanMaster.UI
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if(vlm != null)
            {
                vlm.Dispose();
            }
            listBoxUpdater.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.txtSerialLog = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabModules = new System.Windows.Forms.TabPage();
            this.pnlScanMenu = new System.Windows.Forms.Panel();
            this.lblProgress = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblComPort = new System.Windows.Forms.Label();
            this.btnConnectComPort = new System.Windows.Forms.Button();
            this.cmbComPorts = new System.Windows.Forms.ComboBox();
            this.tabFaults = new System.Windows.Forms.TabPage();
            this.tabDataLists = new System.Windows.Forms.TabPage();
            this.lstDataMenuTXs = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lstDataMenus = new System.Windows.Forms.ListBox();
            this.tabSystemTests = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lstTests = new System.Windows.Forms.ListView();
            this.tabActuators = new System.Windows.Forms.TabPage();
            this.lstActuatorTXs = new System.Windows.Forms.ListView();
            this.lstActuators = new System.Windows.Forms.ListView();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.btnSendMsg = new System.Windows.Forms.Button();
            this.txtMsgToSend = new System.Windows.Forms.TextBox();
            this.btnSetMux = new System.Windows.Forms.Button();
            this.cmbMuxSel = new System.Windows.Forms.ComboBox();
            this.cmbProtocol = new System.Windows.Forms.ComboBox();
            this.tabControl1.SuspendLayout();
            this.tabModules.SuspendLayout();
            this.pnlScanMenu.SuspendLayout();
            this.tabDataLists.SuspendLayout();
            this.tabSystemTests.SuspendLayout();
            this.tabActuators.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.Font = new System.Drawing.Font("Arial Narrow", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 20;
            this.listBox1.Location = new System.Drawing.Point(3, 3);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(569, 357);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // txtSerialLog
            // 
            this.txtSerialLog.Location = new System.Drawing.Point(0, 44);
            this.txtSerialLog.Multiline = true;
            this.txtSerialLog.Name = "txtSerialLog";
            this.txtSerialLog.Size = new System.Drawing.Size(575, 319);
            this.txtSerialLog.TabIndex = 2;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabModules);
            this.tabControl1.Controls.Add(this.tabFaults);
            this.tabControl1.Controls.Add(this.tabDataLists);
            this.tabControl1.Controls.Add(this.tabSystemTests);
            this.tabControl1.Controls.Add(this.tabActuators);
            this.tabControl1.Controls.Add(this.tabLog);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(583, 389);
            this.tabControl1.TabIndex = 3;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabModules
            // 
            this.tabModules.Controls.Add(this.pnlScanMenu);
            this.tabModules.Controls.Add(this.listBox1);
            this.tabModules.Location = new System.Drawing.Point(4, 22);
            this.tabModules.Name = "tabModules";
            this.tabModules.Padding = new System.Windows.Forms.Padding(3);
            this.tabModules.Size = new System.Drawing.Size(575, 363);
            this.tabModules.TabIndex = 0;
            this.tabModules.Text = "Module Selection";
            this.tabModules.UseVisualStyleBackColor = true;
            // 
            // pnlScanMenu
            // 
            this.pnlScanMenu.Controls.Add(this.lblProgress);
            this.pnlScanMenu.Controls.Add(this.progressBar1);
            this.pnlScanMenu.Controls.Add(this.lblComPort);
            this.pnlScanMenu.Controls.Add(this.btnConnectComPort);
            this.pnlScanMenu.Controls.Add(this.cmbComPorts);
            this.pnlScanMenu.Location = new System.Drawing.Point(145, 176);
            this.pnlScanMenu.Name = "pnlScanMenu";
            this.pnlScanMenu.Size = new System.Drawing.Size(583, 389);
            this.pnlScanMenu.TabIndex = 1;
            // 
            // lblProgress
            // 
            this.lblProgress.Location = new System.Drawing.Point(289, 202);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(100, 23);
            this.lblProgress.TabIndex = 4;
            this.lblProgress.Text = "Select a COM Port.";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            this.progressBar1.Enabled = false;
            this.progressBar1.Location = new System.Drawing.Point(128, 202);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(155, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 3;
            this.progressBar1.Visible = false;
            // 
            // lblComPort
            // 
            this.lblComPort.AutoSize = true;
            this.lblComPort.Location = new System.Drawing.Point(125, 151);
            this.lblComPort.Name = "lblComPort";
            this.lblComPort.Size = new System.Drawing.Size(56, 13);
            this.lblComPort.TabIndex = 2;
            this.lblComPort.Text = "COM Port:";
            // 
            // btnConnectComPort
            // 
            this.btnConnectComPort.Location = new System.Drawing.Point(314, 146);
            this.btnConnectComPort.Name = "btnConnectComPort";
            this.btnConnectComPort.Size = new System.Drawing.Size(75, 23);
            this.btnConnectComPort.TabIndex = 1;
            this.btnConnectComPort.Text = "Connect";
            this.btnConnectComPort.UseVisualStyleBackColor = true;
            this.btnConnectComPort.Click += new System.EventHandler(this.btnConnectComPort_Click);
            // 
            // cmbComPorts
            // 
            this.cmbComPorts.FormattingEnabled = true;
            this.cmbComPorts.Location = new System.Drawing.Point(187, 148);
            this.cmbComPorts.Name = "cmbComPorts";
            this.cmbComPorts.Size = new System.Drawing.Size(121, 21);
            this.cmbComPorts.TabIndex = 0;
            // 
            // tabFaults
            // 
            this.tabFaults.Location = new System.Drawing.Point(4, 22);
            this.tabFaults.Name = "tabFaults";
            this.tabFaults.Padding = new System.Windows.Forms.Padding(3);
            this.tabFaults.Size = new System.Drawing.Size(575, 363);
            this.tabFaults.TabIndex = 1;
            this.tabFaults.Text = "Faults";
            this.tabFaults.UseVisualStyleBackColor = true;
            // 
            // tabDataLists
            // 
            this.tabDataLists.Controls.Add(this.lstDataMenuTXs);
            this.tabDataLists.Controls.Add(this.lstDataMenus);
            this.tabDataLists.Location = new System.Drawing.Point(4, 22);
            this.tabDataLists.Name = "tabDataLists";
            this.tabDataLists.Size = new System.Drawing.Size(575, 363);
            this.tabDataLists.TabIndex = 2;
            this.tabDataLists.Text = "Data Lists";
            this.tabDataLists.UseVisualStyleBackColor = true;
            // 
            // lstDataMenuTXs
            // 
            this.lstDataMenuTXs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lstDataMenuTXs.Location = new System.Drawing.Point(197, 3);
            this.lstDataMenuTXs.MultiSelect = false;
            this.lstDataMenuTXs.Name = "lstDataMenuTXs";
            this.lstDataMenuTXs.ShowGroups = false;
            this.lstDataMenuTXs.Size = new System.Drawing.Size(375, 355);
            this.lstDataMenuTXs.TabIndex = 1;
            this.lstDataMenuTXs.UseCompatibleStateImageBehavior = false;
            this.lstDataMenuTXs.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Description";
            this.columnHeader1.Width = 264;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Value";
            this.columnHeader2.Width = 99;
            // 
            // lstDataMenus
            // 
            this.lstDataMenus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstDataMenus.FormattingEnabled = true;
            this.lstDataMenus.Location = new System.Drawing.Point(3, 3);
            this.lstDataMenus.Name = "lstDataMenus";
            this.lstDataMenus.Size = new System.Drawing.Size(187, 355);
            this.lstDataMenus.TabIndex = 0;
            this.lstDataMenus.SelectedIndexChanged += new System.EventHandler(this.lstDataMenus_SelectedIndexChanged);
            // 
            // tabSystemTests
            // 
            this.tabSystemTests.Controls.Add(this.panel1);
            this.tabSystemTests.Controls.Add(this.lstTests);
            this.tabSystemTests.Location = new System.Drawing.Point(4, 22);
            this.tabSystemTests.Name = "tabSystemTests";
            this.tabSystemTests.Size = new System.Drawing.Size(575, 363);
            this.tabSystemTests.TabIndex = 3;
            this.tabSystemTests.Text = "System Tests";
            this.tabSystemTests.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Location = new System.Drawing.Point(196, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(376, 356);
            this.panel1.TabIndex = 1;
            // 
            // lstTests
            // 
            this.lstTests.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstTests.Location = new System.Drawing.Point(3, 3);
            this.lstTests.Name = "lstTests";
            this.lstTests.Size = new System.Drawing.Size(187, 355);
            this.lstTests.TabIndex = 0;
            this.lstTests.UseCompatibleStateImageBehavior = false;
            // 
            // tabActuators
            // 
            this.tabActuators.Controls.Add(this.lstActuatorTXs);
            this.tabActuators.Controls.Add(this.lstActuators);
            this.tabActuators.Location = new System.Drawing.Point(4, 22);
            this.tabActuators.Name = "tabActuators";
            this.tabActuators.Size = new System.Drawing.Size(575, 363);
            this.tabActuators.TabIndex = 5;
            this.tabActuators.Text = "Actuators";
            this.tabActuators.UseVisualStyleBackColor = true;
            // 
            // lstActuatorTXs
            // 
            this.lstActuatorTXs.Location = new System.Drawing.Point(197, 4);
            this.lstActuatorTXs.Name = "lstActuatorTXs";
            this.lstActuatorTXs.Size = new System.Drawing.Size(375, 354);
            this.lstActuatorTXs.TabIndex = 1;
            this.lstActuatorTXs.UseCompatibleStateImageBehavior = false;
            // 
            // lstActuators
            // 
            this.lstActuators.Location = new System.Drawing.Point(3, 3);
            this.lstActuators.Name = "lstActuators";
            this.lstActuators.Size = new System.Drawing.Size(187, 355);
            this.lstActuators.TabIndex = 0;
            this.lstActuators.UseCompatibleStateImageBehavior = false;
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.cmbProtocol);
            this.tabLog.Controls.Add(this.btnSendMsg);
            this.tabLog.Controls.Add(this.txtMsgToSend);
            this.tabLog.Controls.Add(this.btnSetMux);
            this.tabLog.Controls.Add(this.cmbMuxSel);
            this.tabLog.Controls.Add(this.txtSerialLog);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Size = new System.Drawing.Size(575, 363);
            this.tabLog.TabIndex = 4;
            this.tabLog.Text = "Serial Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // btnSendMsg
            // 
            this.btnSendMsg.Location = new System.Drawing.Point(492, 4);
            this.btnSendMsg.Name = "btnSendMsg";
            this.btnSendMsg.Size = new System.Drawing.Size(75, 23);
            this.btnSendMsg.TabIndex = 6;
            this.btnSendMsg.Text = "Send";
            this.btnSendMsg.UseVisualStyleBackColor = true;
            this.btnSendMsg.Click += new System.EventHandler(this.btnSendMsg_Click);
            // 
            // txtMsgToSend
            // 
            this.txtMsgToSend.Location = new System.Drawing.Point(328, 6);
            this.txtMsgToSend.Name = "txtMsgToSend";
            this.txtMsgToSend.Size = new System.Drawing.Size(158, 20);
            this.txtMsgToSend.TabIndex = 5;
            // 
            // btnSetMux
            // 
            this.btnSetMux.Location = new System.Drawing.Point(136, 4);
            this.btnSetMux.Name = "btnSetMux";
            this.btnSetMux.Size = new System.Drawing.Size(100, 23);
            this.btnSetMux.TabIndex = 4;
            this.btnSetMux.Text = "Set Mux State";
            this.btnSetMux.UseVisualStyleBackColor = true;
            this.btnSetMux.Click += new System.EventHandler(this.btnSetMux_Click);
            // 
            // cmbMuxSel
            // 
            this.cmbMuxSel.FormattingEnabled = true;
            this.cmbMuxSel.Items.AddRange(new object[] {
            "None",
            "SCI A Engine",
            "SCI A Trans",
            "SCI B Engine",
            "SCI B Trans",
            "ISO 9141"});
            this.cmbMuxSel.Location = new System.Drawing.Point(9, 6);
            this.cmbMuxSel.Name = "cmbMuxSel";
            this.cmbMuxSel.Size = new System.Drawing.Size(121, 21);
            this.cmbMuxSel.TabIndex = 3;
            // 
            // cmbProtocol
            // 
            this.cmbProtocol.FormattingEnabled = true;
            this.cmbProtocol.Items.AddRange(new object[] {
            "SCI",
            "J1850",
            "CCD",
            "ISO9141"});
            this.cmbProtocol.Location = new System.Drawing.Point(259, 6);
            this.cmbProtocol.Name = "cmbProtocol";
            this.cmbProtocol.Size = new System.Drawing.Size(63, 21);
            this.cmbProtocol.TabIndex = 7;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(583, 389);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.ShowIcon = false;
            this.Text = "Chrysler Scan Master";
            this.tabControl1.ResumeLayout(false);
            this.tabModules.ResumeLayout(false);
            this.pnlScanMenu.ResumeLayout(false);
            this.pnlScanMenu.PerformLayout();
            this.tabDataLists.ResumeLayout(false);
            this.tabSystemTests.ResumeLayout(false);
            this.tabActuators.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox txtSerialLog;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabModules;
        private System.Windows.Forms.TabPage tabFaults;
        private System.Windows.Forms.TabPage tabDataLists;
        private System.Windows.Forms.ListBox lstDataMenus;
        private System.Windows.Forms.TabPage tabSystemTests;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListView lstTests;
        private System.Windows.Forms.TabPage tabActuators;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.ListView lstActuators;
        private System.Windows.Forms.ListView lstActuatorTXs;
        private System.Windows.Forms.ListView lstDataMenuTXs;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Panel pnlScanMenu;
        private System.Windows.Forms.Label lblComPort;
        private System.Windows.Forms.Button btnConnectComPort;
        private System.Windows.Forms.ComboBox cmbComPorts;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnSendMsg;
        private System.Windows.Forms.TextBox txtMsgToSend;
        private System.Windows.Forms.Button btnSetMux;
        private System.Windows.Forms.ComboBox cmbMuxSel;
        private System.Windows.Forms.ComboBox cmbProtocol;
    }
}

