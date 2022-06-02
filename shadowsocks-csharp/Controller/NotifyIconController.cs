using Shadowsocks.Core;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;


namespace Shadowsocks.Controller
{
	public interface INotifyIconController
	{
        void UpdateTrayIcon();

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout);

    }




    internal class NotifyIconController : INotifyIconController
	{
		private readonly NotifyIcon _notifyIcon;

        private readonly BalloonTipManager _balloonTipManager = new BalloonTipManager();

        // Reference
        private readonly IEventBus _eventBus;
		private readonly ShadowsocksController _controller;
		private readonly MenuViewController _menuController;


		public NotifyIconController(IEventBus eventBus, ShadowsocksController controller, MenuViewController menuController)
		{
            _eventBus = eventBus;
			_controller = controller;
			_menuController = menuController;

            // init tray icon
			_notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
			_notifyIcon.ContextMenu = menuController.TrayIconMenu;
			_notifyIcon.MouseClick += menuController.TrayIconClick;

            InitEvents();
        }


        private void InitEvents()
        {
            _eventBus.OnAppExit += (sender, code) => _notifyIcon.Visible = false;
        }




        public void UpdateTrayIcon()
        {
            var config = _controller.GetCurrentConfiguration();
            var proxyMode = (ProxyMode)config.sysProxyMode;

            try
            {
                _notifyIcon.Icon = ResourceFactory.CreateStaticTrayIcon();
            }
            catch
            {
                _notifyIcon.Icon = ResourceFactory.CreateTrayIcon(proxyMode, config.random);
            }

            string text;
            if (proxyMode == ProxyMode.Global)
                text = I18N.GetString("Global");
            else if (proxyMode == ProxyMode.Pac)
                text = I18N.GetString("PAC");
            else
                text = I18N.GetString("Disable system proxy");

            text += "\r\n" + string.Format(I18N.GetString("Running: Port {0}"), config.localPort);

            _notifyIcon.Text = text.Substring(0, Math.Min(63, text.Length));
        }




        public void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

    }




    internal class BalloonTipManager
    {
        private int _id = 0;
        private Dictionary<int, EventHandler> _balloonTipClicked = new Dictionary<int, EventHandler>();


        internal int RegisterBalloonTipClicked(EventHandler balloonTipClicked)
        {
            if (balloonTipClicked == null) return -1;

	        var id = Interlocked.Increment(ref _id);
            _balloonTipClicked.Add(id, balloonTipClicked);
            return id;
        }
    }
}
