// Unity Design Pattern Example: LocalizationCSVSystem
// This script demonstrates the LocalizationCSVSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'LocalizationCSVSystem' is a practical design pattern for managing game localization using CSV (Comma Separated Values) files. It centralizes all translations in a single manager, making it easy to update, add new languages, and integrate with UI elements.

This example provides a complete, ready-to-use system for Unity.

---

### **1. LocalizationManager.cs**

This is the core of the system. It's a `MonoBehaviour` Singleton that loads your CSV data, manages the current language, and provides methods to retrieve localized strings. It also includes an event system to notify UI elements when the language changes.

**How it works:**
*   **Singleton Pattern:** Ensures only one instance of `LocalizationManager` exists throughout the game, making it globally accessible.
*   **CSV Loading:** It takes a `TextAsset` (which Unity can create from a `.csv` file) and parses it. The first row of the CSV is expected to contain language codes (e.g., `KEY,en,es,fr`).
*   **Data Structure:** It stores all translations in a nested dictionary: `Dictionary<string, Dictionary<string, string>>`. The outer dictionary maps a language code (e.g., "en") to another dictionary, which maps a localization key (e.g., "HELLO_WORLD") to its translated string.
*   **Language Switching:** The `SetLanguage` method loads the appropriate translation dictionary and broadcasts an `OnLanguageChanged` event.
*   **String Retrieval:** `GetLocalizedString` retrieves the string for the current language and key. It also supports `string.Format` for dynamic text (e.g., "Score: {0}").
*   **Fallback:** If a key or language is not found, it returns a clear error message instead of crashing.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.IO; // Used for StringReader in TextAsset parsing
using System; // For Action event

namespace LocalizationSystem
{
    /// <summary>
    /// The core Localization Manager responsible for loading, storing, and providing localized strings.
    /// Implements the Singleton pattern for easy global access.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // --- Singleton Instance ---
        public static LocalizationManager Instance { get; private set; }

        // --- Editor-Configurable Fields ---
        [Tooltip("The CSV file containing all localization data.")]
        [SerializeField] private TextAsset localizationCSV;

        [Tooltip("The default language code to use if none is specified or saved.")]
        [SerializeField] private string defaultLanguageCode = "en";

        // --- Private Data Members ---
        // Stores all localization data: LanguageCode -> (Key -> LocalizedString)
        private Dictionary<string, Dictionary<string, string>> allLocalizationData;

        // Stores the currently active localization data: Key -> LocalizedString
        private Dictionary<string, string> currentLanguageData;

        // The code of the currently active language (e.g., "en", "es", "fr")
        private string currentLanguageCode;

        // A list of all available language codes found in the CSV header
        private List<string> availableLanguageCodes;

        // --- Public Properties ---
        public string CurrentLanguageCode => currentLanguageCode;
        public IReadOnlyList<string> AvailableLanguageCodes => availableLanguageCodes.AsReadOnly();

        // --- Events ---
        /// <summary>
        /// Event fired when the language is changed.
        /// UI elements or other components that display localized text should subscribe to this event
        /// to update their content.
        /// </summary>
        public event Action OnLanguageChanged;

        // --- MonoBehaviour Lifecycle ---

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple LocalizationManager instances found. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene loads

            availableLanguageCodes = new List<string>();

            // Load localization data from the CSV file
            LoadLocalizationData(localizationCSV);

