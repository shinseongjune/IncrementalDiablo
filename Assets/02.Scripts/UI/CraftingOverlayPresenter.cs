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
    [SerializeField, Min(0.05f)] private float refreshIntervalSeconds = 0.2f;

    private bool buttonsWired;
    private bool subscribed;
    private int selectedIndex = -1;
    private float nextRefreshTime;
    private string lastMessage = "Crafting ready.";

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
        Subscribe();

        if (selectNewestOnEnable)
        {
            SelectLatest(false);
        }
        else
        {
            ClampSelection();
        }

        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
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

    public void RerollSelectedAffix()
    {
        ResolveReferences();
        ItemInstance item = GetSelectedItem();
        if (!CanRerollAffix(item, out ResourceAmount[] cost, out string failureReason))
        {
            SetMessage($"Reroll unavailable: {failureReason}.");
            return;
        }

        if (!wallet.TrySpend(cost))
        {
            SetMessage($"Reroll needs {FormatRewards(cost)}.");
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

        SetMessage($"Rerolled {item.DisplayName}: {FormatAffix(affixRoll)}.");
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

        return $"Crafting {inventory.Count}/{inventory.Capacity} / Rare items {rareCount}";
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
        return $"{walletText}\n{salvageText}\n{rerollText}";
    }

    private string BuildResultText()
    {
        ItemInstance item = GetSelectedItem();
        if (item == null)
        {
            return "Affixes: none";
        }

        ItemAffixRoll[] affixes = item.AffixRolls;
        if (affixes.Length == 0)
        {
            return "Affixes: none";
        }

        StringBuilder builder = new StringBuilder("Affixes");
        for (int i = 0; i < affixes.Length; i++)
        {
            builder.AppendLine();
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(FormatAffix(affixes[i]));
        }

        return builder.ToString();
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

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (inventory != null)
        {
            inventory.Changed += HandleInventoryChanged;
        }

        if (wallet != null)
        {
            wallet.Changed += Refresh;
        }

        if (equipmentSlots != null)
        {
            equipmentSlots.Changed += Refresh;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (inventory != null)
        {
            inventory.Changed -= HandleInventoryChanged;
        }

        if (wallet != null)
        {
            wallet.Changed -= Refresh;
        }

        if (equipmentSlots != null)
        {
            equipmentSlots.Changed -= Refresh;
        }

        subscribed = false;
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
