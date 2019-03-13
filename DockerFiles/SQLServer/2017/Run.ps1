Write-Host "Use a connection string as follows, after setting {ip}:"
Write-Host "Server={ip};User Id=sa;Password=docker_14.0.1000.169"
docker run --rm -p 1433:1433 -it nata/sqlserver:2017