using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class CustomDictionaryMappingTests
    {
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
                    setup: b => b.For<IDictionary<string, int>>(
                        c => c.UseType<CustomDictionary>()
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