// Unity Design Pattern Example: ProfileSyncSystem
// This script demonstrates the ProfileSyncSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ProfileSyncSystem' design pattern aims to centralize the management, persistence, and synchronization of various game or user profiles. This is crucial in games for saving player progress, settings, character data, or even managing multiple user accounts.

**Core Idea:**
A central manager (often a Singleton) handles loading, saving, switching, and providing access to different profiles. It also notifies other systems when the active profile changes or its data is updated, ensuring all parts of the game operate on the correct and up-to-date information.

**Real-world Use Cases:**
*   **Save/Load Systems:** Managing multiple save slots or player profiles.
*   **User Settings:** Storing graphics, audio, control preferences for different users.
*   **Character Progression:** Handling distinct character data (stats, inventory) for each save file.
*   **Multiplayer Games:** Synchronizing player data across clients or with a server (though this example focuses on local persistence).

---

## 1. Profile Data Structures

These classes define the structure of the data that will be stored for each user profile. They are marked `[System.Serializable]` so Unity's `JsonUtility` can convert them to/from JSON.

*   `UserProfileData`: The main container for all profile-specific data.
*   `PlayerSettings`: Example for general game settings.
*   `GameProgress`: Example for player progress in the game.
*   `InventoryItem`: A simple struct for items in the inventory.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single item in the player's inventory.
/// Marked Serializable to be usable with JsonUtility.
/// </summary>
[System.Serializable]
public struct InventoryItem
{
    public string ItemId;
    public int Quantity;

    public InventoryItem(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}

/// <summary>
/// Contains player-specific settings like volume, brightness, control scheme.
/// Marked Serializable to be usable with JsonUtility.
/// </summary>
[System.Serializable]
public class PlayerSettings
{
    public float MasterVolume = 0.75f;
    public float MusicVolume = 0.5f;
    public float SFXVolume = 0.6f;
    public float Brightness = 1.0f;
    public string ControlScheme = "Default";
    public bool VSyncEnabled = true;

    public void ResetToDefaults()
    {
        MasterVolume = 0.75f;
        MusicVolume = 0.5f;
        SFXVolume = 0.6f;
        Brightness = 1.0f;
        ControlScheme = "Default";
        VSyncEnabled = true;
        Debug.Log("Player Settings reset to defaults.");
    }
}

/// <summary>
/// Contains player's game progression data like current level, high score, inventory.
/// Marked Serializable to be usable with JsonUtility.
/// </summary>
[System.Serializable]
public class GameProgress
{
    public int CurrentLevel = 1;
    public int HighScore = 0;
    public List<string> UnlockedAbilities = new List<string>();
    public List<InventoryItem> Inventory = new List<InventoryItem>();

    public GameProgress()
    {
        // Initialize with some default items if needed for new profiles
        Inventory.Add(new InventoryItem("HealthPotion", 2));
        Inventory.Add(new InventoryItem("Sword", 1));
        UnlockedAbilities.Add("DoubleJump");
    }

    public void ResetToDefaults()
    {
        CurrentLevel = 1;
        HighScore = 0;
        UnlockedAbilities.Clear();
        UnlockedAbilities.Add("DoubleJump"); // Starting ability
        Inventory.Clear();
        Inventory.Add(new InventoryItem("HealthPotion", 2));
        Inventory.Add(new InventoryItem("Sword", 1));
        Debug.Log("Game Progress reset to defaults.");
    }
}

/// <summary>
/// The main container for all data related to a single user profile.
/// It aggregates PlayerSettings and GameProgress.
/// Marked Serializable to be usable with JsonUtility.
/// </summary>
[System.Serializable]
public class UserProfileData
{
    public string ProfileName;
    // DateTime is not directly serializable by JsonUtility.
    // We serialize it as a string and convert back at runtime.
    public string LastPlayedString;

    [System.NonSerialized] public DateTime LastPlayed; // Runtime field

    public PlayerSettings Settings = new PlayerSettings();
    public GameProgress Progress = new GameProgress();

    public UserProfileData(string name)
    {
        ProfileName = name;
        MarkModified(); // Set initial LastPlayed timestamp
    }

    /// <summary>
    /// Updates the LastPlayed timestamp to now and syncs the string representation.
    /// </summary>
    public void MarkModified()
    {
        LastPlayed = DateTime.UtcNow;
        LastPlayedString = LastPlayed.ToString("o"); // "o" for ISO 8601 format
    }

