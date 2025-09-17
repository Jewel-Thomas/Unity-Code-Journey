// Unity Design Pattern Example: HUDCustomizationSystem
// This script demonstrates the HUDCustomizationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a **HUD Customization System** in Unity, incorporating several common design patterns to achieve a flexible, extensible, and persistent system for user interface customization.

**Core Design Patterns Used:**

1.  **Singleton:** The `HUDCustomizationManager` is a Singleton, providing a single, globally accessible point of control for managing HUD settings.
2.  **Observer Pattern:**
    *   The `HUDCustomizationManager` acts as the **Subject/Publisher**, broadcasting changes to HUD element settings through an event (`OnHUDElementSettingsChanged`).
    *   `HUDCustomizableElement` (and its derived classes) act as **Observers/Subscribers**, listening to this event and updating their appearance when their specific element's settings change.
3.  **Component Pattern:** Individual HUD elements are implemented as Unity components (`HUDCustomizableElement` and its derivatives) that can be attached to UI GameObjects.
4.  **Memento Pattern (Simplified for Persistence):** `HUDProfileSettings` and `HUDElementSettings` act as the 'Memento' objects, storing the customizable state of the HUD. The `HUDCustomizationManager` acts as the 'CareTaker', responsible for saving (`PlayerPrefs` + JSON) and loading these mementos.
5.  **Strategy/Template Method (implicit):** `HUDCustomizableElement` provides a base `ApplyCustomization` method with common logic (position, scale, visibility) and a virtual method that derived classes can override to apply element-specific customizations (e.g., changing a health bar's fill color or a minimap's texture).

---

## Project Setup in Unity

1.  Create a new Unity project.
2.  Create a folder `Scripts/HUDCustomizationSystem` in your Assets.
3.  Place all the C# scripts provided below into this folder.
4.  **Create Manager GameObject:**
    *   Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> `Create Empty`).
    *   Rename it `HUDCustomizationManager`.
    *   Attach the `HUDCustomizationManager.cs` script to this GameObject. It will persist across scenes.
5.  **Create Canvas:**
    *   Right-click in Hierarchy -> `UI` -> `Canvas`.
    *   Ensure the `Canvas` component's `Render Mode` is set to `Screen Space - Overlay`. (This example assumes Screen Space - Overlay, but could be adapted for others).
6.  **Create Example HUD Elements:**
    *   **Health Bar:**
        *   Right-click on `Canvas` -> `UI` -> `Image`. Rename it `HealthBar`.
        *   Adjust its `Rect Transform`: Set `Anchors` to `Min(0,0)`, `Max(0,0)` and `Pivot` to `Min(0,0)`, `Max(0,0)`. Set `Pos X: 100`, `Pos Y: 100`, `Width: 200`, `Height: 30`.
        *   Add a `Slider` as a child to the `HealthBar` GameObject (Right-click `HealthBar` -> `UI` -> `Slider`). Configure the Slider to fill its parent.
        *   Attach the `ExampleHUDHealthBar.cs` script to the `HealthBar` GameObject.
        *   In the Inspector for `ExampleHUDHealthBar`: Set `Element ID` to "HealthBar". Drag the `Slider` component and its `Fill Image` (usually located under `Fill Area/Fill` in the Slider hierarchy) into the respective `_healthSlider` and `_fillImage` fields.
    *   **Minimap:**
        *   Right-click on `Canvas` -> `UI` -> `Raw Image`. Rename it `Minimap`.
        *   Adjust its `Rect Transform`: Set `Anchors` to `Min(0,0)`, `Max(0,0)` and `Pivot` to `Min(0,0)`, `Max(0,0)`. Set `Pos X: 800`, `Pos Y: 400`, `Width: 150`, `Height: 150`.
        *   Attach the `ExampleHUDMinimap.cs` script to the `Minimap` GameObject.
        *   In the Inspector for `ExampleHUDMinimap`: Set `Element ID` to "Minimap". Create or find any `Texture2D` asset in your project (e.g., a simple noise texture or a screenshot) and drag it to the `Map Texture` field.
7.  **Run the Scene:**
    *   Play the scene. Observe the initial positions and appearance of your HUD elements.
    *   Select the `HUDCustomizationManager` GameObject in the Hierarchy.
    *   In the Inspector, right-click on the `HUDCustomizationManager` component to access its `Context Menu` methods:
        *   **`Randomize All HUD Element Settings`**: Watch your HUD elements jump to random positions, scales, colors, and visibility states.
        *   **`Save Current HUD Profile`**: Saves the current (possibly randomized) state.
        *   **`Load HUD Profile`**: If you randomized again, then load, they should return to the saved randomized state.
        *   **`Reset All HUD Elements to Default`**: They should revert to their default, configured appearance.
    *   Stop and re-run the scene. If you saved a profile, the HUD elements should load that saved state automatically in `Start()`.

