namespace PodCatch.DataModel
{
    public enum EpisodeEvent
    {
        UpdateDownloadStatus,
        Download,
        DownloadSuccess,
        DownloadFail,
        Play,
        PlayStarted,
        Pause,
        Paused,
        Scan,
        ScanDone,
        DonePlaying,
        Refresh,
    }
}