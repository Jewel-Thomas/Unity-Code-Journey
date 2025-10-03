// Unity Design Pattern Example: ColorGradingManager
// This script demonstrates the ColorGradingManager pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the "ColorGradingManager" design pattern. This pattern provides a centralized, easy-to-use system for managing and switching between different visual styles (represented by Unity's Post-processing Profiles) in your game. It uses the Singleton pattern to ensure global access and a `Dictionary` for efficient profile lookup.

**Key Concepts Demonstrated:**

1.  **Singleton Pattern:** Ensures a single instance of the manager exists throughout the application, providing a global access point.
2.  **Manager Pattern:** Centralizes control over a specific game aspect (in this case, color grading/post-processing profiles).
3.  **Data-Driven Design:** Allows defining and associating PostProcessProfiles with user-friendly string names directly in the Unity Inspector.
4.  **Decoupling:** Game logic (e.g., a `LevelManager` or `PlayerTrigger`) doesn't need direct references to `PostProcessVolume` components; it just tells the `ColorGradingManager` which profile to activate.

---

### Setup Instructions in Unity:

Before using the script, ensure you have the Post Processing Stack v2 installed and configured:

1.  **Install Post Processing Stack v2:**
    *   Go to `Window > Package Manager`.
    *   Select `Unity Registry` from the dropdown.
    *   Find and install `Post Processing`.

2.  **Create Post-Processing Assets:**
    *   In your Project window, right-click -> `Create` -> `Post-processing` -> `Post-process Profile`.
    *   Create several profiles (e.g., "DayProfile", "NightProfile", "HorrorProfile").
    *   Configure each profile with different `Color Grading` settings (and other effects as desired).

3.  **Set up your Camera and Global PostProcessVolume:**
    *   Select your `Main Camera`.
    *   Add a `Post Process Layer` component to it. Ensure its `Layer` dropdown matches the layer you'll use for your volume (e.g., "PostProcessing").
    *   Create an empty GameObject in your scene (e.g., `PostProcessVolumeGlobal`).
    *   Add a `Post Process Volume` component to this GameObject.
    *   Check the `Is Global` checkbox.
    *   Assign the `Layer` of this GameObject to a specific layer (e.g., "PostProcessing").
    *   **Crucially**, initially you can leave the `Profile` field blank on the Global Post Process Volume, or assign one of your profiles if you want a default on scene load. The manager will take over.

4.  **Create and Configure the `ColorGradingManager`:**
    *   Create an empty GameObject in your scene (e.g., `ColorGradingManager`).
    *   Attach the `ColorGradingManager.cs` script (provided below) to this GameObject.
    *   In the Inspector:
        *   Drag your `PostProcessVolumeGlobal` GameObject into the `_globalVolume` slot.
        *   Expand the `_profiles` list.
        *   Add new entries by increasing the `Size`. For each element:
            *   Set `Profile Name` (e.g., "Day", "Night", "Horror"). These are the strings you'll use in your code.
            *   Drag the corresponding `PostProcessProfile` asset (e.g., "DayProfile", "NightProfile", "HorrorProfile") into the `Profile Asset` slot.

---

### `ColorGradingManager.cs`

