using Shadowsocks.Controller;
using Shadowsocks.Core;
using Shadowsocks.Core.Model.Server;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Shadowsocks.Framework.Windows;
using Shadowsocks.SystemX.Drawing;
using static Shadowsocks.Controller.I18N.Static;
using static Shadowsocks.Framework.Util.ByteUtil;
using static Shadowsocks.Framework.Windows.Forms.Menu.MenuFactory;


namespace Shadowsocks.View
{
	public partial class ServerLogForm : Form
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
        private MenuItem topmostItem;
        private MenuItem clearItem;
        private List<int> listOrder = new List<int>();
        private int lastRefreshIndex = 0;
        private bool firstDispley = true;
        private int updatePause = 0;
        private int updateTick = 0;
        private int updateSize = 0;
        private int pendingUpdate = 0;
        private ServerSpeedLogShow[] ServerSpeedLogList;
        private Thread workerThread;
        private AutoResetEvent workerEvent = new AutoResetEvent(false);


        public ServerLogForm(ShadowsocksController controller)
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
            UpdateLog();

            InitMenu();
            controller.ConfigChanged += controller_ConfigChanged;

            SetupServerDataTable();
            AutoSizeFinal();
        }


        private void InitMenu()
        { 
	        this.topmostItem = CreateMenuItem("Always On &Top", this.topmostItem_Click);
	        
	        this.Menu = new MainMenu(new[]
	        {
		        CreateMenuGroup("&Control", new[]
		        {
			        CreateMenuItem("&Disconnect direct connections", this.DisconnectForward_Click),
			        CreateMenuItem("Disconnect &All", this.Disconnect_Click),
			        new MenuItem("-"),
			        CreateMenuItem("Clear &MaxSpeed", this.ClearMaxSpeed_Click),
			        clearItem = CreateMenuItem("&Clear", this.ClearItem_Click),
			        new MenuItem("-"),
			        CreateMenuItem("Clear &Selected Total", this.ClearSelectedTotal_Click),
			        CreateMenuItem("Clear &Total", this.ClearTotal_Click),
		        }),
		        CreateMenuGroup("Port &out", new[]
		        {
			        CreateMenuItem("Copy current link", this.copyLinkItem_Click),
			        CreateMenuItem("Copy current group links", this.copyGroupLinkItem_Click),
			        CreateMenuItem("Copy all enable links", this.copyEnableLinksItem_Click),
			        CreateMenuItem("Copy all links", this.copyLinksItem_Click),
		        }),
		        CreateMenuGroup("&Window", new[]
		        {
			        CreateMenuItem("Auto &size", AutoSizeServerDataTable),
                    topmostItem
                }),
	        });
        }


        private void InitEvent()
        {
			InitServerDataGridEvents();
        }


        private void SetupServerDataTable()
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




        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }




        public void UpdateLogThread()
        {
            while (workerThread != null)
            {
                Configuration config = controller.GetCurrentConfiguration();

                ServerSpeedLogShow[] _ServerSpeedLogList = new ServerSpeedLogShow[config.configs.Count];
                for (int i = 0; i < config.configs.Count && i < _ServerSpeedLogList.Length; ++i)
                {
                    _ServerSpeedLogList[i] = config.configs[i].ServerSpeedLog().Translate();
                }

                ServerSpeedLogList = _ServerSpeedLogList;

                workerEvent.WaitOne();
            }
        }
        public void UpdateLog()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(this.UpdateLogThread);
                workerThread.Start();
            }
            else
            {
                workerEvent.Set();
            }
        }
        public void RefreshLog()
        {
            if (ServerSpeedLogList == null)
                return;

            int last_rowcount = ServerDataGrid.RowCount;
            Configuration config = controller.GetCurrentConfiguration();
            if (listOrder.Count > config.configs.Count)
            {
                listOrder.RemoveRange(config.configs.Count, listOrder.Count - config.configs.Count);
            }
            while (listOrder.Count < config.configs.Count)
            {
                listOrder.Add(0);
            }
            while (ServerDataGrid.RowCount < config.configs.Count && ServerDataGrid.RowCount < ServerSpeedLogList.Length)
            {
                ServerDataGrid.Rows.Add();
                int id = ServerDataGrid.RowCount - 1;
                ServerDataGrid[0, id].Value = id;
            }
            if (ServerDataGrid.RowCount > config.configs.Count)
            {
                for (int list_index = 0; list_index < ServerDataGrid.RowCount; ++list_index)
                {
                    DataGridViewCell id_cell = ServerDataGrid[0, list_index];
                    int id = (int)id_cell.Value;
                    if (id >= config.configs.Count)
                    {
                        ServerDataGrid.Rows.RemoveAt(list_index);
                        --list_index;
                    }
                }
            }
            int displayBeginIndex = ServerDataGrid.FirstDisplayedScrollingRowIndex;
            int displayEndIndex = displayBeginIndex + ServerDataGrid.DisplayedRowCount(true);
            try
            {
                for (int list_index = (lastRefreshIndex >= ServerDataGrid.RowCount) ? 0 : lastRefreshIndex, rowChangeCnt = 0;
                    list_index < ServerDataGrid.RowCount && rowChangeCnt <= 100;
                    ++list_index)
                {
                    lastRefreshIndex = list_index + 1;

                    DataGridViewCell id_cell = ServerDataGrid[0, list_index];
                    int id = (int)id_cell.Value;
                    Server server = config.configs[id];
                    ServerSpeedLogShow serverSpeedLog = ServerSpeedLogList[id];
                    listOrder[id] = list_index;
                    _tableChanged = false;
                    for (int curcol = 0; curcol < ServerDataGrid.Columns.Count; ++curcol)
                    {
                        if (!firstDispley
                            && (ServerDataGrid.SortedColumn == null || ServerDataGrid.SortedColumn.Index != curcol)
                            && (list_index < displayBeginIndex || list_index >= displayEndIndex))
                            continue;
                        DataGridViewCell cell = ServerDataGrid[curcol, list_index];
                        string columnName = ServerDataGrid.Columns[curcol].Name;
                        // Server
                        if (columnName == "Server")
                        {
                            if (config.index == id)
                                SetCellBackColor(cell, Color.Cyan);
                            else
                                SetCellBackColor(cell, Color.White);
                            SetCellText(cell, server.FriendlyName());
                        }
                        if (columnName == "Group")
                        {
                            SetCellText(cell, server.group);
                        }
                        // Enable
                        if (columnName == "Enable")
                        {
                            if (server.enable)
                                SetCellBackColor(cell, Color.White);
                            else
                                SetCellBackColor(cell, Color.Red);
                        }
                        // TotalConnectTimes
                        else if (columnName == "TotalConnect")
                        {
                            SetCellText(cell, serverSpeedLog.totalConnectTimes);
                        }
                        // TotalConnecting
                        else if (columnName == "Connecting")
                        {
                            long connections = serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes;
                            //long ref_connections = server.GetConnections().Count;
                            //if (ref_connections < connections)
                            //{
                            //    connections = ref_connections;
                            //}
                            Color[] colList = new Color[5] { Color.White, Color.LightGreen, Color.Yellow, Color.Red, Color.Red };
                            long[] bytesList = new long[5] { 0, 16, 32, 64, 65536 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (connections < bytesList[i])
                                {
                                    SetCellBackColor(cell,
                                        ColorX.MixColor(colList[i - 1],
                                            colList[i],
                                            (double)(connections - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
                        }
                        // AvgConnectTime
                        else if (columnName == "AvgLatency")
                        {
                            if (serverSpeedLog.avgConnectTime >= 0)
                                SetCellText(cell, serverSpeedLog.avgConnectTime / 1000);
                            else
                                SetCellText(cell, "-");
                        }
                        // AvgDownSpeed
                        else if (columnName == "AvgDownSpeed")
                        {
                            long avgBytes = serverSpeedLog.avgDownloadBytes;
                            string valStr = FormatBytes(avgBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024L * 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (avgBytes < bytesList[i])
                                {
                                    SetCellBackColor(cell,
                                        ColorX.MixColor(colList[i - 1],
                                            colList[i],
                                            (double)(avgBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // MaxDownSpeed
                        else if (columnName == "MaxDownSpeed")
                        {
                            long maxBytes = serverSpeedLog.maxDownloadBytes;
                            string valStr = FormatBytes(maxBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (maxBytes < bytesList[i])
                                {
                                    SetCellBackColor(cell,
                                        ColorX.MixColor(colList[i - 1],
                                            colList[i],
                                            (double)(maxBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // AvgUpSpeed
                        else if (columnName == "AvgUpSpeed")
                        {
                            long avgBytes = serverSpeedLog.avgUploadBytes;
                            string valStr = FormatBytes(avgBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024L * 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (avgBytes < bytesList[i])
                                {
                                    SetCellBackColor(cell,
                                        ColorX.MixColor(colList[i - 1],
                                            colList[i],
                                            (double)(avgBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // MaxUpSpeed
                        else if (columnName == "MaxUpSpeed")
                        {
                            long maxBytes = serverSpeedLog.maxUploadBytes;
                            string valStr = FormatBytes(maxBytes);
                            Color[] colList = new Color[6] { Color.White, Color.LightGreen, Color.Yellow, Color.Pink, Color.Red, Color.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (maxBytes < bytesList[i])
                                {
                                    SetCellBackColor(cell,
                                        ColorX.MixColor(colList[i - 1],
                                            colList[i],
                                            (double)(maxBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            SetCellText(cell, valStr);
                        }
                        // TotalUploadBytes
                        else if (columnName == "Upload")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalUploadBytes);
                            string fullVal = serverSpeedLog.totalUploadBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetCellBackColor(cell, Color.FromArgb(0xf4, 0xff, 0xf4));
                                else
                                {
                                    SetCellBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0) SetCellBackColor(cell, Color.FromArgb(0xf4, 0xff, 0xf4));
                                //Color col = cell.Style.BackColor;
                                //SetCellBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        // TotalDownloadBytes
                        else if (columnName == "Download")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadBytes);
                            string fullVal = serverSpeedLog.totalDownloadBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetCellBackColor(cell, Color.FromArgb(0xff, 0xf0, 0xf0));
                                else
                                {
                                    SetCellBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0) SetCellBackColor(cell, Color.FromArgb(0xff, 0xf0, 0xf0));
                                //Color col = cell.Style.BackColor;
                                //SetCellBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        else if (columnName == "DownloadRaw")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadRawBytes);
                            string fullVal = serverSpeedLog.totalDownloadRawBytes.ToString();
                            if (cell.ToolTipText != fullVal)
                            {
                                if (fullVal == "0")
                                    SetCellBackColor(cell, Color.FromArgb(0xff, 0x80, 0x80));
                                else
                                {
                                    SetCellBackColor(cell, Color.LightGreen);
                                    cell.Tag = 8;
                                }
                            }
                            else if (cell.Tag != null)
                            {
                                cell.Tag = (int)cell.Tag - 1;
                                if ((int)cell.Tag == 0)
                                {
                                    if (fullVal == "0")
                                        SetCellBackColor(cell, Color.FromArgb(0xff, 0x80, 0x80));
                                    else
                                        SetCellBackColor(cell, Color.FromArgb(0xf0, 0xf0, 0xff));
                                }
                                //Color col = cell.Style.BackColor;
                                //SetCellBackColor(cell, Color.FromArgb(Math.Min(255, col.R + colAdd), Math.Min(255, col.G + colAdd), Math.Min(255, col.B + colAdd)));
                            }
                            SetCellToolTipText(cell, fullVal);
                            SetCellText(cell, valStr);
                        }
                        // ErrorConnectTimes
                        else if (columnName == "ConnectError")
                        {
                            long val = serverSpeedLog.errorConnectTimes + serverSpeedLog.errorDecodeTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 2.5), (byte)Math.Max(0, 255 - val * 2.5));
                            SetCellBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorTimeoutTimes
                        else if (columnName == "ConnectTimeout")
                        {
                            SetCellText(cell, serverSpeedLog.errorTimeoutTimes);
                        }
                        // ErrorTimeoutTimes
                        else if (columnName == "ConnectEmpty")
                        {
                            long val = serverSpeedLog.errorEmptyTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            SetCellBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorContinurousTimes
                        else if (columnName == "Continuous")
                        {
                            long val = serverSpeedLog.errorContinurousTimes;
                            Color col = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            SetCellBackColor(cell, col);
                            SetCellText(cell, val);
                        }
                        // ErrorPersent
                        else if (columnName == "ErrorPercent")
                        {
                            if (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes > 0)
                            {
                                double percent = (serverSpeedLog.errorConnectTimes
                                    + serverSpeedLog.errorTimeoutTimes
                                    + serverSpeedLog.errorDecodeTimes)
                                    * 100.00
                                    / (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
                                SetCellBackColor(cell, Color.FromArgb(255, (byte)(255 - percent * 2), (byte)(255 - percent * 2)));
                                SetCellText(cell, percent.ToString("F0") + "%");
                            }
                            else
                            {
                                SetCellBackColor(cell, Color.White);
                                SetCellText(cell, "-");
                            }
                        }
                    }
                    if (_tableChanged && list_index >= displayBeginIndex && list_index < displayEndIndex)
                        rowChangeCnt++;
                }
            }
            catch
            {

            }
            UpdateTitle();
            if (ServerDataGrid.SortedColumn != null)
            {
                ServerDataGrid.Sort(ServerDataGrid.SortedColumn, (ListSortDirection)((int)ServerDataGrid.SortOrder - 1));
            }
            if (last_rowcount == 0 && config.index >= 0 && config.index < ServerDataGrid.RowCount)
            {
                ServerDataGrid[0, config.index].Selected = true;
            }
            if (firstDispley)
            {
                ServerDataGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, config.index - ServerDataGrid.DisplayedRowCount(true) / 2);
                firstDispley = false;
            }
        }


        private void copyLinkItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config.index >= 0 && config.index < config.configs.Count)
            {
                try
                {
                    string link = config.configs[config.index].GetSSRLinkForServer();
                    Clipboard.SetText(link);
                }
                catch { }
            }
        }

        private void copyGroupLinkItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config.index >= 0 && config.index < config.configs.Count)
            {
                string group = config.configs[config.index].group;
                string link = "";
                for (int index = 0; index < config.configs.Count; ++index)
                {
                    if (config.configs[index].group != group)
                        continue;
                    link += config.configs[index].GetSSRLinkForServer() + "\r\n";
                }
                try
                {
                    Clipboard.SetText(link);
                }
                catch { }
            }
        }

        private void copyEnableLinksItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            string link = "";
            for (int index = 0; index < config.configs.Count; ++index)
            {
                if (!config.configs[index].enable)
                    continue;
                link += config.configs[index].GetSSRLinkForServer() + "\r\n";
            }
            try
            {
                Clipboard.SetText(link);
            }
            catch { }
        }

        private void copyLinksItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            string link = "";
            for (int index = 0; index < config.configs.Count; ++index)
            {
                link += config.configs[index].GetSSRLinkForServer() + "\r\n";
            }
            try
            {
                Clipboard.SetText(link);
            }
            catch { }
        }

        private void topmostItem_Click(object sender, EventArgs e)
        {
            topmostItem.Checked = !topmostItem.Checked;
            this.TopMost = topmostItem.Checked;
        }

        private void DisconnectForward_Click(object sender, EventArgs e)
        {
            Core.Model.Server.Server.GetForwardServerRef().GetConnections().CloseAll();
        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            controller.DisconnectAllConnections();
            Core.Model.Server.Server.GetForwardServerRef().GetConnections().CloseAll();
        }

        private void ClearMaxSpeed_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            foreach (Server server in config.configs)
            {
                server.ServerSpeedLog().ClearMaxSpeed();
            }
        }

        private void ClearSelectedTotal_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config.index >= 0 && config.index < config.configs.Count)
            {
                try
                {
                    controller.ClearTransferTotal(config.configs[config.index].server);
                }
                catch { }
            }
        }

        private void ClearTotal_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            foreach (Server server in config.configs)
            {
                controller.ClearTransferTotal(server.server);
            }
        }

        private void ClearItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            foreach (Server server in config.configs)
            {
                server.ServerSpeedLog().Clear();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (updatePause > 0)
            {
                updatePause -= 1;
                return;
            }
            if (this.WindowState == FormWindowState.Minimized)
            {
                if (++pendingUpdate < 40)
                {
                    return;
                }
            }
            else
            {
                ++updateTick;
            }
            pendingUpdate = 0;
            RefreshLog();
            UpdateLog();
            if (updateSize > 1) --updateSize;
            if (updateTick == 2 || updateSize == 1)
            {
                updateSize = 0;
                //AutoSizeColumnsAndForm();
            }
        }





        private void ServerLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
            Thread thread = workerThread;
            workerThread = null;
            while (thread.IsAlive)
            {
                workerEvent.Set();
                Thread.Sleep(50);
            }
        }

        private void ServerLogForm_Move(object sender, EventArgs e)
        {
            updatePause = 0;
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
                    updatePause = 2;
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

        private void ServerLogForm_ResizeEnd(object sender, EventArgs e)
        {
            updatePause = 0;

            int width = 0;
            for (int i = 0; i < ServerDataGrid.Columns.Count; ++i)
            {
                if (!ServerDataGrid.Columns[i].Visible)
                    continue;
                width += ServerDataGrid.Columns[i].Width;
            }
            width += SystemInformation.VerticalScrollBarWidth + (this.Width - this.ClientSize.Width) + 1;
            ServerDataGrid.Columns[2].Width += this.Width - width;
        }
    }
}
