﻿using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Podcatch.Common.StateMachine;
using PodCatch.Common;
using System.Threading.Tasks;

namespace PodCatch.StateMachine.Test
{
    [TestClass]
    public class UnitTest1
    {
        private int m_CountA = 0;
        private int m_CountB = 0;

        // Test
        [TestMethod]
        public async Task TestMethod1()
        {
            ServiceContext serviceContext = new ServiceContext(new DebugTracer());
            SimpleStateMachine<UnitTest1, TestEvent> stateMachine = new SimpleStateMachine<UnitTest1, TestEvent>(serviceContext, this, 5);
            TestStateFactory stateFactory = new TestStateFactory(serviceContext);
            stateMachine.InitState(stateFactory.GetState<StateA>(), true);
            stateMachine.StartPumpEvents();
            stateMachine.PostEvent(TestEvent.A, 0);
            IState<UnitTest1, TestEvent> lastState = await stateMachine.PostEvent(TestEvent.B, 0);

            Assert.AreEqual(lastState, stateFactory.GetState<StateA>());
            Assert.AreEqual(2, m_CountA);
            Assert.AreEqual(1, m_CountB);
        }

        public void incrementA()
        {
            m_CountA++;
        }

        public void incrementB()
        {
            m_CountB++;
        }
    }
}