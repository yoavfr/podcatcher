using Podcatch.Common.StateMachine;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PodCatch.StateMachine.Test
{
    internal class TestStateFactory : AbstractStateFactory<UnitTest1, TestEvent>
    {
        public TestStateFactory()
            : base(new AbstractState<UnitTest1, TestEvent>[] { new StateA(), new StateB() })
        {
        }
    }

    internal class StateA : AbstractState<UnitTest1, TestEvent>
    {
        public override async Task OnEntry(UnitTest1 owner, IState<UnitTest1, TestEvent> fromState, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            Debug.WriteLine("Entering A");
            owner.incrementA();
        }

        public override async Task OnExit(UnitTest1 owner, IState<UnitTest1, TestEvent> toState, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            Debug.WriteLine("Leaving A");
        }

        public override async Task<IState<UnitTest1, TestEvent>> OnEvent(UnitTest1 owner, TestEvent anEvent, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            return Factory.GetState<StateB>();
        }
    }

    internal class StateB : AbstractState<UnitTest1, TestEvent>
    {
        public override async Task OnEntry(UnitTest1 owner, IState<UnitTest1, TestEvent> fromState, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            Debug.WriteLine("Entering B");
            owner.incrementB();
        }

        public override async Task OnExit(UnitTest1 owner, IState<UnitTest1, TestEvent> toState, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            Debug.WriteLine("Leaving B");
        }

        public override async Task<IState<UnitTest1, TestEvent>> OnEvent(UnitTest1 owner, TestEvent anEvent, IEventProcessor<UnitTest1, TestEvent> stateMachine)
        {
            return Factory.GetState<StateA>();
        }
    }

    internal enum TestEvent
    {
        A, B
    }
}