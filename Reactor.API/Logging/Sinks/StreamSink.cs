﻿using Reactor.API.Logging.Base;
using System;
using System.IO;
using System.Reflection;

namespace Reactor.API.Logging.Sinks
{
    public class StreamSink : Sink, IDisposable
    {
        private StreamWriter StreamWriter { get; }

        public StreamSink(Stream stream)
        {
            StreamWriter = new StreamWriter(stream) { AutoFlush = true };
        }

        public override void Write(string message, params object[] args)
        {
            StreamWriter.WriteLine(message);

            if (args.Length == 1)
            {
                if (args[0] is ReflectionTypeLoadException rtle)
                {
                    StreamWriter.WriteLine(
                        Formatting.ReflectionTypeLoadExceptionForLogging(rtle)
                    );
                }
                else if (args[0] is Exception e)
                {
                    StreamWriter.WriteLine(
                        Formatting.ExceptionForLogging(e, true)
                    );
                }
            }
        }

        public override void Write(LogLevel logLevel, string message, params object[] args)
            => Write(message, args);

        public void Dispose()
        {
            StreamWriter.Dispose();
        }
    }
}
