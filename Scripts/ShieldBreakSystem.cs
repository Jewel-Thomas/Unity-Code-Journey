// Unity Design Pattern Example: ShieldBreakSystem
// This script demonstrates the ShieldBreakSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a **'ShieldBreakSystem'** in Unity. While not a classic GoF design pattern, it's a common game mechanic that benefits from structured, pattern-oriented design. This implementation leverages the **State Pattern** for managing the shield's various operational modes and the **Observer Pattern** (via C# events) for communication with other game systems (like UI, visual effects, or core entity health).

The goal is to create a robust, extensible, and easy-to-use component that handles shield durability, breaking, regeneration, and notifications, without tightly coupling it to specific UI or effect implementations.

---

## 1. ShieldBreakSystem.cs (Core Component)

This script is the heart of the system. Attach it to any GameObject that needs a shield.

```csharp
using UnityEngine;
using System;
using System.Collections; // Not strictly required for this specific pattern, but common in Unity scripts.

/// <summary>
/// The ShieldBreakSystem manages an entity's protective shield, handling its durability,
/// taking damage, breaking, and regeneration. It utilizes a State Pattern to manage
/// different operational modes of the shield (Active, Broken, Regenerating) and
/// an Observer Pattern (C# Events) to notify other systems of state changes.
/// </summary>
public class ShieldBreakSystem : MonoBehaviour
{
    [Header("Shield Configuration")]
    [Tooltip("The maximum durability (health) of the shield.")]
    [SerializeField] private float maxShieldDurability = 100f;

    [Tooltip("The rate at which the shield regenerates durability per second.")]
    [SerializeField] private float shieldRegenRate = 5f;

    [Tooltip("The delay after taking damage before shield regeneration can begin.")]
    [SerializeField] private float shieldRegenDelay = 3f;

    [Tooltip("The duration the shield remains completely broken (inactive) before it starts regenerating.")]
    [SerializeField] private float shieldBrokenDuration = 5f;

    // --- Internal State Variables ---
    private float currentShieldDurability;
    private ShieldState currentState;

    // Timers for managing state transitions
    private float timer_regenDelay;       // Tracks time since last damage hit for regeneration delay.
    private float timer_brokenCooldown;   // Tracks time since shield broke for the broken duration.

    /// <summary>
    /// Represents the possible states of the shield.
    /// This is the core of the State Pattern implementation.
    /// </summary>
    private enum ShieldState
    {
        Active,         // Shield is up, absorbing damage, and can regenerate after a delay.
        Broken,         // Shield durability is zero; it's down and undergoing a cooldown period.
        Regenerating    // Shield is currently recovering durability after being broken or damaged.
    }

    // --- Public Properties (Read-only access to shield state) ---
    public float CurrentShieldDurability => currentShieldDurability;
    public float MaxShieldDurability => maxShieldDurability;
    public bool IsShieldActive => currentState == ShieldState.Active;
    public bool IsShieldBroken => currentState == ShieldState.Broken;
    public bool IsShieldRegenerating => currentState == ShieldState.Regenerating;


    // --- Events (Observer Pattern) ---
    // Other systems can subscribe to these events to react to shield changes.

    /// <summary>
    /// Fired when the shield becomes fully operational (from start, or regenerating to full durability).
    /// </summary>
    public event Action OnShieldActivated;

    /// <summary>
    /// Fired when the shield durability drops to 0 and becomes broken.
    /// </summary>
    public event Action OnShieldBroken;

    /// <summary>
    /// Fired when the shield exits the 'Broken' state and begins its regeneration process.
    /// (Does not mean it's fully restored, just that the broken cooldown is over).
    /// </summary>
    public event Action OnShieldBeginsRegeneration;

    /// <summary>
    /// Fired whenever the current shield durability changes (e.g., takes damage, regenerates).
    /// Provides current and max durability values.
    /// </summary>
    public event Action<float, float> OnShieldDurabilityChanged;


    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the shield to its active state.
    /// </summary>
    private void Awake()
    {
        InitializeShield();
    }

    /// <summary>
    /// Initializes or resets the shield to its default active state.
    /// </summary>
    private void InitializeShield()
    {
        currentShieldDurability = maxShieldDurability;
        currentState = ShieldState.Active;
        timer_regenDelay = shieldRegenDelay; // Ready to regen if no damage is taken immediately
        timer_brokenCooldown = 0f;

        // Notify subscribers of initial state
        OnShieldDurabilityChanged?.Invoke(currentShieldDurability, maxShieldDurability);
        OnShieldActivated?.Invoke(); // Indicate shield starts active
    }

    /// <summary>
    /// Update is called once per frame. It manages the shield's state transitions
    /// and regeneration logic based on the current state.
    /// </summary>
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        switch (currentState)
        {
            case ShieldState.Active:
                // If shield is not at max durability, check if it's time to regenerate
                if (currentShieldDurability < maxShieldDurability)
                {
                    timer_regenDelay += deltaTime;
                    if (timer_regenDelay >= shieldRegenDelay)
                    {
                        RegenerateShield(deltaTime);
                    }
                }
                break;

            case ShieldState.Broken:
                // Count down the broken duration
                timer_brokenCooldown += deltaTime;
                if (timer_brokenCooldown >= shieldBrokenDuration)
                {
                    TransitionToState(ShieldState.Regenerating);
                    OnShieldBeginsRegeneration?.Invoke(); // Notify that regeneration has started
                }
                break;

            case ShieldState.Regenerating:
                // Continuously regenerate durability
                RegenerateShield(deltaTime);
                // If shield is fully regenerated, transition back to Active
                if (currentShieldDurability >= maxShieldDurability)
                {
                    TransitionToState(ShieldState.Active);
                    OnShieldActivated?.Invoke(); // Notify that shield is fully active again
                }
                break;
        }
    }

    /// <summary>
    /// Applies damage to the shield.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // Ignore non-positive damage

        if (currentState == ShieldState.Active)
        {
            currentShieldDurability -= amount;
            currentShieldDurability = Mathf.Max(currentShieldDurability, 0f); // Clamp durability to 0
            OnShieldDurabilityChanged?.Invoke(currentShieldDurability, maxShieldDurability); // Notify of change

            timer_regenDelay = 0f; // Reset regeneration delay on damage taken

            if (currentShieldDurability <= 0f)
            {
                BreakShield();
            }
        }
        else
        {
            // If the shield is Broken or Regenerating, damage bypasses the shield.
            // In a real game, this is where you'd typically apply damage to the entity's core health.
            // Example: GetComponent<HealthSystem>()?.TakeDamage(amount);
            Debug.Log($"Shield on {gameObject.name} is {currentState}, damage {amount} bypassed shield.");
        }
    }

    /// <summary>
    /// Forces the shield to break immediately.
    /// </summary>
    private void BreakShield()
    {
        TransitionToState(ShieldState.Broken);
        timer_brokenCooldown = 0f; // Start the broken cooldown timer
        OnShieldBroken?.Invoke();
        Debug.Log($"Shield on {gameObject.name} BROKEN!");
    }

    /// <summary>
    /// Handles the continuous regeneration of shield durability.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    private void RegenerateShield(float deltaTime)
    {
        currentShieldDurability += shieldRegenRate * deltaTime;
        currentShieldDurability = Mathf.Min(currentShieldDurability, maxShieldDurability); // Clamp to max
        OnShieldDurabilityChanged?.Invoke(currentShieldDurability, maxShieldDurability); // Notify of change
    }

    /// <summary>
    /// Handles state transitions, centralizing state change logic.
    /// This is crucial for the State Pattern.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void TransitionToState(ShieldState newState)
    {
        if (currentState == newState) return; // No-op if already in the new state

        // Exit logic for current state (if any specific cleanup is needed)
        // No specific exit logic needed for this basic example.

        currentState = newState;

        // Entry logic for new state (if any specific setup is needed)
        switch (newState)
        {
            case ShieldState.Active:
                // Ensure durability is max when active, especially if transitioning from regenerating
                currentShieldDurability = maxShieldDurability;
                timer_regenDelay = shieldRegenDelay; // Reset regen delay for future damage
                Debug.Log($"Shield on {gameObject.name} is now ACTIVE and fully restored!");
                break;
            case ShieldState.Broken:
                currentShieldDurability = 0f; // Ensure durability is 0 when broken
                timer_brokenCooldown = 0f; // Start cooldown
                Debug.Log($"Shield on {gameObject.name} transitioned to BROKEN state.");
                break;
            case ShieldState.Regenerating:
                Debug.Log($"Shield on {gameObject.name} transitioned to REGENERATING state.");
                break;
        }
    }

    // --- Debug / Editor Helper Functions ---
    [ContextMenu("Debug: Break Shield")]
    public void Debug_BreakShield()
    {
        TakeDamage(maxShieldDurability * 2); // Apply more than enough damage to break
    }

    [ContextMenu("Debug: Fully Restore Shield")]
    public void Debug_FullyRestoreShield()
    {
        InitializeShield(); // Re-initializes everything to active and full
        Debug.Log("Shield debug fully restored via Context Menu!");
    }

    // Ensures values are valid in the Inspector.
    private void OnValidate()
    {
        if (maxShieldDurability < 0) maxShieldDurability = 0;
        if (shieldRegenRate < 0) shieldRegenRate = 0;
        if (shieldRegenDelay < 0) shieldRegenDelay = 0;
        if (shieldBrokenDuration < 0) shieldBrokenDuration = 0;
    }
}
```

