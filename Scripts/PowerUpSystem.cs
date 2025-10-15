// Unity Design Pattern Example: PowerUpSystem
// This script demonstrates the PowerUpSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical, and educational implementation of the 'PowerUpSystem' design pattern in Unity using C#. It leverages Scriptable Objects to define power-up effects, making the system highly flexible and data-driven.

---

## PowerUpSystem Design Pattern in Unity

The PowerUpSystem design pattern allows you to define, manage, and apply various temporary or permanent enhancements (power-ups) to game entities (like players). It often involves:

1.  **Effect Definition:** Abstractly defining what a power-up *does*.
2.  **Concrete Effects:** Specific implementations of power-up effects (e.g., speed boost, invincibility).
3.  **Target Manager:** An entity responsible for applying and removing effects.
4.  **Pickup Mechanism:** How a player acquires a power-up in the game world.

This example uses:
*   **Scriptable Objects** for defining `PowerUpEffect` assets, which encapsulates the `Apply` and `Remove` logic for each power-up type. This makes power-ups data-driven and easy to create/modify in the editor.
*   A `PlayerPowerUpManager` MonoBehaviour that manages all active power-ups on a player, including their durations.
*   A `PowerUpPickup` MonoBehaviour for the actual in-game items players interact with.

---

### Project Setup Steps in Unity:

1.  **Create C# Scripts:** Create the following C# scripts in your Unity project (e.g., in a `Scripts` folder). Copy the code into each respective file.
    *   `PowerUpEffect.cs`
    *   `SpeedBoostPowerUpEffect.cs`
    *   `InvincibilityPowerUpEffect.cs`
    *   `DamageBoostPowerUpEffect.cs`
    *   `PlayerPowerUpManager.cs`
    *   `PowerUpPickup.cs`
    *   `SimplePlayerMovement.cs` (for demonstration)

2.  **Create PowerUp Scriptable Assets:**
    *   In your Project window, right-click -> Create -> PowerUps -> Speed Boost. Name it "SpeedBoost_Effect".
    *   Right-click -> Create -> PowerUps -> Invincibility. Name it "Invincibility_Effect".
    *   Right-click -> Create -> PowerUps -> Damage Boost. Name it "DamageBoost_Effect".
    *   You can adjust their `duration`, `speedMultiplier`, `damageMultiplier` in the Inspector.

3.  **Create a Player GameObject:**
    *   Create a 3D Object (e.g., a `Cube`) and name it "Player".
    *   Add a `CharacterController` component to it.
    *   Add the `PlayerPowerUpManager` script to the "Player" GameObject.
    *   Add the `SimplePlayerMovement` script to the "Player" GameObject.

4.  **Create PowerUp Pickup GameObjects:**
    *   Create a few more 3D Objects (e.g., `Sphere` for Speed, `Capsule` for Invincibility, `Cylinder` for Damage). Name them "PowerUp_Speed", "PowerUp_Invincibility", "PowerUp_Damage".
    *   For *each* of these power-up GameObjects:
        *   Add a `Box Collider` (or other appropriate collider) and **check the "Is Trigger" box**.
        *   Add a `Rigidbody` component. **Check the "Is Kinematic" box** to prevent physics from affecting it, but still allow trigger detection.
        *   Add the `PowerUpPickup` script.
        *   In the Inspector of the `PowerUpPickup` script, drag the corresponding ScriptableObject asset (e.g., "SpeedBoost_Effect" for "PowerUp_Speed") into the `Power Up Effect` slot.
        *   (Optional): You can add some simple materials to distinguish them.

5.  **Set up the Scene:**
    *   Place the "Player" and your "PowerUp" GameObjects in the scene.
    *   Add a `Plane` or `Cube` for the ground so the player has something to move on.

6.  **Run the Scene:**
    *   Press Play. Move the player using WASD.
    *   Observe the console for logs regarding speed, damage, and invincibility when picking up power-ups and using Space (attack) or T (take damage).

---

### 1. `PowerUpEffect.cs` (Abstract Base Class - ScriptableObject)

