// Unity Design Pattern Example: ModularUIFramework
// This script demonstrates the ModularUIFramework pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example provides a complete and practical implementation of the **Modular UI Framework** design pattern. It's designed to be educational, demonstrating how to create independent, reusable UI components that can be managed by a central orchestrator.

## Modular UI Framework Design Pattern Explained:

The Modular UI Framework promotes a decoupled approach to UI development in Unity, addressing common issues like tight coupling, difficult maintenance, and lack of reusability. It breaks down the UI into small, independent "modules" with well-defined responsibilities and a clear lifecycle, managed by a central "UI Manager."

**Core Components:**

1.  **`UIModuleBase` (The Abstract Module):**
    *   This is the foundation for all UI modules. It defines a common interface and essential lifecycle methods (initialize, show, hide, destroy) that every UI module must implement or override.
    *   It also provides common utilities like caching `RectTransform` and `CanvasGroup` for consistent control over visibility and interactivity.
    *   Its `ModuleId` is automatically set to the module's type name, ensuring a unique identifier.

2.  **Concrete UI Modules (Implementations):**
    *   These are the actual, specific UI components in your game (e.g., `PlayerInfoUIModule`, `InventoryUIModule`, `SettingsUIModule`).
    *   Each module is a `MonoBehaviour` (and thus a Prefab) that inherits from `UIModuleBase`.
    *   They manage their own internal UI elements (buttons, text fields, sliders) and their specific logic.
    *   They interact with other parts of the game (or other UI modules) primarily through the `UIManager` or via event systems (like `UnityEvent`s).

3.  **`UIManager` (The Orchestrator):**
    *   This is the central singleton responsible for managing all UI modules.
    *   It acts as a **Service Locator** for UI, allowing any part of the game to request a UI module by its type.
    *   It handles:
        *   **Registration:** Knowing which prefabs correspond to which UI module types.
        *   **Instantiation:** Creating UI module instances from prefabs when needed.
        *   **Parenting/Layering:** Placing modules under appropriate `RectTransform` parents (e.g., permanent, overlay, modal layers) within the main Canvas.
        *   **Lifecycle Management:** Calling `InitializeModule`, `Show`, `Hide`, `DestroyModule` on the modules.
        *   **Access:** Providing methods to retrieve active module instances.

4.  **`GameInitializer` (Entry Point Example):**
    *   A simple script to demonstrate how the game's startup logic would interact with the `UIManager` to display initial UI modules and subscribe to their events.

**Benefits of this Pattern:**

*   **Decoupling:** UI modules don't need direct references to each other or to the game's core logic. They interact via the `UIManager` or events.
*   **Reusability:** Each UI module is self-contained and can be easily reused across different parts of your game or even different projects.
*   **Maintainability:** Changes to one UI module are less likely to break others. Finding and fixing UI-related bugs becomes easier.
*   **Scalability:** Easily add new UI modules without significantly altering existing code.
*   **Team Collaboration:** Different team members can work on different UI modules simultaneously without conflicts.
*   **Clear Structure:** Provides a clear, organized way to manage complex UI systems.

---

## Complete C# Unity Code:

This script includes `UIManager`, `UIModuleBase`, three example concrete modules (`PlayerInfoUIModule`, `InventoryUIModule`, `SettingsUIModule`), and a `GameInitializer` to kick things off.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq; // For cleaner type checking in RegisterModulePrefabs
using UnityEngine.Events; // For module communication via events

// =======================================================================================
// PART 1: UIModuleBase - The Foundation for all UI Modules
// =======================================================================================

/// <summary>
/// The abstract base class for all UI modules in the Modular UI Framework.
/// This defines the essential lifecycle and properties of a UI element.
/// All concrete UI modules should inherit from this class.
/// </summary>
[RequireComponent(typeof(CanvasGroup))] // Ensures every UI module has a CanvasGroup for control
public abstract class UIModuleBase : MonoBehaviour
{
    [Tooltip("The unique identifier for this module. Automatically set to the type name.")]
    public string ModuleId { get; private set; }