---

## 2. Example Usage Scripts

To make the `ShieldBreakSystem` practical, we need scripts that interact with it.

### 2.1. ShieldVisualsAndUI.cs (Observer Example)

This script demonstrates how to subscribe to the `ShieldBreakSystem`'s events to update UI elements or visual effects.

```csharp
using UnityEngine;
using UnityEngine.UI; // For Text and Image components

/// <summary>
/// This script observes the ShieldBreakSystem and updates UI elements
/// and a simple visual indicator based on the shield's state.
/// It demonstrates the Observer Pattern by subscribing to the ShieldBreakSystem's events.
/// </summary>
public class ShieldVisualsAndUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ShieldBreakSystem this script will observe.")]
    [SerializeField] private ShieldBreakSystem targetShieldSystem;

    [Header("UI Elements (Optional)")]
    [Tooltip("Text element to display current shield durability.")]
    [SerializeField] private Text durabilityText;
    [Tooltip("Image element to represent shield fill level (e.g., a progress bar).")]
    [SerializeField] private Image shieldFillImage;

    [Header("Visual Effects (Optional)")]
    [Tooltip("Renderer of the shield mesh/material.")]
    [SerializeField] private Renderer shieldRenderer;
    [Tooltip("Material to use when shield is active.")]
    [SerializeField] private Material activeShieldMaterial;
    [Tooltip("Material to use when shield is broken.")]
    [SerializeField] private Material brokenShieldMaterial;
    [Tooltip("Particle system to play when shield breaks.")]
    [SerializeField] private ParticleSystem breakParticles;
    [Tooltip("Particle system to play when shield regenerates.")]
    [SerializeField] private ParticleSystem regenParticles;

    private void OnEnable()
    {
        if (targetShieldSystem == null)
        {
            Debug.LogError("ShieldVisualsAndUI requires a ShieldBreakSystem reference!", this);
            return;
        }

        // Subscribe to events
        targetShieldSystem.OnShieldActivated += OnShieldActivated;
        targetShieldSystem.OnShieldBroken += OnShieldBroken;
        targetShieldSystem.OnShieldBeginsRegeneration += OnShieldBeginsRegeneration;
        targetShieldSystem.OnShieldDurabilityChanged += OnShieldDurabilityChanged;

        // Initialize UI/visuals based on current state
        UpdateUI(targetShieldSystem.CurrentShieldDurability, targetShieldSystem.MaxShieldDurability);
        UpdateVisuals(targetShieldSystem.IsShieldActive, targetShieldSystem.IsShieldBroken);
    }

    private void OnDisable()
    {
        if (targetShieldSystem == null) return;

        // Unsubscribe from events to prevent memory leaks
        targetShieldSystem.OnShieldActivated -= OnShieldActivated;
        targetShieldSystem.OnShieldBroken -= OnShieldBroken;
        targetShieldSystem.OnShieldBeginsRegeneration -= OnShieldBeginsRegeneration;
        targetShieldSystem.OnShieldDurabilityChanged -= OnShieldDurabilityChanged;
    }

    /// <summary>
    /// Event handler for when the shield becomes active.
    /// </summary>
    private void OnShieldActivated()
    {
        Debug.Log("UI: Shield Activated!");
        UpdateVisuals(true, false);
        // Play an activation sound or effect
    }

    /// <summary>
    /// Event handler for when the shield breaks.
    /// </summary>
    private void OnShieldBroken()
    {
        Debug.Log("UI: Shield Broken!");
        UpdateVisuals(false, true);
        if (breakParticles != null) breakParticles.Play();
        // Play a breaking sound or effect
    }

    /// <summary>
    /// Event handler for when the shield begins its regeneration phase.
    /// </summary>
    private void OnShieldBeginsRegeneration()
    {
        Debug.Log("UI: Shield Begins Regeneration!");
        UpdateVisuals(false, false); // Not active, not broken (is regenerating)
        if (regenParticles != null) regenParticles.Play();
        // Play a regeneration start sound
    }

    /// <summary>
    /// Event handler for when shield durability changes. Updates UI.
    /// </summary>
    /// <param name="current">Current durability.</param>
    /// <param name="max">Maximum durability.</param>
    private void OnShieldDurabilityChanged(float current, float max)
    {
        UpdateUI(current, max);
    }

    /// <summary>
    /// Updates the UI elements (text and fill image).
    /// </summary>
    /// <param name="current">Current durability.</param>
    /// <param name="max">Maximum durability.</param>
    private void UpdateUI(float current, float max)
    {
        if (durabilityText != null)
        {
            durabilityText.text = $"Shield: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
        if (shieldFillImage != null)
        {
            shieldFillImage.fillAmount = max > 0 ? current / max : 0;
        }
    }

    /// <summary>
    /// Updates the visual appearance of the shield.
    /// </summary>
    /// <param name="isActive">True if the shield is currently active.</param>
    /// <param name="isBroken">True if the shield is currently broken.</param>
    private void UpdateVisuals(bool isActive, bool isBroken)
    {
        if (shieldRenderer != null)
        {
            // Simple material swap for active/broken states
            if (isBroken && brokenShieldMaterial != null)
            {
                shieldRenderer.material = brokenShieldMaterial;
                shieldRenderer.enabled = false; // Optionally hide the shield when broken
            }
            else if (isActive && activeShieldMaterial != null)
            {
                shieldRenderer.material = activeShieldMaterial;
                shieldRenderer.enabled = true;
            }
            else // regenerating state or initial
            {
                // Could use a regenerating material or just disable/enable based on durability
                shieldRenderer.enabled = isActive || targetShieldSystem.CurrentShieldDurability > 0;
            }
        }
    }
}
```