    /// <summary>
    /// Converts the LastPlayedString back to DateTime for runtime use.
    /// This should be called after deserialization.
    /// </summary>
    public void SyncLastPlayedFromDateString()
    {
        if (DateTime.TryParse(LastPlayedString, out DateTime parsedDate))
        {
            LastPlayed = parsedDate;
        }
        else
        {
            LastPlayed = DateTime.MinValue; // Default if parsing fails
            Debug.LogWarning($"Failed to parse LastPlayed date string for profile '{ProfileName}': {LastPlayedString}");
        }
    }

    /// <summary>
    /// Resets all nested profile data to their default values.
    /// </summary>
    public void ResetProfileData()
    {
        Settings.ResetToDefaults();
        Progress.ResetToDefaults();
        MarkModified(); // Update timestamp after reset
        Debug.Log($"Profile '{ProfileName}' data has been reset to defaults.");
    }
}
```

---

## 2. ProfileSyncSystem (The Manager)

This is the core of the pattern. It's a `MonoBehaviour` Singleton responsible for:
*   Loading and saving profile data to `PlayerPrefs` (using JSON).
*   Keeping track of all loaded profiles.
*   Managing the currently "active" profile.
*   Notifying other systems when the active profile changes or its data is updated.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The central Profile Synchronization System (Singleton).
/// Manages loading, saving, and switching of user profiles.
/// Notifies subscribers when the active profile changes or its data is updated.
/// Uses JsonUtility and PlayerPrefs for persistence.
/// </summary>
public class ProfileSyncSystem : MonoBehaviour
{
    // --- Singleton Setup ---
    public static ProfileSyncSystem Instance { get; private set; }

    [Header("Profile Settings")]
    [Tooltip("The prefix for PlayerPrefs keys to store profile data.")]
    private const string PROFILE_PLAYERPREFS_PREFIX = "ProfileSync_";
    [Tooltip("The PlayerPrefs key to store the name of the last active profile.")]
    private const string LAST_ACTIVE_PROFILE_KEY = "LastActiveProfile";
    [Tooltip("The default profile name to use if no other profile is found or specified.")]
    [SerializeField] private string _defaultProfileName = "Player1";

    // --- Internal State ---
    // Stores all profiles that have been loaded into memory.
    private Dictionary<string, UserProfileData> _loadedProfiles = new Dictionary<string, UserProfileData>();

    // The name of the currently active profile.
    private string _activeProfileName;

    // The actual data of the currently active profile.
    // Public getter allows other systems to read the active profile's data.
    public UserProfileData ActiveProfile { get; private set; }

    // --- Events ---
    /// <summary>
    /// Event fired when the active profile changes (e.g., a new profile is loaded or switched to).
    /// Subscribers should update their UI/game state based on the new profile.
    /// </summary>
    public event Action<UserProfileData> OnActiveProfileChanged;

    /// <summary>
    /// Event fired when the data within the active profile has been modified.
    /// Subscribers might re-render UI elements or apply settings immediately.
    /// </summary>
    public event Action<UserProfileData> OnProfileDataUpdated;


    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ProfileSyncSystem already exists, destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes

        Debug.Log("ProfileSyncSystem initialized. Attempting to load last active profile...");
        Initialize();
    }

    private void OnApplicationQuit()
    {
        // Ensure the active profile is saved when the application quits
        if (ActiveProfile != null)
        {
            SaveProfile(ActiveProfile.ProfileName);
            Debug.Log($"ProfileSyncSystem: Auto-saved active profile '{ActiveProfile.ProfileName}' on application quit.");
        }
    }

    // --- Public API Methods ---

    /// <summary>
    /// Initializes the ProfileSyncSystem.
    /// Attempts to load the last active profile, otherwise loads the default, or creates a new one.
    /// </summary>
    public void Initialize()
    {
        string lastActive = PlayerPrefs.GetString(LAST_ACTIVE_PROFILE_KEY, _defaultProfileName);

        if (_DoesProfileExist(lastActive))
        {
            LoadProfile(lastActive);
        }
        else if (_DoesProfileExist(_defaultProfileName)) // If last active deleted, try default
        {
            LoadProfile(_defaultProfileName);
        }
        else
        {
            // If neither exists, create and activate the default profile
            CreateProfile(_defaultProfileName);
        }

        Debug.Log($"ProfileSyncSystem: Initialized with active profile '{ActiveProfile?.ProfileName}'.");
    }

    /// <summary>
    /// Retrieves a list of all existing profile names saved in PlayerPrefs.
    /// </summary>
    public List<string> GetAllProfileNames()
    {
        // PlayerPrefs does not provide a direct way to list all keys with a prefix.
        // A common workaround is to store a master list of profile names in PlayerPrefs.
        // For simplicity in this example, we'll assume a separate key for all profile names.
        // A more robust system might iterate through all PlayerPrefs keys, but that's not exposed by Unity.
        // Here, we simulate by listing the active and default if they exist, and leave room for expansion.
        
        List<string> existingProfiles = new List<string>();
        if (_DoesProfileExist(_defaultProfileName) && !existingProfiles.Contains(_defaultProfileName))
        {
            existingProfiles.Add(_defaultProfileName);
        }
        if (ActiveProfile != null && !existingProfiles.Contains(ActiveProfile.ProfileName))
        {
            existingProfiles.Add(ActiveProfile.ProfileName);
        }
        // If we had a master list of all profile names:
        // string allProfileNamesJson = PlayerPrefs.GetString("AllProfileNamesList", "[]");
        // List<string> allNames = JsonUtility.FromJson<List<string>>(allProfileNamesJson); // Requires custom List serializer
        // For now, this is a basic approximation. A robust system would track all profile names explicitly.

        // A better approach for finding all profiles (if we don't have a master list)
        // This is a common hack: try loading numbers, or maintain a separate list of profile names.
        // For this example, let's keep it simple. If you need more dynamic discovery,
        // you'd typically have a `PlayerProfilesIndex` serializable class stored in PlayerPrefs.
        
        // For demonstration, let's just return what's loaded and default if existing.
        // In a real project, you'd save a list of all profile names to a specific PlayerPrefs key.
        List<string> allKnownProfiles = _loadedProfiles.Keys.ToList();
        if (_DoesProfileExist(_defaultProfileName) && !allKnownProfiles.Contains(_defaultProfileName))
        {
            allKnownProfiles.Add(_defaultProfileName);
        }
        
        // This is a placeholder for a more robust "get all profile names" method.
        // A truly dynamic list would require storing a list of profile names in a dedicated PlayerPrefs key.
        // Example: PlayerPrefs.SetString("ProfileNamesList", JsonUtility.ToJson(allNames));
        // And then: JsonUtility.FromJson<List<string>>(PlayerPrefs.GetString("ProfileNamesList", "[]"));
        
        return allKnownProfiles.Distinct().ToList(); // Ensure no duplicates
    }


    /// <summary>
    /// Creates a new profile with the given name, saves it, and sets it as the active profile.
    /// </summary>
    public void CreateProfile(string profileName)
    {
        if (_DoesProfileExist(profileName))
        {
            Debug.LogWarning($"Profile '{profileName}' already exists. Loading existing profile instead.", this);
            LoadProfile(profileName);
            return;
        }
        if (_loadedProfiles.ContainsKey(profileName))
        {
            Debug.LogWarning($"Profile '{profileName}' is already loaded. Setting as active.", this);
            SetActiveProfile(profileName);
            return;
        }

        UserProfileData newProfile = new UserProfileData(profileName);
        _loadedProfiles.Add(profileName, newProfile);
        _SaveProfileInternal(newProfile); // Save immediately
        SetActiveProfile(profileName); // Make it the active profile
        Debug.Log($"ProfileSyncSystem: Created and set active new profile: '{profileName}'.");
    }

    /// <summary>
    /// Loads a profile from persistent storage (PlayerPrefs) and sets it as the active profile.
    /// If the profile is already loaded, it just sets it as active.
    /// </summary>
    public void LoadProfile(string profileName)
    {
        if (_activeProfileName == profileName)
        {
            Debug.Log($"Profile '{profileName}' is already the active profile. No change needed.", this);
            return;
        }

        if (_loadedProfiles.TryGetValue(profileName, out UserProfileData loadedData))
        {
            // Profile is already in memory, just set it as active
            SetActiveProfile(profileName);
        }
        else
        {
            // Profile not in memory, load it from storage
            UserProfileData profileToLoad = _LoadProfileInternal(profileName);
            if (profileToLoad != null)
            {
                _loadedProfiles[profileName] = profileToLoad;
                SetActiveProfile(profileName);
            }
            else
            {
                Debug.LogError($"ProfileSyncSystem: Failed to load profile '{profileName}'. Does not exist in storage.", this);
            }
        }
    }

    /// <summary>
    /// Saves a specific profile from memory to persistent storage.
    /// </summary>
    public void SaveProfile(string profileName)
    {
        if (_loadedProfiles.TryGetValue(profileName, out UserProfileData profileToSave))
        {
            _SaveProfileInternal(profileToSave);
            Debug.Log($"ProfileSyncSystem: Saved profile '{profileName}'.");
        }
        else
        {
            Debug.LogWarning($"ProfileSyncSystem: Cannot save profile '{profileName}' as it is not loaded.", this);
        }
    }

    /// <summary>
    /// Saves the currently active profile to persistent storage.
    /// </summary>
    public void SaveActiveProfile()
    {
        if (ActiveProfile != null)
        {
            SaveProfile(ActiveProfile.ProfileName);
        }
        else
        {
            Debug.LogWarning("ProfileSyncSystem: No active profile to save.", this);
        }
    }
    
    /// <summary>
    /// Deletes a profile from both memory and persistent storage.
    /// If the deleted profile was active, it attempts to load the default profile.
    /// </summary>
    public void DeleteProfile(string profileName)
    {
        if (!_DoesProfileExist(profileName) && !_loadedProfiles.ContainsKey(profileName))
        {
            Debug.LogWarning($"Profile '{profileName}' does not exist and cannot be deleted.", this);
            return;
        }

        // Remove from loaded profiles
        if (_loadedProfiles.Remove(profileName))
        {
            Debug.Log($"Profile '{profileName}' removed from loaded profiles.");
        }

        // Remove from PlayerPrefs
        PlayerPrefs.DeleteKey(PROFILE_PLAYERPREFS_PREFIX + profileName);
        Debug.Log($"Profile '{profileName}' deleted from PlayerPrefs.");

        // If the deleted profile was the active one, clear it and try to load default
        if (_activeProfileName == profileName)
        {
            ActiveProfile = null;
            _activeProfileName = null;
            PlayerPrefs.DeleteKey(LAST_ACTIVE_PROFILE_KEY); // Clear last active
            Debug.Log($"Deleted active profile '{profileName}'. Attempting to load default profile.");
            Initialize(); // Re-initialize to load a default or new profile
        }
        PlayerPrefs.Save(); // Ensure changes are written to disk
    }

    /// <summary>
    /// Sets an existing loaded profile as the active one.
    /// This method is typically called internally after `LoadProfile` or `CreateProfile`.
    /// </summary>
    /// <param name="profileName">The name of the profile to make active.</param>
    private void SetActiveProfile(string profileName)
    {
        if (!_loadedProfiles.TryGetValue(profileName, out UserProfileData profileData))
        {
            Debug.LogError($"ProfileSyncSystem: Attempted to set non-loaded profile '{profileName}' as active.", this);
            return;
        }

        _activeProfileName = profileName;
        ActiveProfile = profileData;
        PlayerPrefs.SetString(LAST_ACTIVE_PROFILE_KEY, profileName);
        PlayerPrefs.Save(); // Persist the last active profile selection

        Debug.Log($"ProfileSyncSystem: Active profile switched to '{profileName}'.");
        OnActiveProfileChanged?.Invoke(ActiveProfile); // Notify subscribers
    }

    /// <summary>
    /// Call this method after you have modified data within the ActiveProfile.
    /// It updates the LastPlayed timestamp and notifies subscribers.
    /// </summary>
    public void UpdateActiveProfileData()
    {
        if (ActiveProfile != null)
        {
            ActiveProfile.MarkModified(); // Update timestamp
            OnProfileDataUpdated?.Invoke(ActiveProfile); // Notify subscribers
            // You might add an auto-save here if frequent updates are expected,
            // or rely on explicit Save calls / OnApplicationQuit.
            // Example: SaveActiveProfile();
            Debug.Log($"ProfileSyncSystem: Active profile '{ActiveProfile.ProfileName}' data updated.");
        }
        else
        {
            Debug.LogWarning("ProfileSyncSystem: Attempted to update data for a null active profile.", this);
        }
    }

    // --- Internal Persistence Helpers ---

    /// <summary>
    /// Checks if a profile exists in PlayerPrefs.
    /// </summary>
    private bool _DoesProfileExist(string profileName)
    {
        return PlayerPrefs.HasKey(PROFILE_PLAYERPREFS_PREFIX + profileName);
    }

    /// <summary>
    /// Internal method to load profile data from PlayerPrefs using JsonUtility.
    /// </summary>
    /// <returns>The UserProfileData object if successful, null otherwise.</returns>
    private UserProfileData _LoadProfileInternal(string profileName)
    {
        string key = PROFILE_PLAYERPREFS_PREFIX + profileName;
        if (PlayerPrefs.HasKey(key))
        {
            string jsonData = PlayerPrefs.GetString(key);
            try
            {
                UserProfileData loadedData = JsonUtility.FromJson<UserProfileData>(jsonData);
                loadedData.SyncLastPlayedFromDateString(); // Convert string to DateTime
                Debug.Log($"ProfileSyncSystem: Successfully loaded profile '{profileName}' from PlayerPrefs.");
                return loadedData;
            }
            catch (Exception e)
            {
                Debug.LogError($"ProfileSyncSystem: Failed to deserialize profile '{profileName}'. Error: {e.Message}. Data: {jsonData}", this);
                return null;
            }
        }
        Debug.LogWarning($"ProfileSyncSystem: Profile '{profileName}' not found in PlayerPrefs.", this);
        return null;
    }

    /// <summary>
    /// Internal method to save profile data to PlayerPrefs using JsonUtility.
    /// </summary>
    private void _SaveProfileInternal(UserProfileData profileData)
    {
        if (profileData == null)
        {
            Debug.LogError("ProfileSyncSystem: Attempted to save a null profile data.", this);
            return;
        }

        profileData.MarkModified(); // Ensure LastPlayed is up-to-date before saving
        string jsonData = JsonUtility.ToJson(profileData);
        string key = PROFILE_PLAYERPREFS_PREFIX + profileData.ProfileName;
        PlayerPrefs.SetString(key, jsonData);
        PlayerPrefs.Save(); // Ensure changes are written to disk
        Debug.Log($"ProfileSyncSystem: Saved profile '{profileData.ProfileName}' to PlayerPrefs.");
    }
}
```

---

## 3. Example Usage Script

This script demonstrates how other parts of your game would interact with the `ProfileSyncSystem`. It shows:
*   Subscribing to events.
*   Accessing the active profile data.
*   Modifying data.
*   Calling save methods.
*   Creating and switching profiles.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements if you want to use them

/// <summary>
/// This script serves as an example of how to use the ProfileSyncSystem.
/// Attach this to an empty GameObject in your scene.
/// It demonstrates subscribing to profile events, modifying data, and saving.
/// </summary>
public class ProfileSyncExample : MonoBehaviour
{
    [Header("UI References (Optional)")]
    public Text profileNameText;
    public Text profileDetailsText;
    public InputField newProfileNameInput;
    public Button createProfileButton;
    public Button loadProfileButton;
    public Dropdown profileSelectionDropdown;
    public Button saveButton;
    public Button modifySettingsButton;
    public Button modifyProgressButton;
    public Button resetProfileButton;
    public Button deleteProfileButton;

