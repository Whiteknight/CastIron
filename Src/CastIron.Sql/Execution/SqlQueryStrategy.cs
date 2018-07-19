using System;
using System.Data;

namespace CastIron.Sql.Execution
{
    public class SqlQueryStrategy<T>
    {
        private readonly ISqlQuery<T> _query;

        public SqlQueryStrategy(ISqlQuery<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            var text = _query.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = text;
                    dbCommand.CommandType = (_query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (_query is ISqlParameterized parameterized)
                        parameterized.SetupParameters(dbCommand.Parameters);
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        var rawResultSet = new SqlResultSet(dbCommand, reader);
                        return _query.Read(rawResultSet);
                    }
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