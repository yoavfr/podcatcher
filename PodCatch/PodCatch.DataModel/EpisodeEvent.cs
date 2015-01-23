namespace PodCatch.DataModel
{
    public enum EpisodeEvent
    {
        UpdateDownloadStatus,
        Download,
        DownloadSuccess,
        DownloadFail,
        Play,
        Pause,
        Scan,
        DonePlaying,
        Refresh,
    }
}