    public RectTransform RectTransform { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    /// <summary>
    /// Indicates if the module is currently active (visible and interactable).
    /// </summary>
    public bool IsActive => CanvasGroup.alpha > 0 && CanvasGroup.interactable;

    protected virtual void Awake()
    {
        // Cache references for performance and convenience
        RectTransform = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();
        ModuleId = GetType().Name; // Set ModuleId to the concrete class name (e.g., "PlayerInfoUIModule")
    }

    /// <summary>
    /// Called once after the module is instantiated by the UIManager.
    /// Use this for initial setup that doesn't need to happen every time the module is shown.
    /// </summary>
    /// <param name="parentTransform">The RectTransform to parent this module under.</param>
    public virtual void InitializeModule(RectTransform parentTransform)
    {
        if (parentTransform != null)
        {
            RectTransform.SetParent(parentTransform, false); // false to maintain local position/scale
            RectTransform.anchorMin = Vector2.zero;          // Stretch to fill parent
            RectTransform.anchorMax = Vector2.one;
            RectTransform.offsetMin = Vector2.zero;
            RectTransform.offsetMax = Vector2.zero;
            RectTransform.localScale = Vector3.one;          // Ensure scale is 1
            RectTransform.localPosition = Vector3.zero;      // Ensure local position is 0
        }

        // Initially hide the module until explicitly shown
        Hide();
    }

    /// <summary>
    /// Makes the UI module visible and interactable.
    /// Override this method in concrete classes for specific show animations or logic.
    /// </summary>
    public virtual void Show()
    {
        gameObject.SetActive(true); // Ensure GameObject is active
        CanvasGroup.alpha = 1f;
        CanvasGroup.interactable = true;
        CanvasGroup.blocksRaycasts = true;
        Debug.Log($"UI Module '{ModuleId}' Shown.");
    }

    /// <summary>
    /// Makes the UI module invisible and non-interactable.
    /// Override this method in concrete classes for specific hide animations or logic.
    /// </summary>
    public virtual void Hide()
    {
        CanvasGroup.alpha = 0f;
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false); // Deactivate GameObject to save resources if not needed
        Debug.Log($"UI Module '{ModuleId}' Hidden.");
    }

    /// <summary>
    /// Called when the module is destroyed by the UIManager.
    /// Use this for any final cleanup of resources.
    /// </summary>
    public virtual void DestroyModule()
    {
        Debug.Log($"UI Module '{ModuleId}' Destroyed.");
        Destroy(gameObject); // Destroy the GameObject associated with this module
    }

    /// <summary>
    /// Optional method for modules that need per-frame updates.
    /// This would typically be called by the UIManager's Update loop if implemented.
    /// </summary>
    public virtual void UpdateModule()
    {
        // Default: Do nothing. Override in concrete modules if needed.
    }
}

// =======================================================================================
// PART 2: UIManager - The Orchestrator
// =======================================================================================

/// <summary>
/// The central manager for all UI modules in the application.
/// It handles instantiation, showing, hiding, and destruction of UI modules.
/// This class follows the Singleton pattern to ensure a single point of control.
/// </summary>
public class UIManager : MonoBehaviour
{
    // Singleton instance for easy global access
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas Parents")]
    [Tooltip("Parent for permanent UI elements (e.g., player info, minimap).")]
    [SerializeField] private RectTransform _permanentUIParent;
    [Tooltip("Parent for overlay UI elements (e.g., notifications, tooltips).")]
    [SerializeField] private RectTransform _overlayUIParent;
    [Tooltip("Parent for modal UI elements (e.g., settings menu, popups).")]
    [SerializeField] private RectTransform _modalUIParent;

    [Header("Module Prefabs")]
    [Tooltip("Drag all your UI module prefabs here to register them with the UIManager.")]
    [SerializeField] private List<GameObject> _modulePrefabs = new List<GameObject>();

    // Dictionaries to manage registered prefabs and active module instances
    // Maps module Type (e.g., typeof(PlayerInfoUIModule)) to its prefab.
    private readonly Dictionary<Type, GameObject> _registeredPrefabs = new Dictionary<Type, GameObject>();
    // Maps module Type to its currently active instance in the scene.
    private readonly Dictionary<Type, UIModuleBase> _activeModules = new Dictionary<Type, UIModuleBase>();

