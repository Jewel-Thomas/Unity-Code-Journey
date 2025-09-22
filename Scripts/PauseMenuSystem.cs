// Unity Design Pattern Example: PauseMenuSystem
// This script demonstrates the PauseMenuSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PauseMenuSystem' design pattern in Unity provides a robust and decoupled way to manage the game's pause state. It typically involves a central manager that handles the actual pausing logic (like setting `Time.timeScale` to 0) and broadcasts state changes. Other game systems (UI, player input, AI, physics, audio) then subscribe to these state changes and react accordingly, enabling or disabling their own functionality.

This pattern promotes **loose coupling** because:
*   The `PauseManager` doesn't need to know about specific UI elements or game objects. It just broadcasts.
*   Individual game objects don't need to know about the `PauseManager`'s internal implementation. They just listen for a global event.

Here's a complete, practical C# Unity example demonstrating the PauseMenuSystem pattern. It includes three scripts:

1.  **`PauseManager.cs`**: The central singleton responsible for managing the pause state, `Time.timeScale`, and broadcasting events.
2.  **`PauseMenuUI.cs`**: A script for your actual UI panel that listens to pause events and displays/hides itself. It also provides methods for UI buttons.
3.  **`PlayerMovement.cs`**: An example consumer script (like a player controller) that listens to pause events and enables/disables its movement.

---

### **1. `PauseManager.cs`**

