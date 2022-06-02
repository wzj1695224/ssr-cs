using Shadowsocks.Model;
using System.Net;


namespace Shadowsocks.Controller
{
	public class ClientFactory
	{
		public static WebClient CreateClient(Configuration config, bool useProxy)
        {
            var userAgent = config.proxyUserAgent;
            if (string.IsNullOrEmpty(config.proxyUserAgent))
                userAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36";

            var client = new WebClient();
            
            // headers
            client.Headers.Add("User-Agent", userAgent);

            // proxy
            if (useProxy)
            {
                var proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
                if (!string.IsNullOrEmpty(config.authPass))
                    proxy.Credentials = new NetworkCredential(config.authUser, config.authPass);

                client.Proxy = proxy;
            }

            // others
            client.QueryString["rnd"] = Util.Utils.RandUInt32().ToString();

            return client;
        }
    }
}
