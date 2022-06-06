using Shadowsocks.Framework.Net;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;


namespace Shadowsocks.Controller
{
	public class Listener
    {
        public interface IService
        {
            bool Handle(byte[] firstPacket, int length, Socket socket);
        }

        Configuration _config;
        bool _shareOverLAN;
        string _authUser;
        string _authPass;
        Socket _socket_v4;
        Socket _socket_v6;
        bool _stop;
        private readonly IList<IService> _services;
        protected System.Timers.Timer timer;
        protected object timerLock = new object();

        public Listener(IList<IService> services)
        {
            this._services = services;
            _stop = false;
        }

        public IList<IService> GetServices()
        {
            return _services;
        }


        public bool isConfigChange(Configuration config)
        {
            try
            {
                if (this._shareOverLAN != config.shareOverLan
                    || _authUser != config.authUser
                    || _authPass != config.authPass
                    || _socket_v4 == null
                    || ((IPEndPoint)_socket_v4.LocalEndPoint).Port != config.localPort)
                {
                    return true;
                }
            }
            catch (Exception)
            { }
            return false;
        }




        public void Start(Configuration config, int port)
        {
            this._config = config;
            this._shareOverLAN = config.shareOverLan;
            this._authUser = config.authUser;
            this._authPass = config.authPass;
            _stop = false;

            var localPort = port == 0 ? _config.localPort : port;
            if (NetTool.IsPortUsed(localPort))
                throw new Exception(I18N.GetString("Port already in use"));

            try
            {
	            InitSocket(localPort, true);
            }
            catch (SocketException e)
            {
                Logging.LogUsefulException(e);
                if (_socket_v4 != null)
                {
                    _socket_v4.Close();
                    _socket_v4 = null;
                }
                if (_socket_v6 != null)
                {
                    _socket_v6.Close();
                    _socket_v6 = null;
                }
                throw;
            }
        }


        private void InitSocket(int localPort, bool ipv6)
        {
	        _socket_v4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	        _socket_v4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket_v4.Bind(new IPEndPoint(IPAddress.Any, localPort));
            _socket_v4.Listen(1024);
            _socket_v4.BeginAccept(AcceptCallback, _socket_v4);

            if (ipv6)
            {
                try
                {
                    _socket_v6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    //_socket_v6.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _socket_v6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                catch
                {
                    _socket_v6 = null;
                }

                if (_socket_v6 != null)
                {
                    _socket_v6.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));
                    _socket_v6.Listen(1024);
					_socket_v6.BeginAccept(AcceptCallback, _socket_v6);
                }
            }

            Console.WriteLine($@"ShadowsocksR started on port {localPort}");
        }




        public void Stop()
        {
            ResetTimeout(0, null);
            _stop = true;
            if (_socket_v4 != null)
            {
                _socket_v4.Close();
                _socket_v4 = null;
            }
            if (_socket_v6 != null)
            {
                _socket_v6.Close();
                _socket_v6 = null;
            }
        }

        private void ResetTimeout(Double time, Socket socket)
        {
            if (time <= 0 && timer == null)
                return;

            lock (timerLock)
            {
                if (time <= 0)
                {
                    if (timer != null)
                    {
                        timer.Enabled = false;
                        timer.Elapsed -= (sender, e) => timer_Elapsed(sender, e, socket);
                        timer.Dispose();
                        timer = null;
                    }
                }
                else
                {
                    if (timer == null)
                    {
                        timer = new System.Timers.Timer(time * 1000.0);
                        timer.Elapsed += (sender, e) => timer_Elapsed(sender, e, socket);
                        timer.Start();
                    }
                    else
                    {
                        timer.Interval = time * 1000.0;
                        timer.Stop();
                        timer.Start();
                    }
                }
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs eventArgs, Socket socket)
        {
            if (timer == null)
            {
                return;
            }
            var listener = socket;
            try
            {
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
                ResetTimeout(0, listener);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                ResetTimeout(5, listener);
            }
        }




        public void AcceptCallback(IAsyncResult ar)
        {
            if (_stop) return;

            var listener = (Socket)ar.AsyncState;
            try
            {
                var conn = listener.EndAccept(ar);

                // check Share Over LAN
                if (!_shareOverLAN && !conn.IsLocal())
                {
                    conn.Shutdown(SocketShutdown.Both);
                    conn.Close();
                }

                var localPort = ((IPEndPoint)conn.LocalEndPoint).Port;

                if ( string.IsNullOrEmpty(_authUser) 
                     && !conn.IsLAN()
                     && !( _config.GetPortMapCache().ContainsKey(localPort) 
                           || _config.GetPortMapCache()[localPort].type == PortMapType.Forward )
                   )
                {
                    conn.Shutdown(SocketShutdown.Both);
                    conn.Close();
                    return;
                }

                var buf = new byte[4096];
                var state = new object[] { conn, buf };

                if (!_config.GetPortMapCache().ContainsKey(localPort) || _config.GetPortMapCache()[localPort].type != PortMapType.Forward)
                {
                    conn.BeginReceive(buf, 0, buf.Length, 0, ReceiveCallback, state);
                }
                else
                {
                    foreach (var service in _services)
                    {
                        if (service.Handle(buf, 0, conn))
	                        return;
                    }

                    // no service found for this shouldn't happen
                    conn.Shutdown(SocketShutdown.Both);
                    conn.Close();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                try
                {
                    listener.BeginAccept(AcceptCallback, listener);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                    ResetTimeout(5, listener);
                }
            }
        }




        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (object[])ar.AsyncState;

            var conn = (Socket)state[0];
            var buf = (byte[])state[1];
            try
            {
                var bytesRead = conn.EndReceive(ar);
                foreach (var service in _services)
                {
                    if (service.Handle(buf, bytesRead, conn))
                    {
                        return;
                    }
                }
                // no service found for this
                // shouldn't happen
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Shutdown(SocketShutdown.Both);
                conn.Close();
            }
        }
    }
}
