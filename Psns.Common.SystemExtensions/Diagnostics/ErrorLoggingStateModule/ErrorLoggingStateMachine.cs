using Psns.Common.Analysis;
using Psns.Common.Functional;
using System;
using static Psns.Common.Analysis.Anomaly;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    /// <summary>
    /// Contains functions to help determine the current <see cref="ErrorLoggingState"/>.
    /// </summary>
    public static partial class ErrorLoggingStateModule
    {
        internal const string LogCategory = "ErrorLoggingState";

        /// <summary>
        /// Composes a function that classifies a <see cref="System.Tuple"/>
        /// of <see cref="Delta"/>,<see cref="double"/>
        /// by applying a given <see cref="Boundary"/> of <see cref="double"/>.
        /// </summary>
        public static Func<
            Func<Tuple<Delta, double>>,
            Func<Tuple<Delta, double>, Boundary<double>>,
            Func<Boundary<double>, double, Classification>,
            Classification> Classify => (getDeltaRate, applyBoundary, classify) =>
                Map(
                    getDeltaRate(),
                    deltaRate =>
                        classify.Par(
                            applyBoundary(deltaRate),
                            deltaRate.Item2)());

        static Func<double, double> round = fun((double d) => Math.Round(d, 4));
        static Func<double, double> min = fun((double d) => d * 0.001).Compose(round);
        static Func<double, double> max = fun((double d) => d * 0.999).Compose(round);

        /// <summary>
        /// Composes a function that creates a <see cref="Boundary"/> based on the memoized
        /// <see cref="System.Tuple{T1, T2}"/> of <see cref="Delta"/>,<see cref="double"/>
        /// given in the previous call by applying the extremes of 0.001 and 0.999 to the previous <c>Rate</c> 
        /// for the <c>Min</c> and <c>Max</c> values of the <see cref="Boundary"/>.
        /// The previous <c>Rate</c> is used because we're interested in the rate change.
        /// 
        /// The <c>infinityBoundary</c> is used to create the <see cref="Boundary"/> if the rate
        /// reaches <c>double.IsInfinity</c>.
        /// </summary>
        public static Func<double, Func<Tuple<Delta, double>, Boundary<double>>> ApplyBoundary => infinityBoundary =>
            Lib.memoizePrevPrev<Boundary<double>, Tuple<Delta, double>>(
                (a, b) => double.IsInfinity(b.Item2) 
                    ? Boundary.ofValues(min(infinityBoundary), max(infinityBoundary))
                    : Boundary.ofValues(min(b.Item2), max(b.Item2)),
                Boundary.ofValues(0d, 0d));

        /// <summary>
        /// Composes a function that determines the current <see cref="ErrorLoggingState"/>
        /// based on the memoized value of the previous <see cref="ErrorLoggingState"/>.
        /// </summary>
        public static Func<
            Log,
            Func<Classification>,
            Maybe<DateTime>,
            Func<ErrorLoggingState>> StateMachineFactory => (log, classify, start) =>
                Lib.memoizePrev(prev =>
                    classify().IsNorm
                        ? prev.IsSaturating || (prev.IsSaturated && !prev.IsNormalizing)
                            ? prev.Normalizing()
                            : prev.AsNormal()
                        : prev.IsSaturating || (prev.IsSaturated && !prev.IsNormalizing)
                            ? prev.AsSaturated()
                            : prev.Saturating(),
                    Normal(start));

        internal static T LogState<T>(this Log log, T val, Func<T, string> format) =>
            log.Log(val, format(val), LogCategory, System.Diagnostics.TraceEventType.Verbose);
    }
}