---

## 1. `HUDElementSettings.cs`

This class defines the data structure for the customizable properties of a single HUD element.

```csharp
using UnityEngine;
using System;

/// <summary>
/// Represents the customizable settings for a single HUD element.
/// This is the 'state' that can be modified by the user and persisted.
/// It's designed to be resolution-independent by using normalized coordinates (0-1).
/// </summary>
[Serializable]
public class HUDElementSettings
{
    public string ElementID; // Unique identifier for the HUD element
    public bool IsVisible = true;
    public Vector2 Position = Vector2.zero; // Normalized position (0-1) relative to parent canvas
    public Vector2 Size = Vector2.one;      // Normalized size (0-1) relative to parent canvas
    public float Scale = 1f;                // Overall scale factor
    public Color Color = Color.white;       // Example: tint color for the element

    // Default constructor for easy instantiation (though not strictly necessary with initializers)
    public HUDElementSettings() { }

    /// <summary>
    /// Full constructor for creating a settings object with specific values.
    /// </summary>
    public HUDElementSettings(string id, bool isVisible, Vector2 position, Vector2 size, float scale, Color color)
    {
        ElementID = id;
        IsVisible = isVisible;
        Position = position;
        Size = size;
        Scale = scale;
        Color = color;
    }

    /// <summary>
    /// Creates a default settings object for a given element ID with sensible initial values.
    /// </summary>
    public static HUDElementSettings Default(string elementID)
    {
        return new HUDElementSettings
        {
            ElementID = elementID,
            IsVisible = true,
            Position = new Vector2(0.5f, 0.5f), // Center of screen, normalized
            Size = new Vector2(0.1f, 0.1f),     // 10% of screen width/height, normalized
            Scale = 1f,
            Color = Color.white
        };
    }
}
```

---

## 2. `HUDProfileSettings.cs`

This class acts as a container for all `HUDElementSettings`, representing a complete user customization profile. It's the 'Memento' for the entire HUD state.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// A container for all HUD element settings for a given profile.
/// This structure is used for saving and loading the complete HUD configuration.
/// It acts as the 'Memento' for the entire HUD customization state.
/// </summary>
[Serializable]
public class HUDProfileSettings
{
    // A list to store the settings for each individual HUD element.
    // Using a list is generally easier for JSON serialization than a Dictionary directly.
    public List<HUDElementSettings> ElementSettings = new List<HUDElementSettings>();

    public HUDProfileSettings() { }

    /// <summary>
    /// Adds or updates the settings for a specific HUD element within this profile.
    /// If an element with the same ID already exists, its settings are updated.
    /// Otherwise, a new entry is added.
    /// </summary>
    /// <param name="settings">The HUDElementSettings to add or update.</param>
    public void AddOrUpdateElementSettings(HUDElementSettings settings)
    {
        if (settings == null) return;

        // Check if settings for this ID already exist
        for (int i = 0; i < ElementSettings.Count; i++)
        {
            if (ElementSettings[i].ElementID == settings.ElementID)
            {
                ElementSettings[i] = settings; // Update existing settings
                return;
            }
        }
        ElementSettings.Add(settings); // Add new settings if not found
    }

    /// <summary>
    /// Retrieves the settings for a specific HUD element ID from this profile.
    /// </summary>
    /// <param name="elementID">The unique identifier of the HUD element.</param>
    /// <returns>The HUDElementSettings if found, otherwise null.</returns>
    public HUDElementSettings GetElementSettings(string elementID)
    {
        foreach (var settings in ElementSettings)
        {
            if (settings.ElementID == elementID)
            {
                return settings;
            }
        }
        return null;
    }
}
```

---

## 3. `IHUDCustomizable.cs`

An interface that defines the contract for any HUD element that wishes to be part of the customization system.

```csharp
using UnityEngine;

/// <summary>
/// Interface for any HUD element that can be customized by the HUDCustomizationSystem.
/// This ensures a common contract for how elements interact with the customization manager.
/// </summary>
public interface IHUDCustomizable
{
    /// <summary>
    /// A unique identifier for this HUD element.
    /// Used by the manager to track and apply specific settings.
    /// </summary>
    string ElementID { get; }

    /// <summary>
    /// Applies the given customization settings to this HUD element.
    /// This method is called by the HUDCustomizationManager when settings change
    /// or when the element is initialized.
    /// </summary>
    /// <param name="settings">The HUDElementSettings to apply.</param>
    void ApplyCustomization(HUDElementSettings settings);