    private List<string> _availableProfileNames = new List<string>();

    private void Start()
    {
        // Subscribe to ProfileSyncSystem events
        // These events notify us when the active profile changes or its data is updated.
        ProfileSyncSystem.Instance.OnActiveProfileChanged += OnActiveProfileChanged;
        ProfileSyncSystem.Instance.OnProfileDataUpdated += OnProfileDataUpdated;

        // Populate initial UI
        UpdateProfileUI();
        PopulateProfileDropdown();

        // Setup UI button listeners (if UI elements are assigned)
        if (createProfileButton != null) createProfileButton.onClick.AddListener(CreateNewProfile);
        if (loadProfileButton != null) loadProfileButton.onClick.AddListener(LoadSelectedProfile);
        if (saveButton != null) saveButton.onClick.AddListener(SaveCurrentProfile);
        if (modifySettingsButton != null) modifySettingsButton.onClick.AddListener(ModifySettingsData);
        if (modifyProgressButton != null) modifyProgressButton.onClick.AddListener(ModifyProgressData);
        if (resetProfileButton != null) resetProfileButton.onClick.AddListener(ResetCurrentProfile);
        if (deleteProfileButton != null) deleteProfileButton.onClick.AddListener(DeleteSelectedProfile);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when this GameObject is destroyed.
        if (ProfileSyncSystem.Instance != null)
        {
            ProfileSyncSystem.Instance.OnActiveProfileChanged -= OnActiveProfileChanged;
            ProfileSyncSystem.Instance.OnProfileDataUpdated -= OnProfileDataUpdated;
        }

        // Clean up UI button listeners
        if (createProfileButton != null) createProfileButton.onClick.RemoveListener(CreateNewProfile);
        if (loadProfileButton != null) loadProfileButton.onClick.RemoveListener(LoadSelectedProfile);
        if (saveButton != null) saveButton.onClick.RemoveListener(SaveCurrentProfile);
        if (modifySettingsButton != null) modifySettingsButton.onClick.RemoveListener(ModifySettingsData);
        if (modifyProgressButton != null) modifyProgressButton.onClick.RemoveListener(ModifyProgressData);
        if (resetProfileButton != null) resetProfileButton.onClick.RemoveListener(ResetCurrentProfile);
        if (deleteProfileButton != null) deleteProfileButton.onClick.RemoveListener(DeleteSelectedProfile);
    }

