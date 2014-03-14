﻿using PodCatch.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PodCatch.Common
{
    public class EpisodeStateToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is EpisodeState)
            {
                switch ((EpisodeState)value)
                {
                    case EpisodeState.PendingDownload:
                        return "Download";
                    case EpisodeState.Downloaded:
                        return "Play";
                    case EpisodeState.Playing:
                        return "Pause";
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