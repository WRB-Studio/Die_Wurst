using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SurvivalSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/SausageSurvivalEnd.unity";

    [MenuItem("Tools/Die Wurst/Build Survival End Scene")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.78f);

        Material sausageMaterial = CreateMaterial("Survival_Sausage", new Color(0.85f, 0.28f, 0.18f));
        Material birdMaterial = CreateMaterial("Survival_Bird", new Color(0.08f, 0.08f, 0.08f));
        Material wallMaterial = CreateMaterial("Survival_Wall", new Color(0.55f, 0.55f, 0.52f));
        Material grassMaterial = CreateMaterial("Survival_Ground", new Color(0.20f, 0.58f, 0.24f));

        CreateCamera();
        CreateLight();
        CreateWorld(wallMaterial, grassMaterial);

        GameObject chainRoot = new("Sausage Chain");
        chainRoot.transform.position = new Vector3(0f, 4.2f, 0f);

        GameObject sausagePrefab = CreateSausagePrefab(sausageMaterial);
        GameObject birdPrefab = CreateBirdPrefab(birdMaterial);
        Text[] uiTexts = CreateUi();

        GameObject manager = new("Survival End Game");
        SurvivalEndGame endGame = manager.AddComponent<SurvivalEndGame>();
        SerializedObject serializedEndGame = new(endGame);
        serializedEndGame.FindProperty("chainRoot").objectReferenceValue = chainRoot.transform;
        serializedEndGame.FindProperty("sausagePrefab").objectReferenceValue = sausagePrefab;
        serializedEndGame.FindProperty("birdPrefab").objectReferenceValue = birdPrefab;
        serializedEndGame.FindProperty("statusText").objectReferenceValue = uiTexts[0];
        serializedEndGame.FindProperty("countText").objectReferenceValue = uiTexts[1];
        serializedEndGame.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {ScenePath}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -12f);
        cameraObject.transform.rotation = Quaternion.identity;
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.53f, 0.78f, 0.94f);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateWorld(Material wallMaterial, Material grassMaterial)
    {
        CreateCube("Factory Wall", new Vector3(0f, 4.2f, 1.2f), new Vector3(12f, 4f, 0.4f), wallMaterial);
        CreateCube("Window Hole", new Vector3(0f, 4.2f, 0.9f), new Vector3(3.6f, 2.2f, 0.35f), grassMaterial);
        CreateCube("Ground", new Vector3(0f, -5.8f, 1f), new Vector3(14f, 0.6f, 2.5f), grassMaterial);
        CreateCube("Safe Landing Mark", new Vector3(0f, -5.35f, 0f), new Vector3(4f, 0.12f, 0.2f), grassMaterial);
    }

    private static GameObject CreateSausagePrefab(Material material)
    {
        GameObject prefabRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        prefabRoot.name = "Survival Sausage Prefab";
        prefabRoot.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
        prefabRoot.GetComponent<Renderer>().sharedMaterial = material;
        prefabRoot.AddComponent<SurvivalSausage>();

        CapsuleCollider collider = prefabRoot.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;

        Rigidbody body = prefabRoot.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        return SavePrefab(prefabRoot, "Assets/_Project/Prefabs/SurvivalSausage.prefab");
    }

    private static GameObject CreateBirdPrefab(Material material)
    {
        GameObject prefabRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        prefabRoot.name = "Survival Bird Prefab";
        prefabRoot.transform.localScale = new Vector3(0.28f, 0.18f, 0.55f);
        prefabRoot.GetComponent<Renderer>().sharedMaterial = material;
        prefabRoot.AddComponent<SurvivalBird>();

        CapsuleCollider collider = prefabRoot.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;

        Rigidbody body = prefabRoot.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        return SavePrefab(prefabRoot, "Assets/_Project/Prefabs/SurvivalBird.prefab");
    }

    private static Text[] CreateUi()
    {
        GameObject canvasObject = new("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        Text statusText = CreateText("Status Text", canvasObject.transform, new Vector2(0f, -30f), 28, TextAnchor.UpperCenter);
        Text countText = CreateText("Count Text", canvasObject.transform, new Vector2(0f, -72f), 24, TextAnchor.UpperCenter);
        return new[] { statusText, countText };
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int size, TextAnchor alignment)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(0f, 40f);

        return text;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = name;
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject SavePrefab(GameObject source, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab;
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == ScenePath)
            {
                return;
            }
        }

        EditorBuildSettingsScene[] updatedScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updatedScenes, 0);
        updatedScenes[^1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }
}
