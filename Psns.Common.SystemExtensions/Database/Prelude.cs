using Psns.Common.Functional;
using System;
using System.Data;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;

namespace Psns.Common.SystemExtensions.Database
{
    public static partial class Prelude
    {
        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbConnection"/>,
        ///   run a <see cref="Func{IDbConnection, Try{T}}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<Func<IDbConnection, Try<T>>, Func<IDbConnection>, Try<T>> Connect<T>() =>
            (func, factory) => 
                Use(factory, func);

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbConnection"/>,
        ///   open a <see cref="IDbConnection"/> asynchronously,
        ///   run a <see cref="Func{IDbConnection, TryAsync{T}}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<
            Func<IDbConnection, TryAsync<T>>,
            Func<IDbConnection, Task<IDbConnection>>,
            Func<IDbConnection>,
            TryAsync<T>> ConnectAsync<T>() => (func, openAsync, factory) =>
                Use(factory, conn => TryAsync(() => openAsync(conn))).Bind(func);

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbTransaction, Try{T}}"/>, 
        ///   and dispose of the <see cref="IDbTransaction"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<Func<IDbTransaction, Try<T>>, IDbConnection, Try<T>> BeginTransaction<T>() =>
            (func, connection) =>
                TryUse(() => connection.BeginTransaction(), func);

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbTransaction, TryAsync{T}}"/> asynchronously, 
        ///   and dispose of the <see cref="IDbTransaction"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<Func<IDbTransaction, TryAsync<T>>, IDbConnection, TryAsync<T>> BeginTransactionAsync<T>() =>
            (func, connection) =>
                TryUse(() => connection.BeginTransaction(), func);

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbCommand"/>,
        ///   associate the <see cref="IDbCommand"/> with the <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbCommand, T}"/>,
        ///   and dispose of the <see cref="IDbCommand"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<Func<IDbCommand, T>, IDbTransaction, Try<T>> CreateCommand<T>() =>
            (func, transaction) =>
                Try(() => transaction.Connection.CreateCommand().Tap(cmd => cmd.Transaction = transaction))
                    .Bind(cmd => Try(() => Use(cmd, func)));

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbCommand"/>,
        ///   associate the <see cref="IDbCommand"/> with the <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbCommand, TaskT}"/> asynchronously,
        ///   and dispose of the <see cref="IDbCommand"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<Func<IDbCommand, Task<T>>, IDbTransaction, TryAsync<T>> CreateCommandAsync<T>() =>
            (func, transaction) =>
                Try(() => transaction.Connection.CreateCommand().Tap(cmd => cmd.Transaction = transaction))
                    .Bind(cmd => TryAsync(() => Use(cmd, func)));

        /// <summary>
        /// Create a meaningful <see cref="string"/> representation of an <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="callerName"></param>
        /// <returns><see cref="string"/></returns>
        public static string ToLogString(this IDbCommand self, Maybe<string> callerName) =>
            $@"{callerName} -> Param Count: {self?.Parameters?.Count.ToString() ?? "Null"} Connection State: {
                self?.Connection?.State.ToString() ?? "Null"} Transaction Isolation Level: {
                self?.Transaction?.IsolationLevel.ToString() ?? "Null"}";
    }
}