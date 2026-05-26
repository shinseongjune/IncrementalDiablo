using System;
using System.Collections;
using UnityEngine;

public class PlayableScreenLayoutController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private RectTransform defensePanel;
    [SerializeField] private RectTransform dungeonPanel;
    [SerializeField] private GameObject inventoryOverlay;
    [SerializeField] private GameObject craftingOverlay;
    [SerializeField] private GameObject rewardOverlay;

    [Header("Focus")]
    [SerializeField] private PlayableScreenFocus startingFocus = PlayableScreenFocus.DefenseFocus;
    [SerializeField, Range(0.05f, 0.95f)] private float dungeonFocusDungeonWidth = 0.7f;
    [SerializeField] private bool defensePanelOnRight = true;
    [SerializeField] private bool keepDungeonPanelActiveInDefenseFocus;
    [SerializeField] private bool applyStartingFocusOnStart = true;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float entryDurationSeconds = 0.38f;
    [SerializeField, Min(0f)] private float exitDurationSeconds = 0.32f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Diagnostics")]
    [SerializeField] private PlayableScreenFocus currentFocus = PlayableScreenFocus.DefenseFocus;
    [SerializeField] private PlayableScreenFocus previousGameplayFocus = PlayableScreenFocus.DefenseFocus;
    [SerializeField] private bool isTransitioning;
    [SerializeField, Range(0f, 1f)] private float transitionProgress = 1f;
    [SerializeField] private string lastLayoutMessage = "Ready";

    private Coroutine transitionRoutine;

    public event Action<PlayableScreenFocus> FocusChanged;

    public PlayableScreenFocus CurrentFocus => currentFocus;
    public PlayableScreenFocus PreviousGameplayFocus => previousGameplayFocus;
    public bool IsTransitioning => isTransitioning;
    public float TransitionProgress => transitionProgress;
    public string LastLayoutMessage => lastLayoutMessage;
    public bool IsOverlayOpen => IsOverlayFocus(currentFocus);

    private void Reset()
    {
        currentFocus = PlayableScreenFocus.DefenseFocus;
        previousGameplayFocus = PlayableScreenFocus.DefenseFocus;
    }

    private void Start()
    {
        if (applyStartingFocusOnStart)
        {
            SetFocus(startingFocus, true);
        }
        else
        {
            SetOverlayObjects(false, false, false);
        }
    }

    private void OnValidate()
    {
        dungeonFocusDungeonWidth = Mathf.Clamp(dungeonFocusDungeonWidth, 0.05f, 0.95f);
        entryDurationSeconds = Mathf.Max(0f, entryDurationSeconds);
        exitDurationSeconds = Mathf.Max(0f, exitDurationSeconds);

        if (!IsGameplayFocus(startingFocus))
        {
            startingFocus = PlayableScreenFocus.DefenseFocus;
        }

        if (!IsGameplayFocus(previousGameplayFocus))
        {
            previousGameplayFocus = PlayableScreenFocus.DefenseFocus;
        }
    }

    public void SetFocus(PlayableScreenFocus focus)
    {
        SetFocus(focus, false);
    }

    public void ShowDefenseFocus()
    {
        SetFocus(PlayableScreenFocus.DefenseFocus);
    }

    public void ShowDungeonFocus()
    {
        SetFocus(PlayableScreenFocus.DungeonFocus);
    }

    public void ToggleGameplayFocus()
    {
        SetFocus(previousGameplayFocus == PlayableScreenFocus.DungeonFocus
            ? PlayableScreenFocus.DefenseFocus
            : PlayableScreenFocus.DungeonFocus);
    }

    public void OpenInventoryOverlay()
    {
        SetFocus(PlayableScreenFocus.InventoryOverlay);
    }

    public void OpenCraftingOverlay()
    {
        SetFocus(PlayableScreenFocus.CraftingOverlay);
    }

    public void OpenRewardOverlay()
    {
        SetFocus(PlayableScreenFocus.RewardOverlay);
    }

    public void CloseOverlay()
    {
        if (!IsOverlayFocus(currentFocus))
        {
            SetOverlayObjects(false, false, false);
            return;
        }

        SetOverlayObjects(false, false, false);
        currentFocus = previousGameplayFocus;
        transitionProgress = 1f;
        lastLayoutMessage = $"Closed overlay; returned to {currentFocus}.";
        FocusChanged?.Invoke(currentFocus);
    }

    private void SetFocus(PlayableScreenFocus focus, bool instant)
    {
        if (IsOverlayFocus(focus))
        {
            OpenOverlay(focus);
            return;
        }

        if (!IsGameplayFocus(focus))
        {
            return;
        }

        SetOverlayObjects(false, false, false);
        previousGameplayFocus = focus;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        float duration = focus == PlayableScreenFocus.DungeonFocus ? entryDurationSeconds : exitDurationSeconds;
        if (instant || duration <= 0f || !isActiveAndEnabled)
        {
            ApplyGameplayFocus(focus);
            return;
        }

        transitionRoutine = StartCoroutine(TransitionToGameplayFocus(focus, duration));
    }

    private void OpenOverlay(PlayableScreenFocus overlayFocus)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (!IsOverlayFocus(currentFocus))
        {
            previousGameplayFocus = currentFocus;
        }

        SetOverlayObjects(
            overlayFocus == PlayableScreenFocus.InventoryOverlay,
            overlayFocus == PlayableScreenFocus.CraftingOverlay,
            overlayFocus == PlayableScreenFocus.RewardOverlay);

        currentFocus = overlayFocus;
        transitionProgress = 1f;
        isTransitioning = false;
        lastLayoutMessage = $"{overlayFocus} opened over {previousGameplayFocus}.";
        FocusChanged?.Invoke(currentFocus);
    }

    private IEnumerator TransitionToGameplayFocus(PlayableScreenFocus targetFocus, float duration)
    {
        isTransitioning = true;
        transitionProgress = 0f;
        currentFocus = targetFocus;
        FocusChanged?.Invoke(currentFocus);

        SetPanelActive(defensePanel, true);
        SetPanelActive(dungeonPanel, true);

        PanelLayout startDefense = CapturePanel(defensePanel);
        PanelLayout startDungeon = CapturePanel(dungeonPanel);
        PanelLayout targetDefense = GetDefenseTarget(targetFocus);
        PanelLayout targetDungeon = GetDungeonTarget(targetFocus);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(duration <= 0f ? 1f : elapsed / duration);
            float eased = Smooth(progress);
            transitionProgress = progress;
            ApplyPanel(defensePanel, PanelLayout.Lerp(startDefense, targetDefense, eased));
            ApplyPanel(dungeonPanel, PanelLayout.Lerp(startDungeon, targetDungeon, eased));
            yield return null;
        }

        ApplyGameplayFocus(targetFocus);
        transitionRoutine = null;
    }

    private void ApplyGameplayFocus(PlayableScreenFocus focus)
    {
        currentFocus = focus;
        previousGameplayFocus = focus;
        isTransitioning = false;
        transitionProgress = 1f;
        ApplyPanel(defensePanel, GetDefenseTarget(focus));
        ApplyPanel(dungeonPanel, GetDungeonTarget(focus));

        SetPanelActive(defensePanel, true);
        SetPanelActive(dungeonPanel, focus == PlayableScreenFocus.DungeonFocus || keepDungeonPanelActiveInDefenseFocus);
        lastLayoutMessage = $"{focus} layout applied.";
        FocusChanged?.Invoke(currentFocus);
    }

    private PanelLayout GetDefenseTarget(PlayableScreenFocus focus)
    {
        if (focus == PlayableScreenFocus.DungeonFocus)
        {
            float dungeonWidth = Mathf.Clamp01(dungeonFocusDungeonWidth);
            return defensePanelOnRight
                ? PanelLayout.FromAnchors(dungeonWidth, 1f)
                : PanelLayout.FromAnchors(0f, 1f - dungeonWidth);
        }

        return PanelLayout.FromAnchors(0f, 1f);
    }

    private PanelLayout GetDungeonTarget(PlayableScreenFocus focus)
    {
        if (focus == PlayableScreenFocus.DungeonFocus)
        {
            float dungeonWidth = Mathf.Clamp01(dungeonFocusDungeonWidth);
            return defensePanelOnRight
                ? PanelLayout.FromAnchors(0f, dungeonWidth)
                : PanelLayout.FromAnchors(1f - dungeonWidth, 1f);
        }

        return defensePanelOnRight
            ? PanelLayout.FromAnchors(0f, 0f)
            : PanelLayout.FromAnchors(1f, 1f);
    }

    private static PanelLayout CapturePanel(RectTransform panel)
    {
        return panel == null
            ? PanelLayout.FromAnchors(0f, 1f)
            : new PanelLayout(panel.anchorMin, panel.anchorMax, panel.offsetMin, panel.offsetMax);
    }

    private static void ApplyPanel(RectTransform panel, PanelLayout layout)
    {
        if (panel == null)
        {
            return;
        }

        panel.anchorMin = layout.anchorMin;
        panel.anchorMax = layout.anchorMax;
        panel.offsetMin = layout.offsetMin;
        panel.offsetMax = layout.offsetMax;
    }

    private void SetOverlayObjects(bool showInventory, bool showCrafting, bool showReward)
    {
        SetObjectActive(inventoryOverlay, showInventory);
        SetObjectActive(craftingOverlay, showCrafting);
        SetObjectActive(rewardOverlay, showReward);
    }

    private static void SetPanelActive(RectTransform panel, bool active)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private static void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static bool IsGameplayFocus(PlayableScreenFocus focus)
    {
        return focus == PlayableScreenFocus.DefenseFocus || focus == PlayableScreenFocus.DungeonFocus;
    }

    private static bool IsOverlayFocus(PlayableScreenFocus focus)
    {
        return focus == PlayableScreenFocus.InventoryOverlay ||
               focus == PlayableScreenFocus.CraftingOverlay ||
               focus == PlayableScreenFocus.RewardOverlay;
    }

    private static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private readonly struct PanelLayout
    {
        public readonly Vector2 anchorMin;
        public readonly Vector2 anchorMax;
        public readonly Vector2 offsetMin;
        public readonly Vector2 offsetMax;

        public PanelLayout(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.offsetMin = offsetMin;
            this.offsetMax = offsetMax;
        }

        public static PanelLayout FromAnchors(float minX, float maxX)
        {
            return new PanelLayout(new Vector2(minX, 0f), new Vector2(maxX, 1f), Vector2.zero, Vector2.zero);
        }

        public static PanelLayout Lerp(PanelLayout from, PanelLayout to, float t)
        {
            return new PanelLayout(
                Vector2.Lerp(from.anchorMin, to.anchorMin, t),
                Vector2.Lerp(from.anchorMax, to.anchorMax, t),
                Vector2.Lerp(from.offsetMin, to.offsetMin, t),
                Vector2.Lerp(from.offsetMax, to.offsetMax, t));
        }
    }
}
