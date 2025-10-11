// Unity Design Pattern Example: NavigationLinkSystem
// This script demonstrates the NavigationLinkSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'NavigationLinkSystem' design pattern provides a structured way to manage transitions between different states, screens, or areas within an application. It emphasizes explicit "links" that define not just a destination, but also potential actions or data associated with that specific transition path. This makes navigation more robust, manageable, and easier to extend.

In Unity, this pattern is particularly useful for:
*   **UI Navigation**: Switching between main menu, options, credits, shop screens, etc.
*   **Game State Management**: Moving between `Loading -> MainMenu -> Gameplay -> Pause -> GameOver`.
*   **Level/Area Transitions**: When moving from one room or zone to another, potentially passing player data or triggering specific events.

This example focuses on **UI Navigation** as a common and clear demonstration.

---

### **NavigationLinkSystem Pattern Components:**

1.  **`UINavigablePanel`**:
    *   Represents a distinct "point" or "state" in the navigation system (e.g., a UI screen).
    *   Has a unique ID.
    *   Manages its own visibility (showing/hiding its associated GameObject).
    *   Registers itself with the `NavigationLinkSystem` on start.

2.  **`NavigationLink`**:
    *   A serializable data structure defining a specific transition *from* one `UINavigablePanel` *to* another.
    *   Crucially, it includes a `UnityEvent` (`OnLinkActivated`) that can be triggered when this specific link is traversed. This allows for custom logic (e.g., saving game state, playing a sound, setting up data) that is unique to *that particular transition path*, not just the destination panel.

3.  **`NavigationLinkSystem`**:
    *   The central manager (a Singleton) that orchestrates all navigation.
    *   Maintains a registry of all `UINavigablePanel`s.
    *   Holds a collection of `NavigationLink` objects, mapping specific transitions.
    *   Provides methods to `NavigateTo` a panel (direct transition) or `NavigateViaLink` (using a defined link, triggering its `OnLinkActivated` event).
    *   Handles the showing/hiding of panels and passes optional payload data.

---

### **C# Unity Implementation:**

You'll need three C# scripts:
1.  `UINavigablePanel.cs`
2.  `NavigationLink.cs`
3.  `NavigationLinkSystem.cs`

#### 1. `UINavigablePanel.cs`

This script should be attached to the root GameObject of each UI screen you want to manage.

```csharp
using UnityEngine;
using System.Collections.Generic; // Used for optional payload

/// <summary>
/// Represents a single navigable UI panel or game state within the NavigationLinkSystem.
/// Each panel has a unique ID and manages its own visibility.
/// </summary>
public class UINavigablePanel : MonoBehaviour
{
    [Tooltip("Unique identifier for this navigation panel.")]
    [SerializeField] private string _panelID;

    [Tooltip("The root GameObject of this panel that will be activated/deactivated.")]
    [SerializeField] private GameObject _panelRoot;

    public string PanelID => _panelID;

    private void Awake()
    {
        // Ensure panel root is assigned
        if (_panelRoot == null)
        {
            _panelRoot = gameObject; // Default to this GameObject if not explicitly set
        }

        // Initially hide the panel
        _panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // Register this panel with the NavigationLinkSystem when it becomes active in the scene.
        // This ensures the system knows about all available panels.
        if (NavigationLinkSystem.Instance != null)
        {
            NavigationLinkSystem.Instance.RegisterPanel(this);
        }
    }

    private void OnDisable()
    {
        // Unregister this panel when it's disabled or destroyed.
        // Important for cleaning up references and preventing null reference exceptions.
        if (NavigationLinkSystem.Instance != null)
        {
            NavigationLinkSystem.Instance.UnregisterPanel(this);
        }
    }

    /// <summary>
    /// Activates the panel's root GameObject and optionally processes incoming data.
    /// </summary>
    /// <param name="payload">Optional data to be passed to the panel upon activation.</param>
    public virtual void ShowPanel(object payload = null)
    {
        _panelRoot.SetActive(true);
        Debug.Log($"<color=cyan>[Navigation]</color> Panel '{PanelID}' shown.");

        // Example: Process a specific payload type
        if (payload is Dictionary<string, object> data)
        {
            Debug.Log($"<color=cyan>[Navigation]</color> Panel '{PanelID}' received payload: {string.Join(", ", new List<string>(data.Keys))}");
            // You can process specific keys here, e.g.:
            // if (data.TryGetValue("playerScore", out object score))
            // {
            //     Debug.Log($"Player Score: {score}");
            // }
        }
        else if (payload != null)
        {
             Debug.Log($"<color=cyan>[Navigation]</color> Panel '{PanelID}' received generic payload: {payload.GetType().Name}");
        }
    }

    /// <summary>
    /// Deactivates the panel's root GameObject.
    /// </summary>
    public virtual void HidePanel()
    {
        _panelRoot.SetActive(false);
        Debug.Log($"<color=cyan>[Navigation]</color> Panel '{PanelID}' hidden.");
    }
}
```

