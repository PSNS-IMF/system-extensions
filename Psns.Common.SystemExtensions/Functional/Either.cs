﻿using Psns.Common.SystemExtensions.Diagnostics;
using System;
using System.Diagnostics;
using static Psns.Common.SystemExtensions.Diagnostics.Prelude;

namespace Psns.Common.Functional
{
    public static partial class Prelude
    {
        public static Either<L, R> Right<L, R>(R right) =>
            Either<L, R>.Right<L, R>(right);

        public static Either<L, R> Left<L, R>(L left) =>
            Either<L, R>.Left<L, R>(left);

        public static TryAsync<R> Bind<T, R>(this Either<Exception, T> self, Func<T, TryAsync<R>> binder) => () =>
            self.Match(
                t => binder(t).TryAsync(),
                e => new TryResult<R>(e).AsTask());

        public static Try<R> Bind<T, R>(this Either<Exception, T> self, Func<T, Try<R>> binder) => () =>
            self.Match(
                t => binder(t).Try(),
                e => new TryResult<R>(e));

        public static UnitValue Match<L, R>(this Either<L, R> self, Action<R> right, Action<L> left) =>
            self.Match(
                r => { right(r); return Unit; },
                e => { left(e); return Unit; });

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

    public struct Either<L, R>
    {
        enum EitherState
        {
            None,
            Left,
            Right
        }

        readonly L _left;
        readonly R _right;
        readonly EitherState _state;

        bool IsLeft =>  _state == EitherState.Left;
        bool IsRight => _state == EitherState.Right;
        bool IsNone =>  _state == EitherState.None;

        Either(L left)
        {
            NullCheck(left, nameof(left));

            _left = left;
            _right = default(R);
            _state = EitherState.Left;
        }

        Either(R right)
        {
            NullCheck(right, nameof(right));

            _right = right;
            _left = default(L);
            _state = EitherState.Right;
        }

        static void NullCheck(object o, string location)
        {
            if (o == null)
                throw new ArgumentNullException(location);
        }

        public static Either<Left, Right> Left<Left, Right>(Left left) =>
            new Either<Left, Right>(left);

        public static Either<Left, Right> Right<Left, Right>(Right right) =>
            new Either<Left, Right>(right);

        public Either<L, Ret> Bind<Ret>(Func<R, Either<L, Ret>> binder) =>
            IsNone
                ? Either<L, Ret>.None
                : IsRight
                    ? binder(_right)
                    : Left<L, Ret>(_left);

        public Either<Left, R> Bind<Left>(Func<L, Either<Left, R>> binder) =>
            IsNone
                ? Either<Left, R>.None
                : IsLeft
                    ? binder(_left)
                    : Right<Left, R>(_right);

        public Either<L, R> Append(Either<L, R> other) =>
            IsNone
                ? None
                : IsLeft
                    ? this : other;

        public Ret Match<Ret>(Func<R, Ret> right, Func<L, Ret> left)
        {
            if (IsNone)
            {
                throw new InvalidOperationException($"Either<{nameof(L)}, {nameof(R)}> of state None does not match Left or Right");
            }

            return IsLeft
                ? left(_left)
                : right(_right);
        }

        public static Either<L, R> None = new Either<L, R>();

        public static implicit operator Either<L, R>(R right) =>
            Right<L, R>(right);

        public static implicit operator Either<L, R>(L left) =>
            Left<L, R>(left);

        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj != null && obj is Either<L, R>)
            {
                var other = (Either<L, R>)obj;

                result = (IsNone && other.IsNone)
                    || (IsNone || other.IsNone
                        ? false
                        : IsLeft && other.IsLeft
                            ? _left.Equals(other._left)
                            : _right.Equals(other._right));
            }

            return result;
        }

        public override int GetHashCode() =>
            IsNone
                ? base.GetHashCode()
                : IsLeft ? _left.GetHashCode() : _right.GetHashCode();

        public static bool operator ==(Either<L, R> a, Either<L, R> b) =>
            a.Equals(b);

        public static bool operator !=(Either<L, R> a, Either<L, R> b) =>
            !a.Equals(b);

        public static Either<L, R> operator |(Either<L, R> a, Either<L, R> b) =>
            a.IsNone || b.IsNone
                ? a
                : a.IsRight 
                    ? a 
                    : b;
    }
}