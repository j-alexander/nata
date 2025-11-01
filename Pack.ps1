param ([parameter(mandatory=$true)][string]$version)

# Split the version string by dots
$versionParts = $version.Split('.')

# Check if we have exactly 3 parts
if ($versionParts.Count -ne 3) {
        Write-Error @"
Version must have exactly 3 parts (e.g., 1.2.27) for NuGet package versioning.

Why 3 parts are required:
  - NuGet normalizes 4-part versions by dropping trailing zeros
  - 1.2.27.0 becomes 1.2.27, causing package mismatch errors
  - Semantic versioning (major.minor.patch) is the NuGet standard

Your version '$version' has $($versionParts.Count) parts.
Please provide a version in the format: major.minor.patch (e.g., 1.2.27)
"@
    exit 1
}

dotnet pack -c Release -o ./nuget /p:PackageVersion=$version /p:AssemblyVersion=$version /p:AssemblyFileVersion=$version