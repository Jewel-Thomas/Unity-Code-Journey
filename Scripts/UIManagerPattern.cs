// Unity Design Pattern Example: UIManagerPattern
// This script demonstrates the UIManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete and practical implementation of the **UIManagerPattern** in Unity using C#. This pattern centralizes the management of your game's user interface (UI) panels, making it easier to show, hide, and switch between different UI states.

---

## The UIManager Pattern in Unity

The UIManagerPattern is a design pattern used to create a single, centralized point of control for all UI-related operations in a game. It typically involves:

1.  **A Singleton Manager:** A single instance of a `UIManager` class that can be accessed globally.
2.  **Centralized Panel Management:** A collection (e.g., a `Dictionary`) to store references to various UI panels by a unique identifier (like an `enum` or `string`).
3.  **Public API:** Methods to show, hide, toggle, and query the state of UI panels.
4.  **Inspector Assignment:** Allowing designers to easily link UI GameObjects to their logical identifiers in the Unity Editor.

**Benefits:**

*   **Decoupling:** Game logic doesn't need to directly reference specific UI GameObjects. It only needs to know the type of panel to show/hide.
*   **Centralized Control:** All UI state changes go through one manager, simplifying complex UI flows and making debugging easier.
*   **Reusability:** UI panels can be easily swapped or modified without affecting the game logic that calls them.
*   **Consistency:** Helps ensure a consistent UI experience across the application.

---

## 1. `UIPanelType.cs` (Enum for Panel Identification)

This simple enum defines the different types of UI panels your game will have. Using an enum is highly recommended over magic strings for type safety and readability.

```csharp
// UIPanelType.cs
using UnityEngine; // Not strictly needed for an enum, but good practice for Unity-related scripts.

/// <summary>
/// Defines the different types of UI panels managed by the UIManager.
/// Using an enum provides type safety and readability over using magic strings.
/// </summary>
public enum UIPanelType
{
    /// <summary>
    /// Default or unassigned type. Generally not to be shown/hidden directly.
    /// </summary>
    None = 0,

    /// <summary>
    /// The main menu screen.
    /// </summary>
    MainMenu,

    /// <summary>
    /// The game settings screen.
    /// </summary>
    Settings,

    /// <summary>
    /// The in-game pause menu.
    /// </summary>
    PauseMenu,

    /// <summary>
    /// The screen displayed when the game is over.
    /// </summary>
    GameOver,

    /// <summary>
    /// Player's inventory screen.
    /// </summary>
    Inventory,

    // Add more UI panel types as your game grows (e.g., Shop, QuestLog, HUD, etc.)
}
```

---

## 2. `UIManager.cs` (The Core Manager Script)

This is the main script that implements the UIManagerPattern.

