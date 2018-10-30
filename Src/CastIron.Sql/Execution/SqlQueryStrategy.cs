using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQueryStrategy<T>
    {
        private readonly ISqlQuery<T> _query;
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlQueryStrategy(ISqlQuery<T> query, IDataInteractionFactory interactionFactory)
        {
            _query = query;
            _interactionFactory = interactionFactory;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var command = context.CreateCommand())
            {
                if (!SetupCommand(command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartAction(index, "Execute");
                    using (var reader = command.ExecuteReader())
                    {
                        context.StartAction(index, "Map Results");
                        var resultSet = new SqlDataReaderResult(command, context, reader);
                        return _query.GetResults(resultSet);
                    }
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(command, index);
                }
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            var interaction = _interactionFactory.Create(command);
            return _query.SetupCommand(interaction);
        }
    }
}