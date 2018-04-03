using Moq;
using NUnit.Framework;
using Psns.Common.Functional;
using System;
using System.Data;
using System.Threading.Tasks;
using static NUnit.StaticExpect.Expectations;
using static Psns.Common.Functional.Prelude;
using static Psns.Common.SystemExtensions.Database.Prelude;

namespace SystemExtensions.UnitTests.Database
{
    [TestFixture]
    public class DatabaseTests
    {
        [Test]
        public void Composing_Funcs_ReturnsResult()
        {
            var mockConnection = new Mock<IDbConnection>();
            var mockTransaction = new Mock<IDbTransaction>();
            var mockCommand = new Mock<IDbCommand>();
            var mockConnectionFactory = new Mock<Func<IDbConnection>>();
            mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
            mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
            mockTransaction.Setup(t => t.Connection).Returns(mockConnection.Object);
            mockConnectionFactory.Setup(f => f()).Returns(mockConnection.Object);

            var withCommand = fun((IDbCommand cmd) => "result");
            var createCommand = CreateCommand<string>().Par(withCommand);
            var beginTransaction = BeginTransaction<string>().Par(createCommand);
            var connect = Connect<string>().Par(beginTransaction).Compose(() => mockConnectionFactory.Object);

            Expect(connect().Match(r => r, e => e.Message), EqualTo("result"));

            mockConnection.Verify(c => c.Dispose());
            mockConnection.Verify(c => c.BeginTransaction());
            mockConnection.Verify(c => c.CreateCommand());
            mockTransaction.Verify(c => c.Dispose());
            mockTransaction.Verify(t => t.Connection);
            mockCommand.VerifySet(c => c.Transaction = mockTransaction.Object);
            mockCommand.Verify(c => c.Dispose());
        }

        [Test]
        public async Task Composing_AsyncFuncs_ReturnsResult()
        {
            var mockConnection = new Mock<IDbConnection>();
            var mockTransaction = new Mock<IDbTransaction>();
            var mockCommand = new Mock<IDbCommand>();
            var mockConnectionFactory = new Mock<Func<IDbConnection>>();
            mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);
            mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
            mockTransaction.Setup(t => t.Connection).Returns(mockConnection.Object);
            mockConnectionFactory.Setup(f => f()).Returns(mockConnection.Object);

            var withCommand = fun((IDbCommand cmd) => "result".AsTask());
            var createCommand = CreateCommandAsync<string>().Par(withCommand);
            var beginTransaction = BeginTransactionAsync<string>().Par(createCommand);
            var connect = ConnectAsync<string>()
                .Par(beginTransaction, conn => conn.AsTask())
                .Compose(() => mockConnectionFactory.Object);

            Expect(await connect().Match(r => r, e => e.Message), EqualTo("result"));

            mockConnection.Verify(c => c.Dispose());
            mockConnection.Verify(c => c.BeginTransaction());
            mockConnection.Verify(c => c.CreateCommand());
            mockTransaction.Verify(c => c.Dispose());
            mockTransaction.Verify(t => t.Connection);
            mockCommand.VerifySet(c => c.Transaction = mockTransaction.Object);
            mockCommand.Verify(c => c.Dispose());
        }
    }
}