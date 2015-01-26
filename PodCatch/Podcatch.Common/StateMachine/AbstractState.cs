using PodCatch.Common;
using System.Threading.Tasks;

namespace Podcatch.Common.StateMachine
{
    /// <summary>
    /// Abstract state. A state can access it's factory in order to return states to transition to from OnEvent
    /// </summary>
    public abstract class AbstractState<O, E> : ServiceConsumer, IState<O, E>
    {

        /// <summary>
        /// The state factory that created this state, and can create all the other states that form the state machine
        /// </summary>
        private IStateFactory<O, E> m_StateFactory;

        public AbstractState(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }
        public IStateFactory<O, E> Factory
        {
            get { return m_StateFactory; }
            set { m_StateFactory = value; }
        }

        public IState<O,E> GetState<T>()
        {
            return m_StateFactory.GetState<T>();
        }

        public abstract Task OnEntry(O owner, IState<O, E> fromState, IEventProcessor<O, E> stateMachine);

        public abstract Task OnExit(O owner, IState<O, E> toState, IEventProcessor<O, E> stateMachine);

        public abstract Task<IState<O, E>> OnEvent(O owner, E anEvent, IEventProcessor<O, E> stateMachine);
    }
}