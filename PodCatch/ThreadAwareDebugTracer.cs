using PodCatch.Common;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PodCatch
{
    public class ThreadAwareDebugTracer : ITracer
    {
        public override void TraceError(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("{0}Error {1}: {2}", ThreadId, DateTime.UtcNow, format), args);
        }

        public override void TraceWarning(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("{0}Warning {1}: {2}", ThreadId, DateTime.UtcNow, format), args);
        }

        public override void TraceInformation(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("{0}Information {1}: {2}", ThreadId, DateTime.UtcNow, format), args);
        }

        public override void TraceVerbose(string format, params object[] args)
        {
            Debug.WriteLine(String.Format("{0}Verbose {1}: {2}", ThreadId, DateTime.UtcNow, format), args);
        }

        public override TracingLevel TracingLevel
        {
            get { return TracingLevel.Verbose; }
        }

        private string ThreadId
        {
            get
            {
                CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

                if (dispatcher.HasThreadAccess) return "UIThread: ";
                return string.Empty;
            }
        }
    }
}