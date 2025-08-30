// Unity Design Pattern Example: LocalizationSystem
// This script demonstrates the LocalizationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The LocalizationSystem design pattern centralizes the management of localized text, audio, and other assets within an application. In Unity, this typically involves a global access point (a Singleton) that provides localized content based on the currently selected language. UI elements and other game objects then query this system for the appropriate content.

This example provides a complete and practical implementation using C# and Unity, focusing on text localization.

---

### Key Components of this LocalizationSystem:

1.  **`Language` Enum:** Defines all supported languages.
2.  **`LocalizationData` (ScriptableObject):** An asset that holds all localized strings. This is highly beneficial as it allows you to create and manage translations directly in the Unity Editor without modifying code.
    *   It uses `LanguageDataEntry` to group all strings for a specific `Language`.
    *   `LocalizedStringEntry` stores the `key` (a unique identifier) and its `value` (the translated string).
3.  **`LocalizationSystem` (Singleton MonoBehaviour):** The central hub of the system.
    *   It's a `MonoBehaviour` that lives in your scene and persists across scene loads (`DontDestroyOnLoad`).
    *   It manages the `_currentLanguage` and the `_currentLanguageDictionary` (a cached dictionary for fast lookups of localized strings).
    *   It provides an `OnLanguageChanged` event that other components can subscribe to, allowing them to automatically update their displayed text when the language changes.
    *   `SetLanguage()`: Switches the active language and notifies subscribers.
    *   `GetLocalizedValue()`: Retrieves the translated string for a given key, with a fallback if the key isn't found.
4.  **`LocalizableText` (Example UI Component):** A simple `MonoBehaviour` that demonstrates how to use the `LocalizationSystem`.
    *   It's attached to a UI TextMeshProUGUI component.
    *   It stores a `localizationKey` and uses `LocalizationSystem.Instance.GetLocalizedValue()` to display the correct text.
    *   It subscribes to `LocalizationSystem.Instance.OnLanguageChanged` to update itself automatically.

---

### File Structure:

For clarity and best practices in a real Unity project, you should create these as **two separate C# script files**:

1.  `LocalizationSystem.cs` (Contains `Language` enum, `LocalizationSystem` class, and `LocalizableText` class)
2.  `LocalizationData.cs` (Contains `LocalizationData` ScriptableObject and its helper classes/structs)

However, to meet the "complete, working C# script" requirement in a single output, I've combined them below, clearly separating them with comments.

---

### 1. `LocalizationSystem.cs` (and `LocalizableText.cs`)

This file contains the core `LocalizationSystem` logic and an example `LocalizableText` component for integration with Unity's UI.

