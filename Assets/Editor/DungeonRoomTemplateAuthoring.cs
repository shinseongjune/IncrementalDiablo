using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the two neutral, NavMesh-baked E3-D room templates and connects them to the persistent
/// Gameplay loader. This deliberately authors only the functional route; art direction remains a
/// separate Unity pass.
/// </summary>
internal static class DungeonRoomTemplateAuthoring
{
    private const string RoomDirectory = "Assets/01.Scenes/DungeonRooms";
    private const string RoomAPath = RoomDirectory + "/DungeonRoom_Crypt_A.unity";
    private const string RoomBPath = RoomDirectory + "/DungeonRoom_Crypt_B.unity";
    private const string GameplayPath = "Assets/01.Scenes/Gameplay.unity";

    [MenuItem("Tools/Incremental Diablo/E3-D/Create Contract Room Templates")]
    private static void CreateContractRoomTemplates()
    {
        Directory.CreateDirectory(RoomDirectory);
        AssetDatabase.Refresh();

        CreateRoom("crypt_a", RoomAPath, new Vector2(18f, 18f), new Vector3(0f, 0f, 2f));
        CreateRoom("crypt_b", RoomBPath, new Vector2(24f, 14f), new Vector3(-3f, 0f, 1f));
        ConfigurePersistentGameplay();
        ApplyDungeonCameraFraming();
        EnsureScenesInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("E3-D authored two additive room templates, baked their NavMeshes, and connected Gameplay/DungeonRoot to the catalog.");
    }

    private static void CreateRoom(string templateId, string scenePath, Vector2 size, Vector3 obstaclePosition)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("DungeonRoom_" + templateId);
        DungeonRoomTemplate template = root.AddComponent<DungeonRoomTemplate>();
        NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;

        CreateCube("WalkableFloor", root.transform, new Vector3(0f, -0.5f, 0f), new Vector3(size.x, 1f, size.y));
        CreateCube("NorthWall", root.transform, new Vector3(0f, 1.5f, size.y * 0.5f), new Vector3(size.x, 3f, 1f));
        CreateCube("WestWall", root.transform, new Vector3(-size.x * 0.5f, 1.5f, 0f), new Vector3(1f, 3f, size.y));
        CreateCube("EastWall", root.transform, new Vector3(size.x * 0.5f, 1.5f, 0f), new Vector3(1f, 3f, size.y));
        CreateCube("RouteObstacle", root.transform, obstaclePosition, new Vector3(2f, 2f, 2f));

        Transform entrance = CreateAnchor("Entrance", root.transform, new Vector3(0f, 0f, -size.y * 0.5f + 2f), Quaternion.identity);
        Transform returnPoint = CreateAnchor("ReturnPortalPoint", root.transform, new Vector3(-size.x * 0.25f, 0f, size.y * 0.5f - 2.5f), Quaternion.identity);
        Transform deeperPoint = CreateAnchor("DeeperExitPoint", root.transform, new Vector3(size.x * 0.25f, 0f, size.y * 0.5f - 2.5f), Quaternion.identity);
        Transform spawnA = CreateAnchor("EnemySpawn_A", root.transform, new Vector3(-size.x * 0.25f, 0f, 0f), Quaternion.identity);
        Transform spawnB = CreateAnchor("EnemySpawn_B", root.transform, new Vector3(size.x * 0.25f, 0f, 1f), Quaternion.identity);

        ReturnPortal returnPortal = CreateExit<ReturnPortal>("ReturnPortal", returnPoint, new Color(0.2f, 0.75f, 1f));
        DeeperExit deeperExit = CreateExit<DeeperExit>("DeeperExit", deeperPoint, new Color(1f, 0.55f, 0.15f));
        SetTemplateReferences(template, templateId, entrance, returnPoint, deeperPoint, returnPortal, deeperExit, spawnA, spawnB);

        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void ConfigurePersistentGameplay()
    {
        Scene gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
        GameObject dungeonRoot = GameObject.Find("DungeonRoot");
        ExpeditionDirector expedition = Object.FindAnyObjectByType<ExpeditionDirector>();
        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>();
        CombatRoom combatRoom = Object.FindAnyObjectByType<CombatRoom>();
        EnemySpawner enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();

        if (dungeonRoot == null || expedition == null || playerController == null || combatRoom == null || enemySpawner == null)
        {
            throw new System.InvalidOperationException("E3-D setup requires DungeonRoot, ExpeditionDirector, PlayerController, CombatRoom, and EnemySpawner in Gameplay.");
        }

        DungeonRoomLoader loader = dungeonRoot.GetComponent<DungeonRoomLoader>();
        if (loader == null)
        {
            loader = dungeonRoot.AddComponent<DungeonRoomLoader>();
        }

        Transform hubReturnPoint = dungeonRoot.transform.Find("DungeonHubReturnPoint");
        if (hubReturnPoint == null)
        {
            GameObject hubPoint = new GameObject("DungeonHubReturnPoint");
            hubReturnPoint = hubPoint.transform;
            hubReturnPoint.SetParent(dungeonRoot.transform, worldPositionStays: true);
        }

        hubReturnPoint.SetPositionAndRotation(playerController.transform.position, playerController.transform.rotation);
        SetLoaderReferences(loader, expedition, playerController.transform, hubReturnPoint);
        SetObjectReference(combatRoom, "roomLoader", loader);
        SetObjectReference(enemySpawner, "roomLoader", loader);

        DungeonTraversalController traversal = Object.FindAnyObjectByType<DungeonTraversalController>();
        if (traversal != null)
        {
            traversal.enabled = false;
            EditorUtility.SetDirty(traversal);
        }

        EditorSceneManager.MarkSceneDirty(gameplay);
        EditorSceneManager.SaveScene(gameplay);
    }

