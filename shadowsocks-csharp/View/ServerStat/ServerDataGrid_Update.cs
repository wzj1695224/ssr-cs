using Shadowsocks.Model;
using Shadowsocks.SystemX.Drawing;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Int32;
using static Shadowsocks.Framework.Util.ByteUnit;
using static Shadowsocks.Framework.Util.ByteUtil;
using static Shadowsocks.View.ServerStat.UpdateServerDataGridCellHelper;
// ReSharper disable InconsistentNaming


namespace Shadowsocks.View.ServerStat
{
	
	internal static class UpdateServerDataGridCellHelper
	{
		internal static readonly int	 MaxRowsPerUpdate		 = 100;

		// Connecting Level
		internal static readonly long[]  PingLevelTime           = { 0,            50,                200,           500,        65536 };
		internal static readonly Color[] PingLevelColors         = { Color.Green,  Color.LightGreen,  Color.Yellow,  Color.Red,  Color.Red };

		// Connecting Level
		internal static readonly long[]  ConnectingLevelCount    = { 0,            16,                32,            64,         65536 };
		internal static readonly Color[] ConnectingLevelColors   = { Color.White,  Color.LightGreen,  Color.Yellow,  Color.Red,  Color.Red };

		// Down & Up Speed Level
		internal static readonly long[]  SpeedLevelBytes         = { 0,            64 * K,            512 * K,       4 * M,       16 * M,     1 * G };
		internal static readonly Color[] SpeedLevelColors        = { Color.White,  Color.LightGreen,  Color.Yellow,  Color.Pink,  Color.Red,  Color.Red };

		// Down Total Colors
		internal static readonly Color   DownTotalResetColor     = Color.FromArgb(0xff, 0xf0, 0xf0);
		internal static readonly Color   DownTotalSteadyColor    = DownTotalResetColor;

		// Up Total Colors
		internal static readonly Color   UpTotalResetColor       = Color.FromArgb(0xf4, 0xff, 0xf4);
		internal static readonly Color   UpTotalSteadyColor      = UpTotalResetColor;

		// Down Raw Total Colors
		internal static readonly Color   DownRawTotalResetColor  = Color.FromArgb(0xff, 0x80, 0x80);
		internal static readonly Color   DownRawTotalSteadyColor = Color.FromArgb(0xf0, 0xf0, 0xff);


		internal static Color GetLevelColor(long degree, long[] levelDegrees, Color[] levelColors)
		{
			Debug.Assert(levelDegrees.Length == levelColors.Length);

			for (var i = 1; i < levelDegrees.Length; ++i)
			{
				if (degree < levelDegrees[i])
				{
					var @base = levelColors[i - 1];
					var next = levelColors[i];
					var alpha = (double)(degree - levelDegrees[i - 1]) / (levelDegrees[i] - levelDegrees[i - 1]);
					return ColorX.MixColor(@base, next, alpha);
				}
			}
			return levelColors[levelColors.Length - 1];
		}
	}




	public partial class ServerStatForm
	{
		private long ServerDataGrid_CellChangedCount = 0;
		private long ServerDataGrid_RowChangedCount  = 0;
		private int  ServerDataGrid_lastRefreshEnd   = MaxValue;
		private bool ServerDataGrid_FirstUpdate      = true;


		private bool SetCellBackColor(DataGridViewCell cell, Color newColor)
		{
			if (cell.Style.BackColor == newColor) return false;
			cell.Style.BackColor = newColor;
			++ServerDataGrid_CellChangedCount;
			return true;
		}

		private bool SetCellToolTipText(DataGridViewCell cell, string newString)
		{
			if (cell.ToolTipText == newString) return false;
			cell.ToolTipText = newString;
			++ServerDataGrid_CellChangedCount;
			return true;
		}

		private bool SetCellText(DataGridViewCell cell, string value)
		{
			if ((string)cell.Value == value) return false;
			cell.Value = value;
			++ServerDataGrid_CellChangedCount;
			return true;
		}

		private bool SetCellText(DataGridViewCell cell, long value)
		{
			if ((string)cell.Value == value.ToString()) return false;
			cell.Value = value.ToString();
			++ServerDataGrid_CellChangedCount;
			return true;
		}