```csharp
// UIManager.cs
using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System; // For [Serializable]

/// <summary>
/// Serializable struct to link a <see cref="UIPanelType"/> enum with its corresponding Unity <see cref="GameObject"/>.
/// This allows designers to easily assign UI panels in the Unity Inspector.
/// </summary>
[Serializable]
public struct UIPanelEntry
{
    /// <summary>
    /// The logical type of the UI panel.
    /// </summary>
    public UIPanelType panelType;

    /// <summary>
    /// The actual Unity GameObject representing this UI panel (e.g., a Canvas or a child panel).
    /// </summary>
    public GameObject panelGameObject;
}

/// <summary>
/// The UIManager class, implementing the Singleton pattern.
/// It centralizes the management (activation and deactivation) of various UI panels
/// based on their <see cref="UIPanelType"/>.
/// This pattern promotes a single point of control for UI, making it easier to manage
/// complex UI flows, debug, and ensure consistent behavior across your application.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- Singleton Instance ---
    /// <summary>
    /// The static instance of the UIManager, accessible globally.
    /// </summary>
    public static UIManager Instance { get; private set; }

    // --- Inspector Assigned UI Panels ---
    /// <summary>
    /// A list of UI panel entries, allowing designers to assign <see cref="UIPanelType"/>
    /// to specific <see cref="GameObject"/>s directly in the Unity Inspector.
    /// </summary>
    [Header("UI Panel Assignments")]
    [SerializeField]
    private List<UIPanelEntry> uiPanelEntries = new List<UIPanelEntry>();

    // --- Internal Panel Storage ---
    /// <summary>
    /// An internal dictionary for quick O(1) average time lookup of UI panels by their type.
    /// This is populated from the `uiPanelEntries` list during initialization.
    /// </summary>
    private Dictionary<UIPanelType, GameObject> uiPanels = new Dictionary<UIPanelType, GameObject>();

    // --- Active Panel Tracking ---
    /// <summary>
    /// Tracks the <see cref="UIPanelType"/> of the currently active "main" panel.
    /// This is useful for logic that expects only one primary UI screen to be active at a time
    /// (e.g., MainMenu, Settings, PauseMenu), and helps in managing transitions between them.
    /// Does not track overlay popups unless explicitly configured to do so.
    /// </summary>
    private UIPanelType _currentActiveMainPanel = UIPanelType.None;

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where Singleton setup and initial panel configuration occurs.
    /// </summary>
    private void Awake()
    {
        // Singleton Enforcement:
        // Ensures that there is only one instance of UIManager in the scene at any given time.
        // If a duplicate is found, it logs a warning and destroys the new instance.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UIManager: Duplicate UIManager detected, destroying this one. " +
                             "Please ensure only one UIManager exists in the scene to prevent conflicts.");
            Destroy(gameObject); // Destroy the duplicate instance
            return;
        }
        Instance = this; // Set this instance as the singleton

        // Optional: Make the UIManager persistent across scene loads.
        // This is a common practice for managers that need to exist throughout the game's lifecycle.
        // If your UI (and UIManager) is scene-specific, you can remove this line.
        DontDestroyOnLoad(gameObject);

        // Initialize the internal dictionary from the Inspector-assigned list of UI panels.
        InitializeUIPanels();

        // Initially hide all managed UI panels.
        // This ensures a clean slate when the game starts or a new scene loads,
        // allowing specific panels to be explicitly shown as needed.
        HideAllPanels();
    }

    /// <summary>
    /// Populates the internal <see cref="uiPanels"/> dictionary with UI panels
    /// based on the entries defined in the Unity Inspector.
    /// Also performs basic validation and ensures panels are initially inactive.
    /// </summary>
    private void InitializeUIPanels()
    {
        uiPanels.Clear(); // Clear any existing entries, useful for editor play/stop cycles

        foreach (var entry in uiPanelEntries)
        {
            if (entry.panelGameObject == null)
            {
                Debug.LogWarning($"UIManager: Panel GameObject for type '{entry.panelType}' is not assigned. " +
                                 "Please ensure it's assigned in the Inspector for proper functionality.");
                continue; // Skip this entry if its GameObject reference is null
            }

            if (uiPanels.ContainsKey(entry.panelType))
            {
                Debug.LogWarning($"UIManager: Duplicate entry for panel type '{entry.panelType}' found in the Inspector list. " +
                                 "Only the first instance encountered will be used to prevent dictionary key conflicts.");
                continue; // Skip duplicate entries
            }

            uiPanels.Add(entry.panelType, entry.panelGameObject);
            // Ensure all panels start in an inactive state to be explicitly shown later.
            entry.panelGameObject.SetActive(false);
        }

        Debug.Log($"UIManager: Initialized {uiPanels.Count} UI panels from Inspector assignments.");
    }

    // --- Public API for Managing UI Panels ---
    // These methods provide the core functionality for other scripts to interact with the UI.

    /// <summary>
    /// Activates a specific UI panel.
    /// This method can optionally hide the currently active 'main' panel before showing the new one,
    /// which is particularly useful for menu screens (e.g., showing Settings should hide MainMenu).
    /// </summary>
    /// <param name="panelType">The <see cref="UIPanelType"/> of the panel to show.</param>
    /// <param name="hidePreviousMainPanel">
    /// If true, the <see cref="GetCurrentActiveMainPanel()"/> (if any and different from <paramref name="panelType"/>)
    /// will be hidden before the new panel is shown. Defaults to true.
    /// </param>
    /// <returns>True if the panel was successfully shown, false otherwise (e.g., panel not found, already active).</returns>
    public bool ShowPanel(UIPanelType panelType, bool hidePreviousMainPanel = true)
    {
        if (panelType == UIPanelType.None)
        {
            Debug.LogWarning("UIManager: Cannot show panel of type 'None'. This type is reserved and cannot be managed directly.");
            return false;
        }

        if (!uiPanels.TryGetValue(panelType, out GameObject panelGameObject))
        {
            Debug.LogError($"UIManager: Panel of type '{panelType}' not found in the dictionary. " +
                           "Please ensure it's assigned in the Inspector under UIManager's 'UI Panel Entries'.");
            return false;
        }

        // If a *different* main panel is currently active and we are configured to hide it
        if (hidePreviousMainPanel && _currentActiveMainPanel != UIPanelType.None && _currentActiveMainPanel != panelType)
        {
            HidePanel(_currentActiveMainPanel); // Hide the previously active main panel
        }

        // Activate the requested panel if it's not already active
        if (!panelGameObject.activeSelf)
        {
            panelGameObject.SetActive(true);
            _currentActiveMainPanel = panelType; // Update the reference to the currently active main panel
            Debug.Log($"UIManager: Successfully shown panel '{panelType}'.");
            return true;
        }
        else
        {
            Debug.Log($"UIManager: Panel '{panelType}' is already active.");
            return false;
        }
    }

    /// <summary>
    /// Deactivates (hides) a specific UI panel.
    /// </summary>
    /// <param name="panelType">The <see cref="UIPanelType"/> of the panel to hide.</param>
    /// <returns>True if the panel was successfully hidden, false otherwise (e.g., panel not found, already inactive).</returns>
    public bool HidePanel(UIPanelType panelType)
    {
        if (panelType == UIPanelType.None)
        {
            Debug.LogWarning("UIManager: Cannot hide panel of type 'None'. This type is reserved.");
            return false;
        }

        if (!uiPanels.TryGetValue(panelType, out GameObject panelGameObject))
        {
            Debug.LogError($"UIManager: Panel of type '{panelType}' not found in the dictionary. " +
                           "Please ensure it's assigned in the Inspector under UIManager's 'UI Panel Entries'.");
            return false;
        }

        // Deactivate the panel if it's currently active
        if (panelGameObject.activeSelf)
        {
            panelGameObject.SetActive(false);
            // If the panel being hidden was the _currentActiveMainPanel, clear the reference
            if (_currentActiveMainPanel == panelType)
            {
                _currentActiveMainPanel = UIPanelType.None;
            }
            Debug.Log($"UIManager: Successfully hidden panel '{panelType}'.");
            return true;
        }
        else
        {
            Debug.Log($"UIManager: Panel '{panelType}' is already inactive.");
            return false;
        }
    }

    /// <summary>
    /// Toggles the activation state of a specific UI panel.
    /// If the panel is currently active, it hides it. If it's inactive, it shows it.
    /// </summary>
    /// <param name="panelType">The <see cref="UIPanelType"/> of the panel to toggle.</param>
    /// <param name="hidePreviousMainPanelOnShow">
    /// If true and the panel is being shown, the <see cref="GetCurrentActiveMainPanel()"/>
    /// (if any and different from <paramref name="panelType"/>) will be hidden. Defaults to true.
    /// </param>
    /// <returns>True if the panel's state was successfully toggled, false otherwise.</returns>
    public bool TogglePanel(UIPanelType panelType, bool hidePreviousMainPanelOnShow = true)
    {
        if (panelType == UIPanelType.None)
        {
            Debug.LogWarning("UIManager: Cannot toggle panel of type 'None'. This type is reserved.");
            return false;
        }

        if (!uiPanels.TryGetValue(panelType, out GameObject panelGameObject))
        {
            Debug.LogError($"UIManager: Panel of type '{panelType}' not found in the dictionary. " +
                           "Please ensure it's assigned in the Inspector under UIManager's 'UI Panel Entries'.");
            return false;
        }

        if (panelGameObject.activeSelf)
        {
            return HidePanel(panelType); // Panel is active, so hide it
        }
        else
        {
            return ShowPanel(panelType, hidePreviousMainPanelOnShow); // Panel is inactive, so show it
        }
    }

    /// <summary>
    /// Deactivates all currently managed UI panels.
    /// This is useful for clearing the UI when transitioning between major game states
    /// (e.g., loading a new level, returning to the main menu).
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panelGameObject in uiPanels.Values)
        {
            if (panelGameObject != null && panelGameObject.activeSelf)
            {
                panelGameObject.SetActive(false);
            }
        }
        _currentActiveMainPanel = UIPanelType.None; // Reset the active panel tracker
        Debug.Log("UIManager: All managed UI panels have been hidden.");
    }

    /// <summary>
    /// Checks if a specific UI panel is currently active in the scene hierarchy.
    /// </summary>
    /// <param name="panelType">The <see cref="UIPanelType"/> of the panel to check.</param>
    /// <returns>True if the panel is found and currently active, false otherwise.</returns>
    public bool IsPanelActive(UIPanelType panelType)
    {
        if (uiPanels.TryGetValue(panelType, out GameObject panelGameObject))
        {
            return panelGameObject.activeSelf;
        }
        Debug.LogWarning($"UIManager: Could not check active state for panel type '{panelType}' because it was not found " +
                         "in the manager's dictionary.");
        return false; // Panel not found
    }

    /// <summary>
    /// Gets the <see cref="UIPanelType"/> of the currently active 'main' panel.
    /// This property tracks the last panel shown using <see cref="ShowPanel"/> where
    /// <c>hidePreviousMainPanel</c> was true, or if it's the only panel explicitly shown.
    /// It returns <see cref="UIPanelType.None"/> if no main panel is currently tracked as active.
    /// </summary>
    /// <returns>The <see cref="UIPanelType"/> of the current main active panel, or <see cref="UIPanelType.None"/>.</returns>
    public UIPanelType GetCurrentActiveMainPanel()
    {
        return _currentActiveMainPanel;
    }
}
```

