[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$OutputPath,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")
} else {
    $ProjectRoot = Resolve-Path -LiteralPath $ProjectRoot
}

$modelPath = Join-Path $ProjectRoot "Assets\02.Scripts\Dungeon\DungeonEncounterModel.cs"
if (-not (Test-Path -LiteralPath $modelPath)) {
    throw "Dungeon encounter model not found: $modelPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot "GameDesign\Balance\DungeonEncounterBalance.csv"
}

$modelText = [System.IO.File]::ReadAllText($modelPath)
$encounterPattern = @'
new\s+DungeonEncounterProfile\(\s*"(?<id>[^"]+)",\s*"(?<display>[^"]+)",\s*"(?<description>[^"]*)",\s*(?<elite>true|false),\s*(?<boss>true|false),\s*(?<hp>[0-9]+(?:\.[0-9]+)?)f,\s*(?<damage>[0-9]+(?:\.[0-9]+)?)f,\s*(?<rewardDepthOffset>[0-9]+)\s*\)
'@
$matches = [regex]::Matches($modelText, $encounterPattern)
if ($matches.Count -lt 3) {
    throw "Dungeon encounter starter set must contain at least three encounters."
}

$seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
$rows = foreach ($match in $matches) {
    $id = $match.Groups["id"].Value
    $display = $match.Groups["display"].Value
    $description = $match.Groups["description"].Value
    $isElite = [bool]::Parse($match.Groups["elite"].Value)
    $isBoss = [bool]::Parse($match.Groups["boss"].Value)
    $hp = [double]::Parse($match.Groups["hp"].Value, [Globalization.CultureInfo]::InvariantCulture)
    $damage = [double]::Parse($match.Groups["damage"].Value, [Globalization.CultureInfo]::InvariantCulture)
    $rewardDepthOffset = [int]::Parse($match.Groups["rewardDepthOffset"].Value, [Globalization.CultureInfo]::InvariantCulture)

    if (-not $seen.Add($id)) {
        throw "Duplicate dungeon encounter id: $id"
    }

    if ($hp -lt 1 -or $damage -lt 1) {
        throw "Dungeon encounter multipliers must be at least baseline x1: $id"
    }

    if ($isElite -and $isBoss) {
        throw "Dungeon encounter cannot be both elite and boss: $id"
    }

    $kind = if ($isBoss) { "Boss" } elseif ($isElite) { "Elite" } else { "Normal" }
    [PSCustomObject]@{
        encounter_id = $id
        display_name = $display
        kind = $kind
        denominator = "per dungeon run spawned encounter"
        enemy_health_multiplier = $hp.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)
        enemy_damage_multiplier = $damage.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)
        reward_depth_offset = $rewardDepthOffset
        generated_rule_source = "DungeonEncounterModel.BuildEncounter(selectedDepth, encounterSeed, selectedContractId)"
        description = $description
    }
}

if (-not ($seen.Contains("crypt_skirmish"))) {
    throw "Dungeon encounters must include crypt_skirmish as the baseline."
}

if (-not @($rows | Where-Object { $_.kind -eq "Elite" }).Count) {
    throw "Dungeon encounters must include at least one elite rule."
}

if (-not @($rows | Where-Object { $_.kind -eq "Boss" }).Count) {
    throw "Dungeon encounters must include at least one boss rule."
}

$csvLines = @($rows | Sort-Object encounter_id | ConvertTo-Csv -NoTypeInformation)
if ($CheckOnly) {
    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "Dungeon encounter CSV missing: $OutputPath"
    }

    $existing = [System.IO.File]::ReadAllLines($OutputPath)
    $expected = $csvLines
    if (($existing -join "`n") -ne ($expected -join "`n")) {
        throw "Dungeon encounter CSV is stale. Run Tools\Automation\Export-DungeonEncounters.ps1."
    }

    Write-Host "Dungeon encounter check passed for $($rows.Count) starter encounters."
    exit 0
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

[System.IO.File]::WriteAllLines($OutputPath, $csvLines)
Write-Host "Exported dungeon encounter rows: $OutputPath"
Write-Host "Dungeon encounter check passed for $($rows.Count) starter encounters."
