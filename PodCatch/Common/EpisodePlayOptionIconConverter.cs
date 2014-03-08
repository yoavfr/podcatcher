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
    public class EpisodePlayOptionIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(IconElement))
                throw new InvalidOperationException("The target must be an IconElement");

            if (value != null && value is EpisodePlayOption)
            {
                switch ((EpisodePlayOption)value)
                {
                    case EpisodePlayOption.Download:
                        return new SymbolIcon(Symbol.Download);
                    case EpisodePlayOption.Play:
                        return new SymbolIcon(Symbol.Play);
                    case EpisodePlayOption.Pause:
                        return new SymbolIcon(Symbol.Pause);
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
