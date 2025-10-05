// Unity Design Pattern Example: FallDamageSystem
// This script demonstrates the FallDamageSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete and practical implementation of a 'FallDamageSystem' in Unity, adhering to good design principles and Unity best practices. It's broken down into several interconnected components to demonstrate separation of concerns and clear responsibilities.

---

### Understanding the 'FallDamageSystem' Design Pattern

The "FallDamageSystem" isn't a standard GoF (Gang of Four) design pattern, but rather represents a common architectural approach for a game mechanic. It embodies several design principles:

1.  **Separation of Concerns:**
    *   **Fall Detection:** Handled by `CharacterFallDetector`. Its sole job is to identify when a character falls and lands, and how far.
    *   **Damage Calculation & Application:** Handled by `FallDamageSystem`. Its sole job is to determine how much damage should be applied based on fall distance and system settings, and then to apply it.
    *   **Health Management:** Handled by `HealthSystem`. Its sole job is to manage an entity's health and respond to damage.

2.  **Interface-Based Interaction:**
    *   The `FallDamageSystem` doesn't care about the concrete type of the object taking damage. It interacts through the `IFallDamageable` interface. This means any class can take fall damage by simply implementing this interface.

3.  **Configurability & Extensibility:**
    *   The `FallDamageSystem` exposes parameters like `minFallHeight`, `damagePerMeter`, and `damageCurve` in the Inspector, allowing designers to easily tune fall damage without code changes.
    *   The use of `IFallDamageable` makes it easy to add new types of entities that can take fall damage (e.g., a destructible environment piece, an enemy).

4.  **Dependency Management:**
    *   The `CharacterFallDetector` depends on the `FallDamageSystem` to apply damage. This dependency is managed via a serialized field (Inspector assignment) or `FindObjectOfType`.

---

### C# Unity Scripts

Here are the four scripts required for the system:

1.  `IFallDamageable.cs`
2.  `HealthSystem.cs`
3.  `FallDamageSystem.cs`
4.  `CharacterFallDetector.cs`

---

#### 1. `IFallDamageable.cs`

This interface defines the contract for any object that can receive fall damage. It's crucial for decoupling.

```csharp
using UnityEngine;

/// <summary>
/// Interface for objects that can take fall damage.
/// This promotes decoupling, allowing the FallDamageSystem to interact with any object
/// that implements this interface, regardless of its specific type (e.g., PlayerHealth, EnemyHealth).
/// </summary>
public interface IFallDamageable
{
    /// <summary>
    /// Applies fall damage to the implementing object.
    /// </summary>
    /// <param name="damage">The amount of fall damage to apply.</param>
    void TakeFallDamage(float damage);
}
```

---

#### 2. `HealthSystem.cs`

A simple health component that implements `IFallDamageable`. This shows how a specific object (like a player) can integrate with the Fall Damage System.

```csharp
using UnityEngine;
using System.Collections; // Standard Unity namespace

/// <summary>
/// A basic HealthSystem component that manages an entity's health.
/// It implements the IFallDamageable interface, allowing the FallDamageSystem
/// to apply damage to it without needing to know it's a "HealthSystem".
/// </summary>
public class HealthSystem : MonoBehaviour, IFallDamageable
{
    [Header("Health Settings")]
    [Tooltip("The maximum health of the character.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("The current health of the character. For debugging/display.")]
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Awake()
    {
        // Initialize current health to max health when the component starts.
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the character's health.
    /// This method is part of the IFallDamageable interface contract.
    /// </summary>
    /// <param name="damage">The amount of damage to take.</param>
    public void TakeFallDamage(float damage)
    {
        if (damage <= 0) return; // Ignore non-positive damage

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // Ensure health doesn't go below zero

        Debug.Log($"{gameObject.name} took {damage:F1} fall damage! Current Health: {currentHealth:F1}/{maxHealth:F1}");

        if (currentHealth <= 0)
        {
            Die(); // Handle death if health drops to zero or below
        }
    }

    /// <summary>
    /// Example method for when the character dies.
    /// In a real game, this would trigger animations, disable input, show UI, etc.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died from fall damage!");
        // For demonstration, we'll just disable the collider and potentially the script.
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
        // You might want to disable the HealthSystem itself or other components.
        // this.enabled = false;
        // Optionally, destroy the GameObject after a delay:
        // Destroy(gameObject, 3f);
    }

    /// <summary>
    /// Heals the character for a specified amount.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(float amount)
    {
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health doesn't exceed max

        Debug.Log($"{gameObject.name} healed {amount:F1}! Current Health: {currentHealth:F1}/{maxHealth:F1}");
    }
}
```

