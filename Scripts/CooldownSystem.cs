// Unity Design Pattern Example: CooldownSystem
// This script demonstrates the CooldownSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Cooldown System is a fundamental design pattern in game development, especially useful for abilities, spells, or actions that shouldn't be spammable. It prevents players from using an action repeatedly within a short timeframe by enforcing a waiting period after each use.

This example provides a complete, practical, and well-commented C# Unity implementation of a `CooldownSystem`, along with a `PlayerAbilityController` to demonstrate its usage.

---

## CooldownSystem Design Pattern: Explained

**Purpose:** To manage and track the availability of various actions or abilities, ensuring they can only be used after a specified waiting period has elapsed since their last activation.

**Key Components:**

1.  **`CooldownSystem` Class:**
    *   Acts as the central manager for all cooldowns.
    *   Stores information about active cooldowns (which action, when it started, and its duration).
    *   Provides methods to check if an action is on cooldown, start a cooldown, get remaining time, and reset a cooldown.

2.  **`CooldownData` (Internal Class/Struct):**
    *   A simple data structure to hold the specific information for a single cooldown entry.
    *   Typically includes the cooldown's `duration` and the `timeWhenCooldownStarted` (or `timeWhenCooldownEnds`).

**How it Works:**

1.  **Registration/Starting:** When an action is successfully used, the `CooldownSystem` is instructed to `StartCooldown` for that specific action ID (e.g., "Fireball"). It records the current game time (`Time.time`) and the action's configured duration.
2.  **Checking Availability:** Before an action can be used, the game queries the `CooldownSystem` using `IsOnCooldown` for that action ID.
    *   The system checks if an entry exists for that action.
    *   If it exists, it calculates if `Time.time` has surpassed `timeWhenCooldownStarted + duration`. If not, the action is still on cooldown.
3.  **Time Tracking:** Unity's `Time.time` is ideal for this, as it represents the total elapsed time since the start of the game. By comparing current `Time.time` with the recorded `startTime` and `duration`, we can accurately determine if a cooldown has expired.
4.  **Retrieving Remaining Time:** The system can also provide the `GetRemainingCooldown` time, which is useful for displaying countdown timers in the UI.

**Benefits:**

*   **Decoupling:** The cooldown logic is separated from the ability logic. Abilities just ask the `CooldownSystem` if they can be used.
*   **Centralized Management:** All cooldowns are managed in one place, making it easy to add, remove, or modify them.
*   **Flexibility:** Easily supports different cooldown durations for different abilities.
*   **Scalability:** Can handle many abilities and actions without becoming complex.

---

### C# Unity Example Code

Here are the two scripts:

1.  **`CooldownSystem.cs`**: The core cooldown logic.
2.  **`PlayerAbilityController.cs`**: Demonstrates how to integrate and use the `CooldownSystem` in a typical player script.

---

**1. `CooldownSystem.cs`**

This script defines the `CooldownSystem` class and an internal `CooldownData` class. It manages all cooldowns using a dictionary.

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single cooldown entry with its duration and start time.
/// </summary>
internal class CooldownData
{
    public float CooldownDuration { get; private set; } // The total duration of the cooldown
    public float CooldownStartTime { get; private set; } // The Time.time when the cooldown started

    /// <summary>
    /// Initializes a new CooldownData instance.
    /// </summary>
    /// <param name="duration">The total time this cooldown should last.</param>
    /// <param name="startTime">The Time.time when this cooldown was initiated.</param>
    public CooldownData(float duration, float startTime)
    {
        CooldownDuration = duration;
        CooldownStartTime = startTime;
    }

    /// <summary>
    /// Calculates the time when this cooldown will expire.
    /// </summary>
    /// <returns>The Time.time value at which the cooldown ends.</returns>
    public float GetCooldownEndTime() => CooldownStartTime + CooldownDuration;

    /// <summary>
    /// Calculates the remaining time on the cooldown.
    /// </summary>
    /// <param name="currentTime">The current Time.time to compare against.</param>
    /// <returns>The remaining time in seconds. Returns 0 if the cooldown has expired or is not active.</returns>
    public float GetRemainingTime(float currentTime) => Mathf.Max(0f, GetCooldownEndTime() - currentTime);