This script is the core of the system. It uses a **Singleton pattern** for easy global access and **C# Events** to notify other systems about state changes.

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
///     The PauseMenuSystem Design Pattern: PauseManager
///
///     This script acts as the central hub for managing the game's pause state.
///     It follows a Singleton pattern for easy global access and uses a single
///     event to notify other systems when the game's pause state changes.
///
///     Key Responsibilities:
///     1.  Manage the global 'IsPaused' state.
///     2.  Control Time.timeScale to freeze/unfreeze game time.
///     3.  Broadcast a single event (OnPauseStateChanged) with the new state.
///     4.  Handle input for toggling the pause state (e.g., Escape key).
/// </summary>
public class PauseManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // A static reference to the single instance of the PauseManager.
    // This allows other scripts to easily access the manager's methods and properties
    // without needing a direct reference, e.g., PauseManager.Instance.PauseGame().
    public static PauseManager Instance { get; private set; }

    // --- State Variable ---
    // Public property to check if the game is currently paused.
    // 'private set' ensures only the PauseManager itself can change this state,
    // maintaining control over the pause logic.
    public bool IsPaused { get; private set; } = false;

    // --- Event for Pause State Changes ---
    // This event allows other scripts to subscribe and react when the game's pause
    // state changes. It passes a 'bool' indicating the new state (true for paused, false for unpaused).
    // 'static' ensures this event is associated with the class, not an instance,
    // making it accessible globally via PauseManager.OnPauseStateChanged.
    public static event Action<bool> OnPauseStateChanged;

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Enforce Singleton pattern:
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PauseManager instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this; // Set this instance as the singleton.
            // Optionally, uncomment the line below if you want the PauseManager to persist
            // across different scenes. For a pause menu, this is often desirable.
            // DontDestroyOnLoad(gameObject); 
        }
    }

    private void Update()
    {
        // Example input handling: Toggle pause with the Escape key.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // --- Public Methods for Pause Control ---

    /// <summary>
    /// Toggles the game's pause state (from paused to unpaused, or vice versa).
    /// This is the primary method for external calls (e.g., UI buttons, input handlers).
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            UnpauseGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pauses the game, setting Time.timeScale to 0 and notifying subscribers.
    /// </summary>
    public void PauseGame()
    {
        if (IsPaused) return; // Already paused, do nothing.

        IsPaused = true;
        Time.timeScale = 0f; // Stops all time-dependent operations (physics, animations, Update loops using Time.deltaTime).
        
        // Invoke the OnPauseStateChanged event, notifying all subscribed listeners
        // that the game is now paused (passing 'true').
        // The '?' operator ensures the event is only invoked if there are subscribers,
        // preventing NullReferenceExceptions.
        OnPauseStateChanged?.Invoke(true);
        Debug.Log("Game Paused!");
    }

    /// <summary>
    /// Unpauses the game, setting Time.timeScale back to 1 and notifying subscribers.
    /// </summary>
    public void UnpauseGame()
    {
        if (!IsPaused) return; // Already unpaused, do nothing.

        IsPaused = false;
        Time.timeScale = 1f; // Resumes normal game time.
        
        // Invoke the OnPauseStateChanged event, notifying all subscribed listeners
        // that the game is now unpaused (passing 'false').
        OnPauseStateChanged?.Invoke(false);
        Debug.Log("Game Unpaused!");
    }

    // --- Example Usage in another script (for explanation purposes) ---
    /*
    /// <summary>
    /// Example of how another script (e.g., PlayerMovement, AIController, AudioSystem)
    /// would react to the PauseManager's events.
    /// </summary>
    public class ExampleSubscriber : MonoBehaviour
    {
        private bool canPerformActions = true;

        private void OnEnable()
        {
            // Subscribe to the pause state change event when this script becomes active.
            PauseManager.OnPauseStateChanged += OnPauseStateChangedHandler;
        }

        private void OnDisable()
        {
            // Unsubscribe from the event when this script is disabled or destroyed
            // to prevent memory leaks and unexpected behavior (e.g., calling methods on a null object).
            PauseManager.OnPauseStateChanged -= OnPauseStateChangedHandler;
        }

        /// <summary>
        /// This method is called by the PauseManager whenever the game's pause state changes.
        /// </summary>
        /// <param name="isPaused">True if the game is paused, false if unpaused.</param>
        private void OnPauseStateChangedHandler(bool isPaused)
        {
            // If the game is paused, set canPerformActions to false; otherwise, set it to true.
            canPerformActions = !isPaused; 
            Debug.Log(gameObject.name + ": Reacted to game state change. Actions " + (canPerformActions ? "enabled." : "disabled."));

            // You could also specifically handle other things here:
            // if (isPaused) { /* Stop character animations, disable input, play pause sound */ }
            // else { /* Resume character animations, re-enable input, play unpause sound */ }
        }

        private void Update()
        {
            if (canPerformActions)
            {
                // Perform regular game logic here (movement, input, AI processing, etc.)
                // Example: transform.Translate(Vector3.forward * Time.deltaTime * 5f);
            }
        }
    }
    */
}
```

---

### **2. `PauseMenuUI.cs`**

This script manages the visual representation of your pause menu panel. It subscribes to the `PauseManager`'s events to show/hide itself automatically.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI components like Button.
using UnityEngine.SceneManagement; // Required for loading scenes.

/// <summary>
///     The PauseMenuSystem Design Pattern: PauseMenuUI
///
///     This script manages the visual representation and interaction of the pause menu.
///     It subscribes to the PauseManager's events to show/hide itself automatically
///     and provides methods for UI buttons to interact with the PauseManager.
///
///     Key Responsibilities:
///     1.  Show/hide the pause menu UI panel based on PauseManager events.
///     2.  Provide public methods for UI buttons (e.g., Resume, Quit, Main Menu).
///     3.  Ensure the UI is in the correct initial state.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Tooltip("Reference to the GameObject representing the entire pause menu panel.")]
    [SerializeField] private GameObject pausePanel;

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Ensure the pause panel is initially hidden when the game starts.
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuUI: Pause Panel reference is missing! Please assign it in the Inspector.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to the PauseManager's state change event.
        // When PauseManager.OnPauseStateChanged is invoked, our HandlePauseStateChange method will be called.
        PauseManager.OnPauseStateChanged += HandlePauseStateChange;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when this script is disabled or destroyed.
        // This is crucial to prevent memory leaks (ghost subscriptions) and ensure
        // that this object doesn't try to react to events after it's gone.
        PauseManager.OnPauseStateChanged -= HandlePauseStateChange;
    }

    // --- Event Handler ---

    /// <summary>
    /// This method is called when the PauseManager's OnPauseStateChanged event is invoked.
    /// It updates the visibility of the pause menu panel based on the current pause state.
    /// </summary>
    /// <param name="isPaused">True if the game is currently paused, false otherwise.</param>
    private void HandlePauseStateChange(bool isPaused)
    {
        if (pausePanel == null) return; // Prevent error if panel reference is missing.

        // Directly use the 'isPaused' parameter from the event to control panel visibility.
        pausePanel.SetActive(isPaused);
    }

    // --- Public UI Button Methods ---
    // These methods are public so they can be easily assigned to UI Button's OnClick() events in the Inspector.

    /// <summary>
    /// Called when the "Resume" button on the UI is pressed.
    /// Delegates the actual unpausing logic to the PauseManager.
    /// </summary>
    public void OnResumeButton()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.UnpauseGame();
        }
        else
        {
            Debug.LogError("PauseMenuUI: PauseManager instance not found!");
        }
    }

    /// <summary>
    /// Called when the "Quit" button on the UI is pressed.
    /// Unpauses the game first (good practice for clean exit), then quits the application.
    /// In a real game, this might load a main menu scene instead.
    /// </summary>
    public void OnQuitButton()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.UnpauseGame(); // Unpause before quitting or loading new scene
        }

        Debug.Log("Quitting Game...");

        // Application.Quit() only works in a built application.
        // In the Unity Editor, it will not stop play mode, but it's good practice.
#if UNITY_EDITOR
        // For testing in editor, you might manually stop play mode or add a breakpoint.
        // UnityEditor.EditorApplication.isPlaying = false; // Uncomment this line if you want to stop play mode in the editor.
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Example of a "Main Menu" button, which would unpause and load a different scene.
    /// </summary>
    public void OnMainMenuButton()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.UnpauseGame(); // Unpause before loading a new scene
        }

        Debug.Log("Loading Main Menu...");
        // IMPORTANT: Replace "MainMenuScene" with the actual name of your main menu scene
        // and ensure that scene is added to your Build Settings (File > Build Settings).
        // If the scene doesn't exist or isn't in Build Settings, this will throw an error.
        SceneManager.LoadScene("MainMenuScene"); 
    }
}
```

