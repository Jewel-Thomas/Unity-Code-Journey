// Unity Design Pattern Example: UnlockableSystem
// This script demonstrates the UnlockableSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Unlockable System' design pattern in Unity provides a robust way to manage items, features, or content that can be unlocked by players through gameplay, achievements, purchases, or other conditions. It separates the definition of an unlockable item (its static data) from its runtime state (whether it's unlocked or not) and provides a centralized manager for all unlockable logic, including persistence.

Here's a complete, practical C# Unity example demonstrating this pattern:

---

### 1. `UnlockableData.cs` (ScriptableObject)

This script defines the static data for an unlockable item. We use a `ScriptableObject` so that you can create these unlockable definitions as assets directly in the Unity editor, making the system data-driven.

```csharp
// UnlockableData.cs
using UnityEngine;
using System.Collections.Generic; // Not strictly needed here, but good practice for ScriptableObjects.

/// <summary>
/// Represents the static definition of an unlockable item.
/// This is a ScriptableObject, allowing us to create concrete unlockable items
/// as assets in the Unity editor. This separates the item's data from its
/// runtime state (unlocked/locked), which is managed by the UnlockableSystemManager.
/// </summary>
[CreateAssetMenu(fileName = "NewUnlockable", menuName = "Unlockable System/Unlockable Data")]
public class UnlockableData : ScriptableObject
{
    [Tooltip("A unique identifier for this unlockable. This is crucial for saving and loading states.")]
    [SerializeField] private string id;

    [Tooltip("The display name for this unlockable item.")]
    [SerializeField] private string displayName;

    [Tooltip("A brief description of the unlockable item.")]
    [SerializeField] private string description;

    [Tooltip("An optional icon to represent the unlockable item in UI.")]
    [SerializeField] private Sprite icon;

    [Tooltip("An optional cost associated with unlocking this item (e.g., in-game currency).")]
    [SerializeField] private int cost;

    // Public properties to access the data.
    // The 'id' setter is internal to allow the manager to potentially assign IDs if needed
    // or to perform validation during asset creation, but generally, IDs are set in editor.
    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int Cost => cost;

    // Optional: Validate the ID on asset creation or modification.
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError($"UnlockableData asset '{name}' has an empty or null ID. Please assign a unique ID.", this);
            // In a real project, you might enforce a unique ID check across all assets here,
            // but for simplicity, we'll let the manager handle duplicate ID warnings during initialization.
        }
    }
}
```

---

### 2. `UnlockableSystemManager.cs` (MonoBehaviour)

This is the core of the Unlockable System. It's a `MonoBehaviour` that will be attached to a GameObject in your scene. It acts as a singleton for easy global access and manages the unlocked status and persistence for all `UnlockableData` items.

