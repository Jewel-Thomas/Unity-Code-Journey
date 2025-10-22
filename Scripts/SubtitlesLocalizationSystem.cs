// Unity Design Pattern Example: SubtitlesLocalizationSystem
// This script demonstrates the SubtitlesLocalizationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `SubtitlesLocalizationSystem` pattern focuses on creating a robust, centralized system for managing and displaying localized subtitles in a Unity application. It separates the concerns of data storage, language switching, and UI display, making the system flexible and scalable.

Here's a breakdown of the pattern's components and their roles:

1.  **`SubtitleData` (ScriptableObject):**
    *   **Purpose:** Acts as the data container for all subtitle entries for a *single specific language*.
    *   **Benefit:** Allows creation of reusable data assets in the Unity Editor (e.g., `en_Subtitles.asset`, `es_Subtitles.asset`). These assets can be easily managed, edited, and referenced. Being a ScriptableObject, it's independent of any GameObject.

2.  **`SubtitleLocalizationManager` (Singleton MonoBehaviour):**
    *   **Purpose:** The central hub for the entire system. It's a singleton to ensure only one instance manages the current language and subtitle data.
    *   **Responsibilities:**
        *   **Language Switching:** Loads `SubtitleData` for the requested language.
        *   **Current Language Tracking:** Stores and exposes the currently active language code.
        *   **Subtitle Retrieval:** Provides a method to fetch localized text based on a unique ID.
        *   **Event Notification:** Notifies all interested UI components when the language changes, prompting them to update their displayed text.

3.  **`LocalizedSubtitleText` (MonoBehaviour):**
    *   **Purpose:** A component attached to UI Text elements (e.g., `TextMeshProUGUI`) that need to display localized subtitles.
    *   **Responsibilities:**
        *   **Subscription:** Subscribes to the `SubtitleLocalizationManager`'s language change event.
        *   **ID Association:** Holds a unique `subtitleID` to identify which specific subtitle text it should display.
        *   **Text Update:** When the language changes (or on initialization), it requests the localized text from the `SubtitleLocalizationManager` using its `subtitleID` and updates its UI element.

**Benefits of this Pattern:**

*   **Centralized Control:** All language and subtitle management logic resides in one place.
*   **Decoupling:** UI elements don't need to know *how* to load subtitles, only *what* `subtitleID` they need. The manager handles the localization logic.
*   **Scalability:** Easily add new languages by creating new `SubtitleData` assets. Add new subtitles by editing the `SubtitleData` assets.
*   **Maintainability:** Changes to the localization logic only affect the manager; changes to subtitle data only affect the `SubtitleData` assets.
*   **Dynamic Updates:** UI elements automatically refresh their text when the language changes, without manual intervention.

---

## Complete C# Unity Example: SubtitlesLocalizationSystem

This example uses `TextMeshProUGUI` for UI text. If you haven't already, import TextMeshPro essentials into your project (Window > TextMeshPro > Import TMP Essential Resources).

### 1. `SubtitleData.cs` (ScriptableObject)

This asset holds all subtitle entries for a specific language.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject to store all subtitle entries for a single language.
/// This allows us to create assets like "en_Subtitles.asset" and "es_Subtitles.asset".
/// </summary>
[CreateAssetMenu(fileName = "NewSubtitleData", menuName = "Subtitle Localization/Subtitle Data")]
public class SubtitleData : ScriptableObject
{
    // The language code for this set of subtitles (e.g., "en", "es", "fr").
    public string languageCode;

    // An array of subtitle entries, exposed in the Inspector.
    // We use a serializable class for entries so Unity can save them.
    public SubtitleEntry[] entries;

    // A private dictionary for fast runtime lookup of subtitles by ID.
    private Dictionary<string, string> _subtitleDictionary;

    /// <summary>
    /// Converts the entries array into a dictionary for efficient lookup.
    /// This is done once after loading or on first access.
    /// </summary>
    public Dictionary<string, string> GetSubtitleDictionary()
    {
        if (_subtitleDictionary == null)
        {
            _subtitleDictionary = new Dictionary<string, string>();
            foreach (var entry in entries)
            {
                if (!_subtitleDictionary.ContainsKey(entry.id)) // Prevent duplicate keys
                {
                    _subtitleDictionary.Add(entry.id, entry.text);
                }
                else
                {
                    Debug.LogWarning($"Duplicate subtitle ID '{entry.id}' found in '{name}'. The first entry will be used.");
                }
            }
        }
        return _subtitleDictionary;
    }

