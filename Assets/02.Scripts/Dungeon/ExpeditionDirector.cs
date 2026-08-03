using System;
using UnityEngine;

public class ExpeditionDirector : MonoBehaviour
{
    [Header("Prototype Definition")]
    [SerializeField] private string dungeonId = "prototype_crypt";
    [SerializeField] private int depth = 1;
    [SerializeField] private int totalRooms = 1;

    [Header("Runtime")]
    [SerializeField] private DungeonExpeditionSnapshot runtime = new DungeonExpeditionSnapshot();

    [Header("Rewards")]
    [SerializeField] private LootDropper lootDropper;
    [SerializeField] private bool autoFindLootDropper = true;

    public event Action Changed;

    /// <summary>
    /// Runtime projections must wait until the save manager has applied either a validated snapshot or
    /// a deliberate new-session snapshot. This prevents Awake/OnEnable ordering from creating a second run.
    /// </summary>
    public bool IsSnapshotReady { get; private set; }

    public DungeonRunState State => runtime == null ? DungeonRunState.Ready : runtime.state;
    public bool IsRunning => State == DungeonRunState.Running;
    public bool IsAwaitingRoomExit => State == DungeonRunState.AwaitingExit;
    public bool HasActiveExpedition => IsRunning || IsAwaitingRoomExit;
    public string DungeonId => runtime == null || string.IsNullOrWhiteSpace(runtime.dungeonId) ? dungeonId : runtime.dungeonId;
    public int Depth => runtime == null ? Mathf.Max(1, depth) : Mathf.Max(1, runtime.depth);
    public int SelectedDepth => runtime == null
        ? Mathf.Max(1, depth)
        : Mathf.Clamp(Mathf.Max(1, runtime.selectedDepth), 1, HighestUnlockedDepth);
    public int HighestUnlockedDepth => runtime == null
        ? Mathf.Max(1, depth)
        : Mathf.Max(1, runtime.highestUnlockedDepth, Mathf.Max(1, runtime.depth));
    public bool CanSelectPreviousDepth => !HasActiveExpedition && SelectedDepth > 1;
    public bool CanSelectNextDepth => !HasActiveExpedition && SelectedDepth < HighestUnlockedDepth;
    public int TotalRooms => runtime == null ? Mathf.Max(1, totalRooms) : Mathf.Max(1, runtime.totalRooms);
    public int CurrentRoomIndex => runtime == null ? 0 : Mathf.Max(0, runtime.currentRoomIndex);
    public int RoomsCompleted => runtime == null ? 0 : Mathf.Max(0, runtime.roomsCompleted);
    public DungeonRunPlan RunPlan => runtime == null ? null : runtime.runPlan;
    public DungeonRoomResumePoint ResumePoint => runtime == null ? DungeonRoomResumePoint.None : runtime.resumePoint;
    public int RunSeed => RunPlan == null ? 0 : RunPlan.runSeed;
    public string CurrentRoomTemplateId => RunPlan == null ? string.Empty : RunPlan.currentRoomTemplateId;
    public float ElapsedSeconds => runtime == null ? 0f : Mathf.Max(0f, runtime.elapsedSeconds);
    public bool RewardPending => runtime != null && runtime.rewardPending;
    public string LastResult => runtime == null ? string.Empty : runtime.lastResult;
    public string OfferedContractIdA => runtime == null ? DungeonContractModel.DefaultContractId : runtime.offeredContractIdA;
    public string OfferedContractIdB => runtime == null ? "ravenous_pact" : runtime.offeredContractIdB;
    public string SelectedContractId => runtime == null ? DungeonContractModel.DefaultContractId : runtime.selectedContractId;
    public string ActiveContractId => runtime == null ? DungeonContractModel.DefaultContractId : runtime.activeContractId;
    public string LastContractSummary => runtime == null ? string.Empty : runtime.lastContractSummary;
    public int EncounterSeed => runtime == null ? 0 : Mathf.Max(0, runtime.encounterSeed);
    public string SelectedEncounterId => runtime == null ? DungeonEncounterModel.DefaultEncounterId : runtime.selectedEncounterId;
    public string ActiveEncounterId => runtime == null ? DungeonEncounterModel.DefaultEncounterId : runtime.activeEncounterId;
    public string LastEncounterSummary => runtime == null ? string.Empty : runtime.lastEncounterSummary;
    public DungeonContractProfile OfferedContractA => DungeonContractModel.GetContractOrDefault(OfferedContractIdA);
    public DungeonContractProfile OfferedContractB => DungeonContractModel.GetContractOrDefault(OfferedContractIdB);
    public DungeonContractProfile SelectedContract => DungeonContractModel.GetContractOrDefault(SelectedContractId);
    public DungeonContractProfile ActiveContract => DungeonContractModel.TryGetContract(ActiveContractId, out DungeonContractProfile activeContract)
        ? activeContract
        : SelectedContract;
    public DungeonEncounterProfile SelectedEncounter => DungeonEncounterModel.GetEncounterOrDefault(SelectedEncounterId);
    public DungeonEncounterProfile ActiveEncounter => DungeonEncounterModel.TryGetEncounter(ActiveEncounterId, out DungeonEncounterProfile activeEncounter)
        ? activeEncounter
        : SelectedEncounter;
    public float ActiveEnemyHealthMultiplier => ActiveContract.EnemyHealthMultiplier * ActiveEncounter.EnemyHealthMultiplier;
    public float ActiveEnemyDamageMultiplier => ActiveContract.EnemyDamageMultiplier * ActiveEncounter.EnemyDamageMultiplier;
    public int ActiveRewardDepthOffset => ActiveContract.RewardDepthOffset + ActiveEncounter.RewardDepthOffset;
    public int ActiveRewardDepth => GetRewardDepth(Depth, ActiveContract, ActiveEncounter);
    public bool CanSelectContract => !HasActiveExpedition;

