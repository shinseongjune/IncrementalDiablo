using UnityEngine;

/// <summary>
/// Shared trigger behavior for an authored room exit. The owning room template exposes the concrete
/// ReturnPortal and DeeperExit components; DungeonRoomLoader alone enables them for a cleared room.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class DungeonRoomExit : MonoBehaviour
{
    [SerializeField] private DungeonRoomLoader roomLoader;
    [SerializeField] private bool autoFindRoomLoader = true;
    [SerializeField] private Collider exitTrigger;
    [SerializeField] private GameObject[] activeVisuals = System.Array.Empty<GameObject>();

    private bool isAvailable;

    public bool IsAvailable => isAvailable;
    public abstract string DisplayName { get; }

    protected abstract bool TryUseExit(DungeonRoomLoader loader);

    private void Awake()
    {
        ResolveRoomLoader();
        EnsureTrigger();
    }

    private void OnEnable()
    {
        ResolveRoomLoader();
        EnsureTrigger();
        SetExitAvailable(roomLoader != null && roomLoader.CanUseCurrentRoomExits);
    }

    private void OnValidate()
    {
        activeVisuals ??= System.Array.Empty<GameObject>();
        EnsureTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isAvailable || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        TryUse();
    }

    /// <summary>
    /// Lets the dungeon viewport's point-and-click input select an available exit directly.
    /// Walking through the trigger remains supported for keyboard/controller movement.
    /// </summary>
    public bool TryUse()
    {
        if (!isAvailable)
        {
            return false;
        }

        ResolveRoomLoader();
        return roomLoader != null && TryUseExit(roomLoader);
    }

    public void SetExitAvailable(bool available)
    {
        isAvailable = available;
        EnsureTrigger();

        if (exitTrigger != null)
        {
            exitTrigger.enabled = available;
        }

        for (int i = 0; i < activeVisuals.Length; i++)
        {
            if (activeVisuals[i] != null)
            {
                activeVisuals[i].SetActive(available);
            }
        }
    }

    private void ResolveRoomLoader()
    {
        if (roomLoader == null && autoFindRoomLoader)
        {
            roomLoader = FindAnyObjectByType<DungeonRoomLoader>();
        }
    }

    private void EnsureTrigger()
    {
        exitTrigger ??= GetComponent<Collider>();
        if (exitTrigger != null)
        {
            exitTrigger.isTrigger = true;
        }
    }
}