		private void ServerDataGrid_UpdateCells(Configuration config)
		{
			var displayBeginIndex = ServerDataGrid.FirstDisplayedScrollingRowIndex;
			var displayEndIndex = displayBeginIndex + ServerDataGrid.DisplayedRowCount(true);

			// each refresh we only update at most 100 rows
			var lastRefreshCompleteAllRows = ServerDataGrid_lastRefreshEnd >= ServerDataGrid.RowCount - 1;
			ServerDataGrid_CellChangedCount = 0;
			ServerDataGrid_RowChangedCount = 0;

			var start = lastRefreshCompleteAllRows ? 0 : ServerDataGrid_lastRefreshEnd;
			for (var row = start; row < ServerDataGrid.RowCount && ServerDataGrid_RowChangedCount <= MaxRowsPerUpdate; ++row)
			{
				var cellChangeCountBefore = ServerDataGrid_CellChangedCount;

				var idCell = ServerDataGrid[0, row];
				var id = (int)idCell.Value;

				ServerDataGrid_lastRefreshEnd = row;
				serverOrder[id] = row;

				for (var col = 0; col < ServerDataGrid.Columns.Count; ++col)
				{
					if (!ServerDataGrid_FirstUpdate
						&& (ServerDataGrid.SortedColumn == null || ServerDataGrid.SortedColumn.Index != col)
						&& (row < displayBeginIndex || row >= displayEndIndex))
						continue;

					ServerDataGrid_UpdateCell(col, row, config, id);
				}

				if (cellChangeCountBefore != ServerDataGrid_CellChangedCount)
					++ServerDataGrid_RowChangedCount;
			}
		}