```csharp
using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // Essential for Post Processing functionality
using System.Collections.Generic;
using System; // For [Serializable]

/// <summary>
/// A serializable struct to pair a string name with a PostProcessProfile asset.
/// This allows us to define named color grading profiles directly in the Unity Inspector.
/// </summary>
[Serializable]
public struct ColorGradingProfileEntry
{
    [Tooltip("A unique identifier for this color grading profile.")]
    public string ProfileName;

    [Tooltip("The PostProcessProfile asset associated with this name.")]
    public PostProcessProfile ProfileAsset;
}

/// <summary>
/// The ColorGradingManager design pattern implementation.
/// This class acts as a central control point (a Singleton) for managing and switching
/// between different PostProcessProfiles, effectively changing the game's overall visual mood or 'color grade'.
/// </summary>
/// <remarks>
/// Pattern: Singleton, Manager
/// Purpose: Provides a single, globally accessible instance to control post-processing effects,
/// specifically focusing on 'Color Grading' by swapping PostProcessProfiles.
/// This allows other parts of the game (e.g., game state managers, area triggers) to
/// request a specific visual look without needing direct references to PostProcessVolumes.
/// </remarks>
public class ColorGradingManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    // A private static reference to the single instance of the manager.
    private static ColorGradingManager _instance;

    /// <summary>
    /// Provides the static instance of the ColorGradingManager.
    /// This is the primary way to access the manager from other scripts (e.g., ColorGradingManager.Instance.SwitchToProfile(...)).
    /// Ensures only one instance exists in the scene, creating one if it doesn't already exist.
    /// </summary>
    public static ColorGradingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene.
                _instance = FindObjectOfType<ColorGradingManager>();

                // If no instance exists, create a new GameObject and attach the manager script to it.
                // This ensures the manager is always available, even if not manually placed in the scene.
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(ColorGradingManager).Name);
                    _instance = singletonObject.AddComponent<ColorGradingManager>();
                    Debug.LogWarning($"ColorGradingManager not found in scene, creating a new one on '{singletonObject.name}'. " +
                                     "It's recommended to have ColorGradingManager pre-configured in your scene for proper setup.");
                }
            }
            return _instance;
        }
    }

    [Header("Color Grading Configuration")]

    [Tooltip("The global PostProcessVolume in your scene that this manager will control. " +
             "This volume should have 'Is Global' checked and a layer assigned that your camera's PostProcessLayer also sees.")]
    [SerializeField]
    private PostProcessVolume _globalVolume;

    [Tooltip("A list of named PostProcessProfiles. Define your different color grades/visual styles here.")]
    [SerializeField]
    private List<ColorGradingProfileEntry> _profiles = new List<ColorGradingProfileEntry>();

    // Internal dictionary for quick lookup of PostProcessProfiles by their string name.
    // This allows for O(1) access time when switching profiles.
    private Dictionary<string, PostProcessProfile> _profileDictionary = new Dictionary<string, PostProcessProfile>();

    // Stores the name of the currently active profile for reference and to prevent redundant switches.
    private string _currentProfileName;

    /// <summary>
    /// Gets the name of the currently active color grading profile.
    /// </summary>
    public string CurrentProfileName => _currentProfileName;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where the Singleton pattern is enforced and the profile dictionary is built.
    /// </summary>
    private void Awake()
    {
        // Enforce Singleton pattern: if another instance already exists, destroy this one.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this; // Set this as the active instance.

        // Optionally, make this GameObject persistent across scene loads if your post-processing needs
        // to carry over between scenes. For many games, post-processing is scene-specific.
        // DontDestroyOnLoad(gameObject);

        InitializeManager();
    }

    /// <summary>
    /// Initializes the manager by performing setup checks and building the dictionary of profiles
    /// from the list defined in the Inspector.
    /// </summary>
    private void InitializeManager()
    {
        // Critical check: Ensure a global volume is assigned. Without it, the manager cannot function.
        if (_globalVolume == null)
        {
            Debug.LogError("ColorGradingManager: No Global PostProcessVolume assigned! Please assign one in the Inspector. Disabling manager.");
            enabled = false; // Disable the script so it doesn't cause further errors.
            return;
        }

        _profileDictionary.Clear(); // Clear any previous data, useful for editor play/stop.
        foreach (var entry in _profiles)
        {
            if (string.IsNullOrEmpty(entry.ProfileName))
            {
                Debug.LogWarning("ColorGradingManager: A profile entry has an empty name. Skipping this entry.");
                continue;
            }
            if (entry.ProfileAsset == null)
            {
                Debug.LogWarning($"ColorGradingManager: Profile '{entry.ProfileName}' has no PostProcessProfile asset assigned. Skipping.");
                continue;
            }

            // Check for duplicate names to prevent unexpected behavior.
            if (_profileDictionary.ContainsKey(entry.ProfileName))
            {
                Debug.LogWarning($"ColorGradingManager: Duplicate profile name '{entry.ProfileName}' found. " +
                                 "The first entry will be used, later ones ignored.");
            }
            else
            {
                _profileDictionary.Add(entry.ProfileName, entry.ProfileAsset);
            }
        }

        // Attempt to determine the initial active profile.
        // If the global volume already has a profile assigned in the Inspector, find its name.
        if (_globalVolume.profile != null)
        {
            foreach (var entry in _profiles)
            {
                if (entry.ProfileAsset == _globalVolume.profile)
                {
                    _currentProfileName = entry.ProfileName;
                    break;
                }
            }
            if (string.IsNullOrEmpty(_currentProfileName))
            {
                Debug.Log($"ColorGradingManager: Initial volume profile '{_globalVolume.profile.name}' is not in the managed list. " +
                          "Consider adding it or explicitly switching to a known profile on start.");
            }
        }
        // If the global volume has no profile, and we have profiles defined, set the first one as default.
        else if (_profiles.Count > 0 && !string.IsNullOrEmpty(_profiles[0].ProfileName))
        {
            SwitchToProfile(_profiles[0].ProfileName, instant: true);
        }
    }

    /// <summary>
    /// Switches the global PostProcessVolume to the specified color grading profile.
    /// This is the primary public method to change the visual style of your game.
    /// </summary>
    /// <param name="profileName">The unique name of the profile to switch to, as defined in the Inspector.</param>
    /// <param name="instant">If true, the switch is immediate. Set to false if you want to implement a smooth transition (not fully implemented in this example for simplicity).</param>
    public void SwitchToProfile(string profileName, bool instant = true) // 'instant' parameter for future expansion
    {
        // Pre-checks to ensure the manager is ready to function.
        if (_globalVolume == null || !enabled)
        {
            Debug.LogError("ColorGradingManager: Cannot switch profile. Global PostProcessVolume is not assigned or manager is disabled.");
            return;
        }

        // If we are already on the requested profile, do nothing to avoid redundant assignments.
        if (string.Equals(_currentProfileName, profileName, StringComparison.OrdinalIgnoreCase))
        {
            // Debug.Log($"ColorGradingManager: Already on profile '{profileName}'. No switch needed.");
            return;
        }

        // Attempt to retrieve the target profile from our dictionary.
        if (_profileDictionary.TryGetValue(profileName, out PostProcessProfile targetProfile))
        {
            // --- Core Logic for switching profile ---
            // For this example, we're performing an instant switch by directly assigning the profile.
            _globalVolume.profile = targetProfile;
            _currentProfileName = profileName; // Update the current profile tracker.
            Debug.Log($"ColorGradingManager: Switched to profile: '{profileName}'");

            // --- Advanced Blending (Not fully implemented in this example) ---
            // If `instant` was false, you would typically implement a smooth transition here.
            // There are several ways to achieve smooth transitions between post-processing profiles:
            // 1. Cross-fading two PostProcessVolumes: Have two global volumes, one with the old profile and one with the new.
            //    Gradually decrease the weight of the old volume while increasing the weight of the new one.
            // 2. Runtime Profile Interpolation: Create a *new* PostProcessProfile instance at runtime.
            //    Assign this runtime profile to the global volume. Then, over time, manually interpolate
            //    the individual settings (e.g., ColorGrading.hueShift, Bloom.intensity) of this runtime
            //    profile from the values of the old profile to the values of the target profile.
            //    This is more complex as it requires knowing and interpolating many specific parameters.
            // For simplicity and clarity of the manager pattern itself, we stick to an instant switch.
        }
        else
        {
            // Log a warning if the requested profile name is not found.
            Debug.LogWarning($"ColorGradingManager: Profile '{profileName}' not found in the configured list.");
        }
    }

    /// <summary>
    /// Attempts to reset the color grading to a neutral or predefined "Default" profile.
    /// Useful for returning to a baseline look.
    /// </summary>
    public void ResetToDefaultProfile()
    {
        // Tries to switch to a profile explicitly named "Default".
        if (_profileDictionary.TryGetValue("Default", out PostProcessProfile defaultProfile))
        {
            SwitchToProfile("Default", instant: true);
        }
        // Fallback: if no "Default" profile, switch to the first one defined in the list.
        else if (_profiles.Count > 0 && !string.IsNullOrEmpty(_profiles[0].ProfileName))
        {
            SwitchToProfile(_profiles[0].ProfileName, instant: true);
        }
        // Last resort: clear the profile entirely, which will remove all post-processing effects.
        else
        {
            _globalVolume.profile = null;
            _currentProfileName = null;
            Debug.Log("ColorGradingManager: Cleared PostProcessVolume profile as no 'Default' or initial profile found.");
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Cleans up the Singleton instance reference.
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
```

