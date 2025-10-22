// Unity Design Pattern Example: StanceSwitchSystem
// This script demonstrates the StanceSwitchSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'StanceSwitchSystem' pattern, while not one of the classic Gang of Four design patterns, is a very common and practical application of the **State design pattern** in game development. It allows an entity (like a player character) to alter its behavior and capabilities based on its current 'stance' or mode (e.g., Normal, Combat, Stealth, Defensive).

This example demonstrates how to implement such a system in Unity using C#. We'll leverage `ScriptableObjects` for our stance definitions, which provides excellent flexibility, reusability, and designer-friendly configuration.

---

### **StanceSwitchSystem Design Pattern Explained**

**Core Idea:**
An object (the **Context**, our `PlayerStanceController`) can change its internal behavior by changing its `CurrentStance` object. Each `Stance` object represents a different state, and encapsulates the behavior specific to that state.

**Components:**

1.  **Context (`PlayerStanceController`):**
    *   Holds a reference to the `CurrentStance` object.
    *   Delegates behavior-specific requests to the `CurrentStance`.
    *   Provides an interface to switch between different stances.
    *   Maintains references to the player's core components (Animator, CharacterController, etc.) that stances might need to manipulate.

2.  **Stance Interface (`IPlayerStance`):**
    *   Defines a common interface for all concrete stance objects.
    *   Declares methods that the Context can call, such as `EnterStance`, `ExitStance`, `UpdateStance`, `PerformPrimaryAction`, `PerformSecondaryAction`, etc.
    *   Each method typically takes the `PlayerStanceController` (Context) as an argument, allowing the stance to interact with and modify the player.

3.  **Abstract Stance Base Class (`StanceScriptableObject`):**
    *   An abstract `ScriptableObject` that implements `IPlayerStance`.
    *   Provides common properties (like `StanceName`) and default (virtual) implementations for interface methods.
    *   Serves as a base for all concrete stances. Using `ScriptableObjects` here allows us to create reusable data assets for each stance, configurable by designers in the Unity Editor.

4.  **Concrete Stance Classes (`NormalStanceSO`, `CombatStanceSO`, `StealthStanceSO`):**
    *   Implement the `StanceScriptableObject` and override methods to provide specific behaviors for each stance.
    *   Define properties unique to that stance (e.g., `attackDamageMultiplier` for `CombatStanceSO`).

**Benefits of this approach:**
*   **Encapsulation:** Behavior specific to each stance is contained within its own class.
*   **Flexibility:** Easily add new stances without modifying existing ones.
*   **Maintainability:** Changes to one stance's behavior don't affect others.
*   **Designer-Friendly:** `ScriptableObjects` allow designers to create and configure stance assets without touching code.

---

### **C# Unity Example Implementation**

Here are the C# scripts. Create them in your Unity project, then follow the setup instructions.

---

#### 1. `IPlayerStance.cs`

```csharp
using UnityEngine;

/// <summary>
/// IPlayerStance Interface: Defines the common contract for all player stances.
/// This is the 'State' interface in the State design pattern.
/// </summary>
public interface IPlayerStance
{
    /// <summary>
    /// Called when the player enters this stance.
    /// Use this for setup actions: changing animation states, modifying stats, playing sounds.
    /// </summary>
    /// <param name="player">A reference to the PlayerStanceController (Context) to manipulate.</param>
    void EnterStance(PlayerStanceController player);

    /// <summary>
    /// Called when the player exits this stance.
    /// Use this for cleanup actions: reverting animation states, restoring stats.
    /// </summary>
    /// <param name="player">A reference to the PlayerStanceController (Context) to manipulate.</param>
    void ExitStance(PlayerStanceController player);

    /// <summary>
    /// Called every frame while this stance is active (like Update()).
    /// Use this for continuous behavior, input checks specific to the stance.
    /// </summary>
    /// <param name="player">A reference to the PlayerStanceController (Context) to manipulate.</param>
    void UpdateStance(PlayerStanceController player);

    /// <summary>
    /// Handles the primary action input for this stance (e.g., attack, interact).
    /// </summary>
    /// <param name="player">A reference to the PlayerStanceController (Context) to manipulate.</param>
    void PerformPrimaryAction(PlayerStanceController player);

    /// <summary>
    /// Handles the secondary action input for this stance (e.g., block, secondary ability).
    /// </summary>
    /// <param name="player">A reference to the PlayerStanceController (Context) to manipulate.</param>
    void PerformSecondaryAction(PlayerStanceController player);
}
```

