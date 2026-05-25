using System;
using UnityEngine;

[Serializable]
public class DefenseRuntimeState
{
    [SerializeField] private DefenseState state = DefenseState.Idle;
    [SerializeField] private FrontlineMode mode = FrontlineMode.Push;
    [SerializeField] private int frontlineLevel = 1;
    [SerializeField] private float wallHealth = 100f;
    [SerializeField] private float wallMaxHealth = 100f;
    [SerializeField] private float enemyPressure;
    [SerializeField] private float enemyPressureCapacity = 100f;
    [SerializeField] private float frontlineProgress;
    [SerializeField] private float frontlineProgressRequired = 100f;
    [SerializeField] private float totalElapsed;
    [SerializeField] private float levelElapsed;
    [SerializeField] private bool wallDamaged;
    [SerializeField] private float lastIncomingPressurePerSecond;
    [SerializeField] private float lastPressureClearedPerSecond;
    [SerializeField] private float lastWallDamagePerSecond;
    [SerializeField] private float lastProgressPerSecond;

    public DefenseState State => state;
    public FrontlineMode Mode => mode;
    public int FrontlineLevel => frontlineLevel;
    public float WallHealth => wallHealth;
    public float WallMaxHealth => wallMaxHealth;
    public float EnemyPressure => enemyPressure;
    public float EnemyPressureCapacity => enemyPressureCapacity;
    public float FrontlineProgress => frontlineProgress;
    public float FrontlineProgressRequired => frontlineProgressRequired;
    public float TotalElapsed => totalElapsed;
    public float LevelElapsed => levelElapsed;
    public bool WallDamaged => wallDamaged;
    public float LastIncomingPressurePerSecond => lastIncomingPressurePerSecond;
    public float LastPressureClearedPerSecond => lastPressureClearedPerSecond;
    public float LastWallDamagePerSecond => lastWallDamagePerSecond;
    public float LastProgressPerSecond => lastProgressPerSecond;
    public float WallHealthPercent => wallMaxHealth <= 0f ? 0f : Mathf.Clamp01(wallHealth / wallMaxHealth);
    public float PressurePercent => enemyPressureCapacity <= 0f ? 0f : Mathf.Clamp01(enemyPressure / enemyPressureCapacity);
    public float FrontlineProgressPercent => frontlineProgressRequired <= 0f ? 0f : Mathf.Clamp01(frontlineProgress / frontlineProgressRequired);
    public bool IsRunning => state == DefenseState.Holding || state == DefenseState.Pushing;

    public void Initialize(float maxWallHealth, int startingLevel, FrontlineMode startingMode, float pressureCapacity, float progressRequired)
    {
        frontlineLevel = Mathf.Max(1, startingLevel);
        mode = startingMode;
        wallMaxHealth = Mathf.Max(1f, maxWallHealth);
        wallHealth = wallMaxHealth;
        enemyPressure = 0f;
        enemyPressureCapacity = Mathf.Max(1f, pressureCapacity);
        frontlineProgress = 0f;
        frontlineProgressRequired = Mathf.Max(1f, progressRequired);
        totalElapsed = 0f;
        levelElapsed = 0f;
        wallDamaged = false;
        ClearLastTickFeedback();
        state = DefenseState.Idle;
    }

    public void StartFrontline()
    {
        state = mode == FrontlineMode.Push ? DefenseState.Pushing : DefenseState.Holding;
    }

    public void SetMode(FrontlineMode nextMode)
    {
        mode = nextMode;

        if (IsRunning)
        {
            state = mode == FrontlineMode.Push ? DefenseState.Pushing : DefenseState.Holding;
        }
    }

    public void SetPressureCapacity(float pressureCapacity)
    {
        enemyPressureCapacity = Mathf.Max(1f, pressureCapacity);
        enemyPressure = Mathf.Clamp(enemyPressure, 0f, enemyPressureCapacity);
    }

    public void SetProgressRequired(float progressRequired)
    {
        frontlineProgressRequired = Mathf.Max(1f, progressRequired);
        frontlineProgress = Mathf.Clamp(frontlineProgress, 0f, frontlineProgressRequired);
    }

    public float TickFrontline(float deltaTime, float incomingPressurePerSecond, float defensePowerPerSecond, float wallDamagePerPressureSecond, float progressPerSecond)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        totalElapsed += safeDeltaTime;
        levelElapsed += safeDeltaTime;

        float safeIncomingPressurePerSecond = Mathf.Max(0f, incomingPressurePerSecond);
        float safeDefensePowerPerSecond = Mathf.Max(0f, defensePowerPerSecond);
        float incomingPressure = safeIncomingPressurePerSecond * safeDeltaTime;
        float clearedPressure = safeDefensePowerPerSecond * safeDeltaTime;
        float pressureBeforeDefense = enemyPressure + incomingPressure;
        float effectiveClearedPressure = Mathf.Min(Mathf.Max(0f, pressureBeforeDefense), clearedPressure);

