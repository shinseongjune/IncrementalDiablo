using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    [SerializeField] private long instanceId;
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private string definitionId;
    [SerializeField] private string displayName;
    [SerializeField] private ItemSlot slot;
    [SerializeField] private ItemRarity rarity = ItemRarity.Normal;
    [SerializeField] private int level = 1;
    [SerializeField] private int rolledPower;
    [SerializeField] private ItemAffixRoll[] affixRolls = new ItemAffixRoll[0];
    [SerializeField] private int durability = 100;
    [SerializeField] private bool equipped;

    public long InstanceId => instanceId;
    public ItemDefinition Definition => definition;
    public string DefinitionId => string.IsNullOrWhiteSpace(definitionId) && definition != null ? definition.Id : definitionId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? DefinitionId : displayName;
    public ItemSlot Slot => slot;
    public ItemRarity Rarity => rarity;
    public int Level => Mathf.Max(1, level);
    public int RolledPower => Mathf.Max(0, rolledPower);
    public ItemAffixRoll[] AffixRolls => affixRolls ?? new ItemAffixRoll[0];
    public int Durability => Mathf.Clamp(durability, 0, 100);
    public bool Equipped => equipped;

    public ItemInstance()
    {
    }

    public ItemInstance(long instanceId, ItemDefinition definition, int level, int rolledPower)
    {
        this.instanceId = Math.Max(1, instanceId);
        this.definition = definition;
        definitionId = definition == null ? string.Empty : definition.Id;
        displayName = definition == null ? string.Empty : definition.DisplayName;
        slot = definition == null ? ItemSlot.Weapon : definition.Slot;
        rarity = definition == null ? ItemRarity.Normal : definition.Rarity;
        this.level = Mathf.Max(1, level);
        this.rolledPower = Mathf.Max(0, rolledPower);
        affixRolls = new ItemAffixRoll[0];
        durability = 100;
    }

    public static ItemInstance CreateFromDefinition(long instanceId, ItemDefinition definition, int level = 1)
    {
        if (definition == null)
        {
            return null;
        }

        int minPower = Mathf.Max(0, definition.BaseMinPower);
        int maxPower = Mathf.Max(minPower, definition.BaseMaxPower);
        int rolledPower = minPower == maxPower ? minPower : UnityEngine.Random.Range(minPower, maxPower + 1);
        return new ItemInstance(instanceId, definition, Mathf.Max(1, level), rolledPower);
    }

    public static ItemInstance FromSaveData(ItemInstanceSaveData saveData)
    {
        if (saveData == null)
        {
            return null;
        }

        ItemInstance item = new ItemInstance
        {
            instanceId = Math.Max(1, saveData.instanceId),
            definitionId = saveData.definitionId,
            displayName = saveData.displayName,
            slot = saveData.slot,
            rarity = saveData.rarity,
            level = Mathf.Max(1, saveData.level),
            rolledPower = Mathf.Max(0, saveData.rolledPower),
            affixRolls = saveData.affixRolls ?? new ItemAffixRoll[0],
            durability = Mathf.Clamp(saveData.durability, 0, 100),
            equipped = saveData.equipped
        };

        return item;
    }

    public bool TrySetDefinition(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return false;
        }

        string currentDefinitionId = DefinitionId;
        if (!string.IsNullOrWhiteSpace(currentDefinitionId) && currentDefinitionId != itemDefinition.Id)
        {
            return false;
        }

        definition = itemDefinition;
        definitionId = itemDefinition.Id;
        displayName = itemDefinition.DisplayName;
        slot = itemDefinition.Slot;
        rarity = itemDefinition.Rarity;
        return true;
    }

    public ItemInstanceSaveData ToSaveData()
    {
        return new ItemInstanceSaveData
        {
            instanceId = InstanceId,
            definitionId = DefinitionId,
            displayName = DisplayName,
            slot = Slot,
            rarity = Rarity,
            level = Level,
            rolledPower = RolledPower,
            affixRolls = AffixRolls,
            durability = Durability,
            equipped = Equipped
        };
    }

    public void EnsureIdentity(long fallbackInstanceId)
    {
        if (instanceId <= 0)
        {
            instanceId = Math.Max(1, fallbackInstanceId);
        }

        if (definition != null)
        {
            definitionId = definition.Id;
            displayName = definition.DisplayName;
            slot = definition.Slot;
            rarity = definition.Rarity;
        }

        level = Mathf.Max(1, level);
        rolledPower = Mathf.Max(0, rolledPower);
        affixRolls ??= new ItemAffixRoll[0];
        durability = Mathf.Clamp(durability, 0, 100);
    }

    public void SetEquipped(bool value)
    {
        equipped = value;
    }
}

[Serializable]
public class ItemAffixRoll
{
    [SerializeField] private string affixId;
    [SerializeField] private StatMod modifier = new StatMod();

    public string AffixId => affixId;
    public StatMod Modifier => modifier;

    public ItemAffixRoll()
    {
    }

    public ItemAffixRoll(string affixId, StatMod modifier)
    {
        this.affixId = affixId;
        this.modifier = modifier;
    }
}
