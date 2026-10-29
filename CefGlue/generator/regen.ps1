<#
.SYNOPSIS
  Regenerate the ZgfCef P/Invoke interop from the pinned CEF 150 headers.

.DESCRIPTION
  Runs the vendored CefGlue interop generator (forked for CEF 150 — see
  cef150-generator.patch and README.md) against cef/<rid>/include, writing the
  generated *.g.cs into the target CefGlue-shaped managed project.

  The generator resolves CEF's API-version gates (CEF_API_ADDED/REMOVED) against
  CEF_API_VERSION (default 15000 = CEF_API_VERSION_LAST for this payload) so the
  emitted vtables/structs are byte-exact for the release libcef.dll's ABI.

  Prereqs: python 3 on PATH, and cef/<rid>/include present (run ../fetch-cef.ps1).

.PARAMETER Rid
  RID whose staged headers to read. Default win-x64.

.PARAMETER OutDir
  The CefGlue-shaped managed project to write into (the vendored fork).
  Default: ../Xilium.CefGlue (Phase 3).

.PARAMETER ApiVersion
  Override the pinned CEF_API_VERSION the gates resolve against.
#>
[CmdletBinding()]
param(
    [string]$Rid = 'win-x64',
    [string]$OutDir,
    [int]$ApiVersion = 15000
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$includeDir = Join-Path $here "../cef/$Rid/include" | Resolve-Path
if (-not $OutDir) { $OutDir = Join-Path $here '../Xilium.CefGlue' }
if (-not (Test-Path $OutDir)) { throw "OutDir not found: $OutDir (Phase 3 vendors the managed lib there)" }
$OutDir = Resolve-Path $OutDir

$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) { throw 'python 3 not found on PATH' }
$python = $pythonCommand.Source

$env:CEF_API_VERSION = "$ApiVersion"
Write-Host "Regenerating interop: CEF_API_VERSION=$ApiVersion"
Write-Host "  headers: $includeDir"
Write-Host "  output : $OutDir"

Push-Location $here
try {
    & $python -B cefglue_interop_gen.py --cpp-header-dir $includeDir --cefglue-dir $OutDir --no-backup
    if ($LASTEXITCODE -ne 0) { throw "generator failed ($LASTEXITCODE)" }
}
finally {
    Pop-Location
    Remove-Item Env:CEF_API_VERSION -ErrorAction SilentlyContinue
}
Write-Host 'Interop regenerated.'
