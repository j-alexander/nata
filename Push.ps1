param ([parameter(mandatory=$true)][string]$key)
dotnet nuget push nuget\*.nupkg -k $key -s https://api.nuget.org/v3/index.json