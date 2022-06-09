using Shadowsocks.Controller.ServerStat;
using Shadowsocks.Framework.Windows;
using System;
using System.Windows.Forms;
using static Shadowsocks.Controller.I18N.Static;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm
	{

		private void SetupServerDataGrid()
		{
			Column = new ServerDataGridColumns(ServerDataGrid);

			var mul = DPI.DpiMul;

			foreach (DataGridViewColumn column in ServerDataGrid.Columns)
			{
				// header text 
				column.HeaderText = S(column.HeaderText);

				// width with dpi
				column.Width = column.Width * mul / 4;
			}

			// TODO change font
			// var font = Fonts.FontAwesome(8);
			// Column.DoForAll(col => col.HeaderCell.Style.Font = font);
			// ServerDataGrid.Columns[9].HeaderText = FontAwesomeIcons.ArrowDownLong + " Avg";

			ServerDataGrid.RowTemplate.Height = 20 * mul / 4;
		}








		private static class CellState
		{
			public static T Get<T>(DataGridViewCell cell) where T : Base
			{
				var stateType = typeof(T);
				if (cell.Tag == null || cell.Tag.GetType() != stateType)
					return null;
				return (T)cell.Tag;
			}


			public static T GetOrCreate<T>(DataGridViewCell cell) where T : Base
			{
				var stateType = typeof(T);
				if (cell.Tag == null || cell.Tag.GetType() != stateType)
					cell.Tag = Activator.CreateInstance(stateType);
				return (T)cell.Tag;
			}




			internal class Base
			{

			}




			internal class Ping : Base
			{
				public const long TIMEOUT_TIME   = int.MaxValue;
				public const long NOT_PING_TIME  = TIMEOUT_TIME - 1;
				public const long PINGING_TIME   = TIMEOUT_TIME - 2;


				public PingState State = PingState.NotPing;
				public long      Time  = NOT_PING_TIME;


				public void IsPinging()
				{
					State = PingState.Pinging;
					Time = PINGING_TIME;
				}

				public void Timeout()
				{
					State = PingState.Timeout;
					Time = TIMEOUT_TIME;
				}

				public void Complete(long time)
				{
					State = PingState.Complete;
					Time = time;
				}
			}
		}

	}


}
