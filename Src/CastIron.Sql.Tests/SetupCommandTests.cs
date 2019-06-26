using System.Data;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
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

        public class CommandNotExecuted : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameterValue("@param").ToString();
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.AnsiString, 4);
                return false;
            }
        }

        [Test]
        public void Execute_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new CommandNotExecuted());
            result.Should().BeNullOrEmpty();
        }

        public class Command2 : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@param");
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText("SELECT @param = 'TEST';");
                command.AddOutputParameter("@param", DbType.StringFixedLength, 4);
                return false;
            }
        }

        [Test]
        public void Execute_T_NotExecuted()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command2());
            result.Should().BeNullOrEmpty();
        }
    }
}