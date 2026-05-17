using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance { get; private set; }

    private static bool skipMainMenuOnNextLoad;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject mainMenu;
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
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private TMP_Text timeValueText;

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
    private float elapsedGameTime;
    private int collectedSausageCount;
    private int hitCount;
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

    public void OpenSettings()
    {
        Debug.Log("Settings button pressed, but no settings view is wired yet.", this);
    }

    public void OpenCredits()
    {
        Debug.Log("Credits button pressed, but no credits view is wired yet.", this);
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isPaused = false;
        SetPauseMenuVisible(true);
        UpdatePauseMenuState(showPause: false, showGameOver: true);
        PauseGameTime();
    }

    private void OpenMainMenu()
    {
        isMainMenuOpen = true;
        isPaused = false;
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);

        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }

        PauseGameTime();
    }

    private void HideAllMenus()
    {
        SetPauseMenuVisible(false);
        UpdatePauseMenuState(showPause: true, showGameOver: false);

        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }
    }

    private void SetPauseMenuVisible(bool isVisible)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isVisible);
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
        currentScore = 0;
        UpdateStatsUI();
    }

    private void RecalculateScore()
    {
        int timeScore = Mathf.FloorToInt(elapsedGameTime) * timeScorePerSecond;
        currentScore = Mathf.Max(0, collectedSausageCount * collectScore + timeScore - hitCount * hitPenalty);
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
