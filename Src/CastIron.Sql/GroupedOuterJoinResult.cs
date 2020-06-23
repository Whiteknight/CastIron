using System.Collections.Generic;

namespace CastIron.Sql
{
    public struct GroupedOuterJoinResult<TLeft, TRight>
    {
        public TLeft Left { get; }
        public IEnumerable<TRight> Right { get; }

        public GroupedOuterJoinResult(TLeft left, IEnumerable<TRight> right)
        {
            Left = left;
            Right = right;
        }
    }

    public struct OuterJoinResult<TLeft, TRight>
    {
        public TLeft Left { get; }
        public TRight Right { get; }

        public OuterJoinResult(TLeft left, TRight right)
        {
            Left = left;
            Right = right;
        }
    }
}