    /// <summary>
    /// Retrieves the current settings from this HUD element.
    /// Used by the manager to get the element's current state (e.g., for saving).
    /// </summary>
    /// <returns>The current HUDElementSettings of this element.</returns>
    HUDElementSettings GetCurrentSettings();
}
```

---

## 4. `HUDCustomizationManager.cs`

The central hub of the system. It's a Singleton, manages the current settings for all elements, acts as the Observer pattern's Subject/Publisher, and handles saving/loading of profiles.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// HUDCustomizationManager: The core of the HUD Customization System.
/// This class acts as a central hub (Singleton) for managing all customizable HUD elements.
/// It uses the Observer pattern to notify elements of setting changes and
/// a Memento-like approach for saving and loading profiles.
///
/// Role in the pattern:
/// 1.  Singleton: Ensures a single, globally accessible instance.
/// 2.  Subject/Publisher (Observer Pattern): Notifies registered HUD elements when their settings change.
/// 3.  CareTaker (Memento Pattern): Manages saving and loading of HUDProfileSettings using PlayerPrefs.
/// </summary>
public class HUDCustomizationManager : MonoBehaviour
{
    // --- Singleton Instance ---
    private static HUDCustomizationManager _instance;
    public static HUDCustomizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<HUDCustomizationManager>();
                if (_instance == null)
                {
                    // If none found, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject(typeof(HUDCustomizationManager).Name);
                    _instance = singletonObject.AddComponent<HUDCustomizationManager>();
                }
            }
            return _instance;
        }
    }

    // --- Events (Observer Pattern) ---
    /// <summary>
    /// Event fired when the settings for a specific HUD element have changed.
    /// Subscribers (IHUDCustomizable elements) can listen to this to update their appearance.
    /// Parameters: string elementID, HUDElementSettings newSettings.
    /// </summary>
    public event Action<string, HUDElementSettings> OnHUDElementSettingsChanged;

    // --- Internal State ---
    // Stores the current customization settings for all HUD elements known to the manager.
    // This dictionary holds the 'desired' state.
    private Dictionary<string, HUDElementSettings> _currentHUDElementSettings = new Dictionary<string, HUDElementSettings>();

    // Temporarily stores references to active IHUDCustomizable elements in the scene.
    // Used for initial application of settings and retrieving current states for saving.
    private Dictionary<string, IHUDCustomizable> _registeredElements = new Dictionary<string, IHUDCustomizable>();

    // --- Persistence ---
    private const string HUD_PROFILE_PLAYERPREFS_KEY = "HUDCustomizationProfile_"; // Suffix with profile ID
    [Tooltip("The ID of the currently active HUD profile. Used for saving/loading.")]
    [SerializeField] private string _activeProfileID = "DefaultProfile";


    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If another instance already exists, destroy this one to enforce singleton
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // Make sure the manager persists across scene loads
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Load the active profile's settings when the manager starts.
        // These settings will be applied to elements as they register.
        LoadHUDProfile(_activeProfileID);
    }

    // --- Element Registration and Deregistration ---

    /// <summary>
    /// Registers a customizable HUD element with the manager.
    /// When an element registers, the manager immediately applies its known settings to it.
    /// This ensures elements are always initialized with the current customization state.
    /// </summary>
    /// <param name="element">The IHUDCustomizable element to register.</param>
    public void RegisterHUDElement(IHUDCustomizable element)
    {
        if (element == null || string.IsNullOrEmpty(element.ElementID))
        {
            Debug.LogError("Attempted to register a null or invalid HUD element.");
            return;
        }

        if (_registeredElements.ContainsKey(element.ElementID))
        {
            Debug.LogWarning($"HUD Element with ID '{element.ElementID}' already registered. Overwriting existing reference.", (MonoBehaviour)element);
        }

        _registeredElements[element.ElementID] = element;
        Debug.Log($"Registered HUD Element: {element.ElementID}");

        // Apply existing settings immediately upon registration
        if (_currentHUDElementSettings.TryGetValue(element.ElementID, out HUDElementSettings settings))
        {
            element.ApplyCustomization(settings);
        }
        else
        {
            // If no settings exist for this element (e.g., first time it's seen),
            // get its current state (likely its default values) and store them.
            HUDElementSettings defaultSettings = element.GetCurrentSettings();
            if (defaultSettings == null) // Fallback if element doesn't provide its own default
            {
                 defaultSettings = HUDElementSettings.Default(element.ElementID);
            }
            _currentHUDElementSettings[element.ElementID] = defaultSettings;
            element.ApplyCustomization(defaultSettings); // Apply the default settings
        }
    }

    /// <summary>
    /// Unregisters a customizable HUD element from the manager.
    /// This prevents the manager from trying to update or save settings for an inactive element.
    /// </summary>
    /// <param name="element">The IHUDCustomizable element to unregister.</param>
    public void UnregisterHUDElement(IHUDCustomizable element)
    {
        if (element == null || string.IsNullOrEmpty(element.ElementID)) return;

        if (_registeredElements.Remove(element.ElementID))
        {
            Debug.Log($"Unregistered HUD Element: {element.ElementID}");
        }
    }

    // --- Customization Management ---

    /// <summary>
    /// Updates the settings for a specific HUD element and notifies all subscribers.
    /// This is the primary method for changing HUD element properties from a customization UI.
    /// </summary>
    /// <param name="newSettings">The updated HUDElementSettings.</param>
    public void UpdateHUDElementSettings(HUDElementSettings newSettings)
    {
        if (newSettings == null || string.IsNullOrEmpty(newSettings.ElementID))
        {
            Debug.LogError("Attempted to update settings with a null or invalid HUDElementSettings object.");
            return;
        }

        // Store the new settings
        _currentHUDElementSettings[newSettings.ElementID] = newSettings;
        Debug.Log($"Updated settings for: {newSettings.ElementID}. Visible={newSettings.IsVisible}, Pos={newSettings.Position}");

        // Notify observers (the actual HUD elements) to apply the changes
        OnHUDElementSettingsChanged?.Invoke(newSettings.ElementID, newSettings);
    }

    /// <summary>
    /// Retrieves the current settings for a specific HUD element from the manager's state.
    /// </summary>
    /// <param name="elementID">The unique ID of the HUD element.</param>
    /// <returns>The current HUDElementSettings, or null if not found.</returns>
    public HUDElementSettings GetHUDElementSettings(string elementID)
    {
        _currentHUDElementSettings.TryGetValue(elementID, out HUDElementSettings settings);
        return settings;
    }

    // --- Persistence (Memento Pattern inspired) ---

    /// <summary>
    /// Saves the current state of all known HUD elements into a profile.
    /// It gathers the latest settings from registered elements and also saves settings
    /// for elements that might not be currently active but were previously customized.
    /// </summary>
    /// <param name="profileID">The unique ID for this profile (e.g., "Default", "Aggressive", "Minimalist").</param>
    [ContextMenu("Save Current HUD Profile")]
    public void SaveHUDProfile(string profileID = null)
    {
        if (string.IsNullOrEmpty(profileID)) profileID = _activeProfileID;

        HUDProfileSettings profile = new HUDProfileSettings();

        // Iterate through all currently managed settings.
        // For active elements, get their very latest state.
        // For inactive elements, save their last known state held by the manager.
        foreach (var entry in _currentHUDElementSettings)
        {
            if (_registeredElements.TryGetValue(entry.Key, out IHUDCustomizable registeredElement))
            {
                // If the element is currently active, get its absolute latest settings
                profile.AddOrUpdateElementSettings(registeredElement.GetCurrentSettings());
            }
            else
            {
                // If not active, use the last known settings stored in the manager
                profile.AddOrUpdateElementSettings(entry.Value);
            }
        }

        // Serialize the profile to JSON and save it in PlayerPrefs
        string json = JsonUtility.ToJson(profile);
        PlayerPrefs.SetString(HUD_PROFILE_PLAYERPREFS_KEY + profileID, json);
        PlayerPrefs.Save(); // Ensures data is written to disk
        Debug.Log($"HUD profile '{profileID}' saved: {json.Length} bytes.");
    }

    /// <summary>
    /// Loads a HUD profile and applies its settings to all registered HUD elements.
    /// Elements that are not currently registered but have settings in the profile
    /// will have their settings stored, to be applied when they become active.
    /// </summary>
    /// <param name="profileID">The unique ID of the profile to load.</param>
    [ContextMenu("Load HUD Profile")]
    public void LoadHUDProfile(string profileID = null)
    {
        if (string.IsNullOrEmpty(profileID)) profileID = _activeProfileID;

        string json = PlayerPrefs.GetString(HUD_PROFILE_PLAYERPREFS_KEY + profileID, string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            HUDProfileSettings loadedProfile = JsonUtility.FromJson<HUDProfileSettings>(json);
            Debug.Log($"HUD profile '{profileID}' loaded.");

            _currentHUDElementSettings.Clear(); // Clear existing settings before loading new ones

            foreach (var settings in loadedProfile.ElementSettings)
            {
                _currentHUDElementSettings[settings.ElementID] = settings;

                // Immediately apply to any currently registered elements
                if (_registeredElements.TryGetValue(settings.ElementID, out IHUDCustomizable element))
                {
                    element.ApplyCustomization(settings);
                }
            }
        }
        else
        {
            Debug.LogWarning($"No HUD profile found for ID '{profileID}'. Applying default settings for all active elements.");
            // If no profile found, ensure all active elements are initialized with their defaults.
            // This loop iterates active elements and ensures they have settings in _currentHUDElementSettings,
            // then applies them.
            foreach (var element in _registeredElements.Values)
            {
                 if (!_currentHUDElementSettings.ContainsKey(element.ElementID))
                 {
                    // If element not in manager's _currentHUDElementSettings (e.g., first load)
                    HUDElementSettings defaultSettings = element.GetCurrentSettings() ?? HUDElementSettings.Default(element.ElementID);
                    _currentHUDElementSettings[element.ElementID] = defaultSettings;
                 }
                element.ApplyCustomization(_currentHUDElementSettings[element.ElementID]);
            }
        }
        // Ensure all registered elements have their current settings applied,
        // especially if settings were loaded for elements not initially registered.
        ApplyAllCurrentSettings();
    }

    /// <summary>
    /// Applies the currently stored settings for ALL known elements to their respective registered GameObjects.
    /// Useful after a load or when a new element registers late to ensure consistency.
    /// </summary>
    public void ApplyAllCurrentSettings()
    {
        foreach (var entry in _currentHUDElementSettings)
        {
            if (_registeredElements.TryGetValue(entry.Key, out IHUDCustomizable element))
            {
                element.ApplyCustomization(entry.Value);
            }
        }
    }

    // --- Editor-only utility methods for demonstration ---
    [ContextMenu("Randomize All HUD Element Settings")]
    private void RandomizeAllSettings()
    {
        if (_registeredElements.Count == 0)
        {
            Debug.LogWarning("No HUD elements registered to randomize.");
            return;
        }

        foreach (var elementEntry in _registeredElements)
        {
            HUDElementSettings current = GetHUDElementSettings(elementEntry.Key);
            if (current == null) continue; // Should not happen for registered elements

            // Randomize various properties
            current.IsVisible = UnityEngine.Random.value > 0.1f; // 90% chance to be visible
            current.Position = new Vector2(UnityEngine.Random.Range(0.05f, 0.95f), UnityEngine.Random.Range(0.05f, 0.95f));
            current.Size = new Vector2(UnityEngine.Random.Range(0.05f, 0.2f), UnityEngine.Random.Range(0.05f, 0.2f));
            current.Scale = UnityEngine.Random.Range(0.5f, 1.5f);
            current.Color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);

            UpdateHUDElementSettings(current); // Apply and broadcast the changes
        }
        Debug.Log("Randomized settings for all registered HUD elements.");
    }

    [ContextMenu("Reset All HUD Elements to Default")]
    private void ResetAllToDefault()
    {
        if (_registeredElements.Count == 0)
        {
            Debug.LogWarning("No HUD elements registered to reset.");
            return;
        }

        foreach (var elementEntry in _registeredElements)
        {
            HUDElementSettings defaultSettings = HUDElementSettings.Default(elementEntry.Key);
            UpdateHUDElementSettings(defaultSettings); // Apply and broadcast default settings
        }
        Debug.Log("Reset all registered HUD elements to default settings.");
    }
}
```

