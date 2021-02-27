using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQueryFromSimple : ISqlQuery
    {
        private readonly ISqlQuerySimple _simple;

        public SqlQueryFromSimple(ISqlQuerySimple simple)
        {
            Argument.NotNull(simple, nameof(simple));
            _simple = simple;
        }

        public bool SetupCommand(IDataInteraction interaction)
        {
            interaction.ExecuteText(_simple.GetSql());
            return interaction.IsValid;
        }

        // Delegate to _simple for both GetHashCode and Equals. What matters for caching purposes
        // is that the underlying query structure is the same. What size/shape the QueryObject
        // that holds the query is, doesn't matter.
        public override int GetHashCode() => _simple.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ISqlQuerySimple asSimple)
                return _simple.Equals(asSimple);
            if (obj is SqlQueryFromSimple asQuery)
                return _simple.Equals(asQuery._simple);
            return false;
        }
    }

    public class SqlQueryFromSimple<T> : ISqlQuery<T>
    {
        private readonly ISqlQuerySimple<T> _simple;

        public SqlQueryFromSimple(ISqlQuerySimple<T> simple)
        {
            Argument.NotNull(simple, nameof(simple));
            _simple = simple;
        }

        public bool SetupCommand(IDataInteraction interaction)
        {
            interaction.ExecuteText(_simple.GetSql());
            return interaction.IsValid;
        }

        // Whether to cache the map or not is the decision of the underlying _simple
        public T Read(IDataResults result) => _simple.Read(result);

        public override int GetHashCode() => _simple.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ISqlQuerySimple<T> asSimple)
                return _simple.Equals(asSimple);
            if (obj is SqlQueryFromSimple<T> asQuery)
                return _simple.Equals(asQuery._simple);
            return false;
        }
    }
}
