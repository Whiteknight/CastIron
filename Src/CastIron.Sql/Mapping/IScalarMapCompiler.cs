using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Compiler type to compile mapping expressions for a single type of primitive value
    /// </summary>
    public interface IScalarMapCompiler
    {
        bool CanMap(Type targetType, Type columnType, string sqlTypeName);
        Expression Map(Type targetType, Type columnType, string sqlTypeName, ParameterExpression rawVar);
    }
}