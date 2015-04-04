using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PodCatch.Common
{
    public class ThreadManager
    {
        public static Task DispatchOnUIthread(Action action)
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

        public static Task<T> RunInBackground<T>(Func<Task<T>> asyncAction)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            if (!dispatcher.HasThreadAccess)
            {
                return asyncAction();
            }
            else
            {
                return Task.Run(asyncAction);
            }
        }
    }
}