    /// <summary>
    /// Resets the internal dictionary. Useful if you modify the 'entries' array
    /// at runtime (though typically ScriptableObjects are read-only at runtime).
    /// </summary>
    public void ClearDictionaryCache()
    {
        _subtitleDictionary = null;
    }
}

/// <summary>
/// A serializable class to represent a single subtitle entry,
/// consisting of a unique ID and the localized text.
/// </summary>
[System.Serializable]
public class SubtitleEntry
{
    public string id;   // Unique identifier for the subtitle (e.g., "intro_line_1", "quest_dialogue_3").
    [TextArea(3, 6)] // Makes the text field larger in the Inspector for easier editing.
    public string text; // The actual subtitle text in this language.
}
```

### 2. `SubtitleLocalizationManager.cs` (Singleton MonoBehaviour)

This is the core manager that handles loading languages, retrieving subtitles, and notifying UI elements of language changes.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For .FirstOrDefault()

/// <summary>
/// The central manager for subtitle localization.
/// This is a singleton responsible for loading subtitle data,
/// managing the current language, and providing localized text.
/// </summary>
public class SubtitleLocalizationManager : MonoBehaviour
{
    // Singleton instance.
    public static SubtitleLocalizationManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("The default language to load when the game starts (e.g., 'en', 'es').")]
    [SerializeField] private string _defaultLanguageCode = "en";

    [Tooltip("Path within Resources folder where SubtitleData assets are located. " +
             "e.g., 'Subtitles' if assets are at Assets/Resources/Subtitles/")]
    [SerializeField] private string _subtitleDataPath = "Subtitles";

    // The currently active language code.
    public string CurrentLanguageCode { get; private set; }

    // Reference to the currently loaded SubtitleData asset.
    private SubtitleData _currentSubtitleData;

    // Cache for loaded SubtitleData assets to avoid repeated loading from Resources.
    private Dictionary<string, SubtitleData> _loadedSubtitleDataCache = new Dictionary<string, SubtitleData>();

    // Event fired when the language changes. Listeners can update their text.
    public event Action<string> OnLanguageChanged;

    /// <summary>
    /// Initializes the singleton and loads the default language.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate SubtitleLocalizationManager found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes.

        LoadLanguage(_defaultLanguageCode);
    }

    /// <summary>
    /// Loads the subtitle data for a specified language code and makes it active.
    /// Notifies all subscribers that the language has changed.
    /// </summary>
    /// <param name="languageCode">The two-letter ISO language code (e.g., "en", "es").</param>
    public void LoadLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            Debug.LogError("Attempted to load a null or empty language code.");
            return;
        }

        // If the language is already loaded and active, do nothing.
        if (CurrentLanguageCode == languageCode && _currentSubtitleData != null)
        {
            Debug.Log($"Language '{languageCode}' is already active.");
            return;
        }

        SubtitleData targetSubtitleData;

        // Check if the data is already in cache
        if (_loadedSubtitleDataCache.TryGetValue(languageCode, out targetSubtitleData))
        {
            Debug.Log($"Loading language '{languageCode}' from cache.");
        }
        else
        {
            // Load from Resources. Note: For large projects, consider Addressables.
            targetSubtitleData = Resources.Load<SubtitleData>($"{_subtitleDataPath}/{languageCode}_Subtitles");

            if (targetSubtitleData == null)
            {
                Debug.LogError($"Failed to load SubtitleData for language '{languageCode}' from path 'Resources/{_subtitleDataPath}/{languageCode}_Subtitles'. " +
                               $"Make sure the asset exists and the path is correct.");
                // Fallback to default or error state
                if (_currentSubtitleData == null) // If no language loaded at all, try default
                {
                    if (languageCode != _defaultLanguageCode)
                    {
                        Debug.LogWarning($"Attempting to load default language '{_defaultLanguageCode}' as fallback.");
                        LoadLanguage(_defaultLanguageCode); // Recursive call, but only once for fallback
                        return;
                    }
                    else
                    {
                        Debug.LogError("No subtitle data could be loaded, even default language failed.");
                        return;
                    }
                }
                return; // Keep current language if new one fails to load
            }

            _loadedSubtitleDataCache.Add(languageCode, targetSubtitleData);
            Debug.Log($"Loaded language '{languageCode}' from Resources.");
        }

        _currentSubtitleData = targetSubtitleData;
        CurrentLanguageCode = languageCode;

        // Notify all listeners that the language has changed.
        OnLanguageChanged?.Invoke(languageCode);
        Debug.Log($"Subtitle language set to: {languageCode}");
    }

    /// <summary>
    /// Retrieves the localized subtitle text for a given ID in the current language.
    /// </summary>
    /// <param name="subtitleID">The unique ID of the subtitle entry.</param>
    /// <returns>The localized text, or an error string if not found.</returns>
    public string GetSubtitle(string subtitleID)
    {
        if (_currentSubtitleData == null)
        {
            Debug.LogWarning($"SubtitleLocalizationManager: No subtitle data loaded. Cannot get subtitle for ID '{subtitleID}'.");
            return $"[NO_LANGUAGE_DATA]";
        }

        if (string.IsNullOrEmpty(subtitleID))
        {
            return "[EMPTY_SUBTITLE_ID]";
        }

        if (_currentSubtitleData.GetSubtitleDictionary().TryGetValue(subtitleID, out string localizedText))
        {
            return localizedText;
        }
        else
        {
            Debug.LogWarning($"Subtitle ID '{subtitleID}' not found in language '{CurrentLanguageCode}'.");
            return $"[MISSING_SUBTITLE: {subtitleID}]";
        }
    }

    /// <summary>
    /// Clears all loaded subtitle data from cache.
    /// Useful if memory needs to be freed, or in editor workflows.
    /// </summary>
    public void ClearCache()
    {
        _loadedSubtitleDataCache.Clear();
        _currentSubtitleData = null;
        CurrentLanguageCode = string.Empty;
        Debug.Log("SubtitleLocalizationManager cache cleared.");
    }
}
```

