Write-Host "Use a connection string as follows, after setting {ip}:"
Write-Host "Server={ip};User Id=sa;Password=docker_13.0.1601.5"
docker run -it -p 1433:1433 sqlserver