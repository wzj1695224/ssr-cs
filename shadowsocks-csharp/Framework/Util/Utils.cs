using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.Framework.Util
{
	public static class Utils
	{
		private const long K = 1024L;
		private const long M = K * 1024L;
		private const long G = M * 1024L;
		private const long T = G * 1024L;
		private const long P = T * 1024L;
		private const long E = P * 1024L;


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

    }
}
