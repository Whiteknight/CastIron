using System.Collections.Generic;
using CastIron.Sql.Mapping;
using FluentAssertions;
using NUnit.Framework;

namespace CastIron.Postgres.Tests.Utility
{
    [TestFixture]
    public class TypeExtensionTests
    {
        [Test]
        public void GetFriendlyName_System()
        {
            typeof(object).GetFriendlyName().Should().Be("System.Object");
            typeof(string).GetFriendlyName().Should().Be("System.String");
            typeof(int).GetFriendlyName().Should().Be("System.Int32");
        }

        [Test]
        public void GetFriendlyName_Array()
        {
            typeof(string[]).GetFriendlyName().Should().Be("System.String[]");
            typeof(string[,]).GetFriendlyName().Should().Be("System.String[,]");
            typeof(string[][]).GetFriendlyName().Should().Be("System.String[][]");
        }

        [Test]
        public void GetFriendlyName_GenericTypeDef()
        {
            typeof(ICollection<>).GetFriendlyName().Should().Be("System.Collections.Generic.ICollection<>");
            typeof(IDictionary<,>).GetFriendlyName().Should().Be("System.Collections.Generic.IDictionary<,>");
            typeof(Dictionary<,>).GetFriendlyName().Should().Be("System.Collections.Generic.Dictionary<,>");
        }

        [Test]
        public void GetFriendlyName_GenericType()
        {
            typeof(ICollection<object>).GetFriendlyName().Should().Be("System.Collections.Generic.ICollection<System.Object>");
            typeof(IDictionary<string,object>).GetFriendlyName().Should().Be("System.Collections.Generic.IDictionary<System.String,System.Object>");
            typeof(Dictionary<string,object>).GetFriendlyName().Should().Be("System.Collections.Generic.Dictionary<System.String,System.Object>");
        }
    }
}