---

#### 3. `FallDamageSystem.cs`

This is the central component of the pattern. It calculates damage based on configured parameters and applies it to any `IFallDamageable` object.

```csharp
using UnityEngine;
using System.Collections; // Standard Unity namespace

/// <summary>
/// The FallDamageSystem component is responsible for calculating and applying fall damage
/// to objects that implement the IFallDamageable interface.
///
/// This system demonstrates a common design pattern in games:
/// 1.  **Centralized Logic:** All fall damage calculation is handled in one place.
/// 2.  **Decoupling:** It doesn't detect falls itself; it relies on other components
///     (like CharacterFallDetector) to tell it when a fall has occurred and how far.
/// 3.  **Interface-Based Interaction:** It interacts with any object that implements `IFallDamageable`,
///     promoting flexibility and reusability (e.g., a player, an enemy, a destructible object).
/// 4.  **Configurability:** Damage calculation is highly customizable via Inspector settings,
///     including a flexible `AnimationCurve` for non-linear damage scaling.
/// </summary>
public class FallDamageSystem : MonoBehaviour
{
    [Header("Fall Damage Settings")]
    [Tooltip("The minimum vertical fall distance (in meters) required to start taking damage.")]
    [SerializeField] private float minFallHeight = 3.0f;

    [Tooltip("The base damage dealt per meter fallen beyond the minimum fall height.")]
    [SerializeField] private float damagePerMeter = 5.0f;

    [Tooltip("An AnimationCurve to modify the damage based on the *excess* fall distance (beyond minFallHeight). " +
             "X-axis: Normalized Excess Fall Distance (0-1), Y-axis: Damage Multiplier. " +
             "The curve is mapped, by default, such that '1' on the X-axis corresponds to 5 times the minFallHeight beyond minFallHeight.")]
    [SerializeField] private AnimationCurve damageCurve = AnimationCurve.Linear(0, 1, 1, 1); // Default to linear (no curve effect)

    [Tooltip("If true, the system will log detailed information about fall damage calculation.")]
    [SerializeField] private bool debugMode = false;

    // A static instance could be used for a true "singleton-like" system,
    // but passing a reference (as done in CharacterFallDetector) is often more explicit
    // and flexible, especially if you might have multiple fall damage systems in different areas.
    // public static FallDamageSystem Instance { get; private set; }
    // void Awake() { if (Instance != null && Instance != this) Destroy(this); else Instance = this; }

    /// <summary>
    /// Calculates and applies fall damage to a target GameObject.
    /// This is the primary public method that external components (like a fall detector) will call.
    /// </summary>
    /// <param name="target">The GameObject that is taking fall damage.</param>
    /// <param name="fallDistance">The total vertical distance the target has fallen (positive value, in meters).</param>
    public void ApplyFallDamage(GameObject target, float fallDistance)
    {
        if (target == null)
        {
            Debug.LogError("FallDamageSystem: Target GameObject is null.", this);
            return;
        }

        // Ensure fallDistance is positive; a negative value would imply rising.
        if (fallDistance < 0)
        {
            if (debugMode)
            {
                Debug.LogWarning($"FallDistance was negative ({fallDistance:F2}). Taking absolute value.", this);
            }
            fallDistance = Mathf.Abs(fallDistance);
        }

        // Check if the fall is below the minimum required height for damage.
        if (fallDistance < minFallHeight)
        {
            if (debugMode)
            {
                Debug.Log($"FallDistance ({fallDistance:F2}m) for {target.name} is below minFallHeight ({minFallHeight:F2}m). No damage applied.", this);
            }
            return; // No damage if fall is too short
        }

        // Calculate the excess fall distance (beyond the minimum).
        float excessFallDistance = fallDistance - minFallHeight;

        // Calculate base damage before applying the curve.
        float baseDamage = excessFallDistance * damagePerMeter;

        // Apply damage curve multiplier for more nuanced damage scaling.
        // The normalized value for the curve's X-axis (0-1) is derived from the excess fall distance.
        // Here, we map 'excessFallDistance' to a range, e.g., 0 to 5 * minFallHeight.
        // This makes the curve's X-axis representative of increasing severity of fall.
        float curveNormalizedInput = Mathf.Clamp01(excessFallDistance / (minFallHeight * 5f)); // Max curve effect at 5x minFallHeight beyond minFallHeight
        float curveMultiplier = damageCurve.Evaluate(curveNormalizedInput);

        // Final damage calculation.
        float finalDamage = baseDamage * curveMultiplier;

        // Ensure damage is never negative or negligibly small.
        if (finalDamage <= 0.001f) // Use a small epsilon for float comparison
        {
            if (debugMode)
            {
                Debug.Log($"Calculated fall damage for {target.name} was zero or negligible ({finalDamage:F2} DMG). No damage applied.", this);
            }
            return;
        }

        // Attempt to get the IFallDamageable interface from the target GameObject.
        if (target.TryGetComponent(out IFallDamageable damageable))
        {
            damageable.TakeFallDamage(finalDamage);
            if (debugMode)
            {
                Debug.Log($"FallDamageSystem: Applied {finalDamage:F1} damage to {target.name} for a fall of {fallDistance:F1}m " +
                          $"(Excess: {excessFallDistance:F1}m, Curve Multiplier: {curveMultiplier:F2}).", this);
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.LogWarning($"FallDamageSystem: {target.name} fell {fallDistance:F1}m but does not implement IFallDamageable. No damage applied.", this);
            }
        }
    }
}
```

