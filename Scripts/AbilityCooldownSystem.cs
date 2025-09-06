// Unity Design Pattern Example: AbilityCooldownSystem
// This script demonstrates the AbilityCooldownSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Ability Cooldown System design pattern**. It provides a flexible and organized way to manage abilities and their cooldowns for game entities like players or enemies.

**Key Components of the Pattern:**

1.  **`AbilityData` (ScriptableObject):** Defines the *type* of an ability, including its name, cooldown duration, and a placeholder for its effect. This allows designers to create and configure abilities directly in the Unity Editor.
2.  **`AbilityCooldownSystem` (MonoBehaviour):** The central manager component. It tracks the cooldown state for all abilities assigned to a particular entity. It provides methods to check an ability's readiness, attempt to activate it (and start its cooldown), and query remaining cooldown times.
3.  **`PlayerAbilityUser` (MonoBehaviour):** An example client that demonstrates how to interact with the `AbilityCooldownSystem`. It binds abilities to input keys and can display their cooldown status (e.g., via UI).

---

### **1. AbilityData.cs**

This ScriptableObject defines the properties of an ability.

```csharp
using UnityEngine;

/// <summary>
/// A ScriptableObject representing the definition of an ability.
/// This allows designers to create various ability types in the Unity Editor,
/// separate from their runtime management or specific usage.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability Data", order = 1)]
public class AbilityData : ScriptableObject
{
    [Header("Ability Information")]
    [Tooltip("The unique name of this ability.")]
    public string abilityName = "New Ability";

    [Tooltip("The duration (in seconds) this ability will be on cooldown after use.")]
    public float cooldownDuration = 5.0f;

    [Tooltip("A brief description of what the ability does.")]
    [TextArea(3, 5)]
    public string description = "A generic ability.";

    // --- You can add more properties here depending on your game's needs, e.g.: ---
    // public Sprite icon;               // For UI representation
    // public float manaCost;             // Resource cost
    // public GameObject visualEffectPrefab; // Visuals to instantiate
    // public AudioClip soundEffect;      // Sound to play
    // public int damageAmount;           // Damage dealt
    // public EffectType[] effects;       // Complex effects data

    /// <summary>
    /// This method simulates the actual effect of the ability.
    /// In a real game, this would trigger specific game logic (e.g., deal damage,
    /// apply a buff, spawn a projectile, play animations/sounds).
    /// </summary>
    public virtual void ExecuteAbilityEffect()
    {
        // For demonstration, we just log a message.
        // In a real game, you would implement the actual mechanics here or
        // delegate to a specialized "AbilityHandler" component.
        Debug.Log($"<color=cyan>Ability '{abilityName}' executed its effect!</color>");

        // Example: Instantiate a particle effect
        // if (visualEffectPrefab != null)
        // {
        //     GameObject effect = Instantiate(visualEffectPrefab, Vector3.zero, Quaternion.identity);
        //     Destroy(effect, 2f); // Clean up after a duration
        // }

        // Example: Play a sound
        // if (soundEffect != null)
        // {
        //     AudioSource.PlayClipAtPoint(soundEffect, Camera.main.transform.position);
        // }
    }
}
```

---

### **2. AbilityCooldownSystem.cs**

This is the core manager for ability cooldowns.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() to safely iterate dictionaries

/// <summary>
/// The AbilityCooldownSystem is a central manager that tracks the cooldown status
/// of multiple abilities for a single game entity (e.g., a player, an enemy NPC).
/// It implements the Ability Cooldown System design pattern by providing:
/// 1. A unified way to assign and manage abilities.
/// 2. Functionality to check if an ability is currently ready.
/// 3. A method to activate an ability and initiate its cooldown.
/// 4. Methods to query remaining cooldown time and normalized progress.
/// 5. Dynamic addition/removal of abilities at runtime.
/// </summary>
public class AbilityCooldownSystem : MonoBehaviour
{
    [Header("Assigned Abilities")]
    [Tooltip("List of AbilityData ScriptableObjects that this entity can use. " +
             "These abilities will be initialized with no cooldown at the start of the game.")]
    public List<AbilityData> assignedAbilities = new List<AbilityData>();

