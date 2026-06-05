using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameHandler : MonoBehaviour
{
    private const string EscapeRoomSceneName = "SausageEscapeRoom";
    private const string SurvivalSceneName = "SausageSurvivalEnd";

    public static GameHandler Instance { get; private set; }

    private static bool skipMainMenuOnNextLoad;
    private static readonly int DirectionPropertyId = Shader.PropertyToID("_direction");
    private readonly List<GameObject> persistentSceneRoots = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject creditsScreen;
    [SerializeField] private GameObject gameWinMenu;
    [SerializeField] private GameObject pausedImage;
    [SerializeField] private GameObject gameOverImage;
    [SerializeField] private GameObject scoreImage;
    [SerializeField] private GameObject timeImage;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Button pauseExitButton;
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button mainMenuSettingsButton;
    [SerializeField] private Button mainMenuCreditsButton;
    [SerializeField] private Button mainMenuExitButton;
    [SerializeField] private Button creditsMainMenuButton;
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private TMP_Text timeValueText;

    [Header("Game Win Score Items")]
    [SerializeField] private GameObject collectedScoreItem;
    [SerializeField] private GameObject timeScoreItem;
    [SerializeField] private GameObject hitScoreItem;
    [SerializeField] private GameObject escapeScoreItem;
    [SerializeField] private GameObject rescuedScoreItem;
    [SerializeField] private GameObject finalScoreItem;

    [Header("Game Win Animation")]
    [SerializeField] private float scoreRevealDuration = 4f;
    [SerializeField] private float finalScoreRevealDuration = 0.8f;

    [Header("Gameplay")]
    [SerializeField] private SausageChainController playerChain;
    [SerializeField] private bool showMainMenuOnStart = true;

    [Header("Scoring")]
    [SerializeField] private int collectScore = 100;
    [SerializeField] private int hitPenalty = 50;
    [SerializeField] private int timeScorePerSecond = 10;
    [SerializeField] private int escapeScore = 500;
    [SerializeField] private int rescuedSausageScore = 100;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverMusic;

    [Header("Difficulty")]
    [SerializeField] private float startThrowInterval = 1.2f;
    [SerializeField] private float endThrowInterval = 0.4f;
    [SerializeField] private AnimationCurve throwIntervalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float throwIntervalCurveDuration = 60f;
    [SerializeField] private float throwIntervalRandomOffset = 0.2f;

    [Header("Conveyor")]
    [SerializeField] private float startConveyorSpeed = 1f;
    [SerializeField] private float endConveyorSpeed = 2f;
    [SerializeField] private AnimationCurve conveyorSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float conveyorSpeedCurveDuration = 60f;
    [SerializeField] private float conveyorMaterialSpeedFactor = 1f;
    [SerializeField] private Material conveyorScrollingMaterial;
    [SerializeField] private Material conveyorScrollingMaterialInverted;

    private bool isPaused;
    private bool isGameOver;
    private bool isMainMenuOpen;
    private bool isCreditsOpen;
    private float elapsedGameTime;
    private int collectedSausageCount;
    private int hitCount;
    private int escapeCount;
    private int rescuedSausageCount;
    private int currentScore;
    private bool isGameWinVisible;
    private bool isGameWinCountingFinalScore;
    private bool isGameWinAnimationComplete;
    private float gameWinRevealTimer;
    private float gameWinFinalRevealTimer;

    public int CurrentScore => currentScore;
    public int CollectedSausageCount => collectedSausageCount;
    public int HitCount => hitCount;
    public int EscapeCount => escapeCount;
    public int RescuedSausageCount => rescuedSausageCount;
    public float ElapsedGameTime => elapsedGameTime;
    public int CollectedScore => collectedSausageCount * collectScore;
    public int TimeScore => Mathf.FloorToInt(elapsedGameTime) * timeScorePerSecond;
    public int HitPenaltyScore => hitCount * hitPenalty;
    public int EscapeScoreTotal => escapeCount * escapeScore;
    public int RescuedScoreTotal => rescuedSausageCount * rescuedSausageScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        EnsureEventSystemExists();
        CachePersistentSceneRoots();
        MakePersistentSceneRootsPersistent();
        Instance = this;
        ApplyConveyorMaterialSettings();
        Time.timeScale = 1f;
        RegisterButtons();
    }

    private void Start()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResetRunStats();

        if (skipMainMenuOnNextLoad)
        {
            skipMainMenuOnNextLoad = false;
            StartGame();
            return;
        }

        if (showMainMenuOnStart && mainMenu != null)
        {
            OpenMainMenu();
            return;
        }

        HideAllMenus();
        ResumeGameTime();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    private void Update()
    {
        if (IsGameplayRunning())
        {
            elapsedGameTime += Time.deltaTime;
            RecalculateScore();
            ApplyConveyorMaterialSettings();
        }

        if (isGameWinVisible)
        {
            UpdateGameWinMenuAnimation();
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isCreditsOpen)
        {
            CloseCredits();
            return;
        }

        if (isMainMenuOpen || isGameOver)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    public void HandlePlayerHit()
    {
        if (playerChain == null || isGameOver)
        {
            return;
        }

        if (playerChain.ReleaseLastSegment())
        {
            RegisterHit("sfx_roblox_oof_short");
            return;
        }

        RegisterHit("sfx_ouch_short");
        TriggerGameOver();
    }

    public void ShowGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        TriggerGameOver();
    }

    public void ShowGameWin()
    {
        if (isGameOver)
        {
            return;
        }

        RecalculateScore();
        AudioManager.Instance?.PlaySFX("sfx_yay", false);
        isGameOver = true;
        isPaused = false;
        SetPauseMenuVisible(false);
        HideGameOverStats();
        HideMainAndCredits();
        UpdateGameWinMenu();
        SetGameWinMenuVisible(true);
        StartGameWinMenuAnimation();
        PauseGameTime();
    }

    public void SaveScoreForNextScene()
    {
        RecalculateScore();
    }

    public void RegisterHit(string soundName = "sfx_roblox_oof_short")
    {
        if (isGameOver)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(soundName))
        {
            AudioManager.Instance?.PlaySFX(soundName, false);
        }

        hitCount++;
        RecalculateScore();
    }

    public void SetAutomaticScoringEnabled(bool isEnabled)
    {
        _ = isEnabled;
        UpdateStatsUI();
    }

    public void RegisterCollectedSausage()
    {
        if (isGameOver)
        {
            return;
        }

        collectedSausageCount++;
        RecalculateScore();
    }

    public void RegisterEscape()
    {
        if (isGameOver)
        {
            return;
        }

        escapeCount++;
        RecalculateScore();
    }

    public void RegisterRescuedSausages(int amount)
    {
        if (isGameOver || amount <= 0)
        {
            return;
        }

        rescuedSausageCount += amount;
        RecalculateScore();
    }

    public void PauseGame()
    {
        if (isGameOver || isMainMenuOpen)
        {
            return;
        }

        isPaused = true;
        SetPauseMenuVisible(true);
        UpdatePauseMenuState(showPause: true, showGameOver: false);
        PauseGameTime();
    }

    public void ResumeGame()
    {
        if (isGameOver || isMainMenuOpen)
        {
            return;
        }

        isPaused = false;
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);
        ResumeGameTime();
    }

    public void RestartScene()
    {
        PrepareForSceneReload();
        ResetRunStats();
        AudioManager.Instance?.PlayStartMusic();
        ResumeGameTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReplayGame()
    {
        if (SceneManager.GetActiveScene().name == SurvivalSceneName)
        {
            RestartEscapeRoom();
            return;
        }

        skipMainMenuOnNextLoad = true;
        RestartScene();
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        isGameOver = false;
        OpenMainMenu();
    }

    public void StartGame()
    {
        isMainMenuOpen = false;
        isPaused = false;
        isGameOver = false;
        ResetRunStats();
        HideAllMenus();
        ResumeGameTime();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void RestartEscapeRoom()
    {
        PrepareForSceneReload();
        ResetRunStats();
        AudioManager.Instance?.PlayStartMusic();
        ResumeGameTime();
        SceneManager.LoadScene(EscapeRoomSceneName);
    }

    public void OpenSettings()
    {
        Debug.Log("Settings button pressed, but no settings view is wired yet.", this);
    }

    public void OpenCredits()
    {
        if (creditsScreen == null)
        {
            Debug.LogWarning("Credits screen is not assigned.", this);
            return;
        }

        isCreditsOpen = true;
        isMainMenuOpen = false;

        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }

        creditsScreen.SetActive(true);
    }

    public void CloseCredits()
    {
        isCreditsOpen = false;

        if (creditsScreen != null)
        {
            creditsScreen.SetActive(false);
        }

        OpenMainMenu();
    }

    public float GetCurrentChefThrowInterval()
    {
        if (throwIntervalCurveDuration <= 0f)
        {
            return Mathf.Max(0.01f, endThrowInterval);
        }

        float normalizedTime = Mathf.Clamp01(elapsedGameTime / throwIntervalCurveDuration);
        float curveValue = Mathf.Clamp01(throwIntervalCurve.Evaluate(normalizedTime));
        float throwInterval = Mathf.Lerp(startThrowInterval, endThrowInterval, curveValue);
        return Mathf.Max(0.01f, throwInterval);
    }

    public float GetChefThrowRandomOffset()
    {
        return Mathf.Max(0f, throwIntervalRandomOffset);
    }

    public float GetConveyorSpeed()
    {
        if (conveyorSpeedCurveDuration <= 0f)
        {
            return Mathf.Max(0f, endConveyorSpeed);
        }

        float normalizedTime = Mathf.Clamp01(elapsedGameTime / conveyorSpeedCurveDuration);
        float curveValue = Mathf.Clamp01(conveyorSpeedCurve.Evaluate(normalizedTime));
        return Mathf.Max(0f, Mathf.Lerp(startConveyorSpeed, endConveyorSpeed, curveValue));
    }

    private void ApplyConveyorMaterialSettings()
    {
        float speed = GetConveyorSpeed() * conveyorMaterialSpeedFactor;
        SetConveyorMaterialDirection(conveyorScrollingMaterial, -speed);
        SetConveyorMaterialDirection(conveyorScrollingMaterialInverted, speed);
    }

    private void SetConveyorMaterialDirection(Material material, float directionX)
    {
        if (material == null)
        {
            return;
        }

        Vector4 direction = material.GetVector(DirectionPropertyId);
        direction.x = directionX;
        material.SetVector(DirectionPropertyId, direction);
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isPaused = false;
        SetPauseMenuVisible(true);
        SetGameWinMenuVisible(false);
        UpdatePauseMenuState(showPause: false, showGameOver: true);
        if (gameOverMusic != null)
        {
            AudioManager.Instance?.PlayMusic(gameOverMusic);
        }
        PauseGameTime();
    }

    private void PrepareForSceneReload()
    {
        isPaused = false;
        isGameOver = false;
        isMainMenuOpen = false;
        isCreditsOpen = false;
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);
        SetGameWinMenuVisible(false);

        HideMainAndCredits();
    }

    private void OpenMainMenu()
    {
        isMainMenuOpen = true;
        isPaused = false;
        isCreditsOpen = false;
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);
        SetGameWinMenuVisible(false);

        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }

        if (creditsScreen != null)
        {
            creditsScreen.SetActive(false);
        }

        PauseGameTime();
    }

    private void HideAllMenus()
    {
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);
        SetGameWinMenuVisible(false);
        isCreditsOpen = false;

        HideMainAndCredits();
    }

    private void SetPauseMenuVisible(bool isVisible)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isVisible);
        }
    }

    private void SetGameWinMenuVisible(bool isVisible)
    {
        isGameWinVisible = isVisible;

        if (gameWinMenu != null)
        {
            gameWinMenu.SetActive(isVisible);
        }
    }

    private void UpdatePauseMenuState(bool showPause, bool showGameOver)
    {
        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(showPause);
        }

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(showGameOver);
        }

        if (pausedImage != null)
        {
            pausedImage.SetActive(showPause);
        }

        if (gameOverImage != null)
        {
            gameOverImage.SetActive(showGameOver);
        }

        if (scoreImage != null)
        {
            scoreImage.SetActive(showGameOver);
        }

        if (timeImage != null)
        {
            timeImage.SetActive(showGameOver);
        }

        if (scoreValueText != null)
        {
            scoreValueText.gameObject.SetActive(showGameOver);
        }

        if (timeValueText != null)
        {
            timeValueText.gameObject.SetActive(showGameOver);
        }
    }

    private void HideGameOverStats()
    {
        UpdatePauseMenuState(showPause: false, showGameOver: false);
    }

    private void HideMainAndCredits()
    {
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }

        if (creditsScreen != null)
        {
            creditsScreen.SetActive(false);
        }
    }

    private void PauseGameTime()
    {
        Time.timeScale = 0f;
        AudioManager.Instance?.PauseMusic();
    }

    private void ResumeGameTime()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.ResumeMusic();
    }

    private bool IsGameplayRunning()
    {
        return !isPaused && !isGameOver && !isMainMenuOpen;
    }

    private void ResetRunStats()
    {
        elapsedGameTime = 0f;
        collectedSausageCount = 0;
        hitCount = 0;
        escapeCount = 0;
        rescuedSausageCount = 0;
        currentScore = 0;
        UpdateStatsUI();
    }

    private void RecalculateScore()
    {
        currentScore = Mathf.Max(
            0,
            CollectedScore +
            TimeScore +
            EscapeScoreTotal +
            RescuedScoreTotal -
            HitPenaltyScore);
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        if (scoreValueText != null)
        {
            scoreValueText.text = currentScore.ToString();
        }

        if (timeValueText != null)
        {
            timeValueText.text = FormatTime(elapsedGameTime);
        }
    }

    private void UpdateGameWinMenu()
    {
        UpdateGameWinMenu(1f, true);
    }

    private void SetScoreItemValue(GameObject scoreItem, int value)
    {
        if (scoreItem == null)
        {
            return;
        }

        TMP_Text[] texts = scoreItem.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "txt_value")
            {
                texts[i].text = value.ToString();
                return;
            }
        }
    }

    private void StartGameWinMenuAnimation()
    {
        gameWinRevealTimer = 0f;
        gameWinFinalRevealTimer = 0f;
        isGameWinCountingFinalScore = false;
        isGameWinAnimationComplete = false;
        UpdateGameWinMenu(0f, false);
    }

    private void UpdateGameWinMenuAnimation()
    {
        if (isGameWinAnimationComplete)
        {
            return;
        }

        if (ShouldSkipGameWinAnimation())
        {
            RevealFullGameWinMenu();
            return;
        }

        if (!isGameWinCountingFinalScore)
        {
            gameWinRevealTimer += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, scoreRevealDuration);
            float progress = Mathf.Clamp01(gameWinRevealTimer / duration);
            UpdateGameWinMenu(progress, false);

            if (progress >= 1f)
            {
                isGameWinCountingFinalScore = true;
                gameWinFinalRevealTimer = 0f;
            }

            return;
        }

        gameWinFinalRevealTimer += Time.unscaledDeltaTime;
        float finalDuration = Mathf.Max(0.01f, finalScoreRevealDuration);
        float finalProgress = Mathf.Clamp01(gameWinFinalRevealTimer / finalDuration);
        UpdateGameWinMenu(1f, true, finalProgress);

        if (finalProgress >= 1f)
        {
            isGameWinAnimationComplete = true;
        }
    }

    private bool ShouldSkipGameWinAnimation()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return false;
        }

        return mouse.leftButton.wasPressedThisFrame ||
               mouse.rightButton.wasPressedThisFrame ||
               mouse.middleButton.wasPressedThisFrame;
    }

    private void RevealFullGameWinMenu()
    {
        gameWinRevealTimer = Mathf.Max(0.01f, scoreRevealDuration);
        gameWinFinalRevealTimer = Mathf.Max(0.01f, finalScoreRevealDuration);
        isGameWinCountingFinalScore = true;
        isGameWinAnimationComplete = true;
        UpdateGameWinMenu(1f, true, 1f);
    }

    private void UpdateGameWinMenu(float scoreProgress, bool showFinalScore, float finalScoreProgress = 0f)
    {
        float clampedScoreProgress = Mathf.Clamp01(scoreProgress);
        float clampedFinalScoreProgress = Mathf.Clamp01(finalScoreProgress);

        SetScoreItemValue(collectedScoreItem, GetAnimatedValue(CollectedScore, clampedScoreProgress));
        SetScoreItemValue(timeScoreItem, GetAnimatedValue(TimeScore, clampedScoreProgress));
        SetScoreItemValue(hitScoreItem, -GetAnimatedValue(HitPenaltyScore, clampedScoreProgress));
        SetScoreItemValue(escapeScoreItem, GetAnimatedValue(EscapeScoreTotal, clampedScoreProgress));
        SetScoreItemValue(rescuedScoreItem, GetAnimatedValue(RescuedScoreTotal, clampedScoreProgress));
        SetScoreItemValue(finalScoreItem, showFinalScore ? GetAnimatedValue(currentScore, clampedFinalScoreProgress) : 0);
    }

    private static int GetAnimatedValue(int targetValue, float progress)
    {
        return Mathf.RoundToInt(targetValue * Mathf.Clamp01(progress));
    }

    private static string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeInSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}m {seconds:00}s";
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystemExists();
        DisableDuplicatePersistentSceneRoots(scene);
        ResolveSceneReferences();

        if (scene.name == SurvivalSceneName)
        {
            isPaused = false;
            isGameOver = false;
            isMainMenuOpen = false;
            isCreditsOpen = false;
            HideAllMenus();
            RecalculateScore();
            ResumeGameTime();
            return;
        }

        if (!skipMainMenuOnNextLoad)
        {
            return;
        }

        skipMainMenuOnNextLoad = false;
        StartGame();
    }

    private void RegisterButtons()
    {
        BindButton(resumeButton, ResumeGame);
        BindButton(replayButton, ReplayGame);
        BindButton(pauseMainMenuButton, ReturnToMainMenu);
        BindButton(pauseExitButton, QuitGame);
        BindButton(gameStartButton, StartGame);
        BindButton(mainMenuSettingsButton, OpenSettings);
        BindButton(mainMenuCreditsButton, OpenCredits);
        BindButton(mainMenuExitButton, QuitGame);
        BindButton(creditsMainMenuButton, ReturnToMainMenu);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ResolveSceneReferences()
    {
        playerChain = FindFirstObjectByType<SausageChainController>();
    }

    private void CachePersistentSceneRoots()
    {
        persistentSceneRoots.Clear();
        AddPersistentCanvasRoot(pauseMenu);
        AddPersistentCanvasRoot(mainMenu);
        AddPersistentCanvasRoot(creditsScreen);
        AddPersistentCanvasRoot(gameWinMenu);

        if (EventSystem.current != null)
        {
            AddPersistentSceneRoot(EventSystem.current.gameObject);
        }
    }

    private void AddPersistentCanvasRoot(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>(true);

        if (canvas == null)
        {
            AddPersistentSceneRoot(target);
            return;
        }

        AddPersistentSceneRoot(canvas.gameObject);
    }

    private void AddPersistentSceneRoot(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        GameObject root = target.transform.root.gameObject;

        if (persistentSceneRoots.Contains(root))
        {
            return;
        }

        persistentSceneRoots.Add(root);
    }

    private void MakePersistentSceneRootsPersistent()
    {
        for (int i = 0; i < persistentSceneRoots.Count; i++)
        {
            GameObject root = persistentSceneRoots[i];

            if (root != null)
            {
                DontDestroyOnLoad(root);
            }
        }
    }

    private void DisableDuplicatePersistentSceneRoots(Scene scene)
    {
        GameObject[] sceneRoots = scene.GetRootGameObjects();

        for (int i = 0; i < sceneRoots.Length; i++)
        {
            GameObject sceneRoot = sceneRoots[i];

            if (sceneRoot == null || !IsDuplicatePersistentSceneRoot(sceneRoot))
            {
                continue;
            }

            sceneRoot.SetActive(false);
        }
    }

    private bool IsDuplicatePersistentSceneRoot(GameObject sceneRoot)
    {
        for (int i = 0; i < persistentSceneRoots.Count; i++)
        {
            GameObject persistentRoot = persistentSceneRoots[i];

            if (persistentRoot == null || persistentRoot == sceneRoot)
            {
                continue;
            }

            if (persistentRoot.name == sceneRoot.name)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        startThrowInterval = Mathf.Max(0.01f, startThrowInterval);
        endThrowInterval = Mathf.Max(0.01f, endThrowInterval);
        throwIntervalCurveDuration = Mathf.Max(0f, throwIntervalCurveDuration);
        throwIntervalRandomOffset = Mathf.Max(0f, throwIntervalRandomOffset);
        startConveyorSpeed = Mathf.Max(0f, startConveyorSpeed);
        endConveyorSpeed = Mathf.Max(0f, endConveyorSpeed);
        conveyorSpeedCurveDuration = Mathf.Max(0f, conveyorSpeedCurveDuration);
        conveyorMaterialSpeedFactor = Mathf.Max(0f, conveyorMaterialSpeedFactor);
        ApplyConveyorMaterialSettings();

        if (throwIntervalCurve == null || throwIntervalCurve.length == 0)
        {
            throwIntervalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        if (conveyorSpeedCurve == null || conveyorSpeedCurve.length == 0)
        {
            conveyorSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