---

## 5. `HUDCustomizableElement.cs`

This abstract base class implements `IHUDCustomizable`. It handles the common logic for registering, unsubscribing, and applying generic UI transformations (position, scale, visibility) based on `HUDElementSettings`. Derived classes will add element-specific logic.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Base class for all customizable HUD elements.
/// This class implements the IHUDCustomizable interface and handles the
/// common logic for registering with the manager, subscribing to updates,
/// and applying basic RectTransform properties.
///
/// Role in the pattern:
/// 1.  Observer (Observer Pattern): Subscribes to OnHUDElementSettingsChanged events from the manager.
/// 2.  Concrete Component (Component Pattern): Attachable to a GameObject to make it a customizable HUD part.
/// 3.  Template Method (Implicit): Provides a virtual ApplyCustomization for common logic,
///     allowing derived classes to extend it for specific visual changes.
/// </summary>
[RequireComponent(typeof(RectTransform))] // Most UI elements need a RectTransform
public abstract class HUDCustomizableElement : MonoBehaviour, IHUDCustomizable
{
    [Tooltip("A unique identifier for this HUD element across the game.")]
    [SerializeField] private string _elementID;
    public string ElementID => _elementID;

    protected RectTransform _rectTransform; // Reference to the element's RectTransform
    protected Graphic _graphic;             // Optional: reference to a Graphic component (Image, Text, RawImage)

