using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Owns the physical route between an expedition entrance, its ordered combat rooms, and the return point.
/// Combat and reward authority remain with CombatRoom and ExpeditionDirector.
/// </summary>
public class DungeonTraversalController : MonoBehaviour
{
    [Header("Runtime Links")]
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private PlayerController player;
    [SerializeField] private bool autoFindRuntimeLinks = true;

    [Header("Physical Route")]
    [SerializeField] private Transform entranceReturnPoint;
    [SerializeField] private DungeonTraversalRoomNode[] rooms = Array.Empty<DungeonTraversalRoomNode>();
    [SerializeField] private DungeonTraversalTrigger returnTrigger;

    [Header("Runtime")]
    [SerializeField, TextArea] private string lastTraversalMessage;

    private ExpeditionDirector subscribedExpedition;
    private CombatRoom subscribedCombatRoom;

    public string LastTraversalMessage => lastTraversalMessage;
    public int ConfiguredRoomCount => rooms == null ? 0 : rooms.Length;
    public bool HasConfiguredRooms => ConfiguredRoomCount > 0;

    private void Awake()
    {
        ResolveRuntimeLinks();
        combatRoom?.SetExternalStartControl(true);
    }

    private void OnEnable()
    {
        ResolveRuntimeLinks();
        combatRoom?.SetExternalStartControl(true);
        Subscribe();
        RefreshTraversal();
    }

    private void OnDisable()
    {
        Unsubscribe();
        combatRoom?.SetExternalStartControl(false);
    }

    private void Update()
    {
        ResolveRuntimeLinks();
        Subscribe();
    }

    private void OnValidate()
    {
        rooms ??= Array.Empty<DungeonTraversalRoomNode>();

        for (int i = 0; i < rooms.Length; i++)
        {
            rooms[i]?.Validate();
        }
    }

    public bool TryEnterRoom(int roomIndex)
    {
        ResolveRuntimeLinks();

        if (expedition == null || combatRoom == null)
        {
            return SetLastTraversalMessage("Dungeon traversal is missing ExpeditionDirector or CombatRoom.");
        }

        if (!expedition.IsRunning)
        {
            return SetLastTraversalMessage("Enter room blocked: start an expedition at the entrance first.");
        }

        if (!IsConfiguredRoomIndex(roomIndex) || expedition.CurrentRoomIndex != roomIndex)
        {
            return SetLastTraversalMessage("Enter room blocked: this room is not the active expedition room.");
        }

        if (combatRoom.State == CombatRoomState.Starting || combatRoom.State == CombatRoomState.Running)
        {
            return SetLastTraversalMessage("Enter room ignored: combat is already active.");
        }

        if (!combatRoom.BeginRoom())
        {
            return SetLastTraversalMessage("Enter room blocked: CombatRoom rejected the active expedition room.");
        }

        SetLastTraversalMessage($"Entered room {roomIndex + 1}/{ConfiguredRoomCount}.");
        RefreshTraversal();
        return true;
    }

    public bool TryReturnToEntrance()
    {
        ResolveRuntimeLinks();

        if (expedition == null || player == null || entranceReturnPoint == null)
        {
            return SetLastTraversalMessage("Return blocked: assign ExpeditionDirector, Player, and Entrance Return Point.");
        }

        if (expedition.State != DungeonRunState.Cleared && expedition.State != DungeonRunState.Failed)
        {
            return SetLastTraversalMessage("Return blocked: clear or fail the expedition before using the exit.");
        }

        if (expedition.RewardPending && !expedition.TryGrantPendingReward())
        {
            return SetLastTraversalMessage("Return blocked: the pending dungeon reward could not be granted.");
        }

        WarpPlayerToEntrance();
        expedition.ResetToReady();
        SetLastTraversalMessage("Returned to the dungeon entrance.");
        RefreshTraversal();
        return true;
    }

    /// <summary>
    /// Returns only the spawn markers for the requested physical room. EnemySpawner keeps
    /// ownership of instantiation and combat tracking, while this route keeps room location data.
    /// </summary>
    public bool TryGetRoomSpawnPoints(int roomIndex, out Transform[] spawnPoints)
    {
        spawnPoints = Array.Empty<Transform>();

        if (!IsConfiguredRoomIndex(roomIndex) || rooms[roomIndex] == null)
        {
            return false;
        }

        spawnPoints = rooms[roomIndex].SpawnPoints;
        return true;
    }

    public bool TryValidateContract(out string message)
    {
        ResolveRuntimeLinks();

        if (expedition == null || combatRoom == null || player == null)
        {
            message = "Assign ExpeditionDirector, CombatRoom, and Player.";
            return false;
        }

        if (entranceReturnPoint == null || returnTrigger == null)
        {
            message = "Assign Entrance Return Point and a ReturnToEntrance trigger.";
            return false;
        }

        if (ConfiguredRoomCount != expedition.TotalRooms)
        {
            message = $"Configure {expedition.TotalRooms} ordered room nodes; currently {ConfiguredRoomCount}.";
            return false;
        }

        for (int i = 0; i < ConfiguredRoomCount; i++)
        {
            DungeonTraversalRoomNode room = rooms[i];
            if (room == null || room.EntryTrigger == null)
            {
                message = $"Room node {i + 1} needs an EnterRoom trigger.";
                return false;
            }

            if (room.EntryTrigger.Action != DungeonTraversalTrigger.TriggerAction.EnterRoom ||
                room.EntryTrigger.RoomIndex != i)
            {
                message = $"Room node {i + 1} needs an EnterRoom trigger with room index {i}.";
                return false;
            }

            if (!room.EntryTrigger.TryValidateTrigger(out message))
            {
                return false;
            }

            if (room.SpawnPointCount == 0)
            {
                message = $"Room node {i + 1} needs at least one room-local enemy spawn marker.";
                return false;
            }

            if (room.ExitBlocker == null)
            {
                message = $"Room node {i + 1} needs an Exit Blocker for the next route state.";
                return false;
            }
        }

        if (returnTrigger.Action != DungeonTraversalTrigger.TriggerAction.ReturnToEntrance)
        {
            message = "Assign a valid ReturnToEntrance trigger.";
            return false;
        }

        if (!returnTrigger.TryValidateTrigger(out message))
        {
            return false;
        }

        message = $"Traversal contract ready: {ConfiguredRoomCount} room(s) plus return route.";
        return true;
    }

