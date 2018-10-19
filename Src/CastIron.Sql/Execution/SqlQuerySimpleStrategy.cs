﻿using System;
using System.Data;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Execution
{
    public class SqlQuerySimpleStrategy<T>
    {
        private readonly ISqlQuerySimple<T> _query;

        public SqlQuerySimpleStrategy(ISqlQuerySimple<T> query)
        {
            _query = query;
        }

        public T Execute(IExecutionContext context, int index)
        {
            context.StartAction(index, "Setup Command");
            using (var dbCommand = context.CreateCommand())
            {
                if (!SetupCommand(dbCommand))
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
                        return _query.Read(rawResultSet);
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
                    throw e.WrapAsSqlProblemException(dbCommand, index);
                }
            }
        }

        public bool SetupCommand(IDbCommand command)
        {
            var text = _query.GetSql();
            if (string.IsNullOrEmpty(text))
                return false;
            command.CommandText = text;
            command.CommandType = (_query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
            if (_query is ISqlParameterized parameterized)
                parameterized.SetupParameters(command, command.Parameters);
            return true;
        }
    }
}