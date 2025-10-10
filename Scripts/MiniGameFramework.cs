// Unity Design Pattern Example: MiniGameFramework
// This script demonstrates the MiniGameFramework pattern in Unity
// Generated automatically - ready to use in your Unity project

The MiniGameFramework design pattern helps organize and manage multiple distinct mini-games within a larger Unity application. It promotes a modular and extensible architecture, making it easy to add, remove, or modify mini-games without affecting the core game logic or other mini-games.

**Core Principles:**

1.  **Abstraction:** Define a common interface or abstract base class (`MiniGameBase`) that all mini-games must adhere to. This ensures a consistent API for the manager.
2.  **Encapsulation:** Each mini-game (`GuessTheNumberMiniGame`, `ReactionTestMiniGame`) encapsulates its own logic, state, and resources.
3.  **Centralized Control:** A `MiniGameManager` orchestrates the loading, starting, ending, and transitioning between different mini-games.
4.  **Decoupling:** Mini-games are largely independent of each other and the main application, communicating through events or the manager.

---

### **How to Use This Example in Unity:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create Folders:**
    *   `Scripts`
    *   `Prefabs`
    *   `UI`
3.  **Create C# Scripts:**
    *   In `Scripts` folder, create `MiniGameBase.cs`, `MiniGameManager.cs`, `GuessTheNumberMiniGame.cs`, `ReactionTestMiniGame.cs`, `MiniGameUIManager.cs`.
    *   Copy the code provided below into their respective files.
4.  **Setup Mini-Game Prefabs:**
    *   For `GuessTheNumberMiniGame`:
        *   Create an empty GameObject in the scene, rename it `GuessTheNumberPrefab`.
        *   Add the `GuessTheNumberMiniGame.cs` component to it.
        *   Drag this GameObject from the Hierarchy into the `Prefabs` folder to create a prefab.
        *   **Delete the GameObject from the Hierarchy** (we only need the prefab).
    *   For `ReactionTestMiniGame`:
        *   Repeat the steps above, naming it `ReactionTestPrefab` and adding `ReactionTestMiniGame.cs`.
        *   **Delete the GameObject from the Hierarchy**.
5.  **Setup MiniGameManager:**
    *   Create an empty GameObject in the scene, rename it `MiniGameManager`.
    *   Add the `MiniGameManager.cs` component to it.
    *   In the Inspector of `MiniGameManager`, locate the `Available Mini Game Prefabs` list.
    *   Drag and drop `GuessTheNumberPrefab` and `ReactionTestPrefab` from your `Prefabs` folder into this list.
6.  **Setup UI:**
    *   Create a UI Canvas (`GameObject -> UI -> Canvas`). Name it `MiniGameCanvas`.
    *   Set its Render Mode to `Screen Space - Overlay`.
    *   Create an empty GameObject as a child of `MiniGameCanvas`, name it `UI_Manager`.
    *   Add the `MiniGameUIManager.cs` component to `UI_Manager`.
    *   Inside `UI_Manager`, create the UI elements as children:
        *   **MiniGameSelectionPanel** (Panel - GameObject, disable it initially)
            *   Button: "Start Guess The Number" (Call `MiniGameUIManager.SelectGuessTheNumber`)
            *   Button: "Start Reaction Test" (Call `MiniGameUIManager.SelectReactionTest`)
        *   **GamePanel** (Panel - GameObject, disable it initially)
            *   TextMeshPro - Game Title
            *   TextMeshPro - Game Instructions
            *   TextMeshPro - Game Message
            *   TextMeshPro - Game Result
            *   Button: "Back to Selection" (Call `MiniGameUIManager.ShowMiniGameSelection`)
        *   **GuessInputPanel** (Panel - GameObject, disable it initially)
            *   Input Field (TMP_InputField)
            *   Button: "Guess" (Call `MiniGameUIManager.OnGuessButtonClicked`)
        *   **ReactionTestPanel** (Panel - GameObject, disable it initially)
            *   Button: "React!" (Call `MiniGameUIManager.OnReactionButtonClicked`)
    *   **Drag and drop** all these UI elements from the Hierarchy into their respective `[SerializeField]` fields in the `MiniGameUIManager` component.
7.  **Import TextMeshPro Essentials:** If you haven't already, go to `Window -> TextMeshPro -> Import TMP Essential Resources`.
8.  **Run the Scene!** You should see the "MiniGame Selection" panel.

