<#
.SYNOPSIS
  NexAlbum'ü Windows için kendi kendine yeten (self-contained) olarak paketler.

.DESCRIPTION
  .NET çalışma zamanı, SkiaSharp ve diğer tüm bağımlılıklar NexAlbum.exe'nin içine gömülür; hedef bilgisayarda .NET kurulu
  olması gerekmez. Çıktılar out/ klasörüne yazılır:
    out/NexAlbum/                         yayın klasörü (NexAlbum.exe + lisanslar)
    out/NexAlbum-<sürüm>-win-x64.zip      taşınabilir paket
    out/NexAlbum-<sürüm>-kurulum.exe      kurulum programı (-Kurulum verilirse; Inno Setup 6 gerekir)
    out/SHA256SUMS.txt                    özetler
  FFmpeg bilerek pakete konmaz (uygulama gerektiğinde izin isteyip indirir).

.EXAMPLE
  pwsh tools/yayin/paketle.ps1
  pwsh tools/yayin/paketle.ps1 -Version 0.2.0 -Kurulum
#>
[CmdletBinding()]
param(
  [string]$Version,
  [ValidateSet("win-x64", "win-arm64")]
  [string]$Runtime = "win-x64",
  [switch]$Kurulum
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $root

if (-not $Version) {
  $Version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.VersionPrefix | Where-Object { $_ } | Select-Object -First 1
}
$out = Join-Path $root "out"
$app = Join-Path $out "NexAlbum"
if (Test-Path $app) { Remove-Item $app -Recurse -Force }
New-Item -ItemType Directory -Force -Path $out | Out-Null

Write-Host "NexAlbum $Version ($Runtime) yayımlanıyor..." -ForegroundColor Cyan
dotnet publish src/NexAlbum.UI/NexAlbum.UI.csproj `
  -c Release -r $Runtime --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None -p:DebugSymbols=false `
  -p:Version=$Version `
  -o $app
if ($LASTEXITCODE -ne 0) { throw "dotnet publish başarısız ($LASTEXITCODE)." }

Copy-Item LICENSE (Join-Path $app "LICENSE.txt")
Copy-Item THIRD-PARTY-NOTICES.md (Join-Path $app "THIRD-PARTY-NOTICES.md")
Copy-Item README.md (Join-Path $app "README.md")

$exe = Join-Path $app "NexAlbum.exe"
if (-not (Test-Path $exe)) { throw "NexAlbum.exe üretilmedi." }

$zip = Join-Path $out "NexAlbum-$Version-$Runtime.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $app "*") -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Taşınabilir paket: $zip" -ForegroundColor Green

$files = @($zip)
if ($Kurulum) {
  $iscc = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
  ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
  if (-not $iscc) { throw "Inno Setup 6 bulunamadı. Kurun: winget install JRSoftware.InnoSetup" }

  $numeric = ($Version -split '-')[0]
  & $iscc /Qp "/DAppVersion=$Version" "/DAppNumericVersion=$numeric" "/DSourceDir=$app" "/DOutputDir=$out" `
    "/DArch=$($Runtime.Split('-')[1])" installer/NexAlbum.iss
  if ($LASTEXITCODE -ne 0) { throw "Inno Setup başarısız ($LASTEXITCODE)." }
  $setup = Join-Path $out "NexAlbum-$Version-kurulum.exe"
  Write-Host "Kurulum programı: $setup" -ForegroundColor Green
  $files += $setup
}

$sums = $files | ForEach-Object { "{0}  {1}" -f (Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $_ -Leaf) }
$sums | Set-Content (Join-Path $out "SHA256SUMS.txt") -Encoding ascii
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Bitti. NexAlbum.exe: $sizeMb MB" -ForegroundColor Cyan
