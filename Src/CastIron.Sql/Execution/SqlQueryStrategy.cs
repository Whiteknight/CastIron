using System;
using System.Threading;
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
            context.StartSetupCommand(index);
            using var command = context.CreateCommand();
            if (!SetupCommand(query, command))
            {
                context.MarkAborted();
                return default;
            }

            try
            {
                context.StartExecute(index, command);
                using var reader = command.ExecuteReader();
                context.StartMapResults(index);
                var resultSet = new DataReaderResults(context.Provider, command, context, reader);
                return queryReader.Read(resultSet);
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

        public async Task<T> ExecuteAsync<T>(ISqlQuery query, IResultMaterializer<T> queryReader, IExecutionContext context, int index, CancellationToken cancellationToken)
        {
            context.StartSetupCommand(index);
            using var command = context.CreateCommand();
            if (!SetupCommand(query, command))
            {
                context.MarkAborted();
                return default;
            }

            try
            {
                context.StartExecute(index, command);
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                context.StartMapResults(index);
                var resultSet = new DataReaderResults(context.Provider, command, context, reader);
                return queryReader.Read(resultSet);
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

        public IDataResultsStream ExecuteStream(ISqlQuery query, IExecutionContext context)
        {
            context.StartSetupCommand(1);
            var command = context.CreateCommand();

            if (!SetupCommand(query, command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.StartExecute(1, command);
                var reader = command.ExecuteReader();

                context.StartMapResults(1);
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

        public async Task<IDataResultsStream> ExecuteStreamAsync(ISqlQuery query, IExecutionContext context, CancellationToken cancellationToken)
        {
            context.StartSetupCommand(1);
            var command = context.CreateCommand();

            if (!SetupCommand(query, command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.StartExecute(1, command);
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                context.StartMapResults(1);
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
                var sql = context.Stringifier.Stringify(command.Command);
                throw SqlQueryException.Wrap(e, sql, 1);
            }
        }

        public bool SetupCommand(ISqlQuery query, IDbCommandAsync command)
        {
            var interaction = _interactionFactory.Create(command.Command);
            return query.SetupCommand(interaction);
        }
    }
}