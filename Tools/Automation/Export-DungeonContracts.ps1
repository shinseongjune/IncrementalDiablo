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

$modelPath = Join-Path $ProjectRoot "Assets\02.Scripts\Dungeon\DungeonContractModel.cs"
if (-not (Test-Path -LiteralPath $modelPath)) {
    throw "Dungeon contract model not found: $modelPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot "GameDesign\Balance\DungeonContractBalance.csv"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot $OutputPath
}

$modelText = [System.IO.File]::ReadAllText($modelPath)
$contractPattern = @'
new\s+DungeonContractProfile\(\s*"(?<id>[^"]+)",\s*"(?<display>[^"]+)",\s*"(?<description>[^"]*)",\s*(?<hp>[0-9]+(?:\.[0-9]+)?)f,\s*(?<damage>[0-9]+(?:\.[0-9]+)?)f,\s*(?<rewardDepthOffset>[0-9]+)\s*\)
'@

$matches = [regex]::Matches($modelText, $contractPattern)
if ($matches.Count -lt 3) {
    throw "Dungeon contract starter set must contain at least three contracts."
}

$rows = @()
$ids = New-Object "System.Collections.Generic.HashSet[string]"
$hasBaseline = $false
$hasRiskReward = $false

foreach ($match in $matches) {
    $id = $match.Groups["id"].Value
    if (-not $ids.Add($id)) {
        throw "Duplicate dungeon contract id: $id"
    }

    $hp = [double]::Parse($match.Groups["hp"].Value, [Globalization.CultureInfo]::InvariantCulture)
    $damage = [double]::Parse($match.Groups["damage"].Value, [Globalization.CultureInfo]::InvariantCulture)
    $rewardDepthOffset = [int]::Parse($match.Groups["rewardDepthOffset"].Value, [Globalization.CultureInfo]::InvariantCulture)

    if ($hp -le 0 -or $damage -le 0) {
        throw "Contract $id has an invalid threat multiplier."
    }

    if ($rewardDepthOffset -lt 0) {
        throw "Contract $id has an invalid reward depth offset."
    }

    if ($id -eq "steady_clear" -and
        [Math]::Abs($hp - 1.0) -lt 0.000001 -and
        [Math]::Abs($damage - 1.0) -lt 0.000001 -and
        $rewardDepthOffset -eq 0) {
        $hasBaseline = $true
    }

    if (($hp -gt 1.0 -or $damage -gt 1.0) -and $rewardDepthOffset -gt 0) {
        $hasRiskReward = $true
    }

    $rows += [PSCustomObject]@{
        contract_id = $id
        display_name = $match.Groups["display"].Value
        denominator = "per-clear guaranteed item reward"
        enemy_health_multiplier = [Math]::Round($hp, 6)
        enemy_damage_multiplier = [Math]::Round($damage, 6)
        reward_depth_offset = $rewardDepthOffset
        generated_offer_source = "DungeonContractModel.BuildOffer(selectedDepth, contractOfferSeed)"
        notes = $match.Groups["description"].Value
    }
}

if (-not $hasBaseline) {
    throw "Dungeon contracts must include steady_clear as the no-added-risk baseline."
}

if (-not $hasRiskReward) {
    throw "Dungeon contracts must include at least one risk/reward contract."
}

if (-not $CheckOnly) {
    $outputDirectory = Split-Path -Parent $OutputPath
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    $csvLines = @($rows | ConvertTo-Csv -NoTypeInformation)
    [System.IO.File]::WriteAllLines(
        $OutputPath,
        $csvLines,
        [System.Text.UTF8Encoding]::new($false))
    Write-Host "Exported dungeon contract rows: $OutputPath"
}

Write-Host "Dungeon contract check passed for $($rows.Count) starter contracts."