            // Set the initial language
            // You might load this from PlayerPrefs in a real game
            string savedLanguage = PlayerPrefs.GetString("SelectedLanguage", defaultLanguageCode);
            SetLanguage(savedLanguage);
        }

        private void OnDestroy()
        {
            // Clear the instance reference if this manager is destroyed
            if (Instance == this)
            {
                Instance = null;
            }
            // IMPORTANT: Unsubscribe all listeners to prevent memory leaks, especially if using DontDestroyOnLoad
            OnLanguageChanged = null;
        }

        // --- Core Localization Logic ---

        /// <summary>
        /// Loads and parses the localization data from the provided TextAsset CSV file.
        /// </summary>
        /// <param name="csvFile">The TextAsset containing CSV localization data.</param>
        private void LoadLocalizationData(TextAsset csvFile)
        {
            if (csvFile == null)
            {
                Debug.LogError("Localization CSV TextAsset is not assigned!", this);
                return;
            }

            allLocalizationData = new Dictionary<string, Dictionary<string, string>>();
            availableLanguageCodes.Clear();

            // Use StringReader to process the CSV text line by line
            using (StringReader reader = new StringReader(csvFile.text))
            {
                string line;
                string[] headers = null; // Stores language codes from the first line

                int lineNum = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNum++;
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = line.Split(',');

                    if (lineNum == 1) // First line is the header (KEY,en,es,fr,...)
                    {
                        if (values.Length < 2 || values[0].ToUpper() != "KEY")
                        {
                            Debug.LogError($"Localization CSV: First line (header) must start with 'KEY,' and have at least one language column. Line: '{line}'", this);
                            return;
                        }
                        headers = values;
                        // Populate available language codes from the header
                        for (int i = 1; i < headers.Length; i++)
                        {
                            string langCode = headers[i].Trim().ToLower();
                            if (!string.IsNullOrEmpty(langCode))
                            {
                                availableLanguageCodes.Add(langCode);
                                allLocalizationData[langCode] = new Dictionary<string, string>(); // Initialize dictionary for each language
                            }
                            else
                            {
                                Debug.LogWarning($"Localization CSV: Empty language code found in header at column {i + 1}.", this);
                            }
                        }
                        if (availableLanguageCodes.Count == 0)
                        {
                            Debug.LogError("Localization CSV: No valid language codes found in the header row!", this);
                            return;
                        }
                    }
                    else // Subsequent lines are actual key-value pairs
                    {
                        if (headers == null)
                        {
                            Debug.LogError("Localization CSV: Data lines found before header line was parsed. This should not happen.", this);
                            return;
                        }
                        if (values.Length < 1) continue; // Skip lines without a key

                        string key = values[0].Trim();
                        if (string.IsNullOrEmpty(key))
                        {
                            Debug.LogWarning($"Localization CSV: Empty key found on line {lineNum}. Skipping line.", this);
                            continue;
                        }

                        // Populate translations for each language
                        for (int i = 1; i < headers.Length; i++)
                        {
                            string langCode = headers[i].Trim().ToLower();
                            string translation = (i < values.Length) ? values[i].Trim() : ""; // Get translation, handle missing columns

                            if (allLocalizationData.TryGetValue(langCode, out var langDict))
                            {
                                if (langDict.ContainsKey(key))
                                {
                                    Debug.LogWarning($"Localization CSV: Duplicate key '{key}' for language '{langCode}' found on line {lineNum}. Overwriting previous value.", this);
                                }
                                langDict[key] = translation;
                            }
                            // else: This language code wasn't in the header, ignore or log error if desired
                        }
                    }
                }
            }

            Debug.Log($"LocalizationManager: Loaded {availableLanguageCodes.Count} languages from CSV. Total keys: " +
                      (allLocalizationData.Count > 0 ? allLocalizationData[availableLanguageCodes[0]].Count.ToString() : "0") + ".");
        }

        /// <summary>
        /// Sets the active language for the application.
        /// </summary>
        /// <param name="languageCode">The code of the language to set (e.g., "en", "es").</param>
        public void SetLanguage(string languageCode)
        {
            string requestedLang = languageCode.Trim().ToLower();

            if (allLocalizationData == null || allLocalizationData.Count == 0)
            {
                Debug.LogError("Localization data not loaded. Cannot set language.", this);
                return;
            }

            if (allLocalizationData.TryGetValue(requestedLang, out var data))
            {
                currentLanguageCode = requestedLang;
                currentLanguageData = data;
                PlayerPrefs.SetString("SelectedLanguage", currentLanguageCode); // Save for next session
                PlayerPrefs.Save(); // Ensure it's written to disk

                Debug.Log($"LocalizationManager: Language set to '{currentLanguageCode}'.");
                OnLanguageChanged?.Invoke(); // Notify subscribers
            }
            else
            {
                Debug.LogWarning($"LocalizationManager: Language code '{requestedLang}' not found. Falling back to default '{defaultLanguageCode}'.", this);
                if (allLocalizationData.TryGetValue(defaultLanguageCode.ToLower(), out var defaultData))
                {
                    currentLanguageCode = defaultLanguageCode.ToLower();
                    currentLanguageData = defaultData;
                    PlayerPrefs.SetString("SelectedLanguage", currentLanguageCode);
                    PlayerPrefs.Save();
                    OnLanguageChanged?.Invoke(); // Notify subscribers
                }
                else
                {
                    Debug.LogError($"LocalizationManager: Default language '{defaultLanguageCode}' not found. No language could be set!", this);
                    currentLanguageData = new Dictionary<string, string>(); // Empty dict to prevent null refs
                    currentLanguageCode = "error";
                }
            }
        }

        /// <summary>
        /// Retrieves the localized string for a given key in the currently active language.
        /// Supports string formatting with `params object[] args`.
        /// </summary>
        /// <param name="key">The localization key (e.g., "HELLO_WORLD", "SCORE_TEXT").</param>
        /// <param name="args">Optional arguments for string.Format, e.g., for "Score: {0}".</param>
        /// <returns>The localized string, or an error message if the key is not found.</returns>
        public string GetLocalizedString(string key, params object[] args)
        {
            if (currentLanguageData == null)
            {
                Debug.LogError("LocalizationManager: No language is currently set or loaded. Returning placeholder.", this);
                return $"NO_LANG_SET: {key}";
            }

            if (currentLanguageData.TryGetValue(key, out string localizedString))
            {
                // Apply string formatting if arguments are provided
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(localizedString, args);
                    }
                    catch (FormatException e)
                    {
                        Debug.LogError($"LocalizationManager: FormatException for key '{key}' in language '{currentLanguageCode}': {e.Message}. String: '{localizedString}'", this);
                        return $"FORMAT_ERROR_{key}";
                    }
                }
                return localizedString;
            }
            else
            {
                Debug.LogWarning($"LocalizationManager: Key '{key}' not found in language '{currentLanguageCode}'. Returning placeholder.", this);
                return $"MISSING_KEY: {key}";
            }
        }
    }
}
```

---

### **2. LocalizedText.cs**

This is an example `MonoBehaviour` script that you attach to a UI Text component. It automatically retrieves and displays the localized string for a specified key and updates itself when the language changes.

**How it works:**
*   **Requires Text Component:** Ensures it's only added to GameObjects with a `Text` or `TextMeshProUGUI` component.
*   **Key Assignment:** You assign a `localizationKey` in the Inspector.
*   **Auto-Update:** It subscribes to `LocalizationManager.OnLanguageChanged` to automatically call `UpdateText` whenever the language is switched.
*   **Initial Update:** `Start()` ensures the text is set correctly on scene load.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Text component
using TMPro; // Optional: If you use TextMeshPro, include this and adapt.
using System; // For EventArgs (though not strictly needed here)

namespace LocalizationSystem
{
    /// <summary>
    /// A component that automatically displays localized text on a UI Text (or TextMeshProUGUI) element.
    /// It subscribes to the LocalizationManager's OnLanguageChanged event to update its text dynamically.
    /// </summary>
    [RequireComponent(typeof(Text))] // Or [RequireComponent(typeof(TextMeshProUGUI))] if using TMP
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("The localization key to retrieve from the LocalizationManager.")]
        [SerializeField] private string localizationKey;

        // Optional: If you want to use string.Format, specify the arguments here.
        // For example, if localizationKey is "PLAYER_SCORE" and the string is "Score: {0}",
        // you might pass a player's score here.
        // For simplicity, this example doesn't expose arguments directly in the inspector
        // but shows how you'd pass them. You'd typically set these dynamically from code.
        // private object[] formatArguments;

        private Text uiText; // Or TextMeshProUGUI tmProText;

        private void Awake()
        {
            uiText = GetComponent<Text>(); // Or tmProText = GetComponent<TextMeshProUGUI>();
            if (uiText == null)
            {
                Debug.LogError($"LocalizedText on {gameObject.name}: No Text component found!", this);
                enabled = false; // Disable component if no Text is found
            }
        }

        private void OnEnable()
        {
            // Subscribe to the language change event
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            }
            else
            {
                Debug.LogWarning("LocalizedText: LocalizationManager.Instance is null. Cannot subscribe to language change event. Make sure LocalizationManager is initialized.", this);
            }
        }

        private void Start()
        {
            // Set initial text on start, in case the manager was initialized before this component's OnEnable
            UpdateText();
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks and unnecessary calls
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
            }
        }

        /// <summary>
        /// Retrieves the localized string from the LocalizationManager and updates the UI Text component.
        /// This method is called automatically when the language changes.
        /// </summary>
        public void UpdateText()
        {
            if (uiText == null || string.IsNullOrEmpty(localizationKey) || LocalizationManager.Instance == null)
            {
                // Don't log error if key is empty, as it might be intentionally left for dynamic setting
                if (uiText != null)
                {
                    if (string.IsNullOrEmpty(localizationKey))
                    {
                        uiText.text = "NO_KEY";
                    }
                    else if (LocalizationManager.Instance == null)
                    {
                        uiText.text = "NO_LOCALIZATION_MANAGER";
                    }
                }
                return;
            }

            // Example of passing format arguments dynamically.
            // For a simple text, you might not need args.
            // For "Score: {0}", you'd call: LocalizationManager.Instance.GetLocalizedString(localizationKey, someScoreVariable);
            // This example doesn't have a direct way to set args from inspector,
            // but you could add a public method to set them.
            uiText.text = LocalizationManager.Instance.GetLocalizedString(localizationKey);
        }

        /// <summary>
        /// Public method to set the localization key dynamically from other scripts.
        /// </summary>
        /// <param name="key">The new localization key.</param>
        /// <param name="refresh">If true, the text will be updated immediately.</param>
        public void SetLocalizationKey(string key, bool refresh = true)
        {
            localizationKey = key;
            if (refresh)
            {
                UpdateText();
            }
        }

        // Example: How you might set text with dynamic arguments
        public void SetLocalizedTextWithArgs(string key, params object[] args)
        {
            localizationKey = key; // Update the key
            if (uiText != null && LocalizationManager.Instance != null)
            {
                uiText.text = LocalizationManager.Instance.GetLocalizedString(key, args);
            }
        }
    }
}
```

