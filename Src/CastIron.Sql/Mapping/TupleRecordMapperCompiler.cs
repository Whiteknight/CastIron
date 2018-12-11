using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class TupleRecordMapperCompiler : IRecordMapperCompiler
    {
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));

            var tupleType = typeof(T);
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var instance = Expression.Variable(tupleType, "instance");
            var context = new DataRecordMapperCompileContext(reader, recordParam, instance, typeof(T), tupleType);

            var typeParams = tupleType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw new Exception($"Cannot create a tuple with {typeParams.Length} parameters. Must be between 1 and 7");
            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw new Exception($"Cannot find factory method for type {tupleType.Name}");
            var args = new Expression[typeParams.Length];
            for (var i = 0; i < typeParams.Length; i++)
                args[i] = DataRecordExpressions.GetConversionExpression(i, context, typeParams[i]);

            context.AddStatement(Expression.Assign(instance, Expression.Call(null, factoryMethod, args)));
            context.AddStatement(Expression.Convert(instance, typeof(T)));
            return context.CompileLambda<T>();
        }

        public static bool IsMatchingType(Type parentType, object factory, object preferredConstructor)
        {
            return parentType.Namespace == "System" && parentType.Name.StartsWith("Tuple") && factory == null && preferredConstructor == null;
        }
    }
}