```csharp
// --- LocalizationSystem.cs ---
// This file contains the main LocalizationSystem (Singleton) and the Language enum.
// It also includes a basic LocalizableText component for demonstration.

using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro; // Required for TextMeshProUGUI. If using legacy UI Text, use 'using UnityEngine.UI;' and 'Text'.

/// <summary>
/// Defines the supported languages for the localization system.
/// Add new languages here as needed. The order can be important if you're
/// mapping to dropdown indices, but generally, the enum name is used directly.
/// </summary>
public enum Language
{
    English,
    Spanish,
    French,
    German,
    // Add more languages here as needed
}

/// <summary>
/// The core Localization System, implemented as a Singleton.
/// It manages loading and switching languages, and provides localized strings.
/// This MonoBehaviour should be placed on a GameObject in your scene and will
/// persist across scene loads.
/// </summary>
public class LocalizationSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static LocalizationSystem _instance;

    /// <summary>
    /// Provides the global access point to the LocalizationSystem instance.
    /// This ensures there's only one instance throughout the application.
    /// If an instance doesn't exist, it will create one.
    /// </summary>
    public static LocalizationSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<LocalizationSystem>();

                if (_instance == null)
                {
                    // If no instance exists, create a new GameObject and add the component
                    GameObject singletonObject = new GameObject(typeof(LocalizationSystem).Name);
                    _instance = singletonObject.AddComponent<LocalizationSystem>();
                    Debug.Log($"LocalizationSystem: No instance found, created new GameObject '{singletonObject.name}'.");
                }
            }
            return _instance;
        }
    }

    // --- Events ---
    /// <summary>
    /// Event fired when the language changes. UI elements and other scripts
    /// that display localized text should subscribe to this event to update themselves.
    /// e.g., LocalizationSystem.Instance.OnLanguageChanged += MyUpdateMethod;
    /// </summary>
    public event Action OnLanguageChanged;

    // --- Configuration Fields (set in Unity Inspector) ---
    [Tooltip("The ScriptableObject asset containing all localization data (translations).")]
    [SerializeField] private LocalizationData localizationData;

    [Tooltip("The default language to load when the system starts up.")]
    [SerializeField] private Language defaultLanguage = Language.English;

    // --- Internal State ---
    private Language _currentLanguage;
    private Dictionary<string, string> _currentLanguageDictionary;

    /// <summary>
    /// Gets the currently active language.
    /// </summary>
    public Language CurrentLanguage => _currentLanguage;

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Ensure only one instance exists. If another instance already exists, destroy this one.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // Keep this GameObject alive across scene loads
        DontDestroyOnLoad(gameObject);

        // Load the default language on startup
        SetLanguage(defaultLanguage);

        Debug.Log($"LocalizationSystem initialized. Default language set to: {_currentLanguage}");
    }

    // --- Public Methods ---

    /// <summary>
    /// Sets the active language for the application.
    /// This will load the new language's data and automatically trigger the
    /// OnLanguageChanged event, causing all subscribed UI elements to update.
    /// </summary>
    /// <param name="newLanguage">The language to switch to.</param>
    public void SetLanguage(Language newLanguage)
    {
        if (localizationData == null)
        {
            Debug.LogError("LocalizationSystem: localizationData ScriptableObject is not assigned! Please assign it in the Inspector.");
            return;
        }

        // If the requested language is already active and its dictionary is loaded, do nothing.
        if (_currentLanguage == newLanguage && _currentLanguageDictionary != null)
        {
            Debug.Log($"LocalizationSystem: Language is already set to {newLanguage}. No change needed.");
            return;
        }

        // Attempt to get the dictionary for the new language from the ScriptableObject.
        Dictionary<string, string> langDict = localizationData.GetLanguageDictionary(newLanguage);

        if (langDict == null)
        {
            Debug.LogError($"LocalizationSystem: No localization data found for language: {newLanguage}. Please check your LocalizationData asset.");
            return;
        }

        // Update the current language and its associated dictionary.
        _currentLanguage = newLanguage;
        _currentLanguageDictionary = langDict;

        // Notify all subscribers (e.g., LocalizableText components) that the language has changed.
        OnLanguageChanged?.Invoke();
        Debug.Log($"LocalizationSystem: Language changed to: {_currentLanguage}");
    }

    /// <summary>
    /// Retrieves the localized string for a given key in the current language.
    /// </summary>
    /// <param name="key">The unique key identifying the string (e.g., "GAME_TITLE", "BUTTON_PLAY").</param>
    /// <returns>The localized string, or the key itself if not found (as a fallback).</returns>
    public string GetLocalizedValue(string key)
    {
        // If no language dictionary is loaded (e.g., system not fully initialized), return the key.
        if (_currentLanguageDictionary == null)
        {
            Debug.LogWarning($"LocalizationSystem: No language dictionary loaded. Returning key: '{key}'.");
            return key; // Fallback: return the key itself
        }

        // Try to get the value for the given key.
        if (_currentLanguageDictionary.TryGetValue(key, out string localizedValue))
        {
            return localizedValue;
        }
        else
        {
            // If the key is not found in the current language's dictionary.
            Debug.LogWarning($"LocalizationSystem: Key '{key}' not found in language '{_currentLanguage}'. Returning key as fallback.");
            return key; // Fallback: return the key itself to indicate a missing translation.
        }
    }
}

// ========================================================================================
// --- LocalizableText.cs ---
// This file contains a simple example component that uses the LocalizationSystem.
// It should be attached to a GameObject with a TextMeshProUGUI component.
// ========================================================================================

/// <summary>
/// A simple component to display localized text on a TextMeshProUGUI element.
/// Attach this script to a GameObject that has a TextMeshProUGUI component
/// and set its 'Localization Key' in the Inspector.
/// </summary>
[RequireComponent(typeof(TMP_Text))] // Ensures a TextMeshProUGUI component is present on the GameObject
public class LocalizableText : MonoBehaviour
{
    [Tooltip("The unique key corresponding to the localized string in your LocalizationData.")]
    [SerializeField] private string localizationKey;

    private TMP_Text _textComponent; // Reference to the TextMeshProUGUI component

    private void Awake()
    {
        // Get the TextMeshProUGUI component on this GameObject.
        _textComponent = GetComponent<TMP_Text>();
        if (_textComponent == null)
        {
            Debug.LogError($"LocalizableText: No TextMeshProUGUI component found on {gameObject.name}. Disabling script.");
            enabled = false; // Disable this script if no text component is found.
        }
    }

    private void OnEnable()
    {
        // Subscribe to the language changed event to automatically update the text
        // whenever the LocalizationSystem switches languages.
        if (LocalizationSystem.Instance != null)
        {
            LocalizationSystem.Instance.OnLanguageChanged += UpdateText;
        }
        // Immediately update the text when this component becomes active,
        // in case the language was already set before this component was enabled.
        UpdateText();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and ensure we don't try to update
        // a destroyed object if the LocalizationSystem outlives this component.
        if (LocalizationSystem.Instance != null)
        {
            LocalizationSystem.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    /// <summary>
    /// Retrieves the localized string from the LocalizationSystem and updates
    /// the TextMeshProUGUI component's text.
    /// This method is called when the language changes or when the component is enabled.
    /// </summary>
    public void UpdateText()
    {
        // Don't update if the text component is missing or the key is empty.
        if (_textComponent == null || string.IsNullOrEmpty(localizationKey))
        {
            return;
        }

        // Request the localized value from the LocalizationSystem.
        if (LocalizationSystem.Instance != null)
        {
            _textComponent.text = LocalizationSystem.Instance.GetLocalizedValue(localizationKey);
        }
        else
        {
            // Fallback if LocalizationSystem isn't ready (should not happen if setup correctly).
            _textComponent.text = $"[Localization System Not Ready] {localizationKey}";
        }
    }

    /// <summary>
    /// Allows setting the localization key programmatically at runtime.
    /// </summary>
    /// <param name="newKey">The new localization key to use.</param>
    public void SetLocalizationKey(string newKey)
    {
        localizationKey = newKey;
        UpdateText(); // Update the text immediately after changing the key.
    }

#if UNITY_EDITOR
    // Optional: Update text in the editor to show the key, as the LocalizationSystem
    // is usually not active in editor mode. This helps identify the text.
    private void OnValidate()
    {
        // Only attempt to update if the application is not playing to avoid
        // editor-specific warnings/errors when LocalizationSystem isn't initialized.
        if (!Application.isPlaying && _textComponent == null)
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        if (_textComponent != null)
        {
            if (!string.IsNullOrEmpty(localizationKey))
            {
                // In editor, just show the key.
                _textComponent.text = $"KEY: {localizationKey}";
            }
            else
            {
                _textComponent.text = "[No Loc Key]";
            }
        }
    }
#endif
}
```

