param ([parameter(mandatory=$true)][string]$version)
dotnet pack -c Release -o ../nuget /p:PackageVersion=$version /p:AssemblyVersion=$version /p:AssemblyFileVersion=$version