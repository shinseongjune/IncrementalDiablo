[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [switch]$IncludeDocs,
    [switch]$SummaryOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")
} else {
    $ProjectRoot = Resolve-Path -LiteralPath $ProjectRoot
}

function Get-RelativeProjectPath {
    param([string]$Path)

    $root = ([string]$ProjectRoot).TrimEnd("\", "/")
    $fullPath = (Resolve-Path -LiteralPath $Path).Path
    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length).TrimStart("\", "/")
    }

    return $fullPath
}

$scanRoots = @("Assets\02.Scripts")
if ($IncludeDocs) {
    $scanRoots += @("GameDesign\ProductionDocs", "GameDesign")
}

$allowedExtensions = @(".cs")
if ($IncludeDocs) {
    $allowedExtensions += ".md"
}

$patternDefinitions = @(
    @{ Kind = "Prototype"; Pattern = "\bprototype\b|MVP temporary|\btemporary\b" },
    @{ Kind = "Fallback"; Pattern = "\bfallback\b" },
    @{ Kind = "Debug"; Pattern = "\bdebug\b|OnGUI|SmokeTest|Smoke Test" },
    @{ Kind = "BuildGated"; Pattern = "UNITY_EDITOR|DEVELOPMENT_BUILD" },
    @{ Kind = "Todo"; Pattern = "\bTODO\b|\bFIXME\b" }
)

$files = @()
foreach ($root in $scanRoots) {
    $path = Join-Path $ProjectRoot $root
    if (Test-Path -LiteralPath $path) {
        $files += Get-ChildItem -LiteralPath $path -Recurse -File |
            Where-Object { $allowedExtensions -contains $_.Extension }
    }
}

$matches = @()
foreach ($file in $files) {
    foreach ($definition in $patternDefinitions) {
        $scanMatches = Select-String -LiteralPath $file.FullName -Pattern $definition.Pattern
        foreach ($match in $scanMatches) {
            $lineText = ($match.Line.Trim() -replace "\s+", " ")
            $matches += [PSCustomObject]@{
                Kind = $definition.Kind
                Path = Get-RelativeProjectPath $file.FullName
                Line = $match.LineNumber
                Text = $lineText
            }
        }
    }
}

$matchCount = @($matches).Count
$fileCount = @($matches | Select-Object -ExpandProperty Path -Unique).Count

Write-Host "Prototype/debug/fallback debt inventory"
Write-Host "Project root: $ProjectRoot"
Write-Host "Scope: $($scanRoots -join ', ')"
Write-Host "Markers: $matchCount across $fileCount files"
Write-Host ""

if ($matchCount -eq 0) {
    Write-Host "No markers found."
    exit 0
}

$matches |
    Group-Object Kind |
    Sort-Object Name |
    ForEach-Object {
        Write-Host ("{0}: {1}" -f $_.Name, $_.Count)
    }

if (-not $SummaryOnly) {
    Write-Host ""
    $matches |
        Sort-Object Path, Line, Kind |
        Format-Table Kind, Path, Line, Text -AutoSize
}

exit 0