---

### 2. `LocalizationData.cs`

This file defines the `ScriptableObject` that holds all your translated strings, making it easy to manage them in the Unity Editor.

```csharp
// --- LocalizationData.cs ---
// This file contains the ScriptableObject that holds all localization data.
// It should be created as a separate asset in your project.

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject to hold all localization data in an editor-friendly format.
/// Create an instance of this asset in your project (e.g., Right-click -> Create -> Localization -> Localization Data).
/// </summary>
[CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Localization Data", order = 1)]
public class LocalizationData : ScriptableObject
{
    /// <summary>
    /// Represents a single key-value pair for a localized string.
    /// Unity's Inspector can serialize public fields or fields marked with [SerializeField].
    /// </summary>
    [Serializable]
    public struct LocalizedStringEntry
    {
        public string key;
        [TextArea(3, 6)] // Make the string field multi-line in the Inspector for easier editing
        public string value;
    }

    /// <summary>
    /// Represents all localized strings for a single language.
    /// This class is designed to be easily managed within the Unity Inspector.
    /// </summary>
    [Serializable]
    public class LanguageDataEntry
    {
        public Language language;
        public List<LocalizedStringEntry> entries = new List<LocalizedStringEntry>();

        // Private field to cache the dictionary after first access for performance.
        // Converting List to Dictionary is done only once per language load.
        private Dictionary<string, string> _cachedDictionary;

        /// <summary>
        /// Converts the list of entries for this language into a Dictionary for efficient lookup.
        /// Caches the result to avoid repeated conversion.
        /// </summary>
        /// <returns>A dictionary of key-value pairs for this language.</returns>
        public Dictionary<string, string> GetDictionary()
        {
            if (_cachedDictionary == null)
            {
                _cachedDictionary = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    if (!_cachedDictionary.ContainsKey(entry.key)) // Prevent duplicate keys
                    {
                        _cachedDictionary.Add(entry.key, entry.value);
                    }
                    else
                    {
                        Debug.LogWarning($"LocalizationData: Duplicate key '{entry.key}' found for language '{language}'. Ignoring duplicate entry.");
                    }
                }
            }
            return _cachedDictionary;
        }

        /// <summary>
        /// Clears the cached dictionary. Useful if data is modified at runtime (rare for static localization data).
        /// </summary>
        public void ClearCache()
        {
            _cachedDictionary = null;
        }
    }

    [Tooltip("List of all language entries, each containing a specific language's strings.")]
    public List<LanguageDataEntry> languageEntries = new List<LanguageDataEntry>();

    // Caches all language dictionaries after their first lookup for even faster access.
    private Dictionary<Language, Dictionary<string, string>> _allLanguagesCachedDictionaries;

    /// <summary>
    /// Retrieves the dictionary of localized strings for a specific language.
    /// Caches the results for all languages for faster subsequent lookups.
    /// </summary>
    /// <param name="lang">The language to retrieve the dictionary for.</param>
    /// <returns>A dictionary of key-value pairs for the specified language, or null if not found.</returns>
    public Dictionary<string, string> GetLanguageDictionary(Language lang)
    {
        if (_allLanguagesCachedDictionaries == null)
        {
            _allLanguagesCachedDictionaries = new Dictionary<Language, Dictionary<string, string>>();
            foreach (var entry in languageEntries)
            {
                if (!_allLanguagesCachedDictionaries.ContainsKey(entry.language))
                {
                    _allLanguagesCachedDictionaries.Add(entry.language, entry.GetDictionary());
                }
                else
                {
                    Debug.LogWarning($"LocalizationData: Duplicate LanguageDataEntry found for language '{entry.language}'. Only the first one will be used.");
                }
            }
        }

        _allLanguagesCachedDictionaries.TryGetValue(lang, out Dictionary<string, string> dict);
        return dict;
    }

    /// <summary>
    /// Clears all cached language dictionaries.
    /// This is automatically called in the editor when the asset changes,
    /// but can be called manually if the ScriptableObject data is modified at runtime.
    /// </summary>
    public void ClearAllCaches()
    {
        _allLanguagesCachedDictionaries = null;
        foreach (var entry in languageEntries)
        {
            entry.ClearCache();
        }
    }

#if UNITY_EDITOR
    // Called when the asset is loaded or changed in the editor.
    // This ensures that any changes made in the Inspector invalidate the cache,
    // so the game gets the updated data when it next runs.
    private void OnValidate()
    {
        ClearAllCaches();
    }
#endif
}
```