---

### **1. MiniGameBase.cs**

This abstract class defines the common interface for all mini-games.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // Although not used directly here, often useful for mini-game logic

namespace MiniGameFramework // Optional: Use a namespace for better organization
{
    /// <summary>
    /// Represents the possible outcomes of a mini-game.
    /// </summary>
    public enum MiniGameResult
    {
        None,   // Game is not yet completed or has no specific outcome
        Win,    // Player won the mini-game
        Loss,   // Player lost the mini-game
        Draw    // Mini-game ended in a draw (less common for simple mini-games)
    }

    /// <summary>
    /// Abstract base class for all mini-games in the framework.
    /// All concrete mini-games must inherit from this and implement its abstract methods.
    /// It provides a common interface for the MiniGameManager to interact with any mini-game.
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        [Header("MiniGame Base Properties")]
        [Tooltip("A unique identifier for this mini-game type.")]
        [SerializeField]
        protected string gameID = "DefaultGameID"; // Default ID, should be overridden by concrete games

        /// <summary>
        /// Gets the unique identifier for this mini-game.
        /// </summary>
        public string GameID => gameID;

        /// <summary>
        /// Indicates if the mini-game is currently running.
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Indicates if the mini-game has completed its lifecycle.
        /// </summary>
        public bool HasCompleted { get; protected set; }

        /// <summary>
        /// The outcome of the mini-game after it has completed.
        /// </summary>
        public MiniGameResult GameResult { get; protected set; } = MiniGameResult.None;

        /// <summary>
        /// Event fired when the mini-game has finished its initialization.
        /// Parameter: GameID
        /// </summary>
        public event Action<string> OnGameInitialized;

        /// <summary>
        /// Event fired when the mini-game officially starts its gameplay phase.
        /// Parameter: GameID
        /// </summary>
        public event Action<string> OnGameStarted;

        /// <summary>
        /// Event fired when the mini-game officially ends.
        /// Parameter: GameID, MiniGameResult, Score
        /// </summary>
        public event Action<string, MiniGameResult, float> OnGameEnded;

        /// <summary>
        /// Event fired to update the UI with a game-specific message.
        /// Parameter: GameID, Message
        /// </summary>
        public event Action<string, string> OnMessageUpdate;

        // =========================================================================
        // Abstract Methods: Must be implemented by concrete mini-game classes
        // =========================================================================

        /// <summary>
        /// Initializes the mini-game. This is called by the MiniGameManager
        /// before StartGame. It's for setting up internal state, loading resources, etc.
        /// </summary>
        /// <param name="onInitializedCallback">Optional callback to be invoked once initialization is complete.</param>
        public abstract void Initialize(Action onInitializedCallback = null);

        /// <summary>
        /// Starts the actual gameplay of the mini-game.
        /// </summary>
        public abstract void StartGame();

        /// <summary>
        /// Ends the mini-game, typically called when the game rules dictate it's over
        /// (win/loss condition met, time ran out, etc.) or when the manager requests it.
        /// </summary>
        public abstract void EndGame();

        /// <summary>
        /// Resets the mini-game to its initial state, ready to be played again.
        /// This might be called between rounds or after an EndGame() call if the game supports replays.
        /// </summary>
        public abstract void ResetGame();

        // =========================================================================
        // Virtual Methods: Can be overridden by concrete mini-game classes
        // =========================================================================

