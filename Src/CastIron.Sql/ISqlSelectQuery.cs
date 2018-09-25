using System;
using System.Collections.Generic;

namespace CastIron.Sql
{
    public interface ISqlSelectQuery<T> : ISqlQuery<IEnumerable<T>>
    {
        ISqlSelectQuery<T> Where(Action<ISelectWhereClauseBuilder<T>> build);
    }
}