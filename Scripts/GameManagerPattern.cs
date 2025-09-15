// Unity Design Pattern Example: GameManagerPattern
// This script demonstrates the GameManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The **GameManagerPattern** in Unity is a robust and widely-used design pattern that leverages the Singleton pattern to create a central, persistent, and accessible hub for managing global game state, services, and core functionalities. It ensures that there's one single point of control for operations that affect the entire game, like managing scores, current level, game state (playing, paused, game over), audio settings, saving/loading, and more.

### Key Characteristics of the GameManagerPattern:

1.  **Singleton:** There is only one instance of the GameManager throughout the application's lifecycle.
2.  **Persistence:** The GameManager typically persists across different scenes in the game, meaning it's not destroyed when a new scene loads. This allows it to maintain consistent state data.
3.  **Centralized Control:** It acts as a central authority for various game systems, making it easy for other scripts to access and modify global game data or trigger global events.
4.  **Loose Coupling (via Events):** While it's a central hub, it can promote loose coupling by using C# events. Other scripts can subscribe to these events to react to changes in the game state without needing direct references to the GameManager or knowing its internal workings.

---

Here's a complete, practical C# Unity script demonstrating the `GameManagerPattern`.

To use this script:
1.  Create an **empty GameObject** in your *first* scene (e.g., your Splash Screen or Main Menu scene). Name it something like `_GameManager` or `GameManagerObject`.
2.  Create a new C# script named `GameManager` and copy the code below into it.
3.  Attach the `GameManager.cs` script to the `_GameManager` GameObject you created.
4.  Run your game. Observe the debug logs and how the game state changes.

