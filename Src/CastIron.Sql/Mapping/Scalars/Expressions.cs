using System;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.Scalars
{
    public static class Expressions
    {
        public static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));
    }
}