### 3. `LocalizedSubtitleText.cs` (MonoBehaviour)

This component is attached to a `TextMeshProUGUI` component to make it display localized subtitles.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System;

/// <summary>
/// Attaches to a TextMeshProUGUI component to automatically display
/// localized subtitles based on a given Subtitle ID.
/// It subscribes to language change events from SubtitleLocalizationManager.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))] // Ensures a TextMeshProUGUI component is present.
public class LocalizedSubtitleText : MonoBehaviour
{
    [Tooltip("The unique ID for the subtitle entry to display.")]
    [SerializeField] private string _subtitleID;

    private TextMeshProUGUI _tmpTextComponent;

    /// <summary>
    /// Gets the TextMeshProUGUI component on this GameObject.
    /// </summary>
    private void Awake()
    {
        _tmpTextComponent = GetComponent<TextMeshProUGUI>();
        if (_tmpTextComponent == null)
        {
            Debug.LogError($"LocalizedSubtitleText requires a TextMeshProUGUI component on {gameObject.name}.");
            enabled = false; // Disable this script if no text component.
        }
    }

    /// <summary>
    /// Subscribes to the language change event and updates text initially.
    /// </summary>
    private void OnEnable()
    {
        if (SubtitleLocalizationManager.Instance != null)
        {
            SubtitleLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            UpdateText(); // Update text immediately when enabled.
        }
        else
        {
            Debug.LogWarning("LocalizedSubtitleText: SubtitleLocalizationManager instance not found. Text will not be localized.");
        }
    }