---

#### 2. `StanceScriptableObject.cs`

```csharp
using UnityEngine;

/// <summary>
/// StanceScriptableObject Abstract Base Class:
/// Provides a base implementation for all player stances using ScriptableObjects.
/// This allows stances to be created as assets in the Unity Editor and configured.
/// It implements the IPlayerStance interface with virtual methods, so concrete stances
/// can override only the behaviors they need to change.
/// </summary>
public abstract class StanceScriptableObject : ScriptableObject, IPlayerStance
{
    [Tooltip("The display name of this stance.")]
    public string StanceName = "Default Stance";

    [Tooltip("Movement speed multiplier while in this stance.")]
    public float movementSpeedMultiplier = 1.0f;

    [Tooltip("Animator state name to set on entering this stance.")]
    public string animatorStanceTrigger = "Stance_Default";

    /// <summary>
    /// Virtual method for entering the stance. Override in concrete stances for specific setup.
    /// </summary>
    public virtual void EnterStance(PlayerStanceController player)
    {
        Debug.Log($"<color=cyan>Entering {StanceName} Stance.</color>");
        player.currentStanceDisplayName.text = $"Current Stance: {StanceName}";
        player.currentStanceDisplayName.color = player.stanceTextColor; // Example: Reset color

        // Apply base properties
        player.animator.SetTrigger(animatorStanceTrigger);
        // Additional setup (e.g., adjust camera, particle systems) can go here
    }

    /// <summary>
    /// Virtual method for exiting the stance. Override in concrete stances for specific cleanup.
    /// </summary>
    public virtual void ExitStance(PlayerStanceController player)
    {
        Debug.Log($"Exiting {StanceName} Stance.");
        // Cleanup actions (e.g., reset camera, stop particle effects)
    }

    /// <summary>
    /// Virtual method for updating the stance logic per frame.
    /// </summary>
    public virtual void UpdateStance(PlayerStanceController player)
    {
        // Example: Handle continuous movement input based on stance speed
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        player.characterController.Move(moveDirection * player.baseMovementSpeed * movementSpeedMultiplier * Time.deltaTime);

        // Update animator for movement
        if (player.animator != null)
        {
            float speed = moveDirection.magnitude * player.baseMovementSpeed * movementSpeedMultiplier;
            player.animator.SetFloat("Speed", speed);
        }
    }

    /// <summary>
    /// Virtual method for the primary action. Override for specific actions.
    /// </summary>
    public virtual void PerformPrimaryAction(PlayerStanceController player)
    {
        Debug.Log($"<color=yellow>{StanceName} Stance: Performing Primary Action (Default).</color>");
        // Default primary action (e.g., basic attack, simple interaction)
        player.audioSource.PlayOneShot(player.defaultActionSound);
    }

    /// <summary>
    /// Virtual method for the secondary action. Override for specific actions.
    /// </summary>
    public virtual void PerformSecondaryAction(PlayerStanceController player)
    {
        Debug.Log($"<color=yellow>{StanceName} Stance: Performing Secondary Action (Default).</color>");
        // Default secondary action (e.g., basic block, secondary interaction)
        player.audioSource.PlayOneShot(player.defaultActionSound);
    }
}
```

---

#### 3. `NormalStanceSO.cs`

