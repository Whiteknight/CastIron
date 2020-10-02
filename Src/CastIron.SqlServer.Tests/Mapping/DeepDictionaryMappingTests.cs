using System.Collections.Generic;
using System.Linq;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.SqlServer.Tests.Mapping
{
    public class DeepDictionaryMappingTests
    {
        public class Test1
        {
            public IDictionary<string, int> Values { get; set; }
        }

        public class Test2
        {
            public Test1 C { get; set; }
        }

        public class Test3
        {
            public Test2 B { get; set; }
        }

        public class Test4
        {
            public Test3 A { get; set; }
        }

        [Test]
        public void DeepMapping_IDictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Test4>("SELECT 1 AS A_B_C_Values_Test;").First();
            result.Should().BeOfType<Test4>();
            result.A.B.C.Values["Test"].Should().Be(1);
        }

        public class Test5
        {
            public Dictionary<string, int> Values { get; set; }
        }

        public class Test6
        {
            public Test5 C { get; set; }
        }

        public class Test7
        {
            public Test6 B { get; set; }
        }

        public class Test8
        {
            public Test7 A { get; set; }
        }

        [Test]
        public void DeepMapping_Dictionary()
        {
            var target = RunnerFactory.Create();
            var result = target.Query<Test8>("SELECT 1 AS A_B_C_Values_Test;").First();
            result.Should().BeOfType<Test8>();
            result.A.B.C.Values["Test"].Should().Be(1);
        }
    }
}