using CastIron.Sql.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

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

            // TODO: Benchmark and figure out which implementation works better
            var pairs = outer.Join(inner, outerKeySelector, innerKeySelector, (o, i) => new
            {
                Outer = o,
                Inner = i
            });
            foreach (var pair in pairs)
            {
                onEach(pair.Outer, pair.Inner);
            }
        }
    }
}