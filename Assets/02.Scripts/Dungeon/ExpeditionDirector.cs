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

        runtime.state = DungeonRunState.Running;
        runtime.dungeonId = dungeonId;
        runtime.depth = SelectedDepth;
        runtime.totalRooms = Mathf.Max(1, totalRooms);
        runtime.currentRoomIndex = 0;
        runtime.roomsCompleted = 0;
        runtime.elapsedSeconds = 0f;
        runtime.rewardPending = false;
        runtime.lastResult = "Expedition started";
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
        runtime.lastResult = "Expedition failed";
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

        if (!lootDropper.TryGrantClearReward(Depth, out ItemInstance item))
        {
            runtime.lastResult = string.IsNullOrWhiteSpace(lootDropper.LastDropMessage)
                ? "Reward pending: loot grant failed"
                : lootDropper.LastDropMessage;
            NotifyChanged();
            return false;
        }

        runtime.rewardPending = false;
        runtime.lastResult = $"Reward granted: {item.DisplayName}";
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
}
