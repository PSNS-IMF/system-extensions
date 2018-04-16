using Psns.Common.Functional;
using System;
using System.Diagnostics;
using static Psns.Common.Functional.Prelude;
using static Psns.Common.SystemExtensions.Diagnostics.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    public static class EitherExtensions
    {
        public static Either<L, Ret> Benchmark<L, R, Ret>(this Either<L, R> self, Func<R, Either<L, Ret>> binder, Maybe<Log> log, string description) =>
            self.Match(
                r => log.Match(l => l.Benchmark(() => binder(r), description, None), () => binder(r)),
                l => Left<L, Ret>(l));

        public static Either<LRet, R> Benchmark<L, R, LRet>(this Either<L, R> self, Func<L, Either<LRet, R>> binder, Maybe<Log> mLog, string description) =>
            self.Match(
                r => Right<LRet, R>(r),
                l => mLog.Match(log => log.Benchmark(() => binder(l), description, None), () => binder(l)));

        public static Either<L, R> Log<L, R>(this Either<L, R> self, Maybe<Log> mLog, Func<R, string> message, string category = GeneralLogCategory, TraceEventType type = DefaultLogEventType) =>
            self.Match(
                r => mLog.Match(
                    log => log.Log(r, message(r), category, type),
                    () => Right<L, R>(r)),
                l => Left<L, R>(l));
    }
}
