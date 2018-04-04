﻿using NUnit.Framework;
using Psns.Common.Functional;
using System;
using System.Threading.Tasks;
using static NUnit.StaticExpect.Expectations;
using static Psns.Common.Functional.Prelude;
using static SystemExtensions.UnitTests.Functional.Ext;

namespace SystemExtensions.UnitTests.Functional
{
    [TestFixture]
    public class TryTests
    {
        [Test]
        public void Try_NoException_ReturnsValue() =>
            Expect(doTry(() => OkVal), EqualTo(OkVal));

        [Test]
        public void TryOfUnit_NoException_ReturnsValue() =>
            Expect(
                Try(() => { return; }).Match(unit => OkVal, e => FailVal), 
                EqualTo(OkVal));

        [Test]
        public void TryOfUnit_NoException_WithEither_ReturnsValue() =>
            Expect(
                Try(() => { return; }).Match(unit => OkVal, _ => FailVal, _ => EitherVal),
                EqualTo(EitherVal));

        [Test]
        public async Task TryAsync_NoException_ReturnsValue() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(t => OkVal)).Match(s => s, _ => FailVal), 
                EqualTo(OkVal));

        [Test]
        public async Task TryAsync_NoException_WithEither_ReturnsValue() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(t => OkVal)).Match(s => s, _ => FailVal, _ => EitherVal),
                EqualTo(EitherVal));

        [Test]
        public void Try_WithException_ReturnsException() =>
            Expect(doTry(failingTry), EqualTo(FailVal));

        [Test]
        public void TryOfUnit_WithException_ReturnsException() =>
            Expect(
                failingTry.Match(unit => OkVal, e => FailVal), 
                EqualTo(FailVal));

        [Test]
        public void Try_WithException_AndEither_ReturnsEither() =>
            Expect(
                failingTry.Match(_ => OkVal, _ => FailVal, f => f), 
                EqualTo(FailVal));

        [Test]
        public async Task TryAsync_WithException_ReturnsException() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(failingTask))
                    .Match(s => s, _ => FailVal),
                EqualTo(FailVal));

        [Test]
        public async Task TryAsync_WithException_AndEither_ReturnsException() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(failingTask))
                    .Match(s => s, _ => FailVal, s => s),
                EqualTo(FailVal));

        [Test]
        public void Binding_SuccessWithSuccess_BothExecuted() =>
            Expect(
                doTry(Try(() => OkVal).Bind(one => Try(() => $"{one} two"))), 
                EqualTo($"{OkVal} two"));

        [Test]
        public async Task BindingAsync_SuccessWithSuccess_ReturnsValue() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(t => OkVal))
                    .Bind(first => TryAsync(() => Task.Delay(1).ContinueWith(t => $"{first} second")))
                    .Match(s => s, _ => FailVal),
                EqualTo($"{OkVal} second"));

        [Test]
        public void Binding_FailureWithSuccess_FirstExceptionReturned() =>
            Expect(
                doTry(failingTry.Bind(one => Try(() => $"{one} two"))),
                EqualTo(FailVal));

        [Test]
        public async Task BindingAsync_SuccessWithFailure_ReturnsValue() =>
            Expect(
                await TryAsync(() => Task.Delay(1).ContinueWith(t => OkVal))
                    .Bind(first => TryAsync(() => Task.Delay(1).ContinueWith(failingTask)))
                    .Match(s => s, _ => FailVal),
                EqualTo(FailVal));

        [Test]
        public void Binding_SuccessWithFailure_ExceptionReturned() =>
            Expect(
                doTry(Try(() => OkVal).Bind(_ => failingTry)),
                EqualTo(FailVal));

        [Test]
        public async Task TryUse_DoesNotCallDisposeBeforeTaskFinishes()
        {
            var disposable = new Disposable();
            var user = fun((Disposable d) => TryAsync(async () =>
            {
                await Task.Delay(500);
                Expect(d.Disposed, False);
            }));

            var t = TryUse(() => disposable, user);

            var result = await t.Match(u => "ok", e => "fail");

            Expect(result, EqualTo("ok"));
            Expect(disposable.Disposed, True);
        }

        class Disposable : IDisposable
        {
            #region IDisposable Support
            public bool Disposed { get; private set; } = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!Disposed)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    Disposed = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
    }

    internal static class Ext
    {
        public const string OkVal = "ok";
        public const string FailVal = "fail";
        public const string EitherVal = "either";

        public static Try<string> failingTry => () =>
            {
                throw new Exception(FailVal);
            };

        public static Func<Task, string> failingTask => task =>
            {
                throw new Exception(FailVal);
            };

        public static string doTry(Try<string> self) =>
            self.Match(val => val, _ => FailVal);
    }
}