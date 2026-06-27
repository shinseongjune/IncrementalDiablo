using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Room Link")]
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private bool autoFindCombatRoom = true;

    [Header("Spawn Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField] private Transform spawnParent;
    [SerializeField] private bool spawnOnRoomStart = true;
    [SerializeField] private bool clearPreviousSpawnsOnRoomReset = true;
    [SerializeField] private bool disableSpawnedEnemiesUntilRoomRuns = true;
    [SerializeField] private bool snapSpawnPointsToNavMesh = true;
    [SerializeField] private float navMeshSpawnSampleRadius = 2f;
    [SerializeField] private int fallbackSpawnCount = 1;
    [SerializeField] private float fallbackSpawnRadius = 2f;
    [SerializeField] private string spawnedNamePrefix = "SpawnedDungeonEnemy";

    [Header("Runtime")]
    [SerializeField] private List<Health> spawnedEnemyHealths = new List<Health>();
    [SerializeField] private string lastSpawnMessage;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private CombatRoom subscribedRoom;
    private int lastSpawnedRoomIndex = int.MinValue;
    private int lastMissingPrefabWarningRoomIndex = int.MinValue;
    private int lastBlockedRoomIndex = int.MinValue;

    public IReadOnlyList<Health> SpawnedEnemyHealths => spawnedEnemyHealths;
    public bool HasEnemyPrefab => enemyPrefab != null;
    public bool HasSpawnedEnemies => HasSpawnedEnemyRecords();
    public string LastSpawnMessage => lastSpawnMessage;

    private void Awake()
    {
        ResolveCombatRoom();
    }

    private void OnEnable()
    {
        ResolveCombatRoom();
        SubscribeToRoom();
        TrySpawnForCurrentRoom();
    }

    private void OnDisable()
    {
        UnsubscribeFromRoom();
    }

    private void OnValidate()
    {
        spawnPoints ??= new Transform[0];
        spawnedEnemyHealths ??= new List<Health>();
        navMeshSpawnSampleRadius = Mathf.Max(0f, navMeshSpawnSampleRadius);
        fallbackSpawnCount = Mathf.Max(1, fallbackSpawnCount);
        fallbackSpawnRadius = Mathf.Max(0f, fallbackSpawnRadius);

        if (string.IsNullOrWhiteSpace(spawnedNamePrefix))
        {
            spawnedNamePrefix = "SpawnedDungeonEnemy";
        }
    }

    public bool SpawnForCurrentRoom()
    {
        ResolveCombatRoom();

        if (combatRoom == null)
        {
            SetLastSpawnMessage("EnemySpawner cannot spawn because no CombatRoom was found.");
            Debug.LogWarning(lastSpawnMessage, this);
            return false;
        }

        int roomIndex = combatRoom.ActiveRoomIndex;
        if (roomIndex < 0)
        {
            SetLastSpawnMessage("EnemySpawner cannot spawn because the CombatRoom has no active room index.");
            Debug.LogWarning(lastSpawnMessage, this);
            return false;
        }

        if (enemyPrefab == null)
        {
            WarnMissingPrefabOnce(roomIndex);
            ReportSpawnBlocker(roomIndex, "EnemySpawner blocked the visible combat path: enemy prefab is not assigned.");
            return false;
        }

        if (!TryValidateEnemyPrefab(out NavMeshAgent templateAgent, out string prefabBlocker))
        {
            ClearPreviousSpawns();
            ReportSpawnBlocker(roomIndex, prefabBlocker);
            Debug.LogWarning(lastSpawnMessage, this);
            return false;
        }

        int spawnCount = ResolveSpawnCount();
        if (!TryResolveSpawnPositions(spawnCount, templateAgent, out List<Vector3> spawnPositions, out string placementBlocker))
        {
            ClearPreviousSpawns();
            ReportSpawnBlocker(roomIndex, placementBlocker);
            Debug.LogWarning(lastSpawnMessage, this);
            return false;
        }

        ClearPreviousSpawns();
        spawnedEnemyHealths.Clear();
        DungeonDepthBalanceProfile balance = combatRoom.DepthBalance;

        for (int i = 0; i < spawnCount; i++)
        {
            Health enemyHealth = SpawnEnemy(i, spawnPositions[i], balance);
            if (enemyHealth != null)
            {
                spawnedEnemyHealths.Add(enemyHealth);
            }
        }

        if (spawnedEnemyHealths.Count == 0)
        {
            ClearPreviousSpawns();
            ReportSpawnBlocker(roomIndex, "EnemySpawner blocked the visible combat path: spawned enemies have no Health component for CombatRoom tracking.");
            Debug.LogWarning(lastSpawnMessage, this);
            return false;
        }

        lastSpawnedRoomIndex = roomIndex;
        lastBlockedRoomIndex = int.MinValue;
        combatRoom.RegisterTrackedEnemies(spawnedEnemyHealths, refill: true);
        SyncSpawnedEnemyActivity();
        string placementText = snapSpawnPointsToNavMesh ? " on NavMesh" : string.Empty;
        DungeonEncounterProfile encounter = combatRoom.ActiveEncounter;
        SetLastSpawnMessage(
            $"EnemySpawner spawned {spawnedEnemyHealths.Count} tracked enemy record(s){placementText} for room {roomIndex + 1} " +
            $"at depth {balance.Depth} with {encounter.DisplayName} (HP x{balance.EnemyHealthMultiplier:0.##}, damage x{balance.EnemyDamageMultiplier:0.##}).");
        return true;
    }

    private void HandleRoomChanged()
    {
        if (combatRoom == null)
        {
            return;
        }

        if (ShouldClearForRoomState())
        {
            ClearPreviousSpawns();
            return;
        }

        TrySpawnForCurrentRoom();
        SyncSpawnedEnemyActivity();
    }

    private void TrySpawnForCurrentRoom()
    {
        if (!spawnOnRoomStart || combatRoom == null)
        {
            return;
        }

        if (combatRoom.State != CombatRoomState.Starting && combatRoom.State != CombatRoomState.Running)
        {
            return;
        }

        if (combatRoom.ActiveRoomIndex == lastBlockedRoomIndex && !HasSpawnedEnemyRecords())
        {
            return;
        }

        if (combatRoom.ActiveRoomIndex == lastSpawnedRoomIndex && HasSpawnedEnemyRecords())
        {
            return;
        }

        SpawnForCurrentRoom();
    }

    private bool ShouldClearForRoomState()
    {
        if (!clearPreviousSpawnsOnRoomReset)
        {
            return false;
        }

        return combatRoom.State == CombatRoomState.Idle ||
               combatRoom.State == CombatRoomState.Cleared ||
               combatRoom.State == CombatRoomState.Failed;
    }

    private Health SpawnEnemy(int spawnIndex, Vector3 position, DungeonDepthBalanceProfile balance)
    {
        Quaternion rotation = ResolveSpawnRotation(spawnIndex);
        Transform parent = spawnParent == null ? transform : spawnParent;
        GameObject spawned = Instantiate(enemyPrefab, position, rotation, parent);
        spawned.name = $"{spawnedNamePrefix}_{spawnIndex + 1:00}";
        spawnedObjects.Add(spawned);

        if (disableSpawnedEnemiesUntilRoomRuns && combatRoom.State != CombatRoomState.Running)
        {
            spawned.SetActive(false);
        }

        Health enemyHealth = spawned.GetComponentInChildren<Health>(includeInactive: true);
        CharacterStats enemyStats = enemyHealth == null ? null : enemyHealth.GetComponent<CharacterStats>();
        enemyStats?.SetRuntimeCombatMultipliers(
            balance.EnemyHealthMultiplier,
            balance.EnemyDamageMultiplier);
        return enemyHealth;
    }

    private bool TryValidateEnemyPrefab(out NavMeshAgent templateAgent, out string blocker)
    {
        templateAgent = enemyPrefab.GetComponentInChildren<NavMeshAgent>(includeInactive: true);

        if (enemyPrefab.GetComponentInChildren<Health>(includeInactive: true) == null)
        {
            blocker = "EnemySpawner blocked the visible combat path: enemy prefab needs a Health component.";
            return false;
        }

        CharacterActor actor = enemyPrefab.GetComponentInChildren<CharacterActor>(includeInactive: true);
        if (actor == null)
        {
            blocker = "EnemySpawner blocked the visible combat path: enemy prefab needs CharacterActor.";
            return false;
        }

        if (actor.Team != CharacterTeam.Enemy)
        {
            blocker = "EnemySpawner blocked the visible combat path: enemy prefab CharacterActor must use the Enemy team.";
            return false;
        }

        if (enemyPrefab.GetComponentInChildren<EnemyAIController>(includeInactive: true) == null)
        {
            blocker = "EnemySpawner blocked the visible combat path: melee enemy prefab needs EnemyAIController.";
            return false;
        }

        if (templateAgent == null || !templateAgent.enabled)
        {
            blocker = "EnemySpawner blocked the visible combat path: melee enemy prefab needs an enabled NavMeshAgent.";
            return false;
        }

        Collider[] colliders = enemyPrefab.GetComponentsInChildren<Collider>(includeInactive: true);
        bool hasEnabledCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled)
            {
                hasEnabledCollider = true;
                break;
            }
        }

        if (!hasEnabledCollider)
        {
            blocker = "EnemySpawner blocked the visible combat path: enemy prefab needs an enabled Collider for player clicks.";
            return false;
        }

        blocker = string.Empty;
        return true;
    }

    private bool TryResolveSpawnPositions(
        int spawnCount,
        NavMeshAgent templateAgent,
        out List<Vector3> spawnPositions,
        out string blocker)
    {
        spawnPositions = new List<Vector3>(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 requestedPosition = ResolveSpawnPosition(i);
            if (!snapSpawnPointsToNavMesh)
            {
                spawnPositions.Add(requestedPosition);
                continue;
            }

            if (!NavMesh.SamplePosition(
                    requestedPosition,
                    out NavMeshHit hit,
                    navMeshSpawnSampleRadius,
                    templateAgent.areaMask))
            {
                blocker =
                    $"EnemySpawner blocked the visible combat path: spawn point {i + 1} has no NavMesh within {navMeshSpawnSampleRadius:0.##} units.";
                return false;
            }

            spawnPositions.Add(hit.position);
        }

        blocker = string.Empty;
        return true;
    }

    private int ResolveSpawnCount()
    {
        int validSpawnPoints = CountValidSpawnPoints();
        return validSpawnPoints > 0 ? validSpawnPoints : fallbackSpawnCount;
    }

    private int CountValidSpawnPoints()
    {
        int count = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private Vector3 ResolveSpawnPosition(int spawnIndex)
    {
        Transform spawnPoint = ResolveSpawnPoint(spawnIndex);
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        if (fallbackSpawnCount <= 1 || Mathf.Approximately(fallbackSpawnRadius, 0f))
        {
            return transform.position;
        }

        float angle = Mathf.PI * 2f * spawnIndex / fallbackSpawnCount;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fallbackSpawnRadius;
        return transform.position + offset;
    }

    private Quaternion ResolveSpawnRotation(int spawnIndex)
    {
        Transform spawnPoint = ResolveSpawnPoint(spawnIndex);
        return spawnPoint == null ? transform.rotation : spawnPoint.rotation;
    }

    private Transform ResolveSpawnPoint(int spawnIndex)
    {
        int seen = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            if (seen == spawnIndex)
            {
                return spawnPoint;
            }

            seen++;
        }

        return null;
    }

    private bool HasSpawnedEnemyRecords()
    {
        for (int i = 0; i < spawnedEnemyHealths.Count; i++)
        {
            if (spawnedEnemyHealths[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void SyncSpawnedEnemyActivity()
    {
        if (!disableSpawnedEnemiesUntilRoomRuns)
        {
            return;
        }

        bool active = combatRoom != null && combatRoom.State == CombatRoomState.Running;
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject spawned = spawnedObjects[i];
            if (spawned != null && spawned.activeSelf != active)
            {
                spawned.SetActive(active);
            }
        }
    }

    private void ClearPreviousSpawns()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject spawned = spawnedObjects[i];
            if (spawned == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(spawned);
            }
            else
            {
                DestroyImmediate(spawned);
            }
        }

        spawnedObjects.Clear();
        spawnedEnemyHealths.Clear();
        lastSpawnedRoomIndex = int.MinValue;
        lastBlockedRoomIndex = int.MinValue;
    }

    private void WarnMissingPrefabOnce(int roomIndex)
    {
        if (lastMissingPrefabWarningRoomIndex == roomIndex)
        {
            return;
        }

        lastMissingPrefabWarningRoomIndex = roomIndex;
        Debug.LogWarning("EnemySpawner needs an enemy prefab before CombatRoom can use prefab-spawned enemies.", this);
    }

    private void ReportSpawnBlocker(int roomIndex, string message)
    {
        lastBlockedRoomIndex = roomIndex;
        SetLastSpawnMessage(message);
        combatRoom?.ReportTrackedEnemySetupBlocker(message);
    }

    private void SetLastSpawnMessage(string message)
    {
        lastSpawnMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    private void ResolveCombatRoom()
    {
        if (combatRoom == null && autoFindCombatRoom)
        {
            combatRoom = GetComponent<CombatRoom>();
        }

        if (combatRoom == null && autoFindCombatRoom)
        {
            combatRoom = GetComponentInParent<CombatRoom>();
        }

        if (combatRoom == null && autoFindCombatRoom)
        {
            combatRoom = FindAnyObjectByType<CombatRoom>();
        }
    }

    private void SubscribeToRoom()
    {
        if (subscribedRoom == combatRoom)
        {
            return;
        }

        UnsubscribeFromRoom();

        if (combatRoom == null)
        {
            return;
        }

        combatRoom.Changed += HandleRoomChanged;
        subscribedRoom = combatRoom;
    }

    private void UnsubscribeFromRoom()
    {
        if (subscribedRoom == null)
        {
            return;
        }

        subscribedRoom.Changed -= HandleRoomChanged;
        subscribedRoom = null;
    }
}
