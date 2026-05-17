using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SurvivalEndGame : MonoBehaviour
{
    private const string CollectedSausageCountKey = "CollectedSausageCount";

    [Header("References")]
    [SerializeField] private Transform chainRoot;
    [SerializeField] private GameObject sausagePrefab;
    [SerializeField] private Sprite fallingSausageSprite;
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private Text statusText;
    [SerializeField] private Text countText;

    [Header("Sausage Chain")]
    [SerializeField] private int startSausages = 8;
    [SerializeField] private int sausagesNeededToSurvive = 5;
    [SerializeField] private float sausageSpacing = 0.55f;
    [SerializeField] private Vector3 sausageSpriteScale = new Vector3(1.25f, 1.25f, 1f);

    [Header("Falling")]
    [SerializeField] private float horizontalSpeed = 4f;
    [SerializeField] private float fallSpeed = 1.2f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float landingY = -5f;

    [Header("Birds")]
    [SerializeField] private float minBirdInterval = 0.8f;
    [SerializeField] private float maxBirdInterval = 1.6f;
    [SerializeField] private float birdSpeed = 7f;
    [SerializeField] private float birdSpawnX = 8f;
    [SerializeField] private float birdVerticalRange = 2.5f;

    private readonly List<SurvivalSausage> sausages = new();
    private float birdTimer;
    private bool isFinished;

    private void Start()
    {
        ApplyCollectedSausageCount();
        EnsureReferences();
        CreateChain();
        ResetBirdTimer();
        UpdateText("Fenster geschafft. Jetzt nicht wegpicken lassen.");
    }

    private void Update()
    {
        if (isFinished)
        {
            return;
        }

        MoveChain();
        UpdateBirds();
        CheckLanding();
    }

    public void StealSausage(SurvivalSausage sausage)
    {
        if (sausage == null || !sausages.Remove(sausage))
        {
            return;
        }

        Destroy(sausage.gameObject);
        UpdateChainPositions();
        UpdateText("Ein Vogel hat eine Wurst geklaut.");
    }

    private void CreateChain()
    {
        if (chainRoot == null)
        {
            return;
        }

        for (int i = 0; i < startSausages; i++)
        {
            GameObject instance = CreateSausage();
            instance.transform.SetParent(chainRoot, false);
            SurvivalSausage sausage = instance.GetComponent<SurvivalSausage>();

            if (sausage == null)
            {
                sausage = instance.AddComponent<SurvivalSausage>();
            }

            sausages.Add(sausage);
        }

        UpdateChainPositions();
    }

    private void ApplyCollectedSausageCount()
    {
        if (!PlayerPrefs.HasKey(CollectedSausageCountKey))
        {
            return;
        }

        startSausages = Mathf.Max(0, PlayerPrefs.GetInt(CollectedSausageCountKey));
        PlayerPrefs.DeleteKey(CollectedSausageCountKey);
    }

    private void UpdateChainPositions()
    {
        for (int i = 0; i < sausages.Count; i++)
        {
            Transform sausageTransform = sausages[i].transform;
            sausageTransform.localPosition = new Vector3(0f, -i * sausageSpacing, 0f);

            if (sausages[i].GetComponent<SpriteRenderer>() == null)
            {
                sausageTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else
            {
                sausageTransform.localRotation = Quaternion.identity;
            }
        }

        UpdateCountText();
    }

    private void MoveChain()
    {
        Vector2 input = GetMoveInput();
        Vector3 position = chainRoot.position;
        position.x = Mathf.Clamp(position.x + input.x * horizontalSpeed * Time.deltaTime, minX, maxX);
        position.y -= fallSpeed * Time.deltaTime;
        chainRoot.position = position;
    }

    private Vector2 GetMoveInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        return new Vector2(x, 0f);
    }

    private void UpdateBirds()
    {
        birdTimer -= Time.deltaTime;

        if (birdTimer > 0f)
        {
            return;
        }

        SpawnBird();
        ResetBirdTimer();
    }

    private void SpawnBird()
    {
        if (chainRoot == null)
        {
            return;
        }

        bool fromLeft = Random.value < 0.5f;
        float x = fromLeft ? -birdSpawnX : birdSpawnX;
        float y = chainRoot.position.y + Random.Range(-birdVerticalRange, birdVerticalRange);
        Vector3 spawnPosition = new(x, y, 0f);
        Vector3 direction = fromLeft ? Vector3.right : Vector3.left;

        GameObject bird = CreateBird(spawnPosition);
        SurvivalBird survivalBird = bird.GetComponent<SurvivalBird>();

        if (survivalBird == null)
        {
            survivalBird = bird.AddComponent<SurvivalBird>();
        }

        survivalBird.Initialize(this, direction, birdSpeed);
    }

    private void CheckLanding()
    {
        if (chainRoot.position.y > landingY)
        {
            return;
        }

        isFinished = true;

        if (sausages.Count >= sausagesNeededToSurvive)
        {
            UpdateText("Überlebt. Die Wurstkette war lang genug.");
        }
        else
        {
            UpdateText("Zu kurz gelandet. Mehr Würstchen hätten geholfen.");
        }
    }

    private void ResetBirdTimer()
    {
        birdTimer = Random.Range(minBirdInterval, maxBirdInterval);
    }

    private void EnsureReferences()
    {
        EnsureCameraAndWorld();

        if (chainRoot == null)
        {
            GameObject chainObject = new("Sausage Chain");
            chainObject.transform.position = new Vector3(0f, 4.2f, 0f);
            chainRoot = chainObject.transform;
        }

        if (statusText == null || countText == null)
        {
            CreateSimpleUi();
        }
    }

    private void EnsureCameraAndWorld()
    {
        if (Camera.main == null)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -12f);
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.53f, 0.78f, 0.94f);
        }

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (GameObject.Find("Survival Ground") == null)
        {
            CreateWorldCube("Factory Wall", new Vector3(0f, 4.2f, 1.2f), new Vector3(12f, 4f, 0.4f), new Color(0.55f, 0.55f, 0.52f));
            CreateWorldCube("Window Opening", new Vector3(0f, 4.2f, 0.85f), new Vector3(3.6f, 2.2f, 0.25f), new Color(0.53f, 0.78f, 0.94f));
            CreateWorldCube("Survival Ground", new Vector3(0f, -5.8f, 1f), new Vector3(14f, 0.6f, 2.5f), new Color(0.20f, 0.58f, 0.24f));
        }
    }

    private GameObject CreateSausage()
    {
        GameObject sausage = fallingSausageSprite != null
            ? CreateSpriteSausage()
            : CreateCapsuleSausage();

        Collider collider = sausage.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Rigidbody body = sausage.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = sausage.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
        return sausage;
    }

    private GameObject CreateSpriteSausage()
    {
        GameObject sausage = new("Survival Sausage");
        SpriteRenderer spriteRenderer = sausage.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = fallingSausageSprite;
        spriteRenderer.sortingOrder = 5;

        sausage.transform.localScale = sausageSpriteScale;
        sausage.AddComponent<BoxCollider>();
        return sausage;
    }

    private GameObject CreateCapsuleSausage()
    {
        GameObject sausage = sausagePrefab != null
            ? Instantiate(sausagePrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        sausage.name = "Survival Sausage";
        sausage.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
        SetMaterialColor(sausage, new Color(0.85f, 0.28f, 0.18f));
        return sausage;
    }

    private GameObject CreateBird(Vector3 spawnPosition)
    {
        GameObject bird = birdPrefab != null
            ? Instantiate(birdPrefab, spawnPosition, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        bird.name = "Survival Bird";
        bird.transform.position = spawnPosition;
        bird.transform.localScale = new Vector3(0.28f, 0.18f, 0.55f);
        SetMaterialColor(bird, new Color(0.08f, 0.08f, 0.08f));

        Collider collider = bird.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Rigidbody body = bird.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = bird.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
        return bird;
    }

    private void CreateSimpleUi()
    {
        GameObject canvasObject = new("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        if (statusText == null)
        {
            statusText = CreateText("Status Text", canvasObject.transform, new Vector2(0f, -30f), 28);
        }

        if (countText == null)
        {
            countText = CreateText("Count Text", canvasObject.transform, new Vector2(0f, -72f), 24);
        }
    }

    private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int fontSize)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperCenter;
        text.color = Color.white;

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(0f, 40f);

        return text;
    }

    private void SetMaterialColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.material.color = color;
    }

    private void CreateWorldCube(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        SetMaterialColor(cube, color);
    }

    private void UpdateText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        UpdateCountText();
    }

    private void UpdateCountText()
    {
        if (countText != null)
        {
            countText.text = $"Würste: {sausages.Count}/{sausagesNeededToSurvive}";
        }
    }
}
