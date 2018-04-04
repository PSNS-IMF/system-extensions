﻿using System;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.Functional
{
    public static partial class Prelude
    {
        public static Maybe<T> Possible<T>(T t) => t;

        public static Maybe<T> Some<T>(T t)
        {
            if (IsNull(t))
            {
                throw new ArgumentNullException($"{nameof(t)} cannot be null");
            }

            return t;
        }

        public static MaybeNone None =>
            MaybeNone.Default;

        public static bool IsNull<T>(T value) =>
            value == null
                || (Nullable.GetUnderlyingType(typeof(T)) != null && value.Equals(default(T)));

        public static UnitValue Match<T>(this Maybe<T> self, Action<T> some, Action none) =>
            self.Match(t => { some(t); return Unit; }, () => { none(); return Unit; });
    }

    public struct Maybe<T>
    {
        readonly T _value;

        public bool IsNone => IsNull(_value);
        public bool IsSome => !IsNone;

        Maybe(T t)
        {
            _value = t;
        }

        public static Maybe<T> Some(T value) =>
            new Maybe<T>(value);

        public static readonly Maybe<T> None = new Maybe<T>();

        public void IfSome(Action<T> action)
        {
            if (IsSome)
            {
                action(_value);
            }
        }

        public Maybe<R> Bind<R>(Func<T, Maybe<R>> binder) =>
            IsNone
                ? Maybe<R>.None 
                : binder(_value);

        public Maybe<T> Append(Maybe<T> other) =>
            IsNone
                ? this
                : other;

        public R Match<R>(Func<T, R> some, Func<R> none) =>
            IsNone
                ? none()
                : some(_value);

        public static implicit operator Maybe<T>(T t) =>
            new Maybe<T>(t);

        public static implicit operator Maybe<T>(MaybeNone none) =>
            None;

        /// <summary>
        /// Left coalescing operator
        /// </summary>
        /// <param name="a"><see cref="Maybe{T}"/> to test</param>
        /// <param name="b">Value assigned if <paramref name="a"/> is <see cref="Maybe{T}.None"/></param>
        /// <returns><typeparamref name="T"/></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static T operator |(Maybe<T> a, T b) =>
            a.IsSome
                ? a._value
                : b.AssertValue();

        public override bool Equals(object obj)
        {
            bool result = false;

            if(obj != null && obj is Maybe<T>)
            {
                var other = (Maybe<T>)obj;

                result = (IsNone && other.IsNone)
                    || _value.Equals(other._value);
            }

            return result;
        }

        public override int GetHashCode() => 
            IsNone
                ? base.GetHashCode()
                : _value.GetHashCode();

        public static bool operator ==(Maybe<T> a, Maybe<T> b) =>
            a.Equals(b);

        public static bool operator !=(Maybe<T> a, Maybe<T> b) =>
            !a.Equals(b);

        public override string ToString() =>
            Match(
                val => val.ToString(),
                () => "None");
    }

    public struct MaybeNone
    {
        public static MaybeNone Default = new MaybeNone();

        public override string ToString() => "None";
    }
}
