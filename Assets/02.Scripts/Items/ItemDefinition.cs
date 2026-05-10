using UnityEngine;

[CreateAssetMenu(menuName = "Incremental Diablo/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private ItemSlot slot;
    [SerializeField] private ItemRarity rarity = ItemRarity.Normal;
    [SerializeField] private int baseTier = 1;
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private int baseMinPower = 1;
    [SerializeField] private int baseMaxPower = 1;
    [SerializeField] private StatMod[] modifiers = new StatMod[0];

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public ItemSlot Slot => slot;
    public ItemRarity Rarity => rarity;
    public int BaseTier => baseTier;
    public int RequiredLevel => requiredLevel;
    public int BaseMinPower => baseMinPower;
    public int BaseMaxPower => baseMaxPower;
    public StatMod[] Modifiers => modifiers;
    public ResourceAmount[] SalvageRewards => ItemEconomyModel.GetSalvageRewards(this);
    public bool CanRerollAffix => ItemEconomyModel.CanRerollAffix(this);
    public ResourceAmount[] AffixRerollCost => ItemEconomyModel.GetAffixRerollCost(this);

    public static ItemDefinition CreateRuntimePrototype(
        string id,
        string displayName,
        ItemSlot slot,
        ItemRarity rarity,
        int baseTier,
        int requiredLevel,
        int baseMinPower,
        int baseMaxPower)
    {
        ItemDefinition definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.hideFlags = HideFlags.DontSave;
        definition.id = string.IsNullOrWhiteSpace(id) ? "runtime_prototype_item" : id;
        definition.displayName = string.IsNullOrWhiteSpace(displayName) ? definition.id : displayName;
        definition.slot = slot;
        definition.rarity = rarity;
        definition.baseTier = Mathf.Max(1, baseTier);
        definition.requiredLevel = Mathf.Max(1, requiredLevel);
        definition.baseMinPower = Mathf.Max(0, baseMinPower);
        definition.baseMaxPower = Mathf.Max(definition.baseMinPower, baseMaxPower);
        definition.modifiers = new StatMod[0];
        return definition;
    }

    private void OnValidate()
    {
        baseTier = Mathf.Max(1, baseTier);
        requiredLevel = Mathf.Max(1, requiredLevel);
        baseMinPower = Mathf.Max(0, baseMinPower);
        baseMaxPower = Mathf.Max(baseMinPower, baseMaxPower);

        if (string.IsNullOrWhiteSpace(id))
        {
            id = name;
        }
    }
}
