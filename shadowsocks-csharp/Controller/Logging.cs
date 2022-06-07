using Shadowsocks.Obfs;
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
	    private readonly string[] LevelStr = new[]
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

        private readonly string _dir;
        private readonly string _name;

        private FileStream   _file         = null;
        private StreamWriter _fileWritter  = null;
        private DateTime?    _fileDate     = null;
        private object       _fileLock     = new object();


        public DateFileWriter(string dir, string name)
        {
            _name = name;
        }
        

        private StreamWriter GetWriter()
        {
	        var today = DateTime.Today;
	        if (_fileDate == today)
                return _fileWritter;

            lock (_fileLock)
            {
                if (_fileDate == today)
                    return _fileWritter;
                
                var filename = $"{_name}_{today:yyyy-MM-dd}.log";
                _fileDate = today;
                _file = CreateFIleStream(_dir, filename);
                _fileWritter = new StreamWriter(_file);
            }

            return _fileWritter;
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
        public static string LogFile;
        public static string LogFilePath;
        public static string LogFileName;
        protected static string date;

        private static FileStream _logFileStream;
        private static StreamWriterWithTimestamp _logStreamWriter;
        private static object _lock = new object();
        public static bool save_to_file = true;


        public static bool OpenLogFile()
        {
            try
            {
                CloseLogFile();

                if (save_to_file)
                {
                    var dir = Path.Combine(System.Windows.Forms.Application.StartupPath, @"temp");// Path.GetFullPath(".");//Path.GetTempPath();
                    if (!Directory.Exists(dir))
	                    Directory.CreateDirectory(dir);
                    
                    var new_date = DateTime.Now.ToString("yyyy-MM");
                    LogFileName = "shadowsocks_" + new_date + ".log";
                    LogFile = Path.Combine(dir, LogFileName);
                    _logFileStream = new FileStream(LogFile, FileMode.Append);
                    _logStreamWriter = new StreamWriterWithTimestamp(_logFileStream);
                    _logStreamWriter.AutoFlush = true;
                    Console.SetOut(_logStreamWriter);
                    Console.SetError(_logStreamWriter);

                    LogFilePath = dir;
                    date = new_date;
                }
                else
                {
                    Console.SetOut(Console.Out);
                    Console.SetError(Console.Error);
                }

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }


        private static void CloseLogFile()
        {
            _logStreamWriter?.Dispose();
            _logFileStream?.Dispose();

            _logStreamWriter = null;
            _logFileStream = null;
        }


        public static void Clear()
        {
            CloseLogFile();
            if (LogFile != null)
            {
                File.Delete(LogFile);
            }
            OpenLogFile();
        }


        public static void Log(LogLevel level, object s)
        {
            UpdateLogFile();
            var strMap = new[]{
                "Debug",
                "Info",
                "Warn",
                "Error",
                "Assert",
            };
            Console.WriteLine($@"[{strMap[(int)level]}] {s}");
        }

        public static void Error(object o)
        {
            Log(LogLevel.Error, o);
            System.Diagnostics.Debug.WriteLine($@"[{DateTime.Now}] ERROR {o}");
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

        private static string ToString(StackFrame[] stacks)
        {
            string result = string.Empty;
            foreach (StackFrame stack in stacks)
            {
                result += string.Format("{0}\r\n", stack.GetMethod().ToString());
            }
            return result;
        }

        protected static void UpdateLogFile()
        {
            if (DateTime.Now.ToString("yyyy-MM") != date)
            {
                lock (_lock)
                {
                    if (DateTime.Now.ToString("yyyy-MM") != date)
                    {
                        OpenLogFile();
                    }
                }
            }
        }

        public static void LogUsefulException(Exception e)
        {
            UpdateLogFile();
            // just log useful exceptions, not all of them
            if (e is SocketException)
            {
                SocketException se = (SocketException)e;
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                }
                else if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // received rst
                }
                else if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    // close when not connected
                }
                else if ((uint)se.SocketErrorCode == 0x80004005)
                {
                    // already closed
                }
                else if (se.SocketErrorCode == SocketError.Shutdown)
                {
                    // ignore
                }
                else if (se.SocketErrorCode == SocketError.Interrupted)
                {
                    // ignore
                }
                else
                {
                    Error(e);

                    Debug(ToString(new StackTrace().GetFrames()));
                }
            }
            else
            {
                Error(e);

                Debug(ToString(new StackTrace().GetFrames()));
            }
        }


        public static bool LogSocketException(string remarks, string server, Exception e)
        {
            UpdateLogFile();
            // just log useful exceptions, not all of them
            if (e is ObfsException)
            {
                ObfsException oe = (ObfsException)e;
                Error("Proxy server [" + remarks + "(" + server + ")] "
                    + oe.Message);
                return true;
            }
            else if (e is NullReferenceException)
            {
                return true;
            }
            else if (e is ObjectDisposedException)
            {
                // ignore
                return true;
            }
            else if (e is SocketException)
            {
                SocketException se = (SocketException)e;
                if ((uint)se.SocketErrorCode == 0x80004005)
                {
                    // already closed
                    return true;
                }
                else if (se.ErrorCode == 11004)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "DNS lookup failed");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.HostNotFound)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "Host not found");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "connection refused");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.NetworkUnreachable)
                {
                    Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                        + "network unreachable");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    //Logging.Log(LogLevel.Warn, "Proxy server [" + remarks + "(" + server + ")] "
                    //    + "connection timeout");
                    return true;
                }
                else if (se.SocketErrorCode == SocketError.Shutdown)
                {
                    return true;
                }
                else
                {
                    Logging.Log(LogLevel.Info, "Proxy server [" + remarks + "(" + server + ")] "
                        + Convert.ToString(se.SocketErrorCode) + ":" + se.Message);

                    Debug(ToString(new StackTrace().GetFrames()));

                    return true;
                }
            }
            return false;
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

    // Simply extended System.IO.StreamWriter for adding timestamp workaround
    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream) : base(stream)
        {
        }

        private string GetTimestamp()
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(GetTimestamp() + value);
        }

        public override void Write(string value)
        {
            base.Write(GetTimestamp() + value);
        }
    }

}