#### 2. `NavigationLink.cs`

This is a simple serializable class defining a link. It's not a `MonoBehaviour`.

```csharp
using UnityEngine;
using UnityEngine.Events;
using System; // For Serializable

/// <summary>
/// Represents a specific navigation link between two UINavigablePanels.
/// Contains a UnityEvent that can be triggered when this specific link is activated.
/// </summary>
[Serializable]
public class NavigationLink
{
    [Tooltip("The ID of the source panel from which this link originates.")]
    public string SourcePanelID;

    [Tooltip("The ID of the target panel to which this link navigates.")]
    public string TargetPanelID;

    [Tooltip("An event that is invoked when this specific navigation link is traversed.")]
    public UnityEvent OnLinkActivated;
}
```

#### 3. `NavigationLinkSystem.cs`

This is the central manager script, implemented as a Singleton.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .FirstOrDefault()

/// <summary>
/// The central Navigation Link System. It manages the registration of navigable panels,
/// defines explicit navigation links, and handles transitions between panels.
/// Implemented as a Singleton for easy access throughout the application.
/// </summary>
public class NavigationLinkSystem : MonoBehaviour
{
    // Singleton Instance
    public static NavigationLinkSystem Instance { get; private set; }

    [Header("Navigation Setup")]
    [Tooltip("The ID of the panel to show initially when the system starts.")]
    [SerializeField] private string _initialPanelID = "MainMenu";

    [Tooltip("A list of predefined navigation links. Each link can trigger specific events.")]
    [SerializeField] private List<NavigationLink> _navigationLinks = new List<NavigationLink>();

    // Internal dictionaries for efficient lookup of panels and links
    private Dictionary<string, UINavigablePanel> _registeredPanels = new Dictionary<string, UINavigablePanel>();
    private Dictionary<(string source, string target), NavigationLink> _linksMap = new Dictionary<(string, string), NavigationLink>();

    private UINavigablePanel _currentActivePanel; // Tracks the currently visible panel

    public UINavigablePanel CurrentActivePanel => _currentActivePanel;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple NavigationLinkSystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads

