// Unity Design Pattern Example: SkillCooldownSystem
// This script demonstrates the SkillCooldownSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SkillCooldownSystem' design pattern is fundamental in many games for managing when player abilities, spells, or enemy actions can be used. It prevents spamming of powerful actions and introduces tactical depth.

This C# Unity example provides a complete, practical, and educational implementation of this pattern.

---

### Understanding the Skill Cooldown System Design Pattern

**Purpose:** To create a centralized and robust system for tracking and managing the cooldown status of various skills or abilities.

**Core Idea:** Each skill has a defined cooldown duration. Once a skill is used, it enters a "cooldown" state, during which it cannot be used again until a specified amount of time has passed.

**Key Components:**

1.  **Skill Definition:** A way to define each skill, typically including a unique identifier (like a string ID) and its base cooldown duration.
2.  **Cooldown Tracking:** A mechanism (like a dictionary) to store the current cooldown state for each active skill. This usually involves recording when the cooldown started and its total duration.
3.  **API for Interaction:** Methods that allow other game systems (e.g., player input, AI) to:
    *   Check if a skill is currently available (`CanUseSkill`).
    *   Initiate a skill's cooldown (`UseSkill`).
    *   Query the remaining cooldown time (`GetCooldownRemaining`).

**Advantages:**

*   **Centralized Management:** All cooldown logic is in one place, making it easy to manage, debug, and modify.
*   **Decoupling:** Game logic (e.g., player input, skill effects) doesn't need to directly manage timers for each skill. It just asks the `SkillCooldownSystem`.
*   **Flexibility:** Easily add, remove, or modify skills and their cooldowns without changing core game logic.
*   **Scalability:** Can handle a large number of skills efficiently using data structures like dictionaries.
*   **UI Integration:** Provides clear interfaces for UI elements to display cooldown status (e.g., progress bars, greyed-out icons).

---

### Complete C# Unity Example: `SkillCooldownSystem.cs`

This script is ready to be dropped into a Unity project.

1.  Create a new C# script named `SkillCooldownSystem`.
2.  Copy and paste the code below into the script.
3.  Create an empty GameObject in your scene (e.g., named "GameManagers" or "CooldownManager").
4.  Attach the `SkillCooldownSystem` component to this GameObject.
5.  In the Inspector, you can now define your initial skills (e.g., "Fireball" with 5s cooldown, "Dash" with 3s cooldown, "Heal" with 10s cooldown).

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

/// <summary>
/// Represents a skill's static data: its unique ID and its base cooldown duration.
/// This struct is marked [System.Serializable] so it can be configured directly
/// in the Unity Inspector.
/// </summary>
[System.Serializable]
public struct SkillData
{
    public string skillId;
    [Tooltip("The base cooldown duration in seconds.")]
    public float baseCooldownDuration;
}

/// <summary>
/// Represents the dynamic runtime information for a skill currently on cooldown.
/// This is a private class as its internal details are managed by the system.
/// </summary>
private class CooldownInfo
{
    // The Time.time (or Time.unscaledTime) when the cooldown started.
    public float cooldownStartTime;
    // The total duration for this specific cooldown instance.
    public float totalDuration;

    public CooldownInfo(float startTime, float duration)
    {
        cooldownStartTime = startTime;
        totalDuration = duration;
    }
}

/// <summary>
/// The main Skill Cooldown System MonoBehaviour.
/// Manages the cooldowns for multiple skills in a centralized manner.
/// </summary>
public class SkillCooldownSystem : MonoBehaviour
{
    [Header("Skill Definitions")]
    [Tooltip("Define skills and their base cooldowns here. These are registered on Awake.")]
    [SerializeField]
    private SkillData[] initialSkills;

    // A dictionary to store the currently active cooldowns.
    // Key: skillId (string)
    // Value: CooldownInfo (object holding start time and total duration).
    // This allows for quick O(1) average time complexity lookup of active cooldowns.
    private Dictionary<string, CooldownInfo> activeCooldowns = new Dictionary<string, CooldownInfo>();

