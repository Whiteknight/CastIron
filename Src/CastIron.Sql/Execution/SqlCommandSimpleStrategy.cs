using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlCommandSimpleStrategy
    {
        public void Execute(ISqlCommandSimple command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(command, dbCommand))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
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

        public async Task ExecuteAsync(ISqlCommandSimple command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(command, dbCommand.Command))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
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

        public T Execute<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(command, dbCommand))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartExecute(index, dbCommand);
                    var rowsAffected = dbCommand.ExecuteNonQuery();

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

        public async Task<T> ExecuteAsync<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(command, dbCommand.Command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartExecute(index, dbCommand.Command);
                    var rowsAffected = await dbCommand.ExecuteNonQueryAsync();

                    context.StartMapResults(index);
                    var resultSet = new DataReaderResults(context.Provider, dbCommand.Command, context, null, rowsAffected);
                    return await Task.Run(() => command.ReadOutputs(resultSet));
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

        public bool SetupCommand(ISqlCommandSimple command, IDbCommand dbCommand)
        {
            var text = command.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            dbCommand.CommandText = text;
            dbCommand.CommandType = (command is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            (command as ISqlParameterized)?.SetupParameters(dbCommand, dbCommand.Parameters);
            return true;
        }
    }
}