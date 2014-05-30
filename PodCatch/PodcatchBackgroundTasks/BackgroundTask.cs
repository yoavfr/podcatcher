using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace PodCatch.BackgroundTasks
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        CancellationTokenSource m_cancellationTokenSouce = new CancellationTokenSource();
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskInstanceCanceled;
            await PodcastDataSource.Instance.Load();
            List<Task> pendingDownloads = new List<Task>();
            /* TODO
            PodcastGroup favorites = PodcastDataSource.Instance.GetGroup(Constants.FavoritesGroupId);
            foreach (Podcast podcast in favorites.Podcasts)
            {
                foreach (Episode episode in podcast.Episodes)
                {
                    if (episode.State == EpisodeState.PendingDownload)
                    {
                        pendingDownloads.Add(episode.Download());
                    }
                }
            }*/
            
            Task.WaitAll(pendingDownloads.ToArray(), m_cancellationTokenSouce.Token);
            deferral.Complete();
        }

        void OnTaskInstanceCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            m_cancellationTokenSouce.Cancel();
        }
    }
}
