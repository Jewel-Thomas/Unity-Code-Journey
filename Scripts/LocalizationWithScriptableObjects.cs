// Unity Design Pattern Example: LocalizationWithScriptableObjects
// This script demonstrates the LocalizationWithScriptableObjects pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the "LocalizationWithScriptableObjects" design pattern in Unity, providing a flexible and scalable way to manage your game's localized text.

**Pattern Overview:**

1.  **`Language` Enum:** Defines all supported languages.
2.  **`LocalizationData` (ScriptableObject):**
    *   An asset that holds *all* localized strings for a *single specific language*.
    *   Contains a list of `LocalizedStringEntry` structs, each mapping a unique `stringId` (e.g., "GREETING", "BUTTON_START") to its translated `textValue`.
    *   Uses an internal `Dictionary` for efficient string lookup.
3.  **`LocalizationManager` (Singleton MonoBehaviour):**
    *   The central hub for localization.
    *   Holds references to all `LocalizationData` assets.
    *   Manages the `CurrentLanguage`.
    *   Provides a `GetLocalizedString` method to retrieve text for the `CurrentLanguage`.
    *   Fires an `OnLanguageChanged` event whenever the language is switched.
4.  **`LocalizedText` (MonoBehaviour):**
    *   A component attached to `TextMeshProUGUI` (or `Text`) elements.
    *   Takes a `stringId` as input.
    *   Subscribes to `LocalizationManager.OnLanguageChanged` to automatically update its text when the language changes.
    *   Retrieves the appropriate text from `LocalizationManager.Instance.GetLocalizedString()`.
5.  **`LanguageSwitcher` (Example MonoBehaviour):**
    *   A demonstration component showing how to interact with the `LocalizationManager` to change languages, typically via UI buttons or a dropdown.

---

### **How to Use This in Your Unity Project:**

1.  **Create Folders:** Create `Scripts/Localization` and `Data/Localization` folders in your Unity Project window.
2.  **Copy Scripts:**
    *   Copy the `Language.cs`, `LocalizationData.cs`, `LocalizationManager.cs`, `LocalizedText.cs`, and `LanguageSwitcher.cs` files into your `Scripts/Localization` folder.
    *   Make sure you have TextMeshPro imported into your project (`Window > TextMeshPro > Import TMP Essential Resources`).
3.  **Create LocalizationData Assets:**
    *   Go to `Assets > Create > Localization > Localization Data`.
    *   Create one `LocalizationData` asset for each language you support (e.g., "LocalizationData_English", "LocalizationData_Spanish").
    *   In each `LocalizationData` asset, set its `Language` field (e.g., English, Spanish).
    *   Populate the `Entries` list with your `stringId` and `textValue` pairs.
        *   **Example for `LocalizationData_English`:**
            *   `stringId`: "GREETING" | `textValue`: "Hello, World!"
            *   `stringId`: "BUTTON_START" | `textValue`: "Start Game"
        *   **Example for `LocalizationData_Spanish`:**
            *   `stringId`: "GREETING" | `textValue`: "¡Hola, Mundo!"
            *   `stringId`: "BUTTON_START" | `textValue`: "Iniciar Juego"
4.  **Set up `LocalizationManager`:**
    *   Create an empty GameObject in your first scene (or a persistent scene) and name it `_LocalizationManager`.
    *   Add the `LocalizationManager` script component to it.
    *   Drag and drop all your `LocalizationData` assets (e.g., "LocalizationData_English", "LocalizationData_Spanish") into the `All Localization Data` list in the Inspector.
    *   Set your `Default Language` (e.g., English).
5.  **Use `LocalizedText` for UI Elements:**
    *   For any `TextMeshProUGUI` element that needs localization, add the `LocalizedText` script component to the same GameObject.
    *   In the `LocalizedText` component, enter the `String Id` that corresponds to your `LocalizationData` assets (e.g., "GREETING", "BUTTON_START").
6.  **Create a Language Switcher (Optional, for testing/game options):**
    *   Create a UI Canvas.
    *   Add buttons for each language you want to switch to, or a `TMP_Dropdown` (UI/Dropdown - TextMeshPro).
    *   Create an empty GameObject (e.g., `LanguageSwitcherUI`) and add the `LanguageSwitcher` script to it.
    *   Link your dropdown or buttons to the appropriate methods in `LanguageSwitcher` (e.g., `SetLanguageToEnglish`, `SetLanguageToSpanish`).

---

### **1. `Language.cs`**
Defines the enum for all supported languages.

