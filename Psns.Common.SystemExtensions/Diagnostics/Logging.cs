using Psns.Common.Analysis;
using Psns.Common.Functional;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    /// <summary>
    /// A function that writes a log message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="category"></param>
    /// <param name="eventType">The type of event to log</param>
    public delegate void Log(string message, string category, TraceEventType eventType);

    /// <summary>
    /// A function that formats bench mark data for logging.
    /// </summary>
    /// <param name="currentThreadId"></param>
    /// <param name="runtime"></param>
    /// <param name="currentProcessMemoryUsage">In MB</param>
    /// <param name="description"></param>
    /// <returns><see cref="string"/> to be logged</returns>
    public delegate string FormatBenchmarkStats(int currentThreadId, TimeSpan runtime, long currentProcessMemoryUsage, string description);

    #pragma warning disable 1591

    public static partial class Prelude
    {
        public const string GeneralLogCategory = "General";
        public const TraceEventType DefaultLogEventType = TraceEventType.Information;

        /// <summary>
        /// Creates a <see cref="Diagnostics.Log"/> that only writes to log if 
        /// error type is not <see cref="TraceEventType.Error"/> 
        /// or <paramref name="classify"/> returns <see cref="Anomaly.Classification.Norm"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="classify">A classifying function</param>
        /// <returns></returns>
        /// <remarks>Logs an entry of type <see cref="TraceEventType.Verbose"/> if not logging.</remarks>
        public static Log UseErrorClassification(
            this Log self,
            Func<Anomaly.Classification> classify) =>
                new Log((msg, cat, eType) =>
                {
                    var classification = classify();

                    var shouldLog = eType == TraceEventType.Error
                        ? classification.IsNorm
                            ? true
                            : false
                        : true;

                    if (shouldLog)
                        self(msg, cat, eType);
                    else
                        self(
                            $"Not logging: Classification: {classification}",
                            cat,
                            TraceEventType.Verbose);
                });

        #region Benchmark

        public static T Benchmark<T>(this Log self, T val, Action<T> func, string description = "") =>
            self.Benchmark(() => { func(val); return val; }, description, None);

        public static R Benchmark<T, R>(this Log self, T val, Func<T, R> func, string description = "") =>
            self.Benchmark(() => func(val), description, None);

        public static UnitValue Benchmark(this Log self, Action act, string description = "") =>
            self.Benchmark(() => { act(); return Unit; }, description, None);

        public static Func<Either<L, R>> ComposeWithBench<T1, R, L>(
            this Func<T1, Either<L, R>> self,
            Log logger,
            Func<Either<L, T1>> func,
            string description = "") =>
            () => func().Match(
                right: t1 => logger.Benchmark(() => self(t1), description, None),
                left: ex => ex);

        public static async Task<T> BenchmarkAsync<T>(
            this Log self,
            Func<Task<T>> func,
            string description,
            Maybe<FormatBenchmarkStats> format,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var val = await func();

            stopWatch.Stop();

            var span = stopWatch.Elapsed;

            return self.Log(
                val,
                Format(format, span, description),
                category,
                type);
        }

        public static T Benchmark<T>(
            this Log self,
            Func<T> func,
            string description,
            Maybe<FormatBenchmarkStats> format,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var val = func();

            stopWatch.Stop();

            var span = stopWatch.Elapsed;

            return self.Log(
                val,
                Format(format, span, description),
                category,
                type);
        }

        static string Format(Maybe<FormatBenchmarkStats> format, TimeSpan span, string description) =>
            format.Match(
                some: fmt => fmt(
                    Thread.CurrentThread.ManagedThreadId,
                    span,
                    getMemoryUsage(),
                    description),
                none: () => string.Format(
                    "{0},{1},{2},{3}",
                    Thread.CurrentThread.ManagedThreadId,
                    description,
                    string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        span.Hours,
                        span.Minutes,
                        span.Seconds,
                        span.Milliseconds / 10),
                    getMemoryUsage()));

        public static long getMemoryUsage() => Process.GetCurrentProcess().PrivateMemorySize64 / 1000000;
        #endregion

        public static T Debug<T>(this Log self, T t) =>
            self.Log(t, type: TraceEventType.Verbose);

        public static T Debug<T>(this Log self, string message) =>
            self.Log<T>(message, type: TraceEventType.Verbose);

        public static T Debug<T>(this Log self, T t, string message) =>
            t.Tap(_ => self.Debug(message));

        public static T Debug<T>(this Log self, T t, Func<T, string> func) =>
            t.Tap(_ => self.Debug(func(t)));

        public static T Info<T>(this Log self, T t) =>
            self.Log(t, type: TraceEventType.Information);

        public static T Info<T>(this Log self, string message) =>
            self.Log<T>(message, type: TraceEventType.Information);

        public static T Info<T>(this Log self, T t, string message) =>
            t.Tap(_ => self.Info(message));

        public static T Info<T>(this Log self, T t, Func<T, string> func) =>
            t.Tap(_ => self.Info(func(t)));

        public static T Warning<T>(this Log self, T t) =>
            self.Log(t, type: TraceEventType.Warning);

        public static T Warning<T>(this Log self, string message) =>
            self.Log<T>(message, type: TraceEventType.Warning);

        public static T Warning<T>(this Log self, T t, Func<T, string> func) =>
            t.Tap(_ => self.Log<T>(func(t), type: TraceEventType.Warning));

        public static T Error<T>(this Log self, T t) =>
            self.Log(t, type: TraceEventType.Error);

        public static T Error<T>(this Log self, string message) =>
            self.Log<T>(message, type: TraceEventType.Error);

        public static T Error<T>(this Log self, T t, Func<T, string> func) =>
            t.Tap(_ => self.Log<T>(func(t), type: TraceEventType.Error));

        public static T LogIf<T>(this Log self, 
            T val, 
            Func<T, bool> predicate, 
            Func<T, string> message,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                predicate(val)
                    ? val.Tap(_ => self.Log(message, category, type))
                    : val;

        public static T LogIf<T>(this Log self,
            T val,
            string message,
            Func<T, bool> predicate,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                predicate(val)
                    ? val.Tap(_ => self.Log(message, category, type))
                    : val;

        public static T Log<T>(
            this Log self, 
            string message, 
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                default(T).Tap(t => self.Log(val: message, type: type));

        public static T Log<T>(
            this Log self,
            T val,
            string message,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                val.Tap(_ => self(message, category, type));

        public static T Log<T>(
            this Log self,
            T val,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                val.Tap(_ => self(val.ToString(), category, type));

        public static T Log<T>(
            this Log self,
            T val,
            Func<T, string> func,
            string category = GeneralLogCategory,
            TraceEventType type = DefaultLogEventType) =>
                val.Tap(_ => self(func(val), category, type));
    }
    
    #pragma warning restore 1591
}