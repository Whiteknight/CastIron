using System.Data;
using System.Threading.Tasks;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests
{
    [TestFixture]
    public class SqlCommandTests
    {
        public class Command1 : ISqlCommand<string>
        {
            public string ReadOutputs(IDataResults result)
            {
                return result.GetOutputParameter<string>("@param");
            }

            public bool SetupCommand(IDataInteraction command)
            {
                command.ExecuteText(GetSql());
                command.AddOutputParameter("@param", DbType.StringFixedLength, 4);

                return true;
            }

            public string GetSql()
            {
                return "SELECT @param = 'TEST';";
            }
        }

        [Test]
        public void Execute_T_Test()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Execute(new Command1());
            result.Should().Be("TEST");
        }

        [Test]
        public async Task Execute_T_Async()
        {
            var runner = RunnerFactory.Create();
            var result = await runner.ExecuteAsync(new Command1());
            result.Should().Be("TEST");
        }
    }
}