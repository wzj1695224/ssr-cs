using Shadowsocks.Controller;
using Shadowsocks.Core;
using Shadowsocks.Framework.Windows;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static Shadowsocks.Controller.I18N.Static;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm : Form
    {
        class DoubleBufferListView : DataGridView
        {
            public DoubleBufferListView()
            {
                SetStyle(ControlStyles.DoubleBuffer
                        | ControlStyles.OptimizedDoubleBuffer
                        | ControlStyles.UserPaint
                        | ControlStyles.AllPaintingInWmPaint
                        , true);
                UpdateStyles();
            }
        }


        private ShadowsocksController controller;
        //private ContextMenu contextMenu1;
        private List<int> serverOrder = new List<int>();

        private int _updateSkip = 0;
        private int _updateTickCount = 0;

        private ServerSpeedLogShow[] _serverStats;

        private Thread                  _workerThread;
        private volatile bool           _workerRunning  = false;
        private readonly AutoResetEvent _workerEvent    = new AutoResetEvent(false);




        public ServerStatForm(ShadowsocksController controller)
        {
            this.controller = controller;

            this.Icon = ResourceFactory.CreateIcon();
            this.Font = SystemFonts.MessageBoxFont;
            InitializeComponent();
            InitEvent();

            var config = controller.GetCurrentConfiguration();

            this.Width = 810;
            this.Height = CalcPreferHeight(config.configs.Count);

            UpdateTitle();
            UpdateServerStat();

            InitMenu();
            controller.ConfigChanged += OnConfigChanged;

            InitServerDataGrid();
            AutoSizeFinal();
        }


        private void InitEvent()
        {
			InitServerDataGridEvents();
        }


        private void InitServerDataGrid()
        {
            var mul = DPI.DpiMul;

            foreach (DataGridViewColumn column in ServerDataGrid.Columns)
            {
                // header text 
                column.HeaderText = S(column.HeaderText);

                // width with dpi
                column.Width = column.Width * mul / 4;
            }

            ServerDataGrid.RowTemplate.Height = 20 * mul / 4;
        }




        #region App Events

        private void OnConfigChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }

        #endregion




        #region Update

        private void UpdateTick(object sender, EventArgs e)
        {
            ++_updateTickCount;

            // skip update if need
            if (_updateSkip-- > 0)
                return;

            // skip some update when window 
            var freeze = this.WindowState == FormWindowState.Minimized;
            if (freeze && _updateTickCount % 40 == 0)
                return;

            ServerDataGrid_Update();
            UpdateTitle();
            UpdateServerStat();
        }


        private void UpdateTitle()
        {
            var title = S("ServerStat");
            const string version = UpdateChecker.FullVersion;
            var count = Core.Model.Server.Server.GetForwardServerRef().GetConnections().Count;
            var share = controller.GetCurrentConfiguration().shareOverLan ? "any" : "local";
            var port = controller.GetCurrentConfiguration().localPort;
            var path = Application.StartupPath;

            this.Text = $@"{title} {version}    Conn {count}    {share}:{port}    {path}";
        }


        private void UpdateServerStat()
        {
            if (_workerRunning)
                _workerEvent.Set();
            else
            {
                _workerRunning = true;
                _workerThread = new Thread(this.UpdateServerStatThread);
                _workerThread.Start();
            }
        }

        private void UpdateServerStatThread()
        {
            while (_workerRunning)
            {
                var config = controller.GetCurrentConfiguration();
                var servers = config.configs;

                _serverStats = servers
                    .Select(server => server.ServerSpeedLog().Translate())
                    .ToArray();

                _workerEvent.WaitOne();
            }
        }


        private void ServerDataGrid_Update()
        {
            if (_serverStats == null)
                return;

            var rowCount = ServerDataGrid.RowCount;
            var config = controller.GetCurrentConfiguration();

            var serverCount = config.configs.Count;

            // init OR trim  serverOrder
            if (serverOrder.Count > serverCount)
                serverOrder.RemoveRange(serverCount, serverOrder.Count - serverCount);
            while (serverOrder.Count < serverCount)
                serverOrder.Add(0);

            // init OR trim  rows for new servers
            while (ServerDataGrid.RowCount < serverCount && ServerDataGrid.RowCount < _serverStats.Length)
            {
                ServerDataGrid.Rows.Add();
                var newId = ServerDataGrid.RowCount - 1;
                ServerDataGrid[0, newId].Value = newId;
            }
            if (ServerDataGrid.RowCount > serverCount)
            {
                for (var i = ServerDataGrid.RowCount - 1; i >= serverCount; --i)
                    ServerDataGrid.Rows.RemoveAt(i);
            }

            try
            {
                ServerDataGrid_UpdateCells(config);
            }
            catch
            {
                // ignore
            }

            // apply sort to new data
            if (ServerDataGrid.SortedColumn != null)
            {
	            var sort = (ListSortDirection)((int)ServerDataGrid.SortOrder - 1);  // convert SortOrder to ListSortDirection
                ServerDataGrid.Sort(ServerDataGrid.SortedColumn, sort);
            }

            // select current server
            if (rowCount == 0 && config.index >= 0 && config.index < ServerDataGrid.RowCount)
            {
                ServerDataGrid[0, config.index].Selected = true;
            }

            if (ServerDataGrid_FirstUpdate)
            {
                ServerDataGrid_FirstUpdate = false;
                ServerDataGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, config.index - ServerDataGrid.DisplayedRowCount(true) / 2);
            }
        }

        #endregion




        #region Window Events

        private void ServerStatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= OnConfigChanged;

            var thread = _workerThread;
            _workerThread = null;
            _workerRunning = false;

            while (thread.IsAlive)
            {
                _workerEvent.Set();
                Thread.Sleep(50);
            }
        }


        private void ServerStatForm_Move(object sender, EventArgs e)
        {
            _updateSkip = 0;
        }


        private void ServerStatForm_ResizeEnd(object sender, EventArgs e)
        {
            _updateSkip = 0;

            var width = CalcMinWidth();
            ServerDataGrid.Columns[2].Width += this.Width - width;
        }


        protected override void WndProc(ref Message message)
        {
            const int WM_SIZING = 532;
            //const int WM_SIZE = 533;
            const int WM_MOVING = 534;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;

            switch (message.Msg)
            {
                case WM_SIZING:
                case WM_MOVING:
                    _updateSkip = 2;
                    break;
                case WM_SYSCOMMAND:
                    if ((int)message.WParam == SC_MINIMIZE)
                    {
                        Util.Utils.ReleaseMemory();
                    }
                    break;
            }

            base.WndProc(ref message);
        }

        #endregion
    }
}
