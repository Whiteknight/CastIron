using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQueryStreamStrategy
    {
        private readonly ISqlQuery _query;
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlQueryStreamStrategy(ISqlQuery query, IDataInteractionFactory interactionFactory)
        {
            _query = query;
            _interactionFactory = interactionFactory;
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
            var interaction = _interactionFactory.Create(command);
            return _query.SetupCommand(interaction);
        }
    }
}