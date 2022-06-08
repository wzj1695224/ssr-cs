using System;
using System.Collections.Generic;
using Shadowsocks.Controller;
using System.Net;


namespace Shadowsocks.Model
{
	public class DnsBuffer
    {
        public IPAddress IP;
        public string Host;
        public DateTime UpdateTime;
        public bool ForceExpired;
	    
        public bool IsExpired(string host)
        {
            if (UpdateTime == null) return true;
            if (this.Host != host) return true;
            if (ForceExpired && (DateTime.Now - UpdateTime).TotalMinutes > 1) return true;
            return (DateTime.Now - UpdateTime).TotalMinutes > 30;
        }
        
        public void UpdateDns(string host, IPAddress ip)
        {
            UpdateTime = DateTime.Now;
            this.IP = new IPAddress(ip.GetAddressBytes());
            this.Host = host;
            ForceExpired = false;
        }
    }




    public class Connections
    {
        public int Count => _sockets.Count;

        private readonly Dictionary<IHandler, int> _sockets = new Dictionary<IHandler, int>();


        public bool AddRef(IHandler socket)
        {
            lock (this)
            {
                if (_sockets.ContainsKey(socket))
	                _sockets[socket] += 1;
                else
	                _sockets[socket] = 1;
            }
	        return true;
        }


        public bool DecRef(IHandler socket)
        {
            lock (this)
            {
                if (!_sockets.ContainsKey(socket))
                    return false;

                _sockets[socket] -= 1;
                if (_sockets[socket] <= 0)
                    _sockets.Remove(socket);
                return true;
            }
        }


        public void CloseAll()
        {
            IHandler[] handlers;
            lock (this)
            {
                handlers = new IHandler[_sockets.Count];
                _sockets.Keys.CopyTo(handlers, 0);
            }

            foreach (var handler in handlers)
            {
                try
                {
                    handler.Shutdown();
                }
                catch
                {
                    // ignore
                }
            }
        }


    }
}
