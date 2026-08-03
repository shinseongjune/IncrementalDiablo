using System;
using UnityEngine;

/// <summary>
/// Authored root contract for one additive dungeon room Scene. This component owns locations only;
/// DungeonRoomLoader owns Scene lifetime and ExpeditionDirector owns run/save state.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonRoomTemplate : MonoBehaviour
{
    [Header("Template Identity")]
    [SerializeField] private string templateId = "prototype_crypt";

    [Header("Required Route Anchors")]
    [SerializeField] private Transform entrancePoint;
    [SerializeField] private Transform returnPortalPoint;
    [SerializeField] private Transform deeperExitPoint;
    [SerializeField] private ReturnPortal returnPortal;
    [SerializeField] private DeeperExit deeperExit;

    [Header("Deterministic Placement Anchors")]
    [SerializeField] private Transform[] enemySpawnAnchors = Array.Empty<Transform>();
    [SerializeField] private Transform[] propAnchors = Array.Empty<Transform>();
    [SerializeField] private Transform[] obstacleAnchors = Array.Empty<Transform>();

    public string TemplateId => templateId;
    public Transform EntrancePoint => entrancePoint;
    public Transform ReturnPortalPoint => returnPortalPoint;
    public Transform DeeperExitPoint => deeperExitPoint;
    public ReturnPortal ReturnPortal => returnPortal;
    public DeeperExit DeeperExit => deeperExit;
    public Transform[] EnemySpawnAnchors => enemySpawnAnchors ?? Array.Empty<Transform>();
    public Transform[] PropAnchors => propAnchors ?? Array.Empty<Transform>();
    public Transform[] ObstacleAnchors => obstacleAnchors ?? Array.Empty<Transform>();

    private void OnValidate()
    {
        templateId = string.IsNullOrWhiteSpace(templateId) ? string.Empty : templateId.Trim();
        enemySpawnAnchors ??= Array.Empty<Transform>();
        propAnchors ??= Array.Empty<Transform>();
        obstacleAnchors ??= Array.Empty<Transform>();
    }

    public bool TryValidate(out string message)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            message = "DungeonRoomTemplate needs a Template Id.";
            return false;
        }

        if (entrancePoint == null || returnPortalPoint == null || deeperExitPoint == null)
        {
            message = $"DungeonRoomTemplate '{templateId}' needs Entrance, Return Portal, and Deeper Exit anchors.";
            return false;
        }

        if (returnPortal == null || deeperExit == null)
        {
            message = $"DungeonRoomTemplate '{templateId}' needs ReturnPortal and DeeperExit trigger components.";
            return false;
        }

        if (!HasAnyAnchor(EnemySpawnAnchors))
        {
            message = $"DungeonRoomTemplate '{templateId}' needs at least one enemy spawn anchor.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool HasAnyAnchor(Transform[] anchors)
    {
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null)
            {
                return true;
            }
        }

        return false;
    }
}
