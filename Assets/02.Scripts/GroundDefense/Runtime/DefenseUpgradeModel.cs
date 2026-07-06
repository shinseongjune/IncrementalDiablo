using System;
using UnityEngine;

public class DefenseUpgradeModel : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private int wallLevel = 1;
    [SerializeField] private int towerLevel = 1;
    [SerializeField] private int defenderLevel = 1;

    [Header("Wall")]
    [SerializeField] private float baseWallHealth = 100f;
    [SerializeField] private float wallHealthPerLevel = 25f;
    [SerializeField] private float wallPressureReductionPerLevel = 0.35f;

    [Header("Damage")]
    [SerializeField] private float baseTowerDamagePerSecond = 8f;
    [SerializeField] private float towerDamagePerLevel = 3f;
    [SerializeField] private float baseDefenderDamagePerSecond = 5f;
    [SerializeField] private float defenderDamagePerLevel = 2f;

    [Header("Costs")]
    [SerializeField] private float costGrowth = 1.35f;
    [SerializeField] private int wallUpgradeBaseGoldCost = 35;
    [SerializeField] private int wallUpgradeBaseScrapCost = 6;
    [SerializeField] private int towerUpgradeBaseGoldCost = 30;
    [SerializeField] private int towerUpgradeBaseScrapCost = 8;
    [SerializeField] private int defenderUpgradeBaseGoldCost = 25;
    [SerializeField] private int defenderUpgradeBaseScrapCost = 5;
    [SerializeField] private float repairGoldPerMissingHealth = 0.2f;
    [SerializeField] private int minimumRepairGoldCost = 1;

    public event Action Changed;

    public int WallLevel => wallLevel;
    public int TowerLevel => towerLevel;
    public int DefenderLevel => defenderLevel;
    public float MaxWallHealth => baseWallHealth + (WallLevel - 1) * wallHealthPerLevel;
    public float TowerDamagePerSecond => baseTowerDamagePerSecond + (TowerLevel - 1) * towerDamagePerLevel;
    public float DefenderDamagePerSecond => baseDefenderDamagePerSecond + (DefenderLevel - 1) * defenderDamagePerLevel;
    public float TotalDefensePower => TowerDamagePerSecond + DefenderDamagePerSecond;
    public float WallPressureReductionPerSecond => (WallLevel - 1) * wallPressureReductionPerLevel;
    public float WallHealthGainPerUpgrade => wallHealthPerLevel;
    public float WallPressureReductionGainPerUpgrade => wallPressureReductionPerLevel;
    public float TowerDamageGainPerUpgrade => towerDamagePerLevel;
    public float DefenderDamageGainPerUpgrade => defenderDamagePerLevel;

    private void OnValidate()
    {
        wallLevel = Mathf.Max(1, wallLevel);
        towerLevel = Mathf.Max(1, towerLevel);
        defenderLevel = Mathf.Max(1, defenderLevel);
        baseWallHealth = Mathf.Max(1f, baseWallHealth);
        wallHealthPerLevel = Mathf.Max(0f, wallHealthPerLevel);
        wallPressureReductionPerLevel = Mathf.Max(0f, wallPressureReductionPerLevel);
        baseTowerDamagePerSecond = Mathf.Max(0f, baseTowerDamagePerSecond);
        towerDamagePerLevel = Mathf.Max(0f, towerDamagePerLevel);
        baseDefenderDamagePerSecond = Mathf.Max(0f, baseDefenderDamagePerSecond);
        defenderDamagePerLevel = Mathf.Max(0f, defenderDamagePerLevel);
        costGrowth = Mathf.Max(1f, costGrowth);
        wallUpgradeBaseGoldCost = Mathf.Max(0, wallUpgradeBaseGoldCost);
        wallUpgradeBaseScrapCost = Mathf.Max(0, wallUpgradeBaseScrapCost);
        towerUpgradeBaseGoldCost = Mathf.Max(0, towerUpgradeBaseGoldCost);
        towerUpgradeBaseScrapCost = Mathf.Max(0, towerUpgradeBaseScrapCost);
        defenderUpgradeBaseGoldCost = Mathf.Max(0, defenderUpgradeBaseGoldCost);
        defenderUpgradeBaseScrapCost = Mathf.Max(0, defenderUpgradeBaseScrapCost);
        repairGoldPerMissingHealth = Mathf.Max(0f, repairGoldPerMissingHealth);
        minimumRepairGoldCost = Mathf.Max(0, minimumRepairGoldCost);
    }

    public ResourceAmount[] GetWallUpgradeCost()
    {
        return new[]
        {
            new ResourceAmount(ResourceId.Gold, ScaleCost(wallUpgradeBaseGoldCost, WallLevel)),
            new ResourceAmount(ResourceId.Scrap, ScaleCost(wallUpgradeBaseScrapCost, WallLevel))
        };
    }

    public ResourceAmount[] GetTowerUpgradeCost()
    {
        return new[]
        {
            new ResourceAmount(ResourceId.Gold, ScaleCost(towerUpgradeBaseGoldCost, TowerLevel)),
            new ResourceAmount(ResourceId.Scrap, ScaleCost(towerUpgradeBaseScrapCost, TowerLevel))
        };
    }

    public ResourceAmount[] GetDefenderUpgradeCost()
    {
        return new[]
        {
            new ResourceAmount(ResourceId.Gold, ScaleCost(defenderUpgradeBaseGoldCost, DefenderLevel)),
            new ResourceAmount(ResourceId.Scrap, ScaleCost(defenderUpgradeBaseScrapCost, DefenderLevel))
        };
    }

    public ResourceAmount[] GetRepairCost(float missingHealth)
    {
        if (missingHealth <= 0f)
        {
            return new ResourceAmount[0];
        }

        int goldCost = Mathf.Max(minimumRepairGoldCost, Mathf.CeilToInt(missingHealth * repairGoldPerMissingHealth));
        return new[] { new ResourceAmount(ResourceId.Gold, goldCost) };
    }

    public bool TryUpgradeWall(CurrencyWallet wallet)
    {
        if (!TrySpend(wallet, GetWallUpgradeCost()))
        {
            return false;
        }

        wallLevel += 1;
        Changed?.Invoke();
        return true;
    }

    public bool TryUpgradeTower(CurrencyWallet wallet)
    {
        if (!TrySpend(wallet, GetTowerUpgradeCost()))
        {
            return false;
        }

        towerLevel += 1;
        Changed?.Invoke();
        return true;
    }

    public bool TryUpgradeDefender(CurrencyWallet wallet)
    {
        if (!TrySpend(wallet, GetDefenderUpgradeCost()))
        {
            return false;
        }

        defenderLevel += 1;
        Changed?.Invoke();
        return true;
    }

    public void SetLevels(int nextWallLevel, int nextTowerLevel, int nextDefenderLevel)
    {
        nextWallLevel = Mathf.Max(1, nextWallLevel);
        nextTowerLevel = Mathf.Max(1, nextTowerLevel);
        nextDefenderLevel = Mathf.Max(1, nextDefenderLevel);

        if (wallLevel == nextWallLevel && towerLevel == nextTowerLevel && defenderLevel == nextDefenderLevel)
        {
            return;
        }

        wallLevel = nextWallLevel;
        towerLevel = nextTowerLevel;
        defenderLevel = nextDefenderLevel;
        Changed?.Invoke();
    }

    private int ScaleCost(int baseCost, int currentLevel)
    {
        return Mathf.CeilToInt(baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, currentLevel - 1)));
    }

    private bool TrySpend(CurrencyWallet wallet, ResourceAmount[] cost)
    {
        if (wallet == null)
        {
            Debug.LogWarning("DefenseUpgradeModel needs a CurrencyWallet before upgrades can be bought.", this);
            return false;
        }

        return wallet.TrySpend(cost);
    }
}
