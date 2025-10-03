// Unity Design Pattern Example: CloudSaveIntegration
// This script demonstrates the CloudSaveIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **CloudSaveIntegration** design pattern in Unity. The core idea is to abstract the specifics of the cloud saving service (like Unity Cloud Save, PlayFab, or a custom backend) behind a common interface. This allows your game logic to interact with a simple `CloudSaveManager` without knowing the underlying save mechanism, making it easy to swap providers or add new ones in the future.

---

### **CloudSaveIntegration Pattern Explanation:**

1.  **`PlayerData` (Data Model):** A simple C# class representing the game data you want to save. It's marked `[System.Serializable]` so `JsonUtility` can convert it to/from JSON.
2.  **`ICloudSaveProvider` (Interface):** Defines the contract for any cloud saving service. It specifies methods like `SaveData`, `LoadData`, `DeleteData`, and `ListKeys`. This is the core abstraction.
3.  **`UnityCloudSaveProvider` (Concrete Provider):** An implementation of `ICloudSaveProvider` that uses Unity Gaming Services (UGS) Cloud Save. This class handles the actual communication with UGS, including JSON serialization/deserialization.
4.  **`DummyCloudSaveProvider` (Concrete Provider - for testing/fallback):** Another implementation of `ICloudSaveProvider` that stores data in memory (a simple `Dictionary`). Useful for local testing, development without internet, or when no cloud service is desired.
5.  **`CloudSaveManager` (Central Service / Facade):** A `MonoBehaviour` Singleton that orchestrates the cloud save operations.
    *   It holds a reference to the currently active `ICloudSaveProvider`.
    *   It provides a simple, high-level API (e.g., `SavePlayerDataAsync`, `LoadPlayerDataAsync`) for other game systems to use.
    *   It initializes the chosen provider and handles game-specific data (like `PlayerData`).
    *   It acts as a facade, hiding the complexity of the underlying cloud provider from the rest of the game.

---

### **Setup Instructions for Unity Project:**

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject and name it `CloudSaveManager`.
2.  **Attach `CloudSaveManager.cs`:** Drag and drop the `CloudSaveManager.cs` script (provided below) onto this GameObject.
3.  **Install Unity Gaming Services Packages (if using UGS):**
    *   Go to `Window > Package Manager`.
    *   In the Package Manager, select `Unity Registry`.
    *   Find and install the following packages:
        *   `Cloud Save`
        *   `Core`
        *   `Authentication`
4.  **Initialize Unity Services Project (if using UGS):**
    *   Go to `Edit > Project Settings > Services`.
    *   Link your Unity project to a Unity Services Project ID. If you don't have one, Unity will guide you to create a new one. This is necessary for Cloud Save to work.
5.  **Configure `CloudSaveManager` in Inspector:**
    *   Select the `CloudSaveManager` GameObject in your scene.
    *   In the Inspector, ensure the `Use Unity Cloud Save` checkbox is ticked if you want to use the actual Unity Cloud Save service. If it's unticked, it will use the `DummyCloudSaveProvider` which stores data only in memory for the current session.
6.  **Create `GameStateController.cs`:** Create another C# script (e.g., `GameStateController.cs`), add the example usage code, and attach it to any GameObject in your scene (or use a dedicated `GameManager` GameObject).

---

### **1. `PlayerData.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine; // For Vector3

/// <summary>
/// Represents the player's saved data.
/// This class defines the structure of information that will be persisted in the cloud.
/// It's marked [System.Serializable] so Unity's JsonUtility can convert it to/from JSON.
/// </summary>
[System.Serializable]
public class PlayerData
{
    public int level;
    public int coins;
    public List<string> inventory;
    public Vector3 lastKnownPosition; // Example of a Unity type that JsonUtility can handle.
    public System.DateTime lastSaveTime;

