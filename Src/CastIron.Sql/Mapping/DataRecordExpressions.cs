using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping
{
    public static class DataRecordExpressions
    {
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(byte?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        private static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
        {
            // TODO: Are there any other types we can map from the DB?
            typeof(bool),
            typeof(bool?),
            typeof(byte),
            typeof(byte?),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(decimal),
            typeof(decimal?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(short),
            typeof(short?),
            typeof(string),
            typeof(uint),
            typeof(uint?),
            typeof(ulong),
            typeof(ulong?),
            typeof(ushort),
            typeof(ushort?),
        };

        private static readonly MethodInfo GetValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));
        private static readonly Expression _dbNullExp = Expression.Field(null, typeof(DBNull), nameof(DBNull.Value));

        public static bool IsSupportedPrimitiveType(Type t)
        {
            return _primitiveTypes.Contains(t);
        }

        public static bool IsNumericType(Type t)
        {
            return _numericTypes.Contains(t);
        }

        public static bool IsSupportedCollectionType(Type t)
        {
            if (t.IsArray && t.HasElementType)
                return _primitiveTypes.Contains(t.GetElementType());

            if (t.IsGenericType)
            {
                if (t.GenericTypeArguments.Length != 1)
                    return false;

                var elementType = t.GenericTypeArguments[0];
                if (!_primitiveTypes.Contains(elementType))
                    return false;

                var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
                if (!collectionType.IsAssignableFrom(t))
                    return false;
            }

            return true;
        }

        public static Expression GetConversionExpression(string columnName, DataRecordMapperCompileContext context, Type targetType)
        {
            if (targetType.IsArray && targetType.HasElementType)
            {
                var indices = GetIndices(columnName, context);
                return GetArrayConversionExpression(context, targetType, indices);
            }

            if (targetType.IsGenericType)
            {
                var indices = GetIndices(columnName, context);
                if (targetType.IsInterface)
                    return GetInterfaceCollectionConversionExpression(context, targetType, indices);

                return GetConcreteCollectionConversionExpression(context, targetType, indices);
            }

            var firstIndex = context.GetColumnIndex(columnName);
            return GetScalarConversionExpression(firstIndex, context, context.Reader.GetFieldType(firstIndex), targetType);
        }

        private static IReadOnlyList<int> GetIndices(string columnName, DataRecordMapperCompileContext context)
        {
            if (string.IsNullOrEmpty(columnName))
                return context.GetUnnamedColumnIndices();
            return context.GetColumnIndices(columnName);
        }

        private static Expression GetInterfaceCollectionConversionExpression(DataRecordMapperCompileContext context, Type targetType, IReadOnlyList<int> indices)
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
            var listVar = context.AddVariable(listType, "list_" + context.GetNextVarNumber());
            context.AddStatement(Expression.Assign(listVar, Expression.New(constructor)));

            foreach (var index in indices)
            {
                var getScalarExpression = GetScalarConversionExpression(index, context, context.Reader.GetFieldType(index), elementType);
                context.AddStatement(Expression.Call(listVar, addMethod, getScalarExpression));
            }

            return Expression.Convert(listVar, targetType);
        }

        private static Expression GetArrayConversionExpression(DataRecordMapperCompileContext context, Type targetType, IReadOnlyList<int> indices)
        {
            var elementType = targetType.GetElementType();

            var constructor = targetType.GetConstructor(new[] {typeof(int)});
            if (constructor == null)
                throw new Exception($"Array type {targetType.FullName} must have a standard array constructor");

            var arrayVar = context.AddVariable(targetType, "array_" + context.GetNextVarNumber());
            context.AddStatement(Expression.Assign(arrayVar, Expression.New(constructor, Expression.Constant(indices.Count))));

            for (int i = 0; i < indices.Count; i++)
            {
                var columnIndex = indices[i];
                var getScalarExpression = GetScalarConversionExpression(columnIndex, context, context.Reader.GetFieldType(columnIndex), elementType);
                context.AddStatement(Expression.Assign(Expression.ArrayAccess(arrayVar, Expression.Constant(i)), getScalarExpression));
            }

            return arrayVar;
        }

        private static Expression GetConcreteCollectionConversionExpression(DataRecordMapperCompileContext context, Type targetType, IReadOnlyList<int> indices)
        {
            if (targetType.GenericTypeArguments.Length != 1)
                throw new Exception($"Cannot map to object of type {targetType.FullName}");
            var elementType = targetType.GenericTypeArguments[0];
            var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
            if (!collectionType.IsAssignableFrom(targetType))
                throw new Exception($"Cannot map to object of type {targetType.FullName}. Expected ICollection<{elementType.Name}>.");

            var constructor = targetType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw new Exception($"Collection type {targetType.FullName} must have a default parameterless constructor");

            var addMethod = collectionType.GetMethod(nameof(ICollection<object>.Add));
            if (addMethod == null)
                throw new Exception($".{nameof(ICollection<object>.Add)}() Method missing from collection type {targetType.FullName}");

            var listVar = context.AddVariable(targetType, "list_" + context.GetNextVarNumber());
            context.AddStatement(Expression.Assign(listVar, Expression.New(constructor)));

            foreach (var index in indices)
            {
                var getScalarExpression = GetScalarConversionExpression(index, context, context.Reader.GetFieldType(index), elementType);
                context.AddStatement(Expression.Call(listVar, addMethod, getScalarExpression));
            }

            return listVar;
        }

        public static Expression GetConversionExpression(int columnIdx, DataRecordMapperCompileContext context, Type targetType)
        {
            var columnType = context.Reader.GetFieldType(columnIdx);
            return GetScalarConversionExpression(columnIdx, context, columnType, targetType);
        }

        private static Expression GetScalarConversionExpression(int columnIdx, DataRecordMapperCompileContext context, Type columnType, Type targetType)
        { 
            // Pull the value out of the reader into the rawVar
            var rawVar = context.AddVariable<object>("raw_" + context.GetNextVarNumber());
            var getRawStmt = Expression.Assign(rawVar, Expression.Call(context.RecordParam, GetValueMethod, Expression.Constant(columnIdx)));
            context.AddStatement(getRawStmt);

            // TODO: Should we store the converted values into variables instead of inlining them?

            if (targetType == typeof(object))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    rawVar,
                    Expression.Convert(Expression.Constant(null), targetType)); 
            }

            // The target is string, regardless of the data type we can .ToString() it
            if (targetType == typeof(string))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Call(rawVar, nameof(ToString), Type.EmptyTypes),
                    Expression.Convert(Expression.Constant(null), typeof(string)));
            }

            // They are the same type, so we can directly assign them
            if (columnType == targetType || (!columnType.IsClass && typeof(Nullable<>).MakeGenericType(columnType) == targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        rawVar,
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // They are both numeric but not the same type. Unbox and convert
            if (_numericTypes.Contains(columnType) && _numericTypes.Contains(targetType))
            {
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.Convert(
                        Expression.Unbox(rawVar, columnType),
                        targetType
                    ),
                    Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
            }

            // Target is bool, source type is numeric. Try to coerce by comparing against 0
            if ((targetType == typeof(bool) || targetType == typeof(bool?)) && _numericTypes.Contains(columnType))
            {
                // var result = rawVar is DBNull ? (rawVar != 0) : false
                return Expression.Condition(
                    Expression.NotEqual(_dbNullExp, rawVar),
                    Expression.NotEqual(Expression.Convert(Expression.Constant(0), columnType), Expression.Unbox(rawVar, columnType)),
                    Expression.Constant(false));
            }

            // We fall back to a basic conversion and hope all goes well
            return Expression.Condition(
                Expression.NotEqual(_dbNullExp, rawVar),
                Expression.Convert(
                    rawVar,
                    targetType
                ),
                Expression.Convert(Expression.Constant(GetDefaultValue(targetType)), targetType));
        }

        public static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}