using System.Collections.Generic;
using UnityEngine;

public static class ItemEconomyModel
{
    public static ResourceAmount[] GetSalvageRewards(ItemDefinition item)
    {
        if (item == null)
        {
            return new ResourceAmount[0];
        }

        int tier = Mathf.Max(1, item.BaseTier);
        int scrap = tier * GetSlotScrapWeight(item.Slot) * GetRarityScrapWeight(item.Rarity);
        List<ResourceAmount> rewards = new List<ResourceAmount>
        {
            new ResourceAmount(ResourceId.Scrap, Mathf.Max(1, scrap))
        };

        int essence = GetEssenceReward(item.Rarity, tier);
        if (essence > 0)
        {
            rewards.Add(new ResourceAmount(ResourceId.Essence, essence));
        }

        return rewards.ToArray();
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
}