```csharp
// UnlockableSystemManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToDictionary() and other LINQ operations
// Note: We're using Unity's built-in JsonUtility with a custom SerializableDictionary
// to avoid external dependencies like Newtonsoft.Json for a drop-in example.

/// <summary>
/// This is the core of the Unlockable System design pattern.
/// It acts as a centralized manager for all unlockable items in the game.
///
/// Key responsibilities:
/// 1.  Holds references to all possible unlockable items (definitions via ScriptableObjects).
/// 2.  Manages the *runtime state* (whether an item is unlocked or not).
/// 3.  Provides methods to unlock items, check their status, and retrieve their data.
/// 4.  Handles persistence: saving and loading the unlocked states.
/// 5.  Uses the Singleton pattern for easy global access.
/// </summary>
public class UnlockableSystemManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global point of access to the UnlockableSystemManager.
    // Ensures there is only one instance of the manager throughout the game.
    public static UnlockableSystemManager Instance { get; private set; }

    [Header("Unlockable Definitions")]
    [Tooltip("Drag all your UnlockableData ScriptableObjects here. These define all possible unlockables.")]
    [SerializeField] private List<UnlockableData> allUnlockableDefinitions = new List<UnlockableData>();

    // Stores the runtime state of each unlockable: Key = Unlockable ID, Value = true if unlocked, false if locked.
    private Dictionary<string, bool> unlockedStates = new Dictionary<string, bool>();

    // --- Persistence Keys ---
    // Keys used for PlayerPrefs to save/load data.
    private const string UNLOCKED_STATES_KEY = "UnlockableSystem_UnlockedStates";

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement Singleton pattern logic
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UnlockableSystemManager: Another instance found, destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Keep the manager alive across scene changes if it manages global game state.
        DontDestroyOnLoad(gameObject);

        InitializeSystem();
    }

    /// <summary>
    /// Initializes the Unlockable System by loading saved states or setting defaults.
    /// </summary>
    private void InitializeSystem()
    {
        // First, populate the unlockedStates dictionary with all known unlockables,
        // assuming they are initially locked.
        unlockedStates.Clear(); // Ensure a clean dictionary on initialization
        foreach (UnlockableData unlockable in allUnlockableDefinitions)
        {
            if (string.IsNullOrWhiteSpace(unlockable.Id))
            {
                Debug.LogError($"UnlockableSystemManager: An UnlockableData asset ('{unlockable.name}') has an empty or null ID. Skipping this unlockable.", unlockable);
                continue;
            }
            if (unlockedStates.ContainsKey(unlockable.Id))
            {
                Debug.LogWarning($"UnlockableSystemManager: Duplicate UnlockableData ID '{unlockable.Id}' found. Only the first instance will be used. Please ensure all IDs are unique.", unlockable);
                continue;
            }
            unlockedStates.Add(unlockable.Id, false); // Default state is locked
        }

        // Then, attempt to load previously saved states, which will override the defaults.
        LoadStates();

        Debug.Log("UnlockableSystemManager initialized. Total unlockables: " + allUnlockableDefinitions.Count);
        Debug.Log($"Currently unlocked items: {unlockedStates.Count(pair => pair.Value)}");
    }

    // --- Public API for Unlockable Management ---

    /// <summary>
    /// Unlocks an item with the given ID.
    /// </summary>
    /// <param name="unlockableId">The unique ID of the unlockable to unlock.</param>
    /// <returns>True if the item was successfully unlocked (or was already unlocked), false if the ID was not found.</returns>
    public bool Unlock(string unlockableId)
    {
        if (string.IsNullOrWhiteSpace(unlockableId))
        {
            Debug.LogError("UnlockableSystemManager.Unlock: Unlockable ID cannot be null or empty.");
            return false;
        }

        if (!unlockedStates.ContainsKey(unlockableId))
        {
            Debug.LogWarning($"UnlockableSystemManager.Unlock: Unlockable with ID '{unlockableId}' not found in definitions. Cannot unlock.");
            return false;
        }

        if (unlockedStates[unlockableId])
        {
            Debug.Log($"Unlockable with ID '{unlockableId}' is already unlocked.");
            return true; // Already unlocked, consider it a success
        }

        unlockedStates[unlockableId] = true;
        Debug.Log($"Unlockable '{GetUnlockableData(unlockableId)?.DisplayName ?? unlockableId}' (ID: {unlockableId}) unlocked!");
        SaveStates(); // Save immediately after an unlock event
        return true;
    }

    /// <summary>
    /// Checks if an item with the given ID is currently unlocked.
    /// </summary>
    /// <param name="unlockableId">The unique ID of the unlockable to check.</param>
    /// <returns>True if the item is unlocked, false if it's locked or the ID was not found.</returns>
    public bool IsUnlocked(string unlockableId)
    {
        if (string.IsNullOrWhiteSpace(unlockableId))
        {
            Debug.LogError("UnlockableSystemManager.IsUnlocked: Unlockable ID cannot be null or empty.");
            return false;
        }

        if (unlockedStates.TryGetValue(unlockableId, out bool isCurrentlyUnlocked))
        {
            return isCurrentlyUnlocked;
        }

        Debug.LogWarning($"UnlockableSystemManager.IsUnlocked: Unlockable with ID '{unlockableId}' not found in definitions. Defaulting to locked.");
        return false; // If not found, it's effectively locked.
    }

    /// <summary>
    /// Retrieves the static definition data for a specific unlockable.
    /// </summary>
    /// <param name="unlockableId">The unique ID of the unlockable.</param>
    /// <returns>The UnlockableData object if found, otherwise null.</returns>
    public UnlockableData GetUnlockableData(string unlockableId)
    {
        if (string.IsNullOrWhiteSpace(unlockableId))
        {
            Debug.LogError("UnlockableSystemManager.GetUnlockableData: Unlockable ID cannot be null or empty.");
            return null;
        }
        return allUnlockableDefinitions.FirstOrDefault(data => data.Id == unlockableId);
    }

    /// <summary>
    /// Gets a list of all defined unlockable data objects.
    /// </summary>
    /// <returns>A read-only list of all UnlockableData objects.</returns>
    public IReadOnlyList<UnlockableData> GetAllUnlockableData()
    {
        return allUnlockableDefinitions.AsReadOnly();
    }

    /// <summary>
    /// Gets a list of all currently unlocked data objects.
    /// </summary>
    /// <returns>A list of UnlockableData objects for all items that are currently unlocked.</returns>
    public IEnumerable<UnlockableData> GetUnlockedData()
    {
        return allUnlockableDefinitions.Where(data => IsUnlocked(data.Id));
    }

    /// <summary>
    /// Gets a list of all currently locked data objects.
    /// </summary>
    /// <returns>A list of UnlockableData objects for all items that are currently locked.</returns>
    public IEnumerable<UnlockableData> GetLockedData()
    {
        return allUnlockableDefinitions.Where(data => !IsUnlocked(data.Id));
    }


    // --- Persistence Methods ---

    /// <summary>
    /// Saves the current unlocked states to PlayerPrefs.
    /// This uses JSON serialization to store the Dictionary.
    /// </summary>
    public void SaveStates()
    {
        // Unity's JsonUtility doesn't handle Dictionaries directly,
        // so we use a custom SerializableDictionary wrapper.
        var serializableStates = new SerializableDictionary<string, bool>(unlockedStates);
        string json = JsonUtility.ToJson(serializableStates);
        
        PlayerPrefs.SetString(UNLOCKED_STATES_KEY, json);
        PlayerPrefs.Save(); // Ensure data is written to disk
        Debug.Log("Unlockable states saved.");
    }

    /// <summary>
    /// Loads the unlocked states from PlayerPrefs.
    /// </summary>
    private void LoadStates()
    {
        if (PlayerPrefs.HasKey(UNLOCKED_STATES_KEY))
        {
            string json = PlayerPrefs.GetString(UNLOCKED_STATES_KEY);
            SerializableDictionary<string, bool> loadedStatesWrapper = new SerializableDictionary<string, bool>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, loadedStatesWrapper); // Deserialize into existing object
                // Now copy the loaded states into our active dictionary, ensuring we only update known unlockables
                foreach (var kvp in loadedStatesWrapper.ToDictionary())
                {
                    if (unlockedStates.ContainsKey(kvp.Key))
                    {
                        unlockedStates[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        Debug.LogWarning($"UnlockableSystemManager.LoadStates: Found saved state for unknown unlockable ID '{kvp.Key}'. This item might have been removed from the game.");
                    }
                }
                Debug.Log("Unlockable states loaded successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UnlockableSystemManager.LoadStates: Failed to deserialize unlockable states. Resetting. Error: {e.Message}");
                // If deserialization fails, clear the saved data to prevent continuous errors
                PlayerPrefs.DeleteKey(UNLOCKED_STATES_KEY);
                PlayerPrefs.Save();
                // Re-initialize with default (locked) states
                InitializeSystem(); 
            }
        }
        else
        {
            Debug.Log("No saved unlockable states found. Initializing with default (locked) states.");
        }
    }

    /// <summary>
    /// Resets all unlockable states to locked and clears saved data.
    /// Useful for testing or starting a new game.
    /// </summary>
    [ContextMenu("Reset All Unlockables (for debugging)")]
    public void ResetAllUnlockables()
    {
        PlayerPrefs.DeleteKey(UNLOCKED_STATES_KEY);
        PlayerPrefs.Save();

        // Reset the runtime dictionary
        foreach (string id in unlockedStates.Keys.ToList()) // ToList to modify while iterating
        {
            unlockedStates[id] = false;
        }

        Debug.LogWarning("All unlockable states have been reset to locked, and saved data cleared.");
        InitializeSystem(); // Re-initialize to reflect changes and ensure system is consistent
    }
}

/// <summary>
/// A helper class to make Dictionary<string, T> serializable by Unity's JsonUtility.
/// Unity's JsonUtility cannot directly serialize Dictionaries, so we convert them
/// to lists of keys and values, which JsonUtility can handle.
/// </summary>
[System.Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> targetDictionary;

    public SerializableDictionary() : this(new Dictionary<TKey, TValue>()) { }

    public SerializableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        targetDictionary = dictionary;
    }

    // Called before serialization (when saving)
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var pair in targetDictionary)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // Called after deserialization (when loading)
    public void OnAfterDeserialize()
    {
        targetDictionary.Clear();
        if (keys.Count != values.Count)
        {
            Debug.LogError("Deserialization error: keys and values count mismatch in SerializableDictionary.");
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            // Handle potential duplicate keys or null keys during deserialization gracefully
            if (targetDictionary.ContainsKey(keys[i]))
            {
                 Debug.LogWarning($"Duplicate key '{keys[i]}' found during deserialization of SerializableDictionary. Skipping duplicate.");
                 continue;
            }
            targetDictionary.Add(keys[i], values[i]);
        }
    }

    // Public method to get the underlying dictionary
    public Dictionary<TKey, TValue> ToDictionary()
    {
        return targetDictionary;
    }
}
```

