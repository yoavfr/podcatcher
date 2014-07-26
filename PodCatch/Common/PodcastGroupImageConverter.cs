using PodCatch.DataModel;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace PodCatch.Common
{
    public class PodcastGroupImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(ImageSource))
                throw new InvalidOperationException("The target must be a string");

            if (value != null)
            {
                string id = value as string;
                if (id != null)
                {
                    switch (id)
                    {
                        case (Constants.FavoritesGroupId):
                            return "ms-appx:///Assets/Favorites.png";
                        case (Constants.SearchGroupId):
                            return "ms-appx:///Assets/Search.png";
                    }
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
