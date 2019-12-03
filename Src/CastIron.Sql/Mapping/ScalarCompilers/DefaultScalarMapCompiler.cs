using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.ScalarCompilers
{
    /// <summary>
    /// Compile an expression to return the default value of the target type, when no other conversion rule
    /// can be determined
    /// </summary>
    public class DefaultScalarMapCompiler : IScalarMapCompiler
    {
        // There is no conversion rule, so just return a default value.

        public bool CanMap(Type targetType, Type columnType, string sqlTypeName) => true;

        public Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar)
            => targetType.GetDefaultValueExpression();
    }
}