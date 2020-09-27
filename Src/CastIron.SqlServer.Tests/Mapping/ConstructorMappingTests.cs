using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests.Mapping
{
    [TestFixture]
    public class ConstructorMappingTests
    {
        private class TestObjectTwoConstructors
        {
            public int Id { get; set; }
            public string DoesNotExist { get; }

            public TestObjectTwoConstructors(int id)
            {
                Id = id;
            }

            public TestObjectTwoConstructors(int id, string doesNotExist)
            {
                Id = id;
                DoesNotExist = doesNotExist;
            }
        }

        private class TestQuerySpecifySecondConstructor : ISqlQuerySimple<IEnumerable<TestObjectTwoConstructors>>
        {
            public string GetSql()
            {
                return "SELECT 5 AS Id";
            }

            public IEnumerable<TestObjectTwoConstructors> Read(IDataResults result)
            {
                var constructor = typeof(TestObjectTwoConstructors).GetConstructor(new[] { typeof(int), typeof(string) });
                return result
                    .AsEnumerable<TestObjectTwoConstructors>(b => b
                        .For<TestObjectTwoConstructors>(c => c.UseConstructor(constructor))
                    )
                    .ToList();
            }
        }


        [Test]
        public void Map_ConstructorWithUnusedParameter()
        {
            var target = RunnerFactory.Create();

            var result = target.Query(new TestQuerySpecifySecondConstructor()).First();
            result.Id.Should().Be(5);
            result.DoesNotExist.Should().BeNull();
        }

    }
}