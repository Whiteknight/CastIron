using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Compiles a mapping to an array type. Columns are mapped to elements until all available columns
    /// are exhausted. Supports any array type and IEnumerable, ICollection and IList if element types are
    /// not provided.
    /// </summary>
    public class ArrayCompiler : ICompiler
    {
        private readonly ICompiler _customObjects;
        private readonly ICompiler _arrayContents;

        public ArrayCompiler(ICompiler customObjects, ICompiler arrayContents)
        {
            _customObjects = customObjects;
            _arrayContents = arrayContents;
        }

        public ConstructedValueExpression Compile(MapState state)
        {
            if (state.TargetType.IsUntypedEnumerableType())
                state = state.ChangeTargetType(typeof(object[]));

            var elementType = state.TargetType.GetElementType();
            if (elementType == null)
                throw MapCompilerException.CannotDetermineArrayElementType(state.TargetType);

            var constructor = state.TargetType.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
                throw MapCompilerException.MissingArrayConstructor(state.TargetType);

            var columns = state.GetColumns().ToList();

            if (elementType.IsMappableCustomObjectType())
                return CompileArrayOfCustomObject(state, elementType, constructor);

            var arrayVar = state.CreateVariable(state.TargetType, "array");
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
                var columnState = state.GetSubstateForColumn(columns[i], elementType, null);
                var getScalarExpression = _arrayContents.Compile(columnState);
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

        private ConstructedValueExpression CompileArrayOfCustomObject(MapState state, Type elementType, ConstructorInfo constructor)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            var arrayVar = state.CreateVariable(state.TargetType, "array");
            variables.Add(arrayVar);
            expressions.Add(
                Expression.Assign(
                    arrayVar,
                    Expression.New(
                        constructor,
                        Expression.Constant(1)
                    )
                )
            );
            var elementState = state.ChangeTargetType(elementType);
            var result = _customObjects.Compile(elementState);
            expressions.AddRange(result.Expressions);
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