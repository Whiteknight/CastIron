using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class UnnamedColumnMappingTests
    { 
        public class TestQuery_UnnamedColumns<T> : ISqlQuerySimple<T>
        {
            public string GetSql()
            {
                return "SELECT 5, 'TEST', 3.14;";
            }

            public T Read(IDataResults result)
            {
                return result.AsEnumerable<T>().FirstOrDefault();
            }
        }

        public class TestObjectUnnamedValues
        {
            [UnnamedColumns]
            public IList<string> UnnamedValues { get; set; }
        }

        [Test]
        public void TestQuery_MapToUnnamedValuesCollection()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_UnnamedColumns<TestObjectUnnamedValues>());
            result.UnnamedValues.Count.Should().Be(3);
            result.UnnamedValues[0].Should().Be("5");
            result.UnnamedValues[1].Should().Be("TEST");
            result.UnnamedValues[2].Should().Be("3.14");
        }

        public class TestObjectUnnamedValuesCtor
        {
            public TestObjectUnnamedValuesCtor([UnnamedColumns] IList<string> unnamedValues)
            {
                UnnamedValues = unnamedValues;
            }

            public IList<string> UnnamedValues { get; }
        }

        [Test]
        public void TestQuery_MapToUnnamedValuesCollectionCtor()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_UnnamedColumns<TestObjectUnnamedValuesCtor>());
            result.UnnamedValues.Count.Should().Be(3);
            result.UnnamedValues[0].Should().Be("5");
            result.UnnamedValues[1].Should().Be("TEST");
            result.UnnamedValues[2].Should().Be("3.14");
        }

        public class TestQuery_AllNamedColumns<T> : ISqlQuerySimple<T>
        {
            public string GetSql()
            {
                return "SELECT 5 AS TestInt, 'TEST' AS TestString, 3.14 AS TestNumber;";
            }

            public T Read(IDataResults result)
            {
                return result.AsEnumerable<T>().FirstOrDefault();
            }
        }

        public class TestObjectUnnamedValues2
        {
            [UnnamedColumns]
            public IList<string> UnnamedValues { get; set; }
        }

        [Test]
        public void TestQuery_MapToUnnamedValuesCollection_None()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_AllNamedColumns<TestObjectUnnamedValues2>());
            result.UnnamedValues.Count.Should().Be(0);
        }

        public class TestObjectUnnamedValuesCtor2
        {
            public TestObjectUnnamedValuesCtor2([UnnamedColumns] IList<string> unnamedValues)
            {
                UnnamedValues = unnamedValues;
            }

            public IList<string> UnnamedValues { get; }
        }

        [Test]
        public void TestQuery_MapToUnnamedValuesCollectionCtor_None()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new TestQuery_AllNamedColumns<TestObjectUnnamedValuesCtor2>());
            result.UnnamedValues.Count.Should().Be(0);
        }

        [Test]
        public void DoNotTreatPostgresColumnAsUnnamed()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObjectUnnamedValues>("SELECT 5 AS [?column?];");
            result.Count.Should().Be(1);
            result[0].UnnamedValues.Count.Should().Be(0);
        }
    }
}