    // Dictionary to store the current remaining cooldown time for each ability.
    // Key: AbilityData ScriptableObject (represents the ability type)
    // Value: float (remaining time in seconds until the ability is ready again)
    private Dictionary<AbilityData, float> _activeCooldowns = new Dictionary<AbilityData, float>();

    // Dictionary to store the *maximum* cooldown duration for quick lookup
    // This is useful for calculating normalized progress and for clarity, though
    // AbilityData.cooldownDuration could also be directly accessed.
    private Dictionary<AbilityData, float> _maxCooldownDurations = new Dictionary<AbilityData, float>();

    void Awake()
    {
        // Initialize the system when the GameObject awakens.
        InitializeCooldowns();
    }

    /// <summary>
    /// Initializes the cooldown system. All abilities defined in 'assignedAbilities'
    /// are added to the tracking system and are set to be immediately ready (0 cooldown).
    /// </summary>
    private void InitializeCooldowns()
    {
        _activeCooldowns.Clear();
        _maxCooldownDurations.Clear();

        foreach (var ability in assignedAbilities)
        {
            if (ability != null)
            {
                // Initially, all abilities are ready to use.
                _activeCooldowns[ability] = 0f;
                // Store max duration for calculation convenience (e.g., UI bars).
                _maxCooldownDurations[ability] = ability.cooldownDuration;
            }
            else
            {
                Debug.LogWarning($"A null AbilityData was found in 'assignedAbilities' on {gameObject.name}. Please remove it.");
            }
        }
        Debug.Log($"<color=green>AbilityCooldownSystem initialized on '{gameObject.name}' with {assignedAbilities.Count} abilities.</color>");
    }

    /// <summary>
    /// Called once per frame. This is where cooldown timers are decremented.
    /// </summary>
    void Update()
    {
        // Iterate through all abilities that are currently on cooldown.
        // We iterate over a copy of the keys to safely modify the dictionary if an ability's cooldown finishes.
        var abilitiesOnCooldown = _activeCooldowns.Keys.ToList();
        foreach (var ability in abilitiesOnCooldown)
        {
            // Only update if the ability is still active in the dictionary and its cooldown is positive.
            if (_activeCooldowns.ContainsKey(ability) && _activeCooldowns[ability] > 0)
            {
                _activeCooldowns[ability] -= Time.deltaTime;

                // Ensure the cooldown doesn't go below zero.
                if (_activeCooldowns[ability] < 0)
                {
                    _activeCooldowns[ability] = 0;
                    Debug.Log($"<color=lime>Ability '{ability.abilityName}' on '{gameObject.name}' is now ready!</color>");
                }
            }
        }
    }

    /// <summary>
    /// Attempts to activate a specific ability.
    /// If the ability is not on cooldown, its effect is executed, and its cooldown timer begins.
    /// If the ability is already on cooldown, nothing happens, and a warning is logged.
    /// </summary>
    /// <param name="abilityToActivate">The AbilityData ScriptableObject to try and activate.</param>
    /// <returns>True if the ability was successfully activated (was ready and started cooldown), false otherwise (e.g., it was on cooldown).</returns>
    public bool TryActivateAbility(AbilityData abilityToActivate)
    {
        if (abilityToActivate == null)
        {
            Debug.LogError("Attempted to activate a null ability.");
            return false;
        }

        // 1. Check if the ability is managed by this system.
        if (!_activeCooldowns.ContainsKey(abilityToActivate))
        {
            Debug.LogWarning($"Ability '{abilityToActivate.abilityName}' is not assigned to the '{gameObject.name}'s AbilityCooldownSystem. Cannot activate.");
            return false;
        }

        // 2. Check if the ability is currently on cooldown.
        if (_activeCooldowns[abilityToActivate] > 0)
        {
            Debug.Log($"<color=yellow>Ability '{abilityToActivate.abilityName}' on '{gameObject.name}' is on cooldown. " +
                      $"Remaining: {GetRemainingCooldown(abilityToActivate):F1}s</color>");
            return false; // Cannot use, it's on cooldown
        }

        // 3. If not on cooldown, activate it!
        Debug.Log($"<color=blue>Activating ability: '{abilityToActivate.abilityName}' for '{gameObject.name}'!</color>");
        abilityToActivate.ExecuteAbilityEffect(); // Trigger the ability's actual game effect.

        // 4. Start the cooldown for this ability.
        _activeCooldowns[abilityToActivate] = abilityToActivate.cooldownDuration;
        Debug.Log($"<color=orange>'{abilityToActivate.abilityName}' on '{gameObject.name}' is now on cooldown for {abilityToActivate.cooldownDuration:F1}s.</color>");

        return true; // Successfully activated
    }