```csharp
using UnityEngine;

/// <summary>
/// NormalStanceSO: A concrete implementation of a player stance.
/// Represents a balanced, general-purpose stance.
/// </summary>
[CreateAssetMenu(fileName = "NewNormalStance", menuName = "Stance/Normal Stance")]
public class NormalStanceSO : StanceScriptableObject
{
    [Header("Normal Stance Specifics")]
    [Tooltip("Bonus to interaction range in normal stance.")]
    public float interactionRangeBonus = 1.0f;

    private float _originalInteractionRange;

    public override void EnterStance(PlayerStanceController player)
    {
        base.EnterStance(player); // Call base implementation for common setup

        StanceName = "Normal";
        movementSpeedMultiplier = 1.0f;
        animatorStanceTrigger = "Stance_Normal"; // Make sure this trigger exists in your Animator

        // Store original value before modification
        _originalInteractionRange = player.baseInteractionRange;
        player.baseInteractionRange += interactionRangeBonus;

        Debug.Log($"<color=green>Player entered {StanceName} Stance. Interaction Range: {player.baseInteractionRange}</color>");
        player.currentStanceDisplayName.color = Color.green;
    }

    public override void ExitStance(PlayerStanceController player)
    {
        base.ExitStance(player); // Call base implementation for common cleanup
        player.baseInteractionRange = _originalInteractionRange; // Revert interaction range
        Debug.Log($"Player exited {StanceName} Stance. Interaction Range reverted to: {player.baseInteractionRange}</color>");
    }

    public override void UpdateStance(PlayerStanceController player)
    {
        base.UpdateStance(player); // Call base for movement and general updates
        // Normal stance might have specific passive abilities or constant checks
        // For example, if (player.IsNearInteractableObject()) { /* show prompt */ }
    }

    public override void PerformPrimaryAction(PlayerStanceController player)
    {
        // Example: In normal stance, primary action might be 'Interact'
        Debug.Log($"<color=lime>{StanceName} Stance: Interacting with environment.</color>");
        player.audioSource.PlayOneShot(player.interactSound);
        player.animator.SetTrigger("Interact"); // Trigger interact animation
    }

    public override void PerformSecondaryAction(PlayerStanceController player)
    {
        // Example: In normal stance, secondary action might be 'Inspect'
        Debug.Log($"<color=lime>{StanceName} Stance: Inspecting surroundings.</color>");
        player.audioSource.PlayOneShot(player.inspectSound);
        player.animator.SetTrigger("Inspect"); // Trigger inspect animation
    }
}
```

---

#### 4. `CombatStanceSO.cs`

```csharp
using UnityEngine;

/// <summary>
/// CombatStanceSO: A concrete implementation of a player stance.
/// Represents an offensive, combat-focused stance.
/// </summary>
[CreateAssetMenu(fileName = "NewCombatStance", menuName = "Stance/Combat Stance")]
public class CombatStanceSO : StanceScriptableObject
{
    [Header("Combat Stance Specifics")]
    [Tooltip("Damage multiplier for attacks in combat stance.")]
    public float attackDamageMultiplier = 1.5f;
    [Tooltip("Defense multiplier when blocking in combat stance.")]
    public float blockDefenseMultiplier = 2.0f;

    public override void EnterStance(PlayerStanceController player)
    {
        base.EnterStance(player); // Call base for common setup

        StanceName = "Combat";
        movementSpeedMultiplier = 0.75f; // Slower movement in combat
        animatorStanceTrigger = "Stance_Combat"; // Make sure this trigger exists in your Animator

        // Apply combat-specific changes
        player.animator.SetBool("InCombat", true);
        Debug.Log($"<color=red>Player entered {StanceName} Stance. Attack Multiplier: {attackDamageMultiplier}</color>");
        player.currentStanceDisplayName.color = Color.red;
    }

    public override void ExitStance(PlayerStanceController player)
    {
        base.ExitStance(player); // Call base for common cleanup
        player.animator.SetBool("InCombat", false); // Revert combat state
        Debug.Log($"Player exited {StanceName} Stance.</color>");
    }

    public override void UpdateStance(PlayerStanceController player)
    {
        base.UpdateStance(player); // Call base for movement and general updates
        // Combat stance might have specific targeting logic or enemy detection
        // if (player.TargetDetected()) { /* auto-lock feature */ }
    }

    public override void PerformPrimaryAction(PlayerStanceController player)
    {
        // Example: Primary action in combat stance is 'Attack'
        Debug.Log($"<color=orange>{StanceName} Stance: Attacking with {attackDamageMultiplier}x damage!</color>");
        player.audioSource.PlayOneShot(player.attackSound);
        player.animator.SetTrigger("Attack"); // Trigger attack animation
        // player.DealDamage(player.baseAttackDamage * attackDamageMultiplier);
    }

    public override void PerformSecondaryAction(PlayerStanceController player)
    {
        // Example: Secondary action in combat stance is 'Block'
        Debug.Log($"<color=orange>{StanceName} Stance: Blocking with {blockDefenseMultiplier}x defense!</color>");
        player.audioSource.PlayOneShot(player.blockSound);
        player.animator.SetTrigger("Block"); // Trigger block animation
        // player.ApplyDefenseModifier(blockDefenseMultiplier);
    }
}
```

---

#### 5. `StealthStanceSO.cs`

