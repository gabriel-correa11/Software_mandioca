param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$project = "src\MandiocaCaipira.Desktop\MandiocaCaipira.Desktop.csproj"
$publishDir = "artifacts\publish"
$releasesDir = "artifacts\releases"
$packId = "MandiocaCaipira"
$mainExe = "MandiocaCaipira.Desktop.exe"

Write-Host "=== Publicando MandiocaCaipira v$Version ===" -ForegroundColor Green

# Clean previous artifacts
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

# Publish
Write-Host "`n[1/4] dotnet publish..." -ForegroundColor Cyan
dotnet publish $project -c Release -r win-x64 -o $publishDir --self-contained -p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

# Pack with Velopack
Write-Host "`n[2/4] vpk pack..." -ForegroundColor Cyan
vpk pack --packId $packId --packVersion $Version --packDir $publishDir --mainExe $mainExe --outputDir $releasesDir --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

# Create GitHub Release and upload assets
Write-Host "`n[3/4] Criando release no GitHub..." -ForegroundColor Cyan
$tag = "v$Version"

# Check if gh CLI is available
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) nao encontrado. Instale em https://cli.github.com/"
}

# Delete existing release with same tag if it exists
gh release delete $tag --yes 2>&1 | Out-Null

# Create the release
gh release create $tag --title "MandiocaCaipira v$Version" --notes "Versao $Version" --latest
if ($LASTEXITCODE -ne 0) { throw "Falha ao criar release no GitHub" }

# Upload all release assets (nupkg, setup, RELEASES)
Write-Host "`n[4/4] Enviando arquivos para o GitHub..." -ForegroundColor Cyan
$assets = Get-ChildItem $releasesDir -File | Where-Object { $_.Name -match '\.(nupkg|exe)$' -or $_.Name -eq 'RELEASES' }
foreach ($asset in $assets) {
    Write-Host "  Enviando $($asset.Name)..."
    gh release upload $tag $asset.FullName --clobber
    if ($LASTEXITCODE -ne 0) { throw "Falha ao enviar $($asset.Name)" }
}

Write-Host "`n=== Pronto! ===" -ForegroundColor Green
Write-Host "Instalador: $releasesDir\${packId}-Setup.exe"
Write-Host "Release: https://github.com/gabriel-correa11/Software_mandioca/releases/tag/$tag"
Write-Host "`nO cliente recebera a atualizacao automaticamente na proxima vez que abrir o app."
