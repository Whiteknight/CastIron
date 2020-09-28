using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    [TestFixture]
    public class DictionaryMappingTests
    {
        [Test]
        public void Map_DictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Dictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void Map_DictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<Dictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_IDictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        [Test]
        public void Map_IDictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<IDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObject()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'TEST' AS TestString, CAST(1 AS BIT) AS TestBool;").First();
            result.Count.Should().Be(3);
            result["TestString"].Should().Be("TEST");
            result["TestInt"].Should().Be(5);
            result["TestBool"].Should().Be(true);
        }

        public class TestObject_ChildDict
        {
            public int Id { get; set; }
            public Dictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_IReadOnlyDictionaryOfObjectWithDuplicates()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<IReadOnlyDictionary<string, object>>("SELECT 5 AS TestInt, 'A' AS TestString, 'B' AS TestString;").First();

            dict.Count.Should().Be(2);
            dict["TestInt"].Should().Be(5);
            dict["TestString"].Should().BeOfType<object[]>();
            var array = dict["TestString"] as object[];
            array[0].Should().Be("A");
            array[1].Should().Be("B");
        }

        [Test]
        public void Map_DictionaryOfListOfInt()
        {
            var target = RunnerFactory.Create();
            var dict = target.Query<Dictionary<string, List<int>>>("SELECT 1 AS A, 2 AS A, 3 AS B, 4 AS B, 5 AS C;").First();

            dict.Count.Should().Be(3);
            dict["A"].Should().ContainInOrder(1, 2);
            dict["B"].Should().ContainInOrder(3, 4);
            dict["C"].Should().ContainInOrder(5);
        }

        [Test]
        public void Map_CustomObjectWithChildDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }

        public class TestObject_ChildExistingDict
        {
            public TestObject_ChildExistingDict()
            {
                Child = new Dictionary<string, string>();
            }

            public int Id { get; set; }
            public Dictionary<string, string> Child { get; }
        }

        [Test]
        public void Map_CustomObjectWithChildExistingDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildExistingDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }

        public class TestObject_ChildIDict
        {
            public int Id { get; set; }
            public IDictionary<string, string> Child { get; set; }
        }

        [Test]
        public void Map_CustomObjectWithChildIDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildIDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }

        public class TestObject_ChildExistingIDict
        {
            public TestObject_ChildExistingIDict()
            {
                Child = new Dictionary<string, string>();
            }

            public int Id { get; set; }
            public IDictionary<string, string> Child { get; }
        }

        [Test]
        public void Map_CustomObjectWithChildExistingIDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<TestObject_ChildExistingIDict>("SELECT 5 AS Id, 'TEST' AS Child_String, 6 AS Child_Int;").First();
            result.Id.Should().Be(5);
            result.Child.Count.Should().Be(2);
            result.Child["String"].Should().Be("TEST");
            result.Child["Int"].Should().Be("6");
        }

        public class CustomDictionary : IDictionary<string, int>
        {
            private readonly Dictionary<string, int> _inner;
            public CustomDictionary()
            {
                _inner = new Dictionary<string, int>();
            }

            public int this[string key] { get => ((IDictionary<string, int>)_inner)[key]; set => ((IDictionary<string, int>)_inner)[key] = value; }

            public ICollection<string> Keys => ((IDictionary<string, int>)_inner).Keys;

            public ICollection<int> Values => ((IDictionary<string, int>)_inner).Values;

            public int Count => ((ICollection<KeyValuePair<string, int>>)_inner).Count;

            public bool IsReadOnly => ((ICollection<KeyValuePair<string, int>>)_inner).IsReadOnly;

            public void Add(string key, int value) => ((IDictionary<string, int>)_inner).Add(key, value);
            public void Add(KeyValuePair<string, int> item) => ((ICollection<KeyValuePair<string, int>>)_inner).Add(item);
            public void Clear() => ((ICollection<KeyValuePair<string, int>>)_inner).Clear();
            public bool Contains(KeyValuePair<string, int> item) => ((ICollection<KeyValuePair<string, int>>)_inner).Contains(item);
            public bool ContainsKey(string key) => ((IDictionary<string, int>)_inner).ContainsKey(key);
            public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, int>>)_inner).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, int>>)_inner).GetEnumerator();
            public bool Remove(string key) => ((IDictionary<string, int>)_inner).Remove(key);
            public bool Remove(KeyValuePair<string, int> item) => ((ICollection<KeyValuePair<string, int>>)_inner).Remove(item);
            public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value) => ((IDictionary<string, int>)_inner).TryGetValue(key, out value);
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_inner).GetEnumerator();
        }

        [Test]
        public void CustomDictionaryType_Map()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<CustomDictionary>("SELECT 5 AS A, 6 AS B, 7 AS C;").First();
            result.Should().BeOfType<CustomDictionary>();
            result["A"].Should().Be(5);
            result["B"].Should().Be(6);
            result["C"].Should().Be(7);
        }

        [Test]
        public void CustomDictionaryType_ConfigureInterface()
        {
            var target = RunnerFactory.Create();
            var result = target
                .Query<IDictionary<string, int>>(
                    "SELECT 5 AS A, 6 AS B, 7 AS C;",
                    b => b.For<IDictionary<string, int>>(
                        c => c.UseClass<CustomDictionary>()
                    )
                )
                .First();
            result.Should().BeOfType<CustomDictionary>();
            result["A"].Should().Be(5);
            result["B"].Should().Be(6);
            result["C"].Should().Be(7);
        }
    }
}