    /// <summary>
    /// Checks if a specific ability is currently ready to be used (i.e., not on cooldown).
    /// </summary>
    /// <param name="ability">The AbilityData ScriptableObject to check.</param>
    /// <returns>True if the ability is ready, false if it's on cooldown or not managed by this system.</returns>
    public bool IsAbilityReady(AbilityData ability)
    {
        // If the ability is null or not managed by this system, it's not "ready" to be used.
        if (ability == null || !_activeCooldowns.ContainsKey(ability))
        {
            return false;
        }
        // An ability is ready if its remaining cooldown is zero or less.
        return _activeCooldowns[ability] <= 0;
    }

    /// <summary>
    /// Gets the remaining cooldown time for a specified ability.
    /// </summary>
    /// <param name="ability">The AbilityData ScriptableObject to query.</param>
    /// <returns>The remaining cooldown time in seconds. Returns 0 if the ability is ready,
    ///         or if the ability is not managed by this system.</returns>
    public float GetRemainingCooldown(AbilityData ability)
    {
        // If the ability is null or not managed, there's no cooldown to report.
        if (ability == null || !_activeCooldowns.ContainsKey(ability))
        {
            return 0f;
        }
        // Return the maximum of 0 and the current cooldown to ensure no negative values are exposed.
        return Mathf.Max(0f, _activeCooldowns[ability]);
    }

    /// <summary>
    /// Gets the normalized cooldown progress (a value between 0 and 1) for an ability.
    /// 0 means the ability is fully ready, 1 means it has just been used (full cooldown remaining).
    /// This is useful for UI elements like cooldown bars or circles.
    /// </summary>
    /// <param name="ability">The AbilityData ScriptableObject to check.</param>
    /// <returns>A float between 0 and 1 representing cooldown progress.
    ///         Returns 0 if the ability is ready or not managed by the system.</returns>
    public float GetCooldownProgressNormalized(AbilityData ability)
    {
        // Handle cases where the ability is invalid or not managed.
        if (ability == null || !_activeCooldowns.ContainsKey(ability) || !_maxCooldownDurations.ContainsKey(ability) || _maxCooldownDurations[ability] <= 0)
        {
            return 0f; // Treat as ready or unmanageable.
        }

        float remaining = GetRemainingCooldown(ability);
        float maxDuration = _maxCooldownDurations[ability];

        if (remaining <= 0) return 0f;          // Ability is ready
        if (remaining >= maxDuration) return 1f; // Ability just used (full cooldown remaining)

        // Calculate the ratio of remaining cooldown to total cooldown duration.
        return remaining / maxDuration;
    }

    /// <summary>
    /// Allows dynamically adding a new ability to the system at runtime.
    /// The newly added ability will be initialized with no cooldown.
    /// </summary>
    /// <param name="newAbility">The AbilityData ScriptableObject to add.</param>
    public void AddAbility(AbilityData newAbility)
    {
        if (newAbility == null)
        {
            Debug.LogError("Attempted to add a null ability dynamically.");
            return;
        }
        if (_activeCooldowns.ContainsKey(newAbility))
        {
            Debug.LogWarning($"Ability '{newAbility.abilityName}' is already managed by this system on '{gameObject.name}'.");
            return;
        }

        assignedAbilities.Add(newAbility); // Add to the visible list (optional, but good for inspection)
        _activeCooldowns[newAbility] = 0f; // New ability starts ready
        _maxCooldownDurations[newAbility] = newAbility.cooldownDuration;
        Debug.Log($"<color=green>Ability '{newAbility.abilityName}' dynamically added to '{gameObject.name}'s system.</color>");
    }