    /// <summary>
    /// Checks if the cooldown is currently active.
    /// </summary>
    /// <param name="currentTime">The current Time.time to compare against.</param>
    /// <returns>True if the cooldown is still active, false otherwise.</returns>
    public bool IsOnCooldown(float currentTime) => currentTime < GetCooldownEndTime();
}

/// <summary>
/// A central system for managing various cooldowns for different actions or abilities.
/// This class is not a MonoBehaviour, allowing it to be instantiated and used by other classes.
/// </summary>
public class CooldownSystem
{
    // Dictionary to store active cooldowns, mapping an action ID (string) to its CooldownData.
    private readonly Dictionary<string, CooldownData> _cooldowns = new Dictionary<string, CooldownData>();

    /// <summary>
    /// Starts or resets a cooldown for a specific action.
    /// If the action is already on cooldown, its cooldown will be refreshed with the new duration.
    /// </summary>
    /// <param name="actionId">A unique string identifier for the action (e.g., "Fireball", "Dash").</param>
    /// <param name="duration">The duration of the cooldown in seconds.</param>
    public void StartCooldown(string actionId, float duration)
    {
        if (duration < 0)
        {
            Debug.LogWarning($"CooldownSystem: Attempted to start cooldown for '{actionId}' with negative duration ({duration}). Cooldown will not be set.");
            return;
        }

        // Store or update the cooldown data with the current Time.time as its start time.
        // Using Time.time ensures consistency even if Time.timeScale changes.
        _cooldowns[actionId] = new CooldownData(duration, Time.time);
        Debug.Log($"CooldownSystem: Started cooldown for '{actionId}' for {duration:F2} seconds. Ends at {Time.time + duration:F2}.");
    }

    /// <summary>
    /// Checks if a specific action is currently on cooldown.
    /// </summary>
    /// <param name="actionId">The unique string identifier for the action.</param>
    /// <returns>True if the action is currently on cooldown, false otherwise or if the action has no cooldown registered.</returns>
    public bool IsOnCooldown(string actionId)
    {
        if (_cooldowns.TryGetValue(actionId, out CooldownData data))
        {
            // If the cooldown data exists, check if it's still active based on current Time.time.
            return data.IsOnCooldown(Time.time);
        }
        // If no cooldown data is found for the action, it's not on cooldown.
        return false;
    }

    /// <summary>
    /// Gets the remaining time for a specific action's cooldown.
    /// </summary>
    /// <param name="actionId">The unique string identifier for the action.</param>
    /// <returns>The remaining cooldown time in seconds. Returns 0 if the action is not on cooldown or has no cooldown registered.</returns>
    public float GetRemainingCooldown(string actionId)
    {
        if (_cooldowns.TryGetValue(actionId, out CooldownData data))
        {
            // If cooldown data exists, return its remaining time.
            return data.GetRemainingTime(Time.time);
        }
        // If no cooldown data is found, there's no remaining time.
        return 0f;
    }

    /// <summary>
    /// Forcefully removes an action from cooldown, making it immediately available.
    /// </summary>
    /// <param name="actionId">The unique string identifier for the action.</param>
    public void ResetCooldown(string actionId)
    {
        if (_cooldowns.Remove(actionId))
        {
            Debug.Log($"CooldownSystem: Cooldown for '{actionId}' was manually reset.");
        }
    }

    /// <summary>
    /// Removes all active cooldowns.
    /// </summary>
    public void ResetAllCooldowns()
    {
        _cooldowns.Clear();
        Debug.Log("CooldownSystem: All cooldowns have been reset.");
    }
}
```

---

**2. `PlayerAbilityController.cs`**

This script demonstrates how a `MonoBehaviour` (like a player character or an NPC) would utilize the `CooldownSystem`. It defines a few example abilities and checks their cooldowns before execution.

```csharp
using UnityEngine;

/// <summary>
/// Demonstrates the usage of the CooldownSystem for various player abilities.
/// This script would typically be attached to a Player GameObject.
/// </summary>
public class PlayerAbilityController : MonoBehaviour
{
    // --- CooldownSystem Instance ---
    // The CooldownSystem is instantiated here. It's a plain C# class, not a MonoBehaviour,
    // so it needs to be created or provided.
    private CooldownSystem _cooldownSystem;

