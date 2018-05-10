using Psns.Common.Functional;
using System;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    /// <summary>
    /// Represents the error logging state based on it's rate of occurrence.
    /// </summary>
    public struct ErrorLoggingState
    {
        internal readonly Maybe<DateTime> Default;

        public readonly Maybe<DateTime> SaturatingAck;
        public readonly Maybe<DateTime> Saturated;
        public readonly Maybe<DateTime> NormalizingAck;
        public readonly Maybe<DateTime> Normal;

        /// <summary>
        /// Default uninitialized state.
        /// </summary>
        public bool IsUninitiated =>
            !IsNormal
                && !IsSaturating
                && !IsSaturated
                && !IsNormalizing;

        /// <summary>
        /// Error rate is normal.
        /// </summary>
        public bool IsNormal =>
            Normal.IsSome
                && !IsSaturating
                && !IsSaturated
                && !IsNormalizing;

        /// <summary>
        /// Error rate limit has been reached for the first time.
        /// </summary>
        public bool IsSaturating =>
            SaturatingAck.IsSome && !IsSaturated;

        /// <summary>
        /// Error rate limit has been reached.
        /// </summary>
        public bool IsSaturated =>
            Saturated.IsSome;

        /// <summary>
        /// Error rate is returning to normal.
        /// </summary>
        public bool IsNormalizing =>
            NormalizingAck.IsSome;

        internal ErrorLoggingState(
            Maybe<DateTime> def,
            Maybe<DateTime> normal,
            Maybe<DateTime> saturating,
            Maybe<DateTime> saturated,
            Maybe<DateTime> normalizing)
        {
            Default = def | DateTime.Now;

            Normal = normal;
            SaturatingAck = saturating;
            Saturated = saturated;
            NormalizingAck = normalizing;
        }

        public override bool Equals(object obj)
        {
            var result = false;

            if (!IsNull(obj) && obj is ErrorLoggingState)
            {
                var other = (ErrorLoggingState)obj;

                result = other.Normal == Normal
                    && other.SaturatingAck == SaturatingAck
                    && other.Saturated == Saturated
                    && other.NormalizingAck == NormalizingAck;
            }

            return result;
        }

        public override int GetHashCode() =>
            Normal.GetHashCode()
                ^ SaturatingAck.GetHashCode()
                ^ Saturated.GetHashCode()
                ^ NormalizingAck.GetHashCode();

        public override string ToString() =>
            $@"{{{nameof(IsNormal)}: {IsNormal}, {
                nameof(IsSaturating)}: {IsSaturating}, {
                nameof(IsSaturated)}: {IsSaturated}, {
                nameof(IsNormalizing)}: {IsNormalizing}}}";
    }

    public static partial class ErrorLoggingStateModule
    {
        /// <summary>
        /// Creates a Normal state using <see cref="DateTime.Now"/>.
        /// </summary>
        /// <returns></returns>
        public static ErrorLoggingState Normal() =>
            new ErrorLoggingState(None, DateTime.Now, None, None, None);

        /// <summary>
        /// Creates a Normal state using <paramref name="def"/> as the time stamp to use
        /// as each state is reached.
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public static ErrorLoggingState Normal(Maybe<DateTime> def) =>
            new ErrorLoggingState(def, def, None, None, None);
    }

    public static class ErrorLoggingStateExtensions
    {
        public static ErrorLoggingState AsNormal(this ErrorLoggingState self) =>
            new ErrorLoggingState(self.Default, self.Normal, None, None, None);

        public static ErrorLoggingState Saturating(this ErrorLoggingState self) =>
            new ErrorLoggingState(self.Default, self.Normal, self.Default, None, None);

        public static ErrorLoggingState AsSaturated(this ErrorLoggingState self) =>
            new ErrorLoggingState(self.Default, self.Normal, None, self.Default, None);

        public static ErrorLoggingState Normalizing(this ErrorLoggingState self) =>
            new ErrorLoggingState(self.Default, self.Normal, None, self.Saturated, self.Default);

        public static T Map<T>(this ErrorLoggingState self, Func<ErrorLoggingState, T> func) =>
            func(self);
    }
}
