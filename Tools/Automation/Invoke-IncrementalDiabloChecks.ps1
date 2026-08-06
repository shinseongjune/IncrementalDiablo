param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$results = [System.Collections.Generic.List[object]]::new()

function Add-Result {
    param([string]$Name, [string]$Status, [string]$Detail)
    $results.Add([pscustomobject]@{ Name = $Name; Status = $Status; Detail = $Detail })
}

function Get-ProjectPath {
    param([string]$RelativePath)
    Join-Path $ProjectRoot $RelativePath
}

function Require-Path {
    param([string]$Name, [string]$RelativePath)
    if (Test-Path -LiteralPath (Get-ProjectPath $RelativePath)) {
        Add-Result $Name "PASS" $RelativePath
    } else {
        Add-Result $Name "FAIL" "Missing: $RelativePath"
    }
}

function Require-Text {
    param([string]$Name, [string]$Text, [string]$Token)
    if ($Text.Contains($Token)) {
        Add-Result $Name "PASS" $Token
    } else {
        Add-Result $Name "FAIL" "Missing token: $Token"
    }
}

function Invoke-Check {
    param([string]$Name, [scriptblock]$Action)
    try {
        & $Action
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
            Add-Result $Name "FAIL" "Exit code $LASTEXITCODE"
        } else {
            Add-Result $Name "PASS" "Exit code 0"
        }
    } catch {
        Add-Result $Name "FAIL" $_.Exception.Message
    }
}

if (-not (Test-Path -LiteralPath $ProjectRoot)) {
    throw "Project root does not exist: $ProjectRoot"
}

Write-Host "IncrementalDiablo essential checks"
Write-Host "Project root: $ProjectRoot"