```csharp
using UnityEngine;

/// <summary>
/// StealthStanceSO: A concrete implementation of a player stance.
/// Represents a covert, stealth-focused stance.
/// </summary>
[CreateAssetMenu(fileName = "NewStealthStance", menuName = "Stance/Stealth Stance")]
public class StealthStanceSO : StanceScriptableObject
{
    [Header("Stealth Stance Specifics")]
    [Tooltip("Reduces player visibility/detection range.")]
    public float stealthVisibilityMultiplier = 0.5f;
    [Tooltip("Allows for silent takedowns.")]
    public bool canPerformTakedown = true;

    private float _originalVisibility;

    public override void EnterStance(PlayerStanceController player)
    {
        base.EnterStance(player); // Call base for common setup

        StanceName = "Stealth";
        movementSpeedMultiplier = 0.5f; // Slower movement in stealth
        animatorStanceTrigger = "Stance_Stealth"; // Make sure this trigger exists in your Animator

        // Apply stealth-specific changes
        _originalVisibility = player.baseVisibilityFactor;
        player.baseVisibilityFactor *= stealthVisibilityMultiplier;
        player.animator.SetBool("IsStealthy", true);
        Debug.Log($"<color=blue>Player entered {StanceName} Stance. Visibility: {player.baseVisibilityFactor}</color>");
        player.currentStanceDisplayName.color = Color.blue;
    }

    public override void ExitStance(PlayerStanceController player)
    {
        base.ExitStance(player); // Call base for common cleanup
        player.baseVisibilityFactor = _originalVisibility; // Revert visibility
        player.animator.SetBool("IsStealthy", false); // Revert stealth state
        Debug.Log($"Player exited {StanceName} Stance. Visibility reverted to: {player.baseVisibilityFactor}</color>");
    }

    public override void UpdateStance(PlayerStanceController player)
    {
        base.UpdateStance(player); // Call base for movement and general updates
        // Stealth stance might involve checking for cover, enemy line-of-sight
        // if (player.IsHiddenFromEnemies()) { /* special effects */ }
    }

    public override void PerformPrimaryAction(PlayerStanceController player)
    {
        // Example: Primary action in stealth stance is 'Takedown' if applicable
        if (canPerformTakedown && player.IsNearEnemyForTakedown()) // Assume player has this check
        {
            Debug.Log($"<color=purple>{StanceName} Stance: Performing silent takedown!</color>");
            player.audioSource.PlayOneShot(player.takedownSound);
            player.animator.SetTrigger("Takedown"); // Trigger takedown animation
            // player.ExecuteTakedown();
        }
        else
        {
            Debug.Log($"<color=purple>{StanceName} Stance: Sneaking (no takedown opportunity).</color>");
            player.audioSource.PlayOneShot(player.stealthMoveSound);
            player.animator.SetTrigger("SneakAction"); // Generic stealth action
        }
    }

    public override void PerformSecondaryAction(PlayerStanceController player)
    {
        // Example: Secondary action in stealth stance is 'Hide'
        Debug.Log($"<color=purple>{StanceName} Stance: Attempting to hide in cover!</color>");
        player.audioSource.PlayOneShot(player.hideSound);
        player.animator.SetTrigger("Hide"); // Trigger hide animation
        // player.EnterCoverState();
    }
}
```

---

#### 6. `PlayerStanceController.cs`

