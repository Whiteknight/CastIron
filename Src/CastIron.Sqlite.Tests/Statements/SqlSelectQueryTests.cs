﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CastIron.Sql;
using CastIron.Sql.Statements;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Statements
{
    [TestFixture]
    public class SqlSelectQueryTests
    {
        [Table("TestObject")]
        private class TestObject
        {
            public int TestInt { get; set; }
            public string TestString { get; set; }

            [Column("SecondInt")]
            public int OtherTestInt { get; set; }
        }

        public class CreateTestObjectTableCommand : ISqlCommand
        {
            public bool SetupCommand(IDataInteraction interaction)
            {
                interaction.ExecuteText(@"
CREATE TEMP TABLE TestObject (
    TestInt INT NOT NULL,
    TestString VARCHAR(4) NOT NULL,
    SecondInt INT NOT NULL
);
INSERT INTO TestObject (TestInt, TestString, SecondInt) 
    VALUES 
        (3, 'FAIL', 4),
        (5, 'OK',   6);");
                return true;
            }
        }

        [Test]
        public void ToSql_Test()
        {
            // TODO: Fix this
            Assert.Inconclusive("SQL Lite cannot use dbo. prefixes on table names");
            var runner = RunnerFactory.Create();

            var batch = runner.CreateBatch();

            // Create the table and populate with test data
            batch.Add(new CreateTestObjectTableCommand());

            // Query the objects from the table
            var promise = batch.Add(new SqlSelectQuery<TestObject>()
                .Where(b => b.Equal(c => c.TestInt, 5)));

            runner.Execute(batch);

            var result = promise.GetValue().ToList();
            result.Count.Should().Be(1);
            result[0].TestInt.Should().Be(5);
            result[0].TestString.Should().Be("OK");
            result[0].OtherTestInt.Should().Be(6);
        }
    }
}
