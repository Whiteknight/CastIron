using System;

namespace CastIron.Sql.Statements
{
    public class SqlQueryFromDelegate : ISqlQuery
    {
        private readonly Func<IDataInteraction, bool> _setup;

        public SqlQueryFromDelegate(Func<IDataInteraction, bool> setup)
        {
            _setup = setup;
        }

        public bool SetupCommand(IDataInteraction interaction) => _setup(interaction);
    }

    public class SqlQueryFromDelegate<T> : ISqlQuery<T>
    {
        private readonly Func<IDataInteraction, bool> _setup;
        private readonly IResultMaterializer<T> _materializer;

        public SqlQueryFromDelegate(Func<IDataInteraction, bool> setup, IResultMaterializer<T> materializer)
        {
            _setup = setup;
            _materializer = materializer;
        }

        public bool SetupCommand(IDataInteraction interaction) => _setup(interaction);

        public T Read(IDataResults result) => _materializer.Read(result);
    }
}
