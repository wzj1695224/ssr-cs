using System.Drawing;


namespace Shadowsocks.SystemX.Drawing
{
	public class ColorX
	{
		private static byte MixColor(byte a, byte b, double alpha)
        {
            return (byte)(b * alpha + a * (1 - alpha));
        }


        public static Color MixColor(Color a, Color b, double alpha)
        {
            return Color.FromArgb(
	            MixColor(a.R, b.R, alpha),
                MixColor(a.G, b.G, alpha),
                MixColor(a.B, b.B, alpha));
        }
    }
}
