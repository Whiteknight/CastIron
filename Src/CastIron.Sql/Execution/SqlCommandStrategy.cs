using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlCommandStrategy
    {
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlCommandStrategy(IDataInteractionFactory interactionFactory)
        {
            _interactionFactory = interactionFactory;
        }

        public void Execute(ISqlCommand command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand))
                    {
                        context.MarkAborted();
                        return;
                    }

                    context.StartExecute(index, dbCommand);
                    dbCommand.ExecuteNonQuery();
                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(dbCommand);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public async Task ExecuteAsync(ISqlCommand command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateAsyncCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand.Command))
                    {
                        context.MarkAborted();
                        return;
                    }

                    context.StartExecute(index, dbCommand.Command);
                    await dbCommand.ExecuteNonQueryAsync();

                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(dbCommand.Command);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public T Execute<T>(ISqlCommand<T> command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartExecute(index, dbCommand);
                    int rowsAffected = dbCommand.ExecuteNonQuery();

                    context.StartMapResults(index);
                    var resultSet = new DataReaderResults(context.Provider, dbCommand, context, null, rowsAffected);
                    return command.ReadOutputs(resultSet);
                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(dbCommand);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlCommand<T> command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateAsyncCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand.Command))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartExecute(index, dbCommand.Command);
                    var rowsAffected = await dbCommand.ExecuteNonQueryAsync();

                    context.StartMapResults(index);
                    var resultSet = new DataReaderResults(context.Provider, dbCommand.Command, context, null, rowsAffected);
                    return command.ReadOutputs(resultSet);
                }
                catch (SqlQueryException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    var sql = context.Stringifier.Stringify(dbCommand.Command);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public bool SetupCommand(ISqlCommand command, IDbCommand dbCommand)
        {
            var interaction = _interactionFactory.Create(dbCommand);
            return command.SetupCommand(interaction);
        }
    }
}