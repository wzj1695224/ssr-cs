using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Shadowsocks.Controller;
using Shadowsocks.Model;


namespace Shadowsocks.Core.Model.Server
{
	[Serializable]
	public class Server
	{
		public string id;
		public string server;
		public ushort server_port;
		public ushort server_udp_port;
		public string password;
		public string method;
		public string protocol;
		public string protocolparam;
		public string obfs;
		public string obfsparam;
		public string remarks_base64;
		public string group;
		public bool enable;
		public bool udp_over_tcp;

		private object protocoldata;
		private object obfsdata;
		private ServerSpeedLog serverSpeedLog = new ServerSpeedLog();
		private DnsBuffer dnsBuffer = new DnsBuffer();
		private Connections Connections = new Connections();
		private static Server forwardServer = new Server();

		public void CopyServer(Server Server)
		{
			protocoldata = Server.protocoldata;
			obfsdata = Server.obfsdata;
			serverSpeedLog = Server.serverSpeedLog;
			dnsBuffer = Server.dnsBuffer;
			Connections = Server.Connections;
			enable = Server.enable;
		}

		public void CopyServerInfo(Server Server)
		{
			remarks = Server.remarks;
			group = Server.group;
		}

		public static Server GetForwardServerRef()
		{
			return forwardServer;
		}

		public void SetConnections(Connections Connections)
		{
			this.Connections = Connections;
		}

		public Connections GetConnections()
		{
			return Connections;
		}

		public DnsBuffer DnsBuffer()
		{
			return dnsBuffer;
		}

		public ServerSpeedLog ServerSpeedLog()
		{
			return serverSpeedLog;
		}
		public void SetServerSpeedLog(ServerSpeedLog log)
		{
			serverSpeedLog = log;
		}

		public string remarks
		{
			get
			{
				if (remarks_base64.Length == 0)
				{
					return string.Empty;
				}
				try
				{
					return Util.Base64.DecodeUrlSafeBase64(remarks_base64);
				}
				catch (FormatException)
				{
					var old = remarks_base64;
					remarks = remarks_base64;
					return old;
				}
			}
			set
			{
				remarks_base64 = Util.Base64.EncodeUrlSafeBase64(value);
			}
		}


		public string HiddenName(bool hide = true)
		{
			return DecorName(hide);
		}


		public string DecorName(bool mask = false)
		{
			if (string.IsNullOrEmpty(server))
				return I18N.GetString("New server");

			var host = mask ? Util.ServerName.HideServerAddr(server) : server;

			if (string.IsNullOrEmpty(remarks_base64))
			{
				var containColon = server.IndexOf(':') >= 0;
				return containColon ?
					$"[{host}]:{server_port}" :
					$"{host}:{server_port}";
			}
			else
			{
				var containColon = server.IndexOf(':') >= 0;
				return containColon ?
					$"{remarks} ([{host}]:{server_port})" :
					$"{remarks} ({host}:{server_port})";
			}
		}


		public Server Clone()
		{
			Server ret = new Server();
			ret.server = server;
			ret.server_port = server_port;
			ret.password = password;
			ret.method = method;
			ret.protocol = protocol;
			ret.obfs = obfs;
			ret.obfsparam = obfsparam ?? "";
			ret.remarks_base64 = remarks_base64;
			ret.group = group;
			ret.enable = enable;
			ret.udp_over_tcp = udp_over_tcp;
			ret.id = id;
			ret.protocoldata = protocoldata;
			ret.obfsdata = obfsdata;
			return ret;
		}

		public Server()
		{
			server = "server host";
			server_port = 8388;
			method = "aes-256-cfb";
			protocol = "origin";
			protocolparam = "";
			obfs = "plain";
			obfsparam = "";
			password = "0";
			remarks_base64 = "";
			group = "FreeSSR-public";
			udp_over_tcp = false;
			enable = true;
			byte[] id = new byte[16];
			Util.Utils.RandBytes(id, id.Length);
			this.id = BitConverter.ToString(id).Replace("-", "");
		}
		

		public bool isMatchServer(Server server)
		{
			return this.server == server.server
			       && server_port == server.server_port
			       && server_udp_port == server.server_udp_port
			       && method == server.method
			       && protocol == server.protocol
			       && protocolparam == server.protocolparam
			       && obfs == server.obfs
			       && obfsparam == server.obfsparam
			       && password == server.password
			       && udp_over_tcp == server.udp_over_tcp;
		}
		

		public string GetSSLinkForServer()
		{
			string parts = method + ":" + password + "@" + server + ":" + server_port;
			string base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(parts)).Replace("=", "");
			return "ss://" + base64;
		}

		public string GetSSRLinkForServer()
		{
			string main_part = server + ":" + server_port + ":" + protocol + ":" + method + ":" + obfs + ":" + Util.Base64.EncodeUrlSafeBase64(password);
			string param_str = "obfsparam=" + Util.Base64.EncodeUrlSafeBase64(obfsparam ?? "");
			if (!string.IsNullOrEmpty(protocolparam))
				param_str += "&protoparam=" + Util.Base64.EncodeUrlSafeBase64(protocolparam);
			if (!string.IsNullOrEmpty(remarks))
				param_str += "&remarks=" + Util.Base64.EncodeUrlSafeBase64(remarks);
			if (!string.IsNullOrEmpty(group))
				param_str += "&group=" + Util.Base64.EncodeUrlSafeBase64(group);
			if (udp_over_tcp)
				param_str += "&uot=" + "1";
			if (server_udp_port > 0)
				param_str += "&udpport=" + server_udp_port.ToString();
			string base64 = Util.Base64.EncodeUrlSafeBase64(main_part + "/?" + param_str);
			return "ssr://" + base64;
		}


		public object getObfsData()
		{
			return this.obfsdata;
		}
		public void setObfsData(object data)
		{
			this.obfsdata = data;
		}

		public object getProtocolData()
		{
			return this.protocoldata;
		}
		public void setProtocolData(object data)
		{
			this.protocoldata = data;
		}
	}
}