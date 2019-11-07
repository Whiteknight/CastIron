using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public class TupleCompiler : ICompiler
    {
        private readonly ICompiler _values;

        public TupleCompiler(ICompiler values)
        {
            _values = values;
        }

        // value = Tuple.Create(converted columns)
        public ConstructedValueExpression Compile(MapState state)
        {
            var typeParams = GetTupleTypeParameters(state);
            var factoryMethod = GetTupleFactoryMethod(state, typeParams);
            return MapTupleParameters(state, factoryMethod, typeParams);
        }

        private ConstructedValueExpression MapTupleParameters(MapState state, MethodInfo factoryMethod, Type[] typeParams)
        {
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            var args = new Expression[typeParams.Length];

            // We need to assign a value to every parameter. Loop over each parameter and map values where
            // available. When we run out of columns or have a column we cannot map, fill in a default value.
            // Where a value type may consume many columns (a custom object type, for example) it will 
            // greedily consume as much as possible before moving to the next parameter.
            for (var i = 0; i < typeParams.Length; i++)
            {
                if (!state.HasColumns())
                {
                    args[i] = typeParams[i].GetDefaultValueExpression();
                    continue;
                }

                var elementState = state.ChangeTargetType(typeParams[i]);
                var expr = _values.Compile(elementState);
                if (expr.IsNothing)
                {
                    args[i] = typeParams[i].GetDefaultValueExpression();
                    continue;
                }

                expressions.AddRange(expr.Expressions);
                variables.AddRange(expr.Variables);
                args[i] = expr.FinalValue;
            }
            return new ConstructedValueExpression(expressions, Expression.Call(null, factoryMethod, args), variables);
        }

        private static Type[] GetTupleTypeParameters(MapState state)
        {
            var typeParams = state.TargetType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, state.TargetType);
            return typeParams;
        }

        private static MethodInfo GetTupleFactoryMethod(MapState state, Type[] typeParams)
        {
            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, state.TargetType);
            return factoryMethod;
        }
    }
}