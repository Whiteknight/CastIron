using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiles a mapping to an array type. Columns are mapped to elements until all available columns
    /// are exhausted. Supports any array type
    /// </summary>
    public class ArrayCompiler : ICompiler
    {
        private readonly ICompiler _customObjects;
        private readonly ICompiler _scalars;

        public ArrayCompiler(ICompiler customObjects, ICompiler scalars)
        {
            _customObjects = customObjects;
            _scalars = scalars;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            // Element type and constructor should both always be available for Array types, and
            // we know this is an array type in MapCompiler. Just assume we have these things, but
            // Debug.Assert them just to be sure.
            var elementType = context.TargetType.GetElementType();
            Debug.Assert(elementType != null);

            var constructor = context.TargetType.GetConstructor(new[] { typeof(int) });
            Debug.Assert(constructor != null);

            if (elementType.IsMappableCustomObjectType())
                return CompileArrayOfCustomObject(context, elementType, constructor);

            // In an array, object is always treated as a scalar type
            if (elementType.IsSupportedPrimitiveType() || elementType == typeof(object))
            {
                var columns = context.GetColumns().ToList();
                return CompileArrayOfScalar(context, constructor, columns, elementType);
            }

            // It's not a type we know how to support, so do nothing
            return ConstructedValueExpression.Nothing;
        }

        private ConstructedValueExpression CompileArrayOfScalar(MapTypeContext context, ConstructorInfo constructor, List<ColumnInfo> columns, Type elementType)
        {
            var arrayVar = context.CreateVariable(context.TargetType, "array");
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            variables.Add(arrayVar);
            expressions.Add(
                Expression.Assign(
                    arrayVar,
                    Expression.New(
                        constructor,
                        Expression.Constant(columns.Count)
                    )
                )
            );
            for (int i = 0; i < columns.Count; i++)
            {
                var columnState = context.GetSubstateForColumn(columns[i], elementType, null);
                var getScalarExpression = _scalars.Compile(columnState);
                expressions.AddRange(getScalarExpression.Expressions);
                expressions.Add(
                    Expression.Assign(
                        Expression.ArrayAccess(
                            arrayVar,
                            Expression.Constant(i)
                        ),
                        getScalarExpression.FinalValue
                    )
                );
                variables.AddRange(getScalarExpression.Variables);
            }

            return new ConstructedValueExpression(expressions, arrayVar, variables);
        }

        private ConstructedValueExpression CompileArrayOfCustomObject(MapTypeContext context, Type elementType, ConstructorInfo constructor)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // We don't loop here because we need to know how many items we have in the array and we
            // can't calculate that because the custom object might consume any number of columns. We don't 
            // know until after each iteration has returned how many columns are left. We could map to
            // a List and then .ToArray() to get the array, but that seems wasteful.

            var arrayVar = context.CreateVariable(context.TargetType, "array");
            variables.Add(arrayVar);
            var elementState = context.ChangeTargetType(elementType);
            var result = _customObjects.Compile(elementState);
            expressions.AddRange(result.Expressions);

            // Create a new array with a length of 1
            expressions.Add(
                Expression.Assign(
                    arrayVar,
                    Expression.New(
                        constructor,
                        Expression.Constant(1)
                    )
                )
            );

            // Set the value to the first slot in the array
            expressions.Add(
                Expression.Assign(
                    Expression.ArrayAccess(
                        arrayVar,
                        Expression.Constant(0)
                    ),
                    result.FinalValue
                )
            );
            variables.AddRange(result.Variables);
            return new ConstructedValueExpression(expressions, arrayVar, variables);
        }
    }
}