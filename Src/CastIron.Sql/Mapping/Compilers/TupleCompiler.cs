using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CastIron.Sql.Mapping.Compilers
{
    public class TupleCompiler : ICompiler
    {
        private readonly ICompiler _valueCompiler;

        public TupleCompiler(ICompiler valueCompiler)
        {
            _valueCompiler = valueCompiler;
        }

        // value = Tuple.Create(converted columns)
        public ConstructedValueExpression Compile(MapState state)
        {
            var typeParams = state.TargetType.GenericTypeArguments;
            if (typeParams.Length == 0 || typeParams.Length > 7)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, state.TargetType);

            var factoryMethod = typeof(Tuple).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == typeParams.Length)
                .Select(m => m.MakeGenericMethod(typeParams))
                .FirstOrDefault();
            if (factoryMethod == null)
                throw MapCompilerException.InvalidTupleMap(typeParams.Length, state.TargetType);

            var args = new Expression[typeParams.Length];
            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            var columns = state.GetColumns().Take(typeParams.Length).ToList();
            for (var i = 0; i < typeParams.Length; i++)
            {
                if (i >= columns.Count)
                {
                    args[i] = typeParams[i].GetDefaultValueExpression();
                    continue;
                }

                var substate = state.GetSubstateForColumn(columns[i], typeParams[i], null);
                var expr = _valueCompiler.Compile(substate);
                expressions.AddRange(expr.Expressions);
                variables.AddRange(expr.Variables);
                args[i] = expr.FinalValue;
            }

            return new ConstructedValueExpression(expressions, Expression.Call(null, factoryMethod, args), variables);
        }
    }
}