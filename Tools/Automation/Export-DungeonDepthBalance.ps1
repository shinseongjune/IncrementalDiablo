[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [ValidateRange(1, 10000)]
    [int]$MaxDepth = 100,
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

$modelPath = Join-Path $ProjectRoot "Assets\02.Scripts\Dungeon\DungeonDepthBalanceModel.cs"
if (-not (Test-Path -LiteralPath $modelPath)) {
    throw "Dungeon depth balance model not found: $modelPath"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ProjectRoot "GameDesign\Balance\DungeonDepthBalance.csv"
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

$depthsPerBand = Get-ModelConstant "DepthsPerBand" -AsInteger
$enemyHealthBandMultiplier = Get-ModelConstant "EnemyHealthBandMultiplier"
$enemyHealthStepPerDepth = Get-ModelConstant "EnemyHealthStepPerDepth"
$enemyDamageBandMultiplier = Get-ModelConstant "EnemyDamageBandMultiplier"
$enemyDamageStepPerDepth = Get-ModelConstant "EnemyDamageStepPerDepth"
$rewardPowerBandMultiplier = Get-ModelConstant "RewardPowerBandMultiplier"
$rewardPowerStepPerDepth = Get-ModelConstant "RewardPowerStepPerDepth"
$materialYieldBandMultiplier = Get-ModelConstant "MaterialYieldBandMultiplier"
$materialYieldStepPerDepth = Get-ModelConstant "MaterialYieldStepPerDepth"
$maxScaleMultiplier = Get-ModelConstant "MaxScaleMultiplier"

function Get-Scale {
    param(
        [double]$BandMultiplier,
        [double]$StepPerDepth,
        [int]$BandIndex,
        [int]$DepthInBand
    )

    $scale = [Math]::Pow([Math]::Max(1.0, $BandMultiplier), [Math]::Max(0, $BandIndex)) *
        (1.0 + [Math]::Max(0.0, $StepPerDepth) * [Math]::Max(0, $DepthInBand))
    return [Math]::Min($maxScaleMultiplier, [Math]::Max(1.0, $scale))
}

function Get-ScaledMaterial {
    param(
        [int]$BaseAmount,
        [double]$Multiplier
    )

    if ($BaseAmount -le 0) {
        return 0
    }

    return [Math]::Max(1, [int][Math]::Round($BaseAmount * $Multiplier))
}

$rows = @()
$previous = $null
for ($depth = 1; $depth -le $MaxDepth; $depth++) {
    $zeroBasedDepth = $depth - 1
    $bandIndex = [Math]::Floor($zeroBasedDepth / $depthsPerBand)
    $depthInBand = $zeroBasedDepth % $depthsPerBand
    $enemyHealth = Get-Scale $enemyHealthBandMultiplier $enemyHealthStepPerDepth $bandIndex $depthInBand
    $enemyDamage = Get-Scale $enemyDamageBandMultiplier $enemyDamageStepPerDepth $bandIndex $depthInBand
    $rewardPower = Get-Scale $rewardPowerBandMultiplier $rewardPowerStepPerDepth $bandIndex $depthInBand
    $materialYield = Get-Scale $materialYieldBandMultiplier $materialYieldStepPerDepth $bandIndex $depthInBand

    $row = [PSCustomObject]@{
        depth = $depth
        band = $bandIndex + 1
        depth_in_band = $depthInBand
        enemy_health_multiplier = [Math]::Round($enemyHealth, 6)
        enemy_damage_multiplier = [Math]::Round($enemyDamage, 6)
        reward_power_multiplier = [Math]::Round($rewardPower, 6)
        material_yield_multiplier = [Math]::Round($materialYield, 6)
        sample_power_from_2 = [int][Math]::Ceiling(2 * $rewardPower)
        sample_power_from_5 = [int][Math]::Ceiling(5 * $rewardPower)
        sample_normal_weapon_scrap = Get-ScaledMaterial 4 $materialYield
        sample_magic_weapon_scrap = Get-ScaledMaterial 8 $materialYield
        sample_magic_essence = Get-ScaledMaterial 1 $materialYield
        sample_rare_weapon_scrap = Get-ScaledMaterial 16 $materialYield
        sample_rare_essence = Get-ScaledMaterial 2 $materialYield
        sample_rare_alter_stone = Get-ScaledMaterial 1 $materialYield
    }

    if ($depth -eq 1) {
        foreach ($field in @(
            "enemy_health_multiplier",
            "enemy_damage_multiplier",
            "reward_power_multiplier",
            "material_yield_multiplier"
        )) {
            if ([Math]::Abs([double]$row.$field - 1.0) -gt 0.000001) {
                throw "Depth 1 baseline failed for $field."
            }
        }
    }

    if ($previous -ne $null) {
        foreach ($field in @(
            "enemy_health_multiplier",
            "enemy_damage_multiplier",
            "reward_power_multiplier",
            "material_yield_multiplier"
        )) {
            if ([double]$row.$field -lt [double]$previous.$field) {
                throw "Balance curve decreased at depth $depth for $field."
            }
        }
    }

    if ($depthInBand -lt 0 -or $depthInBand -ge $depthsPerBand) {
        throw "Depth $depth resolved outside the configured band."
    }

    $rows += $row
    $previous = $row
}

if ($MaxDepth -ge 2 -and
    ([double]$rows[1].enemy_health_multiplier -le 1.0 -or
     [double]$rows[1].reward_power_multiplier -le 1.0)) {
    throw "Depth 2 must increase both threat and reward power."
}

if (-not $CheckOnly) {
    $outputDirectory = Split-Path -Parent $OutputPath
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    $csvLines = @($rows | ConvertTo-Csv -NoTypeInformation)
    [System.IO.File]::WriteAllLines(
        $OutputPath,
        $csvLines,
        [System.Text.UTF8Encoding]::new($false))
    Write-Host "Exported dungeon depth balance rows: $OutputPath"
}

$last = $rows[$rows.Count - 1]
Write-Host (
    "Dungeon depth balance check passed for depths 1-{0}. Final multipliers: HP x{1}, damage x{2}, reward x{3}, materials x{4}." -f
    $MaxDepth,
    $last.enemy_health_multiplier,
    $last.enemy_damage_multiplier,
    $last.reward_power_multiplier,
    $last.material_yield_multiplier)
