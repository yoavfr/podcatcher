using System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PodCatch.Common
{
    public class EpisodePlayedStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool played = (bool)value;
            string kind = parameter as string;
            Style style = null;
            if ("AppBarButton" == kind)
            {
                style = new Style(typeof(AppBarButton));
                if (played)
                {
                    style.Setters.Add(new Setter(AppBarButton.ForegroundProperty, new SolidColorBrush(Colors.Gray)));
                }
            }
            else if ("Text" == kind)
            {
                style = (Style)Application.Current.Resources["ListViewItemTextBlockStyle"];
                if (played)
                {
                    Style playedStyle = new Style(typeof(TextBlock));
                    playedStyle.BasedOn = style.BasedOn;
                    foreach (var setter in style.Setters)
                    {
                        playedStyle.Setters.Add(setter);
                    }
                    playedStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Gray)));
                    style = playedStyle;
                }
            }
            return style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}