docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=passwordABC123' -e 'MSSQL_PID=Developer' -p 1433:1433 -d --name mssql microsoft/mssql-server-linux:2017-CU4
echo MS SQL Server has been started
echo Notice that it make take several seconds before a connection can be established