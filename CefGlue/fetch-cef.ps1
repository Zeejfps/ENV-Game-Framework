<#
.SYNOPSIS
  Fetch + verify + stage the pinned CEF binary distribution for the ZgfCef binding.

.DESCRIPTION
  Reads the version + SHA1s from CEF_VERSION (next to this script), downloads the
  requested Spotify CDN distribution, verifies its SHA1, extracts it, and stages it
  into cef/<rid>/{include,Release,Resources} — the layout the generator (headers)
  and the harness (runtime) expect.

  The CEF payload is large (~160 MB compressed) and is .gitignored; CI and fresh
  clones run this script instead of committing binaries.

.PARAMETER Rid
  Runtime identifier. Only win-x64 is wired today; the CDN naming is centralised
  below so linux64/macosx64 are a small addition.

.PARAMETER Distribution
  'minimal' (default) = headers + Release runtime, what the binding + harness need.
  'client'            = prebuilt cefclient.exe/cefsimple.exe for the Phase 0 OSR probe.

.EXAMPLE
  ./fetch-cef.ps1                       # minimal win-x64 into cef/win-x64/
  ./fetch-cef.ps1 -Distribution client  # OSR probe sample apps
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64')]
    [string]$Rid = 'win-x64',
    [ValidateSet('minimal', 'client')]
    [string]$Distribution = 'minimal',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = $PSScriptRoot

# --- read the pin -----------------------------------------------------------
$versionFile = Join-Path $root 'CEF_VERSION'
if (-not (Test-Path $versionFile)) { throw "CEF_VERSION not found at $versionFile" }
$pins = @{}
foreach ($line in Get-Content $versionFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*([A-Z0-9_]+)\s*=\s*(.+?)\s*$') { $pins[$Matches[1]] = $Matches[2] }
}
$cefVersion = $pins['CEF_VERSION']
if (-not $cefVersion) { throw "CEF_VERSION= not set in $versionFile" }

# --- map rid/distribution -> CDN file + expected sha1 -----------------------
$cdnPlatform = switch ($Rid) { 'win-x64' { 'windows64' } }
$shaKey = switch ("$Rid|$Distribution") {
    'win-x64|minimal' { 'SHA1_WINDOWS64_MINIMAL' }
    'win-x64|client'  { 'SHA1_WINDOWS64_CLIENT' }
}
$expectedSha1 = $pins[$shaKey]
if (-not $expectedSha1) { throw "$shaKey not set in $versionFile" }

# CDN suffix: minimal/client get a suffix; the 'standard' distro has none.
$suffix = switch ($Distribution) { 'minimal' { '_minimal' } 'client' { '_client' } }
$fileName = "cef_binary_${cefVersion}_${cdnPlatform}${suffix}.tar.bz2"
# The '+' chars in the version are %2B in the URL.
$urlName = $fileName -replace '\+', '%2B'
$url = "https://cef-builds.spotifycdn.com/$urlName"

$dlDir = Join-Path $root '.cef-download'
New-Item -ItemType Directory -Force -Path $dlDir | Out-Null
$archive = Join-Path $dlDir $fileName

# --- download (skip if cached + valid) --------------------------------------
function Get-Sha1($path) { (Get-FileHash -Algorithm SHA1 -Path $path).Hash.ToLowerInvariant() }

if ((Test-Path $archive) -and -not $Force -and (Get-Sha1 $archive) -eq $expectedSha1) {
    Write-Host "Using cached $fileName (sha1 ok)"
}
else {
    Write-Host "Downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $archive
    $actual = Get-Sha1 $archive
    if ($actual -ne $expectedSha1) {
        throw "SHA1 mismatch for $fileName`n  expected $expectedSha1`n  actual   $actual"
    }
    Write-Host "SHA1 verified: $actual"
}

# --- extract ----------------------------------------------------------------
# bsdtar ships with Windows 10+ and handles .tar.bz2 in one pass.
$extractDir = Join-Path $dlDir "extract-$Distribution"
if (Test-Path $extractDir) { Remove-Item -Recurse -Force $extractDir }
New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
Write-Host "Extracting..."
tar -xjf $archive -C $extractDir
if ($LASTEXITCODE -ne 0) { throw "tar extraction failed ($LASTEXITCODE)" }
$payload = Get-ChildItem -Directory $extractDir | Select-Object -First 1
if (-not $payload) { throw "no payload directory after extraction" }

# --- stage into cef/<rid>/{include,Release,Resources} -----------------------
$stage = Join-Path (Join-Path $root 'cef') $Rid
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

foreach ($sub in @('include', 'Release', 'Resources')) {
    $srcSub = Join-Path $payload.FullName $sub
    if (Test-Path $srcSub) {
        Copy-Item -Recurse -Force $srcSub (Join-Path $stage $sub)
        Write-Host "  staged $sub/"
    }
}

Write-Host "CEF $cefVersion ($Distribution) staged at $stage"
