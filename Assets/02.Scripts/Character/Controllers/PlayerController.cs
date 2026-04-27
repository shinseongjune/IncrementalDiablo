using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterActor))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask clickMask = ~0;
    [SerializeField] private float rayDistance = 200f;
    [SerializeField] private float attackMoveRefreshInterval = 0.15f;

    private CharacterActor actor;
    private Health attackTarget;
    private float nextAttackMoveRefreshTime;

    private void Awake()
    {
        actor = GetComponent<CharacterActor>();

        if (inputCamera == null)
        {
            inputCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (WasPrimaryClickPressed())
        {
            HandlePrimaryClick(GetPointerPosition());
        }

        TickAttackTarget();
    }

    private void HandlePrimaryClick(Vector2 screenPosition)
    {
        if (inputCamera == null)
        {
            Debug.LogWarning("PlayerController needs a camera to resolve clicks.", this);
            return;
        }

        Ray ray = inputCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, clickMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Health clickedHealth = hit.collider.GetComponentInParent<Health>();
        if (IsValidAttackTarget(clickedHealth))
        {
            SetAttackTarget(clickedHealth);
            return;
        }

        attackTarget = null;
        actor.Motor.TryMoveTo(hit.point);
    }

    private void TickAttackTarget()
    {
        if (!IsValidAttackTarget(attackTarget))
        {
            attackTarget = null;
            return;
        }

        if (actor.Combat.TryBasicAttack(attackTarget))
        {
            actor.Motor.Stop();
            return;
        }

        if (Time.time < nextAttackMoveRefreshTime)
        {
            return;
        }

        nextAttackMoveRefreshTime = Time.time + attackMoveRefreshInterval;
        actor.Motor.TryMoveTo(attackTarget.transform.position);
    }

    private void SetAttackTarget(Health target)
    {
        attackTarget = target;
        nextAttackMoveRefreshTime = 0f;
        TickAttackTarget();
    }

    private bool IsValidAttackTarget(Health target)
    {
        if (target == null || !target.IsAlive || target == actor.Health)
        {
            return false;
        }

        CharacterActor targetActor = target.GetComponent<CharacterActor>();
        if (targetActor == null)
        {
            return true;
        }

        return targetActor.Team != actor.Team;
    }

    private bool WasPrimaryClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }
}
