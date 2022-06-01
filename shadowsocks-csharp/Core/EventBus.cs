using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.Core
{
	internal interface IEventBus
	{
		event Action<ProxyMode> OnProxyModeChange;
	}
}