```csharp
using UnityEngine;
using TMPro; // For TextMeshPro, make sure it's imported in your project
using System.Collections.Generic; // For Dictionary

/// <summary>
/// PlayerStanceController: The 'Context' in the State design pattern.
/// This MonoBehaviour manages the player's current stance and delegates actions to it.
/// It also holds references to player components that stances might need to interact with.
/// </summary>
[RequireComponent(typeof(Animator), typeof(CharacterController), typeof(AudioSource))]
public class PlayerStanceController : MonoBehaviour
{
    [Header("Stance Settings")]
    [Tooltip("The default stance the player starts in.")]
    public StanceScriptableObject defaultStance;
    [Tooltip("References to all available stances, keyed by an integer for quick switching.")]
    public List<StanceScriptableObject> availableStances;

    // The currently active stance. All actions are delegated to this object.
    private IPlayerStance _currentStance;
    public IPlayerStance CurrentStance => _currentStance; // Public getter for current stance

    [Header("Player Components")]
    // References to actual player components for stances to manipulate
    public Animator animator;
    public CharacterController characterController;
    public AudioSource audioSource;

    [Header("Base Player Attributes (modified by stances)")]
    public float baseMovementSpeed = 5.0f;
    public float baseAttackDamage = 10.0f;
    public float baseInteractionRange = 2.0f;
    public float baseVisibilityFactor = 1.0f; // 1.0 = fully visible, 0.5 = less visible

    [Header("UI & Feedback")]
    [Tooltip("TextMeshPro text element to display the current stance.")]
    public TextMeshProUGUI currentStanceDisplayName;
    [Tooltip("Default color for stance display text.")]
    public Color stanceTextColor = Color.white;

    [Header("Sound Effects")]
    public AudioClip defaultActionSound;
    public AudioClip attackSound;
    public AudioClip blockSound;
    public AudioClip interactSound;
    public AudioClip inspectSound;
    public AudioClip takedownSound;
    public AudioClip stealthMoveSound;
    public AudioClip hideSound;

    // Private dictionary for efficient stance lookup by key/index
    private Dictionary<int, StanceScriptableObject> _stanceLookup = new Dictionary<int, StanceScriptableObject>();

    /// <summary>
    /// Unity's Awake method. Initializes components and sets the default stance.
    /// </summary>
    void Awake()
    {
        // Get references to components
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Populate the stance lookup dictionary
        for (int i = 0; i < availableStances.Count; i++)
        {
            if (availableStances[i] != null)
            {
                _stanceLookup[i + 1] = availableStances[i]; // Use 1-based indexing for input keys
            }
        }

        // Set the initial stance if a default is provided
        if (defaultStance != null)
        {
            SwitchStance(defaultStance);
        }
        else if (availableStances != null && availableStances.Count > 0)
        {
            SwitchStance(availableStances[0]); // Fallback to the first available stance
        }
        else
        {
            Debug.LogError("No default stance or available stances set for PlayerStanceController!");
        }

        // Initialize UI
        if (currentStanceDisplayName == null)
        {
            GameObject uiCanvas = GameObject.Find("Canvas");
            if (uiCanvas != null)
            {
                // Try to find a TextMeshProUGUI in the scene if not assigned
                currentStanceDisplayName = uiCanvas.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (currentStanceDisplayName == null)
            {
                Debug.LogWarning("TextMeshProUGUI for currentStanceDisplayName not assigned. Stance UI will not update.");
            }
        }
    }

    /// <summary>
    /// Unity's Update method. Handles input for switching stances and delegates
    /// continuous behavior and actions to the current stance.
    /// </summary>
    void Update()
    {
        // Delegate continuous update logic to the current stance
        _currentStance?.UpdateStance(this);

        // Handle input for switching stances (e.g., Number keys 1, 2, 3)
        HandleStanceSwitchInput();

        // Handle input for primary and secondary actions, delegated to the current stance
        HandleActionInput();
    }

    /// <summary>
    /// Checks for input to switch stances.
    /// </summary>
    private void HandleStanceSwitchInput()
    {
        foreach (var entry in _stanceLookup)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + entry.Key)) // KeyCode.Alpha1 for 1, Alpha2 for 2, etc.
            {
                SwitchStance(entry.Value);
                break;
            }
        }
    }

    /// <summary>
    /// Checks for input to perform actions.
    /// </summary>
    private void HandleActionInput()
    {
        if (Input.GetButtonDown("Fire1")) // Default Primary Action (Left Mouse Button/Ctrl)
        {
            _currentStance?.PerformPrimaryAction(this);
        }

        if (Input.GetButtonDown("Fire2")) // Default Secondary Action (Right Mouse Button/Alt)
        {
            _currentStance?.PerformSecondaryAction(this);
        }
    }

    /// <summary>
    /// Switches the player to a new stance. This is the core of the StanceSwitchSystem.
    /// It ensures the old stance is properly exited and the new one is properly entered.
    /// </summary>
    /// <param name="newStance">The ScriptableObject representing the new stance to switch to.</param>
    public void SwitchStance(StanceScriptableObject newStance)
    {
        if (newStance == null || _currentStance == newStance)
        {
            Debug.LogWarning("Attempted to switch to null stance or already in this stance.");
            return;
        }

        // 1. Exit the current stance (if any)
        if (_currentStance != null)
        {
            _currentStance.ExitStance(this);
        }

        // 2. Set the new stance
        _currentStance = newStance;

        // 3. Enter the new stance
        _currentStance.EnterStance(this);

        // Update UI immediately (can also be done in EnterStance)
        if (currentStanceDisplayName != null)
        {
            currentStanceDisplayName.text = $"Current Stance: {newStance.StanceName}";
            // Color is updated by specific StanceSO Enter methods
        }
    }

    // Helper methods for stances to call (examples)
    public bool IsNearEnemyForTakedown()
    {
        // Placeholder for actual game logic
        Debug.Log("Checking if near enemy for takedown...");
        return Random.value > 0.5f; // Simulate a 50% chance
    }
}
```

