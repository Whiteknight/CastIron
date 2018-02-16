using System;
using System.Data;
using System.Data.SqlClient;

namespace CastIron.Sql
{
    public class SqlQueryRunner
    {
        private readonly string _connectionString;

        public SqlQueryRunner(string connectionString)
        {
            _connectionString = connectionString;
        }

        public T Query<T>(ISqlQuery<T> query)
        {
            var text = query.GetSql();
            if (string.IsNullOrEmpty(text))
                return default(T);
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = CommandType.Text;
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
                using (SqlConnection connection = new SqlConnection(_connectionString))
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
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = CommandType.Text;
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
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = text;
                        command.CommandType = CommandType.Text;
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
                using (SqlConnection connection = new SqlConnection(_connectionString))
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
                using (SqlConnection connection = new SqlConnection(_connectionString))
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
    }
}