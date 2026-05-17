using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameHandler : MonoBehaviour
{
    private const string CarryOverScoreKey = "CarryOverScore";
    private const int EscapeRoomSceneBuildIndex = 1;

    public static GameHandler Instance { get; private set; }

    private static bool skipMainMenuOnNextLoad;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject creditsScreen;
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

    [Header("Fallback Game Over UI")]
    [SerializeField] private Sprite fallbackGameOverSprite;
    [SerializeField] private Sprite fallbackReplaySprite;
    [SerializeField] private Sprite fallbackMainMenuSprite;
    [SerializeField] private Sprite fallbackExitSprite;
    [SerializeField] private Sprite fallbackScoreSprite;
    [SerializeField] private Sprite fallbackTimeSprite;

    [Header("Gameplay")]
    [SerializeField] private SausageChainController playerChain;
    [SerializeField] private bool showMainMenuOnStart = true;

    [Header("Scoring")]
    [SerializeField] private int collectScore = 100;
    [SerializeField] private int hitPenalty = 50;
    [SerializeField] private int timeScorePerSecond = 10;

    private bool isPaused;
    private bool isGameOver;
    private bool isMainMenuOpen;
    private bool isCreditsOpen;
    private float elapsedGameTime;
    private int collectedSausageCount;
    private int hitCount;
    private int baseScore;
    private int currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureEventSystemExists();
        Instance = this;
        Time.timeScale = 1f;
        RegisterButtons();
    }

    private void Start()
    {
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

        hitCount++;
        RecalculateScore();

        if (playerChain.ReleaseLastSegment())
        {
            return;
        }

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

    public void SaveScoreForNextScene()
    {
        PlayerPrefs.SetInt(CarryOverScoreKey, currentScore);
        PlayerPrefs.Save();
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
        ResumeGameTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReplayGame()
    {
        skipMainMenuOnNextLoad = true;
        PlayerPrefs.DeleteKey(CarryOverScoreKey);
        ResumeGameTime();

        int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;

        if (activeBuildIndex == EscapeRoomSceneBuildIndex)
        {
            SceneManager.LoadScene(activeBuildIndex);
            return;
        }

        SceneManager.LoadScene(EscapeRoomSceneBuildIndex);
    }

    public void ReturnToMainMenu()
    {
        if (mainMenu == null)
        {
            ResumeGameTime();
            SceneManager.LoadScene(0);
            return;
        }

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

    private void TriggerGameOver()
    {
        isGameOver = true;
        isPaused = false;
        EnsureFallbackGameOverUi();
        SetPauseMenuVisible(true);
        UpdatePauseMenuState(showPause: false, showGameOver: true);
        PauseGameTime();
    }

    private void OpenMainMenu()
    {
        isMainMenuOpen = true;
        isPaused = false;
        isCreditsOpen = false;
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);

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
        isCreditsOpen = false;

        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }

        if (creditsScreen != null)
        {
            creditsScreen.SetActive(false);
        }
    }

    private void SetPauseMenuVisible(bool isVisible)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isVisible);
        }
    }

    private void EnsureFallbackGameOverUi()
    {
        if (pauseMenu != null && gameOverImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Game Over Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        pauseMenu = new GameObject("Game Over Menu");
        pauseMenu.transform.SetParent(canvasObject.transform, false);

        RectTransform menuRect = pauseMenu.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(1920f, 1080f);

        Image background = pauseMenu.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);

        if (fallbackGameOverSprite == null)
        {
            gameOverImage = CreateTextObject("Game Over Image", pauseMenu.transform, "GAME OVER", 54, Vector2.zero, new Vector2(650f, 160f));
            return;
        }

        gameOverImage = CreateImageObject("Game Over Image", pauseMenu.transform, fallbackGameOverSprite, new Vector2(-0.50002444f, 281f), new Vector2(1000f, 633f));
        replayButton = CreateImageButton("Replay Button", pauseMenu.transform, fallbackReplaySprite, Vector2.zero, new Vector2(552f, 123f), ReplayGame);
        pauseMainMenuButton = CreateImageButton("Main Menu Button", pauseMenu.transform, fallbackMainMenuSprite, new Vector2(0f, -175f), new Vector2(633f, 109f), ReturnToMainMenu);
        pauseExitButton = CreateImageButton("Exit Button", pauseMenu.transform, fallbackExitSprite, new Vector2(0f, -350f), new Vector2(588f, 114f), QuitGame);
        scoreImage = CreateImageObject("Score Image", pauseMenu.transform, fallbackScoreSprite, new Vector2(540f, 79f), new Vector2(600f, 157f));
        timeImage = CreateImageObject("Time Image", pauseMenu.transform, fallbackTimeSprite, new Vector2(626f, -244f), new Vector2(469.5f, 114.3411f));
        scoreValueText = CreateTmpText("Score Value", pauseMenu.transform, new Vector2(594f, -14f), new Vector2(498.13f, 108.5f), 70f);
        timeValueText = CreateTmpText("Time Value", pauseMenu.transform, new Vector2(700f, -324f), new Vector2(498.13f, 108.5f), 70f);
        UpdateStatsUI();
    }

    private GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.preserveAspect = false;

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        return imageObject;
    }

    private Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImageObject(name, parent, sprite, position, size);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        return button;
    }

    private GameObject CreateTextObject(string name, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        return textObject;
    }

    private TMP_Text CreateTmpText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.cyan;
        text.fontStyle = FontStyles.Bold;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        return text;
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

    private void PauseGameTime()
    {
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMusic();
        }
    }

    private void ResumeGameTime()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
        }
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
        baseScore = PlayerPrefs.GetInt(CarryOverScoreKey, 0);
        PlayerPrefs.DeleteKey(CarryOverScoreKey);
        currentScore = baseScore;
        UpdateStatsUI();
    }

    private void RecalculateScore()
    {
        int timeScore = Mathf.FloorToInt(elapsedGameTime) * timeScorePerSecond;
        currentScore = Mathf.Max(0, baseScore + collectedSausageCount * collectScore + timeScore - hitCount * hitPenalty);
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
}
