using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardOverlayPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private LootDropper lootDropper;
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private ItemSalvageService salvageService;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private PlayableScreenLayoutController screenLayout;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Labels")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text itemDetailText;
    [SerializeField] private TMP_Text materialsText;
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private Button openInventoryButton;
    [SerializeField] private Button equipRewardButton;
    [SerializeField] private Button salvageRewardButton;
    [SerializeField] private Button closeOverlayButton;

    [Header("Presentation")]
    [SerializeField] private bool selectLatestItemOnEnable = true;
    [SerializeField, Min(0.05f)] private float refreshIntervalSeconds = 0.2f;

    private bool buttonsWired;
    private float nextRefreshTime;
    private long rewardItemInstanceId;
    private bool rewardItemConsumed;
    private string lastMessage = "Reward ready.";
    private ExpeditionDirector subscribedExpedition;
    private LootDropper subscribedLootDropper;
    private SimpleInventory subscribedInventory;
    private CurrencyWallet subscribedWallet;
    private EquipmentSlots subscribedEquipmentSlots;

    private void Reset()
    {
        ResolveReferences(true);
    }

    private void Awake()
    {
        ResolveReferences();
        WireButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        WireButtons();
        SynchronizeSubscriptions();

        if (selectLatestItemOnEnable)
        {
            SelectLatestInventoryItem();
        }

        Refresh();
    }

    private void OnDisable()
    {
        ClearSubscriptions();
        UnwireButtons();
    }

    private void OnValidate()
    {
        refreshIntervalSeconds = Mathf.Max(0.05f, refreshIntervalSeconds);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
        Refresh();
    }

    public void ClaimPendingReward()
    {
        ResolveReferences();
        if (expedition == null)
        {
            SetMessage("Claim failed: dungeon is not available.");
            return;
        }

        if (!expedition.RewardPending)
        {
            SelectLatestInventoryItem();
            SetMessage("No pending reward. Showing latest inventory item.");
            return;
        }

        if (!expedition.TryGrantPendingReward())
        {
            SetMessage(string.IsNullOrWhiteSpace(expedition.LastResult)
                ? "Reward claim failed."
                : expedition.LastResult);
            return;
        }

        SelectLatestInventoryItem();
        ItemInstance item = GetRewardItem();
        SetMessage(item == null ? "Reward claimed." : $"Reward claimed: {item.DisplayName}.");
    }

    public void OpenInventoryOverlay()
    {
        ResolveReferences();
        if (screenLayout == null)
        {
            SetMessage("Screen layout is not available.");
            return;
        }

        if (!screenLayout.TryOpenInventoryOverlay())
        {
            SetMessage(screenLayout.LastLayoutMessage);
            return;
        }

        SetMessage(screenLayout.LastLayoutMessage);
    }

    public void EquipReward()
    {
        ResolveReferences();
        ItemInstance item = GetRewardItem();
        if (item == null)
        {
            SetMessage("No reward item is available to equip.");
            return;
        }

        if (inventory == null)
        {
            SetMessage("Equip failed: inventory is not available.");
            return;
        }

        if (equipmentSlots == null)
        {
            SetMessage("Equip failed: EquipmentSlots is not available.");
            return;
        }

        if (!inventory.TryEquip(item.InstanceId, equipmentSlots, out string failureReason))
        {
            SetMessage($"Equip failed: {failureReason}.");
            return;
        }

        rewardItemInstanceId = item.InstanceId;
        rewardItemConsumed = false;
        SetMessage($"Equipped reward: {item.DisplayName}.");
    }

    public void SalvageReward()
    {
        ResolveReferences();
        ItemInstance item = GetRewardItem();
        if (item == null)
        {
            SetMessage("No reward item is available to salvage.");
            return;
        }

        if (salvageService == null)
        {
            SetMessage("Salvage failed: ItemSalvageService is not available.");
            return;
        }

        string itemName = item.DisplayName;
        ItemSlot slot = item.Slot;
        bool wasEquipped = item.Equipped;
        if (!salvageService.TrySalvage(item, out ResourceAmount[] rewards))
        {
            SetMessage($"Salvage failed: {itemName}.");
            return;
        }

        if (wasEquipped)
        {
            equipmentSlots?.Unequip(slot);
        }

        rewardItemInstanceId = 0;
        rewardItemConsumed = true;
        SetMessage($"Salvaged reward {itemName}: {FormatRewards(rewards)}.");
    }

    public void CloseOverlay()
    {
        ResolveReferences();
        if (screenLayout == null)
        {
            SetMessage("Screen layout is not available.");
            return;
        }

        screenLayout.CloseOverlay();
        SetMessage(screenLayout.LastLayoutMessage);
    }

    public void Refresh()
    {
        ResolveReferences();
        SynchronizeSubscriptions();
        ItemInstance item = GetRewardItem();

        SetText(headerText, BuildHeaderText(item));
        SetText(rewardText, BuildRewardText(item));
        SetText(itemDetailText, BuildItemDetailText(item));
        SetText(materialsText, BuildMaterialsText(item));
        SetText(messageText, lastMessage);
        RefreshButtons(item);
    }

    private string BuildHeaderText(ItemInstance item)
    {
        if (expedition == null)
        {
            return "Reward: unavailable";
        }

        if (expedition.RewardPending)
        {
            return $"Reward ready / Dungeon {expedition.State}";
        }

        if (item != null)
        {
            return $"Latest reward / {item.Rarity} {item.Slot}";
        }

        return $"Reward: none / Dungeon {expedition.State}";
    }

    private string BuildRewardText(ItemInstance item)
    {
        string sourceText = lootDropper == null
            ? "Loot source: unavailable"
            : $"Loot source: {FormatLootRewardSource(lootDropper.LastRewardSource)}";

        if (expedition != null && expedition.RewardPending)
        {
            return $"A dungeon reward is waiting.\n{sourceText}\nClaim it to reveal the item.";
        }

        if (item != null)
        {
            return $"Reward item\n{item.DisplayName}\n{sourceText}";
        }

        if (lootDropper != null && !string.IsNullOrWhiteSpace(lootDropper.LastDropMessage))
        {
            return $"{lootDropper.LastDropMessage}\n{sourceText}";
        }

        if (expedition != null && !string.IsNullOrWhiteSpace(expedition.LastResult))
        {
            return expedition.LastResult;
        }

        return "Clear a dungeon room to reveal a reward here.";
    }

    private static string BuildItemDetailText(ItemInstance item)
    {
        if (item == null)
        {
            return "Item: none";
        }

        string definitionText = item.IsDefinitionResolved
            ? item.DefinitionId
            : $"UNRESOLVED {item.DefinitionId}";
        string equippedText = item.Equipped ? "Equipped" : "Stored";
        return $"{item.DisplayName}\n{item.Rarity} {item.Slot} Lv.{item.Level} / Power {item.RolledPower}\n{equippedText} / Durability {item.Durability}% / Affixes {item.AffixRolls.Length}\nDefinition: {definitionText}";
    }

    private string BuildMaterialsText(ItemInstance item)
    {
        string walletText = wallet == null ? "Wallet: unavailable" : $"Wallet: {wallet.FormatAll()}";
        if (item == null)
        {
            return walletText;
        }

        string salvageText = item.IsDefinitionResolved
            ? $"Salvage returns: {FormatRewards(GetSalvagePreview(item))}"
            : "Salvage unavailable: item definition is unresolved.";
        string rerollText = item.Definition != null && item.Definition.CanRerollAffix
            ? $"Rare reroll cost: {FormatRewards(item.Definition.AffixRerollCost)}"
            : "Rare reroll cost: unavailable for this item.";
        return $"{walletText}\n{salvageText}\n{rerollText}";
    }

    private void RefreshButtons(ItemInstance item)
    {
        SetInteractable(claimRewardButton, expedition != null && expedition.RewardPending);
        SetInteractable(openInventoryButton, screenLayout != null && screenLayout.CanOpenInventoryOverlay);
        SetInteractable(equipRewardButton, item != null && item.IsDefinitionResolved && !item.Equipped && inventory != null && equipmentSlots != null);
        SetInteractable(salvageRewardButton, item != null && item.IsDefinitionResolved && salvageService != null);
        SetInteractable(closeOverlayButton, screenLayout != null);
    }

    private ItemInstance GetRewardItem()
    {
        if (inventory == null)
        {
            return null;
        }

        if (rewardItemInstanceId > 0 && inventory.TryGet(rewardItemInstanceId, out ItemInstance selectedItem))
        {
            return selectedItem;
        }

        if (rewardItemConsumed)
        {
            return null;
        }

        if (inventory.Items.Count == 0)
        {
            return null;
        }

        ItemInstance latestItem = inventory.Items[inventory.Items.Count - 1];
        rewardItemInstanceId = latestItem == null ? 0 : latestItem.InstanceId;
        return latestItem;
    }

    private void SelectLatestInventoryItem()
    {
        if (inventory == null || inventory.Items.Count == 0)
        {
            rewardItemInstanceId = 0;
            rewardItemConsumed = false;
            return;
        }

        ItemInstance latestItem = inventory.Items[inventory.Items.Count - 1];
        rewardItemInstanceId = latestItem == null ? 0 : latestItem.InstanceId;
        rewardItemConsumed = false;
    }

    private static ResourceAmount[] GetSalvagePreview(ItemInstance item)
    {
        if (item == null)
        {
            return new ResourceAmount[0];
        }

        return ItemEconomyModel.GetSalvageRewards(item);
    }

    private void SetMessage(string message)
    {
        lastMessage = string.IsNullOrWhiteSpace(message) ? "Ready." : message;
        Refresh();
    }

    private void ResolveReferences(bool force = false)
    {
        if (!autoFindReferences && !force)
        {
            return;
        }

        if (expedition == null || force)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }

        if (lootDropper == null || force)
        {
            lootDropper = FindAnyObjectByType<LootDropper>();
        }

        if (inventory == null || force)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }

        if (salvageService == null || force)
        {
            salvageService = FindAnyObjectByType<ItemSalvageService>();
        }

        if (wallet == null || force)
        {
            wallet = FindAnyObjectByType<CurrencyWallet>();
        }

        if (screenLayout == null || force)
        {
            screenLayout = FindAnyObjectByType<PlayableScreenLayoutController>();
        }

        if (equipmentSlots == null || force)
        {
            equipmentSlots = FindEquipmentSlots();
        }
    }

    private void WireButtons()
    {
        if (buttonsWired)
        {
            return;
        }

        AddListener(claimRewardButton, ClaimPendingReward);
        AddListener(openInventoryButton, OpenInventoryOverlay);
        AddListener(equipRewardButton, EquipReward);
        AddListener(salvageRewardButton, SalvageReward);
        AddListener(closeOverlayButton, CloseOverlay);
        buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!buttonsWired)
        {
            return;
        }

        RemoveListener(claimRewardButton, ClaimPendingReward);
        RemoveListener(openInventoryButton, OpenInventoryOverlay);
        RemoveListener(equipRewardButton, EquipReward);
        RemoveListener(salvageRewardButton, SalvageReward);
        RemoveListener(closeOverlayButton, CloseOverlay);
        buttonsWired = false;
    }

    private void SynchronizeSubscriptions()
    {
        if (subscribedExpedition != expedition)
        {
            if (subscribedExpedition != null)
            {
                subscribedExpedition.Changed -= Refresh;
            }

            subscribedExpedition = expedition;
            if (subscribedExpedition != null)
            {
                subscribedExpedition.Changed += Refresh;
            }
        }

        if (subscribedLootDropper != lootDropper)
        {
            if (subscribedLootDropper != null)
            {
                subscribedLootDropper.RewardGranted -= HandleRewardGranted;
            }

            subscribedLootDropper = lootDropper;
            if (subscribedLootDropper != null)
            {
                subscribedLootDropper.RewardGranted += HandleRewardGranted;
            }
        }

        if (subscribedInventory != inventory)
        {
            if (subscribedInventory != null)
            {
                subscribedInventory.Changed -= HandleInventoryChanged;
            }

            subscribedInventory = inventory;
            if (subscribedInventory != null)
            {
                subscribedInventory.Changed += HandleInventoryChanged;
            }
        }

        if (subscribedWallet != wallet)
        {
            if (subscribedWallet != null)
            {
                subscribedWallet.Changed -= Refresh;
            }

            subscribedWallet = wallet;
            if (subscribedWallet != null)
            {
                subscribedWallet.Changed += Refresh;
            }
        }

        if (subscribedEquipmentSlots != equipmentSlots)
        {
            if (subscribedEquipmentSlots != null)
            {
                subscribedEquipmentSlots.Changed -= Refresh;
            }

            subscribedEquipmentSlots = equipmentSlots;
            if (subscribedEquipmentSlots != null)
            {
                subscribedEquipmentSlots.Changed += Refresh;
            }
        }
    }

    private void ClearSubscriptions()
    {
        if (subscribedExpedition != null)
        {
            subscribedExpedition.Changed -= Refresh;
            subscribedExpedition = null;
        }

        if (subscribedLootDropper != null)
        {
            subscribedLootDropper.RewardGranted -= HandleRewardGranted;
            subscribedLootDropper = null;
        }

        if (subscribedInventory != null)
        {
            subscribedInventory.Changed -= HandleInventoryChanged;
            subscribedInventory = null;
        }

        if (subscribedWallet != null)
        {
            subscribedWallet.Changed -= Refresh;
            subscribedWallet = null;
        }

        if (subscribedEquipmentSlots != null)
        {
            subscribedEquipmentSlots.Changed -= Refresh;
            subscribedEquipmentSlots = null;
        }
    }

    private void HandleRewardGranted(ItemInstance item)
    {
        rewardItemInstanceId = item == null ? 0 : item.InstanceId;
        rewardItemConsumed = false;
        lastMessage = item == null ? "Reward granted." : $"Reward granted: {item.DisplayName}.";
        Refresh();
    }

    private void HandleInventoryChanged()
    {
        if (rewardItemInstanceId > 0 && inventory != null && !inventory.Contains(rewardItemInstanceId))
        {
            rewardItemInstanceId = 0;
            rewardItemConsumed = true;
        }

        Refresh();
    }

    private static EquipmentSlots FindEquipmentSlots()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null && player.TryGetComponent(out EquipmentSlots playerEquipmentSlots))
        {
            return playerEquipmentSlots;
        }

        return FindAnyObjectByType<EquipmentSlots>();
    }

    private static string FormatRewards(ResourceAmount[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return "no materials";
        }

        return string.Join(", ", Array.ConvertAll(rewards, reward => reward.ToString()));
    }

    private static string FormatLootRewardSource(LootRewardSource rewardSource)
    {
        return rewardSource switch
        {
            LootRewardSource.WeightedRewardTable => "authored table",
            LootRewardSource.RewardDefinitions => "legacy list",
            LootRewardSource.PrototypeFallback => "prototype fallback",
            _ => "not rolled yet"
        };
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
}
