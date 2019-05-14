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
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(command, dbCommand))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
                    context.StartAction(index, "Execute");
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
                    throw SqlQueryException.Wrap(e, dbCommand, index);
                }
            }
        }

        public async Task ExecuteAsync(ISqlCommandSimple command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(command, dbCommand.Command))
                {
                    context.MarkAborted();
                    return;
                }

                try
                {
                    context.StartAction(index, "Execute");
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
                    throw SqlQueryException.Wrap(e, dbCommand.Command, index);
                }
            }
        }

        public T Execute<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(command, dbCommand))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartAction(index, "Execute");
                    var rowsAffected = dbCommand.ExecuteNonQuery();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResults(dbCommand, context, null, rowsAffected);
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
                    throw SqlQueryException.Wrap(e, dbCommand, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlCommandSimple<T> command, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(command, dbCommand.Command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartAction(index, "Execute");
                    var rowsAffected = await dbCommand.ExecuteNonQueryAsync();

                    context.StartAction(index, "Map Results");
                    var resultSet = new SqlDataReaderResults(dbCommand.Command, context, null, rowsAffected);
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
                    throw SqlQueryException.Wrap(e, dbCommand.Command, index);
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