using System;
using CastIron.Sql;
using CastIron.Sql.Mapping;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Sqlite.Tests.Mapping
{
    public class ScalarCompilersTests
    {
        [Test]
        public void NoScalarCompilers_Error()
        {
            // If we don't have any scalar compilers configured, there's no mapping we can do
            var runner = RunnerFactory.Create();
            runner.MapCompiler.Clear();
            Action act = () => runner.Query<object>("SELECT 'TEST'");
            act.Should().Throw<MapCompilerException>();
        }
    }
}

