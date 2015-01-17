using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;

namespace PodCatch.Common
{
    public class UIThread
    {
        public static void Dispatch(Action action)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                IAsyncAction t = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    action();
                });
            }
        }

        public static Task DispatchAsync(Action action)
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
    }
}
