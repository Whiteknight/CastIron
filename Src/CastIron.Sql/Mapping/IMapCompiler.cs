using System;
using System.Data;

namespace CastIron.Sql.Mapping
{
    public interface IMapCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context);
    }
}