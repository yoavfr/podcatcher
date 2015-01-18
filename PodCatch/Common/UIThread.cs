using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;

namespace PodCatch.Common
{
    public class UIThread
    {
        public static Task Dispatch(Action action)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                action();
                return VoidTask.Completed;
            }
            return dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                action();
            }).AsTask();
        }

        public static Task RunInBackground(Action action)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            if (!dispatcher.HasThreadAccess)
            {
                action();
                return VoidTask.Completed;
            }
            else
            {
                return Task.Run(action);
            }
        }
    }
}
