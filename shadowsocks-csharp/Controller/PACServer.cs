using Shadowsocks.Framework.Net;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Shadowsocks.Controller
{
	internal class PACServer : Listener.IService
    {
        public static string PAC_FILE = "pac.txt";

        public static string USER_RULE_FILE = "user-rule.txt";

        public static string USER_ABP_FILE = "abp.txt";

        FileSystemWatcher PACFileWatcher;
        FileSystemWatcher UserRuleFileWatcher;
        private Configuration _config;

        public event EventHandler PACFileChanged;
        public event EventHandler UserRuleFileChanged;


        public PACServer()
        {
            this.WatchPacFile();
            this.WatchUserRuleFile();
        }


        public void UpdateConfiguration(Configuration config)
        {
            this._config = config;
        }




        public bool Handle(byte[] firstPacket, int length, Socket socket)
        {
            try
            {
                var request = Encoding.UTF8.GetString(firstPacket, 0, length);
                var lines = request.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                // context var
                bool hostMatch = false, pathMatch = false;
                var socksType = 0;
                string proxy = null;

                foreach (var line in lines)
                {
                    var kv = line.Split(new[]{':'}, 2);
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "Host")
                        {
                            if (kv[1].Trim() == ((IPEndPoint)socket.LocalEndPoint).ToString())
                            {
                                hostMatch = true;
                            }
                        }
                        else if (kv[0] == "User-Agent")
                        {
                            // we need to drop connections when changing servers
                            /* if (kv[1].IndexOf("Chrome") >= 0)
                            {
                                useSocks = true;
                            } */
                        }
                    }
                    else if (kv.Length == 1)
                    {
                        if (!socket.IsLocal() || line.IndexOf("auth=" + _config.localAuthPassword) > 0)
                        {
	                        var isPACRequest = line.IndexOf(" /pac?", StringComparison.Ordinal) > 0 && line.StartsWith("GET");
	                        if ( isPACRequest )
                            {
                                var url = line.Substring(line.IndexOf(" ") + 1);
                                url = url.Substring(0, url.IndexOf(" "));
                                pathMatch = true;

                                var port_pos = url.IndexOf("port=", StringComparison.Ordinal);
                                if (port_pos > 0)
                                {
                                    var port = url.Substring(port_pos + 5);
                                    if (port.IndexOf("&") >= 0)
                                    {
                                        port = port.Substring(0, port.IndexOf("&"));
                                    }

                                    var ip_pos = url.IndexOf("ip=");
                                    if (ip_pos > 0)
                                    {
                                        proxy = url.Substring(ip_pos + 3);
                                        if (proxy.IndexOf("&") >= 0)
                                        {
                                            proxy = proxy.Substring(0, proxy.IndexOf("&"));
                                        }
                                        proxy += ":" + port + ";";
                                    }
                                    else
                                    {
                                        proxy = "127.0.0.1:" + port + ";";
                                    }
                                }

                                if (url.IndexOf("type=socks4") > 0 || url.IndexOf("type=s4") > 0)
                                {
                                    socksType = 4;
                                }
                                if (url.IndexOf("type=socks5") > 0 || url.IndexOf("type=s5") > 0)
                                {
                                    socksType = 5;
                                }
                            }
                        }
                    }
                }
                
                if (hostMatch && pathMatch)
                {
                    SendResponse(firstPacket, length, socket, socksType, proxy);
                    return true;
                }
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }




        private void SendResponse(byte[] firstPacket, int length, Socket socket, int socksType, string setProxy)
        {
            try
            {
                var pac = CreatePACScript(firstPacket, length, socket, socksType, setProxy);
                var response = PACFactory.CreatePACResponse(pac);
                socket.BeginSend(response, 0, response.Length, 0, CloseAfterSent, socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }


        private string CreatePACScript(byte[] firstPacket, int length, Socket socket, int socksType, string setProxy)
        {
	        var pac = PACFactory.GetPACScript();

	        var localEndPoint = (IPEndPoint)socket.LocalEndPoint;

	        var proxy =
		        setProxy == null ? GetPACAddress(firstPacket, length, localEndPoint, socksType) :
		        socksType == 5 ? "SOCKS5 " + setProxy :
		        socksType == 4 ? "SOCKS " + setProxy :
		        "PROXY " + setProxy;

            // __DIRECT__
	        if (_config.pacDirectGoProxy && _config.proxyEnable)
	        {
		        if (_config.proxyType == 0)
			        pac = pac.Replace("__DIRECT__", "SOCKS5 " + _config.proxyHost + ":" + _config.proxyPort + ";DIRECT;");
		        else if (_config.proxyType == 1)
			        pac = pac.Replace("__DIRECT__", "PROXY " + _config.proxyHost + ":" + _config.proxyPort + ";DIRECT;");
	        }
	        else
		        pac = pac.Replace("__DIRECT__", "DIRECT;");

            // __PROXY__
	        pac = pac.Replace("__PROXY__", proxy + "DIRECT;");

	        return pac;
        }




        private static void CloseAfterSent(IAsyncResult ar)
        {
            var conn = (Socket)ar.AsyncState;
            try
            {
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
            catch
            {
	            // ignored
            }
        }




        public string TouchPACFile()
        {
            if (File.Exists(PAC_FILE))
            {
                return PAC_FILE;
            }
            else
            {
                FileManager.UncompressFile(PAC_FILE, Resources.proxy_pac_txt);
                return PAC_FILE;
            }
        }

        internal string TouchUserRuleFile()
        {
            if (File.Exists(USER_RULE_FILE))
            {
                return USER_RULE_FILE;
            }
            else
            {
                File.WriteAllText(USER_RULE_FILE, Resources.user_rule);
                return USER_RULE_FILE;
            }
        }




        private void WatchPacFile()
        {
	        PACFileWatcher?.Dispose();

            PACFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory())
            {
	            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
	            EnableRaisingEvents = true,
	            Filter = PAC_FILE
            };

            PACFileWatcher.Changed += Watcher_Changed;
            PACFileWatcher.Created += Watcher_Changed;
            PACFileWatcher.Deleted += Watcher_Changed;
            PACFileWatcher.Renamed += Watcher_Changed;
        }


        private void WatchUserRuleFile()
        {
	        UserRuleFileWatcher?.Dispose();

            UserRuleFileWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory())
            {
	            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
	            EnableRaisingEvents = true,
	            Filter = USER_RULE_FILE
            };

            UserRuleFileWatcher.Changed += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Created += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Deleted += UserRuleFileWatcher_Changed;
            UserRuleFileWatcher.Renamed += UserRuleFileWatcher_Changed;
        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
	        PACFileChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UserRuleFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
	        UserRuleFileChanged?.Invoke(this, EventArgs.Empty);
        }


        private string GetPACAddress(byte[] requestBuf, int length, IPEndPoint localEndPoint, int socksType)
        {
	        // try
         //    {
         //        string requestString = Encoding.UTF8.GetString(requestBuf);
         //        if (requestString.IndexOf("AppleWebKit") >= 0)
         //        {
         //            string address = "" + localEndPoint.Address + ":" + config.GetCurrentServer().local_port;
         //            proxy = "SOCKS5 " + address + "; SOCKS " + address + ";";
         //        }
         //    }
         //    catch (Exception e)
         //    {
         //        Console.WriteLine(e);
         //    }
         switch (socksType)
            {
	            case 5:
		            return "SOCKS5 " + localEndPoint.Address + ":" + this._config.localPort + ";";
	            case 4:
		            return "SOCKS " + localEndPoint.Address + ":" + this._config.localPort + ";";
	            default:
		            return "PROXY " + localEndPoint.Address + ":" + this._config.localPort + ";";
            }
        }
    }




	internal static class PACFactory
	{
        private const string PACFile = "pac.txt";

        private const string PACRespTemplate = 
@"HTTP/1.1 200 OK
Server: ShadowsocksR
Content-Type: application/x-ns-proxy-autoconfig
Content-Length: {0}
Connection: Close

";


		public static byte[] CreatePACResponse(string script)
		{
			var body = Encoding.UTF8.GetBytes(script);

			var headerText = string.Format(PACRespTemplate, body.Length);
			var header = Encoding.UTF8.GetBytes(headerText);

            var resp = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, resp, 0, header.Length);
            Buffer.BlockCopy(body, 0, resp, header.Length, body.Length);
            return resp;
        }



        public static string GetPACScript()
        {
            if (File.Exists(PACFile))
                return File.ReadAllText(PACFile, Encoding.UTF8);
            return Utils.UnGzip(Resources.proxy_pac_txt);
        }

    }

}