---

## 3. `TestUIManager.cs` (Example Usage Script)

This script demonstrates how other parts of your game (e.g., a `GameManager`, button click handlers, or player input) would interact with the `UIManager`.

```csharp
// TestUIManager.cs
using UnityEngine;
using UnityEngine.UI; // Required for Button and Text components

/// <summary>
/// This script demonstrates how other parts of your game would interact with the UIManager.
/// It uses UI Buttons to trigger various panel show/hide/toggle actions through the UIManager.
/// </summary>
public class TestUIManager : MonoBehaviour
{
    // --- Inspector References for UI Buttons ---
    [Header("UI Buttons (Drag from Scene)")]
    [SerializeField] private Button showMainMenuButton;
    [SerializeField] private Button showSettingsButton;
    [SerializeField] private Button showPauseMenuButton;
    [SerializeField] private Button showInventoryButton;
    [SerializeField] private Button togglePauseMenuButton;
    [SerializeField] private Button hideAllButton;
    [SerializeField] private Button showGameOverButton;

    // --- Debugging and Status Display ---
    [Header("Debug Display")]
    [SerializeField] private Text statusText; // To display the current active main panel or messages

    /// <summary>
    /// Called when the script instance is being loaded.
    /// We use Start() here to ensure the UIManager has already completed its Awake() initialization.
    /// </summary>
    void Start()
    {
        // Add listeners to the buttons for UI interaction.
        // It's good practice to do this programmatically or via UnityEvents in the Inspector.
        // The null checks ensure that the application doesn't crash if a button isn't assigned.
        if (showMainMenuButton != null) showMainMenuButton.onClick.AddListener(ShowMainMenu);
        if (showSettingsButton != null) showSettingsButton.onClick.AddListener(ShowSettings);
        if (showPauseMenuButton != null) showPauseMenuButton.onClick.AddListener(ShowPauseMenu);
        if (showInventoryButton != null) showInventoryButton.onClick.AddListener(ShowInventory);
        if (togglePauseMenuButton != null) togglePauseMenuButton.onClick.AddListener(TogglePauseMenu);
        if (hideAllButton != null) hideAllButton.onClick.AddListener(HideAllPanels);
        if (showGameOverButton != null) showGameOverButton.onClick.AddListener(ShowGameOver);

        // Initial state: Show the main menu when this script starts (e.g., game launch).
        // The UIManager handles ensuring all other panels are hidden first.
        UIManager.Instance.ShowPanel(UIPanelType.MainMenu);
        UpdateStatusText(); // Update our debug display
    }

    // --- Public Methods to be called by UI Events (e.g., Button Clicks) or Game Logic ---

    public void ShowMainMenu()
    {
        // Calls the UIManager to show the MainMenu panel.
        // The 'true' argument ensures that any previously active *main* panel (like Settings) is hidden.
        UIManager.Instance.ShowPanel(UIPanelType.MainMenu, true);
        UpdateStatusText();
    }

    public void ShowSettings()
    {
        // Shows the Settings panel, hiding any previously active *main* panel.
        UIManager.Instance.ShowPanel(UIPanelType.Settings, true);
        UpdateStatusText();
    }

    public void ShowPauseMenu()
    {
        // Shows the PauseMenu. This will hide the current *main* panel (e.g., game HUD or MainMenu).
        // If you wanted the PauseMenu to overlay without hiding previous, you would set 'hidePreviousMainPanel' to false.
        UIManager.Instance.ShowPanel(UIPanelType.PauseMenu, true);
        UpdateStatusText();
    }

    public void ShowInventory()
    {
        // Shows the Inventory panel.
        // For this example, we assume Inventory is a full-screen panel that replaces the current view.
        // If it were an overlay (e.g., an expandable window over the game), you might set 'hidePreviousMainPanel' to false.
        UIManager.Instance.ShowPanel(UIPanelType.Inventory, true);
        UpdateStatusText();
    }

    public void TogglePauseMenu()
    {
        // Toggles the PauseMenu's visibility.
        // If it's open, it closes it. If closed, it opens it (hiding the previous main panel on open).
        UIManager.Instance.TogglePanel(UIPanelType.PauseMenu, true);
        UpdateStatusText();
    }

    public void HideAllPanels()
    {
        // Deactivates all panels currently managed by the UIManager.
        // Useful for a complete UI reset, e.g., before loading a new level.
        UIManager.Instance.HideAllPanels();
        UpdateStatusText();
    }

    public void ShowGameOver()
    {
        // Displays the Game Over screen, ensuring all other main UI is hidden.
        UIManager.Instance.ShowPanel(UIPanelType.GameOver, true);
        UpdateStatusText();
    }

    // --- Helper for Debugging / Status Display ---

    /// <summary>
    /// Updates the status text UI element to show which main panel is currently active.
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            UIPanelType currentPanel = UIManager.Instance.GetCurrentActiveMainPanel();
            if (currentPanel != UIPanelType.None)
            {
                statusText.text = $"Current Active Main Panel: <color=lime>{currentPanel}</color>";
            }
            else
            {
                statusText.text = "No main panel currently active.";
            }
        }
    }

    // Example of calling from game logic (not a button click)
    /// <summary>
    /// Simulates an event from game logic (e.g., player's health reaches zero).
    /// </summary>
    public void OnPlayerDeath()
    {
        Debug.Log("Player has died! Showing Game Over screen.");
        // ... any other game-specific logic for player death ...

        UIManager.Instance.ShowPanel(UIPanelType.GameOver, true);
        UpdateStatusText();
    }
}
```