---

### Example Usage in a Unity Project

Follow these steps to set up and use the Unlockable System in your Unity project:

#### **SETUP STEPS:**

1.  **Create `UnlockableData.cs` script:**
    *   In your Unity Project window, right-click -> Create -> C# Script.
    *   Name it `UnlockableData`.
    *   Copy the code from the first section (`UnlockableData.cs`) into this new script.

2.  **Create `UnlockableSystemManager.cs` script:**
    *   In your Unity Project window, right-click -> Create -> C# Script.
    *   Name it `UnlockableSystemManager`.
    *   Copy the code from the second section (`UnlockableSystemManager.cs` including the `SerializableDictionary` helper class) into this new script.

3.  **Create Unlockable Assets:**
    *   In your Unity Project window, right-click -> Create -> Unlockable System -> Unlockable Data.
    *   Name this asset (e.g., "SwordUnlockableData").
    *   In its Inspector, fill in:
        *   **ID:** `sword_01` (MUST be unique across all `UnlockableData` assets!)
        *   **Display Name:** `Master Sword`
        *   **Description:** `A legendary blade.`
        *   **Icon:** (Optional) Assign a Sprite asset.
        *   **Cost:** `100` (Optional, for gameplay logic if you have currency)
    *   Repeat this for other unlockables (e.g., "BowUnlockableData" with ID `bow_01`, "MagicSpellUnlockableData" with ID `spell_fireball`).

