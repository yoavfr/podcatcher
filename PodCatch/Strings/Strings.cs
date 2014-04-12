using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace PodCatch.Strings
{
    public static class LocalizedStrings
    {
        private static ResourceLoader s_Loader = new ResourceLoader();

        public static string FavoritesPodcastGroupName
        {
            get 
            {
                return s_Loader.GetString("FavoritesPodcastGroupName");
            }
        }
    }
}