This abstract class defines the common interface for all power-up effects. By inheriting from `ScriptableObject`, we can create asset files for each specific power-up type, making them highly reusable and data-driven.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T> if used in concrete effects, good practice to include

/// <summary>
/// The abstract base class for all power-up effects.
/// This uses the Strategy Pattern to define how a power-up is applied and removed.
/// By inheriting from ScriptableObject, we can create individual power-up assets
/// in the Unity Editor, making them highly reusable and data-driven.
/// </summary>
public abstract class PowerUpEffect : ScriptableObject
{
    [Tooltip("The duration of this power-up effect in seconds.")]
    public float duration = 5f; // Default duration for the effect

    /// <summary>
    /// Applies the power-up effect to the target.
    /// Concrete implementations will override this to modify player stats or behavior.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager instance to which the effect is applied.</param>
    public abstract void Apply(PlayerPowerUpManager target);

    /// <summary>
    /// Removes the power-up effect from the target, reverting any changes made by Apply().
    /// Concrete implementations will override this to restore original player stats or behavior.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager instance from which the effect is removed.</param>
    public abstract void Remove(PlayerPowerUpManager target);
}
```

### 2. Concrete `PowerUpEffect` Implementations (ScriptableObjects)

These classes define specific power-up behaviors. They are created as assets in the Unity Editor.

#### `SpeedBoostPowerUpEffect.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete PowerUpEffect that increases the player's movement speed.
/// </summary>
[CreateAssetMenu(fileName = "NewSpeedBoostPowerUp", menuName = "PowerUps/Speed Boost")]
public class SpeedBoostPowerUpEffect : PowerUpEffect
{
    [Tooltip("The multiplier for the player's movement speed.")]
    public float speedMultiplier = 1.5f; // How much to multiply speed by

    /// <summary>
    /// Applies the speed boost by multiplying the player's current speed.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to apply the effect to.</param>
    public override void Apply(PlayerPowerUpManager target)
    {
        Debug.Log($"Applying Speed Boost: Speed multiplied by {speedMultiplier} for {duration} seconds.");
        target.ApplySpeedModifier(speedMultiplier);
    }

    /// <summary>
    /// Removes the speed boost by dividing the player's speed back to its previous value.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to remove the effect from.</param>
    public override void Remove(PlayerPowerUpManager target)
    {
        Debug.Log("Removing Speed Boost: Reverting speed.");
        target.RemoveSpeedModifier(speedMultiplier);
    }
}
```

#### `InvincibilityPowerUpEffect.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete PowerUpEffect that makes the player invincible.
/// </summary>
[CreateAssetMenu(fileName = "NewInvincibilityPowerUp", menuName = "PowerUps/Invincibility")]
public class InvincibilityPowerUpEffect : PowerUpEffect
{
    /// <summary>
    /// Applies invincibility by setting the player's invincibility status to true.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to apply the effect to.</param>
    public override void Apply(PlayerPowerUpManager target)
    {
        Debug.Log($"Applying Invincibility for {duration} seconds.");
        target.SetInvincible(true);
    }

    /// <summary>
    /// Removes invincibility by setting the player's invincibility status to false.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to remove the effect from.</param>
    public override void Remove(PlayerPowerUpManager target)
    {
        Debug.Log("Removing Invincibility.");
        target.SetInvincible(false);
    }
}
```

#### `DamageBoostPowerUpEffect.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete PowerUpEffect that increases the player's attack damage.
/// </summary>
[CreateAssetMenu(fileName = "NewDamageBoostPowerUp", menuName = "PowerUps/Damage Boost")]
public class DamageBoostPowerUpEffect : PowerUpEffect
{
    [Tooltip("The multiplier for the player's attack damage.")]
    public float damageMultiplier = 2.0f;

    /// <summary>
    /// Applies the damage boost by multiplying the player's current damage.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to apply the effect to.</param>
    public override void Apply(PlayerPowerUpManager target)
    {
        Debug.Log($"Applying Damage Boost: Damage multiplied by {damageMultiplier} for {duration} seconds.");
        target.ApplyDamageModifier(damageMultiplier);
    }

