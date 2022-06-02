using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shadowsocks.Core.Model.Server;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;


namespace test
{
	[TestClass]
    public class ServerTest
    {
        [TestMethod]
        public void SSR_1()
        {
            const string normalCase = "ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUQ";

            var server = ServerFactory.Create(normalCase, "");

            Assert.AreEqual(server.server, "127.0.0.1");
            Assert.AreEqual<ushort>(server.server_port, 1234);
            Assert.AreEqual(server.protocol, "auth_aes128_md5");
            Assert.AreEqual(server.method, "aes-128-cfb");
            Assert.AreEqual(server.obfs, "tls1.2_ticket_auth");
            Assert.AreEqual(server.obfsparam, "breakwa11.moe");
            Assert.AreEqual(server.password, "aaabbb");
        }


        [TestMethod]
        public void SSR_2()
        {
            const string normalCaseWithRemark = "ssr://MTI3LjAuMC4xOjEyMzQ6YXV0aF9hZXMxMjhfbWQ1OmFlcy0xMjgtY2ZiOnRsczEuMl90aWNrZXRfYXV0aDpZV0ZoWW1KaS8_b2Jmc3BhcmFtPVluSmxZV3QzWVRFeExtMXZaUSZyZW1hcmtzPTVyV0w2Sy1WNUxpdDVwYUg";

            var server = ServerFactory.Create(normalCaseWithRemark, "firewallAirport");

            Assert.AreEqual(server.server, "127.0.0.1");
            Assert.AreEqual<ushort>(server.server_port, 1234);
            Assert.AreEqual(server.protocol, "auth_aes128_md5");
            Assert.AreEqual(server.method, "aes-128-cfb");
            Assert.AreEqual(server.obfs, "tls1.2_ticket_auth");
            Assert.AreEqual(server.obfsparam, "breakwa11.moe");
            Assert.AreEqual(server.password, "aaabbb");

            Assert.AreEqual(server.remarks, "测试中文");
            Assert.AreEqual(server.group, "firewallAirport");
        }


        [TestMethod]
        public void TestHideServerName()
        {
            var addrs = new Dictionary<string, string>
            {
	            { "127.0.0.1", "127.**.1" },
	            { "2001:db8:85a3:8d3:1319:8a2e:370:7348", "2001:**:7348" },
	            { "::1319:8a2e:370:7348", "**:7348" },
	            { "::1", "**:1" }
            };

            foreach (var key in addrs.Keys)
            {
                var val = ServerName.HideServerAddr(key);
                Assert.AreEqual(addrs[key], val);
            }
        }


        [TestMethod]
        public void TestBadPortNumber()
        {
	        const string link = "ssr://MTI3LjAuMC4xOjgwOmF1dGhfc2hhMV92NDpjaGFjaGEyMDpodHRwX3NpbXBsZTplaWZnYmVpd3ViZ3IvP29iZnNwYXJhbT0mcHJvdG9wYXJhbT0mcmVtYXJrcz0mZ3JvdXA9JnVkcHBvcnQ9NDY0MzgxMzYmdW90PTQ2MDA3MTI4";

	        try
	        {
                ServerFactory.Create(link, "firewallAirport");
            }
            catch (OverflowException e)
            {
                Console.Write(e.ToString());
            }
        }
    }
}
