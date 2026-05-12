using System;
using UnityEngine;

public class LootDropper : MonoBehaviour
{
    [Header("Inventory Link")]
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private bool autoFindInventory = true;

    [Header("Reward Definitions")]
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
        inventory?.RegisterDefinitions(rewardDefinitions);
    }

    private ItemDefinition SelectRewardDefinition()
    {
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
