using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PodCatch
{
    public class MediaElementWrapper
    {
        private static MediaElementWrapper s_Insatnce;
        private TimeSpan m_Position;
        private MediaElement MediaElement { get; set; }
        public TimeSpan Position {
            get
            {
                return MediaElement.Position;
            }
            set
            {
                m_Position = value;
                MediaElement.Position = value;
            }
        }
        public Uri Source 
        {
            get
            {
                return MediaElement.Source;
            }
            set
            {
                MediaElement.Source = value;
            }
        }
        public void Play()
        {
            MediaElement.Play();
        }

        public void Pause()
        {
            MediaElement.Pause();
        }

        public static MediaElementWrapper Instance
        {
            get
            {
                if (s_Insatnce == null)
                {
                    DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
                    MediaElement mediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);
                    mediaElement.AutoPlay = true;
                    s_Insatnce = new MediaElementWrapper(mediaElement);
                }
                return s_Insatnce;
            }
        }

        private MediaElementWrapper(MediaElement mediaElement)
        {
            MediaElement = mediaElement;
            MediaElement.MediaOpened += MediaElement_MediaOpened;
        }

        void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = m_Position;
        }
    }
}
