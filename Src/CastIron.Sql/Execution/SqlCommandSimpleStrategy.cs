using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlCommandSimpleStrategy
    {
        public void Execute(ISqlCommandSimple command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using var dbCommand = context.CreateCommand();
            if (!SetupCommand(command, dbCommand))
            {
                context.MarkAborted();
                return;
            }

            try
            {
                context.StartExecute(index, dbCommand);
                dbCommand.Command.ExecuteNonQuery();
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

        public T Execute<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using var dbCommand = context.CreateCommand();
            if (!SetupCommand(command, dbCommand))
            {
                context.MarkAborted();
                return default;
            }

            try
            {
                context.StartExecute(index, dbCommand);
                var rowsAffected = dbCommand.Command.ExecuteNonQuery();

                context.StartMapResults(index);
                var resultSet = new DataReaderResults(dbCommand, context, null, rowsAffected);
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

        public async Task ExecuteAsync(ISqlCommandSimple command, IExecutionContext context, int index, CancellationToken cancellationToken)
        {
            context.StartSetupCommand(index);
            using var dbCommand = context.CreateCommand();
            if (!SetupCommand(command, dbCommand))
            {
                context.MarkAborted();
                return;
            }

            try
            {
                context.StartExecute(index, dbCommand);
                await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

        public async Task<T> ExecuteAsync<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index, CancellationToken cancellationToken)
        {
            context.StartSetupCommand(index);
            using var dbCommand = context.CreateCommand();
            if (!SetupCommand(command, dbCommand))
            {
                context.MarkAborted();
                return default;
            }

            try
            {
                context.StartExecute(index, dbCommand);
                var rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                context.StartMapResults(index);
                var resultSet = new DataReaderResults(dbCommand, context, null, rowsAffected);
                return await Task.Run(() => command.ReadOutputs(resultSet), cancellationToken).ConfigureAwait(false);
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

        public bool SetupCommand(ISqlCommandSimple command, IDbCommandAsync dbCommand)
        {
            var text = command.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            dbCommand.Command.CommandText = text;
            dbCommand.Command.CommandType = (command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            (command as ISqlParameterized)?.SetupParameters(dbCommand.Command, dbCommand.Command.Parameters);
            return true;
        }
    }
}