    private void Awake()
    {
        // Singleton enforcement logic
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UIManager: Duplicate instance detected, destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep UIManager persistent across scene loads

        // Validate that parent RectTransforms are assigned in the Inspector
        if (_permanentUIParent == null || _overlayUIParent == null || _modalUIParent == null)
        {
            Debug.LogError("UIManager: One or more UI parent RectTransforms are not assigned! Please assign them in the Inspector.", this);
            return;
        }

        // Register all prefabs specified in the Inspector
        RegisterAllModulePrefabs();
    }

    /// <summary>
    /// Registers all UI module prefabs provided in the Inspector list.
    /// This allows the UIManager to know which prefab to instantiate for each module type.
    /// </summary>
    private void RegisterAllModulePrefabs()
    {
        _registeredPrefabs.Clear(); // Clear any previous registrations (e.g., in editor resets)
        foreach (GameObject prefab in _modulePrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("UIManager: A null prefab was found in the _modulePrefabs list. Please remove it.", this);
                continue;
            }

            // Ensure the prefab has a UIModuleBase component
            UIModuleBase moduleBase = prefab.GetComponent<UIModuleBase>();
            if (moduleBase == null)
            {
                Debug.LogWarning($"UIManager: Prefab '{prefab.name}' does not have a UIModuleBase component. It will not be registered.", prefab);
                continue;
            }

            // Get the concrete type of the module (e.g., PlayerInfoUIModule, not UIModuleBase)
            Type moduleType = moduleBase.GetType();
            if (_registeredPrefabs.ContainsKey(moduleType))
            {
                Debug.LogWarning($"UIManager: Duplicate module type '{moduleType.Name}' found. Only the first instance will be registered.", prefab);
                continue;
            }

            _registeredPrefabs.Add(moduleType, prefab);
            Debug.Log($"UIManager: Registered UI module prefab: {moduleType.Name}", prefab);
        }
    }

    /// <summary>
    /// Shows a UI module of a specific type.
    /// If the module is not yet instantiated, it will be instantiated from its prefab and initialized.
    /// If it's already instantiated, it will simply be made visible.
    /// </summary>
    /// <typeparam name="T">The type of the UI module to show (must inherit from UIModuleBase).</typeparam>
    /// <param name="parentOverride">Optional: A specific RectTransform to parent the module under. If null, default parents (_permanent, _overlay, _modal) are used based on convention.</param>
    /// <returns>The instance of the shown UI module, or null if it could not be shown.</returns>
    public T ShowModule<T>(RectTransform parentOverride = null) where T : UIModuleBase
    {
        Type moduleType = typeof(T);
        UIModuleBase module = null;

        // 1. Check if the module is already active
        if (_activeModules.TryGetValue(moduleType, out module))
        {
            Debug.Log($"UIManager: Module '{moduleType.Name}' is already active. Showing it again.");
            module.Show(); // Just ensure it's visible
            return module as T;
        }

        // 2. Module not active, need to instantiate it
        if (!_registeredPrefabs.TryGetValue(moduleType, out GameObject prefab))
        {
            Debug.LogError($"UIManager: No prefab registered for module type '{moduleType.Name}'. Cannot show module.");
            return null;
        }

        // Instantiate the module from its prefab
        GameObject moduleGameObject = Instantiate(prefab);
        module = moduleGameObject.GetComponent<T>();
        if (module == null)
        {
            Debug.LogError($"UIManager: Instantiated prefab for '{moduleType.Name}' does not contain component '{moduleType.Name}'. Something is wrong with the prefab setup.");
            Destroy(moduleGameObject); // Clean up the instantiated GameObject
            return null;
        }

        // 3. Determine the parent transform for the module
        RectTransform actualParent = parentOverride;
        if (actualParent == null)
        {
            // Simple convention for default parenting based on module type.
            // This can be extended with attributes on modules, a custom enum, or a more complex layering system.
            if (module is SettingsUIModule) // Example: Settings is typically a modal overlay
            {
                actualParent = _modalUIParent;
            }
            else if (module is InventoryUIModule || module is PlayerInfoUIModule) // Example: Player HUD elements are permanent
            {
                actualParent = _permanentUIParent;
            }
            else // Default to overlay for other unknown types (e.g., temporary notifications)
            {
                actualParent = _overlayUIParent;
            }
        }

        // 4. Initialize the module, parent it, add to active modules, and show it
        module.InitializeModule(actualParent);
        _activeModules.Add(moduleType, module);
        module.Show();

        Debug.Log($"UIManager: Instantiated and showed module '{moduleType.Name}'. Parented under: {actualParent.name}");
        return module as T;
    }

    /// <summary>
    /// Hides a UI module of a specific type. The module remains instantiated in the scene,
    /// ready to be shown again quickly without re-instantiation.
    /// </summary>
    /// <typeparam name="T">The type of the UI module to hide.</typeparam>
    public void HideModule<T>() where T : UIModuleBase
    {
        Type moduleType = typeof(T);
        if (_activeModules.TryGetValue(moduleType, out UIModuleBase module))
        {
            module.Hide();
            Debug.Log($"UIManager: Hid module '{moduleType.Name}'.");
        }
        else
        {
            Debug.LogWarning($"UIManager: Attempted to hide module '{moduleType.Name}' but it was not found or not active.");
        }
    }

    /// <summary>
    /// Destroys a UI module of a specific type, removing it from the scene and memory.
    /// This should be used for modules that are no longer needed at all.
    /// </summary>
    /// <typeparam name="T">The type of the UI module to destroy.</typeparam>
    public void DestroyModule<T>() where T : UIModuleBase
    {
        Type moduleType = typeof(T);
        if (_activeModules.TryGetValue(moduleType, out UIModuleBase module))
        {
            module.DestroyModule(); // Call the module's cleanup method
            _activeModules.Remove(moduleType); // Remove from our tracking dictionary
            Debug.Log($"UIManager: Destroyed module '{moduleType.Name}'.");
        }
        else
        {
            Debug.LogWarning($"UIManager: Attempted to destroy module '{moduleType.Name}' but it was not found or not active.");
        }
    }

    /// <summary>
    /// Retrieves an active instance of a UI module by its type.
    /// This allows other scripts to interact with specific module instances directly (e.g., subscribe to events).
    /// </summary>
    /// <typeparam name="T">The type of the UI module to retrieve.</typeparam>
    /// <returns>The active instance of the UI module, or null if not found.</returns>
    public T GetModule<T>() where T : UIModuleBase
    {
        Type moduleType = typeof(T);
        if (_activeModules.TryGetValue(moduleType, out UIModuleBase module))
        {
            return module as T;
        }
        return null;
    }

    /// <summary>
    /// Updates all currently active UI modules that override UpdateModule().
    /// This provides an optional centralized update mechanism for modules requiring per-frame logic.
    /// </summary>
    private void Update()
    {
        // Iterate over a copy of the values to avoid issues if modules are added/removed during iteration
        // (though this is typically handled in FixedUpdate/LateUpdate for specific game loops)
        foreach (var module in _activeModules.Values.ToList()) // .ToList() creates a copy for safe iteration
        {
            if (module.IsActive) // Only update if the module is currently visible and interactable
            {
                module.UpdateModule();
            }
        }
    }
}

