#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$SlangVersion = if ($env:SLANG_VERSION) { $env:SLANG_VERSION } else { '2026.7.1' }

$ScriptDir  = $PSScriptRoot
$ProjectDir = Split-Path $ScriptDir -Parent
$NativeDir  = Join-Path $ProjectDir 'Native'

$arch = if ([Environment]::Is64BitOperatingSystem -and $env:PROCESSOR_ARCHITECTURE -eq 'ARM64') {
    'aarch64'
} else {
    'x86_64'
}

$ridArch      = if ($arch -eq 'aarch64') { 'arm64' } else { 'x64' }
$archive      = "slang-$SlangVersion-windows-$arch.zip"
$targetDir    = Join-Path $NativeDir "win-$ridArch"
$sourceSubdir = 'bin'
$filePattern  = '*.dll'

$stampFile = Join-Path $targetDir '.slang-version'
if ((Test-Path $stampFile) -and ((Get-Content $stampFile -Raw).Trim() -eq $SlangVersion)) {
    exit 0
}

$url    = "https://github.com/shader-slang/slang/releases/download/v$SlangVersion/$archive"
$tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tmpDir | Out-Null

try {
    Write-Host "fetch-slang: downloading Slang $SlangVersion (Windows-$arch)"
    Write-Host "  $url"

    $archivePath = Join-Path $tmpDir 'slang.zip'
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $archivePath -UseBasicParsing

    $extractDir = Join-Path $tmpDir 'extract'
    Expand-Archive -Path $archivePath -DestinationPath $extractDir -Force

    $sourceDir = Join-Path $extractDir $sourceSubdir
    if (-not (Test-Path $sourceDir)) {
        $nested = Get-ChildItem $extractDir -Directory |
            ForEach-Object { Join-Path $_.FullName $sourceSubdir } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($nested) { $sourceDir = $nested }
    }

    if (-not (Test-Path $sourceDir)) {
        throw "could not find $sourceSubdir/ in extracted archive"
    }

    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    $files = Get-ChildItem $sourceDir -Filter $filePattern
    if ($files.Count -eq 0) {
        throw "no $filePattern files in $sourceDir"
    }

    $files | Copy-Item -Destination $targetDir -Force
    Set-Content -Path $stampFile -Value $SlangVersion -NoNewline

    Write-Host "fetch-slang: installed $($files.Count) file(s) to $targetDir"
} finally {
    Remove-Item -Recurse -Force $tmpDir -ErrorAction SilentlyContinue
}
