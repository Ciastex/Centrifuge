﻿using Reactor.API.Logging.Decorators;
using Reactor.API.Logging.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Reactor.API.Logging
{
    public class LogManager
    {
        private static List<LogInfo> Logs { get; }

        static LogManager()
        {
            if (!Directory.Exists(Defaults.ManagerLogDirectory))
                Directory.CreateDirectory(Defaults.ManagerLogDirectory);

            Logs = new List<LogInfo>();
        }

        public static string GetCurrentAssemblyLogPath()
        {
            return GetAssemblyLogPath(
                Assembly.GetCallingAssembly()
            );
        }

        public static Log CreateBareLog()
        {
            return new Log();
        }

        public static Log GetForCurrentAssembly(bool initializeDefaults = true)
        {
            var asm = Assembly.GetCallingAssembly();

            return GetForAssembly(asm, initializeDefaults, (log) =>
            {
                log.SinkTo(new FileSink(GetAssemblyLogPath(asm)));
            });
        }

        internal static Log GetForInternalAssembly()
        {
            var asm = Assembly.GetCallingAssembly();

            return GetForAssembly(asm, true, (log) =>
            {
                var logPath = Path.Combine(Defaults.ManagerLogDirectory, $"{asm.GetName().Name}.log");
                log.SinkTo(new FileSink(logPath));
            });
        }

        private static Log GetForAssembly(Assembly assembly, bool initializeDefaults, Action<Log> postInit = null)
        {
            var logInfo = Logs.FirstOrDefault(x => x.OwningAssembly == assembly);

            if (logInfo == null)
            {
                var log = new Log();

                if (initializeDefaults)
                {
                    log.WithOutputTemplate("[{DateTime} {LogLevel}] [{ClassName}] {Message}")
                       .DecorateWith<LogLevelDecorator>("LogLevel")
                       .DecorateWith<DateTimeDecorator>("DateTime")
                       .DecorateWith<ClassNameDecorator>("ClassName")
                       .DecorateWith<MessageOutputDecorator>("Message")
                       .SinkTo<ConsoleSink>();

                    postInit?.Invoke(log);
                }

                logInfo = new LogInfo
                {
                    OwningAssembly = assembly,
                    Log = log
                };

                Logs.Add(logInfo);
            }

            return logInfo.Log;
        }

        private static string GetAssemblyLogPath(Assembly assembly)
        {
            var asmDirectory = Path.GetDirectoryName(assembly.Location);
            return Path.Combine(asmDirectory, $"{assembly.GetName().Name}.log");
        }
    }
}
