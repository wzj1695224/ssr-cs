using System;
using System.Linq;
using System.Windows.Forms;
using static Shadowsocks.Framework.Util.ByteUtil;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm
	{
		internal delegate void ServerDataGridViewCellEventHandlerFunc(object sender, DataGridViewCellEventArgs e, DataGridViewCell cell, DataGridViewColumn column, int id);


		private void InitServerDataGridEvents()
		{
			this.ServerDataGrid.CellClick           += ServerDataGridViewCellEventHandler(this.ServerDataGrid_CellClick);
			this.ServerDataGrid.CellDoubleClick     += ServerDataGridViewCellEventHandler(this.ServerDataGrid_CellDoubleClick);
			this.ServerDataGrid.ColumnWidthChanged  += this.ServerDataGrid_ColumnWidthChanged;
			this.ServerDataGrid.SortCompare         += this.ServerDataGrid_SortCompare;
			this.ServerDataGrid.MouseUp             += this.ServerDataGrid_MouseUp;
		}


		/// <summary>
		/// Wrap ServerDataGridViewCellEventHandlerFunc as a DataGridViewCellEventHandler
		/// </summary>
		private DataGridViewCellEventHandler ServerDataGridViewCellEventHandler(ServerDataGridViewCellEventHandlerFunc func)
		{
			return (sender, e) =>
			{
				if (e.RowIndex < 0) return;

				var idCell = ServerDataGrid[0, e.RowIndex];

				var id = (int)idCell.Value;
				var column = ServerDataGrid.Columns[e.ColumnIndex];
				var cell = ServerDataGrid[e.ColumnIndex, e.RowIndex];
				func(sender, e, cell, column, id);

				idCell.Selected = true;
			};
		}




		private void ServerDataGrid_CellClick(object sender, DataGridViewCellEventArgs e, DataGridViewCell cell, DataGridViewColumn column, int id)
		{
			var config = _controller.GetCurrentConfiguration();
			var server = config.configs[id];
			var host = server.server;

			switch ((ColumnIndex)column.Index)
			{
				case ColumnIndex.Server:
					Console.WriteLine("config.checkSwitchAutoCloseAll:" + config.checkSwitchAutoCloseAll);
					if (config.checkSwitchAutoCloseAll)
						_controller.DisconnectAllConnections();
					_controller.SelectServerIndex(id);
					break;
				case ColumnIndex.Group:
					{
						var group = server.group;
						if (!string.IsNullOrEmpty(group))
						{
							var enable = !server.enable;
							var servers = config.configs.Where(s => s.group == group);
							foreach (var s in servers)
								s.enable = enable;
							_controller.SelectServerIndex(config.index);
						}
						break;
					}
				case ColumnIndex.Enable:
					server.enable = !server.enable;
					_controller.SelectServerIndex(config.index);
					break;
				case ColumnIndex.Ping:
					_serverDiagnostic.PingAsync(host, false);
					break;
			}
		}




		private void ServerDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e, DataGridViewCell cell, DataGridViewColumn column, int id)
		{
			var config = _controller.GetCurrentConfiguration();
			var server = config.configs[id];

			switch ((ColumnIndex)column.Index)
			{
				case ColumnIndex.Id:
				case ColumnIndex.Server:
					_controller.ShowConfigForm(id);
					break;
				case ColumnIndex.Connecting:
					server.GetConnections().CloseAll();
					break;
				case ColumnIndex.MaxDownSpeed:
				case ColumnIndex.MaxUpSpeed:
					server.ServerSpeedLog().ClearMaxSpeed();
					break;

				case ColumnIndex.UpBytes:
				case ColumnIndex.DownBytes:
					server.ServerSpeedLog().ClearTrans();
					break;
				case ColumnIndex.DownBytesRaw:
					server.ServerSpeedLog().Clear();
					server.enable = true;
					break;
				case ColumnIndex.ConnectError:
				case ColumnIndex.ConnectTimeout:
				case ColumnIndex.ConnectEmptyResp:
				case ColumnIndex.Continuous:
					server.ServerSpeedLog().ClearError();
					server.enable = true;
					break;
			}
		}




		private void ServerDataGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			AutoSizeFinal();
		}




		private void ServerDataGrid_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
		{
			var cell1 = ServerDataGrid[e.Column.Index, e.RowIndex1];
			var cell2 = ServerDataGrid[e.Column.Index, e.RowIndex2];

			switch ((ColumnIndex)e.Column.Index)
			{
				case ColumnIndex.Server:
				case ColumnIndex.Group:
				{
					// sort string
					var s1 = Convert.ToString(e.CellValue1);
					var s2 = Convert.ToString(e.CellValue2);
					e.SortResult = string.CompareOrdinal(s1, s2);
					e.Handled = true;
					break;
				}
				case ColumnIndex.Id:
				case ColumnIndex.TotalConnect:
				case ColumnIndex.Connecting:
				case ColumnIndex.ConnectError:
				case ColumnIndex.ConnectTimeout:
				case ColumnIndex.Continuous:
				{
					// sort int
					var v1 = Convert.ToInt32(e.CellValue1);
					var v2 = Convert.ToInt32(e.CellValue2);
					e.SortResult = v1 - v2;
					break;
				}
				case ColumnIndex.ErrorPercent:
				{
					// TODO ?
					var s1 = Convert.ToString(e.CellValue1);
					var s2 = Convert.ToString(e.CellValue2);
					var v1 = s1.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s1.Substring(0, s1.Length - 1)) * 100);
					var v2 = s2.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s2.Substring(0, s2.Length - 1)) * 100);
					e.SortResult = v1 == v2 ? 0 : v1 < v2 ? -1 : 1;
					break;
				}
				case ColumnIndex.Ping:
				{
					var state1 = CellState.Get<CellState.Ping>(cell1);
					var state2 = CellState.Get<CellState.Ping>(cell2);
					var ping1 = state1?.Time ?? CellState.Ping.NOT_PING_TIME;
					var ping2 = state2?.Time ?? CellState.Ping.NOT_PING_TIME;
					e.SortResult = (int)(ping1 - ping2);
					break;
				}
				case ColumnIndex.AvgLatency:
				case ColumnIndex.AvgDownSpeed:
				case ColumnIndex.MaxDownSpeed:
				case ColumnIndex.AvgUpSpeed:
				case ColumnIndex.MaxUpSpeed:
				case ColumnIndex.UpBytes:
				case ColumnIndex.DownBytes:
				case ColumnIndex.DownBytesRaw:
				{
					var s1 = Convert.ToString(e.CellValue1);
					var s2 = Convert.ToString(e.CellValue2);
					var v1 = ParseByteStr(s1);
					var v2 = ParseByteStr(s2);
					e.SortResult = (int)(v1 - v2);
					break;
				}
			}

			// sort by id order (by serverOrder)
			if (e.SortResult == 0)
			{
				var idCell1 = ServerDataGrid[0, e.RowIndex1];
				var idCell2 = ServerDataGrid[0, e.RowIndex2];
				var id1 = Convert.ToInt32(idCell1.Value);
				var id2 = Convert.ToInt32(idCell2.Value);

				var v1 = serverOrder[id1];
				var v2 = serverOrder[id2];
				e.SortResult = v1 - v2;
				if (e.SortResult != 0 && ServerDataGrid.SortOrder == SortOrder.Descending)
					e.SortResult = -e.SortResult;
			}

			// fix e.Handled
			if (e.SortResult != 0)
				e.Handled = true;
		}




		private void ServerDataGrid_MouseUp(object sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
				case MouseButtons.Right:
					ServerDataGrid_MouseRightUp(sender, e);
					break;
				case MouseButtons.Left:
					ServerDataGrid_MouseLeftUp(sender, e);
					break;
			}
		}


		private void ServerDataGrid_MouseLeftUp(object sender, MouseEventArgs e)
		{
			var cell = ServerDataGrid.SelectedCells?[0];
			if (cell == null) return;

			// select id cell
			var idCell = ServerDataGrid[0, cell.RowIndex];
			idCell.Selected = true;

			var id = (int)idCell.Value;
			var column = ServerDataGrid.Columns[cell.ColumnIndex];

			var config = _controller.GetCurrentConfiguration();
			var server = config.configs[id];

			switch ((ColumnIndex)column.Index)
			{
				case ColumnIndex.Server:
				{
					_controller.SelectServerIndex(id);
					break;
				}
				case ColumnIndex.Group:
				{
					var group = server.group;
					if (!string.IsNullOrEmpty(group))
					{
						var enable = !server.enable;
						var servers = config.configs.Where(s => s.group == group);
						foreach (var s in servers) 
							s.enable = enable;
						_controller.SelectServerIndex(config.index);
					}
					break;
				}
				case ColumnIndex.Enable:
				{
					server.enable = !server.enable;
					_controller.SelectServerIndex(config.index);
					break;
				}
			}
		}


		private void ServerDataGrid_MouseRightUp(object sender, MouseEventArgs e)
		{

		}
	}
}
