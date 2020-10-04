using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Mapping
{
    [TestFixture]
    public class UnnamedColumnMappingTests
    { 
        public class Map_UnnamedColumns<T> : ISqlQuerySimple<T>
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
        public void Map_MapToUnnamedValuesCollection()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_UnnamedColumns<TestObjectUnnamedValues>());
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
        public void Map_MapToUnnamedValuesCollectionCtor()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_UnnamedColumns<TestObjectUnnamedValuesCtor>());
            result.UnnamedValues.Count.Should().Be(3);
            result.UnnamedValues[0].Should().Be("5");
            result.UnnamedValues[1].Should().Be("TEST");
            result.UnnamedValues[2].Should().Be("3.14");
        }

        public class Map_AllNamedColumns<T> : ISqlQuerySimple<T>
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
        public void Map_MapToUnnamedValuesCollection_None()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_AllNamedColumns<TestObjectUnnamedValues2>());
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
        public void Map_MapToUnnamedValuesCollectionCtor_None()
        {
            var target = RunnerFactory.Create();
            var result = target.Query(new Map_AllNamedColumns<TestObjectUnnamedValuesCtor2>());
            result.UnnamedValues.Count.Should().Be(0);
        }
    }
}