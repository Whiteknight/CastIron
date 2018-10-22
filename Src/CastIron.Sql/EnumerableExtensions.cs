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
            Assert.ArgumentNotNull(enumerable, nameof(enumerable));
            Assert.ArgumentNotNull(getAggregate, nameof(getAggregate));
            Assert.ArgumentNotNull(mapOnto, nameof(mapOnto));

            foreach (var record in enumerable)
            {
                var aggregate = getAggregate(record);
                // TODO: Should we filter out cases where aggregate is null?
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
            Assert.ArgumentNotNull(outer, nameof(outer));
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(outerKeySelector, nameof(outerKeySelector));
            Assert.ArgumentNotNull(innerKeySelector, nameof(innerKeySelector));
            Assert.ArgumentNotNull(onEach, nameof(onEach));

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

            // TODO: This one needs to account for duplicate keys in the outer list
            //var outerLookup = outer.ToDictionary(outerKeySelector);
            //foreach (var innerItem in inner)
            //{
            //    var key = innerKeySelector(innerItem);
            //    if (!outerLookup.ContainsKey(key))
            //        continue;
            //    onEach(outerLookup[key], innerItem);
            //}
        }
    }
}