using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// Abstract flyweight State factory. The Factory contains a single instance of each participating state. 
    /// This allows many objects to share the same state factory and state objects without incurring proportional memory overhead.
    /// States of the factory must therefore be stateless themselves - i.e. define behavior only
    /// 
    /// A typical implementation will call the constructor with the state machine specific set of states
    /// </summary>
    public abstract class AbstractStateFactory<O, E> : IStateFactory<O, E>
    {
        Dictionary<Type, AbstractState<O, E>> m_StateByType = new Dictionary<Type, AbstractState<O, E>>();


        private AbstractStateFactory()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="states">all the states that can be delivered by this factory</param>
        protected AbstractStateFactory(AbstractState<O, E>[] states)
        {
            foreach (AbstractState<O, E> state in states)
            {
                state.Factory = this;
                m_StateByType[state.GetType()] = state;
            }
        }

        public IState<O, E> GetState<S>()
        {
            return (IState<O, E>)m_StateByType[typeof(S)];
        }
    }
}
