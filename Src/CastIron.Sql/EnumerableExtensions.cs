using System;
using System.Collections.Generic;
using System.Linq;
using CastIron.Sql.Utility;

namespace CastIron.Sql
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// For every record in the sequence, find an aggregate object to own the record, and map the record onto the aggregate
        /// </summary>
        /// <typeparam name="TRecord"></typeparam>
        /// <typeparam name="TAggregate"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="getAggregate"></param>
        /// <param name="mapOnto"></param>
        public static void MapOnto<TRecord, TAggregate>(this IEnumerable<TRecord> enumerable, Func<TRecord, TAggregate> getAggregate, Action<TRecord, TAggregate> mapOnto)
        {
            Argument.NotNull(enumerable, nameof(enumerable));
            Argument.NotNull(getAggregate, nameof(getAggregate));
            Argument.NotNull(mapOnto, nameof(mapOnto));

            foreach (var record in enumerable)
            {
                var aggregate = getAggregate(record);
                if (aggregate == null)
                    continue;
                mapOnto(record, aggregate);
            }
        }

        /// <summary>
        /// Inner Join the two enumerables on matching keys, and for each pair execute a callback function
        /// </summary>
        /// <typeparam name="TOuter"></typeparam>
        /// <typeparam name="TInner"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="outer"></param>
        /// <param name="inner"></param>
        /// <param name="outerKeySelector"></param>
        /// <param name="innerKeySelector"></param>
        /// <param name="onEach"></param>
        public static void ForEachInnerJoin<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Action<TOuter, TInner> onEach)
        {
            Argument.NotNull(outer, nameof(outer));
            Argument.NotNull(inner, nameof(inner));
            Argument.NotNull(outerKeySelector, nameof(outerKeySelector));
            Argument.NotNull(innerKeySelector, nameof(innerKeySelector));
            Argument.NotNull(onEach, nameof(onEach));

            var pairs = outer.Join(inner, outerKeySelector, innerKeySelector, (o, i) => new
            {
                Outer = o,
                Inner = i
            });
            foreach (var pair in pairs)
                onEach(pair.Outer, pair.Inner);
        }

        /// <summary>
        /// Left outer join the two enumerables on matching keys. For every item in the left enumerable,
        /// return a result containing all matching items from the right enumerable, if any.
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="leftKeySelector"></param>
        /// <param name="rightKeySelector"></param>
        /// <returns></returns>
        public static IEnumerable<GroupedOuterJoinResult<TLeft, TRight>> GroupedLeftOuterJoin<TLeft, TRight, TKey>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector)
        {
            Argument.NotNull(left, nameof(left));
            Argument.NotNull(right, nameof(right));
            Argument.NotNull(leftKeySelector, nameof(leftKeySelector));
            Argument.NotNull(rightKeySelector, nameof(rightKeySelector));

            var rightGroups = right
                .GroupBy(rightKeySelector)
                .ToDictionary(g => g.Key, g => g.ToList());

            return left
                .Select(leftItem =>
                {
                    var leftKey = leftKeySelector(leftItem);
                    var rightItems = rightGroups.ContainsKey(leftKey) ? rightGroups[leftKey] : Enumerable.Empty<TRight>();
                    return new GroupedOuterJoinResult<TLeft, TRight>(leftItem, rightItems);
                });
        }

        /// <summary>
        /// Left outer join two enumerables. Return an enumerable for every item in the left enumerable
        /// and every matching item in the right enumerable. If there are no matching items in the right
        /// enumerable, a result is returned with the left item and a default value for the right item.
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="leftKeySelector"></param>
        /// <param name="rightKeySelector"></param>
        /// <returns></returns>
        public static IEnumerable<OuterJoinResult<TLeft, TRight>> LeftOuterJoin<TLeft, TRight, TKey>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector)
        {
            Argument.NotNull(left, nameof(left));
            Argument.NotNull(right, nameof(right));
            Argument.NotNull(leftKeySelector, nameof(leftKeySelector));
            Argument.NotNull(rightKeySelector, nameof(rightKeySelector));

            var rightGroups = right
                .GroupBy(rightKeySelector)
                .ToDictionary(g => g.Key, g => g.ToList());

            return left
                .SelectMany(leftItem =>
                {
                    var leftKey = leftKeySelector(leftItem);
                    if (!rightGroups.ContainsKey(leftKey))
                        return new[] { new OuterJoinResult<TLeft, TRight>(leftItem, default) };

                    return rightGroups[leftKey].Select(r => new OuterJoinResult<TLeft, TRight>(leftItem, r));
                });
        }
    }
}