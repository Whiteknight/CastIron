using System;
using System.Threading;
using System.Threading.Tasks;

namespace CastIron.Sql
{
    public class SqlResultPromise : ISqlResultPromise
    {
        private readonly ManualResetEvent _waitHandle;

        public SqlResultPromise()
        {
            _waitHandle = new ManualResetEvent(false);
        }

        public bool IsComplete { get; set; }

        public WaitHandle WaitHandle => _waitHandle;

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
    }

    public class SqlResultPromise<T> : ISqlResultPromise<T>
    {
        private readonly ManualResetEvent _waitHandle;

        private T _result;


        public SqlResultPromise()
        {
            _waitHandle = new ManualResetEvent(false);
        }

        public WaitHandle WaitHandle => _waitHandle;

        public bool IsComplete { get; private set; }

        public T GetValue()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Result is not ready to be read. Maybe the statement has not been executed?");
            return _result;
        }

        public void SetValue(T value)
        {
            _result = value;
            IsComplete = true;
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
    }
}