        /// <summary>
        /// Cleans up any resources held by the mini-game.
        /// By default, it destroys the GameObject this script is attached to.
        /// </summary>
        public virtual void Cleanup()
        {
            // Unsubscribe all listeners from events to prevent memory leaks
            OnGameInitialized = null;
            OnGameStarted = null;
            OnGameEnded = null;
            OnMessageUpdate = null;

            // Destroy the GameObject associated with this mini-game instance.
            // This is crucial for managing instantiated prefabs.
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        // =========================================================================
        // Protected Helper Methods: For concrete mini-games to report state changes
        // =========================================================================

        /// <summary>
        /// Reports that the mini-game has finished initializing.
        /// </summary>
        protected void ReportGameInitialized()
        {
            OnGameInitialized?.Invoke(gameID);
        }

        /// <summary>
        /// Reports that the mini-game has started. Sets IsRunning to true.
        /// </summary>
        protected void ReportGameStarted()
        {
            IsRunning = true;
            OnGameStarted?.Invoke(gameID);
        }

        /// <summary>
        /// Reports that the mini-game has ended with a specific result and optional score.
        /// Sets IsRunning to false and HasCompleted to true.
        /// </summary>
        /// <param name="result">The outcome of the mini-game (Win, Loss, Draw).</param>
        /// <param name="score">An optional score associated with the game's outcome.</param>
        protected void ReportGameEnded(MiniGameResult result, float score = 0f)
        {
            IsRunning = false;
            HasCompleted = true;
            GameResult = result;
            OnGameEnded?.Invoke(gameID, result, score);
        }

        /// <summary>
        /// Sends a message to the UI or other listening components.
        /// </summary>
        /// <param name="message">The message string to display.</param>
        protected void ReportMessage(string message)
        {
            OnMessageUpdate?.Invoke(gameID, message);
        }
    }
}
```

---

### **2. MiniGameManager.cs**

This is the central coordinator, often implemented as a Singleton, responsible for managing the lifecycle of different mini-games.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like ToDictionary

namespace MiniGameFramework
{
    /// <summary>
    /// The MiniGameManager is a singleton responsible for coordinating
    /// the lifecycle of various mini-games. It loads, starts, and ends mini-games.
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        // Singleton instance pattern
        public static MiniGameManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Drag all your MiniGameBase prefabs here from the Project window.")]
        [SerializeField]
        private List<MiniGameBase> availableMiniGamePrefabs = new List<MiniGameBase>();

        // Internal dictionary to quickly look up mini-game prefabs by their ID.
        private Dictionary<string, MiniGameBase> registeredMiniGamePrefabs;

        // Reference to the currently active mini-game instance.
        private MiniGameBase currentActiveMiniGame;
        public MiniGameBase CurrentActiveMiniGame => currentActiveMiniGame;

        // Events for other systems (like UI) to listen to manager-level game state changes.
        public event Action<string> OnMiniGameLoaded;
        public event Action<string> OnMiniGameStarted;
        public event Action<string, MiniGameResult, float> OnMiniGameEnded;
        public event Action<string, string> OnMiniGameMessage; // For general messages from the active game

        private void Awake()
        {
            // Singleton enforcement
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple MiniGameManagers found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize the dictionary of mini-game prefabs.
            // Ensure no duplicate GameIDs among the prefabs.
            try
            {
                registeredMiniGamePrefabs = availableMiniGamePrefabs.ToDictionary(
                    game => game.GameID,
                    game => game
                );
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"MiniGameManager: Found duplicate GameIDs in availableMiniGamePrefabs! Please ensure all GameIDs are unique. Error: {e.Message}");
                enabled = false; // Disable manager if there's a critical error
                return;
            }

            Debug.Log($"MiniGameManager initialized. Registered {registeredMiniGamePrefabs.Count} mini-game types.");
            foreach (var game in registeredMiniGamePrefabs)
            {
                Debug.Log($" - Registered: {game.Key}");
            }
        }

        /// <summary>
        /// Starts a specific mini-game by its unique ID.
        /// </summary>
        /// <param name="gameID">The unique identifier of the mini-game to start.</param>
        public void StartMiniGame(string gameID)
        {
            // 1. End any currently active mini-game
            if (currentActiveMiniGame != null)
            {
                Debug.Log($"Ending current mini-game: {currentActiveMiniGame.GameID} before starting {gameID}.");
                EndCurrentMiniGame();
            }

            // 2. Check if the requested mini-game ID exists.
            if (!registeredMiniGamePrefabs.TryGetValue(gameID, out MiniGameBase gamePrefab))
            {
                Debug.LogError($"MiniGameManager: Mini-game with ID '{gameID}' not found in registered prefabs.");
                return;
            }

            // 3. Instantiate the mini-game prefab.
            // The instantiated GameObject will be managed by this manager.
            currentActiveMiniGame = Instantiate(gamePrefab);
            currentActiveMiniGame.transform.SetParent(this.transform); // Keep instantiated games organized under manager
            currentActiveMiniGame.gameObject.name = $"{gameID}_Instance";
            Debug.Log($"MiniGameManager: Instantiated mini-game prefab '{gameID}'.");
            OnMiniGameLoaded?.Invoke(gameID);

            // 4. Subscribe to the new mini-game's events.
            // This allows the manager to react to state changes within the mini-game.
            currentActiveMiniGame.OnGameInitialized += HandleGameInitialized;
            currentActiveMiniGame.OnGameStarted += HandleGameStarted;
            currentActiveMiniGame.OnGameEnded += HandleGameEnded;
            currentActiveMiniGame.OnMessageUpdate += HandleGameMessage;

            // 5. Initialize the mini-game.
            // The mini-game will call ReportGameInitialized() when it's ready.
            Debug.Log($"MiniGameManager: Initializing mini-game '{gameID}'.");
            currentActiveMiniGame.Initialize(() =>
            {
                Debug.Log($"MiniGameManager: Mini-game '{gameID}' reported initialization complete via callback.");
                // This callback ensures that StartGame is only called after Initialize is truly done.
                // If Initialize does async work, this is crucial. For simple sync init,
                // HandleGameInitialized would suffice.
                currentActiveMiniGame.StartGame();
            });
        }

        /// <summary>
        /// Ends the currently active mini-game gracefully.
        /// </summary>
        public void EndCurrentMiniGame()
        {
            if (currentActiveMiniGame != null)
            {
                Debug.Log($"MiniGameManager: Requesting EndGame for '{currentActiveMiniGame.GameID}'.");
                currentActiveMiniGame.EndGame(); // Allows the mini-game to perform its own shutdown logic
                
                // The HandleGameEnded will be called by the game itself
                // which then calls Cleanup and nulls currentActiveMiniGame
            }
            else
            {
                Debug.LogWarning("MiniGameManager: No active mini-game to end.");
            }
        }

        // =========================================================================
        // Event Handlers for the current active mini-game
        // =========================================================================

        private void HandleGameInitialized(string gameID)
        {
            // This is primarily for logging, as StartGame is usually called directly
            // after Initialize by the manager, or via callback for async initialization.
            Debug.Log($"MiniGameManager: Received OnGameInitialized for '{gameID}'.");
            // If Initialize is synchronous, we could call StartGame here instead of the callback in StartMiniGame.
            // For robustness, especially with async initializations, using the callback is safer.
        }

        private void HandleGameStarted(string gameID)
        {
            Debug.Log($"MiniGameManager: Received OnGameStarted for '{gameID}'.");
            OnMiniGameStarted?.Invoke(gameID); // Propagate to UI or other systems
        }

        private void HandleGameEnded(string gameID, MiniGameResult result, float score)
        {
            Debug.Log($"MiniGameManager: Received OnGameEnded for '{gameID}' with result: {result}, score: {score}.");
            OnMiniGameEnded?.Invoke(gameID, result, score); // Propagate to UI or other systems

            // Unsubscribe from events to prevent memory leaks, especially when destroying the object.
            if (currentActiveMiniGame != null)
            {
                currentActiveMiniGame.OnGameInitialized -= HandleGameInitialized;
                currentActiveMiniGame.OnGameStarted -= HandleGameStarted;
                currentActiveMiniGame.OnGameEnded -= HandleGameEnded;
                currentActiveMiniGame.OnMessageUpdate -= HandleGameMessage;

                currentActiveMiniGame.Cleanup(); // Perform game-specific cleanup (e.g., destroy its GameObject)
                currentActiveMiniGame = null; // Clear reference to the ended game
            }
        }

        private void HandleGameMessage(string gameID, string message)
        {
            Debug.Log($"MiniGameManager: Game '{gameID}' message: {message}");
            OnMiniGameMessage?.Invoke(gameID, message); // Propagate message to UI
        }

        private void OnDestroy()
        {
            // Clear singleton instance on destruction to prevent references to a destroyed object.
            if (Instance == this)
            {
                Instance = null;
            }

            // Ensure to end and clean up any active game if the manager is destroyed.
            if (currentActiveMiniGame != null)
            {
                EndCurrentMiniGame(); // This will trigger Cleanup and null out currentActiveMiniGame
            }
        }
    }
}
```

