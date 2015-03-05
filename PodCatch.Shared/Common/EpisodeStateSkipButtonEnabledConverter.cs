using Podcatch.Common.StateMachine;
using PodCatch.DataModel;
using System;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateSkipButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IState<Episode, EpisodeEvent> state = value as IState<Episode, EpisodeEvent>;
            if (state != null)
            {
                if (state is EpisodeStateDownloaded || state is EpisodeStatePlaying)
                {
                    return true;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}