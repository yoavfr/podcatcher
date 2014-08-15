using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Podcatch.StateMachine;
using System.Threading.Tasks;

namespace PodCatch.StateMachine.Test
{
    [TestClass]
    public class UnitTest1
    {
        private int m_CountA = 0;
        private int m_CountB = 0;

        [TestMethod]
        public async Task TestMethod1()
        {
            TestLogger logger = new TestLogger();
            SimpleStateMachine<UnitTest1> stateMachine = new SimpleStateMachine<UnitTest1>(logger, this, 5);
            TestStateFactory stateFactory = new TestStateFactory();
            stateMachine.InitState(stateFactory.GetState(typeof(StateA)), true);
            stateMachine.StartPumpEvents();
            stateMachine.PostEvent("event1", 0);
            IState<UnitTest1> lastState = await stateMachine.PostEvent("event2", 0);

            Assert.AreEqual(lastState, stateFactory.GetState(typeof(StateA)));
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
