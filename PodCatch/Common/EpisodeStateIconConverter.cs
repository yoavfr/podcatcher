using Podcatch.Common.StateMachine;
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

            IState<Episode, EpisodeEvent> state = value as IState<Episode, EpisodeEvent>;
            if (state != null)
            {
                if (state is EpisodeStatePendingDownload) return new SymbolIcon(Symbol.Download);
                if (state is EpisodeStateDownloading) return new SymbolIcon(Symbol.MoveToFolder);
                if (state is EpisodeStateDownloaded) return new SymbolIcon(Symbol.Play);
                if (state is EpisodeStatePlaying) return new SymbolIcon(Symbol.Pause);
                if (state is EpisodeStateScanning) return new SymbolIcon(Symbol.Find);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
