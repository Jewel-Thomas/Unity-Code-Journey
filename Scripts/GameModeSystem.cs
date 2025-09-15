// Unity Design Pattern Example: GameModeSystem
// This script demonstrates the GameModeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete and practical implementation of the **GameModeSystem** design pattern in Unity using C#. It's designed to be educational, demonstrating how to structure your game's high-level states (like Main Menu, Gameplay, Pause, Game Over) in a clean, scalable, and decoupled manner.

---

### Understanding the GameModeSystem Pattern

The GameModeSystem pattern, also known as State Machine for Game Modes, provides a structured way to manage the different high-level states of your game.

1.  **`AGameMode` (Abstract Base Class)**:
    *   Defines the common interface for all game modes.
    *   Contains virtual methods for lifecycle events (Enter, Exit, Update, FixedUpdate, OnGUI) that concrete modes can override.
    *   Provides a reference to the `GameModeManager` for mode switching or accessing global functionalities.

2.  **`GameModeManager` (Singleton MonoBehaviour)**:
    *   The central hub that orchestrates game mode changes.
    *   Acts as a singleton (`Instance`) so it can be easily accessed from anywhere in the game.
    *   Manages a collection of all registered `AGameMode` instances.
    *   Handles switching between modes, ensuring the `ExitMode()` of the old mode and `EnterMode()` of the new mode are called correctly.
    *   Delegates Unity's `Update`, `FixedUpdate`, and `OnGUI` calls to the currently active game mode.

3.  **Concrete Game Modes (e.g., `MainMenuMode`, `GameplayMode`)**:
    *   Implement the `AGameMode` abstract class.
    *   Each concrete mode encapsulates the logic, UI, and input handling specific to that game state.
    *   They interact with the `GameModeManager` to request mode switches.

**Benefits of this pattern:**

*   **Clear Separation of Concerns**: Each game mode is a self-contained unit, making code easier to understand, maintain, and test.
*   **Scalability**: Adding new game modes is straightforward; just create a new class inheriting from `AGameMode` and register it.
*   **Controlled State Transitions**: The `GameModeManager` ensures proper cleanup (Exit) and setup (Enter) during mode changes.
*   **Reduced Coupling**: Game logic doesn't need to know about other game modes directly; it just requests a switch via the manager.

---

### Project Setup and How to Use

1.  **Create C# Scripts:**
    *   Create a folder named `GameModeSystem` (or similar) in your Unity project.
    *   Inside this folder, create the following C# script files:
        *   `AGameMode.cs`
        *   `GameModeManager.cs`
        *   `MainMenuMode.cs`
        *   `GameplayMode.cs`
        *   `PauseMode.cs`
        *   `GameOverMode.cs`
    *   Copy the corresponding code blocks into each file.

2.  **TextMeshPro (for UI Text):**
    *   If you don't have TextMeshPro imported into your project, go to `Window > TextMeshPro > Import TMP Essential Resources`. This is needed for the optional UI display.

3.  **Create GameModeManager GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., `GameModeSystem`).
    *   Attach the `GameModeManager.cs` script to this GameObject.

4.  **Setup UI Text (Optional but recommended):**
    *   Create a new UI Canvas: `GameObject > UI > Canvas`.
    *   Inside the Canvas, create a TextMeshPro Text object: `GameObject > UI > Text - TextMeshPro`. Name it something like `CurrentModeText`.
    *   Position this text component somewhere visible on your screen (e.g., top-left).
    *   Drag this `CurrentModeText` GameObject from the Hierarchy into the `Current Mode UI Text` field of the `GameModeManager` component in the Inspector.

5.  **Set Initial Mode:**
    *   In the `GameModeManager` component in the Inspector, ensure the `Initial Mode Name` field is set to `MainMenu`. This tells the system which mode to start in.

6.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe the `Debug.Log` messages in the Console, which will show the lifecycle events of the game modes.
    *   If you set up the UI Text, it will display the current active mode.

7.  **Interact with the System (using keyboard input):**
    *   **MainMenuMode**: Press 'P' to Play, 'E' to Exit (logs in editor, quits in build), 'O' for Options (example of custom event).
    *   **GameplayMode**: Press 'Escape' to Pause, 'G' to simulate Game Over, 'Space' to score points.
    *   **PauseMode**: Press 'Escape' to Resume (back to Gameplay), 'M' for Main Menu.
    *   **GameOverMode**: Press 'R' to Restart (back to Gameplay), 'M' for Main Menu.

---

### 1. `AGameMode.cs` (Abstract Base Class)

