using System;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private bool ignoreClicksOverUi = true;
    [SerializeField] private string lastClickMessage = "Ready";

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

    public string LastClickMessage => lastClickMessage;

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
            if (!ignoreClicksOverUi || !IsPointerOverUi())
            {
                HandlePrimaryClick(GetPointerPosition());
            }
        }

        TickPendingAttack();
    }

    private void HandlePrimaryClick(Vector2 screenPosition)
    {
        if (inputCamera == null)
        {
            lastClickMessage = "Click ignored: PlayerController needs a camera.";
            Debug.LogWarning(lastClickMessage, this);
            return;
        }

        Ray ray = inputCamera.ScreenPointToRay(screenPosition);
        HandlePrimaryClickRay(ray, IsStationaryAttackHeld());
    }

    public bool HandlePrimaryClickRay(Ray ray, bool stationaryAttack)
    {
        if (!TryResolveClick(ray, out RaycastHit hit, out Health clickedHealth))
        {
            lastClickMessage = "Click ignored: no valid dungeon surface or enemy hit.";
            return false;
        }

        if (stationaryAttack)
        {
            SetStationaryAttack(clickedHealth, hit.point);
            lastClickMessage = clickedHealth == null
                ? $"Stationary attack toward {FormatPoint(hit.point)}."
                : $"Stationary attack target: {clickedHealth.name}.";
            return true;
        }

        if (IsValidAttackTarget(clickedHealth))
        {
            SetTargetAttack(clickedHealth);
            lastClickMessage = $"Attack target: {clickedHealth.name}.";
            return true;
        }

        ClearAttackCommand();
        lastClickMessage = actor.Motor.TryMoveTo(hit.point)
            ? $"Move to {FormatPoint(hit.point)}."
            : $"Move failed: no NavMesh point near {FormatPoint(hit.point)}.";
        return true;
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
                lastClickMessage = $"Stationary attack hit: {pendingAttackTarget.name}.";
                ClearAttackCommand();
            }

            return;
        }

        if (IsValidAttackTarget(pendingAttackTarget))
        {
            if (actor.Combat.TryPlayBasicAttackInPlace())
            {
                lastClickMessage = $"Stationary attack waiting for range: {pendingAttackTarget.name}.";
            }

            return;
        }

        if (actor.Combat.TryPlayBasicAttackInPlace())
        {
            lastClickMessage = $"Stationary attack toward {FormatPoint(stationaryAttackPoint)}.";
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

    private bool TryResolveClick(Ray ray, out RaycastHit resolvedHit, out Health attackTarget)
    {
        resolvedHit = default(RaycastHit);
        attackTarget = null;

        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, clickMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, CompareRaycastHitsByDistance);

        bool hasMovementHit = false;
        RaycastHit movementHit = default(RaycastHit);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Health clickedHealth = hit.collider.GetComponentInParent<Health>();

            if (IsValidAttackTarget(clickedHealth))
            {
                resolvedHit = hit;
                attackTarget = clickedHealth;
                return true;
            }

            if (!hasMovementHit && clickedHealth == null)
            {
                movementHit = hit;
                hasMovementHit = true;
            }
        }

        if (!hasMovementHit)
        {
            return false;
        }

        resolvedHit = movementHit;
        return true;
    }

    private static int CompareRaycastHitsByDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
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

    private static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId) ||
                   EventSystem.current.IsPointerOverGameObject();
        }
#endif

        return EventSystem.current.IsPointerOverGameObject();
    }

    private static string FormatPoint(Vector3 point)
    {
        return $"{point.x:0.0}, {point.y:0.0}, {point.z:0.0}";
    }
}
