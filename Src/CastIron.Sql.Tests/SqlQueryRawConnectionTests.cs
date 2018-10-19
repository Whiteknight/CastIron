using System.Data;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlQueryRawConnectionTests
    {
        public class Query1 : ISqlConnectionAccessor<string>
        {
            
            public string Read(IDataResults result)
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
        public void SqlQuery_Tests([Values("MSSQL", "SQLITE")] string provider)
        {
            var runner = RunnerFactory.Create(provider);
            var result = runner.Query(new Query1());
            result.Should().Be("TEST");
        }
    }
}