    // A dictionary to quickly look up the base cooldown duration for any registered skill ID.
    // This is crucial because a skill's base duration is needed even when it's NOT on cooldown.
    private Dictionary<string, float> baseSkillDurations = new Dictionary<string, float>();

    [Header("Events")]
    [Tooltip("Event fired when a skill starts its cooldown. Use for UI updates.")]
    public event System.Action<string, float> onCooldownStarted; // Args: skillId, duration
    [Tooltip("Event fired when a skill's cooldown ends. Use for UI updates.")]
    public event System.Action<string> onCooldownEnded; // Args: skillId

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the base skill durations from the Inspector-configured initialSkills.
    /// </summary>
    void Awake()
    {
        InitializeSkills(initialSkills);
    }

    /// <summary>
    /// Populates the base skill durations dictionary from an array of SkillData.
    /// This method is called internally on Awake and can be used for runtime registration.
    /// </summary>
    /// <param name="skillsToRegister">An array of SkillData to register.</param>
    public void InitializeSkills(SkillData[] skillsToRegister)
    {
        foreach (var skill in skillsToRegister)
        {
            if (!baseSkillDurations.ContainsKey(skill.skillId))
            {
                baseSkillDurations.Add(skill.skillId, skill.baseCooldownDuration);
            }
            else
            {
                Debug.LogWarning($"SkillCooldownSystem: Duplicate skillId '{skill.skillId}' found during initialization. " +
                                 $"The existing base duration ({baseSkillDurations[skill.skillId]}s) will be kept.");
            }
        }
    }

    /// <summary>
    /// Registers a new skill with its cooldown duration at runtime.
    /// Can be used to add skills dynamically that weren't configured in the Inspector.
    /// If the skill already exists, its base duration will be updated.
    /// </summary>
    /// <param name="skillId">Unique identifier for the skill.</param>
    /// <param name="duration">The base cooldown duration in seconds.</param>
    public void RegisterSkill(string skillId, float duration)
    {
        if (!baseSkillDurations.ContainsKey(skillId))
        {
            baseSkillDurations.Add(skillId, duration);
            Debug.Log($"SkillCooldownSystem: Registered new skill '{skillId}' with {duration}s cooldown.");
        }
        else
        {
            // Optionally update the duration if the skill is re-registered with a new value.
            baseSkillDurations[skillId] = duration;
            Debug.Log($"SkillCooldownSystem: Skill '{skillId}' already registered. Updated its base duration to {duration}s.");
        }
    }

    /// <summary>
    /// Checks if a skill is currently on cooldown.
    /// If the cooldown has expired, it's removed from activeCooldowns.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <returns>True if the skill is currently on cooldown, false otherwise.</returns>
    public bool IsSkillOnCooldown(string skillId)
    {
        // First, check if the skill is even registered in our system.
        if (!baseSkillDurations.ContainsKey(skillId))
        {
            Debug.LogWarning($"SkillCooldownSystem: Attempted to check cooldown for unregistered skill '{skillId}'. Returning false.");
            return false;
        }

        // Try to get cooldown info from the activeCooldowns dictionary.
        if (activeCooldowns.TryGetValue(skillId, out CooldownInfo info))
        {
            // Calculate remaining time.
            float remainingTime = (info.cooldownStartTime + info.totalDuration) - Time.time;

            if (remainingTime > 0)
            {
                // Skill is still on cooldown.
                return true;
            }
            else
            {
                // Cooldown has expired. Clean up by removing it from the dictionary.
                activeCooldowns.Remove(skillId);
                onCooldownEnded?.Invoke(skillId); // Notify listeners (e.g., UI)
                return false;
            }
        }
        // If the skill is not in the activeCooldowns dictionary, it's not on cooldown.
        return false;
    }

