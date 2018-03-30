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
        ///   run a <see cref="Func{IDbConnection, T}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<Func<IDbConnection>, Func<IDbConnection, T>, Try<T>> Connect<T>() =>
            (factory, func) => 
                Use(factory, conn => Try(() => func(conn)));

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbConnection"/>,
        ///   open a <see cref="IDbConnection"/> asynchronously,
        ///   run a <see cref="Func{IDbConnection, Task{T}}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<
            Func<IDbConnection>,
            Func<IDbConnection, Task<IDbConnection>>,
            Func<IDbConnection, Task<T>>,
            TryAsync<T>> ConnectAsync<T>() => (factory, openAsync, func) =>
                Use(factory, conn => TryAsync(() => openAsync(conn))).Bind(func);

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbTransaction, T}"/>, 
        ///   and dispose of the <see cref="IDbTransaction"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<IDbConnection, Func<IDbTransaction, T>, Try<T>> BeginTransaction<T>() =>
            (connection, func) =>
                Try(() => Use(connection.BeginTransaction(), func));

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbTransaction, Task{T}}"/> asynchronously, 
        ///   and dispose of the <see cref="IDbTransaction"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<IDbConnection, Func<IDbTransaction, Task<T>>, TryAsync<T>> BeginTransactionAsync<T>() =>
            (connection, func) =>
                TryAsync(() => Use(connection.BeginTransaction(), func));

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbCommand"/>,
        ///   associate the <see cref="IDbCommand"/> with the <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbCommand, T}"/>,
        ///   and dispose of the <see cref="IDbCommand"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<IDbTransaction, Func<IDbCommand, T>, Try<T>> CreateCommand<T>() =>
            (transaction, func) =>
                Try(() => transaction.Connection.CreateCommand().Tap(cmd => cmd.Transaction = transaction))
                    .Bind(cmd => Try(() => Use(cmd, func)));

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbCommand"/>,
        ///   associate the <see cref="IDbCommand"/> with the <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbCommand, Task{T}}"/> asynchronously,
        ///   and dispose of the <see cref="IDbCommand"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<IDbTransaction, Func<IDbCommand, Task<T>>, TryAsync<T>> CreateCommandAsync<T>() =>
            (transaction, func) =>
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