    // --- Unity Lifecycle Methods ---
    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _graphic = GetComponent<Graphic>(); // Try to get a graphic component if it exists
        if (_rectTransform == null)
        {
            Debug.LogError($"HUDCustomizableElement '{_elementID}' requires a RectTransform component.", this);
            enabled = false; // Disable if critical component is missing
        }
    }

    protected virtual void OnEnable()
    {
        if (!Application.isPlaying) return; // Don't register in editor mode (unless explicitly testing)

        if (string.IsNullOrEmpty(_elementID))
        {
            Debug.LogError($"HUDCustomizableElement on '{gameObject.name}' has no Element ID assigned. It will not be customizable.", this);
            return;
        }

        if (HUDCustomizationManager.Instance != null)
        {
            HUDCustomizationManager.Instance.RegisterHUDElement(this);
            HUDCustomizationManager.Instance.OnHUDElementSettingsChanged += HandleSettingsChanged;
        }
        else
        {
            Debug.LogError("HUDCustomizationManager not found. Ensure it's in the scene and set up correctly.", this);
        }
    }

    protected virtual void OnDisable()
    {
        if (!Application.isPlaying) return; // Don't unregister in editor mode
        if (HUDCustomizationManager.Instance != null)
        {
            HUDCustomizationManager.Instance.OnHUDElementSettingsChanged -= HandleSettingsChanged;
            HUDCustomizationManager.Instance.UnregisterHUDElement(this);
        }
    }

    // --- IHUDCustomizable Implementation ---

    /// <summary>
    /// Applies the given customization settings to this HUD element's RectTransform.
    /// This method handles common properties like visibility, position, size, and scale.
    /// Derived classes should override this to add their specific visual or functional changes,
    /// typically by calling `base.ApplyCustomization(settings)` first.
    /// </summary>
    /// <param name="settings">The HUDElementSettings to apply.</param>
    public virtual void ApplyCustomization(HUDElementSettings settings)
    {
        if (settings == null) return;

        // Apply common visibility
        _rectTransform.gameObject.SetActive(settings.IsVisible);

        // Apply common RectTransform properties if the element is visible
        if (settings.IsVisible && _rectTransform != null)
        {
            // Set anchors to bottom-left for easier normalized position handling.
            // This makes `anchoredPosition` relative to the bottom-left of the parent.
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;

            // Get the parent's RectTransform to calculate pixel positions/sizes from normalized values.
            RectTransform parentRect = _rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                float parentHeight = parentRect.rect.height;

                // Calculate pixel position based on normalized settings.Position
                // `anchoredPosition` sets the pivot of the element.
                // If settings.Position is (0.5, 0.5) and parent is 1000x500, it sets pivot at (500, 250).
                _rectTransform.anchoredPosition = new Vector2(
                    settings.Position.x * parentWidth,
                    settings.Position.y * parentHeight
                );

                // Set size based on normalized settings.Size
                // `sizeDelta` defines the width/height of the RectTransform.
                _rectTransform.sizeDelta = new Vector2(
                    settings.Size.x * parentWidth,
                    settings.Size.y * parentHeight
                );
            }
            else
            {
                Debug.LogWarning($"HUD element '{_elementID}' has no RectTransform parent. Position/Size might not behave as expected.", this);
                // Fallback: apply raw pixel values, might not scale well
                _rectTransform.anchoredPosition = settings.Position * 1000f; // Arbitrary pixel scale
                _rectTransform.sizeDelta = settings.Size * 100f; // Arbitrary pixel scale
            }

            // Apply overall scale
            _rectTransform.localScale = Vector3.one * settings.Scale;
        }

        // Apply common Graphic properties (if the element has an Image, Text, RawImage etc.)
        if (_graphic != null)
        {
            _graphic.color = settings.Color;
        }
    }

    /// <summary>
    /// Retrieves the current settings from this HUD element.
    /// This method is called by the HUDCustomizationManager when saving a profile.
    /// Derived classes should override this to include their specific custom settings,
    /// typically by calling `base.GetCurrentSettings()` first and then modifying the result.
    /// </summary>
    /// <returns>The current HUDElementSettings of this element.</returns>
    public virtual HUDElementSettings GetCurrentSettings()
    {
        Vector2 currentPosition = Vector2.zero;
        Vector2 currentSize = Vector2.one;
        float currentScale = 1f;

        if (_rectTransform != null)
        {
            RectTransform parentRect = _rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                // Convert current pixel position to normalized position
                currentPosition = new Vector2(
                    _rectTransform.anchoredPosition.x / parentRect.rect.width,
                    _rectTransform.anchoredPosition.y / parentRect.rect.height
                );
                // Convert current pixel size to normalized size
                currentSize = new Vector2(
                    _rectTransform.sizeDelta.x / parentRect.rect.width,
                    _rectTransform.sizeDelta.y / parentRect.rect.height
                );
            }
            else
            {
                // Fallback if no parent, assume a default scale for conversion
                currentPosition = _rectTransform.anchoredPosition / 1000f; // Inverse of arbitrary pixel scale
                currentSize = _rectTransform.sizeDelta / 100f; // Inverse of arbitrary pixel scale
            }
            currentScale = _rectTransform.localScale.x; // Assuming uniform scale
        }

        Color currentColor = _graphic != null ? _graphic.color : Color.white;

        return new HUDElementSettings
        {
            ElementID = _elementID,
            IsVisible = gameObject.activeSelf, // Get current active state
            Position = currentPosition,
            Size = currentSize,
            Scale = currentScale,
            Color = currentColor
        };
    }

    // --- Event Handling (Observer Pattern) ---
    private void HandleSettingsChanged(string changedElementID, HUDElementSettings newSettings)
    {
        // Only apply settings if the event is for this specific element
        if (changedElementID == _elementID)
        {
            ApplyCustomization(newSettings);
        }
    }

    // --- Editor only ---
    // Auto-assign ID if empty when added in editor or GameObject is renamed
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(_elementID))
        {
            _elementID = gameObject.name;
        }
    }
}
```

---

## 6. `ExampleHUDHealthBar.cs`

A concrete example of a customizable health bar, derived from `HUDCustomizableElement`. It adds specific logic for a UI `Slider`.

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An example concrete implementation of a customizable HUD element: a Health Bar.
/// This script demonstrates how to extend HUDCustomizableElement to add specific
/// visual or functional logic for a particular HUD component, in this case, a UI Slider.
/// </summary>
public class ExampleHUDHealthBar : HUDCustomizableElement
{
    [Header("Health Bar Specifics")]
    [Tooltip("Reference to the UI Slider component representing the health bar.")]
    [SerializeField] private Slider _healthSlider; 
    [Tooltip("Reference to the Image component that acts as the fill of the health bar.")]
    [SerializeField] private Image _fillImage;     
    [Tooltip("Example current health value (0-1).")]
    [SerializeField] private float _currentHealth = 0.75f; 

    protected override void Awake()
    {
        base.Awake(); // Call base Awake to initialize RectTransform and Graphic

        // Attempt to auto-find components if not set in Inspector
        if (_healthSlider == null)
        {
            _healthSlider = GetComponentInChildren<Slider>(true);
            if (_healthSlider == null)
            {
                Debug.LogError($"HealthBar HUD element '{ElementID}' requires a Slider component as a child.", this);
            }
        }
        if (_fillImage == null && _healthSlider != null)
        {
            // Common paths for the fill image within a Slider's hierarchy
            Transform fillTransform = _healthSlider.transform.Find("Fill Area/Fill");
            if (fillTransform == null) fillTransform = _healthSlider.transform.Find("Fill");
            if (fillTransform != null) _fillImage = fillTransform.GetComponent<Image>();
        }

        // Initialize health slider properties
        if (_healthSlider != null)
        {
            _healthSlider.minValue = 0f;
            _healthSlider.maxValue = 1f;
            _healthSlider.value = _currentHealth;
            _healthSlider.interactable = false; // Health bar is usually not interactive by user
        }
    }

    /// <summary>
    /// Overrides the base ApplyCustomization to add health bar specific logic.
    /// </summary>
    public override void ApplyCustomization(HUDElementSettings settings)
    {
        base.ApplyCustomization(settings); // Apply common RectTransform and Graphic settings first

        // Health bar specific adjustments:
        if (_healthSlider != null)
        {
            _healthSlider.gameObject.SetActive(settings.IsVisible); // Ensure slider itself matches visibility
            _healthSlider.value = _currentHealth; // Keep current health, or update if settings included health (advanced)
        }
        if (_fillImage != null)
        {
            // Use the generic settings.Color to tint the health bar fill
            _fillImage.color = settings.Color; 
        }

        Debug.Log($"Applied settings to Health Bar ({ElementID}): Visible={settings.IsVisible}, Pos={settings.Position}, Scale={settings.Scale}, Color={settings.Color}");
    }

    /// <summary>
    /// Overrides GetCurrentSettings to include any health bar specific data if needed.
    /// For this example, it just returns the base settings as no extra specific settings are stored.
    /// </summary>
    public override HUDElementSettings GetCurrentSettings()
    {
        // Get the base settings first
        HUDElementSettings baseSettings = base.GetCurrentSettings();

        // If there were health-bar specific settings (e.g., custom border color, specific value range),
        // you would add them here by either extending HUDElementSettings or adding custom fields.
        // For simplicity, we're using the base settings as-is.
        return baseSettings;
    }

    // Example methods to simulate health change for demonstration
    [ContextMenu("Take Damage")]
    public void TakeDamage()
    {
        _currentHealth = Mathf.Max(0f, _currentHealth - 0.1f);
        if (_healthSlider != null)
        {
            _healthSlider.value = _currentHealth;
        }
        Debug.Log($"{ElementID} Current Health: {_currentHealth * 100}%");
    }

    [ContextMenu("Heal")]
    public void Heal()
    {
        _currentHealth = Mathf.Min(1f, _currentHealth + 0.1f);
        if (_healthSlider != null)
        {
            _healthSlider.value = _currentHealth;
        }
        Debug.Log($"{ElementID} Current Health: {_currentHealth * 100}%");
    }
}
```

