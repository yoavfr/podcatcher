using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodCatch.DataModel
{
    public class TouchedFiles
    {
        private static TouchedFiles s_Instance = new TouchedFiles();
        public static TouchedFiles Instance
        {
            get
            {
                return s_Instance;
            }
        }

        private HashSet<string> m_Files = new HashSet<string>();

        public void Add (string fileName)
        {
            m_Files.Add(fileName);
        }

        public void Clear()
        {
            m_Files.Clear();
        }

        public bool Contains(string fileName)
        {
            return m_Files.Contains(fileName);
        }
    }
}
