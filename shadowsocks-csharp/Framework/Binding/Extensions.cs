using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Shadowsocks.Framework.Binding
{
	internal static class Extensions
	{
		public static MenuItem Observe(this MenuItem menu, EventHandler handler)
		{
			return menu;
		}
	}
}
