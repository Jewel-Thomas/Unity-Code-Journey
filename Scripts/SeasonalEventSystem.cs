// Unity Design Pattern Example: SeasonalEventSystem
// This script demonstrates the SeasonalEventSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Seasonal Event System design pattern is used to manage game-world changes, events, or content based on predefined "seasons" or time periods. These seasons could be real-world calendar seasons, in-game calendar seasons, or custom periods like "Holiday Event," "Summer Sale," etc.

This pattern allows different parts of your game (environment, UI, quests, store items, gameplay mechanics) to react and adapt dynamically when a new season begins or an old one ends, promoting modularity and easier content management.

### Key Components of the Seasonal Event System:

1.  **Season Definition:** Data that defines what a season *is*. This could include its name, unique ID, visual assets, associated events, start/end dates, or gameplay modifiers. `ScriptableObject` is an excellent choice for this in Unity.
2.  **Season Manager (Singleton):** A central system responsible for:
    *   Determining the current season (based on real-world time, game time, or explicit setting).
    *   Holding a collection of all defined seasons.
    *   Providing methods to change the current season.
    *   Notifying other systems when a season changes. This is typically done via events or callbacks.
3.  **Seasonal Listeners/Subscribers:** Any game object or system that needs to react to season changes. These subscribe to the season manager's events and implement logic to update themselves based on the new season's data.

---

### Complete Unity Example: Seasonal Event System

This example provides:
*   A `SeasonType` enum for simple season identification.
*   A `SeasonDefinition` ScriptableObject to hold season-specific data (e.g., color, description).
*   A `SeasonalEventSystem` MonoBehaviour (Singleton) to manage seasons and broadcast events.
*   An example `SeasonalObjectChanger` MonoBehaviour that listens for season changes and updates a Renderer's material color and a TextMeshPro component's text.

**How to Use:**

