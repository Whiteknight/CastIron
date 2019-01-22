using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Debugging;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class SqlDataReaderResults : IDataResults
    {
        private readonly IDbCommand _command;
        private readonly IExecutionContext _context;
        private readonly IDataReader _reader;

        private bool _isConsumed;
        private bool _isConsuming;
        public int CurrentSet { get; private set; }

        public SqlDataReaderResults(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected = null)
        {
            _command = command;
            _context = context;
            _reader = reader;
            _isConsumed = false;
            _isConsuming = false;
            CurrentSet = 0;
            RowsAffected = rowsAffected ?? reader?.RecordsAffected ?? 0;
        }

        public int RowsAffected { get; }

        public IDataReader AsRawReader()
        {
            AssertHasReader();
            MarkRawReaderBeingConsumed();
            return _reader;
        }

        public IDataReader AsRawReaderWithBetterErrorMessages()
        {
            AssertHasReader();
            MarkRawReaderBeingConsumed();
            return new DataReaderWithBetterErrorMessages(_reader);
        }

        public IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerBuilder<T>> setup = null)
        {
            AssertHasReader();
            MarkForInPlaceConsuming();
            if (CurrentSet == 0)
                CurrentSet = 1;
            var context = new MapCompilerBuilder<T>();
            setup?.Invoke(context);
            var map = context.Compile(_reader);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public object GetOutputParameterValue(string name)
        {
            var parameterName = (name.StartsWith("@") ? name : "@" + name).ToLowerInvariant();

            var param = _command?.Parameters.Cast<DbParameter>()
                .Where(p => p.Direction != ParameterDirection.Input)
                .FirstOrDefault(p => p.ParameterName.ToLowerInvariant() == parameterName);
            if (param == null)
                return null;

            if (param.Value == DBNull.Value)
                return null;
            return param.Value;
        }

        public T GetOutputParameter<T>(string name)
        {
            var value = GetOutputParameterValue(name);
            if (value == null)
                return default(T);
            if (typeof(T) == typeof(object))
                return (T) value;
            if (value is T asT)
                return asT;
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T) Convert.ChangeType(value, typeof(T));

            return default(T);
        }

        public T GetOutputParameterOrThrow<T>(string name)
        {
            var value = GetOutputParameterValue(name);
            if (value == null)
                return default(T);
            if (typeof(T) == typeof(object))
                return (T) value;
            if (value is T asT)
                return asT;
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T) Convert.ChangeType(value, typeof(T));

            throw new Exception($"Cannot get value '{name}'. Expected type {typeof(T).FullName} but found {value.GetType().FullName}");
        }

        public T GetOutputParameters<T>()
            where T : class, new()
        {
            // TODO: Can we introspect and get constructor parameters by name?
            var t = new T();
            if (_command == null)
                return t;

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Where(p => p.PropertyType == typeof(object) || typeof(IConvertible).IsAssignableFrom(p.PropertyType))
                .ToList();
            var getMethod = typeof(SqlDataReaderResults).GetMethod(nameof(GetOutputParameter));
            if (getMethod == null)
                throw new Exception($"Cannot find method {nameof(SqlDataReaderResults)}.{nameof(GetOutputParameter)}");
            foreach (var property in properties)
            {
                var getMethodTyped = getMethod.MakeGenericMethod(property.PropertyType);
                var value = getMethodTyped.Invoke(this, new object[] {property.Name});
                property.SetValue(t, value);
            }

            return t;
        }

        public IDataResults AdvanceToNextResultSet()
        {
            return AdvanceToResultSet(CurrentSet + 1);
        }

        public IDataResults AdvanceToResultSet(int num)
        {
            AssertHasReader();

            // TODO: Review all this logic to make sure it is sane and necessary
            if (CurrentSet > num)
                throw new Exception("Cannot read result sets out of order. At Set=" + CurrentSet + " but requested Set=" + num);
            if (CurrentSet == 0 )
            {
                if (num == 1)
                {
                    CurrentSet = num;
                    return this;
                }

                CurrentSet = 1;
            }

            while (CurrentSet < num)
            {
                CurrentSet++;
                if (!_reader.NextResult())
                    throw new Exception("Could not read result Set=" + CurrentSet + " (requested result Set=" + num + ")");
            }

            if (CurrentSet != num)
                throw new Exception("Could not find result Set=" + num);

            return this;
        }

        public bool TryAdvanceToNextResultSet()
        {
            return TryAdvanceToResultSet(CurrentSet + 1);
        }

        public bool TryAdvanceToResultSet(int num)
        {
            AssertHasReader();

            if (CurrentSet > num)
                return false;

            if (CurrentSet == 0)
            {
                if (num == 1)
                {
                    CurrentSet = num;
                    return true;
                }

                CurrentSet = 1;
            }

            while (CurrentSet < num)
            {
                CurrentSet++;
                if (!_reader.NextResult())
                    return false;
            }

            if (CurrentSet != num)
                return false;

            return true;
        }

        protected void DisposeHeldReferences()
        {
            _context?.MarkComplete();
            _reader.Dispose();
            _command?.Dispose();
            _context?.Dispose();
        }

        private void MarkRawReaderBeingConsumed()
        {
            if (_isConsumed || _isConsuming)
                throw new Exception("SqlDataReader cannot be consumed more than once. Have you accessed the reader already or have you started reading values from it already?");
            _isConsumed = true;
        }

        private void MarkForInPlaceConsuming()
        {
            if (_isConsumed)
                throw new Exception("SqlDataReader is being consumed and cannot be accessed again.");
            _isConsuming = true;
        }


        private void AssertHasReader()
        {
            if (_reader == null)
                throw new Exception($"This result does not contain a data reader. Are you executing an {nameof(ISqlCommand)} variant?");
        }
    }

    public class SqlDataReaderResultsStream : SqlDataReaderResults, IDataResultsStream
    {
        public SqlDataReaderResultsStream(IDbCommand command, IExecutionContext context, IDataReader reader)
            : base(command, context, reader)
        {
        }

        // TODO: .AsRawReader() will return the reader which the user can .Dispose(), but not dispose the connection or command

        public void Dispose()
        {
            DisposeHeldReferences();
        }
    }
}