        // Populate the links map for quick lookups
        foreach (var link in _navigationLinks)
        {
            var key = (link.SourcePanelID, link.TargetPanelID);
            if (_linksMap.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate navigation link found from '{link.SourcePanelID}' to '{link.TargetPanelID}'. Only the first one will be used.");
            }
            else
            {
                _linksMap.Add(key, link);
            }
        }
    }

    private void Start()
    {
        // Navigate to the initial panel defined in the inspector
        if (!string.IsNullOrEmpty(_initialPanelID))
        {
            NavigateTo(_initialPanelID);
        }
        else
        {
            Debug.LogWarning("No initial panel ID set for NavigationLinkSystem. No panel will be shown on start.");
        }
    }

    /// <summary>
    /// Registers a UINavigablePanel with the system.
    /// Panels should call this themselves in their OnEnable/Awake.
    /// </summary>
    /// <param name="panel">The UINavigablePanel to register.</param>
    public void RegisterPanel(UINavigablePanel panel)
    {
        if (string.IsNullOrEmpty(panel.PanelID))
        {
            Debug.LogError($"Panel '{panel.name}' has no PanelID set. Cannot register.");
            return;
        }

        if (_registeredPanels.ContainsKey(panel.PanelID))
        {
            Debug.LogWarning($"A panel with ID '{panel.PanelID}' is already registered. Overwriting with new instance: {panel.name}.");
            _registeredPanels[panel.PanelID].HidePanel(); // Hide the old instance if it was active
        }
        _registeredPanels[panel.PanelID] = panel;
        Debug.Log($"<color=lime>[Navigation]</color> Registered panel: {panel.PanelID}");

        // If the newly registered panel is the current active one, ensure it's shown.
        // This handles cases where panels might be re-enabled/re-loaded.
        if (_currentActivePanel == null && panel.PanelID == _initialPanelID)
        {
            _currentActivePanel = panel;
            _currentActivePanel.ShowPanel();
        }
        else if (_currentActivePanel != null && _currentActivePanel.PanelID == panel.PanelID)
        {
             _currentActivePanel = panel; // Update reference in case of scene unload/load
             _currentActivePanel.ShowPanel();
        }
    }

    /// <summary>
    /// Unregisters a UINavigablePanel from the system.
    /// Panels should call this themselves in their OnDisable/OnDestroy.
    /// </summary>
    /// <param name="panel">The UINavigablePanel to unregister.</param>
    public void UnregisterPanel(UINavigablePanel panel)
    {
        if (_registeredPanels.ContainsKey(panel.PanelID))
        {
            _registeredPanels.Remove(panel.PanelID);
            Debug.Log($"<color=orange>[Navigation]</color> Unregistered panel: {panel.PanelID}");
        }
    }

    /// <summary>
    /// Navigates directly to a target panel by its ID.
    /// This method does not explicitly use a pre-defined NavigationLink,
    /// so no OnLinkActivated event will be triggered.
    /// </summary>
    /// <param name="targetPanelID">The ID of the panel to navigate to.</param>
    /// <param name="payload">Optional data to pass to the target panel.</param>
    public void NavigateTo(string targetPanelID, object payload = null)
    {
        Debug.Log($"<color=yellow>[Navigation Request]</color> Direct navigation to: {targetPanelID}");

        if (!_registeredPanels.TryGetValue(targetPanelID, out UINavigablePanel targetPanel))
        {
            Debug.LogError($"Cannot navigate to '{targetPanelID}': Panel not found or not registered.");
            return;
        }

        // If we are already on the target panel, do nothing (or re-show with new payload)
        if (_currentActivePanel != null && _currentActivePanel.PanelID == targetPanelID)
        {
            Debug.LogWarning($"Already on panel '{targetPanelID}'. Re-showing with new payload (if any).");
            _currentActivePanel.ShowPanel(payload);
            return;
        }

        // Hide the current panel if one is active
        if (_currentActivePanel != null)
        {
            _currentActivePanel.HidePanel();
        }

        // Show the new target panel
        _currentActivePanel = targetPanel;
        _currentActivePanel.ShowPanel(payload);
    }

    /// <summary>
    /// Navigates from a source panel to a target panel using a pre-defined NavigationLink.
    /// If a matching link is found, its OnLinkActivated event will be invoked before
    /// the panel transition occurs.
    /// </summary>
    /// <param name="sourcePanelID">The ID of the current (source) panel.</param>
    /// <param name="targetPanelID">The ID of the panel to navigate to.</param>
    /// <param name="payload">Optional data to pass to the target panel.</param>
    public void NavigateViaLink(string sourcePanelID, string targetPanelID, object payload = null)
    {
        Debug.Log($"<color=yellow>[Navigation Request]</color> Navigating via link from '{sourcePanelID}' to '{targetPanelID}'");

        if (!_registeredPanels.TryGetValue(targetPanelID, out UINavigablePanel targetPanel))
        {
            Debug.LogError($"Cannot navigate to '{targetPanelID}': Target panel not found or not registered.");
            return;
        }

        if (!_registeredPanels.ContainsKey(sourcePanelID))
        {
            Debug.LogWarning($"Source panel '{sourcePanelID}' not registered. Proceeding to target without source context.");
            // We can still proceed if the source isn't registered, but the link won't be found based on it.
        }

        NavigationLink activeLink = null;
        if (_linksMap.TryGetValue((sourcePanelID, targetPanelID), out activeLink))
        {
            Debug.Log($"<color=green>[Navigation Link]</color> Activating link from '{sourcePanelID}' to '{targetPanelID}'.");
            activeLink.OnLinkActivated.Invoke(); // Trigger the event defined on the specific link
        }
        else
        {
            Debug.LogWarning($"No explicit link found from '{sourcePanelID}' to '{targetPanelID}'. Performing direct navigation.");
        }

        // Perform the actual panel showing/hiding, similar to NavigateTo
        if (_currentActivePanel != null && _currentActivePanel.PanelID == targetPanelID)
        {
            Debug.LogWarning($"Already on panel '{targetPanelID}'. Re-showing with new payload (if any).");
            _currentActivePanel.ShowPanel(payload);
            return;
        }

        if (_currentActivePanel != null)
        {
            _currentActivePanel.HidePanel();
        }

        _currentActivePanel = targetPanel;
        _currentActivePanel.ShowPanel(payload);
    }
}
```

---

### **How to Set Up and Use in Unity:**

1.  **Create the Scripts**:
    *   Create three new C# scripts in your Unity project: `UINavigablePanel.cs`, `NavigationLink.cs`, and `NavigationLinkSystem.cs`. Copy the code above into them.

2.  **Create NavigationLinkSystem GameObject**:
    *   Create an empty GameObject in your scene (e.g., in your `Canvas` or at the root of your scene hierarchy).
    *   Rename it to `NavigationLinkSystem`.
    *   Attach the `NavigationLinkSystem.cs` script to this GameObject.
    *   **Crucially**, ensure this GameObject is in a scene that loads early (like a "Boot" or "Managers" scene) or on a persistent GameObject (using `DontDestroyOnLoad` which is already included in the script) to keep it alive across scene changes.

3.  **Define Your UI Panels**:
    *   Create your UI screens. For example:
        *   Right-click in Hierarchy -> UI -> Canvas (if you don't have one).
        *   Inside the Canvas, right-click -> UI -> Panel. Rename this `MainMenuPanel`.
        *   Duplicate `MainMenuPanel` two times. Rename them `OptionsPanel` and `CreditsPanel`.
    *   Design your UI as needed (add Text, Buttons, etc. to each panel).

4.  **Attach `UINavigablePanel.cs` to Each UI Root**:
    *   Select `MainMenuPanel`, `OptionsPanel`, and `CreditsPanel` in the Hierarchy.
    *   Attach the `UINavigablePanel.cs` script to each of them.
    *   For each panel:
        *   Set its `Panel ID` property (e.g., "MainMenu", "Options", "Credits"). These *must* be unique.
        *   Drag the respective panel GameObject itself (e.g., `MainMenuPanel` object) into the `_panelRoot` field of the `UINavigablePanel` component. (It defaults to `gameObject` if not set, but explicit is clearer).

5.  **Configure `NavigationLinkSystem` in the Inspector**:
    *   Select the `NavigationLinkSystem` GameObject.
    *   In the Inspector, set `Initial Panel ID` to "MainMenu" (or whichever panel you want to show first).
    *   **Define Navigation Links**: Expand the `Navigation Links` list. Add new elements for each important transition.
        *   **Link 1: MainMenu to Options**
            *   `Source Panel ID`: `MainMenu`
            *   `Target Panel ID`: `Options`
            *   `On Link Activated`: (Click the `+` icon to add an event. You could, for example, play a sound or save player preferences here).
        *   **Link 2: MainMenu to Credits**
            *   `Source Panel ID`: `MainMenu`
            *   `Target Panel ID`: `Credits`
            *   `On Link Activated`: (Perhaps log something or prepare a credits sequence).
        *   **Link 3: Options to MainMenu**
            *   `Source Panel ID`: `Options`
            *   `Target Panel ID`: `MainMenu`
            *   `On Link Activated`: (Maybe save options or confirm changes).
        *   **Link 4: Credits to MainMenu**
            *   `Source Panel ID`: `Credits`
            *   `Target Panel ID`: `MainMenu`
            *   `On Link Activated`: (Clean up credits specific assets).

6.  **Wire Up Buttons to `NavigationLinkSystem`**:
    *   **On `MainMenuPanel`**:
        *   Add a Button "Options".
        *   In its `OnClick()` event, drag the `NavigationLinkSystem` GameObject.
        *   Select Function: `NavigationLinkSystem.NavigateViaLink(string sourcePanelID, string targetPanelID, object payload = null)`
        *   For `sourcePanelID`, type `"MainMenu"`.
        *   For `targetPanelID`, type `"Options"`.
        *   Add a Button "Credits".
        *   In its `OnClick()` event, drag the `NavigationLinkSystem` GameObject.
        *   Select Function: `NavigationLinkSystem.NavigateViaLink(string sourcePanelID, string targetPanelID, object payload = null)`
        *   For `sourcePanelID`, type `"MainMenu"`.
        *   For `targetPanelID`, type `"Credits"`.

    *   **On `OptionsPanel`**:
        *   Add a Button "Back".
        *   In its `OnClick()` event, drag the `NavigationLinkSystem` GameObject.
        *   Select Function: `NavigationLinkSystem.NavigateViaLink(string sourcePanelID, string targetPanelID, object payload = null)`
        *   For `sourcePanelID`, type `"Options"`.
        *   For `targetPanelID`, type `"MainMenu"`.

    *   **On `CreditsPanel`**:
        *   Add a Button "Back".
        *   In its `OnClick()` event, drag the `NavigationLinkSystem` GameObject.
        *   Select Function: `NavigationLinkSystem.NavigateViaLink(string sourcePanelID, string targetPanelID, object payload = null)`
        *   For `sourcePanelID`, type `"Credits"`.
        *   For `targetPanelID`, type `"MainMenu"`.

7.  **Run Your Scene!**
    *   The `MainMenuPanel` should appear first.
    *   Clicking buttons will navigate between panels, and any `OnLinkActivated` events you set up in the `NavigationLinkSystem` will fire. Watch the Console for debug messages.

---

### **Example of Payload Data (Advanced Usage):**

You can pass data using the `payload` argument in `NavigateTo` or `NavigateViaLink`.

```csharp
// Example custom data class
public class GameSettingsPayload
{
    public float MasterVolume;
    public bool FullscreenMode;
    public string Language;
}

