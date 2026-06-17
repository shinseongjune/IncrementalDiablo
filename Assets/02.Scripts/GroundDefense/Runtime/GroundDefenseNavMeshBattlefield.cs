using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public sealed class GroundDefenseNavMeshBattlefield : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DefenseDirector defense;
    [SerializeField] private Camera defenseCamera;
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Texture2D readabilitySheet;

    [Header("Battlefield")]
    [SerializeField, Min(3f)] private float battlefieldWidth = 8f;
    [SerializeField, Min(0.05f)] private float groundHeight = 0.18f;
    [SerializeField, Min(0f)] private float endPadding = 1.5f;
    [SerializeField] private Color groundColor = new Color(0.16f, 0.12f, 0.08f, 1f);

    [Header("Forces")]
    [SerializeField, Min(1)] private int defenderCount = 2;
    [SerializeField, Min(1)] private int enemyCount = 3;
    [SerializeField, Min(0.2f)] private float unitSpacing = 1.5f;
    [SerializeField, Min(1f)] private float defenderDistanceFromWall = 4f;
    [SerializeField, Min(0.2f)] private float wallAttackDistance = 1.25f;
    [SerializeField, Min(0.5f)] private float defenderEngageRadius = 7f;

    [Header("Formula Scale")]
    [SerializeField] private bool useFormulaDrivenForceScale = true;
    [SerializeField, Min(1)] private int maxFormulaDefenders = 4;
    [SerializeField, Min(1)] private int maxFormulaEnemies = 6;
    [SerializeField, Min(1)] private int extraEnemyEveryLevelsInBand = 5;
    [SerializeField, Range(0f, 1f)] private float formulaCadenceStrength = 0.28f;
    [SerializeField, Min(0.1f)] private float minimumEnemyRespawnSeconds = 0.9f;
    [SerializeField, Min(0.1f)] private float minimumDefenderRespawnSeconds = 1.6f;
    [SerializeField] private GroundDefenseNavMeshEnemyRole[] enemyRoles;

    [Header("Defender Stats")]
    [SerializeField, Min(1f)] private float defenderHealth = 90f;
    [SerializeField, Min(0f)] private float defenderMoveSpeed = 3.2f;
    [SerializeField, Min(0f)] private float defenderDamage = 12f;
    [SerializeField, Min(0.1f)] private float defenderAttackRange = 1.5f;
    [SerializeField, Min(0.05f)] private float defenderAttackCooldown = 0.85f;

    [Header("Enemy Stats")]
    [SerializeField, Min(1f)] private float enemyHealth = 55f;
    [SerializeField, Min(0f)] private float enemyMoveSpeed = 2.8f;
    [SerializeField, Min(0f)] private float enemyDamage = 8f;
    [SerializeField, Min(0.1f)] private float enemyAttackRange = 1.35f;
    [SerializeField, Min(0.05f)] private float enemyAttackCooldown = 1.05f;

    [Header("Respawn")]
    [SerializeField, Min(0.1f)] private float enemyRespawnSeconds = 2.2f;
    [SerializeField, Min(0.1f)] private float defenderRespawnSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float defeatedBodySeconds = 0.65f;

    [Header("Role Visuals")]
    [SerializeField] private Rect enemyUv = new Rect(0f, 0.5f, 0.33333334f, 0.5f);
    [SerializeField] private Rect defenderUv = new Rect(0f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Rect wallUv = new Rect(0.6666667f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Vector2 enemyVisualSize = new Vector2(1.65f, 2.35f);
    [SerializeField] private Vector2 defenderVisualSize = new Vector2(1.55f, 2.2f);
    [SerializeField] private Vector2 wallVisualSize = new Vector2(4.8f, 3.5f);
    [SerializeField, Min(0f)] private float enemyVisualHeight = 1.18f;
    [SerializeField, Min(0f)] private float defenderVisualHeight = 1.1f;
    [SerializeField, Min(0f)] private float wallVisualHeight = 1.65f;

    [Header("Readable Ownership")]
    [SerializeField] private Color enemyOwnershipColor = new Color(1f, 0.16f, 0.08f, 1f);
    [SerializeField] private Color defenderOwnershipColor = new Color(0.15f, 0.42f, 1f, 1f);
    [SerializeField, Min(0.05f)] private float ownershipBaseRadius = 0.62f;
    [SerializeField, Min(0.01f)] private float ownershipBaseHeight = 0.04f;
    [SerializeField] private Vector2 ownershipBadgeSize = new Vector2(0.5f, 0.64f);
    [SerializeField, Min(0f)] private float ownershipBadgeHeight = 2.25f;

    private readonly List<GroundDefenseNavMeshUnit> defenders =
        new List<GroundDefenseNavMeshUnit>();
    private readonly List<GroundDefenseNavMeshUnit> enemies =
        new List<GroundDefenseNavMeshUnit>();

    private GameObject generatedRoot;
    private NavMeshSurface navMeshSurface;
    private GroundDefenseBillboardHandle wallVisual;
    private Material groundMaterial;
    private Vector3 travelDirection;
    private Vector3 sideDirection;
    private GroundDefenseVisualForceProfile activeForceProfile;
    private bool shuttingDown;

    public Vector3 WallPosition => wallAnchor == null ? transform.position : wallAnchor.position;
    public Vector3 WallApproachPosition =>
        WallPosition - travelDirection * Mathf.Max(0.2f, wallAttackDistance);
    public int ActiveDefenderCount => CountAlive(defenders);
    public int ActiveEnemyCount => CountAlive(enemies);

    private void Start()
    {
        ResolveReferences();
        BuildBattlefield();
    }

    private void OnValidate()
    {
        defenderCount = Mathf.Max(1, defenderCount);
        enemyCount = Mathf.Max(1, enemyCount);
        maxFormulaDefenders = Mathf.Max(defenderCount, maxFormulaDefenders);
        maxFormulaEnemies = Mathf.Max(enemyCount, maxFormulaEnemies);
        extraEnemyEveryLevelsInBand = Mathf.Max(1, extraEnemyEveryLevelsInBand);
        formulaCadenceStrength = Mathf.Clamp01(formulaCadenceStrength);
        minimumEnemyRespawnSeconds = Mathf.Max(0.1f, minimumEnemyRespawnSeconds);
        minimumDefenderRespawnSeconds = Mathf.Max(0.1f, minimumDefenderRespawnSeconds);

        if (enemyRoles == null || enemyRoles.Length == 0)
        {
            enemyRoles = CreateDefaultEnemyRoles();
        }

        for (int i = 0; i < enemyRoles.Length; i++)
        {
            enemyRoles[i]?.Validate();
        }
    }

    private void OnDisable()
    {
        shuttingDown = true;
        ClearBattlefield();
    }

    private void Update()
    {
        if (wallVisual == null || defense == null || defense.Runtime == null)
        {
            return;
        }

        RefreshFormulaForceScale();

        float wallHealth = defense.Runtime.WallHealthPercent;
        wallVisual.Renderer.transform.localScale =
            Vector3.one * Mathf.Lerp(0.92f, 1f, wallHealth);
        if (wallVisual.Renderer is SpriteRenderer spriteRenderer)
        {
            spriteRenderer.color = Color.Lerp(
                new Color(0.55f, 0.12f, 0.08f, 1f),
                Color.white,
                wallHealth);
        }
    }

    public GroundDefenseNavMeshUnit FindNearestEnemy(
        Vector3 position,
        Vector3 defenderHome)
    {
        GroundDefenseNavMeshUnit nearest = FindNearestAlive(enemies, position);
        if (nearest == null ||
            Vector3.Distance(nearest.transform.position, defenderHome) > defenderEngageRadius)
        {
            return null;
        }

        return nearest;
    }

    public GroundDefenseNavMeshUnit FindNearestDefender(Vector3 position)
    {
        return FindNearestAlive(defenders, position);
    }

    public void ApplyEnemyWallHit(float damage)
    {
        defense?.ApplyBattlefieldWallDamage(Mathf.Max(0f, damage));
        if (wallVisual != null &&
            wallVisual.Renderer is SpriteRenderer spriteRenderer)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.18f, 1f);
        }
    }

    public void NotifyUnitDefeated(GroundDefenseNavMeshUnit unit)
    {
        if (unit == null || shuttingDown)
        {
            return;
        }

        StartCoroutine(ReplaceDefeatedUnit(unit));
    }

    private void BuildBattlefield()
    {
        ClearBattlefield();
        shuttingDown = false;
        ResolveReferences();

        if (enemySpawnAnchor == null ||
            wallAnchor == null ||
            readabilitySheet == null)
        {
            Debug.LogWarning(
                "GroundDefenseNavMeshBattlefield needs enemy/wall anchors and the readability sheet.",
                this);
            return;
        }

        activeForceProfile = EvaluateVisualForceProfile();
        defenseCamera = GroundDefenseBillboardUtility.FindDefenseCamera(defenseCamera);
        travelDirection = wallAnchor.position - enemySpawnAnchor.position;
        travelDirection.y = 0f;
        travelDirection = travelDirection.sqrMagnitude <= 0.0001f
            ? Vector3.left
            : travelDirection.normalized;
        sideDirection = Vector3.Cross(Vector3.up, travelDirection).normalized;

        generatedRoot = new GameObject("GroundDefense_NavMeshBattlefield");
        generatedRoot.transform.SetParent(transform, false);

        BuildGroundAndNavMesh();
        BuildWall();

        for (int i = 0; i < activeForceProfile.DefenderCount; i++)
        {
            SpawnUnit(
                GroundDefenseNavMeshUnitSide.Defender,
                i,
                activeForceProfile.DefenderCount);
        }

        for (int i = 0; i < activeForceProfile.EnemyCount; i++)
        {
            SpawnUnit(
                GroundDefenseNavMeshUnitSide.Enemy,
                i,
                activeForceProfile.EnemyCount);
        }
    }

    private void BuildGroundAndNavMesh()
    {
        Vector3 start = enemySpawnAnchor.position - travelDirection * endPadding;
        Vector3 end = wallAnchor.position + travelDirection * endPadding;
        float length = Vector3.Distance(start, end);
        Vector3 center = (start + end) * 0.5f;
        center.y = enemySpawnAnchor.position.y - groundHeight * 0.5f;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "BattlefieldGround";
        ground.transform.SetParent(generatedRoot.transform, true);
        ground.transform.position = center;
        ground.transform.rotation = Quaternion.FromToRotation(Vector3.right, travelDirection);
        ground.transform.localScale = new Vector3(length, groundHeight, battlefieldWidth);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard");
        groundMaterial = new Material(shader)
        {
            name = "GroundDefenseBattlefieldGround_Runtime",
            color = groundColor
        };
        if (groundMaterial.HasProperty("_BaseColor"))
        {
            groundMaterial.SetColor("_BaseColor", groundColor);
        }

        groundRenderer.sharedMaterial = groundMaterial;
        groundRenderer.shadowCastingMode = ShadowCastingMode.On;
        groundRenderer.receiveShadows = true;

        navMeshSurface = generatedRoot.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.layerMask = ~0;
        navMeshSurface.BuildNavMesh();
    }

    private void BuildWall()
    {
        wallVisual = GroundDefenseBillboardUtility.CreateBillboard(
            "DefenseWall",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            wallUv,
            wallVisualSize,
            Color.white,
            20);
        wallVisual.Root.transform.position =
            wallAnchor.position + Vector3.up * wallVisualHeight;
    }

    private void SpawnUnit(
        GroundDefenseNavMeshUnitSide side,
        int slotIndex,
        int slotCount)
    {
        Vector3 desiredPosition = GetSpawnPosition(side, slotIndex, slotCount);
        if (!NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            Debug.LogWarning(
                $"Ground defense could not place {side} slot {slotIndex + 1} on its NavMesh.",
                this);
            return;
        }

        string unitName = side == GroundDefenseNavMeshUnitSide.Defender
            ? $"Defender_{slotIndex + 1:00}"
            : $"Enemy_{slotIndex + 1:00}";
        GameObject unitObject = new GameObject(unitName);
        unitObject.transform.SetParent(generatedRoot.transform, true);
        unitObject.transform.position = hit.position;

        CharacterStats stats = unitObject.AddComponent<CharacterStats>();
        Health health = unitObject.AddComponent<Health>();
        NavMeshAgent agent = unitObject.AddComponent<NavMeshAgent>();
        unitObject.AddComponent<CharacterMotor>();
        unitObject.AddComponent<CombatDriver>();
        unitObject.AddComponent<EquipmentSlots>();
        CharacterActor actor = unitObject.AddComponent<CharacterActor>();
        CapsuleCollider collider = unitObject.AddComponent<CapsuleCollider>();
        GroundDefenseNavMeshUnit unit =
            unitObject.AddComponent<GroundDefenseNavMeshUnit>();

        bool isDefender = side == GroundDefenseNavMeshUnitSide.Defender;
        GroundDefenseNavMeshEnemyRole enemyRole = isDefender
            ? null
            : GetEnemyRoleForSlot(slotIndex);
        stats.ConfigureBaseStats(
            isDefender ? defenderHealth : enemyHealth * enemyRole.HealthMultiplier,
            isDefender ? defenderMoveSpeed : enemyMoveSpeed * enemyRole.MoveSpeedMultiplier,
            isDefender ? defenderDamage : enemyDamage * enemyRole.DamageMultiplier,
            isDefender ? defenderAttackRange : enemyAttackRange,
            isDefender ? defenderAttackCooldown : enemyAttackCooldown * enemyRole.AttackCooldownMultiplier);
        health.Refill();
        actor.ConfigureTeam(isDefender ? CharacterTeam.Player : CharacterTeam.Enemy);

        agent.radius = 0.42f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.acceleration = 12f;
        agent.angularSpeed = 480f;
        agent.stoppingDistance =
            (isDefender ? defenderAttackRange : enemyAttackRange) * 0.72f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        if (!agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
        }

        collider.radius = 0.42f;
        collider.height = 1.8f;
        collider.center = new Vector3(0f, 0.9f, 0f);

        GroundDefenseBillboardHandle visual =
            GroundDefenseBillboardUtility.CreateBillboard(
                "CharacterVisual",
                unitObject.transform,
                defenseCamera,
                readabilitySheet,
                isDefender ? defenderUv : enemyRole.VisualUv,
                isDefender ? defenderVisualSize : enemyRole.VisualSize,
                Color.white,
                isDefender ? 12 : 10,
                flipX: isDefender);
        visual.Root.transform.localPosition =
            Vector3.up * (isDefender ? defenderVisualHeight : enemyRole.VisualHeight);

        BuildOwnershipMarker(unitObject.transform, isDefender);
        unit.Configure(this, side, hit.position, visual.Root.transform, visual.Renderer);
        if (isDefender)
        {
            defenders.Add(unit);
        }
        else
        {
            enemies.Add(unit);
        }
    }

    private void RefreshFormulaForceScale()
    {
        GroundDefenseVisualForceProfile nextProfile = EvaluateVisualForceProfile();
        if (activeForceProfile.RequiresRebuild(nextProfile))
        {
            BuildBattlefield();
            return;
        }

        activeForceProfile = nextProfile;
    }

    private GroundDefenseVisualForceProfile EvaluateVisualForceProfile()
    {
        GroundDefenseBalanceProfile progression = defense == null
            ? GroundDefenseBalanceModel.Evaluate(1)
            : defense.CurrentProgressionProfile;

        if (!useFormulaDrivenForceScale)
        {
            return new GroundDefenseVisualForceProfile(
                defenderCount,
                enemyCount,
                0,
                enemyRespawnSeconds,
                defenderRespawnSeconds,
                progression.FrontlineLevel,
                progression.BandNumber);
        }

        int bandBonus = Mathf.Max(0, progression.BandNumber - 1);
        int levelBonus = Mathf.Max(0, progression.LevelInBand / extraEnemyEveryLevelsInBand);
        int scaledEnemyCount = Mathf.Clamp(
            enemyCount + bandBonus + levelBonus,
            1,
            Mathf.Max(enemyCount, maxFormulaEnemies));
        int scaledDefenderCount = Mathf.Clamp(
            defenderCount + bandBonus / 2,
            1,
            Mathf.Max(defenderCount, maxFormulaDefenders));
        int availableRoleCount = Mathf.Max(1, GetEnemyRoles().Length);
        int roleTier = Mathf.Clamp(
            progression.BandNumber - 1,
            0,
            availableRoleCount - 1);
        float incomingCadenceScale = Mathf.Pow(
            Mathf.Max(1f, progression.IncomingPressureMultiplier),
            formulaCadenceStrength);
        float defenderCadenceScale = Mathf.Pow(
            Mathf.Max(1f, progression.DefenseOutputMultiplier),
            formulaCadenceStrength);

        return new GroundDefenseVisualForceProfile(
            scaledDefenderCount,
            scaledEnemyCount,
            roleTier,
            Mathf.Max(minimumEnemyRespawnSeconds, enemyRespawnSeconds / incomingCadenceScale),
            Mathf.Max(minimumDefenderRespawnSeconds, defenderRespawnSeconds / defenderCadenceScale),
            progression.FrontlineLevel,
            progression.BandNumber);
    }

    private GroundDefenseNavMeshEnemyRole GetEnemyRoleForSlot(int slotIndex)
    {
        GroundDefenseNavMeshEnemyRole[] roles = GetEnemyRoles();
        int availableRoleCount = Mathf.Clamp(
            activeForceProfile.EnemyRoleTier + 1,
            1,
            roles.Length);
        int totalWeight = 0;
        for (int i = 0; i < availableRoleCount; i++)
        {
            totalWeight += roles[i].SpawnWeight;
        }

        int selector = totalWeight <= 0
            ? 0
            : Mathf.Abs(slotIndex * 2 + activeForceProfile.BandNumber) % totalWeight;
        for (int i = 0; i < availableRoleCount; i++)
        {
            int nextWeight = roles[i].SpawnWeight;
            if (selector < nextWeight)
            {
                return roles[i];
            }

            selector -= nextWeight;
        }

        return roles[0];
    }

    private GroundDefenseNavMeshEnemyRole[] GetEnemyRoles()
    {
        if (enemyRoles == null || enemyRoles.Length == 0)
        {
            enemyRoles = CreateDefaultEnemyRoles();
        }

        for (int i = 0; i < enemyRoles.Length; i++)
        {
            if (enemyRoles[i] == null)
            {
                enemyRoles[i] = CreateBaselineEnemyRole();
            }

            enemyRoles[i].Validate();
        }

        return enemyRoles;
    }

    private GroundDefenseNavMeshEnemyRole[] CreateDefaultEnemyRoles()
    {
        GroundDefenseNavMeshEnemyRole[] roles = GroundDefenseNavMeshEnemyRole.CreateDefaults();
        roles[0] = CreateBaselineEnemyRole();
        return roles;
    }

    private GroundDefenseNavMeshEnemyRole CreateBaselineEnemyRole()
    {
        return GroundDefenseNavMeshEnemyRole.CreateGrunt(
            enemyUv,
            enemyVisualSize,
            enemyVisualHeight);
    }

    private Vector3 GetSpawnPosition(
        GroundDefenseNavMeshUnitSide side,
        int slotIndex,
        int slotCount)
    {
        float centeredSlot = slotCount <= 1
            ? 0f
            : slotIndex - (slotCount - 1) * 0.5f;
        Vector3 sideOffset = sideDirection * centeredSlot * unitSpacing;
        if (side == GroundDefenseNavMeshUnitSide.Defender)
        {
            return wallAnchor.position -
                   travelDirection * defenderDistanceFromWall +
                   sideOffset;
        }

        return enemySpawnAnchor.position +
               travelDirection * 0.8f +
               sideOffset;
    }

    private void BuildOwnershipMarker(Transform parent, bool isDefender)
    {
        Color color = isDefender ? defenderOwnershipColor : enemyOwnershipColor;
        BuildOwnershipBase(parent, color);
        BuildOwnershipBadge(parent, isDefender, color);
    }

    private void BuildOwnershipBase(Transform parent, Color color)
    {
        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "FactionBase";
        baseObject.transform.SetParent(parent, false);
        baseObject.transform.localPosition = Vector3.up * (ownershipBaseHeight * 0.5f);
        baseObject.transform.localScale = new Vector3(
            ownershipBaseRadius,
            ownershipBaseHeight,
            ownershipBaseRadius);

        Collider baseCollider = baseObject.GetComponent<Collider>();
        if (baseCollider != null)
        {
            Destroy(baseCollider);
        }

        Renderer renderer = baseObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = CreateOwnershipMaterial("FactionBase", color);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GroundDefenseGeneratedVisual resources =
                parent.GetComponent<GroundDefenseGeneratedVisual>() ??
                parent.gameObject.AddComponent<GroundDefenseGeneratedVisual>();
            resources.Track(material);
        }
    }

    private void BuildOwnershipBadge(Transform parent, bool isDefender, Color color)
    {
        GameObject badge = new GameObject(isDefender ? "DefenderShieldBadge" : "EnemyThreatBadge");
        badge.transform.SetParent(parent, false);
        badge.transform.localPosition = Vector3.up * ownershipBadgeHeight;

        GroundDefenseBillboardFacing facing = badge.AddComponent<GroundDefenseBillboardFacing>();
        facing.Configure(defenseCamera);

        Mesh mesh = isDefender
            ? CreateShieldBadgeMesh(ownershipBadgeSize)
            : CreateThreatBadgeMesh(ownershipBadgeSize);
        MeshFilter filter = badge.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = badge.AddComponent<MeshRenderer>();
        Material material = CreateOwnershipMaterial(badge.name, color);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = isDefender ? 24 : 23;

        GroundDefenseGeneratedVisual resources =
            parent.GetComponent<GroundDefenseGeneratedVisual>() ??
            parent.gameObject.AddComponent<GroundDefenseGeneratedVisual>();
        resources.Track(mesh);
        resources.Track(material);
    }

    private static Mesh CreateShieldBadgeMesh(Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Mesh mesh = new Mesh
        {
            name = "GroundDefense_DefenderShieldBadge_RuntimeMesh",
            vertices = new[]
            {
                new Vector3(-halfWidth, halfHeight * 0.55f, 0f),
                new Vector3(halfWidth, halfHeight * 0.55f, 0f),
                new Vector3(halfWidth * 0.82f, -halfHeight * 0.18f, 0f),
                new Vector3(0f, -halfHeight, 0f),
                new Vector3(-halfWidth * 0.82f, -halfHeight * 0.18f, 0f)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 4, 4, 2, 3 }
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateThreatBadgeMesh(Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Mesh mesh = new Mesh
        {
            name = "GroundDefense_EnemyThreatBadge_RuntimeMesh",
            vertices = new[]
            {
                new Vector3(0f, halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight * 0.2f, 0f),
                new Vector3(halfWidth * 0.28f, -halfHeight * 0.2f, 0f),
                new Vector3(halfWidth * 0.28f, -halfHeight, 0f),
                new Vector3(-halfWidth * 0.28f, -halfHeight, 0f),
                new Vector3(-halfWidth * 0.28f, -halfHeight * 0.2f, 0f),
                new Vector3(-halfWidth, -halfHeight * 0.2f, 0f)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 5, 0, 5, 6, 2, 3, 4, 2, 4, 5 }
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateOwnershipMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Sprites/Default") ??
                        Shader.Find("Standard");
        Material material = new Material(shader)
        {
            name = $"{name}_RuntimeMaterial",
            color = color
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private IEnumerator ReplaceDefeatedUnit(GroundDefenseNavMeshUnit unit)
    {
        GroundDefenseNavMeshUnitSide side = unit.Side;
        yield return new WaitForSeconds(defeatedBodySeconds);

        List<GroundDefenseNavMeshUnit> list =
            side == GroundDefenseNavMeshUnitSide.Defender ? defenders : enemies;
        int slotIndex = Mathf.Max(0, list.IndexOf(unit));
        list.Remove(unit);
        if (unit != null)
        {
            Destroy(unit.gameObject);
        }

        float respawnDelay = side == GroundDefenseNavMeshUnitSide.Defender
            ? activeForceProfile.DefenderRespawnSeconds
            : activeForceProfile.EnemyRespawnSeconds;
        yield return new WaitForSeconds(respawnDelay);

        if (!shuttingDown && generatedRoot != null)
        {
            int desiredCount = side == GroundDefenseNavMeshUnitSide.Defender
                ? activeForceProfile.DefenderCount
                : activeForceProfile.EnemyCount;
            SpawnUnit(side, Mathf.Min(slotIndex, desiredCount - 1), desiredCount);
        }
    }

    private GroundDefenseNavMeshUnit FindNearestAlive(
        List<GroundDefenseNavMeshUnit> units,
        Vector3 position)
    {
        GroundDefenseNavMeshUnit nearest = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < units.Count; i++)
        {
            GroundDefenseNavMeshUnit candidate = units[i];
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            float distance = (candidate.transform.position - position).sqrMagnitude;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static int CountAlive(List<GroundDefenseNavMeshUnit> units)
    {
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].IsAlive)
            {
                count += 1;
            }
        }

        return count;
    }

    private void ClearBattlefield()
    {
        StopAllCoroutines();
        defenders.Clear();
        enemies.Clear();
        wallVisual = null;
        navMeshSurface = null;

        if (generatedRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }
        }

        generatedRoot = null;
        if (groundMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(groundMaterial);
            }
            else
            {
                DestroyImmediate(groundMaterial);
            }
        }

        groundMaterial = null;
    }

    private void ResolveReferences()
    {
        if (defense == null)
        {
            defense = FindAnyObjectByType<DefenseDirector>();
        }
    }
}

