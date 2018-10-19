:: TODO: Break out separate build scripts per project
dotnet build Src\CastIron.Sql\CastIron.Sql.csproj --configuration Release
if ERRORLEVEL 1 GOTO :error

dotnet pack Src\CastIron.Sql\CastIron.Sql.csproj --configuration Release --no-build --no-restore
if ERRORLEVEL 1 GOTO :error

dotnet build Src\CastIron.Sqlite\CastIron.Sqlite.csproj --configuration Release
if ERRORLEVEL 1 GOTO :error

dotnet pack Src\CastIron.Sqlite\CastIron.Sqlite.csproj --configuration Release --no-build --no-restore
if ERRORLEVEL 1 GOTO :error

goto :done

:error
echo Build FAILED

:done