---

### How to Implement and Use in Unity:

Follow these steps to integrate the LocalizationSystem into your Unity project:

1.  **Create Script Files:**
    *   Create a C# script named `LocalizationSystem.cs` in your Assets folder and paste the code from **Section 1** into it.
    *   Create a C# script named `LocalizationData.cs` in your Assets folder and paste the code from **Section 2** into it.

2.  **Create `LocalizationData` Asset:**
    *   In your Unity Project window, right-click -> `Create` -> `Localization` -> `Localization Data`. Name it something descriptive, like `GameLocalizationData`.
    *   Select this `GameLocalizationData` asset. In the Inspector:
        *   Expand `Language Entries`.
        *   Click the `+` button to add elements for each language you want to support (e.g., English, Spanish, French, German).
        *   For each `Language Data Entry`:
            *   Set the `Language` enum (e.g., `English`).
            *   Expand its `Entries` list. Click `+` to add `LocalizedStringEntry` elements.
            *   For each `LocalizedStringEntry`:
                *   Fill in the `Key` (a unique identifier, e.g., `"GAME_TITLE"`, `"WELCOME_MESSAGE"`, `"PLAY_BUTTON"`).
                *   Fill in the `Value` (the translated string for that key and language).
            *   Repeat this process for all your languages, ensuring each key has a translation for every language.

    **Example `GameLocalizationData` Setup in Inspector:**
    *   **Language Entries (Size: 2)**
        *   **Element 0**
            *   Language: `English`
            *   Entries (Size: 3)
                *   Element 0: Key: `GAME_TITLE`, Value: `My Awesome Game`
                *   Element 1: Key: `WELCOME_MESSAGE`, Value: `Welcome, brave adventurer!`
                *   Element 2: Key: `PLAY_BUTTON`, Value: `Play Game`
        *   **Element 1**
            *   Language: `Spanish`
            *   Entries (Size: 3)
                *   Element 0: Key: `GAME_TITLE`, Value: `Mi Juego Impresionante`
                *   Element 1: Key: `WELCOME_MESSAGE`, Value: `¡Bienvenido, valiente aventurero!`
                *   Element 2: Key: `PLAY_BUTTON`, Value: `Jugar`

