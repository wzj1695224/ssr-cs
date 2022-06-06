using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.NetworkInformation;
using static Shadowsocks.Framework.Net.Diagnostic;


namespace test.Framework.Diagnostic
{
	[TestClass]
	public class PingTest
	{
		private const string Host = "www.google.com";




		[TestMethod]
		public void PingCommand()
		{
			Console.WriteLine($@"Pinging {Host} [XXX.XXX.XXX.XXX] with 32 bytes of data:");

			Ping(Host, (@h, id, reply) =>
			{
				if (reply == null || reply.Status == IPStatus.TimedOut)
					Console.WriteLine(@"Request timed out.");
				else
					Console.WriteLine($@"Reply from {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options.Ttl}");
				return id != 4;
			});
		}




		[TestMethod]
		public void TestPingAvg()
		{
			var pingAvg = PingAvg(Host, 4);
			Console.WriteLine(pingAvg);
		}
	}
}
