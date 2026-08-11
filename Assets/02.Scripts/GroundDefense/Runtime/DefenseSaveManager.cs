using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class DefenseSaveManager : MonoBehaviour
{
    public const string NoSaveRecoveryGuidance =
        "No world checkpoint yet. Start the frontline, choose a contract, enter a room, then save from a stable room state.";

    [Header("Account Owners")]
    [SerializeField] private DefenseDirector director;
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private PlayableLoopHud playableHud;

    [Header("World Owners")]
    [SerializeField] private GroundDefenseNavMeshBattlefield defenseBattlefield;
    [SerializeField] private DungeonRoomLoader roomLoader;
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerController player;

    [Header("Checkpoint")]
    [SerializeField] private string saveFileName = "incremental_diablo_world_v2.json";
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool simulateOfflineOnLoad = false;
    [SerializeField] private float autoSaveIntervalSeconds = 15f;
    [SerializeField, Min(1f)] private float restoreTimeoutSeconds = 12f;

    private const string LegacySaveFileName = "incremental_diablo_save.json";
    private const string ProfileV1SaveFileName = "incremental_diablo_profile_v1.json";

    private float autoSaveElapsed;
    private bool primarySaveIsTrusted = true;
    private long latestKnownGeneration;
    private Coroutine restoreRoutine;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public string BackupSavePath => SavePath + ".bak";
    public string LegacySavePath => Path.Combine(Application.persistentDataPath, LegacySaveFileName);
    public string ProfileV1SavePath => Path.Combine(Application.persistentDataPath, ProfileV1SaveFileName);
    public bool HasSaveFile => File.Exists(SavePath);
    public bool IsRestoreInProgress => restoreRoutine != null || GameRuntimeRestoreGate.IsRestoring;
    public string LastLoadReport { get; private set; } = "Load has not run.";

    private void Start()
    {
        ResolveReferences();
        bool queuedRestore = loadOnStart && TryLoadAndSimulateOfflineProgress();
        if (!queuedRestore)
        {
            expedition?.InitializeFreshSnapshot();
        }
    }

    private void Update()
    {
        if (IsRestoreInProgress || autoSaveIntervalSeconds <= 0f)
        {
            return;
        }

        autoSaveElapsed += Time.unscaledDeltaTime;
        if (autoSaveElapsed >= autoSaveIntervalSeconds)
        {
            autoSaveElapsed = 0f;
            TrySave();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && !IsRestoreInProgress)
        {
            TrySave();
        }
    }

    private void OnApplicationQuit()
    {
        if (!IsRestoreInProgress)
        {
            TrySave();
        }
    }

    public bool TrySave()
    {
        if (IsRestoreInProgress)
        {
            Debug.LogWarning("World checkpoint skipped while a restore is still projecting.", this);
            return false;
        }

        if (!TryCreateProfile(out GameProfileSave profile, out string captureFailure))
        {
            Debug.LogWarning($"DefenseSaveManager cannot capture a stable world checkpoint: {captureFailure}", this);
            return false;
        }

        if (!GameProfileSaveValidator.TryValidate(profile, inventory?.DefinitionRegistry, out string validationReport))
        {
            Debug.LogWarning($"DefenseSaveManager refused to write an invalid world checkpoint: {validationReport}", this);
            return false;
        }

        if (!TryWriteProfile(profile, out string failureReason))
        {
            Debug.LogWarning(failureReason, this);
            return false;
        }

        autoSaveElapsed = 0f;
        return true;
    }

    public GameProfileSave CreateSaveDataSnapshot()
    {
        return TryCreateProfile(out GameProfileSave profile, out _) ? profile : null;
    }

    public bool TryValidateCurrentSaveData(out string report)
    {
        ResolveReferences();
        if (!TryCreateProfile(out GameProfileSave profile, out string captureFailure))
        {
            report = captureFailure;
            return false;
        }

        return GameProfileSaveValidator.TryValidate(profile, inventory?.DefinitionRegistry, out report);
    }

    public bool TryValidateSavedFile(out string report)
    {
        ResolveReferences();
        if (!TryReadProfile(out GameProfileSave profile, out string source, out string failureReason))
        {
            report = failureReason;
            return false;
        }

        bool valid = GameProfileSaveValidator.TryValidate(profile, inventory?.DefinitionRegistry, out report);
        if (valid && source == BackupSavePath)
        {
            report = $"Backup recovery candidate. {report}";
        }

        return valid;
    }

    public bool TryLoad()
    {
        return TryLoadInternal(applyOfflineProgress: false);
    }

    public bool TryLoadAndSimulateOfflineProgress()
    {
        return TryLoadInternal(applyOfflineProgress: true);
    }

    private bool TryLoadInternal(bool applyOfflineProgress)
    {
        ResolveReferences();
        if (IsRestoreInProgress)
        {
            LastLoadReport = "Load blocked: a world restore is already in progress.";
            return false;
        }

        if (director == null || expedition == null || defenseBattlefield == null)
        {
            LastLoadReport = "Load failed: one or more account/world owners are missing.";
            return false;
        }

        if (!HasSaveFile && !File.Exists(BackupSavePath))
        {
            LastLoadReport = File.Exists(ProfileV1SavePath) || File.Exists(LegacySavePath)
                ? $"Older saves remain untouched at {ProfileV1SavePath} / {LegacySavePath}. World v2 starts a separate checkpoint and never imports them."
                : NoSaveRecoveryGuidance;
            return false;
        }

        if (!TryReadProfile(out GameProfileSave profile, out string source, out string failureReason))
        {
            LastLoadReport = failureReason;
            Debug.LogWarning(failureReason, this);
            return false;
        }

        if (!TryPreflightRestore(profile, out failureReason))
        {
            LastLoadReport = $"Load preflight failed: {failureReason}";
            Debug.LogWarning(LastLoadReport, this);
            return false;
        }

        restoreRoutine = StartCoroutine(RestoreProfileCoroutine(profile, source, applyOfflineProgress));
        LastLoadReport = $"Validated generation {profile.generation}; restoring world projection.";
        return true;
    }

    private bool TryCreateProfile(out GameProfileSave profile, out string failureReason)
    {
        ResolveReferences();
        profile = null;
        failureReason = string.Empty;
        if (director == null || expedition == null || defenseBattlefield == null || !expedition.TryCreateStableSnapshot(out DungeonExpeditionSnapshot expeditionSnapshot, out failureReason))
        {
            failureReason = string.IsNullOrWhiteSpace(failureReason)
                ? "Account or expedition owner is missing."
                : failureReason;
            return false;
        }

        if (!defenseBattlefield.TryCreateWorldSnapshot(out DefenseWorldSnapshot defenseWorld, out failureReason))
        {
            return false;
        }

        if (!TryCreateDungeonWorldSnapshot(expeditionSnapshot, out DungeonWorldSnapshot dungeonWorld, out failureReason))
        {
            return false;
        }

        profile = new GameProfileSave
        {
            formatVersion = GameProfileSave.CurrentFormatVersion,
            generation = Math.Max(1L, latestKnownGeneration + 1L),
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            account = new AccountSnapshot
            {
                playTimeSeconds = director.Runtime.TotalElapsed,
                currencies = director.Wallet == null ? Array.Empty<ResourceAmount>() : director.Wallet.ExportAmounts(),
                defense = director.CreateSaveData(),
                expedition = expeditionSnapshot,
                hero = CreateHeroSaveData(),
                inventory = inventory == null ? new InventorySaveData() : inventory.CreateSaveData(),
                uiSettings = playableHud == null ? new UiSettingsSaveData() : playableHud.CreateUiSettingsSaveData()
            },
            defenseWorld = defenseWorld,
            dungeonWorld = dungeonWorld
        };
        GameProfileSaveValidator.Seal(profile);
        return true;
    }

    private bool TryCreateDungeonWorldSnapshot(
        DungeonExpeditionSnapshot expeditionSnapshot,
        out DungeonWorldSnapshot snapshot,
        out string failureReason)
    {
        snapshot = null;
        failureReason = string.Empty;
        if (expeditionSnapshot.state == DungeonRunState.Ready)
        {
            return true;
        }

        if (roomLoader == null || combatRoom == null || player == null || roomLoader.IsTransitioning || !roomLoader.HasLoadedActiveRoom ||
            expeditionSnapshot.runPlan == null || !string.Equals(roomLoader.CurrentTemplateId, expeditionSnapshot.runPlan.currentRoomTemplateId, StringComparison.Ordinal))
        {
            failureReason = "Dungeon checkpoint blocked until the active additive room and its owner state are stable.";
            return false;
        }

        if (!combatRoom.TryCreateWorldSnapshot(out DungeonCombatWorldSnapshot combat, out failureReason))
        {
            return false;
        }

        DungeonActorWorldSnapshot hero = player.CreateWorldSnapshot();
        if (hero == null)
        {
            failureReason = "Dungeon checkpoint requires the active hero Health owner.";
            return false;
        }

        DungeonActorWorldSnapshot[] actors = Array.Empty<DungeonActorWorldSnapshot>();
        if (expeditionSnapshot.state == DungeonRunState.Running)
        {
            if (enemySpawner == null || !enemySpawner.TryCreateWorldSnapshots(out actors, out failureReason))
            {
                return false;
            }
        }

        snapshot = new DungeonWorldSnapshot
        {
            isOpen = true,
            templateId = roomLoader.CurrentTemplateId,
            roomSeed = expeditionSnapshot.runPlan.currentRoomSeed,
            roomIndex = expeditionSnapshot.currentRoomIndex,
            combat = combat,
            hero = hero,
            actors = actors
        };
        return WorldSaveSnapshotValidator.TryValidate(snapshot, expeditionSnapshot, out failureReason);
    }

    private bool TryPreflightRestore(GameProfileSave profile, out string failureReason)
    {
        failureReason = string.Empty;
        if (!GameProfileSaveValidator.TryValidate(profile, inventory?.DefinitionRegistry, out failureReason))
        {
            return false;
        }

        DungeonExpeditionSnapshot expeditionSnapshot = profile.account?.expedition;
        if (expeditionSnapshot == null || expeditionSnapshot.state == DungeonRunState.Ready)
        {
            return true;
        }

        if (profile.dungeonWorld == null || roomLoader == null || combatRoom == null || enemySpawner == null || player == null)
        {
            failureReason = "Active dungeon restore requires all dungeon world owners.";
            return false;
        }

        if (!roomLoader.TryValidateCatalog(out failureReason))
        {
            return false;
        }

        if (!string.Equals(profile.dungeonWorld.templateId, expeditionSnapshot.runPlan?.currentRoomTemplateId, StringComparison.Ordinal))
        {
            failureReason = "Dungeon world template does not match its saved run plan.";
            return false;
        }

        return true;
    }

    private IEnumerator RestoreProfileCoroutine(GameProfileSave profile, string source, bool applyOfflineProgress)
    {
        bool restored = false;
        string failure = string.Empty;
        GameRuntimeRestoreGate.BeginRestore();
        try
        {
            if (!TryApplyAccountSnapshot(profile.account, out failure) ||
                !defenseBattlefield.TryRestoreWorldSnapshot(profile.defenseWorld, out failure) ||
                !expedition.TryRestoreSnapshot(profile.account.expedition, out failure))
            {
                yield break;
            }

            if (profile.dungeonWorld != null && profile.dungeonWorld.isOpen)
            {
                float deadline = Time.unscaledTime + restoreTimeoutSeconds;
                while (roomLoader != null && !roomLoader.HasLoadedActiveRoom && Time.unscaledTime < deadline)
                {
                    yield return null;
                }

                if (roomLoader == null || !roomLoader.HasLoadedActiveRoom ||
                    !string.Equals(roomLoader.CurrentTemplateId, profile.dungeonWorld.templateId, StringComparison.Ordinal))
                {
                    failure = "Dungeon restore timed out before its saved additive room became active.";
                    yield break;
                }

                CharacterActor heroActor = player.GetComponent<CharacterActor>();
                if (!enemySpawner.TryRestoreWorldSnapshots(profile.dungeonWorld.actors, heroActor, out List<Health> restoredEnemies, out failure))
                {
                    yield break;
                }

                Dictionary<string, Health> actorsById = new Dictionary<string, Health>(StringComparer.Ordinal)
                {
                    ["hero"] = heroActor == null ? null : heroActor.Health
                };
                for (int i = 0; i < restoredEnemies.Count; i++)
                {
                    Health enemy = restoredEnemies[i];
                    WorldEntityIdentity identity = enemy == null ? null : enemy.GetComponent<WorldEntityIdentity>();
                    if (enemy != null && identity != null && !string.IsNullOrWhiteSpace(identity.EntityId))
                    {
                        actorsById[identity.EntityId] = enemy;
                    }
                }

                if (!player.TryRestoreWorldSnapshot(profile.dungeonWorld.hero, actorsById, out failure) ||
                    !combatRoom.TryRestoreWorldSnapshot(profile.dungeonWorld.combat, heroActor == null ? null : heroActor.Health, restoredEnemies, out failure))
                {
                    yield break;
                }
            }

            if (playableHud != null && profile.account.uiSettings != null)
            {
                playableHud.ApplyUiSettingsSaveData(profile.account.uiSettings);
            }

            if (applyOfflineProgress && simulateOfflineOnLoad)
            {
                Debug.Log("Offline simulation skipped: a world checkpoint resumes the authored live state exactly.", this);
            }

            latestKnownGeneration = Math.Max(latestKnownGeneration, profile.generation);
            autoSaveElapsed = 0f;
            string recovery = source == BackupSavePath ? "Recovered the highest valid backup generation." : "World checkpoint loaded.";
            LastLoadReport = $"{recovery} {GameProfileSaveValidator.BuildShortSummary(profile)}";
            restored = true;
        }
        finally
        {
            GameRuntimeRestoreGate.EndRestore();
            restoreRoutine = null;
            if (!restored)
            {
                LastLoadReport = $"Load projection failed without writing a replacement checkpoint: {failure}";
                Debug.LogWarning(LastLoadReport, this);
            }
        }
    }

    private bool TryApplyAccountSnapshot(AccountSnapshot account, out string failureReason)
    {
        failureReason = string.Empty;
        if (account == null)
        {
            failureReason = "Account snapshot is missing.";
            return false;
        }

        try
        {
            inventory?.ApplySaveData(account.inventory);
            RestoreEquipmentState(account);
            if (director.Wallet != null && account.currencies != null)
            {
                director.Wallet.ImportAmounts(account.currencies);
            }

            director.ApplySaveData(account.defense);
            return true;
        }
        catch (Exception exception)
        {
            failureReason = $"Account projection failed: {exception.Message}";
            return false;
        }
    }

    private HeroSaveData CreateHeroSaveData()
    {
        long[] equippedItemInstanceIds = inventory == null ? Array.Empty<long>() : inventory.GetEquippedItemInstanceIds();
        if (equippedItemInstanceIds.Length == 0 && equipmentSlots != null)
        {
            equippedItemInstanceIds = equipmentSlots.GetEquippedItemInstanceIds();
        }

        return new HeroSaveData { equippedItemInstanceIds = equippedItemInstanceIds };
    }

    private bool TryReadProfile(out GameProfileSave profile, out string source, out string failureReason)
    {
        profile = null;
        source = string.Empty;
        failureReason = string.Empty;
        bool primaryValid = TryReadCandidate(SavePath, out GameProfileSave primary, out string primaryFailure);
        bool backupValid = TryReadCandidate(BackupSavePath, out GameProfileSave backup, out string backupFailure);
        primarySaveIsTrusted = primaryValid;

        if (!primaryValid && !backupValid)
        {
            failureReason = $"World checkpoint recovery failed. Primary: {primaryFailure} Backup: {backupFailure}";
            return false;
        }

        profile = WorldCheckpointRecovery.SelectHighestValid(primaryValid ? primary : null, backupValid ? backup : null);
        bool useBackup = profile == backup;
        source = useBackup ? BackupSavePath : SavePath;
        latestKnownGeneration = Math.Max(latestKnownGeneration, profile.generation);
        return true;
    }

    private bool TryReadCandidate(string path, out GameProfileSave profile, out string failureReason)
    {
        profile = null;
        failureReason = string.Empty;
        if (!File.Exists(path))
        {
            failureReason = $"Missing at {path}.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                failureReason = $"Empty at {path}.";
                return false;
            }

            profile = JsonUtility.FromJson<GameProfileSave>(json);
            if (!GameProfileSaveValidator.TryValidate(profile, inventory?.DefinitionRegistry, out string validationReport))
            {
                profile = null;
                failureReason = $"Invalid at {path}: {validationReport}";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            failureReason = $"Read failed at {path}: {exception.Message}";
            return false;
        }
    }

    private bool TryWriteProfile(GameProfileSave profile, out string failureReason)
    {
        failureReason = string.Empty;
        string temporaryPath = SavePath + ".tmp";
        try
        {
            string saveDirectory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            GameProfileSaveValidator.Seal(profile);
            WriteTextAndFlush(temporaryPath, JsonUtility.ToJson(profile, true));
            if (!TryReadCandidate(temporaryPath, out _, out string tempFailure))
            {
                failureReason = $"World checkpoint temporary write did not round-trip: {tempFailure}";
                return false;
            }

            if (!HasSaveFile)
            {
                File.Move(temporaryPath, SavePath);
            }
            else if (primarySaveIsTrusted)
            {
                File.Replace(temporaryPath, SavePath, BackupSavePath, ignoreMetadataErrors: true);
            }
            else
            {
                PromoteAfterInvalidPrimary(temporaryPath);
            }

            latestKnownGeneration = Math.Max(latestKnownGeneration, profile.generation);
            primarySaveIsTrusted = true;
            return true;
        }
        catch (Exception exception)
        {
            failureReason = $"World checkpoint write failed at {SavePath}: {exception.Message}";
            return false;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void PromoteAfterInvalidPrimary(string temporaryPath)
    {
        string corruptPath = $"{SavePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        File.Move(SavePath, corruptPath);
        try
        {
            File.Move(temporaryPath, SavePath);
        }
        catch
        {
            File.Move(corruptPath, SavePath);
            throw;
        }
    }

    private static void WriteTextAndFlush(string path, string text)
    {
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(text);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private void RestoreEquipmentState(AccountSnapshot account)
    {
        if (inventory == null || equipmentSlots == null)
        {
            return;
        }

        long[] equippedItemInstanceIds = account?.hero?.equippedItemInstanceIds;
        int unresolvedDefinitionCount = inventory.RestoreEquipment(equipmentSlots, equippedItemInstanceIds, out int restoredCount);
        if (unresolvedDefinitionCount > 0)
        {
            Debug.LogWarning($"Restored {restoredCount} equipped item(s); skipped {unresolvedDefinitionCount} unresolved saved item(s).", this);
        }
    }

    private void ResolveReferences()
    {
        director ??= FindAnyObjectByType<DefenseDirector>();
        inventory ??= FindAnyObjectByType<SimpleInventory>();
        expedition ??= FindAnyObjectByType<ExpeditionDirector>();
        playableHud ??= FindAnyObjectByType<PlayableLoopHud>();
        defenseBattlefield ??= FindAnyObjectByType<GroundDefenseNavMeshBattlefield>();
        roomLoader ??= FindAnyObjectByType<DungeonRoomLoader>();
        combatRoom ??= FindAnyObjectByType<CombatRoom>();
        enemySpawner ??= FindAnyObjectByType<EnemySpawner>();
        player ??= FindAnyObjectByType<PlayerController>();

        if (equipmentSlots == null && player != null)
        {
            player.TryGetComponent(out equipmentSlots);
        }

        equipmentSlots ??= FindAnyObjectByType<EquipmentSlots>();
    }
}