4.  **Create Manager GameObject:**
    *   Create an empty GameObject in your scene (e.g., named "UnlockableSystem").
    *   Attach the `UnlockableSystemManager.cs` script to it.

5.  **Assign Unlockable Assets to Manager:**
    *   Select the "UnlockableSystem" GameObject in your Hierarchy.
    *   In its Inspector, expand the "Unlockable Definitions" list.
    *   Drag all your created `UnlockableData` assets (e.g., SwordUnlockableData, BowUnlockableData, MagicSpellUnlockableData) from your Project window into this list.

6.  **Run the Scene:** The system will initialize, load saved states (if any), and be ready for use. Check the Unity Console for initialization messages.

#### **EXAMPLE USAGE IN OTHER SCRIPTS (e.g., `GameProgressionManager.cs`):**

This script demonstrates how other parts of your game would interact with the `UnlockableSystemManager`. You can attach this to any GameObject in your scene and link it to UI elements for interactive testing.

```csharp
// GameProgressionManager.cs
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text and Button
using System.Collections.Generic; // Not strictly needed here, but often useful

public class GameProgressionManager : MonoBehaviour
{
    [Header("UI References")]
    public Text statusText; // Assign a UI Text element in Inspector for feedback
    public Button unlockSwordButton; // Assign a UI Button
    public Button checkSwordButton; // Assign a UI Button
    public Button unlockBowButton;
    public Button checkBowButton;
    public Button resetAllButton; // Button to reset progress

    // Define the IDs of your unlockable items (must match the IDs in your UnlockableData assets)
    private string swordId = "sword_01";
    private string bowId = "bow_01";
    private string fireballSpellId = "spell_fireball";

    void Start()
    {
        // Ensure the UnlockableSystemManager exists and is initialized
        if (UnlockableSystemManager.Instance == null)
        {
            Debug.LogError("UnlockableSystemManager not found in scene. Please ensure it's set up on a GameObject.", this);
            return;
        }

        // Initial UI setup
        UpdateUI();

        // Assign button listeners
        if (unlockSwordButton != null) unlockSwordButton.onClick.AddListener(() => TryUnlockItem(swordId));
        if (checkSwordButton != null) checkSwordButton.onClick.AddListener(() => CheckItemStatus(swordId));
        if (unlockBowButton != null) unlockBowButton.onClick.AddListener(() => TryUnlockItem(bowId));
        if (checkBowButton != null) checkBowButton.onClick.AddListener(() => CheckItemStatus(bowId));
        if (resetAllButton != null) resetAllButton.onClick.AddListener(ResetGameProgress);

        // Example: Automatically unlock an item after a certain condition (e.g., first start, tutorial completion)
        // Uncomment the line below to auto-unlock the Fireball spell on the first run.
        /*
        if (!UnlockableSystemManager.Instance.IsUnlocked(fireballSpellId))
        {
            Debug.Log($"Attempting to auto-unlock {fireballSpellId}...");
            if (UnlockableSystemManager.Instance.Unlock(fireballSpellId))
            {
                Debug.Log($"Fireball spell unlocked automatically!");
                UpdateUI();
            }
        }
        */

        Debug.Log("GameProgressionManager initialized.");
    }

    void Update()
    {
        // Example: Check for unlocks based on keyboard input (for quick testing)
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Attempting to unlock sword via U key...");
            TryUnlockItem(swordId);
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Checking sword status via I key...");
            CheckItemStatus(swordId);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Attempting to unlock bow via B key...");
            TryUnlockItem(bowId);
        }
    }

    /// <summary>
    /// Attempts to unlock an item using its ID.
    /// </summary>
    /// <param name="itemId">The ID of the item to unlock.</param>
    void TryUnlockItem(string itemId)
    {
        if (UnlockableSystemManager.Instance.Unlock(itemId))
        {
            Debug.Log($"Item '{itemId}' was unlocked or already unlocked.");
            UpdateUI();
            // Trigger any post-unlock events (e.g., show notification, play sound, instantiate item)
        }
        else
        {
            Debug.LogWarning($"Failed to unlock item '{itemId}'. Check ID or if it's defined in the manager.");
        }
    }

    /// <summary>
    /// Checks and logs the status of an item.
    /// </summary>
    /// <param name="itemId">The ID of the item to check.</param>
    void CheckItemStatus(string itemId)
    {
        if (UnlockableSystemManager.Instance.IsUnlocked(itemId))
        {
            Debug.Log($"Item '{itemId}' is currently UNLOCKED.");
            UnlockableData data = UnlockableSystemManager.Instance.GetUnlockableData(itemId);
            if (data != null)
            {
                if (statusText != null) statusText.text = $"'{data.DisplayName}' (ID: {data.Id}) is UNLOCKED!";
                Debug.Log($"  Display Name: {data.DisplayName}, Description: {data.Description}, Cost: {data.Cost}");
                // You could use `data.Icon` here to update an image in the UI.
            }
            else
            {
                if (statusText != null) statusText.text = $"Unknown item '{itemId}' is UNLOCKED (data missing)!";
            }
        }
        else
        {
            Debug.Log($"Item '{itemId}' is currently LOCKED.");
            UnlockableData data = UnlockableSystemManager.Instance.GetUnlockableData(itemId);
            if (data != null)
            {
                 if (statusText != null) statusText.text = $"'{data.DisplayName}' (ID: {data.Id}) is LOCKED.";
            }
            else
            {
                if (statusText != null) statusText.text = $"Unknown item '{itemId}' is LOCKED (data missing)!";
            }
        }
        UpdateUI(); // Refresh UI to reflect status
    }

    /// <summary>
    /// Resets all unlockable progress for testing purposes.
    /// </summary>
    void ResetGameProgress()
    {
        if (UnlockableSystemManager.Instance != null)
        {
            UnlockableSystemManager.Instance.ResetAllUnlockables();
            Debug.Log("Game progress (unlockables) has been reset!");
            UpdateUI();
        }
    }

    /// <summary>
    /// Updates the UI to show the current status of all unlockables.
    /// </summary>
    void UpdateUI()
    {
        if (statusText != null && UnlockableSystemManager.Instance != null)
        {
            string uiOutput = "--- Unlockable Status ---\n";
            foreach (var unlockableData in UnlockableSystemManager.Instance.GetAllUnlockableData())
            {
                uiOutput += $"- {unlockableData.DisplayName} (ID: {unlockableData.Id}): {(UnlockableSystemManager.Instance.IsUnlocked(unlockableData.Id) ? "UNLOCKED" : "LOCKED")}\n";
            }
            statusText.text = uiOutput;
        }

        // Update button interactability based on state
        if (unlockSwordButton != null) unlockSwordButton.interactable = !UnlockableSystemManager.Instance.IsUnlocked(swordId);
        if (unlockBowButton != null) unlockBowButton.interactable = !UnlockableSystemManager.Instance.IsUnlocked(bowId);
        // Add similar logic for other unlock buttons
    }
}
```

