using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CastIron.Sql;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace CastIron.Sqlite.Tests.Examples
{
    [TestFixture]
    public class ProductGroupTests
    {
        private class ProductGroup
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CompanyId { get; set; }
            public int UserId { get; set; }
            public int? CustomerId { get; set; }
            public string CustomerName { get; set; }
            public DateTime Date { get; set; }
            public Guid Guid { get; set; }
            public bool IsOwner { get; set; }
            public int? ProductCount { get; set; }
            public List<ProductGroupProduct> Products { get; set; }
            public int? ProjectId { get; set; }
            public int? ProjectItemId { get; set; }
            public int? SkinId { get; set; }
            public string SkinName { get; set; }
            public int? TemplateId { get; set; }
            public string TemplateName { get; set; }
        }

        private class ProductGroupProduct
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
        }

        private class GetProductGroupQuery : ISqlQuerySimple<ProductGroup>
        {
            public string GetSql()
            {
                return @"
                SELECT	
	                12345 AS ID,
	                '12345678-1234-1234-1234-123456789012' AS [Guid],
	                3 AS ProductCount;";
            }

            public ProductGroup Read(IDataResults result)
            {
                var productGroup = result.AsEnumerable<ProductGroup>().Single();
                return productGroup;
            }
        }

        [Test]
        public void TestProductGroup()
        {
            var runner = RunnerFactory.Create();
            var result = runner.Query(new GetProductGroupQuery());
            result.Products.Count.Should().Be(0);
        }
    }
}
