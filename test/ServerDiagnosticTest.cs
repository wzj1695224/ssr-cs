using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Controller.ServerStat;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


namespace test
{
	[TestClass]
	public class ServerDiagnosticTest
	{
		private static readonly string[] HOSTS = new []
			{
				// Google Public DNS
				"8.8.8.8",
				"8.8.4.4",
				// Cloudflare 1.1.1.1 DNS
				"208.67.222.222",
				"208.67.220.220",
				// Cisco OpenDNS
				"1.1.1.1",
				"1.0.0.1",
				// Quad9 DNS
				"9.9.9.9",
				"149.112.112.112"
			};




		[TestMethod]
		public void TestSingleThread()
		{
			const int pingTimes = (int)(HostPingHistory.MAX_HISTORY * 1.5);
			const int hostCount = 2;
			Assert.IsTrue(hostCount <= HOSTS.Length);
			Assert.IsTrue(HostPingHistory.MAX_HISTORY <= pingTimes);

			var serverResults = new Dictionary<string, List<PingResult>>();
			var diagnostic = new ServerDiagnostic();

			// ping hosts
			for (var i = 0; i < 2; i++)
			{
				var host = HOSTS[i];
				var results = new List<PingResult>(pingTimes);
				serverResults[host] = results;

				for (var t = 0; t < pingTimes ; t++)
					results.Add(diagnostic.Ping(host));
			}

			// check results
			foreach (var pair in serverResults)
			{
				var host = pair.Key;
				var results = pair.Value;
				Assert.IsTrue(diagnostic.TryGetHostPingHistory(host, out var history));

				for (var i = 0; i < HostPingHistory.MAX_HISTORY; i++)
				{
					var r1 = results[pingTimes - 1 - i];  // iterate results from end to start
					var r2 = history[i];
					Assert.AreEqual(r1, r2);
				}
			}
		}




		[TestMethod]
		public void TestMultiThread()
		{
			const int pingTimes = (int)(HostPingHistory.MAX_HISTORY * 1.2);
			const int hostCount = 8;
			Assert.IsTrue(hostCount <= HOSTS.Length);
			Assert.IsTrue(HostPingHistory.MAX_HISTORY <= pingTimes);

			var serverResults = new Dictionary<string, List<PingResult>>();
			var diagnostic = new ServerDiagnostic();

			// ping hosts
			var countdownEvent = new CountdownEvent(hostCount);
			for (var i = 0; i < hostCount; i++)
			{
				var index = i;
				ThreadPool.QueueUserWorkItem(state =>
				{
					var host = HOSTS[index];
					var results = new List<PingResult>(pingTimes);
					serverResults[host] = results;
					Console.WriteLine($@"{host}   start   by  {Thread.CurrentThread.ManagedThreadId}");

					for (var t = 0; t < pingTimes; t++)
					{
						results.Add(diagnostic.Ping(host));
						Console.WriteLine($@"{host}   {t}   by  {Thread.CurrentThread.ManagedThreadId}");
					}

					countdownEvent.Signal();
				});
			}
			if (!countdownEvent.Wait(60000))
				throw new Exception("timeout");
			

			// check results
			foreach (var pair in serverResults)
			{
				var host = pair.Key;
				var results = pair.Value;
				Assert.IsTrue(diagnostic.TryGetHostPingHistory(host, out var history));

				for (var i = 0; i < HostPingHistory.MAX_HISTORY; i++)
				{
					var r1 = results[pingTimes - 1 - i];  // iterate results from end to start
					var r2 = history[i];
					Assert.AreEqual(r1, r2);
				}
			}
		}

	}
}
