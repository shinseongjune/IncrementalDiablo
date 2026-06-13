using UnityEngine;

public sealed class GroundDefenseBattlefieldView : MonoBehaviour
{
    private enum DefenderVisualState
    {
        Active,
        Defeated,
        Reinforcing
    }

    [Header("References")]
    [SerializeField] private Camera defenseCamera;
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Texture2D readabilitySheet;

    [Header("Battlefield Formation")]
    [SerializeField, Range(0.5f, 0.9f)] private float contactLinePercent = 0.72f;
    [SerializeField, Min(0.1f)] private float enemyLaneSpacing = 0.72f;
    [SerializeField, Min(1)] private int defenderCount = 3;
    [SerializeField, Min(0.1f)] private float defenderSpacing = 0.92f;
    [SerializeField, Min(0.1f)] private float defenderLineGap = 0.72f;

    [Header("Readable Defense Line")]
    [SerializeField] private Rect defenderUv = new Rect(0f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Rect towerUv = new Rect(0.33333334f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Rect wallUv = new Rect(0.6666667f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Vector2 defenderSize = new Vector2(1.45f, 2.05f);
    [SerializeField] private Vector2 towerSize = new Vector2(3.1f, 3.8f);
    [SerializeField] private Vector2 wallSize = new Vector2(4.8f, 3.5f);
    [SerializeField] private Vector3 towerOffset = new Vector3(0f, 1.85f, 0f);
    [SerializeField] private Vector3 wallOffset = new Vector3(0f, 1.55f, 0f);

    [Header("Attack And Reinforcement")]
    [SerializeField, Min(1)] private int projectileCapacity = 5;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0f)] private float projectileArcHeight = 0.45f;
    [SerializeField] private Color projectileColor = new Color(1f, 0.78f, 0.28f);
    [SerializeField, Min(0.01f)] private float meleeLungeSeconds = 0.18f;
    [SerializeField, Min(0f)] private float meleeLungeDistance = 0.42f;
    [SerializeField, Min(0.01f)] private float defenderDeathSeconds = 0.45f;
    [SerializeField, Min(0.01f)] private float reinforcementSeconds = 0.85f;
    [SerializeField, Min(0.1f)] private float casualtyCooldownSeconds = 2.5f;

    [Header("Wall Feedback")]
    [SerializeField] private Color damagedWallColor = new Color(1f, 0.55f, 0.36f);
    [SerializeField] private Color breachedWallColor = new Color(0.65f, 0.12f, 0.08f);
    [SerializeField, Min(0.01f)] private float wallHitSeconds = 0.22f;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private GameObject generatedRoot;
    private GroundDefenseBillboardHandle wallVisual;
    private GroundDefenseBillboardHandle towerVisual;
    private GroundDefenseBillboardHandle wallHealthBar;
    private Transform wallHealthFill;
    private Renderer wallHealthFillRenderer;
    private GroundDefenseBillboardHandle[] defenderVisuals = new GroundDefenseBillboardHandle[0];
    private DefenderState[] defenderStates = new DefenderState[0];
    private GroundDefenseBillboardHandle[] projectileVisuals = new GroundDefenseBillboardHandle[0];
    private ProjectileState[] projectileStates = new ProjectileState[0];
    private float wallHealthPercent = 1f;
    private float wallHitRemaining;
    private bool wallBreached;
    private float casualtyCooldownRemaining;
    private int casualtySequence;
    private int attackSequence;

    public bool IsReady =>
        enemySpawnAnchor != null &&
        wallAnchor != null &&
        attackOrigin != null &&
        readabilitySheet != null;

    public int ActiveProjectileCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < projectileStates.Length; i++)
            {
                if (projectileStates[i].active)
                {
                    count += 1;
                }
            }