This abstract class defines the contract that all specific game modes must adhere to. It establishes the lifecycle methods that the `GameModeManager` will call.

```csharp
using UnityEngine;

// This is the abstract base class for all game modes.
// It defines the common interface and lifecycle methods that every game mode must implement.
// This ensures that the GameModeManager can interact with any game mode in a consistent way.
public abstract class AGameMode
{
    // A reference to the GameModeManager. This allows game modes to request
    // mode switches or access other manager functionalities.
    protected GameModeManager Manager { get; private set; }

    // A unique identifier for this game mode.
    // It's used by the GameModeManager to register and retrieve modes.
    public abstract string ModeName { get; }

    // This method is called once when the game mode is first created and registered with the manager.
    // It's a good place for one-time setup that doesn't depend on the mode being active.
    public virtual void Initialize(GameModeManager manager)
    {
        Manager = manager;
        Debug.Log($"[GameModeSystem] Initialized Mode: {ModeName}");
    }

    // Called when this game mode becomes the active mode.
    // Use this for setup that should happen every time the mode is entered
    // (e.g., enabling specific UI, activating player input, loading level elements).
    public virtual void EnterMode()
    {
        Debug.Log($"[GameModeSystem] Entering Mode: {ModeName}");
    }

    // Called when this game mode is no longer the active mode, just before
    // another mode becomes active.
    // Use this for cleanup specific to this mode (e.g., disabling UI,
    // deactivating player input, saving progress).
    public virtual void ExitMode()
    {
        Debug.Log($"[GameModeSystem] Exiting Mode: {ModeName}");
    }

    // Called every frame while this game mode is active, similar to MonoBehaviour.Update().
    // Use this for frame-dependent logic like input handling, UI updates, etc.
    public virtual void UpdateMode() { }

    // Called every fixed frame while this game mode is active, similar to MonoBehaviour.FixedUpdate().
    // Use this for physics calculations or other fixed-timestep logic.
    public virtual void FixedUpdateMode() { }

    // Called for rendering and handling GUI events while this game mode is active, similar to MonoBehaviour.OnGUI().
    // Use this for IMGUI-based UI or debugging overlays.
    public virtual void OnGUIMode() { }

    // (Optional) Example of a custom event handler for a mode.
    // You could expand this to include more specific event types or data.
    public virtual void HandleCustomEvent(string eventType, object data)
    {
        Debug.Log($"[GameModeSystem] {ModeName} received custom event: {eventType} with data: {data}");
    }
}
```

---

### 2. `GameModeManager.cs` (Singleton MonoBehaviour)

This is the core of the GameModeSystem. It's a `MonoBehaviour` singleton that manages all game modes, handles transitions, and delegates Unity's update calls.

