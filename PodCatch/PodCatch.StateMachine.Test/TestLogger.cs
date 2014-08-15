using Podcatch.StateMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.StateMachine.Test
{
    class TestLogger : IBasicLogger
    {
        public void LogInfo(string msg, params object[] args)
        {
            Debug.WriteLine("Info : {0}", String.Format(msg, args));
        }

        public void LogWarning(string msg, params object[] args)
        {
            Debug.WriteLine("Warning : {0}", String.Format(msg, args));
        }

        public void LogError(string msg, params object[] args)
        {
            Debug.WriteLine("Error : {0}", String.Format(msg, args));
        }
    }
}
