using System;
using UnityEngine;

public class ExpeditionDirector : MonoBehaviour
{
    [Header("Prototype Definition")]
    [SerializeField] private string dungeonId = "prototype_crypt";
    [SerializeField] private int depth = 1;
    [SerializeField] private int totalRooms = 1;

    [Header("Runtime")]
    [SerializeField] private bool resetToReadyOnAwake = true;
    [SerializeField] private DungeonSaveData runtime = new DungeonSaveData();

    [Header("Rewards")]
    [SerializeField] private LootDropper lootDropper;
    [SerializeField] private bool autoFindLootDropper = true;
    [SerializeField] private bool grantRewardOnExpeditionClear = true;

    public event Action Changed;

    public DungeonRunState State => runtime == null ? DungeonRunState.Ready : runtime.state;
    public bool IsRunning => State == DungeonRunState.Running;
    public string DungeonId => runtime == null || string.IsNullOrWhiteSpace(runtime.dungeonId) ? dungeonId : runtime.dungeonId;
    public int Depth => runtime == null ? Mathf.Max(1, depth) : Mathf.Max(1, runtime.depth);
    public int SelectedDepth => runtime == null
        ? Mathf.Max(1, depth)
        : Mathf.Clamp(Mathf.Max(1, runtime.selectedDepth), 1, HighestUnlockedDepth);
    public int HighestUnlockedDepth => runtime == null
        ? Mathf.Max(1, depth)
        : Mathf.Max(1, runtime.highestUnlockedDepth, Mathf.Max(1, runtime.depth));
    public bool CanSelectPreviousDepth => !IsRunning && SelectedDepth > 1;
    public bool CanSelectNextDepth => !IsRunning && SelectedDepth < HighestUnlockedDepth;
    public int TotalRooms => runtime == null ? Mathf.Max(1, totalRooms) : Mathf.Max(1, runtime.totalRooms);
    public int CurrentRoomIndex => runtime == null ? 0 : Mathf.Max(0, runtime.currentRoomIndex);
    public int RoomsCompleted => runtime == null ? 0 : Mathf.Max(0, runtime.roomsCompleted);
    public float ElapsedSeconds => runtime == null ? 0f : Mathf.Max(0f, runtime.elapsedSeconds);
    public bool RewardPending => runtime != null && runtime.rewardPending;
    public string LastResult => runtime == null ? string.Empty : runtime.lastResult;
    public string OfferedContractIdA => runtime == null ? DungeonContractModel.DefaultContractId : runtime.offeredContractIdA;
    public string OfferedContractIdB => runtime == null ? "ravenous_pact" : runtime.offeredContractIdB;
    public string SelectedContractId => runtime == null ? DungeonContractModel.DefaultContractId : runtime.selectedContractId;
    public string ActiveContractId => runtime == null ? DungeonContractModel.DefaultContractId : runtime.activeContractId;
    public string LastContractSummary => runtime == null ? string.Empty : runtime.lastContractSummary;
    public DungeonContractProfile OfferedContractA => DungeonContractModel.GetContractOrDefault(OfferedContractIdA);
    public DungeonContractProfile OfferedContractB => DungeonContractModel.GetContractOrDefault(OfferedContractIdB);
    public DungeonContractProfile SelectedContract => DungeonContractModel.GetContractOrDefault(SelectedContractId);
    public DungeonContractProfile ActiveContract => DungeonContractModel.TryGetContract(ActiveContractId, out DungeonContractProfile activeContract)
        ? activeContract
        : SelectedContract;
    public float ActiveEnemyHealthMultiplier => ActiveContract.EnemyHealthMultiplier;
    public float ActiveEnemyDamageMultiplier => ActiveContract.EnemyDamageMultiplier;
    public int ActiveRewardDepthOffset => ActiveContract.RewardDepthOffset;
    public int ActiveRewardDepth => GetRewardDepth(Depth, ActiveContract);
    public bool CanSelectContract => !IsRunning;

    private void Awake()
    {
        EnsureRuntime();

        if (resetToReadyOnAwake)
        {
            ResetToReady();
        }
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
        runtime.dungeonId = dungeonId;
        runtime.depth = SelectedDepth;
        runtime.totalRooms = Mathf.Max(1, totalRooms);
        runtime.currentRoomIndex = 0;
        runtime.roomsCompleted = 0;
        runtime.elapsedSeconds = 0f;
        runtime.rewardPending = false;
        runtime.activeContractId = string.Empty;
        EnsureContractOffer();
        EnsureSelectedContract();
        runtime.lastContractSummary = DungeonContractModel.FormatOfferText(
            runtime.offeredContractIdA,
            runtime.offeredContractIdB);
        runtime.lastResult = "Ready";
        NotifyChanged();
    }

