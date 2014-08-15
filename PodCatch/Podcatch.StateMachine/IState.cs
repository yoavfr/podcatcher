using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// A state defines the behavior of it's owner object to events.
    /// </summary>
    public interface IState<T>
    {
        /// <summary>
        /// OnEntry actions are performed when the state is entered, or transitioned to
        /// </summary>
        /// <param name="owner">The owner of the state. i.e. if this state is defined within the context of a Car object - Car is the owner of the state</param>
        /// <param name="from">The state that preceded this one. Null if this is the entry point of the state machine</param>
        Task OnEntry(T owner, IState<T> fromState, IEventProcessor<T> stateMachine);

        /// <summary>
        /// OnExit actions are performed when the state is exited
        /// </summary>
        /// <param name="owner">The owner of the state. i.e. if this state is defined within the context of a Car object - Car is the owner of the state</param>
        /// <param name="to">The state that is being transitioned to</param>
        Task OnExit(T owner, IState<T> toState, IEventProcessor<T> stateMachine);

        /// <summary>
        /// Called with events that should be handled by the state. OnEvent will normally contain some kind of switch case to handle the different kinds of events that 
        /// may be handled, then call the appropriate methods on it's owner, and return the state to transition to, or Null if no transition takes place
        /// </summary>
        /// <param name="owner">The owner of the state. i.e. if this state is defined within the context of a Car object - Car is the owner of the state</param>
        /// <param name="anEvent">The event to handle</param>
        /// <returns>The state to transition to as a result of this event, or null to remain in the current state</returns>
        Task<IState<T>> OnEvent(T owner, Object anEvent, IEventProcessor<T> stateMachine);
    }
}