---

## Unity Setup Instructions:

To get this example working in your Unity project:

1.  **Create C# Scripts:**
    *   Create a new C# script named `UIPanelType.cs` and copy the enum code into it.
    *   Create a new C# script named `UIManager.cs` and copy the main manager code into it.
    *   Create a new C# script named `TestUIManager.cs` and copy the example usage code into it.

2.  **Create a Canvas:**
    *   In your scene (e.g., SampleScene), right-click in the Hierarchy -> UI -> Canvas.
    *   Set its Render Mode to "Screen Space - Camera" and drag your Main Camera into the "Render Camera" slot for better UI scaling. (Or "Screen Space - Overlay" if you prefer).

3.  **Create UI Panels:**
    *   Under the Canvas, create several empty GameObjects to represent your UI panels. Name them according to your `UIPanelType` enum, e.g., `MainMenuPanel`, `SettingsPanel`, `PauseMenuPanel`, `InventoryPanel`, `GameOverPanel`.
    *   **Add Visuals:** To make them visible, add an `Image` component to each panel GameObject (e.g., a colored background) and optionally some `Text` to clearly label them (e.g., "Main Menu Screen", "Settings Screen").
    *   **Deactivate Initially:** Make sure all these UI panel GameObjects are **initially inactive** in the Inspector. The `UIManager` will activate them as needed.

