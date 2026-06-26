using System;
using System.Collections.Generic;
using UnityEngine;

public static class ItemEconomyModel
{
    private const float RerollGoldBaseCost = 50f;
    private const float RerollGoldGrowth = 1.35f;

    private static readonly ItemAffixProfile[] RareAffixes =
    {
        new ItemAffixProfile(
            "rare_wounding_edge",
            "Wounding Edge",
            ItemSlot.Weapon,
            StatId.AttackDamage,
            StatMod.StatModType.Flat,
            2f,
            0.8f,
            0.5f,
            120,
            "weapon|offense|flat"),
        new ItemAffixProfile(
            "rare_quickened_edge",
            "Quickened Edge",
            ItemSlot.Weapon,
            StatId.AttackSpeed,
            StatMod.StatModType.PercentAdd,
            3f,
            0.25f,
            0.2f,
            85,
            "weapon|offense|speed"),
        new ItemAffixProfile(
            "rare_vital_plating",
            "Vital Plating",
            ItemSlot.Armor,
            StatId.MaxHealth,
            StatMod.StatModType.Flat,
            8f,
            3.5f,
            2f,
            120,
            "armor|survival|flat"),
        new ItemAffixProfile(
            "rare_runner_plate",
            "Runner Plate",
            ItemSlot.Armor,
            StatId.MoveSpeed,
            StatMod.StatModType.PercentAdd,
            2f,
            0.2f,
            0.15f,
            70,
            "armor|mobility|speed"),
        new ItemAffixProfile(
            "rare_swift_band",
            "Swift Band",
            ItemSlot.Ring,
            StatId.AttackSpeed,
            StatMod.StatModType.PercentAdd,
            4f,
            0.35f,
            0.25f,
            100,
            "ring|offense|speed"),
        new ItemAffixProfile(
            "rare_runner_band",
            "Runner Band",
            ItemSlot.Ring,
            StatId.MoveSpeed,
            StatMod.StatModType.PercentAdd,
            3f,
            0.3f,
            0.2f,
            90,
            "ring|mobility|speed")
    };

    public static IReadOnlyList<ItemAffixProfile> AuthoredRareAffixes => RareAffixes;

    public static bool TryFindAutoConversionMatch(
        ItemInstance candidate,
        IReadOnlyList<ItemInstance> ownedItems,
        out ItemInstance retainedItem)
    {
        retainedItem = null;
        if (candidate == null ||
            !candidate.IsDefinitionResolved ||
            string.IsNullOrWhiteSpace(candidate.DefinitionId) ||
            ownedItems == null)
        {
            return false;
        }

        for (int i = 0; i < ownedItems.Count; i++)
        {
            ItemInstance ownedItem = ownedItems[i];
            if (ownedItem == null ||
                !ownedItem.IsDefinitionResolved ||
                !string.Equals(ownedItem.DefinitionId, candidate.DefinitionId, StringComparison.Ordinal) ||
                ownedItem.Level < candidate.Level ||
                ownedItem.RolledPower < candidate.RolledPower)
            {
                continue;
            }

            if (retainedItem == null ||
                ownedItem.Level > retainedItem.Level ||
                (ownedItem.Level == retainedItem.Level && ownedItem.RolledPower > retainedItem.RolledPower))
            {
                retainedItem = ownedItem;
            }
        }

        return retainedItem != null;
    }

    public static ResourceAmount[] GetSalvageRewards(ItemInstance item)
    {
        if (item == null)
        {
            return new ResourceAmount[0];
        }

        float yieldMultiplier = DungeonDepthBalanceModel.Evaluate(item.Level).MaterialYieldMultiplier;
        return item.Definition == null
            ? GetSalvageRewards(item.Slot, item.Rarity, 1, yieldMultiplier)
            : GetSalvageRewards(item.Definition, yieldMultiplier);
    }

    public static ResourceAmount[] GetSalvageRewards(ItemDefinition item)
    {
        return GetSalvageRewards(item, 1f);
    }

    public static ResourceAmount[] GetSalvageRewards(ItemDefinition item, float yieldMultiplier)
    {
        if (item == null)
        {
            return new ResourceAmount[0];
        }

        return GetSalvageRewards(item.Slot, item.Rarity, item.BaseTier, yieldMultiplier);
    }

    public static ResourceAmount[] GetSalvageRewards(ItemSlot slot, ItemRarity rarity, int tier)
    {
        return GetSalvageRewards(slot, rarity, tier, 1f);
    }

