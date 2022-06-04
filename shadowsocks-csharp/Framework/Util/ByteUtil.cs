using System;
using System.Collections.Generic;
using static Shadowsocks.Framework.Util.ByteUnit;


namespace Shadowsocks.Framework.Util
{
	public static class ByteUnit
	{
	    public const long K = 1024L;
		public const long M = K * 1024L;
		public const long G = M * 1024L;
		public const long T = G * 1024L;
		public const long P = T * 1024L;
		public const long E = P * 1024L;
	}




	public static class ByteUtil
	{
		private static readonly Dictionary<char, long> UNITS = new Dictionary<char, long>
		{
			{ 'K', K },
			{ 'M', M },
			{ 'G', G },
			{ 'T', T },
			{ 'P', P },
			{ 'E', E }
		};


		public static string FormatBytes(long bytes)
		{
			if (bytes >= M * 990)
			{
				if (bytes >= G * 990)
				{
					if (bytes >= P * 990)
						return (bytes / (double)E).ToString("F3") + "E";
					if (bytes >= T * 990)
						return (bytes / (double)P).ToString("F3") + "P";
					return (bytes / (double)T).ToString("F3") + "T";
				}
				else
				{
					if (bytes >= G * 99)
						return (bytes / (double)G).ToString("F2") + "G";
					if (bytes >= G * 9)
						return (bytes / (double)G).ToString("F3") + "G";
					return (bytes / (double)G).ToString("F4") + "G";
				}
			}
			else
			{
				if (bytes >= K * 990)
				{
					if (bytes >= M * 100)
						return (bytes / (double)M).ToString("F1") + "M";
					if (bytes > M * 9.9)
						return (bytes / (double)M).ToString("F2") + "M";
					return (bytes / (double)M).ToString("F3") + "M";
				}
				else
				{
					if (bytes > K * 99)
						return (bytes / (double)K).ToString("F0") + "K";
					if (bytes > 900)
						return (bytes / (double)K).ToString("F1") + "K";
					return bytes.ToString();
				}
			}
		}


		public static long ParseByteStr(string bytesStr)
		{
			if (string.IsNullOrEmpty(bytesStr))
				return -1;

			// check unit
			var last = bytesStr[bytesStr.Length - 1];
			if (!UNITS.TryGetValue(last, out var mul))
			{
				try
				{
					return (long)Convert.ToDouble(bytesStr);
				}
				catch
				{
					return -1;
				}
			}

			// parse num
			var numStr = bytesStr.Substring(0, bytesStr.Length - 1);
			var num = Convert.ToDouble(numStr);

			return (long)(num * mul);
		}
	}
}