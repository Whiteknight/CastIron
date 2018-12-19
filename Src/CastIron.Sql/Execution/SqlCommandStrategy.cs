﻿using System;
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
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand))
                    {
                        context.MarkAborted();
                        return;
                    }

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public async Task ExecuteAsync(ISqlCommand command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand.Command))
                    {
                        context.MarkAborted();
                        return;
                    }

                    context.StartAction(index, "Execute");
                    await dbCommand.ExecuteNonQueryAsync();

                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand.Command, index);
                }
            }
        }

        public T Execute<T>(ISqlCommand<T> command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResult(dbCommand, context, null);
                    return command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlCommand<T> command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                try
                {
                    if (!SetupCommand(command, dbCommand.Command))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartAction(index, "Execute");
                    await dbCommand.ExecuteNonQueryAsync();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResult(dbCommand.Command, context, null);
                    return command.ReadOutputs(resultSet);
                }
                catch (SqlProblemException)
                {
                    context.MarkAborted();
                    throw;
                }
                catch (Exception e)
                {
                    context.MarkAborted();
                    throw e.WrapAsSqlProblemException(dbCommand.Command, index);
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