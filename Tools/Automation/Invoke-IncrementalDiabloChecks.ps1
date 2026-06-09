[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$AutomationTomlPath,
    [switch]$SkipBuild,
    [switch]$SkipDiffCheck
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")
} else {
    $ProjectRoot = Resolve-Path -LiteralPath $ProjectRoot
}

if ([string]::IsNullOrWhiteSpace($AutomationTomlPath)) {
    $codexHome = $env:CODEX_HOME
    if ([string]::IsNullOrWhiteSpace($codexHome)) {
        $codexHome = Join-Path $HOME ".codex"
    }

    $AutomationTomlPath = Join-Path $codexHome "automations\incrementaldiablo-daily-production-review\automation.toml"
}

$script:Results = @()

function Add-Result {
    param(
        [string]$Name,
        [ValidateSet("PASS", "WARN", "FAIL", "SKIP")]
        [string]$Status,
        [string]$Details = ""
    )

    $script:Results += [PSCustomObject]@{
        Status = $Status
        Name = $Name
        Details = $Details
    }
}

function Join-ProjectPath {
    param([string]$RelativePath)
    return Join-Path $ProjectRoot $RelativePath
}

function Read-TextFile {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Assert-PathExists {
    param(
        [string]$Name,
        [string]$RelativePath
    )

    $path = Join-ProjectPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        Add-Result $Name "PASS" $RelativePath
        return $true
    }

    Add-Result $Name "FAIL" "Missing: $RelativePath"
    return $false
}

function Assert-TextContains {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Needle,
        [ValidateSet("PASS", "WARN", "FAIL")]
        [string]$MissingStatus = "FAIL"
    )

    if ($Text.Contains($Needle)) {
        Add-Result $Name "PASS" $Needle
        return $true
    }

    Add-Result $Name $MissingStatus "Missing token: $Needle"
    return $false
}

function Get-SceneBehaviourBlocks {
    param(
        [string]$SceneText,
        [string]$ClassName
    )

    $escapedClassName = [regex]::Escape($ClassName)
    $pattern = "(?ms)^--- !u!114 &\d+\r?\nMonoBehaviour:.*?m_EditorClassIdentifier: Assembly-CSharp::${escapedClassName}.*?(?=^--- |\z)"
    return [regex]::Matches($SceneText, $pattern)
}

function Assert-SceneBehaviourReference {
    param(
        [string]$Name,
        [string]$SceneText,
        [string]$ClassName,
        [string]$FieldName
    )

    $blocks = @(Get-SceneBehaviourBlocks $SceneText $ClassName)
    if ($blocks.Count -eq 0) {
        Add-Result $Name "FAIL" "Missing MonoBehaviour: $ClassName"
        return $false
    }

    $escapedFieldName = [regex]::Escape($FieldName)
    $pattern = "(?m)^\s*${escapedFieldName}:\s+\{fileID:\s+(?!0\})\d+"
    foreach ($block in $blocks) {
        if ($block.Value -match $pattern) {
            Add-Result $Name "PASS" "$ClassName.$FieldName is wired."
            return $true
        }
    }

    Add-Result $Name "FAIL" "$ClassName.$FieldName is not wired."
    return $false
}

function Invoke-CheckedCommand {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Push-Location $ProjectRoot
    try {
        & $Command
        $exitCode = $LASTEXITCODE
        if ($null -eq $exitCode) {
            $exitCode = 0
        }

        if ($exitCode -eq 0) {
            Add-Result $Name "PASS" "Exit code 0"
        } else {
            Add-Result $Name "FAIL" "Exit code $exitCode"
        }
    } catch {
        Add-Result $Name "FAIL" $_.Exception.Message
    } finally {
        Pop-Location
    }
}

Write-Host "IncrementalDiablo automation checks"
Write-Host "Project root: $ProjectRoot"
Write-Host ""

