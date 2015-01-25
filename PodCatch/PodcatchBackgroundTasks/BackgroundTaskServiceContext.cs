using PodCatch.Common;
using PodCatch.DataModel;
using PodCatch.DataModel.Search;

namespace PodCatch.BackgroundTasks
{
    sealed public class BackgroundTaskServiceContext
    {
        private static IServiceContext s_Instance;

        internal static IServiceContext Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    ServiceContext serviceContext = new ServiceContext(new DebugTracer());
                    serviceContext.PublishService<PodcastDataSource>();
                    serviceContext.PublishService<DownloadService>();
                    serviceContext.PublishService<ITunesSearch>();
                    s_Instance = serviceContext;
                }
                return s_Instance;
            }
            set
            {
                s_Instance = value;
            }
        }
    }
}