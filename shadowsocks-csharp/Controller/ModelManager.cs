using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shadowsocks.Model;


namespace Shadowsocks.Controller
{
	internal sealed class ModelManager
	{
		public ConfigurationModel Configuration { get; private set; }


		public static ModelManager Instance { get; private set; }
		private static readonly object INIT_LOCK = new object();


		public static void Init(Configuration configuration)
		{
			lock (INIT_LOCK)
			{
				if (Instance != null)
					throw new Exception("ModelManager already init");

				Instance = new ModelManager
				{
					Configuration = new ConfigurationModel(configuration)
				};
			}
		}

		private ModelManager() {}

	}
}
