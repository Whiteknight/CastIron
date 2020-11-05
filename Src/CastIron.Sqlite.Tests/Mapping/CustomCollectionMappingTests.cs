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
        private class TestCollectionType : IList<string>
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

            public int IndexOf(string item) => throw new NotImplementedException();
            public void Insert(int index, string item) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();

            public int Count => _list.Count;
            public bool IsReadOnly => false;

            public string this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
        public void CustomCollection_ICollection()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<ICollection<string>>(
                    "SELECT 'A', 'B', 'C'",
                    setup: b => b
                        .For<ICollection<string>>(c => c
                            .UseType<TestCollectionType>()
                        )
                    )
                .First();
            result.Should().BeOfType<TestCollectionType>();
            var r = result.ToList();
            r[0].Should().Be("A");
            r[1].Should().Be("B");
            r[2].Should().Be("C");
        }

        [Test]
        public void CustomCollection_IEnumerable()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<IEnumerable<string>>(
                    "SELECT 'A', 'B', 'C'",
                    setup: b => b
                        .For<IEnumerable<string>>(c => c
                            .UseType<TestCollectionType>()
                        )
                    )
                .First();
            result.Should().BeOfType<TestCollectionType>();
            var r = result.ToList();
            r[0].Should().Be("A");
            r[1].Should().Be("B");
            r[2].Should().Be("C");
        }


        [Test]
        public void CustomCollection_IList()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<IList<string>>(
                    "SELECT 'A', 'B', 'C'",
                    setup: b => b
                        .For<IList<string>>(c => c
                            .UseType<TestCollectionType>()
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