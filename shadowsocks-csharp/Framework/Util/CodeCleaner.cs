using System;


namespace Shadowsocks.Framework.Util
{
	internal static class CodeCleaner
	{
		public static void IgnoreError(Action action)
		{
			try
			{
				action();
			}
			catch
			{
				// ignore
			}
		}
	}
}