    public static ResourceAmount[] GetSalvageRewards(
        ItemSlot slot,
        ItemRarity rarity,
        int tier,
        float yieldMultiplier)
    {
        int safeTier = Mathf.Max(1, tier);
        int scrap = safeTier * GetSlotScrapWeight(slot) * GetRarityScrapWeight(rarity);
        List<ResourceAmount> rewards = new List<ResourceAmount>
        {
            new ResourceAmount(ResourceId.Scrap, Mathf.Max(1, scrap))
        };

        int essence = GetEssenceReward(rarity, safeTier);
        if (essence > 0)
        {
            rewards.Add(new ResourceAmount(ResourceId.Essence, essence));
        }

        int alterStone = GetAlterStoneReward(rarity, safeTier);
        if (alterStone > 0)
        {
            rewards.Add(new ResourceAmount(ResourceId.AlterStone, alterStone));
        }

        return ScaleRewards(rewards, yieldMultiplier);
    }

    public static bool CanRerollAffix(ItemDefinition item)
    {
        return item != null && item.Rarity == ItemRarity.Rare;
    }

    public static ResourceAmount[] GetAffixRerollCost(ItemDefinition item)
    {
        if (!CanRerollAffix(item))
        {
            return new ResourceAmount[0];
        }

        int tier = Mathf.Max(1, item.BaseTier);
        return new[]
        {
            new ResourceAmount(ResourceId.Gold, Mathf.CeilToInt(RerollGoldBaseCost * Mathf.Pow(RerollGoldGrowth, tier - 1))),
            new ResourceAmount(ResourceId.Essence, Mathf.Max(1, Mathf.CeilToInt(tier * 0.75f))),
            new ResourceAmount(ResourceId.AlterStone, 1 + tier / 10)
        };
    }