		private void ServerDataGrid_UpdateCell(int col, int row, Configuration config, int id)
		{
			var column = ServerDataGrid.Columns[col];
			var cell = ServerDataGrid[col, row];
			var server = config.configs[id];
			var serverSpeedLog = _serverStats[id];
			var host = server.server;

			switch (column.Name)
			{
				case "Server":
				{
					var color = config.index == id ? Color.Cyan : Color.White;
					SetCellBackColor(cell, color);
					SetCellText(cell, server.SimpleName());
					break;
				}
				case "ServerAddr":
				{
					var color = config.index == id ? Color.Cyan : Color.White; 
					SetCellBackColor(cell, color);
					SetCellText(cell, server.Addr());
					break;
				}
				case "Group":
					SetCellText(cell, server.group);
					break;
				case "Enable" when server.enable:
					SetCellBackColor(cell, Color.White);
					break;
				case "Enable":
					SetCellBackColor(cell, Color.Red);
					break;
				case "TotalConnect":
					SetCellText(cell, serverSpeedLog.totalConnectTimes);
					break;
				case "Connecting":
				{
					var connecting = serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes;
					var color = GetLevelColor(connecting, ConnectingLevelCount, ConnectingLevelColors);
					SetCellBackColor(cell, color);
					SetCellText(cell, connecting);
					break;
				}
				case "Ping":
					{
						if ( _serverDiagnostic.IsPinging(host) )
						{
							SetCellText(cell, "...");
							SetCellBackColor(cell, Color.White);
							break;
						}
						
						if (_serverDiagnostic.TryGetHostPingHistory(host, out var history))
						{
							var pingAvg = history[0]?.Replies
								.Select(r => r.RoundtripTime)
								.Average();
							if (pingAvg != null)
							{
								var ping = (long)pingAvg;
								var color = GetLevelColor(ping, PingLevelTime, PingLevelColors);
								SetCellBackColor(cell, color); 
								SetCellText(cell, ping);
								break;
							}
						}

						SetCellText(cell, "-");
						SetCellBackColor(cell, Color.White);
						break;
					}
				case "AvgLatency" when serverSpeedLog.avgConnectTime >= 0:
					SetCellText(cell, serverSpeedLog.avgConnectTime / 1000);
					break;
				case "AvgLatency":
					SetCellText(cell, "-");
					break;
				case "AvgDownSpeed":
					ServerDataGrid_UpdateSpeedCell(cell, serverSpeedLog.avgDownloadBytes);
					break;
				case "MaxDownSpeed":
					ServerDataGrid_UpdateSpeedCell(cell, serverSpeedLog.maxDownloadBytes);
					break;
				case "AvgUpSpeed":
					ServerDataGrid_UpdateSpeedCell(cell, serverSpeedLog.avgUploadBytes);
					break;
				case "MaxUpSpeed":
					ServerDataGrid_UpdateSpeedCell(cell, serverSpeedLog.maxUploadBytes);
					break;
				case "Upload":
					ServerDataGrid_UpdateAwareChangeCell(cell, serverSpeedLog.totalUploadBytes, UpTotalSteadyColor, UpTotalResetColor);
					break;
				case "Download":
					ServerDataGrid_UpdateAwareChangeCell(cell, serverSpeedLog.totalDownloadBytes, DownTotalSteadyColor, DownTotalResetColor);
					break;
				case "DownloadRaw":
					ServerDataGrid_UpdateAwareChangeCell(cell, serverSpeedLog.totalDownloadRawBytes, DownRawTotalSteadyColor, DownRawTotalResetColor);
					break;
				case "ConnectError":
				{
					var val = serverSpeedLog.errorConnectTimes + serverSpeedLog.errorDecodeTimes;
					var color = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 2.5), (byte)Math.Max(0, 255 - val * 2.5));
					SetCellBackColor(cell, color);
					SetCellText(cell, val);
					break;
				}
				case "ConnectTimeout":
					SetCellText(cell, serverSpeedLog.errorTimeoutTimes);
					break;
				case "ConnectEmpty":
				{
					var val = serverSpeedLog.errorEmptyTimes;
					var color = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
					SetCellBackColor(cell, color);
					SetCellText(cell, val);
					break;
				}
				case "Continuous":
				{
					var val = serverSpeedLog.errorContinurousTimes;
					var color = Color.FromArgb(255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
					SetCellBackColor(cell, color);
					SetCellText(cell, val);
					break;
				}
				case "ErrorPercent" when serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes > 0:
				{
					var percent = (serverSpeedLog.errorConnectTimes + serverSpeedLog.errorTimeoutTimes + serverSpeedLog.errorDecodeTimes)
									  * 100.00
									  / (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
					SetCellBackColor(cell, Color.FromArgb(255, (byte)(255 - percent * 2), (byte)(255 - percent * 2)));
					SetCellText(cell, percent.ToString("F0") + "%");
					break;
				}
				case "ErrorPercent":
					SetCellBackColor(cell, Color.White);
					SetCellText(cell, "-");
					break;
			}
		}




		private void ServerDataGrid_UpdateSpeedCell(DataGridViewCell cell, long speed)
		{
			var color = GetLevelColor(speed, SpeedLevelBytes, SpeedLevelColors);
			SetCellBackColor(cell, color);
			SetCellText(cell, FormatBytes(speed));
		}


		private void ServerDataGrid_UpdateAwareChangeCell(DataGridViewCell cell, long bytes, Color steadyColor, Color resetColor)
		{
			// update cell state
			var state = (CellState<long>)cell.Tag;
			if (state == null)
			{
				state = new CellState<long>(bytes);
				cell.Tag = state;
			}
			else
				state.UpdateValue(bytes);

			// choose awareness color or not
			var steady = state.UnchangedCount > 8;
			var reset = bytes == 0;
			var color = reset ? resetColor : steady ? steadyColor : Color.LightGreen;

			SetCellToolTipText(cell, bytes.ToString());
			SetCellText(cell, FormatBytes(bytes));
			SetCellBackColor(cell, color);
		}


	}




	internal class CellState<T>
	{
		public T LastValue;
		public int UnchangedCount;
		public bool Change;

		public CellState(T value)
		{
			LastValue = value;
			UnchangedCount = 0;
		}

		public void UpdateValue(T value)
		{
			if (LastValue.Equals(value))
			{
				++UnchangedCount;
				Change = false;
			}
			else
			{
				UnchangedCount = 0;
				Change = true;
				LastValue = value;
			}
		}
	}

}
