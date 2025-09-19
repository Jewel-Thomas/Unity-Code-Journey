// Unity Design Pattern Example: MainMenuSystem
// This script demonstrates the MainMenuSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `MainMenuSystem` design pattern in Unity provides a centralized and robust way to manage the various states and panels of your game's main menu. Instead of individual UI buttons directly enabling/disabling panels, they communicate with a central `MainMenuSystem` which then handles the logic for showing the correct panel and hiding others.

This approach offers several advantages:
*   **Centralization:** All menu navigation logic resides in one place, making it easier to understand, debug, and modify.
*   **Decoupling:** UI elements (buttons) are decoupled from the specific menu panels. A "Show Options" button only needs to call `MainMenuSystem.Instance.ShowOptions()`, without knowing which panel to activate or which others to deactivate.
*   **Maintainability:** Adding new menu screens (e.g., "Settings", "Achievements") or modifying existing ones becomes much simpler, as the central system dictates the flow.
*   **Consistency:** Ensures that only one menu panel is active at a time, preventing overlapping UI or inconsistent states.
*   **Scalability:** Easily extendable for more complex menu interactions, animations, or sound effects tied to menu transitions.

Below is a complete, commented C# Unity script demonstrating the `MainMenuSystem` pattern.

---

```csharp
using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes
using System.Collections;          // Not strictly necessary for this minimal example, but often useful in Unity scripts.

/// <summary>
///     The MainMenuSystem design pattern provides a centralized, robust, and easily manageable
///     way to handle different states and panels within a game's main menu.
///     It acts as a single point of control for navigating between menu screens
///     (e.g., Main, Options, Credits, Quit Confirmation) and executing primary
///     game actions like starting a new game or quitting.
///
///     This pattern is beneficial because:
///     1.  **Centralization:** All menu logic is in one place, making it easy to understand and modify.
///     2.  **Decoupling:** UI buttons don't need to know about each other; they just tell the MainMenuSystem
///         what action to perform (e.g., "show options"). The MainMenuSystem handles the actual panel
///         switching.
///     3.  **Maintainability:** Adding or removing menu screens is simpler.
///     4.  **Error Prevention:** Enforces a consistent state, preventing multiple menu panels from being active simultaneously accidentally.
/// </summary>
/// <remarks>
///     **How to Use This Script in Unity:**
///
///     1.  **Create a New Scene:** Create a dedicated "MainMenu" scene in your Unity project (e.g., `Assets/Scenes/MainMenu`).
///     2.  **Create an Empty GameObject:** In your "MainMenu" scene, create an empty GameObject (e.g., named "MainMenuManager").
///     3.  **Attach Script:** Attach this `MainMenuSystem.cs` script to the "MainMenuManager" GameObject.
///     4.  **Setup UI Canvas:** Create a UI Canvas (Right-click in Hierarchy -> UI -> Canvas). Ensure its Render Mode is `Screen Space - Overlay` or `Screen Space - Camera`.
///     5.  **Create UI Panels:** Inside the Canvas, create several empty UI Panels (Right-click on Canvas -> UI -> Panel).
///         -   Rename them meaningfully (e.g., "MainMenuPanel", "OptionsPanel", "CreditsPanel", "QuitConfirmationPanel").
///         -   Design each panel with appropriate UI elements (Buttons, Text, Sliders, etc.). Ensure they are visually distinct.
///         -   **MainMenuPanel:** Add buttons for "New Game", "Options", "Credits", "Quit".
///         -   **OptionsPanel:** Add a "Back" button to return to the main menu.
///         -   **CreditsPanel:** Add a "Back" button.
///         -   **QuitConfirmationPanel:** Add "Yes (Quit)" and "No (Back to Main Menu)" buttons.
///         -   Initially, you can leave all these panels active to design them, but the script will ensure only one is active at runtime.
///     6.  **Assign Panels in Inspector:**
///         -   Select the "MainMenuManager" GameObject in your Hierarchy.
///         -   In its Inspector, drag and drop your created UI Panel GameObjects from the Hierarchy into the corresponding
///             `_mainMenuPanel`, `_optionsPanel`, `_creditsPanel`, and `_quitConfirmationPanel` slots in the `MainMenuSystem` component.
///     7.  **Set Gameplay Scene Name:**
///         -   In the Inspector of "MainMenuManager", set the `Gameplay Scene Name` field to the exact name
///             of your main game scene (e.g., "GameScene").
///         -   **IMPORTANT:** Make sure this "GameScene" (and "MainMenu" scene) is added to your Build Settings
///             (File -> Build Settings... -> Add Open Scenes).
///     8.  **Hook Up Buttons (using Unity's OnClick Events):**
///         -   For each UI Button you created:
///             -   Select the Button in the Hierarchy.
///             -   In the Inspector, find the "On Click ()" event list (at the bottom of the Button component).
///             -   Click the "+" button to add a new event.
///             -   Drag the "MainMenuManager" GameObject from the Hierarchy into the "None (Object)" slot of the new event.
///             -   From the "No Function" dropdown, navigate to `MainMenuSystem` and select the appropriate public method:
///                 -   **On "MainMenuPanel":**
///                     -   "New Game" Button: `MainMenuSystem.StartNewGame()`
///                     -   "Options" Button: `MainMenuSystem.ShowOptions()`
///                     -   "Credits" Button: `MainMenuSystem.ShowCredits()`
///                     -   "Quit" Button: `MainMenuSystem.ShowQuitConfirmation()`
///                 -   **On "OptionsPanel":**
///                     -   "Back" Button: `MainMenuSystem.ShowMainMenu()`
///                 -   **On "CreditsPanel":**
///                     -   "Back" Button: `MainMenuSystem.ShowMainMenu()`
///                 -   **On "QuitConfirmationPanel":**
///                     -   "Yes (Quit)" Button: `MainMenuSystem.QuitGame()`
///                     -   "No (Back)" Button: `MainMenuSystem.ShowMainMenu()`
///     9.  **Test:** Save your scene and run it to test the menu navigation and actions.
/// </remarks>
public class MainMenuSystem : MonoBehaviour
{
    // A static reference to the single instance of the MainMenuSystem.
    // This implements the Singleton pattern, allowing other scripts (like UI buttons)
    // to easily access this manager without needing a direct reference
    // (e.g., MainMenuSystem.Instance.ShowOptions()).
    public static MainMenuSystem Instance { get; private set; }

    [Header("UI Panel References")]
    [Tooltip("Drag the Main Menu UI GameObject here. This panel contains 'New Game', 'Options', 'Credits', 'Quit' buttons.")]
    [SerializeField] private GameObject _mainMenuPanel;
    [Tooltip("Drag the Options Menu UI GameObject here. This panel contains settings and a 'Back' button.")]
    [SerializeField] private GameObject _optionsPanel;
    [Tooltip("Drag the Credits Menu UI GameObject here. This panel contains credits information and a 'Back' button.")]
    [SerializeField] private GameObject _creditsPanel;
    [Tooltip("Drag the Quit Confirmation UI GameObject here. This panel typically asks 'Are you sure?' with 'Yes' and 'No' buttons.")]
    [SerializeField] private GameObject _quitConfirmationPanel;

    [Header("Scene Management")]
    [Tooltip("The name of the scene to load when 'Start New Game' is pressed. Ensure this scene is added to your Build Settings!")]
    [SerializeField] private string _gameplaySceneName = "GameScene";

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to implement the Singleton pattern and ensure only one instance of MainMenuSystem exists.
    /// </summary>
    private void Awake()
    {
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MainMenuSystem: Multiple instances found! Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            // Otherwise, set this as the singleton instance.
            Instance = this;
            // Optionally, if you wanted this system to persist across scenes (uncommon for a main menu manager,
            // as you typically load a new game scene which replaces the menu):
            // DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Used here to initialize the menu state, ensuring only the main menu is visible at start.
    /// </summary>
    private void Start()
    {
        // Ensure the main menu is active and all others are hidden when the scene starts.
        // This handles cases where panels might have been left active in the editor.
        ShowMainMenu();
    }

    /// <summary>
    /// Helper method that deactivates all registered UI panels.
    /// This is crucial for ensuring only one menu panel is active at a time,
    /// preventing UI overlaps and maintaining a clean menu state.
    /// </summary>
    private void HideAllPanels()
    {
        _mainMenuPanel?.SetActive(false); // The '?' (null-conditional operator) prevents a NullReferenceException if the panel isn't assigned.
        _optionsPanel?.SetActive(false);
        _creditsPanel?.SetActive(false);
        _quitConfirmationPanel?.SetActive(false);
    }

    /// <summary>
    /// Displays the main menu panel and hides all other panels.
    /// This method is typically called by "Back" buttons from sub-menus or at scene start.
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllPanels();
        // Check if the panel is assigned before trying to activate it, and log an error if not.
        if (_mainMenuPanel == null) { Debug.LogError("MainMenuSystem: Main Menu Panel is not assigned in the Inspector!", this); return; }
        _mainMenuPanel.SetActive(true);
        Debug.Log("MainMenuSystem: Displaying Main Menu.");
    }

    /// <summary>
    /// Displays the options panel and hides all other panels.
    /// This method is typically called by an "Options" button on the main menu.
    /// </summary>
    public void ShowOptions()
    {
        HideAllPanels();
        if (_optionsPanel == null) { Debug.LogError("MainMenuSystem: Options Panel is not assigned in the Inspector!", this); return; }
        _optionsPanel.SetActive(true);
        Debug.Log("MainMenuSystem: Displaying Options Menu.");
    }

    /// <summary>
    /// Displays the credits panel and hides all other panels.
    /// This method is typically called by a "Credits" button on the main menu.
    /// </summary>
    public void ShowCredits()
    {
        HideAllPanels();
        if (_creditsPanel == null) { Debug.LogError("MainMenuSystem: Credits Panel is not assigned in the Inspector!", this); return; }
        _creditsPanel.SetActive(true);
        Debug.Log("MainMenuSystem: Displaying Credits Menu.");
    }

    /// <summary>
    /// Displays the quit confirmation panel and hides all other panels.
    /// This method is typically called by a "Quit" button on the main menu,
    /// providing a user with a chance to confirm before exiting.
    /// </summary>
    public void ShowQuitConfirmation()
    {
        HideAllPanels();
        if (_quitConfirmationPanel == null) { Debug.LogError("MainMenuSystem: Quit Confirmation Panel is not assigned in the Inspector!", this); return; }
        _quitConfirmationPanel.SetActive(true);
        Debug.Log("MainMenuSystem: Displaying Quit Confirmation.");
    }

    /// <summary>
    /// Initiates a new game by loading the specified gameplay scene.
    /// This method is typically called by a "New Game" or "Start Game" button.
    /// The current MainMenu scene will be unloaded, and the new scene loaded.
    /// </summary>
    public void StartNewGame()
    {
        if (string.IsNullOrEmpty(_gameplaySceneName))
        {
            Debug.LogError("MainMenuSystem: Gameplay Scene Name is not set in the Inspector! Cannot load scene.", this);
            return;
        }

        Debug.Log($"MainMenuSystem: Starting new game. Loading scene: {_gameplaySceneName}");
        SceneManager.LoadScene(_gameplaySceneName);
    }

    /// <summary>
    /// Quits the application. This method handles both Unity Editor and standalone build quitting.
    /// This is typically called by a "Yes" button on a quit confirmation panel.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("MainMenuSystem: Quitting game...");

        // This ensures the application quits correctly whether in editor or a standalone build.
#if UNITY_EDITOR
        // If we are in the Unity Editor, stop playing.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If we are in a standalone build, quit the application.
        Application.Quit();
#endif
    }
}
```