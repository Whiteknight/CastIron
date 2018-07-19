using System;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    /// <summary>
    /// A promise-like result from a single statement in a batch. Returns a result after the statement has
    /// been executed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlResultPromise<T>
    {
        bool IsComplete { get; }
        T GetValue();
        WaitHandle WaitHandle { get; }
        Task<T> AsTask(TimeSpan timeout);
    }

    /// <summary>
    /// A promise-like result from a single statement in a batch. Indicates when the statement has been 
    /// executed
    /// </summary>
    public interface ISqlResultPromise
    {
        bool IsComplete { get; }
        WaitHandle WaitHandle { get; }
        Task AsTask(TimeSpan timeout);
    }
}