    /// <summary>
    /// Allows dynamically removing an ability from the system at runtime.
    /// </summary>
    /// <param name="abilityToRemove">The AbilityData ScriptableObject to remove.</param>
    public void RemoveAbility(AbilityData abilityToRemove)
    {
        if (abilityToRemove == null)
        {
            Debug.LogError("Attempted to remove a null ability dynamically.");
            return;
        }
        if (!_activeCooldowns.ContainsKey(abilityToRemove))
        {
            Debug.LogWarning($"Ability '{abilityToRemove.abilityName}' is not managed by this system on '{gameObject.name}', cannot remove.");
            return;
        }

        assignedAbilities.Remove(abilityToRemove); // Remove from the visible list
        _activeCooldowns.Remove(abilityToRemove);
        _maxCooldownDurations.Remove(abilityToRemove);
        Debug.Log($"<color=red>Ability '{abilityToRemove.abilityName}' removed from '{gameObject.name}'s system.</color>");
    }
}
```

---

### **3. PlayerAbilityUser.cs**

This script demonstrates how a player character would use the `AbilityCooldownSystem`.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI UI elements
using System.Linq; // For Contains() on List

/// <summary>
/// This class serves as a client for the AbilityCooldownSystem.
/// It demonstrates how a player or an AI character would interact with the
/// cooldown system to try and use abilities based on input or logic.
/// It also shows how to display cooldown status using UI elements.
/// </summary>
public class PlayerAbilityUser : MonoBehaviour
{
    [Header("Ability System Reference")]
    [Tooltip("Reference to the AbilityCooldownSystem component. " +
             "If left unassigned, it will try to find one on this GameObject.")]
    public AbilityCooldownSystem abilitySystem;

    [Header("Abilities to Bind to Keys")]
    [Tooltip("Assign specific AbilityData assets here to bind them to input keys.")]
    public AbilityData fireballAbility;
    public KeyCode fireballKey = KeyCode.Q;

    public AbilityData dashAbility;
    public KeyCode dashKey = KeyCode.E;

    public AbilityData healAbility;
    public KeyCode healKey = KeyCode.R;

    [Header("UI Feedback (Optional)")]
    [Tooltip("Assign TextMeshProUGUI components to display remaining cooldowns visually.")]
    public TextMeshProUGUI fireballCooldownText;
    public TextMeshProUGUI dashCooldownText;
    public TextMeshProUGUI healCooldownText;

    void Start()
    {
        // Ensure the abilitySystem reference is valid.
        // If not assigned in the inspector, try to find it on the same GameObject.
        if (abilitySystem == null)
        {
            abilitySystem = GetComponent<AbilityCooldownSystem>();
            if (abilitySystem == null)
            {
                Debug.LogError("AbilityCooldownSystem not found on this GameObject or assigned in Inspector. " +
                               "Please add an AbilityCooldownSystem component or assign it. Disabling PlayerAbilityUser.", this);
                enabled = false; // Disable this script if the system isn't found
                return;
            }
        }

        // --- Important Best Practice: Ensure all abilities bound here are also managed by the system. ---
        // If a designer binds an AbilityData here but forgets to add it to the AbilityCooldownSystem's
        // 'assignedAbilities' list, this code will automatically add it for convenience, preventing runtime errors.
        AddAbilityToSystemIfMissing(fireballAbility);
        AddAbilityToSystemIfMissing(dashAbility);
        AddAbilityToSystemIfMissing(healAbility);
    }

    /// <summary>
    /// Helper method to add an ability to the AbilityCooldownSystem if it's not already present.
    /// </summary>
    private void AddAbilityToSystemIfMissing(AbilityData ability)
    {
        if (ability != null && !abilitySystem.assignedAbilities.Contains(ability))
        {
            Debug.LogWarning($"Ability '{ability.abilityName}' was assigned to {name}'s PlayerAbilityUser " +
                             $"but not to its AbilityCooldownSystem. Adding it dynamically for convenience. " +
                             $"Consider adding it directly to the AbilityCooldownSystem's 'Assigned Abilities' list.", this);
            abilitySystem.AddAbility(ability);
        }
    }

    void Update()
    {
        HandleInput(); // Check for player input to use abilities
        UpdateUI();    // Refresh UI elements to show cooldown status
    }

    /// <summary>
    /// Processes player input to activate abilities via the AbilityCooldownSystem.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKeyDown(fireballKey) && fireballAbility != null)
        {
            // The PlayerAbilityUser doesn't care *how* cooldowns are managed,
            // it just asks the system to try and activate the ability.
            abilitySystem.TryActivateAbility(fireballAbility);
        }

        if (Input.GetKeyDown(dashKey) && dashAbility != null)
        {
            abilitySystem.TryActivateAbility(dashAbility);
        }

        if (Input.GetKeyDown(healKey) && healAbility != null)
        {
            abilitySystem.TryActivateAbility(healAbility);
        }
    }

    /// <summary>
    /// Updates the UI text elements to display remaining cooldowns or "READY" status.
    /// </summary>
    private void UpdateUI()
    {
        UpdateAbilityUIText(fireballCooldownText, fireballAbility);
        UpdateAbilityUIText(dashCooldownText, dashAbility);
        UpdateAbilityUIText(healCooldownText, healAbility);
    }

    /// <summary>
    /// Helper method to update a single TextMeshProUGUI element for an ability's cooldown.
    /// </summary>
    private void UpdateAbilityUIText(TextMeshProUGUI textMesh, AbilityData ability)
    {
        if (textMesh != null && ability != null)
        {
            float remainingCooldown = abilitySystem.GetRemainingCooldown(ability);
            if (remainingCooldown > 0)
            {
                textMesh.text = $"<color=red>{remainingCooldown:F1}s</color>"; // Display remaining time in red
            }
            else
            {
                textMesh.text = $"<color=green>READY</color>"; // Display "READY" in green
            }

            // --- Example for a cooldown fill bar (e.g., Image.fillAmount) ---
            // float normalizedProgress = abilitySystem.GetCooldownProgressNormalized(ability);
            // Debug.Log($"'{ability.abilityName}' Cooldown Progress: {normalizedProgress:F2}");
            // if (cooldownFillImage != null)
            // {
            //     cooldownFillImage.fillAmount = normalizedProgress; // 0 when ready, 1 when just used
            // }
        }
        else if (textMesh != null)
        {
            // Clear text if the ability is not assigned or null.
            textMesh.text = "";
        }
    }

    // --- Dynamic Ability Management Demonstration (Optional) ---
    // These inputs are just for demonstrating AddAbility/RemoveAbility at runtime.
    [Header("Dynamic Ability Management (Demo)")]
    [Tooltip("Assign an AbilityData here to test adding/removing it at runtime.")]
    public AbilityData dynamicAbilityToAdd;
    public KeyCode addAbilityKey = KeyCode.Alpha1; // Press '1' to add
    public KeyCode removeAbilityKey = KeyCode.Alpha2; // Press '2' to remove

    // Using OnGUI for simple key input demonstration that doesn't conflict with Update's GetKeyDown.
    void OnGUI()
    {
        // Simple UI text instructions
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, Screen.height - 100, 350, 90));
        GUILayout.Label("Press 'Q' for Fireball, 'E' for Dash, 'R' for Heal.");
        GUILayout.Label($"Press '{addAbilityKey}' to Add '{ (dynamicAbilityToAdd != null ? dynamicAbilityToAdd.abilityName : "Null") }' (check console for logs)");
        GUILayout.Label($"Press '{removeAbilityKey}' to Remove '{ (dynamicAbilityToAdd != null ? dynamicAbilityToAdd.abilityName : "Null") }' (check console for logs)");
        GUILayout.EndArea();

        // Handle dynamic add/remove inputs
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == addAbilityKey && dynamicAbilityToAdd != null)
            {
                abilitySystem.AddAbility(dynamicAbilityToAdd);
                Event.current.Use(); // Consume the event so it doesn't trigger other inputs
            }
            if (Event.current.keyCode == removeAbilityKey && dynamicAbilityToAdd != null)
            {
                abilitySystem.RemoveAbility(dynamicAbilityToAdd);
                Event.current.Use(); // Consume the event
            }
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create C# Scripts:**
    *   Save `AbilityData.cs`, `AbilityCooldownSystem.cs`, and `PlayerAbilityUser.cs` into your Unity project (e.g., `Assets/Scripts/Abilities/`).

2.  **Create AbilityData Assets:**
    *   In the Unity Project window, right-click -> `Create` -> `Abilities` -> `Ability Data`.
    *   Create a few instances:
        *   Rename one to "FireballAbility", set `Cooldown Duration` to `3.0`.
        *   Rename another to "DashAbility", set `Cooldown Duration` to `2.5`.
        *   Rename a third to "HealAbility", set `Cooldown Duration` to `5.0`.
        *   (Optional, for dynamic demo) Create a fourth one named "StunAbility" with `4.0` cooldown.

3.  **Setup the Player GameObject:**
    *   In your scene, create an empty GameObject (e.g., `GameObject -> Create Empty`).
    *   Rename it to "Player".
    *   Add the `AbilityCooldownSystem` component to the "Player" GameObject.
    *   Add the `PlayerAbilityUser` component to the "Player" GameObject.

4.  **Configure AbilityCooldownSystem:**
    *   Select your "Player" GameObject in the Hierarchy.
    *   In the Inspector, locate the `AbilityCooldownSystem` component.
    *   Drag and drop your "FireballAbility", "DashAbility", and "HealAbility" assets from the Project window into the `Assigned Abilities` list.

5.  **Configure PlayerAbilityUser:**
    *   Still on the "Player" GameObject, locate the `PlayerAbilityUser` component.
    *   Ensure the `Ability System` field correctly references the `AbilityCooldownSystem` on the same GameObject (it should auto-populate if left blank).
    *   Drag your "FireballAbility" asset into the `Fireball Ability` slot.
    *   Drag your "DashAbility" asset into the `Dash Ability` slot.
    *   Drag your "HealAbility" asset into the `Heal Ability` slot.
    *   (Optional, for dynamic demo) Drag your "StunAbility" asset into the `Dynamic Ability To Add` slot.

6.  **Setup UI (Optional, but Recommended for Visual Feedback):**
    *   Create a UI Canvas (`GameObject -> UI -> Canvas`).
    *   (If prompted, import TextMeshPro Essentials).
    *   Inside the Canvas, create three TextMeshPro - Text (UI) elements (`GameObject -> UI -> Text - TextMeshPro`).
    *   Position these text elements clearly on the screen (e.g., Q, E, R above them) and adjust font size for readability.
    *   Back on your "Player" GameObject, in the `PlayerAbilityUser` component, drag each of your `TextMeshProUGUI` elements from the Hierarchy into the corresponding `Fireball Cooldown Text`, `Dash Cooldown Text`, and `Heal Cooldown Text` slots.

7.  **Run the Scene:**
    *   Play the scene.
    *   Press 'Q' to use Fireball, 'E' for Dash, 'R' for Heal.
    *   Observe the console logs for activation and cooldown messages.
    *   If you set up the UI, you'll see "READY" or the remaining cooldown time for each ability.
    *   Press '1' to dynamically add the "StunAbility" to the system, and '2' to remove it. (Note: The `PlayerAbilityUser` doesn't have a key bound to "StunAbility", so you would only see the system logs for its addition/removal).

This example provides a robust and educational foundation for implementing ability cooldowns in your Unity projects, adhering to common design patterns and best practices.