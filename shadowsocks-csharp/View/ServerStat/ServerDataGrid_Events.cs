using System;
using System.Linq;
using System.Windows.Forms;
using static Shadowsocks.Framework.Util.ByteUtil;
using static Shadowsocks.View.ServerDataGridEventHelper;


namespace Shadowsocks.View
{
	internal static class ServerDataGridEventHelper
	{
		internal delegate void ServerDataGridViewCellEventHandlerFunc(object sender, DataGridViewCellEventArgs e, DataGridViewCell cell, DataGridViewColumn column, int id);

	}




	public partial class ServerLogForm
	{
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
			var config = controller.GetCurrentConfiguration();
			var server = config.configs[id];

			switch (column.Name)
			{
				case "Server":
					Console.WriteLine("config.checkSwitchAutoCloseAll:" + config.checkSwitchAutoCloseAll);
					if (config.checkSwitchAutoCloseAll)
						controller.DisconnectAllConnections();
					controller.SelectServerIndex(id);
					break;
				case "Group":
					{
						var group = server.group;
						if (!string.IsNullOrEmpty(group))
						{
							var enable = !server.enable;
							var servers = config.configs.Where(s => s.group == group);
							foreach (var s in servers)
								s.enable = enable;
							controller.SelectServerIndex(config.index);
						}
						break;
					}
				case "Enable":
					server.enable = !server.enable;
					controller.SelectServerIndex(config.index);
					break;
			}
		}




		private void ServerDataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e, DataGridViewCell cell, DataGridViewColumn column, int id)
		{
			var config = controller.GetCurrentConfiguration();
			var server = config.configs[id];

			switch (column.Name)
			{
				case "ID":
				case "Server":
					controller.ShowConfigForm(id);
					break;
				case "Connecting":
					server.GetConnections().CloseAll();
					break;
				case "MaxDownSpeed":
				case "MaxUpSpeed":
					server.ServerSpeedLog().ClearMaxSpeed();
					break;

				case "Upload":
				case "Download":
					server.ServerSpeedLog().ClearTrans();
					break;
				case "DownloadRaw":
					server.ServerSpeedLog().Clear();
					server.enable = true;
					break;
				case "ConnectError":
				case "ConnectTimeout":
				case "ConnectEmpty":
				case "Continuous":
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
			switch (e.Column.Name)
			{
				case "Server":
				case "Group":
				{
					// sort string
					var s1 = Convert.ToString(e.CellValue1);
					var s2 = Convert.ToString(e.CellValue2);
					e.SortResult = string.CompareOrdinal(s1, s2);
					e.Handled = true;
					break;
				}
				case "ID":
				case "TotalConnect":
				case "Connecting":
				case "ConnectError":
				case "ConnectTimeout":
				case "Continuous":
				{
					// sort int
					var v1 = Convert.ToInt32(e.CellValue1);
					var v2 = Convert.ToInt32(e.CellValue2);
					e.SortResult = v1 - v2;
					break;
				}
				case "ErrorPercent":
				{
					// TODO ?
					var s1 = Convert.ToString(e.CellValue1);
					var s2 = Convert.ToString(e.CellValue2);
					var v1 = s1.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s1.Substring(0, s1.Length - 1)) * 100);
					var v2 = s2.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(s2.Substring(0, s2.Length - 1)) * 100);
					e.SortResult = v1 == v2 ? 0 : v1 < v2 ? -1 : 1;
					break;
				}
				case "AvgLatency":
				case "AvgDownSpeed":
				case "MaxDownSpeed":
				case "AvgUpSpeed":
				case "MaxUpSpeed":
				case "Upload":
				case "Download":
				case "DownloadRaw":
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

			var config = controller.GetCurrentConfiguration();
			var server = config.configs[id];

			switch (column.Name)
			{
				case "Server":
				{
					controller.SelectServerIndex(id);
					break;
				}
				case "Group":
				{
					var group = server.group;
					if (!string.IsNullOrEmpty(group))
					{
						var enable = !server.enable;
						var servers = config.configs.Where(s => s.group == group);
						foreach (var s in servers) 
							s.enable = enable;
						controller.SelectServerIndex(config.index);
					}
					break;
				}
				case "Enable":
				{
					server.enable = !server.enable;
					controller.SelectServerIndex(config.index);
					break;
				}
			}
		}


		private void ServerDataGrid_MouseRightUp(object sender, MouseEventArgs e)
		{

		}
	}
}