    public static bool TryRollAuthoredRareAffix(ItemInstance item, out ItemAffixRoll affixRoll)
    {
        affixRoll = null;
        if (item == null || item.Rarity != ItemRarity.Rare)
        {
            return false;
        }

        List<ItemAffixProfile> candidates = CollectRareAffixCandidates(item.Slot, item.AffixRolls, true);
        if (candidates.Count == 0)
        {
            candidates = CollectRareAffixCandidates(item.Slot, item.AffixRolls, false);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        ItemAffixProfile profile = PickWeightedAffix(candidates);
        affixRoll = profile?.CreateRoll(item.Level, item.RolledPower);
        return affixRoll != null;
    }

    public static bool TryGetAffixProfile(string affixId, out ItemAffixProfile profile)
    {
        profile = null;
        if (string.IsNullOrWhiteSpace(affixId))
        {
            return false;
        }

        for (int i = 0; i < RareAffixes.Length; i++)
        {
            ItemAffixProfile candidate = RareAffixes[i];
            if (candidate != null && string.Equals(candidate.Id, affixId, StringComparison.Ordinal))
            {
                profile = candidate;
                return true;
            }
        }

        return false;
    }

    public static string FormatAffixRoll(ItemAffixRoll affixRoll)
    {
        if (affixRoll == null || affixRoll.Modifier == null)
        {
            return "empty affix";
        }

        string modifierText = FormatModifier(affixRoll.Modifier);
        if (TryGetAffixProfile(affixRoll.AffixId, out ItemAffixProfile profile))
        {
            return $"{profile.DisplayName}: {modifierText}";
        }

        return $"{affixRoll.AffixId}: {modifierText}";
    }

    private static List<ItemAffixProfile> CollectRareAffixCandidates(
        ItemSlot slot,
        ItemAffixRoll[] currentRolls,
        bool avoidCurrentAffixIds)
    {
        List<ItemAffixProfile> candidates = new List<ItemAffixProfile>(RareAffixes.Length);
        for (int i = 0; i < RareAffixes.Length; i++)
        {
            ItemAffixProfile profile = RareAffixes[i];
            if (profile == null || !profile.AppliesTo(slot))
            {
                continue;
            }

            if (avoidCurrentAffixIds && HasAffixId(currentRolls, profile.Id))
            {
                continue;
            }

            candidates.Add(profile);
        }

        return candidates;
    }

    private static ItemAffixProfile PickWeightedAffix(IReadOnlyList<ItemAffixProfile> candidates)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += Mathf.Max(1, candidates[i].Weight);
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            ItemAffixProfile candidate = candidates[i];
            roll -= Mathf.Max(1, candidate.Weight);
            if (roll < 0)
            {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private static bool HasAffixId(ItemAffixRoll[] rolls, string affixId)
    {
        if (rolls == null || string.IsNullOrWhiteSpace(affixId))
        {
            return false;
        }

        for (int i = 0; i < rolls.Length; i++)
        {
            if (rolls[i] != null && string.Equals(rolls[i].AffixId, affixId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatModifier(StatMod modifier)
    {
        if (modifier == null)
        {
            return "empty modifier";
        }

        string sign = modifier.Value >= 0f ? "+" : string.Empty;
        switch (modifier.Type)
        {
            case StatMod.StatModType.Flat:
                return $"{modifier.StatId} flat {sign}{modifier.Value:0.#}";
            case StatMod.StatModType.PercentAdd:
                return $"{modifier.StatId} additive {sign}{modifier.Value:0.#}%";
            case StatMod.StatModType.PercentMult:
                return $"{modifier.StatId} multiplier {sign}{modifier.Value:0.#}%";
            default:
                return $"{modifier.StatId} {modifier.Type} {sign}{modifier.Value:0.#}";
        }
    }

    private static int GetSlotScrapWeight(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon:
                return 4;
            case ItemSlot.Armor:
                return 3;
            case ItemSlot.Ring:
                return 2;
            default:
                return 2;
        }
    }

    private static int GetRarityScrapWeight(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Normal:
                return 1;
            case ItemRarity.Magic:
                return 2;
            case ItemRarity.Rare:
                return 4;
            default:
                return 1;
        }
    }

    private static int GetEssenceReward(ItemRarity rarity, int tier)
    {
        switch (rarity)
        {
            case ItemRarity.Magic:
                return Mathf.Max(1, Mathf.CeilToInt(tier * 0.5f));
            case ItemRarity.Rare:
                return Mathf.Max(2, tier);
            default:
                return 0;
        }
    }

    private static int GetAlterStoneReward(ItemRarity rarity, int tier)
    {
        if (rarity != ItemRarity.Rare)
        {
            return 0;
        }

        return Mathf.Max(1, tier / 4);
    }

    private static ResourceAmount[] ScaleRewards(List<ResourceAmount> rewards, float yieldMultiplier)
    {
        float safeMultiplier = Mathf.Max(1f, yieldMultiplier);
        ResourceAmount[] scaledRewards = new ResourceAmount[rewards.Count];
        for (int i = 0; i < rewards.Count; i++)
        {
            ResourceAmount reward = rewards[i];
            int scaledAmount = reward.Amount <= 0
                ? 0
                : Mathf.Max(1, Mathf.RoundToInt(reward.Amount * safeMultiplier));
            scaledRewards[i] = new ResourceAmount(reward.Resource, scaledAmount);
        }

        return scaledRewards;
    }
}

public class ItemAffixProfile
{
    public ItemAffixProfile(
        string id,
        string displayName,
        ItemSlot slot,
        StatId statId,
        StatMod.StatModType modifierType,
        float baseValue,
        float perItemLevel,
        float perRolledPower,
        int weight,
        string tags)
    {
        Id = string.IsNullOrWhiteSpace(id) ? "rare_affix" : id;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        Slot = slot;
        StatId = statId;
        ModifierType = modifierType;
        BaseValue = Mathf.Max(0f, baseValue);
        PerItemLevel = Mathf.Max(0f, perItemLevel);
        PerRolledPower = Mathf.Max(0f, perRolledPower);
        Weight = Mathf.Max(1, weight);
        Tags = string.IsNullOrWhiteSpace(tags) ? "rare" : tags;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ItemSlot Slot { get; }
    public StatId StatId { get; }
    public StatMod.StatModType ModifierType { get; }
    public float BaseValue { get; }
    public float PerItemLevel { get; }
    public float PerRolledPower { get; }
    public int Weight { get; }
    public string Tags { get; }

    public bool AppliesTo(ItemSlot slot)
    {
        return Slot == slot;
    }

    public ItemAffixRoll CreateRoll(int itemLevel, int rolledPower)
    {
        float value = BaseValue
            + Mathf.Max(1, itemLevel) * PerItemLevel
            + Mathf.Max(0, rolledPower) * PerRolledPower;
        return new ItemAffixRoll(Id, new StatMod(StatId, ModifierType, Mathf.Ceil(value)));
    }
}
