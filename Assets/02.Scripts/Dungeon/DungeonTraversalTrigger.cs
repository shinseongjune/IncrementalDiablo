using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DungeonTraversalTrigger : MonoBehaviour
{
    public enum TriggerAction
    {
        EnterRoom,
        ReturnToEntrance
    }

    [SerializeField] private DungeonTraversalController traversal;
    [SerializeField] private bool autoFindTraversal = true;
    [SerializeField] private TriggerAction action = TriggerAction.EnterRoom;
    [SerializeField, Min(0)] private int roomIndex;
    [SerializeField] private Collider triggerCollider;

    public TriggerAction Action => action;
    public int RoomIndex => roomIndex;

    private void Awake()
    {
        ResolveReferences();
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        roomIndex = Mathf.Max(0, roomIndex);
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        ResolveReferences();
        if (traversal == null)
        {
            return;
        }

        if (action == TriggerAction.EnterRoom)
        {
            traversal.TryEnterRoom(roomIndex);
            return;
        }

        traversal.TryReturnToEntrance();
    }

    public void SetTraversalEnabled(bool enabled)
    {
        EnsureTriggerCollider();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = enabled;
        }
    }

    private void ResolveReferences()
    {
        if (traversal == null && autoFindTraversal)
        {
            traversal = FindAnyObjectByType<DungeonTraversalController>();
        }
    }

    private void EnsureTriggerCollider()
    {
        triggerCollider ??= GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