    /// <summary>
    /// Determines if a skill can be used. A skill can be used if it's registered
    /// and not currently on cooldown.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <returns>True if the skill can be used, false otherwise.</returns>
    public bool CanUseSkill(string skillId)
    {
        // A skill can be used if it's not on cooldown (which also checks if it's registered).
        return !IsSkillOnCooldown(skillId);
    }

    /// <summary>
    /// Puts a skill on cooldown. This should be called after a skill has successfully activated.
    /// This method implicitly registers the skill with a 0-second cooldown if it's not known.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill.</param>
    public void UseSkill(string skillId)
    {
        // Ensure the skill is registered before attempting to use it.
        // If not registered, register it with a 0s cooldown (or handle as an error).
        if (!baseSkillDurations.TryGetValue(skillId, out float duration))
        {
            Debug.LogWarning($"SkillCooldownSystem: Attempted to use unregistered skill '{skillId}'. Registering it with 0s cooldown for now.");
            RegisterSkill(skillId, 0f); // Register with 0s cooldown as a fallback
            duration = 0f; // Set duration to 0 for immediate use in this call
            // If you prefer to strictly disallow usage of unregistered skills, uncomment the line below:
            // return;
        }

        // Add or update the skill in the activeCooldowns dictionary.
        // If it's already on cooldown, calling UseSkill again effectively resets its cooldown timer.
        activeCooldowns[skillId] = new CooldownInfo(Time.time, duration);

        // Notify listeners that a skill has started its cooldown.
        onCooldownStarted?.Invoke(skillId, duration);
        Debug.Log($"SkillCooldownSystem: '{skillId}' has been put on cooldown for {duration:F1}s.");
    }

    /// <summary>
    /// Gets the remaining cooldown time for a specific skill.
    /// If the skill is not on cooldown or not registered, returns 0.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <returns>The remaining cooldown time in seconds. Returns 0 if not on cooldown or skill not found.</returns>
    public float GetCooldownRemaining(string skillId)
    {
        // First, check if the skill is even registered.
        if (!baseSkillDurations.ContainsKey(skillId))
        {
            // Debug.LogWarning($"SkillCooldownSystem: Attempted to get remaining cooldown for unregistered skill '{skillId}'. Returning 0.");
            return 0f;
        }

        if (activeCooldowns.TryGetValue(skillId, out CooldownInfo info))
        {
            float remainingTime = (info.cooldownStartTime + info.totalDuration) - Time.time;

            // Ensure remaining time is not negative. If it's <= 0, the cooldown has expired.
            if (remainingTime <= 0)
            {
                // Cleanup: remove expired cooldown from the dictionary.
                activeCooldowns.Remove(skillId);
                onCooldownEnded?.Invoke(skillId); // Notify listeners
                return 0f;
            }
            return remainingTime;
        }
        // If the skill is not in the activeCooldowns dictionary, it's not on cooldown.
        return 0f;
    }

    /// <summary>
    /// Gets the normalized cooldown progress (0.0 to 1.0) for a skill.
    /// 0.0 means cooldown just started, 1.0 means cooldown has finished.
    /// This is useful for UI elements like progress bars.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill.</param>
    /// <returns>A float between 0.0 (cooldown just started) and 1.0 (cooldown finished). Returns 1.0 if not on cooldown or skill not found.</returns>
    public float GetCooldownProgressNormalized(string skillId)
    {
        if (activeCooldowns.TryGetValue(skillId, out CooldownInfo info))
        {
            // Avoid division by zero if totalDuration is somehow 0.
            if (info.totalDuration <= 0) return 1.0f; // Treat as immediately ready

            float elapsedTime = Time.time - info.cooldownStartTime;
            float progress = elapsedTime / info.totalDuration;
            return Mathf.Clamp01(progress); // Clamp between 0 and 1
        }
        // If not on cooldown, consider it 100% complete.
        return 1.0f;
    }

