using System;
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
    }

    public static class F
    {
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