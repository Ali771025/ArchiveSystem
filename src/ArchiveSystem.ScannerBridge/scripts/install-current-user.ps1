param(
    [Parameter(Mandatory = $true)]
    [string[]]$AllowedOrigins,

    [string]$PublishedPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "publish\win-x64")
)

$ErrorActionPreference = "Stop"
$executableName = "ArchiveSystem.ScannerBridge.exe"
$sourceExecutable = Join-Path $PublishedPath $executableName

if (-not (Test-Path $sourceExecutable)) {
    throw "Published Scanner Bridge not found. Run publish-win-x64.ps1 first: $sourceExecutable"
}

$installPath = Join-Path $env:LOCALAPPDATA "ArchiveSystem\ScannerBridge"
New-Item -ItemType Directory -Path $installPath -Force | Out-Null

Get-Process -Name "ArchiveSystem.ScannerBridge" -ErrorAction SilentlyContinue |
    Stop-Process -Force

Copy-Item (Join-Path $PublishedPath "*") $installPath -Recurse -Force

$settings = [ordered]@{
    ScannerBridge = [ordered]@{
        Port = 17891
        AllowedOrigins = $AllowedOrigins
        Naps2ConsolePath = $null
        EnableTwain = $true
        EnableWia = $true
        ScanTimeoutSeconds = 300
        MaxPagesPerScan = 200
        MaxResponseBytes = 104857600
        JpegQuality = 85
        MaxProcessOutputCharacters = 32768
    }
    Logging = [ordered]@{
        LogLevel = [ordered]@{
            Default = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
}

$settings | ConvertTo-Json -Depth 5 |
    Set-Content -Path (Join-Path $installPath "appsettings.json") -Encoding UTF8

$startupScript = Join-Path $installPath "scripts\start-installed.ps1"
$startupCommand = 'powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "{0}"' -f $startupScript
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
New-Item -Path $runKey -Force | Out-Null
New-ItemProperty `
    -Path $runKey `
    -Name "ArchiveSystemScannerBridge" `
    -Value $startupCommand `
    -PropertyType String `
    -Force | Out-Null

& $startupScript
Write-Host "Scanner Bridge installed for the current Windows user."
Write-Host "Health endpoint: http://127.0.0.1:17891/api/health"