    public bool StartExpedition()
    {
        EnsureRuntime();

        if (runtime.state == DungeonRunState.Running)
        {
            return false;
        }

        EnsureContractOffer();
        EnsureSelectedContract();
        DungeonContractProfile contract = SelectedContract;
        runtime.state = DungeonRunState.Running;
        runtime.dungeonId = dungeonId;
        runtime.depth = SelectedDepth;
        runtime.activeContractId = contract.Id;
        runtime.totalRooms = Mathf.Max(1, totalRooms);
        runtime.currentRoomIndex = 0;
        runtime.roomsCompleted = 0;
        runtime.elapsedSeconds = 0f;
        runtime.rewardPending = false;
        runtime.lastContractSummary = DungeonContractModel.FormatDetailText(contract);
        runtime.lastResult = $"Expedition started / Contract: {contract.DisplayName}";
        NotifyChanged();
        return true;
    }

    public bool TrySelectDepth(int targetDepth)
    {
        EnsureRuntime();

        if (runtime.state == DungeonRunState.Running ||
            targetDepth < 1 ||
            targetDepth > runtime.highestUnlockedDepth)
        {
            return false;
        }

        runtime.selectedDepth = targetDepth;
        GenerateContractOffer(clearSelection: true);
        EnsureSelectedContract();
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
        DungeonContractProfile contract = State == DungeonRunState.Ready ? SelectedContract : ActiveContract;
        DungeonDepthBalanceProfile rewardProfile = DungeonDepthBalanceModel.Evaluate(GetRewardDepth(targetDepth, contract));
        return new DungeonDepthBalanceProfile(
            baseProfile.Depth,
            baseProfile.BandNumber,
            baseProfile.DepthInBand,
            baseProfile.EnemyHealthMultiplier * contract.EnemyHealthMultiplier,
            baseProfile.EnemyDamageMultiplier * contract.EnemyDamageMultiplier,
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

        if (runtime.state == DungeonRunState.Running ||
            string.IsNullOrWhiteSpace(contractId) ||
            !DungeonContractModel.TryGetContract(contractId, out DungeonContractProfile contract) ||
            !IsOfferedContract(contract.Id))
        {
            return false;
        }

        runtime.selectedContractId = contract.Id;
        runtime.lastContractSummary = DungeonContractModel.FormatDetailText(contract);
        runtime.lastResult = $"Contract selected: {contract.DisplayName}";
        NotifyChanged();
        return true;
    }

    public bool RefreshContractOffer()
    {
        EnsureRuntime();

        if (runtime.state == DungeonRunState.Running)
        {
            return false;
        }

        runtime.contractOfferSeed++;
        GenerateContractOffer(clearSelection: true);
        EnsureSelectedContract();
        runtime.lastContractSummary = DungeonContractModel.FormatOfferText(
            runtime.offeredContractIdA,
            runtime.offeredContractIdB);
        runtime.lastResult = "Dungeon contract offer refreshed";
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

        runtime.roomsCompleted = Mathf.Clamp(runtime.roomsCompleted + 1, 0, Mathf.Max(1, runtime.totalRooms));

        if (runtime.roomsCompleted >= runtime.totalRooms)
        {
            runtime.state = DungeonRunState.Cleared;
            runtime.currentRoomIndex = Mathf.Max(0, runtime.totalRooms - 1);
            runtime.rewardPending = true;
            runtime.lastResult = "Expedition cleared";
            int unlockedDepth = TryUnlockNextDepth();
            if (grantRewardOnExpeditionClear)
            {
                TryGrantPendingReward();
            }

            if (unlockedDepth > 0)
            {
                runtime.lastResult = $"{runtime.lastResult} / Depth {unlockedDepth} unlocked";
            }

            if (!string.IsNullOrWhiteSpace(runtime.activeContractId))
            {
                runtime.lastResult = $"{runtime.lastResult} / Contract: {ActiveContract.DisplayName}";
            }
        }
        else
        {
            runtime.currentRoomIndex = runtime.roomsCompleted;
            runtime.lastResult = "Room cleared";
        }

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

        runtime.state = DungeonRunState.Failed;
        runtime.rewardPending = false;
        runtime.lastResult = $"Expedition failed / Contract: {ActiveContract.DisplayName}";
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
        runtime.lastResult = lootDropper.LastRewardAutoConverted
            ? $"Reward converted: {item.DisplayName} -> {FormatRewards(lootDropper.LastConversionRewards)} / Contract: {ActiveContract.DisplayName}"
            : $"Reward granted: {item.DisplayName} / Contract: {ActiveContract.DisplayName}";
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

        return new DungeonSaveData
        {
            state = runtime.state,
            dungeonId = runtime.dungeonId,
            depth = Mathf.Max(1, runtime.depth),
            selectedDepth = SelectedDepth,
            highestUnlockedDepth = HighestUnlockedDepth,
            contractOfferSeed = runtime.contractOfferSeed,
            offeredContractIdA = runtime.offeredContractIdA,
            offeredContractIdB = runtime.offeredContractIdB,
            selectedContractId = runtime.selectedContractId,
            activeContractId = runtime.activeContractId,
            lastContractSummary = runtime.lastContractSummary,
            totalRooms = Mathf.Max(1, runtime.totalRooms),
            currentRoomIndex = Mathf.Max(0, runtime.currentRoomIndex),
            roomsCompleted = Mathf.Clamp(runtime.roomsCompleted, 0, Mathf.Max(1, runtime.totalRooms)),
            elapsedSeconds = Mathf.Max(0f, runtime.elapsedSeconds),
            rewardPending = runtime.rewardPending,
            lastResult = runtime.lastResult
        };
    }

    public void ApplySaveData(DungeonSaveData saveData)
    {
        if (saveData == null)
        {
            runtime = new DungeonSaveData
            {
                depth = Mathf.Max(1, depth),
                selectedDepth = Mathf.Max(1, depth),
                highestUnlockedDepth = Mathf.Max(1, depth)
            };
            ResetToReady();
            return;
        }

        int activeDepth = Mathf.Max(1, saveData.depth);
        int highestDepth = Mathf.Max(activeDepth, Mathf.Max(1, saveData.highestUnlockedDepth));
        int selectedDepth = saveData.selectedDepth > 0 ? saveData.selectedDepth : activeDepth;
        runtime = new DungeonSaveData
        {
            state = saveData.state,
            dungeonId = string.IsNullOrWhiteSpace(saveData.dungeonId) ? dungeonId : saveData.dungeonId,
            depth = Mathf.Clamp(activeDepth, 1, highestDepth),
            selectedDepth = Mathf.Clamp(selectedDepth, 1, highestDepth),
            highestUnlockedDepth = highestDepth,
            contractOfferSeed = Mathf.Max(0, saveData.contractOfferSeed),
            offeredContractIdA = saveData.offeredContractIdA,
            offeredContractIdB = saveData.offeredContractIdB,
            selectedContractId = saveData.selectedContractId,
            activeContractId = saveData.activeContractId,
            lastContractSummary = saveData.lastContractSummary,
            totalRooms = Mathf.Max(1, saveData.totalRooms),
            currentRoomIndex = Mathf.Max(0, saveData.currentRoomIndex),
            roomsCompleted = Mathf.Max(0, saveData.roomsCompleted),
            elapsedSeconds = Mathf.Max(0f, saveData.elapsedSeconds),
            rewardPending = saveData.rewardPending,
            lastResult = saveData.lastResult
        };

        runtime.roomsCompleted = Mathf.Clamp(runtime.roomsCompleted, 0, runtime.totalRooms);
        runtime.currentRoomIndex = Mathf.Clamp(runtime.currentRoomIndex, 0, Mathf.Max(0, runtime.totalRooms - 1));

        if (runtime.state != DungeonRunState.Ready &&
            runtime.state != DungeonRunState.Running &&
            runtime.state != DungeonRunState.Cleared &&
            runtime.state != DungeonRunState.Failed)
        {
            runtime.state = DungeonRunState.Ready;
        }

        EnsureContractOffer();
        EnsureSelectedContract();
        if (runtime.state == DungeonRunState.Ready)
        {
            runtime.activeContractId = string.Empty;
        }
        else if (!DungeonContractModel.TryGetContract(runtime.activeContractId, out _))
        {
            runtime.activeContractId = runtime.selectedContractId;
        }

        NotifyChanged();
    }

    private void EnsureRuntime()
    {
        if (runtime == null)
        {
            int initialDepth = Mathf.Max(1, depth);
            runtime = new DungeonSaveData
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
        EnsureContractOffer();
        EnsureSelectedContract();
        if (runtime.state == DungeonRunState.Ready)
        {
            runtime.activeContractId = string.Empty;
        }
        else if (!DungeonContractModel.TryGetContract(runtime.activeContractId, out _))
        {
            runtime.activeContractId = runtime.selectedContractId;
        }
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

    private bool IsOfferedContract(string contractId)
    {
        return string.Equals(contractId, runtime.offeredContractIdA, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contractId, runtime.offeredContractIdB, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRewardDepth(int sourceDepth, DungeonContractProfile contract)
    {
        return Mathf.Max(1, sourceDepth + Mathf.Max(0, contract.RewardDepthOffset));
    }

    private int TryUnlockNextDepth()
    {
        int clearedDepth = Mathf.Max(1, runtime.depth);
        if (clearedDepth != runtime.highestUnlockedDepth ||
            runtime.highestUnlockedDepth == int.MaxValue)
        {
            return 0;
        }

        runtime.highestUnlockedDepth += 1;
        return runtime.highestUnlockedDepth;
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
