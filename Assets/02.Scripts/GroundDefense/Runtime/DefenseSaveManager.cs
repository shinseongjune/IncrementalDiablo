using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class DefenseSaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 5;

    [SerializeField] private DefenseDirector director;
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private string saveFileName = "incremental_diablo_save.json";
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool simulateOfflineOnLoad = true;
    [SerializeField] private float maxOfflineSeconds = 28800f;
    [SerializeField] private float autoSaveIntervalSeconds = 15f;

    private float autoSaveElapsed;
    private bool lastLoadHasUnresolvedItems;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public bool HasSaveFile => File.Exists(SavePath);
    public string LastLoadReport { get; private set; } = "Load has not run.";

    private void Start()
    {
        ResolveReferences();

        if (loadOnStart)
        {
            TryLoadAndSimulateOfflineProgress();
        }
    }

    private void Update()
    {
        if (autoSaveIntervalSeconds <= 0f)
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
        if (paused)
        {
            TrySave();
        }
    }

    private void OnApplicationQuit()
    {
        TrySave();
    }

    public bool TrySave()
    {
        if (!TryCreateSaveDataSnapshot(out GameSaveData saveData))
        {
            Debug.LogWarning("DefenseSaveManager cannot save without a DefenseDirector.", this);
            return false;
        }

        try
        {
            string saveDirectory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(saveData, true));
            autoSaveElapsed = 0f;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DefenseSaveManager failed to save to {SavePath}: {exception.Message}", this);
            return false;
        }
    }

    public GameSaveData CreateSaveDataSnapshot()
    {
        return TryCreateSaveDataSnapshot(out GameSaveData saveData) ? saveData : null;
    }

    public bool TryValidateCurrentSaveData(out string report)
    {
        ResolveReferences();
        GameSaveData saveData = CreateSaveDataSnapshot();
        return GameSaveDataDiagnostics.TryValidate(saveData, inventory?.DefinitionRegistry, out report);
    }

    public bool TryValidateSavedFile(out string report)
    {
        ResolveReferences();
        if (!TryReadSaveFile(out GameSaveData saveData, out string failureReason))
        {
            report = failureReason;
            return false;
        }

        return GameSaveDataDiagnostics.TryValidate(saveData, inventory?.DefinitionRegistry, out report);
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

        if (director == null)
        {
            LastLoadReport = "Load failed: DefenseDirector is missing.";
            return false;
        }

        if (!HasSaveFile)
        {
            LastLoadReport = $"Save file missing at {SavePath}.";
            return false;
        }

        if (!TryReadSaveFile(out GameSaveData saveData, out string failureReason))
        {
            LastLoadReport = failureReason;
            Debug.LogWarning(failureReason, this);
            return false;
        }

        if (!GameSaveDataDiagnostics.TryValidate(saveData, inventory?.DefinitionRegistry, out string validationReport))
        {
            LastLoadReport = validationReport;
            Debug.LogWarning($"DefenseSaveManager refused an invalid save file: {validationReport}", this);
            return false;
        }

        try
        {
            if (director.Wallet != null && saveData.currencies != null)
            {
                director.Wallet.ImportAmounts(saveData.currencies);
            }

            director.ApplySaveData(saveData.defense);
            LastLoadReport = AppendLoadReport(LastLoadReport, BuildDefenseLoadSummary(saveData.defense));
            if (expedition != null)
            {
                expedition.ApplySaveData(saveData.dungeon);
            }

            if (inventory != null)
            {
                inventory.ApplySaveData(saveData.inventory);
                LastLoadReport = $"{LastLoadReport} {inventory.LastRestoreReport}";
            }

            RestoreEquipmentState(saveData);
            if (applyOfflineProgress)
            {
                ApplyOfflineProgress(saveData);
            }

            autoSaveElapsed = 0f;
            if (lastLoadHasUnresolvedItems)
            {
                Debug.LogWarning(LastLoadReport, this);
            }

            return true;
        }
        catch (Exception exception)
        {
            LastLoadReport = $"Load failed at {SavePath}: {exception.Message}";
            Debug.LogWarning($"DefenseSaveManager failed to load from {SavePath}: {exception.Message}", this);
            return false;
        }
    }

    private bool TryReadSaveFile(out GameSaveData saveData, out string failureReason)
    {
        saveData = null;
        failureReason = string.Empty;

        if (!HasSaveFile)
        {
            failureReason = $"Save file missing at {SavePath}.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                failureReason = $"Save file is empty at {SavePath}.";
                return false;
            }

            saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                failureReason = $"Save file could not be parsed at {SavePath}.";
                return false;
            }

            MigrateSaveData(saveData);
            return true;
        }
        catch (Exception exception)
        {
            failureReason = $"Save file read failed at {SavePath}: {exception.Message}";
            return false;
        }
    }

    private HeroSaveData CreateHeroSaveData()
    {
        long[] equippedItemInstanceIds = inventory == null
            ? new long[0]
            : inventory.GetEquippedItemInstanceIds();

        if (equippedItemInstanceIds.Length == 0 && equipmentSlots != null)
        {
            equippedItemInstanceIds = equipmentSlots.GetEquippedItemInstanceIds();
        }

        return new HeroSaveData
        {
            equippedItemInstanceIds = equippedItemInstanceIds
        };
    }

    private static string BuildDefenseLoadSummary(DefenseSaveData defense)
    {
        if (defense == null)
        {
            return "Defense restore: missing.";
        }

        int wallHealth = Mathf.CeilToInt(Mathf.Max(0f, defense.wallCurrentHealth));
        float progress = Mathf.Max(0f, defense.frontlineProgress);
        return $"Defense restored: FL {Mathf.Max(1, defense.frontlineLevel)}, {defense.state}/{defense.mode}, wall {wallHealth}, progress {progress:0.#}.";
    }

    private static string AppendLoadReport(string report, string detail)
    {
        if (string.IsNullOrWhiteSpace(report))
        {
            return detail;
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            return report;
        }

        return $"{report} {detail}";
    }

    private bool TryCreateSaveDataSnapshot(out GameSaveData saveData)
    {
        ResolveReferences();
        saveData = null;

        if (director == null)
        {
            return false;
        }

        saveData = new GameSaveData
        {
            version = CurrentSaveVersion,
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            playTimeSeconds = director.Runtime.TotalElapsed,
            currencies = director.Wallet == null ? null : director.Wallet.ExportAmounts(),
            defense = director.CreateSaveData(),
            dungeon = expedition == null ? new DungeonSaveData() : expedition.CreateSaveData(),
            hero = CreateHeroSaveData(),
            inventory = inventory == null ? new InventorySaveData() : inventory.CreateSaveData()
        };

        return true;
    }

    private void MigrateSaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        int sourceVersion = saveData.version;
        if (sourceVersion < 2)
        {
            saveData.dungeon ??= new DungeonSaveData();
            int activeDepth = Mathf.Max(1, saveData.dungeon.depth);
            int highestDepth = Mathf.Max(activeDepth, Mathf.Max(1, saveData.dungeon.highestUnlockedDepth));
            int selectedDepth = saveData.dungeon.selectedDepth > 0
                ? saveData.dungeon.selectedDepth
                : activeDepth;

            saveData.dungeon.depth = activeDepth;
            saveData.dungeon.highestUnlockedDepth = highestDepth;
            saveData.dungeon.selectedDepth = Mathf.Clamp(selectedDepth, 1, highestDepth);
        }

        MigrateDungeonContractSaveData(saveData.dungeon);
        MigrateDungeonEncounterSaveData(saveData.dungeon);

        ItemDefinitionRegistry registry = inventory?.DefinitionRegistry;
        ItemDefinitionMigrationReport itemReport = registry?.MigrateInventorySaveData(saveData.inventory);
        lastLoadHasUnresolvedItems = itemReport == null || itemReport.HasUnresolved;
        LastLoadReport = itemReport == null
            ? "Item migration blocked: item definition registry is missing."
            : itemReport.BuildSummary();

        if (sourceVersion < CurrentSaveVersion)
        {
            saveData.version = CurrentSaveVersion;
            LastLoadReport = $"Save schema v{sourceVersion} -> v{CurrentSaveVersion}. {LastLoadReport}";
        }
    }

    private static void MigrateDungeonContractSaveData(DungeonSaveData dungeon)
    {
        if (dungeon == null)
        {
            return;
        }

        dungeon.contractOfferSeed = Mathf.Max(0, dungeon.contractOfferSeed);
        bool firstValid = DungeonContractModel.TryGetContract(dungeon.offeredContractIdA, out DungeonContractProfile first);
        bool secondValid = DungeonContractModel.TryGetContract(dungeon.offeredContractIdB, out DungeonContractProfile second);
        if (!firstValid ||
            !secondValid ||
            string.Equals(first.Id, second.Id, StringComparison.OrdinalIgnoreCase))
        {
            DungeonContractModel.BuildOffer(
                Mathf.Max(1, dungeon.selectedDepth),
                dungeon.contractOfferSeed,
                out dungeon.offeredContractIdA,
                out dungeon.offeredContractIdB);
        }

        if (!DungeonContractModel.TryGetContract(dungeon.selectedContractId, out DungeonContractProfile selected) ||
            (!string.Equals(selected.Id, dungeon.offeredContractIdA, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(selected.Id, dungeon.offeredContractIdB, StringComparison.OrdinalIgnoreCase)))
        {
            dungeon.selectedContractId = dungeon.offeredContractIdA;
        }

        if (dungeon.state == DungeonRunState.Running ||
            dungeon.state == DungeonRunState.Cleared ||
            dungeon.state == DungeonRunState.Failed)
        {
            if (!DungeonContractModel.TryGetContract(dungeon.activeContractId, out _))
            {
                dungeon.activeContractId = dungeon.selectedContractId;
            }
        }
        else if (!DungeonContractModel.TryGetContract(dungeon.activeContractId, out _))
        {
            dungeon.activeContractId = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(dungeon.lastContractSummary))
        {
            dungeon.lastContractSummary = DungeonContractModel.FormatOfferText(
                dungeon.offeredContractIdA,
                dungeon.offeredContractIdB);
        }
    }

    private static void MigrateDungeonEncounterSaveData(DungeonSaveData dungeon)
    {
        if (dungeon == null)
        {
            return;
        }

        dungeon.encounterSeed = Mathf.Max(0, dungeon.encounterSeed);
        if (!DungeonEncounterModel.TryGetEncounter(dungeon.selectedEncounterId, out _))
        {
            DungeonEncounterProfile encounter = DungeonEncounterModel.BuildEncounter(
                Mathf.Max(1, dungeon.selectedDepth),
                dungeon.encounterSeed,
                dungeon.selectedContractId);
            dungeon.selectedEncounterId = encounter.Id;
        }

        if (dungeon.state == DungeonRunState.Running ||
            dungeon.state == DungeonRunState.Cleared ||
            dungeon.state == DungeonRunState.Failed)
        {
            if (!DungeonEncounterModel.TryGetEncounter(dungeon.activeEncounterId, out _))
            {
                dungeon.activeEncounterId = dungeon.selectedEncounterId;
            }
        }
        else if (!DungeonEncounterModel.TryGetEncounter(dungeon.activeEncounterId, out _))
        {
            dungeon.activeEncounterId = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(dungeon.lastEncounterSummary))
        {
            dungeon.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(
                DungeonEncounterModel.GetEncounterOrDefault(dungeon.selectedEncounterId));
        }
    }

    private void RestoreEquipmentState(GameSaveData saveData)
    {
        if (inventory == null || equipmentSlots == null)
        {
            return;
        }

        long[] equippedItemInstanceIds = saveData?.hero == null
            ? null
            : saveData.hero.equippedItemInstanceIds;

        int unresolvedDefinitionCount = inventory.RestoreEquipment(equipmentSlots, equippedItemInstanceIds, out int restoredCount);
        if (unresolvedDefinitionCount > 0)
        {
            Debug.LogWarning(
                $"Restored {restoredCount} equipped item(s); skipped {unresolvedDefinitionCount} unresolved saved item(s).",
                this);
        }
    }

    private void ApplyOfflineProgress(GameSaveData saveData)
    {
        if (!simulateOfflineOnLoad || saveData == null || string.IsNullOrEmpty(saveData.savedAtUtc))
        {
            return;
        }

        if (!DateTime.TryParse(saveData.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime savedAtUtc))
        {
            return;
        }

        float offlineSeconds = (float)Math.Max(0d, (DateTime.UtcNow - savedAtUtc).TotalSeconds);
        float cappedOfflineSeconds = Mathf.Min(Mathf.Max(0f, maxOfflineSeconds), offlineSeconds);
        director.SimulateOffline(cappedOfflineSeconds);
    }

    private void ResolveReferences()
    {
        if (director == null)
        {
            director = FindAnyObjectByType<DefenseDirector>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }

        if (expedition == null)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }

        if (equipmentSlots == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.TryGetComponent(out equipmentSlots);
            }
        }

        if (equipmentSlots == null)
        {
            equipmentSlots = FindAnyObjectByType<EquipmentSlots>();
        }
    }
}
