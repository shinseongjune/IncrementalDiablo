using System;

/// <summary>
/// Complete, self-contained player profile. Format v2 separates account progression from the
/// concrete defense and dungeon worlds so runtime actors no longer disappear into aggregate data.
/// </summary>
[Serializable]
public sealed class GameProfileSave
{
    public const int CurrentFormatVersion = 2;

    public int formatVersion = CurrentFormatVersion;
    public long generation;
    public string savedAtUtc;
    public string integrityHash;
    public AccountSnapshot account = new AccountSnapshot();
    public DefenseWorldSnapshot defenseWorld = new DefenseWorldSnapshot();
    public DungeonWorldSnapshot dungeonWorld;
}

[Serializable]
public class UiSettingsSaveData
{
    public bool useCompactStatusText = true;
    public bool showDetailedBalanceText;
    public bool showDiagnosticStatusText;
    public bool showFirstSessionGuide = true;
    public bool emphasizeFirstRecoverySave = true;
}

[Serializable]
public class DefenseSaveData
{
    public DefenseState state = DefenseState.Idle;
    public FrontlineMode mode = FrontlineMode.Push;
    public int frontlineLevel = 1;
    public int wallLevel = 1;
    public int towerLevel = 1;
    public int defenderLevel = 1;
    public float wallCurrentHealth = 100f;
    public float enemyPressure;
    public float frontlineProgress;
    public bool wallDamaged;
    public float totalElapsed;
    public float levelElapsed;
}

[Serializable]
public class HeroSaveData
{
    public int level = 1;
    public float experience;
    public long[] equippedItemInstanceIds = Array.Empty<long>();
}

[Serializable]
public class InventorySaveData
{
    public long nextItemInstanceId = 1;
    public ItemInstanceSaveData[] itemInstances = Array.Empty<ItemInstanceSaveData>();
}

[Serializable]
public class ItemInstanceSaveData
{
    public long instanceId;
    public string definitionId;
    public string displayName;
    public ItemSlot slot;
    public ItemRarity rarity = ItemRarity.Normal;
    public int level = 1;
    public int rolledPower;
    public ItemAffixRoll[] affixRolls = Array.Empty<ItemAffixRoll>();
    public int durability = 100;
    public bool equipped;
}