// =======================================================================================
// PART 3: Concrete UI Module Implementations
// =======================================================================================

// --- Module 1: Player Information (Permanent UI) ---
/// <summary>
/// Displays player name and health. Demonstrates a permanent UI element with internal state
/// and communication back to game logic via UnityEvents.
/// </summary>
public class PlayerInfoUIModule : UIModuleBase
{
    [Header("Player Info UI Elements")]
    [SerializeField] private Text _playerNameText;
    [SerializeField] private Text _healthText;
    [SerializeField] private Button _takeDamageButton;
    [SerializeField] private Button _healButton;

    // Events for external listeners (e.g., game logic) to react to player actions from UI
    public UnityEvent OnPlayerDamaged = new UnityEvent();
    public UnityEvent OnPlayerHealed = new UnityEvent();

    private string _playerName = "Hero";
    private int _health = 100;
    private const int MAX_HEALTH = 100;

    protected override void Awake()
    {
        base.Awake(); // Call base Awake to initialize ModuleId, RectTransform, CanvasGroup

        // Add listeners to buttons
        if (_takeDamageButton != null)
        {
            _takeDamageButton.onClick.AddListener(TakeDamage);
        }
        if (_healButton != null)
        {
            _healButton.onClick.AddListener(Heal);
        }
    }

