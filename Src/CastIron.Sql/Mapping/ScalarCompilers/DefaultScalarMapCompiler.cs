using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    public class DefaultScalarMapCompiler : IScalarMapCompiler
    {
        // There is no conversion rule, so just return a default value.

        public bool CanMap(Type targetType, ColumnInfo column) => true;

        public Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar)
            => targetType.GetDefaultValueExpression();
    }
}