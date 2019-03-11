.paket/paket.bootstrapper.exe
.paket/paket.exe restore
dotnet restore
dotnet build --no-restore
docker build .\
