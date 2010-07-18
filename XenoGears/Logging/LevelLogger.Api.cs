using System;
using System.Diagnostics;
using XenoGears.Functional;

namespace XenoGears.Logging
{
    [DebuggerNonUserCode]
    public partial class LevelLogger
    {
        public Level Level { get; private set; }
        public Logger Logger { get; private set; }
        public LogWriter Writer { get; set; }

        public LevelLogger(Level level, Logger logger)
        {
            Level = level;
            Logger = logger;
            Writer = logger.Writer ?? LogWriter.Get("Adhoc");

#if TRACE
            IsEnabled = true;
#endif
        }

        public LevelLogger Write(Object o)
        {
            RawWrite(o);
            return this;
        }

        public LevelLogger Write(String message)
        {
            RawWrite(message);
            return this;
        }

        public LevelLogger Write(String format, params Object[] args)
        {
            RawWrite(String.Format(format, args));
            return this;
        }

        public LevelLogger WriteLine(Object o)
        {
            return Write(o).Eoln();
        }

        public LevelLogger WriteLine(String message)
        {
            return Write(message).Eoln();
        }

        public LevelLogger WriteLine(String format, params Object[] args)
        {
            return Write(format, args).Eoln();
        }

        private void RawWrite(Object o)
        {
            if (IsMuted()) return;
            Writer.Write(Logger, Level, o);
        }
    }
}