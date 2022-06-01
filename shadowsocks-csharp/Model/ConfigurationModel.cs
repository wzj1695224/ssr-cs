using System;
using Shadowsocks.Core;
using Shadowsocks.Framework.Binding;


namespace Shadowsocks.Model
{
	public class ConfigurationModel
	{
		public event EventHandler OnConfigChange;


		public readonly LiveData<ProxyMode> ProxyMode;


		public Configuration Conf;


		public ConfigurationModel(Configuration configuration)
		{
			this.Conf = configuration;

			ProxyMode = new LiveData<ProxyMode>( (ProxyMode) Conf.sysProxyMode );
			ProxyMode.OnChange += (mode) => Conf.sysProxyMode = (int)mode;
		}
	}
}
