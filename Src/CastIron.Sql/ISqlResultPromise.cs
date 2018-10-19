using System;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    /// <summary>
    /// A promise-like result from a single statement in a batch. Returns a result after the statement has
    /// been executed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlResultPromise<T> : IDisposable
    {
        bool IsComplete { get; }
        T GetValue();
        Task<T> AsTask(TimeSpan timeout);
        bool WaitForComplete();
        bool WaitForComplete(TimeSpan wait);
        bool WaitForComplete(int waitMs);
    }

    /// <summary>
    /// A promise-like result from a single statement in a batch. Indicates when the statement has been 
    /// executed
    /// </summary>
    public interface ISqlResultPromise : IDisposable
    {
        bool IsComplete { get; }
        Task AsTask(TimeSpan timeout);
        bool WaitForComplete();
        bool WaitForComplete(TimeSpan wait);
        bool WaitForComplete(int waitMs);
    }
}