    // Constructor for convenience when creating new default data
    public PlayerData()
    {
        level = 1;
        coins = 0;
        inventory = new List<string>();
        lastKnownPosition = Vector3.zero;
        lastSaveTime = System.DateTime.UtcNow;
    }
}
```

---

### **2. `ICloudSaveProvider.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The ICloudSaveProvider interface defines the contract for any cloud saving service.
/// This is the core abstraction of the CloudSaveIntegration pattern.
/// By programming to this interface, the CloudSaveManager (and thus your game logic)
/// remains independent of the specific cloud provider implementation.
/// </summary>
public interface ICloudSaveProvider
{
    /// <summary>
    /// Saves data of a specified type associated with a given key.
    /// </summary>
    /// <typeparam name="T">The type of data to save.</typeparam>
    /// <param name="key">The unique key to identify the data.</param>
    /// <param name="data">The data object to be saved.</param>
    /// <returns>True if save was successful, false otherwise.</returns>
    Task<bool> SaveData<T>(string key, T data);

    /// <summary>
    /// Loads data of a specified type associated with a given key.
    /// </summary>
    /// <typeparam name="T">The expected type of data to load.</typeparam>
    /// <param name="key">The unique key to identify the data.</param>
    /// <returns>The loaded data object if found and successfully deserialized; otherwise, default(T).</returns>
    Task<T> LoadData<T>(string key);

    /// <summary>
    /// Deletes data associated with a given key.
    /// </summary>
    /// <param name="key">The unique key of the data to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeleteData(string key);

    /// <summary>
    /// Lists all available keys stored in the cloud.
    /// </summary>
    /// <returns>A list of strings representing all stored keys.</returns>
    Task<List<string>> ListKeys();
}
```

---

### **3. `UnityCloudSaveProvider.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Concrete implementation of ICloudSaveProvider using Unity Gaming Services (UGS) Cloud Save.
/// This class handles the actual interaction with the UGS Cloud Save API, including
/// authentication, serialization/deserialization to JSON, and error handling specific to UGS.
/// </summary>
public class UnityCloudSaveProvider : ICloudSaveProvider
{
    private const string LogPrefix = "[UnityCloudSaveProvider]";

    /// <summary>
    /// Initializes Unity Services and authenticates the player anonymously.
    /// This is a prerequisite for using UGS Cloud Save.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (UnityServices.State == UnityServicesState.Uninitialized)
        {
            try
            {
                Debug.Log($"{LogPrefix} Initializing Unity Services...");
                await UnityServices.InitializeAsync();
                Debug.Log($"{LogPrefix} Unity Services initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Unity Services initialization failed: {e.Message}");
                return;
            }
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                Debug.Log($"{LogPrefix} Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"{LogPrefix} Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Anonymous sign-in failed: {e.Message}");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SaveData<T>(string key, T data)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError($"{LogPrefix} Not signed in. Cannot save data for key '{key}'.");
            return false;
        }

        try
        {
            // Unity Cloud Save typically stores data as key-value pairs where values are strings.
            // We serialize our object to JSON before saving.
            string jsonData = JsonUtility.ToJson(data);
            var dataToSave = new Dictionary<string, string> { { key, jsonData } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
            Debug.Log($"{LogPrefix} Data saved successfully for key: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to save data for key '{key}': {e.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<T> LoadData<T>(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError($"{LogPrefix} Not signed in. Cannot load data for key '{key}'.");
            return default(T);
        }

        try
        {
            // Load data for the specified key(s). Returns a dictionary of string values.
            var loadedData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });

            if (loadedData != null && loadedData.ContainsKey(key))
            {
                string jsonData = loadedData[key];
                T deserializedData = JsonUtility.FromJson<T>(jsonData);
                Debug.Log($"{LogPrefix} Data loaded successfully for key: {key}");
                return deserializedData;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} No data found in cloud for key: {key}");
                return default(T);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to load data for key '{key}': {e.Message}");
            return default(T);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteData(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError($"{LogPrefix} Not signed in. Cannot delete data for key '{key}'.");
            return false;
        }

        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(new HashSet<string> { key });
            Debug.Log($"{LogPrefix} Data deleted successfully for key: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to delete data for key '{key}': {e.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> ListKeys()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError($"{LogPrefix} Not signed in. Cannot list keys.");
            return new List<string>();
        }

        try
        {
            var keys = await CloudSaveService.Instance.Data.Player.GetKeysAsync();
            Debug.Log($"{LogPrefix} Keys listed successfully.");
            return new List<string>(keys);
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to list keys: {e.Message}");
            return new List<string>();
        }
    }
}
```

---

### **4. `DummyCloudSaveProvider.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine; // For JsonUtility

/// <summary>
/// A dummy implementation of ICloudSaveProvider.
/// This provider stores data in a simple in-memory dictionary.
/// It's useful for:
/// 1. Local development and testing without needing a live cloud service.
/// 2. Providing a fallback when a cloud service is unavailable or not configured.
/// 3. Demonstrating how easy it is to swap out providers thanks to the interface.
/// </summary>
public class DummyCloudSaveProvider : ICloudSaveProvider
{
    // In-memory storage for dummy data. Simulates cloud storage.
    private Dictionary<string, string> _dummyData = new Dictionary<string, string>();
    private const string LogPrefix = "[DummyCloudSaveProvider]";