### 2.2. SimpleDamageDealer.cs (Interaction Example)

This script simulates an entity dealing damage to the `ShieldBreakSystem`.

```csharp
using UnityEngine;

/// <summary>
/// A simple script to demonstrate dealing damage to a ShieldBreakSystem.
/// This could be attached to a player, enemy, or projectile.
/// </summary>
public class SimpleDamageDealer : MonoBehaviour
{
    [Tooltip("The amount of damage this dealer applies per hit.")]
    [SerializeField] private float damageAmount = 25f;

    [Tooltip("The ShieldBreakSystem target to deal damage to.")]
    [SerializeField] private ShieldBreakSystem targetShieldSystem;

    // Optional: Use a button click or collision for damage.
    // For this example, we'll use a simple keyboard input.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DealDamage();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Attempting to restore shield via debug.");
            targetShieldSystem?.Debug_FullyRestoreShield();
        }
    }

    /// <summary>
    /// Triggers damage application to the target shield system.
    /// </summary>
    public void DealDamage()
    {
        if (targetShieldSystem != null)
        {
            Debug.Log($"{gameObject.name} dealing {damageAmount} damage to {targetShieldSystem.gameObject.name}'s shield.");
            targetShieldSystem.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning("SimpleDamageDealer needs a target ShieldBreakSystem assigned!", this);
        }
    }
}
```

