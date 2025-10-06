// Unity Design Pattern Example: HitStopSystem
// This script demonstrates the HitStopSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'HitStopSystem' is a design pattern commonly used in games, especially fighting games, action RPGs, or any game where impactful events (like a successful attack, a parry, or an explosion) need to be emphasized. It works by briefly pausing or slowing down global game time to give the player an immediate, satisfying feedback sensation, making the event feel more powerful.

Here's a complete, practical C# Unity example demonstrating the HitStopSystem pattern. This script includes the core system, along with example usage for a player shooting projectiles that trigger hit stop on collision.

To use this:
1.  Create a new C# script named `HitStopSystemExample.cs`.
2.  Copy and paste the entire code below into it.
3.  Follow the "Unity Scene Setup" instructions provided in the comments at the top of the script.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// --- HitStopSystem Design Pattern Example ---
///
/// This script demonstrates the 'HitStopSystem' design pattern in Unity.
/// It provides a centralized, reusable system for briefly pausing or slowing down
/// game time to emphasize impactful events like hits, explosions, or critical actions.
/// This enhances player feedback by making these moments feel more powerful and responsive.
///
/// Key characteristics of this implementation:
/// - **Centralized Control (Singleton):** A single point of access (`HitStopSystem.Instance`)
///   to trigger hit stops from anywhere in your game.
/// - **Global Time Manipulation:** Uses `Time.timeScale` to affect the global speed of time.
/// - **Real-time Waiting:** Uses `WaitForSecondsRealtime` for the hit stop duration
///   to ensure the pause lasts for actual seconds, regardless of `Time.timeScale`.
/// - **Smooth Transitions:** Can smoothly ramp `Time.timeScale` back to normal
///   to avoid abrupt jumps, using `Mathf.Lerp` and `Time.unscaledDeltaTime`.
/// - **Prevent Overlap:** Handles multiple hit stop requests by stopping an ongoing one
///   before starting a new one, ensuring predictable behavior.
///
/// --- Unity Scene Setup Instructions ---
///
/// To get this example working in your Unity project:
///
/// 1.  **Create a C# Script:** Create a new C# script in your Project window (e.g., `Assets/Scripts`)
///     and name it `HitStopSystemExample`. Copy and paste the entire code from this file into it.
///
/// 2.  **Scene Setup - Core Components:**
///     *   **GameManagers (HitStopSystem):** Create an empty GameObject in your scene,
///         rename it to "GameManagers". Attach the `HitStopSystem` component (from this script) to it.
///         This makes the HitStopSystem globally accessible.
///
/// 3.  **Scene Setup - Player & Shooter:**
///     *   **Player:** Create a 3D Cube (e.g., at `(0, 1, 0)`), rename it to "Player".
///         Attach the `PlayerShooter` component (from this script) to it.
///         This will be your projectile spawner.
///     *   **SpawnPoint:** As a child of "Player", create an empty GameObject (e.g., at `(0, 0, 1)`
///         relative to the Player). Rename it "SpawnPoint". Drag this "SpawnPoint" GameObject
///         from the Hierarchy to the `Spawn Point` field of the `PlayerShooter` component on "Player".
///
/// 4.  **Scene Setup - Target:**
///     *   **Target Dummy:** Create a 3D Cube (e.g., at `(0, 1, 10)`), rename it "TargetDummy".
///         Attach the `TargetDummy` component (from this script) to it.
///     *   **Layer for Target:** Select "TargetDummy". In the Inspector, click the "Layer" dropdown
///         (top right, usually defaults to "Default"). Click "Add Layer...". Create a new layer, e.g., "Targets".
///         Go back to "TargetDummy" and assign it to the "Targets" layer.
///
/// 5.  **Scene Setup - Projectile Prefab:**
///     *   **Create Projectile:** Create a 3D Sphere (e.g., scale `0.5, 0.5, 0.5`). Rename it "Projectile".
///     *   **Components for Projectile:**
///         *   Attach the `ProjectileBehavior` component (from this script) to "Projectile".
///         *   Ensure it has a `SphereCollider`. **Crucially, check the "Is Trigger" box on the Collider.**
///         *   Ensure it has a `Rigidbody` component (Unity often adds this automatically with a trigger collider).
///             You can leave `Is Kinematic` checked and `Use Gravity` unchecked as `ProjectileBehavior` handles movement.
///     *   **Configure Projectile Behavior:**
///         *   In the `ProjectileBehavior` component, click the "Hit Layers" dropdown and select "Targets"
///             (the layer you created for the TargetDummy).
///     *   **Create Prefab:** Drag the "Projectile" GameObject from the Hierarchy into your Project window
///         (e.g., into an "Assets/Prefabs" folder). This turns it into a Prefab.
///     *   Delete "Projectile" from the Hierarchy (we will instantiate it from the Prefab).
///
/// 6.  **Connect Projectile Prefab to Shooter:**
///     *   Select the "Player" GameObject in the Hierarchy.
///     *   Drag the "Projectile" Prefab from your Project window into the `Projectile Prefab` field
///         of the `PlayerShooter` component on "Player".
///
/// 7.  **Run the Scene:** Press Play. Click the Left Mouse Button to shoot projectiles.
///     Observe how game time pauses or slows down when a projectile hits the "TargetDummy".
///     Check the Console for debug messages.
/// </summary>
public class HitStopSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    // The static 'Instance' property makes the HitStopSystem globally accessible.
    // This is a common pattern for manager-like systems that should only exist once.
    public static HitStopSystem Instance { get; private set; }

    // --- Configuration Variables (Adjustable in Inspector) ---
    [Header("Hit Stop System Settings")]
    [Tooltip("The default Time.timeScale value to resume to after a hit stop (usually 1.0 for normal speed).")]
    [SerializeField] private float defaultResumeTimeScale = 1.0f;
    [Tooltip("The default speed at which Time.timeScale ramps back to 'defaultResumeTimeScale'.")]
    [SerializeField] private float defaultResumeSpeed = 5.0f;
    [Tooltip("The default Time.timeScale value during a hit stop (0 for full pause, small value for slow-motion).")]
    [SerializeField] private float defaultHitStopTimeScale = 0.0f;

    // --- Internal State Variables ---
    private Coroutine _hitStopCoroutine; // Stores a reference to the active hit stop coroutine.
    private bool _isHitStopping = false; // Flag to indicate if a hit stop is currently active.

    // --- Unity Lifecycle Method for Singleton Initialization ---
    private void Awake()
    {
        // Implement the Singleton pattern: Ensures only one instance of HitStopSystem exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("HitStopSystem: Duplicate instance found, destroying this one. " +
                             "Please ensure only one HitStopSystem exists in your scene.");
            Destroy(gameObject); // Destroy any duplicate instances.
        }
        else
        {
            Instance = this; // Set this instance as the singleton.
            // Optionally, uncomment the line below if you want the HitStopSystem to persist
            // across scene loads, useful for systems that manage global game state.
            // DontDestroyOnLoad(gameObject);
        }
    }

    // --- Public API: Trigger a Hit Stop ---
    /// <summary>
    /// Initiates a hit stop effect, pausing or slowing down game time for a specified duration.
    /// If a hit stop is already active, the current one will be stopped and a new one will begin.
    /// This prevents overlapping hit stops and ensures the most recent request takes precedence.
    /// </summary>
    /// <param name="duration">
    /// How long the game time will be held at the 'hitStopTimeScale' value (in real seconds).
    /// Must be greater than 0.
    /// </param>
    /// <param name="hitStopTimeScale">
    /// The Time.timeScale value to set during the hit stop.
    /// If null, 'defaultHitStopTimeScale' will be used (0 for full pause by default).
    /// </param>
    /// <param name="resumeSpeed">
    /// The speed at which Time.timeScale ramps back to 'defaultResumeTimeScale' after the duration.
    /// If null, 'defaultResumeSpeed' will be used (5.0f by default).
    /// </param>
    public void DoHitStop(float duration, float? hitStopTimeScale = null, float? resumeSpeed = null)
    {
        // Input validation: Ensure duration is valid to prevent errors or unintended behavior.
        if (duration <= 0)
        {
            Debug.LogWarning("HitStopSystem: Hit stop duration must be greater than 0. Ignoring request.");
            return;
        }

        // If a hit stop is currently active, stop it first.
        // This ensures that new hit stop requests always override previous ones,
        // preventing unexpected behavior from multiple concurrent time scale changes.
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            // Immediately restore time scale to default before starting a new one.
            // This prevents the game from being stuck in a paused state if the new hit stop
            // has a shorter duration or different properties.
            Time.timeScale = defaultResumeTimeScale;
            _isHitStopping = false;
            Debug.Log("HitStopSystem: Stopped previous hit stop to start a new one.");
        }

        // Start the new hit stop coroutine with the provided parameters,
        // or fall back to the default values if specific ones are not supplied.
        _hitStopCoroutine = StartCoroutine(HitStopEffectCoroutine(
            duration,
            hitStopTimeScale ?? defaultHitStopTimeScale, // Use provided value or default
            resumeSpeed ?? defaultResumeSpeed            // Use provided value or default
        ));
    }

    /// <summary>
    /// Returns true if a hit stop effect is currently active (i.e., game time is paused or slowed down
    /// due to a hit stop), false otherwise. Useful for other game systems that might need to know
    /// if a hit stop is in progress (e.g., preventing certain player inputs).
    /// </summary>
    public bool IsHitStopping()
    {
        return _isHitStopping;
    }

    // --- Private Coroutine Logic for Hit Stop Effect ---
    private IEnumerator HitStopEffectCoroutine(float duration, float targetHitStopTimeScale, float currentResumeSpeed)
    {
        _isHitStopping = true; // Set flag to indicate a hit stop is active.

        // Step 1: Instantaneously set Time.timeScale to the hit stop value.
        // This is the core of the hit stop. Game logic (like movement, animations, physics)
        // that uses `Time.deltaTime` will immediately pause or slow down.
        Time.timeScale = targetHitStopTimeScale;
        Debug.Log($"HitStopSystem: Time scale set to {Time.timeScale} for {duration} seconds (real time).");

        // Step 2: Wait for the specified duration.
        // `WaitForSecondsRealtime` is crucial here! It waits based on real-world time,
        // completely unaffected by the modified `Time.timeScale`. This ensures the hit stop
        // duration is consistent regardless of how much time scale was reduced.
        yield return new WaitForSecondsRealtime(duration);

        // Step 3: Smoothly ramp Time.timeScale back to the default resume value.
        // This provides a more natural and less jarring transition back to normal speed.
        float timeElapsed = 0f;
        float startResumeTimeScale = Time.timeScale; // The time scale we are resuming *from* (which is targetHitStopTimeScale)

        while (Time.timeScale < defaultResumeTimeScale)
        {
            // Use `Time.unscaledDeltaTime` for calculations within this loop.
            // This ensures the easing speed is consistent, even if Time.timeScale is still very low.
            timeElapsed += Time.unscaledDeltaTime * currentResumeSpeed;
            Time.timeScale = Mathf.Lerp(startResumeTimeScale, defaultResumeTimeScale, timeElapsed);

            // Prevent overshooting: Ensure Time.timeScale doesn't go beyond defaultResumeTimeScale,
            // which can happen if `currentResumeSpeed` is very high or frame rate is low.
            if (Time.timeScale > defaultResumeTimeScale)
            {
                Time.timeScale = defaultResumeTimeScale;
            }
            yield return null; // Wait for the next frame (using unscaled time implicitly for the yield)
        }

        // Step 4: Ensure Time.timeScale is exactly the defaultResumeTimeScale at the very end.
        // This guarantees a clean reset.
        Time.timeScale = defaultResumeTimeScale;
        Debug.Log($"HitStopSystem: Time scale resumed to {Time.timeScale}.");

        // Step 5: Reset internal state.
        _isHitStopping = false;      // Mark hit stop as no longer active.
        _hitStopCoroutine = null;    // Clear the coroutine reference.
    }
}


