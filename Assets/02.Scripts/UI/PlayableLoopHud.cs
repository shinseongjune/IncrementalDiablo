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
    [SerializeField] private Health heroHealth;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private DefenseSaveManager saveManager;
    [SerializeField] private GroundDefenseCombatPresenter groundCombatPresenter;
    [SerializeField] private PlayableScreenLayoutController screenLayout;
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool syncScreenFocusWithDungeon = true;

    [Header("Labels")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text resourcesText;
    [SerializeField] private TMP_Text dungeonText;
    [SerializeField] private TMP_Text latestItemText;
    [SerializeField] private TMP_Text heroStatsText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text actionHintText;

    [Header("Buttons")]
    [SerializeField] private Button startDefenseButton;
    [SerializeField] private Button repairWallButton;
    [SerializeField] private Button toggleFrontlineModeButton;
    [SerializeField] private Button upgradeWallButton;
    [SerializeField] private Button upgradeTowerButton;
    [SerializeField] private Button upgradeDefenderButton;
    [SerializeField] private Button startDungeonButton;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private Button equipLatestButton;
    [SerializeField] private Button salvageLatestButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button openInventoryOverlayButton;
    [SerializeField] private Button openCraftingOverlayButton;
    [SerializeField] private Button openRewardOverlayButton;
    [SerializeField] private Button closeOverlayButton;

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

        if (syncScreenFocusWithDungeon)
        {
            screenLayout?.ShowDungeonFocus();
        }

        SetMessage(combatRoom == null
            ? "Dungeon started, but no CombatRoom is linked. Room progress cannot advance yet."
            : "Dungeon started. Room progress is shown in the Dungeon line.");
    }

    public void StartDefense()
    {
        ResolveReferences();
        if (defense == null)
        {
            SetMessage("Frontline is not available.");
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        if (runtime.IsRunning)
        {
            SetMessage($"Frontline is already running in {runtime.Mode} mode.");
            return;
        }

        if (runtime.WallHealth <= 0f)
        {
            SetMessage("Repair the wall before restarting the frontline.");
            return;
        }

        defense.StartDefense();
        SetMessage($"Frontline started in {defense.Runtime.Mode} mode.");
    }

    public void RepairWall()
    {
        ResolveReferences();
        if (!TryGetDefenseUpgradeContext(out DefenseUpgradeModel upgrades, out CurrencyWallet defenseWallet, out string failureReason))
        {
            SetMessage($"Repair failed: {failureReason}.");
            return;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        float missingHealth = Mathf.Max(0f, runtime.WallMaxHealth - runtime.WallHealth);
        if (missingHealth <= 0f)
        {
            SetMessage("Wall is already fully repaired.");
            return;
        }

        ResourceAmount[] cost = upgrades.GetRepairCost(missingHealth);
        if (!defenseWallet.CanSpend(cost))
        {
            SetMessage($"Repair needs {FormatRewards(cost)}.");
            return;
        }

        SetMessage(defense.TryRepairWall()
            ? "Wall repaired. Restart or push the frontline."
            : "Repair failed.");
    }

    public void ToggleFrontlineMode()
    {
        ResolveReferences();
        if (defense == null)
        {
            SetMessage("Frontline is not available.");
            return;
        }

        if (defense.Runtime.State == DefenseState.Breached)
        {
            SetMessage("Repair the wall before changing frontline mode.");
            return;
        }

        defense.ToggleMode();
        SetMessage($"Frontline mode changed to {defense.Runtime.Mode}.");
    }

    public void UpgradeWall()
    {
        TryUpgradeDefense("Wall", upgrades => upgrades.GetWallUpgradeCost(), () => defense.TryUpgradeWall());
    }

    public void UpgradeTower()
    {
        TryUpgradeDefense("Tower", upgrades => upgrades.GetTowerUpgradeCost(), () => defense.TryUpgradeTower());
    }

    public void UpgradeDefenders()
    {
        TryUpgradeDefense("Defenders", upgrades => upgrades.GetDefenderUpgradeCost(), () => defense.TryUpgradeDefender());
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

    public void OpenInventoryOverlay()
    {
        TryOpenScreenOverlay(PlayableScreenFocus.InventoryOverlay);
    }

    public void OpenCraftingOverlay()
    {
        TryOpenScreenOverlay(PlayableScreenFocus.CraftingOverlay);
    }

    public void OpenRewardOverlay()
    {
        TryOpenScreenOverlay(PlayableScreenFocus.RewardOverlay);
    }

    public void CloseOverlay()
    {
        ResolveReferences();
        if (screenLayout == null)
        {
            SetMessage("Screen overlays are not available.");
            return;
        }

        screenLayout.CloseOverlay();
        SetMessage(screenLayout.LastLayoutMessage);
    }

    public void Refresh()
    {
        ResolveReferences();
        SetText(summaryText, BuildSummaryText());
        SetText(resourcesText, wallet == null ? "Resources: unavailable" : wallet.FormatAll());
        SetText(dungeonText, BuildDungeonText());
        SetText(latestItemText, BuildLatestItemText());
        SetText(heroStatsText, BuildHeroStatsText());
        string actionHint = BuildActionHintText();
        SetText(messageText, actionHintText == null ? BuildMessageText(actionHint) : lastMessage);
        SetText(actionHintText, actionHint);
        RefreshButtons();
    }

    private string BuildSummaryText()
    {
        if (defense == null)
        {
            return "Frontline: unavailable";
        }

        DefenseRuntimeState runtime = defense.Runtime;
        string pressureText = $"{Mathf.CeilToInt(runtime.EnemyPressure)}/{Mathf.CeilToInt(runtime.EnemyPressureCapacity)}";
        string progressText = $"{Mathf.RoundToInt(runtime.FrontlineProgressPercent * 100f)}%";
        string upgradeText = defense.Upgrades == null
            ? "Upgrades unavailable"
            : $"Wall Lv.{defense.Upgrades.WallLevel} / Tower Lv.{defense.Upgrades.TowerLevel} / Defenders Lv.{defense.Upgrades.DefenderLevel}";

        string groundCombatText = groundCombatPresenter == null ? string.Empty : $"\n{groundCombatPresenter.LastCombatMessage}";
        string screenText = screenLayout == null ? string.Empty : $"\n{BuildScreenLayoutText()}";
        return $"Frontline Lv.{runtime.FrontlineLevel} / {runtime.State} / {runtime.Mode} / Wall {Mathf.CeilToInt(runtime.WallHealth)}/{Mathf.CeilToInt(runtime.WallMaxHealth)}\nPressure {pressureText} / Progress {progressText}\n{upgradeText}{groundCombatText}{screenText}";
    }

    private string BuildDungeonText()
    {
        if (expedition == null)
        {
            return "Dungeon: unavailable";
        }

        string rewardState = BuildRewardStateText();
        string result = string.IsNullOrWhiteSpace(expedition.LastResult) ? "none" : expedition.LastResult;
        string dungeonTextValue = $"Dungeon: {expedition.State} / Room {expedition.RoomsCompleted}/{expedition.TotalRooms} / {expedition.ElapsedSeconds:0.0}s / {rewardState} / Loot {BuildLootSourceText()}\nLast: {result}";

        if (combatRoom == null)
        {
            return $"{dungeonTextValue}\nRoom: unavailable";
        }

        CombatRoomResult roomResult = combatRoom.LastResult;
        string roomMessage = string.IsNullOrWhiteSpace(roomResult.message) ? "none" : roomResult.message;
        string roomProgress = combatRoom.State == CombatRoomState.Starting
            ? $"starting in {combatRoom.CountdownRemaining:0.0}s"
            : $"{combatRoom.ElapsedSeconds:0.0}s";

        return $"{dungeonTextValue}\nRoom: {combatRoom.State} / {roomProgress} / Path {BuildCombatPathText()} / Hero {combatRoom.CurrentHeroHealth:0.#} / Enemy {combatRoom.CurrentEnemyHealth:0.#}\nRoom Last: {roomMessage}";
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

    private string BuildLootSourceText()
    {
        if (lootDropper == null)
        {
            return "unavailable";
        }

        if (lootDropper.LastRewardSource != LootRewardSource.None)
        {
            return FormatLootRewardSource(lootDropper.LastRewardSource);
        }

        if (lootDropper.HasValidWeightedRewardTable)
        {
            return $"authored table ({lootDropper.RewardTableWeight:0.#}w)";
        }

        return "prototype fallback";
    }

    private string BuildCombatPathText()
    {
        if (combatRoom == null)
        {
            return "unavailable";
        }

        if (combatRoom.HasTrackedEnemySetupBlocker)
        {
            return "setup blocked";
        }

        if (combatRoom.UsesTrackedCombatants)
        {
            return "tracked enemies";
        }

        return combatRoom.IsPrototypeSimulationAvailable ? "prototype simulation" : "waiting for enemies";
    }

    private string BuildLatestItemText()
    {
        ItemInstance item = GetLatestItem();
        if (item == null)
        {
            return inventory == null ? "Inventory: unavailable" : $"Inventory: {inventory.Count}/{inventory.Capacity} / Latest: none";
        }

        string equippedText = item.Equipped ? " / Equipped" : string.Empty;
        return $"Inventory: {inventory.Count}/{inventory.Capacity} / Latest: {item.DisplayName} / {item.Rarity} {item.Slot} Power {item.RolledPower}{equippedText}";
    }

    private string BuildHeroStatsText()
    {
        if (characterStats == null)
        {
            return "Hero Stats: unavailable";
        }

        string healthText = heroHealth == null
            ? $"{characterStats.GetValue(StatId.MaxHealth):0.#}"
            : $"{heroHealth.Current:0.#}/{heroHealth.Max:0.#}";
        return $"Hero: ATK {characterStats.GetValue(StatId.AttackDamage):0.#} / HP {healthText} / APS {characterStats.GetValue(StatId.AttackSpeed):0.##}";
    }

    private string BuildScreenLayoutText()
    {
        if (screenLayout == null)
        {
            return "Screen: layout unavailable";
        }

        string overlayText = screenLayout.IsOverlayOpen ? $" / over {screenLayout.PreviousGameplayFocus}" : string.Empty;
        string transitionText = screenLayout.IsTransitioning ? $" / transition {Mathf.RoundToInt(screenLayout.TransitionProgress * 100f)}%" : string.Empty;
        return $"Screen: {screenLayout.CurrentFocus}{overlayText}{transitionText}";
    }

    private string BuildActionHintText()
    {
        if (defense == null)
        {
            return "Next: connect DefenseDirector so frontline rewards and upgrades are visible.";
        }

        if (screenLayout != null && screenLayout.IsOverlayOpen)
        {
            return $"Next: close the overlay to return to {screenLayout.PreviousGameplayFocus}.";
        }

        DefenseRuntimeState runtime = defense.Runtime;
        if (runtime.State == DefenseState.Breached || runtime.WallHealth <= 0f)
        {
            return CanRepairWall()
                ? "Next: repair the wall, then restart the frontline."
                : "Next: frontline income is reduced while breached; wait for repair Gold, then recover the wall.";
        }

        if (!runtime.IsRunning)
        {
            return "Next: start the frontline so Gold and Scrap keep feeding the loop.";
        }

        if (expedition == null)
        {
            return "Next: connect ExpeditionDirector so dungeon attempts are available.";
        }

        if (expedition.IsRunning)
        {
            if (combatRoom != null && combatRoom.HasTrackedEnemySetupBlocker)
            {
                return $"Next: fix enemy spawn setup: {combatRoom.TrackedEnemySetupBlocker}";
            }

            return combatRoom != null && combatRoom.UsesTrackedCombatants
                ? "Next: click enemies to fight through the room; one click issues one order."
                : "Next: wait for the room result; failure should explain what to improve.";
        }

        if (expedition.RewardPending)
        {
            return "Next: claim the dungeon reward.";
        }

        ItemInstance latest = GetLatestItem();
        if (latest != null && !latest.Equipped)
        {
            return "Next: equip the latest item or salvage it into upgrade materials.";
        }

        if (CanBuyAnyDefenseUpgrade())
        {
            return "Next: buy a defense upgrade, then run another dungeon for gear.";
        }

        if (latest != null)
        {
            return "Next: keep the equipped item, salvage spares, or start another dungeon.";
        }

        return "Next: start a dungeon, then use its reward to choose equip or salvage.";
    }

    private string BuildMessageText(string actionHint)
    {
        if (string.IsNullOrWhiteSpace(actionHint))
        {
            return lastMessage;
        }

        if (string.IsNullOrWhiteSpace(lastMessage) || lastMessage == "Ready")
        {
            return actionHint;
        }

        return $"{lastMessage}\n{actionHint}";
    }

    private void RefreshButtons()
    {
        ItemInstance latest = GetLatestItem();
        DefenseRuntimeState runtime = defense == null ? null : defense.Runtime;
        DefenseUpgradeModel upgrades = defense == null ? null : defense.Upgrades;
        CurrencyWallet defenseWallet = GetDefenseWallet();
        bool canUseDefense = defense != null && runtime != null;
        bool canUseUpgrades = canUseDefense && upgrades != null && defenseWallet != null;

        SetInteractable(startDefenseButton, canUseDefense && !runtime.IsRunning && runtime.WallHealth > 0f);
        SetInteractable(repairWallButton, CanRepairWall());
        SetInteractable(toggleFrontlineModeButton, canUseDefense && runtime.State != DefenseState.Breached);
        SetInteractable(upgradeWallButton, canUseUpgrades && defenseWallet.CanSpend(upgrades.GetWallUpgradeCost()));
        SetInteractable(upgradeTowerButton, canUseUpgrades && defenseWallet.CanSpend(upgrades.GetTowerUpgradeCost()));
        SetInteractable(upgradeDefenderButton, canUseUpgrades && defenseWallet.CanSpend(upgrades.GetDefenderUpgradeCost()));
        SetInteractable(startDungeonButton, expedition != null && !expedition.IsRunning);
        SetInteractable(claimRewardButton, expedition != null && (expedition.RewardPending || expedition.State == DungeonRunState.Cleared));
        SetInteractable(equipLatestButton, latest != null && inventory != null && equipmentSlots != null);
        SetInteractable(salvageLatestButton, latest != null && salvageService != null);
        SetInteractable(saveButton, saveManager != null);
        SetInteractable(loadButton, saveManager != null && saveManager.HasSaveFile);
        SetInteractable(openInventoryOverlayButton, screenLayout != null && screenLayout.CanOpenInventoryOverlay);
        SetInteractable(openCraftingOverlayButton, screenLayout != null && screenLayout.CanOpenCraftingOverlay);
        SetInteractable(openRewardOverlayButton, screenLayout != null && screenLayout.CanOpenRewardOverlay);
        SetInteractable(closeOverlayButton, screenLayout != null && screenLayout.IsOverlayOpen);
    }

    private void TryOpenScreenOverlay(PlayableScreenFocus overlayFocus)
    {
        ResolveReferences();
        if (screenLayout == null)
        {
            SetMessage("Screen overlays are not available.");
            return;
        }

        _ = overlayFocus switch
        {
            PlayableScreenFocus.InventoryOverlay => screenLayout.TryOpenInventoryOverlay(),
            PlayableScreenFocus.CraftingOverlay => screenLayout.TryOpenCraftingOverlay(),
            PlayableScreenFocus.RewardOverlay => screenLayout.TryOpenRewardOverlay(),
            _ => false
        };

        SetMessage(screenLayout.LastLayoutMessage);
    }

    private void TryUpgradeDefense(string label, Func<DefenseUpgradeModel, ResourceAmount[]> costSelector, Func<bool> upgradeAction)
    {
        ResolveReferences();
        if (!TryGetDefenseUpgradeContext(out DefenseUpgradeModel upgrades, out CurrencyWallet defenseWallet, out string failureReason))
        {
            SetMessage($"{label} upgrade failed: {failureReason}.");
            return;
        }

        ResourceAmount[] cost = costSelector(upgrades);
        if (!defenseWallet.CanSpend(cost))
        {
            SetMessage($"{label} upgrade needs {FormatRewards(cost)}.");
            return;
        }

        SetMessage(upgradeAction()
            ? $"{label} upgraded."
            : $"{label} upgrade failed.");
    }

    private bool TryGetDefenseUpgradeContext(out DefenseUpgradeModel upgrades, out CurrencyWallet defenseWallet, out string failureReason)
    {
        upgrades = null;
        defenseWallet = null;
        failureReason = string.Empty;

        if (defense == null)
        {
            failureReason = "frontline is not available";
            return false;
        }

        upgrades = defense.Upgrades;
        if (upgrades == null)
        {
            failureReason = "DefenseUpgradeModel is not available";
            return false;
        }

        defenseWallet = GetDefenseWallet();
        if (defenseWallet == null)
        {
            failureReason = "CurrencyWallet is not available";
            return false;
        }

        return true;
    }

    private bool CanBuyAnyDefenseUpgrade()
    {
        if (!TryGetDefenseUpgradeContext(out DefenseUpgradeModel upgrades, out CurrencyWallet defenseWallet, out _))
        {
            return false;
        }

        return defenseWallet.CanSpend(upgrades.GetWallUpgradeCost()) ||
               defenseWallet.CanSpend(upgrades.GetTowerUpgradeCost()) ||
               defenseWallet.CanSpend(upgrades.GetDefenderUpgradeCost());
    }

    private bool CanRepairWall()
    {
        if (!TryGetDefenseUpgradeContext(out DefenseUpgradeModel upgrades, out CurrencyWallet defenseWallet, out _))
        {
            return false;
        }

        DefenseRuntimeState runtime = defense.Runtime;
        float missingHealth = Mathf.Max(0f, runtime.WallMaxHealth - runtime.WallHealth);
        return missingHealth > 0f && defenseWallet.CanSpend(upgrades.GetRepairCost(missingHealth));
    }

    private CurrencyWallet GetDefenseWallet()
    {
        if (defense != null && defense.Wallet != null)
        {
            return defense.Wallet;
        }

        return wallet;
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

        if (groundCombatPresenter == null || force)
        {
            groundCombatPresenter = FindAnyObjectByType<GroundDefenseCombatPresenter>();
        }

        if (screenLayout == null || force)
        {
            screenLayout = FindAnyObjectByType<PlayableScreenLayoutController>();
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

        if (heroHealth == null || force)
        {
            heroHealth = equipmentSlots == null
                ? FindPlayerHealth()
                : equipmentSlots.GetComponent<Health>();
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

    private static Health FindPlayerHealth()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        return player == null ? null : player.GetComponent<Health>();
    }

    private void WireButtons()
    {
        if (buttonsWired)
        {
            return;
        }

        AddListener(startDefenseButton, StartDefense);
        AddListener(repairWallButton, RepairWall);
        AddListener(toggleFrontlineModeButton, ToggleFrontlineMode);
        AddListener(upgradeWallButton, UpgradeWall);
        AddListener(upgradeTowerButton, UpgradeTower);
        AddListener(upgradeDefenderButton, UpgradeDefenders);
        AddListener(startDungeonButton, StartDungeon);
        AddListener(claimRewardButton, ClaimPendingReward);
        AddListener(equipLatestButton, EquipLatest);
        AddListener(salvageLatestButton, SalvageLatest);
        AddListener(saveButton, SaveGame);
        AddListener(loadButton, LoadGame);
        AddListener(openInventoryOverlayButton, OpenInventoryOverlay);
        AddListener(openCraftingOverlayButton, OpenCraftingOverlay);
        AddListener(openRewardOverlayButton, OpenRewardOverlay);
        AddListener(closeOverlayButton, CloseOverlay);
        buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!buttonsWired)
        {
            return;
        }

        RemoveListener(startDefenseButton, StartDefense);
        RemoveListener(repairWallButton, RepairWall);
        RemoveListener(toggleFrontlineModeButton, ToggleFrontlineMode);
        RemoveListener(upgradeWallButton, UpgradeWall);
        RemoveListener(upgradeTowerButton, UpgradeTower);
        RemoveListener(upgradeDefenderButton, UpgradeDefenders);
        RemoveListener(startDungeonButton, StartDungeon);
        RemoveListener(claimRewardButton, ClaimPendingReward);
        RemoveListener(equipLatestButton, EquipLatest);
        RemoveListener(salvageLatestButton, SalvageLatest);
        RemoveListener(saveButton, SaveGame);
        RemoveListener(loadButton, LoadGame);
        RemoveListener(openInventoryOverlayButton, OpenInventoryOverlay);
        RemoveListener(openCraftingOverlayButton, OpenCraftingOverlay);
        RemoveListener(openRewardOverlayButton, OpenRewardOverlay);
        RemoveListener(closeOverlayButton, CloseOverlay);
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
            combatRoom.Resolved += HandleRoomResolved;
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

        if (screenLayout != null)
        {
            screenLayout.FocusChanged += HandleScreenFocusChanged;
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
            combatRoom.Resolved -= HandleRoomResolved;
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

        if (screenLayout != null)
        {
            screenLayout.FocusChanged -= HandleScreenFocusChanged;
        }

        subscribed = false;
    }

    private void SetMessage(string message)
    {
        lastMessage = message;
        Refresh();
    }

    private void HandleRoomResolved(CombatRoomResult result)
    {
        ResolveReferences();
        if (syncScreenFocusWithDungeon)
        {
            screenLayout?.ShowDefenseFocus();
        }

        if (result.resolution == CombatRoomResolution.Cleared)
        {
            string rewardText = expedition != null && expedition.State == DungeonRunState.Cleared
                ? "Dungeon cleared."
                : "Room cleared.";
            SetMessage(rewardText);
            return;
        }

        if (result.resolution == CombatRoomResolution.Failed)
        {
            SetMessage("Room failed. Prepare, then try again.");
        }
    }

    private void HandleScreenFocusChanged(PlayableScreenFocus focus)
    {
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

    private static string FormatLootRewardSource(LootRewardSource rewardSource)
    {
        return rewardSource switch
        {
            LootRewardSource.WeightedRewardTable => "authored table",
            LootRewardSource.RewardDefinitions => "legacy list",
            LootRewardSource.PrototypeFallback => "prototype fallback",
            _ => "none"
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
