$Source = $PSScriptRoot

dotnet tool restore
dotnet paket restore
dotnet restore
dotnet build -c Debug --no-restore 
ForEach ($Test in $(ls *.Tests)) {
dotnet publish -c Debug --no-restore --no-build --output bin\Debug\netcoreapp3.1 $Test
}

$Target = "C:\Nata"
$Volume = $Source+":"+$Target
Write-Host "Mounting $Source into the container as C:\Nata"

$Command = "net start CosmosDB ; Set-Location /Nata ; ./Test-Local.ps1"

docker run --rm --memory=8GB -it -v $Volume nata/test PowerShell.exe -NoProfile -Command "$Command"

