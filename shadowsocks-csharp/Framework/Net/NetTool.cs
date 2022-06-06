using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;


namespace Shadowsocks.Framework.Net
{
	public static class NetTool
	{
        public static bool IsPortUsed(int port)
        {
            try
            {
                return IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners()
                    .Any(endPoint => endPoint.Port == port);
            }
            catch
            {
                return false;
            }
        }




        public static bool IsMatchSubnet(IPAddress ip, string netmask)
        {
            return Subnet.IsMatchSubnet(ip, netmask);
        }

        public static bool IsMatchSubnets(IPAddress ip, string[] netmasks)
        {
            return netmasks.Any(netmask => Subnet.IsMatchSubnet(ip, netmask));
        }

    }



    internal static class Subnet
    {
        internal static bool IsMatchSubnet(IPAddress ip, string netmask)
        {
            var mask = netmask.Split('/');
            var netmaskIp = IPAddress.Parse(mask[0]);
            if (ip.AddressFamily != netmaskIp.AddressFamily) return false;

            try
            {
	            return IsMatchSubnet(ip, netmaskIp, Convert.ToInt16(mask[1]));
            }
            catch
            {
	            // ignore
            }

            return false;
        }


        private static bool IsMatchSubnet(IPAddress ip, IPAddress net, int netmask)
        {
            var ipAddr = ip.GetAddressBytes();
            var netAddr = net.GetAddressBytes();
            int i = 8, index = 0;
            for (; i < netmask; i += 8, index += 1)
            {
                if (ipAddr[index] != netAddr[index])
                    return false;
            }
            return (ipAddr[index] >> (i - netmask)) == (netAddr[index] >> (i - netmask));
        }
    }




    internal static class NetworkType
    {
        // localhost
        private static readonly string[] LOCALHOST_4 = {
	        "127.0.0.0/8",
	        "169.254.0.0/16",
        };
        private static readonly string[] LOCALHOST_6 = {
            "::1/128"
        };

        // LAN
        private static readonly string[] LAN_4 = {
	        "0.0.0.0/8",
	        "10.0.0.0/8",
	        //"100.64.0.0/10", //部分地区运营商貌似在使用这个，这个可能不安全
	        "127.0.0.0/8",
	        "169.254.0.0/16",
	        "172.16.0.0/12",
	        //"192.0.0.0/24",
	        //"192.0.2.0/24",
	        "192.168.0.0/16",
	        //"198.18.0.0/15",
	        //"198.51.100.0/24",
	        //"203.0.113.0/24",
        };
        private static readonly string[] LAN_6 = {
	        "::1/128",
	        "fc00::/7",
	        "fe80::/10"
        };
        

        public static bool IsLocal(IPAddress ip)
        {
            var addr = ip.GetAddressBytes();

            switch (addr.Length)
            {
	            case 4:
		            return NetTool.IsMatchSubnets(ip, LOCALHOST_4);
	            case 16:
		            return NetTool.IsMatchSubnets(ip, LOCALHOST_6);
                default:
		            return true;
            }
        }


        // ReSharper disable once InconsistentNaming
        public static bool IsLAN(IPAddress ip)
        {
            var addr = ip.GetAddressBytes();

            switch (addr.Length)
            {
                case 4:
                    return NetTool.IsMatchSubnets(ip, LAN_4);
                case 16:
                    return NetTool.IsMatchSubnets(ip, LAN_6);
                default:
                    return true;
            }
        }
    }

}