    /// <summary>
    /// Removes the damage boost by dividing the player's damage back to its previous value.
    /// </summary>
    /// <param name="target">The PlayerPowerUpManager to remove the effect from.</param>
    public override void Remove(PlayerPowerUpManager target)
    {
        Debug.Log("Removing Damage Boost: Reverting damage.");
        target.RemoveDamageModifier(damageMultiplier);
    }
}
```

### 3. `PlayerPowerUpManager.cs` (Monobehaviour on Player)

This script manages all active power-ups on the player. It handles applying effects, starting duration timers, and removing effects when they expire or are refreshed.

```csharp
using UnityEngine;
using System.Collections; // For Coroutines
using System.Collections.Generic; // For List<T>

/// <summary>
/// Helper class to track an active power-up instance on the player.
/// Stores the effect, its expiration time, and the coroutine managing its removal.
/// </summary>
[System.Serializable] // Allows viewing in Inspector for debugging, though not directly edited
public class ActivePowerUp
{
    public PowerUpEffect effect;
    public float endTime;
    public Coroutine removalCoroutine; // To manage timed removal

    public ActivePowerUp(PowerUpEffect effect, float duration, Coroutine coroutine)
    {
        this.effect = effect;
        this.endTime = Time.time + duration; // Calculate when the effect should end
        this.removalCoroutine = coroutine;
    }
}

/// <summary>
/// Manages all power-up effects applied to the player.
/// This script holds the player's dynamic stats and provides methods for power-up effects
/// to modify these stats. It also handles the timed application and removal of effects.
/// </summary>
public class PlayerPowerUpManager : MonoBehaviour
{
    // --- Player Base Stats (example) ---
    [Header("Player Base Stats")]
    [Tooltip("The default movement speed of the player.")]
    public float baseMoveSpeed = 5f;
    [Tooltip("The default attack damage of the player.")]
    public float baseDamage = 10f;

    // --- Current Player Stats (modified by power-ups) ---
    private float _currentMoveSpeed;
    /// <summary>
    /// The player's current movement speed, potentially modified by power-ups.
    /// </summary>
    public float currentMoveSpeed
    {
        get { return _currentMoveSpeed; }
        private set
        {
            if (_currentMoveSpeed != value) // Only update if value changed to avoid unnecessary logs/events
            {
                _currentMoveSpeed = value;
                Debug.Log($"<color=cyan>Player Move Speed updated to: {_currentMoveSpeed:F2}</color>");
            }
        }
    }

    private float _currentDamage;
    /// <summary>
    /// The player's current attack damage, potentially modified by power-ups.
    /// </summary>
    public float currentDamage
    {
        get { return _currentDamage; }
        private set
        {
            if (_currentDamage != value)
            {
                _currentDamage = value;
                Debug.Log($"<color=orange>Player Damage updated to: {_currentDamage:F2}</color>");
            }
        }
    }

    /// <summary>
    /// Indicates if the player is currently invincible.
    /// </summary>
    public bool isInvincible { get; private set; } = false;

    // List to keep track of currently active power-ups and their timers
    [Header("Active Power-Ups (Debug)")]
    [SerializeField] // Make private list visible in Inspector for debugging
    private List<ActivePowerUp> activePowerUps = new List<ActivePowerUp>();

    void Awake()
    {
        // Initialize current stats to base stats at the start
        _currentMoveSpeed = baseMoveSpeed;
        _currentDamage = baseDamage;
    }