```csharp
using UnityEngine; // Required for Unity's inspector to recognize serializable fields correctly

namespace MyGame.Localization
{
    /// <summary>
    /// Defines the enumeration of all supported languages in the game.
    /// New languages should be added here.
    /// </summary>
    public enum Language
    {
        English,
        Spanish,
        French,
        German,
        // Add more languages as needed
        // The order here can influence dropdowns, but typically doesn't matter for core logic.
    }
}
```

---

### **2. `LocalizationData.cs`**
A ScriptableObject to hold all localized strings for a single language.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For dictionary conversion (ToDictionary)
using System; // For [Serializable]

namespace MyGame.Localization
{
    /// <summary>
    /// Represents a single localized string entry, mapping a unique ID to its translated text.
    /// This struct is used within the LocalizationData ScriptableObject.
    /// </summary>
    [Serializable] // Makes this struct visible and editable in the Unity Inspector
    public struct LocalizedStringEntry
    {
        [Tooltip("A unique identifier for the string (e.g., 'GREETING_MESSAGE', 'BUTTON_START').")]
        public string stringId; 

        [Tooltip("The actual translated text for this stringId in the specific language.")]
        [TextArea(1, 5)] // Makes the text field multi-line in the Inspector for easier editing
        public string textValue; 
    }

    /// <summary>
    /// ScriptableObject that acts as a data container for all localized strings
    /// pertaining to a specific language. You create one of these assets for each language.
    /// 
    /// This is the core data asset for the LocalizationWithScriptableObjects pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationData_NewLanguage", menuName = "Localization/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [Tooltip("The language this data asset contains translations for.")]
        public Language language;

        [Tooltip("A list of all localized string entries for this language.")]
        public List<LocalizedStringEntry> entries = new List<LocalizedStringEntry>();

        // A private dictionary for faster lookup of strings by their ID at runtime.
        // It's built from the 'entries' list to optimize performance.
        private Dictionary<string, string> _localizedStrings;

        /// <summary>
        /// Called when the ScriptableObject is loaded or enabled.
        /// Builds the internal dictionary for fast string lookups.
        /// </summary>
        private void OnEnable()
        {
            BuildDictionary();
        }

        /// <summary>
        /// Called in the editor when the script is loaded or a value is changed in the Inspector.
        /// This ensures the dictionary is always up-to-date during development.
        /// </summary>
        private void OnValidate()
        {
            BuildDictionary();
        }

        /// <summary>
        /// Rebuilds the internal dictionary from the 'entries' list.
        /// This method is called whenever the data asset is loaded or changed in the editor,
        /// ensuring the dictionary is always fresh.
        /// </summary>
        public void BuildDictionary()
        {
            // Clear existing dictionary to prevent old data or duplicates if entries were removed/changed
            _localizedStrings = new Dictionary<string, string>();

            // Populate the dictionary from the serializable list of entries.
            // Using ToDictionary directly ensures no duplicate keys.
            // If duplicate stringIds are present in 'entries', ToDictionary will throw an error.
            // A more robust approach might be to check for duplicates and log warnings.
            foreach (var entry in entries)
            {
                if (_localizedStrings.ContainsKey(entry.stringId))
                {
                    Debug.LogWarning($"LocalizationData: Duplicate string ID '{entry.stringId}' found for language '{language}'. " +
                                     $"Only the first occurrence will be used. Please ensure unique string IDs.", this);
                }
                else
                {
                    _localizedStrings.Add(entry.stringId, entry.textValue);
                }
            }
        }

