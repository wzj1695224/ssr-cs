using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace Shadowsocks.View
{
	internal static class AutoSizeHelper
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
		private void AutoSizeColumnsAndForm()
		{
			var columns = ServerDataGrid.Columns;

			for (var i = 0; i < columns.Count; ++i)
			{
				var column = columns[i];

				var name = column.Name;
				if (!AutoSizeHelper.AutoSizeColumns.Contains(name)) continue;

				if (column.Width <= 2)
					continue;

				ServerDataGrid.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCellsExceptHeader);

				// check min size
				if ( AutoSizeHelper.KeepMinSizeColumns.Contains(name) )
					column.MinimumWidth = column.Width;
			}

			// col
			var colTotalWidth = columns.Cast<DataGridViewColumn>()
				.Where(c => c.Visible)
				.Sum(c => c.Width);

			this.Width = colTotalWidth + SystemInformation.VerticalScrollBarWidth + (this.Width - this.ClientSize.Width) + 1;

			ServerDataGrid.AutoResizeColumnHeadersHeight();
		}
	}
}