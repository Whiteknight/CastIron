using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public class MapCompiler : IMapCompiler
    {
        private static readonly ConstructorInfo _exceptionConstructor = typeof(Exception).GetConstructor(new[] { typeof(string) });

        // TODO: Ability to take the IDataRecord in the factory and consume some columns for constructor params so they aren't used later for properties?
        public Func<IDataRecord, T> CompileExpression<T>(MapCompileContext context)
        {
            Assert.ArgumentNotNull(context, nameof(context));

            var targetType = context.Specific;
            context.PopulateColumnLookups(context.Reader);

            if (targetType == typeof(object))
            {
                var expr = GetConcreteDictionaryConversionExpression(context, null, typeof(Dictionary<string, object>));
                context.AddStatement(Expression.Convert(expr, typeof(T)));
                return context.CompileLambda<T>();
            }
            if (IsObjectArrayType(targetType))
                return MapObjectArray<T>;

            if (IsTupleType(targetType, context.Factory))
                return CompileTupleExpression<T>(context, context.Specific);

            var expression = GetConversionExpression(context, null, targetType, null);
            context.AddStatement(Expression.Convert(expression, targetType));

            return context.CompileLambda<T>();
        }

        public Func<IDataRecord, T> CompileTupleExpression<T>(MapCompileContext context, Type tupleType)
        {
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
                args[i] = DataRecordExpressions.GetScalarConversionExpression(i, context, context.Reader.GetFieldType(i), typeParams[i]);

            context.AddStatement(Expression.Assign(context.Instance, Expression.Call(null, factoryMethod, args)));
            context.AddStatement(Expression.Convert(context.Instance, typeof(T)));
            return context.CompileLambda<T>();
        }

        private static T MapObjectArray<T>(IDataRecord r)
        {
            var buffer = new object[r.FieldCount];
            r.GetValues(buffer);
            return (T)(object)buffer;
        }

        private static bool IsTupleType(Type parentType, object factory)
        {
            return parentType.Namespace == "System" && parentType.Name.StartsWith("Tuple") && factory == null;
        }

        private static bool IsObjectArrayType(Type t)
        {
            return t == null
                   || t == typeof(object[])
                   || t == typeof(IEnumerable<object>) || t == typeof(IList<object>) || t == typeof(IReadOnlyList<object>) || t == typeof(ICollection<object>)
                   || t == typeof(IEnumerable) || t == typeof(IList) || t == typeof(ICollection);
        }

        private static void WriteInstantiationExpressionForObjectInstance(MapCompileContext context)
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
                Expression.Convert(Expression.Call(Expression.Constant(factory.Target), factory.Method), context.Specific)));            
            context.AddStatement(Expression.IfThen(
                Expression.Equal(context.Instance, Expression.Constant(null)),
                Expression.Throw(
                    Expression.New(
                        _exceptionConstructor, 
                        Expression.Constant($"Provided factory method returned a null or unusable value for type {context.Specific.Name}")))));
        }

        private static NewExpression WriteConstructorCallExpression(MapCompileContext context)
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
                    args[i] = GetConversionExpression(context, parameter.Name.ToLowerInvariant(), parameter.ParameterType, parameter);
                    continue;
                }

                if (parameter.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                {
                    args[i] = GetConversionExpression(context, parameter.Name.ToLowerInvariant(), parameter.ParameterType, parameter);
                    continue;
                }

                // No matching column name, fill in the default value
                args[i] = Expression.Convert(Expression.Constant(DataRecordExpressions.GetDefaultValue(parameter.ParameterType)), parameter.ParameterType);
            }

            return Expression.New(constructor, args);
        }

        private static void WritePropertyAssignmentExpressions(MapCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific);
            foreach (var property in properties)
            {
                var expression = GetConversionExpression(context, property.Name.ToLowerInvariant(), property.PropertyType, property);
                context.AddStatement(Expression.Call(context.Instance, property.GetSetMethod(), Expression.Convert(expression, property.PropertyType)));
            }
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type specific)
        {
            return specific.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.GetMethod != null)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.GetMethod.IsPrivate && !p.SetMethod.IsPrivate);
        }

        private static Expression GetConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            if (targetType == typeof(object))
                return GetArrayConversionExpression(context, null, typeof(object[]), attrs);

            // TODO: If we're asking for "object", should we just map a single column as a scalar or should we map to 
            // Dictionary<string, object> like we do at the top level?
            if (DataRecordExpressions.IsMappableScalarType(targetType))
            {
                var firstColumn = context.GetColumn(name);
                if (firstColumn == null)
                    return Expression.Constant(DataRecordExpressions.GetDefaultValue(targetType));
                firstColumn.MarkMapped();
                return DataRecordExpressions.GetScalarConversionExpression(firstColumn.Index, context, context.Reader.GetFieldType(firstColumn.Index), targetType);
            }

            if (targetType.IsArray && targetType.HasElementType)
                return GetArrayConversionExpression(context, name, targetType, attrs);

            if (IsConcreteDictionaryType(targetType))
                return GetConcreteDictionaryConversionExpression(context, name, targetType);
            if (IsDictionaryInterfaceType(targetType))
                return GetDictionaryInterfaceConversionExpression(context, name, targetType);

            if (targetType.IsGenericType && targetType.IsInterface && (targetType.Namespace ?? string.Empty).StartsWith("System.Collections"))
                return GetInterfaceCollectionConversionExpression(context, name, targetType, attrs);

            if (IsConcreteCollectionType(targetType))
                return GetConcreteCollectionConversionExpression(context, name, targetType, attrs);
        
            if (IsMappableCustomObjectType(targetType))
            {
                var contextToUse = name == null ? context : context.CreateSubcontext(targetType, name);
                WriteInstantiationExpressionForObjectInstance(contextToUse);
                WritePropertyAssignmentExpressions(contextToUse);

                return Expression.Convert(contextToUse.Instance, targetType);
            }

            throw new Exception($"Cannot find conversion rule for type {targetType.Name}");
        }

        // It is Dictionary<string,X> or is concrete and inherits from IDictionary<string,X>
        private static bool IsConcreteDictionaryType(Type t)
        {
            if (t.IsInterface || t.IsAbstract)
                return false;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>) && t.GenericTypeArguments[0] == typeof(string))
                return true;
            var dictType = t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string));
            return dictType != null;
        }

        // Is one of IDictionary<string,X> or IReadOnlyDictionary<string,X>
        private static bool IsDictionaryInterfaceType(Type t)
        {
            if (!t.IsGenericType && !t.IsInterface)
                return false;
            var genericDef = t.GetGenericTypeDefinition();
            return (genericDef == typeof(IDictionary<,>) || genericDef == typeof(IReadOnlyDictionary<,>)) && t.GenericTypeArguments[0] == typeof(string);
                
        }

        private static bool IsConcreteCollectionType(Type t)
        {
            return !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        private static bool IsMappableCustomObjectType(Type t)
        {
            return t.IsClass
                   && !t.IsInterface
                   && !t.IsAbstract
                   && !t.IsArray
                   && t != typeof(string)
                   && !(t.Namespace ?? string.Empty).StartsWith("System.Collections");
        }

        private static Expression GetConcreteDictionaryConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var idictType = targetType.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string));
            if (idictType == null)
                throw new Exception($"Dictionary type {targetType.Name} does not inherit from IDictionary<string,>");
            var elementType = idictType.GetGenericArguments()[1];

            var dictType = typeof(IDictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!dictType.IsAssignableFrom(targetType))
                throw new Exception($"Cannot convert from requested type {targetType.Name} to dictionary type {dictType.Name}");

            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new Exception($"Collection type {targetType.FullName} must have a default parameterless constructor");

            var addMethod = dictType.GetMethod(nameof(IDictionary<string, object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(IDictionary<string, object>.Add)}() Method missing from dictionary type {targetType.Name}");

            var dictVar = context.AddVariable(targetType, "dict");
            context.AddStatement(Expression.Assign(dictVar, Expression.New(constructor)));

            if (name == null)
            {
                var columns = context.GetFirstIndexForEachColumnName();
                foreach (var column in columns)
                {
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(column.OriginalName), getScalarExpression));
                    column.MarkMapped();
                }
            }
            else
            {
                var subcontext = context.CreateSubcontext(null, name);
                var columns = subcontext.GetColumns(null).ToList();
                foreach (var column in columns)
                {
                    var childName = column.OriginalName.Substring(name.Length + 1);
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(childName), getScalarExpression));
                    column.MarkMapped();
                }
            }

            return Expression.Convert(dictVar, targetType);
        }

        private static Expression GetDictionaryInterfaceConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var elementType = targetType.GetGenericArguments()[1];

            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
            if (!targetType.IsAssignableFrom(dictType))
                throw new Exception($"Cannot map to object of type {targetType.FullName}. Expected Dictionary<string, {elementType.Name}>.");

            var constructor = dictType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new Exception($"Collection type {dictType.FullName} must have a default parameterless constructor");

            var addMethod = dictType.GetMethod(nameof(Dictionary<string, object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(Dictionary<string, object>.Add)}() Method missing from dictionary type {targetType.FullName}");

            var dictVar = context.AddVariable(dictType, "dict");
            context.AddStatement(Expression.Assign(dictVar, Expression.New(constructor)));

            if (name == null)
            {
                var columns = context.GetFirstIndexForEachColumnName();
                foreach (var column in columns)
                {
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(column.OriginalName), getScalarExpression));
                    column.MarkMapped();
                }
            }
            else
            {
                var subcontext = context.CreateSubcontext(null, name);
                var columns = subcontext.GetColumns(null).ToList();
                foreach (var column in columns)
                {
                    var childName = column.OriginalName.Substring(name.Length + 1);
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(childName), getScalarExpression));
                    column.MarkMapped();
                }
            }

            return targetType == dictType ? (Expression)dictVar : Expression.Convert(dictVar, targetType);
        }

        private static Expression GetConcreteCollectionConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            if (targetType.GenericTypeArguments.Length != 1)
                throw new Exception($"Cannot map to object of type {targetType.FullName}");
            var elementType = targetType.GenericTypeArguments[0];

            var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
            if (!collectionType.IsAssignableFrom(targetType))
                throw new Exception($"Cannot map to object of type {targetType.FullName}. Expected ICollection<{elementType.Name}>.");

            var constructor = targetType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new Exception($"Collection type {targetType.FullName} must have a default parameterless constructor");

            var addMethod = collectionType.GetMethod(nameof(ICollection<object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(ICollection<object>.Add)}() Method missing from collection type {targetType.FullName}");

            var listVar = context.AddVariable(targetType, "list");
            context.AddStatement(Expression.Assign(listVar, Expression.New(constructor)));

            if (DataRecordExpressions.IsMappableScalarType(elementType))
            {
                var columns = GetColumnsForProperty(context, name, attrs);
                foreach (var column in columns)
                {
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(listVar, addMethod, getScalarExpression));
                    column.MarkMapped();
                }
                return listVar;
            }

            if (IsMappableCustomObjectType(elementType))
            {
                var subcontext = context.CreateSubcontext(elementType, name);

                WriteInstantiationExpressionForObjectInstance(subcontext);
                WritePropertyAssignmentExpressions(subcontext);

                context.AddStatement(Expression.Call(listVar, addMethod, subcontext.Instance));
                return listVar;
            }

            throw new Exception($"Cannot map collection of type {targetType.Name}");
        }

        private static Expression GetInterfaceCollectionConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            if (targetType.GenericTypeArguments.Length != 1)
                throw new Exception($"Cannot map to object of type {targetType.FullName}");

            var elementType = targetType.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            if (!targetType.IsAssignableFrom(listType))
                throw new Exception($"Cannot map to object of type {targetType.FullName}. Must be compatible with List<{elementType.Name}>.");

            var constructor = listType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw new Exception($"Collection type {listType.FullName} must have a default parameterless constructor");

            var addMethod = listType.GetMethod(nameof(List<object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(List<object>.Add)}() Method missing from collection type {listType.FullName}");

            var listVar = context.AddVariable(listType, "list");
            context.AddStatement(Expression.Assign(listVar, Expression.New(constructor)));

            if (DataRecordExpressions.IsMappableScalarType(elementType))
            {
                var columns = GetColumnsForProperty(context, name, attrs);
                foreach (var column in columns)
                {
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Call(listVar, addMethod, getScalarExpression));
                    column.MarkMapped();
                }
                return Expression.Convert(listVar, targetType);
            }

            if (IsMappableCustomObjectType(elementType))
            {
                var subcontext = context.CreateSubcontext(elementType, name);

                WriteInstantiationExpressionForObjectInstance(subcontext);
                WritePropertyAssignmentExpressions(subcontext);

                context.AddStatement(Expression.Call(listVar, addMethod, subcontext.Instance));
                return Expression.Convert(listVar, targetType);
            }

            throw new Exception($"Cannot map Property '{name}' with type {targetType.Name}");
        }

        private static string GetColumnNameFromProperty(MapCompileContext context, string name, ICustomAttributeProvider attrs)
        {
            var propertyName = name.ToLowerInvariant();
            if (context.HasColumn(propertyName))
                return propertyName;
            var columnName = attrs.GetTypedAttributes<ColumnAttribute>()
                .Select(c => c.Name.ToLowerInvariant())
                .FirstOrDefault();
            if (context.HasColumn(columnName))
                return columnName;
            var acceptsUnnamed = attrs.GetTypedAttributes<UnnamedColumnsAttribute>().Any();
            if (acceptsUnnamed)
                return string.Empty;
            return null;
        }

        private static IEnumerable<ColumnInfo> GetColumnsForProperty(MapCompileContext context, string name, ICustomAttributeProvider attrs)
        {
            if (name == null)
                return context.GetColumns(null);
            var columnName = GetColumnNameFromProperty(context, name, attrs);
            if (columnName == null)
                return Enumerable.Empty<ColumnInfo>();
            return context.GetColumns(columnName);
        }

        private static Expression GetArrayConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            var elementType = targetType.GetElementType();
            if (elementType == null)
                throw new Exception($"Cannot find element type for arraytype {targetType.Name}");

            var constructor = targetType.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
                throw new Exception($"Array type {targetType.FullName} must have a standard array constructor");

            if (DataRecordExpressions.IsMappableScalarType(elementType))
            {
                var columns = GetColumnsForProperty(context, name, attrs).ToList();
                var arrayVar = context.AddVariable(targetType, "array_" + context.GetNextVarNumber());
                context.AddStatement(Expression.Assign(arrayVar, Expression.New(constructor, Expression.Constant(columns.Count))));
                for (var i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var getScalarExpression = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, elementType);
                    context.AddStatement(Expression.Assign(Expression.ArrayAccess(arrayVar, Expression.Constant(i)), getScalarExpression));
                    column.MarkMapped();
                }
                return arrayVar;
            }

            if (IsMappableCustomObjectType(elementType))
            {
                var subcontext = context.CreateSubcontext(elementType, name);
                var arrayVar = subcontext.AddVariable(targetType, "array");
                context.AddStatement(Expression.Assign(arrayVar, Expression.New(constructor, Expression.Constant(1))));

                WriteInstantiationExpressionForObjectInstance(subcontext);
                WritePropertyAssignmentExpressions(subcontext);
                context.AddStatement(Expression.Assign(Expression.ArrayAccess(arrayVar, Expression.Constant(0)), Expression.Convert(subcontext.Instance, elementType)));

                return arrayVar;
            }

            throw new Exception($"Cannot map array of type {elementType.Name}[]");
        }
    }
}