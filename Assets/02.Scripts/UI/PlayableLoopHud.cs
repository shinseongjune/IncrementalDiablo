using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayableLoopHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private LootDropper lootDropper;
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private ItemSalvageService salvageService;
    [SerializeField] private EquipmentSlots equipmentSlots;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private DefenseSaveManager saveManager;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Labels")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text resourcesText;
    [SerializeField] private TMP_Text dungeonText;
    [SerializeField] private TMP_Text latestItemText;
    [SerializeField] private TMP_Text heroStatsText;
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button startDungeonButton;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private Button equipLatestButton;
    [SerializeField] private Button salvageLatestButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Refresh")]
    [SerializeField] private float refreshIntervalSeconds = 0.2f;

    private bool buttonsWired;
    private bool subscribed;
    private float nextRefreshTime;
    private string lastMessage = "Ready";

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
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
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

    public void StartDungeon()
    {
        ResolveReferences();
        if (expedition == null)
        {
            SetMessage("Dungeon is not available.");
            return;
        }

        if (!expedition.StartExpedition())
        {
            SetMessage("Dungeon expedition is already running.");
            return;
        }

        SetMessage(combatRoom == null
            ? "Dungeon started, but no CombatRoom is linked. Room progress cannot advance yet."
            : "Dungeon started. Room progress is shown in the Dungeon line.");
    }

    public void ClaimPendingReward()
    {
        ResolveReferences();
        if (expedition == null)
        {
            SetMessage("Dungeon is not available.");
            return;
        }

        if (expedition.RewardPending)
        {
            SetMessage(expedition.TryGrantPendingReward()
                ? "Claimed dungeon reward."
                : "Reward claim failed. Check LootDropper and inventory.");
            return;
        }

        SetMessage(expedition.State == DungeonRunState.Cleared
            ? "Reward was already claimed automatically. Check Latest Item."
            : "No dungeon reward is ready.");
    }

    public void EquipLatest()
    {
        ResolveReferences();
        ItemInstance item = GetLatestItem();
        if (item == null)
        {
            SetMessage("No item is available to equip.");
            return;
        }

        if (inventory == null)
        {
            SetMessage("Equip failed: inventory is not available.");
            return;
        }

        if (!inventory.TryEquip(item.InstanceId, equipmentSlots, out string failureReason))
        {
            SetMessage($"Equip failed: {failureReason}.");
            return;
        }

        SetMessage($"Equipped {item.DisplayName}.");
    }

    public void SalvageLatest()
    {
        ResolveReferences();
        ItemInstance item = GetLatestItem();
        if (item == null)
        {
            SetMessage("No item is available to salvage.");
            return;
        }

        if (salvageService == null)
        {
            SetMessage("Salvage is not available.");
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

        SetMessage($"Salvaged {itemName}: {FormatRewards(rewards)}.");
    }

    public void SaveGame()
    {
        ResolveReferences();
        if (saveManager == null)
        {
            SetMessage("Save is not available.");
            return;
        }

        SetMessage(saveManager.TrySave() ? "Game saved." : "Save failed.");
    }

    public void LoadGame()
    {
        ResolveReferences();
        if (saveManager == null)
        {
            SetMessage("Load is not available.");
            return;
        }

        SetMessage(saveManager.TryLoad() ? "Game loaded." : "Load failed.");
    }

    public void Refresh()
    {
        ResolveReferences();
        SetText(summaryText, BuildSummaryText());
        SetText(resourcesText, wallet == null ? "Resources: unavailable" : wallet.FormatAll());
        SetText(dungeonText, BuildDungeonText());
        SetText(latestItemText, BuildLatestItemText());
        SetText(heroStatsText, BuildHeroStatsText());
        SetText(messageText, lastMessage);
        RefreshButtons();
    }

    private string BuildSummaryText()
    {
        if (defense == null)
        {
            return "Frontline: unavailable";
        }

        DefenseRuntimeState runtime = defense.Runtime;
        return $"Frontline Lv.{runtime.FrontlineLevel} / {runtime.State} / {runtime.Mode} / Wall {Mathf.CeilToInt(runtime.WallHealth)}/{Mathf.CeilToInt(runtime.WallMaxHealth)}";
    }

    private string BuildDungeonText()
    {
        if (expedition == null)
        {
            return "Dungeon: unavailable";
        }

        string rewardState = BuildRewardStateText();
        string result = string.IsNullOrWhiteSpace(expedition.LastResult) ? "none" : expedition.LastResult;
        string dungeonTextValue = $"Dungeon: {expedition.State} / Room {expedition.RoomsCompleted}/{expedition.TotalRooms} / {expedition.ElapsedSeconds:0.0}s / {rewardState}\nLast: {result}";

        if (combatRoom == null)
        {
            return $"{dungeonTextValue}\nRoom: unavailable";
        }

        CombatRoomResult roomResult = combatRoom.LastResult;
        string roomMessage = string.IsNullOrWhiteSpace(roomResult.message) ? "none" : roomResult.message;
        string roomProgress = combatRoom.State == CombatRoomState.Starting
            ? $"starting in {combatRoom.CountdownRemaining:0.0}s"
            : $"{combatRoom.ElapsedSeconds:0.0}s";

        return $"{dungeonTextValue}\nRoom: {combatRoom.State} / {roomProgress} / Hero {combatRoom.CurrentHeroHealth:0.#} / Enemy {combatRoom.CurrentEnemyHealth:0.#}\nRoom Last: {roomMessage}";
    }

    private string BuildRewardStateText()
    {
        if (expedition == null)
        {
            return "Reward unavailable";
        }

        if (expedition.RewardPending)
        {
            return "Reward ready";
        }

        if (expedition.State == DungeonRunState.Cleared)
        {
            if (!string.IsNullOrWhiteSpace(expedition.LastResult) &&
                expedition.LastResult.StartsWith("Reward granted:", StringComparison.OrdinalIgnoreCase))
            {
                return "Reward auto-claimed";
            }

            if (lootDropper != null && !string.IsNullOrWhiteSpace(lootDropper.LastDropMessage))
            {
                return "Reward resolved";
            }
        }

        return "No reward pending";
    }

    private string BuildLatestItemText()
    {
        ItemInstance item = GetLatestItem();
        if (item == null)
        {
            return inventory == null ? "Inventory: unavailable" : $"Inventory: {inventory.Count}/{inventory.Capacity} / Latest: none";
        }

        return $"Inventory: {inventory.Count}/{inventory.Capacity} / Latest: {item.DisplayName} / {item.Rarity} {item.Slot} Power {item.RolledPower}";
    }

    private string BuildHeroStatsText()
    {
        if (characterStats == null)
        {
            return "Hero Stats: unavailable";
        }

        return $"Hero: ATK {characterStats.GetValue(StatId.AttackDamage):0.#} / HP {characterStats.GetValue(StatId.MaxHealth):0.#} / APS {characterStats.GetValue(StatId.AttackSpeed):0.##}";
    }

    private void RefreshButtons()
    {
        ItemInstance latest = GetLatestItem();
        SetInteractable(startDungeonButton, expedition != null && !expedition.IsRunning);
        SetInteractable(claimRewardButton, expedition != null && (expedition.RewardPending || expedition.State == DungeonRunState.Cleared));
        SetInteractable(equipLatestButton, latest != null && inventory != null && equipmentSlots != null);
        SetInteractable(salvageLatestButton, latest != null && salvageService != null);
        SetInteractable(saveButton, saveManager != null);
        SetInteractable(loadButton, saveManager != null && saveManager.HasSaveFile);
    }

    private ItemInstance GetLatestItem()
    {
        if (inventory == null || inventory.Items.Count == 0)
        {
            return null;
        }

        return inventory.Items[inventory.Items.Count - 1];
    }

    private void ResolveReferences(bool force = false)
    {
        if (!autoFindReferences && !force)
        {
            return;
        }

        if (defense == null || force)
        {
            defense = FindAnyObjectByType<DefenseDirector>();
        }

        if (expedition == null || force)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }

        if (combatRoom == null || force)
        {
            combatRoom = FindAnyObjectByType<CombatRoom>();
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

        if (saveManager == null || force)
        {
            saveManager = FindAnyObjectByType<DefenseSaveManager>();
        }

        if (equipmentSlots == null || force)
        {
            equipmentSlots = FindEquipmentSlots();
        }

        if (characterStats == null || force)
        {
            characterStats = equipmentSlots == null
                ? FindAnyObjectByType<CharacterStats>()
                : equipmentSlots.GetComponent<CharacterStats>();
        }
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

    private void WireButtons()
    {
        if (buttonsWired)
        {
            return;
        }

        AddListener(startDungeonButton, StartDungeon);
        AddListener(claimRewardButton, ClaimPendingReward);
        AddListener(equipLatestButton, EquipLatest);
        AddListener(salvageLatestButton, SalvageLatest);
        AddListener(saveButton, SaveGame);
        AddListener(loadButton, LoadGame);
        buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!buttonsWired)
        {
            return;
        }

        RemoveListener(startDungeonButton, StartDungeon);
        RemoveListener(claimRewardButton, ClaimPendingReward);
        RemoveListener(equipLatestButton, EquipLatest);
        RemoveListener(salvageLatestButton, SalvageLatest);
        RemoveListener(saveButton, SaveGame);
        RemoveListener(loadButton, LoadGame);
        buttonsWired = false;
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (defense != null)
        {
            defense.Changed += Refresh;
        }

        if (expedition != null)
        {
            expedition.Changed += Refresh;
        }

        if (combatRoom != null)
        {
            combatRoom.Changed += Refresh;
        }

        if (inventory != null)
        {
            inventory.Changed += Refresh;
        }

        if (wallet != null)
        {
            wallet.Changed += Refresh;
        }

        if (equipmentSlots != null)
        {
            equipmentSlots.Changed += Refresh;
        }

        if (characterStats != null)
        {
            characterStats.Changed += Refresh;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (defense != null)
        {
            defense.Changed -= Refresh;
        }

        if (expedition != null)
        {
            expedition.Changed -= Refresh;
        }

        if (combatRoom != null)
        {
            combatRoom.Changed -= Refresh;
        }

        if (inventory != null)
        {
            inventory.Changed -= Refresh;
        }

        if (wallet != null)
        {
            wallet.Changed -= Refresh;
        }

        if (equipmentSlots != null)
        {
            equipmentSlots.Changed -= Refresh;
        }

        if (characterStats != null)
        {
            characterStats.Changed -= Refresh;
        }

        subscribed = false;
    }

    private void SetMessage(string message)
    {
        lastMessage = message;
        Refresh();
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
