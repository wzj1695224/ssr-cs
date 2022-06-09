namespace Shadowsocks.View.Log
{
    partial class LogForm
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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			this.logMenu = new System.Windows.Forms.MenuStrip();
			this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.clearLogMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.showInExplorerMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.closeMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.viewMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.fontMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.wrapTextMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.alwaysOnTopMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.logTextBox = new System.Windows.Forms.TextBox();
			this.refreshTimer = new System.Windows.Forms.Timer(this.components);
			this.logMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// logMenu
			// 
			this.logMenu.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
			this.logMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.logMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.viewMenu});
			this.logMenu.Location = new System.Drawing.Point(0, 0);
			this.logMenu.Name = "logMenu";
			this.logMenu.Size = new System.Drawing.Size(1479, 35);
			this.logMenu.TabIndex = 0;
			this.logMenu.Text = "menuStrip1";
			// 
			// fileMenu
			// 
			this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearLogMenu,
            this.showInExplorerMenu,
            this.toolStripSeparator1,
            this.closeMenu});
			this.fileMenu.Name = "fileMenu";
			this.fileMenu.Size = new System.Drawing.Size(54, 29);
			this.fileMenu.Text = "&File";
			// 
			// clearLogMenu
			// 
			this.clearLogMenu.Name = "clearLogMenu";
			this.clearLogMenu.Size = new System.Drawing.Size(246, 34);
			this.clearLogMenu.Text = "Clear &log";
			this.clearLogMenu.Click += new System.EventHandler(this.clearLogToolStripMenuItem_Click);
			// 
			// showInExplorerMenu
			// 
			this.showInExplorerMenu.Name = "showInExplorerMenu";
			this.showInExplorerMenu.Size = new System.Drawing.Size(246, 34);
			this.showInExplorerMenu.Text = "Show in &Explorer";
			this.showInExplorerMenu.Click += new System.EventHandler(this.showInExplorerToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(243, 6);
			// 
			// closeMenu
			// 
			this.closeMenu.Name = "closeMenu";
			this.closeMenu.Size = new System.Drawing.Size(246, 34);
			this.closeMenu.Text = "&Close";
			this.closeMenu.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			// 
			// viewMenu
			// 
			this.viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fontMenu,
            this.wrapTextMenu,
            this.alwaysOnTopMenu});
			this.viewMenu.Name = "viewMenu";
			this.viewMenu.Size = new System.Drawing.Size(65, 29);
			this.viewMenu.Text = "&View";
			// 
			// fontMenu
			// 
			this.fontMenu.Name = "fontMenu";
			this.fontMenu.Size = new System.Drawing.Size(228, 34);
			this.fontMenu.Text = "&Font";
			this.fontMenu.Click += new System.EventHandler(this.fontToolStripMenuItem_Click);
			// 
			// wrapTextMenu
			// 
			this.wrapTextMenu.Name = "wrapTextMenu";
			this.wrapTextMenu.Size = new System.Drawing.Size(228, 34);
			this.wrapTextMenu.Text = "&Wrap text";
			this.wrapTextMenu.CheckedChanged += new System.EventHandler(this.wrapTextToolStripMenuItem_CheckedChanged);
			this.wrapTextMenu.Click += new System.EventHandler(this.wrapTextToolStripMenuItem_Click);
			// 
			// alwaysOnTopMenu
			// 
			this.alwaysOnTopMenu.Name = "alwaysOnTopMenu";
			this.alwaysOnTopMenu.Size = new System.Drawing.Size(228, 34);
			this.alwaysOnTopMenu.Text = "&Always on top";
			this.alwaysOnTopMenu.CheckedChanged += new System.EventHandler(this.alwaysOnTopToolStripMenuItem_CheckedChanged);
			this.alwaysOnTopMenu.Click += new System.EventHandler(this.alwaysOnTopToolStripMenuItem_Click);
			// 
			// logTextBox
			// 
			this.logTextBox.BackColor = System.Drawing.Color.Black;
			this.logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logTextBox.Font = new System.Drawing.Font("Courier New", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.logTextBox.ForeColor = System.Drawing.Color.White;
			this.logTextBox.Location = new System.Drawing.Point(0, 35);
			this.logTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.logTextBox.MaxLength = 10485760;
			this.logTextBox.Multiline = true;
			this.logTextBox.Name = "logTextBox";
			this.logTextBox.ReadOnly = true;
			this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.logTextBox.Size = new System.Drawing.Size(1479, 692);
			this.logTextBox.TabIndex = 1;
			this.logTextBox.WordWrap = false;
			// 
			// refreshTimer
			// 
			this.refreshTimer.Enabled = true;
			this.refreshTimer.Tick += new System.EventHandler(this.refreshTimer_Tick);
			// 
			// LogForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1479, 727);
			this.Controls.Add(this.logTextBox);
			this.Controls.Add(this.logMenu);
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "LogForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Log Viewer";
			this.Load += new System.EventHandler(this.LogForm_Load);
			this.Shown += new System.EventHandler(this.LogForm_Shown);
			this.logMenu.ResumeLayout(false);
			this.logMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip logMenu;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem showInExplorerMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem closeMenu;
        private System.Windows.Forms.ToolStripMenuItem viewMenu;
        private System.Windows.Forms.ToolStripMenuItem fontMenu;
        private System.Windows.Forms.ToolStripMenuItem wrapTextMenu;
        private System.Windows.Forms.ToolStripMenuItem alwaysOnTopMenu;
        private System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.ToolStripMenuItem clearLogMenu;
        private System.Windows.Forms.Timer refreshTimer;
    }
}