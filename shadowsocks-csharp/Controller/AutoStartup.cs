using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows.Forms;


namespace Shadowsocks.Controller
{
	internal class AutoStartup
    {
	    private static readonly string KEY = "ShadowsocksR_" + Application.StartupPath.GetHashCode();


        public static bool Set(bool toEnable)
        {
	        if (toEnable)
		        RegistryStartup.Del(KEY);
	        else
	        {
		        var executablePath = Util.Utils.GetExecutablePath();
		        RegistryStartup.Set(KEY,  executablePath);
	        }

	        return true;
        }


        public static bool Switch()
        {
	        var enabled = Check();
	        return Set(!enabled);
        }


        public static bool Check()
        {
	        return RegistryStartup.Contain(KEY);
        }




		/// <summary>
		/// AutoStartup by Registry
		/// </summary>
		private static class RegistryStartup
		{
			public static object Get(string name)
			{
				using ( var runKey = Registry.LocalMachine.OpenSubKey(GetRegistryRunPath(), false) )
				{
					return runKey?.GetValue(name, null);
				}
			}


			public static void Set(string name, object val)
			{
				using ( var runKey = Registry.LocalMachine.OpenSubKey(GetRegistryRunPath(), false) )
				{
					runKey?.SetValue(name, val);
				}
			}


			public static void Del(string name)
			{
				using ( var runKey = Registry.LocalMachine.OpenSubKey(GetRegistryRunPath(), false) )
				{
					runKey?.DeleteValue(name);
				}
			}


			public static bool Contain(string name)
			{
				using ( var runKey = Registry.LocalMachine.OpenSubKey(GetRegistryRunPath(), false) )
				{
					return runKey != null && runKey.GetValueNames().Any(declName => name == declName);
				}
			}


			private static string GetRegistryRunPath()
			{
				return IntPtr.Size == 4 ?
					@"Software\Microsoft\Windows\CurrentVersion\Run" :
					@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run";
			}
		}
	}

}