---

### **Unity Editor Setup Instructions**

To get this example working in your Unity project:

1.  **Create a C# Scripts Folder:** In your Project window, create a folder named `Scripts` (or similar).
2.  **Create C# Scripts:** Inside the `Scripts` folder, create six new C# scripts with the exact names as provided above:
    *   `IPlayerStance`
    *   `StanceScriptableObject`
    *   `NormalStanceSO`
    *   `CombatStanceSO`
    *   `StealthStanceSO`
    *   `PlayerStanceController`
3.  **Copy and Paste Code:** Open each script and paste the corresponding code into it, replacing the default Unity template code. Save all scripts.
4.  **Install TextMeshPro (if not already):**
    *   Go to `Window > TextMeshPro > Import TMP Essential Resources`.
5.  **Create Stance ScriptableObjects:**
    *   In your Project window, create a new folder (e.g., `Stances`).
    *   Right-click in the `Stances` folder -> `Create > Stance > Normal Stance`. Name it `NormalStanceAsset`.
    *   Repeat for `Combat Stance` (name it `CombatStanceAsset`) and `Stealth Stance` (name it `StealthStanceAsset`).
    *   **Configure Each Stance Asset:** Select each `StanceAsset` in the Project window and adjust its `movementSpeedMultiplier`, `attackDamageMultiplier`, `stealthVisibilityMultiplier`, etc., in the Inspector. Also, ensure `animatorStanceTrigger` names match your Animator setup (e.g., "Stance_Normal", "Stance_Combat", "Stance_Stealth").
6.  **Create a Player GameObject:**
    *   In your Hierarchy, create an empty GameObject named `Player`.
    *   Add a `CharacterController` component to the `Player` GameObject (`Add Component > Physics > Character Controller`).
    *   Add an `Animator` component to the `Player` GameObject (`Add Component > Animation > Animator`).
        *   *For basic animation feedback:* You can create a simple Animator Controller (e.g., "PlayerAnimatorController") and add `Speed` (float), `InCombat` (bool), `IsStealthy` (bool), and trigger parameters like `Stance_Normal`, `Stance_Combat`, `Stance_Stealth`, `Attack`, `Block`, `Interact`, `Inspect`, `Takedown`, `SneakAction`, `Hide`. Link these to dummy animation states for visual feedback.
    *   Add an `AudioSource` component to the `Player` GameObject.
    *   Add the `PlayerStanceController` script component to the `Player` GameObject.
7.  **Configure `PlayerStanceController`:**
    *   Select the `Player` GameObject in the Hierarchy.
    *   In the Inspector for the `PlayerStanceController` script:
        *   Drag your `NormalStanceAsset` into the `Default Stance` field.
        *   Expand `Available Stances` list. Set Size to 3.
        *   Drag `NormalStanceAsset` into Element 0.
        *   Drag `CombatStanceAsset` into Element 1.
        *   Drag `StealthStanceAsset` into Element 2.
        *   **Assign Sound Effects:** Create some dummy `AudioClip` assets or use placeholders and assign them to the `Sound Effects` fields.
8.  **Create UI for Stance Display:**
    *   In the Hierarchy, right-click -> `UI > Canvas`.
    *   Right-click on the `Canvas` -> `UI > Text - TextMeshPro`. Name it `StanceDisplay`.
    *   Adjust its position and size to be visible (e.g., top-left corner).
    *   Select the `Player` GameObject again. Drag your `StanceDisplay` TextMeshProUGUI into the `Current Stance Display Name` field of the `PlayerStanceController`.
9.  **Run the Scene:** Press Play!

### **How to Use:**

*   **Switch Stances:** Press `1`, `2`, or `3` on your keyboard to switch between Normal, Combat, and Stealth stances respectively. Watch the Debug.Log output and the UI Text.
*   **Perform Actions:**
    *   Click the **Left Mouse Button (Fire1)** to perform the primary action of the current stance.
    *   Click the **Right Mouse Button (Fire2)** to perform the secondary action of the current stance.
*   **Observe Behavior:**
    *   The player's movement speed will change according to the `movementSpeedMultiplier` defined in each stance asset.
    *   The `Animator` parameters (`Speed`, `InCombat`, `IsStealthy`, and trigger states) will be updated.
    *   Different debug messages and sounds will play for actions based on the active stance.

This setup provides a robust and easily extensible 'StanceSwitchSystem' using the State pattern with `ScriptableObjects`, making it highly practical for real Unity game development.