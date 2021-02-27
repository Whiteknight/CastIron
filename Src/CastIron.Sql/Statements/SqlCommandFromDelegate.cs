using System;

namespace CastIron.Sql.Statements
{
    public class SqlCommandFromDelegate : ISqlCommand
    {
        private readonly Func<IDataInteraction, bool> _setup;

        public SqlCommandFromDelegate(Func<IDataInteraction, bool> setup)
        {
            _setup = setup;
        }

        public bool SetupCommand(IDataInteraction interaction) => _setup(interaction);
    }
}