$requiredPaths = @(
    @{ Name = "Solution file"; Path = "IncrementalDiablo.sln" },
    @{ Name = "Gameplay scene"; Path = "Assets\01.Scenes\Gameplay.unity" },
    @{ Name = "Dungeon enemy prefab"; Path = "Assets\04.Prefabs\Dungeon\PF_DungeonEnemy_Melee.prefab" },
    @{ Name = "Enemy spawner script"; Path = "Assets\02.Scripts\Dungeon\EnemySpawner.cs" },
    @{ Name = "Dungeon depth balance model"; Path = "Assets\02.Scripts\Dungeon\DungeonDepthBalanceModel.cs" },
    @{ Name = "Expedition director script"; Path = "Assets\02.Scripts\Dungeon\ExpeditionDirector.cs" },
    @{ Name = "Save data script"; Path = "Assets\02.Scripts\Shared\GameSaveData.cs" },
    @{ Name = "Save diagnostics script"; Path = "Assets\02.Scripts\Shared\GameSaveDataDiagnostics.cs" },
    @{ Name = "Save manager script"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs" },
    @{ Name = "Item definition registry script"; Path = "Assets\02.Scripts\Items\ItemDefinitionRegistry.cs" },
    @{ Name = "Item definition registry asset"; Path = "Assets\05.ScriptableObjects\Items\ItemDefinitionRegistry.asset" },
    @{ Name = "Simple inventory script"; Path = "Assets\02.Scripts\Items\SimpleInventory.cs" },
    @{ Name = "Playable HUD script"; Path = "Assets\02.Scripts\UI\PlayableLoopHud.cs" },
    @{ Name = "Screen layout controller script"; Path = "Assets\02.Scripts\UI\PlayableScreenLayoutController.cs" },
    @{ Name = "Ground defense actor runtime script"; Path = "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseActorRuntime.cs" },
    @{ Name = "Automation plan"; Path = "GameDesign\ProductionDocs\10_PlayableLoopMvpAutomationPlan.md" },
    @{ Name = "Prototype debt register"; Path = "GameDesign\ProductionDocs\12_PrototypeDebtRegister.md" },
    @{ Name = "Scene setup guide"; Path = "GameDesign\ProductionDocs\06_UnitySceneAndPrefabSetupGuide.md" },
    @{ Name = "Script usage guide"; Path = "GameDesign\ProductionDocs\09_BaseScriptUsageGuide.md" },
    @{ Name = "Script folder map"; Path = "GameDesign\ScriptFolderStructure.md" },
    @{ Name = "Prototype debt inventory script"; Path = "Tools\Automation\Get-PrototypeDebtInventory.ps1" },
    @{ Name = "Dungeon depth balance export"; Path = "Tools\Automation\Export-DungeonDepthBalance.ps1" },
    @{ Name = "Dungeon depth balance CSV"; Path = "GameDesign\Balance\DungeonDepthBalance.csv" }
)

foreach ($entry in $requiredPaths) {
    [void](Assert-PathExists $entry.Name $entry.Path)
}

$scenePath = Join-ProjectPath "Assets\01.Scenes\Gameplay.unity"
$enemyPrefabPath = Join-ProjectPath "Assets\04.Prefabs\Dungeon\PF_DungeonEnemy_Melee.prefab"
$enemySpawnerPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\EnemySpawner.cs"
$depthBalanceModelPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\DungeonDepthBalanceModel.cs"
$expeditionDirectorPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\ExpeditionDirector.cs"
$saveDataPath = Join-ProjectPath "Assets\02.Scripts\Shared\GameSaveData.cs"
$saveDiagnosticsPath = Join-ProjectPath "Assets\02.Scripts\Shared\GameSaveDataDiagnostics.cs"
$saveManagerPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs"
$itemRegistryPath = Join-ProjectPath "Assets\02.Scripts\Items\ItemDefinitionRegistry.cs"
$itemRegistryAssetPath = Join-ProjectPath "Assets\05.ScriptableObjects\Items\ItemDefinitionRegistry.asset"
$simpleInventoryPath = Join-ProjectPath "Assets\02.Scripts\Items\SimpleInventory.cs"
$playableHudPath = Join-ProjectPath "Assets\02.Scripts\UI\PlayableLoopHud.cs"
$planPath = Join-ProjectPath "GameDesign\ProductionDocs\10_PlayableLoopMvpAutomationPlan.md"
$debtRegisterPath = Join-ProjectPath "GameDesign\ProductionDocs\12_PrototypeDebtRegister.md"

if (Test-Path -LiteralPath $scenePath) {
    $sceneText = Read-TextFile $scenePath
    $requiredSceneTokens = @(
        @{ Name = "Scene has PlayableScreenLayoutController"; Token = "m_EditorClassIdentifier: Assembly-CSharp::PlayableScreenLayoutController" },
        @{ Name = "Scene has defense side panel"; Token = "m_Name: Panel_DefenseSide" },
        @{ Name = "Scene has dungeon viewport panel"; Token = "m_Name: Panel_DungeonViewport" },
        @{ Name = "HUD syncs screen focus"; Token = "syncScreenFocusWithDungeon: 1" },
        @{ Name = "Scene has ground combat presenter"; Token = "m_EditorClassIdentifier: Assembly-CSharp::GroundDefenseCombatPresenter" },
        @{ Name = "Scene has ground actor runtime"; Token = "m_EditorClassIdentifier: Assembly-CSharp::GroundDefenseActorRuntime" },
        @{ Name = "Scene has dungeon combat room"; Token = "m_EditorClassIdentifier: Assembly-CSharp::CombatRoom" },
        @{ Name = "Scene has enemy spawner"; Token = "m_EditorClassIdentifier: Assembly-CSharp::EnemySpawner" },
        @{ Name = "Scene has NavMesh surface"; Token = "m_EditorClassIdentifier: Unity.AI.Navigation::Unity.AI.Navigation.NavMeshSurface" },
        @{ Name = "Scene NavMesh surface has baked data"; Token = "m_NavMeshData: {fileID: 23800000" },
        @{ Name = "Scene enemy spawns snap to NavMesh"; Token = "snapSpawnPointsToNavMesh: 1" },
        @{ Name = "Scene normal combat disables prototype simulation"; Token = "simulateWhenNoEnemies: 0" },
        @{ Name = "Scene has loot dropper"; Token = "m_EditorClassIdentifier: Assembly-CSharp::LootDropper" },
        @{ Name = "Scene disables runtime loot fallback"; Token = "createPrototypeRewardWhenTableEmpty: 0" },
        @{ Name = "Scene has dungeon RawImage viewport"; Token = "m_Name: RawImage_DungeonViewport" },
        @{ Name = "Scene has dungeon panel camera"; Token = "m_Name: Camera_DungeonPanel" },
        @{ Name = "Scene has dungeon render target"; Token = "m_EditorClassIdentifier: Assembly-CSharp::PanelCameraRenderTarget" },
        @{ Name = "Scene has dungeon input router"; Token = "m_EditorClassIdentifier: Assembly-CSharp::DungeonViewportInputRouter" },
        @{ Name = "Scene has previous dungeon depth button"; Token = "m_Name: Button_DungeonDepthPrevious" },
        @{ Name = "Scene has next dungeon depth button"; Token = "m_Name: Button_DungeonDepthNext" },
        @{ Name = "Scene initializes selected dungeon depth"; Token = "selectedDepth: 1" },
        @{ Name = "Scene initializes highest unlocked dungeon depth"; Token = "highestUnlockedDepth: 1" }
    )

    foreach ($entry in $requiredSceneTokens) {
        [void](Assert-TextContains $entry.Name $sceneText $entry.Token)
    }

    [void](Assert-SceneBehaviourReference "Dungeon render target source camera" $sceneText "PanelCameraRenderTarget" "sourceCamera")
    [void](Assert-SceneBehaviourReference "Dungeon render target RawImage" $sceneText "PanelCameraRenderTarget" "targetImage")
    [void](Assert-SceneBehaviourReference "Dungeon input router RawImage" $sceneText "DungeonViewportInputRouter" "viewportImage")
    [void](Assert-SceneBehaviourReference "Dungeon input router camera" $sceneText "DungeonViewportInputRouter" "viewportCamera")
    [void](Assert-SceneBehaviourReference "Dungeon input router player" $sceneText "DungeonViewportInputRouter" "player")
    [void](Assert-SceneBehaviourReference "Dungeon input router screen layout" $sceneText "DungeonViewportInputRouter" "screenLayout")
    [void](Assert-SceneBehaviourReference "Ground actor runtime defense" $sceneText "GroundDefenseActorRuntime" "defense")
    [void](Assert-SceneBehaviourReference "Ground combat presenter actor runtime" $sceneText "GroundDefenseCombatPresenter" "actorRuntime")
    [void](Assert-SceneBehaviourReference "Playable HUD previous depth button" $sceneText "PlayableLoopHud" "previousDungeonDepthButton")
    [void](Assert-SceneBehaviourReference "Playable HUD next depth button" $sceneText "PlayableLoopHud" "nextDungeonDepthButton")
    [void](Assert-SceneBehaviourReference "Simple inventory item registry" $sceneText "SimpleInventory" "definitionRegistry")

    if ($sceneText -match "m_Script:\s+\{fileID:\s+0\}") {
        Add-Result "Scene missing-script scan" "FAIL" "Gameplay.unity contains at least one MonoBehaviour with m_Script fileID 0."
    } else {
        Add-Result "Scene missing-script scan" "PASS" "No m_Script fileID 0 token found."
    }

    $unwiredOverlays = @()
    if ($sceneText.Contains("inventoryOverlay: {fileID: 0}")) { $unwiredOverlays += "inventoryOverlay" }
    if ($sceneText.Contains("craftingOverlay: {fileID: 0}")) { $unwiredOverlays += "craftingOverlay" }
    if ($sceneText.Contains("rewardOverlay: {fileID: 0}")) { $unwiredOverlays += "rewardOverlay" }

    if ($unwiredOverlays.Count -gt 0) {
        Add-Result "Optional playable screen overlays" "WARN" ("Not wired yet: " + ($unwiredOverlays -join ", "))
    } else {
        Add-Result "Optional playable screen overlays" "PASS" "All overlay references are wired."
    }
}

if ((Test-Path -LiteralPath $expeditionDirectorPath) -and
    (Test-Path -LiteralPath $saveDataPath) -and
    (Test-Path -LiteralPath $saveDiagnosticsPath) -and
    (Test-Path -LiteralPath $saveManagerPath) -and
    (Test-Path -LiteralPath $playableHudPath)) {
    $expeditionDirectorText = Read-TextFile $expeditionDirectorPath
    $saveDataText = Read-TextFile $saveDataPath
    $saveDiagnosticsText = Read-TextFile $saveDiagnosticsPath
    $saveManagerText = Read-TextFile $saveManagerPath
    $playableHudText = Read-TextFile $playableHudPath

    [void](Assert-TextContains "Expedition exposes selected depth" $expeditionDirectorText "public int SelectedDepth")
    [void](Assert-TextContains "Expedition exposes highest unlocked depth" $expeditionDirectorText "public int HighestUnlockedDepth")
    [void](Assert-TextContains "Expedition starts selected depth" $expeditionDirectorText "runtime.depth = SelectedDepth;")
    [void](Assert-TextContains "Expedition unlocks after clear" $expeditionDirectorText "int unlockedDepth = TryUnlockNextDepth();")
    [void](Assert-TextContains "Dungeon save stores selected depth" $saveDataText "public int selectedDepth = 1;")
    [void](Assert-TextContains "Dungeon save stores highest unlocked depth" $saveDataText "public int highestUnlockedDepth = 1;")
    [void](Assert-TextContains "Save manager writes schema v3" $saveManagerText "private const int CurrentSaveVersion = 3;")
    [void](Assert-TextContains "Save manager migrates legacy dungeon depth" $saveManagerText "MigrateSaveData(saveData);")
    [void](Assert-TextContains "Save manager runs item id migration" $saveManagerText "MigrateInventorySaveData")
    [void](Assert-TextContains "Save diagnostics use item registry" $saveDiagnosticsText "ItemDefinitionRegistry definitionRegistry")
    [void](Assert-TextContains "Save diagnostics validate selected depth" $saveDiagnosticsText "dungeon selectedDepth must be within the unlocked depth range")
    [void](Assert-TextContains "Playable HUD exposes previous depth action" $playableHudText "public void SelectPreviousDungeonDepth()")
    [void](Assert-TextContains "Playable HUD exposes next depth action" $playableHudText "public void SelectNextDungeonDepth()")
}

if ((Test-Path -LiteralPath $itemRegistryPath) -and
    (Test-Path -LiteralPath $itemRegistryAssetPath) -and
    (Test-Path -LiteralPath $simpleInventoryPath)) {
    $itemRegistryText = Read-TextFile $itemRegistryPath
    $itemRegistryAssetText = Read-TextFile $itemRegistryAssetPath
    $simpleInventoryText = Read-TextFile $simpleInventoryPath

    [void](Assert-TextContains "Item registry resolves canonical ids" $itemRegistryText "ItemDefinitionResolution.Canonical")
    [void](Assert-TextContains "Item registry supports id migration" $itemRegistryText "ItemDefinitionResolution.Migrated")
    [void](Assert-TextContains "Item registry reports unresolved ids" $itemRegistryText "AddUnresolved")
    [void](Assert-TextContains "Inventory resolves saved definitions through registry" $simpleInventoryText "definitionRegistry.TryResolve")
    [void](Assert-TextContains "Inventory blocks unresolved equipment" $simpleInventoryText "item definition '{item.DefinitionId}' is unresolved")

    $expectedItemDefinitionGuids = @(
        "5c7ea7142bbf48549d79dd5d1dcb9769",
        "4710f92715434bb4aecec1a83d521aa9",
        "f49089b5a6114f4e824cbbb5d8d247a7",
        "11f6d572f53041b2896866876adc9fa5",
        "32d9a563a33d4a299ee2be950495bdb2",
        "0a1eea40b7ab4a30a6653b3db81b6e7c"
    )

    foreach ($guid in $expectedItemDefinitionGuids) {
        [void](Assert-TextContains "Item registry authored definition" $itemRegistryAssetText $guid)
    }
}

if (Test-Path -LiteralPath $enemyPrefabPath) {
    $enemyPrefabText = Read-TextFile $enemyPrefabPath
    $requiredEnemyPrefabTokens = @(
        "m_EditorClassIdentifier: Assembly-CSharp::CharacterActor",
        "m_EditorClassIdentifier: Assembly-CSharp::Health",
        "m_EditorClassIdentifier: Assembly-CSharp::EnemyAIController",
        "NavMeshAgent:",
        "CapsuleCollider:",
        "team: 2"
    )

    foreach ($token in $requiredEnemyPrefabTokens) {
        [void](Assert-TextContains "Dungeon enemy prefab contract" $enemyPrefabText $token)
    }
}

if (Test-Path -LiteralPath $enemySpawnerPath) {
    $enemySpawnerText = Read-TextFile $enemySpawnerPath
    [void](Assert-TextContains "Enemy spawner validates prefab contract" $enemySpawnerText "TryValidateEnemyPrefab")
    [void](Assert-TextContains "Enemy spawner validates NavMesh placement" $enemySpawnerText "NavMesh.SamplePosition")
    [void](Assert-TextContains "Enemy spawner applies depth combat scaling" $enemySpawnerText "SetRuntimeCombatMultipliers")
}

if (Test-Path -LiteralPath $depthBalanceModelPath) {
    $depthBalanceModelText = Read-TextFile $depthBalanceModelPath
    [void](Assert-TextContains "Depth balance uses bounded bands" $depthBalanceModelText "public const int DepthsPerBand = 10;")
    [void](Assert-TextContains "Depth balance exposes enemy health scaling" $depthBalanceModelText "EnemyHealthMultiplier")
    [void](Assert-TextContains "Depth balance exposes reward power scaling" $depthBalanceModelText "RewardPowerMultiplier")
    [void](Assert-TextContains "Depth balance exposes material yield scaling" $depthBalanceModelText "MaterialYieldMultiplier")
}

if (Test-Path -LiteralPath $planPath) {
    $planText = Read-TextFile $planPath
    $requiredPlanTokens = @(
        "Phase Promotion Rule",
        "Visible Game Production Rule",
        "Prototype Debt Sweep Rule",
        "No-Stagnation Rules",
        "Progress Tracker",
        "Current phase | Phase D - Long-Horizon Systems Foundation",
        "Next unlock",
        "D0-A | P0 | Save-backed dungeon depth progression | Done",
        "D0-B | P0 | Formula-driven depth threat and reward bands | Done",
        "D0-C | P0 | Item registry and save migration | Done",
        "D0-D | P0 | Duplicate-item sink and conversion | Next",
        "Tools/Automation/Invoke-IncrementalDiabloChecks.ps1",
        "12_PrototypeDebtRegister.md",
        "Get-PrototypeDebtInventory.ps1"
    )

    foreach ($token in $requiredPlanTokens) {
        [void](Assert-TextContains "Automation plan token" $planText $token)
    }

    if ($planText.Contains("Add the layout controller to the `Gameplay` Canvas")) {
        Add-Result "Automation next-unlock freshness" "WARN" "Plan still asks to add a layout controller that exists in Gameplay.unity."
    } else {
        Add-Result "Automation next-unlock freshness" "PASS" "No stale layout-controller add instruction found."
    }
}

if (Test-Path -LiteralPath $debtRegisterPath) {
    $debtText = Read-TextFile $debtRegisterPath
    $requiredDebtTokens = @(
        "Automation Contract",
        "Current Register",
        "TD-01",
        "Alpha blocker",
        "Get-PrototypeDebtInventory.ps1"
    )

    foreach ($token in $requiredDebtTokens) {
        [void](Assert-TextContains "Prototype debt register token" $debtText $token)
    }
}

$debtScriptPath = Join-ProjectPath "Tools\Automation\Get-PrototypeDebtInventory.ps1"
if (Test-Path -LiteralPath $debtScriptPath) {
    Invoke-CheckedCommand "Prototype debt inventory" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Get-PrototypeDebtInventory.ps1 -SummaryOnly }
}