    /// <summary>
    /// Initializes player info and updates UI elements.
    /// </summary>
    public override void InitializeModule(RectTransform parentTransform)
    {
        base.InitializeModule(parentTransform); // Call base initialization
        UpdateUI(); // Set initial UI values
    }

    private void UpdateUI()
    {
        if (_playerNameText != null) _playerNameText.text = $"Player: {_playerName}";
        if (_healthText != null) _healthText.text = $"Health: {_health}/{MAX_HEALTH}";
    }

    private void TakeDamage()
    {
        if (_health > 0)
        {
            _health -= 10;
            if (_health < 0) _health = 0;
            UpdateUI();
            Debug.Log($"Player {_playerName} took damage. Current Health: {_health}");
            OnPlayerDamaged.Invoke(); // Notify listeners that damage occurred
        }
    }

    private void Heal()
    {
        if (_health < MAX_HEALTH)
        {
            _health += 10;
            if (_health > MAX_HEALTH) _health = MAX_HEALTH;
            UpdateUI();
            Debug.Log($"Player {_playerName} healed. Current Health: {_health}");
            OnPlayerHealed.Invoke(); // Notify listeners that healing occurred
        }
    }

    protected virtual void OnDestroy()
    {
        // Clean up button listeners to prevent memory leaks if the module is destroyed
        if (_takeDamageButton != null)
        {
            _takeDamageButton.onClick.RemoveListener(TakeDamage);
        }
        if (_healButton != null)
        {
            _healButton.onClick.RemoveListener(Heal);
        }
    }
}

// --- Module 2: Inventory (Permanent/Toggleable UI) ---
/// <summary>
/// Represents an inventory panel. Demonstrates showing another module (Settings) on button click.
/// </summary>
public class InventoryUIModule : UIModuleBase
{
    [Header("Inventory UI Elements")]
    [SerializeField] private Button _openSettingsButton;
    [SerializeField] private Text _inventoryStatusText;

    private int _itemCount = 5;

    protected override void Awake()
    {
        base.Awake();
        if (_openSettingsButton != null)
        {
            _openSettingsButton.onClick.AddListener(OpenSettings);
        }
    }

    public override void InitializeModule(RectTransform parentTransform)
    {
        base.InitializeModule(parentTransform);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_inventoryStatusText != null) _inventoryStatusText.text = $"Inventory: {_itemCount} items";
    }

    private void OpenSettings()
    {
        Debug.Log("Inventory wants to open Settings.");
        // Example of one UI module showing another UI module through the UIManager.
        // This is a common pattern for opening related pop-ups or menus.
        UIManager.Instance.ShowModule<SettingsUIModule>();
    }

    protected virtual void OnDestroy()
    {
        if (_openSettingsButton != null)
        {
            _openSettingsButton.onClick.RemoveListener(OpenSettings);
        }
    }
}