$requiredPaths = @(
    @{ Name = "Gameplay scene"; Path = "Assets\01.Scenes\Gameplay.unity" },
    @{ Name = "Dungeon enemy prefab"; Path = "Assets\04.Prefabs\Dungeon\PF_DungeonEnemy_Melee.prefab" },
    @{ Name = "Defense authority"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseDirector.cs" },
    @{ Name = "Defense save manager"; Path = "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs" },
    @{ Name = "Dungeon director"; Path = "Assets\02.Scripts\Dungeon\ExpeditionDirector.cs" },
    @{ Name = "Dungeon room"; Path = "Assets\02.Scripts\Dungeon\CombatRoom.cs" },
    @{ Name = "Dungeon traversal controller"; Path = "Assets\02.Scripts\Dungeon\DungeonTraversalController.cs" },
    @{ Name = "Dungeon traversal trigger"; Path = "Assets\02.Scripts\Dungeon\DungeonTraversalTrigger.cs" },
    @{ Name = "Dungeon spawner"; Path = "Assets\02.Scripts\Dungeon\EnemySpawner.cs" },
    @{ Name = "Dungeon run plan"; Path = "Assets\02.Scripts\Dungeon\DungeonRunPlan.cs" },
    @{ Name = "Dungeon expedition snapshot"; Path = "Assets\02.Scripts\Dungeon\DungeonExpeditionSnapshot.cs" },
    @{ Name = "Dungeon room template"; Path = "Assets\02.Scripts\Dungeon\DungeonRoomTemplate.cs" },
    @{ Name = "Dungeon room loader"; Path = "Assets\02.Scripts\Dungeon\DungeonRoomLoader.cs" },
    @{ Name = "Dungeon room exit"; Path = "Assets\02.Scripts\Dungeon\DungeonRoomExit.cs" },
    @{ Name = "Return portal"; Path = "Assets\02.Scripts\Dungeon\ReturnPortal.cs" },
    @{ Name = "Deeper exit"; Path = "Assets\02.Scripts\Dungeon\DeeperExit.cs" },
    @{ Name = "Combat animation binding"; Path = "Assets\02.Scripts\Character\Core\CombatAnimationDriver.cs" },
    @{ Name = "Playable HUD"; Path = "Assets\02.Scripts\UI\PlayableLoopHud.cs" },
    @{ Name = "Product direction"; Path = "GameDesign\GameDesignDocument.md" },
    @{ Name = "Ground defense contract"; Path = "GameDesign\ProductionDocs\03_GroundDefenseSystemSpec.md" },
    @{ Name = "Dungeon contract"; Path = "GameDesign\ProductionDocs\04_DungeonExpeditionSystemSpec.md" },
    @{ Name = "Economy contract"; Path = "GameDesign\ProductionDocs\05_ItemsCraftingEconomySpec.md" },
    @{ Name = "Unity setup guide"; Path = "GameDesign\ProductionDocs\06_UnitySceneAndPrefabSetupGuide.md" },
    @{ Name = "Save and balance contract"; Path = "GameDesign\ProductionDocs\07_DataSaveAndBalanceSpec.md" },
    @{ Name = "Production roadmap"; Path = "GameDesign\ProductionDocs\08_ProductionRoadmap.md" }
)

foreach ($entry in $requiredPaths) {
    Require-Path $entry.Name $entry.Path
}

$scenePath = Get-ProjectPath "Assets\01.Scenes\Gameplay.unity"
if (Test-Path -LiteralPath $scenePath) {
    $sceneText = Get-Content -LiteralPath $scenePath -Raw
    foreach ($token in @(
        "Assembly-CSharp::DefenseDirector",
        "Assembly-CSharp::GroundDefenseNavMeshBattlefield",
        "Assembly-CSharp::ExpeditionDirector",
        "Assembly-CSharp::CombatRoom",
        "Assembly-CSharp::CombatAnimationDriver",
        "Assembly-CSharp::PlayableLoopHud")) {
        Require-Text "Gameplay scene contract" $sceneText $token
    }

    if ($sceneText -match 'm_Script: \{fileID: 0\}') {
        Add-Result "Gameplay missing scripts" "FAIL" "Found a MonoBehaviour with m_Script fileID 0."
    } else {
        Add-Result "Gameplay missing scripts" "PASS" "No missing MonoBehaviour scripts."
    }
}

$enemyPrefabPath = Get-ProjectPath "Assets\04.Prefabs\Dungeon\PF_DungeonEnemy_Melee.prefab"
if (Test-Path -LiteralPath $enemyPrefabPath) {
    $enemyPrefabText = Get-Content -LiteralPath $enemyPrefabPath -Raw
    foreach ($token in @("NavMeshAgent:", "CapsuleCollider:", "team: 2", "Assembly-CSharp::CombatAnimationDriver")) {
        Require-Text "Dungeon enemy prefab contract" $enemyPrefabText $token
    }
}

$traversalPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonTraversalController.cs"
if (Test-Path -LiteralPath $traversalPath) {
    $traversalText = Get-Content -LiteralPath $traversalPath -Raw
    foreach ($token in @("class DungeonTraversalController", "TryEnterRoom", "TryReturnToEntrance", "TryGetRoomSpawnPoints", "SpawnPointCount", "SetExternalStartControl")) {
        Require-Text "Dungeon traversal contract" $traversalText $token
    }
}

$enemySpawnerPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\EnemySpawner.cs"
if (Test-Path -LiteralPath $enemySpawnerPath) {
    $enemySpawnerText = Get-Content -LiteralPath $enemySpawnerPath -Raw
    foreach ($token in @("DungeonTraversalController traversal", "DungeonRoomLoader roomLoader", "TryResolveActiveSpawnPoints", "EnemySpawnAnchors", "maxEnemiesPerRoom", "ResolveSpawnCount", "room-local spawn marker")) {
        Require-Text "Dungeon room-local spawn contract" $enemySpawnerText $token
    }
}

if (Test-Path -LiteralPath $scenePath) {
    Require-Text "Dungeon opening enemy cap" $sceneText "maxEnemiesPerRoom: 1"
}

$combatRoomPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\CombatRoom.cs"
if (Test-Path -LiteralPath $combatRoomPath) {
    $combatRoomText = Get-Content -LiteralPath $combatRoomPath -Raw
    foreach ($token in @("DungeonRoomLoader roomLoader", "TryValidateActiveRoomTemplate", "CurrentRoomTemplateId", "HasLoadedActiveRoom", "EnemySpawnAnchors")) {
        Require-Text "Dungeon additive-room combat gate" $combatRoomText $token
    }
}

$runPlanPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonRunPlan.cs"
if (Test-Path -LiteralPath $runPlanPath) {
    $runPlanText = Get-Content -LiteralPath $runPlanPath -Raw
    foreach ($token in @("class DungeonRunPlan", "runSeed", "currentRoomTemplateId", "hasAssignedRoomTemplate", "AssignCurrentRoomTemplate", "pendingRewardDepth", "CreateMigrated", "TryValidate")) {
        Require-Text "Dungeon run plan contract" $runPlanText $token
    }
}

$expeditionSnapshotPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonExpeditionSnapshot.cs"
if (Test-Path -LiteralPath $expeditionSnapshotPath) {
    $expeditionSnapshotText = Get-Content -LiteralPath $expeditionSnapshotPath -Raw
    foreach ($token in @("class DungeonExpeditionSnapshot", "DungeonRoomResumePoint", "RestartCurrentRoom", "AwaitingExit", "TryValidate", "MatchesLegacy", "ToSaveData")) {
        Require-Text "Dungeon expedition snapshot contract" $expeditionSnapshotText $token
    }
}

$roomTemplatePath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonRoomTemplate.cs"
if (Test-Path -LiteralPath $roomTemplatePath) {
    $roomTemplateText = Get-Content -LiteralPath $roomTemplatePath -Raw
    foreach ($token in @("class DungeonRoomTemplate", "entrancePoint", "returnPortalPoint", "deeperExitPoint", "ReturnPortal returnPortal", "DeeperExit deeperExit", "enemySpawnAnchors", "TryValidate")) {
        Require-Text "Dungeon room template contract" $roomTemplateText $token
    }
}

$playerControllerPath = Get-ProjectPath "Assets\02.Scripts\Character\Controllers\PlayerController.cs"
if (Test-Path -LiteralPath $playerControllerPath) {
    $playerControllerText = Get-Content -LiteralPath $playerControllerPath -Raw
    foreach ($token in @("TryResolveExitClick", "DungeonRoomExit", "QueryTriggerInteraction.Collide", "DisplayName")) {
        Require-Text "Dungeon portal click contract" $playerControllerText $token
    }
}

$roomExitPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonRoomExit.cs"
if (Test-Path -LiteralPath $roomExitPath) {
    $roomExitText = Get-Content -LiteralPath $roomExitPath -Raw
    foreach ($token in @("abstract string DisplayName", "public bool TryUse()", "OnTriggerEnter")) {
        Require-Text "Dungeon portal interaction contract" $roomExitText $token
    }
}

$roomLoaderPath = Get-ProjectPath "Assets\02.Scripts\Dungeon\DungeonRoomLoader.cs"
if (Test-Path -LiteralPath $roomLoaderPath) {
    $roomLoaderText = Get-Content -LiteralPath $roomLoaderPath -Raw
    foreach ($token in @("class DungeonRoomLoader", "DungeonRoomCatalogEntry", "LoadSceneAsync", "UnloadSceneAsync", "TryValidateCatalog", "TryAssignCurrentRoomTemplate", "TryReturnToHub", "TryEnterDeeperRoom", "HasLoadedActiveRoom", "IsSnapshotReady", "returnToHubPoint")) {
        Require-Text "Dungeon room loader contract" $roomLoaderText $token
    }
}

$saveDataPath = Get-ProjectPath "Assets\02.Scripts\Shared\GameSaveData.cs"
if (Test-Path -LiteralPath $saveDataPath) {
    $saveDataText = Get-Content -LiteralPath $saveDataPath -Raw
    foreach ($token in @("version = 9", "DungeonExpeditionSnapshot expeditionSnapshot", "DungeonRunPlan runPlan")) {
        Require-Text "Dungeon run-plan save contract" $saveDataText $token
    }
}

$saveManagerPath = Get-ProjectPath "Assets\02.Scripts\GroundDefense\Runtime\DefenseSaveManager.cs"
if (Test-Path -LiteralPath $saveManagerPath) {
    $saveManagerText = Get-Content -LiteralPath $saveManagerPath -Raw
    foreach ($token in @("CurrentSaveVersion = 9", "TryMigrateSaveData", "MigrateDungeonSnapshotSource", "FinalizeDungeonSnapshot", "MigrateDungeonExitChoiceSaveData", "MigrateDungeonRunPlanSaveData", "GetPendingRewardDepth")) {
        Require-Text "Dungeon run-plan migration contract" $saveManagerText $token
    }
}

Push-Location $ProjectRoot
try {
    Invoke-Check "git diff --check" { git diff --check }
    if ($SkipBuild) {
        Add-Result "dotnet build" "SKIP" "Skipped by -SkipBuild."
    } else {
        Invoke-Check "dotnet build" { dotnet build .\IncrementalDiablo.sln -v:minimal }
    }
} finally {
    Pop-Location
}

$results | Format-Table -AutoSize
$failCount = @($results | Where-Object Status -eq "FAIL").Count
Write-Host "Summary: $failCount failed."
if ($failCount -gt 0) {
    exit 1
}
