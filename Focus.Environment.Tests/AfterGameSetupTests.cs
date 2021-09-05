using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Focus.Environment.Tests
{
    public class AfterGameSetupTests
    {
        private readonly AfterGameSetup<string> afterGameSetup;
        private readonly Mock<IGameSetup> gameSetupMock;
        private readonly Lazy<string> lazy;

        public AfterGameSetupTests()
        {
            gameSetupMock = new Mock<IGameSetup>();
            lazy = new(() => "Dummy Value");
            afterGameSetup = new AfterGameSetup<string>(gameSetupMock.Object, lazy);
        }

        [Fact]
        public void WhenValueAccessedBeforeSetupConfirmed_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => afterGameSetup.Value);
        }

        [Fact]
        public void WhenValueAccessedAfterSetupConfirmed_Resolves()
        {
            gameSetupMock.SetupGet(x => x.IsConfirmed).Returns(true);

            Assert.Equal("Dummy Value", afterGameSetup.Value);
        }

        [Fact]
        public void WhenCallbacksRegistered_DoesNotInvokeBeforeNotify()
        {
            string cbResult1 = null;
            string cbResult2 = null;
            afterGameSetup.OnValue(x => cbResult1 = x + "1");
            afterGameSetup.OnValue(x => cbResult2 = x + "2");

            Assert.Null(cbResult1);
            Assert.Null(cbResult2);
        }

        [Fact]
        public void WhenCallbacksRegistered_InvokesAfterExplicitNotify()
        {
            string cbResult1 = null;
            string cbResult2 = null;
            afterGameSetup.OnValue(x => cbResult1 = x + "1");
            afterGameSetup.OnValue(x => cbResult2 = x + "2");
            afterGameSetup.NotifyValue();

            Assert.Equal("Dummy Value1", cbResult1);
            Assert.Equal("Dummy Value2", cbResult2);
        }

        [Fact]
        public void WhenNotifiedMultipleTimes_InvokesCallbacksOnlyOnce()
        {
            var cbResults = new List<string>();
            afterGameSetup.OnValue(x => cbResults.Add(x));
            afterGameSetup.NotifyValue();
            afterGameSetup.NotifyValue();
            afterGameSetup.NotifyValue();

            Assert.Collection(cbResults, x => Assert.Equal("Dummy Value", x));
        }

        [Fact]
        public void WhenCallbacksRegisteredAfterAlreadyNotified_InvokesImmediately()
        {
            string cbResult = null;
            afterGameSetup.NotifyValue();
            afterGameSetup.OnValue(x => cbResult = x);

            Assert.Equal("Dummy Value", cbResult);
        }
    }
}
