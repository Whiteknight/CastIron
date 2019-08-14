using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping
{
    public class ConstructedValueExpression
    {
        public ConstructedValueExpression(Expression finalValue)
        {
            Expressions = Enumerable.Empty<Expression>();
            FinalValue = finalValue;

        }

        public ConstructedValueExpression(IEnumerable<Expression> expressions, Expression finalValue)
        {
            Expressions = expressions;
            FinalValue = finalValue;
        }

        public static ConstructedValueExpression Nothing()
        {
            return new ConstructedValueExpression(null);
        }

        public IEnumerable<Expression> Expressions { get; }
        public Expression FinalValue { get; }

    }
}