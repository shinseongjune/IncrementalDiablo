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
