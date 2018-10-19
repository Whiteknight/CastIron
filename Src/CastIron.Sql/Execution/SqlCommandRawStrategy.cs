﻿using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlCommandRawStrategy
    {
        private readonly ISqlCommand _command;
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlCommandRawStrategy(ISqlCommand command, IDataInteractionFactory interactionFactory)
        {
            _command = command;
            _interactionFactory = interactionFactory;
        }

        public void Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(dbCommand))
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

        public bool SetupCommand(IDbCommand command)
        {
            var interaction = _interactionFactory.Create(command);
            return _command.SetupCommand(interaction);
        }
    }

    public class SqlCommandRawStrategy<T>
    {
        private readonly ISqlCommand<T> _command;
        private readonly IDataInteractionFactory _interactionFactory;

        public SqlCommandRawStrategy(ISqlCommand<T> command, IDataInteractionFactory interactionFactory)
        {
            _command = command;
            _interactionFactory = interactionFactory;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                try
                {
                    if (!SetupCommand(dbCommand))
                    {
                        context.MarkAborted();
                        return default(T);
                    }

                    context.StartAction(index, "Execute");
                    dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResult(dbCommand, context, null);
                    return _command.ReadOutputs(resultSet);
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

        public bool SetupCommand(IDbCommand command)
        {
            var interaction = _interactionFactory.Create(command);
            return _command.SetupCommand(interaction);
        }
    }
}