4.  **Create the UIManager GameObject:**
    *   Create an empty GameObject in your scene (outside the Canvas, usually at the root of the Hierarchy) and name it `UIManager`.
    *   Drag the `UIManager.cs` script onto this `UIManager` GameObject.

5.  **Assign UI Panels in UIManager Inspector:**
    *   Select the `UIManager` GameObject in the Hierarchy.
    *   In the Inspector, find the "UI Panel Entries" list.
    *   Increase the "Size" property to match the number of UI panels you created (e.g., 5 for MainMenu, Settings, PauseMenu, Inventory, GameOver).
    *   For each entry:
        *   Select the corresponding `Panel Type` from the dropdown (e.g., `MainMenu`).
        *   Drag the corresponding UI panel GameObject (e.g., `MainMenuPanel`) from your Hierarchy into the `Panel Game Object` slot.
    *   Repeat for all your panels.

6.  **Create Test UI (Buttons and Status Text):**
    *   Under your Canvas, create another empty GameObject named `TestUI`.
    *   Under `TestUI`, create several `Button` UI elements (Right-click -> UI -> Button - TextMeshPro or Button). Label them clearly, e.g., "Show Main Menu", "Show Settings", "Show Pause Menu", "Toggle Pause Menu", "Show Inventory", "Hide All Panels", "Show Game Over".
    *   Also, add a `Text` UI element (Right-click -> UI -> Text - TextMeshPro) and name it `StatusText`. Position it somewhere visible.

7.  **Create the TestUIManager GameObject:**
    *   Create an empty GameObject in your scene (e.g., under the Canvas or at the root) named `TestUIManager`.
    *   Drag the `TestUIManager.cs` script onto this `TestUIManager` GameObject.

8.  **Assign Buttons and Status Text in TestUIManager Inspector:**
    *   Select the `TestUIManager` GameObject in the Hierarchy.
    *   In the Inspector, drag your newly created `Button` UI elements into their respective slots (`Show Main Menu Button`, `Show Settings Button`, etc.).
    *   Drag your `StatusText` UI element into the `Status Text` slot.

9.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   You should see the "Main Menu Screen" active and the status text indicating "Current Active Main Panel: MainMenu".
    *   Click the various buttons in your `TestUI` to observe how the `UIManager` switches between and manages the different UI panels.

This complete setup provides a robust and educational example of the UIManagerPattern in action!