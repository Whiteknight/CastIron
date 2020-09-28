using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    /// <summary>
    /// Facilitates pretty-printing of commands for the purposes of debugging, logging and auditing
    /// Notice that the stringified commands may not be complete or valid SQL in some situations
    /// </summary>
    public class QueryObjectStringifier
    {
        private readonly IDataInteractionFactory _interactionFactory;
        private readonly IDbCommandStringifier _stringifier;

        public QueryObjectStringifier(IDbCommandStringifier commandStringifier, IDataInteractionFactory interactionFactory)
        {
            Argument.NotNull(commandStringifier, nameof(commandStringifier));
            Argument.NotNull(interactionFactory, nameof(interactionFactory));

            _interactionFactory = interactionFactory;
            _stringifier = commandStringifier;
        }

        public string Stringify<T>(ISqlQuerySimple<T> query)
        {
            var dummy = new DummyCommandAsync(new DummyDbCommand());
            new SqlQuerySimpleStrategy().SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommand command)
        {
            var dummy = new DummyCommandAsync(new DummyDbCommand());
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlCommand<T> command)
        {
            var dummy = new DummyCommandAsync(new DummyDbCommand());
            new SqlCommandStrategy(_interactionFactory).SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify<T>(ISqlQuery<T> query)
        {
            var dummy = new DummyCommandAsync(new DummyDbCommand());
            new SqlQueryStrategy(_interactionFactory).SetupCommand(query, dummy);
            return _stringifier.Stringify(dummy);
        }

        public string Stringify(ISqlCommandSimple command)
        {
            var dummy = new DummyCommandAsync(new DummyDbCommand());
            new SqlCommandSimpleStrategy().SetupCommand(command, dummy);
            return _stringifier.Stringify(dummy);
        }

        private class DummyDbDataParameter : IDbDataParameter
        {
            public byte Precision { get; set; }
            public byte Scale { get; set; }
            public int Size { get; set; }
            public DbType DbType { get; set; }
            public ParameterDirection Direction { get; set; }

            public bool IsNullable => true;

            public string ParameterName { get; set; }
            public string SourceColumn { get; set; }
            public DataRowVersion SourceVersion { get; set; }
            public object Value { get; set; }
        }

        private class DummyDataParameterCollection : IDataParameterCollection
        {
            private readonly List<object> _parameters;

            public DummyDataParameterCollection()
            {
                _parameters = new List<object>();
            }

            public object this[string parameterName] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public object this[int index] { get => _parameters[0]; set => _parameters[0] = value; }

            public bool IsFixedSize => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsSynchronized => throw new NotImplementedException();

            public object SyncRoot => throw new NotImplementedException();

            public int Add(object value)
            {
                _parameters.Add(value);
                return _parameters.Count;
            }
            public void Clear() => _parameters.Clear();
            public bool Contains(string parameterName) => throw new NotImplementedException();
            public bool Contains(object value) => throw new NotImplementedException();
            public void CopyTo(Array array, int index) => throw new NotImplementedException();
            public IEnumerator GetEnumerator() => _parameters.GetEnumerator();
            public int IndexOf(string parameterName) => throw new NotImplementedException();
            public int IndexOf(object value) => throw new NotImplementedException();
            public void Insert(int index, object value) => throw new NotImplementedException();
            public void Remove(object value) => throw new NotImplementedException();
            public void RemoveAt(string parameterName) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();
        }

        private sealed class DummyDbCommand : IDbCommand
        {
            public string CommandText { get; set; }
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection Connection { get; set; }

            public IDataParameterCollection Parameters { get; } = new DummyDataParameterCollection();

            public IDbTransaction Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }

            public void Cancel()
            {
                // No work needs to be done here, we never execute this so we never cancel it
            }

            public IDbDataParameter CreateParameter() => new DummyDbDataParameter();

            public void Dispose()
            {
                // No state to dispose here
            }

            public int ExecuteNonQuery() => throw new System.NotImplementedException();
            public IDataReader ExecuteReader() => throw new System.NotImplementedException();
            public IDataReader ExecuteReader(CommandBehavior behavior) => throw new System.NotImplementedException();
            public object ExecuteScalar() => throw new System.NotImplementedException();
            public void Prepare() => throw new System.NotImplementedException();
        }

        private sealed class DummyCommandAsync : IDbCommandAsync
        {
            public DummyCommandAsync(IDbCommand command)
            {
                Command = command;
            }

            public IDbCommand Command { get; }

            public void Dispose() => throw new System.NotImplementedException();


            public IDataReaderAsync ExecuteReader() => throw new System.NotImplementedException();

            public Task<IDataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken) => throw new System.NotImplementedException();

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
    }
}