    /// <summary>
    /// Unsubscribe from the language change event to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        if (SubtitleLocalizationManager.Instance != null)
        {
            SubtitleLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// Callback method invoked when the language changes.
    /// </summary>
    /// <param name="newLanguageCode">The new active language code.</param>
    private void OnLanguageChanged(string newLanguageCode)
    {
        UpdateText();
    }

    /// <summary>
    /// Requests the localized text from the manager and updates the TextMeshProUGUI component.
    /// </summary>
    private void UpdateText()
    {
        if (_tmpTextComponent == null) return;
        if (string.IsNullOrEmpty(_subtitleID))
        {
            _tmpTextComponent.text = "[NO_SUBTITLE_ID_SET]";
            return;
        }

        if (SubtitleLocalizationManager.Instance != null)
        {
            _tmpTextComponent.text = SubtitleLocalizationManager.Instance.GetSubtitle(_subtitleID);
        }
        else
        {
            _tmpTextComponent.text = $"[MANAGER_NOT_FOUND: {_subtitleID}]";
        }
    }

    /// <summary>
    /// Public method to dynamically change the subtitle ID and refresh the text.
    /// Useful for displaying dynamic subtitles during gameplay (e.g., character dialogue).
    /// </summary>
    /// <param name="newSubtitleID">The new subtitle ID to display.</param>
    public void SetSubtitleID(string newSubtitleID)
    {
        _subtitleID = newSubtitleID;
        UpdateText();
    }

    // Optional: Public getter for the current subtitle ID.
    public string GetSubtitleID() => _subtitleID;
}
```

### 4. `LanguageSwitcherUI.cs` (Example for UI Interaction)

A simple script to demonstrate changing languages via UI buttons.

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI script to demonstrate language switching.
/// Attach this to a GameObject that contains buttons for different languages.
/// </summary>
public class LanguageSwitcherUI : MonoBehaviour
{
    [Header("Language Buttons")]
    [SerializeField] private Button _englishButton;
    [SerializeField] private Button _spanishButton;
    [SerializeField] private Button _frenchButton; // Add more as needed

    [Header("Current Language Display")]
    [SerializeField] private TextMeshProUGUI _currentLanguageText;

    private void Start()
    {
        // Add listeners to the buttons.
        _englishButton?.onClick.AddListener(() => SetLanguage("en"));
        _spanishButton?.onClick.AddListener(() => SetLanguage("es"));
        _frenchButton?.onClick.AddListener(() => SetLanguage("fr")); // Add more listeners

        // Update the display text initially
        if (SubtitleLocalizationManager.Instance != null)
        {
            SubtitleLocalizationManager.Instance.OnLanguageChanged += UpdateCurrentLanguageDisplay;
            UpdateCurrentLanguageDisplay(SubtitleLocalizationManager.Instance.CurrentLanguageCode);
        }
    }

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks, especially important for singletons.
        _englishButton?.onClick.RemoveAllListeners();
        _spanishButton?.onClick.RemoveAllListeners();
        _frenchButton?.onClick.RemoveAllListeners();

        if (SubtitleLocalizationManager.Instance != null)
        {
            SubtitleLocalizationManager.Instance.OnLanguageChanged -= UpdateCurrentLanguageDisplay;
        }
    }

    /// <summary>
    /// Calls the SubtitleLocalizationManager to change the active language.
    /// </summary>
    /// <param name="languageCode">The two-letter ISO language code.</param>
    public void SetLanguage(string languageCode)
    {
        if (SubtitleLocalizationManager.Instance != null)
        {
            SubtitleLocalizationManager.Instance.LoadLanguage(languageCode);
        }
        else
        {
            Debug.LogError("SubtitleLocalizationManager.Instance is null. Cannot change language.");
        }
    }

    /// <summary>
    /// Updates the UI text to show the currently active language.
    /// </summary>
    /// <param name="languageCode">The code of the new active language.</param>
    private void UpdateCurrentLanguageDisplay(string languageCode)
    {
        if (_currentLanguageText != null)
        {
            _currentLanguageText.text = $"Current Language: {languageCode.ToUpper()}";
        }
    }
}
```

---

### How to Set Up in Unity

1.  **Create Folders:**
    *   `Assets/Scripts/Localization` (for `SubtitleData`, `SubtitleLocalizationManager`, `LocalizedSubtitleText`, `LanguageSwitcherUI`)
    *   `Assets/Resources/Subtitles` (crucial for `Resources.Load` to find assets)

2.  **Place Scripts:** Drop the four C# scripts into `Assets/Scripts/Localization`.

3.  **Create SubtitleData Assets:**
    *   Go to `Assets/Resources/Subtitles`.
    *   Right-click -> Create -> Subtitle Localization -> Subtitle Data.
    *   Create at least two:
        *   Rename one to `en_Subtitles.asset`. Select it in the Inspector:
            *   Set `Language Code` to `en`.
            *   Expand `Entries`, set `Size` to `3`.
            *   `Element 0`: `ID = greeting`, `Text = Hello, adventurer! Welcome to the world.`
            *   `Element 1`: `ID = farewell`, `Text = Farewell, and may your journey be safe!`
            *   `Element 2`: `ID = quest_start`, `Text = A new quest awaits you! Find the ancient relic.`
        *   Rename another to `es_Subtitles.asset`. Select it in the Inspector:
            *   Set `Language Code` to `es`.
            *   Expand `Entries`, set `Size` to `3`.
            *   `Element 0`: `ID = greeting`, `Text = ¡Hola, aventurero! Bienvenido al mundo.`
            *   `Element 1`: `ID = farewell`, `Text = ¡Adiós, y que tu viaje sea seguro!`
            *   `Element 2`: `ID = quest_start`, `Text = ¡Una nueva misión te espera! Encuentra la reliquia antigua.`
        *   (Optional) Create `fr_Subtitles.asset`:
            *   Set `Language Code` to `fr`.
            *   `Element 0`: `ID = greeting`, `Text = Bonjour, aventurier ! Bienvenue dans le monde.`
            *   `Element 1`: `ID = farewell`, `Text = Adieu, et que votre voyage soit sûr !`
            *   `Element 2`: `ID = quest_start`, `Text = Une nouvelle quête vous attend ! Trouvez l'ancienne relique.`

4.  **Create SubtitleLocalizationManager:**
    *   Create an empty GameObject in your scene (e.g., `_Managers`).
    *   Add `SubtitleLocalizationManager.cs` component to it.
    *   In the Inspector, ensure `Default Language Code` is `en` and `Subtitle Data Path` is `Subtitles`.

5.  **Create UI for Display and Switching:**
    *   Create a UI Canvas (`GameObject -> UI -> Canvas`).
    *   Add a `TextMeshPro - Text (UI)` (`GameObject -> UI -> Text - TextMeshPro`).
        *   Rename it `LocalizedTextDisplay`.
        *   Adjust its size and position.
        *   Add `LocalizedSubtitleText.cs` component to it.
        *   In the Inspector, set `Subtitle ID` to `greeting`. (You should immediately see "Hello, adventurer..." or the default language text appear).
    *   Add a second `TextMeshPro - Text (UI)`
        *   Rename it `QuestTextDisplay`.
        *   Adjust its size and position.
        *   Add `LocalizedSubtitleText.cs` component to it.
        *   In the Inspector, set `Subtitle ID` to `quest_start`.
    *   Add another `TextMeshPro - Text (UI)`
        *   Rename it `CurrentLanguageDisplay`.
        *   Adjust its size and position.
        *   Leave its `Text` blank, or set to "Current Language:".
        *   **Do NOT** add `LocalizedSubtitleText` to this one, as `LanguageSwitcherUI` will update it.
    *   Add some UI Buttons (`GameObject -> UI -> Button - TextMeshPro`):
        *   Create three buttons (e.g., "English", "Spanish", "French"). Position them nicely on the Canvas.
    *   Create an empty GameObject `LanguageSwitcher` on the Canvas.
    *   Add `LanguageSwitcherUI.cs` to the `LanguageSwitcher` GameObject.
    *   **Drag & Drop** the buttons and `CurrentLanguageDisplay` TextMeshPro component into the `LanguageSwitcherUI` script's serialized fields in the Inspector.

6.  **Run the Scene:**
    *   Play the scene.
    *   You should see the "Hello, adventurer..." and "A new quest awaits you!" texts.
    *   Click the "Spanish" button. The text should instantly change to Spanish.
    *   Click the "French" button. The text should change to French.
    *   Click the "English" button. The text should revert to English.

---

### Example Usage in Comments (already included in scripts)

The code includes `[Tooltip(...)]`, `[Header(...)]`, `[SerializeField]` attributes, and clear comments explaining each part of the system, making it ready to use and easy to understand for Unity developers learning design patterns.

For instance, `LocalizedSubtitleText` demonstrates:
```csharp
/// <summary>
/// Public method to dynamically change the subtitle ID and refresh the text.
/// Useful for displaying dynamic subtitles during gameplay (e.g., character dialogue).
/// </summary>
/// <param name="newSubtitleID">The new subtitle ID to display.</param>
public void SetSubtitleID(string newSubtitleID)
{
    _subtitleID = newSubtitleID;
    UpdateText();
}

// Example usage elsewhere in a game script (e.g., a DialogueManager):
/*
public class DialogueManager : MonoBehaviour
{
    public LocalizedSubtitleText dialogueTextDisplay; // Assign in Inspector

    public void StartDialogue(string dialogueID)
    {
        // Example: "character_name_line_01"
        dialogueTextDisplay.SetSubtitleID(dialogueID);
    }
}
*/
```

This complete setup provides a practical and educational example of the SubtitlesLocalizationSystem design pattern, ready to be dropped into a Unity project.