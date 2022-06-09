using Shadowsocks.SystemX.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;


namespace Shadowsocks.Controller
{
	public enum LogLevel
    {
        Debug = 0,
        Info,
        Warn,
        Error,
        Assert,
    }




    internal class Logger
    {
	    internal static readonly string[] LevelStr = new[]
        {
            "Debug",
            "Info",
            "Warn",
            "Error",
            "Assert"
        };


        private readonly TextWriter _out;


        public Logger(TextWriter @out)
        {
	        _out = @out;
        }


        public void Log(LogLevel level, params object[] s)
        {
	        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
	        var levelChar = LevelStr[(int)level][0];

            _out.Write(time);
            _out.Write("  ");
            _out.Write(levelChar);
            _out.Write("  ");

            for (var i = 0; i < s.Length - 1; i++)
            {
				_out.Write(s[i]);
                _out.Write(' ');
            }
            _out.Write(s[s.Length - 1]);

            _out.WriteLine();
        }
    }




    internal  class DateFileWriter : TextWriter
    {
        public override Encoding Encoding { get; }

        private readonly string   _dir;
        private readonly string   _name;

        private FileStream        _file         = null;
        private StreamWriter      _fileWriter   = null;
        private DateTime?         _fileDate     = null;
        private readonly object   _fileLock     = new object();


        public DateFileWriter(string dir, string name)
        {
            _dir = dir;
            _name = name;
        }
        

        private StreamWriter GetWriter()
        {
	        var today = DateTime.Today;
	        if (_fileDate == today)
                return _fileWriter;

            lock (_fileLock)
            {
                if (_fileDate == today)
                    return _fileWriter;
                
                var filename = $"{_name}_{today:yyyy-MM-dd}.log";
                _fileDate = today;
                _file = CreateFIleStream(_dir, filename);
                _fileWriter = new StreamWriter(_file);
            }

            return _fileWriter;
        }

        private FileStream CreateFIleStream(string dir, string filename)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, filename);
            return new FileStream(path, FileMode.Append);
        }


        public override void WriteLine(string value)
        {
	        try
	        {
                GetWriter().WriteLine(value);
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine(e);
		        throw;
	        }
        }


        public override void Write(string value)
        {
            try
            {
                GetWriter().Write(value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }




    public class Logging
    {
        public delegate void OnLog(string msg);

        public static string        LogFilePath;
        public static string        LogFileName;
        public static DateTime      LogFileDate;

        public static event OnLog   LogListener;

        private static FileStream   _logFileStream;
        private static StreamWriter _logStreamWriter;

        private static object _lock = new object();
        public static bool save_to_file = true;




        public static void Log(LogLevel level, object s)
        {
            RefreshOutput();

            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var lv = Logger.LevelStr[(int)level][0];
            var msg = $@"{time}  {lv}  {s}";

            Console.WriteLine(msg);
            LogListener?.Invoke(msg);
        }

        public static void Error(object o)
        {
            Log(LogLevel.Error, o);
            System.Diagnostics.Debug.WriteLine($@"[{DateTime.Now}] ERROR {o}");
        }

        public static void Warn(object o)
        {
            Log(LogLevel.Error, o);
            System.Diagnostics.Debug.WriteLine($@"[{DateTime.Now}] WARN  {o}");
        }

        public static void Info(object o)
        {
            Log(LogLevel.Info, o);
            System.Diagnostics.Debug.WriteLine($@"[{DateTime.Now}] INFO  {o}");
        }

        [Conditional("DEBUG")]
        public static void Debug(object o)
        {
            Log(LogLevel.Debug, o);
            System.Diagnostics.Debug.WriteLine($@"[{DateTime.Now}] DEBUG {o}");
        }




        protected static void RefreshOutput()
        {
            if (!save_to_file)
            {
                Console.SetOut(Console.Out);
                Console.SetError(Console.Error);
                return;
            }

	        var today = DateTime.Today;
	        if (today == LogFileDate) return;

            lock (_lock)
            {
                if (today != LogFileDate)
                    ReopenLogFile();
            }
        }


        private static void ReopenLogFile()
        {
            try
            {
                CloseLogFile();

                var dir = Path.Combine(System.Windows.Forms.Application.StartupPath, @"logs");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var today = DateTime.Today;
                var filename = $"shadowsocks_{today:yyyy-MM-dd}.log";
                var filepath = Path.Combine(dir, filename);

                var fs = new FileStream(filepath, FileMode.Append);
                var writer = new StreamWriter(fs);
                writer.AutoFlush = true;
                Console.SetOut(writer);
                Console.SetError(writer);

                LogFilePath = filepath;
                LogFileName = filename;
                LogFileDate = today;
                _logFileStream = fs;
                _logStreamWriter = writer;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private static void CloseLogFile()
        {
            _logStreamWriter?.Dispose();
            _logStreamWriter = null;

            _logFileStream?.Dispose();
            _logFileStream = null;
        }


        public static void Clear()
        {
            CloseLogFile();
            if (LogFilePath != null)
	            File.Delete(LogFilePath);
        }


        public static void LogUsefulException(Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException se)
            {
	            // already closed
                if ((uint)se.SocketErrorCode == 0x80004005)
                    return;

                switch (se.SocketErrorCode)
                {
	                case SocketError.ConnectionAborted:
		                // closed by browser when sending
		                // normally happens when download is canceled or a tab is closed before page is loaded
		                break;
	                case SocketError.ConnectionReset:
		                // received rst
		                break;
	                case SocketError.NotConnected:
		                // close when not connected
		                break;
	                case SocketError.Shutdown:
	                case SocketError.Interrupted:
		                // ignore
		                break;
                }
            }

            Error(e);
            Debug(new StackTrace().GetFramesString());
        }

        
        [Conditional("DEBUG")]
        public static void LogBin(LogLevel level, string info, byte[] data, int length)
        {
            //string s = "";
            //for (int i = 0; i < length; ++i)
            //{
            //    string fs = "0" + Convert.ToString(data[i], 16);
            //    s += " " + fs.Substring(fs.Length - 2, 2);
            //}
            //Log(level, info + s);
        }

    }

}
