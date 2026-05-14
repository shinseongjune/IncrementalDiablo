using System;
using System.Globalization;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class DefenseSaveManager : MonoBehaviour
{
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

    private void Start()
    {
        ResolveReferences();

        if (loadOnStart)
        {
            TryLoad();
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

    public bool TryLoad()
    {
        ResolveReferences();

        if (director == null || !File.Exists(SavePath))
        {
            return false;
        }

        try
        {
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
            if (saveData == null)
            {
                return false;
            }

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
            ApplyOfflineProgress(saveData);
            autoSaveElapsed = 0f;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DefenseSaveManager failed to load from {SavePath}: {exception.Message}", this);
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
            version = 1,
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
