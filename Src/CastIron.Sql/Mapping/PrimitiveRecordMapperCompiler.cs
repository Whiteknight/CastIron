using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public class PrimitiveRecordMapperCompiler : IRecordMapperCompiler
    {
        private readonly int _columnIndex;

        public PrimitiveRecordMapperCompiler(int columnIndex = 0)
        {
            _columnIndex = columnIndex;
        }

        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            var t = typeof(T);
            if (DataRecordExpressions.IsSupportedPrimitiveType(t))
            {
                var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
                var context = new DataRecordMapperCompileContext(reader, recordParam, null, typeof(T), typeof(T));

                var expr = DataRecordExpressions.GetConversionExpression(_columnIndex, context, context.Parent);
                context.AddStatement(expr);
                return context.CompileLambda<T>();
            }

            if (DataRecordExpressions.IsSupportedCollectionType(t))
            {
                var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
                var context = new DataRecordMapperCompileContext(reader, recordParam, null, typeof(T), typeof(T));

                var expr = DataRecordExpressions.GetConversionExpression(context, context.Parent);
                context.AddStatement(expr);
                return context.CompileLambda<T>();
            }

            return r => default(T);
        }

        public static bool IsMatchingType(Type t)
        {
            return DataRecordExpressions.IsSupportedPrimitiveType(t) || DataRecordExpressions.IsSupportedCollectionType(t);
        }
    }
}