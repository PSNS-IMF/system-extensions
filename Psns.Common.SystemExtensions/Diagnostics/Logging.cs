using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Psns.Common.Logging;
using static LanguageExt.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    public static partial class Prelude
    {
        // Just a bunch of functional friendly logging functions and extension methods

        public static Logging.ILogger Logger;
        public static NLog.ILogger BenchmarkLogger;

        public static T LogIf<T>(this T self,
            Func<T, bool> predicate,
            Func<T, string> format,
            string category = "General",
            TraceEventType type = TraceEventType.Information) =>
                predicate(self)
                    ? Log(self, format, category, type)
                    : self;

        public static T LogError<T>(this T self, Func<T, string> format) =>
            Log(self, format, type: TraceEventType.Error);

        public static T LogInfo<T>(this T self, Func<T, string> format) =>
            Log(self, format);

        public static T LogVerbose<T>(this T self, Func<T, string> format, string category) =>
            Log(self, format, category, TraceEventType.Verbose);

        public static T Log<T>(
            this T self,
            Func<T, string> format,
            string category = "General",
            TraceEventType type = TraceEventType.Information)
        {
            if(Logger == null)
                throw new NullReferenceException("Psns.Common.SystemExtensions.Diagnostics.Logger must be initialized");

            Logger.Write(format(self), type, category);
            return self;
        }

        public static Either<Exception, R> LogEither<R>(
            this R r,
            Func<R, string> format,
            string category = "General",
            TraceEventType type = TraceEventType.Information) =>
                r.Log(format, category, type);

        #region Benchmark

        public static T LogBench<T>(this T self, Action<T> func, string description = "") =>
            logBench(() => { func(self); return self; }, description);

        public static R LogBench<T, R>(this T self, Func<T, R> func, string description = "") =>
            logBench(() => func(self), description);

        public static Unit logBench(Action act, string description = "") =>
            logBench(() => { act(); return unit; }, description);

        public static Func<Either<L, R>> ComposeWithBench<T1, R, L>(
            this Func<T1, Either<L, R>> self,
            Func<Either<L, T1>> func,
            string description = "") =>
            () => func().Match(Right: t1 => logBench(() => self(t1), description), Left: ex => ex);

        public static T logBench<T>(
            Func<T> func,
            string description = "")
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var val = func();

            stopWatch.Stop();

            var span = stopWatch.Elapsed;

            log(description, span);

            return val;
        }

        public static async Task<T> logBenchAsync<T>(
            Func<Task<T>> func,
            string description = "")
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var val = await func();

            stopWatch.Stop();

            var span = stopWatch.Elapsed;

            log(description, span);

            return val;
        }

        static void log(string description, TimeSpan span)
        {
            if(BenchmarkLogger == null)
                throw new NullReferenceException("Psns.Common.SystemExtensions.Diagnostics.BenchmarkLogger must be initialized");

            BenchmarkLogger.Info(
                "{0},{1},{2},{3}",
                Thread.CurrentThread.ManagedThreadId.ToString(),
                description,
                string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    span.Hours,
                    span.Minutes,
                    span.Seconds,
                    span.Milliseconds / 10),
                getMemoryUsage().ToString());
        }

        public static long getMemoryUsage() => Process.GetCurrentProcess().PrivateMemorySize64 / 1000000;
        #endregion
    }
}