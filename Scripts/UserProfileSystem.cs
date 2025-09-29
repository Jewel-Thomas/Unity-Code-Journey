// Unity Design Pattern Example: UserProfileSystem
// This script demonstrates the UserProfileSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The UserProfileSystem pattern is crucial in game development for managing player data, preferences, and progress. It centralizes all operations related to a player's profile, such as loading, saving, creating new profiles, and updating specific data points (e.g., level, currency, achievements).

This example demonstrates a complete, practical implementation of a UserProfileSystem in Unity using C#.

**Key Features:**
1.  **Singleton Pattern:** Ensures a single, globally accessible instance of the UserProfileSystem.
2.  **Persistent Data:** Saves and loads user profiles to/from JSON files in `Application.persistentDataPath`, making data persist across game sessions.
3.  **Multiple Profiles:** Supports managing multiple distinct user profiles.
4.  **Active Profile Management:** Allows setting and retrieving the currently active user profile.
5.  **Extensible `UserProfile` Data:** The `UserProfile` class is easily extendable to include more game-specific data.
6.  **Error Handling:** Basic error handling for file operations and duplicate usernames.
7.  **Unity Best Practices:** Uses `MonoBehaviour`, `Awake`, `OnApplicationQuit`, `[SerializeField]`, and `JsonUtility`.

---

### UserProfileSystem.cs

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO; // Required for file operations (File.Exists, File.ReadAllText, File.WriteAllText, Directory.CreateDirectory, Directory.GetFiles)

/// <summary>
/// Serializable class to hold a single user's profile data.
/// This data will be serialized to JSON and saved to disk.
/// </summary>
[Serializable]
public class UserProfile
{
    public string Username;
    public int Level;
    public int Coins;
    public List<string> UnlockedAchievements;
    public DateTime LastLogin; // Using DateTime for login tracking

    // Constructor to initialize a new profile with default values
    public UserProfile(string username)
    {
        Username = username;
        Level = 1;
        Coins = 100;
        UnlockedAchievements = new List<string>();
        LastLogin = DateTime.Now;
    }

    // Method to update last login time, called when profile is loaded or used
    public void UpdateLastLogin()
    {
        LastLogin = DateTime.Now;
    }

    // Example method to add an achievement
    public bool AddAchievement(string achievementName)
    {
        if (!UnlockedAchievements.Contains(achievementName))
        {
            UnlockedAchievements.Add(achievementName);
            Debug.Log($"Achievement Unlocked: {achievementName} for {Username}");
            return true;
        }
        Debug.LogWarning($"Achievement {achievementName} already unlocked for {Username}");
        return false;
    }
}


