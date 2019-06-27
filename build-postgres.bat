dotnet build Src\CastIron.Postgres\CastIron.Postgres.csproj --configuration Release
if ERRORLEVEL 1 GOTO :error

dotnet pack Src\CastIron.Postgres\CastIron.Postgres.csproj --configuration Release --no-build --no-restore
if ERRORLEVEL 1 GOTO :error

goto :done

:error
echo Build FAILED

:done