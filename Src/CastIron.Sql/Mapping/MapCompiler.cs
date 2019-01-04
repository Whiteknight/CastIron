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

            // For all other cases, fall back to normal recursive conversion routines.
            var expression = GetConversionExpression(context, null, targetType, null);
            if (expression == null)
                expression = DataRecordExpressions.GetDefaultValueExpression(typeof(T));
            context.AddStatement(Expression.Convert(expression, targetType));
            return context.CompileLambda<T>();
        }

        private static bool IsTupleType(Type parentType, object factory)
        {
            return parentType.Namespace == "System" && parentType.Name.StartsWith("Tuple") && factory == null;
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

        private static Expression GetConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            if (targetType == typeof(object))
                return GetObjectConversionExpression(context, name, targetType, attrs);
            if (DataRecordExpressions.IsSupportedPrimitiveType(targetType))
                return GetPrimitiveConversionExpression(context, name, targetType);

            if (targetType.IsArray && targetType.HasElementType)
                return GetArrayConversionExpression(context, name, targetType, attrs);
            if (targetType == typeof(IEnumerable) || targetType == typeof(IList) || targetType == typeof(ICollection))
                return GetArrayConversionExpression(context, name, typeof(object[]), attrs);

            // TODO: For Collection and Dictionary types, if the collection or dictionary already exists, add to it in-place instead of creating new and assigning.
            if (IsConcreteDictionaryType(targetType))
                return GetConcreteDictionaryConversionExpression(context, name, targetType);
            if (IsDictionaryInterfaceType(targetType))
                return GetDictionaryInterfaceConversionExpression(context, name, targetType);

            if (targetType.IsGenericType && targetType.IsInterface && (targetType.Namespace ?? string.Empty).StartsWith("System.Collections"))
                return GetInterfaceCollectionConversionExpression(context, name, targetType, attrs);

            if (IsConcreteCollectionType(targetType))
                return GetConcreteCollectionConversionExpression(context, name, targetType, attrs);

            if (IsTupleType(targetType, null))
                return GetTupleConversionExpression(context, name, targetType);

            if (IsMappableCustomObjectType(targetType))
                return GetCustomObjectConversionExpression(context, name, targetType);

            return null;
        }

        private static Expression GetCustomObjectConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var contextToUse = name == null ? context : context.CreateSubcontext(targetType, name);
            AddInstantiationExpressionForObjectInstance(contextToUse);
            AddPropertyAssignmentExpressions(contextToUse);

            return Expression.Convert(contextToUse.Instance, targetType);
        }

        private static Expression GetPrimitiveConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var firstColumn = context.GetColumn(name);
            if (firstColumn == null)
                return null;
            firstColumn.MarkMapped();
            return DataRecordExpressions.GetScalarConversionExpression(firstColumn.Index, context, context.Reader.GetFieldType(firstColumn.Index), targetType);
        }

        private static Expression GetObjectConversionExpression(MapCompileContext context, string name, Type targetType, ICustomAttributeProvider attrs)
        {
            // At the top-level (name==null) we convert to dictionary to preserve column name information
            if (name == null)
            {
                var dictExpr = GetConcreteDictionaryConversionExpression(context, null, typeof(Dictionary<string, object>));
                return Expression.Convert(dictExpr, typeof(object));
            }
            
            // At the nested level (name!=null) we convert to a scalar (n==1) or an array (n>1) because name is always the same
            var numColumns = context.GetColumns(name).Count();
            if (numColumns == 0)
                return Expression.Convert(Expression.Constant(null), typeof(object));
            if (numColumns == 1)
            {
                var firstColumn = context.GetColumn(name);
                firstColumn.MarkMapped();
                return DataRecordExpressions.GetScalarConversionExpression(firstColumn.Index, context, context.Reader.GetFieldType(firstColumn.Index), targetType);
            }

            var expr = GetArrayConversionExpression(context, name, typeof(object[]), attrs);
            return Expression.Convert(expr, typeof(object));
        }

        private static Expression GetTupleConversionExpression(MapCompileContext context, string name, Type targetType)
        {
            var columns = context.GetColumns(name).ToList();
            return GetTupleConversionExpression(context, targetType, columns);
        }

        private static Expression GetTupleConversionExpression(MapCompileContext context, Type targetType, IReadOnlyList<ColumnInfo> columns)
        {
            var typeParams = targetType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw new Exception($"Cannot create a tuple with {typeParams.Length} parameters. Must be between 1 and 7");
            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw new Exception($"Cannot find factory method for type {targetType.Name}");
            var args = new Expression[typeParams.Length];

            for (var i = 0; i < typeParams.Length; i++)
            {
                var parameterType = typeParams[i];
                if (i >= columns.Count)
                {
                    args[i] = DataRecordExpressions.GetDefaultValueExpression(parameterType);
                    continue;
                }

                var column = columns[i];
                args[i] = DataRecordExpressions.GetScalarConversionExpression(column.Index, context, column.ColumnType, parameterType);
            }
            return Expression.Call(null, factoryMethod, args);
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
                    var getScalarExpression = GetConversionExpression(context, column.CanonicalName, elementType, null);
                    if (getScalarExpression == null)
                        continue;
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(column.OriginalName), getScalarExpression));
                    column.MarkMapped();
                }
            }
            else
            {
                var subcontext = context.CreateSubcontext(null, name);
                var columns = subcontext.GetFirstIndexForEachColumnName();
                foreach (var column in columns)
                {
                    var keyName = column.OriginalName.Substring(name.Length + 1);
                    var childName = column.CanonicalName.Substring(name.Length + 1);
                    var getScalarExpression = GetConversionExpression(subcontext, childName, elementType, null);
                    if (getScalarExpression == null)
                        continue;
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(keyName), getScalarExpression));
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
                    var getScalarExpression = GetConversionExpression(context, column.CanonicalName, elementType, null);
                    if (getScalarExpression == null)
                        continue;
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(column.OriginalName), getScalarExpression));
                }
            }
            else
            {
                var subcontext = context.CreateSubcontext(null, name);
                var columns = subcontext.GetFirstIndexForEachColumnName();
                foreach (var column in columns)
                {
                    var key = column.OriginalName.Substring(name.Length + 1);
                    var childName = column.CanonicalName.Substring(name.Length + 1);
                    var getScalarExpression = GetConversionExpression(subcontext, childName, elementType, null);
                    context.AddStatement(Expression.Call(dictVar, addMethod, Expression.Constant(key), getScalarExpression));
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

                AddInstantiationExpressionForObjectInstance(subcontext);
                AddPropertyAssignmentExpressions(subcontext);

                context.AddStatement(Expression.Call(listVar, addMethod, subcontext.Instance));
                return listVar;
            }

            // TODO: Is it worthwhile to recurse here? 

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

                AddInstantiationExpressionForObjectInstance(subcontext);
                AddPropertyAssignmentExpressions(subcontext);

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
            if (attrs == null)
                return null;
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

                AddInstantiationExpressionForObjectInstance(subcontext);
                AddPropertyAssignmentExpressions(subcontext);
                context.AddStatement(Expression.Assign(Expression.ArrayAccess(arrayVar, Expression.Constant(0)), Expression.Convert(subcontext.Instance, elementType)));

                return arrayVar;
            }

            throw new Exception($"Cannot map array of type {elementType.Name}[]");
        }

        private static void AddInstantiationExpressionForObjectInstance(MapCompileContext context)
        {
            if (context.Factory != null)
            {
                AddFactoryMethodCallExpression(context.Factory, context);
                return;
            }
            var constructorCall = GetConstructorCallExpression(context);
            context.AddStatement(Expression.Assign(context.Instance, constructorCall));
        }

        private static void AddFactoryMethodCallExpression<T>(Func<T> factory, MapCompileContext context)
        {
            context.AddStatement(Expression.Assign(
                context.Instance,
                Expression.Convert(
                    Expression.Call(
                        Expression.Constant(factory.Target), 
                        factory.Method), 
                    context.Specific)));
            context.AddStatement(Expression.IfThen(
                Expression.Equal(context.Instance, Expression.Constant(null)),
                Expression.Throw(
                    Expression.New(
                        _exceptionConstructor,
                        Expression.Constant($"Provided factory method returned a null or unusable value for type {context.Specific.Name}")))));
        }

        private static NewExpression GetConstructorCallExpression(MapCompileContext context)
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
                    var expr = GetConversionExpression(context, parameter.Name.ToLowerInvariant(), parameter.ParameterType, parameter);
                    if (expr != null)
                    {
                        args[i] = expr;
                        continue;
                    }
                }

                if (parameter.GetCustomAttributes<UnnamedColumnsAttribute>().Any())
                {
                    var expr = GetConversionExpression(context, parameter.Name.ToLowerInvariant(), parameter.ParameterType, parameter);
                    if (expr != null)
                    {
                        args[i] = expr;
                        continue;
                    }
                }

                // No matching column name, fill in the default value
                args[i] = DataRecordExpressions.GetDefaultValueExpression(parameter.ParameterType);
            }

            return Expression.New(constructor, args);
        }

        private static void AddPropertyAssignmentExpressions(MapCompileContext context)
        {
            var properties = GetMappableProperties(context.Specific);
            foreach (var property in properties)
            {
                var expression = GetConversionExpression(context, property.Name.ToLowerInvariant(), property.PropertyType, property);
                if (expression == null)
                    continue;
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
    }
}