        /// <summary>
        /// Retrieves the localized string for a given string ID.
        /// </summary>
        /// <param name="stringId">The unique identifier for the string (e.g., "GREETING").</param>
        /// <returns>The translated string, or a clear warning message if the ID is not found.</returns>
        public string GetLocalizedString(string stringId)
        {
            // Ensure the dictionary is built before attempting to retrieve.
            // This is a safeguard if OnEnable/OnValidate wasn't called (e.g., due to script compilation issues).
            if (_localizedStrings == null || _localizedStrings.Count == 0 && entries.Count > 0)
            {
                BuildDictionary();
            }

            if (_localizedStrings.TryGetValue(stringId, out string result))
            {
                return result;
            }

            // Log a warning if the string ID is not found in this language's data.
            Debug.LogWarning($"Localization Data for '{language}' does not contain string ID: '{stringId}'");
            return $"MISSING_STRING_ID: {stringId}"; // Return a placeholder for missing strings for easier debugging
        }
    }
}
```

---

### **3. `LocalizationManager.cs`**
The central manager (Singleton) for handling language selection and string retrieval.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // For dictionary conversion (ToDictionary)

namespace MyGame.Localization
{
    /// <summary>
    /// The central manager for handling localization in the game.
    /// It is implemented as a Singleton to provide easy global access from anywhere.
    /// It manages the current active language and retrieves localized strings.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // --- Singleton Instance ---
        /// <summary>
        /// The static instance of the LocalizationManager, ensuring only one exists.
        /// </summary>
        public static LocalizationManager Instance { get; private set; }

        // --- Configuration Fields ---
        [Tooltip("The default language to load when the game starts.")]
        [SerializeField] private Language _defaultLanguage = Language.English;

        [Tooltip("List of all LocalizationData ScriptableObjects, one for each supported language. " +
                 "Drag your LocalizationData assets here.")]
        [SerializeField] private List<LocalizationData> _allLocalizationData = new List<LocalizationData>();

        // --- Private State ---
        private Language _currentLanguage; // The currently active language
        private LocalizationData _currentLocalizationData; // The LocalizationData SO for the current language
        
        // A dictionary for quick lookup of LocalizationData assets by their Language enum.
        private Dictionary<Language, LocalizationData> _languageToDataMap; 

        // --- Events ---
        /// <summary>
        /// An event fired whenever the language changes.
        /// Components like LocalizedText should subscribe to this to update their content.
        /// </summary>
        public static event Action OnLanguageChanged;

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            // Implement Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("LocalizationManager: Another instance already exists. Destroying this duplicate.", this);
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                // Prevent this GameObject from being destroyed when loading new scenes.
                // This ensures the localization state persists across the game.
                DontDestroyOnLoad(gameObject); 

                InitializeManager();
            }
        }

        /// <summary>
        /// Initializes the localization manager:
        /// 1. Builds an internal map for fast lookup of LocalizationData by Language.
        /// 2. Sets the initial language based on '_defaultLanguage'.
        /// </summary>
        private void InitializeManager()
        {
            // Build the dictionary for quick lookup of LocalizationData by Language enum.
            // This prevents iterating through the list every time we switch languages.
            _languageToDataMap = _allLocalizationData.ToDictionary(data => data.language, data => data);

            // Set the initial language using the configured default.
            SetLanguage(_defaultLanguage);
        }

        // --- Public API ---
        /// <summary>
        /// Gets the current active language.
        /// </summary>
        public Language CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Sets the active language for the game.
        /// This will update the internal localization data and notify all subscribed components.
        /// </summary>
        /// <param name="newLanguage">The language to switch to.</param>
        public void SetLanguage(Language newLanguage)
        {
            // Only proceed if the language is actually changing or if no data is currently loaded.
            if (_currentLanguage == newLanguage && _currentLocalizationData != null)
            {
                return; // Language is already set, no action needed.
            }

            // Try to retrieve the LocalizationData for the new language from our map.
            if (_languageToDataMap.TryGetValue(newLanguage, out LocalizationData data))
            {
                _currentLanguage = newLanguage;
                _currentLocalizationData = data;
                Debug.Log($"LocalizationManager: Language set to {newLanguage}");

                // Invoke the event to notify all listeners (e.g., LocalizedText components)
                // that they should update their displayed text.
                OnLanguageChanged?.Invoke();
            }
            else
            {
                Debug.LogError($"LocalizationManager: No LocalizationData found for language: {newLanguage}. " +
                               "Please ensure you have created and assigned a LocalizationData ScriptableObject for this language " +
                               "in the LocalizationManager's '_allLocalizationData' list.", this);
            }
        }

        /// <summary>
        /// Retrieves the localized string for a given string ID in the current active language.
        /// </summary>
        /// <param name="stringId">The unique identifier for the string (e.g., "GREETING").</param>
        /// <returns>The translated string, or a warning message if the ID is not found or no language data is loaded.</returns>
        public string GetLocalizedString(string stringId)
        {
            if (_currentLocalizationData == null)
            {
                Debug.LogError("LocalizationManager: No current localization data loaded. " +
                               "Has SetLanguage been called successfully? Is the manager properly initialized?", this);
                return $"ERROR: NO_LANG_DATA_LOADED for ID: {stringId}";
            }

            // Delegate the actual string lookup to the current LocalizationData asset.
            return _currentLocalizationData.GetLocalizedString(stringId);
        }

        /// <summary>
        /// Returns a list of all available languages that have associated LocalizationData assets.
        /// Useful for populating UI elements like language selection dropdowns.
        /// </summary>
        public List<Language> GetAvailableLanguages()
        {
            return _allLocalizationData.Select(data => data.language).ToList();
        }
    }
}
```

