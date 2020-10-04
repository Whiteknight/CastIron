using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CastIron.Sql.Mapping.Compilers
{
    /// <summary>
    /// Instantiate an object. The concrete type chosen is decided upon by heuristic and may be a dictionary,
    /// array or primitive type depending on where we are in the object graph and how many columns we have
    /// to map.
    /// </summary>
    public class ObjectCompiler : ICompiler
    {
        private readonly ICompiler _dictionaries;
        private readonly ICompiler _scalars;
        private readonly ICompiler _arrays;

        public ObjectCompiler(ICompiler dictionaries, ICompiler scalars, ICompiler arrays)
        {
            _dictionaries = dictionaries;
            _scalars = scalars;
            _arrays = arrays;
        }

        public ConstructedValueExpression Compile(MapTypeContext context)
        {
            // At the top-level (name==null) we convert to dictionary to preserve column name information
            if (context.Name == null)
            {
                // Dictionary mapping logic will group by key name and recurse. When we get back to the 
                // ObjectCompiler, we will have a smaller set of columns and we will have a non-null name
                var dictState = context.ChangeTargetType(typeof(Dictionary<string, object>));
                var dictExpr = _dictionaries.Compile(dictState);
                return new ConstructedValueExpression(dictExpr.Expressions, Expression.Convert(dictExpr.FinalValue, typeof(object)), dictExpr.Variables);
            }

            var columns = context.GetColumns().ToList();
            var numColumns = columns.Count;

            // If we have no columns, just return null
            if (numColumns == 0)
            {
                return new ConstructedValueExpression(
                    Expression.Convert(
                        Expression.Constant(null),
                        typeof(object)
                    )
                );
            }

            // If we have exactly one column, map the value as a scalar and return a single result
            if (numColumns == 1)
            {
                var firstColumn = columns[0];
                var objectState = context.GetSubstateForColumn(firstColumn, typeof(object), null);
                var asScalar = _scalars.Compile(objectState);
                return new ConstructedValueExpression(asScalar.Expressions, Expression.Convert(asScalar.FinalValue, typeof(object)), asScalar.Variables);
            }

            // If we have more than one column, map to object[]
            var arrayState = context.ChangeTargetType(typeof(object[]));
            var exprs = _arrays.Compile(arrayState);
            return new ConstructedValueExpression(exprs.Expressions, Expression.Convert(exprs.FinalValue, typeof(object)), exprs.Variables);
        }
    }
}
