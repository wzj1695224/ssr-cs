using System;
using System.Windows.Forms;


namespace Shadowsocks.Framework.Windows.Forms
{
	public static class DataGridViewHelper
	{
		public static void UseDoubleBuffer(DataGridView view)
		{
			if (ControlHelper.GetStyle(view, ControlStyles.DoubleBuffer))
				throw new Exception($"{view} already set DoubleBuffer flag");

			const ControlStyles flags = ControlStyles.DoubleBuffer
			                            | ControlStyles.OptimizedDoubleBuffer
			                            | ControlStyles.UserPaint
			                            | ControlStyles.AllPaintingInWmPaint;
			ControlHelper.SetStyle(view, flags, true);
			ControlHelper.UpdateStyles(view);

			if (!ControlHelper.GetStyle(view, ControlStyles.DoubleBuffer))
				throw new Exception($"{view} set DoubleBuffer flag failed");
		}

	}
}