---

### Example Usage in Other Scripts:

Here are two examples of how you might use the `ColorGradingManager` from other scripts in your game.

**1. `LevelManager.cs` (or `GameStateManager.cs`)**

This script demonstrates how to switch profiles based on game state changes.

```csharp
// Example Script: LevelManager.cs
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public enum GameState { Day, Night, Spooky }
    public GameState currentGameState = GameState.Day;

    void Start()
    {
        // Initial setup: switch to the day profile when the level starts.
        SetGameState(GameState.Day);
    }

    /// <summary>
    /// Changes the game's state and updates the color grading profile accordingly.
    /// </summary>
    /// <param name="newState">The new game state to transition to.</param>
    public void SetGameState(GameState newState)
    {
        // Only switch if the state is actually changing.
        if (currentGameState == newState) return;

        currentGameState = newState;
        string profileToSwitchTo = "";

        // Map game states to specific profile names defined in the ColorGradingManager.
        switch (currentGameState)
        {
            case GameState.Day:
                profileToSwitchTo = "Day"; // Corresponds to Profile Name "Day" in manager
                break;
            case GameState.Night:
                profileToSwitchTo = "Night"; // Corresponds to Profile Name "Night" in manager
                break;
            case GameState.Spooky:
                profileToSwitchTo = "Horror"; // Corresponds to Profile Name "Horror" in manager
                break;
            default:
                Debug.LogWarning($"LevelManager: Unhandled game state: {newState}. No profile change.");
                return;
        }

        // Use the ColorGradingManager's singleton instance to switch the profile.
        // This decouples the LevelManager from direct knowledge of the PostProcessVolume.
        if (!string.IsNullOrEmpty(profileToSwitchTo))
        {
            ColorGradingManager.Instance.SwitchToProfile(profileToSwitchTo);
            Debug.Log($"LevelManager: Switched to {currentGameState} profile (Profile: {profileToSwitchTo}).");
        }
    }

    // Example: A simple trigger to change game state (attach to a collider with Is Trigger checked)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Assuming your player has the "Player" tag
        {
            // When player enters a specific area, change the mood.
            if (gameObject.name == "SpookyAreaTrigger") // Name this GameObject "SpookyAreaTrigger" in editor
            {
                SetGameState(GameState.Spooky);
            }
            else if (gameObject.name == "DaylightAreaTrigger") // Name this GameObject "DaylightAreaTrigger" in editor
            {
                SetGameState(GameState.Day);
            }
        }
    }
}
```

**2. `PlayerController.cs` (for testing with input)**

This script demonstrates changing profiles based on player input, useful for quick testing or debug tools.

```csharp
// Example Script: PlayerController.cs
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    void Update()
    {
        // Check for specific key presses to switch color grading profiles.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Switch to the profile named "Day".
            ColorGradingManager.Instance.SwitchToProfile("Day");
            Debug.Log("Switched to Day profile via Input 1");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Switch to the profile named "Night".
            ColorGradingManager.Instance.SwitchToProfile("Night");
            Debug.Log("Switched to Night profile via Input 2");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Switch to the profile named "Horror".
            ColorGradingManager.Instance.SwitchToProfile("Horror");
            Debug.Log("Switched to Horror profile via Input 3");
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            // Reset to a default or neutral profile.
            ColorGradingManager.Instance.ResetToDefaultProfile();
            Debug.Log("Reset to default profile via Input 0");
        }
    }
}
```