    /// <summary>
    /// Event handler for when the active profile changes.
    /// This method will be called by ProfileSyncSystem.
    /// </summary>
    private void OnActiveProfileChanged(UserProfileData newProfile)
    {
        Debug.Log($"Example: Active profile changed to '{newProfile.ProfileName}'. Last Played: {newProfile.LastPlayed:G}");
        UpdateProfileUI();
        PopulateProfileDropdown(); // Update dropdown with new profile
    }

    /// <summary>
    /// Event handler for when the active profile's data is updated.
    /// This method will be called by ProfileSyncSystem.
    /// </summary>
    private void OnProfileDataUpdated(UserProfileData updatedProfile)
    {
        Debug.Log($"Example: Active profile '{updatedProfile.ProfileName}' data updated. Last Played: {updatedProfile.LastPlayed:G}");
        UpdateProfileUI();
    }

    /// <summary>
    /// Updates the UI text elements with the current active profile's data.
    /// </summary>
    private void UpdateProfileUI()
    {
        UserProfileData activeProfile = ProfileSyncSystem.Instance.ActiveProfile;

        if (activeProfile != null)
        {
            string profileInfo = $"Active Profile: {activeProfile.ProfileName}\n" +
                                 $"Last Played: {activeProfile.LastPlayed.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n\n" +
                                 $"--- Player Settings ---\n" +
                                 $"Master Volume: {activeProfile.Settings.MasterVolume:F2}\n" +
                                 $"Brightness: {activeProfile.Settings.Brightness:F2}\n" +
                                 $"Control Scheme: {activeProfile.Settings.ControlScheme}\n" +
                                 $"VSync: {activeProfile.Settings.VSyncEnabled}\n\n" +
                                 $"--- Game Progress ---\n" +
                                 $"Current Level: {activeProfile.Progress.CurrentLevel}\n" +
                                 $"High Score: {activeProfile.Progress.HighScore}\n" +
                                 $"Unlocked Abilities: {string.Join(", ", activeProfile.Progress.UnlockedAbilities)}\n" +
                                 $"Inventory: ";

            if (activeProfile.Progress.Inventory.Count > 0)
            {
                foreach (var item in activeProfile.Progress.Inventory)
                {
                    profileInfo += $"\n  - {item.ItemId} x{item.Quantity}";
                }
            }
            else
            {
                profileInfo += "None";
            }


            if (profileNameText != null) profileNameText.text = $"Current Profile: {activeProfile.ProfileName}";
            if (profileDetailsText != null) profileDetailsText.text = profileInfo;
        }
        else
        {
            if (profileNameText != null) profileNameText.text = "No Active Profile";
            if (profileDetailsText != null) profileDetailsText.text = "Load or Create a Profile.";
        }
    }