    /// <inheritdoc/>
    public Task<bool> SaveData<T>(string key, T data)
    {
        // Serialize the data to JSON, just like a real cloud provider would.
        string jsonData = JsonUtility.ToJson(data);
        _dummyData[key] = jsonData;
        Debug.Log($"{LogPrefix} Dummy data saved for key: '{key}' (Data: {jsonData.Substring(0, Mathf.Min(jsonData.Length, 100))}...)");
        return Task.FromResult(true); // Always succeeds for dummy
    }

    /// <inheritdoc/>
    public Task<T> LoadData<T>(string key)
    {
        if (_dummyData.ContainsKey(key))
        {
            string jsonData = _dummyData[key];
            T deserializedData = JsonUtility.FromJson<T>(jsonData);
            Debug.Log($"{LogPrefix} Dummy data loaded for key: '{key}'");
            return Task.FromResult(deserializedData);
        }
        Debug.LogWarning($"{LogPrefix} No dummy data found for key: '{key}'");
        return Task.FromResult(default(T)); // Return default if key not found
    }

    /// <inheritdoc/>
    public Task<bool> DeleteData(string key)
    {
        bool deleted = _dummyData.Remove(key);
        if (deleted)
        {
            Debug.Log($"{LogPrefix} Dummy data deleted for key: '{key}'");
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} No dummy data found to delete for key: '{key}'");
        }
        return Task.FromResult(deleted);
    }

    /// <inheritdoc/>
    public Task<List<string>> ListKeys()
    {
        Debug.Log($"{LogPrefix} Listing dummy keys. Count: {_dummyData.Keys.Count}");
        return Task.FromResult(new List<string>(_dummyData.Keys));
    }
}
```

---

### **5. `CloudSaveManager.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The CloudSaveManager is the central point of contact for all cloud save operations in the game.
/// It implements the Singleton pattern to ensure only one instance exists and provides a global access point.
/// It acts as a Facade, hiding the complexities of the underlying cloud save provider from other game systems.
/// </summary>
public class CloudSaveManager : MonoBehaviour
{
    // Singleton instance for global access.
    public static CloudSaveManager Instance { get; private set; }

    // The currently active cloud save provider, chosen based on configuration.
    private ICloudSaveProvider _cloudSaveProvider;

    // The key used to save/load the main PlayerData object in the cloud.
    private const string PLAYER_DATA_KEY = "playerData";

    [Header("Cloud Save Configuration")]
    [Tooltip("If true, Unity Cloud Save will be used. Otherwise, a dummy in-memory provider.")]
    [SerializeField] private bool _useUnityCloudSave = true;

