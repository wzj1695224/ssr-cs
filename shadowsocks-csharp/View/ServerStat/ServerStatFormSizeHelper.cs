using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Shadowsocks.Framework.Windows;


namespace Shadowsocks.View
{
	internal static class ColumnSizeHelper
	{
		internal static HashSet<string> AutoSizeColumns = new HashSet<string>
		{
			"AvgLatency", "AvgDownSpeed", "MaxDownSpeed", "AvgUpSpeed", "MaxUpSpeed", "Upload", "Download",
			"DownloadRaw", "Group", "Connecting", "ErrorPercent", "ConnectError", "ConnectTimeout", "Continuous",
			"ConnectEmpty"
		};

		internal static HashSet<string> KeepMinSizeColumns = new HashSet<string>
		{
			"AvgLatency", "Connecting", "AvgDownSpeed", "MaxDownSpeed", "AvgUpSpeed", "MaxUpSpeed"
		};
	}




	public partial class ServerLogForm
	{
		private int CalcPreferHeight(int serverCount)
		{
			var mul = DPI.DpiMul;

			if (serverCount < 8)
				return 300 * mul / 4;
			if (serverCount < 20)
				this.Height = (300 + (serverCount - 8) * 16) * mul / 4;
			return 500 * mul / 4;
		}


		private int CalcMinWidth()
		{
			var columnTotalWidth = ServerDataGrid.Columns.Cast<DataGridViewColumn>()
				.Where(c => c.Visible)
				.Sum(c => c.Width);

			return columnTotalWidth + SystemInformation.VerticalScrollBarWidth + (this.Width - this.ClientSize.Width) + 1;
		}


		private void AutoSizeFinal()
		{
			this.Width = CalcMinWidth();
			ServerDataGrid.AutoResizeColumnHeadersHeight();
		}


		private void AutoSizeServerDataTable()
		{
			var columns = ServerDataGrid.Columns;

			for (var i = 0; i < columns.Count; ++i)
			{
				var column = columns[i];

				var name = column.Name;
				if (!ColumnSizeHelper.AutoSizeColumns.Contains(name)) continue;

				if (column.Width <= 2)
					continue;

				ServerDataGrid.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCellsExceptHeader);

				// check min size
				if ( ColumnSizeHelper.KeepMinSizeColumns.Contains(name) )
					column.MinimumWidth = column.Width;
			}

			AutoSizeFinal();
		}
	}

}