// Unity Design Pattern Example: TargetLockSystem
// This script demonstrates the TargetLockSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TargetLockSystem' design pattern, while not one of the canonical Gang of Four patterns, is a common and crucial architectural feature in many games (especially action RPGs, third-person shooters, and combat-focused games). It provides a structured way for a player or AI to focus on a specific enemy or object, enabling other game systems (like camera, combat, UI) to react accordingly.

This example demonstrates the pattern by:

1.  **Defining an `ITargetable` interface:** This ensures any object can be a target, promoting loose coupling.
2.  **Implementing a `Targetable` component:** A concrete example of how an enemy or interactable would become targetable.
3.  **Creating a `TargetLockSystem` MonoBehaviour:** The central manager responsible for finding, locking, unlocking, and cycling targets. It uses events to notify other systems.
4.  **Creating a `PlayerTargetingInput` MonoBehaviour:** Demonstrates how a player controller would interact with the `TargetLockSystem` using input and subscribing to its events.

---

Here are the complete C# scripts, ready to be dropped into a Unity project.

### 1. `ITargetable.cs`

This interface defines the contract for any object that can be locked onto. It ensures flexibility, as the `TargetLockSystem` doesn't need to know the specific type of the target, only that it implements this interface.

```csharp
using UnityEngine;

/// <summary>
/// Interface for objects that can be locked onto by the TargetLockSystem.
/// This promotes loose coupling, allowing any GameObject to become a target
/// without the TargetLockSystem needing to know its concrete type.
/// </summary>
public interface ITargetable
{
    /// <summary>
    /// Gets the primary Transform of the targetable object.
    /// </summary>
    /// <returns>The Transform of the target.</returns>
    Transform GetTargetTransform();

    /// <summary>
    /// Gets the specific world-space point on the target that the lock system should aim at.
    /// This could be the center, head, or a specific attachment point.
    /// </summary>
    /// <returns>The Vector3 world position for the lock point.</returns>
    Vector3 GetLockPoint();

    /// <summary>
    /// Determines if this targetable object is currently valid to be locked onto.
    /// (e.g., not dead, active in hierarchy, not hidden).
    /// </summary>
    /// <returns>True if the target is valid, false otherwise.</returns>
    bool IsValidTarget();

    /// <summary>
    /// Called by the TargetLockSystem when this object becomes the currently locked target.
    /// Use this for target-specific visual feedback (e.g., highlight).
    /// </summary>
    void OnLocked();

    /// <summary>
    /// Called by the TargetLockSystem when this object is no longer the currently locked target.
    /// Use this to revert any 'locked' state feedback.
    /// </summary>
    void OnUnlocked();
}
```

### 2. `Targetable.cs`

This is a concrete implementation of `ITargetable`. Attach this component to any GameObject that you want to be a potential target.

```csharp
using UnityEngine;

/// <summary>
/// A concrete MonoBehaviour implementation of the ITargetable interface.
/// Attach this script to any GameObject that should be discoverable and lockable
/// by the TargetLockSystem.
/// </summary>
[RequireComponent(typeof(Collider))] // Targets typically need a collider for detection via Physics.OverlapSphere
public class Targetable : MonoBehaviour, ITargetable
{
    [Tooltip("The offset from the GameObject's origin to determine the specific point to lock onto.")]
    public Vector3 lockOffset = Vector3.up * 1f; // Default to slightly above the base

    private Renderer _renderer; // Used for simple visual feedback (material color change)

    /// <summary>
    /// Initializes references.
    /// </summary>
    void Awake()
    {
        // Get the Renderer component if available, for visual feedback.
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogWarning($"Targetable on {gameObject.name} has no Renderer. Visual feedback will not work.", this);
        }
    }

    /// <summary>
    /// Returns the Transform of this GameObject.
    /// </summary>
    public Transform GetTargetTransform()
    {
        return transform;
    }

    /// <summary>
    /// Returns the world position for the lock point, based on the GameObject's position and the lock offset.
    /// </summary>
    public Vector3 GetLockPoint()
    {
        return transform.position + lockOffset;
    }

    /// <summary>
    /// Checks if this target is currently valid.
    /// Extend this with more complex logic (e.g., health > 0, not destroyed, specific state).
    /// </summary>
    public bool IsValidTarget()
    {
        // A simple validity check: the GameObject must be active in the hierarchy.
        // In a real game, you might add checks for health, specific states (e.g., 'not dead'), etc.
        return gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Called when this target is locked onto by the TargetLockSystem.
    /// Provides simple visual feedback by changing the material color to red.
    /// </summary>
    public void OnLocked()
    {
        if (_renderer != null)
        {
            _renderer.material.color = Color.red; // Visual feedback: turn red
            Debug.Log($"{gameObject.name} is now locked by the system!", this);
        }
    }

    /// <summary>
    /// Called when this target is unlocked from by the TargetLockSystem.
    /// Reverts simple visual feedback by changing the material color back to white.
    /// </summary>
    public void OnUnlocked()
    {
        if (_renderer != null)
        {
            _renderer.material.color = Color.white; // Revert to original color (assuming default white)
            Debug.Log($"{gameObject.name} is now unlocked by the system.", this);
        }
    }

    /// <summary>
    /// Draws a gizmo in the editor to visualize the lock point.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + lockOffset, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + lockOffset);
    }
}
```

