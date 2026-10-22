$ErrorActionPreference = "Stop"
$installPath = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $installPath "logs"
New-Item -ItemType Directory -Path $logPath -Force | Out-Null

$running = Get-Process -Name "ArchiveSystem.ScannerBridge" -ErrorAction SilentlyContinue
if ($running) {
    exit 0
}

Start-Process `
    (Join-Path $installPath "ArchiveSystem.ScannerBridge.exe") `
    -WorkingDirectory $installPath `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $logPath "scanner-bridge.log") `
    -RedirectStandardError (Join-Path $logPath "scanner-bridge-error.log")
