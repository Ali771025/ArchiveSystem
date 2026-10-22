param(
    [string]$BaseUrl = "http://127.0.0.1:17891"
)

$ErrorActionPreference = "Stop"

$health = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get
$devices = Invoke-RestMethod -Uri "$BaseUrl/api/scanners" -Method Get

Write-Host "Status: $($health.status), Version: $($health.version)"
if ($devices.devices.Count -eq 0) {
    Write-Warning "No scanner was detected. Verify USB and PaperStream IP (TWAIN)."
} else {
    $devices.devices | Format-Table name, driver, isDefault, id
}
