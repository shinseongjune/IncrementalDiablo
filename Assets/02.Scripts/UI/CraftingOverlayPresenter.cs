using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingOverlayPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private ItemSalvageService salvageService;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private PlayableScreenLayoutController screenLayout;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Labels")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text itemListText;
    [SerializeField] private TMP_Text selectedItemText;
    [SerializeField] private TMP_Text materialsText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button previousItemButton;
    [SerializeField] private Button nextItemButton;
    [SerializeField] private Button selectLatestButton;
    [SerializeField] private Button rerollAffixButton;
    [SerializeField] private Button salvageSelectedButton;
    [SerializeField] private Button closeOverlayButton;

    [Header("Presentation")]
    [SerializeField, Min(3)] private int maxVisibleRows = 8;
    [SerializeField] private bool selectNewestOnEnable = true;
    [SerializeField] private bool preferRerollCandidateOnEnable = true;
    [SerializeField, Min(0.05f)] private float refreshIntervalSeconds = 0.2f;

    private bool buttonsWired;
    private int selectedIndex = -1;
    private float nextRefreshTime;
    private string lastMessage = "Crafting ready.";
    private long lastRerollItemInstanceId = -1;
    private string lastRerollSummary = string.Empty;
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

        bool selectedRerollCandidate = preferRerollCandidateOnEnable && TrySelectNewestRerollCandidate(false);
        if (!selectedRerollCandidate && selectNewestOnEnable)
        {
            SelectLatest(false);
        }
        else if (!selectedRerollCandidate)
        {
            ClampSelection();
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
        maxVisibleRows = Mathf.Max(3, maxVisibleRows);
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

    public void SelectPrevious()
    {
        ResolveReferences();
        int count = GetItemCount();
        if (count == 0)
        {
            SetMessage("Inventory is empty.");
            return;
        }

        selectedIndex = selectedIndex <= 0 ? count - 1 : selectedIndex - 1;
        SetMessage($"Selected {GetSelectedItem()?.DisplayName ?? "item"}.");
    }

    public void SelectNext()
    {
        ResolveReferences();
        int count = GetItemCount();
        if (count == 0)
        {
            SetMessage("Inventory is empty.");
            return;
        }

        selectedIndex = selectedIndex >= count - 1 ? 0 : selectedIndex + 1;
        SetMessage($"Selected {GetSelectedItem()?.DisplayName ?? "item"}.");
    }

    public void SelectLatest()
    {
        SelectLatest(true);
    }

    public void SelectRerollCandidate()
    {
        ResolveReferences();
        if (TrySelectNewestRerollCandidate(true))
        {
            return;
        }

        SetMessage("No rerollable Rare item is available yet.");
    }

    public void RerollSelectedAffix()
    {
        ResolveReferences();
        ItemInstance item = GetSelectedItem();
        if (!CanRerollAffix(item, out ResourceAmount[] cost, out string failureReason))
        {
            SetMessage($"Reroll unavailable: {failureReason}.");
            return;
        }

        string beforeAffixText = FormatAffixSummary(item.AffixRolls);
        string costText = FormatRewards(cost);
        if (!wallet.TrySpend(cost))
        {
            SetMessage($"Reroll needs {costText}.");
            return;
        }

        if (!item.TryApplyPrototypeAffixReroll(out ItemAffixRoll affixRoll))
        {
            wallet.Add(cost);
            SetMessage("Reroll failed before changing the item.");
            return;
        }

        inventory?.NotifyItemsChanged();
        if (item.Equipped)
        {
            equipmentSlots?.RefreshEquippedModifiers();
        }

        string afterAffixText = FormatAffix(affixRoll);
        lastRerollItemInstanceId = item.InstanceId;
        lastRerollSummary = $"Last reroll spent {costText}: {beforeAffixText} -> {afterAffixText}";
        SetMessage($"Rerolled {item.DisplayName}: spent {costText}.");
    }

    public void SalvageSelected()
    {
        ResolveReferences();
        ItemInstance item = GetSelectedItem();
        if (item == null)
        {
            SetMessage("No inventory item is selected.");
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

        ClampSelection();
        SetMessage($"Salvaged {itemName}: {FormatRewards(rewards)}.");
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
        ClampSelection();

        SetText(headerText, BuildHeaderText());
        SetText(itemListText, BuildItemListText());
        SetText(selectedItemText, BuildSelectedItemText());
        SetText(materialsText, BuildMaterialsText());
        SetText(resultText, BuildResultText());
        SetText(messageText, lastMessage);
        RefreshButtons();
    }

    private void SelectLatest(bool updateMessage)
    {
        int count = GetItemCount();
        selectedIndex = count == 0 ? -1 : count - 1;

        if (updateMessage)
        {
            SetMessage(count == 0 ? "Inventory is empty." : $"Selected latest item: {GetSelectedItem()?.DisplayName}.");
        }
    }

    private string BuildHeaderText()
    {
        if (inventory == null)
        {
            return "Crafting: unavailable";
        }

        int rareCount = 0;
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            if (inventory.Items[i] != null && inventory.Items[i].Rarity == ItemRarity.Rare)
            {
                rareCount++;
            }
        }

        int rerollCandidateCount = CountRerollCandidates();
        return $"Crafting {inventory.Count}/{inventory.Capacity} / Rare items {rareCount} / Reroll ready {rerollCandidateCount}";
    }

    private string BuildItemListText()
    {
        if (inventory == null)
        {
            return "Connect SimpleInventory to show craftable items.";
        }

        int count = inventory.Items.Count;
        if (count == 0)
        {
            return "No items yet. Clear a dungeon room to create a reward.";
        }

        int visibleRows = Mathf.Clamp(maxVisibleRows, 3, Mathf.Max(3, count));
        int start = Mathf.Clamp(selectedIndex - visibleRows / 2, 0, Mathf.Max(0, count - visibleRows));
        int end = Mathf.Min(count, start + visibleRows);

        StringBuilder builder = new StringBuilder();
        for (int i = start; i < end; i++)
        {
            ItemInstance item = inventory.Items[i];
            if (item == null)
            {
                continue;
            }

            string selectedMarker = i == selectedIndex ? ">" : " ";
            string equippedMarker = item.Equipped ? "E" : " ";
            string rerollMarker = IsRerollCandidate(item) ? "R" : " ";
            builder.Append(selectedMarker);
            builder.Append(equippedMarker);
            builder.Append(rerollMarker);
            builder.Append(' ');
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(item.DisplayName);
            builder.Append(" [");
            builder.Append(item.Rarity);
            builder.Append(' ');
            builder.Append(item.Slot);
            builder.Append("] P");
            builder.Append(item.RolledPower);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildSelectedItemText()
    {
        ItemInstance item = GetSelectedItem();
        if (item == null)
        {
            return inventory == null ? "Selected: unavailable" : "Selected: none";
        }

        string definitionText = string.IsNullOrWhiteSpace(item.DefinitionId) ? "runtime snapshot" : item.DefinitionId;
        string equippedText = item.Equipped ? "Equipped" : "Stored";
        return $"Selected #{item.InstanceId}\n{item.DisplayName}\n{item.Rarity} {item.Slot} Lv.{item.Level} / Power {item.RolledPower}\n{equippedText} / Durability {item.Durability}%\nDefinition: {definitionText}";
    }

    private string BuildMaterialsText()
    {
        ItemInstance item = GetSelectedItem();
        string walletText = wallet == null ? "Wallet: unavailable" : $"Wallet: {wallet.FormatAll()}";
        if (item == null)
        {
            return walletText;
        }

        string salvageText = $"Salvage returns: {FormatRewards(GetSalvagePreview(item))}";
        string rerollText = IsRerollCandidate(item)
            ? $"Rare reroll cost: {FormatRewards(item.Definition.AffixRerollCost)}"
            : "Rare reroll cost: unavailable for this item.";
        return $"{walletText}\n{salvageText}\n{rerollText}\n{BuildRerollGuidanceText(item)}";
    }

    private string BuildResultText()
    {
        ItemInstance item = GetSelectedItem();
        if (item == null)
        {
            return "Affixes: none";
        }

        StringBuilder builder = new StringBuilder();
        ItemAffixRoll[] affixes = item.AffixRolls;
        if (affixes.Length == 0)
        {
            builder.Append("Affixes: none");
            AppendRerollStatusAndSummary(builder, item);
            return builder.ToString();
        }

        builder.Append("Affixes");
        for (int i = 0; i < affixes.Length; i++)
        {
            builder.AppendLine();
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(FormatAffix(affixes[i]));
        }

        AppendRerollStatusAndSummary(builder, item);
        return builder.ToString();
    }

    private void AppendRerollStatusAndSummary(StringBuilder builder, ItemInstance item)
    {
        builder.AppendLine();
        builder.Append(BuildRerollStatusText(item));

        string lastRerollText = BuildLastRerollText(item);
        if (!string.IsNullOrWhiteSpace(lastRerollText))
        {
            builder.AppendLine();
            builder.Append(lastRerollText);
        }
    }

    private void RefreshButtons()
    {
        ItemInstance selectedItem = GetSelectedItem();
        int count = GetItemCount();
        SetInteractable(previousItemButton, count > 1);
        SetInteractable(nextItemButton, count > 1);
        SetInteractable(selectLatestButton, count > 0 && selectedIndex != count - 1);
        SetInteractable(rerollAffixButton, CanRerollAffix(selectedItem, out _, out _));
        SetInteractable(salvageSelectedButton, selectedItem != null && salvageService != null);
        SetInteractable(closeOverlayButton, screenLayout != null);
    }

    private bool TrySelectNewestRerollCandidate(bool updateMessage)
    {
        if (inventory == null || inventory.Items.Count == 0)
        {
            if (updateMessage)
            {
                SetMessage("Inventory is empty.");
            }

            return false;
        }

        for (int i = inventory.Items.Count - 1; i >= 0; i--)
        {
            ItemInstance item = inventory.Items[i];
            if (!IsRerollCandidate(item))
            {
                continue;
            }

            selectedIndex = i;
            if (updateMessage)
            {
                SetMessage($"Selected reroll candidate: {item.DisplayName}.");
            }

            return true;
        }

        return false;
    }

    private bool CanRerollAffix(ItemInstance item, out ResourceAmount[] cost, out string failureReason)
    {
        cost = new ResourceAmount[0];
        failureReason = string.Empty;

        if (item == null)
        {
            failureReason = "no item selected";
            return false;
        }

        if (!IsRerollCandidate(item))
        {
            failureReason = item.Rarity == ItemRarity.Rare
                ? "rare item definition is not resolved"
                : "only Rare items can be rerolled";
            return false;
        }

        if (wallet == null)
        {
            failureReason = "CurrencyWallet is not available";
            return false;
        }

        cost = item.Definition.AffixRerollCost;
        if (!wallet.CanSpend(cost))
        {
            failureReason = $"needs {FormatRewards(cost)}";
            return false;
        }

        return true;
    }

    private static bool IsRerollCandidate(ItemInstance item)
    {
        return item != null && item.Definition != null && item.Definition.CanRerollAffix;
    }

    private int CountRerollCandidates()
    {
        if (inventory == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            if (IsRerollCandidate(inventory.Items[i]))
            {
                count++;
            }
        }

        return count;
    }

    private string BuildRerollStatusText(ItemInstance item)
    {
        return CanRerollAffix(item, out _, out string failureReason)
            ? "Reroll status: ready."
            : $"Reroll status: {failureReason}.";
    }

    private string BuildRerollGuidanceText(ItemInstance item)
    {
        if (item == null)
        {
            return "Next: clear a dungeon to get a craftable item.";
        }

        if (!IsRerollCandidate(item))
        {
            return item.Rarity == ItemRarity.Rare
                ? "Next: reconnect the Rare item definition before rerolling."
                : "Next: select a Rare item or salvage this item for materials.";
        }

        ResourceAmount[] cost = item.Definition.AffixRerollCost;
        if (wallet == null)
        {
            return "Next: connect CurrencyWallet to spend reroll materials.";
        }

        if (wallet.CanSpend(cost))
        {
            return "Next: press Reroll Affix to spend materials and change the Rare affix.";
        }

        return HasRerollCost(ResourceId.AlterStone, cost) && wallet.GetAmount(ResourceId.AlterStone) <= 0
            ? "Next: salvage one spare Rare for AlterStone, then reroll the next Rare."
            : $"Next: gather reroll materials: {FormatRewards(cost)}.";
    }

    private string BuildLastRerollText(ItemInstance item)
    {
        if (item == null || item.InstanceId != lastRerollItemInstanceId || string.IsNullOrWhiteSpace(lastRerollSummary))
        {
            return string.Empty;
        }

        return lastRerollSummary;
    }

    private static bool HasRerollCost(ResourceId resource, ResourceAmount[] cost)
    {
        if (cost == null)
        {
            return false;
        }

        for (int i = 0; i < cost.Length; i++)
        {
            if (cost[i].Resource == resource && cost[i].Amount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ClampSelection()
    {
        int count = GetItemCount();
        if (count == 0)
        {
            selectedIndex = -1;
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex < 0 ? count - 1 : selectedIndex, 0, count - 1);
    }

    private int GetItemCount()
    {
        return inventory == null ? 0 : inventory.Items.Count;
    }

    private ItemInstance GetSelectedItem()
    {
        if (inventory == null)
        {
            return null;
        }

        int count = inventory.Items.Count;
        if (count == 0)
        {
            return null;
        }

        if (selectedIndex < 0 || selectedIndex >= count)
        {
            return null;
        }

        return inventory.Items[selectedIndex];
    }

    private static ResourceAmount[] GetSalvagePreview(ItemInstance item)
    {
        if (item == null)
        {
            return new ResourceAmount[0];
        }

        return item.Definition == null
            ? ItemEconomyModel.GetSalvageRewards(item.Slot, item.Rarity, item.Level)
            : ItemEconomyModel.GetSalvageRewards(item.Definition);
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

        AddListener(previousItemButton, SelectPrevious);
        AddListener(nextItemButton, SelectNext);
        AddListener(selectLatestButton, SelectLatest);
        AddListener(rerollAffixButton, RerollSelectedAffix);
        AddListener(salvageSelectedButton, SalvageSelected);
        AddListener(closeOverlayButton, CloseOverlay);
        buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!buttonsWired)
        {
            return;
        }

        RemoveListener(previousItemButton, SelectPrevious);
        RemoveListener(nextItemButton, SelectNext);
        RemoveListener(selectLatestButton, SelectLatest);
        RemoveListener(rerollAffixButton, RerollSelectedAffix);
        RemoveListener(salvageSelectedButton, SalvageSelected);
        RemoveListener(closeOverlayButton, CloseOverlay);
        buttonsWired = false;
    }

    private void SynchronizeSubscriptions()
    {
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

    private void HandleInventoryChanged()
    {
        ClampSelection();
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

    private static string FormatAffix(ItemAffixRoll affixRoll)
    {
        if (affixRoll == null || affixRoll.Modifier == null)
        {
            return "empty affix";
        }

        StatMod modifier = affixRoll.Modifier;
        return $"{affixRoll.AffixId}: {modifier.StatId} {modifier.Type} {modifier.Value:0.#}";
    }

    private static string FormatAffixSummary(ItemAffixRoll[] affixes)
    {
        if (affixes == null || affixes.Length == 0)
        {
            return "none";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < affixes.Length; i++)
        {
            if (i > 0)
            {
                builder.Append("; ");
            }

            builder.Append(FormatAffix(affixes[i]));
        }

        return builder.ToString();
    }

    private static string FormatRewards(ResourceAmount[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return "no materials";
        }

        return string.Join(", ", Array.ConvertAll(rewards, reward => reward.ToString()));
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
