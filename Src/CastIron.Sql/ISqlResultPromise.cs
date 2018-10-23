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
        /// <summary>
        /// True when the interaction has been executed and the result has been returned. False otherwise
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Get the result value. Throws an exception if the value is not ready yet. Use IsComplete or
        /// WaitForComplete() to determine when the value is available to be read safely.
        /// </summary>
        /// <returns></returns>
        T GetValue();

        /// <summary>
        /// Convert this promise into a Task which can be awaited
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<T> AsTask(TimeSpan timeout);

        /// <summary>
        /// Wait for the result
        /// </summary>
        /// <returns></returns>
        bool WaitForComplete();

        /// <summary>
        /// Wait for the result up to the given timeout. Returns true if the result is available, false if
        /// the timespan elapsed.
        /// </summary>
        /// <param name="wait"></param>
        /// <returns></returns>
        bool WaitForComplete(TimeSpan wait);

        /// <summary>
        /// Wait for the result up to the given timeout, in milliseconds. Returns true if the result is 
        /// available, false if the timespan elapsed.
        /// </summary>
        /// <param name="waitMs"></param>
        /// <returns></returns>
        bool WaitForComplete(int waitMs);
    }

    /// <summary>
    /// A promise-like result from a single statement in a batch. Indicates when the statement has been 
    /// executed
    /// </summary>
    public interface ISqlResultPromise : IDisposable
    {
        /// <summary>
        /// Returns true if the statement has been executed, false otherwise
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Convert this promise into a Task which can be awaited
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task AsTask(TimeSpan timeout);

        /// <summary>
        /// Wait for the statement to be executed
        /// </summary>
        /// <returns></returns>
        bool WaitForComplete();

        /// <summary>
        /// Wait for the statement to be executed up to the given timeout. Returns true if the statement is
        /// executed before the timeout, false if the timeout elapsed
        /// </summary>
        /// <param name="wait"></param>
        /// <returns></returns>
        bool WaitForComplete(TimeSpan wait);

        /// <summary>
        /// Wait for the statement to be executed up to the given timeout in milliseconds. Returns true if 
        /// the statement is executed before the timeout, false if the timeout elapsed
        /// </summary>
        /// <param name="waitMs"></param>
        /// <returns></returns>
        bool WaitForComplete(int waitMs);
    }
}