---

## How to Set Up in Unity:

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create C# Scripts:**
    *   Create a new C# script named `ShieldBreakSystem.cs` and copy the code into it.
    *   Create a new C# script named `ShieldVisualsAndUI.cs` and copy the code into it.
    *   Create a new C# script named `SimpleDamageDealer.cs` and copy the code into it.
3.  **Create Game Objects:**
    *   **Player/Target Entity:**
        *   Create an empty GameObject (e.g., `Player`).
        *   Add the `ShieldBreakSystem.cs` component to `Player`.
        *   Adjust the shield configuration in the Inspector (e.g., `Max Shield Durability`, `Regen Rate`, etc.).
        *   **Visuals:** Add a 3D Object (e.g., a Sphere or Cube) as a child of `Player` to represent the shield. This is where `ShieldVisualsAndUI` will reference its `Renderer`.
        *   Create two new Materials (e.g., `ShieldActiveMat` - blue/green transparent, `ShieldBrokenMat` - red/grey transparent). Assign them to the `activeShieldMaterial` and `brokenShieldMaterial` slots in `ShieldVisualsAndUI`.
        *   Add the `ShieldVisualsAndUI.cs` component to `Player`. Drag the `Player`'s `ShieldBreakSystem` component into the `Target Shield System` slot of `ShieldVisualsAndUI`. Drag the `Renderer` of the child shield object into the `Shield Renderer` slot.
    *   **UI Canvas (Optional but Recommended):**
        *   Create a UI -> Canvas GameObject.
        *   Inside the Canvas, create a UI -> Text GameObject (e.g., `ShieldDurabilityText`). Position it somewhere visible.
        *   Inside the Canvas, create a UI -> Image GameObject (e.g., `ShieldFillBar`). Set its `Image Type` to `Filled` and `Fill Method` to `Horizontal` or `Radial`. Position it.
        *   Go back to the `Player` GameObject. Drag `ShieldDurabilityText` into the `Durability Text` slot of `ShieldVisualsAndUI`. Drag `ShieldFillBar` into the `Shield Fill Image` slot.
    *   **Damage Dealer:**
        *   Create an empty GameObject (e.g., `DamageSource`).
        *   Add the `SimpleDamageDealer.cs` component to `DamageSource`.
        *   Drag the `Player`'s `ShieldBreakSystem` component into the `Target Shield System` slot of `SimpleDamageDealer`.
4.  **Play the Scene:**
    *   Press the `Spacebar` to simulate damage from the `DamageSource`. Watch the shield durability decrease.
    *   Observe how the shield's visual state and UI update.
    *   When the shield breaks, it will enter the `Broken` state (e.g., change material, stop taking damage) and then transition to `Regenerating` after the `shieldBrokenDuration`.
    *   Press `R` to instantly restore the shield (debug function).

This setup provides a complete, working, and educational example of the ShieldBreakSystem pattern in Unity, demonstrating state management and event-driven communication.