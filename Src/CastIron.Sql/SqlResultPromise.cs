using System;

namespace CastIron.Sql
{
    public class SqlResultPromise : ISqlResultPromise
    {
        public bool IsComplete { get; set; }

        // TODO: ToTask() convert this promise to a Task which can be awaited?
        // TODO: Thread safety?
    }

    public class SqlResultPromise<T> : ISqlResultPromise<T>
    {
        private T _result;

        public bool IsComplete { get; private set; }

        public T GetValue()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Result is not ready to be read. Maybe the statement has not been executed?");
            return _result;
        }

        // TODO: ToTask() convert this promise to a Task which can be awaited?
        // TODO: Thread safety?

        public void SetValue(T value)
        {
            _result = value;
            IsComplete = true;
        }
    }
}