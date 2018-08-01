using Psns.Common.Analysis;
using Psns.Common.Functional;
using System;
using System.Threading.Tasks;
using static Psns.Common.Analysis.Anomaly;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    /// <summary>
    /// Contains functions to help determine the current <see cref="ErrorState"/>.
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
                map(
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
        /// Possible error states
        /// </summary>
        public enum ErrorState
        {
            /// <summary>
            /// The error rate is normal
            /// </summary>
            Normal,
            /// <summary>
            /// The error rate is saturated
            /// </summary>
            Saturated
        }

        /// <summary>
        /// Changes in <see cref="ErrorState"/>.
        /// </summary>
        public enum ErrorStateTransition
        {
            /// <summary>
            /// No change
            /// </summary>
            None,

            /// <summary>
            /// <see cref="ErrorState.Normal"/> -> <see cref="ErrorState.Saturated"/>
            /// </summary>
            Saturating,

            /// <summary>
            /// <see cref="ErrorState.Saturated"/> -> <see cref="ErrorState.Normal"/>
            /// </summary>
            Normalizing
        }

        /// <summary>
        /// <see cref="ErrorState.Normal" /> as a <see cref="Functional.State{TValue, TState}"/>.
        /// </summary>
        /// <returns></returns>
        public static State<Classification, ErrorState> NormalState(Classification classification) => oldState =>
            map(ErrorState.Normal, state => (classification, state));

        /// <summary>
        /// <see cref="ErrorState.Saturated" /> as a <see cref="Functional.State{TValue, TState}"/>.
        /// </summary>
        /// <returns></returns>
        public static State<Classification, ErrorState> SaturatedState(Classification classification) => oldState =>
            map(ErrorState.Saturated, state => (classification, state));

        /// <summary>
        /// Composes a function that determines the current <see cref="ErrorState"/>
        /// based on the result of <see cref="Func{Classification}"/>.
        /// </summary>
        /// <returns></returns>
        public static Func<Func<Classification>, State<Classification, ErrorState>> ErrorRateStateMachine => classify =>
            from classification in State<Classification, ErrorState>(classify())
            from next in classification.IsNorm
                ? NormalState(classification)
                : SaturatedState(classification)
            select next;

        /// <summary>
        /// Composes a function that only executes the given function after
        /// an initial/first call when<see cref="ErrorState"/> is becoming <c>Saturated</c>.
        /// </summary>
        /// <returns></returns>
        public static Func<
            Func<string, ErrorState, Task>,
            State<Classification, ErrorState>,
            string,
            State<Classification, ErrorState>> StateMachineObserver => (sendMail, source, errMsg) =>
                from classification in source.Bind((prev, current) =>
                    (current.Item1, current.Item2.Tap(_ =>
                        Match(DidChange(current.Item1, prev) && current.Item1.IsHigh && prev == ErrorState.Normal,
                            AsEqual(true, __ => sendMail(errMsg, ErrorState.Saturated)),
                            __ => UnitTask()))))
                select classification;

        static bool DidChange(Classification classification, ErrorState state) =>
            (classification.IsNorm && state != ErrorState.Normal)
                || (!classification.IsNorm && state == ErrorState.Normal);

        internal static T LogState<T>(this Log log, T val, Func<T, string> format) =>
            log.Log(val, format(val), LogCategory, System.Diagnostics.TraceEventType.Verbose);
    }

    public static partial class Prelude
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <param name="self"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public static State<TResult, TState> Bind<TValue, TResult, TState>(
            this State<TValue, TState> self,
            State<TResult, TState> next) => state =>
                map(self(state), res => next(res.State));

        /// <summary>
        /// Binds a function to <paramref name="self"/> that takes both 
        /// previous and current <see cref="Functional.State{TValue, TState}"/>s.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TState"></typeparam>
        /// <param name="self"></param>
        /// <param name="binder">Takes the previous and the current <see cref="Functional.State{TValue, TState}"/></param>
        /// <returns></returns>
        public static State<TResult, TState> Bind<TValue, TResult, TState>(
            this State<TValue, TState> self,
            Func<TState, (TValue, TState), (TResult, TState)> binder) => prevState =>
                map(self(prevState), state => binder(prevState, (state.Value, state.State)));
    }
}