    [MenuItem("Tools/Incremental Diablo/E3-D/Apply Dungeon Camera Framing")]
    private static void ApplyDungeonCameraFraming()
    {
        Scene gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
        GameObject cameraObject = GameObject.Find("Camera_DungeonPanel");
        Camera dungeonCamera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
        if (dungeonCamera == null)
        {
            throw new System.InvalidOperationException("E3-D camera framing requires Camera_DungeonPanel in Gameplay.");
        }

        // The Game panel is close to portrait. A 16.5 orthographic half-height keeps the full 24-unit
        // room width and the north exit anchors in frame at 16:9 without making the choice UI offscreen.
        dungeonCamera.orthographic = true;
        dungeonCamera.orthographicSize = 16.5f;
        dungeonCamera.transform.SetPositionAndRotation(
            new Vector3(0f, 16f, -7.5f),
            Quaternion.Euler(65f, 0f, 0f));

        EditorUtility.SetDirty(dungeonCamera);
        EditorSceneManager.MarkSceneDirty(gameplay);
        EditorSceneManager.SaveScene(gameplay);
        Debug.Log("E3-D dungeon camera now frames the entrance and north portal choices in the panel viewport.");
    }

    private static void EnsureScenesInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddBuildSceneIfMissing(scenes, RoomAPath);
        AddBuildSceneIfMissing(scenes, RoomBPath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddBuildSceneIfMissing(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, worldPositionStays: false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.isStatic = true;
        return cube;
    }

    private static Transform CreateAnchor(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(parent, worldPositionStays: false);
        anchor.transform.SetLocalPositionAndRotation(position, rotation);
        return anchor.transform;
    }

    private static T CreateExit<T>(string name, Transform anchor, Color color) where T : DungeonRoomExit
    {
        GameObject exit = new GameObject(name);
        exit.transform.SetParent(anchor, worldPositionStays: false);
        exit.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SphereCollider trigger = exit.AddComponent<SphereCollider>();
        trigger.radius = 1.25f;
        trigger.isTrigger = true;
        T exitComponent = exit.AddComponent<T>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "ActiveVisual";
        visual.transform.SetParent(exit.transform, worldPositionStays: false);
        visual.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.1f, 0f), Quaternion.identity);
        visual.transform.localScale = new Vector3(1.25f, 0.1f, 1.25f);
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.color = color;
        }

        SerializedObject serializedExit = new SerializedObject(exitComponent);
        SerializedProperty activeVisuals = serializedExit.FindProperty("activeVisuals");
        activeVisuals.arraySize = 1;
        activeVisuals.GetArrayElementAtIndex(0).objectReferenceValue = visual;
        serializedExit.ApplyModifiedPropertiesWithoutUndo();
        exitComponent.SetExitAvailable(false);
        return exitComponent;
    }

    private static void SetTemplateReferences(
        DungeonRoomTemplate template,
        string templateId,
        Transform entrance,
        Transform returnPoint,
        Transform deeperPoint,
        ReturnPortal returnPortal,
        DeeperExit deeperExit,
        params Transform[] spawnAnchors)
    {
        SerializedObject serializedTemplate = new SerializedObject(template);
        serializedTemplate.FindProperty("templateId").stringValue = templateId;
        serializedTemplate.FindProperty("entrancePoint").objectReferenceValue = entrance;
        serializedTemplate.FindProperty("returnPortalPoint").objectReferenceValue = returnPoint;
        serializedTemplate.FindProperty("deeperExitPoint").objectReferenceValue = deeperPoint;
        serializedTemplate.FindProperty("returnPortal").objectReferenceValue = returnPortal;
        serializedTemplate.FindProperty("deeperExit").objectReferenceValue = deeperExit;

        SerializedProperty enemySpawns = serializedTemplate.FindProperty("enemySpawnAnchors");
        enemySpawns.arraySize = spawnAnchors.Length;
        for (int i = 0; i < spawnAnchors.Length; i++)
        {
            enemySpawns.GetArrayElementAtIndex(i).objectReferenceValue = spawnAnchors[i];
        }

        serializedTemplate.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(template);
    }

    private static void SetLoaderReferences(
        DungeonRoomLoader loader,
        ExpeditionDirector expedition,
        Transform player,
        Transform hubReturnPoint)
    {
        SerializedObject serializedLoader = new SerializedObject(loader);
        serializedLoader.FindProperty("expedition").objectReferenceValue = expedition;
        serializedLoader.FindProperty("player").objectReferenceValue = player;
        serializedLoader.FindProperty("returnToHubPoint").objectReferenceValue = hubReturnPoint;

        SerializedProperty catalog = serializedLoader.FindProperty("roomCatalog");
        catalog.arraySize = 2;
        SetCatalogEntry(catalog.GetArrayElementAtIndex(0), "crypt_a", RoomAPath);
        SetCatalogEntry(catalog.GetArrayElementAtIndex(1), "crypt_b", RoomBPath);
        serializedLoader.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loader);
    }

    private static void SetCatalogEntry(SerializedProperty entry, string templateId, string scenePath)
    {
        entry.FindPropertyRelative("templateId").stringValue = templateId;
        entry.FindPropertyRelative("scenePath").stringValue = scenePath;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(propertyName).objectReferenceValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
