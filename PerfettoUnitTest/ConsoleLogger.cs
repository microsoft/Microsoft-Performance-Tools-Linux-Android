using Microsoft.Performance.SDK;
using Microsoft.Performance.SDK.Processing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfettoUnitTest
{
    //Microsoft.Performance.SDK.Runtime.Logger and ConsoleLogger is not accessible

    class ConsoleLogger : ILogger
    {
        private readonly Type type;

        public ConsoleLogger()
        {
        }

        public ConsoleLogger(Type type)
        {
            Guard.NotNull(type, nameof(type));

            this.type = type;
        }

        public void Error(string fmt, params object[] args)
        {
            this.Error(null, fmt, args);
        }

        public void Error(Exception e, string fmt, params object[] args)
        {
            this.Write(this.type, "Error", e, fmt, args);
        }

        public void Fatal(string fmt, params object[] args)
        {
            this.Fatal(null, fmt, args);
        }

        public void Fatal(Exception e, string fmt, params object[] args)
        {
            this.Write(this.type, "Fatal", e, fmt, args);
            throw e;
        }

        public void Info(string fmt, params object[] args)
        {
            this.Info(null, fmt, args);
        }

        public void Info(Exception e, string fmt, params object[] args)
        {
            this.Write(this.type, "Info", e, fmt, args);
        }

        public void Verbose(string fmt, params object[] args)
        {
            this.Verbose(null, fmt, args);
        }

        public void Verbose(Exception e, string fmt, params object[] args)
        {
            this.Write(this.type, "Verbose", e, fmt, args);
        }

        public void Warn(string fmt, params object[] args)
        {
            this.Warn(null, fmt, args);
        }

        public void Warn(Exception e, string fmt, params object[] args)
        {
            this.Write(this.type, "Warn", e, fmt, args);
        }

        protected void Write(
            Type type,
            string level,
            Exception e,
            string fmt,
            params object[] args)
        {
            var message = new StringBuilder();
            message.Append("[").Append(type.FullName).Append("]: ")
                .Append(level).Append(" - ")
                .AppendFormat(fmt, args);
            if (e != null)
            {
                // todo: inner exceptions
                message.AppendLine()
                    .Append("Exception detail: ").Append(e.GetType()).AppendLine()
                    .Append("    Message: ").Append(e.Message)
                    .Append("    Stack: ").Append(e.StackTrace);
            }

            Console.WriteLine(message);
        }
    }
}
