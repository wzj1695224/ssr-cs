using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace Shadowsocks.Controller
{
	public class I18N
	{
		protected static Dictionary<string, string> Strings = new Dictionary<string, string>();


		static I18N()
		{
			var langRes = GetLangRes();
			if (langRes == null) return;

			var lines = Regex.Split(langRes, "\r\n|\r|\n");
			foreach (var line in lines)
			{
				if (line.StartsWith("#")) continue;

				var kv = Regex.Split(line, "=");
				if (kv.Length != 2) continue;

				var val = Regex.Replace(kv[1], "\\\\n", "\r\n");
				Strings[kv[0]] = val;
			}
		}


		public static string GetString(string key)
		{
			return Strings.TryGetValue(key, out var value) ? value : key;
		}


		private static string GetLangRes()
		{
			var name = System.Globalization.CultureInfo.CurrentCulture.Name;

			if (name.StartsWith("zh"))
			{
				if (name == "zh" || name == "zh-CN")
					return Resources.cn;
				else
					return Resources.zh_tw;
			}
			else
				return null;
		}
	}
}