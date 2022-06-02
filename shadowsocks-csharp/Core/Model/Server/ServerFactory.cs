using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Shadowsocks.Core.Model.Server
{
	public static class ServerFactory
	{
		private static readonly IServerParser SS = new SS();
		private static readonly IServerParser SSR = new SSR();


		public static Server Create(string ssUrl, string forceGroup, bool throwIfFail = true)
		{
			Server server = null;
			try
			{
				server = DoCreate(ssUrl);
			}
			catch
			{
				if (throwIfFail)
					throw;
			}

			if (server == null && throwIfFail)
				throw new FormatException();

			// TODO remove
			if (server != null)
			{
				if (!string.IsNullOrEmpty(forceGroup))
					server.group = forceGroup;
				else if (server.group == null || server.group == "FreeSSR-public")
					server.group = "";
			}

			return server;
		}


		private static Server DoCreate(string url)
		{
			if (url.StartsWith("ss://", StringComparison.OrdinalIgnoreCase))
				return SS.Parse(new Server(), url);

			if (url.StartsWith("ssr://", StringComparison.OrdinalIgnoreCase))
				return SSR.Parse(new Server(), url);

			return null;
		}
	}




	internal interface IServerParser
	{
		Server Parse(Server server, string url);
	}




	internal class SS : IServerParser
	{
		private readonly Regex _urlFind = new Regex("^(?i)ss://([A-Za-z0-9+-/=_]+)(#(.+))?", RegexOptions.IgnoreCase);
		private readonly Regex _parser = new Regex("^((?<method>.+):(?<password>.*)@(?<hostname>.+?):(?<port>\\d+?))$", RegexOptions.IgnoreCase);


		public Server Parse(Server server, string url)
		{
			var match = _urlFind.Match(url);
			if (!match.Success)
				throw new FormatException();

			var base64 = match.Groups[1].Value;
			var data = Base64.DecodeToString(base64, "");
			match = _parser.Match(data);

			server.protocol = "origin";
			server.method = match.Groups["method"].Value;
			server.password = match.Groups["password"].Value;
			server.server = match.Groups["hostname"].Value;
			server.server_port = ushort.Parse(match.Groups["port"].Value);

			return server;
		}
	}




	internal class SSR : IServerParser
	{
		private readonly Regex _parser = new Regex("^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]+)");


		public Server Parse(Server server, string url)
		{
			var data = GetData(url);

			var paramsDict = new Dictionary<string, string>();
			{
				var paramStartPos = data.IndexOf("?", StringComparison.Ordinal);
				if (paramStartPos > 0)
				{
					paramsDict = ParseParam(data.Substring(paramStartPos + 1));
					data = data.Substring(0, paramStartPos);
				}
			}

			var pos = data.IndexOf("/", StringComparison.Ordinal);
			if (pos >= 0)
				data = data.Substring(0, pos);

			var match = _parser.Match(data);
			if (match == null || !match.Success)
				throw new FormatException();

			server.server = match.Groups[1].Value;
			server.server_port = ushort.Parse(match.Groups[2].Value);
			var protocol = match.Groups[3].Value.Length == 0 ? "origin" : match.Groups[3].Value;
			server.protocol = protocol.Replace("_compatible", "");
			server.method = match.Groups[4].Value;
			var obfs = match.Groups[5].Value.Length == 0 ? "plain" : match.Groups[5].Value;
			server.obfs = obfs.Replace("_compatible", "");
			server.password = Base64.DecodeStandardSSRUrlSafeBase64(match.Groups[6].Value);

			if (paramsDict.ContainsKey("protoparam"))
				server.protocolparam = Base64.DecodeStandardSSRUrlSafeBase64(paramsDict["protoparam"]);
			if (paramsDict.ContainsKey("obfsparam"))
				server.obfsparam = Base64.DecodeStandardSSRUrlSafeBase64(paramsDict["obfsparam"]);
			if (paramsDict.ContainsKey("remarks"))
				server.remarks = Base64.DecodeStandardSSRUrlSafeBase64(paramsDict["remarks"]);
			server.group = paramsDict.ContainsKey("group") ? Base64.DecodeStandardSSRUrlSafeBase64(paramsDict["group"]) : "";
			if (paramsDict.ContainsKey("uot"))
				server.udp_over_tcp = int.Parse(paramsDict["uot"]) != 0;
			if (paramsDict.ContainsKey("udpport"))
				server.server_udp_port = ushort.Parse(paramsDict["udpport"]);

			return server;
		}


		private static string GetData(string url)
		{
			// ssr://host:port:protocol:method:obfs:base64pass/?obfsparam=base64&remarks=base64&group=base64&udpport=0&uot=1
			var match = Regex.Match(url, "ssr://([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
			if (!match.Success)
				throw new FormatException();

			var base64 = match.Groups[1].Value;
			return  Base64.DecodeUrlSafeBase64(base64);
		}


		private static Dictionary<string, string> ParseParam(string paramStr)
		{
			var dict = new Dictionary<string, string>();

			var @params = paramStr.Split('&');
			foreach (var param in @params)
			{
				var index = param.IndexOf('=');
				if (index <= 0) continue;
				
				var key = param.Substring(0, index);
				var val = param.Substring(index + 1);
				dict[key] = val;
			}

			return dict;
		}
	}
}
