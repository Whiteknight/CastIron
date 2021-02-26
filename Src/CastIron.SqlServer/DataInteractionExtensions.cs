using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using CastIron.Sql;
using CastIron.Sql.Mapping;
using CastIron.Sql.Utility;

namespace CastIron.SqlServer
{
    public static class DataInteractionExtensions
    {
        public static IDataInteraction AddTableValuedParameter(this IDataInteraction interaction, string name, string typeName, DataTable dataTable)
        {
            Argument.NotNullOrEmpty(name, nameof(name));
            Argument.NotNullOrEmpty(typeName, nameof(typeName));
            Argument.NotNull(dataTable, nameof(dataTable));

            return interaction.AddParameter(param =>
            {
                param.Direction = ParameterDirection.Input;
                param.ParameterName = name;
                if (param is SqlParameter sqlParameter)
                    sqlParameter.TypeName = typeName;
                param.Value = dataTable;
            });
        }

        public static IDataInteraction AddTableValuedParameter<T>(this IDataInteraction interaction, string name, string typeName, IEnumerable<T> rows)
        {
            Argument.NotNull(rows, nameof(rows));

            var dataTable = new DataTable();
            var mappableProperties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsSupportedPrimitiveType());

            var mappers = new List<Action<T, DataRow>>();
            foreach (var property in mappableProperties)
            {
                var p = property;
                dataTable.Columns.Add(p.Name, p.PropertyType);
                mappers.Add((t, row) => row[p.Name] = p.GetValue(t));
            }

            foreach (var item in rows)
            {
                var row = dataTable.NewRow();
                foreach (var mapper in mappers)
                    mapper(item, row);
                dataTable.Rows.Add(row);
            }

            return AddTableValuedParameter(interaction, name, typeName, dataTable);
        }
    }
}