    /// <summary>
    /// Populates the dropdown menu with all available profile names.
    /// </summary>
    private void PopulateProfileDropdown()
    {
        if (profileSelectionDropdown == null) return;

        _availableProfileNames = ProfileSyncSystem.Instance.GetAllProfileNames();
        profileSelectionDropdown.ClearOptions();
        profileSelectionDropdown.AddOptions(_availableProfileNames);

        // Select the current active profile in the dropdown
        if (ProfileSyncSystem.Instance.ActiveProfile != null)
        {
            int activeIndex = _availableProfileNames.IndexOf(ProfileSyncSystem.Instance.ActiveProfile.ProfileName);
            if (activeIndex != -1)
            {
                profileSelectionDropdown.value = activeIndex;
            }
        }
    }

    /// <summary>
    /// Initiates the creation of a new profile based on user input.
    /// </summary>
    public void CreateNewProfile()
    {
        if (newProfileNameInput == null || string.IsNullOrWhiteSpace(newProfileNameInput.text))
        {
            Debug.LogWarning("Please enter a name for the new profile.");
            return;
        }

        string newName = newProfileNameInput.text.Trim();
        ProfileSyncSystem.Instance.CreateProfile(newName);
        newProfileNameInput.text = ""; // Clear input field
    }

    /// <summary>
    /// Initiates loading of the profile selected in the dropdown.
    /// </summary>
    public void LoadSelectedProfile()
    {
        if (profileSelectionDropdown == null || profileSelectionDropdown.options.Count == 0)
        {
            Debug.LogWarning("No profiles to load.");
            return;
        }

        string selectedProfileName = _availableProfileNames[profileSelectionDropdown.value];
        ProfileSyncSystem.Instance.LoadProfile(selectedProfileName);
    }
    
