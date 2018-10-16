using System;
using System.Collections.Generic;

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
            CIAssert.ArgumentNotNull(enumerable, nameof(enumerable));
            CIAssert.ArgumentNotNull(getAggregate, nameof(getAggregate));
            CIAssert.ArgumentNotNull(mapOnto, nameof(mapOnto));

            foreach (var record in enumerable)
            {
                var aggregate = getAggregate(record);
                mapOnto(record, aggregate);
            }
        }
    }
}