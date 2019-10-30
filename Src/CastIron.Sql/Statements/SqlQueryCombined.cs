using CastIron.Sql.Utility;

namespace CastIron.Sql.Statements
{
    public class SqlQueryCombined<T> : ISqlQuery<T>
    {
        private readonly ISqlQuery _query;
        private readonly IResultMaterializer<T> _materializer;

        public SqlQueryCombined(ISqlQuery query, IResultMaterializer<T> materializer)
        {
            Argument.NotNull(query, nameof(query));
            Argument.NotNull(materializer, nameof(materializer));
            _query = query;
            _materializer = materializer;
        }

        public bool SetupCommand(IDataInteraction interaction) => _query.SetupCommand(interaction);

        public T Read(IDataResults result) => _materializer.Read(result);
    }
}