using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQuerySimpleStreamStrategy
    {
        private readonly ISqlQuerySimple _query;

        public SqlQuerySimpleStreamStrategy(ISqlQuerySimple query)
        {
            _query = query;
        }

        public IDataResultsStream Execute(IExecutionContext context)
        {
            context.StartAction(1, "Setup Command");
            var command = context.CreateCommand();
            
            if (!SetupCommand(command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.StartAction(1, "Execute");
                var reader = command.ExecuteReader();

                context.StartAction(1, "Map Results");
                return new SqlDataReaderResultStream(command, context, reader);
            }
            catch (SqlProblemException)
            {
                context.MarkAborted();
                command.Dispose();
                throw;
            }
            catch (Exception e)
            {
                context.MarkAborted();
                command.Dispose();
                throw e.WrapAsSqlProblemException(command, 1);
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            var text = _query.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            command.CommandText = text;
            command.CommandType = (_query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            (_query as ISqlParameterized)?.SetupParameters(command, command.Parameters);
            return true;
        }
    }
}