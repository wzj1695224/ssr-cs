using System;
using System.Windows.Forms;
using Shadowsocks.Controller.ServerStat;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm
	{
		private class DoubleBufferListView : DataGridView
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
