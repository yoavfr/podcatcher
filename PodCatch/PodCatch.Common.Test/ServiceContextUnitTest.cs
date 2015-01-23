using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace PodCatch.Common.Test
{
    [TestClass]
    public class ServiceContextUnitTest
    {
        [TestMethod]
        public void TestServiceContext()
        {
            ITracer tracer = new DebugTracer();
            ServiceContext serviceContext = new ServiceContext(tracer);
            serviceContext.PublishService<TestService>();

            ITestService testService = serviceContext.GetService<ITestService>();
            IServiceContext same = testService.foo();
            Assert.AreEqual(serviceContext, same);
        }
    }
}