    /// <summary>
    /// Manually resets a specific skill's cooldown, making it available immediately.
    /// </summary>
    /// <param name="skillId">The unique identifier of the skill to reset.</param>
    public void ResetSkillCooldown(string skillId)
    {
        if (activeCooldowns.Remove(skillId))
        {
            onCooldownEnded?.Invoke(skillId);
            Debug.Log($"SkillCooldownSystem: Cooldown for skill '{skillId}' has been manually reset.");
        }
        else
        {
            Debug.Log($"SkillCooldownSystem: Skill '{skillId}' was not on cooldown to reset.");
        }
    }

    /// <summary>
    /// Resets all currently active skill cooldowns, making all skills immediately available.
    /// </summary>
    public void ResetAllCooldowns()
    {
        // Iterate over a copy of the keys to avoid modifying the collection while iterating.
        foreach (var skillId in new List<string>(activeCooldowns.Keys))
        {
            ResetSkillCooldown(skillId); // Calls the individual reset, which also fires events
        }
        activeCooldowns.Clear(); // Ensure it's fully cleared
        Debug.Log("SkillCooldownSystem: All skill cooldowns have been reset.");
    }
}

```

---

### Example Usage: `PlayerSkillController.cs` (Demonstration Script)

To show how another script would interact with the `SkillCooldownSystem`, create another C# script named `PlayerSkillController` and attach it to your player GameObject or any empty GameObject in the scene.

```csharp
using UnityEngine;

