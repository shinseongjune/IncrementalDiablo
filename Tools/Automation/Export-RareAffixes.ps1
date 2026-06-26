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

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot "GameDesign\Balance\RareAffixPool.csv"
}

$modelPath = Join-Path $ProjectRoot "Assets\02.Scripts\Items\ItemEconomyModel.cs"
if (-not (Test-Path -LiteralPath $modelPath)) {
    throw "Item economy model not found: $modelPath"
}

$modelText = [System.IO.File]::ReadAllText($modelPath)
$affixPattern = @'
new\s+ItemAffixProfile\(\s*"(?<id>[^"]+)",\s*"(?<display>[^"]+)",\s*ItemSlot\.(?<slot>[A-Za-z0-9_]+),\s*StatId\.(?<stat>[A-Za-z0-9_]+),\s*StatMod\.StatModType\.(?<modifierType>[A-Za-z0-9_]+),\s*(?<baseValue>[0-9]+(?:\.[0-9]+)?)f?,\s*(?<perItemLevel>[0-9]+(?:\.[0-9]+)?)f?,\s*(?<perRolledPower>[0-9]+(?:\.[0-9]+)?)f?,\s*(?<weight>[0-9]+),\s*"(?<tags>[^"]+)"\s*\)
'@

$matches = [regex]::Matches($modelText, $affixPattern)
if ($matches.Count -lt 6) {
    throw "Rare affix pool must contain at least six authored entries."
}

$ids = New-Object 'System.Collections.Generic.HashSet[string]'
$slotCounts = @{}
$rows = foreach ($match in $matches) {
    $id = $match.Groups["id"].Value
    $slot = $match.Groups["slot"].Value
    $weight = [int]$match.Groups["weight"].Value
    $tags = $match.Groups["tags"].Value

    if (-not $ids.Add($id)) {
        throw "Duplicate rare affix id: $id"
    }

    if ($weight -le 0) {
        throw "Rare affix weight must be positive: $id"
    }

    if (-not $tags.ToLowerInvariant().Contains($slot.ToLowerInvariant())) {
        throw "Rare affix tags must include the slot '$slot': $id"
    }

    if (-not $slotCounts.ContainsKey($slot)) {
        $slotCounts[$slot] = 0
    }

    $slotCounts[$slot]++

    [PSCustomObject]@{
        affix_id = $id
        display_name = $match.Groups["display"].Value
        denominator = "per-paid Rare affix reroll"
        slot = $slot
        stat = $match.Groups["stat"].Value
        modifier_type = $match.Groups["modifierType"].Value
        base_value = $match.Groups["baseValue"].Value
        per_item_level = $match.Groups["perItemLevel"].Value
        per_rolled_power = $match.Groups["perRolledPower"].Value
        weight = $weight
        tags = $tags
        roll_formula = "ceil(base_value + item_level * per_item_level + rolled_power * per_rolled_power)"
        source = "ItemEconomyModel.AuthoredRareAffixes"
    }
}

foreach ($requiredSlot in @("Weapon", "Armor", "Ring")) {
    if (-not $slotCounts.ContainsKey($requiredSlot) -or $slotCounts[$requiredSlot] -lt 2) {
        throw "Rare affix pool must include at least two entries for $requiredSlot."
    }
}

$csvLines = @($rows | Sort-Object slot, affix_id | ConvertTo-Csv -NoTypeInformation)
$csvText = ($csvLines -join [Environment]::NewLine) + [Environment]::NewLine

if ($CheckOnly) {
    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "Rare affix CSV missing: $OutputPath"
    }

    $existingText = [System.IO.File]::ReadAllText($OutputPath)
    if ($existingText -ne $csvText) {
        throw "Rare affix CSV is stale. Run Tools\Automation\Export-RareAffixes.ps1."
    }

    Write-Host "Rare affix check passed for $($rows.Count) authored affixes."
    exit 0
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllText($OutputPath, $csvText)
Write-Host "Exported rare affix rows: $OutputPath"
Write-Host "Rare affix check passed for $($rows.Count) authored affixes."