---

### **3. GuessTheNumberMiniGame.cs**

A concrete implementation of `MiniGameBase` for a "Guess the Number" game.

```csharp
using UnityEngine;
using System; // For Action
using Random = UnityEngine.Random; // Clarify that we're using Unity's Random

namespace MiniGameFramework
{
    /// <summary>
    /// A concrete mini-game: "Guess the Number".
    /// The player tries to guess a secret number within a given range and number of attempts.
    /// </summary>
    public class GuessTheNumberMiniGame : MiniGameBase
    {
        [Header("Guess The Number Settings")]
        [Tooltip("The minimum possible number to guess.")]
        [SerializeField] private int minNumber = 1;
        [Tooltip("The maximum possible number to guess.")]
        [SerializeField] private int maxNumber = 100;
        [Tooltip("Maximum number of attempts the player has.")]
        [SerializeField] private int maxAttempts = 7;

        private int targetNumber;
        private int currentAttempts;
        private Action onInitializedCallback; // Store callback for Initialize

        void Awake()
        {
            gameID = "GuessTheNumber"; // Set the unique ID for this mini-game
        }

        /// <summary>
        /// Initializes the Guess the Number mini-game.
        /// Resets game state and prepares for a new round.
        /// </summary>
        public override void Initialize(Action callback = null)
        {
            onInitializedCallback = callback;
            Debug.Log($"'{gameID}' Initializing...");
            ResetGame(); // Reset to initial state
            ReportGameInitialized(); // Inform manager that initialization is complete
            onInitializedCallback?.Invoke(); // Invoke the callback if provided
        }

        /// <summary>
        /// Starts the Guess the Number mini-game.
        /// Generates a new target number and informs the player.
        /// </summary>
        public override void StartGame()
        {
            if (IsRunning) return; // Prevent starting if already running

            IsRunning = true;
            targetNumber = Random.Range(minNumber, maxNumber + 1); // +1 because max is exclusive for int
            currentAttempts = 0;
            HasCompleted = false;
            GameResult = MiniGameResult.None;

            ReportMessage($"Guess the number between {minNumber} and {maxNumber}. You have {maxAttempts} attempts.");
            ReportGameStarted();
            Debug.Log($"'{gameID}' Started. Target number (for debugging): {targetNumber}"); // For debug
        }

        /// <summary>
        /// Ends the Guess the Number mini-game.
        /// </summary>
        public override void EndGame()
        {
            if (!IsRunning && HasCompleted) return; // Already ended

            Debug.Log($"'{gameID}' Ending Game.");
            ReportGameEnded(GameResult, currentAttempts); // Report final result and score (attempts)
        }

        /// <summary>
        /// Resets the Guess the Number mini-game to its default state.
        /// </summary>
        public override void ResetGame()
        {
            IsRunning = false;
            HasCompleted = false;
            GameResult = MiniGameResult.None;
            targetNumber = 0;
            currentAttempts = 0;
            ReportMessage("Game Reset. Ready to play!");
            Debug.Log($"'{gameID}' Reset.");
        }

        /// <summary>
        /// Player attempts to guess the number.
        /// This method is called by the UI when the player submits a guess.
        /// </summary>
        /// <param name="guess">The number guessed by the player.</param>
        public void MakeGuess(int guess)
        {
            if (!IsRunning)
            {
                ReportMessage("Game is not running. Start a new game first!");
                return;
            }

            if (guess < minNumber || guess > maxNumber)
            {
                ReportMessage($"Please guess a number between {minNumber} and {maxNumber}.");
                return;
            }

            currentAttempts++;

            if (guess == targetNumber)
            {
                ReportMessage($"Congratulations! You guessed {targetNumber} in {currentAttempts} attempts!");
                GameResult = MiniGameResult.Win;
                EndGame();
            }
            else if (currentAttempts >= maxAttempts)
            {
                ReportMessage($"Out of attempts! The number was {targetNumber}.");
                GameResult = MiniGameResult.Loss;
                EndGame();
            }
            else
            {
                string hint = (guess < targetNumber) ? "Higher!" : "Lower!";
                ReportMessage($"Your guess {guess} is incorrect. {hint} You have {maxAttempts - currentAttempts} attempts left.");
            }
        }
    }
}
```

