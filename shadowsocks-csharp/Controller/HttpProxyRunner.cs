using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;


namespace Shadowsocks.Controller
{
	internal class HttpProxyRunner
    {
        public int RunningPort { get; private set; }
       
        private const string SubPath = @"temp";

        private Process _process;
        private static readonly string RUNNING_PATH;
        private static readonly string EXE_NAME_NO_EXT = @"/ssr_privoxy";
        private static readonly string EXE_NAME = @"/ssr_privoxy.exe";


        static HttpProxyRunner()
        {
            RUNNING_PATH = Path.Combine(System.Windows.Forms.Application.StartupPath, SubPath);
            if (!Directory.Exists(RUNNING_PATH))
                Directory.CreateDirectory(RUNNING_PATH);

            EXE_NAME_NO_EXT = Path.GetFileNameWithoutExtension(Util.Utils.GetExecutablePath());
            EXE_NAME = @"/" + EXE_NAME_NO_EXT + @".exe";

            Kill();
            try
            {
                FileManager.UncompressFile(RUNNING_PATH + EXE_NAME, Resources.privoxy_exe);
                FileManager.UncompressFile(RUNNING_PATH + "/mgwz.dll", Resources.mgwz_dll);
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }


        public bool HasExited()
        {
            if (_process == null)
                return true;
            try
            {
                return _process.HasExited;
            }
            catch
            {
                return false;
            }
        }




        public static void Kill()
        {
            var existingPolipo = Process.GetProcessesByName(EXE_NAME_NO_EXT);
            foreach (var process in existingPolipo)
            {
	            var str = process.MainModule?.FileName;
                if (str != Path.GetFullPath(RUNNING_PATH + EXE_NAME))
                    continue;
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }




        public void Start(Configuration configuration)
        {
            if (_process == null)
            {
                Kill();
                RunningPort = GetFreePort();

                var polipoConfig = Resources.privoxy_conf;
                polipoConfig = polipoConfig.Replace("__SOCKS_PORT__", configuration.localPort.ToString());
                polipoConfig = polipoConfig.Replace("__PRIVOXY_BIND_PORT__", RunningPort.ToString());
                polipoConfig = polipoConfig.Replace("__PRIVOXY_BIND_IP__", "127.0.0.1");
                polipoConfig = polipoConfig.Replace("__BYPASS_ACTION__", "");

                FileManager.ByteArrayToFile(RUNNING_PATH + "/privoxy.conf", System.Text.Encoding.UTF8.GetBytes(polipoConfig));

                Restart();
            }
        }


        public void Restart()
        {
            var process = new Process();
            process.StartInfo.FileName = RUNNING_PATH + EXE_NAME;
            process.StartInfo.Arguments = " \"" + RUNNING_PATH + "/privoxy.conf\"";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;

            try
            {
                process.Start();
                _process = process;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public void Stop()
        {
	        if (_process == null) return;
	        try
	        {
		        _process.Kill();
		        _process.WaitForExit();
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine(e.ToString());
	        }
	        finally
	        {
		        _process = null;
	        }
        }


        private static int GetFreePort()
        {
            const int defaultPort = 60000;
            try
            {
	            var usedPorts = IPGlobalProperties
	                .GetIPGlobalProperties()
	                .GetActiveTcpListeners()
	                .Select(endPoint => endPoint.Port)
	                .ToList();

	            var random = new Random(Util.Utils.GetExecutablePath().GetHashCode() ^ (int)DateTime.Now.Ticks);
                for (var @try = 0; @try < 1000; @try++)
                {
                    var port = random.Next(10000, 65536);
                    if (!usedPorts.Contains(port))
	                    return port;
                }
            }
            catch (Exception e)
            {
                // in case access denied
                Logging.LogUsefulException(e);
                return defaultPort;
            }

            throw new Exception("No free port found.");
        }
    }
}
