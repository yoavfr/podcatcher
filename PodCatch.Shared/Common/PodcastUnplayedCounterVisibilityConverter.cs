using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class PodcastUnplayedCounterVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is int)
            {
                int numUnplayedEpisodes = (int)value;
                if (numUnplayedEpisodes > 0)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}