// --- EXAMPLE USAGE SCRIPTS ---
// These scripts demonstrate how to integrate the HitStopSystem into game logic.
// In a real project, you would typically place these in separate C# files.


/// <summary>
/// Example Script: ProjectileBehavior
/// Attach this script to a GameObject that acts as a projectile.
/// It moves forward and triggers a hit stop when it collides with an object
/// on the specified 'hitLayers'.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present for collision detection.
public class ProjectileBehavior : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Speed at which the projectile moves.")]
    public float speed = 20f;
    [Tooltip("Maximum lifetime of the projectile before it automatically destroys itself.")]
    public float lifetime = 3f;
    [Tooltip("Defines which layers this projectile can hit to trigger a hit stop.")]
    public LayerMask hitLayers;

    [Header("Hit Stop Settings for this Projectile")]
    [Tooltip("Duration of the hit stop (in real seconds) when this projectile hits something.")]
    public float hitStopDuration = 0.1f;
    [Tooltip("Time scale during the hit stop. Set to 0 for full pause, small value for slow-motion.")]
    public float hitStopTimeScale = 0.05f;
    [Tooltip("Speed at which time ramps back to normal after the hit stop.")]
    public float hitStopResumeSpeed = 8.0f;

    private Rigidbody _rb; // Reference to the projectile's Rigidbody.

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // For simple, non-physics-driven projectiles, it's often best to set Rigidbody as kinematic
        // and disable gravity, controlling movement manually via `MovePosition` or `transform.Translate`.
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    void Start()
    {
        // Destroy the projectile after its lifetime to prevent accumulating inactive objects.
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Move the projectile forward based on its `transform.forward` direction.
        // Using `Time.deltaTime` here makes its movement frame-rate independent.
        _rb.MovePosition(_rb.position + transform.forward * speed * Time.deltaTime);
    }

    /// <summary>
    /// Called when this projectile's trigger collider enters another collider.
    /// This is used for "soft" collisions where objects pass through each other
    /// but we still want to detect an event.
    /// </summary>
    /// <param name="other">The Collider that was entered.</param>
    void OnTriggerEnter(Collider other)
    {
        // Check if the collided object's layer is included in our `hitLayers` mask.
        // This ensures the hit stop only triggers on intended targets.
        if (((1 << other.gameObject.layer) & hitLayers) != 0)
        {
            Debug.Log($"Projectile hit {other.name} (Trigger)! Triggering Hit Stop.");

            // *** Core HitStopSystem Usage ***
            // This is where the hit stop is actually triggered.
            // We access the global singleton instance and call its `DoHitStop` method,
            // passing in parameters specific to this projectile's impact.
            if (HitStopSystem.Instance != null)
            {
                HitStopSystem.Instance.DoHitStop(hitStopDuration, hitStopTimeScale, hitStopResumeSpeed);
            }
            else
            {
                Debug.LogWarning("HitStopSystem.Instance is null. Make sure a HitStopSystem GameObject is in the scene.");
            }

            // After hitting, destroy the projectile.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when this projectile's non-trigger collider collides with another collider.
    /// Use this if your projectile is a solid object that should physically interact.
    /// (For this example, `OnTriggerEnter` is preferred as the projectile's collider is set to "Is Trigger").
    /// </summary>
    /// <param name="collision">Detailed information about the collision.</param>
    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Debug.Log($"Projectile hit {collision.gameObject.name} (Solid Collision)! Triggering Hit Stop.");
            if (HitStopSystem.Instance != null)
            {
                HitStopSystem.Instance.DoHitStop(hitStopDuration, hitStopTimeScale, hitStopResumeSpeed);
            }
            else
            {
                Debug.LogWarning("HitStopSystem.Instance is null. Make sure a HitStopSystem GameObject is in the scene.");
            }
            Destroy(gameObject);
        }
    }
}