    private void Awake()
    {
        EnsureRuntime();
        IsSnapshotReady = false;
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        runtime.elapsedSeconds += Time.deltaTime;
    }

    private void OnValidate()
    {
        depth = Mathf.Max(1, depth);
        totalRooms = Mathf.Max(1, totalRooms);

        if (string.IsNullOrWhiteSpace(dungeonId))
        {
            dungeonId = "prototype_crypt";
        }
    }

    public void ResetToReady()
    {
        EnsureRuntime();
        runtime.state = DungeonRunState.Ready;
        runtime.resumePoint = DungeonRoomResumePoint.None;
        runtime.dungeonId = dungeonId;
        runtime.depth = SelectedDepth;
        runtime.totalRooms = Mathf.Max(1, totalRooms);
        runtime.currentRoomIndex = 0;
        runtime.roomsCompleted = 0;
        runtime.runPlan = null;
        runtime.elapsedSeconds = 0f;
        runtime.rewardPending = false;
        runtime.activeContractId = string.Empty;
        runtime.activeEncounterId = string.Empty;
        EnsureContractOffer();
        EnsureSelectedContract();
        EnsureSelectedEncounter();
        runtime.lastContractSummary = DungeonContractModel.FormatOfferText(
            runtime.offeredContractIdA,
            runtime.offeredContractIdB);
        runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(SelectedEncounter);
        runtime.lastResult = "Ready";
        IsSnapshotReady = true;
        NotifyChanged();
    }

    /// <summary>
    /// Establishes the first authoritative snapshot when no readable save exists. It is intentionally
    /// called by DefenseSaveManager after startup, never by Awake.
    /// </summary>
    public void InitializeFreshSnapshot()
    {
        ResetToReady();
    }

    public bool StartExpedition()
    {
        EnsureRuntime();

        if (HasActiveExpedition)
        {
            return false;
        }

        EnsureContractOffer();
        EnsureSelectedContract();
        EnsureSelectedEncounter();
        DungeonContractProfile contract = SelectedContract;
        DungeonEncounterProfile encounter = SelectedEncounter;
        runtime.state = DungeonRunState.Running;
        runtime.resumePoint = DungeonRoomResumePoint.RestartCurrentRoom;
        runtime.dungeonId = dungeonId;
        runtime.depth = SelectedDepth;
        runtime.activeContractId = contract.Id;
        runtime.activeEncounterId = encounter.Id;
        runtime.encounterSeed++;
        GenerateSelectedEncounter();
        runtime.totalRooms = Mathf.Max(1, totalRooms);
        runtime.currentRoomIndex = 0;
        runtime.roomsCompleted = 0;
        runtime.runPlan = DungeonRunPlan.CreateNew(
            dungeonId,
            DungeonRunPlan.CreateRuntimeSeed(),
            runtime.depth);
        runtime.elapsedSeconds = 0f;
        runtime.rewardPending = false;
        runtime.lastContractSummary = DungeonContractModel.FormatDetailText(contract);
        runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(encounter);
        runtime.lastResult = $"Expedition started / Contract: {contract.DisplayName} / Encounter: {encounter.DisplayName}";
        NotifyChanged();
        return true;
    }