---

### **3. `PlayerMovement.cs` (Example Consumer)**

This script demonstrates how a typical game component (like a player controller) would integrate with the `PauseMenuSystem` by listening for pause/unpause events and disabling/enabling its own functionality.

```csharp
using UnityEngine;

/// <summary>
///     The PauseMenuSystem Design Pattern: Example Consumer (PlayerMovement)
///
///     This script demonstrates how a game object, like a player character,
///     would integrate with the PauseMenuSystem. It subscribes to the
///     PauseManager's events to enable/disable its own functionality.
///
///     Key Responsibilities:
///     1.  Perform player movement based on input.
///     2.  Stop/start movement in response to pause/unpause events from PauseManager.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Movement speed of the player.")]
    [SerializeField] private float moveSpeed = 5f;

    // Internal flag to control if the player can move.
    private bool canMove = true;

    // --- Unity Lifecycle Methods ---

    private void OnEnable()
    {
        // Subscribe to the PauseManager's state change event when this script is enabled.
        PauseManager.OnPauseStateChanged += OnPauseStateChangedHandler;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when this script is disabled or destroyed.
        // This is crucial for preventing memory leaks and ensuring the script doesn't
        // attempt to react to events after it's no longer active.
        PauseManager.OnPauseStateChanged -= OnPauseStateChangedHandler;
    }

    private void Update()
    {
        // Only allow movement if 'canMove' is true (i.e., game is not paused).
        if (canMove)
        {
            HandleMovementInput();
        }
    }

    // --- Event Handler ---

    /// <summary>
    /// Called by the PauseManager whenever the game's pause state changes.
    /// Updates the 'canMove' flag accordingly.
    /// </summary>
    /// <param name="isPaused">True if the game is currently paused, false otherwise.</param>
    private void OnPauseStateChangedHandler(bool isPaused)
    {
        // If isPaused is true, canMove becomes false. If isPaused is false, canMove becomes true.
        canMove = !isPaused;
        Debug.Log(gameObject.name + ": Movement " + (canMove ? "enabled" : "disabled") + " due to game state change.");

        // You might also want to stop any ongoing movement immediately when paused
        // For example, if using a Rigidbody: GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    // --- Movement Logic ---

    /// <summary>
    /// Handles player input for movement.
    /// </summary>
    private void HandleMovementInput()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        if (moveDirection != Vector3.zero)
        {
            // Move the player relative to the world space
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            
            // Optional: Rotate player to face movement direction
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), 0.15f);
        }
    }
}
```

