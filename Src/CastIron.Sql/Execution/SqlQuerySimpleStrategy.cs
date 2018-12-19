using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQuerySimpleStrategy
    {
        public T Execute<T>(ISqlQuerySimple<T> query, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(query, dbCommand))
                {
                    context.MarkAborted();
                    return default(T);
                }

                context.StartAction(index, "Execute");
                try
                {
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        context.StartAction(index, "Map Results");
                        var rawResultSet = new SqlDataReaderResult(dbCommand, context, reader);
                        return query.Read(rawResultSet);
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
                    throw SqlProblemException.Wrap(e, dbCommand, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlQuerySimple<T> query, IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(query, dbCommand.Command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                context.StartAction(index, "Execute");
                try
                {
                    using (var reader = await dbCommand.ExecuteReaderAsync())
                    {
                        context.StartAction(index, "Map Results");
                        var rawResultSet = new SqlDataReaderResult(dbCommand.Command, context, reader);
                        return await Task.Run(() => query.Read(rawResultSet));
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
                    throw SqlProblemException.Wrap(e, dbCommand.Command, index);
                }
            }
        }

        public IDataResultsStream ExecuteStream(ISqlQuerySimple query, IExecutionContext context)
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
                throw SqlProblemException.Wrap(e, command, 1);
            }
        }

        public async Task<IDataResultsStream> ExecuteStreamAsync(ISqlQuerySimple query, IExecutionContext context)
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
                context.StartAction(1, "Execute");
                var reader = await command.ExecuteReaderAsync();

                context.StartAction(1, "Map Results");
                return new SqlDataReaderResultStream(command.Command, context, reader);
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
                throw SqlProblemException.Wrap(e, command.Command, 1);
            }
        }

        public bool SetupCommand(ISqlQuerySimple query, IDbCommand command)
        {
            var text = query.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            command.CommandText = text;
            command.CommandType = (query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            (query as ISqlParameterized)?.SetupParameters(command, command.Parameters);
            return true;
        }
    }
}