    public bool TrySelectDepth(int targetDepth)
    {
        EnsureRuntime();

        if (HasActiveExpedition ||
            targetDepth < 1 ||
            targetDepth > runtime.highestUnlockedDepth)
        {
            return false;
        }

        runtime.selectedDepth = targetDepth;
        GenerateContractOffer(clearSelection: true);
        EnsureSelectedContract();
        GenerateSelectedEncounter();
        runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(SelectedEncounter);
        NotifyChanged();
        return true;
    }

    public bool SelectPreviousDepth()
    {
        return TrySelectDepth(SelectedDepth - 1);
    }

    public bool SelectNextDepth()
    {
        return TrySelectDepth(SelectedDepth + 1);
    }

    public void StartExpeditionFromButton()
    {
        StartExpedition();
    }

    public DungeonDepthBalanceProfile GetEffectiveDepthBalance(int targetDepth)
    {
        DungeonDepthBalanceProfile baseProfile = DungeonDepthBalanceModel.Evaluate(targetDepth);
        bool useActiveRunProfile = IsRunning || RewardPending;
        DungeonContractProfile contract = useActiveRunProfile ? ActiveContract : SelectedContract;
        DungeonEncounterProfile encounter = useActiveRunProfile ? ActiveEncounter : SelectedEncounter;
        DungeonDepthBalanceProfile rewardProfile = DungeonDepthBalanceModel.Evaluate(GetRewardDepth(targetDepth, contract, encounter));
        return new DungeonDepthBalanceProfile(
            baseProfile.Depth,
            baseProfile.BandNumber,
            baseProfile.DepthInBand,
            baseProfile.EnemyHealthMultiplier * contract.EnemyHealthMultiplier * encounter.EnemyHealthMultiplier,
            baseProfile.EnemyDamageMultiplier * contract.EnemyDamageMultiplier * encounter.EnemyDamageMultiplier,
            rewardProfile.RewardPowerMultiplier,
            rewardProfile.MaterialYieldMultiplier);
    }

    public bool SelectFirstContract()
    {
        EnsureRuntime();
        return TrySelectContract(runtime.offeredContractIdA);
    }

    public bool SelectSecondContract()
    {
        EnsureRuntime();
        return TrySelectContract(runtime.offeredContractIdB);
    }

    public bool TrySelectContract(string contractId)
    {
        EnsureRuntime();

        if (HasActiveExpedition ||
            string.IsNullOrWhiteSpace(contractId) ||
            !DungeonContractModel.TryGetContract(contractId, out DungeonContractProfile contract) ||
            !IsOfferedContract(contract.Id))
        {
            return false;
        }

        runtime.selectedContractId = contract.Id;
        GenerateSelectedEncounter();
        runtime.lastContractSummary = DungeonContractModel.FormatDetailText(contract);
        runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(SelectedEncounter);
        runtime.lastResult = $"Contract selected: {contract.DisplayName} / Next encounter: {SelectedEncounter.DisplayName}";
        NotifyChanged();
        return true;
    }

    public bool RefreshContractOffer()
    {
        EnsureRuntime();

        if (HasActiveExpedition)
        {
            return false;
        }

        runtime.contractOfferSeed++;
        GenerateContractOffer(clearSelection: true);
        EnsureSelectedContract();
        runtime.lastContractSummary = DungeonContractModel.FormatOfferText(
            runtime.offeredContractIdA,
            runtime.offeredContractIdB);
        GenerateSelectedEncounter();
        runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(SelectedEncounter);
        runtime.lastResult = $"Dungeon contract offer refreshed / Next encounter: {SelectedEncounter.DisplayName}";
        NotifyChanged();
        return true;
    }

