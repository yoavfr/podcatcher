using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(IconElement))
                throw new InvalidOperationException("The target must be an IconElement");

            if (value != null && value is EpisodeState)
            {
                switch ((EpisodeState)value)
                {
                    case EpisodeState.PendingDownload:
                        return new SymbolIcon(Symbol.Download);
                    case EpisodeState.Downloading:
                        return new SymbolIcon(Symbol.MoveToFolder);
                    case EpisodeState.Downloaded:
                        return new SymbolIcon(Symbol.Play);
                    case EpisodeState.Playing:
                        return new SymbolIcon(Symbol.Pause);
                    case EpisodeState.Scanning:
                        return new SymbolIcon(Symbol.Find);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