    // The player's current game data, loaded from or prepared for the cloud.
    public PlayerData CurrentPlayerData { get; private set; }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern and initializes the chosen cloud save provider.
    /// </summary>
    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one to ensure a single instance.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Make this GameObject persistent across scene loads.
        DontDestroyOnLoad(gameObject);

        // Initialize the selected cloud save provider.
        await InitializeCloudSaveProvider();
    }

    /// <summary>
    /// Selects and initializes the appropriate ICloudSaveProvider based on editor settings.
    /// </summary>
    private async Task InitializeCloudSaveProvider()
    {
        if (_useUnityCloudSave)
        {
            // Use the Unity Cloud Save implementation.
            _cloudSaveProvider = new UnityCloudSaveProvider();
            Debug.Log("[CloudSaveManager] Initializing Unity Cloud Save Provider...");
            // Specific initialization for UGS, including authentication.
            await ((UnityCloudSaveProvider)_cloudSaveProvider).InitializeAsync();
            Debug.Log("[CloudSaveManager] Unity Cloud Save Provider initialized.");
        }
        else
        {
            // Use the dummy provider for local testing or when UGS is not desired.
            Debug.LogWarning("[CloudSaveManager] Using Dummy Cloud Save Provider. Data will not persist beyond the current session.");
            _cloudSaveProvider = new DummyCloudSaveProvider();
            // Dummy provider does not require special initialization.
        }

        if (_cloudSaveProvider == null)
        {
            Debug.LogError("[CloudSaveManager] No Cloud Save Provider could be initialized!");
        }
    }

    /// <summary>
    /// Saves the current player data to the configured cloud save provider.
    /// If no player data exists, it creates a default one before saving.
    /// </summary>
    public async Task SavePlayerDataAsync()
    {
        if (_cloudSaveProvider == null)
        {
            Debug.LogError("[CloudSaveManager] Cloud Save Provider not initialized. Cannot save data.");
            return;
        }

        if (CurrentPlayerData == null)
        {
            Debug.LogWarning("[CloudSaveManager] No player data currently loaded. Creating default data for first save.");
            CurrentPlayerData = CreateDefaultPlayerData();
        }

        // Update the last save time before saving.
        CurrentPlayerData.lastSaveTime = System.DateTime.UtcNow;

        bool success = await _cloudSaveProvider.SaveData(PLAYER_DATA_KEY, CurrentPlayerData);

        if (success)
        {
            Debug.Log("[CloudSaveManager] Player data saved successfully to cloud.");
        }
        else
        {
            Debug.LogError("[CloudSaveManager] Failed to save player data to cloud.");
        }
    }

    /// <summary>
    /// Loads player data from the configured cloud save provider.
    /// If no data is found in the cloud, it initializes CurrentPlayerData with default values.
    /// </summary>
    public async Task LoadPlayerDataAsync()
    {
        if (_cloudSaveProvider == null)
        {
            Debug.LogError("[CloudSaveManager] Cloud Save Provider not initialized. Cannot load data.");
            CurrentPlayerData = CreateDefaultPlayerData(); // Provide default data if provider is missing
            return;
        }

        PlayerData loadedData = await _cloudSaveProvider.LoadData<PlayerData>(PLAYER_DATA_KEY);

        if (loadedData != null)
        {
            CurrentPlayerData = loadedData;
            Debug.Log($"[CloudSaveManager] Player data loaded successfully. Level: {CurrentPlayerData.level}, Coins: {CurrentPlayerData.coins}");
        }
        else
        {
            // No data found in the cloud (or error during load), so create new default data.
            Debug.Log("[CloudSaveManager] No player data found in cloud. Creating new default data locally.");
            CurrentPlayerData = CreateDefaultPlayerData();
        }
    }

    /// <summary>
    /// Creates and returns a new PlayerData object with default starting values.
    /// </summary>
    private PlayerData CreateDefaultPlayerData()
    {
        return new PlayerData
        {
            level = 1,
            coins = 100,
            inventory = new List<string> { "Basic Sword", "Health Potion" },
            lastKnownPosition = Vector3.zero,
            lastSaveTime = System.DateTime.UtcNow
        };
    }

    // --- Public methods for other game systems to update PlayerData ---
    // These methods provide controlled access to modify the current player data.
    // Call SavePlayerDataAsync() afterwards to persist these changes.

    public void UpdatePlayerLevel(int newLevel)
    {
        if (CurrentPlayerData == null) CurrentPlayerData = CreateDefaultPlayerData();
        CurrentPlayerData.level = newLevel;
        Debug.Log($"[CloudSaveManager] Player level updated to: {newLevel}");
    }

    public void AddCoins(int amount)
    {
        if (CurrentPlayerData == null) CurrentPlayerData = CreateDefaultPlayerData();
        CurrentPlayerData.coins += amount;
        Debug.Log($"[CloudSaveManager] Player coins updated to: {CurrentPlayerData.coins}");
    }

    public void AddItemToInventory(string item)
    {
        if (CurrentPlayerData == null) CurrentPlayerData = CreateDefaultPlayerData();
        if (CurrentPlayerData.inventory == null) CurrentPlayerData.inventory = new List<string>();
        CurrentPlayerData.inventory.Add(item);
        Debug.Log($"[CloudSaveManager] Item '{item}' added to inventory. Current inventory: {string.Join(", ", CurrentPlayerData.inventory)}");
    }

    public void SetPlayerPosition(Vector3 position)
    {
        if (CurrentPlayerData == null) CurrentPlayerData = CreateDefaultPlayerData();
        CurrentPlayerData.lastKnownPosition = position;
        Debug.Log($"[CloudSaveManager] Player position updated to: {position}");
    }
}
```

---

### **Example Usage: `GameStateController.cs`**

This script demonstrates how other parts of your game would interact with the `CloudSaveManager`. It includes methods to load and save data, which you might trigger from UI buttons, game events, or scene transitions.

```csharp
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// This class serves as an example of how other game systems (like a GameManager,
/// UI Controller, or Player Controller) would interact with the CloudSaveManager.
/// It uses the CloudSaveManager's high-level API without needing to know the
/// specifics of the underlying cloud provider.
/// </summary>
public class GameStateController : MonoBehaviour
{
    private const string LogPrefix = "[GameStateController]";