1.  Create a new C# script named `SeasonalEventSystem.cs` and copy the code below into it.
2.  Create a new C# script named `SeasonalObjectChanger.cs` and copy its code below into it.
3.  In Unity, go to `Assets -> Create -> Seasonal -> Season Definition` to create several `SeasonDefinition` ScriptableObjects (e.g., "Spring", "Summer", "Autumn", "Winter"). Customize their names, types, colors, etc.
4.  Create an empty GameObject in your scene named "SeasonalEventSystem".
5.  Attach the `SeasonalEventSystem.cs` script to this GameObject.
6.  In the Inspector of the "SeasonalEventSystem" object, drag all your created `SeasonDefinition` assets into the `All Season Definitions` list.
7.  Create a 3D object (e.g., a Cube) in your scene. Add a `TextMeshPro - Text (UI)` component to it (you might need to import TMP Essentials if it's your first time).
8.  Attach the `SeasonalObjectChanger.cs` script to the Cube.
9.  Drag the Cube's `Renderer` component into the `Target Renderer` slot and its `TextMeshProUGUI` (or `TextMeshPro`) component into the `Target Text` slot of the `SeasonalObjectChanger` script in the Inspector.
10. Run the scene. You can manually advance seasons using the `[ContextMenu]` items on the `SeasonalEventSystem` component in the Inspector during Play Mode, or by calling `SeasonalEventSystem.Instance.AdvanceSeason()` from another script.

---

### 1. `SeasonalEventSystem.cs`

This script contains the core logic for defining and managing seasons, and broadcasting events.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events; // Required for UnityEvent

// --- 1. SeasonType Enum ---
// Defines the different types of seasons your game might have.
// Using an enum provides a clear, type-safe way to refer to seasons.
public enum SeasonType
{
    None,       // Default or uninitialized state
    Spring,
    Summer,
    Autumn,
    Winter,
    HolidayEvent, // Example of a custom, non-calendar season
    Halloween,
    NewYear
}

// --- 2. SeasonDefinition ScriptableObject ---
// This ScriptableObject defines the data for a single season.
// Using ScriptableObjects allows you to create and manage season data
// easily in the Unity Editor, separating data from logic.
[CreateAssetMenu(fileName = "NewSeasonDefinition", menuName = "Seasonal/Season Definition")]
public class SeasonDefinition : ScriptableObject
{
    [Tooltip("The unique identifier for this season.")]
    public SeasonType seasonType = SeasonType.None;

    [Tooltip("A display name for this season (e.g., 'Spring Bloom').")]
    public string seasonName = "New Season";

    [Tooltip("A brief description of the season.")]
    [TextArea]
    public string description = "A default season description.";

    [Tooltip("Example: A color associated with this season.")]
    public Color seasonalColor = Color.white;

    [Tooltip("Example: A specific texture or skybox associated with this season.")]
    public Texture2D seasonalTexture;

    // You can add many more season-specific data here:
    // public GameObject[] seasonalPrefabs;
    // public List<QuestData> seasonalQuests;
    // public float gameplayModifier;
    // public Material seasonalSkybox;
    // public AudioClip seasonalMusic;
}

// --- 3. SeasonalEventSystem MonoBehaviour (Singleton) ---
// This is the core manager that determines the current season and notifies
// other parts of the game about season changes.
public class SeasonalEventSystem : MonoBehaviour
{
    // Singleton pattern to ensure only one instance of the system exists.
    // This provides easy global access to the SeasonalEventSystem.
    public static SeasonalEventSystem Instance { get; private set; }

    [Header("Season Configuration")]
    [Tooltip("All available season definitions. Drag your SeasonDefinition ScriptableObjects here.")]
    [SerializeField] private List<SeasonDefinition> allSeasonDefinitions = new List<SeasonDefinition>();

    [Tooltip("The season that will be active when the game starts.")]
    [SerializeField] private SeasonType initialSeason = SeasonType.Spring;

    // The currently active season's definition.
    private SeasonDefinition _currentSeasonDefinition;
    public SeasonDefinition CurrentSeasonDefinition => _currentSeasonDefinition;

    // Helper property to get the current season type.
    public SeasonType CurrentSeasonType => _currentSeasonDefinition != null ? _currentSeasonDefinition.seasonType : SeasonType.None;

    // --- Events for Season Changes ---
    // UnityEvents allow you to hook up methods directly from the Unity Editor,
    // as well as programmatically. They are useful for notifying other systems.

    [Header("Season Events")]
    [Tooltip("Invoked BEFORE a season officially ends. Use this for cleanup or 'end of season' effects.")]
    public UnityEvent<SeasonDefinition> OnSeasonEnding = new UnityEvent<SeasonDefinition>();

    [Tooltip("Invoked AFTER a new season has officially started. Use this for setup or 'start of season' effects.")]
    public UnityEvent<SeasonDefinition> OnSeasonStarted = new UnityEvent<SeasonDefinition>();

    // Using System.Action for programmatic subscriptions (more common in C# code).
    // This provides an alternative or additional way for other scripts to subscribe.
    public event Action<SeasonDefinition> OnSeasonEndingAction;
    public event Action<SeasonDefinition> OnSeasonStartedAction;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SeasonalEventSystem: Multiple instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the system alive across scene changes.
        }
    }

    private void Start()
    {
        InitializeSeasons();
    }

    // --- Season Management Methods ---

    // Initializes the season system. This could determine the season based on real-world date
    // or set a default/initial season.
    private void InitializeSeasons()
    {
        if (allSeasonDefinitions == null || allSeasonDefinitions.Count == 0)
        {
            Debug.LogError("SeasonalEventSystem: No season definitions found! Please add them in the Inspector.");
            return;
        }

        // In a real game, you might load the last saved season or determine it based on date.
        // For this example, we'll start with the initialSeason or the first one in the list.
        SeasonDefinition initialDef = GetSeasonDefinition(initialSeason);
        if (initialDef == null)
        {
            Debug.LogWarning($"SeasonalEventSystem: Initial season '{initialSeason}' not found. Defaulting to first season in list.");
            initialDef = allSeasonDefinitions[0];
        }
        
        // Directly set the season without triggering "ending" event as it's the first one.
        _currentSeasonDefinition = initialDef;
        Debug.Log($"SeasonalEventSystem: Initialized with season: {_currentSeasonDefinition.seasonName}");
        
        // Notify listeners that the first season has started.
        OnSeasonStarted?.Invoke(_currentSeasonDefinition);
        OnSeasonStartedAction?.Invoke(_currentSeasonDefinition);
    }

    // Sets the current season to a specified SeasonType.
    public void SetCurrentSeason(SeasonType targetSeasonType)
    {
        if (_currentSeasonDefinition != null && _currentSeasonDefinition.seasonType == targetSeasonType)
        {
            Debug.Log($"SeasonalEventSystem: Season is already {targetSeasonType}. No change needed.");
            return;
        }

        SeasonDefinition newSeasonDef = GetSeasonDefinition(targetSeasonType);

        if (newSeasonDef == null)
        {
            Debug.LogError($"SeasonalEventSystem: Season definition for '{targetSeasonType}' not found!");
            return;
        }

        // 1. Notify listeners that the OLD season is ending.
        if (_currentSeasonDefinition != null)
        {
            Debug.Log($"SeasonalEventSystem: Season {_currentSeasonDefinition.seasonName} is ending...");
            OnSeasonEnding?.Invoke(_currentSeasonDefinition);
            OnSeasonEndingAction?.Invoke(_currentSeasonDefinition);
        }

        // 2. Update the current season.
        _currentSeasonDefinition = newSeasonDef;
        Debug.Log($"SeasonalEventSystem: Current season changed to: {_currentSeasonDefinition.seasonName} ({_currentSeasonDefinition.seasonType})");

        // 3. Notify listeners that the NEW season has started.
        OnSeasonStarted?.Invoke(_currentSeasonDefinition);
        OnSeasonStartedAction?.Invoke(_currentSeasonDefinition);
    }

    // Retrieves a SeasonDefinition ScriptableObject by its SeasonType.
    public SeasonDefinition GetSeasonDefinition(SeasonType type)
    {
        return allSeasonDefinitions.FirstOrDefault(s => s.seasonType == type);
    }

    // --- Example/Demo Methods (Context Menu for Editor Testing) ---

    // [ContextMenu] attributes allow you to call these methods directly
    // from the Inspector during Play Mode, which is useful for testing.

    [ContextMenu("Advance Season (Next in list)")]
    public void AdvanceSeason()
    {
        if (allSeasonDefinitions == null || allSeasonDefinitions.Count == 0)
        {
            Debug.LogWarning("No season definitions to advance through.");
            return;
        }

        int currentIndex = allSeasonDefinitions.IndexOf(_currentSeasonDefinition);
        int nextIndex = (currentIndex + 1) % allSeasonDefinitions.Count;

        SetCurrentSeason(allSeasonDefinitions[nextIndex].seasonType);
    }

    [ContextMenu("Set Season: Spring")]
    public void SetSeasonSpring() => SetCurrentSeason(SeasonType.Spring);

    [ContextMenu("Set Season: Summer")]
    public void SetSeasonSummer() => SetCurrentSeason(SeasonType.Summer);

    [ContextMenu("Set Season: Autumn")]
    public void SetSeasonAutumn() => SetCurrentSeason(SeasonType.Autumn);

    [ContextMenu("Set Season: Winter")]
    public void SetSeasonWinter() => SetCurrentSeason(SeasonType.Winter);

    [ContextMenu("Set Season: Holiday Event")]
    public void SetSeasonHolidayEvent() => SetCurrentSeason(SeasonType.HolidayEvent);

    // Placeholder for real-world date determination.
    // You would typically call this once per day or on game start.
    [ContextMenu("Determine Season Based on Real-World Date")]
    public void DetermineCurrentSeasonBasedOnDate()
    {
        Debug.Log("Determining season based on real-world date (Not fully implemented in example).");
        // Example logic:
        // DateTime now = DateTime.Now;
        // if (now.Month >= 3 && now.Month <= 5) { SetCurrentSeason(SeasonType.Spring); }
        // else if (now.Month >= 6 && now.Month <= 8) { SetCurrentSeason(SeasonType.Summer); }
        // ... and so on for all seasons, potentially including specific date ranges for holiday events.
        // This would require adding start/end dates to your SeasonDefinition ScriptableObjects.
    }
}
```

---

### 2. `SeasonalObjectChanger.cs`

This script demonstrates how a game object can act as a listener, subscribing to the `SeasonalEventSystem`'s events and reacting to season changes.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.UI; // Required for Image if you use it

// This script is an example of a Seasonal Listener.
// It subscribes to the SeasonalEventSystem and changes its appearance
// based on the currently active season.
public class SeasonalObjectChanger : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The Renderer whose material color will change.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("The TextMeshPro component whose text will change.")]
    [SerializeField] private TextMeshProUGUI targetText; // Or TextMeshPro if not UI-based

    private Material _originalMaterial;

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Get components if not assigned in Inspector.
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
        if (targetText == null)
        {
            targetText = GetComponentInChildren<TextMeshProUGUI>(); // Look in children if parent is UI element
            if (targetText == null)
            {
                targetText = GetComponent<TextMeshProUGUI>(); // Try on self
            }
        }

        if (targetRenderer != null)
        {
            // Store the original material to restore if needed, or just keep a reference.
            // For simple color changes, creating a new material instance is safer.
            _originalMaterial = targetRenderer.material; // Gets a unique instance of the material for this renderer.
        }
    }

    private void OnEnable()
    {
        // It's crucial to check if the Instance exists before subscribing,
        // especially if this script's Awake/OnEnable might run before the manager's.
        if (SeasonalEventSystem.Instance != null)
        {
            // Subscribe to the OnSeasonStarted UnityEvent.
            // When a new season starts, the UpdateSeasonalAppearance method will be called.
            SeasonalEventSystem.Instance.OnSeasonStarted.AddListener(UpdateSeasonalAppearance);
            
            // Optionally, you can also subscribe to the Action event for code-only subscriptions.
            // SeasonalEventSystem.Instance.OnSeasonStartedAction += UpdateSeasonalAppearance;

            // Call it immediately to set the initial appearance based on the current season.
            if (SeasonalEventSystem.Instance.CurrentSeasonDefinition != null)
            {
                UpdateSeasonalAppearance(SeasonalEventSystem.Instance.CurrentSeasonDefinition);
            }
        }
        else
        {
            Debug.LogWarning("SeasonalObjectChanger: SeasonalEventSystem.Instance not found on OnEnable. " +
                             "Ensure SeasonalEventSystem is initialized before this object.");
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe from events when the object is disabled or destroyed
        // to prevent memory leaks and 'missing reference' errors.
        if (SeasonalEventSystem.Instance != null)
        {
            SeasonalEventSystem.Instance.OnSeasonStarted.RemoveListener(UpdateSeasonalAppearance);
            // SeasonalEventSystem.Instance.OnSeasonStartedAction -= UpdateSeasonalAppearance;
        }
    }

    // This method is called by the SeasonalEventSystem when a new season starts.
    private void UpdateSeasonalAppearance(SeasonDefinition newSeason)
    {
        if (newSeason == null)
        {
            Debug.LogError("SeasonalObjectChanger: Received a null SeasonDefinition.");
            return;
        }

        // Example: Change material color
        if (targetRenderer != null)
        {
            // Ensure we are operating on an instance of the material, not the shared asset.
            targetRenderer.material.color = newSeason.seasonalColor;
            Debug.Log($"Changed renderer color to {newSeason.seasonalColor} for {newSeason.seasonName}.");
        }

        // Example: Change TextMeshPro text
        if (targetText != null)
        {
            targetText.text = $"Current Season:\n<color=#{ColorUtility.ToHtmlStringRGB(newSeason.seasonalColor)}>{newSeason.seasonName}</color>\n{newSeason.description}";
            Debug.Log($"Changed text to {newSeason.seasonName}.");
        }

        // Add more logic here to change other aspects based on 'newSeason' data:
        // - Change skybox: RenderSettings.skybox = newSeason.seasonalSkybox;
        // - Spawn seasonal particle effects: Instantiate(newSeason.seasonalEffectPrefab);
        // - Enable/disable seasonal objects: foreach (var obj in newSeason.seasonalObjects) obj.SetActive(true);
        // - Adjust gameplay parameters, etc.
    }
}
```

