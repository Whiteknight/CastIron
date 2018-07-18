using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryTests
    {
        public class Query1 : ISqlQuery<string>
        {
            public string GetSql()
            {
                return "SELECT 'TEST';";
            }

            public string Read(SqlQueryRawResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQuery_Tests()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }
    }

    [TestFixture]
    public class SqlQueryRawCommandTests
    {
        public class Query1 : ISqlQueryRawCommand<string>
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT 'TEST';";
                return true;
            }

            public string Read(SqlQueryRawResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_Executed()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }

        public class Query2 : ISqlQueryRawCommand<string>
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT 'TEST';";
                return false;
            }

            public string Read(SqlQueryRawResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void SqlQueryRawCommand_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query2());
            result.Should().BeNullOrEmpty();
        }
    }

    [TestFixture]
    public class SqlQueryRawConnectionTests
    {
        public class Query1 : ISqlQueryRawConnection<string>
        {
            
            public string Read(SqlQueryRawResultSet result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }

            public string Query(IDbConnection connection, IDbTransaction transaction)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 'TEST';";
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return reader.GetString(0);
                    }
                }
            }
        }

        [Test]
        public void SqlQuery_Tests()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }
    }

    [TestFixture]
    public class SqlCommandTTests
    {
        public class Command1 : ISqlCommand<string>, ISqlParameterized
        {
            public string ReadOutputs(SqlQueryRawResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public string GetSql()
            {
                return "SELECT @param = 'TEST';";
            }

            public IEnumerable<Parameter> GetParameters()
            {
                return new[]
                {
                    Parameter.CreateStringOutputParameter("@param", 4)
                };
            }
        }

        [Test]
        public void SqlCommandT_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        public class Command2 : ISqlCommandRaw<string>
        {
            public string ReadOutputs(SqlQueryRawResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
                return false;
            }
        }

        [Test]
        public void SqlCommandT_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command2());
            result.Should().BeNullOrEmpty();
        }
    }

    [TestFixture]
    public class SqlCommandRawTests
    {
        public class Command1 : ISqlCommandRaw<string>
        {
            public string ReadOutputs(SqlQueryRawResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
                return true;
            }
        }

        [Test]
        public void SqlCommandRaw_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        public class Command2 : ISqlCommandRaw<string>
        {
            public string ReadOutputs(SqlQueryRawResultSet result)
            {
                return result.GetOutputParameter("@param").ToString();
            }

            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = "SELECT @param = 'TEST';";
                var p = new SqlParameter("@param", SqlDbType.Char, 4)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(p);
                return false;
            }
        }

        [Test]
        public void SqlCommandRaw_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command2());
            result.Should().BeNullOrEmpty();
        }
    }
}
