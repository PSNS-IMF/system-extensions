using System;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Psns.Common.SystemExtensions
{
    public static class LanguageExtExtensions
    {
        public static T Tee<T>(this T self, params Action<T>[] acts)
        {
            foreach(var act in acts)
                act(self);
            return self;
        }

        public static async Task<T> TeeAsync<T>(this T self, params Func<T, Task>[] actions)
        {
            foreach(var action in actions)
                await action(self);
            return self;
        }

        public static Either<Exception, T> ToEither<T>(this Try<T> self) =>
            self.Match(Succ: r => r, Fail: ex => Left<Exception, T>(ex));

        public static Either<Exception, R> ToEither<R>(this Try<Either<Exception, R>> self) =>
            self.Match(Succ: r => r, Fail: ex => Left<Exception, R>(ex));

        public static Either<L, Ret> Regardless<L, R, Ret>(this Either<L, R> self, Func<Either<L, Ret>> func) =>
            self.Bind(Right: t => func(), Left: ex => func());
    }
}