    /// <summary>
    /// Initiates deletion of the profile selected in the dropdown.
    /// </summary>
    public void DeleteSelectedProfile()
    {
        if (profileSelectionDropdown == null || profileSelectionDropdown.options.Count == 0)
        {
            Debug.LogWarning("No profiles to delete.");
            return;
        }

        string selectedProfileName = _availableProfileNames[profileSelectionDropdown.value];
        if (selectedProfileName == ProfileSyncSystem.Instance.ActiveProfile?.ProfileName)
        {
            // If deleting the active profile, the system will handle loading a default or creating one.
            Debug.Log($"Attempting to delete active profile '{selectedProfileName}'.");
        }
        else
        {
            Debug.Log($"Attempting to delete profile '{selectedProfileName}'.");
        }
        
        ProfileSyncSystem.Instance.DeleteProfile(selectedProfileName);
        PopulateProfileDropdown(); // Refresh dropdown after deletion
    }


    /// <summary>
    /// Saves the currently active profile.
    /// </summary>
    public void SaveCurrentProfile()
    {
        ProfileSyncSystem.Instance.SaveActiveProfile();
    }

    /// <summary>
    /// Modifies some settings data in the active profile.
    /// Remember to call UpdateActiveProfileData() after modifications.
    /// </summary>
    public void ModifySettingsData()
    {
        UserProfileData activeProfile = ProfileSyncSystem.Instance.ActiveProfile;
        if (activeProfile == null)
        {
            Debug.LogWarning("No active profile to modify settings.");
            return;
        }

        activeProfile.Settings.MasterVolume = Random.Range(0f, 1f);
        activeProfile.Settings.Brightness = Random.Range(0.5f, 1.5f);
        activeProfile.Settings.ControlScheme = (activeProfile.Settings.ControlScheme == "Default") ? "Alternate" : "Default";
        activeProfile.Settings.VSyncEnabled = !activeProfile.Settings.VSyncEnabled;

        // Important: Notify the system that the profile data has changed.
        ProfileSyncSystem.Instance.UpdateActiveProfileData();
        Debug.Log("Settings data modified in active profile.");
    }

