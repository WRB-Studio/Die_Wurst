using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SurvivalEndGame : MonoBehaviour
{
    private const string CollectedSausageCountKey = "CollectedSausageCount";

    private enum SurvivalPhase
    {
        Falling,
        Running,
        Finished
    }

    [Header("References")]
    [SerializeField] private Transform chainRoot;
    [SerializeField] private GameObject mainSausagePrefab;
    [SerializeField] private GameObject extraSausagePrefab;
    [SerializeField] private GameObject groundMainSausagePrefab;
    [SerializeField] private GameObject groundExtraSausagePrefab;
    [SerializeField] private BirdController birdController;
    [SerializeField] private SurvivalChainController chainController;

    [Header("Start Setup")]
    [SerializeField] private float startSpawnY = 4.2f;

    [Header("Falling")]
    [SerializeField] private float horizontalSpeed = 4f;
    [SerializeField] private float fallSpeed = 1.2f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float landingY = -5f;

    [Header("Ground Run")]
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float leftEscapeX = -5f;
    [SerializeField] private float rightEscapeX = 5f;
    [SerializeField] private Color escapeGizmoColor = new Color(0.95f, 0.35f, 0.2f, 0.9f);

    [Header("Camera")]
    [SerializeField] private SurvivalCameraFollow cameraFollow;

    private readonly List<SurvivalSausage> sausages = new List<SurvivalSausage>();

    private SurvivalPhase phase;
    private float lastHorizontalDirection = 1f;
    private int initialExtraSausagesOverride = -1;

    public Vector3 ChainPosition => chainRoot != null ? chainRoot.position : Vector3.zero;

    private void Start()
    {
        ApplyCollectedSausageCount();
        EnsureSceneSetup();
        ConfigureChainController();
        CreateChain();
        ConfigureCameraFollow();
        ConfigureBirdController();
        ConfigureGameHandler();
        chainController?.SetAirPhase();
        phase = SurvivalPhase.Falling;
    }

    private void Update()
    {
        if (phase == SurvivalPhase.Finished)
        {
            return;
        }

        if (phase == SurvivalPhase.Falling)
        {
            UpdateFallingPhase();
            return;
        }

        if (phase == SurvivalPhase.Running)
        {
            UpdateRunningPhase();
        }
    }

    public bool HandleBirdHit(SurvivalSausage sausage, SurvivalBird bird)
    {
        if (phase != SurvivalPhase.Falling || sausage == null || bird == null)
        {
            return false;
        }

        if (sausage.IsMainSausage)
        {
            GameHandler.Instance?.RegisterHit("sfx_ouch_short");
            bird.MarkForRemoval();
            FinishAsGameOver();
            return true;
        }

        if (!sausages.Remove(sausage))
        {
            return false;
        }

        chainController?.RemoveSausage(sausage);
        GameHandler.Instance?.RegisterHit("sfx_ouch_short");
        sausage.Consume();
        bird.StartRetreat();
        return true;
    }

    public float GetBirdSpawnY()
    {
        return chainController != null ? chainController.GetMainWorldPosition().y : (chainRoot != null ? chainRoot.position.y : 0f);
    }

    public float GetHorizontalCameraEdge(bool leftEdge)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return leftEdge ? minX : maxX;
        }

        float distanceToGameplayPlane = Mathf.Abs(mainCamera.transform.position.z);
        float viewportX = leftEdge ? 0f : 1f;
        Vector3 edgePosition = mainCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, distanceToGameplayPlane));
        return edgePosition.x;
    }

    private void UpdateFallingPhase()
    {
        Vector2 input = GetMoveInput();
        Vector3 position = GetControlledPosition();

        if (Mathf.Abs(input.x) > 0.01f)
        {
            GetMainSausage()?.SetFacingDirection(input.x);
        }

        position.x = Mathf.Clamp(position.x + input.x * horizontalSpeed * Time.deltaTime, minX, maxX);
        position.y -= fallSpeed * Time.deltaTime;
        SetControlledPosition(position);

        if (position.y <= landingY)
        {
            BeginGroundRun();
        }
    }

    private void UpdateRunningPhase()
    {
        Vector2 input = GetMoveInput();
        float inputX = input.x;
        SurvivalSausage mainSausage = GetMainSausage();

        if (Mathf.Abs(inputX) > 0.01f)
        {
            lastHorizontalDirection = Mathf.Sign(inputX);
            chainController?.SetGroundDirection(lastHorizontalDirection);
            mainSausage?.SetFacingDirection(inputX);
        }

        if (mainSausage != null)
        {
            mainSausage.SetRunning(Mathf.Abs(inputX) > 0.01f);
        }

        Vector3 position = GetControlledPosition();
        position.x += inputX * runSpeed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, leftEscapeX, rightEscapeX);
        position.y = landingY;
        SetControlledPosition(position);

        if (position.x <= leftEscapeX || position.x >= rightEscapeX)
        {
            FinishAsSuccess();
        }
    }

    private void BeginGroundRun()
    {
        phase = SurvivalPhase.Running;
        ReplaceSausagesForGroundPhase();

        Vector3 mainPosition = GetControlledPosition();
        mainPosition.y = landingY;
        SetControlledPosition(mainPosition);
        chainController?.SetGroundPhase();
    }

    private void FinishAsGameOver()
    {
        phase = SurvivalPhase.Finished;
        StopBirds();
        GameHandler.Instance?.ShowGameOver();
    }

    private void FinishAsSuccess()
    {
        phase = SurvivalPhase.Finished;
        StopBirds();
        GameHandler.Instance?.RegisterEscape();
        GameHandler.Instance?.RegisterRescuedSausages(sausages.Count);
        GameHandler.Instance?.ShowGameWin();
    }

    private void StopBirds()
    {
        if (birdController == null)
        {
            return;
        }

        birdController.SetSpawningEnabled(false);
        birdController.ClearBirds();
    }

    private void ConfigureGameHandler()
    {
        if (GameHandler.Instance == null)
        {
            GameObject gameHandlerObject = new GameObject("GameHandler");
            gameHandlerObject.AddComponent<GameHandler>();
        }
    }

    private void ConfigureBirdController()
    {
        if (birdController == null)
        {
            birdController = GetComponent<BirdController>();
        }

        if (birdController == null)
        {
            birdController = FindFirstObjectByType<BirdController>();
        }

        if (birdController == null)
        {
            birdController = gameObject.AddComponent<BirdController>();
        }

        birdController.Initialize(this);
    }

    private void ConfigureChainController()
    {
        if (chainController == null)
        {
            chainController = GetComponent<SurvivalChainController>();
        }

        if (chainController == null)
        {
            chainController = FindFirstObjectByType<SurvivalChainController>();
        }

        if (chainController == null)
        {
            chainController = gameObject.AddComponent<SurvivalChainController>();
        }

        if (initialExtraSausagesOverride >= 0)
        {
            chainController.SetInitialExtraSausages(initialExtraSausagesOverride);
        }

        chainController.SetGroundDirection(lastHorizontalDirection);
    }

    private void CreateChain()
    {
        SurvivalSausage mainSausage = CreateSausage(true);
        mainSausage.Initialize(this, true);
        mainSausage.SetLocalPositionImmediate(Vector3.zero);
        sausages.Add(mainSausage);
        chainController?.RegisterSausage(mainSausage);

        int extraCount = chainController != null ? chainController.InitialExtraSausages : 0;

        for (int i = 0; i < extraCount; i++)
        {
            SurvivalSausage sausage = CreateSausage(false);
            sausage.Initialize(this, false);
            sausages.Add(sausage);
            chainController?.RegisterSausage(sausage);
        }
    }

    private void ApplyCollectedSausageCount()
    {
        if (!PlayerPrefs.HasKey(CollectedSausageCountKey))
        {
            return;
        }

        int totalSausages = Mathf.Max(1, PlayerPrefs.GetInt(CollectedSausageCountKey));
        initialExtraSausagesOverride = Mathf.Max(0, totalSausages - 1);
        PlayerPrefs.DeleteKey(CollectedSausageCountKey);
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

    private void EnsureSceneSetup()
    {
        EnsureCameraAndWorld();
        HideGeneratedWorld();

        if (chainRoot == null)
        {
            GameObject chainObject = new GameObject("Sausage Chain");
            chainRoot = chainObject.transform;
        }

        Vector3 chainPosition = chainRoot.position;
        chainPosition.y = startSpawnY;
        chainRoot.position = chainPosition;
    }

    private void EnsureCameraAndWorld()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -12f);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 6f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.53f, 0.78f, 0.94f);
        }

        if (mainCamera != null && FindFirstObjectByType<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private void ConfigureCameraFollow()
    {
        if (chainRoot == null)
        {
            return;
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<SurvivalCameraFollow>();
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetCamera(Camera.main);
            cameraFollow.SetTarget(chainController != null ? chainController.MainSausageTransform : chainRoot);
        }
    }

    private void OnValidate()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<SurvivalCameraFollow>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        float minY = landingY - 2f;
        float maxY = landingY + 2f;

        Gizmos.color = escapeGizmoColor;
        Gizmos.DrawLine(new Vector3(leftEscapeX, minY, 0f), new Vector3(leftEscapeX, maxY, 0f));
        Gizmos.DrawLine(new Vector3(rightEscapeX, minY, 0f), new Vector3(rightEscapeX, maxY, 0f));
    }

    private void HideGeneratedWorld()
    {
        HideObject("Factory Wall");
        HideObject("Window Hole");
        HideObject("Window Opening");
        HideObject("Ground");
        HideObject("Survival Ground");
        HideObject("Safe Landing Mark");
    }

    private void HideObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);

        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private SurvivalSausage CreateSausage(bool isMainSausage)
    {
        GameObject prefab = isMainSausage ? mainSausagePrefab : extraSausagePrefab;

        if (prefab == null)
        {
            Debug.LogError(isMainSausage ? "Main survival sausage prefab is missing." : "Extra survival sausage prefab is missing.", this);
            GameObject fallbackObject = new GameObject("Missing Survival Sausage");
            fallbackObject.transform.SetParent(chainRoot, false);
            SurvivalSausage fallbackSausage = fallbackObject.AddComponent<SurvivalSausage>();
            return fallbackSausage;
        }

        GameObject sausageObject = Instantiate(prefab, chainRoot);
        sausageObject.name = isMainSausage ? "Survival Main Sausage" : "Survival Extra Sausage";

        SurvivalSausage sausage = sausageObject.GetComponent<SurvivalSausage>();

        if (sausage == null)
        {
            sausage = sausageObject.AddComponent<SurvivalSausage>();
        }

        return sausage;
    }

    private void ReplaceSausagesForGroundPhase()
    {
        Vector3 mainWorldPositionBeforeSwap = GetControlledPosition();
        SurvivalSausage replacementMainSausage = null;

        for (int i = 0; i < sausages.Count; i++)
        {
            SurvivalSausage oldSausage = sausages[i];

            if (oldSausage == null)
            {
                continue;
            }

            GameObject groundPrefab = oldSausage.IsMainSausage ? groundMainSausagePrefab : groundExtraSausagePrefab;

            if (groundPrefab == null)
            {
                continue;
            }

            Vector3 worldPosition = oldSausage.transform.position;
            Quaternion worldRotation = oldSausage.transform.rotation;
            Vector3 localScale = oldSausage.transform.localScale;
            Vector3 localPosition = oldSausage.transform.localPosition;

            GameObject replacementObject = Instantiate(groundPrefab);
            replacementObject.name = oldSausage.IsMainSausage ? "Ground Main Sausage" : "Ground Extra Sausage";
            replacementObject.transform.SetParent(chainRoot, true);
            replacementObject.transform.position = worldPosition;
            replacementObject.transform.rotation = worldRotation;
            replacementObject.transform.localScale = localScale;
            replacementObject.transform.localPosition = localPosition;

            SurvivalSausage replacement = replacementObject.GetComponent<SurvivalSausage>();

            if (replacement == null)
            {
                replacement = replacementObject.AddComponent<SurvivalSausage>();
            }

            replacement.Initialize(this, oldSausage.IsMainSausage);
            replacement.SetLocalPositionImmediate(localPosition);
            chainController?.ReplaceSausage(oldSausage, replacement);
            sausages[i] = replacement;

            if (replacement.IsMainSausage)
            {
                replacementMainSausage = replacement;
            }

            Destroy(oldSausage.gameObject);
        }

        if (replacementMainSausage != null)
        {
            replacementMainSausage.transform.position = mainWorldPositionBeforeSwap;
        }
    }

    private SurvivalSausage GetMainSausage()
    {
        return sausages.Count > 0 ? sausages[0] : null;
    }

    private Vector3 GetControlledPosition()
    {
        return chainController != null ? chainController.GetMainWorldPosition() : chainRoot.position;
    }

    private void SetControlledPosition(Vector3 position)
    {
        if (chainController != null)
        {
            chainController.SetMainWorldPosition(position);
            return;
        }

        chainRoot.position = position;
    }
}
