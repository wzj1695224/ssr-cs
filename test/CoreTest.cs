using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller;
using Shadowsocks.Core;


namespace test
{
	internal class Holder<T>
	{
		public T Obj;
	}


    [TestClass]
	public class CoreTest
	{
		[TestMethod]
		public void TestCompareVersion()
		{
			var holder = new Holder<ProxyMode>
			{
				Obj = ProxyMode.NoModify
			};

			var store = new ProxyModeStore();
			store.OnProxyModeChange += (mode) => holder.Obj = mode;

			Assert.AreEqual(holder.Obj, ProxyMode.NoModify);

			store.ProxyMode = ProxyMode.Direct;
			Assert.AreEqual(holder.Obj, ProxyMode.Direct);

			store.ProxyMode = ProxyMode.Pac;
			store.ProxyMode = ProxyMode.Global;
			Assert.AreEqual(holder.Obj, ProxyMode.Global);
		}


	}
}
