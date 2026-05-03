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
    [SerializeField] private float chaseRefreshInterval = 0.15f;

    private CharacterActor actor;
    private Health pendingAttackTarget;
    private AttackCommandMode attackCommandMode;
    private Vector3 stationaryAttackPoint;
    private float nextChaseRefreshTime;

    private enum AttackCommandMode
    {
        None,
        ChaseTarget,
        Stationary
    }

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

        TickPendingAttack();
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
        bool stationaryAttack = IsStationaryAttackHeld();

        if (stationaryAttack)
        {
            SetStationaryAttack(clickedHealth, hit.point);
            return;
        }

        if (IsValidAttackTarget(clickedHealth))
        {
            SetTargetAttack(clickedHealth);
            return;
        }

        ClearAttackCommand();
        actor.Motor.TryMoveTo(hit.point);
    }

    private void TickPendingAttack()
    {
        switch (attackCommandMode)
        {
            case AttackCommandMode.ChaseTarget:
                TickChaseAttack();
                break;
            case AttackCommandMode.Stationary:
                TickStationaryAttack();
                break;
        }
    }

    private void TickChaseAttack()
    {
        if (!IsValidAttackTarget(pendingAttackTarget))
        {
            ClearAttackCommand();
            return;
        }

        if (actor.Combat.IsInRange(pendingAttackTarget.transform))
        {
            actor.Motor.Stop();
            actor.Motor.FaceToward(pendingAttackTarget.transform.position);

            if (actor.Combat.TryBasicAttack(pendingAttackTarget))
            {
                ClearAttackCommand();
            }

            return;
        }

        if (Time.time < nextChaseRefreshTime)
        {
            return;
        }

        nextChaseRefreshTime = Time.time + chaseRefreshInterval;
        actor.Motor.TryMoveTo(pendingAttackTarget.transform.position);
    }

    private void TickStationaryAttack()
    {
        actor.Motor.Stop();
        Vector3 facePoint = IsValidAttackTarget(pendingAttackTarget)
            ? pendingAttackTarget.transform.position
            : stationaryAttackPoint;
        actor.Motor.FaceToward(facePoint);

        if (IsValidAttackTarget(pendingAttackTarget) && actor.Combat.IsInRange(pendingAttackTarget.transform))
        {
            if (actor.Combat.TryBasicAttack(pendingAttackTarget))
            {
                ClearAttackCommand();
            }

            return;
        }

        if (actor.Combat.TryPlayBasicAttackInPlace())
        {
            ClearAttackCommand();
        }
    }

    private void SetTargetAttack(Health target)
    {
        pendingAttackTarget = target;
        attackCommandMode = AttackCommandMode.ChaseTarget;
        nextChaseRefreshTime = 0f;
        TickChaseAttack();
    }

    private void SetStationaryAttack(Health target, Vector3 point)
    {
        pendingAttackTarget = IsValidAttackTarget(target) ? target : null;
        stationaryAttackPoint = pendingAttackTarget == null ? point : pendingAttackTarget.transform.position;
        attackCommandMode = AttackCommandMode.Stationary;
        actor.Motor.Stop();
        TickStationaryAttack();
    }

    private void ClearAttackCommand()
    {
        pendingAttackTarget = null;
        attackCommandMode = AttackCommandMode.None;
        nextChaseRefreshTime = 0f;
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

    private bool IsStationaryAttackHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}
