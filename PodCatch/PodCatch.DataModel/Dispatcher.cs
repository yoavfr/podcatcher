using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace PodCatch.DataModel
{
    public static class Dispatcher
    {
        private static CoreDispatcher s_Instance;
        public static CoreDispatcher Instance
        {
            get
            {
                return s_Instance;
            }
            set
            {
                s_Instance = value;
            }
        }
    }
}
