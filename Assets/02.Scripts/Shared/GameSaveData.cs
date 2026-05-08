using System;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public string savedAtUtc;
    public float playTimeSeconds;
    public ResourceAmount[] currencies;
    public DefenseSaveData defense = new DefenseSaveData();
    public DungeonSaveData dungeon = new DungeonSaveData();
    public HeroSaveData hero = new HeroSaveData();
    public InventorySaveData inventory = new InventorySaveData();
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
    public float currentHealth = 100f;
    public long[] equippedItemInstanceIds = new long[0];
}

[Serializable]
public class DungeonSaveData
{
    public DungeonRunState state = DungeonRunState.Ready;
    public string dungeonId;
    public int depth = 1;
    public int totalRooms = 1;
    public int currentRoomIndex;
    public int roomsCompleted;
    public float elapsedSeconds;
    public bool rewardPending;
    public string lastResult;
}

[Serializable]
public class InventorySaveData
{
    public long nextItemInstanceId = 1;
    public ItemInstanceSaveData[] itemInstances = new ItemInstanceSaveData[0];
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
    public ItemAffixRoll[] affixRolls = new ItemAffixRoll[0];
    public int durability = 100;
    public bool equipped;
}
