namespace PodCatch.Common.Test
{
    public interface ITestService
    {
        IServiceContext foo();
    }

    public class TestService : ServiceConsumer, ITestService
    {
        public TestService(IServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        public IServiceContext foo()
        {
            return ServiceContext;
        }
    }
}