```csharp
using UnityEngine;
using System.Collections.Generic;
using TMPro; // Required for TextMeshProUGUI. Add 'using UnityEngine.UI;' if using legacy UI.

// The central manager for controlling game modes.
// It's implemented as a MonoBehaviour singleton to persist across scenes
// and provide a global access point for mode management.
public class GameModeManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // Provides a globally accessible instance of the GameModeManager.
    public static GameModeManager Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Game Mode Manager Settings")]
    [Tooltip("The initial game mode to start when the application begins.")]
    [SerializeField] private string _initialModeName = MainMenuMode.ModeKey; // Default to MainMenu

    [Tooltip("Optional: A TextMeshProUGUI component to display the current game mode in the UI.")]
    [SerializeField] private TextMeshProUGUI _currentModeUIText;

    // --- Internal State ---
    // The currently active game mode. Only one mode can be active at a time.
    private AGameMode _currentMode;
    // A dictionary to store all registered game modes, allowing quick lookup by name.
    private Dictionary<string, AGameMode> _registeredModes = new Dictionary<string, AGameMode>();

    // Public property to get the currently active mode.
    public AGameMode CurrentMode => _currentMode;

    // --- MonoBehaviour Lifecycle Methods ---

    private void Awake()
    {
        // Enforce the singleton pattern. If another instance exists, destroy this one.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameModeSystem] Duplicate GameModeManager detected, destroying this instance.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Make sure the manager persists across scene loads.
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameModeSystem] GameModeManager initialized.");
        }

        // Initialize and register all game modes. This should happen early.
        RegisterAllGameModes();
    }

    private void Start()
    {
        // Switch to the initial game mode specified in the Inspector.
        if (!string.IsNullOrEmpty(_initialModeName))
        {
            SwitchMode(_initialModeName);
        }
        else
        {
            Debug.LogError("[GameModeSystem] No initial mode name specified! Please set it in the Inspector.");
        }
    }

    // These MonoBehaviour update methods simply delegate calls to the current active game mode.
    // This allows the game mode to handle its own frame-based logic.
    private void Update()
    {
        _currentMode?.UpdateMode();
    }

    private void FixedUpdate()
    {
        _currentMode?.FixedUpdateMode();
    }

    private void OnGUI()
    {
        _currentMode?.OnGUIMode();
    }

    // --- Game Mode Registration & Switching ---

    // Registers all concrete game mode implementations with the manager.
    // This method should be called once, typically during Awake().
    // Add any new game modes here.
    private void RegisterAllGameModes()
    {
        RegisterMode(new MainMenuMode());
        RegisterMode(new GameplayMode());
        RegisterMode(new PauseMode());
        RegisterMode(new GameOverMode());

        Debug.Log($"[GameModeSystem] Registered {_registeredModes.Count} game modes.");
    }

    // Adds a game mode to the internal dictionary.
    // Each mode's Initialize method is called here.
    private void RegisterMode(AGameMode mode)
    {
        if (_registeredModes.ContainsKey(mode.ModeName))
        {
            Debug.LogWarning($"[GameModeSystem] Mode '{mode.ModeName}' already registered. Skipping.");
            return;
        }

        _registeredModes.Add(mode.ModeName, mode);
        mode.Initialize(this); // Pass a reference to this manager for modes to use.
    }

    // Attempts to switch the current game mode.
    // This is the primary method for changing game states (e.g., from MainMenu to Gameplay).
    public bool SwitchMode(string newModeName)
    {
        if (!_registeredModes.TryGetValue(newModeName, out AGameMode newMode))
        {
            Debug.LogError($"[GameModeSystem] Cannot switch to mode '{newModeName}'. Mode not registered.");
            return false;
        }

        if (_currentMode == newMode)
        {
            Debug.LogWarning($"[GameModeSystem] Already in mode '{newModeName}'. No switch needed.");
            return true;
        }

        // 1. Exit the current mode if one is active.
        _currentMode?.ExitMode();

        // 2. Set the new mode.
        _currentMode = newMode;

        // 3. Enter the new mode.
        _currentMode.EnterMode();

        // 4. Update UI if a TextMeshPro component is assigned.
        UpdateModeUIText();

        Debug.Log($"[GameModeSystem] Successfully switched to mode: {_currentMode.ModeName}");
        return true;
    }

    // Retrieves a registered game mode instance by its name.
    // Useful if you need to access specific properties or methods of a particular mode
    // (e.g., in GameOverMode, you might want to get the score from GameplayMode).
    public T GetMode<T>(string modeName) where T : AGameMode
    {
        if (_registeredModes.TryGetValue(modeName, out AGameMode mode))
        {
            if (mode is T castMode)
            {
                return castMode;
            }
            else
            {
                Debug.LogError($"[GameModeSystem] Mode '{modeName}' found, but it's not of expected type '{typeof(T).Name}'.");
                return null;
            }
        }
        // Debug.LogWarning($"[GameModeSystem] Mode '{modeName}' not found."); // Uncomment if you want warnings for unfound modes
        return null;
    }

    // --- UI Helpers ---
    private void UpdateModeUIText()
    {
        if (_currentModeUIText != null)
        {
            _currentModeUIText.text = $"Current Mode: {_currentMode?.ModeName ?? "None"}";
        }
    }

    // --- Example of a global event dispatcher that modes can listen to ---
    // You can expand this for a more robust event system (e.g., UnityEvents, custom delegate events).
    public void DispatchGlobalEvent(string eventType, object data = null)
    {
        Debug.Log($"[GameModeSystem] Dispatching global event: {eventType}");
        _currentMode?.HandleCustomEvent(eventType, data);
        // You could also iterate through all registered modes or specific listeners
        // if events should be handled by multiple modes, even inactive ones.
    }
}
```

---

### 3. `MainMenuMode.cs` (Concrete Game Mode)

An example implementation for the main menu state. It handles displaying the menu and transitioning to gameplay or quitting.

