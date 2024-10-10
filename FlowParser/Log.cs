using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace FlowParser
{
    public static class Log
    {
        // Default log file path, can be modified as needed
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "FlowParserLog.txt");
        private static readonly object lockObj = new object();
        private const long MaxLogFileSize = 5 * 1024 * 1024; // 5 MB

        static Log()
        {
            EnsureLogDirectoryExists();
        }

        // Allows setting a custom log file path if needed
        public static void SetLogFilePath(string path)
        {
            logFilePath = path;
            EnsureLogDirectoryExists();
        }

        public static void Info(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("INFO", message, caller, args);
        }

        public static void Warning(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("WARNING", message, caller, args);
        }

        public static void Error(string message, [CallerMemberName] string caller = "", params object[] args)
        {
            WriteLog("ERROR", message, caller, args);
        }

        private static void WriteLog(string level, string message, string caller, params object[] args)
        {
            string formattedMessage = args != null && args.Length > 0
                ? string.Format(message, args)
                : message;

            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{caller}] {formattedMessage}";
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
