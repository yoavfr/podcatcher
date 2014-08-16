using System;
using System.Collections.Generic;
using System.Text;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// An event driven state machine. The state machine holds a single state at any given time, and guarantees
    /// that events posted to the state machine are processed in order and synchronously:
    ///  - Events of high (low numeric value) priority are processed before events of low priority
    ///  - Events of the same priority are processed in the order in which they were posted
    ///  - Events are processed only when the state has stabilized - i.e. after the state has been entered and OnEnter has been invoked
    /// </summary>
    public interface IStateMachine<O, E> : IEventProcessor<O, E>
    {
        /// <summary>
        /// Place the state machine in it's inital state, and optionally perform it's OnEnter method. 
        /// When recovering from persistency you may want to resume the state without performing it's
        /// OnEnter method.
        /// </summary>
        /// <param name="initialState">The initial state of the state machine</param>
        /// <param name="enter"></param>
        void InitState(IState<O, E> initialState, bool enter);
        
        
        /// <summary>
        /// Check the state of the state machine
        /// </summary>
        /// <param name="state">the type of state to check against</param>
        /// <returns>true if the current state of the state machine matches the state that was provided as a paramter</returns>
        bool ValidateStateType(Type stateType);
        
        /// <summary>
        /// Get the current state of the state machine
        /// </summary>
        /// <returns>current state of the state machine</returns>
        IState<O, E> State { get; }

        /// <summary>
        /// Stops delivering posted events to the state machine
        /// </summary>
        void StopPumpEvents();
        
        /// <summary>
        /// Starts delivering posted events to the state machine
        /// </summary>
        void StartPumpEvents();
    }
}