$depthBalanceExportPath = Join-ProjectPath "Tools\Automation\Export-DungeonDepthBalance.ps1"
if (Test-Path -LiteralPath $depthBalanceExportPath) {
    Invoke-CheckedCommand "Dungeon depth balance curve" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-DungeonDepthBalance.ps1 -CheckOnly }
}

if (Test-Path -LiteralPath $AutomationTomlPath) {
    $automationText = Read-TextFile $AutomationTomlPath
    $bytes = [System.IO.File]::ReadAllBytes($AutomationTomlPath)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        Add-Result "Automation TOML BOM" "FAIL" "automation.toml starts with UTF-8 BOM."
    } else {
        Add-Result "Automation TOML BOM" "PASS" "No UTF-8 BOM detected."
    }

    [void](Assert-TextContains "Automation config active" $automationText 'status = "ACTIVE"')
    [void](Assert-TextContains "Automation prompt uses verification harness" $automationText "Invoke-IncrementalDiabloChecks.ps1")
} else {
    Add-Result "Automation TOML check" "SKIP" "Not found: $AutomationTomlPath"
}

if ($SkipBuild) {
    Add-Result "dotnet build" "SKIP" "Skipped by -SkipBuild."
} else {
    Invoke-CheckedCommand "dotnet build" { dotnet build .\IncrementalDiablo.sln -v:minimal }
}

if ($SkipDiffCheck) {
    Add-Result "git diff --check" "SKIP" "Skipped by -SkipDiffCheck."
} else {
    Invoke-CheckedCommand "git diff --check" { git diff --check }
}

Write-Host ""
$script:Results | Format-Table Status, Name, Details -AutoSize

$failCount = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = @($script:Results | Where-Object { $_.Status -eq "WARN" }).Count

Write-Host ""
Write-Host "Summary: $failCount failed, $warnCount warnings."

if ($failCount -gt 0) {
    exit 1
}

exit 0
