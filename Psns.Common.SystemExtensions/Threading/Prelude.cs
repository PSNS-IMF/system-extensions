using Psns.Common.Functional;
using System;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions
{
    /// <summary>
    /// Stateless helper functions.
    /// </summary>
    public static partial class Prelude
    {
        /// <summary>
        /// Parses <paramref name="configValue"/> as an <see cref="int"/>;
        /// otherwise, uses <see cref="Environment.ProcessorCount"/>.
        /// </summary>
        /// <param name="configValue">A <see cref="string"/> to be parsed as an <see cref="int"/></param>
        /// <returns></returns>
        public static int GetDegreeOfParallelism(Maybe<string> configValue) =>
            configValue.Bind(parseInt).Match(
                some: i => i,
                none: () => Environment.ProcessorCount);
    }
}
