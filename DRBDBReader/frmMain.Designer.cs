namespace DRBDB
{
	partial class frmMain
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.spltLeftRight = new System.Windows.Forms.SplitContainer();
            this.tvMain = new System.Windows.Forms.TreeView();
            this.dgvIdk = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.spltLeftRight)).BeginInit();
            this.spltLeftRight.Panel1.SuspendLayout();
            this.spltLeftRight.Panel2.SuspendLayout();
            this.spltLeftRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIdk)).BeginInit();
            this.SuspendLayout();
            // 
            // spltLeftRight
            // 
            this.spltLeftRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spltLeftRight.Location = new System.Drawing.Point(0, 0);
            this.spltLeftRight.Name = "spltLeftRight";
            // 
            // spltLeftRight.Panel1
            // 
            this.spltLeftRight.Panel1.Controls.Add(this.tvMain);
            // 
            // spltLeftRight.Panel2
            // 
            this.spltLeftRight.Panel2.Controls.Add(this.dgvIdk);
            this.spltLeftRight.Size = new System.Drawing.Size(805, 405);
            this.spltLeftRight.SplitterDistance = 205;
            this.spltLeftRight.TabIndex = 0;
            // 
            // tvMain
            // 
            this.tvMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvMain.Location = new System.Drawing.Point(0, 0);
            this.tvMain.Name = "tvMain";
            this.tvMain.Size = new System.Drawing.Size(205, 405);
            this.tvMain.TabIndex = 0;
            this.tvMain.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvMain_NodeMouseClick);
            // 
            // dgvIdk
            // 
            this.dgvIdk.AllowUserToAddRows = false;
            this.dgvIdk.AllowUserToDeleteRows = false;
            this.dgvIdk.AllowUserToOrderColumns = true;
            this.dgvIdk.AllowUserToResizeColumns = false;
            this.dgvIdk.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvIdk.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvIdk.Location = new System.Drawing.Point(0, 0);
            this.dgvIdk.Name = "dgvIdk";
            this.dgvIdk.ReadOnly = true;
            this.dgvIdk.Size = new System.Drawing.Size(596, 405);
            this.dgvIdk.TabIndex = 0;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 405);
            this.Controls.Add(this.spltLeftRight);
            this.Name = "frmMain";
            this.ShowIcon = false;
            this.Text = "DRB DB Reader";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.spltLeftRight.Panel1.ResumeLayout(false);
            this.spltLeftRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.spltLeftRight)).EndInit();
            this.spltLeftRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvIdk)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.SplitContainer spltLeftRight;
		private System.Windows.Forms.TreeView tvMain;
		private System.Windows.Forms.DataGridView dgvIdk;
    }
}