    // --- Ability Definitions (Configurable in Inspector) ---
    [Header("Ability Cooldowns")]
    [SerializeField] private float _fireballCooldown = 3.0f;
    [SerializeField] private float _healCooldown = 8.0f;
    [SerializeField] private float _dashCooldown = 1.5f;
    [SerializeField] private float _ultimateCooldown = 60.0f;

    // --- Ability IDs (Constants for clarity and typo prevention) ---
    // Using string constants or enums for action IDs is a good practice.
    // Strings are flexible, enums offer compile-time checking.
    private const string FIREBALL_ABILITY_ID = "Fireball";
    private const string HEAL_ABILITY_ID = "Heal";
    private const string DASH_ABILITY_ID = "Dash";
    private const string ULTIMATE_ABILITY_ID = "Ultimate";


    private void Awake()
    {
        // Initialize the CooldownSystem when the object awakes.
        // This ensures it's ready before any abilities are attempted.
        _cooldownSystem = new CooldownSystem();
        Debug.Log("PlayerAbilityController: CooldownSystem initialized.");
    }

    private void Update()
    {
        // --- Fireball Ability (Key: Q) ---
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryUseAbility(FIREBALL_ABILITY_ID, _fireballCooldown, "Cast Fireball!", "Pew! (Fireball)");
        }

