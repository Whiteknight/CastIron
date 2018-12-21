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
    public class PropertyAndConstructorMapCompiler : IMapCompiler
    {
        // TODO: Nested objects. Columns starting with <NAME>_* will be used to populate the fields of property <NAME>

        private static readonly ConstructorInfo _exceptionConstructor = typeof(Exception).GetConstructor(new[] { typeof(string) });

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext<T> context)
        {
            Assert.ArgumentNotNull(context, nameof(context));

            context.PopulateColumnLookups(context.Reader);

            // Prepare the list of expressions
            WriteInstantiationExpressionForObjectInstance<T>(context);
            WritePropertyAssignmentExpressions(context);

            // Add return statement to return the converted instance
            context.AddStatement(Expression.Convert(context.Instance, typeof(T)));

            return context.CompileLambda<T>();
        }

        private void WriteInstantiationExpressionForObjectInstance<T>(MapCompileContext<T> context)
        {
            if (context.Factory != null)
            {
                WriteFactoryMethodCallExpression(context.Factory, context);
                return;
            }

            var constructorCall = WriteConstructorCallExpression(context);
            context.AddStatement(Expression.Assign(context.Instance, constructorCall));
        }

        private static void WriteFactoryMethodCallExpression<T>(Func<T> factory, MapCompileContext context)
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

        private static NewExpression WriteConstructorCallExpression( MapCompileContext context)
        {
            var constructor = context.GetConstructor();
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

        private static void WritePropertyAssignmentExpressions(MapCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific, context);
            foreach (var property in properties)
                WritePropertyAssignmentExpression(context, property);
        }

        private static void WritePropertyAssignmentExpression(MapCompileContext context, PropertyInfo property)
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

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type specific, MapCompileContext context)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate)
                .Where(p => !context.IsMapped(p.Name.ToLowerInvariant()));
        }
    }
}