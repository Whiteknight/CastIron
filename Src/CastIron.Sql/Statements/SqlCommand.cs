﻿namespace CastIron.Sql.Statements
{
    public class SqlCommand : ISqlCommandSimple
    {
        private readonly string _sql;

        public SqlCommand(string sql)
        {
            _sql = sql;
        }

        public string GetSql()
        {
            return _sql;
        }
    }
}