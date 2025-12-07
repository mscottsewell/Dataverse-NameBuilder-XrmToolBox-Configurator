using System;
using System.IO;

namespace NameBuilderConfigurator
{
    /// <summary>
    /// Provides diagnostic logging to a local file for troubleshooting issues.
    /// Logs are stored in %APPDATA%\NameBuilderConfigurator\diagnostics.log
    /// </summary>
    internal static class DiagnosticLog
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NameBuilderConfigurator");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "diagnostics.log");

        /// <summary>Logs an error with operation context for troubleshooting.</summary>
        /// <param name="operation">The operation being performed when the error occurred.</param>
        /// <param name="exception">The exception that occurred.</param>
        public static void LogError(string operation, Exception exception)
        {
            LogMessage("ERROR", operation, exception);
        }

        /// <summary>Logs a warning message.</summary>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="message">The warning message.</param>
        public static void LogWarning(string operation, string message)
        {
            LogMessage("WARNING", operation, message);
        }

        /// <summary>Logs an informational message.</summary>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="message">The information message.</param>
        public static void LogInfo(string operation, string message)
        {
            LogMessage("INFO", operation, message);
        }

        private static void LogMessage(string level, string operation, object content)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Format the message
                var timestamp = DateTime.UtcNow.ToString("o");
                var message = content is Exception ex
                    ? $"{timestamp} [{level}] [{operation}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n"
                    : $"{timestamp} [{level}] [{operation}] {content}\n";

                // Append to log file (with basic file size management)
                lock (LogFilePath)
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    if (fileInfo.Exists && fileInfo.Length > 5_000_000) // 5MB limit
                    {
                        // Archive old log
                        var archivePath = Path.Combine(LogDirectory, 
                            $"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                        File.Move(LogFilePath, archivePath);
                    }

                    File.AppendAllText(LogFilePath, message);
                }
            }
            catch
            {
                // Prevent logging errors from crashing the application
                // Silently ignore logging failures
            }
        }
    }
}