[Serializable]
public sealed class GroundDefenseNavMeshEnemyRole
{
    [SerializeField] private string id;
    [SerializeField, Min(1)] private int spawnWeight = 1;
    [SerializeField] private Rect visualUv;
    [SerializeField] private Vector2 visualSize;
    [SerializeField, Min(0f)] private float visualHeight;
    [SerializeField, Min(0.05f)] private float healthMultiplier = 1f;
    [SerializeField, Min(0.05f)] private float moveSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0.05f)] private float attackCooldownMultiplier = 1f;

    public Rect VisualUv => visualUv;
    public Vector2 VisualSize => visualSize;
    public float VisualHeight => visualHeight;
    public int SpawnWeight => spawnWeight;
    public float HealthMultiplier => healthMultiplier;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public float AttackCooldownMultiplier => attackCooldownMultiplier;

    public GroundDefenseNavMeshEnemyRole()
    {
        id = "grunt";
        spawnWeight = 1;
        visualUv = new Rect(0f, 0.5f, 0.33333334f, 0.5f);
        visualSize = new Vector2(1.65f, 2.35f);
        visualHeight = 1.18f;
        Validate();
    }

    private GroundDefenseNavMeshEnemyRole(
        string nextId,
        int nextSpawnWeight,
        Rect nextVisualUv,
        Vector2 nextVisualSize,
        float nextVisualHeight,
        float nextHealthMultiplier,
        float nextMoveSpeedMultiplier,
        float nextDamageMultiplier,
        float nextAttackCooldownMultiplier)
    {
        id = nextId;
        spawnWeight = nextSpawnWeight;
        visualUv = nextVisualUv;
        visualSize = nextVisualSize;
        visualHeight = nextVisualHeight;
        healthMultiplier = nextHealthMultiplier;
        moveSpeedMultiplier = nextMoveSpeedMultiplier;
        damageMultiplier = nextDamageMultiplier;
        attackCooldownMultiplier = nextAttackCooldownMultiplier;
        Validate();
    }

    public static GroundDefenseNavMeshEnemyRole[] CreateDefaults()
    {
        return new[]
        {
            CreateGrunt(),
            new GroundDefenseNavMeshEnemyRole(
                "shield",
                1,
                new Rect(0.33333334f, 0.5f, 0.33333334f, 0.5f),
                new Vector2(2.1f, 2.6f),
                1.3f,
                1.45f,
                0.82f,
                0.95f,
                1.1f),
            new GroundDefenseNavMeshEnemyRole(
                "runner",
                2,
                new Rect(0.6666667f, 0.5f, 0.33333334f, 0.5f),
                new Vector2(2f, 2.1f),
                1.05f,
                0.65f,
                1.2f,
                0.8f,
                0.82f)
        };
    }

    public static GroundDefenseNavMeshEnemyRole CreateGrunt()
    {
        return CreateGrunt(
            new Rect(0f, 0.5f, 0.33333334f, 0.5f),
            new Vector2(1.65f, 2.35f),
            1.18f);
    }

    public static GroundDefenseNavMeshEnemyRole CreateGrunt(
        Rect visualUv,
        Vector2 visualSize,
        float visualHeight)
    {
        return new GroundDefenseNavMeshEnemyRole(
            "grunt",
            4,
            visualUv,
            visualSize,
            visualHeight,
            1f,
            1f,
            1f,
            1f);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "grunt";
        }

        spawnWeight = Mathf.Max(1, spawnWeight);
        visualSize = new Vector2(
            Mathf.Max(0.1f, visualSize.x),
            Mathf.Max(0.1f, visualSize.y));
        visualHeight = Mathf.Max(0f, visualHeight);
        healthMultiplier = Mathf.Max(0.05f, healthMultiplier);
        moveSpeedMultiplier = Mathf.Max(0.05f, moveSpeedMultiplier);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        attackCooldownMultiplier = Mathf.Max(0.05f, attackCooldownMultiplier);
    }
}