---

### **How to Implement in Unity (Example Setup):**

1.  **Create C# Scripts:**
    *   Create three new C# scripts in your Unity project (e.g., in an `Assets/Scripts` folder) and name them `PauseManager`, `PauseMenuUI`, and `PlayerMovement`.
    *   Copy and paste the corresponding code into each script.

2.  **Setup the `PauseManager`:**
    *   Create an Empty GameObject in your scene and name it `_GameManager` (or any suitable name).
    *   Attach the `PauseManager.cs` script to this `_GameManager` GameObject.

3.  **Setup the UI Canvas and Pause Menu:**
    *   In the Hierarchy, right-click -> UI -> Canvas. This will create a Canvas and an EventSystem.
    *   Inside the `Canvas` GameObject, right-click -> UI -> Panel. Rename this panel to `PauseMenuPanel`.
    *   Attach the `PauseMenuUI.cs` script to the `PauseMenuPanel`.
    *   In the Inspector of `PauseMenuPanel` (with the `PauseMenuUI` script), drag `PauseMenuPanel` itself from the Hierarchy into the `Pause Panel` slot in the `PauseMenuUI` component.
    *   **Add UI Buttons:** Inside `PauseMenuPanel`, right-click -> UI -> Button. Create at least two buttons:
        *   Rename one to `ResumeButton`. Change its text to "Resume".
        *   Rename the other to `QuitButton`. Change its text to "Quit".
        *   (Optional) Create a `MainMenuButton` and change its text to "Main Menu".
    *   **Configure Button Actions:**
        *   Select `ResumeButton`. In its Button component, scroll down to the `OnClick()` list. Click the `+` icon.
        *   Drag the `PauseMenuPanel` GameObject from the Hierarchy into the `None (Object)` slot.
        *   From the `No Function` dropdown, select `PauseMenuUI -> OnResumeButton()`.
        *   Repeat similar steps for `QuitButton`, linking it to `PauseMenuUI -> OnQuitButton()`.
        *   (Optional) Link `MainMenuButton` to `PauseMenuUI -> OnMainMenuButton()`. *Remember to create a scene named "MainMenuScene" and add it to your Build Settings if you use this button.*

4.  **Setup the Player (Example Consumer):**
    *   Create a simple 3D Object (e.g., a `Cube`) in your scene. Rename it `Player`.
    *   Attach the `PlayerMovement.cs` script to the `Player` GameObject.
    *   Give the `Player` a `Rigidbody` component (Component -> Physics -> Rigidbody) so it interacts with physics if you have a floor/world. Uncheck "Use Gravity" if you don't want it falling.

5.  **Run the Game:**
    *   Start the game. You should be able to move the `Player` using WASD or arrow keys.
    *   Press the `Escape` key. The `PauseMenuPanel` should appear, `PlayerMovement` should stop, and `Time.timeScale` will be 0.
    *   Click "Resume". The panel should disappear, movement should resume, and `Time.timeScale` will be 1.
    *   Click "Quit". The game (or editor play mode if uncommented) should exit.

This complete example provides a clear, educational, and practical demonstration of the PauseMenuSystem design pattern, ready to be integrated into your Unity projects.