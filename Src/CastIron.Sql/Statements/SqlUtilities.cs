using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Mapping;

namespace CastIron.Sql.Statements
{
    public static class SqlUtilities
    {
        public static string GetTableName(Type type)
        {
            var attr = type.GetCustomAttributes(true).OfType<TableAttribute>().FirstOrDefault();
            if (attr == null)
                return $"[dbo].[{type.Name}]";
            return $"{(attr.Schema ?? "[dbo]")}.{(attr.Name ?? type.Name)}";
        }

        public static string GetPropertyColumnName<TObj, TProperty>(Expression<Func<TObj, TProperty>> propertyLambda)
        {
            var property = GetPropertyFromMemberExpression(propertyLambda);
            var attr = property.GetCustomAttributes(typeof(ColumnAttribute)).OfType<ColumnAttribute>().FirstOrDefault();
            return attr?.Name ?? property.Name;
        }

        public static PropertyInfo GetPropertyFromMemberExpression<TObj, TProperty>(Expression<Func<TObj, TProperty>> propertyLambda)
        {
            Type type = typeof(TObj);
            if (!(propertyLambda.Body is MemberExpression member))
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType ?? typeof(object)))
                throw new ArgumentException($"Expression '{propertyLambda.ToString()}' refers to a property that is not from type {type}.");

            return propInfo;
        }

        public static string ToSqlValue(object value)
        {
            if (value == null)
                return "NULL";
            if (value is string valueStr)
                return "'" + valueStr.Replace("'", "''") + "'";
            if (value is DateTime valueDt)
                return "'" + valueDt.ToString("yyyy-MM-dd HH:mm:ss") + "'";

            if (CompilerTypes.Numeric.Contains(value.GetType()))
                return value.ToString();

            if (value is byte[] valueBytes)
                return string.Join("", valueBytes.Select(b => b.ToString("X2")));

            return "'" + value.ToString().Replace("'", "''") + "'";
        }
    }
}