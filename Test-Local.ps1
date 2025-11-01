$ErrorActionPreference = 'Stop'
ForEach ($Test in $(ls *.Tests)) {
  Write-Host "Testing $Test"
  dotnet test -r win-x64 -f netcoreapp3.1 -c Debug --no-build --no-restore $Test
}