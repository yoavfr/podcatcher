using Windows.ApplicationModel.Resources;

namespace PodCatch.Resources
{
    public static class LocalizedStrings
    {
        private static ResourceLoader s_Loader = new ResourceLoader();

        public static string FavoritesTitle
        {
            get
            {
                return s_Loader.GetString("FavoritesTitle");
            }
        }

        public static string Get(string identifier)
        {
            return s_Loader.GetString(identifier);
        }
    }
}