3.  **Setup the `LocalizationSystem` in your Scene:**
    *   In your current Unity Scene, create an empty GameObject (e.g., named `_GameManager` or `LocalizationManager`).
    *   Add the `LocalizationSystem.cs` script component to this GameObject.
    *   In the Inspector for the `LocalizationSystem` component:
        *   Drag your `GameLocalizationData` asset from the Project window into the `Localization Data` field.
        *   Select your desired `Default Language` (e.g., `English`).

4.  **Use `LocalizableText` for UI Elements:**
    *   Create a UI Text element using TextMeshPro (e.g., `GameObject` -> `UI` -> `TextMeshPro - Text`). If you haven't already, Unity will prompt you to import the TMP Essentials.
    *   Select the created TextMeshProUGUI GameObject in your Hierarchy.
    *   Add the `LocalizableText.cs` component to it.
    *   In the Inspector for the `LocalizableText` component, enter the `Localization Key` that corresponds to one of your entries in `GameLocalizationData` (e.g., `"GAME_TITLE"`, `"WELCOME_MESSAGE"`, `"PLAY_BUTTON"`).
    *   When you run the game, this TextMeshProUGUI element will display the localized value for the current language. In the editor, it will display `KEY: YOUR_KEY`.

5.  **Switching Languages at Runtime (Example Script):**
    You can create a simple UI (e.g., buttons or a dropdown) to switch languages. Here's an example script:

    ```csharp
    using UnityEngine;
    using UnityEngine.UI; // For Button components
    using TMPro; // For TMP_Dropdown if you use one

    public class LanguageSwitcherUI : MonoBehaviour
    {
        [Header("Buttons for Language Selection")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button spanishButton;
        [SerializeField] private Button frenchButton;
        [SerializeField] private Button germanButton; // Add more as per your Language enum

        [Header("Display Current Language (Optional)")]
        [SerializeField] private TextMeshProUGUI currentLanguageDisplay;

        private void Start()
        {
            // Subscribe button clicks to the SetLanguage method of the LocalizationSystem
            englishButton?.onClick.AddListener(() => LocalizationSystem.Instance.SetLanguage(Language.English));
            spanishButton?.onClick.AddListener(() => LocalizationSystem.Instance.SetLanguage(Language.Spanish));
            frenchButton?.onClick.AddListener(() => LocalizationSystem.Instance.SetLanguage(Language.French));
            germanButton?.onClick.AddListener(() => LocalizationSystem.Instance.SetLanguage(Language.German));

            // Optional: Subscribe to language changes to update a display text
            if (LocalizationSystem.Instance != null)
            {
                LocalizationSystem.Instance.OnLanguageChanged += UpdateCurrentLanguageDisplay;
                UpdateCurrentLanguageDisplay(); // Initial display update
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (LocalizationSystem.Instance != null)
            {
                LocalizationSystem.Instance.OnLanguageChanged -= UpdateCurrentLanguageDisplay;
            }
        }

        // Method to update the UI text displaying the current language
        private void UpdateCurrentLanguageDisplay()
        {
            if (currentLanguageDisplay != null && LocalizationSystem.Instance != null)
            {
                currentLanguageDisplay.text = $"Current Language: {LocalizationSystem.Instance.CurrentLanguage}";
            }
        }

        // Example method for a TMP_Dropdown if you prefer that for language selection
        public void OnLanguageDropdownValueChanged(TMP_Dropdown dropdown)
        {
            // Ensure the dropdown options match the order of your Language enum
            Language selectedLanguage = (Language)dropdown.value;
            LocalizationSystem.Instance.SetLanguage(selectedLanguage);
        }
    }
    ```
    *   Create a new C# script named `LanguageSwitcherUI.cs`, paste the code above, and attach it to an empty GameObject in your scene (e.g., on your UI Canvas).
    *   Create UI Buttons (or a Dropdown) and assign them to the `SerializeField` slots in the `LanguageSwitcherUI` component's Inspector.
    *   Run your game and click the buttons to see the localized text update in real-time!

---

This complete example provides a robust, extensible, and easy-to-use localization system for your Unity projects, adhering to common design patterns and best practices.