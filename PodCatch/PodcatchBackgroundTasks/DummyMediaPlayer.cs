using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.BackgroundTasks
{
    internal class DummyMediaPlayer : IMediaPlayer
    {
        public string NowPlaying
        {
            get { return null; }
        }

        public string EndedMediaId
        {
            get { return null; }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromTicks(0); }
        }

        public TimeSpan Position
        {
            get
            {
                return TimeSpan.FromTicks(0);
            }
            set
            {
            }
        }

        public Task Play(string path, TimeSpan position, string mediaId)
        {
            return Task.FromResult<object>(null);
        }

        public void Pause()
        {
        }

        public bool IsMediaPlaying(string mediaId)
        {
            return false;
        }

        public void SkipForward()
        {
        }

        public void SkipBackward()
        {
        }

        public event MediaPlayerStateChangedHandler MediaPlayerStateChanged;
    }
}
