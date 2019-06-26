using System.Data;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests
{
    [TestFixture]
    public class SetupCommandTests
    {

        public class QueryExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return true;
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void Query_Executed()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new QueryExecuted());
            result.Should().Be("TEST");
        }

        public class QueryNotExecuted : ISqlQuery<string>
        {
            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT 'TEST';");
                return false;
            }

            public string Read(IDataResults result)
            {
                return result.AsEnumerable<string>().FirstOrDefault();
            }
        }

        [Test]
        public void Query_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new QueryNotExecuted());
            result.Should().BeNullOrEmpty();
        }
    }
}