    /// <summary>
    /// Applies a new power-up effect to the player.
    /// If the same effect type is already active, its duration is refreshed.
    /// This is the main entry point for picking up a power-up.
    /// </summary>
    /// <param name="newEffect">The PowerUpEffect ScriptableObject to apply.</param>
    public void ApplyPowerUp(PowerUpEffect newEffect)
    {
        // Check if an effect of the same *type* is already active
        // This allows for refreshing duration or stacking effects depending on game design.
        ActivePowerUp existingPowerUp = activePowerUps.Find(ap => ap.effect.GetType() == newEffect.GetType());

        if (existingPowerUp != null)
        {
            // If the same effect type is already active, we refresh its duration.
            // 1. Stop the old removal coroutine to prevent it from removing the effect prematurely.
            StopCoroutine(existingPowerUp.removalCoroutine);

            // 2. IMPORTANT: Remove the old effect *before* reapplying the new one.
            // This is crucial for effects that modify stats by multiplication/addition,
            // ensuring the base value is correct before the new multiplier is applied.
            existingPowerUp.effect.Remove(this);
            activePowerUps.Remove(existingPowerUp); // Remove from list to replace with refreshed one
            Debug.Log($"<color=yellow>Refreshed {newEffect.name} duration.</color>");
        }

        // Apply the new effect (or re-apply if refreshed)
        newEffect.Apply(this);

        // Start a coroutine to handle the timed removal of this specific effect
        Coroutine removalCoroutine = StartCoroutine(RemovePowerUpAfterDuration(newEffect));

        // Add the newly applied (or refreshed) power-up to our active list
        activePowerUps.Add(new ActivePowerUp(newEffect, newEffect.duration, removalCoroutine));
    }

    /// <summary>
    /// Coroutine to handle the timed removal of a power-up effect.
    /// Waits for the specified duration, then triggers the effect's removal logic.
    /// </summary>
    /// <param name="effectToRemove">The PowerUpEffect ScriptableObject to remove.</param>
    private IEnumerator RemovePowerUpAfterDuration(PowerUpEffect effectToRemove)
    {
        yield return new WaitForSeconds(effectToRemove.duration);

        // Find the power-up in the list to ensure it hasn't been refreshed or removed manually
        ActivePowerUp expiredPowerUp = activePowerUps.Find(ap => ap.effect == effectToRemove);

        if (expiredPowerUp != null)
        {
            expiredPowerUp.effect.Remove(this); // Call the specific removal logic
            activePowerUps.Remove(expiredPowerUp); // Remove from our tracking list
            Debug.Log($"<color=grey>{expiredPowerUp.effect.name} has expired and been removed.</color>");
        }
    }

    // --- Methods for PowerUpEffects to interact with player stats ---
    // These methods provide a controlled interface for effects to modify player attributes.

    /// <summary>
    /// Multiplies the player's current movement speed by a given factor.
    /// </summary>
    /// <param name="multiplier">The factor to multiply speed by.</param>
    public void ApplySpeedModifier(float multiplier)
    {
        currentMoveSpeed *= multiplier;
    }

    /// <summary>
    /// Divides the player's current movement speed by a given factor.
    /// Used to revert speed changes made by power-ups.
    /// </summary>
    /// <param name="multiplier">The factor to divide speed by.</param>
    public void RemoveSpeedModifier(float multiplier)
    {
        currentMoveSpeed /= multiplier;
    }

    /// <summary>
    /// Sets the player's invincibility status.
    /// </summary>
    /// <param name="state">True for invincible, false otherwise.</param>
    public void SetInvincible(bool state)
    {
        isInvincible = state;
        Debug.Log($"<color=lime>Player Invincibility set to: {isInvincible}</color>");
    }

    /// <summary>
    /// Multiplies the player's current attack damage by a given factor.
    /// </summary>
    /// <param name="multiplier">The factor to multiply damage by.</param>
    public void ApplyDamageModifier(float multiplier)
    {
        currentDamage *= multiplier;
    }

    /// <summary>
    /// Divides the player's current attack damage by a given factor.
    /// Used to revert damage changes made by power-ups.
    /// </summary>
    /// <param name="multiplier">The factor to divide damage by.</param>
    public void RemoveDamageModifier(float multiplier)
    {
        currentDamage /= multiplier;
    }

    // --- Example methods to simulate player actions/reactions ---
    // These methods use the player's current stats, which are influenced by power-ups.

    /// <summary>
    /// Simulates the player taking damage, respecting invincibility.
    /// </summary>
    /// <param name="amount">The amount of damage to attempt to take.</param>
    public void TakeDamage(float amount)
    {
        if (isInvincible)
        {
            Debug.Log("<color=green>Player is invincible! No damage taken.</color>");
            return;
        }
        Debug.Log($"<color=red>Player took {amount} damage.</color> (Current HP not implemented, but you'd subtract it here.)");
    }

