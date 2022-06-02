using System;


namespace Shadowsocks.Core.Model.Server
{
	public static class ServerFactory
	{
		public static Server Create(string ssUrl, string forceGroup, bool throwIfFail = true)
		{
			if (ssUrl.StartsWith("ss://", StringComparison.OrdinalIgnoreCase))
			{
				var server = new Server();
				server.ServerFromSS(ssUrl, forceGroup);
				return server;
			}

			if (ssUrl.StartsWith("ssr://", StringComparison.OrdinalIgnoreCase))
			{
				var server = new Server();
				server.ServerFromSSR(ssUrl, forceGroup);
				return server;
			}
			
			if (throwIfFail)
				throw new FormatException();
			return null;
		}
	}
}