### 3. `TargetLockSystem.cs`

This is the central manager for the Target Lock System. It handles discovering, setting, and clearing targets, and uses C# events to notify other parts of the game about target changes.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like OrderBy

/// <summary>
/// The core component of the TargetLockSystem design pattern.
/// This MonoBehaviour manages the currently locked target, provides methods for
/// finding and cycling targets, and exposes events for other systems to react to
/// target lock status changes.
/// </summary>
public class TargetLockSystem : MonoBehaviour
{
    [Header("Target Detection Settings")]
    [Tooltip("The radius around this GameObject within which to search for targetable objects.")]
    [SerializeField] private float searchRadius = 20f;
    [Tooltip("The LayerMask specifying which layers contain potential targetable objects.")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("The LayerMask specifying which layers can block the line of sight to a target.")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Visual Feedback Settings")]
    [Tooltip("A Prefab to instantiate and display at the locked target's lock point.")]
    [SerializeField] private GameObject lockIndicatorPrefab;
    private GameObject _currentLockIndicator; // The instantiated indicator object

    // --- Private Fields ---
    private ITargetable _currentTarget; // The currently locked target
    private Collider[] _hitColliders = new Collider[50]; // Pre-allocated array for Physics.OverlapSphereNonAlloc to reduce garbage collection

    // --- Events (Observer Pattern) ---
    // These static events allow any interested system to subscribe and react
    // to target lock state changes without direct dependencies on this class.
    // They are static to provide a global point of access, implying a single, global lock system.
    public static event Action<ITargetable> OnTargetLocked;      // Fired when a new target is successfully locked
    public static event Action<ITargetable> OnTargetUnlocked;    // Fired when the current target is unlocked (can be null if system becomes disabled or target invalid)
    public static event Action<ITargetable> OnTargetChanged;     // Fired when the locked target either changes or becomes null (more general notification)

    /// <summary>
    /// Public read-only property to access the currently locked target.
    /// </summary>
    public ITargetable CurrentTarget => _currentTarget;

    // --- Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Instantiates the lock indicator prefab.
    /// </summary>
    void Awake()
    {
        // Instantiate the lock indicator prefab if provided.
        // It will be hidden until a target is actually locked.
        if (lockIndicatorPrefab != null)
        {
            _currentLockIndicator = Instantiate(lockIndicatorPrefab);
            _currentLockIndicator.transform.SetParent(transform); // Parent to this system for organization
            _currentLockIndicator.SetActive(false); // Start hidden
        }
        else
        {
            Debug.LogWarning("Lock Indicator Prefab is not assigned in TargetLockSystem. No visual lock indicator will be shown.", this);
        }
    }

    /// <summary>
    /// Called every frame. Updates the lock indicator's position and checks target validity.
    /// </summary>
    void Update()
    {
        if (_currentTarget != null)
        {
            // If the current target is no longer valid (e.g., destroyed, dead), unlock it.
            if (!_currentTarget.IsValidTarget())
            {
                Debug.Log($"Current target '{_currentTarget.GetTargetTransform().name}' became invalid. Unlocking...", this);
                UnlockTarget();
                return; // Exit to avoid trying to update indicator for an invalid target
            }

            // Keep the lock indicator updated on the target's lock point.
            UpdateLockIndicatorPosition();
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// Ensures the current target is unlocked and cleans up the indicator.
    /// </summary>
    void OnDisable()
    {
        UnlockTarget(); // Ensure we unlock if the system is going offline
        if (_currentLockIndicator != null)
        {
            Destroy(_currentLockIndicator); // Clean up the instantiated indicator
            _currentLockIndicator = null;
        }
    }

    // --- Public Methods (Core Functionality) ---

    /// <summary>
    /// Attempts to lock onto a specific <see cref="ITargetable"/> object.
    /// </summary>
    /// <param name="target">The <see cref="ITargetable"/> object to lock onto.</param>
    /// <returns>True if the target was successfully locked, false otherwise (e.g., target is null or invalid).</returns>
    public bool LockOnTarget(ITargetable target)
    {
        // 1. Validate the incoming target.
        if (target == null || !target.IsValidTarget())
        {
            Debug.Log("Cannot lock on a null or invalid target.", this);
            return false;
        }

        // 2. If already locked onto this target, no action needed.
        if (_currentTarget == target)
        {
            // Debug.Log($"Already locked onto '{target.GetTargetTransform().name}'.", this);
            return true;
        }

        // 3. Unlock the previously locked target, if any.
        if (_currentTarget != null)
        {
            _currentTarget.OnUnlocked();          // Notify the old target
            OnTargetUnlocked?.Invoke(_currentTarget); // Fire event for systems interested in the old target being unlocked
        }

        // 4. Set the new target.
        _currentTarget = target;
        _currentTarget.OnLocked();             // Notify the new target it's locked
        OnTargetLocked?.Invoke(_currentTarget);  // Fire event for systems interested in a new target being locked
        OnTargetChanged?.Invoke(_currentTarget); // Fire general change event

        // 5. Update visual feedback (lock indicator).
        if (_currentLockIndicator != null)
        {
            _currentLockIndicator.SetActive(true);
            UpdateLockIndicatorPosition();
        }

        Debug.Log($"Successfully locked onto: '{_currentTarget.GetTargetTransform().name}'", this);
        return true;
    }

    /// <summary>
    /// Unlocks the current target, if one is locked.
    /// </summary>
    public void UnlockTarget()
    {
        if (_currentTarget != null)
        {
            // Notify the current target it's being unlocked.
            _currentTarget.OnUnlocked();
            OnTargetUnlocked?.Invoke(_currentTarget); // Fire event for the specific target being unlocked
            OnTargetChanged?.Invoke(null);           // Fire general change event (target is now null)

            Debug.Log($"Unlocked from: '{_currentTarget.GetTargetTransform().name}'", this);
            _currentTarget = null; // Clear the reference

            // Hide the visual lock indicator.
            if (_currentLockIndicator != null)
            {
                _currentLockIndicator.SetActive(false);
            }
        }
        // If _currentTarget is already null, do nothing.
    }

    /// <summary>
    /// Scans for the nearest valid target within the <see cref="searchRadius"/> and locks onto it.
    /// Prioritizes a new nearest target over the currently locked one if the new one is closer.
    /// </summary>
    /// <returns>True if a target was found and locked, false otherwise (e.g., no valid targets).</returns>
    public bool FindAndLockNearestTarget()
    {
        ITargetable nearestTarget = FindNearestTargetWithinRadius();

        if (nearestTarget != null)
        {
            // Lock onto the found nearest target. This will handle unlocking the previous one if different.
            return LockOnTarget(nearestTarget);
        }
        else
        {
            // No valid target found within radius, ensure we're unlocked.
            UnlockTarget();
            Debug.Log("No target found within search radius.", this);
            return false;
        }
    }

    /// <summary>
    /// Cycles through available targets within the <see cref="searchRadius"/>.
    /// The order is determined by distance from the TargetLockSystem's position.
    /// </summary>
    /// <param name="forward">If true, cycles to the next target in the sorted list; otherwise, cycles to the previous.</param>
    /// <returns>True if a new target was locked, false otherwise (e.g., no available targets).</returns>
    public bool CycleTargets(bool forward)
    {
        // Get all valid targets within range, ordered by distance for consistent cycling.
        List<ITargetable> availableTargets = GetAllValidTargetsWithinRadius()
                                            .OrderBy(t => Vector3.Distance(transform.position, t.GetTargetTransform().position))
                                            .ToList();

        if (availableTargets.Count == 0)
        {
            UnlockTarget();
            Debug.Log("No available targets to cycle through.", this);
            return false;
        }

        int currentIndex = -1;
        if (_currentTarget != null && availableTargets.Contains(_currentTarget))
        {
            // If the current target is still valid and in the list, find its index.
            currentIndex = availableTargets.IndexOf(_currentTarget);
        }

        int nextIndex;
        if (forward)
        {
            // Calculate next index, wrapping around to the beginning if at the end.
            nextIndex = (currentIndex + 1) % availableTargets.Count;
        }
        else
        {
            // Calculate previous index, wrapping around to the end if at the beginning.
            nextIndex = (currentIndex - 1 + availableTargets.Count) % availableTargets.Count;
        }

        // Lock onto the target at the calculated next index.
        return LockOnTarget(availableTargets[nextIndex]);
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Finds the single nearest valid target within the search radius that has line of sight.
    /// </summary>
    /// <returns>The nearest <see cref="ITargetable"/> object, or null if none found.</returns>
    private ITargetable FindNearestTargetWithinRadius()
    {
        float minDistanceSqr = float.MaxValue; // Squared distance for performance
        ITargetable nearestTarget = null;

        // Use Physics.OverlapSphereNonAlloc to find colliders without generating garbage.
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitColliders, targetLayers);

        for (int i = 0; i < numColliders; i++)
        {
            ITargetable target = _hitColliders[i].GetComponent<ITargetable>();
            if (target != null && target.IsValidTarget())
            {
                // Perform a line-of-sight check to ensure the target isn't obstructed.
                if (!Physics.Linecast(transform.position, target.GetLockPoint(), obstacleLayers))
                {
                    float distanceSqr = (transform.position - target.GetTargetTransform().position).sqrMagnitude;
                    if (distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                        nearestTarget = target;
                    }
                }
            }
        }
        return nearestTarget;
    }

    /// <summary>
    /// Retrieves a list of all valid targets within the search radius that have line of sight.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of all valid <see cref="ITargetable"/> objects found.</returns>
    private List<ITargetable> GetAllValidTargetsWithinRadius()
    {
        List<ITargetable> targets = new List<ITargetable>();
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitColliders, targetLayers);

        for (int i = 0; i < numColliders; i++)
        {
            ITargetable target = _hitColliders[i].GetComponent<ITargetable>();
            if (target != null && target.IsValidTarget())
            {
                // Check line of sight for all potential targets before adding them to the list.
                if (!Physics.Linecast(transform.position, target.GetLockPoint(), obstacleLayers))
                {
                    targets.Add(target);
                }
            }
        }
        return targets;
    }

    /// <summary>
    /// Updates the position and optionally rotation of the visual lock indicator.
    /// </summary>
    private void UpdateLockIndicatorPosition()
    {
        if (_currentLockIndicator != null && _currentTarget != null && _currentTarget.IsValidTarget())
        {
            _currentLockIndicator.transform.position = _currentTarget.GetLockPoint();
            // Optional: Make the indicator face the camera or player for a billboard effect.
            // _currentLockIndicator.transform.LookAt(Camera.main.transform.position);
            // _currentLockIndicator.transform.rotation = Quaternion.LookRotation(transform.position - _currentLockIndicator.transform.position);
        }
    }

    // --- Debugging and Editor Visuals ---

    /// <summary>
    /// Draws debug gizmos in the editor to visualize the search radius and the line to the current target.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw the search radius sphere.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        // Draw a line to the currently locked target's lock point.
        if (_currentTarget != null && _currentTarget.IsValidTarget())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _currentTarget.GetLockPoint());
            Gizmos.DrawWireSphere(_currentTarget.GetLockPoint(), 0.3f); // Highlight the lock point
        }
    }
}
```

### 4. `PlayerTargetingInput.cs`

This script demonstrates how a player controller (or any other entity) would interact with the `TargetLockSystem`. It handles user input and subscribes to the system's events to react to target changes.

```csharp
using UnityEngine;