---

### **4. ReactionTestMiniGame.cs**

Another concrete implementation of `MiniGameBase` for a "Reaction Test" game.

```csharp
using UnityEngine;
using System; // For Action
using System.Collections; // For Coroutines
using Random = UnityEngine.Random;

namespace MiniGameFramework
{
    /// <summary>
    /// A concrete mini-game: "Reaction Test".
    /// The player must react as quickly as possible after a random delay.
    /// </summary>
    public class ReactionTestMiniGame : MiniGameBase
    {
        [Header("Reaction Test Settings")]
        [Tooltip("Minimum random delay before the 'REACT!' signal appears.")]
        [SerializeField] private float minDelay = 1.0f;
        [Tooltip("Maximum random delay before the 'REACT!' signal appears.")]
        [SerializeField] private float maxDelay = 5.0f;
        [Tooltip("Time limit after 'REACT!' before failure.")]
        [SerializeField] private float reactionTimeLimit = 2.0f; 

        private float startTime;
        private bool canReact; // True when player should react
        private Coroutine gameCoroutine;
        private Action onInitializedCallback; // Store callback for Initialize

        void Awake()
        {
            gameID = "ReactionTest"; // Set the unique ID for this mini-game
        }

        /// <summary>
        /// Initializes the Reaction Test mini-game.
        /// </summary>
        public override void Initialize(Action callback = null)
        {
            onInitializedCallback = callback;
            Debug.Log($"'{gameID}' Initializing...");
            ResetGame(); // Reset to initial state
            ReportGameInitialized();
            onInitializedCallback?.Invoke(); // Invoke the callback if provided
        }

        /// <summary>
        /// Starts the Reaction Test mini-game.
        /// Initiates a random delay before prompting the player to react.
        /// </summary>
        public override void StartGame()
        {
            if (IsRunning) return;

            IsRunning = true;
            canReact = false;
            HasCompleted = false;
            GameResult = MiniGameResult.None;

            ReportMessage($"Get ready! Click 'React!' button as fast as you can when prompted.");
            ReportGameStarted();

            gameCoroutine = StartCoroutine(ReactionSequence());
            Debug.Log($"'{gameID}' Started.");
        }

        /// <summary>
        /// Coroutine handling the reaction test sequence.
        /// </summary>
        private IEnumerator ReactionSequence()
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            // Time to react!
            startTime = Time.time;
            canReact = true;
            ReportMessage("REACT NOW!");

            // Start a timer for the reaction time limit
            yield return new WaitForSeconds(reactionTimeLimit);

            if (canReact) // If player didn't react within the limit
            {
                ReportMessage("Too slow! You didn't react in time.");
                GameResult = MiniGameResult.Loss;
                EndGame();
            }
        }

        /// <summary>
        /// Ends the Reaction Test mini-game.
        /// </summary>
        public override void EndGame()
        {
            if (!IsRunning && HasCompleted) return;

            Debug.Log($"'{gameID}' Ending Game.");
            if (gameCoroutine != null)
            {
                StopCoroutine(gameCoroutine);
                gameCoroutine = null;
            }
            canReact = false;
            ReportGameEnded(GameResult, GameResult == MiniGameResult.Win ? Time.time - startTime : 0f);
        }

        /// <summary>
        /// Resets the Reaction Test mini-game to its default state.
        /// </summary>
        public override void ResetGame()
        {
            if (gameCoroutine != null)
            {
                StopCoroutine(gameCoroutine);
                gameCoroutine = null;
            }
            IsRunning = false;
            HasCompleted = false;
            GameResult = MiniGameResult.None;
            canReact = false;
            startTime = 0f;
            ReportMessage("Game Reset. Ready to react!");
            Debug.Log($"'{gameID}' Reset.");
        }

        /// <summary>
        /// Called when the player clicks the "React!" button.
        /// </summary>
        public void PlayerReacted()
        {
            if (!IsRunning)
            {
                ReportMessage("Game is not running. Start a new game first!");
                return;
            }

            if (!canReact)
            {
                ReportMessage("Too early! You reacted before the prompt.");
                GameResult = MiniGameResult.Loss;
                EndGame();
            }
            else
            {
                float reactionTime = Time.time - startTime;
                ReportMessage($"Reaction time: {reactionTime:F3} seconds!");
                GameResult = MiniGameResult.Win;
                EndGame();
            }
        }
    }
}
```

