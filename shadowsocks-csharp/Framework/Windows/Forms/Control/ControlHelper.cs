using System;
using System.Reflection;
using System.Windows.Forms;


namespace Shadowsocks.Framework.Windows.Forms
{
	internal class ControlHelper
	{
		private static readonly MethodInfo Control_SetStyle      = typeof(Control).GetMethod("SetStyle",      BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo Control_GetStyle      = typeof(Control).GetMethod("GetStyle",      BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo Control_UpdateStyles  = typeof(Control).GetMethod("UpdateStyles",  BindingFlags.NonPublic | BindingFlags.Instance);


		public static void SetStyle(Control control, ControlStyles flag, bool value, bool ignoreError = false)
		{
			Control_SetStyle.Invoke(control, new object[] { flag, value });
		}


		public static bool GetStyle(Control control, ControlStyles flag)
		{
			return (bool)Control_GetStyle.Invoke(control, new object[] { flag });
		}


		public static void UpdateStyles(Control control, bool ignoreError = false)
		{
			Control_UpdateStyles.Invoke(control, Array.Empty<object>());
		}
	}




	internal static class ControlExtension
	{
		public static @ControlHide @Hide(this Control control)
		{
			return new @ControlHide(control);
		}


		public class @ControlHide
		{
			private readonly Control _object;

			public ControlHide(Control o)
			{
				_object = o;
			}


			public void SetStyle(ControlStyles flag, bool value)
			{
				ControlHelper.SetStyle(_object, flag, value);
			}

			public bool GetStyle(ControlStyles flag)
			{
				return ControlHelper.GetStyle(_object, flag);
			}

			public void UpdateStyles()
			{
				ControlHelper.UpdateStyles(_object);
			}
		}
	}
}
