using System;

namespace Psns.Common.SystemExtensions
{
    public static class ExceptionExtensions
    {
        public static string GetExceptionChainMessages(this Exception exception)
        {
            var messageBuilder = new System.Text.StringBuilder(string.Format("Type: {0} Message: {1} Data: {2} Trace: {3}", 
                exception.GetType().Name,
                exception.Message,
                exception.Data,
                exception.StackTrace));

            var currentException = exception.InnerException;

            while(currentException != null)
            {
                messageBuilder.Append(string.Format(" InnerException Type: {0} Message: {1} Data: {2} Trace: {3}", 
                    currentException.GetType().Name,
                    currentException.Message,
                    currentException.Data,
                    currentException.StackTrace));

                currentException = currentException.InnerException;
            }

            return messageBuilder.ToString();
        }
    }
}