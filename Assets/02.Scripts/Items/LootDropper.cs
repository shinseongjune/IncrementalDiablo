using System;
using UnityEngine;

public class LootDropper : MonoBehaviour
{
    [Serializable]
    public class RewardEntry
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(0f)] private float weight = 1f;

        public ItemDefinition Definition => definition;
        public float Weight => Mathf.Max(0f, weight);
    }

    [Header("Inventory Link")]
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private bool autoFindInventory = true;

    [Header("Duplicate Conversion")]
    [SerializeField] private ItemSalvageService salvageService;
    [SerializeField] private bool autoFindSalvageService = true;
    [SerializeField] private bool autoConvertInferiorDuplicates = true;

    [Header("Reward Definitions")]
    [SerializeField] private RewardEntry[] rewardTable = new RewardEntry[0];
    [SerializeField] private ItemDefinition[] rewardDefinitions = new ItemDefinition[0];

    [Header("Rarity Pacing")]
    [SerializeField] private bool guaranteeRareWhenInventoryHasNoRare = true;
    [SerializeField, Min(0)] private int maxWeightedNonRareRewardsBeforeRare = 6;
    [SerializeField] private int weightedNonRareRewardsSinceLastRare;

    [Header("Prototype Fallback")]
    [SerializeField] private bool createPrototypeRewardWhenTableEmpty = true;
    [SerializeField] private string prototypeIdPrefix = "prototype_crypt";
    [SerializeField] private int prototypeTier = 1;
    [SerializeField] private int prototypeLevel = 1;
    [SerializeField] private int prototypeMinPower = 2;
    [SerializeField] private int prototypeMaxPower = 5;
    [SerializeField, Range(0f, 100f)] private float prototypeMagicChancePercent = 25f;
    [SerializeField, Range(0f, 100f)] private float prototypeRareChancePercent = 3f;

    [Header("Diagnostics")]
    [SerializeField] private bool logGrantedRewards = true;
    [SerializeField] private string lastDropMessage;
    [SerializeField] private LootRewardSource lastRewardSource = LootRewardSource.None;
    [SerializeField] private bool lastRewardAutoConverted;
    [SerializeField] private string lastConvertedItemName;
    [SerializeField] private ResourceAmount[] lastConversionRewards = new ResourceAmount[0];

    public event Action<ItemInstance> RewardGranted;
    public event Action<ItemInstance, ResourceAmount[]> RewardConverted;

    public string LastDropMessage => lastDropMessage;
    public LootRewardSource LastRewardSource => lastRewardSource;
    public bool LastRewardAutoConverted => lastRewardAutoConverted;
    public string LastConvertedItemName => lastConvertedItemName;
    public ResourceAmount[] LastConversionRewards => lastConversionRewards ?? new ResourceAmount[0];
    public bool HasValidWeightedRewardTable => CalculateRewardTableWeight() > 0f;
    public float RewardTableWeight => CalculateRewardTableWeight();

    private void Awake()
    {
        ResolveInventory();
        ResolveSalvageService();
    }

    private void OnValidate()
    {
        rewardTable ??= new RewardEntry[0];
        rewardDefinitions ??= new ItemDefinition[0];
        lastConversionRewards ??= new ResourceAmount[0];
        maxWeightedNonRareRewardsBeforeRare = Mathf.Max(0, maxWeightedNonRareRewardsBeforeRare);
        weightedNonRareRewardsSinceLastRare = Mathf.Max(0, weightedNonRareRewardsSinceLastRare);
        prototypeTier = Mathf.Max(1, prototypeTier);
        prototypeLevel = Mathf.Max(1, prototypeLevel);
        prototypeMinPower = Mathf.Max(0, prototypeMinPower);
        prototypeMaxPower = Mathf.Max(prototypeMinPower, prototypeMaxPower);
        prototypeMagicChancePercent = Mathf.Clamp(prototypeMagicChancePercent, 0f, 100f);
        prototypeRareChancePercent = Mathf.Clamp(prototypeRareChancePercent, 0f, 100f);
    }

    public bool TryGrantClearReward()
    {
        return TryGrantClearReward(1, out _);
    }

    public bool TryGrantClearReward(out ItemInstance item)
    {
        return TryGrantClearReward(1, out item);
    }

    public bool TryGrantClearReward(int depth, out ItemInstance item)
    {
        item = null;
        ResolveInventory();

        if (inventory == null)
        {
            SetLastDropMessage("Loot reward failed: no SimpleInventory found.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        DungeonDepthBalanceProfile balance = DungeonDepthBalanceModel.Evaluate(depth);
        ItemDefinition definition = SelectRewardDefinition(out LootRewardSource rewardSource);
        if (definition == null)
        {
            lastRewardSource = LootRewardSource.None;
            SetLastDropMessage("Loot reward failed: no ItemDefinition reward or prototype fallback is available.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        ItemInstance candidate = ItemInstance.CreateFromDefinition(
            inventory.NextItemInstanceId,
            definition,
            balance.Depth,
            balance.RewardPowerMultiplier);
        if (candidate == null)
        {
            lastRewardSource = LootRewardSource.None;
            ResetLastConversion();
            SetLastDropMessage($"Loot reward failed: could not roll {definition.DisplayName}.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        lastRewardSource = rewardSource;
        if (TryAutoConvertInferiorDuplicate(candidate, balance, out ResourceAmount[] conversionRewards))
        {
            item = candidate;
            RewardConverted?.Invoke(candidate, conversionRewards);
            return true;
        }

        if (!inventory.TryAdd(candidate))
        {
            lastRewardSource = LootRewardSource.None;
            ResetLastConversion();
            SetLastDropMessage($"Loot reward failed: inventory is full or rejected {definition.DisplayName}.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        item = candidate;
        ResetLastConversion();
        SetLastDropMessage(
            $"Loot reward granted from {FormatRewardSource(rewardSource)} at depth {balance.Depth}: " +
            $"{item.DisplayName} ({item.Rarity}, level {item.Level}, power {item.RolledPower}, reward x{balance.RewardPowerMultiplier:0.##}).");
        if (logGrantedRewards)
        {
            Debug.Log(lastDropMessage, this);
        }

        RewardGranted?.Invoke(item);
        return true;
    }

    private bool TryAutoConvertInferiorDuplicate(
        ItemInstance candidate,
        DungeonDepthBalanceProfile balance,
        out ResourceAmount[] rewards)
    {
        rewards = new ResourceAmount[0];
        if (!autoConvertInferiorDuplicates ||
            !ItemEconomyModel.TryFindAutoConversionMatch(candidate, inventory.Items, out ItemInstance retainedItem))
        {
            return false;
        }

        ResolveSalvageService();
        if (salvageService == null)
        {
            return false;
        }

        lastRewardAutoConverted = true;
        lastConvertedItemName = candidate.DisplayName;
        lastConversionRewards = ItemEconomyModel.GetSalvageRewards(candidate);
        if (!salvageService.TryConvertReward(candidate, out rewards))
        {
            ResetLastConversion();
            return false;
        }

        lastConversionRewards = rewards;
        SetLastDropMessage(
            $"Loot reward auto-converted from {FormatRewardSource(lastRewardSource)} at depth {balance.Depth}: " +
            $"{candidate.DisplayName} (level {candidate.Level}, power {candidate.RolledPower}) -> {FormatRewards(rewards)}. " +
            $"Kept level {retainedItem.Level}, power {retainedItem.RolledPower}.");
        if (logGrantedRewards)
        {
            Debug.Log(lastDropMessage, this);
        }

        return true;
    }

    private ItemDefinition SelectRewardDefinition(out LootRewardSource rewardSource)
    {
        rewardSource = LootRewardSource.None;
        ItemDefinition weightedReward = SelectWeightedRewardDefinition();
        if (weightedReward != null)
        {
            rewardSource = LootRewardSource.WeightedRewardTable;
            return weightedReward;
        }

        if (rewardDefinitions != null && rewardDefinitions.Length > 0)
        {
            int startIndex = UnityEngine.Random.Range(0, rewardDefinitions.Length);
            for (int offset = 0; offset < rewardDefinitions.Length; offset++)
            {
                int index = (startIndex + offset) % rewardDefinitions.Length;
                if (rewardDefinitions[index] != null)
                {
                    rewardSource = LootRewardSource.RewardDefinitions;
                    return rewardDefinitions[index];
                }
            }
        }

        if (!createPrototypeRewardWhenTableEmpty)
        {
            return null;
        }

        rewardSource = LootRewardSource.PrototypeFallback;
        return CreatePrototypeDefinition();
    }

    private ItemDefinition SelectWeightedRewardDefinition()
    {
        if (rewardTable == null || rewardTable.Length == 0)
        {
            return null;
        }

        if (TrySelectRareRewardForPacing(out ItemDefinition pacedRareReward))
        {
            weightedNonRareRewardsSinceLastRare = 0;
            return pacedRareReward;
        }

        float totalWeight = 0f;
        for (int i = 0; i < rewardTable.Length; i++)
        {
            RewardEntry entry = rewardTable[i];
            if (entry?.Definition != null && entry.Weight > 0f)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;
        for (int i = 0; i < rewardTable.Length; i++)
        {
            RewardEntry entry = rewardTable[i];
            if (entry?.Definition == null || entry.Weight <= 0f)
            {
                continue;
            }

            accumulated += entry.Weight;
            if (roll <= accumulated)
            {
                UpdateWeightedRarityPacing(entry.Definition);
                return entry.Definition;
            }
        }

        return null;
    }

    private bool TrySelectRareRewardForPacing(out ItemDefinition reward)
    {
        reward = null;
        bool inventoryNeedsFirstRare = guaranteeRareWhenInventoryHasNoRare && !InventoryHasRarity(ItemRarity.Rare);
        bool pityReached = maxWeightedNonRareRewardsBeforeRare > 0
            && weightedNonRareRewardsSinceLastRare >= maxWeightedNonRareRewardsBeforeRare;

        if (!inventoryNeedsFirstRare && !pityReached)
        {
            return false;
        }

        return TrySelectWeightedRewardByRarity(ItemRarity.Rare, out reward);
    }

    private bool TrySelectWeightedRewardByRarity(ItemRarity rarity, out ItemDefinition reward)
    {
        reward = null;
        float totalWeight = 0f;
        for (int i = 0; i < rewardTable.Length; i++)
        {
            RewardEntry entry = rewardTable[i];
            if (entry?.Definition != null && entry.Definition.Rarity == rarity && entry.Weight > 0f)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;
        for (int i = 0; i < rewardTable.Length; i++)
        {
            RewardEntry entry = rewardTable[i];
            if (entry?.Definition == null || entry.Definition.Rarity != rarity || entry.Weight <= 0f)
            {
                continue;
            }

            accumulated += entry.Weight;
            if (roll <= accumulated)
            {
                reward = entry.Definition;
                return true;
            }
        }

        return false;
    }

    private void UpdateWeightedRarityPacing(ItemDefinition definition)
    {
        if (definition != null && definition.Rarity == ItemRarity.Rare)
        {
            weightedNonRareRewardsSinceLastRare = 0;
            return;
        }

        weightedNonRareRewardsSinceLastRare++;
    }

    private bool InventoryHasRarity(ItemRarity rarity)
    {
        if (inventory == null)
        {
            return false;
        }

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemInstance item = inventory.Items[i];
            if (item != null && item.Rarity == rarity)
            {
                return true;
            }
        }

        return false;
    }

    private float CalculateRewardTableWeight()
    {
        if (rewardTable == null || rewardTable.Length == 0)
        {
            return 0f;
        }

        float totalWeight = 0f;
        for (int i = 0; i < rewardTable.Length; i++)
        {
            RewardEntry entry = rewardTable[i];
            if (entry?.Definition != null && entry.Weight > 0f)
            {
                totalWeight += entry.Weight;
            }
        }

        return totalWeight;
    }

    private ItemDefinition CreatePrototypeDefinition()
    {
        ItemRarity rarity = RollPrototypeRarity();
        ItemSlot slot = RollPrototypeSlot();
        int tier = Mathf.Max(1, prototypeTier);
        int minPower = Mathf.Max(0, prototypeMinPower + (tier - 1) * 2);
        int maxPower = Mathf.Max(minPower, prototypeMaxPower + (tier - 1) * 2);
        string slotName = slot.ToString().ToLowerInvariant();
        string rarityName = rarity.ToString().ToLowerInvariant();
        string idPrefix = string.IsNullOrWhiteSpace(prototypeIdPrefix) ? "prototype_crypt" : prototypeIdPrefix;

        return ItemDefinition.CreateRuntimePrototype(
            $"{idPrefix}_{rarityName}_{slotName}_t{tier}",
            $"{rarity} Prototype {slot}",
            slot,
            rarity,
            tier,
            prototypeLevel,
            minPower,
            maxPower);
    }

    private ItemRarity RollPrototypeRarity()
    {
        float rareChance = Mathf.Clamp(prototypeRareChancePercent, 0f, 100f);
        float magicChance = Mathf.Clamp(prototypeMagicChancePercent, 0f, 100f - rareChance);
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll < rareChance)
        {
            return ItemRarity.Rare;
        }

        if (roll < rareChance + magicChance)
        {
            return ItemRarity.Magic;
        }

        return ItemRarity.Normal;
    }

    private static ItemSlot RollPrototypeSlot()
    {
        ItemSlot[] slots = (ItemSlot[])Enum.GetValues(typeof(ItemSlot));
        if (slots.Length == 0)
        {
            return ItemSlot.Weapon;
        }

        return slots[UnityEngine.Random.Range(0, slots.Length)];
    }

    private void ResolveInventory()
    {
        if (inventory == null && autoFindInventory)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }
    }

    private void ResolveSalvageService()
    {
        if (salvageService == null && autoFindSalvageService)
        {
            salvageService = FindAnyObjectByType<ItemSalvageService>();
        }
    }

    private void ResetLastConversion()
    {
        lastRewardAutoConverted = false;
        lastConvertedItemName = string.Empty;
        lastConversionRewards = new ResourceAmount[0];
    }

    private void SetLastDropMessage(string message)
    {
        lastDropMessage = message;
    }

    private static string FormatRewards(ResourceAmount[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return "no materials";
        }

        return string.Join(", ", Array.ConvertAll(rewards, reward => reward.ToString()));
    }

    private static string FormatRewardSource(LootRewardSource rewardSource)
    {
        return rewardSource switch
        {
            LootRewardSource.WeightedRewardTable => "authored weighted table",
            LootRewardSource.RewardDefinitions => "legacy definition list",
            LootRewardSource.PrototypeFallback => "prototype fallback",
            _ => "no source"
        };
    }
}

public enum LootRewardSource
{
    None,
    WeightedRewardTable,
    RewardDefinitions,
    PrototypeFallback
}
