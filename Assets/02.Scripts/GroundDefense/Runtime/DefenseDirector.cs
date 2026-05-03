using System;
using UnityEngine;

public class DefenseDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private DefenseUpgradeModel upgrades;

    [Header("Startup")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool startOnPlay = true;
    [SerializeField] private int startingLevel = 1;
    [SerializeField] private FrontlineMode startingMode = FrontlineMode.Push;

    [Header("Frontline Pressure")]
    [SerializeField] private float baseIncomingPressurePerSecond = 10f;
    [SerializeField] private float pressureGrowthPerLevel = 1.12f;
    [SerializeField] private float pushPressureMultiplier = 1.3f;
    [SerializeField] private float basePressureCapacity = 100f;
    [SerializeField] private float pressureCapacityGrowthPerLevel = 1.08f;
    [SerializeField] private float wallDamagePerPressureSecond = 0.018f;

    [Header("Frontline Progress")]
    [SerializeField] private float baseProgressRequired = 100f;
    [SerializeField] private float progressRequiredGrowthPerLevel = 1.1f;
    [SerializeField] private float basePushProgressPerSecond = 2.5f;
    [SerializeField] private float surplusDefenseProgressMultiplier = 0.25f;

    [Header("Rewards")]
    [SerializeField] private float baseGoldPerMinute = 30f;
    [SerializeField] private float baseScrapPerMinute = 4f;
    [SerializeField] private float rewardGrowthPerLevel = 1.08f;
    [SerializeField] private float pushRewardMultiplier = 1.15f;
    [SerializeField] private float breachedRewardMultiplier = 0f;

    [SerializeField] private DefenseRuntimeState runtime = new DefenseRuntimeState();

    private float goldRemainder;
    private float scrapRemainder;

    public event Action Changed;

    public DefenseRuntimeState Runtime => runtime;
    public CurrencyWallet Wallet => wallet;
    public DefenseUpgradeModel Upgrades => upgrades;

    private void Awake()
    {
        ResolveReferences();

        if (runtime == null)
        {
            runtime = new DefenseRuntimeState();
        }

        if (initializeOnAwake)
        {
            ResetDefense();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (upgrades != null)
        {
            upgrades.Changed += HandleUpgradesChanged;
        }
    }

    private void OnDisable()
    {
        if (upgrades != null)
        {
            upgrades.Changed -= HandleUpgradesChanged;
        }
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartDefense();
        }
        else
        {
            NotifyChanged();
        }
    }

    private void Update()
    {
        if (runtime.IsRunning)
        {
            TickFrontline(Time.deltaTime);
        }
    }

    private void OnValidate()
    {
        startingLevel = Mathf.Max(1, startingLevel);
        baseIncomingPressurePerSecond = Mathf.Max(0f, baseIncomingPressurePerSecond);
        pressureGrowthPerLevel = Mathf.Max(1f, pressureGrowthPerLevel);
        pushPressureMultiplier = Mathf.Max(1f, pushPressureMultiplier);
        basePressureCapacity = Mathf.Max(1f, basePressureCapacity);
        pressureCapacityGrowthPerLevel = Mathf.Max(1f, pressureCapacityGrowthPerLevel);
        wallDamagePerPressureSecond = Mathf.Max(0f, wallDamagePerPressureSecond);
        baseProgressRequired = Mathf.Max(1f, baseProgressRequired);
        progressRequiredGrowthPerLevel = Mathf.Max(1f, progressRequiredGrowthPerLevel);
        basePushProgressPerSecond = Mathf.Max(0f, basePushProgressPerSecond);
        surplusDefenseProgressMultiplier = Mathf.Max(0f, surplusDefenseProgressMultiplier);
        baseGoldPerMinute = Mathf.Max(0f, baseGoldPerMinute);
        baseScrapPerMinute = Mathf.Max(0f, baseScrapPerMinute);
        rewardGrowthPerLevel = Mathf.Max(1f, rewardGrowthPerLevel);
        pushRewardMultiplier = Mathf.Max(1f, pushRewardMultiplier);
        breachedRewardMultiplier = Mathf.Clamp01(breachedRewardMultiplier);
    }

    public void ResetDefense()
    {
        runtime.Initialize(GetMaxWallHealth(), startingLevel, startingMode, GetPressureCapacity(startingLevel), GetProgressRequired(startingLevel));
        goldRemainder = 0f;
        scrapRemainder = 0f;
        NotifyChanged();
    }

    public void StartDefense()
    {
        if (runtime.IsRunning)
        {
            return;
        }

        if (runtime.WallHealth <= 0f)
        {
            runtime.MoveToRepairState();
            NotifyChanged();
            return;
        }

        runtime.StartFrontline();
        NotifyChanged();
    }

    public void SetMode(FrontlineMode mode)
    {
        runtime.SetMode(mode);
        NotifyChanged();
    }

    public void ToggleMode()
    {
        SetMode(runtime.Mode == FrontlineMode.Push ? FrontlineMode.Hold : FrontlineMode.Push);
    }

    public bool TryRepairWall()
    {
        if (upgrades == null || wallet == null)
        {
            Debug.LogWarning("DefenseDirector needs both DefenseUpgradeModel and CurrencyWallet to repair the wall.", this);
            return false;
        }

        float missingHealth = Mathf.Max(0f, GetMaxWallHealth() - runtime.WallHealth);
        if (!wallet.TrySpend(upgrades.GetRepairCost(missingHealth)))
        {
            return false;
        }

        runtime.SetWallMaxHealth(GetMaxWallHealth(), false);
        runtime.RefillWall();
        runtime.MoveToRepairState();
        NotifyChanged();
        return true;
    }

    public bool TryUpgradeWall()
    {
        if (upgrades == null || !upgrades.TryUpgradeWall(wallet))
        {
            return false;
        }

        runtime.SetWallMaxHealth(GetMaxWallHealth(), true);
        NotifyChanged();
        return true;
    }

    public bool TryUpgradeTower()
    {
        if (upgrades == null || !upgrades.TryUpgradeTower(wallet))
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    public bool TryUpgradeDefender()
    {
        if (upgrades == null || !upgrades.TryUpgradeDefender(wallet))
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    public DefenseSaveData CreateSaveData()
    {
        DefenseSaveData saveData = new DefenseSaveData();
        runtime.WriteSaveData(saveData);

        if (upgrades != null)
        {
            saveData.wallLevel = upgrades.WallLevel;
            saveData.towerLevel = upgrades.TowerLevel;
            saveData.defenderLevel = upgrades.DefenderLevel;
        }

        return saveData;
    }

    public void ApplySaveData(DefenseSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        ResolveReferences();

        if (runtime == null)
        {
            runtime = new DefenseRuntimeState();
        }

        if (upgrades != null)
        {
            upgrades.SetLevels(saveData.wallLevel, saveData.towerLevel, saveData.defenderLevel);
        }

        int loadedLevel = Mathf.Max(1, saveData.frontlineLevel);
        runtime.ApplySaveData(saveData, GetMaxWallHealth(), GetPressureCapacity(loadedLevel), GetProgressRequired(loadedLevel));
        goldRemainder = 0f;
        scrapRemainder = 0f;
        NotifyChanged();
    }

    public float SimulateOffline(float offlineSeconds)
    {
        if (offlineSeconds <= 0f || !runtime.IsRunning)
        {
            return 0f;
        }

        float remainingSeconds = offlineSeconds;
        const float maxStepSeconds = 5f;

        while (remainingSeconds > 0f && runtime.IsRunning)
        {
            float stepSeconds = Mathf.Min(maxStepSeconds, remainingSeconds);
            TickFrontline(stepSeconds, false);
            remainingSeconds -= stepSeconds;
        }

        NotifyChanged();
        return offlineSeconds - remainingSeconds;
    }

    private void TickFrontline(float deltaTime, bool notify = true)
    {
        runtime.SetWallMaxHealth(GetMaxWallHealth(), false);
        runtime.SetPressureCapacity(GetPressureCapacity(runtime.FrontlineLevel));
        runtime.SetProgressRequired(GetProgressRequired(runtime.FrontlineLevel));

        float incomingPressure = GetIncomingPressurePerSecond();
        float defensePower = GetDefensePowerPerSecond();
        float surplusDefense = Mathf.Max(0f, defensePower - incomingPressure);
        float progressPerSecond = GetProgressPerSecond(surplusDefense);

        runtime.TickFrontline(deltaTime, incomingPressure, defensePower, wallDamagePerPressureSecond, progressPerSecond);
        GrantContinuousRewards(deltaTime);

        if (runtime.WallHealth <= 0f || runtime.EnemyPressure >= runtime.EnemyPressureCapacity)
        {
            runtime.MarkBreached();
            if (notify)
            {
                NotifyChanged();
            }

            return;
        }

        while (runtime.FrontlineProgress >= runtime.FrontlineProgressRequired)
        {
            int nextLevel = runtime.FrontlineLevel + 1;
            runtime.AdvanceLevel(GetProgressRequired(nextLevel), GetPressureCapacity(nextLevel));
        }

        if (notify)
        {
            NotifyChanged();
        }
    }

    private void GrantContinuousRewards(float deltaTime)
    {
        if (wallet == null)
        {
            return;
        }

        float rewardMultiplier = runtime.State == DefenseState.Breached ? breachedRewardMultiplier : GetModeRewardMultiplier();
        goldRemainder += GetGoldPerSecond() * rewardMultiplier * Mathf.Max(0f, deltaTime);
        scrapRemainder += GetScrapPerSecond() * rewardMultiplier * Mathf.Max(0f, deltaTime);

        int goldToAdd = Mathf.FloorToInt(goldRemainder);
        int scrapToAdd = Mathf.FloorToInt(scrapRemainder);

        if (goldToAdd > 0)
        {
            wallet.Add(ResourceId.Gold, goldToAdd);
            goldRemainder -= goldToAdd;
        }

        if (scrapToAdd > 0)
        {
            wallet.Add(ResourceId.Scrap, scrapToAdd);
            scrapRemainder -= scrapToAdd;
        }
    }

    private float GetMaxWallHealth()
    {
        return upgrades == null ? 100f : upgrades.MaxWallHealth;
    }

    private float GetDefensePowerPerSecond()
    {
        return upgrades == null ? 10f : upgrades.TotalDefensePower + upgrades.WallPressureReductionPerSecond;
    }

    private float GetIncomingPressurePerSecond()
    {
        float pressure = baseIncomingPressurePerSecond * Mathf.Pow(pressureGrowthPerLevel, Mathf.Max(0, runtime.FrontlineLevel - 1));
        return runtime.Mode == FrontlineMode.Push ? pressure * pushPressureMultiplier : pressure;
    }

    private float GetPressureCapacity(int level)
    {
        return basePressureCapacity * Mathf.Pow(pressureCapacityGrowthPerLevel, Mathf.Max(0, level - 1));
    }

    private float GetProgressRequired(int level)
    {
        return baseProgressRequired * Mathf.Pow(progressRequiredGrowthPerLevel, Mathf.Max(0, level - 1));
    }

    private float GetProgressPerSecond(float surplusDefense)
    {
        if (runtime.Mode != FrontlineMode.Push)
        {
            return 0f;
        }

        return basePushProgressPerSecond + surplusDefense * surplusDefenseProgressMultiplier;
    }

    private float GetGoldPerSecond()
    {
        return baseGoldPerMinute * Mathf.Pow(rewardGrowthPerLevel, Mathf.Max(0, runtime.FrontlineLevel - 1)) / 60f;
    }

    private float GetScrapPerSecond()
    {
        return baseScrapPerMinute * Mathf.Pow(rewardGrowthPerLevel, Mathf.Max(0, runtime.FrontlineLevel - 1)) / 60f;
    }

    private float GetModeRewardMultiplier()
    {
        return runtime.Mode == FrontlineMode.Push ? pushRewardMultiplier : 1f;
    }

    private void ResolveReferences()
    {
        if (wallet == null)
        {
            wallet = GetComponent<CurrencyWallet>();
        }

        if (upgrades == null)
        {
            upgrades = GetComponent<DefenseUpgradeModel>();
        }

        if (wallet == null)
        {
            wallet = FindAnyObjectByType<CurrencyWallet>();
        }

        if (upgrades == null)
        {
            upgrades = FindAnyObjectByType<DefenseUpgradeModel>();
        }
    }

    private void HandleUpgradesChanged()
    {
        runtime.SetWallMaxHealth(GetMaxWallHealth(), false);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