---

### **4. `LocalizedText.cs`**
A MonoBehaviour that attaches to UI Text elements to display localized text.

```csharp
using UnityEngine;
using TMPro; // Using TextMeshPro for modern Unity UI text components

namespace MyGame.Localization
{
    /// <summary>
    /// A MonoBehaviour component that automatically displays localized text
    /// by fetching it from the LocalizationManager.
    /// It subscribes to language change events and updates its text content accordingly.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))] // Ensures this component is always on an object with a TextMeshPro component
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("The unique ID of the string to display (e.g., 'GREETING_MESSAGE', 'BUTTON_START'). " +
                 "This ID must exist in your LocalizationData assets.")]
        [SerializeField] private string _stringId;

        private TMP_Text _tmpTextComponent; // Reference to the TextMeshPro component on this GameObject

        /// <summary>
        /// Gets the string ID this component is currently set to display.
        /// </summary>
        public string StringId => _stringId;

        /// <summary>
        /// Sets a new string ID for this component and updates its text immediately.
        /// Useful for dynamic text that changes during runtime (e.g., a dialogue line).
        /// </summary>
        /// <param name="newStringId">The new unique ID of the string to display.</param>
        public void SetStringId(string newStringId)
        {
            if (_stringId != newStringId)
            {
                _stringId = newStringId;
                UpdateText(); // Update immediately when the ID changes
            }
        }

        // --- MonoBehaviour Lifecycle ---
        private void Awake()
        {
            // Get the TextMeshPro component once when the object wakes up.
            _tmpTextComponent = GetComponent<TMP_Text>();
            if (_tmpTextComponent == null)
            {
                Debug.LogError("LocalizedText: No TMP_Text component found on this GameObject. " +
                               "This component requires one to display text.", this);
                enabled = false; // Disable this script if its dependency is missing.
            }
        }

        private void OnEnable()
        {
            // Subscribe to the language changed event. When the language switches,
            // the UpdateText method will be called to refresh this component's text.
            LocalizationManager.OnLanguageChanged += UpdateText;
            UpdateText(); // Perform an initial update when the component is enabled or scene loads.
        }

        private void OnDisable()
        {
            // Unsubscribe from the event when disabled to prevent memory leaks or
            // trying to call on a destroyed object (important for clean up).
            LocalizationManager.OnLanguageChanged -= UpdateText;
        }

        /// <summary>
        /// Retrieves the localized string from the LocalizationManager using the '_stringId'
        /// and updates the TextMeshPro component's text with the result.
        /// </summary>
        public void UpdateText()
        {
            if (_tmpTextComponent == null || string.IsNullOrEmpty(_stringId))
            {
                // Nothing to update if no text component or no string ID is set.
                return; 
            }

            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning($"LocalizedText: No LocalizationManager.Instance found while trying to update text " +
                                 $"for ID '{_stringId}'. Is the manager in the scene and initialized?", this);
                _tmpTextComponent.text = $"[No Manager] {_stringId}"; // Display a placeholder
                return;
            }

            // Fetch the localized string from the manager and apply it to the TextMeshPro component.
            _tmpTextComponent.text = LocalizationManager.Instance.GetLocalizedString(_stringId);
        }

        /// <summary>
        /// Called in the editor when the script is loaded or a value is changed in the Inspector.
        /// This allows for some real-time feedback during development.
        /// </summary>
        private void OnValidate()
        {
            // Ensure we have a reference to the TMP_Text component.
            if (_tmpTextComponent == null)
            {
                _tmpTextComponent = GetComponent<TMP_Text>();
            }

            // Only attempt to update the text if the application is playing,
            // as LocalizationManager.Instance is only reliably available then.
            // In editor mode, we'll just show the string ID as a placeholder for design,
            // which gives immediate feedback on what ID is assigned.
            if (Application.isPlaying)
            {
                UpdateText();
            }
            else
            {
                if (_tmpTextComponent != null)
                {
                    _tmpTextComponent.text = string.IsNullOrEmpty(_stringId) ? "[No String ID]" : $"[ID: {_stringId}]";
                }
            }
        }
    }
}
```

---

### **5. `LanguageSwitcher.cs`**
An example UI component to demonstrate changing the language.