    /// <summary>
    /// Simulates the player attacking, using their current damage stat.
    /// </summary>
    public void Attack()
    {
        Debug.Log($"<color=blue>Player attacks for {currentDamage:F2} damage!</color>");
    }
}
```

### 4. `PowerUpPickup.cs` (Monobehaviour on Power-Up GameObjects)

This script is attached to the actual 3D objects in your scene that represent power-ups. When the player collides with them, they trigger the `ApplyPowerUp` method on the player's manager.

```csharp
using UnityEngine;

/// <summary>
/// Represents a physical power-up item in the game world that a player can pick up.
/// When triggered by the player, it applies its associated PowerUpEffect.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure there's a collider for trigger detection
public class PowerUpPickup : MonoBehaviour
{
    [Tooltip("The ScriptableObject defining this power-up's effect.")]
    public PowerUpEffect powerUpEffect;

    [Tooltip("Optional: A GameObject prefab to instantiate as a visual/audio effect on pickup.")]
    public GameObject pickupEffectPrefab;

    void Awake()
    {
        // Ensure the collider is set to 'Is Trigger' for OnTriggerEnter to work
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} is not set to 'Is Trigger'. Setting it now. " +
                             "Ensure your power-up pickups have a Rigidbody (even kinematic) for triggers to work correctly.", this);
            col.isTrigger = true;
        }

        // Also ensure a Rigidbody is present for trigger events
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Make it kinematic so it doesn't fall/move from physics
            Debug.LogWarning($"Added Kinematic Rigidbody to {gameObject.name} for trigger detection.", this);
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        // Attempt to get the PlayerPowerUpManager from the colliding object
        PlayerPowerUpManager playerManager = other.GetComponent<PlayerPowerUpManager>();

        // If the colliding object has a PlayerPowerUpManager and we have an effect defined
        if (playerManager != null && powerUpEffect != null)
        {
            // Apply the power-up effect to the player
            playerManager.ApplyPowerUp(powerUpEffect);

            // Play optional pickup effect (e.g., particle system, sound)
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy the power-up item from the scene after pickup
            Destroy(gameObject);
            Debug.Log($"<color=purple>Picked up: {powerUpEffect.name}</color>");
        }
    }

    /// <summary>
    /// Draws a debug gizmo in the editor to visualize the pickup area.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Draw a wire sphere based on the collider's bounds if available, otherwise a default size
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireSphere(col.bounds.center, col.bounds.extents.magnitude);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
```

### 5. `SimplePlayerMovement.cs` (Demonstration Player Controller)

A basic player movement script to demonstrate how power-ups affect player stats. It will get the current speed from the `PlayerPowerUpManager`.

```csharp
using UnityEngine;

/// <summary>
/// A simple player movement script that uses a CharacterController
/// and gets its movement speed from the PlayerPowerUpManager.
/// Also includes basic attack and take damage functions for testing power-ups.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerPowerUpManager))] // Ensure the player has the power-up manager
public class SimplePlayerMovement : MonoBehaviour
{
    private PlayerPowerUpManager playerPowerUpManager;
    private CharacterController controller;

    [Tooltip("How fast the player rotates to face movement direction.")]
    public float rotationSpeed = 720f; // Degrees per second

    void Awake()
    {
        playerPowerUpManager = GetComponent<PlayerPowerUpManager>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- Player Movement ---
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down Arrow

        // Calculate movement direction relative to the camera/world
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical);
        moveDirection.Normalize(); // Ensure consistent speed when moving diagonally

        // Apply movement using CharacterController and currentMoveSpeed from PowerUpManager
        controller.Move(moveDirection * playerPowerUpManager.currentMoveSpeed * Time.deltaTime);

        // Optional: Rotate player to face movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }


        // --- Simulate Actions for testing power-ups ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playerPowerUpManager.Attack(); // Test damage boost
        }
        if (Input.GetKeyDown(KeyCode.T)) // 'T' for Take damage
        {
            playerPowerUpManager.TakeDamage(25); // Test invincibility
        }
    }
}
```