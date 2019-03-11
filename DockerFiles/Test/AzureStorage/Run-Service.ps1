if (-not (Test-Path "/AzureStorage/Configuration.info")) {
     Write-Host "Running as:"
     WhoAmI
     AzureStorageEmulator.exe init -server localhost -forcecreate -inprocess
     AzureStorageEmulator.exe status > /AzureStorage/Configuration.info
}
AzureStorageEmulator.exe start -inprocess
