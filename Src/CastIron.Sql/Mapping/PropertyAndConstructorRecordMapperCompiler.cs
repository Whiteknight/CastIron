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
        // TODO: Nested objects. Columns starting with <NAME>_* will be used to populate the fields of property <NAME>
        // TODO: Need a way to dump contents of all columns which aren't explicitly mapped to an ICollection<object> or similar

        private static readonly ConstructorInfo _exceptionConstructor = typeof(Exception).GetConstructor(new[] { typeof(string) });
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
                WriteFactoryMethodCallExpression(factory, context);
                return;
            }

            var constructor = _constructorFinder.FindBestMatch(preferredConstructor, context.Specific, context.GetColumnNameCounts());
            var constructorCall = WriteConstructorCallExpression(constructor, context);
            context.AddStatement(Expression.Assign(context.Instance, constructorCall));
        }

        private static void WriteFactoryMethodCallExpression<T>(Func<T> factory, DataRecordMapperCompileContext context)
        {
            context.AddStatement(Expression.Assign(
                context.Instance, 
                Expression.Call(Expression.Constant(factory.Target), factory.Method)));            
            context.AddStatement(Expression.IfThen(
                Expression.Equal(context.Instance, Expression.Constant(null)),
                Expression.Throw(
                    Expression.New(
                        _exceptionConstructor, 
                        Expression.Constant($"Provided factory method returned a null value for type {typeof(T).FullName}")))));
        }

        private static NewExpression WriteConstructorCallExpression(ConstructorInfo constructor, DataRecordMapperCompileContext context)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
                return Expression.New(constructor);

            // Map columns to constructor parameters by name, marking the columns consumed so they aren't used again later
            var args = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var name = parameter.Name.ToLowerInvariant();
                if (context.HasColumn(name))
                {
                    context.MarkMapped(name);
                    args[i] = DataRecordExpressions.GetConversionExpression(name, context, parameter.ParameterType);
                    continue;
                }

                if (parameter.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                {
                    context.MarkMapped("");
                    args[i] = DataRecordExpressions.GetConversionExpression("", context, parameter.ParameterType);
                    continue;
                }

                // No matching column name, fill in the default value
                args[i] = Expression.Convert(Expression.Constant(DataRecordExpressions.GetDefaultValue(parameter.ParameterType)), parameter.ParameterType);
            }

            return Expression.New(constructor, args);
        }

        private static void WritePropertyAssignmentExpressions(DataRecordMapperCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific, context);
            foreach (var property in properties)
                WritePropertyAssignmentExpression(context, property);
        }

        private static void WritePropertyAssignmentExpression(DataRecordMapperCompileContext context, PropertyInfo property)
        {
            // Look for a column name matching the property name
            var columnName = property.Name.ToLowerInvariant();
            if (context.HasColumn(columnName))
            {
                var conversion = DataRecordExpressions.GetConversionExpression(columnName, context, property.PropertyType);
                context.AddStatement(Expression.Call(context.Instance, property.GetSetMethod(), conversion));
                return;
            }

            // Look for a column name matching the ColumnNameAttribute.Name value
            columnName = property.GetTypedAttributes<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (context.HasColumn(columnName))
            {
                var conversion = DataRecordExpressions.GetConversionExpression(columnName, context, property.PropertyType);
                context.AddStatement(Expression.Call(context.Instance, property.GetSetMethod(), conversion));
                return;
            }

            // If the property is tagged with UnnamedColumnsAttribute, we use all unnamed columns
            var acceptsUnnamed = property.GetTypedAttributes<UnnamedColumnsAttribute>().Any();
            if (acceptsUnnamed)
            {
                var conversion = DataRecordExpressions.GetConversionExpression("", context, property.PropertyType);
                context.AddStatement(Expression.Call(context.Instance, property.GetSetMethod(), conversion));
            }
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