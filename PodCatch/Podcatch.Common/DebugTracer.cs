using System;
using System.Diagnostics;

namespace PodCatch.Common
{
    public class DebugTracer : ITracer
    {
        public override void TraceError(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("Error {1}: {2}", DateTime.UtcNow, format), args);
        }

        public override void TraceWarning(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("Warning {0}: {1}", DateTime.UtcNow, format), args);
        }

        public override void TraceInformation(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("Information {0}: {1}", DateTime.UtcNow, format), args);
        }

        public override void TraceVerbose(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("Verbose {0}: {1}", DateTime.UtcNow, format), args);
        }

        public override TracingLevel TracingLevel
        {
            get { return TracingLevel.Verbose; }
        }
    }
}