// --- Module 3: Settings (Modal UI) ---
/// <summary>
/// A modal settings panel. Demonstrates closing itself and interaction with game state (Time.timeScale).
/// </summary>
public class SettingsUIModule : UIModuleBase
{
    [Header("Settings UI Elements")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Text _volumeValueText;

    protected override void Awake()
    {
        base.Awake();
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(CloseSettings);
        }
        if (_volumeSlider != null)
        {
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    public override void InitializeModule(RectTransform parentTransform)
    {
        base.InitializeModule(parentTransform);
        // Load initial settings, e.g., from PlayerPrefs or a GameManager
        float currentVolume = PlayerPrefs.GetFloat("GameVolume", 0.75f);
        if (_volumeSlider != null) _volumeSlider.value = currentVolume;
        UpdateVolumeText(currentVolume);
    }

    public override void Show()
    {
        base.Show();
        // Additional logic for a modal show, e.g., pause the game
        Debug.Log("Game Paused for Settings.");
        Time.timeScale = 0f; // Example: pause game when a modal menu is open
    }

    public override void Hide()
    {
        base.Hide();
        // Additional logic for a modal hide, e.g., resume the game
        Debug.Log("Game Resumed from Settings.");
        Time.timeScale = 1f; // Example: resume game when settings are closed
    }

    private void CloseSettings()
    {
        Debug.Log("Closing Settings.");
        // Example of a UI module hiding itself through the UIManager.
        UIManager.Instance.HideModule<SettingsUIModule>();
    }

    private void OnVolumeChanged(float value)
    {
        UpdateVolumeText(value);
        PlayerPrefs.SetFloat("GameVolume", value); // Save setting
        Debug.Log($"Volume set to: {value:F2}");
        // In a real game, you'd update AudioListener.volume or an AudioMixer parameter here
    }

    private void UpdateVolumeText(float value)
    {
        if (_volumeValueText != null) _volumeValueText.text = $"Volume: {Mathf.RoundToInt(value * 100)}%";
    }

    protected virtual void OnDestroy()
    {
        // Clean up button and slider listeners
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(CloseSettings);
        }
        if (_volumeSlider != null)
        {
            _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
}


// =======================================================================================
// PART 4: Game Initializer (Entry Point)
// =======================================================================================

/// <summary>
/// A simple script to initialize the game and demonstrate how to use the UIManager.
/// This would typically be on a GameManager or a Bootstrap scene in a real project.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // Ensure UIManager exists and is initialized. If it's persistent and already in scene,
        // UIManager's Awake will have set Instance. If not, this check ensures it's available.
        if (UIManager.Instance == null)
        {
            Debug.LogError("GameInitializer: UIManager.Instance is null. Make sure UIManager GameObject is in the scene and its script is enabled.");
            return;
        }

        // --- Demonstrating showing permanent UI modules at game start ---
        Debug.Log("GameInitializer: Showing initial permanent UI modules...");
        PlayerInfoUIModule playerInfo = UIManager.Instance.ShowModule<PlayerInfoUIModule>();
        InventoryUIModule inventory = UIManager.Instance.ShowModule<InventoryUIModule>();

        // --- Demonstrating module communication (optional but good practice) ---
        // Subscribe to events from the PlayerInfoUIModule.
        // This shows how game logic can react to actions originating from UI modules
        // without the UI module knowing anything about the game logic.
        if (playerInfo != null)
        {
            playerInfo.OnPlayerDamaged.AddListener(() => Debug.Log("GameInitializer: Received PlayerDamaged event from UIModule! (Game logic reacting)"));
            playerInfo.OnPlayerHealed.AddListener(() => Debug.Log("GameInitializer: Received PlayerHealed event from UIModule! (Game logic reacting)"));
        }

        // Example of showing a modal UI module immediately for testing:
        // UIManager.Instance.ShowModule<SettingsUIModule>();
    }

    private void OnDestroy()
    {
        // It's good practice to clean up event subscriptions to prevent potential null reference exceptions
        // if the GameInitializer is destroyed before the modules it subscribed to.
        if (UIManager.Instance != null)
        {
            PlayerInfoUIModule playerInfo = UIManager.Instance.GetModule<PlayerInfoUIModule>();
            if (playerInfo != null)
            {
                playerInfo.OnPlayerDamaged.RemoveAllListeners();
                playerInfo.OnPlayerHealed.RemoveAllListeners();
            }
        }
    }
}

```

---

## HOW TO SET UP THIS EXAMPLE IN UNITY:

Follow these steps to get the Modular UI Framework running in your Unity project:

1.  **Create a New Unity Project** (or open an existing one).

2.  **Create a New Scene** (e.g., "ModularUIScene").

3.  **Create a Main Canvas:**
    *   Right-click in the Hierarchy window -> UI -> Canvas.
    *   Rename it to "MainCanvas".
    *   In the Inspector for "MainCanvas":
        *   Set **Render Mode** to "Screen Space - Camera".
        *   Drag your **Main Camera** (from the Hierarchy) into the "Render Camera" slot.
        *   Change **UI Scale Mode** to "Scale With Screen Size".
        *   Set **Reference Resolution** to (1920, 1080) (common for full HD).
        *   Set **Screen Match Mode** to "Match Width Or Height" (0.5).

4.  **Create UI Parent GameObjects under MainCanvas:**
    *   These will act as "layers" for your UI modules.
    *   Right-click on "MainCanvas" in the Hierarchy -> Create Empty.
    *   Rename it **"PermanentUIParent"**.
    *   In its Inspector, ensure its Rect Transform fills the entire canvas:
        *   Set **Anchor Presets** to the bottom-right option (stretch, stretch).
        *   Ensure **Pos X, Y, Z** are 0, and **Width, Height** are 0 (this means `offsetMin` and `offsetMax` should be `(0,0)` if `anchorMin` is `(0,0)` and `anchorMax` is `(1,1)`).
    *   Repeat this process for two more empty GameObjects: **"OverlayUIParent"** and **"ModalUIParent"**.
    *   **Order them in the Hierarchy:** PermanentUIParent (bottom), OverlayUIParent (middle), ModalUIParent (top). This ensures correct visual layering.

5.  **Create the UIManager GameObject:**
    *   Right-click in the Hierarchy -> Create Empty.
    *   Rename it **"UIManager"**.
    *   **Attach the `UIManager.cs` script** to this GameObject.
    *   In the UIManager's Inspector:
        *   **Drag the "PermanentUIParent", "OverlayUIParent", and "ModalUIParent" GameObjects** (from your Hierarchy) into their respective slots under "UI Canvas Parents".

6.  **Create UI Module Prefabs:**
    *   For each concrete UI Module (`PlayerInfoUIModule`, `InventoryUIModule`, `SettingsUIModule`):
        *   **Create a new Empty GameObject** in the Hierarchy (e.g., "PlayerInfoUI").
        *   Add the corresponding script to it (e.g., `PlayerInfoUIModule.cs`). This will also automatically add a `CanvasGroup` component.
        *   **Add child UI elements** (Text, Buttons, Sliders) as needed. For example:
            *   **PlayerInfoUI:** Add two UI Texts (for player name and health) and two UI Buttons (for Take Damage and Heal).
            *   **InventoryUI:** Add one UI Button (for Open Settings) and one UI Text (for inventory status).
            *   **SettingsUI:** Add one UI Button (for Close), one UI Slider (for Volume), and one UI Text (for volume value).
        *   **Crucially: Drag and drop these child UI elements into the `[SerializeField]` slots** of the module's script component in the Inspector.
        *   **Adjust the `RectTransform` of the root module GameObject** (e.g., "PlayerInfoUI") to position and size it appropriately for its role within the screen (e.g., PlayerInfoUI to top-left, InventoryUI to bottom-right, SettingsUI to center-screen, perhaps covering more area).
        *   **Create a Prefab:** Drag the entire GameObject (e.g., "PlayerInfoUI") from the Hierarchy into your Project window (e.g., in a new "Assets/Prefabs/UI" folder).
        *   **Delete the GameObject from the Hierarchy.** The UIManager will instantiate it when needed.
        *   **Repeat this process for InventoryUI and SettingsUI.**

7.  **Register Prefabs with UIManager:**
    *   Select the "UIManager" GameObject in the Hierarchy.
    *   In the Inspector, find the "_modulePrefabs" list under "Module Prefabs".
    *   Increase its size if needed, and then **drag your created UI module Prefabs** (PlayerInfoUI, InventoryUI, SettingsUI) from your Project window into this list.

8.  **Create the GameInitializer GameObject:**
    *   Right-click in the Hierarchy -> Create Empty.
    *   Rename it **"GameInitializer"**.
    *   **Attach the `GameInitializer.cs` script** to it.

9.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   You should see the "Player Info" (top-left) and "Inventory" (bottom-right) modules appear on the screen.
    *   Click "Take Damage" or "Heal" on the Player Info module to see health change and messages in the Console.
    *   Click "Open Settings" on the Inventory module to open the "Settings" modal.
    *   Adjust the volume slider in Settings, then click "Close" to hide it.
    *   Observe the Debug.Log messages in the Console for module lifecycle events and communication.

This setup provides a fully functional demonstration of the Modular UI Framework, ready for further expansion in your Unity projects.