    public bool CompleteRoom()
    {
        EnsureRuntime();

        if (runtime.state != DungeonRunState.Running)
        {
            return false;
        }

        runtime.roomsCompleted = runtime.roomsCompleted == int.MaxValue
            ? int.MaxValue
            : Mathf.Max(0, runtime.roomsCompleted) + 1;
        runtime.totalRooms = Mathf.Max(runtime.totalRooms, runtime.currentRoomIndex + 1, runtime.roomsCompleted);
        runtime.state = DungeonRunState.AwaitingExit;
        runtime.resumePoint = DungeonRoomResumePoint.AwaitingExit;
        runtime.rewardPending = true;
        EnsureRunPlan();
        runtime.runPlan.SetRewardPending(true, ActiveRewardDepth);
        runtime.lastResult = $"Room cleared / Choose Return Portal to bank the reward or Deeper Exit for depth {Depth + 1}.";

        NotifyChanged();
        return true;
    }

    /// <summary>
    /// Continues the active seeded expedition after a cleared room. The unbanked reward stays pending,
    /// while DungeonRunPlan derives and persists the next room's depth, index, and placement seeds.
    /// </summary>
    public bool TryEnterDeeperRoom()
    {
        EnsureRuntime();

        if (!IsAwaitingRoomExit || !runtime.rewardPending)
        {
            return false;
        }

        runtime.depth = runtime.depth == int.MaxValue ? int.MaxValue : Mathf.Max(1, runtime.depth) + 1;
        runtime.highestUnlockedDepth = Mathf.Max(runtime.highestUnlockedDepth, runtime.depth);
        runtime.currentRoomIndex = runtime.currentRoomIndex == int.MaxValue
            ? int.MaxValue
            : Mathf.Max(0, runtime.currentRoomIndex) + 1;
        runtime.totalRooms = Mathf.Max(runtime.totalRooms, runtime.currentRoomIndex + 1);
        runtime.state = DungeonRunState.Running;
        runtime.resumePoint = DungeonRoomResumePoint.RestartCurrentRoom;
        EnsureRunPlan();
        runtime.runPlan.SetCurrentRoom(dungeonId, runtime.depth, runtime.currentRoomIndex);
        runtime.runPlan.SetRewardPending(true, ActiveRewardDepth);
        runtime.lastResult = $"Deeper exit chosen / Loading depth {runtime.depth}, room {runtime.currentRoomIndex + 1}.";
        NotifyChanged();
        return true;
    }

    /// <summary>
    /// Banks the cleared room's pending reward and closes the expedition. Physical hub placement is owned
    /// by DungeonRoomLoader so the current additive room can be unloaded safely after this state change.
    /// </summary>
    public bool TryReturnToHub()
    {
        EnsureRuntime();

        if (!IsAwaitingRoomExit)
        {
            return false;
        }

        if (runtime.rewardPending && !TryGrantPendingReward())
        {
            return false;
        }

        ResetToReady();
        runtime.lastResult = "Return portal banked the expedition reward. Ready for a new expedition.";
        NotifyChanged();
        return true;
    }

    /// <summary>
    /// Persists the catalog choice for the active room so a later load cannot roll a different template.
    /// DungeonRoomLoader owns catalog lookup and scene lifetime; ExpeditionDirector retains save authority.
    /// </summary>
    public bool TryAssignCurrentRoomTemplate(string templateId)
    {
        EnsureRuntime();

        if (!IsRunning || runtime.runPlan == null || !runtime.runPlan.AssignCurrentRoomTemplate(templateId))
        {
            return false;
        }

        runtime.lastResult = $"Room template assigned: {runtime.runPlan.currentRoomTemplateId}";
        NotifyChanged();
        return true;
    }

    public void CompleteRoomFromButton()
    {
        CompleteRoom();
    }

    public bool FailExpedition()
    {
        EnsureRuntime();

        if (runtime.state != DungeonRunState.Running)
        {
            return false;
        }

        string contractName = ActiveContract.DisplayName;
        string encounterName = ActiveEncounter.DisplayName;
        runtime.rewardPending = false;
        EnsureRunPlan();
        runtime.runPlan.SetRewardPending(false, 0);
        ResetToReady();
        runtime.lastResult = $"Expedition failed / Contract: {contractName} / Encounter: {encounterName}";
        NotifyChanged();
        return true;
    }

