using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class DefenseSaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 2;

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

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public bool HasSaveFile => File.Exists(SavePath);

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
        GameSaveData saveData = CreateSaveDataSnapshot();
        return GameSaveDataDiagnostics.TryValidate(saveData, out report);
    }

    public bool TryValidateSavedFile(out string report)
    {
        if (!TryReadSaveFile(out GameSaveData saveData, out string failureReason))
        {
            report = failureReason;
            return false;
        }

        return GameSaveDataDiagnostics.TryValidate(saveData, out report);
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

        if (director == null || !HasSaveFile)
        {
            return false;
        }

        if (!TryReadSaveFile(out GameSaveData saveData, out string failureReason))
        {
            Debug.LogWarning(failureReason, this);
            return false;
        }

        if (!GameSaveDataDiagnostics.TryValidate(saveData, out string validationReport))
        {
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
            if (expedition != null)
            {
                expedition.ApplySaveData(saveData.dungeon);
            }

            if (inventory != null)
            {
                inventory.ApplySaveData(saveData.inventory);
            }

            RestoreEquipmentState(saveData);
            if (applyOfflineProgress)
            {
                ApplyOfflineProgress(saveData);
            }

            autoSaveElapsed = 0f;
            return true;
        }
        catch (Exception exception)
        {
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

    private static void MigrateSaveData(GameSaveData saveData)
    {
        if (saveData == null || saveData.version >= CurrentSaveVersion)
        {
            return;
        }

        saveData.dungeon ??= new DungeonSaveData();
        int activeDepth = Mathf.Max(1, saveData.dungeon.depth);
        int highestDepth = Mathf.Max(activeDepth, Mathf.Max(1, saveData.dungeon.highestUnlockedDepth));
        int selectedDepth = saveData.dungeon.selectedDepth > 0
            ? saveData.dungeon.selectedDepth
            : activeDepth;

        saveData.dungeon.depth = activeDepth;
        saveData.dungeon.highestUnlockedDepth = highestDepth;
        saveData.dungeon.selectedDepth = Mathf.Clamp(selectedDepth, 1, highestDepth);
        saveData.version = CurrentSaveVersion;
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

        int snapshotDefinitionCount = inventory.RestoreEquipment(equipmentSlots, equippedItemInstanceIds, out int restoredCount);
        if (snapshotDefinitionCount > 0)
        {
            Debug.Log(
                $"Restored {restoredCount} equipped item(s); {snapshotDefinitionCount} used saved prototype power because their ItemDefinition assets were not resolved.",
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
