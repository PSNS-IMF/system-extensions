﻿using Psns.Common.Functional;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;
using static Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule;

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
        /// Enables throttling of Error logging based on the rate of incoming Errors.
        /// Only Errors are throttled and are only logged if the current <see cref="ErrorState"/>
        /// is <c>Normal</c>.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="stateMachine">A function that determines the current 
        /// <see cref="ErrorStateTransition"/> and <see cref="ErrorState"/></param>
        /// <returns></returns>
        public static Log WithThrottling(this Log self, Func<string, (ErrorStateTransition, ErrorState)> stateMachine) =>
            new Log((msg, cat, eType) =>
            {
                var logStatus = fun(((ErrorStateTransition, ErrorState) transitionState) =>
                    transitionState.Tap(
                    _ => self(transitionState.ToString(), ErrorLoggingStateModule.LogCategory, TraceEventType.Verbose),
                    _ => { if (transitionState.Item1 == ErrorStateTransition.Saturating) { self($"{nameof(ErrorState)} has become saturated. Last Error was: {msg}", "General", TraceEventType.Error); } },
                    _ => { if (transitionState.Item1 == ErrorStateTransition.Normalizing) { self($"{nameof(ErrorState)} has normalized", "General", TraceEventType.Error); } }));

                var getState = logStatus.Compose(stateMachine);

                var doLog = eType == TraceEventType.Error
                    ? map(getState(msg), tState => tState.Item2 == ErrorState.Normal)
                    : true;

                if (doLog)
                    self(msg, cat, eType);
            });

        #region Benchmark

        public static T Benchmark<T>(this Log self, T val, Action<T> func, string description = "") =>
            self.Benchmark(() => { func(val); return val; }, description, None);

        public static R Benchmark<T, R>(this Log self, T val, Func<T, R> func, string description = "") =>
            self.Benchmark(() => func(val), description, None);

        public static Unit Benchmark(this Log self, Action act, string description = "") =>
            self.Benchmark(() => { act(); return unit; }, description, None);

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