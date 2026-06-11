[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [ValidateRange(1, 100000)]
    [int]$MaxLevel = 1000,
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

$modelPath = Join-Path $ProjectRoot "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseBalanceModel.cs"
if (-not (Test-Path -LiteralPath $modelPath)) {
    throw "Ground defense balance model not found: $modelPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot "GameDesign\Balance\GroundDefenseBalance.csv"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot $OutputPath
}

$modelText = [System.IO.File]::ReadAllText($modelPath)

function Get-ModelConstant {
    param(
        [string]$Name,
        [switch]$AsInteger
    )

    $pattern = "public const (?:int|float) $([regex]::Escape($Name)) = (?<value>[0-9]+(?:\.[0-9]+)?)f?;"
    $match = [regex]::Match($modelText, $pattern)
    if (-not $match.Success) {
        throw "Could not read model constant '$Name' from $modelPath"
    }

    if ($AsInteger) {
        return [int]::Parse($match.Groups["value"].Value, [Globalization.CultureInfo]::InvariantCulture)
    }

    return [double]::Parse($match.Groups["value"].Value, [Globalization.CultureInfo]::InvariantCulture)
}

$levelsPerBand = Get-ModelConstant "LevelsPerBand" -AsInteger
$incomingPressureBandMultiplier = Get-ModelConstant "IncomingPressureBandMultiplier"
$incomingPressureStepPerLevel = Get-ModelConstant "IncomingPressureStepPerLevel"
$defenseOutputBandMultiplier = Get-ModelConstant "DefenseOutputBandMultiplier"
$defenseOutputStepPerLevel = Get-ModelConstant "DefenseOutputStepPerLevel"
$pressureCapacityBandMultiplier = Get-ModelConstant "PressureCapacityBandMultiplier"
$pressureCapacityStepPerLevel = Get-ModelConstant "PressureCapacityStepPerLevel"
$progressRequirementBandMultiplier = Get-ModelConstant "ProgressRequirementBandMultiplier"
$progressRequirementStepPerLevel = Get-ModelConstant "ProgressRequirementStepPerLevel"
$rewardBandMultiplier = Get-ModelConstant "RewardBandMultiplier"
$rewardStepPerLevel = Get-ModelConstant "RewardStepPerLevel"
$baseMilestoneGoldReward = Get-ModelConstant "BaseMilestoneGoldReward" -AsInteger
$baseMilestoneScrapReward = Get-ModelConstant "BaseMilestoneScrapReward" -AsInteger
$milestoneRewardBandMultiplier = Get-ModelConstant "MilestoneRewardBandMultiplier"
$maxScaleMultiplier = Get-ModelConstant "MaxScaleMultiplier"

function Get-Scale {
    param(
        [double]$BandMultiplier,
        [double]$StepPerLevel,
        [int]$BandIndex,
        [int]$LevelInBand
    )

    $scale = [Math]::Pow([Math]::Max(1.0, $BandMultiplier), [Math]::Max(0, $BandIndex)) *
        (1.0 + [Math]::Max(0.0, $StepPerLevel) * [Math]::Max(0, $LevelInBand))
    return [Math]::Min($maxScaleMultiplier, [Math]::Max(1.0, $scale))
}

function Get-MilestoneReward {
    param(
        [int]$BaseAmount,
        [int]$BandNumber
    )

    if ($BandNumber -le 1) {
        return 0
    }

    $scale = [Math]::Pow(
        [Math]::Max(1.0, $milestoneRewardBandMultiplier),
        [Math]::Max(0, $BandNumber - 2))
    $scaled = [Math]::Ceiling([Math]::Max(0, $BaseAmount) * [Math]::Min($maxScaleMultiplier, $scale))
    if ($scaled -ge [int]::MaxValue) {
        return [int]::MaxValue
    }

    return [int]$scaled
}

$rows = @()
$previous = $null
for ($level = 1; $level -le $MaxLevel; $level++) {
    $zeroBasedLevel = $level - 1
    $bandIndex = [Math]::Floor($zeroBasedLevel / $levelsPerBand)
    $levelInBand = $zeroBasedLevel % $levelsPerBand
    $bandNumber = $bandIndex + 1
    $pressure = Get-Scale $incomingPressureBandMultiplier $incomingPressureStepPerLevel $bandIndex $levelInBand
    $defense = Get-Scale $defenseOutputBandMultiplier $defenseOutputStepPerLevel $bandIndex $levelInBand
    $capacity = Get-Scale $pressureCapacityBandMultiplier $pressureCapacityStepPerLevel $bandIndex $levelInBand
    $progress = Get-Scale $progressRequirementBandMultiplier $progressRequirementStepPerLevel $bandIndex $levelInBand
    $reward = Get-Scale $rewardBandMultiplier $rewardStepPerLevel $bandIndex $levelInBand
    $isBandStart = $levelInBand -eq 0

    $row = [PSCustomObject]@{
        frontline_level = $level
        band = $bandNumber
        level_in_band = $levelInBand
        incoming_pressure_multiplier = [Math]::Round($pressure, 6)
        defense_output_multiplier = [Math]::Round($defense, 6)
        pressure_capacity_multiplier = [Math]::Round($capacity, 6)
        progress_requirement_multiplier = [Math]::Round($progress, 6)
        reward_multiplier = [Math]::Round($reward, 6)
        sample_push_pressure_per_second = [Math]::Round(10 * $pressure * 1.3, 3)
        sample_base_defense_per_second = [Math]::Round(13 * $defense, 3)
        sample_push_gold_per_minute = [Math]::Round(30 * $reward * 1.15, 3)
        sample_push_scrap_per_minute = [Math]::Round(4 * $reward * 1.15, 3)
        milestone_gold = if ($isBandStart) { Get-MilestoneReward $baseMilestoneGoldReward $bandNumber } else { 0 }
        milestone_scrap = if ($isBandStart) { Get-MilestoneReward $baseMilestoneScrapReward $bandNumber } else { 0 }
        next_band_level = $bandNumber * $levelsPerBand + 1
    }

    if ($level -eq 1) {
        foreach ($field in @(
            "incoming_pressure_multiplier",
            "defense_output_multiplier",
            "pressure_capacity_multiplier",
            "progress_requirement_multiplier",
            "reward_multiplier"
        )) {
            if ([Math]::Abs([double]$row.$field - 1.0) -gt 0.000001) {
                throw "Frontline Level 1 baseline failed for $field."
            }
        }
    }

    if ($previous -ne $null) {
        foreach ($field in @(
            "incoming_pressure_multiplier",
            "defense_output_multiplier",
            "pressure_capacity_multiplier",
            "progress_requirement_multiplier",
            "reward_multiplier"
        )) {
            if ([double]$row.$field -lt [double]$previous.$field) {
                throw "Ground defense curve decreased at Frontline Level $level for $field."
            }
        }
    }

    if ($isBandStart -and $bandNumber -gt 1 -and
        ($row.milestone_gold -le 0 -or $row.milestone_scrap -le 0)) {
        throw "Band $bandNumber does not grant both milestone resources."
    }

    $rows += $row
    $previous = $row
}

if ($MaxLevel -ge 2 -and
    ([double]$rows[1].incoming_pressure_multiplier -le 1.0 -or
     [double]$rows[1].defense_output_multiplier -le 1.0 -or
     [double]$rows[1].reward_multiplier -le 1.0)) {
    throw "Frontline Level 2 must increase pressure, defense output, and rewards."
}

if (-not $CheckOnly) {
    $outputDirectory = Split-Path -Parent $OutputPath
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    $csvLines = @($rows | ConvertTo-Csv -NoTypeInformation)
    [System.IO.File]::WriteAllLines(
        $OutputPath,
        $csvLines,
        [System.Text.UTF8Encoding]::new($false))
    Write-Host "Exported ground defense balance rows: $OutputPath"
}

$last = $rows[$rows.Count - 1]
Write-Host (
    "Ground defense balance check passed for Frontline Levels 1-{0}. Final multipliers: pressure x{1}, defense x{2}, rewards x{3}; band {4}." -f
    $MaxLevel,
    $last.incoming_pressure_multiplier,
    $last.defense_output_multiplier,
    $last.reward_multiplier,
    $last.band)
