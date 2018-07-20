using System.Data;
using System.Data.SqlClient;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
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

            public string Read(SqlResultSet result)
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

            public string Read(SqlResultSet result)
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
}