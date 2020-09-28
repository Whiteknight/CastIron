using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class CustomCollectionMappingTests
    {
        private class TestCollectionType : ICollection<string>
        {
            private readonly List<string> _list;

            public TestCollectionType()
            {
                _list = new List<string>();
            }

            public IEnumerator<string> GetEnumerator() => _list.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(string item) => _list.Add(item);

            public void Clear() => _list.Clear();

            public bool Contains(string item) => _list.Contains(item);

            public void CopyTo(string[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public bool Remove(string item)
            {
                throw new NotImplementedException();
            }

            public int Count => _list.Count;
            public bool IsReadOnly => false;
        }

        [Test]
        public void TestQuery_CustomCollectionType()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestCollectionType>("SELECT 'A', 'B', 'C'").First().ToList();
            result[0].Should().Be("A");
            result[1].Should().Be("B");
            result[2].Should().Be("C");
        }

        [Test]
        public void TestQuery_CustomCollectionType_Configured()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<ICollection<string>>(
                    "SELECT 'A', 'B', 'C'",
                    b => b
                        .For<ICollection<string>>(c => c
                            .UseClass<TestCollectionType>()
                        )
                    )
                .First();
            result.Should().BeOfType<TestCollectionType>();
            var r = result.ToList();
            r[0].Should().Be("A");
            r[1].Should().Be("B");
            r[2].Should().Be("C");
        }
    }
}