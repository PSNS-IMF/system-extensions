﻿using Psns.Common.Functional;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;
using static Psns.Common.SystemExtensions.Prelude;

namespace Psns.Common.SystemExtensions.Database
{
    /// <summary>
    /// Stateless Database Helpers
    /// </summary>
    public static partial class Prelude
    {
        /// <summary>
        /// Build a connection string including MinPoolSize set to <paramref name="degreeOfParallelism"/>.
        /// </summary>
        /// <param name="existingString">An already valid connection string</param>
        /// <param name="degreeOfParallelism">If <see cref="None"/>, then
        /// <see cref="GetDegreeOfParallelism(Maybe{string})"/> is used.</param>
        /// <returns></returns>
        public static string BuildConnectionString(string existingString, Maybe<int> degreeOfParallelism) =>
            new SqlConnectionStringBuilder(existingString)
                .Tap(b => b.MinPoolSize = degreeOfParallelism | GetDegreeOfParallelism(None))
                .ToString();

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbConnection"/>,
        ///   run a <see cref="Func{IDbConnection, Try}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.Try{T}"/></returns>
        public static Func<Func<IDbConnection, Try<T>>, Func<IDbConnection>, Try<T>> Connect<T>() =>
            (func, factory) => () =>
                Use(factory, conn => func(conn).Try());

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbConnection"/>,
        ///   open a <see cref="IDbConnection"/> asynchronously,
        ///   run a <see cref="Func{IDbConnection, TryAsync}"/>, 
        ///   and dispose of the <see cref="IDbConnection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns><see cref="Functional.TryAsync{T}"/></returns>
        public static Func<
            Func<IDbConnection, TryAsync<T>>,
            Func<IDbConnection, Task<IDbConnection>>,
            Func<IDbConnection>,
            TryAsync<T>> ConnectAsync<T>() => (func, openAsync, factory) => async () =>
                await UseAsync(
                    factory, 
                    async conn => await TryAsync(() => openAsync(conn))
                        .Bind(func)
                        .TryAsync());

        /// <summary>
        /// Creates a function that tries to: 
        ///   create a <see cref="IDbTransaction"/>,
        ///   run a <see cref="Func{IDbTransaction, Try}"/>, 
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
        ///   run a <see cref="Func{IDbTransaction, TryAsync}"/> asynchronously, 
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
            (func, transaction) => () =>
                Try(() => transaction.Connection.CreateCommand().Tap(cmd => cmd.Transaction = transaction))
                    .Bind(cmd => Try(() => Use(cmd, func))).Try();

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
            (func, transaction) => () =>
                Try(() => transaction.Connection.CreateCommand().Tap(cmd => cmd.Transaction = transaction))
                    .Bind(cmd => TryAsync(() => Use(cmd, func))).TryAsync();

        /// <summary>
        /// Create a meaningful <see cref="string"/> representation of an <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="callerName"></param>
        /// <returns><see cref="string"/></returns>
        public static string ToLogString(this IDbCommand self, Maybe<string> callerName) =>
            $@"{callerName} -> {{Params: [{self.Params()}]}}, {{Text: {self.CommandText}}}, {{Connection State: {
                self?.Connection?.State.ToString() ?? "Null"}}}, Transaction Isolation Level: {{{
                self?.Transaction?.IsolationLevel.ToString() ?? "Null"}}}";

        /// <summary>
        /// Gets a string representation of <see cref="IDbCommand"/>'s parameters.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string Params(this IDbCommand command) =>
            Possible(command.Parameters).Match(
                some: prams => 
                    string.Join(", ", Possible(prams.OfType<object>()).Match(
                        some: objects => objects.Aggregate(
                            string.Empty,
                            (prev, next) => map(Possible((IDataParameter)next), posParam => posParam.Match(
                                some: param => prev += $"Name: {param.ParameterName} Value: {param.Value}",
                                none: () => prev += $"Could not parse Parameter of Type: {next.GetType()}"))),
                        none: () => string.Empty)),
                none: () => None.ToString());
    }
}