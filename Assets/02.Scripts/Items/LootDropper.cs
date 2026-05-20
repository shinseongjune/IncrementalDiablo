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

    [Header("Reward Definitions")]
    [SerializeField] private RewardEntry[] rewardTable = new RewardEntry[0];
    [SerializeField] private ItemDefinition[] rewardDefinitions = new ItemDefinition[0];

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

    public event Action<ItemInstance> RewardGranted;

    public string LastDropMessage => lastDropMessage;

    private void Awake()
    {
        ResolveInventory();
        RegisterRewardDefinitions();
    }

    private void OnValidate()
    {
        rewardTable ??= new RewardEntry[0];
        rewardDefinitions ??= new ItemDefinition[0];
        prototypeTier = Mathf.Max(1, prototypeTier);
        prototypeLevel = Mathf.Max(1, prototypeLevel);
        prototypeMinPower = Mathf.Max(0, prototypeMinPower);
        prototypeMaxPower = Mathf.Max(prototypeMinPower, prototypeMaxPower);
        prototypeMagicChancePercent = Mathf.Clamp(prototypeMagicChancePercent, 0f, 100f);
        prototypeRareChancePercent = Mathf.Clamp(prototypeRareChancePercent, 0f, 100f);
    }

    public bool TryGrantClearReward()
    {
        return TryGrantClearReward(out _);
    }

    public bool TryGrantClearReward(out ItemInstance item)
    {
        item = null;
        ResolveInventory();

        if (inventory == null)
        {
            SetLastDropMessage("Loot reward failed: no SimpleInventory found.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        ItemDefinition definition = SelectRewardDefinition();
        if (definition == null)
        {
            SetLastDropMessage("Loot reward failed: no ItemDefinition reward or prototype fallback is available.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        if (!inventory.TryAdd(definition, out item))
        {
            SetLastDropMessage($"Loot reward failed: inventory is full or rejected {definition.DisplayName}.");
            Debug.LogWarning(lastDropMessage, this);
            return false;
        }

        SetLastDropMessage($"Loot reward granted: {item.DisplayName} ({item.Rarity}, power {item.RolledPower}).");
        if (logGrantedRewards)
        {
            Debug.Log(lastDropMessage, this);
        }

        RewardGranted?.Invoke(item);
        return true;
    }

    private void RegisterRewardDefinitions()
    {
        if (rewardTable != null)
        {
            for (int i = 0; i < rewardTable.Length; i++)
            {
                inventory?.RegisterDefinition(rewardTable[i]?.Definition);
            }
        }

        inventory?.RegisterDefinitions(rewardDefinitions);
    }

    private ItemDefinition SelectRewardDefinition()
    {
        ItemDefinition weightedReward = SelectWeightedRewardDefinition();
        if (weightedReward != null)
        {
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
                    return rewardDefinitions[index];
                }
            }
        }

        return createPrototypeRewardWhenTableEmpty ? CreatePrototypeDefinition() : null;
    }

    private ItemDefinition SelectWeightedRewardDefinition()
    {
        if (rewardTable == null || rewardTable.Length == 0)
        {
            return null;
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
                return entry.Definition;
            }
        }

        return null;
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

    private void SetLastDropMessage(string message)
    {
        lastDropMessage = message;
    }
}