// How to call it from a script (e.g., attached to a button or game manager):
public class ButtonClickHandler : MonoBehaviour
{
    public void GoToOptionsWithSettings()
    {
        var settings = new GameSettingsPayload
        {
            MasterVolume = 0.75f,
            FullscreenMode = true,
            Language = "English"
        };

        // Option 1: Direct navigation (no specific link event triggered)
        NavigationLinkSystem.Instance.NavigateTo("Options", settings);

        // Option 2: Navigation via a defined link (its OnLinkActivated event will fire)
        // NavigationLinkSystem.Instance.NavigateViaLink("MainMenu", "Options", settings);
    }
}

// How UINavigablePanel could receive and use it:
public class OptionsUIPanel : UINavigablePanel
{
    // Add UI elements here like sliders, toggles, etc.

    public override void ShowPanel(object payload = null)
    {
        base.ShowPanel(payload); // Call base to activate root GameObject

        if (payload is GameSettingsPayload settings)
        {
            Debug.Log($"Options Panel received settings: Volume={settings.MasterVolume}, Fullscreen={settings.FullscreenMode}, Language={settings.Language}");
            // Update your UI elements here based on the 'settings' data
            // e.g., volumeSlider.value = settings.MasterVolume;
            // fullscreenToggle.isOn = settings.FullscreenMode;
        }
        else if (payload != null)
        {
            Debug.LogWarning($"Options Panel expected GameSettingsPayload but received {payload.GetType().Name}.");
        }
    }
}
```

---

This complete example provides a robust, educational, and practical `NavigationLinkSystem` for managing application states and UI in Unity, leveraging explicit links for powerful event-driven transitions.