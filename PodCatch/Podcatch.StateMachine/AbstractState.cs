using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// Abstract state. A state can access it's factory in order to return states to transition to from OnEvent
    /// </summary>
    public abstract class AbstractState<T> : IState<T>
    {
        private IStateFactory<T> m_StateFactory;

        /// <summary>
        /// The state factory that created this state, and can create all the other states that form the state machine
        /// </summary>
        public IStateFactory<T> Factory
        {
            get { return m_StateFactory; }
            set { m_StateFactory = value; }
        }

        public abstract Task OnEntry(T owner, IState<T> fromState, IEventProcessor<T> stateMachine);
        public abstract Task OnExit(T owner, IState<T> toState, IEventProcessor<T> stateMachine);
        public abstract Task<IState<T>> OnEvent(T owner, Object anEvent, IEventProcessor<T> stateMachine);
    }
}
