using System;
using System.Data;

namespace CastIron.Sql
{
    public class SqlQueryRunner
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SqlQueryRunner(string connectionString, IDbConnectionFactory connectionFactory = null)
        {
            _connectionFactory = connectionFactory ?? new SqlServerDbConnectionFactory(connectionString);
        }

        public T Query<T>(ISqlQuery<T> query)
        {
            var text = query.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = (query is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                        AddParameters(query, command);
                        using (var reader = command.ExecuteReader())
                        {
                            var result = new SqlQueryResult(command, reader);
                            return query.Read(result);
                        }
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, text, e);
            }
        }

        public T Query<T>(ISqlQueryRaw<T> query)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        if (!query.SetupCommand(command))
                            return default(T);
                        try
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                var result = new SqlQueryResult(command, reader);
                                return query.Read(result);
                            }
                        }
                        catch (Exception e1)
                        {
                            throw new SqlProblemException(e1.Message, command.CommandText, e1);
                        }
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, e);
            }
        }

        public void Execute(ISqlCommand commandObject)
        {
            var text = commandObject.GetSql();
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = (commandObject is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                        AddParameters(commandObject, command);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, text, e);
            }
        }

        public T Execute<T>(ISqlCommand<T> commandObject)
        {
            var text = commandObject.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);

            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = (commandObject is ISqlStoredProc) ? CommandType.StoredProcedure : CommandType.Text;
                        AddParameters(commandObject, command);
                        command.ExecuteNonQuery();
                        var result = new SqlQueryResult(command, null);
                        return commandObject.ReadOutputs(result);
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, text, e);
            }
        }

        public void Execute(ISqlCommandRaw commandObject)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        if (!commandObject.SetupCommand(command))
                            return;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception e1)
                        {
                            throw new SqlProblemException(e1.Message, command.CommandText, e1);
                        }
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, e);
            }
        }

        public T Execute<T>(ISqlCommandRaw<T> commandObject)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        if (!commandObject.SetupCommand(command))
                            return default(T);
                        try
                        {
                            command.ExecuteNonQuery();
                            var result = new SqlQueryResult(command, null);
                            return commandObject.ReadOutputs(result);
                        }
                        catch (Exception e1)
                        {
                            throw new SqlProblemException(e1.Message, command.CommandText, e1);
                        }
                    }
                }
            }
            catch (SqlProblemException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SqlProblemException(e.Message, e);
            }
        }

        private static void AddParameters(object obj, IDbCommand command)
        {
            if (obj is ISqlParameterized withParams)
            {
                var parameters = withParams.GetParameters();
                foreach (var parameter in parameters)
                    AddParameter(command, parameter);
            }
        }

        private static void AddParameter(IDbCommand command, Parameter parameter)
        {
            var param = command.CreateParameter();
            param.ParameterName = parameter.Name;
            param.Value = parameter.Value;
            param.Direction = parameter.Direction;
            command.Parameters.Add(param);
        }
    }
}