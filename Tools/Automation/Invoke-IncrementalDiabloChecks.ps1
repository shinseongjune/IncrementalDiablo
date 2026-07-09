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

function Get-SceneGameObjectChunk {
    param(
        [string]$SceneText,
        [string]$GameObjectName
    )

    $escapedGameObjectName = [regex]::Escape($GameObjectName)
    $pattern = "(?ms)^--- !u!1 &\d+\r?\nGameObject:.*?m_Name:\s+${escapedGameObjectName}\r?\n.*?(?=^--- !u!1 &|\z)"
    return [regex]::Match($SceneText, $pattern)
}

function Assert-SceneNamedRenderTargetBridge {
    param(
        [string]$Name,
        [string]$SceneText,
        [string]$RawImageName
    )

    $chunk = Get-SceneGameObjectChunk $SceneText $RawImageName
    if (-not $chunk.Success) {
        Add-Result $Name "FAIL" "Missing GameObject: $RawImageName"
        return $false
    }

    if (-not $chunk.Value.Contains("m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.RawImage")) {
        Add-Result $Name "FAIL" "$RawImageName is missing RawImage."
        return $false
    }

    if (-not $chunk.Value.Contains("m_EditorClassIdentifier: Assembly-CSharp::PanelCameraRenderTarget")) {
        Add-Result $Name "FAIL" "$RawImageName is missing PanelCameraRenderTarget."
        return $false
    }

    if ($chunk.Value -notmatch "(?m)^\s*sourceCamera:\s+\{fileID:\s+(?!0\})\d+") {
        Add-Result $Name "FAIL" "$RawImageName render target has no source camera."
        return $false
    }

    if ($chunk.Value -notmatch "(?m)^\s*targetImage:\s+\{fileID:\s+(?!0\})\d+") {
        Add-Result $Name "FAIL" "$RawImageName render target has no target RawImage."
        return $false
    }

    Add-Result $Name "PASS" "$RawImageName has RawImage + PanelCameraRenderTarget with source camera and target image."
    return $true
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

function Assert-SceneBehaviourEnabled {
    param(
        [string]$Name,
        [string]$SceneText,
        [string]$ClassName,
        [bool]$ExpectedEnabled
    )

    $blocks = @(Get-SceneBehaviourBlocks $SceneText $ClassName)
    if ($blocks.Count -eq 0) {
        Add-Result $Name "FAIL" "Missing MonoBehaviour: $ClassName"
        return $false
    }

    $expectedToken = if ($ExpectedEnabled) { "m_Enabled: 1" } else { "m_Enabled: 0" }
    foreach ($block in $blocks) {
        if ($block.Value.Contains($expectedToken)) {
            Add-Result $Name "PASS" "$ClassName has $expectedToken."
            return $true
        }
    }

    Add-Result $Name "FAIL" "$ClassName does not have $expectedToken."
    return $false
}

function Assert-SceneBehaviourAbsent {
    param(
        [string]$Name,
        [string]$SceneText,
        [string]$ClassName
    )

    $blocks = @(Get-SceneBehaviourBlocks $SceneText $ClassName)
    if ($blocks.Count -eq 0) {
        Add-Result $Name "PASS" "$ClassName is absent."
        return $true
    }

    Add-Result $Name "FAIL" "$ClassName is still present in Gameplay.unity."
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
    @{ Name = "Dungeon contract model"; Path = "Assets\02.Scripts\Dungeon\DungeonContractModel.cs" },
    @{ Name = "Dungeon encounter model"; Path = "Assets\02.Scripts\Dungeon\DungeonEncounterModel.cs" },
    @{ Name = "Expedition director script"; Path = "Assets\02.Scripts\Dungeon\ExpeditionDirector.cs" },
    @{ Name = "Save data script"; Path = "Assets\02.Scripts\Shared\GameSaveData.cs" },
    @{ Name = "Save diagnostics script"; Path = "Assets\02.Scripts\Shared\GameSaveDataDiagnostics.cs" },
    @{ Name = "Save manager script"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs" },
    @{ Name = "Ground defense balance model"; Path = "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseBalanceModel.cs" },
    @{ Name = "Defense director script"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseDirector.cs" },
    @{ Name = "Defense upgrade model"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseUpgradeModel.cs" },
    @{ Name = "Ground defense NavMesh battlefield"; Path = "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseNavMeshBattlefield.cs" },
    @{ Name = "Ground defense NavMesh unit"; Path = "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseNavMeshUnit.cs" },
    @{ Name = "Ground defense billboard utility"; Path = "Assets\02.Scripts\GroundDefense\UI\GroundDefenseBillboardUtility.cs" },
    @{ Name = "Ground defense readability sheet"; Path = "Assets\06.Art\Sprites\GroundDefense\GroundDefense_ReadabilitySheet.png" },
    @{ Name = "Item definition registry script"; Path = "Assets\02.Scripts\Items\ItemDefinitionRegistry.cs" },
    @{ Name = "Item definition registry asset"; Path = "Assets\05.ScriptableObjects\Items\ItemDefinitionRegistry.asset" },
    @{ Name = "Item economy model"; Path = "Assets\02.Scripts\Items\ItemEconomyModel.cs" },
    @{ Name = "Item salvage service"; Path = "Assets\02.Scripts\Items\ItemSalvageService.cs" },
    @{ Name = "Loot dropper script"; Path = "Assets\02.Scripts\Items\LootDropper.cs" },
    @{ Name = "Simple inventory script"; Path = "Assets\02.Scripts\Items\SimpleInventory.cs" },
    @{ Name = "Equipment slots script"; Path = "Assets\02.Scripts\Character\Core\EquipmentSlots.cs" },
    @{ Name = "Playable HUD script"; Path = "Assets\02.Scripts\UI\PlayableLoopHud.cs" },
    @{ Name = "Screen layout controller script"; Path = "Assets\02.Scripts\UI\PlayableScreenLayoutController.cs" },
    @{ Name = "Automation plan"; Path = "GameDesign\ProductionDocs\10_PlayableLoopMvpAutomationPlan.md" },
    @{ Name = "Release readiness plan"; Path = "GameDesign\ProductionDocs\13_ReleaseReadinessAndProductionGates.md" },
    @{ Name = "Prototype debt register"; Path = "GameDesign\ProductionDocs\12_PrototypeDebtRegister.md" },
    @{ Name = "Scene setup guide"; Path = "GameDesign\ProductionDocs\06_UnitySceneAndPrefabSetupGuide.md" },
    @{ Name = "Script usage guide"; Path = "GameDesign\ProductionDocs\09_BaseScriptUsageGuide.md" },
    @{ Name = "Script folder map"; Path = "GameDesign\ScriptFolderStructure.md" },
    @{ Name = "Prototype debt inventory script"; Path = "Tools\Automation\Get-PrototypeDebtInventory.ps1" },
    @{ Name = "Dungeon depth balance export"; Path = "Tools\Automation\Export-DungeonDepthBalance.ps1" },
    @{ Name = "Dungeon depth balance CSV"; Path = "GameDesign\Balance\DungeonDepthBalance.csv" },
    @{ Name = "Dungeon contract export"; Path = "Tools\Automation\Export-DungeonContracts.ps1" },
    @{ Name = "Dungeon contract balance CSV"; Path = "GameDesign\Balance\DungeonContractBalance.csv" },
    @{ Name = "Dungeon encounter export"; Path = "Tools\Automation\Export-DungeonEncounters.ps1" },
    @{ Name = "Dungeon encounter balance CSV"; Path = "GameDesign\Balance\DungeonEncounterBalance.csv" },
    @{ Name = "Rare affix export"; Path = "Tools\Automation\Export-RareAffixes.ps1" },
    @{ Name = "Rare affix pool CSV"; Path = "GameDesign\Balance\RareAffixPool.csv" },
    @{ Name = "Ground defense balance export"; Path = "Tools\Automation\Export-GroundDefenseBalance.ps1" },
    @{ Name = "Ground defense balance CSV"; Path = "GameDesign\Balance\GroundDefenseBalance.csv" }
)

foreach ($entry in $requiredPaths) {
    [void](Assert-PathExists $entry.Name $entry.Path)
}

$scenePath = Join-ProjectPath "Assets\01.Scenes\Gameplay.unity"
$enemyPrefabPath = Join-ProjectPath "Assets\04.Prefabs\Dungeon\PF_DungeonEnemy_Melee.prefab"
$enemySpawnerPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\EnemySpawner.cs"
$depthBalanceModelPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\DungeonDepthBalanceModel.cs"
$dungeonContractModelPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\DungeonContractModel.cs"
$dungeonEncounterModelPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\DungeonEncounterModel.cs"
$expeditionDirectorPath = Join-ProjectPath "Assets\02.Scripts\Dungeon\ExpeditionDirector.cs"
$saveDataPath = Join-ProjectPath "Assets\02.Scripts\Shared\GameSaveData.cs"
$saveDiagnosticsPath = Join-ProjectPath "Assets\02.Scripts\Shared\GameSaveDataDiagnostics.cs"
$saveManagerPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs"
$groundBalanceModelPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseBalanceModel.cs"
$defenseDirectorPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\DefenseDirector.cs"
$defenseUpgradePath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\DefenseUpgradeModel.cs"
$groundNavMeshBattlefieldPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseNavMeshBattlefield.cs"
$groundNavMeshUnitPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\GroundDefenseNavMeshUnit.cs"
$groundBillboardUtilityPath = Join-ProjectPath "Assets\02.Scripts\GroundDefense\UI\GroundDefenseBillboardUtility.cs"
$itemRegistryPath = Join-ProjectPath "Assets\02.Scripts\Items\ItemDefinitionRegistry.cs"
$itemRegistryAssetPath = Join-ProjectPath "Assets\05.ScriptableObjects\Items\ItemDefinitionRegistry.asset"
$itemEconomyPath = Join-ProjectPath "Assets\02.Scripts\Items\ItemEconomyModel.cs"
$itemSalvagePath = Join-ProjectPath "Assets\02.Scripts\Items\ItemSalvageService.cs"
$lootDropperPath = Join-ProjectPath "Assets\02.Scripts\Items\LootDropper.cs"
$simpleInventoryPath = Join-ProjectPath "Assets\02.Scripts\Items\SimpleInventory.cs"
$equipmentSlotsPath = Join-ProjectPath "Assets\02.Scripts\Character\Core\EquipmentSlots.cs"
$playableHudPath = Join-ProjectPath "Assets\02.Scripts\UI\PlayableLoopHud.cs"
$planPath = Join-ProjectPath "GameDesign\ProductionDocs\10_PlayableLoopMvpAutomationPlan.md"
$releaseReadinessPath = Join-ProjectPath "GameDesign\ProductionDocs\13_ReleaseReadinessAndProductionGates.md"
$sceneSetupGuidePath = Join-ProjectPath "GameDesign\ProductionDocs\06_UnitySceneAndPrefabSetupGuide.md"
$debtRegisterPath = Join-ProjectPath "GameDesign\ProductionDocs\12_PrototypeDebtRegister.md"

if (Test-Path -LiteralPath $scenePath) {
    $sceneText = Read-TextFile $scenePath
    $requiredSceneTokens = @(
        @{ Name = "Scene has PlayableScreenLayoutController"; Token = "m_EditorClassIdentifier: Assembly-CSharp::PlayableScreenLayoutController" },
        @{ Name = "Scene has defense side panel"; Token = "m_Name: Panel_DefenseSide" },
        @{ Name = "Scene has dungeon viewport panel"; Token = "m_Name: Panel_DungeonViewport" },
        @{ Name = "HUD syncs screen focus"; Token = "syncScreenFocusWithDungeon: 1" },
        @{ Name = "Scene has active NavMesh defense battlefield"; Token = "m_EditorClassIdentifier: Assembly-CSharp::GroundDefenseNavMeshBattlefield" },
        @{ Name = "Scene wires ground readability sheet"; Token = "a041289685f941e8a40086ddca94abc3" },
        @{ Name = "Scene starts with two defenders"; Token = "defenderCount: 2" },
        @{ Name = "Scene starts with three enemies"; Token = "enemyCount: 3" },
        @{ Name = "Scene has dungeon combat room"; Token = "m_EditorClassIdentifier: Assembly-CSharp::CombatRoom" },
        @{ Name = "Scene has enemy spawner"; Token = "m_EditorClassIdentifier: Assembly-CSharp::EnemySpawner" },
        @{ Name = "Scene has NavMesh surface"; Token = "m_EditorClassIdentifier: Unity.AI.Navigation::Unity.AI.Navigation.NavMeshSurface" },
        @{ Name = "Scene NavMesh surface has baked data"; Token = "m_NavMeshData: {fileID: 23800000" },
        @{ Name = "Scene enemy spawns snap to NavMesh"; Token = "snapSpawnPointsToNavMesh: 1" },
        @{ Name = "Scene normal combat disables prototype simulation"; Token = "simulateWhenNoEnemies: 0" },
        @{ Name = "Scene has loot dropper"; Token = "m_EditorClassIdentifier: Assembly-CSharp::LootDropper" },
        @{ Name = "Scene disables runtime loot fallback"; Token = "createPrototypeRewardWhenTableEmpty: 0" },
        @{ Name = "Scene enables inferior duplicate conversion"; Token = "autoConvertInferiorDuplicates: 1" },
        @{ Name = "Scene has dungeon RawImage viewport"; Token = "m_Name: RawImage_DungeonViewport" },
        @{ Name = "Scene has dungeon panel camera"; Token = "m_Name: Camera_DungeonPanel" },
        @{ Name = "Scene has dungeon render target"; Token = "m_EditorClassIdentifier: Assembly-CSharp::PanelCameraRenderTarget" },
        @{ Name = "Scene has dungeon input router"; Token = "m_EditorClassIdentifier: Assembly-CSharp::DungeonViewportInputRouter" },
        @{ Name = "Scene has defense RawImage viewport"; Token = "m_Name: RawImage_DefenseViewport" },
        @{ Name = "Scene has defense panel camera"; Token = "m_Name: Camera_DefensePanel" },
        @{ Name = "Scene has previous dungeon depth button"; Token = "m_Name: Button_DungeonDepthPrevious" },
        @{ Name = "Scene has next dungeon depth button"; Token = "m_Name: Button_DungeonDepthNext" },
        @{ Name = "Scene has dungeon contract A button"; Token = "m_Name: Button_DungeonContractA" },
        @{ Name = "Scene has dungeon contract B button"; Token = "m_Name: Button_DungeonContractB" },
        @{ Name = "Scene has dungeon contract refresh button"; Token = "m_Name: Button_DungeonContractRefresh" },
        @{ Name = "Scene initializes selected dungeon depth"; Token = "selectedDepth: 1" },
        @{ Name = "Scene initializes highest unlocked dungeon depth"; Token = "highestUnlockedDepth: 1" }
    )

    foreach ($entry in $requiredSceneTokens) {
        [void](Assert-TextContains $entry.Name $sceneText $entry.Token)
    }

    [void](Assert-SceneBehaviourReference "Dungeon render target source camera" $sceneText "PanelCameraRenderTarget" "sourceCamera")
    [void](Assert-SceneBehaviourReference "Dungeon render target RawImage" $sceneText "PanelCameraRenderTarget" "targetImage")
    [void](Assert-SceneNamedRenderTargetBridge "Dungeon viewport render bridge" $sceneText "RawImage_DungeonViewport")
    [void](Assert-SceneNamedRenderTargetBridge "Defense viewport render bridge" $sceneText "RawImage_DefenseViewport")
    [void](Assert-SceneBehaviourReference "Dungeon input router RawImage" $sceneText "DungeonViewportInputRouter" "viewportImage")
    [void](Assert-SceneBehaviourReference "Dungeon input router camera" $sceneText "DungeonViewportInputRouter" "viewportCamera")
    [void](Assert-SceneBehaviourReference "Dungeon input router player" $sceneText "DungeonViewportInputRouter" "player")
    [void](Assert-SceneBehaviourReference "Dungeon input router screen layout" $sceneText "DungeonViewportInputRouter" "screenLayout")
    [void](Assert-SceneBehaviourReference "Ground NavMesh battlefield defense" $sceneText "GroundDefenseNavMeshBattlefield" "defense")
    [void](Assert-SceneBehaviourReference "Ground NavMesh battlefield camera" $sceneText "GroundDefenseNavMeshBattlefield" "defenseCamera")
    [void](Assert-SceneBehaviourReference "Ground NavMesh battlefield enemy spawn" $sceneText "GroundDefenseNavMeshBattlefield" "enemySpawnAnchor")
    [void](Assert-SceneBehaviourReference "Ground NavMesh battlefield wall" $sceneText "GroundDefenseNavMeshBattlefield" "wallAnchor")
    [void](Assert-SceneBehaviourReference "Ground NavMesh battlefield readability sheet" $sceneText "GroundDefenseNavMeshBattlefield" "readabilitySheet")
    [void](Assert-SceneBehaviourEnabled "Ground NavMesh battlefield is enabled" $sceneText "GroundDefenseNavMeshBattlefield" $true)
    [void](Assert-SceneBehaviourAbsent "Legacy ground lane presenter is removed" $sceneText "GroundDefenseLanePresenter")
    [void](Assert-SceneBehaviourAbsent "Legacy ground actor runtime is removed" $sceneText "GroundDefenseActorRuntime")
    [void](Assert-SceneBehaviourAbsent "Legacy ground enemy pool is removed" $sceneText "GroundDefenseEnemyPool")
    [void](Assert-SceneBehaviourAbsent "Legacy ground battlefield view is removed" $sceneText "GroundDefenseBattlefieldView")
    [void](Assert-SceneBehaviourAbsent "Legacy ground combat presenter is removed" $sceneText "GroundDefenseCombatPresenter")
    [void](Assert-SceneBehaviourReference "Playable HUD previous depth button" $sceneText "PlayableLoopHud" "previousDungeonDepthButton")
    [void](Assert-SceneBehaviourReference "Playable HUD next depth button" $sceneText "PlayableLoopHud" "nextDungeonDepthButton")
    [void](Assert-SceneBehaviourReference "Playable HUD contract A button" $sceneText "PlayableLoopHud" "selectContractAButton")
    [void](Assert-SceneBehaviourReference "Playable HUD contract B button" $sceneText "PlayableLoopHud" "selectContractBButton")
    [void](Assert-SceneBehaviourReference "Playable HUD contract refresh button" $sceneText "PlayableLoopHud" "refreshDungeonContractButton")
    [void](Assert-SceneBehaviourReference "Simple inventory item registry" $sceneText "SimpleInventory" "definitionRegistry")
    [void](Assert-SceneBehaviourReference "Loot dropper salvage service" $sceneText "LootDropper" "salvageService")

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
    [void](Assert-TextContains "Expedition exposes contract selection" $expeditionDirectorText "public bool SelectFirstContract()")
    [void](Assert-TextContains "Expedition applies contract balance" $expeditionDirectorText "GetEffectiveDepthBalance")
    [void](Assert-TextContains "Expedition grants contract reward depth" $expeditionDirectorText "lootDropper.TryGrantClearReward(ActiveRewardDepth")
    [void](Assert-TextContains "Expedition exposes selected encounter" $expeditionDirectorText "public DungeonEncounterProfile SelectedEncounter")
    [void](Assert-TextContains "Expedition stores active encounter" $expeditionDirectorText "runtime.activeEncounterId = encounter.Id;")
    [void](Assert-TextContains "Expedition starts selected depth" $expeditionDirectorText "runtime.depth = SelectedDepth;")
    [void](Assert-TextContains "Expedition unlocks after clear" $expeditionDirectorText "int unlockedDepth = TryUnlockNextDepth();")
    [void](Assert-TextContains "Dungeon save stores selected depth" $saveDataText "public int selectedDepth = 1;")
    [void](Assert-TextContains "Dungeon save stores highest unlocked depth" $saveDataText "public int highestUnlockedDepth = 1;")
    [void](Assert-TextContains "Dungeon save stores selected contract" $saveDataText "public string selectedContractId")
    [void](Assert-TextContains "Dungeon save stores active contract" $saveDataText "public string activeContractId")
    [void](Assert-TextContains "Dungeon save stores selected encounter" $saveDataText "public string selectedEncounterId")
    [void](Assert-TextContains "Dungeon save stores active encounter" $saveDataText "public string activeEncounterId")
    [void](Assert-TextContains "Save data stores UI settings" $saveDataText "public UiSettingsSaveData uiSettings")
    [void](Assert-TextContains "Save manager writes schema v6" $saveManagerText "private const int CurrentSaveVersion = 6;")
    [void](Assert-TextContains "Save manager migrates legacy dungeon depth" $saveManagerText "MigrateSaveData(saveData);")
    [void](Assert-TextContains "Save manager migrates dungeon contracts" $saveManagerText "MigrateDungeonContractSaveData")
    [void](Assert-TextContains "Save manager migrates dungeon encounters" $saveManagerText "MigrateDungeonEncounterSaveData")
    [void](Assert-TextContains "Save manager snapshots HUD settings" $saveManagerText "playableHud.CreateUiSettingsSaveData")
    [void](Assert-TextContains "Save manager restores HUD settings" $saveManagerText "playableHud.ApplyUiSettingsSaveData")
    [void](Assert-TextContains "Save manager owns no-save recovery guidance" $saveManagerText "NoSaveRecoveryGuidance")
    [void](Assert-TextContains "Save manager hides save path before first save" $saveManagerText "LastLoadReport = NoSaveRecoveryGuidance")
    [void](Assert-TextContains "Save manager runs item id migration" $saveManagerText "MigrateInventorySaveData")
    [void](Assert-TextContains "Save manager resets autosave after manual save" $saveManagerText "autoSaveElapsed = 0f;")
    [void](Assert-TextContains "Save manager reports defense restore" $saveManagerText "BuildDefenseLoadSummary")
    [void](Assert-TextContains "Save diagnostics use item registry" $saveDiagnosticsText "ItemDefinitionRegistry definitionRegistry")
    [void](Assert-TextContains "Save diagnostics validate selected depth" $saveDiagnosticsText "dungeon selectedDepth must be within the unlocked depth range")
    [void](Assert-TextContains "Save diagnostics validate selected contract" $saveDiagnosticsText "dungeon selectedContractId must be one of the offered contract ids")
    [void](Assert-TextContains "Save diagnostics validate active encounter" $saveDiagnosticsText "dungeon activeEncounterId is required for active or reward-pending encounter resolution")
    [void](Assert-TextContains "Save diagnostics validate UI settings" $saveDiagnosticsText "ValidateUiSettings")
    [void](Assert-TextContains "Playable HUD exposes previous depth action" $playableHudText "public void SelectPreviousDungeonDepth()")
    [void](Assert-TextContains "Playable HUD exposes next depth action" $playableHudText "public void SelectNextDungeonDepth()")
    [void](Assert-TextContains "Playable HUD exposes contract actions" $playableHudText "public void SelectFirstDungeonContract()")
    [void](Assert-TextContains "Playable HUD exposes encounter text" $playableHudText "BuildDungeonEncounterText")
    [void](Assert-TextContains "Playable HUD exposes first-session guide" $playableHudText "showFirstSessionGuide")
    [void](Assert-TextContains "Playable HUD snapshots UI settings" $playableHudText "CreateUiSettingsSaveData")
    [void](Assert-TextContains "Playable HUD applies UI settings" $playableHudText "ApplyUiSettingsSaveData")
    [void](Assert-TextContains "Playable HUD guides first recovery save" $playableHudText "keep frontline, dungeon, inventory, equipment, and HUD settings")
    [void](Assert-TextContains "Playable HUD reuses no-save recovery guidance" $playableHudText "DefenseSaveManager.NoSaveRecoveryGuidance")
    [void](Assert-TextContains "Playable HUD allows no-save load guidance" $playableHudText "SetInteractable(loadButton, saveManager != null);")
    [void](Assert-TextContains "Playable HUD shows contract goal comparison" $playableHudText "BuildSelectedContractGoalText")
    [void](Assert-TextContains "Playable HUD routes E2-B action hint" $playableHudText "BuildSelectedContractActionHint")
    [void](Assert-TextContains "Playable HUD shows latest item comparison" $playableHudText "BuildLatestItemComparisonText")
    [void](Assert-TextContains "Playable HUD routes latest item action hint" $playableHudText "BuildLatestItemActionHint")
    [void](Assert-TextContains "Playable HUD prioritizes latest item decision before next contract" $playableHudText "TryBuildLatestItemDecisionHint")
    [void](Assert-TextContains "Playable HUD shows defense upgrade comparison" $playableHudText "BuildDefenseUpgradeComparisonText")
    [void](Assert-TextContains "Playable HUD routes defense upgrade action hint" $playableHudText "TryBuildDefenseUpgradeDecisionHint")
    [void](Assert-TextContains "Playable HUD shows defense upgrade shortfall guidance" $playableHudText "FormatMissingRewards")
    [void](Assert-TextContains "Playable HUD shows post-upgrade return guidance" $playableHudText "BuildPostDefenseUpgradeActionText")
    [void](Assert-TextContains "Playable HUD reads equipped item by slot" $playableHudText "GetEquippedItemForSlot")
    [void](Assert-TextContains "Save diagnostics summarize guide state" $saveDiagnosticsText "guide off")
}

if (Test-Path -LiteralPath $equipmentSlotsPath) {
    $equipmentSlotsText = Read-TextFile $equipmentSlotsPath
    [void](Assert-TextContains "Equipment slots exposes same-slot item" $equipmentSlotsText "public ItemInstance GetEquippedItem")
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

if ((Test-Path -LiteralPath $itemEconomyPath) -and
    (Test-Path -LiteralPath $itemSalvagePath) -and
    (Test-Path -LiteralPath $lootDropperPath)) {
    $itemEconomyText = Read-TextFile $itemEconomyPath
    $itemSalvageText = Read-TextFile $itemSalvagePath
    $lootDropperText = Read-TextFile $lootDropperPath

    [void](Assert-TextContains "Duplicate conversion requires same definition" $itemEconomyText "StringComparison.Ordinal")
    [void](Assert-TextContains "Duplicate conversion preserves stronger depth" $itemEconomyText "ownedItem.Level < candidate.Level")
    [void](Assert-TextContains "Duplicate conversion preserves stronger power" $itemEconomyText "ownedItem.RolledPower < candidate.RolledPower")
    [void](Assert-TextContains "Rare affix pool exposes authored entries" $itemEconomyText "AuthoredRareAffixes")
    [void](Assert-TextContains "Rare affix reroll uses authored pool" $itemEconomyText "TryRollAuthoredRareAffix")
    [void](Assert-TextContains "Rare affix reroll avoids current ids" $itemEconomyText "CollectRareAffixCandidates(item.Slot, item.AffixRolls, true)")
    [void](Assert-TextContains "Rare affix text uses display names" $itemEconomyText "FormatAffixRoll")
    [void](Assert-TextContains "Salvage service converts unstored reward" $itemSalvageText "public bool TryConvertReward")
    [void](Assert-TextContains "Loot dropper evaluates duplicate conversion" $lootDropperText "TryAutoConvertInferiorDuplicate")
    [void](Assert-TextContains "Loot dropper reports converted reward" $lootDropperText "RewardConverted?.Invoke")
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
    [void](Assert-TextContains "Enemy spawner reports active encounter" $enemySpawnerText "combatRoom.ActiveEncounter")
}

if (Test-Path -LiteralPath $depthBalanceModelPath) {
    $depthBalanceModelText = Read-TextFile $depthBalanceModelPath
    [void](Assert-TextContains "Depth balance uses bounded bands" $depthBalanceModelText "public const int DepthsPerBand = 10;")
    [void](Assert-TextContains "Depth balance exposes enemy health scaling" $depthBalanceModelText "EnemyHealthMultiplier")
    [void](Assert-TextContains "Depth balance exposes reward power scaling" $depthBalanceModelText "RewardPowerMultiplier")
    [void](Assert-TextContains "Depth balance exposes material yield scaling" $depthBalanceModelText "MaterialYieldMultiplier")
}

if (Test-Path -LiteralPath $dungeonContractModelPath) {
    $dungeonContractModelText = Read-TextFile $dungeonContractModelPath
    [void](Assert-TextContains "Dungeon contracts define default id" $dungeonContractModelText "DefaultContractId")
    [void](Assert-TextContains "Dungeon contracts expose starter set" $dungeonContractModelText "StarterContracts")
    [void](Assert-TextContains "Dungeon contracts generate two offers" $dungeonContractModelText "BuildOffer")
    [void](Assert-TextContains "Dungeon contracts state denominator" $dungeonContractModelText "per-clear guaranteed item reward")
    [void](Assert-TextContains "Dungeon contracts include risk reward" $dungeonContractModelText "RewardDepthOffset")
    [void](Assert-TextContains "Dungeon contracts format goal comparison" $dungeonContractModelText "FormatGoalComparisonText")
}

if (Test-Path -LiteralPath $dungeonEncounterModelPath) {
    $dungeonEncounterModelText = Read-TextFile $dungeonEncounterModelPath
    [void](Assert-TextContains "Dungeon encounters define default id" $dungeonEncounterModelText "DefaultEncounterId")
    [void](Assert-TextContains "Dungeon encounters expose starter set" $dungeonEncounterModelText "StarterEncounters")
    [void](Assert-TextContains "Dungeon encounters generate reusable rules" $dungeonEncounterModelText "BuildEncounter")
    [void](Assert-TextContains "Dungeon encounters state denominator" $dungeonEncounterModelText "per dungeon run spawned encounter")
    [void](Assert-TextContains "Dungeon encounters include elite rule" $dungeonEncounterModelText "IsElite")
    [void](Assert-TextContains "Dungeon encounters include boss rule" $dungeonEncounterModelText "IsBoss")
}

if ((Test-Path -LiteralPath $groundBalanceModelPath) -and
    (Test-Path -LiteralPath $defenseDirectorPath) -and
    (Test-Path -LiteralPath $defenseUpgradePath) -and
    (Test-Path -LiteralPath $playableHudPath)) {
    $groundBalanceModelText = Read-TextFile $groundBalanceModelPath
    $defenseDirectorText = Read-TextFile $defenseDirectorPath
    $defenseUpgradeText = Read-TextFile $defenseUpgradePath
    $playableHudText = Read-TextFile $playableHudPath

    [void](Assert-TextContains "Ground balance uses reusable bands" $groundBalanceModelText "public const int LevelsPerBand = 10;")
    [void](Assert-TextContains "Ground balance exposes pressure scaling" $groundBalanceModelText "IncomingPressureMultiplier")
    [void](Assert-TextContains "Ground balance exposes defense scaling" $groundBalanceModelText "DefenseOutputMultiplier")
    [void](Assert-TextContains "Ground balance exposes reward scaling" $groundBalanceModelText "RewardMultiplier")
    [void](Assert-TextContains "Ground balance exposes milestone rewards" $groundBalanceModelText "GetMilestoneRewards")
    [void](Assert-TextContains "Defense director consumes ground profile" $defenseDirectorText "CurrentProgressionProfile")
    [void](Assert-TextContains "Defense director grants band milestone" $defenseDirectorText "GrantMilestoneRewards")
    [void](Assert-TextContains "Breached defense keeps recovery income" $defenseDirectorText "runtime.IsRunning || runtime.State == DefenseState.Breached")
    [void](Assert-TextContains "Defense director accepts visible wall hits" $defenseDirectorText "ApplyBattlefieldWallDamage")
    [void](Assert-TextContains "Defense director exposes save-apply event" $defenseDirectorText "public event Action SaveDataApplied")
    [void](Assert-TextContains "Defense director notifies save-apply event" $defenseDirectorText "SaveDataApplied?.Invoke()")
    [void](Assert-TextContains "Defense upgrades expose wall comparison gain" $defenseUpgradeText "WallHealthGainPerUpgrade")
    [void](Assert-TextContains "Defense upgrades expose tower comparison gain" $defenseUpgradeText "TowerDamageGainPerUpgrade")
    [void](Assert-TextContains "Defense upgrades expose defender comparison gain" $defenseUpgradeText "DefenderDamageGainPerUpgrade")
    [void](Assert-TextContains "Playable HUD exposes ground band" $playableHudText "Next Band Lv.")
}

if ((Test-Path -LiteralPath $groundNavMeshBattlefieldPath) -and
    (Test-Path -LiteralPath $groundNavMeshUnitPath)) {
    $groundNavMeshBattlefieldText = Read-TextFile $groundNavMeshBattlefieldPath
    $groundNavMeshUnitText = Read-TextFile $groundNavMeshUnitPath

    [void](Assert-TextContains "Ground battlefield builds runtime NavMesh" $groundNavMeshBattlefieldText "navMeshSurface.BuildNavMesh()")
    [void](Assert-TextContains "Ground battlefield spawns real character stats" $groundNavMeshBattlefieldText "AddComponent<CharacterStats>()")
    [void](Assert-TextContains "Ground battlefield spawns real health" $groundNavMeshBattlefieldText "AddComponent<Health>()")
    [void](Assert-TextContains "Ground battlefield spawns real NavMesh agents" $groundNavMeshBattlefieldText "AddComponent<NavMeshAgent>()")
    [void](Assert-TextContains "Ground battlefield spawns real combat drivers" $groundNavMeshBattlefieldText "AddComponent<CombatDriver>()")
    [void](Assert-TextContains "Ground battlefield creates defender force" $groundNavMeshBattlefieldText "GroundDefenseNavMeshUnitSide.Defender")
    [void](Assert-TextContains "Ground battlefield creates enemy force" $groundNavMeshBattlefieldText "GroundDefenseNavMeshUnitSide.Enemy")
    [void](Assert-TextContains "Ground battlefield routes wall damage" $groundNavMeshBattlefieldText "ApplyBattlefieldWallDamage")
    [void](Assert-TextContains "Ground battlefield adds ownership markers" $groundNavMeshBattlefieldText "BuildOwnershipMarker")
    [void](Assert-TextContains "Ground battlefield creates defender shield badge" $groundNavMeshBattlefieldText "DefenderShieldBadge")
    [void](Assert-TextContains "Ground battlefield creates enemy threat badge" $groundNavMeshBattlefieldText "EnemyThreatBadge")
    [void](Assert-TextContains "Ground battlefield evaluates formula force scale" $groundNavMeshBattlefieldText "EvaluateVisualForceProfile")
    [void](Assert-TextContains "Ground battlefield consumes frontline profile" $groundNavMeshBattlefieldText "defense.CurrentProgressionProfile")
    [void](Assert-TextContains "Ground battlefield subscribes to save-apply event" $groundNavMeshBattlefieldText "subscribedDefense.SaveDataApplied += HandleDefenseSaveDataApplied")
    [void](Assert-TextContains "Ground battlefield rebuilds on save load" $groundNavMeshBattlefieldText "HandleDefenseSaveDataApplied")
    [void](Assert-TextContains "Ground battlefield caps scaled enemy density" $groundNavMeshBattlefieldText "maxFormulaEnemies")
    [void](Assert-TextContains "Ground battlefield selects formula role mix" $groundNavMeshBattlefieldText "GetEnemyRoleForSlot")
    [void](Assert-TextContains "Ground battlefield scales reinforcement cadence" $groundNavMeshBattlefieldText "formulaCadenceStrength")
    [void](Assert-TextContains "Ground units require active defense state" $groundNavMeshUnitText "battlefield.UnitsCanAct")
    [void](Assert-TextContains "Defenders acquire enemy targets" $groundNavMeshUnitText "FindNearestEnemy")
    [void](Assert-TextContains "Enemies acquire defender targets" $groundNavMeshUnitText "FindNearestDefender")
    [void](Assert-TextContains "Units move through character motor" $groundNavMeshUnitText "actor.Motor.TryMoveTo")
    [void](Assert-TextContains "Units attack through combat driver" $groundNavMeshUnitText "actor.Combat.TryBasicAttack")
    [void](Assert-TextContains "Enemies attack the wall in place" $groundNavMeshUnitText "TryPlayBasicAttackInPlace")
    [void](Assert-TextContains "Units draw attack ownership line" $groundNavMeshUnitText "AttackOwnershipLine")
    [void](Assert-TextContains "Units show attacker-owned hits" $groundNavMeshUnitText "ShowAttackOwnership")
    [void](Assert-TextContains "Units show target reaction" $groundNavMeshUnitText "hitRecoil")
}

if (Test-Path -LiteralPath $groundBillboardUtilityPath) {
    $groundBillboardUtilityText = Read-TextFile $groundBillboardUtilityPath
    [void](Assert-TextContains "Ground billboard utility faces defense camera" $groundBillboardUtilityText "GroundDefenseBillboardFacing")
    [void](Assert-TextContains "Ground billboard utility creates runtime sprites" $groundBillboardUtilityText "Sprite.Create(")
    [void](Assert-TextContains "Ground billboard utility cuts dark sprite matte" $groundBillboardUtilityText "TryCreateCutoutTexture")
    [void](Assert-TextContains "Ground billboard utility softens black sheet background" $groundBillboardUtilityText "ApplyDarkMatteCutout")
    [void](Assert-TextContains "Ground billboard utility uses SpriteRenderer" $groundBillboardUtilityText "SpriteRenderer renderer")
    [void](Assert-TextContains "Ground billboard utility supports role facing" $groundBillboardUtilityText "renderer.flipX = flipX")
}

if (Test-Path -LiteralPath $planPath) {
    $planText = Read-TextFile $planPath
    $requiredPlanTokens = @(
        "Phase Promotion Rule",
        "Visible Game Production Rule",
        "Prototype Debt Sweep Rule",
        "No-Stagnation Rules",
        "Progress Tracker",
        "Current phase | Phase E - Early Access Readiness Slice",
        "Next unlock",
        "D0-A | P0 | Save-backed dungeon depth progression | Done",
        "D0-B | P0 | Formula-driven depth threat and reward bands | Done",
        "D0-C | P0 | Item registry and save migration | Done",
        "D0-D | P0 | Duplicate-item sink and conversion | Done",
        "D1-A | P1 | Formula-driven ground scaling | Done",
        "E0-A | P0 | RTS-readable automatic defense battlefield | Done / E0-A3 accepted",
        "E0-B | P0 | Defense camera and reference composition pass | Done / User accepted camera composition",
        "Current product queue",
        "E1-A | P0 | Formula-driven dungeon contract choice | Done / User accepted Play Mode validation",
        "E1-B | P0 | Authored Rare affix pool | Done / User accepted Play Mode validation",
        "E1-C | P1 | Reusable dungeon encounter variety | Done / User accepted Play Mode validation",
        "E2-A | P1 | Onboarding, settings, recovery | Done / User accepted recovery guidance",
        "E2-B | P1 | Goal comparison clarity | Done / User accepted Play Mode validation",
        "E3-A | P1 | Settings menu scope and first-session QA checklist | Needs product decision",
        "E3-A first-session QA checklist",
        "RTS-readable automatic defense",
        "Actual NavMesh battlefield",
        "Tools/Automation/Invoke-IncrementalDiabloChecks.ps1",
        "12_PrototypeDebtRegister.md",
        "Get-PrototypeDebtInventory.ps1",
        "Closed work is not a default task source"
    )

    foreach ($token in $requiredPlanTokens) {
        [void](Assert-TextContains "Automation plan token" $planText $token)
    }

    if ($planText.Contains("Add the layout controller to the `Gameplay` Canvas")) {
        Add-Result "Automation next-unlock freshness" "WARN" "Plan still asks to add a layout controller that exists in Gameplay.unity."
    } else {
        Add-Result "Automation next-unlock freshness" "PASS" "No stale layout-controller add instruction found."
    }

    if ($planText.Contains("The next proof is a camera/reference-image composition pass")) {
        Add-Result "Automation closed-gate freshness" "FAIL" "Plan still routes completed E0-B composition as the next proof."
    } else {
        Add-Result "Automation closed-gate freshness" "PASS" "No completed E0-B composition task is routed as current work."
    }
}

if (Test-Path -LiteralPath $releaseReadinessPath) {
    $releaseReadinessText = Read-TextFile $releaseReadinessPath
    $requiredReleaseTokens = @(
        "Current Product Contract",
        "What Counts As Production Movement",
        "Two-Hour Repeatable Slice",
        "Ten-Hour Alpha Loop",
        "E1-A | Done / P0",
        "E1-B | Done / P0",
        "E1-C | Done / P1",
        "E2-A | Done / P1",
        "E2-B | Done / P1",
        "E3-A | Needs decision / P1",
        "R3 first-session QA checklist",
        "post-upgrade return guidance",
        "settings persistence",
        "900+ hour target is a long-horizon design constraint",
        "A green harness means safe structure, not a completed product gate"
    )

    foreach ($token in $requiredReleaseTokens) {
        [void](Assert-TextContains "Release readiness token" $releaseReadinessText $token)
    }
}

if (Test-Path -LiteralPath $sceneSetupGuidePath) {
    $sceneSetupGuideText = Read-TextFile $sceneSetupGuidePath
    $requiredSceneSetupTokens = @(
        "E3-A R3 first-session QA checklist",
        "no-save Load guidance",
        "contract -> run -> reward",
        "HUD settings restored",
        "Until the E3-A settings menu scope is approved"
    )

    foreach ($token in $requiredSceneSetupTokens) {
        [void](Assert-TextContains "Scene setup E3-A QA token" $sceneSetupGuideText $token)
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

$dungeonContractExportPath = Join-ProjectPath "Tools\Automation\Export-DungeonContracts.ps1"
if (Test-Path -LiteralPath $dungeonContractExportPath) {
    Invoke-CheckedCommand "Dungeon contract export" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-DungeonContracts.ps1 -CheckOnly }
}

$dungeonEncounterExportPath = Join-ProjectPath "Tools\Automation\Export-DungeonEncounters.ps1"
if (Test-Path -LiteralPath $dungeonEncounterExportPath) {
    Invoke-CheckedCommand "Dungeon encounter export" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-DungeonEncounters.ps1 -CheckOnly }
}

$rareAffixExportPath = Join-ProjectPath "Tools\Automation\Export-RareAffixes.ps1"
if (Test-Path -LiteralPath $rareAffixExportPath) {
    Invoke-CheckedCommand "Rare affix export" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-RareAffixes.ps1 -CheckOnly }
}

$groundBalanceExportPath = Join-ProjectPath "Tools\Automation\Export-GroundDefenseBalance.ps1"
if (Test-Path -LiteralPath $groundBalanceExportPath) {
    Invoke-CheckedCommand "Ground defense balance curve" { powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Automation\Export-GroundDefenseBalance.ps1 -CheckOnly }
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
    [void](Assert-TextContains "Automation prompt reads release gates" $automationText "13_ReleaseReadinessAndProductionGates.md" "WARN")
    [void](Assert-TextContains "Automation prompt forbids closed-gate repeats" $automationText "Closed gates are regression-only" "WARN")
    [void](Assert-TextContains "Automation prompt distinguishes harness from product progress" $automationText "A green harness is structural verification" "WARN")
} else {
    Add-Result "Automation TOML check" "SKIP" "Not found: $AutomationTomlPath"
}

function Invoke-DotnetBuildCheck {
    Push-Location $ProjectRoot
    try {
        $buildOutput = @(& dotnet build .\IncrementalDiablo.sln -v:minimal 2>&1)
        foreach ($line in $buildOutput) {
            Write-Host $line
        }

        $exitCode = $LASTEXITCODE
        if ($null -eq $exitCode) {
            $exitCode = 0
        }

        if ($exitCode -ne 0) {
            Add-Result "dotnet build" "FAIL" "Exit code $exitCode"
            return
        }

        Add-Result "dotnet build" "PASS" "Exit code 0"
        $warningLines = @($buildOutput | Where-Object { $_.ToString() -match '(?i):\s*warning\s+[A-Z]+\d+' })
        if ($warningLines.Count -eq 0) {
            Add-Result "dotnet build warnings" "PASS" "No compiler warnings detected."
            return
        }

        $warningText = ($warningLines | ForEach-Object { $_.ToString() }) -join "`n"
        $isKnownUnityAiBindingWarning = $warningText.Contains("MSB3277") -and $warningText.Contains("Unity.AI.") -and $warningText.Contains("System.Net.Http")
        if ($isKnownUnityAiBindingWarning) {
            Add-Result "dotnet build warnings" "WARN" "Known Unity AI package System.Net.Http binding warnings ($($warningLines.Count) lines); build succeeds, but package compatibility remains triaged risk."
        } else {
            Add-Result "dotnet build warnings" "WARN" "Compiler warnings detected ($($warningLines.Count) lines). Review before claiming release readiness."
        }
    } catch {
        Add-Result "dotnet build" "FAIL" $_.Exception.Message
    } finally {
        Pop-Location
    }
}

if ($SkipBuild) {
    Add-Result "dotnet build" "SKIP" "Skipped by -SkipBuild."
} else {
    Invoke-DotnetBuildCheck
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
