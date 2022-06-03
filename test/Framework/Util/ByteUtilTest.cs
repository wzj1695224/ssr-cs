using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Shadowsocks.Framework.Util.ByteUtil;


namespace test.Framework.Util
{
	[TestClass]
	public class ByteUtilTest
	{
		[TestMethod]
		public void TestParseByteStr()
		{
			Assert.AreEqual(10,                  ParseByteStr("10"));
			Assert.AreEqual(10000000,            ParseByteStr("10000000"));
			Assert.AreEqual(1024,                ParseByteStr("1K"));
			Assert.AreEqual(1024000,             ParseByteStr("1000K"));
			Assert.AreEqual(1024000000,          ParseByteStr("1000000K"));
			Assert.AreEqual(1048576,             ParseByteStr("1M"));
			Assert.AreEqual(1048576000,          ParseByteStr("1000M"));
			Assert.AreEqual(1048576000000,       ParseByteStr("1000000M"));
			Assert.AreEqual(1073741824,          ParseByteStr("1G"));
			Assert.AreEqual(1073741824000,       ParseByteStr("1000G"));
			Assert.AreEqual(1073741824000000,    ParseByteStr("1000000G"));
			Assert.AreEqual(1099511627776,       ParseByteStr("1T"));
			Assert.AreEqual(1099511627776000,    ParseByteStr("1000T"));
			Assert.AreEqual(1099511627776000000, ParseByteStr("1000000T"));
			Assert.AreEqual(-1,                  ParseByteStr("-"));
			Assert.AreEqual(-1,                  ParseByteStr("AAA"));
		}
	}
}
