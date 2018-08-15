#Scripts/start-mssql.sh
#dotnet test --no-build --no-restore --verbosity normal Src/CastIron.Sql.Tests/CastIron.Sql.Tests.csproj
dotnet test --no-build --no-restore --verbosity normal Src/CastIron.Sql.Tests/CastIron.Sqlite.Tests.csproj
#Scripts/stop-mssql.sh