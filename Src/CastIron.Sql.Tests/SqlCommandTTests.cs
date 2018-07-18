﻿using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
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
}