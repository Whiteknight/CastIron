using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Statements
{
    [TestFixture]
    public class SqlSelectQueryTests
    {
        [Table("#TestObject")]
        private class TestObject
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }

            [Column("SecondInt")]
            public int OtherTestInt { get; set; }
        }

        public class CreateTestObjectTableCommand : ISqlCommand
        {
            public bool SetupCommand(IDbCommand command)
            {
                command.CommandText = @"
CREATE TABLE #TestObject (
    TestInt INT NOT NULL,
    TestString VARCHAR(10) NOT NULL,
    SecondInt INT NOT NULL
);
INSERT INTO #TestObject (TestInt, TestString, SecondInt) VALUES (3, 'FAIL', 4);
INSERT INTO #TestObject (TestInt, TestString, SecondInt) VALUES (5, 'OK', 6);";
                return true;
            }
        }

        [Test]
        public void ToSql_Test()
        {
            var target = new SqlSelectQuery<TestObject>()
                .Where(b => b.Equal(c => c.TestInt, 5));
            var batch = new SqlBatch();
            batch.Add(new CreateTestObjectTableCommand());
            var promise = batch.Add(target);

            var runner = RunnerFactory.Create();
            runner.Execute(batch);

            var result = promise.GetValue().ToList();
            result.Count.Should().Be(1);
            result[0].TestInt.Should().Be(5);
            result[0].TestString.Should().Be("OK");
            result[0].OtherTestInt.Should().Be(6);
        }
    }
}
