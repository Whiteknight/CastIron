using System;
using System.Data;
using System.Threading.Tasks;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQuerySimpleStrategy
    {
        public T Execute<T>(ISqlQuerySimple query, IResultMaterializer<T> queryReader, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(query, dbCommand))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartExecute(index, dbCommand);
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        context.StartMapResults(index);
                        var rawResultSet = new DataReaderResults(context.Provider, dbCommand, context, reader);
                        return queryReader.Read(rawResultSet);
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
                    var sql = context.Stringifier.Stringify(dbCommand);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(ISqlQuerySimple query, IResultMaterializer<T> queryReader, IExecutionContext context, int index)
        {
            context.StartSetupCommand(index);
            using (var dbCommand = context.CreateAsyncCommand())
            {
                if (!SetupCommand(query, dbCommand.Command))
                {
                    context.MarkAborted();
                    return default(T);
                }

                try
                {
                    context.StartExecute(index, dbCommand.Command);
                    using (var reader = await dbCommand.ExecuteReaderAsync())
                    {
                        context.StartMapResults(index);
                        var rawResultSet = new DataReaderResults(context.Provider, dbCommand.Command, context, reader);
                        return await Task.Run(() => queryReader.Read(rawResultSet));
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
                    var sql = context.Stringifier.Stringify(dbCommand.Command);
                    throw SqlQueryException.Wrap(e, sql, index);
                }
            }
        }

        public IDataResultsStream ExecuteStream(ISqlQuerySimple query, IExecutionContext context)
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

        public async Task<IDataResultsStream> ExecuteStreamAsync(ISqlQuerySimple query, IExecutionContext context)
        {
            context.StartSetupCommand(1);
            var command = context.CreateAsyncCommand();

            if (!SetupCommand(query, command.Command))
            {
                context.MarkAborted();
                command.Dispose();
                return null;
            }

            try
            {
                context.StartExecute(1, command.Command);
                var reader = await command.ExecuteReaderAsync();

                context.StartMapResults(1);
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