    /// <summary>
    /// Modifies some game progress data in the active profile.
    /// Remember to call UpdateActiveProfileData() after modifications.
    /// </summary>
    public void ModifyProgressData()
    {
        UserProfileData activeProfile = ProfileSyncSystem.Instance.ActiveProfile;
        if (activeProfile == null)
        {
            Debug.LogWarning("No active profile to modify progress.");
            return;
        }

        activeProfile.Progress.CurrentLevel = Random.Range(1, 10);
        activeProfile.Progress.HighScore += Random.Range(100, 1000);

        // Example: Add a new random item to inventory
        string[] itemNames = { "ManaPotion", "Shield", "BootsOfSpeed", "Key" };
        string newItem = itemNames[Random.Range(0, itemNames.Length)];
        int existingItemIndex = activeProfile.Progress.Inventory.FindIndex(item => item.ItemId == newItem);
        if (existingItemIndex != -1)
        {
            InventoryItem existing = activeProfile.Progress.Inventory[existingItemIndex];
            activeProfile.Progress.Inventory[existingItemIndex] = new InventoryItem(existing.ItemId, existing.Quantity + 1);
        }
        else
        {
            activeProfile.Progress.Inventory.Add(new InventoryItem(newItem, 1));
        }

        // Example: Add a new ability
        string[] abilities = { "SuperJump", "Dash", "Glide" };
        string newAbility = abilities[Random.Range(0, abilities.Length)];
        if (!activeProfile.Progress.UnlockedAbilities.Contains(newAbility))
        {
            activeProfile.Progress.UnlockedAbilities.Add(newAbility);
        }

        // Important: Notify the system that the profile data has changed.
        ProfileSyncSystem.Instance.UpdateActiveProfileData();
        Debug.Log("Progress data modified in active profile.");
    }