/// <summary>
/// Demonstrates how a player controller would interact with the TargetLockSystem.
/// It takes player input to manage target locking and unlocking, and subscribes
/// to the TargetLockSystem's events to react to changes.
/// </summary>
public class PlayerTargetingInput : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the TargetLockSystem instance in the scene.")]
    [SerializeField] private TargetLockSystem targetLockSystem;

    [Header("Input Settings")]
    [Tooltip("Key to find and lock onto the nearest target within range.")]
    [SerializeField] private KeyCode lockNearestKey = KeyCode.Q;
    [Tooltip("Key to unlock the current target.")]
    [SerializeField] private KeyCode unlockKey = KeyCode.R;
    [Tooltip("Key to cycle to the next available target.")]
    [SerializeField] private KeyCode cycleNextKey = KeyCode.E;
    [Tooltip("Key to cycle to the previous available target.")]
    [SerializeField] private KeyCode cyclePrevKey = KeyCode.T;


    /// <summary>
    /// Verifies dependencies upon script awakening.
    /// </summary>
    void Awake()
    {
        if (targetLockSystem == null)
        {
            Debug.LogError("PlayerTargetingInput requires a TargetLockSystem reference to function!", this);
            enabled = false; // Disable this component if the essential reference is missing.
        }
    }

    /// <summary>
    /// Subscribes to TargetLockSystem events when this component is enabled.
    /// This is where the Observer pattern is applied: this script observes changes
    /// in the TargetLockSystem without direct knowledge of its internal workings.
    /// </summary>
    void OnEnable()
    {
        TargetLockSystem.OnTargetLocked += HandleTargetLocked;
        TargetLockSystem.OnTargetUnlocked += HandleTargetUnlocked;
        TargetLockSystem.OnTargetChanged += HandleTargetChanged; // A general event for any change
    }

    /// <summary>
    /// Unsubscribes from TargetLockSystem events when this component is disabled.
    /// This is crucial to prevent memory leaks and unexpected behavior if the observer
    /// (this script) is destroyed or disabled while the observed (TargetLockSystem) persists.
    /// </summary>
    void OnDisable()
    {
        TargetLockSystem.OnTargetLocked -= HandleTargetLocked;
        TargetLockSystem.OnTargetUnlocked -= HandleTargetUnlocked;
        TargetLockSystem.OnTargetChanged -= HandleTargetChanged;
    }

    /// <summary>
    /// Handles player input for managing the target lock.
    /// </summary>
    void Update()
    {
        // --- Input Handling ---

        // Find and lock nearest target
        if (Input.GetKeyDown(lockNearestKey))
        {
            Debug.Log("Player Input: Attempting to find and lock nearest target...");
            targetLockSystem.FindAndLockNearestTarget();
        }

        // Unlock current target
        if (Input.GetKeyDown(unlockKey))
        {
            Debug.Log("Player Input: Attempting to unlock current target...");
            targetLockSystem.UnlockTarget();
        }

        // Cycle targets (forward)
        if (Input.GetKeyDown(cycleNextKey))
        {
            Debug.Log("Player Input: Attempting to cycle to next target...");
            targetLockSystem.CycleTargets(true); // Cycle forward
        }

        // Cycle targets (backward)
        if (Input.GetKeyDown(cyclePrevKey))
        {
            Debug.Log("Player Input: Attempting to cycle to previous target...");
            targetLockSystem.CycleTargets(false); // Cycle backward
        }

        // --- Example Usage of Current Target ---
        // This demonstrates how a player controller might use the current locked target
        // for actions like aiming, attacking, or displaying UI information.
        if (targetLockSystem.CurrentTarget != null)
        {
            // For demonstration: log the target's name periodically.
            // In a real game, this might orient the player character, aim a weapon,
            // or update a UI element to show target health/name.
            if (Time.frameCount % 120 == 0) // Roughly twice per second at 60 FPS
            {
                Debug.Log($"Player's current locked target: {targetLockSystem.CurrentTarget.GetTargetTransform().name} at {targetLockSystem.CurrentTarget.GetLockPoint()}", this);
            }
        }
    }

    // --- Event Handlers for TargetLockSystem Events ---

    /// <summary>
    /// Handler for when a target is successfully locked.
    /// </summary>
    /// <param name="target">The newly locked target.</param>
    private void HandleTargetLocked(ITargetable target)
    {
        Debug.Log($"PlayerTargetingInput: Successfully locked onto {target.GetTargetTransform().name}! (Event)", this);
        // Here, you would typically trigger camera adjustments, UI updates,
        // activate targeting reticles, or inform combat systems.
    }

    /// <summary>
    /// Handler for when a target is unlocked.
    /// </summary>
    /// <param name="target">The target that was just unlocked (can be null if it became invalid).</param>
    private void HandleTargetUnlocked(ITargetable target)
    {
        if (target != null)
        {
            Debug.Log($"PlayerTargetingInput: Unlocked from {target.GetTargetTransform().name}. (Event)", this);
        }
        else
        {
            Debug.Log("PlayerTargetingInput: Target unlocked (was null or became invalid). (Event)", this);
        }
        // Here, you would typically reset camera, hide target UI, or deactivate targeting modes.
    }

    /// <summary>
    /// General handler for any change in the locked target (either a new target, or no target).
    /// This can be used as a single point to update UI or camera, simplifying logic.
    /// </summary>
    /// <param name="newTarget">The new target (can be null if unlocked).</param>
    private void HandleTargetChanged(ITargetable newTarget)
    {
        if (newTarget != null)
        {
            Debug.Log($"PlayerTargetingInput: Target changed to '{newTarget.GetTargetTransform().name}'. (General Change Event)", this);
        }
        else
        {
            Debug.Log("PlayerTargetingInput: Target changed to null. (General Change Event)", this);
        }
        // This is a good place for central logic that needs to run whenever the target status changes,
        // regardless of whether it was a lock or unlock.
    }
}
```

---

### How to Set Up in Unity

1.  **Create C# Scripts:**
    *   Create `ITargetable.cs` and paste the `ITargetable` interface code.
    *   Create `Targetable.cs` and paste the `Targetable` class code.
    *   Create `TargetLockSystem.cs` and paste the `TargetLockSystem` class code.
    *   Create `PlayerTargetingInput.cs` and paste the `PlayerTargetingInput` class code.

2.  **Scene Setup:**

    *   **Create the Player:**
        *   Create an empty GameObject, name it "Player".
        *   Add the `PlayerTargetingInput` script to it.
        *   (Optional but recommended for visual player position) Add a simple 3D object (e.g., Capsule) as a child to "Player".

    *   **Create the TargetLockSystem Manager:**
        *   Create another empty GameObject, name it "TargetLockSystem_Manager".
        *   Add the `TargetLockSystem` script to this GameObject.
        *   Drag this "TargetLockSystem_Manager" GameObject into the `Target Lock System` slot of the `PlayerTargetingInput` script on your "Player" GameObject in the Inspector.

    *   **Configure TargetLockSystem_Manager:**
        *   In the Inspector for "TargetLockSystem_Manager":
            *   Set `Search Radius` (e.g., `20`).
            *   Set `Target Layers`: You'll need to create a new layer for targets first (see step 3).
            *   Set `Obstacle Layers`: Choose layers that should block line of sight (e.g., `Default`, `Environment`).
            *   **Lock Indicator Prefab (Optional):** Create a simple 3D Sphere (GameObject -> 3D Object -> Sphere), scale it down (e.g., 0.2, 0.2, 0.2), and drag it from the Hierarchy to your Project window to make it a Prefab. Then, drag this Prefab into the `Lock Indicator Prefab` slot.

    *   **Create Targetable Objects (Enemies/Interactables):**
        *   Create several 3D objects (e.g., Cubes or Spheres) in various locations around your player. Name them "Enemy1", "Enemy2", etc.
        *   For each enemy:
            *   Add the `Targetable` script to it.
            *   Ensure it has a `Collider` component (e.g., Box Collider for a Cube).
            *   Assign its GameObject to the "Targets" layer (created in step 3).
            *   (Optional) Give them distinct materials for better visual feedback when locked.

3.  **Layers Setup:**
    *   Go to `Edit -> Project Settings -> Tags and Layers`.
    *   Under "Layers", find an empty `User Layer` slot (e.g., `User Layer 8`) and name it "Targets".
    *   Now, back in your scene:
        *   Select all your "Enemy" GameObjects and set their Layer dropdown (top right of Inspector) to "Targets".
        *   Select "TargetLockSystem_Manager" and in its `TargetLockSystem` component, set the `Target Layers` field to "Targets".
        *   If you have environment objects, ensure they are on `Default` or other layers you include in `Obstacle Layers`.

4.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Use the configured keys (default: `Q` to find/lock nearest, `R` to unlock, `E` to cycle next, `T` to cycle previous).
    *   Observe the Debug logs, the Gizmos in the Scene view (if enabled), and the lock indicator on the target.
    *   Move your "Player" around to test target finding from different positions.
    *   Place an obstacle (e.g., a large Cube on the `Default` layer) between the player and a target to see the line-of-sight blocking in action.

This setup provides a robust and educational example of the TargetLockSystem pattern, ready for expansion and integration into a full Unity game.