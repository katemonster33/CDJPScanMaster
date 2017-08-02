namespace CDJPScanMaster
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
            StopQuerying();
            arduino.Dispose();
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
            this.tabControl1.SuspendLayout();
            this.tabModules.SuspendLayout();
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
            this.txtSerialLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSerialLog.Location = new System.Drawing.Point(0, 0);
            this.txtSerialLog.Multiline = true;
            this.txtSerialLog.Name = "txtSerialLog";
            this.txtSerialLog.Size = new System.Drawing.Size(575, 363);
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
            this.tabModules.Controls.Add(this.listBox1);
            this.tabModules.Location = new System.Drawing.Point(4, 22);
            this.tabModules.Name = "tabModules";
            this.tabModules.Padding = new System.Windows.Forms.Padding(3);
            this.tabModules.Size = new System.Drawing.Size(575, 363);
            this.tabModules.TabIndex = 0;
            this.tabModules.Text = "Module Selection";
            this.tabModules.UseVisualStyleBackColor = true;
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
            this.tabLog.Controls.Add(this.txtSerialLog);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Size = new System.Drawing.Size(575, 363);
            this.tabLog.TabIndex = 4;
            this.tabLog.Text = "Serial Log";
            this.tabLog.UseVisualStyleBackColor = true;
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
    }
}

