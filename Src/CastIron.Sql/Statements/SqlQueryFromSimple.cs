using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQueryFromSimple : ISqlQuery
    {
        private readonly ISqlQuerySimple _simple;

        public SqlQueryFromSimple(ISqlQuerySimple simple)
        {
            Assert.ArgumentNotNull(simple, nameof(simple));
            _simple = simple;
        }

        public bool SetupCommand(IDataInteraction interaction)
        {
            interaction.ExecuteText(_simple.GetSql());
            return interaction.IsValid;
        }
    }

    public class SqlQueryFromSimple<T> : ISqlQuery<T>
    {
        private readonly ISqlQuerySimple<T> _simple;

        public SqlQueryFromSimple(ISqlQuerySimple<T> simple)
        {
            Assert.ArgumentNotNull(simple, nameof(simple));
            _simple = simple;
        }

        public bool SetupCommand(IDataInteraction interaction)
        {
            interaction.ExecuteText(_simple.GetSql());
            return interaction.IsValid;
        }

        public T Read(IDataResults result)
        {
            return _simple.Read(result);
        }
    }
}