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
        ResolveReferences();

        if (director == null)
        {
            Debug.LogWarning("DefenseSaveManager cannot save without a DefenseDirector.", this);
            return false;
        }

        GameSaveData saveData = new GameSaveData
        {
            version = 1,
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            playTimeSeconds = director.Runtime.TotalElapsed,
            currencies = director.Wallet == null ? null : director.Wallet.ExportAmounts(),
            defense = director.CreateSaveData(),
            dungeon = expedition == null ? new DungeonSaveData() : expedition.CreateSaveData(),
            inventory = inventory == null ? new InventorySaveData() : inventory.CreateSaveData()
        };

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
    }
}
