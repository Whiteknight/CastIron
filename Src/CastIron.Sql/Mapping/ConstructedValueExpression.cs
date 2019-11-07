using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public struct ConstructedValueExpression
    {
        public ConstructedValueExpression(Expression finalValue)
        {
            Expressions = Enumerable.Empty<Expression>();
            Variables = Enumerable.Empty<ParameterExpression>();
            FinalValue = finalValue;
        }

        public ConstructedValueExpression(IEnumerable<Expression> expressions, Expression finalValue, IEnumerable<ParameterExpression> variables)
        {
            Expressions = expressions ?? Enumerable.Empty<Expression>();
            FinalValue = finalValue;
            Variables = variables ?? Enumerable.Empty<ParameterExpression>();
        }

        public bool IsNothing => Expressions == null;
        public static ConstructedValueExpression Nothing => default;

        public IEnumerable<Expression> Expressions { get; }

        public Expression FinalValue { get; }
        public IEnumerable<ParameterExpression> Variables { get; }

        // TODO: Method to merge one of these into another
    }
}