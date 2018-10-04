using System;
using System.Data;

namespace CastIron.Sql.Mapping
{
    public interface IRecordMapperCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(IDataReader reader);
        Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory);
    }
}