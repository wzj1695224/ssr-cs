using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Net;


namespace Shadowsocks.Controller
{
	public class UpdateFreeNode
    {
	    public event EventHandler NewFreeNodeFound;
        public string FreeNodeResult;
        public ServerSubscribe subscribeTask;
        public bool Notify;

        public const string Name = "ShadowsocksR";


        public void CheckUpdate(Configuration config, ServerSubscribe subscribeTask, bool userProxy, bool notify)
        {
            try
            {
                var client = ClientFactory.CreateClient(config, userProxy);
                
                this.subscribeTask = subscribeTask;
				this.Notify = notify;
                this.FreeNodeResult = null;

                var url = subscribeTask.URL;
                
                if ( url.StartsWith("https", StringComparison.OrdinalIgnoreCase) )
	                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls;

                client.DownloadStringCompleted += http_DownloadStringCompleted;
                client.DownloadStringAsync(new Uri(url));
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }


        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                FreeNodeResult = e.Result;
                NewFreeNodeFound?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                if (e.Error != null)
                {
                    Logging.Debug(e.Error.ToString());
                }
                Logging.Debug(ex.ToString());
                NewFreeNodeFound?.Invoke(this, EventArgs.Empty);
            }
        }
    }




    public class UpdateSubscribeManager
    {
        private Configuration _config;
        private UpdateFreeNode _updater;
        private List<ServerSubscribe> _serverSubscribes;
        private bool _useProxy;
        public string Url { get; private set; }
        public bool Notify;

        public void CreateTask(Configuration config, UpdateFreeNode updater, int index, bool useProxy, bool notify)
        {
	        if (_config != null) return;

	        _config = config;
	        _updater = updater;
	        _useProxy = useProxy;
	        Notify = notify;

	        if (index < 0)
	        {
		        _serverSubscribes = new List<ServerSubscribe>(config.serverSubscribes);
	        }
	        else if (index < _config.serverSubscribes.Count)
	        {
		        _serverSubscribes = new List<ServerSubscribe>();
		        _serverSubscribes.Add(config.serverSubscribes[index]);
	        }
	        Next();
        }


        public bool Next()
        {
            if (_serverSubscribes.Count == 0)
            {
                _config = null;
                return false;
            }

            Url = _serverSubscribes[0].URL;
            _updater.CheckUpdate(_config, _serverSubscribes[0], _useProxy, Notify);
            _serverSubscribes.RemoveAt(0);
            return true;
        }

    }
}
