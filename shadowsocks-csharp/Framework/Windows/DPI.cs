using System;
using System.Drawing;


namespace Shadowsocks.Framework.Windows
{
	public static class DPI
	{
		public static float DpiX => GraphicsHelper.GetDpiX();
		
		public static int DpiMul => ((int)DpiX * 4 + 48) / 96;
	}




	internal static class GraphicsHelper
	{
		public static float GetDpiX()
		{
			using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
				return graphics.DpiX;
		}
	}
}