---

## 7. `ExampleHUDMinimap.cs`

Another concrete example of a customizable minimap, also derived from `HUDCustomizableElement`. This one utilizes a `RawImage` to display a texture.

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Another example concrete implementation of a customizable HUD element: a Minimap.
/// This script demonstrates extending HUDCustomizableElement for a different type of UI component,
/// in this case, a RawImage used to display a map texture.
/// </summary>
public class ExampleHUDMinimap : HUDCustomizableElement
{
    [Header("Minimap Specifics")]
    [Tooltip("Reference to the RawImage component that displays the minimap texture.")]
    [SerializeField] private RawImage _minimapTextureDisplay; 
    [Tooltip("The actual map texture to be displayed on the minimap.")]
    [SerializeField] private Texture2D _mapTexture; 

    protected override void Awake()
    {
        base.Awake(); // Call base Awake to initialize RectTransform and Graphic

        // Attempt to auto-find components if not set in Inspector
        if (_minimapTextureDisplay == null)
        {
            _minimapTextureDisplay = GetComponent<RawImage>();
            if (_minimapTextureDisplay == null)
            {
                Debug.LogError($"Minimap HUD element '{ElementID}' requires a RawImage component.", this);
            }
        }

        // Set the map texture
        if (_minimapTextureDisplay != null)
        {
            _minimapTextureDisplay.texture = _mapTexture;
        }
    }

