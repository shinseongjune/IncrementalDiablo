using System.Collections.Generic;
using UnityEngine;

public static class ItemEconomyModel
{
    private const float RerollGoldBaseCost = 50f;
    private const float RerollGoldGrowth = 1.35f;

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
