using UnityEngine;

public sealed class GroundDefenseBattlefieldView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera defenseCamera;
    [SerializeField] private Transform wallAnchor;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Texture2D readabilitySheet;

    [Header("Readable Defense Line")]
    [SerializeField] private Rect defenderUv = new Rect(0f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Rect towerUv = new Rect(0.33333334f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Rect wallUv = new Rect(0.6666667f, 0f, 0.33333334f, 0.5f);
    [SerializeField] private Vector2 defenderSize = new Vector2(2.4f, 3.2f);
    [SerializeField] private Vector2 towerSize = new Vector2(3.1f, 3.8f);
    [SerializeField] private Vector2 wallSize = new Vector2(4.8f, 3.5f);
    [SerializeField] private Vector3 defenderOffset = new Vector3(1.05f, 1.55f, 0.4f);
    [SerializeField] private Vector3 towerOffset = new Vector3(0f, 1.85f, 0f);
    [SerializeField] private Vector3 wallOffset = new Vector3(0f, 1.55f, 0f);

    private GameObject generatedRoot;

    public bool IsReady =>
        wallAnchor != null &&
        attackOrigin != null &&
        readabilitySheet != null;

    private void OnEnable()
    {
        Build();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnValidate()
    {
        defenderSize = ClampSize(defenderSize);
        towerSize = ClampSize(towerSize);
        wallSize = ClampSize(wallSize);
    }

    public void Build()
    {
        Clear();
        if (!Application.isPlaying || !IsReady)
        {
            return;
        }

        defenseCamera = GroundDefenseBillboardUtility.FindDefenseCamera(defenseCamera);
        generatedRoot = new GameObject("ReadableDefenseLine");
        generatedRoot.transform.SetParent(transform, false);

        GroundDefenseBillboardHandle wall = GroundDefenseBillboardUtility.CreateBillboard(
            "DefenseWall_Readable",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            wallUv,
            wallSize,
            Color.white,
            6);
        wall.Root.transform.position = wallAnchor.position + wallOffset;

        GroundDefenseBillboardHandle tower = GroundDefenseBillboardUtility.CreateBillboard(
            "CrossbowTower_Readable",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            towerUv,
            towerSize,
            Color.white,
            4);
        tower.Root.transform.position = attackOrigin.position + towerOffset;

        GroundDefenseBillboardHandle defender = GroundDefenseBillboardUtility.CreateBillboard(
            "FrontlineDefender_Readable",
            generatedRoot.transform,
            defenseCamera,
            readabilitySheet,
            defenderUv,
            defenderSize,
            Color.white,
            7);
        defender.Root.transform.position = attackOrigin.position + defenderOffset;
    }

    private void Clear()
    {
        GroundDefenseBillboardUtility.DestroyVisual(generatedRoot);
        generatedRoot = null;
    }

    private static Vector2 ClampSize(Vector2 size)
    {
        return new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
    }
}
