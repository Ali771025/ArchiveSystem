param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $projectRoot "publish\win-x64"

dotnet publish (Join-Path $projectRoot "ArchiveSystem.ScannerBridge.csproj") `
    --configuration $Configuration `
    --output $outputPath

Copy-Item (Join-Path $projectRoot "README.md") $outputPath -Force
Copy-Item (Join-Path $projectRoot "THIRD-PARTY-NOTICES.md") $outputPath -Force
New-Item -ItemType Directory -Path (Join-Path $outputPath "scripts") -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot "start-installed.ps1") (Join-Path $outputPath "scripts") -Force
Write-Host "Scanner Bridge published to: $outputPath"
