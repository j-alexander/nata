$ErrorActionPreference = 'Stop'
ForEach ($Test in $(ls *.Tests)) {
  Write-Host "Testing $Test"
  dotnet test -c Debug --no-build --no-restore $Test
}