---

### How the Seasonal Event System Pattern Works (Detailed Comments):

The comments within the code are quite detailed, but here's a summary of how each part contributes to the pattern:

*   **`SeasonType` Enum:** Provides a clear, type-safe list of possible seasons. This acts as a lightweight identifier.
*   **`SeasonDefinition` (`ScriptableObject`):**
    *   **Data-Driven:** It decouples season-specific data (color, description, textures, potentially quests, items, modifiers) from the game logic.
    *   **Editor Workflow:** Artists, designers, and developers can create and configure new seasons directly in the Unity Editor without touching code, making content management very efficient.
    *   **Extensible:** Easily add new fields to `SeasonDefinition` to support more types of seasonal content or mechanics.
*   **`SeasonalEventSystem` (`MonoBehaviour`, Singleton):**
    *   **Centralized Control:** Acts as the single source of truth for the current season. All other systems query it or listen to its events.
    *   **Singleton:** Ensures easy global access (`SeasonalEventSystem.Instance`) and prevents multiple, conflicting season managers.
    *   **Season Progression Logic:** Contains the methods (`SetCurrentSeason`, `AdvanceSeason`, `DetermineCurrentSeasonBasedOnDate`) that control when and how the season changes. In a real project, `DetermineCurrentSeasonBasedOnDate` would use `System.DateTime.Now` to match real-world dates, or an in-game calendar system.
    *   **Event Broadcasting (`UnityEvent<T>`, `System.Action<T>`):** This is the core of the pattern's flexibility. When the season changes:
        *   `OnSeasonEnding` is invoked, allowing systems to clean up resources or finalize effects of the *old* season.
        *   `OnSeasonStarted` is invoked, allowing systems to set up assets, enable features, or apply effects for the *new* season.
        *   Using `UnityEvent` allows designers to directly drag-and-drop method calls in the Inspector, while `System.Action` provides a more traditional C# event subscription.
*   **`SeasonalObjectChanger` (`MonoBehaviour`, Listener):**
    *   **Decoupled Reactions:** This script doesn't need to know *how* the season changes, only *that* it changed. It's solely responsible for updating its own appearance/behavior based on the `SeasonDefinition` it receives.
    *   **Subscription/Unsubscription:** Uses `OnEnable` and `OnDisable` to properly `AddListener` and `RemoveListener` from the `SeasonalEventSystem`'s events. This prevents memory leaks and ensures it only reacts when active.
    *   **Direct Data Access:** Receives the `SeasonDefinition` object directly, allowing it to easily access all relevant data (color, text, etc.) for the new season.

This pattern makes your game highly modular and scalable for content updates related to different seasons or events, reducing the need for large, complex conditional logic (`if (currentSeason == SeasonType.Winter) ...`) scattered throughout your codebase.