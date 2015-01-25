using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PodCatch.Common.Collections;

namespace PodCatch.Common.Test
{
    [TestClass]
    public class ObservableconcurrentCollectionUnitTests
    {
        [TestMethod]
        public void TestConcurrentObservableCollectionOrdering()
        {
            ConcurrentObservableCollection<string> collection = new ConcurrentObservableCollection<string>(s => int.Parse(s));

            for (int i=0; i<20; i++)
            {
                collection.Add(i.ToString());
            }

            int j = 0;
            foreach (string s in collection)
            {
                Assert.AreEqual(j.ToString(), s);
                j++;
            }
        }
    }
}