    /// <summary>
    /// Overrides the base ApplyCustomization to add minimap specific logic.
    /// </summary>
    public override void ApplyCustomization(HUDElementSettings settings)
    {
        base.ApplyCustomization(settings); // Apply common RectTransform and Graphic settings first

        // Minimap specific adjustments:
        if (_minimapTextureDisplay != null)
        {
            _minimapTextureDisplay.gameObject.SetActive(settings.IsVisible);
            // Example: You could change the minimap texture based on settings here if you had
            // an enum or string field in HUDElementSettings (e.g., settings.MinimapStyle)
            _minimapTextureDisplay.color = settings.Color; // Apply general color to tint the minimap
        }

        Debug.Log($"Applied settings to Minimap ({ElementID}): Visible={settings.IsVisible}, Pos={settings.Position}, Scale={settings.Scale}, Color={settings.Color}");
    }

    /// <summary>
    /// Overrides GetCurrentSettings to include any minimap specific data if needed.
    /// For this example, it just returns the base settings as no extra specific settings are stored.
    /// </summary>
    public override HUDElementSettings GetCurrentSettings()
    {
        // Get the base settings first
        HUDElementSettings baseSettings = base.GetCurrentSettings();

        // If there were minimap-specific settings (e.g., zoom level, rotation, different map texture IDs),
        // you would add them here or derive HUDElementSettings into HUDMinimapSettings.
        // For simplicity, we're using the base settings as-is.
        return baseSettings;
    }

    // Example method for minimap specific interaction (not using the manager for simplicity here,
    // a real customization would update via the manager to be persistent and broadcasted).
    [ContextMenu("Zoom In Minimap")]
    public void ZoomIn()
    {
        // For a real minimap, this might involve changing a camera's orthographic size.
        // For this UI component, we'll simulate by slightly increasing its UI scale directly.
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale *= 1.1f;
            Debug.Log($"Minimap zoomed in. New Scale: {rt.localScale.x}");
        }
    }
}
```