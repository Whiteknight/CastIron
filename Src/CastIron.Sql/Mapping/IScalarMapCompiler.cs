using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public interface IScalarMapCompiler
    {
        bool CanMap(Type targetType, ColumnInfo column);
        Expression Map(Type targetType, ColumnInfo column, ParameterExpression rawVar);
    }
}