        // --- Heal Ability (Key: E) ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryUseAbility(HEAL_ABILITY_ID, _healCooldown, "Heal self!", "Zzzzz... (Heal)");
        }

        // --- Dash Ability (Key: Space) ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryUseAbility(DASH_ABILITY_ID, _dashCooldown, "Dash forward!", "Whoosh! (Dash)");
        }

        // --- Ultimate Ability (Key: R) ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryUseAbility(ULTIMATE_ABILITY_ID, _ultimateCooldown, "Unleash Ultimate!", "KA-BOOM! (Ultimate)");
        }

        // Optional: Display remaining cooldowns in the console for monitoring
        DisplayAllCooldowns();
    }

    /// <summary>
    /// Generic method to attempt using an ability, checking its cooldown first.
    /// </summary>
    /// <param name="abilityId">The unique ID of the ability.</param>
    /// <param name="cooldownDuration">The duration of the cooldown for this ability.</param>
    /// <param name="abilityMessage">Message to display when ability is successfully used.</param>
    /// <param name="cooldownMessage">Message to display when ability is on cooldown.</param>
    private void TryUseAbility(string abilityId, float cooldownDuration, string abilityMessage, string cooldownMessage)
    {
        // 1. Check if the ability is on cooldown.
        if (_cooldownSystem.IsOnCooldown(abilityId))
        {
            float remainingTime = _cooldownSystem.GetRemainingCooldown(abilityId);
            Debug.LogWarning($"{cooldownMessage} - {abilityId} is on cooldown! Remaining: {remainingTime:F2}s");
        }
        else
        {
            // 2. If not on cooldown, execute the ability logic.
            // In a real game, this would call a method that handles the ability's effects.
            Debug.Log($"<color=green>{abilityMessage}</color>");

            // 3. Start the cooldown for this ability.
            _cooldownSystem.StartCooldown(abilityId, cooldownDuration);
        }
    }

    /// <summary>
    /// Displays the remaining cooldowns for all registered abilities.
    /// This is for debugging/demonstration purposes. In a real game, this would update UI.
    /// </summary>
    private void DisplayAllCooldowns()
    {
        string cooldownStatus = "--- Cooldown Status ---";
        float fbRemaining = _cooldownSystem.GetRemainingCooldown(FIREBALL_ABILITY_ID);
        float healRemaining = _cooldownSystem.GetRemainingCooldown(HEAL_ABILITY_ID);
        float dashRemaining = _cooldownSystem.GetRemainingCooldown(DASH_ABILITY_ID);
        float ultRemaining = _cooldownSystem.GetRemainingCooldown(ULTIMATE_ABILITY_ID);

        // Only show if there's actually a cooldown.
        if (fbRemaining > 0) cooldownStatus += $"\n{FIREBALL_ABILITY_ID}: {fbRemaining:F1}s";
        if (healRemaining > 0) cooldownStatus += $"\n{HEAL_ABILITY_ID}: {healRemaining:F1}s";
        if (dashRemaining > 0) cooldownStatus += $"\n{DASH_ABILITY_ID}: {dashRemaining:F1}s";
        if (ultRemaining > 0) cooldownStatus += $"\n{ULTIMATE_ABILITY_ID}: {ultRemaining:F1}s";

        // To prevent spamming the console, only log if there's an actual cooldown or if no cooldowns are active.
        // For a more dynamic display, consider using UI Text elements.
        if (Time.frameCount % 30 == 0) // Log every 30 frames (approx. every 0.5 seconds at 60 FPS)
        {
            // Only log if there are *any* active cooldowns to avoid constant "--- Cooldown Status ---"
            if (fbRemaining > 0 || healRemaining > 0 || dashRemaining > 0 || ultRemaining > 0)
            {
                Debug.Log(cooldownStatus);
            }
        }
    }

    // Example of manually resetting a cooldown (e.g., via a power-up or cheat code)
    private void OnGUI()
    {
        // Create a simple button for demonstration purposes to reset the Ultimate cooldown.
        // This is not for production UI, just to show the `ResetCooldown` functionality.
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 20;
        if (GUI.Button(new Rect(10, 10, 200, 50), $"Reset {ULTIMATE_ABILITY_ID} (C)", style))
        {
            _cooldownSystem.ResetCooldown(ULTIMATE_ABILITY_ID);
        }
        if (GUI.Button(new Rect(10, 70, 200, 50), "Reset All Cooldowns (X)", style))
        {
            _cooldownSystem.ResetAllCooldowns();
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create C# Scripts:**
    *   In your Unity Project window, right-click -> `Create` -> `C# Script`.
    *   Name one `CooldownSystem` and paste the content of `CooldownSystem.cs` into it.
    *   Name the other `PlayerAbilityController` and paste the content of `PlayerAbilityController.cs` into it.

2.  **Create a GameObject:**
    *   In your Unity Hierarchy window, right-click -> `Create Empty`.
    *   Name it `Player` (or `AbilityManager`).

3.  **Attach `PlayerAbilityController`:**
    *   Drag the `PlayerAbilityController.cs` script from your Project window onto the `Player` GameObject in the Hierarchy.

4.  **Run the Scene:**
    *   Press the `Play` button in the Unity Editor.
    *   **Observe the Console:**
        *   Press `Q` for Fireball, `E` for Heal, `Space` for Dash, `R` for Ultimate.
        *   You'll see messages indicating successful ability use and when abilities are on cooldown.
        *   Try spamming a key; you'll get "on cooldown" warnings.
        *   The console will also periodically show remaining cooldowns.
    *   **Test Reset Buttons:** The `OnGUI` method in `PlayerAbilityController` creates two buttons in the top-left corner of your Game view: "Reset Ultimate" and "Reset All Cooldowns". Click them to instantly make abilities available again.

---

### Key Takeaways for Unity Developers:

*   **Plain C# Classes for Logic:** The `CooldownSystem` is a standard C# class, *not* a `MonoBehaviour`. This makes it reusable, testable, and independent of specific Unity GameObjects. It's owned and instantiated by a `MonoBehaviour` (like `PlayerAbilityController`) that needs its functionality.
*   **`Time.time` is Your Friend:** Always use `Time.time` for tracking game time for cooldowns, rather than accumulating `Time.deltaTime`. `Time.time` provides the absolute game time, which is robust against frame rate fluctuations and `Time.timeScale` changes (if you want cooldowns to be affected by time scale, otherwise use `Time.unscaledTime`).
*   **Action IDs:** Using `string` constants (or `enum`s) for `actionId`s is crucial for preventing typos and making your code more readable and maintainable than magic strings.
*   **Encapsulation:** The `CooldownData` class is `internal`, meaning it's only accessible within the assembly, keeping the internal workings of `CooldownSystem` private.
*   **Inspector Configurability:** `[SerializeField]` allows you to easily tweak cooldown durations in the Unity Inspector without changing code.
*   **Clear Debugging:** `Debug.Log` and `Debug.LogWarning` provide excellent feedback in the console, which is invaluable during development.
*   **UI Integration (Next Step):** In a real game, instead of `Debug.Log` for remaining cooldowns, you would update UI elements (Text, Image fills) to visually represent the cooldown status to the player. The `GetRemainingCooldown` method is perfect for this.

This example provides a solid foundation for implementing a robust and flexible cooldown system in any Unity project.