using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private float navMeshSampleRadius = 2f;

    private NavMeshAgent agent;
    private CharacterStats stats;

    public bool IsOnNavMesh => agent != null && agent.isOnNavMesh;
    public bool HasPath => agent != null && agent.hasPath;
    public float RemainingDistance => agent == null ? 0f : agent.remainingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();
        RefreshMoveSpeed();
    }

    private void OnValidate()
    {
        navMeshSampleRadius = Mathf.Max(0f, navMeshSampleRadius);
    }

    public void MoveTo(Vector3 position)
    {
        TryMoveTo(position);
    }

    public bool TryMoveTo(Vector3 position)
    {
        if (!IsOnNavMesh)
        {
            return false;
        }

        if (!TryFindNearestNavMeshPoint(position, out Vector3 navMeshPosition))
        {
            return false;
        }

        return agent.SetDestination(navMeshPosition);
    }

    public bool TryFindNearestNavMeshPoint(Vector3 position, out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = position;
        return false;
    }

    public void Stop()
    {
        if (!IsOnNavMesh)
        {
            return;
        }

        agent.ResetPath();
    }

    public bool IsNearDestination(float distance = 0.15f)
    {
        if (!IsOnNavMesh || agent.pathPending)
        {
            return false;
        }

        return !agent.hasPath || agent.remainingDistance <= distance;
    }

    public void RefreshMoveSpeed()
    {
        if (agent == null || stats == null)
        {
            return;
        }

        agent.speed = stats.GetValue(StatId.MoveSpeed);
    }
}
