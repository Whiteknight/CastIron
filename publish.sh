if [ -e Src/CastIron.Sql/bin/Release/CastIron.Sql.$1.nupkg ]; then
    dotnet nuget push Src/CastIron.Sql/bin/Release/CastIron.Sql.$1.nupkg --source https://www.nuget.org/api/v2/package
fi

if [ -e Src/CastIron.SqlServer/bin/Release/CastIron.SqlServer.$1.nupkg ]; then
    dotnet nuget push Src/CastIron.SqlServer/bin/Release/CastIron.SqlServer.$1.nupkg --source https://www.nuget.org/api/v2/package
fi

if [ -e Src/CastIron.Sqlite/bin/Release/CastIron.Sqlite.$1.nupkg ]; then
    dotnet nuget push Src/CastIron.Sqlite/bin/Release/CastIron.Sqlite.$1.nupkg --source https://www.nuget.org/api/v2/package
fi

if [ -e Src/CastIron.Postgres/bin/Release/CastIron.Postgres.$1.nupkg ]; then
    dotnet nuget push Src/CastIron.Postgres/bin/Release/CastIron.Postgres.$1.nupkg --source https://www.nuget.org/api/v2/package
fi