---

### **3. LanguageSwitcher.cs (Example UI Script)**

This script demonstrates how to interact with the `LocalizationManager` to change languages, typically via UI buttons.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button component
using System.Collections.Generic; // For List

namespace LocalizationSystem
{
    /// <summary>
    /// An example script to demonstrate how to switch languages using UI buttons.
    /// You would typically attach this to a canvas or a UI panel.
    /// </summary>
    public class LanguageSwitcher : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The parent transform where language buttons will be instantiated.")]
        [SerializeField] private Transform buttonParent;
        [Tooltip("A prefab for a button that will switch to a specific language.")]
        [SerializeField] private Button languageButtonPrefab;

        private void Start()
        {
            // Clear existing buttons if any (e.g., from editor setup)
            foreach (Transform child in buttonParent)
            {
                Destroy(child.gameObject);
            }

            if (LocalizationManager.Instance == null)
            {
                Debug.LogError("LanguageSwitcher: LocalizationManager.Instance is null. Cannot create language buttons.", this);
                return;
            }

            // Create a button for each available language
            foreach (string langCode in LocalizationManager.Instance.AvailableLanguageCodes)
            {
                if (languageButtonPrefab != null)
                {
                    Button newButton = Instantiate(languageButtonPrefab, buttonParent);
                    newButton.GetComponentInChildren<Text>().text = langCode.ToUpper(); // Display language code
                    string currentLangCode = langCode; // Capture for lambda
                    newButton.onClick.AddListener(() => OnLanguageButtonClicked(currentLangCode));
                }
                else
                {
                    Debug.LogWarning("LanguageSwitcher: Language button prefab is not assigned.", this);
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a language button is clicked.
        /// </summary>
        /// <param name="languageCode">The code of the language to switch to.</param>
        private void OnLanguageButtonClicked(string languageCode)
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(languageCode);
                Debug.Log($"LanguageSwitcher: Switched to language: {languageCode}");
            }
        }
    }
}
```

---

### **4. Example Localization.csv File**

Create a file named `Localization.csv` (or whatever you name your `TextAsset` in `LocalizationManager`) in your Unity project's `Assets` folder.
**Important:** Ensure the file is saved with UTF-8 encoding if you have special characters.

```csv
KEY,en,es,fr,de
GAME_TITLE,My Awesome Game,Mi Juego Impresionante,Mon Jeu Génial,Mein Tolles Spiel
HELLO_WORLD,Hello World!,¡Hola Mundo!,Bonjour le monde!,Hallo Welt!
PLAYER_SCORE,Score: {0},Puntuación: {0},Score: {0},Punktestand: {0}
START_BUTTON,Start Game,Iniciar Juego,Commencer le jeu,Spiel Starten
QUIT_BUTTON,Quit Game,Salir del Juego,Quitter le jeu,Spiel Beenden
SETTINGS_MENU,Settings,Ajustes,Paramètres,Einstellungen
WELCOME_MESSAGE,Welcome to our game!,¡Bienvenido a nuestro juego!,Bienvenue dans notre jeu !,Willkommen in unserem Spiel!
DESCRIPTION,This is a simple description of the game. It uses multiple lines.,Esta es una descripción simple del juego. Utiliza varias líneas.,Ceci est une description simple du jeu. Il utilise plusieurs lignes.,Dies ist eine einfache Beschreibung des Spiels. Es verwendet mehrere Zeilen.
```

**CSV Structure Notes:**
*   **Header Row:** The very first row must contain `KEY` in the first column, followed by your language codes (e.g., `en`, `es`, `fr`). These codes should be unique and descriptive.
*   **Keys:** The first column of every subsequent row is the unique localization key (e.g., `GAME_TITLE`). Use uppercase and underscores for readability.
*   **Translations:** Each subsequent column in a row contains the translation for the corresponding language code in the header.
*   **String Formatting:** Use `{0}`, `{1}`, etc., in your translations if you intend to use `string.Format` with `GetLocalizedString`. The `PLAYER_SCORE` example shows this.
*   **Commas in Text:** If a translation needs to contain a comma, you generally need to enclose the entire field in double quotes (e.g., `"Hello, World!"`). This example's parser handles basic comma separation, but for complex CSV parsing, a dedicated CSV library might be better. For simplicity and avoiding issues, try to avoid commas in translations, or structure your CSV carefully.

---

### **Unity Project Setup Steps:**

1.  **Create C# Scripts:**
    *   Create a new folder in your `Assets` (e.g., `Assets/Scripts/Localization`).
    *   Create three new C# scripts: `LocalizationManager.cs`, `LocalizedText.cs`, and `LanguageSwitcher.cs`.
    *   Copy and paste the code for each script into its respective file.
    *   Ensure the `namespace LocalizationSystem` is consistent across all files.

2.  **Create CSV File:**
    *   Create a new file in your `Assets` folder (e.g., `Assets/Localization/Localization.csv`). You can do this by right-clicking in the Project window -> Create -> Text Document, then renaming it to `Localization.csv`.
    *   Copy and paste the example CSV content into this file.
    *   Select `Localization.csv` in the Project window. In the Inspector, ensure "Asset Type" is `TextAsset`.

3.  **Setup LocalizationManager GameObject:**
    *   Create an empty GameObject in your scene (Right-click in Hierarchy -> Create Empty).
    *   Rename it to `LocalizationManager`.
    *   Add the `LocalizationManager` script component to this GameObject.
    *   In the Inspector of the `LocalizationManager` GameObject:
        *   Drag your `Localization.csv` file from the Project window into the `Localization CSV` field.
        *   You can set `Default Language Code` (e.g., `en`).

4.  **Setup UI Text Elements:**
    *   Create a UI Text element (Right-click in Hierarchy -> UI -> Text - Legacy or TextMeshPro - Text). If using TextMeshPro, make sure you import TMP Essentials.
    *   Rename it to something descriptive (e.g., `GameTitleText`).
    *   Add the `LocalizedText` script component to this UI Text GameObject.
    *   In the Inspector of the `GameTitleText` GameObject:
        *   Set the `Localization Key` field to one of your keys from the CSV, e.g., `GAME_TITLE`.
    *   Repeat this for other texts, e.g., a `WelcomeMessageText` with `WELCOME_MESSAGE` key, and a `PlayerScoreText` with `PLAYER_SCORE` key.

5.  **Setup Language Switcher UI:**
    *   Create a UI Panel (Right-click in Hierarchy -> UI -> Panel) or an empty GameObject to act as a container for your language buttons.
    *   Rename it to `LanguageSelectionPanel`.
    *   Add the `LanguageSwitcher` script component to this GameObject.
    *   Create a UI Button Prefab:
        *   Create a UI Button (Right-click in Hierarchy -> UI -> Button - Legacy).
        *   Adjust its appearance as desired.
        *   Drag this Button GameObject from the Hierarchy into your Project window (e.g., `Assets/Prefabs`) to create a Prefab. Delete the button from the Hierarchy.
    *   In the Inspector of `LanguageSelectionPanel`:
        *   Drag the `LanguageSelectionPanel` itself into the `Button Parent` field.
        *   Drag your Button Prefab from the Project window into the `Language Button Prefab` field.

6.  **Run the Scene:**
    *   Press Play. You should see your UI Text elements displaying the localized text in your default language.
    *   Buttons should appear on your `LanguageSelectionPanel` for each language found in your CSV.
    *   Clicking these buttons should change the language, and all `LocalizedText` components in the scene will automatically update!

---

This complete system provides a robust and flexible way to handle localization in your Unity projects, adhering to common design patterns and best practices.