        lastIncomingPressurePerSecond = safeIncomingPressurePerSecond;
        lastPressureClearedPerSecond = safeDeltaTime <= 0f ? 0f : effectiveClearedPressure / safeDeltaTime;
        lastWallDamagePerSecond = 0f;
        lastProgressPerSecond = 0f;

        enemyPressure = Mathf.Clamp(enemyPressure + incomingPressure - clearedPressure, 0f, enemyPressureCapacity);

        if (enemyPressure > 0f)
        {
            float wallDamage = enemyPressure * Mathf.Max(0f, wallDamagePerPressureSecond) * safeDeltaTime;
            lastWallDamagePerSecond = safeDeltaTime <= 0f ? 0f : wallDamage / safeDeltaTime;
            ApplyWallDamage(wallDamage);
            return 0f;
        }

        if (mode != FrontlineMode.Push)
        {
            return 0f;
        }

        lastProgressPerSecond = Mathf.Max(0f, progressPerSecond);
        float progressAdded = lastProgressPerSecond * safeDeltaTime;
        frontlineProgress += progressAdded;
        return progressAdded;
    }

    public void AdvanceLevel(float nextProgressRequired, float nextPressureCapacity)
    {
        frontlineLevel += 1;
        frontlineProgress = 0f;
        levelElapsed = 0f;
        SetProgressRequired(nextProgressRequired);
        SetPressureCapacity(nextPressureCapacity);
    }

    public void MarkBreached()
    {
        wallHealth = 0f;
        wallDamaged = true;
        enemyPressure = enemyPressureCapacity;
        state = DefenseState.Breached;
    }

    public void MoveToRepairState()
    {
        state = DefenseState.WaitingForRepairOrUpgrade;
    }

    public void ApplyWallDamage(float amount)
    {
        if (amount <= 0f || wallHealth <= 0f)
        {
            return;
        }

        wallHealth = Mathf.Max(0f, wallHealth - amount);
        wallDamaged = wallHealth < wallMaxHealth;
    }

    public void RefillWall()
    {
        wallHealth = wallMaxHealth;
        wallDamaged = false;
        enemyPressure = 0f;
    }

    public void SetWallMaxHealth(float maxWallHealth, bool refillToMax)
    {
        wallMaxHealth = Mathf.Max(1f, maxWallHealth);
        wallHealth = refillToMax ? wallMaxHealth : Mathf.Clamp(wallHealth, 0f, wallMaxHealth);
        wallDamaged = wallHealth < wallMaxHealth;
    }

    public void WriteSaveData(DefenseSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.state = state;
        saveData.mode = mode;
        saveData.frontlineLevel = frontlineLevel;
        saveData.wallCurrentHealth = wallHealth;
        saveData.enemyPressure = enemyPressure;
        saveData.frontlineProgress = frontlineProgress;
        saveData.wallDamaged = wallDamaged;
        saveData.totalElapsed = totalElapsed;
        saveData.levelElapsed = levelElapsed;
    }

    public void ApplySaveData(DefenseSaveData saveData, float maxWallHealth, float pressureCapacity, float progressRequired)
    {
        if (saveData == null)
        {
            return;
        }

        state = Enum.IsDefined(typeof(DefenseState), saveData.state) ? saveData.state : DefenseState.Idle;
        mode = Enum.IsDefined(typeof(FrontlineMode), saveData.mode) ? saveData.mode : FrontlineMode.Push;
        frontlineLevel = Mathf.Max(1, saveData.frontlineLevel);
        wallMaxHealth = Mathf.Max(1f, maxWallHealth);
        wallHealth = Mathf.Clamp(saveData.wallCurrentHealth <= 0f && state != DefenseState.Breached ? wallMaxHealth : saveData.wallCurrentHealth, 0f, wallMaxHealth);
        enemyPressureCapacity = Mathf.Max(1f, pressureCapacity);
        enemyPressure = Mathf.Clamp(saveData.enemyPressure, 0f, enemyPressureCapacity);
        frontlineProgressRequired = Mathf.Max(1f, progressRequired);
        frontlineProgress = Mathf.Clamp(saveData.frontlineProgress, 0f, frontlineProgressRequired);
        totalElapsed = Mathf.Max(0f, saveData.totalElapsed);
        levelElapsed = Mathf.Max(0f, saveData.levelElapsed);
        wallDamaged = saveData.wallDamaged || wallHealth < wallMaxHealth;
        ClearLastTickFeedback();

        if (state == DefenseState.Holding && mode == FrontlineMode.Push)
        {
            state = DefenseState.Pushing;
        }
        else if (state == DefenseState.Pushing && mode == FrontlineMode.Hold)
        {
            state = DefenseState.Holding;
        }
    }

    private void ClearLastTickFeedback()
    {
        lastIncomingPressurePerSecond = 0f;
        lastPressureClearedPerSecond = 0f;
        lastWallDamagePerSecond = 0f;
        lastProgressPerSecond = 0f;
    }
}
