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

            while (chunk.Count() > 0)
            {
                yield return chunk;
                chunk = items.Skip(chunkSize * pageNumber++).Take(chunkSize);
            }
        }

        public static Unit Iter<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self)
            {
                action(item);
            }

            return unit;
        }

        public static async Task<Unit> IterAsync<T>(this IEnumerable<T> self,
            Action<T> func,
            Maybe<CancellationToken> token,
            Maybe<TaskScheduler> scheduler) =>
                await map(scheduler | TaskScheduler.Current, async chosenScheduler =>
                    await map(token | CancellationToken.None, async chosenToken =>
                        (await Task.WhenAll(self.Aggregate(
                            new List<Task<Unit>>(),
                            (list, next) =>
                            {
                                list.Add(Task.Factory.StartNew(() =>
                                    {
                                        chosenToken.ThrowIfCancellationRequested();
                                        func(next);
                                        return unit;
                                    },
                                    chosenToken,
                                    TaskCreationOptions.None,
                                    chosenScheduler));

                                return list;
                            })))
                        .FirstOrDefault()));

        public static TryAsync<Unit> TryIterAsync<T>(this IEnumerable<T> self, 
            Action<T> action,
            Maybe<CancellationToken> cancelToken,
            Maybe<TaskScheduler> scheduler) =>
                TryAsync(() => self.IterAsync(action, cancelToken, scheduler));
    }
}