public readonly struct GroundDefenseVisualForceProfile
{
    public int DefenderCount { get; }
    public int EnemyCount { get; }
    public int EnemyRoleTier { get; }
    public float EnemyRespawnSeconds { get; }
    public float DefenderRespawnSeconds { get; }
    public int FrontlineLevel { get; }
    public int BandNumber { get; }

    public GroundDefenseVisualForceProfile(
        int defenderCount,
        int enemyCount,
        int enemyRoleTier,
        float enemyRespawnSeconds,
        float defenderRespawnSeconds,
        int frontlineLevel,
        int bandNumber)
    {
        DefenderCount = Mathf.Max(1, defenderCount);
        EnemyCount = Mathf.Max(1, enemyCount);
        EnemyRoleTier = Mathf.Max(0, enemyRoleTier);
        EnemyRespawnSeconds = Mathf.Max(0.1f, enemyRespawnSeconds);
        DefenderRespawnSeconds = Mathf.Max(0.1f, defenderRespawnSeconds);
        FrontlineLevel = Mathf.Max(1, frontlineLevel);
        BandNumber = Mathf.Max(1, bandNumber);
    }

    public bool RequiresRebuild(GroundDefenseVisualForceProfile other)
    {
        return DefenderCount != other.DefenderCount ||
               EnemyCount != other.EnemyCount ||
               EnemyRoleTier != other.EnemyRoleTier;
    }
}
