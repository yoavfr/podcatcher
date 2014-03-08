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
    public class EpisodePlayOptionToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is EpisodePlayOption)
            {
                switch ((EpisodePlayOption)value)
                {
                    case EpisodePlayOption.Download:
                        return "Download";
                    case EpisodePlayOption.Play:
                        return "Play";
                    case EpisodePlayOption.Pause:
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