```csharp
using UnityEngine;

// Concrete implementation of a game mode for the main menu.
public class MainMenuMode : AGameMode
{
    // Define a constant for the mode's key to avoid magic strings.
    public const string ModeKey = "MainMenu";

    // Override the ModeName property to return the unique key.
    public override string ModeName => ModeKey;

    // --- Game Mode Lifecycle Overrides ---

    public override void EnterMode()
    {
        base.EnterMode(); // Call the base implementation for common logging.
        Debug.Log("[MainMenuMode] Displaying main menu UI. Press 'P' to Play, 'E' to Exit, 'O' for Options.");
        // Example: Enable Main Menu Canvas, show title, play main menu music.
        // UIManager.Instance.ShowPanel("MainMenuPanel");
    }

    public override void ExitMode()
    {
        base.ExitMode(); // Call the base implementation.
        Debug.Log("[MainMenuMode] Hiding main menu UI.");
        // Example: Hide Main Menu Canvas.
        // UIManager.Instance.HidePanel("MainMenuPanel");
    }

    public override void UpdateMode()
    {
        // Example: Handle input specific to the main menu.
        if (Input.GetKeyDown(KeyCode.P)) // 'P' for Play
        {
            Debug.Log("[MainMenuMode] 'Play' pressed. Switching to Gameplay Mode...");
            Manager.SwitchMode(GameplayMode.ModeKey);
        }
        else if (Input.GetKeyDown(KeyCode.E)) // 'E' for Exit
        {
            Debug.Log("[MainMenuMode] 'Exit' pressed. Quitting Application...");
            Application.Quit();
            // In editor, Application.Quit doesn't actually quit, so log it.
#if UNITY_EDITOR
            Debug.Log("[MainMenuMode] Application.Quit() called in editor.");
#endif
        }
        else if (Input.GetKeyDown(KeyCode.O)) // 'O' for Options (example of custom event)
        {
            Debug.Log("[MainMenuMode] 'Options' pressed. Dispatching custom event...");
            Manager.DispatchGlobalEvent("ShowOptions", "From MainMenu");
        }
    }

    // Example of handling a custom event within this specific mode.
    public override void HandleCustomEvent(string eventType, object data)
    {
        base.HandleCustomEvent(eventType, data); // Call base for logging
        if (eventType == "ShowOptions")
        {
            Debug.Log($"[MainMenuMode] Options requested. Showing options menu with data: {data}");
            // Example: UIManager.Instance.ShowPanel("OptionsPanel");
        }
    }
}
```

---

### 4. `GameplayMode.cs` (Concrete Game Mode)

An example implementation for the active gameplay state. It manages game time, score, and transitions to pause or game over.

```csharp
using UnityEngine;

// Concrete implementation of a game mode for active gameplay.
public class GameplayMode : AGameMode
{
    public const string ModeKey = "Gameplay";
    public override string ModeName => ModeKey;

    private float _gameplayTime = 0f;
    private int _score = 0;

    // --- Game Mode Lifecycle Overrides ---

    public override void Initialize(GameModeManager manager)
    {
        base.Initialize(manager);
        Debug.Log("[GameplayMode] Gameplay systems initialized (e.g., enemy spawning, player setup).");
        // Example: Register to an event, e.g., PlayerDiedEvent += OnPlayerDied;
    }

    public override void EnterMode()
    {
        base.EnterMode();
        Debug.Log("[GameplayMode] Starting gameplay. Player input enabled, enemies spawning. Press 'Escape' to Pause, 'G' for Game Over, 'Space' to Score.");
        _gameplayTime = 0f;
        _score = 0;
        // Example: Enable player controller, activate game logic, load game scene if not already loaded.
        // PlayerController.Instance.EnableInput();
        // EnemySpawner.Instance.StartSpawning();
    }

    public override void ExitMode()
    {
        base.ExitMode();
        Debug.Log("[GameplayMode] Ending gameplay. Player input disabled, game logic paused/reset.");
        // Example: Disable player controller, stop enemy spawning, save score.
        // PlayerController.Instance.DisableInput();
        // EnemySpawner.Instance.StopSpawning();
    }

    public override void UpdateMode()
    {
        _gameplayTime += Time.deltaTime;
        // Example: Check for player input, update game state.
        if (Input.GetKeyDown(KeyCode.Escape)) // 'Escape' for Pause
        {
            Debug.Log("[GameplayMode] 'Escape' pressed. Switching to Pause Mode...");
            Manager.SwitchMode(PauseMode.ModeKey);
        }
        else if (Input.GetKeyDown(KeyCode.G)) // 'G' for Game Over (simulate condition)
        {
            Debug.Log("[GameplayMode] 'G' pressed. Simulating Game Over...");
            Manager.SwitchMode(GameOverMode.ModeKey);
            // Example: Manager.DispatchGlobalEvent("GameOver", new GameOverData { Score = _score, Time = _gameplayTime });
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            _score += 10;
            Debug.Log($"[GameplayMode] Player scored! Current Score: {_score}");
        }
    }

    // You can add specific methods for this mode if needed, e.g., for other modes to query.
    public int GetCurrentScore() => _score;
    public float GetCurrentGameplayTime() => _gameplayTime;
}
```

