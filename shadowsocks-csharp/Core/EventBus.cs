using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.Core
{
	internal interface IEventBus
	{
		/// <summary>
		/// Application Exit
		/// </summary>
		event EventHandler<int> OnAppExit;

		void NotifyAppExit(object sender, int code);
	}




	internal class EventBus : IEventBus
	{
		public event EventHandler<int> OnAppExit;


		public void NotifyAppExit(object sender, int code)
		{
			OnAppExit?.Invoke(sender, code);
		}
	}
}
