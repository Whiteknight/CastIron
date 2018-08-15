  dotnet build -f netstandard2.0 Src/CastIron.Sql/CastIron.Sql.csproj
#  dotnet build --no-dependencies -f netstandard2.0 Src/CastIron.MySql/CastIron.MySql.csproj
#  dotnet build --no-dependencies -f netstandard2.0 Src/CastIron.Postgres/CastIron.Postgres.csproj
  dotnet build --no-dependencies -f netstandard2.0 Src/CastIron.Sqlite/CastIron.Sqlite.csproj

#  dotnet build --no-dependencies -f netcoreapp2.0 Src/CastIron.Sql.Tests/CastIron.Sql.Tests.csproj
 dotnet build --no-dependencies -f netcoreapp2.0 Src/CastIron.Sql.Tests/CastIron.Sqlite.Tests.csproj