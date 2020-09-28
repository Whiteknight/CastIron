using System;
using System.Data;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Compiles a mapping lambda from a populated Context and a column list from IDataReader
    /// </summary>
    public interface IMapCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(MapContext operation, IDataReader reader);
    }
}