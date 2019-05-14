using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace CastIron.Sql.Statements
{
    public class SelectListBuilder : ISelectListBuilder
    {
        private static readonly SelectListBuilder _defaultBuilder = new SelectListBuilder();
        private static readonly ISelectListBuilder _cachedBuilder = new CachedSelectListBuilder(_defaultBuilder);

        public static ISelectListBuilder GetDefault()
        {
            return _defaultBuilder;
        }

        public static ISelectListBuilder GetCached()
        {
            return _cachedBuilder;
        }

        public string BuildSelectList(Type type, string prefix = "")
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(prefix))
                prefix = $"[{prefix}].";
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attrs = property.GetCustomAttributes().ToList();
                if (attrs.Any(a => a is NotMappedAttribute))
                    continue;

                var propertyName = property.Name;
                var columnName = propertyName;

                var columnAttr = attrs.OfType<ColumnAttribute>().FirstOrDefault();
                if (!string.IsNullOrEmpty(columnAttr?.Name))
                    columnName = columnAttr.Name;

                list.Add($"{prefix}[{columnName}] AS [{propertyName}]");
            }

            return string.Join(",", list);
        }
    }
}
