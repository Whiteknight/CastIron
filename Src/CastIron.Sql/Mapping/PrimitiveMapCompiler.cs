using System;
using System.Data;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class PrimitiveMapCompiler : IMapCompiler
    {
        private readonly int _columnIndex;

        public PrimitiveMapCompiler(int columnIndex = 0)
        {
            _columnIndex = columnIndex;
        }

        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext<T> context)
        {
            Assert.ArgumentNotNull(context, nameof(context));
            var t = typeof(T);
            if (DataRecordExpressions.IsSupportedPrimitiveType(t))
            {
                var expr = DataRecordExpressions.GetConversionExpression(_columnIndex, context, context.Parent);
                context.AddStatement(expr);
                return context.CompileLambda<T>();
            }

            if (DataRecordExpressions.IsSupportedCollectionType(t))
            {
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