using System;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    public sealed class SqlResultPromise : ISqlResultPromise
    {
        private readonly ManualResetEvent _waitHandle;
        private volatile bool _isComplete;

        public SqlResultPromise()
        {
            _waitHandle = new ManualResetEvent(false);
        }

        public bool IsComplete => _isComplete;

        public void SetComplete()
        {
            if (_isComplete)
                return;
            _isComplete = true;
            Interlocked.MemoryBarrier();
            _waitHandle.Set();
        }

        public Task AsTask(TimeSpan timeout)
        {
            var completionSource = new TaskCompletionSource<object>();
            var registration = ThreadPool.RegisterWaitForSingleObject(_waitHandle, (state, timedOut) =>
            {
                var tcs = (TaskCompletionSource<object>)state;
                if (timedOut)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(null);
            }, completionSource, timeout, true);
            completionSource.Task.ContinueWith((t, state) => registration.Unregister(null), registration, TaskScheduler.Default);
            return completionSource.Task;
        }

        public void Dispose()
        {
            _waitHandle?.Dispose();
        }

        public bool WaitForComplete() => _isComplete || _waitHandle.WaitOne();

        public bool WaitForComplete(TimeSpan wait) => _isComplete || _waitHandle.WaitOne(wait);

        public bool WaitForComplete(int waitMs) => _isComplete || _waitHandle.WaitOne(waitMs);
    }

    public sealed class SqlResultPromise<T> : ISqlResultPromise<T>
    {
        private readonly ManualResetEvent _waitHandle;
        private volatile bool _isComplete;

        private T _result;

        public SqlResultPromise()
        {
            _waitHandle = new ManualResetEvent(false);
        }

        public bool IsComplete => _isComplete;

        public T GetValue()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Result is not ready to be read. Maybe the statement has not been executed?");
            return _result;
        }

        public void SetValue(T value)
        {
            if (_isComplete)
                throw new InvalidOperationException("The value for this promise has already been set, and can only be set once");
            _result = value;
            _isComplete = true;
            Interlocked.MemoryBarrier();
            _waitHandle.Set();
        }

        public Task<T> AsTask(TimeSpan timeout)
        {
            var completionSource = new TaskCompletionSource<T>();
            var registration = ThreadPool.RegisterWaitForSingleObject(_waitHandle, (state, timedOut) =>
            {
                var tcs = (TaskCompletionSource<T>)state;
                if (timedOut)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(_result);
            }, completionSource, timeout, true);
            completionSource.Task.ContinueWith((t, state) => registration.Unregister(null), registration, TaskScheduler.Default);
            return completionSource.Task;
        }

        public void Dispose()
        {
            _waitHandle?.Dispose();
        }

        public bool WaitForComplete() => _isComplete || _waitHandle.WaitOne();

        public bool WaitForComplete(TimeSpan wait) => _isComplete || _waitHandle.WaitOne(wait);

        public bool WaitForComplete(int waitMs) => _isComplete || _waitHandle.WaitOne(waitMs);
    }
}