    public void FailExpeditionFromButton()
    {
        FailExpedition();
    }

    public bool TryGrantPendingReward()
    {
        EnsureRuntime();

        if (!runtime.rewardPending)
        {
            return false;
        }

        ResolveLootDropper();
        if (lootDropper == null)
        {
            runtime.lastResult = "Reward pending: no LootDropper found";
            Debug.LogWarning("ExpeditionDirector could not grant a reward because no LootDropper was found.", this);
            NotifyChanged();
            return false;
        }

        if (!lootDropper.TryGrantClearReward(ActiveRewardDepth, out ItemInstance item))
        {
            runtime.lastResult = string.IsNullOrWhiteSpace(lootDropper.LastDropMessage)
                ? "Reward pending: loot grant failed"
                : lootDropper.LastDropMessage;
            NotifyChanged();
            return false;
        }

        runtime.rewardPending = false;
        if (runtime.runPlan != null)
        {
            runtime.runPlan.SetRewardPending(false, 0);
        }
        runtime.lastResult = lootDropper.LastRewardAutoConverted
            ? $"Reward converted: {item.DisplayName} -> {FormatRewards(lootDropper.LastConversionRewards)} / Contract: {ActiveContract.DisplayName} / Encounter: {ActiveEncounter.DisplayName}"
            : $"Reward granted: {item.DisplayName} / Contract: {ActiveContract.DisplayName} / Encounter: {ActiveEncounter.DisplayName}";
        NotifyChanged();
        return true;
    }

    public void GrantPendingRewardFromButton()
    {
        TryGrantPendingReward();
    }

    public DungeonSaveData CreateSaveData()
    {
        EnsureRuntime();

        runtime.version = DungeonExpeditionSnapshot.CurrentVersion;
        runtime.resumePoint = DungeonExpeditionSnapshot.GetExpectedResumePoint(runtime.state);
        return runtime.ToSaveData();
    }

    public void ApplySaveData(DungeonSaveData saveData)
    {
        TryApplySaveData(saveData, out _);
    }

    public bool TryApplySaveData(DungeonSaveData saveData, out string report)
    {
        DungeonExpeditionSnapshot incoming = saveData?.expeditionSnapshot == null
            ? DungeonExpeditionSnapshot.FromLegacy(saveData)
            : saveData.expeditionSnapshot.Clone();

        if (!incoming.TryValidate(out report))
        {
            return false;
        }

        runtime = incoming;
        IsSnapshotReady = true;
        report = $"Dungeon snapshot v{runtime.version} restored: {runtime.state}/{runtime.resumePoint}, depth {runtime.depth}, room {runtime.currentRoomIndex + 1}.";
        NotifyChanged();
        return true;
    }

    private void EnsureRuntime()
    {
        if (runtime == null)
        {
            int initialDepth = Mathf.Max(1, depth);
            runtime = new DungeonExpeditionSnapshot
            {
                depth = initialDepth,
                selectedDepth = initialDepth,
                highestUnlockedDepth = initialDepth
            };
            return;
        }

        int activeDepth = Mathf.Max(1, runtime.depth);
        runtime.highestUnlockedDepth = Mathf.Max(activeDepth, Mathf.Max(1, runtime.highestUnlockedDepth));
        int selectedDepth = runtime.selectedDepth > 0 ? runtime.selectedDepth : activeDepth;
        runtime.selectedDepth = Mathf.Clamp(selectedDepth, 1, runtime.highestUnlockedDepth);
        runtime.depth = Mathf.Clamp(activeDepth, 1, runtime.highestUnlockedDepth);
        runtime.version = DungeonExpeditionSnapshot.CurrentVersion;
        runtime.resumePoint = DungeonExpeditionSnapshot.GetExpectedResumePoint(runtime.state);
        EnsureContractOffer();
        EnsureSelectedContract();
        EnsureSelectedEncounter();
        if (runtime.state == DungeonRunState.Ready)
        {
            runtime.activeContractId = string.Empty;
            runtime.activeEncounterId = string.Empty;
        }
        else if (!DungeonContractModel.TryGetContract(runtime.activeContractId, out _))
        {
            runtime.activeContractId = runtime.selectedContractId;
        }

        if (runtime.state != DungeonRunState.Ready &&
            !DungeonEncounterModel.TryGetEncounter(runtime.activeEncounterId, out _))
        {
            runtime.activeEncounterId = runtime.selectedEncounterId;
        }

        EnsureRunPlan();
    }