    [ContextMenu("Validate Traversal Contract")]
    private void ValidateTraversalContract()
    {
        bool valid = TryValidateContract(out string message);
        if (valid)
        {
            Debug.Log(message, this);
            return;
        }

        Debug.LogWarning(message, this);
    }

    private void HandleExpeditionChanged()
    {
        RefreshTraversal();
    }

    private void HandleCombatRoomResolved(CombatRoomResult result)
    {
        if (result.resolution == CombatRoomResolution.Cleared &&
            IsConfiguredRoomIndex(result.roomIndex) &&
            rooms[result.roomIndex].ExitBlocker != null)
        {
            rooms[result.roomIndex].ExitBlocker.SetActive(false);
        }

        RefreshTraversal();
    }

    private void RefreshTraversal()
    {
        bool expeditionRunning = expedition != null && expedition.IsRunning;
        int activeRoomIndex = expeditionRunning ? expedition.CurrentRoomIndex : -1;
        bool combatActive = combatRoom != null &&
            (combatRoom.State == CombatRoomState.Starting || combatRoom.State == CombatRoomState.Running);

        for (int i = 0; i < ConfiguredRoomCount; i++)
        {
            DungeonTraversalRoomNode room = rooms[i];
            if (room == null)
            {
                continue;
            }

            bool cleared = expedition != null && expedition.RoomsCompleted > i;
            if (room.ExitBlocker != null)
            {
                room.ExitBlocker.SetActive(!cleared);
            }

            bool canEnter = expeditionRunning && i == activeRoomIndex && !combatActive;
            room.EntryTrigger?.SetTraversalEnabled(canEnter);
        }

        bool canReturn = expedition != null &&
            (expedition.State == DungeonRunState.Cleared || expedition.State == DungeonRunState.Failed);
        returnTrigger?.SetTraversalEnabled(canReturn);
    }

    private void ResolveRuntimeLinks()
    {
        if (!autoFindRuntimeLinks)
        {
            return;
        }

        expedition ??= FindAnyObjectByType<ExpeditionDirector>();
        combatRoom ??= FindAnyObjectByType<CombatRoom>();
        player ??= FindAnyObjectByType<PlayerController>();
    }

    private void Subscribe()
    {
        if (subscribedExpedition != expedition)
        {
            if (subscribedExpedition != null)
            {
                subscribedExpedition.Changed -= HandleExpeditionChanged;
            }

            subscribedExpedition = expedition;
            if (subscribedExpedition != null)
            {
                subscribedExpedition.Changed += HandleExpeditionChanged;
            }
        }

        if (subscribedCombatRoom != combatRoom)
        {
            if (subscribedCombatRoom != null)
            {
                subscribedCombatRoom.Resolved -= HandleCombatRoomResolved;
            }

            subscribedCombatRoom = combatRoom;
            if (subscribedCombatRoom != null)
            {
                subscribedCombatRoom.Resolved += HandleCombatRoomResolved;
            }
        }
    }

    private void Unsubscribe()
    {
        if (subscribedExpedition != null)
        {
            subscribedExpedition.Changed -= HandleExpeditionChanged;
            subscribedExpedition = null;
        }

        if (subscribedCombatRoom != null)
        {
            subscribedCombatRoom.Resolved -= HandleCombatRoomResolved;
            subscribedCombatRoom = null;
        }
    }

    private void WarpPlayerToEntrance()
    {
        Transform playerTransform = player.transform;
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(entranceReturnPoint.position);
        }
        else
        {
            playerTransform.position = entranceReturnPoint.position;
        }

        playerTransform.rotation = entranceReturnPoint.rotation;
    }

    private bool IsConfiguredRoomIndex(int roomIndex)
    {
        return roomIndex >= 0 && roomIndex < ConfiguredRoomCount;
    }

    private bool SetLastTraversalMessage(string message)
    {
        lastTraversalMessage = message;
        return false;
    }
}

[Serializable]
public sealed class DungeonTraversalRoomNode
{
    [SerializeField] private DungeonTraversalTrigger entryTrigger;
    [SerializeField] private GameObject exitBlocker;
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();

    public DungeonTraversalTrigger EntryTrigger => entryTrigger;
    public GameObject ExitBlocker => exitBlocker;
    public Transform[] SpawnPoints => spawnPoints ?? Array.Empty<Transform>();

    public int SpawnPointCount
    {
        get
        {
            int count = 0;
            Transform[] configuredSpawnPoints = SpawnPoints;
            for (int i = 0; i < configuredSpawnPoints.Length; i++)
            {
                if (configuredSpawnPoints[i] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public void Validate()
    {
        spawnPoints ??= Array.Empty<Transform>();
    }
}
