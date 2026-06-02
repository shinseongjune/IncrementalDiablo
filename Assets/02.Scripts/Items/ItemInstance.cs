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

    public bool TryApplyPrototypeAffixReroll(out ItemAffixRoll affixRoll)
    {
        affixRoll = null;
        if (Rarity != ItemRarity.Rare)
        {
            return false;
        }

        if (!TryCreatePrototypeAffixModifierAvoidingCurrent(out string affixId, out StatMod modifier))
        {
            return false;
        }

        affixRoll = new ItemAffixRoll(affixId, modifier);
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

    private bool TryCreatePrototypeAffixModifierAvoidingCurrent(out string affixId, out StatMod modifier)
    {
        affixId = string.Empty;
        modifier = null;

        int candidateCount = GetPrototypeAffixCandidateCount();
        if (candidateCount <= 0)
        {
            return false;
        }

        int firstRoll = UnityEngine.Random.Range(0, candidateCount);
        string fallbackAffixId = null;
        StatMod fallbackModifier = null;
        for (int attempt = 0; attempt < candidateCount; attempt++)
        {
            int candidateRoll = (firstRoll + attempt) % candidateCount;
            if (!TryCreatePrototypeAffixModifier(candidateRoll, out string candidateAffixId, out StatMod candidateModifier))
            {
                continue;
            }

            fallbackAffixId ??= candidateAffixId;
            fallbackModifier ??= candidateModifier;
            if (!HasMatchingAffix(AffixRolls, candidateAffixId, candidateModifier))
            {
                affixId = candidateAffixId;
                modifier = candidateModifier;
                return true;
            }
        }

        affixId = fallbackAffixId ?? string.Empty;
        modifier = fallbackModifier;
        return modifier != null;
    }

    private int GetPrototypeAffixCandidateCount()
    {
        switch (Slot)
        {
            case ItemSlot.Weapon:
            case ItemSlot.Armor:
            case ItemSlot.Ring:
                return 2;
            default:
                return 0;
        }
    }

    private bool TryCreatePrototypeAffixModifier(int roll, out string affixId, out StatMod modifier)
    {
        affixId = string.Empty;
        modifier = null;

        int normalizedRoll = Mathf.Abs(roll) % 2;
        float scaledPower = Mathf.Max(1f, Level + RolledPower * 0.25f);
        switch (Slot)
        {
            case ItemSlot.Weapon:
                if (normalizedRoll == 0)
                {
                    affixId = "prototype_attack_damage";
                    modifier = new StatMod(StatId.AttackDamage, StatMod.StatModType.Flat, Mathf.Ceil(scaledPower * 1.15f));
                }
                else
                {
                    affixId = "prototype_attack_speed";
                    modifier = new StatMod(StatId.AttackSpeed, StatMod.StatModType.PercentAdd, Mathf.Ceil(3f + scaledPower * 0.4f));
                }

                return true;
            case ItemSlot.Armor:
                if (normalizedRoll == 0)
                {
                    affixId = "prototype_max_health";
                    modifier = new StatMod(StatId.MaxHealth, StatMod.StatModType.Flat, Mathf.Ceil(6f + scaledPower * 4f));
                }
                else
                {
                    affixId = "prototype_move_speed";
                    modifier = new StatMod(StatId.MoveSpeed, StatMod.StatModType.PercentAdd, Mathf.Ceil(2f + scaledPower * 0.25f));
                }

                return true;
            case ItemSlot.Ring:
                if (normalizedRoll == 0)
                {
                    affixId = "prototype_attack_speed";
                    modifier = new StatMod(StatId.AttackSpeed, StatMod.StatModType.PercentAdd, Mathf.Ceil(4f + scaledPower * 0.55f));
                }
                else
                {
                    affixId = "prototype_move_speed";
                    modifier = new StatMod(StatId.MoveSpeed, StatMod.StatModType.PercentAdd, Mathf.Ceil(3f + scaledPower * 0.45f));
                }

                return true;
            default:
                return false;
        }
    }

    private static bool HasMatchingAffix(ItemAffixRoll[] rolls, string affixId, StatMod modifier)
    {
        if (rolls == null || modifier == null)
        {
            return false;
        }

        for (int i = 0; i < rolls.Length; i++)
        {
            ItemAffixRoll roll = rolls[i];
            if (roll == null || roll.Modifier == null)
            {
                continue;
            }

            if (string.Equals(roll.AffixId, affixId, StringComparison.Ordinal) && HasSameModifier(roll.Modifier, modifier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSameModifier(StatMod first, StatMod second)
    {
        return first != null
            && second != null
            && first.StatId == second.StatId
            && first.Type == second.Type
            && Mathf.Approximately(first.Value, second.Value);
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
