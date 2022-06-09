using Shadowsocks.Framework;
using Shadowsocks.Properties;
using System.Drawing;


namespace Shadowsocks.Core.Fonts
{
	internal static class Fonts
	{
		static Fonts()
		{
			MemoryFonts.AddMemoryFont(Resources.FontAwesome6_Free_Solid_900);
		}


		public static Font FontAwesome(float fontSize = 20)
		{
			return MemoryFonts.GetFont(0, fontSize);
		}

	}
}
