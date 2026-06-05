using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeWindowExit : MonoBehaviour
{
    private const string CollectedSausageCountKey = "CollectedSausageCount";

    [Header("Scene")]
    [SerializeField] private string survivalSceneName = "SausageSurvivalEnd";

    [Header("References")]
    [SerializeField] private ChefThrowSpawner chefThrowSpawner;
    [SerializeField] private Transform windowTransform;
    [SerializeField] private Transform windowHingeTransform;
    [SerializeField] private Collider exitTrigger;

    [Header("Window Timing")]
    [SerializeField] private float minWindowEventDelay = 30f;
    [SerializeField] private float maxWindowEventDelay = 50f;
    [SerializeField] private float minWindowOpenDuration = 4f;
    [SerializeField] private float maxWindowOpenDuration = 7f;

    [Header("Window Animation")]
    [SerializeField] private Vector3 openLocalEulerOffset = new Vector3(-75f, 0f, 0f);
    [SerializeField] private float windowRotationSpeed = 240f;
    [SerializeField] private Vector3 windowHitLocalOffset = new Vector3(0f, -0.15f, 0.05f);

    [Header("Landing Lane")]
    [SerializeField] private int minLandingLaneIndex = 1;
    [SerializeField] private int maxLandingLaneIndex = 2;
    [SerializeField] private float laneTargetMinXOffset = -0.25f;
    [SerializeField] private float laneTargetMaxXOffset = 0.25f;
    [SerializeField] private float laneTargetYOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool logWindowEvents;

    private Quaternion closedWindowRotation;
    private Quaternion openWindowRotation;
    private Quaternion targetWindowRotation;
    private float windowEventTimer;
    private float windowOpenTimer;
    private bool isWindowOpen;
    private bool isWaitingForImpact;
    private bool isLoading;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (windowTransform == null)
        {
            enabled = false;
            return;
        }

        if (windowHingeTransform == null)
        {
            windowHingeTransform = windowTransform;
        }

        if (exitTrigger != null)
        {
            exitTrigger.isTrigger = true;
        }

        closedWindowRotation = windowHingeTransform.localRotation;
        openWindowRotation = closedWindowRotation * Quaternion.Euler(openLocalEulerOffset);
        targetWindowRotation = closedWindowRotation;
        windowHingeTransform.localRotation = closedWindowRotation;
        SetExitTriggerEnabled(false);
        ScheduleNextWindowEvent();
    }

    private void Update()
    {
        UpdateWindowRotation();

        if (isLoading || Time.timeScale <= 0f || isWaitingForImpact)
        {
            return;
        }

        if (isWindowOpen)
        {
            windowOpenTimer -= Time.deltaTime;

            if (windowOpenTimer <= 0f)
            {
                CloseWindow();
            }

            return;
        }

        windowEventTimer -= Time.deltaTime;

        if (windowEventTimer > 0f)
        {
            return;
        }

        TriggerWindowEvent();
    }

    private void ResolveReferences()
    {
        if (chefThrowSpawner == null)
        {
            chefThrowSpawner = FindFirstObjectByType<ChefThrowSpawner>();
        }

        if (windowTransform == null)
        {
            GameObject windowObject = GameObject.Find("Window");

            if (windowObject != null)
            {
                windowTransform = windowObject.transform;
            }
        }
    }

    private void UpdateWindowRotation()
    {
        if (windowTransform == null)
        {
            return;
        }

        windowHingeTransform.localRotation = Quaternion.RotateTowards(
            windowHingeTransform.localRotation,
            targetWindowRotation,
            windowRotationSpeed * Time.deltaTime);
    }

    private void TriggerWindowEvent()
    {
        if (chefThrowSpawner == null)
        {
            ScheduleNextWindowEvent();
            return;
        }

        bool specialThrowStarted = chefThrowSpawner.TryThrowSpecialObjectToWindow(
            GetWorldPoint(windowHitLocalOffset),
            minLandingLaneIndex,
            maxLandingLaneIndex,
            laneTargetMinXOffset,
            laneTargetMaxXOffset,
            laneTargetYOffset,
            OpenWindowAfterImpact);

        if (!specialThrowStarted)
        {
            ScheduleNextWindowEvent();
            return;
        }

        isWaitingForImpact = true;

        if (logWindowEvents)
        {
            Debug.Log("Chef triggered the escape window event.", this);
        }
    }

    private void OpenWindowAfterImpact()
    {
        isWaitingForImpact = false;
        isWindowOpen = true;
        windowOpenTimer = Random.Range(minWindowOpenDuration, maxWindowOpenDuration);
        targetWindowRotation = openWindowRotation;
        SetExitTriggerEnabled(true);

        if (logWindowEvents)
        {
            Debug.Log($"Escape window opened for {windowOpenTimer:0.0}s.", this);
        }
    }

    private void CloseWindow()
    {
        isWindowOpen = false;
        targetWindowRotation = closedWindowRotation;
        SetExitTriggerEnabled(false);
        ScheduleNextWindowEvent();

        if (logWindowEvents)
        {
            Debug.Log("Escape window closed.", this);
        }
    }

    private void ScheduleNextWindowEvent()
    {
        float minDelay = Mathf.Max(0.01f, minWindowEventDelay);
        float maxDelay = Mathf.Max(minDelay, maxWindowEventDelay);
        windowEventTimer = Random.Range(minDelay, maxDelay);
        isWaitingForImpact = false;
    }

    private void SetExitTriggerEnabled(bool isEnabled)
    {
        if (exitTrigger == null)
        {
            return;
        }

        exitTrigger.enabled = isEnabled;
    }

    private Vector3 GetWorldPoint(Vector3 localOffset)
    {
        if (windowTransform == null)
        {
            return localOffset;
        }

        return windowTransform.TransformPoint(localOffset);
    }

    public void HandleWindowTrigger(Collider other)
    {
        TryLoadSurvivalScene(other);
    }

    private void TryLoadSurvivalScene(Collider other)
    {
        if (!isWindowOpen || isLoading || other == null)
        {
            return;
        }

        SausageMovement sausageMovement = other.GetComponentInParent<SausageMovement>();

        if (sausageMovement == null)
        {
            return;
        }

        SaveCollectedSausageCount(sausageMovement);
        GameHandler.Instance?.SaveScoreForNextScene();
        isLoading = true;
        SceneManager.LoadScene(survivalSceneName);
    }

    private void SaveCollectedSausageCount(SausageMovement sausageMovement)
    {
        SausageChainController chainController = sausageMovement.GetComponent<SausageChainController>();
        int collectedCount = chainController != null ? chainController.SegmentCount : 0;
        int survivalSausageCount = collectedCount + 1;

        PlayerPrefs.SetInt(CollectedSausageCountKey, survivalSausageCount);
        PlayerPrefs.Save();
    }

    private void OnValidate()
    {
        minWindowEventDelay = Mathf.Max(0.01f, minWindowEventDelay);
        maxWindowEventDelay = Mathf.Max(minWindowEventDelay, maxWindowEventDelay);
        minWindowOpenDuration = Mathf.Max(0.01f, minWindowOpenDuration);
        maxWindowOpenDuration = Mathf.Max(minWindowOpenDuration, maxWindowOpenDuration);
        windowRotationSpeed = Mathf.Max(1f, windowRotationSpeed);
        maxLandingLaneIndex = Mathf.Max(minLandingLaneIndex, maxLandingLaneIndex);
    }
}
