using System;
using System.Net;
using System.Net.Sockets;


namespace Shadowsocks.Framework.Net
{
	public static class IPAddressExtension
	{
		public static bool IsLocalAddr(this IPAddress self)
		{
			return NetworkType.IsLocal(self);
		}

		public static bool IsLANAddr(this IPAddress self)
		{
			return NetworkType.IsLAN(self);
		}
	}


	public static class SocketExtension
	{
		/// <summary>
		/// Is connect from localhost
		/// </summary>
		public static bool IsLocal(this Socket self)
		{
			if ( !(self.RemoteEndPoint is IPEndPoint endPoint) )
				throw new Exception("Not a IP socket");
			return endPoint.Address.IsLocalAddr();
		}


		/// <summary>
		/// Is connect from LAN host
		/// </summary>
		public static bool IsLAN(this Socket self)
		{
			if (!(self.RemoteEndPoint is IPEndPoint endPoint))
				throw new Exception("Not a IP socket");
			return endPoint.Address.IsLANAddr();
		}
	}
}
