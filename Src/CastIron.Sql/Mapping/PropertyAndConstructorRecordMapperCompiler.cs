using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class PropertyAndConstructorRecordMapperCompiler : IRecordMapperCompiler
    {
        private readonly ConstructorFinder _constructorFinder;

        public PropertyAndConstructorRecordMapperCompiler()
        {
            _constructorFinder = new ConstructorFinder();
        }

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(Type specific, IDataReader reader, Func<T> factory, ConstructorInfo preferredConstructor)
        {
            Assert.ArgumentNotNull(reader, nameof(reader));
            specific = specific ?? typeof(T);
            if (factory != null && preferredConstructor != null)
                throw new Exception($"May specify at most one of {nameof(factory)} or {nameof(preferredConstructor)}");

            var context = CreateCompileContextForObjectInstance<T>(specific, reader);

            // Prepare the list of expressions
            WriteInstantiationExpressionForObjectInstance(factory, preferredConstructor, context);
            WritePropertyAssignmentExpressions(context);

            // Add return statement to return the converted instance
            context.AddStatement(Expression.Convert(context.Instance, typeof(T)));

            return context.CompileLambda<T>();
        }

        private static DataRecordMapperCompileContext CreateCompileContextForObjectInstance<T>(Type specific, IDataReader reader)
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var instance = Expression.Variable(specific, "instance");
            var context = new DataRecordMapperCompileContext(reader, recordParam, instance, typeof(T), specific);
            context.PopulateColumnLookups(reader);
            return context;
        }

        private void WriteInstantiationExpressionForObjectInstance<T>(Func<T> factory, ConstructorInfo preferredConstructor, DataRecordMapperCompileContext context)
        {
            if (factory != null)
            {
                context.AddStatement(Expression.Assign(context.Instance, Expression.Call(Expression.Constant(factory.Target), factory.Method)));
                // TODO: If the factory returns null, should we skip the record or throw an exception?
                var exceptionConstructor = typeof(Exception).GetConstructor(new[] {typeof(string)});
                if (exceptionConstructor == null)
                    return;
                context.AddStatement(Expression.IfThen(
                    Expression.Equal(context.Instance, Expression.Constant(null)), 
                    Expression.Throw(Expression.New(exceptionConstructor, Expression.Constant("Provided factory method returned a null value")))));
                return;
            }

            var constructor = _constructorFinder.FindBestMatch(preferredConstructor, context.Specific, context);
            var constructorCall = CreateConstructorCallExpression(constructor, context);
            context.AddStatement(Expression.Assign(context.Instance, constructorCall));
        }

        private static void WritePropertyAssignmentExpressions(DataRecordMapperCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific, context);
            foreach (var property in properties)
            {
                var columnName = property.Name.ToLowerInvariant();
                var idx = context.GetColumnIndex(columnName);
                if (idx < 0)
                {
                    columnName = property
                        .GetCustomAttributes(typeof(ColumnAttribute), true)
                        .Cast<ColumnAttribute>()
                        .Select(c => c.Name.ToLowerInvariant())
                        .FirstOrDefault();
                    idx = context.GetColumnIndex(columnName);
                }
                if (idx < 0)
                    continue;

                // Get the index by ColumnAttribute.Name
                var conversion = DataRecordExpressions.GetConversionExpression(columnName, context, property.PropertyType);
                context.AddStatement(Expression.Call(context.Instance, property.GetSetMethod(), conversion));
            }
        }

        private static NewExpression CreateConstructorCallExpression(ConstructorInfo constructor, DataRecordMapperCompileContext context)
        {
            var parameters = constructor.GetParameters();
            // Default parameterless constructor, just call it
            if (parameters.Length == 0)
                return Expression.New(constructor);

            // Map columns to constructor parameters by name, marking the columns consumed so they aren't used again later
            var args = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                if (!context.HasColumn(name))
                {
                    // The user has specified a constructor where a parameter does not correspond to a column 
                    // Fill in the default value and hope the user knows what they are doing.
                    args[i] = Expression.Constant(DataRecordExpressions.GetDefaultValue(parameter.ParameterType));
                    continue;
                }

                // Get the column index and mark the column as being mapped
                context.MarkMapped(name);

                args[i] = DataRecordExpressions.GetConversionExpression(name, context, parameter.ParameterType);
            }

            return Expression.New(constructor, args);
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type specific, DataRecordMapperCompileContext context)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate)
                .Where(p => !context.IsMapped(p.Name.ToLowerInvariant()));
        }
    }
}