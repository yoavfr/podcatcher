using System;
using System.Collections.Generic;
using System.Text;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// A factory for states by type
    /// </summary>
    public interface IStateFactory<O, E>
    {
        /// <summary>
        /// Get an instance of a state
        /// </summary>
        /// <param name="stateType">the type of the state</param>
        /// <returns>The desired state</returns>
        /// <throws>NoSuchElementException if the factory does not know how to produce a state of this type</throws>
        IState<O, E> GetState<S>();
    }
}
