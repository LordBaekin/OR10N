using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace FlowParser
{
    public static class Log
    {
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "FlowParserLog.txt");
        private static readonly object lockObj = new object();
        private const long MaxLogFileSize = 5 * 1024 * 1024; // 5 MB

        static Log()
        {
            EnsureLogDirectoryExists();
        }

        public static void SetLogFilePath(string path)
        {
            logFilePath = path;
            EnsureLogDirectoryExists();
        }

        public static void Info(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("INFO", string.Format(message, args), caller);
        }

        public static void Warning(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("WARNING", string.Format(message, args), caller);
        }

        public static void Error(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("ERROR", string.Format(message, args), caller);
        }

        private static void WriteLog(string level, string message, string caller)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{caller}] {message}";
            Console.WriteLine(logMessage);

            lock (lockObj)
            {
                try
                {
                    RotateLogFileIfNeeded();
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write log: {ex.Message}");
                }
            }
        }

        private static void RotateLogFileIfNeeded()
        {
            try
            {
                if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > MaxLogFileSize)
                {
                    string archiveFilePath = Path.Combine(
                        Path.GetDirectoryName(logFilePath),
                        $"FlowParserLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.Move(logFilePath, archiveFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to rotate log file: {ex.Message}");
            }
        }

        private static void EnsureLogDirectoryExists()
        {
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Clear()
        {
            lock (lockObj)
            {
                try
                {
                    if (File.Exists(logFilePath))
                    {
                        File.WriteAllText(logFilePath, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to clear log: {ex.Message}");
                }
            }
        }
    }
}