    /// <summary>
    /// Resets the current active profile's data to default values.
    /// </summary>
    public void ResetCurrentProfile()
    {
        UserProfileData activeProfile = ProfileSyncSystem.Instance.ActiveProfile;
        if (activeProfile == null)
        {
            Debug.LogWarning("No active profile to reset.");
            return;
        }

        activeProfile.ResetProfileData();
        ProfileSyncSystem.Instance.UpdateActiveProfileData(); // Notify system of reset
        Debug.Log($"Active profile '{activeProfile.ProfileName}' data has been reset.");
    }
}
```

---

## How to Set Up in Unity:

1.  **Create C# Scripts:**
    *   Create a new C# script named `ProfileSyncSystem.cs` and paste the content from Section 2.
    *   Create a new C# script named `UserProfileData.cs` and paste the content from Section 1 (this script will contain `UserProfileData`, `PlayerSettings`, `GameProgress`, and `InventoryItem` classes).
    *   Create a new C# script named `ProfileSyncExample.cs` and paste the content from Section 3.

2.  **Create a Manager GameObject:**
    *   In your Unity scene, create an empty GameObject (e.g., named `_GameManagers`).
    *   Attach the `ProfileSyncSystem.cs` script to this `_GameManagers` GameObject. This will make it a Singleton that persists across scenes.

3.  **Create an Example GameObject:**
    *   Create another empty GameObject (e.g., named `ProfileExample`).
    *   Attach the `ProfileSyncExample.cs` script to this `ProfileExample` GameObject.

4.  **Create Basic UI (Optional, but Recommended for Interaction):**
    *   Go to `GameObject -> UI -> Canvas`.
    *   Right-click on the Canvas: `UI -> Text` (for profile name, details).
    *   Right-click on the Canvas: `UI -> Input Field` (for new profile name).
    *   Right-click on the Canvas: `UI -> Button` (for create, load, save, modify settings, modify progress, reset, delete).
    *   Right-click on the Canvas: `UI -> Dropdown` (for profile selection).
    *   Drag these UI elements into the public fields of the `ProfileSyncExample` script in the Inspector.

5.  **Run the Scene:**
    *   Press Play.
    *   The `ProfileSyncSystem` will initialize, loading the `Player1` profile by default (or creating it if it doesn't exist).
    *   You can use the UI to:
        *   Create new profiles (e.g., "Player2", "TestProfile").
        *   Load existing profiles.
        *   Modify settings and progress data.
        *   Save the current profile.
        *   Reset the current profile's data.
        *   Delete a profile.
    *   Check the Console for `Debug.Log` messages showing the system's operations.
    *   Stop the game and restart. The last active profile and its saved data should be loaded automatically.

This complete setup provides a robust and educational example of the ProfileSyncSystem pattern, ready to be integrated and extended in your Unity projects.