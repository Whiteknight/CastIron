dotnet build Src/CastIron.Sql/CastIron.Sql.csproj --configuration Release
dotnet pack --no-build --no-restore Src/CastIron.Sql/CastIron.Sql.csproj --configuration Release

dotnet build Src/CastIron.SqlServer/CastIron.SqlServer.csproj --configuration Release
dotnet pack --no-build --no-restore Src/CastIron.SqlServer/CastIron.SqlServer.csproj --configuration Release

dotnet build Src/CastIron.Sqlite/CastIron.Sqlite.csproj --configuration Release
dotnet pack --no-build --no-restore Src/CastIron.Sqlite/CastIron.Sqlite.csproj --configuration Release

dotnet build Src/CastIron.Postgres/CastIron.Postgres.csproj --configuration Release
dotnet pack --no-build --no-restore Src/CastIron.Postgres/CastIron.Postgres.csproj --configuration Release

dotnet build Src/CastIron.MySql/CastIron.MySql.csproj --configuration Release
dotnet pack --no-build --no-restore Src/CastIron.MySql/CastIron.MySql.csproj --configuration Release