---

### **5. MiniGameUIManager.cs**

A simple UI manager to display mini-game information and handle player input. It subscribes to `MiniGameManager` events to update its state.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI and TMP_InputField
using UnityEngine.UI; // Required for Button
using System; // For Action

namespace MiniGameFramework
{
    /// <summary>
    /// Manages the UI for interacting with the MiniGameFramework.
    /// It subscribes to events from MiniGameManager to display game state,
    /// and provides methods for UI elements to trigger game actions.
    /// </summary>
    public class MiniGameUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject miniGameSelectionPanel;
        [SerializeField] private GameObject gamePanel; // General panel for active game display
        [SerializeField] private GameObject guessInputPanel; // Specific panel for GuessTheNumber game input
        [SerializeField] private GameObject reactionTestPanel; // Specific panel for ReactionTest game input

        [Header("Common Game UI Elements")]
        [SerializeField] private TextMeshProUGUI gameTitleText;
        [SerializeField] private TextMeshProUGUI gameInstructionsText;
        [SerializeField] private TextMeshProUGUI gameMessageText;
        [SerializeField] private TextMeshProUGUI gameResultText;
        [SerializeField] private Button backToSelectionButton;

        [Header("Guess The Number UI")]
        [SerializeField] private TMP_InputField guessInputField;
        [SerializeField] private Button guessSubmitButton;