    private void EnsureRunPlan()
    {
        bool hasActiveRunState = HasActiveExpedition;
        if (!hasActiveRunState)
        {
            runtime.runPlan = null;
            return;
        }

        if (runtime.runPlan == null)
        {
            runtime.runPlan = DungeonRunPlan.CreateMigrated(
                dungeonId,
                runtime.depth,
                runtime.currentRoomIndex,
                runtime.contractOfferSeed,
                runtime.encounterSeed);
        }
        else
        {
            runtime.runPlan.Normalize(dungeonId, runtime.depth, runtime.currentRoomIndex);
        }

        int rewardDepth = runtime.rewardPending ? ActiveRewardDepth : 0;
        runtime.runPlan.SetRewardPending(runtime.rewardPending, rewardDepth);
    }

    private void GenerateContractOffer(bool clearSelection)
    {
        DungeonContractModel.BuildOffer(
            SelectedDepth,
            runtime.contractOfferSeed,
            out runtime.offeredContractIdA,
            out runtime.offeredContractIdB);

        if (clearSelection)
        {
            runtime.selectedContractId = string.Empty;
        }
    }

    private void EnsureContractOffer()
    {
        bool firstValid = DungeonContractModel.TryGetContract(runtime.offeredContractIdA, out DungeonContractProfile first);
        bool secondValid = DungeonContractModel.TryGetContract(runtime.offeredContractIdB, out DungeonContractProfile second);
        if (!firstValid || !secondValid || string.Equals(first.Id, second.Id, StringComparison.OrdinalIgnoreCase))
        {
            GenerateContractOffer(clearSelection: true);
        }
    }

    private void EnsureSelectedContract()
    {
        if (!DungeonContractModel.TryGetContract(runtime.selectedContractId, out DungeonContractProfile selected) ||
            !IsOfferedContract(selected.Id))
        {
            runtime.selectedContractId = DungeonContractModel.TryGetContract(runtime.offeredContractIdA, out DungeonContractProfile first)
                ? first.Id
                : DungeonContractModel.DefaultContractId;
        }
    }

    private void GenerateSelectedEncounter()
    {
        DungeonEncounterProfile encounter = DungeonEncounterModel.BuildEncounter(
            SelectedDepth,
            runtime.encounterSeed,
            runtime.selectedContractId);
        runtime.selectedEncounterId = encounter.Id;
    }

    private void EnsureSelectedEncounter()
    {
        runtime.encounterSeed = Mathf.Max(0, runtime.encounterSeed);
        if (!DungeonEncounterModel.TryGetEncounter(runtime.selectedEncounterId, out _))
        {
            GenerateSelectedEncounter();
        }

        if (string.IsNullOrWhiteSpace(runtime.lastEncounterSummary))
        {
            runtime.lastEncounterSummary = DungeonEncounterModel.FormatDetailText(SelectedEncounter);
        }
    }

    private bool IsOfferedContract(string contractId)
    {
        return string.Equals(contractId, runtime.offeredContractIdA, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contractId, runtime.offeredContractIdB, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRewardDepth(int sourceDepth, DungeonContractProfile contract, DungeonEncounterProfile encounter)
    {
        int rewardDepthOffset = Mathf.Max(0, contract.RewardDepthOffset) + Mathf.Max(0, encounter.RewardDepthOffset);
        return Mathf.Max(1, sourceDepth + rewardDepthOffset);
    }

    private void ResolveLootDropper()
    {
        if (lootDropper == null && autoFindLootDropper)
        {
            lootDropper = FindAnyObjectByType<LootDropper>();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    private static string FormatRewards(ResourceAmount[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return "no materials";
        }

        return string.Join(", ", Array.ConvertAll(rewards, reward => reward.ToString()));
    }
}