/// <summary>
/// Example Script: PlayerShooter
/// Attach this script to a player GameObject. It handles player input to spawn projectiles.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Shooter Settings")]
    [Tooltip("Prefab of the projectile GameObject to instantiate.")]
    public GameObject projectilePrefab;
    [Tooltip("The Transform from which projectiles will be spawned.")]
    public Transform spawnPoint;
    [Tooltip("Time delay between consecutive shots (in unscaled seconds).")]
    public float fireRate = 0.5f;

    private float _nextFireTime = 0f; // Stores the earliest time the next shot can be fired.

    void Update()
    {
        // Check for Left Mouse Button click.
        // `Time.unscaledTime` is used here because we want the player to be able to shoot
        // even during a hit stop (when Time.timeScale is altered).
        if (Input.GetMouseButtonDown(0) && Time.unscaledTime >= _nextFireTime)
        {
            Shoot();
            _nextFireTime = Time.unscaledTime + fireRate; // Set cooldown based on unscaled time.
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("PlayerShooter: Projectile Prefab or Spawn Point not assigned. Cannot shoot.");
            return;
        }

        // Instantiate a new projectile at the spawn point's position and rotation.
        Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("PlayerShooter: Fired projectile.");
    }
}

/// <summary>
/// Example Script: TargetDummy
/// Attach this script to any GameObject that should act as a target for projectiles.
/// It primarily exists to have a distinct layer for collision detection.
/// </summary>
public class TargetDummy : MonoBehaviour
{
    // OnDrawGizmos is a Unity Editor-only function used for drawing visual aids.
    // Here, it draws a red wireframe cube around the GameObject in the editor view,
    // making it easier to identify the target.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Draw a wireframe cube matching the GameObject's bounds.
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
```