---

#### 4. `CharacterFallDetector.cs`

This component is attached to the character. It uses a `CharacterController` to determine when the character is grounded or airborne and reports fall distances to the `FallDamageSystem`.

```csharp
using UnityEngine;
using System.Collections; // Standard Unity namespace

/// <summary>
/// The CharacterFallDetector component is responsible for detecting when a character
/// (assumed to be controlled by a CharacterController) lands after a fall.
/// It then calculates the fall distance and reports it to a FallDamageSystem instance.
///
/// This component demonstrates the decoupling aspect of the FallDamageSystem pattern:
/// - It focuses solely on *detecting* the fall state and distance.
/// - It does *not* calculate damage or apply it directly.
/// - It depends on a `FallDamageSystem` to handle the actual damage logic.
/// </summary>
[RequireComponent(typeof(CharacterController))] // This detector works best with Unity's CharacterController
public class CharacterFallDetector : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the FallDamageSystem in the scene. Assign manually or ensure one exists.")]
    [SerializeField] private FallDamageSystem fallDamageSystem;

    [Header("Detection Settings")]
    [Tooltip("The LayerMask for what is considered 'ground'. Only objects on these layers will prevent falling.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("The radius of the sphere used to check for ground.")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [Tooltip("The vertical offset from the character's pivot point for the ground check sphere. " +
             "Should be slightly above the bottom of the CharacterController to avoid ground clipping.")]
    [SerializeField] private float groundCheckOffset = 0.1f;
    [Tooltip("A small vertical threshold to prevent reporting fall damage for tiny height changes on uneven terrain.")]
    [SerializeField] private float minFallDistanceThreshold = 0.1f;

    [Header("Debug Settings")]
    [Tooltip("If true, draws Gizmos for ground checks and logs fall information to the console.")]
    [SerializeField] private bool debugMode = false;

    private CharacterController characterController;
    private Vector3 lastGroundedPosition;
    private bool wasGrounded;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterFallDetector requires a CharacterController component on the same GameObject.", this);
            enabled = false; // Disable this component if the dependency is missing
        }

        // Attempt to find FallDamageSystem if not assigned in Inspector
        if (fallDamageSystem == null)
        {
            fallDamageSystem = FindObjectOfType<FallDamageSystem>();
            if (fallDamageSystem == null)
            {
                Debug.LogError("No FallDamageSystem found in the scene. Please create one.", this);
                enabled = false; // Disable if no FallDamageSystem is present
            }
        }
    }

    void Start()
    {
        // Initialize lastGroundedPosition if the character starts grounded.
        // This prevents immediate fall damage if the player starts slightly above ground.
        if (IsGrounded())
        {
            lastGroundedPosition = transform.position;
            wasGrounded = true;
        }
    }

    void Update()
    {
        // Determine the current grounded state using a robust sphere check.
        bool isCurrentlyGrounded = IsGrounded();

        if (isCurrentlyGrounded)
        {
            // Character is currently on the ground.
            if (!wasGrounded)
            {
                // Character just landed after being airborne.
                HandleLanding();
            }
            // Always update lastGroundedPosition when on the ground to track the highest point before falling.
            lastGroundedPosition = transform.position;
        }
        else
        {
            // Character is currently in the air.
            if (wasGrounded && debugMode)
            {
                // Character just became airborne (e.g., walked off a ledge, jumped).
                // lastGroundedPosition is already set from the moment before leaving the ground.
                Debug.Log($"{gameObject.name} just became airborne from Y={lastGroundedPosition.y:F1}m.", this);
            }
        }
        wasGrounded = isCurrentlyGrounded; // Update grounded state for next frame
    }

    /// <summary>
    /// Checks if the character is currently on the ground using a Physics.CheckSphere.
    /// This is more robust than CharacterController.isGrounded alone, especially on slopes.
    /// </summary>
    /// <returns>True if the character is grounded, false otherwise.</returns>
    private bool IsGrounded()
    {
        // Calculate the origin for the sphere check (at the base of the character, slightly adjusted).
        // The CharacterController's bottom is at transform.position.y - controller.height/2.
        // We want the sphere to be just below the character's feet.
        Vector3 sphereOrigin = transform.position + characterController.center - Vector3.up * (characterController.height / 2f - groundCheckRadius - groundCheckOffset);

        // Perform the sphere check against the defined groundLayer.
        return Physics.CheckSphere(sphereOrigin, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Called when the character lands after being airborne.
    /// Calculates the fall distance and notifies the FallDamageSystem.
    /// </summary>
    private void HandleLanding()
    {
        // Calculate the vertical fall distance from the last known grounded position.
        float fallDistance = lastGroundedPosition.y - transform.position.y;

        if (debugMode)
        {
            Debug.Log($"--- {gameObject.name} Landed ---", this);
            Debug.Log($"Last Grounded Y: {lastGroundedPosition.y:F1}", this);
            Debug.Log($"Current Landed Y: {transform.position.y:F1}", this);
            Debug.Log($"Calculated Raw Fall Distance: {fallDistance:F1}m", this);
        }

        // Only report positive fall distances greater than a small threshold
        // to ignore minor vertical jitters or small height differences on uneven terrain.
        if (fallDistance > minFallDistanceThreshold)
        {
            // Delegate the damage calculation and application to the FallDamageSystem.
            fallDamageSystem.ApplyFallDamage(gameObject, fallDistance);
        }
        else if (debugMode)
        {
            Debug.Log($"Fall distance ({fallDistance:F2}m) was below threshold ({minFallDistanceThreshold:F2}m). No fall damage.", this);
        }
    }

    /// <summary>
    /// Draws debugging Gizmos in the Scene view to visualize the ground check.
    /// </summary>
    void OnDrawGizmos()
    {
        if (debugMode && characterController != null)
        {
            // Calculate the sphere origin for Gizmo drawing.
            Vector3 sphereOrigin = transform.position + characterController.center - Vector3.up * (characterController.height / 2f - groundCheckRadius - groundCheckOffset);

            // Draw the ground check sphere. Green if grounded, Red if airborne.
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(sphereOrigin, groundCheckRadius);

            // Draw a line and sphere at the last grounded position if currently airborne.
            if (wasGrounded && !IsGrounded()) // Only draw when airborne after being grounded
            {
                Gizmos.color = Color.yellow;
                // Draw a sphere at the point the character last touched ground.
                Gizmos.DrawWireSphere(lastGroundedPosition, 0.2f);
                // Draw a line connecting the last grounded position to the current position (visualizing the fall path).
                Gizmos.DrawLine(lastGroundedPosition, transform.position);
            }
        }
    }
}
```

