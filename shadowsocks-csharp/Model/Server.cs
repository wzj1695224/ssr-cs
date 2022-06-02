using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
#if !_CONSOLE
using SimpleJson;
#endif
using Shadowsocks.Controller;
using System.Net;
using System.Net.Sockets;
using Shadowsocks.Encryption;

namespace Shadowsocks.Model
{
    public class DnsBuffer
    {
        public IPAddress ip;
        public DateTime updateTime;
        public string host;
        public bool force_expired;
         public bool isExpired(string host)
        {
            if (updateTime == null) return true;
            if (this.host != host) return true;
            if (force_expired && (DateTime.Now - updateTime).TotalMinutes > 1) return true;
            return (DateTime.Now - updateTime).TotalMinutes > 30;
        }
        public void UpdateDns(string host, IPAddress ip)
        {
            updateTime = DateTime.Now;
            this.ip = new IPAddress(ip.GetAddressBytes());
            this.host = host;
            force_expired = false;
        }
    }

    public class Connections
    {
        private System.Collections.Generic.Dictionary<IHandler, Int32> sockets = new Dictionary<IHandler, int>();
        public bool AddRef(IHandler socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] += 1;
                }
                else
                {
                    sockets[socket] = 1;
                }
                return true;
            }
        }
        public bool DecRef(IHandler socket)
        {
            lock (this)
            {
                if (sockets.ContainsKey(socket))
                {
                    sockets[socket] -= 1;
                    if (sockets[socket] == 0)
                    {
                        sockets.Remove(socket);
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
        }
        public void CloseAll()
        {
            IHandler[] s;
            lock (this)
            {
                s = new IHandler[sockets.Count];
                sockets.Keys.CopyTo(s, 0);
            }
            foreach (IHandler handler in s)
            {
                try
                {
                    handler.Shutdown();
                }
                catch
                {

                }
            }
        }
        public int Count
        {
            get
            {
                return sockets.Count;
            }
        }
    }
}
