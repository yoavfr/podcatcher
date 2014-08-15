using System;

namespace Podcatch.StateMachine
{
    /// <summary>
    /// Test results
    /// </summary>
    public enum TestResult
    {
        None = 0,
        Pass = 1,
        Fail = 2,
        Blocked = 3,
        Warning = 4, 
        Skipped = 5
    }

    /// <summary>
    /// Basic simplified interface
    /// </summary>
    public interface IBasicLogger
    {
        void LogInfo(string msg, params object[] args);
        void LogWarning(string msg, params object[] args);
        void LogError(string msg, params object[] args);
    }

    /// <summary>
    /// Generic interface for all test loggers
    /// </summary>
    public interface ITestLogger : IDisposable, IBasicLogger
    {
        void Init( string initData);

        void StartTest( string testName);
        void EndTest( TestResult result);

        void LogVerbose( string msg, params object[] args);
    }
}
