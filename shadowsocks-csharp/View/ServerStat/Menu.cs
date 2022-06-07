using System;
using System.Windows.Forms;
using Shadowsocks.Core.Model.Server;
using Shadowsocks.Model;
using static Shadowsocks.Framework.Windows.Forms.Menu.MenuFactory;


namespace Shadowsocks.View.ServerStat
{
	public partial class ServerStatForm
	{
        private MenuItem _clearMenu;
		private MenuItem _alwaysTopMenu;


		private void InitMenu()
		{
			this._alwaysTopMenu = CreateMenuItem("Always On &Top",   ToggleAlwaysOnTop);
			this._clearMenu     = CreateMenuItem("&Clear",           ClearItem_Click);

			this.Menu = new MainMenu(new[]
			{
				CreateMenuGroup("&Control", new[]
				{
					CreateMenuItem("&Disconnect direct connections", DisconnectForward_Click),
					CreateMenuItem("Disconnect &All",                Disconnect_Click),
					new MenuItem("-"),
					CreateMenuItem("Clear &MaxSpeed",                ClearMaxSpeed_Click),
					this._clearMenu,
					new MenuItem("-"),
					CreateMenuItem("Clear &Selected Total",          ClearSelectedTotal_Click),
					CreateMenuItem("Clear &Total",                   ClearTotal_Click),
				}),
				CreateMenuGroup("Port &out", new[]
				{
					CreateMenuItem("Copy current link",              copyLinkItem_Click),
					CreateMenuItem("Copy current group links",       copyGroupLinkItem_Click),
					CreateMenuItem("Copy all enable links",          copyEnableLinksItem_Click),
					CreateMenuItem("Copy all links",                 copyLinksItem_Click),
				}),
				CreateMenuGroup("Diagnostic", new[]
				{
					CreateMenuItem("Ping all server",                PingAllServer),
				}),
				CreateMenuGroup("&Window", new[]
				{
					CreateMenuItem("Auto &size",                     AutoSizeServerDataTable),
					this._alwaysTopMenu
				}),
			});
		}




		private void ToggleAlwaysOnTop(object sender, EventArgs e)
		{
			_alwaysTopMenu.Checked = !_alwaysTopMenu.Checked;
			this.TopMost = _alwaysTopMenu.Checked;
		}


		private void ClearItem_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			foreach (Server server in config.configs)
			{
				server.ServerSpeedLog().Clear();
			}
		}


		private void DisconnectForward_Click(object sender, EventArgs e)
		{
			Core.Model.Server.Server.GetForwardServerRef().GetConnections().CloseAll();
		}


		private void Disconnect_Click(object sender, EventArgs e)
		{
			_controller.DisconnectAllConnections();
			Core.Model.Server.Server.GetForwardServerRef().GetConnections().CloseAll();
		}


		private void ClearMaxSpeed_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			foreach (Server server in config.configs)
			{
				server.ServerSpeedLog().ClearMaxSpeed();
			}
		}


		private void ClearSelectedTotal_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			if (config.index >= 0 && config.index < config.configs.Count)
			{
				try
				{
					_controller.ClearTransferTotal(config.configs[config.index].server);
				}
				catch { }
			}
		}


		private void ClearTotal_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			foreach (Server server in config.configs)
			{
				_controller.ClearTransferTotal(server.server);
			}
		}


		private void copyLinkItem_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			if (config.index >= 0 && config.index < config.configs.Count)
			{
				try
				{
					string link = config.configs[config.index].GetSSRLinkForServer();
					Clipboard.SetText(link);
				}
				catch { }
			}
		}


		private void copyGroupLinkItem_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			if (config.index >= 0 && config.index < config.configs.Count)
			{
				string group = config.configs[config.index].group;
				string link = "";
				for (int index = 0; index < config.configs.Count; ++index)
				{
					if (config.configs[index].group != group)
						continue;
					link += config.configs[index].GetSSRLinkForServer() + "\r\n";
				}
				try
				{
					Clipboard.SetText(link);
				}
				catch { }
			}
		}


		private void copyEnableLinksItem_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			string link = "";
			for (int index = 0; index < config.configs.Count; ++index)
			{
				if (!config.configs[index].enable)
					continue;
				link += config.configs[index].GetSSRLinkForServer() + "\r\n";
			}
			try
			{
				Clipboard.SetText(link);
			}
			catch { }
		}


		private void copyLinksItem_Click(object sender, EventArgs e)
		{
			Configuration config = _controller.GetCurrentConfiguration();
			string link = "";
			for (int index = 0; index < config.configs.Count; ++index)
			{
				link += config.configs[index].GetSSRLinkForServer() + "\r\n";
			}
			try
			{
				Clipboard.SetText(link);
			}
			catch { }
		}


		private void PingAllServer(object sender, EventArgs e)
		{
			var config = _controller.GetCurrentConfiguration();
			var servers = config.configs;
			foreach (var server in servers)
			{
				var host = server.server;
				_serverDiagnostic.PingAsync(host, false);
			}
		}

	}
}
