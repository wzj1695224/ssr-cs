using System;
using System.Windows.Forms;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm
	{
		// ReSharper disable once InconsistentNaming
		private ServerDataGridColumns Column;


		private enum ColumnIndex
		{
			Id = 0,
			Group,
			Server,
			ServerAddr,
			Enable,
			TotalConnect,
			Connecting,
			Ping,
			AvgLatency,
			AvgDownSpeed,
			MaxDownSpeed,
			AvgUpSpeed,
			MaxUpSpeed,
			DownBytes,
			UpBytes,
			DownBytesRaw,
			ErrorPercent,
			ConnectError,
			ConnectTimeout,
			ConnectEmptyResp,
			Continuous
		}


		private class ServerDataGridColumns
		{
			public DataGridViewColumn Id				=> _serverDataGrid.Columns[(int)ColumnIndex.Id];
			public DataGridViewColumn Group				=> _serverDataGrid.Columns[(int)ColumnIndex.Group];
			public DataGridViewColumn Server			=> _serverDataGrid.Columns[(int)ColumnIndex.Server];
			public DataGridViewColumn ServerAddr		=> _serverDataGrid.Columns[(int)ColumnIndex.ServerAddr];
			public DataGridViewColumn Enable			=> _serverDataGrid.Columns[(int)ColumnIndex.Enable];
			public DataGridViewColumn TotalConnect		=> _serverDataGrid.Columns[(int)ColumnIndex.TotalConnect];
			public DataGridViewColumn Connecting		=> _serverDataGrid.Columns[(int)ColumnIndex.Connecting];
			public DataGridViewColumn Ping				=> _serverDataGrid.Columns[(int)ColumnIndex.Ping];
			public DataGridViewColumn AvgLatency		=> _serverDataGrid.Columns[(int)ColumnIndex.AvgLatency];
			public DataGridViewColumn AvgDownSpeed		=> _serverDataGrid.Columns[(int)ColumnIndex.AvgDownSpeed];
			public DataGridViewColumn MaxDownSpeed		=> _serverDataGrid.Columns[(int)ColumnIndex.MaxDownSpeed];
			public DataGridViewColumn AvgUpSpeed		=> _serverDataGrid.Columns[(int)ColumnIndex.AvgUpSpeed];
			public DataGridViewColumn MaxUpSpeed		=> _serverDataGrid.Columns[(int)ColumnIndex.MaxUpSpeed];
			public DataGridViewColumn DownBytes			=> _serverDataGrid.Columns[(int)ColumnIndex.DownBytes];
			public DataGridViewColumn UpBytes			=> _serverDataGrid.Columns[(int)ColumnIndex.UpBytes];
			public DataGridViewColumn DownBytesRaw		=> _serverDataGrid.Columns[(int)ColumnIndex.DownBytesRaw];
			public DataGridViewColumn ErrorPercent		=> _serverDataGrid.Columns[(int)ColumnIndex.ErrorPercent];
			public DataGridViewColumn ConnectError		=> _serverDataGrid.Columns[(int)ColumnIndex.ConnectError];
			public DataGridViewColumn ConnectTimeout	=> _serverDataGrid.Columns[(int)ColumnIndex.ConnectTimeout];
			public DataGridViewColumn ConnectEmptyResp	=> _serverDataGrid.Columns[(int)ColumnIndex.ConnectEmptyResp];
			public DataGridViewColumn Continuous		=> _serverDataGrid.Columns[(int)ColumnIndex.Continuous];


			private readonly DataGridView _serverDataGrid;

			public ServerDataGridColumns(DataGridView serverDataGrid)
			{
				_serverDataGrid = serverDataGrid;
			}

			public void DoForAll(Action<DataGridViewColumn> action)
			{
				foreach (DataGridViewColumn column in _serverDataGrid.Columns)
					action(column);
			}
		}


	}
}
