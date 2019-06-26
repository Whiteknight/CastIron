using System.Data;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    [TestFixture]
    public class SqlConnectionAccessorTests
    {
        public class Accessor1 : ISqlConnectionAccessor<string>
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
        public void SqlQuery_Tests()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Access(new Accessor1());
            result.Should().Be("TEST");
        }
    }
}