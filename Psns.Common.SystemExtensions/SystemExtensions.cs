using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Psns.Common.Logging;

namespace Psns.Common.SystemExtensions
{
    public static class SystemExtensions
    {
        public static void ThrowIfNull(this object o, string paramName = "", string message = "")
        {
            if(o == null)
                throw new ArgumentNullException(paramName, message);
        }

        public static T Map<T>(this T @this, Action<T> action)
        {
            action(@this);
            return @this;
        }

        public static R Map<T, R>(this T @this, Func<T, R> func)
        {
            return func(@this);
        }

        public static bool Match(this bool @this, Action @true = null, Action @false = null, Action both = null)
        {
            var func = @this ? @true : @false;

            try
            {
                if(func != null)
                    func();
            }
            finally
            {
                if(both != null) both();
            }

            return @this;
        }

        public static T Match<T>(this bool @this, Func<T> @true = null, Func<T> @false = null, Action both = null)
        {
            var func = @this ? @true : @false;
            T result = default(T);

            try
            {
                if(func != null)
                    result = func();
            }
            finally
            {
                if(both != null) both();
            }

            return result;
        }

        public static void Iter<T>(this IEnumerable<T> items, Action<T> action)
        {
            items.Iter((item, index) => action(item));
        }

        public static void Iter<T>(this IEnumerable<T> items, Action<T, int> action)
        {
            for(var i = 0; i < items.Count(); i++)
                action(items.ElementAt(i), i);
        }
    }

    public static class F
    {
        public static void Use<T>(T resource, Action<T> func) where T : IDisposable
        {
            using(resource) func(resource);
        }

        public static R Use<T, R>(T resource, Func<T, R> func) where T : IDisposable
        {
            using(resource) return func(resource);
        }

        public static T Fun<T>(Func<T> func) { return func(); }

        /// <summary>
        /// Performs a synchronized action after acquiring a Mutex
        /// </summary>
        /// <param name="name">The name of the Mutex</param>
        /// <param name="work"></param>
        /// <param name="waitTimeout"></param>
        /// <param name="logger">Optional logger for potentional Mutex debugging info</param>
        /// <param name="runAnyway">If True, do work even if mutex can't be acquired</param>
        /// <returns>False if mutex wasn't acquired and work wasn't performed; otherwise, True</returns>
        public static bool WithMutex(string name, Action work, int waitTimeout = Timeout.Infinite, ILogger logger = null, bool runAnyway = false)
        {
            using(var mutex = new Mutex(false, name))
            {
                var hasHandle = false;

                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(waitTimeout, false);
                    }
                    catch(AbandonedMutexException e)
                    {
                        if(logger != null)
                            logger.Warning(string.Format("Mutex was aquired, but another process didn't properly release it: {0}", e.GetExceptionChainMessages()));

                        hasHandle = true;
                    }

                    if(hasHandle || runAnyway)
                        work();
                }
                finally
                {
                    if(hasHandle) mutex.ReleaseMutex();
                }

                return hasHandle;
            }
        }
    }
}