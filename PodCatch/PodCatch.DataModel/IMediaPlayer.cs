using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public interface IMediaPlayer
    {
        string NowPlaying { get; }

        string EndedMediaId { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; set; }
        Task Play(string path, TimeSpan position, string mediaId);
        void Pause();
        bool IsMediaPlaying(string mediaId);
        void SkipForward();
        void SkipBackward();

        event MediaPlayerStateChangedHandler MediaPlayerStateChanged;
    }

    public delegate void MediaPlayerStateChangedHandler(MediaPlayerEvent eventType, object parameter);

    public enum MediaPlayerEvent
    {
        Play,
        Pause,
        Ended,
        SwappedOut,
        Tick
    }
}
