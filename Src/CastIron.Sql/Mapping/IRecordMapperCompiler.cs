using System;
using System.Data;

namespace CastIron.Sql.Mapping
{
    public interface IRecordMapperCompiler
    {
        Func<IDataRecord, T> CompileExpression<T>(IDataReader reader);
    }
}