            return count;
        }
    }

    public int ActiveDefenderCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < defenderStates.Length; i++)
            {
                if (defenderStates[i].state == DefenderVisualState.Active)
                {
                    count += 1;
                }
            }

            return count;
        }
    }

    private void OnEnable()
    {
        Build();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void Update()
    {
        if (!Application.isPlaying || generatedRoot == null)
        {
            return;
        }

        float deltaTime = Mathf.Max(0f, Time.deltaTime);
        casualtyCooldownRemaining = Mathf.Max(0f, casualtyCooldownRemaining - deltaTime);
        wallHitRemaining = Mathf.Max(0f, wallHitRemaining - deltaTime);
        TickDefenders(deltaTime);
        TickProjectiles(deltaTime);
        UpdateWallVisual();
    }

    private void OnValidate()
    {
        contactLinePercent = Mathf.Clamp(contactLinePercent, 0.5f, 0.9f);
        enemyLaneSpacing = Mathf.Max(0.1f, enemyLaneSpacing);
        defenderCount = Mathf.Max(1, defenderCount);
        defenderSpacing = Mathf.Max(0.1f, defenderSpacing);
        defenderLineGap = Mathf.Max(0.1f, defenderLineGap);
        defenderSize = ClampSize(defenderSize);
        towerSize = ClampSize(towerSize);
        wallSize = ClampSize(wallSize);
        projectileCapacity = Mathf.Max(1, projectileCapacity);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileArcHeight = Mathf.Max(0f, projectileArcHeight);
        meleeLungeSeconds = Mathf.Max(0.01f, meleeLungeSeconds);
        meleeLungeDistance = Mathf.Max(0f, meleeLungeDistance);
        defenderDeathSeconds = Mathf.Max(0.01f, defenderDeathSeconds);
        reinforcementSeconds = Mathf.Max(0.01f, reinforcementSeconds);
        casualtyCooldownSeconds = Mathf.Max(0.1f, casualtyCooldownSeconds);
        wallHitSeconds = Mathf.Max(0.01f, wallHitSeconds);
    }

    public void Build()
    {
        Clear();
        if (!Application.isPlaying || !IsReady)
        {
            return;
        }

        defenseCamera = GroundDefenseBillboardUtility.FindDefenseCamera(defenseCamera);
        generatedRoot = new GameObject("AutomaticDefenseBattlefield");
        generatedRoot.transform.SetParent(transform, false);

        BuildStructures();
        BuildDefenders();
        BuildProjectilePool();
        UpdateWallVisual();
    }

    public Vector3 GetEnemyWorldPosition(int actorIndex, float travelPercent)
    {
        if (!IsReady)
        {
            return Vector3.zero;
        }

        Vector3 start = enemySpawnAnchor.position;
        Vector3 wall = wallAnchor.position;
        Vector3 contact = Vector3.Lerp(start, wall, contactLinePercent);
        float safeTravel = Mathf.Clamp01(travelPercent);
        float contactTravel = 0.9f;
        Vector3 basePosition = safeTravel <= contactTravel
            ? Vector3.Lerp(start, contact, safeTravel / contactTravel)
            : Vector3.Lerp(contact, wall, (safeTravel - contactTravel) / (1f - contactTravel));

        Vector3 sideAxis = GetSideAxis();
        float lane = GetCenteredSlot(actorIndex, 5) * enemyLaneSpacing;
        float wallConvergence = safeTravel <= contactTravel
            ? 1f
            : 1f - (safeTravel - contactTravel) / (1f - contactTravel);
        return basePosition + sideAxis * lane * Mathf.Clamp01(wallConvergence);
    }

    public void PlayDefenseHit(int actorIndex, Vector3 targetPosition)
    {
        if (generatedRoot == null)
        {
            return;
        }

        attackSequence += 1;
        if (attackSequence % 3 == 0)
        {
            LaunchTowerProjectile(targetPosition);
        }
        else
        {
            PlayMeleeStrike(actorIndex, targetPosition);
        }
    }

    public void ApplyWallState(float healthPercent, bool tookDamage, bool breached)
    {
        wallHealthPercent = Mathf.Clamp01(healthPercent);
        wallBreached = breached;
        if (!tookDamage)
        {
            return;
        }

        wallHitRemaining = wallHitSeconds;
        if (casualtyCooldownRemaining <= 0f)
        {
            TriggerDefenderCasualty();
            casualtyCooldownRemaining = casualtyCooldownSeconds;
        }
    }

    private void BuildStructures()
    {
        wallVisual = GroundDefenseBillboardUtility.CreateBillboard(
            "DefenseWall",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            wallUv,
            wallSize,
            Color.white,
            6);
        wallVisual.Root.transform.position = wallAnchor.position + wallOffset;

        towerVisual = GroundDefenseBillboardUtility.CreateBillboard(
            "CrossbowTower",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            towerUv,
            towerSize,
            Color.white,
            5);
        towerVisual.Root.transform.position = attackOrigin.position + towerOffset;

        float barWidth = Mathf.Max(1.2f, wallSize.x * 0.72f);
        wallHealthBar = GroundDefenseBillboardUtility.CreateBillboard(
            "WallHealthBar",
            generatedRoot.transform,
            defenseCamera,
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(barWidth, 0.14f),
            new Color(0.06f, 0.06f, 0.06f, 0.92f),
            20);
        wallHealthBar.Root.transform.position =
            wallAnchor.position + wallOffset + Vector3.up * (wallSize.y * 0.62f);
        wallHealthFillRenderer = GroundDefenseBillboardUtility.CreateQuad(
            "Fill",
            wallHealthBar.Root.transform,
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(barWidth, 0.09f),
            new Color(0.25f, 0.78f, 0.3f),
            21);
        wallHealthFill = wallHealthFillRenderer.transform;
    }

    private void BuildDefenders()
    {
        defenderVisuals = new GroundDefenseBillboardHandle[defenderCount];
        defenderStates = new DefenderState[defenderCount];

        for (int i = 0; i < defenderCount; i++)
        {
            GroundDefenseBillboardHandle defender = GroundDefenseBillboardUtility.CreateBillboard(
                $"Defender_{i + 1:00}",
                generatedRoot.transform,
                defenseCamera,
                readabilitySheet,
                defenderUv,
                defenderSize,
                Color.white,
                10 + i);
            defenderVisuals[i] = defender;
            defenderStates[i] = new DefenderState
            {
                state = DefenderVisualState.Active,
                homePosition = GetDefenderHomePosition(i),
                targetPosition = GetDefenderHomePosition(i)
            };
            defender.Root.transform.position = defenderStates[i].homePosition;
        }
    }

    private void BuildProjectilePool()
    {
        projectileVisuals = new GroundDefenseBillboardHandle[projectileCapacity];
        projectileStates = new ProjectileState[projectileCapacity];

        for (int i = 0; i < projectileCapacity; i++)
        {
            GroundDefenseBillboardHandle projectile = GroundDefenseBillboardUtility.CreateBillboard(
                $"TowerProjectile_{i + 1:00}",
                generatedRoot.transform,
                defenseCamera,
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.34f, 0.11f),
                projectileColor,
                30);
            projectile.Root.SetActive(false);
            projectileVisuals[i] = projectile;
        }
    }

    private void PlayMeleeStrike(int actorIndex, Vector3 targetPosition)
    {
        if (defenderStates.Length == 0)
        {
            return;
        }

        int startIndex = Mathf.Abs(actorIndex) % defenderStates.Length;
        for (int offset = 0; offset < defenderStates.Length; offset++)
        {
            int index = (startIndex + offset) % defenderStates.Length;
            DefenderState defender = defenderStates[index];
            if (defender.state != DefenderVisualState.Active)
            {
                continue;
            }

            defender.actionRemaining = meleeLungeSeconds;
            defender.targetPosition = targetPosition;
            defenderStates[index] = defender;
            return;
        }
    }

    private void LaunchTowerProjectile(Vector3 targetPosition)
    {
        int projectileIndex = -1;
        for (int i = 0; i < projectileStates.Length; i++)
        {
            if (!projectileStates[i].active)
            {
                projectileIndex = i;
                break;
            }
        }

        if (projectileIndex < 0)
        {
            projectileIndex = attackSequence % projectileStates.Length;
        }

        Vector3 start = attackOrigin.position + towerOffset + Vector3.up * 0.35f;
        float distance = Vector3.Distance(start, targetPosition);
        projectileStates[projectileIndex] = new ProjectileState
        {
            active = true,
            start = start,
            end = targetPosition,
            duration = Mathf.Max(0.08f, distance / projectileSpeed),
            remaining = Mathf.Max(0.08f, distance / projectileSpeed)
        };
        projectileVisuals[projectileIndex].Root.transform.position = start;
        projectileVisuals[projectileIndex].Root.SetActive(true);
    }

    private void TriggerDefenderCasualty()
    {
        if (defenderStates.Length == 0)
        {
            return;
        }

        for (int offset = 0; offset < defenderStates.Length; offset++)
        {
            int index = (casualtySequence + offset) % defenderStates.Length;
            DefenderState defender = defenderStates[index];
            if (defender.state != DefenderVisualState.Active)
            {
                continue;
            }

            defender.state = DefenderVisualState.Defeated;
            defender.actionRemaining = defenderDeathSeconds;
            defenderStates[index] = defender;
            casualtySequence = (index + 1) % defenderStates.Length;
            return;
        }
    }

    private void TickDefenders(float deltaTime)
    {
        for (int i = 0; i < defenderStates.Length; i++)
        {
            DefenderState defender = defenderStates[i];
            GroundDefenseBillboardHandle visual = defenderVisuals[i];
            defender.homePosition = GetDefenderHomePosition(i);
            defender.actionRemaining = Mathf.Max(0f, defender.actionRemaining - deltaTime);

            switch (defender.state)
            {
                case DefenderVisualState.Active:
                    UpdateActiveDefender(visual, ref defender);
                    break;
                case DefenderVisualState.Defeated:
                    UpdateDefeatedDefender(visual, ref defender);
                    break;
                case DefenderVisualState.Reinforcing:
                    UpdateReinforcingDefender(visual, ref defender);
                    break;
            }

            defenderStates[i] = defender;
        }
    }

    private void UpdateActiveDefender(
        GroundDefenseBillboardHandle visual,
        ref DefenderState defender)
    {
        float actionPercent = meleeLungeSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(defender.actionRemaining / meleeLungeSeconds);
        float lunge = Mathf.Sin((1f - actionPercent) * Mathf.PI) * meleeLungeDistance;
        Vector3 direction = defender.targetPosition - defender.homePosition;
        direction.y = 0f;
        direction = direction.sqrMagnitude <= 0.0001f ? Vector3.zero : direction.normalized;
        visual.Root.transform.position = defender.homePosition + direction * lunge;
        visual.Root.transform.localScale = Vector3.one;
        SetRendererColor(visual.Renderer, Color.white);
    }

    private void UpdateDefeatedDefender(
        GroundDefenseBillboardHandle visual,
        ref DefenderState defender)
    {
        float percent = defenderDeathSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(defender.actionRemaining / defenderDeathSeconds);
        visual.Root.transform.position = defender.homePosition;
        visual.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, percent);
        SetRendererColor(visual.Renderer, new Color(0.42f, 0.16f, 0.14f, 0.85f));

        if (defender.actionRemaining <= 0f)
        {
            defender.state = DefenderVisualState.Reinforcing;
            defender.actionRemaining = reinforcementSeconds;
        }
    }

    private void UpdateReinforcingDefender(
        GroundDefenseBillboardHandle visual,
        ref DefenderState defender)
    {
        float percent = reinforcementSeconds <= 0f
            ? 1f
            : 1f - Mathf.Clamp01(defender.actionRemaining / reinforcementSeconds);
        Vector3 start = wallAnchor.position + wallOffset * 0.35f;
        visual.Root.transform.position = Vector3.Lerp(start, defender.homePosition, percent);
        visual.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1f, percent);
        SetRendererColor(visual.Renderer, new Color(0.62f, 0.82f, 1f, 1f));

        if (defender.actionRemaining <= 0f)
        {
            defender.state = DefenderVisualState.Active;
            defender.targetPosition = defender.homePosition;
        }
    }

    private void TickProjectiles(float deltaTime)
    {
        for (int i = 0; i < projectileStates.Length; i++)
        {
            ProjectileState projectile = projectileStates[i];
            if (!projectile.active)
            {
                continue;
            }

            projectile.remaining = Mathf.Max(0f, projectile.remaining - deltaTime);
            float percent = projectile.duration <= 0f
                ? 1f
                : 1f - projectile.remaining / projectile.duration;
            Vector3 position = Vector3.Lerp(projectile.start, projectile.end, percent);
            position.y += Mathf.Sin(percent * Mathf.PI) * projectileArcHeight;
            projectileVisuals[i].Root.transform.position = position;

            if (projectile.remaining <= 0f)
            {
                projectile.active = false;
                projectileVisuals[i].Root.SetActive(false);
            }

            projectileStates[i] = projectile;
        }
    }

    private void UpdateWallVisual()
    {
        if (wallVisual == null)
        {
            return;
        }

        Color wallColor = wallBreached
            ? breachedWallColor
            : Color.Lerp(damagedWallColor, Color.white, wallHealthPercent);
        SetRendererColor(wallVisual.Renderer, wallColor);
        float hitPercent = wallHitSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(wallHitRemaining / wallHitSeconds);
        wallVisual.Root.transform.localScale =
            Vector3.one * Mathf.Lerp(1f, 1.08f, hitPercent);

        if (wallHealthFill == null)
        {
            return;
        }

        wallHealthFill.localScale = new Vector3(wallHealthPercent, 1f, 1f);
        wallHealthFill.localPosition = new Vector3(
            -wallSize.x * 0.72f * (1f - wallHealthPercent) * 0.5f,
            0f,
            -0.01f);
        SetRendererColor(
            wallHealthFillRenderer,
            Color.Lerp(breachedWallColor, new Color(0.25f, 0.78f, 0.3f), wallHealthPercent));
    }

    private Vector3 GetDefenderHomePosition(int index)
    {
        Vector3 start = enemySpawnAnchor.position;
        Vector3 wall = wallAnchor.position;
        Vector3 directionToWall = (wall - start).normalized;
        Vector3 contact = Vector3.Lerp(start, wall, contactLinePercent);
        float side = GetCenteredSlot(index, defenderCount) * defenderSpacing;
        return contact + directionToWall * defenderLineGap + GetSideAxis() * side;
    }

    private Vector3 GetSideAxis()
    {
        Vector3 direction = wallAnchor.position - enemySpawnAnchor.position;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        return side.sqrMagnitude <= 0.0001f ? Vector3.forward : side.normalized;
    }

    private static float GetCenteredSlot(int index, int count)
    {
        if (count <= 1)
        {
            return 0f;
        }

        return index % count - (count - 1) * 0.5f;
    }

    private void SetRendererColor(Renderer target, Color color)
    {
        if (target == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorProperty, color);
        propertyBlock.SetColor(ColorProperty, color);
        target.SetPropertyBlock(propertyBlock);
    }

    private void Clear()
    {
        GroundDefenseBillboardUtility.DestroyVisual(generatedRoot);
        generatedRoot = null;
        wallVisual = null;
        towerVisual = null;
        wallHealthBar = null;
        wallHealthFill = null;
        wallHealthFillRenderer = null;
        defenderVisuals = new GroundDefenseBillboardHandle[0];
        defenderStates = new DefenderState[0];
        projectileVisuals = new GroundDefenseBillboardHandle[0];
        projectileStates = new ProjectileState[0];
        wallHealthPercent = 1f;
        wallHitRemaining = 0f;
        wallBreached = false;
        casualtyCooldownRemaining = 0f;
        casualtySequence = 0;
        attackSequence = 0;
    }

    private static Vector2 ClampSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
    }

    private struct DefenderState
    {
        public DefenderVisualState state;
        public Vector3 homePosition;
        public Vector3 targetPosition;
        public float actionRemaining;
    }

    private struct ProjectileState
    {
        public bool active;
        public Vector3 start;
        public Vector3 end;
        public float duration;
        public float remaining;
    }
}
