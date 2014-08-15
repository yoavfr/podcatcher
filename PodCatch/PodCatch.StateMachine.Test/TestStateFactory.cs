using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.StateMachine.Test
{
    class TestStateFactory : AbstractStateFactory<UnitTest1> 
    {
        public TestStateFactory() : base(new AbstractState<UnitTest1>[] {new StateA(), new StateB()})
        {

        }
    }

    class StateA : AbstractState<UnitTest1>
    {
        public override async Task OnEntry(UnitTest1 owner, IState<UnitTest1> fromState, IEventProcessor<UnitTest1> stateMachine)
        {
            Debug.WriteLine("Entering A");
            owner.incrementA();
        }

        public override async Task OnExit(UnitTest1 owner, IState<UnitTest1> toState, IEventProcessor<UnitTest1> stateMachine)
        {
            Debug.WriteLine("Leaving A");
        }

        public override async Task<IState<UnitTest1>> OnEvent(UnitTest1 owner, object anEvent, IEventProcessor<UnitTest1> stateMachine)
        {
            return Factory.GetState(typeof(StateB));
        }
    }

    class StateB : AbstractState<UnitTest1>
    {
        public override async Task OnEntry(UnitTest1 owner, IState<UnitTest1> fromState, IEventProcessor<UnitTest1> stateMachine)
        {
            Debug.WriteLine("Entering B");
            owner.incrementB();
        }

        public override async Task OnExit(UnitTest1 owner, IState<UnitTest1> toState, IEventProcessor<UnitTest1> stateMachine)
        {
            Debug.WriteLine("Leaving B");
        }

        public override async Task<IState<UnitTest1>> OnEvent(UnitTest1 owner, object anEvent, IEventProcessor<UnitTest1> stateMachine)
        {
            return Factory.GetState(typeof(StateA));
        }
    }
}
