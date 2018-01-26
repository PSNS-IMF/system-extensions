using LanguageExt;
using System;
using System.Data;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static Psns.Common.SystemExtensions.LanguageExtExtensions;

namespace Psns.Common.SystemExtensions.Database
{
    public static partial class Prelude
    {
        /// <summary>
        /// Open a SqlConnection, run a function, then Dispose of connection
        /// </summary>
        public async static Task<Either<Exception, R>> ConnectAsync<R>(
            Func<Either<Exception, string>> buildConnectionString,
            Func<string, IDbConnection> factory,
            Func<IDbConnection, Task<IDbConnection>> openAsync,
            Func<IDbConnection, Task<Either<Exception, R>>> func) =>
                await bindAsync(
                    buildConnectionString(),
                    cString =>
                        use(factory(cString),
                            connection =>
                                bindAsync(
                                    TryAsync(() => openAsync(connection)),
                                    conn => func(conn))));

        /// <summary>
        /// Open a SqlConnection, begin a transaction, execute function, dispose of connection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connect"></param>
        /// <returns></returns>
        public static Func<Func<IDbTransaction, Either<Exception, T>>, Either<Exception, T>> BeginTransaction<T>(
            Func<Func<IDbConnection, Either<Exception, T>>, Either<Exception, T>> connect) =>
            func =>
                connect(
                    connection =>
                        Try(() => connection.BeginTransaction())
                            .Match(
                                Succ: transaction => func(transaction),
                                Fail: exception => Left<Exception, T>(exception)));

        /// <summary>
        /// Create a SqlCommand and associate it to the given transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static Func<IDbCommand> CommandFactory(IDbTransaction transaction) => () =>
            transaction.Connection.CreateCommand().Tee(cmd => cmd.Transaction = transaction);
    }
}