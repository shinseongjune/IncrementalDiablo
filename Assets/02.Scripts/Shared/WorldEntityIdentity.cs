using UnityEngine;

/// <summary>
/// Runtime-only stable identity for a generated or spawned actor. Saved snapshots use this value
/// to reconnect action targets after all entities have been created during the second restore pass.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldEntityIdentity : MonoBehaviour
{
    [SerializeField] private string entityId;

    public string EntityId => entityId;

    public void Configure(string nextEntityId)
    {
        entityId = string.IsNullOrWhiteSpace(nextEntityId) ? string.Empty : nextEntityId.Trim();
    }
}
