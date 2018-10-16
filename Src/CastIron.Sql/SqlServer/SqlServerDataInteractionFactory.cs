﻿using System.Data;

namespace CastIron.Sql.SqlServer
{
    public class SqlServerDataInteractionFactory : IDataInteractionFactory
    {
        public IDataInteraction Create(IDbCommand command)
        {
            CIAssert.ArgumentNotNull(command, nameof(command));
            return new SqlServerDataInteraction(command);
        }
    }
}