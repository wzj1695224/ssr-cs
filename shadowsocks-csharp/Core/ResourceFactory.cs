using Shadowsocks.Properties;
using System;
using System.Drawing;


namespace Shadowsocks.Core
{
	internal class ResourceFactory
	{
		public static Icon CreateIcon()
		{
			try
			{
				return Icon.FromHandle((new Bitmap("icon.png")).GetHicon());
			}
			catch
			{
				return Icon.FromHandle(Resources.ssw128.GetHicon());
			}
		}




		public static Icon CreateStaticTrayIcon()
        {
			using ( var bitmap = new Bitmap("icon.png") )
                return Icon.FromHandle( bitmap.GetHicon() );
		}


		/// <summary>
		/// Create a tray icon and apply transformation
		/// </summary>
		public static Icon CreateTrayIcon(Action<Bitmap> transformation = null)
		{
			int dpi;
			using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
				dpi = (int)graphics.DpiX;

            Bitmap raw;
            if (dpi <= 96)
                raw = Resources.ss16;
            else if (dpi <= 120)
	            raw = Resources.ss20;
            else
	            raw = Resources.ss24;

            using (var bitmap = new Bitmap(raw))
            {
				// do transformation
				transformation?.Invoke(bitmap);

				return Icon.FromHandle(bitmap.GetHicon());
			}
		}


		/// <summary>
		/// Create a tray icon and apply transformation for shift each color
		/// </summary>
		public static Icon CreateTrayIcon(Func<Color, Color> shiftColor)
		{
			return CreateTrayIcon((bitmap) =>
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					for (var y = 0; y < bitmap.Height; y++)
					{
						var color = bitmap.GetPixel(x, y);
						var newColor = shiftColor(color);
						bitmap.SetPixel(x, y, newColor);
					}
				}
			});
		}


		/// <summary>
		/// Create a tray icon by ProxyMode
		/// </summary>
		/// <param name="mode">ProxyMode</param>
		/// <param name="random">TODO don't know</param>
		public static Icon CreateTrayIcon(ProxyMode mode, bool random)
		{
			var enabled = mode != ProxyMode.NoModify && mode != ProxyMode.Direct;
			var global = mode == ProxyMode.Global;

			var needTransformation = false;
			double mulA = 1.0, mulR = 1.0, mulG = 1.0, mulB = 1.0;
			if (!enabled)
			{
				mulG = 0.4;
				needTransformation = true;
			}
			else if (!global)
			{
				mulB = 0.4;
				mulG = 0.8;
				needTransformation = true;
			}
			if (!random)
			{
				mulR = 0.4;
				needTransformation = true;
			}

			if (!needTransformation)
				return CreateTrayIcon();
			else
				return CreateTrayIcon((color) => Color.FromArgb(
					(byte)(color.A * mulA),
					(byte)(color.R * mulR),
					(byte)(color.G * mulG),
					(byte)(color.B * mulB))
				);
		}

	}
}
