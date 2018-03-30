using Psns.Common.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions
{
    public static class IEnumerableExtensions
    {
        public const int ChunkSize = 500;

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> items, int chunkSize = ChunkSize)
        {
            int pageNumber = 1;
            var chunk = items.Take(chunkSize);

            do
            {
                chunk = items.Skip(chunkSize * pageNumber++).Take(chunkSize);
                yield return chunk;
            }
            while(chunk.Count() > 0);
        }

        public static UnitValue Iter<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self)
            {
                action(item);
            }

            return Unit;
        }

        public static async Task<UnitValue> IterAsync<T>(this IEnumerable<T> self, Action<T> action, CancellationToken? cancelToken = null) =>
            await Map(cancelToken ?? CancellationToken.None, async token =>
                await Task.Run(async () =>
                {
                    foreach (var item in self)
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            action(item);

                            token.ThrowIfCancellationRequested();
                        },
                            token,
                            TaskCreationOptions.AttachedToParent,
                            TaskScheduler.Current);
                    }

                    token.ThrowIfCancellationRequested();

                    return Unit;
                }, token));

        public static TryAsync<UnitValue> TryIterAsync<T>(this IEnumerable<T> self, Action<T> action, CancellationToken? cancelToken = null) =>
            TryAsync(() => self.IterAsync(action, cancelToken));
    }
}