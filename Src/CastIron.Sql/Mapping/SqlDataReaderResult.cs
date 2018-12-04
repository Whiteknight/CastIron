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
    public class SqlDataReaderResult : IDataResults
    {
        private readonly IDbCommand _command;
        private readonly IExecutionContext _context;
        private readonly IDataReader _reader;

        private bool _isConsumed;
        private bool _isConsuming;
        public int CurrentSet { get; private set; }

        public SqlDataReaderResult(IDbCommand command, IExecutionContext context, IDataReader reader)
        {
            _command = command;
            _context = context;
            _reader = reader;
            _isConsumed = false;
            _isConsuming = false;
            CurrentSet = 0;
        }

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

        public IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            AssertHasReader();
            MarkForInPlaceConsuming();
            if (CurrentSet == 0)
                CurrentSet = 1;
            map = map ?? CachingMappingCompiler.GetDefaultInstance().CompileExpression<T>(_reader);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public IEnumerable<T> AsEnumerable<T>(IRecordMapperCompiler compiler, Func<T> factory = null, ConstructorInfo preferredConstructor = null)
        {
            AssertHasReader();
            MarkForInPlaceConsuming();
            if (CurrentSet == 0)
                CurrentSet = 1;
            var map = (compiler ?? CachingMappingCompiler.GetDefaultInstance()).CompileExpression(typeof(T), _reader, factory, preferredConstructor);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public IEnumerable<T> AsEnumerable<T>(Func<ISubclassMapping<T>, ISubclassMapping<T>> setup)
        {
            AssertHasReader();
            var mapping = new SubclassMapping<T>();
            setup(mapping);
            return AsEnumerable(mapping.BuildThunk(_reader));
        }

        // TODO: Method to .AsEnumerable the next N consecutive result sets
        // TODO: Method to .AsEnumerable all remaining result sets

        public object GetOutputParameter(string name)
        {
            if (_command == null || !_command.Parameters.Contains(name))
                return null;

            if (!(_command.Parameters[name] is DbParameter param))
                return null;

            if (param.Direction == ParameterDirection.Input)
                return null;
            if (param.Value == DBNull.Value)
                return null;
            return param.Value;
        }

        public T GetOutputParameter<T>(string name)
        {
            var value = GetOutputParameter(name);
            if (value == null)
                return default(T);
            if (!(value is T))
                throw new Exception($"Cannot get value '{name}'. Expected type {typeof(T).FullName} but found {value.GetType().FullName}");
            return (T) value;
        }

        public T GetOutputParameters<T>()
            where T : class, new()
        {
            // TODO: Can we introspect and get constructor parameters by name?
            var t = new T();
            if (_command == null)
                return t;

            var propertyMap = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name.ToLowerInvariant());
            foreach (var param in _command.Parameters.OfType<IDbDataParameter>())
            {
                var normalizedName = param.ParameterName.StartsWith("@") ? param.ParameterName.Substring(1) : param.ParameterName;
                normalizedName = normalizedName.ToLowerInvariant();
                if (propertyMap.ContainsKey(normalizedName))
                    propertyMap[normalizedName].SetValue(t, param.Value);
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

    public class SqlDataReaderResultStream : SqlDataReaderResult, IDataResultsStream
    {
        public SqlDataReaderResultStream(IDbCommand command, IExecutionContext context, IDataReader reader)
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