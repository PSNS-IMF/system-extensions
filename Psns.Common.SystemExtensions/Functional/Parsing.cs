using Psns.Common.Functional;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Functional
{
    public static partial class Prelude
    {
        public static Maybe<int> ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed)
                ? Some(parsed)
                : Maybe<int>.None;
        }
    }
}