/// <summary>
/// This script demonstrates how a 'Player' or another game entity would interact
/// with the SkillCooldownSystem. It simulates trying to use skills via key presses.
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    // Reference to the SkillCooldownSystem instance in the scene.
    // It's good practice to find it once (e.g., in Awake or Start) or use a Singleton.
    private SkillCooldownSystem cooldownSystem;

    // Define skill IDs as constants for type safety and to avoid magic strings.
    private const string SKILL_FIREBALL = "Fireball";
    private const string SKILL_DASH = "Dash";
    private const string SKILL_HEAL = "Heal";
    private const string SKILL_ULTIMATE = "Ultimate"; // Example of a runtime registered skill

    void Awake()
    {
        // Attempt to find the SkillCooldownSystem in the scene.
        // For larger projects, consider a Singleton pattern for manager classes.
        cooldownSystem = FindObjectOfType<SkillCooldownSystem>();
        if (cooldownSystem == null)
        {
            Debug.LogError("PlayerSkillController: SkillCooldownSystem not found in the scene! Please add one.");
            enabled = false; // Disable this script if the system isn't found
            return;
        }

        // --- Runtime Skill Registration Example ---
        // You can register skills dynamically if they aren't configured in the Inspector.
        // For example, if a player unlocks a new ability.
        cooldownSystem.RegisterSkill(SKILL_ULTIMATE, 20.0f); // 20-second cooldown

        // --- Event Subscription Example ---
        // Subscribe to cooldown events to update UI or play effects
        cooldownSystem.onCooldownStarted += HandleCooldownStarted;
        cooldownSystem.onCooldownEnded += HandleCooldownEnded;

        Debug.Log("PlayerSkillController Ready. Press Q, W, E, R to try skills.");
    }

    void OnDestroy()
    {
        // It's crucial to unsubscribe from events to prevent memory leaks,
        // especially if the cooldownSystem is a persistent object (like a singleton)
        // and this PlayerSkillController might be destroyed and recreated.
        if (cooldownSystem != null)
        {
            cooldownSystem.onCooldownStarted -= HandleCooldownStarted;
            cooldownSystem.onCooldownEnded -= HandleCooldownEnded;
        }
    }

    void Update()
    {
        // Example: Using Fireball with 'Q' key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryUseSkill(SKILL_FIREBALL);
        }

        // Example: Using Dash with 'W' key
        if (Input.GetKeyDown(KeyCode.W))
        {
            TryUseSkill(SKILL_DASH);
        }

        // Example: Using Heal with 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryUseSkill(SKILL_HEAL);
        }

        // Example: Using Ultimate with 'R' key (runtime registered skill)
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryUseSkill(SKILL_ULTIMATE);
        }

        // Example: Reset all cooldowns with 'F' key
        if (Input.GetKeyDown(KeyCode.F))
        {
            cooldownSystem.ResetAllCooldowns();
        }

        // --- UI Update Simulation (for debugging, you'd typically update actual UI elements) ---
        // These logs show how you'd query the system for UI display.
        LogSkillCooldownStatus(SKILL_FIREBALL);
        LogSkillCooldownStatus(SKILL_DASH);
        LogSkillCooldownStatus(SKILL_HEAL);
        LogSkillCooldownStatus(SKILL_ULTIMATE);
    }

    /// <summary>
    /// Tries to use a skill by checking its cooldown status and then initiating its cooldown if successful.
    /// </summary>
    /// <param name="skillId">The ID of the skill to try and use.</param>
    private void TryUseSkill(string skillId)
    {
        if (cooldownSystem.CanUseSkill(skillId))
        {
            // The skill is ready to be used!
            Debug.Log($"<color=green>Player used {skillId}!</color>");
            // --- ACTUAL SKILL LOGIC GOES HERE ---
            // Example: Instantiate a projectile, apply movement, trigger healing, etc.
            // For this example, we'll just log it.

            cooldownSystem.UseSkill(skillId); // Put the skill on cooldown
        }
        else
        {
            // The skill is on cooldown.
            float remaining = cooldownSystem.GetCooldownRemaining(skillId);
            Debug.Log($"<color=red>{skillId} is on cooldown. Remaining: {remaining:F1}s</color>");
        }
    }

    /// <summary>
    /// Logs the current cooldown status of a skill.
    /// In a real game, this would update UI elements (e.g., skill icon, cooldown timer).
    /// </summary>
    /// <param name="skillId">The ID of the skill to log.</param>
    private void LogSkillCooldownStatus(string skillId)
    {
        // Check if the skill is even registered before querying.
        if (!cooldownSystem.baseSkillDurations.ContainsKey(skillId))
        {
            // Debug.LogWarning($"Skill '{skillId}' is not registered with the cooldown system.");
            return;
        }

        float remaining = cooldownSystem.GetCooldownRemaining(skillId);
        float progress = cooldownSystem.GetCooldownProgressNormalized(skillId);

        if (remaining > 0)
        {
            // Example for UI: Update a cooldown bar or timer text.
            // Debug.Log($"Cooldown for {skillId}: {remaining:F1}s remaining (Progress: {progress:P0})");
        }
        else
        {
            // Example for UI: Display skill as ready.
            // Debug.Log($"{skillId} is ready!");
        }
    }

    /// <summary>
    /// Event handler for when a skill's cooldown starts.
    /// This is where you'd typically update your UI to show a skill is on cooldown.
    /// </summary>
    /// <param name="skillId">The ID of the skill that started cooldown.</param>
    /// <param name="duration">The total duration of the cooldown.</param>
    private void HandleCooldownStarted(string skillId, float duration)
    {
        Debug.Log($"<color=blue>UI Update: {skillId} started cooldown for {duration:F1}s. Grey out icon, start timer.</color>");
        // Example: Play a sound effect, trigger an animation, update a UI element's color.
    }

    /// <summary>
    /// Event handler for when a skill's cooldown ends.
    /// This is where you'd typically update your UI to show a skill is available again.
    /// </summary>
    /// <param name="skillId">The ID of the skill whose cooldown ended.</param>
    private void HandleCooldownEnded(string skillId)
    {
        Debug.Log($"<color=blue>UI Update: {skillId} cooldown ended. Make icon bright, hide timer.</color>");
        // Example: Play a 'skill ready' sound, make the UI icon bright again.
    }
}
```