using PodCatch.DataModel;
using System;
using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel.Background;

namespace PodCatch.BackgroundTasks
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        private CancellationTokenSource m_cancellationTokenSouce = new CancellationTokenSource();

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try
            {
                taskInstance.Canceled += OnTaskInstanceCanceled;

                DoHouseCleaning();
            }
            catch (Exception e)
            {
                Debug.WriteLine("BackgroundTask.Run() - Error {0}", e);
            }
            finally
            {
                deferral.Complete();
            }
        }

        public async void DoHouseCleaning()
        {
            try
            {
                IPodcastDataSource podcastDataSource = BackgroundTaskServiceContext.Instance.GetService<PodcastDataSource>();
                await podcastDataSource.DoHouseKeeping();
            }
            catch (Exception e)
            {
                Debug.WriteLine("BackgroundTask.Run() - Error {0}", e);
            }
            //List<Task> pendingDownloads = new List<Task>();
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

            /*var tileContent = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText01);
            var tileLines = tileContent.SelectNodes("tile/visual/binding/text");
            tileLines[0].InnerText = "test";
            TileNotification notification = new TileNotification(tileContent);
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.Update(notification);

            Task.WaitAll(pendingDownloads.ToArray(), m_cancellationTokenSouce.Token);*/
        }

        private void OnTaskInstanceCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            m_cancellationTokenSouce.Cancel();
        }
    }
}