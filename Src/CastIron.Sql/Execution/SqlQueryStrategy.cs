using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQueryStrategy
    {
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlQueryStrategy(IDataInteractionFactory interactionFactory)
        {
            _interactionFactory = interactionFactory;
        }

        public T Execute<T>(ISqlQuery query, IResultMaterializer<T> queryReader, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var command = context.CreateCommand())
            {
                if (!SetupCommand(query, command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.BeforeExecution(command);
                    context.StartAction(index, "Execute");
                    using (var reader = command.ExecuteReader())
                    {
                        context.StartAction(index, "Map Results");
                        var resultSet = new DataReaderResults(context.Provider, command, context, reader);
                        return queryReader.Read(resultSet);
                    }
                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(command);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlQuery query, IResultMaterializer<T> queryReader, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var command = context.CreateAsyncCommand())
            {
                if (!SetupCommand(query, command.Command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.BeforeExecution(command.Command);
                    context.StartAction(index, "Execute");
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        context.StartAction(index, "Map Results");
                        var resultSet = new DataReaderResults(context.Provider, command.Command, context, reader);
                        return queryReader.Read(resultSet);
                    }
                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(command.Command);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public IDataResultsStream ExecuteStream(ISqlQuery query, IExecutionContext context)
        {
            context.StartAction(1, "Setup Command");
            var command = context.CreateCommand();

            if (!SetupCommand(query, command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.BeforeExecution(command);
                context.StartAction(1, "Execute");
                var reader = command.ExecuteReader();

                context.StartAction(1, "Map Results");
                return new DataReaderResultsStream(context.Provider, command, context, reader);
            }
            catch (SqlQueryException)
            {
                context.MarkAborted();
                command.Dispose();
                throw;
            }
            catch (Exception e)
            {
                context.MarkAborted();
                command.Dispose();
                var sql = context.Stringifier.Stringify(command);
                throw SqlQueryException.Wrap(e, sql, 1);
            }
        }

        public async Task<IDataResultsStream> ExecuteStreamAsync(ISqlQuery query, IExecutionContext context)
        {
            context.StartAction(1, "Setup Command");
            var command = context.CreateAsyncCommand();

            if (!SetupCommand(query, command.Command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.BeforeExecution(command.Command);
                context.StartAction(1, "Execute");
                var reader = await command.ExecuteReaderAsync();

                context.StartAction(1, "Map Results");
                return new DataReaderResultsStream(context.Provider, command.Command, context, reader);
            }
            catch (SqlQueryException)
            {
                context.MarkAborted();
                command.Dispose();
                throw;
            }
            catch (Exception e)
            {
                context.MarkAborted();
                command.Dispose();
                var sql = context.Stringifier.Stringify(command.Command);
                throw SqlQueryException.Wrap(e, sql, 1);
            }
        }

        public bool SetupCommand(ISqlQuery query, IDbCommand command)
        {
            var interaction = _interactionFactory.Create(command);
            return query.SetupCommand(interaction);
        }
    }
}