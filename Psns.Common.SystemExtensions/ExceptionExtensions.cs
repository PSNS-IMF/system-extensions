using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

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

        public static string GetExceptionChainMessagesWithSql(this Exception self)
        {
            var builder = new StringBuilder();

            var sqlException = self is SqlException
                ? self as SqlException
                : self.InnerException is SqlException
                    ? self.InnerException as SqlException
                    : null;

            if (sqlException != null)
            {
                builder.Append($@"ErrorCode: {sqlException.ErrorCode}, Number: {sqlException.Number}, ");

                var errors = new SqlError[sqlException.Errors.Count];
                sqlException.Errors.CopyTo(errors, 0);

                builder.Append("Errors: [");
                builder.Append(string.Join(", ", errors.Select((error, index) =>
                    $"{{ Index: {index} Message:{error.Message} Number:{error.Number} SPName: {error.Procedure} Line Number: {error.LineNumber} }}")));
                builder.Append("]");
            }

            return self.GetExceptionChainMessages() + Environment.NewLine + builder.ToString();
        }
    }
}