---

### How to Implement in Unity Editor (Example Usage)

Follow these steps to set up and test the Fall Damage System in your Unity project:

1.  **Create a 'Ground' Layer:**
    *   Go to `Edit` > `Project Settings` > `Tags and Layers`.
    *   Under `Layers`, add a new layer, e.g., "Ground".
    *   Make sure any objects you want your character to land on (planes, terrain, platforms) are assigned to this "Ground" layer in their Inspector.

2.  **Create the Fall Damage System Manager:**
    *   Create an Empty GameObject in your scene (e.g., `Right-click` in Hierarchy > `Create Empty`).
    *   Rename it to `FallDamageSystemManager`.
    *   Add the `FallDamageSystem.cs` script to this GameObject.
    *   **Configure `FallDamageSystem`:**
        *   Adjust `Min Fall Height` (e.g., `3` meters).
        *   Adjust `Damage Per Meter` (e.g., `5`).
        *   Modify the `Damage Curve` to control how damage scales. For instance, you could make it exponential for very high falls or cap it. Click on the curve field to open the curve editor.
        *   Set `Debug Mode` to `true` to see detailed console logs.

3.  **Set up your Player Character (or any entity that takes fall damage):**
    *   Create your Player GameObject. It should ideally have a `Capsule Collider` or similar and a `Rigidbody` (though `CharacterController` handles its own physics).
    *   Add a `CharacterController` component to your Player GameObject. (This script uses `CharacterController.isGrounded` and `Move()` for movement, not a `Rigidbody` directly).
    *   Add the `HealthSystem.cs` script to your Player GameObject.
    *   Add the `CharacterFallDetector.cs` script to your Player GameObject.
    *   **Configure `CharacterFallDetector`:**
        *   **`Fall Damage System`:** Drag your `FallDamageSystemManager` GameObject from the Hierarchy into this slot.
        *   **`Ground Layer`:** Select the "Ground" layer you created earlier.
        *   Adjust `Ground Check Radius` and `Ground Check Offset` to match your character's size and `CharacterController` setup. The Gizmos (when `Debug Mode` is on) will help visualize this.
        *   Set `Debug Mode` to `true` to see detailed fall detection logs and Gizmos.

4.  **Create a Scene for Testing:**
    *   Add a simple 3D Plane or Cube to act as your ground. Make sure it's on the "Ground" Layer.
    *   Create some elevated platforms or ledges for your character to fall from.

5.  **Simulate Falling:**
    *   Run the game.
    *   Move your character (using your existing character movement script or temporarily by manually positioning in the editor) onto a high platform.
    *   Walk or fall off the platform.
    *   Observe the Console window for messages from `CharacterFallDetector`, `FallDamageSystem`, and `HealthSystem`. You should see logs indicating fall detection, damage calculation, and health reduction.
    *   Experiment with different fall heights and `Damage Curve` settings.

This setup provides a robust, educational, and practical example of how to implement a 'FallDamageSystem' using Unity's component-based architecture and good design principles.