```csharp
using UnityEngine;
using UnityEngine.UI; // For old UI Toggle, Button if needed
using TMPro; // For TextMeshPro Dropdown and Text
using System.Linq; // For LINQ operations like Select, ToList
using System.Collections.Generic; // For List

namespace MyGame.Localization
{
    /// <summary>
    /// An example UI component that demonstrates how to interact with the
    /// LocalizationManager to switch the active language.
    /// This can be used for a language selection screen or a debug tool.
    /// </summary>
    public class LanguageSwitcher : MonoBehaviour
    {
        [Tooltip("Optional: Reference to a TMP_Dropdown to select the language. " +
                 "If assigned, the dropdown will be populated with available languages.")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Tooltip("Optional: Reference to a TMP_Text to display the current language. " +
                 "This will update automatically when the language changes.")]
        [SerializeField] private TMP_Text _currentLanguageText;

        private List<Language> _availableLanguages; // Cache of languages supported by LocalizationManager

        private void Start()
        {
            // Ensure the LocalizationManager exists before trying to interact with it.
            if (LocalizationManager.Instance == null)
            {
                Debug.LogError("LanguageSwitcher: LocalizationManager.Instance not found. " +
                               "Please ensure a LocalizationManager GameObject exists in your scene.", this);
                enabled = false; // Disable this component if manager is missing.
                return;
            }

            InitializeDropdown(); // Populate the dropdown if one is assigned.
            UpdateCurrentLanguageDisplay(); // Set the initial display of the current language.

            // Subscribe to language changes so this switcher can update its display
            // if the language is changed by another source (e.g., another UI button).
            LocalizationManager.OnLanguageChanged += UpdateCurrentLanguageDisplay;
        }

        private void OnDestroy()
        {
            // Unsubscribe from the event to prevent potential NullReferenceExceptions
            // if the LocalizationManager outlives this switcher object.
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.OnLanguageChanged -= UpdateCurrentLanguageDisplay;
            }
        }

        /// <summary>
        /// Initializes the dropdown UI component with the languages available
        /// in the LocalizationManager.
        /// </summary>
        private void InitializeDropdown()
        {
            if (_languageDropdown == null) return; // Only proceed if a dropdown is assigned.

            _availableLanguages = LocalizationManager.Instance.GetAvailableLanguages();

            _languageDropdown.ClearOptions(); // Remove any default options.

            // Convert the list of Language enums to a list of strings for the dropdown options.
            _languageDropdown.AddOptions(_availableLanguages.Select(lang => lang.ToString()).ToList());

            // Set the dropdown's selected value to match the current active language.
            int currentIndex = _availableLanguages.IndexOf(LocalizationManager.Instance.CurrentLanguage);
            if (currentIndex >= 0)
            {
                _languageDropdown.value = currentIndex;
            }

            // Add a listener to handle when the user changes the dropdown selection.
            _languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        /// <summary>
        /// Callback method for when the dropdown's selected value changes.
        /// It calls the LocalizationManager to switch to the newly selected language.
        /// </summary>
        /// <param name="index">The index of the selected option in the dropdown.</param>
        private void OnDropdownValueChanged(int index)
        {
            if (index >= 0 && index < _availableLanguages.Count)
            {
                // Set the new language via the LocalizationManager.
                // This will trigger the OnLanguageChanged event and update all LocalizedText components.
                LocalizationManager.Instance.SetLanguage(_availableLanguages[index]);
            }
        }

        /// <summary>
        /// Updates the optional current language text display.
        /// This method is subscribed to the OnLanguageChanged event.
        /// </summary>
        private void UpdateCurrentLanguageDisplay()
        {
            if (_currentLanguageText != null && LocalizationManager.Instance != null)
            {
                _currentLanguageText.text = $"Current Language: {LocalizationManager.Instance.CurrentLanguage}";
            }
        }

        // --- Example Methods for Buttons (can be hooked up in Inspector) ---
        public void SetLanguageToEnglish()
        {
            LocalizationManager.Instance?.SetLanguage(Language.English);
        }

        public void SetLanguageToSpanish()
        {
            LocalizationManager.Instance?.SetLanguage(Language.Spanish);
        }

        public void SetLanguageToFrench()
        {
            LocalizationManager.Instance?.SetLanguage(Language.French);
        }
        // Add more methods for other languages as needed, or use a generic one
        // public void SetLanguage(int languageIndex) { LocalizationManager.Instance?.SetLanguage(_availableLanguages[languageIndex]); }
    }
}
```