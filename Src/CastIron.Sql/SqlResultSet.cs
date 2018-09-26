using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Debugging;
using CastIron.Sql.Execution;
using CastIron.Sql.Mapping;

namespace CastIron.Sql
{
    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class SqlResultSet
    {
        private readonly IDbCommand _command;
        private readonly IExecutionContext _context;
        private readonly IDataReader _reader;
        private bool _isConsumed;

        public SqlResultSet(IDbCommand command, IExecutionContext context, IDataReader reader)
        {
            _command = command;
            _context = context;
            _reader = reader;
        }

        public IDataReader AsRawReader()
        {
            AssertHasReader();
            MarkConsumed();
            return _reader;
        }

        public IDataReader AsRawReaderWithBetterErrorMessages()
        {
            AssertHasReader();
            MarkConsumed();
            return new DataReaderWithBetterErrorMessages(_reader);
        }

        public MultiResultMapper AsResultMapper()
        {
            AssertHasReader();
            MarkConsumed();
            return new MultiResultMapper(_reader, _context);
        }

        public IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> map = null)
        {
            ValidateDataReader();
            MarkConsumed();
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public IEnumerable<T> AsEnumerable<T>(IRecordMapperCompiler compiler)
        {
            ValidateDataReader();
            MarkConsumed();
            return new DataRecordMappingEnumerable<T>(_reader, _context, compiler);
        }

        private void ValidateDataReader()
        {
            if (_reader == null)
                throw new InvalidOperationException("Cannot map results to enumerable because the reader is null. Are you executing an ISqlCommand variant?");
        }

        public IEnumerable<T> AsEnumerable<T>(Func<SubclassRecordMapperCompiler<T>, SubclassRecordMapperCompiler<T>> setupCompiler)
        {
            ValidateDataReader();
            var compiler = new SubclassRecordMapperCompiler<T>();
            setupCompiler(compiler);
            return AsEnumerable<T>(compiler);
        }

        public object GetOutputParameter(string name)
        {
            if (!_command.Parameters.Contains(name))
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

        private void MarkConsumed()
        {
            if (_isConsumed)
                throw new Exception("SqlDataReader is forward-only, and cannot be consumed more than once");
            _isConsumed = true;
        }

        private void AssertHasReader()
        {
            if (_reader == null)
                throw new Exception("This result does not contain a data reader");
        }
    }
}