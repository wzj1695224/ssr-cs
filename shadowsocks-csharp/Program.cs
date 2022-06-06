using Shadowsocks.Controller;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Shadowsocks.Controller.ServerStat;
using Shadowsocks.Model;
#if !_CONSOLE
using Shadowsocks.Core;
using Shadowsocks.View;
#endif


namespace Shadowsocks
{
    static class Program
    {
        static ShadowsocksController _controller;
        static ServerDiagnostic _serverDiagnostic;
#if !_CONSOLE
        static MenuViewController _menuController;
#endif

        [STAThread]
        static void Main(string[] args)
        {
#if !_CONSOLE
            foreach (string arg in args)
            {
                if (arg == "--setautorun")
                {
                    if (!AutoStartup.Switch())
	                    Environment.ExitCode = 1;
                    return;
                }
            }

            using (Mutex mutex = new Mutex(false, "Global\\ShadowsocksR_" + Application.StartupPath.GetHashCode()))
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Application.ApplicationExit += Application_ApplicationExit;
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show(I18N.GetString("Find Shadowsocks icon in your notify tray.") + "\n" +
                        I18N.GetString("If you want to start multiple Shadowsocks, make a copy in another directory."),
                        I18N.GetString("ShadowsocksR is already running."));
                    return;
                }
#endif
                Directory.SetCurrentDirectory(Application.StartupPath);

#if !_CONSOLE
                int try_times = 0;
                while (Configuration.Load() == null)
                {
                    if (try_times >= 5)
                        return;
                    using (InputPassword dlg = new InputPassword())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                            Configuration.SetPassword(dlg.password);
                        else
                            return;
                    }
                    try_times += 1;
                }
                if (try_times > 0)
                    Logging.save_to_file = false;
#endif

                _controller = new ShadowsocksController();
                HostMap.Instance().LoadHostFile();

                // Logging
                Configuration cfg = _controller.GetConfiguration();
                Logging.save_to_file = cfg.logEnable;

                //#if !DEBUG
                Logging.OpenLogFile();
                //#endif

#if _DOTNET_4_0
                // Enable Modern TLS when .NET 4.5+ installed.
                if (Util.EnvCheck.CheckDotNet45())
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
#endif
#if !_CONSOLE
                _serverDiagnostic = new ServerDiagnostic();

                // MenuController
                _menuController = new MenuViewController(_controller, _serverDiagnostic);

                // INotifyIconController
                INotifyIconController notifyIconController = new NotifyIconController(ServiceManager.EventBus, _controller, _menuController);
                _menuController.NotifyIconController = notifyIconController;
#endif

                _controller.Start();

#if !_CONSOLE
                //Util.Utils.ReleaseMemory();

                Application.Run();
            }
#else
            Console.ReadLine();
            _controller.Stop();
#endif
        }


        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    StartControllerDelayed(5 * 1000);
                    break;
                case PowerModes.Suspend:
                    _controller?.Stop();
                    break;
            }
        }

		private static void StartControllerDelayed(double interval)
		{
			if (_controller == null) return;

			var timer = new System.Timers.Timer(interval);
			timer.Elapsed += (sender, e) =>
			{
				_controller?.Start();
				timer.Enabled = false;
				timer.Stop();
				timer.Dispose();
			};
			timer.AutoReset = false;
			timer.Enabled = true;
			timer.Start();
		}


		private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            _controller?.Stop();
            _controller = null;
        }


		private static int _exited = 0;
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (Interlocked.Increment(ref _exited) != 1) return;

			var es = e.ExceptionObject != null ? e.ExceptionObject.ToString() : "";

			Logging.Log(LogLevel.Error, es);

			MessageBox.Show(I18N.GetString("Unexpected error, ShadowsocksR will exit.") + Environment.NewLine + es,
				"Shadowsocks Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			Application.Exit();
		}
	}
}
