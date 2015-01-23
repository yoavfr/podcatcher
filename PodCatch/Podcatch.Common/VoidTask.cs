using System.Threading.Tasks;

namespace PodCatch.Common
{
    public static class VoidTask
    {
        private static Task<object> s_CompletedVoid = Task.FromResult<object>(null);

        public static Task Completed { get { return s_CompletedVoid; } }
    }
}