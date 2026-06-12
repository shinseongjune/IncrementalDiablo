using UnityEngine;

[CreateAssetMenu(menuName = "Incremental Diablo/Ground Defense/Enemy Archetype")]
public sealed class GroundDefenseEnemyArchetype : ScriptableObject
{
    [Header("Identity And View")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private GroundDefenseEnemyView viewPrefab;
    [SerializeField, Min(1)] private int spawnWeight = 1;
    [SerializeField] private Texture2D visualTexture;
    [SerializeField] private Rect visualUvRect = new Rect(0f, 0.5f, 0.33333334f, 0.5f);
    [SerializeField] private Vector2 visualSize = new Vector2(1.75f, 2.4f);
    [SerializeField, Min(0f)] private float visualHeightOffset = 1.2f;

    [Header("Continuous Frontline Projection")]
    [SerializeField, Min(0.01f)] private float maxHealth = 12f;
    [SerializeField, Min(0.01f)] private float pressurePerSpawn = 8f;
    [SerializeField, Min(0.01f)] private float damagePerHit = 3f;
    [SerializeField, Min(0f)] private float baseAdvancePerSecond = 0.1f;
    [SerializeField, Min(0f)] private float pressureAdvancePerSecond = 0.12f;

    [Header("Reusable Feedback")]
    [SerializeField, Min(0.01f)] private float hitFeedbackSeconds = 0.16f;
    [SerializeField, Min(0.01f)] private float defeatFeedbackSeconds = 0.24f;
    [SerializeField, Min(0.01f)] private float wallContactFeedbackSeconds = 0.18f;
    [SerializeField] private Color baseColor = new Color(0.82f, 0.18f, 0.12f);
    [SerializeField] private Color underFireColor = new Color(1f, 0.68f, 0.18f);
    [SerializeField] private Color defeatColor = new Color(0.35f, 0.08f, 0.05f);
    [SerializeField] private Color wallContactColor = new Color(1f, 0.25f, 0.1f);

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public GroundDefenseEnemyView ViewPrefab => viewPrefab;
    public int SpawnWeight => spawnWeight;
    public Texture2D VisualTexture => visualTexture;
    public Rect VisualUvRect => visualUvRect;
    public Vector2 VisualSize => visualSize;
    public float VisualHeightOffset => visualHeightOffset;
    public float MaxHealth => maxHealth;
    public float PressurePerSpawn => pressurePerSpawn;
    public float DamagePerHit => damagePerHit;
    public float BaseAdvancePerSecond => baseAdvancePerSecond;
    public float PressureAdvancePerSecond => pressureAdvancePerSecond;
    public float HitFeedbackSeconds => hitFeedbackSeconds;
    public float DefeatFeedbackSeconds => defeatFeedbackSeconds;
    public float WallContactFeedbackSeconds => wallContactFeedbackSeconds;
    public Color BaseColor => baseColor;
    public Color UnderFireColor => underFireColor;
    public Color DefeatColor => defeatColor;
    public Color WallContactColor => wallContactColor;

    private void OnValidate()
    {
        spawnWeight = Mathf.Max(1, spawnWeight);
        visualSize = new Vector2(
            Mathf.Max(0.1f, visualSize.x),
            Mathf.Max(0.1f, visualSize.y));
        visualHeightOffset = Mathf.Max(0f, visualHeightOffset);
        maxHealth = Mathf.Max(0.01f, maxHealth);
        pressurePerSpawn = Mathf.Max(0.01f, pressurePerSpawn);
        damagePerHit = Mathf.Max(0.01f, damagePerHit);
        baseAdvancePerSecond = Mathf.Max(0f, baseAdvancePerSecond);
        pressureAdvancePerSecond = Mathf.Max(0f, pressureAdvancePerSecond);
        hitFeedbackSeconds = Mathf.Max(0.01f, hitFeedbackSeconds);
        defeatFeedbackSeconds = Mathf.Max(0.01f, defeatFeedbackSeconds);
        wallContactFeedbackSeconds = Mathf.Max(0.01f, wallContactFeedbackSeconds);

        if (string.IsNullOrWhiteSpace(id))
        {
            id = name;
        }
    }
}