#### **How this demonstrates the Unlockable System pattern:**

1.  **Separation of Concerns:**
    *   `UnlockableData` (ScriptableObject): Defines *what* an unlockable is (its static properties like name, description, icon, cost). It holds data, not behavior or state.
    *   `UnlockableSystemManager` (MonoBehaviour): Manages the *state* (locked/unlocked) and *behavior* (unlocking, checking status, persistence). It's the operational hub, agnostic to the specific type of unlockable beyond its unique ID.

2.  **Centralized Management:** All unlockable states are managed in one place (`UnlockableSystemManager`), making it easy to query, modify, and persist them. Other game systems don't need to know the internal details of how unlocks are stored.

3.  **Data-Driven:** New unlockables can be added simply by creating new `UnlockableData` assets in the Unity editor and assigning them to the manager. This requires no code changes, making content creation flexible and efficient.

4.  **Persistence:** The system automatically saves and loads unlockable states using `PlayerPrefs` (serializing a dictionary to JSON), ensuring player progress is maintained across game sessions. For more complex games, this could be extended to file I/O, cloud saves, or other robust serialization methods.

5.  **Extensibility:**
    *   **Adding new unlockables:** Create a new `UnlockableData` asset.
    *   **Adding more specific data to unlockables:** You could create new `ScriptableObject` classes that inherit from `UnlockableData` if you need specialized properties for certain categories (e.g., `WeaponUnlockableData`, `CharacterUnlockableData`).
    *   **Changing persistence mechanism:** Modify `SaveStates` and `LoadStates` methods to use a different serialization library or storage solution.

6.  **Loose Coupling:** Game components that need to interact with unlockables only need a reference to `UnlockableSystemManager.Instance` and the `ID` of the item. They don't need direct knowledge of `UnlockableData` assets or the internal state storage. This reduces dependencies and makes the system easier to maintain and extend.