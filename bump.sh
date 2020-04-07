sed -i -E "s/<Version>[0-9]+.[0-9]+.[0-9]+/<Version>$1/" \
    Src/CastIron.Sql/CastIron.Sql.csproj \
    Src/CastIron.Postgres/CastIron.Postgres.csproj \
    Src/CastIron.Sqlite/CastIron.Sqlite.csproj 
    