using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.Core
{
	public interface IMenuClickService
	{
		/// <summary>
		/// Call when a menu click
		/// </summary>
		event EventHandler OnMenuClick;
	}
	
}