    // Example UI for demonstration (can be replaced with actual UI elements)
    [SerializeField] private TMPro.TextMeshProUGUI statusText; // Requires TextMeshPro installed
    [SerializeField] private TMPro.TextMeshProUGUI dataDisplay;

    // Called when the script instance is being loaded.
    async void Start()
    {
        // Ensure the CloudSaveManager instance is available.
        if (CloudSaveManager.Instance == null)
        {
            Debug.LogError($"{LogPrefix} CloudSaveManager not found in scene! Please ensure it's set up correctly.");
            if (statusText != null) statusText.text = "Error: CloudSaveManager missing!";
            return;
        }

        Debug.Log($"{LogPrefix} Attempting to load player data on game start...");
        if (statusText != null) statusText.text = "Loading data...";
        await CloudSaveManager.Instance.LoadPlayerDataAsync();

        // After loading, update the game state or UI based on the loaded data.
        UpdateGameUI();
        if (statusText != null) statusText.text = "Data loaded/initialized.";
        Debug.Log($"{LogPrefix} Game initialized with player data.");
    }

    /// <summary>
    /// Triggers a save operation. This might be called when the player exits the game,
    /// reaches a checkpoint, or manually clicks a save button.
    /// </summary>
    public async void SaveGameData()
    {
        if (CloudSaveManager.Instance == null)
        {
            Debug.LogError($"{LogPrefix} CloudSaveManager is not available.");
            return;
        }

        // --- Example of modifying data before saving ---
        // (In a real game, these updates would come from gameplay mechanics)
        CloudSaveManager.Instance.UpdatePlayerLevel(CloudSaveManager.Instance.CurrentPlayerData.level + 1);
        CloudSaveManager.Instance.AddCoins(Random.Range(20, 100));
        CloudSaveManager.Instance.AddItemToInventory("Mystery Crate");
        CloudSaveManager.Instance.SetPlayerPosition(new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)));

        Debug.Log($"{LogPrefix} Attempting to save player data...");
        if (statusText != null) statusText.text = "Saving data...";
        await CloudSaveManager.Instance.SavePlayerDataAsync();

        UpdateGameUI();
        if (statusText != null) statusText.text = "Data saved!";
        Debug.Log($"{LogPrefix} Game data save initiated.");
    }

    /// <summary>
    /// Triggers a load operation. This might be called when the player selects "Continue Game".
    /// </summary>
    public async void LoadGameData()
    {
        if (CloudSaveManager.Instance == null)
        {
            Debug.LogError($"{LogPrefix} CloudSaveManager is not available.");
            return;
        }

        Debug.Log($"{LogPrefix} Attempting to load player data...");
        if (statusText != null) statusText.text = "Loading data...";
        await CloudSaveManager.Instance.LoadPlayerDataAsync();

        UpdateGameUI();
        if (statusText != null) statusText.text = "Data reloaded!";
        Debug.Log($"{LogPrefix} Game data load initiated.");
    }

    /// <summary>
    /// Updates the UI to display the current player data.
    /// </summary>
    public void UpdateGameUI()
    {
        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentPlayerData == null)
        {
            if (dataDisplay != null) dataDisplay.text = "No player data loaded.";
            return;
        }

        var data = CloudSaveManager.Instance.CurrentPlayerData;
        string inventoryList = data.inventory.Count > 0 ? string.Join(", ", data.inventory) : "None";

        string uiText = $"--- Current Player Data ---\n" +
                        $"Level: {data.level}\n" +
                        $"Coins: {data.coins}\n" +
                        $"Inventory: {inventoryList}\n" +
                        $"Last Position: {data.lastKnownPosition:F2}\n" +
                        $"Last Save Time (UTC): {data.lastSaveTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

        if (dataDisplay != null)
        {
            dataDisplay.text = uiText;
        }
        Debug.Log(uiText);
    }

    // --- Optional UI Button Callbacks for quick testing in Editor ---
    // You can hook these methods up to actual UI Buttons in a Canvas.

    public void OnSaveButtonClicked()
    {
        SaveGameData();
    }

    public void OnLoadButtonClicked()
    {
        LoadGameData();
    }

    public void OnDisplayButtonClicked()
    {
        UpdateGameUI();
    }

    // Example of a minimal UI for quick testing without TextMeshPro:
    private void OnGUI()
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20; // Make buttons larger

        // Create a simple UI layout
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Label("Cloud Save Controls", new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });

        if (GUILayout.Button("Save Game Data", buttonStyle, GUILayout.Height(40)))
        {
            OnSaveButtonClicked();
        }
        if (GUILayout.Button("Load Game Data", buttonStyle, GUILayout.Height(40)))
        {
            OnLoadButtonClicked();
        }
        if (GUILayout.Button("Display Current Data", buttonStyle, GUILayout.Height(40)))
        {
            OnDisplayButtonClicked();
        }
        GUILayout.EndArea();

        // Display status and data if TextMeshPro is not used or configured
        if (statusText == null && dataDisplay == null)
        {
            // Simple text display for status and data for OnGUI
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 18;
            textStyle.wordWrap = true;

            GUILayout.BeginArea(new Rect(220, 10, Screen.width - 230, Screen.height - 20));
            GUILayout.Label("Status: " + (statusText != null ? statusText.text : "N/A (Configure TextMeshPro)"), textStyle);
            GUILayout.Label("Data:", textStyle);
            GUILayout.Label(dataDisplay != null ? dataDisplay.text : "N/A (Configure TextMeshPro)", textStyle);
            GUILayout.EndArea();
        }
    }
}
```