---

### 5. `PauseMode.cs` (Concrete Game Mode)

An example implementation for the pause state, handling pausing/unpausing game time and menu options.

```csharp
using UnityEngine;

// Concrete implementation of a game mode for pausing the game.
public class PauseMode : AGameMode
{
    public const string ModeKey = "Pause";
    public override string ModeName => ModeKey;

    // --- Game Mode Lifecycle Overrides ---

    public override void EnterMode()
    {
        base.EnterMode();
        Debug.Log("[PauseMode] Game paused. Displaying pause menu. Press 'Escape' to Resume, 'M' for Main Menu.");
        Time.timeScale = 0f; // Pause game time.
        // Example: Enable Pause Menu Canvas.
        // UIManager.Instance.ShowPanel("PauseMenuPanel");
    }

    public override void ExitMode()
    {
        base.ExitMode();
        Debug.Log("[PauseMode] Game unpaused. Hiding pause menu.");
        Time.timeScale = 1f; // Resume game time.
        // Example: Hide Pause Menu Canvas.
        // UIManager.Instance.HidePanel("PauseMenuPanel");
    }

    public override void UpdateMode()
    {
        // In a real game, you might check for specific input to resume or go to main menu.
        // Because Time.timeScale is 0, Input.GetKeyDown still works.
        if (Input.GetKeyDown(KeyCode.Escape)) // 'Escape' to Resume
        {
            Debug.Log("[PauseMode] 'Escape' pressed. Switching back to Gameplay Mode...");
            Manager.SwitchMode(GameplayMode.ModeKey);
        }
        else if (Input.GetKeyDown(KeyCode.M)) // 'M' to Main Menu
        {
            Debug.Log("[PauseMode] 'M' pressed. Switching to Main Menu Mode...");
            Manager.SwitchMode(MainMenuMode.ModeKey);
        }
    }
}
```

---

### 6. `GameOverMode.cs` (Concrete Game Mode)

An example implementation for the game over state, displaying results and offering restart or main menu options.

```csharp
using UnityEngine;

// Concrete implementation of a game mode for the game over state.
public class GameOverMode : AGameMode
{
    public const string ModeKey = "GameOver";
    public override string ModeName => ModeKey;

    // --- Game Mode Lifecycle Overrides ---

    public override void EnterMode()
    {
        base.EnterMode();
        Debug.Log("[GameOverMode] Game Over! Displaying score and options. Press 'R' to Restart, 'M' for Main Menu.");
        // Example: Display Game Over screen with score, high score, etc.
        // UIManager.Instance.ShowPanel("GameOverPanel");

        // Example of accessing data from a previous mode (GameplayMode) if needed.
        // This demonstrates how one mode can query information from another via the manager.
        GameplayMode gameplayMode = Manager.GetMode<GameplayMode>(GameplayMode.ModeKey);
        if (gameplayMode != null)
        {
            Debug.Log($"[GameOverMode] Final Score: {gameplayMode.GetCurrentScore()} in {gameplayMode.GetCurrentGameplayTime():F2} seconds.");
        }
        else
        {
            Debug.LogWarning("[GameOverMode] Could not retrieve GameplayMode to get final score. Was it active before?");
        }
    }

    public override void ExitMode()
    {
        base.ExitMode();
        Debug.Log("[GameOverMode] Hiding game over screen.");
        // Example: Hide Game Over screen.
        // UIManager.Instance.HidePanel("GameOverPanel");
    }

    public override void UpdateMode()
    {
        // In this mode, typically only UI interaction for restarting or going to main menu.
        if (Input.GetKeyDown(KeyCode.R)) // 'R' for Restart
        {
            Debug.Log("[GameOverMode] 'R' pressed. Restarting Gameplay Mode...");
            // Restart by going back to gameplay, which will re-initialize its state via EnterMode.
            Manager.SwitchMode(GameplayMode.ModeKey);
        }
        else if (Input.GetKeyDown(KeyCode.M)) // 'M' for Main Menu
        {
            Debug.Log("[GameOverMode] 'M' pressed. Switching to Main Menu Mode...");
            Manager.SwitchMode(MainMenuMode.ModeKey);
        }
    }
}
```