```csharp
using UnityEngine;
using System; // Required for Action delegate (used in events)
using System.Collections; // Required for IEnumerator (used for coroutines)

// GameManager.cs
// This script demonstrates the GameManagerPattern design pattern in Unity.

/// <summary>
/// Defines the possible states of our game.
/// Using an enum provides clear, distinct states and avoids magic strings/numbers.
/// </summary>
public enum GameState
{
    Loading,    // Game is loading assets, scene, or initial setup
    Playing,    // Game is actively being played
    Paused,     // Game is paused (e.g., pause menu is open)
    GameOver    // Game has ended
}

/// <summary>
/// The GameManager class implements the Singleton pattern and serves as a central hub
/// for managing global game state, services, and common functionalities.
/// It persists across scenes and provides a single point of access for various game systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ------------------------------------------------------------------------------------------------
    // 1. Singleton Implementation
    // ------------------------------------------------------------------------------------------------

    // Static reference to the singleton instance.
    // 'private set' ensures that only the GameManager itself can set the instance,
    // protecting it from accidental external modification.
    public static GameManager Instance { get; private set; }

    // ------------------------------------------------------------------------------------------------
    // 2. Global Game State Variables
    //    These properties hold the current state of important game elements.
    //    '[SerializeField]' allows private fields to be viewed/edited in the Unity Inspector.
    //    'private set' on properties ensures they can only be modified via GameManager methods,
    //    enforcing proper control over state changes.
    // ------------------------------------------------------------------------------------------------

    // Player Score
    [SerializeField] private int _playerScore = 0; // Backing field for the PlayerScore property
    public int PlayerScore
    {
        get { return _playerScore; }
        private set
        {
            // Only update if the score has actually changed to avoid unnecessary event calls
            if (_playerScore != value)
            {
                _playerScore = value;
                // Raise an event whenever the score changes, notifying subscribers (e.g., UI).
                OnScoreChanged?.Invoke(_playerScore);
                Debug.Log($"[GameManager] Score updated: {_playerScore}");
            }
        }
    }

    // Current Game State
    [SerializeField] private GameState _currentGameState = GameState.Loading; // Initial state
    public GameState CurrentGameState
    {
        get { return _currentGameState; }
        private set
        {
            // Only update if the state has actually changed
            if (_currentGameState != value)
            {
                _currentGameState = value;
                // Raise an event whenever the game state changes.
                OnGameStateChanged?.Invoke(_currentGameState);
                Debug.Log($"[GameManager] Game State changed to: {_currentGameState}");
            }
        }
    }

    // Current Level
    [SerializeField] private int _currentLevel = 1;
    public int CurrentLevel
    {
        get { return _currentLevel; }
        private set
        {
            if (_currentLevel != value)
            {
                _currentLevel = value;
                // Raise an event if a level transition occurs.
                OnLevelChanged?.Invoke(_currentLevel);
                Debug.Log($"[GameManager] Current Level set to: {_currentLevel}");
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    // 3. Event System for Loose Coupling
    //    Events allow other scripts to react to changes in the GameManager's state
    //    without directly referencing the GameManager or being tightly coupled to its implementation.
    //    This is crucial for building modular and scalable game systems.
    // ------------------------------------------------------------------------------------------------

    // Event fired when the player's score changes. Subscribers receive the new score value.
    public event Action<int> OnScoreChanged;
    // Event fired when the game state changes. Subscribers receive the new GameState enum.
    public event Action<GameState> OnGameStateChanged;
    // Event fired when the current level changes. Subscribers receive the new level number.
    public event Action<int> OnLevelChanged;

    // ------------------------------------------------------------------------------------------------
    // 4. Initialization and Persistence
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is the ideal place to enforce the Singleton pattern and ensure persistence.
    /// </summary>
    private void Awake()
    {
        // Check if an instance already exists.
        if (Instance != null && Instance != this)
        {
            // If another GameManager already exists, destroy this one.
            // This prevents duplicate GameManagers and ensures there's only one active.
            Debug.LogWarning("[GameManager] Found another GameManager instance. Destroying this duplicate.");
            Destroy(gameObject);
        }
        else
        {
            // If no instance exists, make this the singleton instance.
            Instance = this;

            // Make sure this GameObject (and thus the GameManager script) persists across scene loads.
            // This is essential for a GameManager that needs to maintain state throughout the entire game.
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameManager] Initialized and set to persist across scenes.");
            InitializeGame(); // Perform initial game setup tasks
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    private void Start()
    {
        // Example: Transition from Loading to Playing after a short delay, simulating initial game setup.
        StartCoroutine(SimulateLoadingPhase());
    }

    /// <summary>
    /// Performs initial setup for the game, such as resetting scores or setting the starting state.
    /// </summary>
    private void InitializeGame()
    {
        PlayerScore = 0; // Reset score at the start of a new game session
        CurrentGameState = GameState.Loading; // Set initial game state
        CurrentLevel = 1; // Start at level 1
        Debug.Log("[GameManager] Initial game parameters set.");
    }

    /// <summary>
    /// Simulates a loading phase before the game enters the Playing state.
    /// </summary>
    private IEnumerator SimulateLoadingPhase()
    {
        Debug.Log("[GameManager] Simulating loading phase...");
        yield return new WaitForSeconds(3f); // Simulate some loading time
        SetGameState(GameState.Playing); // Transition to playing state after loading
        Debug.Log("[GameManager] Loading complete. Game is now playing.");
    }

    // ------------------------------------------------------------------------------------------------
    // 5. Public Methods for Game Management
    //    These methods provide the primary interface for other scripts to interact
    //    with the GameManager, modifying its state and triggering actions.
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Adds score to the player's current score.
    /// Only allows score modification if the game is in the 'Playing' state.
    /// </summary>
    /// <param name="amount">The amount of score to add (can be negative to subtract).</param>
    public void AddScore(int amount)
    {
        if (CurrentGameState == GameState.Playing)
        {
            PlayerScore += amount;
        }
        else
        {
            Debug.LogWarning($"[GameManager] Cannot add score (amount: {amount}) when not in Playing state. Current state: {CurrentGameState}");
        }
    }

    /// <summary>
    /// Sets the current game state and performs actions relevant to that state change.
    /// </summary>
    /// <param name="newState">The new GameState to set.</param>
    public void SetGameState(GameState newState)
    {
        CurrentGameState = newState; // The property setter will trigger the OnGameStateChanged event.

        // Perform specific actions based on the new game state
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f; // Resume normal game time
                Debug.Log("[GameManager] Game resumed.");
                break;
            case GameState.Paused:
                Time.timeScale = 0f; // Pause game time
                Debug.Log("[GameManager] Game paused.");
                break;
            case GameState.GameOver:
                Time.timeScale = 0f; // Stop game time completely
                Debug.Log("[GameManager] Game Over!");
                // Here you might load a game over scene, display a game over UI, etc.
                break;
            case GameState.Loading:
                // Time.timeScale might remain normal during loading screens
                Debug.Log("[GameManager] Entering loading state.");
                break;
        }
    }

    /// <summary>
    /// Toggles the game's paused state between Playing and Paused.
    /// </summary>
    public void TogglePause()
    {
        if (CurrentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (CurrentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
        else
        {
            Debug.LogWarning($"[GameManager] Cannot toggle pause from current state: {CurrentGameState}");
        }
    }

    /// <summary>
    /// Advances the game to the next level.
    /// In a real game, this would typically involve loading a new scene.
    /// </summary>
    public void LoadNextLevel()
    {
        if (CurrentGameState == GameState.Playing || CurrentGameState == GameState.Loading)
        {
            Debug.Log($"[GameManager] Attempting to load next level. Current Level: {CurrentLevel}");
            SetGameState(GameState.Loading); // Indicate that a level load is in progress
            CurrentLevel++; // Increment level
            
            // In a real game, you would use SceneManager.LoadScene() here:
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Level" + CurrentLevel);

            // For this example, we simulate the loading process with a delay.
            StartCoroutine(SimulateLevelLoadAndResumePlaying(3f));
        }
        else
        {
            Debug.LogWarning($"[GameManager] Cannot load next level from current state: {CurrentGameState}");
        }
    }

    /// <summary>
    /// Simulates loading a new level over a specified duration, then transitions back to Playing.
    /// </summary>
    private IEnumerator SimulateLevelLoadAndResumePlaying(float delay)
    {
        Debug.Log($"[GameManager] Simulating loading for Level {CurrentLevel}...");
        yield return new WaitForSeconds(delay); // Wait for the simulated loading time
        Debug.Log($"[GameManager] Level {CurrentLevel} loaded (simulated).");
        SetGameState(GameState.Playing); // Resume playing after the level is "loaded"
    }

    /// <summary>
    /// Ends the current game session by setting the state to GameOver.
    /// </summary>
    public void EndGame()
    {
        SetGameState(GameState.GameOver);
        // Additional game over logic, like displaying a score screen or resetting player data, can go here.
    }

    // ------------------------------------------------------------------------------------------------
    // 6. Example Usage in Other Scripts (demonstrated in comments)
    //    These examples show how other scripts would typically interact with the GameManager.
    // ------------------------------------------------------------------------------------------------

    /*
    // ==============================================================================================
    // EXAMPLE SCRIPT: PlayerController.cs
    // How a PlayerController might interact with the GameManager.
    // Attach this to your Player GameObject.
    // ==============================================================================================
    public class PlayerController : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Coin"))
            {
                // Access the GameManager via its static Instance property to add score.
                GameManager.Instance.AddScore(10);
                Destroy(other.gameObject); // Destroy the coin GameObject
            }

            if (other.CompareTag("Enemy"))
            {
                // Player hit an enemy, trigger game over state.
                GameManager.Instance.EndGame();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Use the GameManager to toggle the pause state.
                GameManager.Instance.TogglePause();
            }
        }
    }

    // ==============================================================================================
    // EXAMPLE SCRIPT: UIManager.cs
    // How a UIManager might subscribe to GameManager events to update the User Interface.
    // Attach this to a UI Canvas GameObject. Requires a UnityEngine.UI.Text component for scoreText.
    // ==============================================================================================
    using UnityEngine;
    using UnityEngine.UI; // Required for Text component

    public class UIManager : MonoBehaviour
    {
        public Text scoreText;           // Assign a UI Text element in the Inspector
        public GameObject pausePanel;    // Assign a UI Panel for the pause menu
        public GameObject gameOverPanel; // Assign a UI Panel for the game over screen

        void OnEnable()
        {
            // Subscribe to GameManager events when this UI Manager is enabled.
            // This is crucial for reacting to changes without constantly checking the GameManager.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += UpdateScoreUI;
                GameManager.Instance.OnGameStateChanged += UpdateGameStateUI;
            }
        }

        void OnDisable()
        {
            // IMPORTANT: Unsubscribe from events when the UI Manager is disabled
            // to prevent memory leaks and 'missing reference' errors if GameManager is destroyed first.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
                GameManager.Instance.OnGameStateChanged -= UpdateGameStateUI;
            }
        }

        void Start()
        {
            // Ensure UI reflects the current state immediately on start.
            if (GameManager.Instance != null)
            {
                UpdateScoreUI(GameManager.Instance.PlayerScore);
                UpdateGameStateUI(GameManager.Instance.CurrentGameState);
            }
        }

        void UpdateScoreUI(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + newScore.ToString();
                Debug.Log("[UIManager] Score UI updated.");
            }
        }

        void UpdateGameStateUI(GameState newState)
        {
            // Control visibility of UI panels based on the game state.
            if (pausePanel != null) pausePanel.SetActive(newState == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(newState == GameState.GameOver);

            // You might also show/hide loading screens, disable player input based on state, etc.
            Debug.Log("[UIManager] Game state UI updated for: " + newState);
        }
    }

    // ==============================================================================================
    // EXAMPLE SCRIPT: LevelExitTrigger.cs
    // How a trigger in a level might signal the GameManager to load the next level.
    // Attach this to an invisible trigger GameObject at the end of a level.
    // ==============================================================================================
    public class LevelExitTrigger : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("[LevelExitTrigger] Player reached end of level! Requesting next level load.");
                GameManager.Instance.LoadNextLevel();
            }
        }
    }
    */
}
```