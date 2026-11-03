param(
    [string]$Version = "1.0.0",
    [string]$UpdateDir = ""
)

if (-not $UpdateDir) {
    $UpdateDir = "$env:LOCALAPPDATA\MandiocaCaipira\Updates"
}

$ErrorActionPreference = "Stop"

$project = "src\MandiocaCaipira.Desktop\MandiocaCaipira.Desktop.csproj"
$publishDir = "artifacts\publish"
$packId = "MandiocaCaipira"
$mainExe = "MandiocaCaipira.Desktop.exe"

Write-Host "=== Publicando MandiocaCaipira v$Version ===" -ForegroundColor Green

# Clean previous artifacts
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

# Publish
Write-Host "`n[1/3] dotnet publish..." -ForegroundColor Cyan
dotnet publish $project -c Release -r win-x64 -o $publishDir --no-self-contained -p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

# Pack with Velopack
Write-Host "`n[2/3] vpk pack..." -ForegroundColor Cyan
vpk pack --packId $packId --packVersion $Version --packDir $publishDir --mainExe $mainExe --outputDir "artifacts\releases" --runtime win-x64 --framework "net10.0-x64-desktop"
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

# Copy releases to update directory
Write-Host "`n[3/3] Copiando para $UpdateDir..." -ForegroundColor Cyan
if (-not (Test-Path $UpdateDir)) {
    New-Item -ItemType Directory -Path $UpdateDir -Force | Out-Null
}
Copy-Item "artifacts\releases\*" $UpdateDir -Force

Write-Host "`n=== Pronto! ===" -ForegroundColor Green
Write-Host "Instalador: artifacts\releases\${packId}-Setup.exe"
Write-Host "Releases copiados para: $UpdateDir"