/// <summary>
/// The central UserProfileSystem MonoBehaviour.
/// Implements the Singleton pattern to provide a single, globally accessible instance.
/// Manages loading, saving, creating, and switching between user profiles.
/// </summary>
public class UserProfileSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static UserProfileSystem Instance { get; private set; }

    // --- Profile Data Storage ---
    // A dictionary to store all loaded user profiles, keyed by username for quick access.
    private Dictionary<string, UserProfile> _allProfiles = new Dictionary<string, UserProfile>();
    
    // The currently active user profile. This is the profile the game is currently using.
    public UserProfile CurrentUserProfile { get; private set; }

    // --- Configuration ---
    // The directory name where profiles will be saved within Application.persistentDataPath.
    [SerializeField]
    private string _profilesDirectoryName = "UserProfiles";
    private string _profilesFolderPath; // Full path to the profiles directory.
    private string _activeProfileFileName = "active_profile.txt"; // File to remember the last active profile.

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Implement the Singleton pattern:
        // Ensure only one instance of UserProfileSystem exists.
        if (Instance == null)
        {
            Instance = this;
            // Make sure the UserProfileSystem persists across scene loads.
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else
        {
            // If another instance already exists, destroy this one.
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        // When the application quits, save the current user profile (if one is active).
        // This ensures the latest changes are persisted.
        if (CurrentUserProfile != null)
        {
            SaveProfile(CurrentUserProfile);
            Debug.Log($"User profile '{CurrentUserProfile.Username}' saved on application quit.");
        }
        // Save the currently active profile's username for next launch
        SaveActiveProfileName();
    }

    // --- System Initialization ---

    private void InitializeSystem()
    {
        // Construct the full path to the profiles directory.
        _profilesFolderPath = Path.Combine(Application.persistentDataPath, _profilesDirectoryName);

        // Ensure the profiles directory exists. If not, create it.
        if (!Directory.Exists(_profilesFolderPath))
        {
            Directory.CreateDirectory(_profilesFolderPath);
            Debug.Log($"Created profiles directory: {_profilesFolderPath}");
        }

        // Load all existing profiles from disk.
        LoadAllProfilesFromDisk();

        // Attempt to load the last active profile
        string lastActiveUsername = LoadActiveProfileName();
        if (!string.IsNullOrEmpty(lastActiveUsername) && _allProfiles.ContainsKey(lastActiveUsername))
        {
            LoadProfile(lastActiveUsername);
            Debug.Log($"Automatically loaded last active profile: {lastActiveUsername}");
        }
        else if (_allProfiles.Count > 0)
        {
            // If no specific active profile, but profiles exist, load the first one.
            // Or leave CurrentUserProfile null and force user to select one.
            // For this example, let's leave it null if no active profile found.
            Debug.LogWarning("No previous active profile found or profile no longer exists. CurrentUserProfile is null.");
        }
    }

    // --- Profile Management Methods ---

    /// <summary>
    /// Creates a new user profile.
    /// </summary>
    /// <param name="username">The desired username for the new profile.</param>
    /// <returns>True if the profile was created successfully, false if a profile with that username already exists.</returns>
    public bool CreateNewProfile(string username)
    {
        if (_allProfiles.ContainsKey(username))
        {
            Debug.LogWarning($"Cannot create profile: A profile with username '{username}' already exists.");
            return false;
        }

        UserProfile newProfile = new UserProfile(username);
        _allProfiles.Add(username, newProfile);
        SaveProfile(newProfile); // Save the new profile immediately.
        Debug.Log($"New profile '{username}' created and saved.");
        
        // Optionally, set the new profile as the current active profile.
        // CurrentUserProfile = newProfile; 
        // SaveActiveProfileName(); // Remember this as the active profile
        return true;
    }

    /// <summary>
    /// Loads an existing user profile and sets it as the current active profile.
    /// </summary>
    /// <param name="username">The username of the profile to load.</param>
    /// <returns>True if the profile was loaded successfully, false if no such profile exists.</returns>
    public bool LoadProfile(string username)
    {
        if (_allProfiles.TryGetValue(username, out UserProfile profile))
        {
            CurrentUserProfile = profile;
            CurrentUserProfile.UpdateLastLogin(); // Update login time
            Debug.Log($"User profile '{username}' loaded successfully and set as current.");
            SaveActiveProfileName(); // Remember this as the active profile
            return true;
        }
        
        Debug.LogWarning($"Cannot load profile: Profile with username '{username}' does not exist.");
        CurrentUserProfile = null; // Ensure no stale profile is active
        return false;
    }

    /// <summary>
    /// Logs out the current user, setting CurrentUserProfile to null.
    /// </summary>
    public void LogoutProfile()
    {
        if (CurrentUserProfile != null)
        {
            SaveProfile(CurrentUserProfile); // Ensure current profile is saved before logging out
            Debug.Log($"User profile '{CurrentUserProfile.Username}' logged out and saved.");
            CurrentUserProfile = null;
            DeleteActiveProfileName(); // Clear the remembered active profile
        }
        else
        {
            Debug.LogWarning("No user profile is currently active to log out.");
        }
    }

    /// <summary>
    /// Deletes a user profile from the system and disk.
    /// </summary>
    /// <param name="username">The username of the profile to delete.</param>
    /// <returns>True if the profile was deleted successfully, false if no such profile exists.</returns>
    public bool DeleteProfile(string username)
    {
        if (!_allProfiles.ContainsKey(username))
        {
            Debug.LogWarning($"Cannot delete profile: Profile with username '{username}' does not exist.");
            return false;
        }

        // If the profile to be deleted is the current active profile, log it out first.
        if (CurrentUserProfile != null && CurrentUserProfile.Username == username)
        {
            LogoutProfile(); // This also handles saving and clearing active profile name
        }

        _allProfiles.Remove(username); // Remove from in-memory dictionary
        string profileFilePath = GetProfileFilePath(username);

        if (File.Exists(profileFilePath))
        {
            File.Delete(profileFilePath); // Delete the file from disk
            Debug.Log($"Profile '{username}' deleted from disk.");
            return true;
        }
        else
        {
            Debug.LogWarning($"Profile file for '{username}' not found at {profileFilePath}, but removed from memory.");
            return false;
        }
    }

    /// <summary>
    /// Retrieves a list of all available profile usernames.
    /// </summary>
    public List<string> GetAllProfileNames()
    {
        return new List<string>(_allProfiles.Keys);
    }

    // --- Persistence Methods ---

    /// <summary>
    /// Saves a specific user profile to its JSON file on disk.
    /// </summary>
    /// <param name="profile">The UserProfile object to save.</param>
    public void SaveProfile(UserProfile profile)
    {
        if (profile == null)
        {
            Debug.LogError("Cannot save a null profile.");
            return;
        }

        string json = JsonUtility.ToJson(profile, true); // 'true' for pretty printing JSON
        string profileFilePath = GetProfileFilePath(profile.Username);

        try
        {
            File.WriteAllText(profileFilePath, json);
            // Debug.Log($"Profile '{profile.Username}' saved to {profileFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save profile '{profile.Username}': {e.Message}");
        }
    }

    /// <summary>
    /// Loads all user profiles from the profiles directory into memory.
    /// </summary>
    private void LoadAllProfilesFromDisk()
    {
        _allProfiles.Clear(); // Clear any existing profiles in memory

        string[] profileFiles = Directory.GetFiles(_profilesFolderPath, "*.json");

        foreach (string filePath in profileFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                UserProfile profile = JsonUtility.FromJson<UserProfile>(json);
                if (profile != null && !_allProfiles.ContainsKey(profile.Username))
                {
                    _allProfiles.Add(profile.Username, profile);
                    Debug.Log($"Loaded profile: {profile.Username}");
                }
                else if (profile != null)
                {
                    Debug.LogWarning($"Duplicate profile detected for '{profile.Username}' from file {filePath}. Skipping.");
                }
                else
                {
                    Debug.LogError($"Failed to deserialize profile from {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading profile from {filePath}: {e.Message}");
            }
        }
        Debug.Log($"Finished loading {_allProfiles.Count} profiles.");
    }

    /// <summary>
    /// Gets the full file path for a given username's profile.
    /// </summary>
    private string GetProfileFilePath(string username)
    {
        return Path.Combine(_profilesFolderPath, $"{username}.json");
    }

    /// <summary>
    /// Saves the username of the currently active profile to a file.
    /// </summary>
    private void SaveActiveProfileName()
    {
        string filePath = Path.Combine(Application.persistentDataPath, _activeProfileFileName);
        if (CurrentUserProfile != null)
        {
            File.WriteAllText(filePath, CurrentUserProfile.Username);
        }
        else if (File.Exists(filePath))
        {
            // If no profile is active, but a file exists, clear it.
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Loads the username of the last active profile from a file.
    /// </summary>
    /// <returns>The username of the last active profile, or null if not found.</returns>
    private string LoadActiveProfileName()
    {
        string filePath = Path.Combine(Application.persistentDataPath, _activeProfileFileName);
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        return null;
    }

    /// <summary>
    /// Deletes the file that remembers the last active profile.
    /// </summary>
    private void DeleteActiveProfileName()
    {
        string filePath = Path.Combine(Application.persistentDataPath, _activeProfileFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // --- Example Usage and Debugging ---

    /// <summary>
    /// Example method to demonstrate modifying the current user's data.
    /// In a real game, this would be triggered by game events (e.g., player gains XP, completes quest).
    /// </summary>
    public void AwardCoinsToCurrentUser(int amount)
    {
        if (CurrentUserProfile != null)
        {
            CurrentUserProfile.Coins += amount;
            Debug.Log($"Awarded {amount} coins to {CurrentUserProfile.Username}. New balance: {CurrentUserProfile.Coins}");
            SaveProfile(CurrentUserProfile); // Persist the change immediately
        }
        else
        {
            Debug.LogWarning("No user profile active to award coins to.");
        }
    }

    /// <summary>
    /// Example method to demonstrate unlocking an achievement for the current user.
    /// </summary>
    public void UnlockAchievementForCurrentUser(string achievementName)
    {
        if (CurrentUserProfile != null)
        {
            if (CurrentUserProfile.AddAchievement(achievementName))
            {
                SaveProfile(CurrentUserProfile); // Persist the change immediately
            }
        }
        else
        {
            Debug.LogWarning("No user profile active to unlock achievement for.");
        }
    }
}
```

---

### How to use this in your Unity project:

1.  **Create the script:**
    *   Create a new C# script named `UserProfileSystem.cs` in your Unity project (e.g., in a `Scripts` folder).
    *   Copy and paste the entire code above into this script.

2.  **Add to a GameObject:**
    *   Create an empty GameObject in your first scene (e.g., a "Boot" or "System" scene). Name it `_UserProfileSystem`.
    *   Drag and drop the `UserProfileSystem.cs` script onto this new GameObject.
    *   Because it uses `DontDestroyOnLoad`, this GameObject (and thus the system) will persist across all your scenes.

3.  **Example Usage from another script:**

    Let's create a simple script to demonstrate interacting with the `UserProfileSystem`.

    ```csharp
    using UnityEngine;
    using System.Collections.Generic;

    public class UserProfileSystemDemo : MonoBehaviour
    {
        [Header("Profile Management")]
        public string newProfileUsername = "Player1";
        public string profileToLoad = "Player1";
        public string profileToDelete = "Player2";

        [Header("Current User Actions")]
        public int coinsToAward = 50;
        public string achievementToUnlock = "First Kill";

        void Start()
        {
            // The UserProfileSystem initializes itself in Awake.
            // We can now access it directly via its static Instance.

            Debug.Log("--- UserProfileSystem Demo Start ---");

            // 1. Create a new profile
            Debug.Log("\nAttempting to create a new profile: " + newProfileUsername);
            if (UserProfileSystem.Instance.CreateNewProfile(newProfileUsername))
            {
                Debug.Log($"Profile '{newProfileUsername}' created successfully.");
            }
            else
            {
                Debug.Log($"Profile '{newProfileUsername}' already exists or creation failed.");
            }

            // Create another profile for demonstration
            if (UserProfileSystem.Instance.CreateNewProfile("Player2"))
            {
                Debug.Log("Profile 'Player2' created.");
            }

            // 2. Load an existing profile
            Debug.Log("\nAttempting to load profile: " + profileToLoad);
            if (UserProfileSystem.Instance.LoadProfile(profileToLoad))
            {
                Debug.Log($"Profile '{profileToLoad}' loaded and set as current.");
                PrintCurrentProfileDetails();
            }
            else
            {
                Debug.Log($"Failed to load profile '{profileToLoad}'.");
            }

            // 3. Perform actions on the current profile
            if (UserProfileSystem.Instance.CurrentUserProfile != null)
            {
                Debug.Log("\nPerforming actions on current profile...");
                UserProfileSystem.Instance.AwardCoinsToCurrentUser(coinsToAward);
                UserProfileSystem.Instance.UnlockAchievementForCurrentUser(achievementToUnlock);
                UserProfileSystem.Instance.UnlockAchievementForCurrentUser("Game Started"); // Another achievement
                PrintCurrentProfileDetails(); // Print updated details
            }

            // 4. Logout (this will also save the current profile)
            Debug.Log("\nLogging out current profile...");
            UserProfileSystem.Instance.LogoutProfile();
            PrintCurrentProfileDetails(); // Should show no current profile

            // 5. Load another profile
            Debug.Log("\nAttempting to load profile: Player2");
            if (UserProfileSystem.Instance.LoadProfile("Player2"))
            {
                Debug.Log($"Profile 'Player2' loaded.");
                UserProfileSystem.Instance.AwardCoinsToCurrentUser(200); // Give Player2 some coins
                UserProfileSystem.Instance.UnlockAchievementForCurrentUser("Second Player Bonus");
                PrintCurrentProfileDetails();
            }

            // 6. Get all available profiles
            Debug.Log("\nAll available profiles:");
            List<string> allUsernames = UserProfileSystem.Instance.GetAllProfileNames();
            foreach (string username in allUsernames)
            {
                Debug.Log($"- {username}");
            }

            // 7. Delete a profile
            Debug.Log($"\nAttempting to delete profile: {profileToDelete}");
            if (UserProfileSystem.Instance.DeleteProfile(profileToDelete))
            {
                Debug.Log($"Profile '{profileToDelete}' deleted successfully.");
            }
            else
            {
                Debug.Log($"Failed to delete profile '{profileToDelete}'.");
            }

            Debug.Log("\n--- UserProfileSystem Demo End ---");
        }

        // Helper method to print details of the current profile
        void PrintCurrentProfileDetails()
        {
            if (UserProfileSystem.Instance.CurrentUserProfile != null)
            {
                UserProfile profile = UserProfileSystem.Instance.CurrentUserProfile;
                Debug.Log($"\n--- Current Profile: {profile.Username} ---");
                Debug.Log($"Level: {profile.Level}");
                Debug.Log($"Coins: {profile.Coins}");
                Debug.Log($"Last Login: {profile.LastLogin}");
                string achievements = profile.UnlockedAchievements.Count > 0 ? string.Join(", ", profile.UnlockedAchievements) : "None";
                Debug.Log($"Achievements: {achievements}");
                Debug.Log("-----------------------------");
            }
            else
            {
                Debug.Log("\nNo user profile is currently active.");
            }
        }
    }
    ```
    *   Create another empty GameObject in your scene, e.g., `_DemoManager`.
    *   Attach `UserProfileSystemDemo.cs` to it.
    *   Run the game, and observe the `Debug.Log` output in the Console window.
    *   You can modify `newProfileUsername`, `profileToLoad`, etc., in the Inspector of the `_DemoManager` GameObject.

### How to test persistence:

1.  Run the game.
2.  The demo script will create "Player1" (and maybe "Player2"), load "Player1", award coins/achievements, then log out.
3.  Stop the game.
4.  Run the game again.
5.  Observe the console: It should automatically load the "Player1" profile (because it was the last active one), and its data (coins, achievements) should reflect the changes made in the previous run.

You can also navigate to `Application.persistentDataPath` to see the generated JSON files:
*   On Windows: `C:\Users\<YourUser>\AppData\LocalLow\<CompanyName>\<ProductName>\UserProfiles\`
*   On macOS: `/Users/<YourUser>/Library/Application Support/<CompanyName>/<ProductName>/UserProfiles/`
*   On Android: `/storage/emulated/0/Android/data/<bundleID>/files/UserProfiles/`
*   On iOS: `Application/dataPath` (which is typically a sandboxed directory)

This setup provides a robust and flexible foundation for managing user profiles in any Unity game.