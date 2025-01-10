using System.Runtime.CompilerServices;

using RealynxServices.Config;
using RealynxServices.Interfaces;

namespace RealynxServices {
    [Flags]
    public enum LogLevel {
        Debugging = 1,
        Errors = 2,
        Warnings = 4,
        Info = 8
    }

    public class Logger : ILogger {
        public LogLevel Level { get; set; }

        private readonly LoggerConfig _loggerConfig;
        private readonly StreamWriter? _logWriter;
        private readonly Lock _logLock = new();

        private string TimeStamp {
            get {
                return DateTime.Now.ToLongTimeString();
            }
        }


        public Logger(LoggerConfig loggerConfig) {
            _loggerConfig = loggerConfig;
            var logLocation = loggerConfig.LogFile;

            Level = LogLevel.Info | LogLevel.Warnings | LogLevel.Errors;

            if (loggerConfig.WriteFile) {
                _logWriter = new StreamWriter(logLocation, false);
            }

            if (loggerConfig.DebugLogs) {
                Level |= LogLevel.Debugging;
                Debug("Enabled debug logs...");
            }
        }

        public void Info(string info,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Info)) {
                LogOutput(ConsoleColor.Green, info, classFile, lineNumber, callerName);
            }
        }

        public void Info(DefaultInterpolatedStringHandler info,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Info)) {
                var infoString = info.ToStringAndClear();
                LogOutput(ConsoleColor.Green, infoString, classFile, lineNumber, callerName);
            }
        }

        public void Error(string error,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Errors)) {
                LogOutput(ConsoleColor.Red, error, classFile, lineNumber, callerName);
            }
        }

        public void Error(DefaultInterpolatedStringHandler error,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Errors)) {
                var errorString = error.ToStringAndClear();
                LogOutput(ConsoleColor.Red, errorString, classFile, lineNumber, callerName);
            }
        }

        public void Warning(string warning,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Warnings)) {
                LogOutput(ConsoleColor.Yellow, warning, classFile, lineNumber, callerName);
            }
        }

        public void Warning(DefaultInterpolatedStringHandler warning,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Warnings)) {
                var warningString = warning.ToStringAndClear();
                LogOutput(ConsoleColor.Yellow, warningString, classFile, lineNumber, callerName);
            }
        }

        public void Debug(string debug,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Debugging)) {
                LogOutput(ConsoleColor.Magenta, debug, classFile, lineNumber, callerName);
            }
        }

        public void Debug(DefaultInterpolatedStringHandler debug,
            [CallerFilePath] string classFile = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string callerName = "") {
            if (Level.HasFlag(LogLevel.Debugging)) {
                var debugString = debug.ToStringAndClear();
                LogOutput(ConsoleColor.Magenta, debugString, classFile, lineNumber, callerName);
            }
        }

        private void LogOutput(ConsoleColor color, string log, string classFile, int lineNumber, string callerName) {
            lock (_logLock) {
                // If the binary was compiled on windows the constant class filenames will have windows path separators, and vice versa for linux.
                // So we must check for this manually.
                var directorySeparatorChar = classFile.Contains('/') ? '/' : '\\';

                var className = classFile;
                if (className.Contains(directorySeparatorChar)) {
                    className = className.Split(directorySeparatorChar)[^1];
                }

                className = Path.GetFileNameWithoutExtension(className);

                var timeStamp = $"[{TimeStamp}]";

                var logPreamble = $"{timeStamp}[{className}::{callerName};{lineNumber}]: ";

                Console.ForegroundColor = color;
                Console.Write(logPreamble);

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(log);

                if (_loggerConfig.WriteFile && _logWriter is not null) {
                    _logWriter.Write(timeStamp);
                    _logWriter.Write(' ');
                    _logWriter.WriteLine(log);
                    _logWriter.Flush();
                }
            }
        }
    }
}