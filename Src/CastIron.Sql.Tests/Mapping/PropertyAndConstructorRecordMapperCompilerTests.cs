using System;
using System.Collections.Generic;
using System.Text;
using CastIron.Sql.Mapping;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sql.Tests.Mapping
{
    [TestFixture]
    public class PropertyAndConstructorRecordMapperCompilerTests
    {
        public class Result
        {
            public Result(long a, long b, double c)
            {
                A = a;
                B = b;
                C = c;
            }

            public long A { get; }
            public long B { get; }
            public double C { get; }
        }

        [Test]
        public void GetConstructor_Test()
        {
            var result = PropertyAndConstructorRecordMapperCompiler.CompileExpression<Result>(new [] { "A", "B", "C" });
            result.Should().NotBeNull();
        }
    }
}
