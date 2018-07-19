using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class SqlCommandStrategy
    {
        private readonly ISqlCommand _command;

        public SqlCommandStrategy(ISqlCommand command)
        {
            _command = command;
        }

        public void Execute(IExecutionContext context, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return;

            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (_command is ISqlParameterized parameterized)
                        parameterized.SetupParameters(dbCommand.Parameters);
                    dbCommand.ExecuteNonQuery();
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, text, index);
                }
            }
        }
    }

    public class SqlCommandStrategy<T>
    {
        private readonly ISqlCommand<T> _command;

        public SqlCommandStrategy(ISqlCommand<T> command)
        {
            _command = command;
        }

        public T Execute(IExecutionContext context, int index)
        {
            var text = _command.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (_command is ISqlParameterized parameterized)
                        parameterized.SetupParameters(dbCommand.Parameters);
                    dbCommand.ExecuteNonQuery();
                    var resultSet = new SqlResultSet(dbCommand, null);
                    return _command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw e.WrapAsSqlProblemException(dbCommand, text, index);
                }
            }
        }
    }
}