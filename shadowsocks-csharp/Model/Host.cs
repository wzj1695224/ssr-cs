using System;
using System.Collections.Generic;
using System.Net;


namespace Shadowsocks.Model
{
	internal class HostNode
    {
        public bool IncludeSub;
        public string Addr;
        public Dictionary<string, HostNode> Subnode;

        public HostNode()
        {
            IncludeSub = false;
            Addr = "";
            Subnode = new Dictionary<string, HostNode>();
        }

        public HostNode(bool sub, string addr)
        {
            IncludeSub = sub;
            this.Addr = addr;
            Subnode = null;
        }
    }




    public class HostMap
    {
        Dictionary<string, HostNode> root = new Dictionary<string, HostNode>();
        IPSegment ips = new IPSegment("remoteproxy");

        static HostMap instance = new HostMap();
        const string HOST_FILENAME = "user.rule";


        public static HostMap Instance()
        {
            return instance;
        }


        public void Clear(HostMap newInstance)
        {
	        instance = newInstance ?? new HostMap();
        }




        public bool LoadHostFile()
        {
            var filePath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, HOST_FILENAME);

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    LoadHostFile(filePath);
                    return true;
                }
            }
            catch
            {
                // ignored
            }
            return false;
        }


        private void LoadHostFile(string filePath)
        {
            using (var stream = System.IO.File.OpenText(filePath))
            {
                while (true)
                {
                    var line = stream.ReadLine();
                    if (line == null)
                        break;
                    if (line.Length > 0 && line.StartsWith("#"))
                        continue;
                    var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;
                    AddHost(parts[0], parts[1]);
                }
            }
        }




        public void AddHost(string host, string addr)
        {
	        if (IPAddress.TryParse(host, out _))
            {
                var addrParts = addr.Split(new char[] { ' ', '\t', }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (addrParts.Length >= 2)
                {
	                var ipStart = new IPAddressCmp(host);
	                var ipEnd = new IPAddressCmp(addrParts[0]);
	                ips.Insert(ipStart, ipEnd, addrParts[1]);
                    return;
                }
            }

            var parts = host.Split('.');
            var node = root;

            var includeSub = false;
            var end = 0;
            if (parts[0].Length == 0)
            {
                end = 1;
                includeSub = true;
            }

            for (var i = parts.Length - 1; i > end; --i)
            {
                if (!node.ContainsKey(parts[i]))
	                node[parts[i]] = new HostNode();

                if (node[parts[i]].Subnode == null)
	                node[parts[i]].Subnode = new Dictionary<string, HostNode>();

                node = node[parts[i]].Subnode;
            }
            node[parts[end]] = new HostNode(includeSub, addr);
        }




        public bool GetHost(string host, out string addr)
        {
            addr = null;
            var parts = host.Split('.');

            var node = root;
            for (var i = parts.Length - 1; i >= 0; --i)
            {
	            var part = parts[i];
	            if (!node.ContainsKey(part))
	                return false;
                
                if (node[part].Addr.Length > 0 || node[part].IncludeSub)
                {
                    addr = node[part].Addr;
                    return true;
                }
                if (node.ContainsKey("*"))
                {
                    addr = node["*"].Addr;
                    return true;
                }

                if (node[part].Subnode == null)
	                return false;
                
                node = node[part].Subnode;
            }
            return false;
        }




        public bool GetIP(IPAddress ip, out string addr)
        {
            var host = ip.ToString();
            addr = ips.Get(new IPAddressCmp(host)) as string;
            return addr != null;
        }

    }
}
