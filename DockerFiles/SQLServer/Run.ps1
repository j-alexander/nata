Write-Host "Use a connection string as follows, after setting {ip}:"
Write-Host "Server={ip};User Id=sa;Password=docker_13.0.4001.0"
docker run --rm -p 1433:1433 -it nata/sqlserver