        [Header("Reaction Test UI")]
        [SerializeField] private Button reactButton;

        // Reference to the MiniGameManager singleton
        private MiniGameManager manager;

        void Awake()
        {
            // Find the MiniGameManager instance and subscribe to its events.
            // Ensure MiniGameManager's Awake runs before this UIManager's Awake if possible
            // (e.g., set Script Execution Order in Project Settings).
            manager = MiniGameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("MiniGameUIManager: MiniGameManager not found! Make sure it exists in the scene.");
                enabled = false;
                return;
            }

            // Subscribe to manager events
            manager.OnMiniGameLoaded += HandleMiniGameLoaded;
            manager.OnMiniGameStarted += HandleMiniGameStarted;
            manager.OnMiniGameEnded += HandleMiniGameEnded;
            manager.OnMiniGameMessage += HandleMiniGameMessage;

            // Set up UI button listeners
            if (backToSelectionButton != null)
            {
                backToSelectionButton.onClick.AddListener(ShowMiniGameSelection);
            }
            if (guessSubmitButton != null)
            {
                guessSubmitButton.onClick.AddListener(OnGuessButtonClicked);
            }
            if (reactButton != null)
            {
                reactButton.onClick.AddListener(OnReactionButtonClicked);
            }
        }

        void Start()
        {
            // Show the initial game selection panel
            ShowMiniGameSelection();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (manager != null)
            {
                manager.OnMiniGameLoaded -= HandleMiniGameLoaded;
                manager.OnMiniGameStarted -= HandleMiniGameStarted;
                manager.OnMiniGameEnded -= HandleMiniGameEnded;
                manager.OnMiniGameMessage -= HandleMiniGameMessage;
            }

            // Clean up button listeners
            if (backToSelectionButton != null)
            {
                backToSelectionButton.onClick.RemoveListener(ShowMiniGameSelection);
            }
            if (guessSubmitButton != null)
            {
                guessSubmitButton.onClick.RemoveListener(OnGuessButtonClicked);
            }
            if (reactButton != null)
            {
                reactButton.onClick.RemoveListener(OnReactionButtonClicked);
            }
        }

        // =========================================================================
        // UI Display Control
        // =========================================================================

        /// <summary>
        /// Shows the panel for selecting a mini-game and hides all other game-specific UI.
        /// </summary>
        public void ShowMiniGameSelection()
        {
            // Clear previous game state displays
            gameTitleText.text = "";
            gameInstructionsText.text = "";
            gameMessageText.text = "";
            gameResultText.text = "";

            miniGameSelectionPanel.SetActive(true);
            gamePanel.SetActive(false);
            guessInputPanel.SetActive(false);
            reactionTestPanel.SetActive(false);

            // If a game is currently active, ensure it's ended
            if (manager.CurrentActiveMiniGame != null)
            {
                manager.EndCurrentMiniGame();
            }
            Debug.Log("UI: Showing Mini-Game Selection.");
        }

        /// <summary>
        /// Shows the general game panel and hides selection. Game-specific panels will be shown later.
        /// </summary>
        private void ShowGamePanel()
        {
            miniGameSelectionPanel.SetActive(false);
            gamePanel.SetActive(true);
            guessInputPanel.SetActive(false);
            reactionTestPanel.SetActive(false);
        }

        /// <summary>
        /// Handles the UI response when a mini-game is loaded by the manager.
        /// </summary>
        /// <param name="gameID">The ID of the loaded game.</param>
        private void HandleMiniGameLoaded(string gameID)
        {
            ShowGamePanel();
            gameTitleText.text = $"Loading: {gameID}";
            gameInstructionsText.text = "";
            gameMessageText.text = "Initializing...";
            gameResultText.text = "";

            // Activate specific UI panels based on the loaded game type
            if (gameID == "GuessTheNumber")
            {
                guessInputPanel.SetActive(true);
                guessInputField.text = "";
                guessInputField.Select();
                guessInputField.ActivateInputField(); // Focus the input field
            }
            else if (gameID == "ReactionTest")
            {
                reactionTestPanel.SetActive(true);
            }
            Debug.Log($"UI: Mini-game '{gameID}' loaded.");
        }

        /// <summary>
        /// Handles the UI response when a mini-game starts.
        /// </summary>
        /// <param name="gameID">The ID of the started game.</param>
        private void HandleMiniGameStarted(string gameID)
        {
            gameTitleText.text = $"Playing: {gameID}";
            Debug.Log($"UI: Mini-game '{gameID}' started.");
        }

        /// <summary>
        /// Handles the UI response when a mini-game ends.
        /// </summary>
        /// <param name="gameID">The ID of the ended game.</param>
        /// <param name="result">The outcome of the game.</param>
        /// <param name="score">The score obtained in the game.</param>
        private void HandleMiniGameEnded(string gameID, MiniGameResult result, float score)
        {
            string resultString = $"Result: {result}";
            if (gameID == "ReactionTest")
            {
                resultString += result == MiniGameResult.Win ? $" ({score:F3}s)" : "";
            }
            else if (gameID == "GuessTheNumber")
            {
                resultString += result == MiniGameResult.Win ? $" ({score} attempts)" : "";
            }

            gameResultText.text = resultString;
            Debug.Log($"UI: Mini-game '{gameID}' ended with {result} and score {score}.");

            // Disable game-specific input panels
            guessInputPanel.SetActive(false);
            reactionTestPanel.SetActive(false);
        }

        /// <summary>
        /// Handles and displays general messages from the active mini-game.
        /// </summary>
        /// <param name="gameID">The ID of the game sending the message.</param>
        /// <param name="message">The message string.</param>
        private void HandleMiniGameMessage(string gameID, string message)
        {
            gameMessageText.text = message;
        }

        // =========================================================================
        // UI Button Handlers (Connected via Inspector or Code)
        // =========================================================================

        /// <summary>
        /// Called when the "Start Guess The Number" button is clicked.
        /// </summary>
        public void SelectGuessTheNumber()
        {
            manager.StartMiniGame("GuessTheNumber");
        }

        /// <summary>
        /// Called when the "Start Reaction Test" button is clicked.
        /// </summary>
        public void SelectReactionTest()
        {
            manager.StartMiniGame("ReactionTest");
        }

        /// <summary>
        /// Called when the "Guess" button is clicked for GuessTheNumber game.
        /// </summary>
        public void OnGuessButtonClicked()
        {
            if (manager.CurrentActiveMiniGame != null && manager.CurrentActiveMiniGame.GameID == "GuessTheNumber")
            {
                if (int.TryParse(guessInputField.text, out int guess))
                {
                    (manager.CurrentActiveMiniGame as GuessTheNumberMiniGame)?.MakeGuess(guess);
                    guessInputField.text = ""; // Clear input after guess
                    guessInputField.Select();
                    guessInputField.ActivateInputField(); // Re-focus for next guess
                }
                else
                {
                    gameMessageText.text = "Please enter a valid number!";
                }
            }
        }

        /// <summary>
        /// Called when the "React!" button is clicked for ReactionTest game.
        /// </summary>
        public void OnReactionButtonClicked()
        {
            if (manager.CurrentActiveMiniGame != null && manager.CurrentActiveMiniGame.GameID == "ReactionTest")
            {
                (manager.CurrentActiveMiniGame as ReactionTestMiniGame)?.PlayerReacted();
            }
        }
    }
}
```