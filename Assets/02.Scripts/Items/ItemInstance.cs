using System;
using System.Collections.Generic;
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
    public bool IsDefinitionResolved => definition != null;
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
        return CreateFromDefinition(instanceId, definition, level, 1f);
    }

    public static ItemInstance CreateFromDefinition(
        long instanceId,
        ItemDefinition definition,
        int level,
        float powerMultiplier)
    {
        if (definition == null)
        {
            return null;
        }

        int minPower = Mathf.Max(0, definition.BaseMinPower);
        int maxPower = Mathf.Max(minPower, definition.BaseMaxPower);
        int basePower = minPower == maxPower ? minPower : UnityEngine.Random.Range(minPower, maxPower + 1);
        int rolledPower = Mathf.CeilToInt(basePower * Mathf.Max(1f, powerMultiplier));
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

    public bool ApplyResolvedDefinition(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
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

    public bool TryApplyAuthoredAffixReroll(out ItemAffixRoll affixRoll)
    {
        affixRoll = null;
        if (Rarity != ItemRarity.Rare)
        {
            return false;
        }

        if (!ItemEconomyModel.TryRollAuthoredRareAffix(this, out affixRoll))
        {
            return false;
        }

        affixRolls = new[] { affixRoll };
        return true;
    }

    public void AppendModifiers(StatId statId, List<StatMod> results)
    {
        if (results == null)
        {
            return;
        }

        AppendDefinitionModifiers(statId, results);
        AppendAffixModifiers(statId, results);
        AppendRolledPowerModifier(statId, results);
    }

    private void AppendDefinitionModifiers(StatId statId, List<StatMod> results)
    {
        if (definition == null || definition.Modifiers == null)
        {
            return;
        }

        StatMod[] modifiers = definition.Modifiers;
        for (int i = 0; i < modifiers.Length; i++)
        {
            StatMod modifier = modifiers[i];
            if (modifier != null && modifier.AppliesTo(statId))
            {
                results.Add(modifier);
            }
        }
    }

    private void AppendAffixModifiers(StatId statId, List<StatMod> results)
    {
        ItemAffixRoll[] rolls = AffixRolls;
        for (int i = 0; i < rolls.Length; i++)
        {
            StatMod modifier = rolls[i]?.Modifier;
            if (modifier != null && modifier.AppliesTo(statId))
            {
                results.Add(modifier);
            }
        }
    }

    private void AppendRolledPowerModifier(StatId statId, List<StatMod> results)
    {
        if (!TryCreateRolledPowerModifier(statId, out StatMod modifier))
        {
            return;
        }

        results.Add(modifier);
    }

    private bool TryCreateRolledPowerModifier(StatId statId, out StatMod modifier)
    {
        modifier = null;

        int power = RolledPower;
        if (power <= 0)
        {
            return false;
        }

        float scaledPower = power * GetRarityPowerMultiplier(Rarity);
        switch (Slot)
        {
            case ItemSlot.Weapon:
                if (statId != StatId.AttackDamage)
                {
                    return false;
                }

                modifier = new StatMod(statId, StatMod.StatModType.Flat, Mathf.Ceil(scaledPower));
                return true;
            case ItemSlot.Armor:
                if (statId != StatId.MaxHealth)
                {
                    return false;
                }

                modifier = new StatMod(statId, StatMod.StatModType.Flat, Mathf.Ceil(scaledPower * 4f));
                return true;
            case ItemSlot.Ring:
                if (statId != StatId.AttackSpeed)
                {
                    return false;
                }

                modifier = new StatMod(statId, StatMod.StatModType.PercentAdd, Mathf.Ceil(scaledPower * 1.5f));
                return true;
            default:
                return false;
        }
    }

    private static float GetRarityPowerMultiplier(ItemRarity itemRarity)
    {
        switch (itemRarity)
        {
            case ItemRarity.Magic:
                return 1.2f;
            case ItemRarity.Rare:
                return 1.5f;
            default:
                return 1f;
        }
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
