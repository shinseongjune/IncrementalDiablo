using UnityEngine;

public class DungeonDebugHud : MonoBehaviour
{
    [SerializeField] private ExpeditionDirector expedition;
    [SerializeField] private CombatRoom combatRoom;
    [SerializeField] private LootDropper lootDropper;
    [SerializeField] private SimpleInventory inventory;
    [SerializeField] private DefenseSaveManager saveManager;
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool showPanel = true;
    [SerializeField] private Rect panelRect = new Rect(16f, 16f, 380f, 312f);
    [SerializeField] private string lastActionMessage;
    [SerializeField] private string lastSaveValidationMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        panelRect.width = Mathf.Max(280f, panelRect.width);
        panelRect.height = Mathf.Max(260f, panelRect.height);
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            return;
        }

        ResolveReferences();

        GUILayout.BeginArea(panelRect, GUI.skin.window);
        GUILayout.Label("Dungeon Loop Debug");

        if (expedition == null)
        {
            GUILayout.Label("ExpeditionDirector: missing");
            if (GUILayout.Button("Refresh References"))
            {
                ResolveReferences(true);
            }

            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"State: {expedition.State} / Depth {expedition.Depth}");
        GUILayout.Label($"Room: {expedition.RoomsCompleted}/{expedition.TotalRooms} / Elapsed {expedition.ElapsedSeconds:0.0}s");
        GUILayout.Label($"Reward Pending: {expedition.RewardPending}");
        GUILayout.Label(string.IsNullOrWhiteSpace(expedition.LastResult) ? "Last: none" : $"Last: {expedition.LastResult}");

        GUILayout.BeginHorizontal();
        if (DrawButton("Start Dungeon", !expedition.IsRunning))
        {
            lastActionMessage = expedition.StartExpedition()
                ? "Started dungeon expedition."
                : "Start failed: expedition is already running.";
        }

        if (DrawButton("Force Clear", expedition.IsRunning))
        {
            lastActionMessage = expedition.CompleteRoom()
                ? "Forced room clear."
                : "Clear failed: expedition must be running.";
        }

        if (DrawButton("Force Fail", expedition.IsRunning))
        {
            lastActionMessage = expedition.FailExpedition()
                ? "Forced expedition failure."
                : "Fail failed: expedition must be running.";
        }

        GUILayout.EndHorizontal();

        if (DrawButton("Grant Pending Reward", expedition.RewardPending))
        {
            lastActionMessage = expedition.TryGrantPendingReward()
                ? "Granted pending reward."
                : "Reward grant failed; check LootDropper and inventory.";
        }

        if (combatRoom != null)
        {
            GUILayout.Label($"CombatRoom: {combatRoom.State} / {combatRoom.LastResult.message}");
        }

        if (lootDropper != null && !string.IsNullOrWhiteSpace(lootDropper.LastDropMessage))
        {
            GUILayout.Label(lootDropper.LastDropMessage);
        }

        GUILayout.Label(inventory == null ? "Inventory: missing" : $"Inventory: {inventory.Count}/{inventory.Capacity}");

        DrawSaveControls();

        if (!string.IsNullOrWhiteSpace(lastActionMessage))
        {
            GUILayout.Label(lastActionMessage);
        }

        if (!string.IsNullOrWhiteSpace(lastSaveValidationMessage))
        {
            GUILayout.Label(lastSaveValidationMessage);
        }

        GUILayout.EndArea();
    }

    private void DrawSaveControls()
    {
        GUILayout.BeginHorizontal();

        if (DrawButton("Save", saveManager != null))
        {
            lastActionMessage = saveManager.TrySave()
                ? "Saved playable loop snapshot."
                : "Save failed; check DefenseSaveManager references.";
            lastSaveValidationMessage = string.Empty;
        }

        if (DrawButton("Load", saveManager != null))
        {
            lastActionMessage = saveManager.TryLoad()
                ? "Loaded playable loop snapshot."
                : "Load failed; save file missing or invalid.";
            lastSaveValidationMessage = string.Empty;
        }

        if (DrawButton("Validate Save", saveManager != null))
        {
            bool valid = saveManager.TryValidateCurrentSaveData(out string report);
            lastActionMessage = valid
                ? "Save snapshot validation passed."
                : "Save snapshot validation found blocking errors.";
            lastSaveValidationMessage = report;
        }

        GUILayout.EndHorizontal();

        if (saveManager == null)
        {
            GUILayout.Label("SaveManager: missing");
        }
    }

    private static bool DrawButton(string label, bool enabled)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && enabled;
        bool clicked = GUILayout.Button(label);
        GUI.enabled = previousEnabled;
        return clicked && enabled;
    }

    private void ResolveReferences(bool force = false)
    {
        if (!autoFindReferences && !force)
        {
            return;
        }

        if (expedition == null || force)
        {
            expedition = FindAnyObjectByType<ExpeditionDirector>();
        }

        if (combatRoom == null || force)
        {
            combatRoom = FindAnyObjectByType<CombatRoom>();
        }

        if (lootDropper == null || force)
        {
            lootDropper = FindAnyObjectByType<LootDropper>();
        }

        if (inventory == null || force)
        {
            inventory = FindAnyObjectByType<SimpleInventory>();
        }

        if (saveManager == null || force)
        {
            saveManager = FindAnyObjectByType<DefenseSaveManager>();
        }
    }
}
