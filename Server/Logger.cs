using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MMO.Server
{
    public enum LogLevel
    {
        Debug = 0,
        Error = 1,
        System = 2,
    }

    public static class Logger
    {
        private static string _logFolder = "Log";
        private static LogLevel _logLevel = LogLevel.System;

        private static readonly object _lock = new object();

        public static void Init(string logDirName)
        {
            _logFolder = logDirName;
            Directory.CreateDirectory(_logFolder);
        }

        public static void SetLevel(LogLevel level)
        {
            _logLevel = level;
        }

        public static void Log(string logType, LogLevel logLevel, string message)
        {
            if (logLevel < _logLevel)
            {
                return;
            }

            DateTime now = DateTime.Now;

            string tag;
            if (logLevel == LogLevel.Debug)
            {
                tag = "DEBUG";
            }
            else if (logLevel == LogLevel.Error)
            {
                tag = "ERROR";
            }
            else
            {
                tag = "SYSTEM";
            }

            string path = Path.Combine(_logFolder, $"{logType}_{now.Year}_{now.Month}.txt");
            string line = $"[{tag}] [{now:yyyy.MM.dd HH:mm:ss}] {message} {Environment.NewLine}";


            lock(_lock)
            {
                try
                {
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"failed to log : {e.Message}");
                }
            }
        }

    }

 
}
