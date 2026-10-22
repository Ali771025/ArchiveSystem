$ErrorActionPreference = "Stop"

Get-Process -Name "ArchiveSystem.ScannerBridge" -ErrorAction SilentlyContinue |
    Stop-Process -Force

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runKey -Name "ArchiveSystemScannerBridge" -ErrorAction SilentlyContinue

$installPath = Join-Path $env:LOCALAPPDATA "ArchiveSystem\ScannerBridge"
if (Test-Path $installPath) {
    Remove-Item $installPath -Recurse -Force
}

Write-Host "Scanner Bridge was removed for the current Windows user."
