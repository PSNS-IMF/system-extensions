using System.Diagnostics;
using NUnit.Framework;
using Psns.Common.SystemExtensions.Diagnostics;

namespace SystemExtensions.UnitTests
{
    [TestFixture]
    public class ProcessStateTests
    {
        [Test]
        public void EmptyProcessStates_AreEqual()
        {
            Assert.AreEqual(ProcessState.Empty, ProcessState.Empty);
        }

        [Test]
        public void SameId_AreEqual()
        {
            Assert.AreEqual(new ProcessState(new Process(), "Id"), new ProcessState(new Process(), "Id"));
        }

        [Test]
        public void DifferentIds_AreNotEqual()
        {
            Assert.AreNotEqual(new ProcessState(new Process(), "Other"), new ProcessState(new Process(), "Id"));
        }
    }
}