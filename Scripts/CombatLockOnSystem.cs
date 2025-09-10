// Unity Design Pattern Example: CombatLockOnSystem
// This script demonstrates the CombatLockOnSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates a **Combat Lock-On System** using several design patterns and best practices.

**Design Patterns Illustrated:**

1.  **Facade:** The `CombatLockOnSystem` class acts as a facade, providing a simple interface (`ToggleLockOn`, `CycleTarget`, `UnlockTarget`) for other game systems to interact with target lock-on functionality. It hides the complexity of target discovery, selection, cycling, and state management.
2.  **Observer (Events):** The system uses C# `Action` events (`OnTargetLockedOn`, `OnTargetUnlocked`) to notify other independent systems (e.g., Camera Controller, Player Combat Controller, UI Manager) when the lock-on state changes or a new target is selected. This promotes loose coupling, as these systems don't need direct references to the `CombatLockOnSystem` or its internal logic, only to subscribe to its events.
3.  **Iterator (Implicit):** The `CycleTarget` method, by maintaining an internal index (`_currentPotentialTargetIndex`) and iterating through the `_potentialTargets` list, implicitly uses an iterator-like mechanism to move through available targets.
4.  **State (Simple Implicit):** The `_isLockedOn` boolean variable and the conditional logic based on it (`if (_isLockedOn) ... else ...`) represent a very simple form of state management. For a more complex system, one might abstract this into a full State pattern (e.g., `LockedOnState`, `UnlockedState` classes). For this example, the simpler boolean suffices to demonstrate the core concept.

---

## `CombatLockOnSystem.cs`

This script is designed to be attached to your player character.

```csharp
using UnityEngine;
using System; // For Action delegate
using System.Collections; // For IEnumerator and lists
using System.Collections.Generic; // For List<GameObject>
using System.Linq; // For LINQ queries like OrderBy

/// <summary>
/// CombatLockOnSystem: Manages target acquisition, lock-on, and cycling for a player character.
/// This system acts as a Facade, abstracting the complexities of target finding and state.
/// It uses Events (Observer Pattern) to notify other systems about lock-on changes.
/// </summary>
public class CombatLockOnSystem : MonoBehaviour
{
    // === REFERENCES ===
    [Header("Core References")]
    [Tooltip("The player's transform. This script should ideally be on the player.")]
    [SerializeField] private Transform _playerTransform;
    [Tooltip("Optional: A UI element (e.g., an Image) to indicate the locked-on target.")]
    [SerializeField] private GameObject _lockOnIndicatorPrefab;
    private GameObject _currentIndicatorInstance;

    // === LOCK-ON SETTINGS ===
    [Header("Lock-On Settings")]
    [Tooltip("The radius around the player within which to search for potential targets.")]
    [SerializeField] private float _lockOnSearchRadius = 20f;
    [Tooltip("The maximum distance to maintain a lock-on. If the target exceeds this, lock-on is lost.")]
    [SerializeField] private float _maxLockOnDistance = 25f;
    [Tooltip("The angle (in degrees) in front of the player within which targets are considered viable.")]
    [Range(0, 180)]
    [SerializeField] private float _lockOnViewAngle = 90f;
    [Tooltip("Layer mask for GameObjects that can be targeted (e.g., 'Enemies').")]
    [SerializeField] private LayerMask _targetLayer;
    [Tooltip("How fast the player rotates to face the locked-on target.")]
    [SerializeField] private float _playerRotationSpeed = 10f;
    [Tooltip("Time in seconds between re-evaluating potential targets when unlocked. Set to 0 to evaluate every frame.")]
    [SerializeField] private float _potentialTargetRefreshRate = 0.5f;

    // === INPUT SETTINGS ===
    [Header("Input Settings")]
    [Tooltip("Key to toggle lock-on (initial lock or unlock).")]
    [SerializeField] private KeyCode _toggleLockOnKey = KeyCode.Tab;
    [Tooltip("Key to cycle to the next target (right).")]
    [SerializeField] private KeyCode _cycleTargetRightKey = KeyCode.E;
    [Tooltip("Key to cycle to the previous target (left).")]
    [SerializeField] private KeyCode _cycleTargetLeftKey = KeyCode.Q;

    // === INTERNAL STATE ===
    private bool _isLockedOn = false;
    public bool IsLockedOn => _isLockedOn; // Public getter for other systems
    private GameObject _currentTarget;
    public GameObject CurrentTarget => _currentTarget; // Public getter for other systems

    private List<GameObject> _potentialTargets = new List<GameObject>();
    private int _currentPotentialTargetIndex = -1; // -1 means no target selected from list
    private Coroutine _refreshPotentialTargetsCoroutine;

    // === EVENTS (Observer Pattern) ===
    /// <summary>
    /// Event fired when a new target is successfully locked onto.
    /// Subscribers receive the GameObject of the locked target.
    /// </summary>
    public static event Action<GameObject> OnTargetLockedOn;
    /// <summary>
    /// Event fired when the current target is unlocked or lost.
    /// Subscribers receive the GameObject that was previously locked (can be null if lost unexpectedly).
    /// </summary>
    public static event Action<GameObject> OnTargetUnlocked;

    // ====================================================================================
    // MONOBEHAVIOUR LIFECYCLE
    // ====================================================================================

    private void Awake()
    {
        // Ensure player transform is set. If not, try to get it from this GameObject.
        if (_playerTransform == null)
        {
            _playerTransform = transform;
        }

        // Pre-instantiate the indicator if it exists, but keep it hidden.
        if (_lockOnIndicatorPrefab != null)
        {
            _currentIndicatorInstance = Instantiate(_lockOnIndicatorPrefab);
            _currentIndicatorInstance.SetActive(false);
        }
    }

    private void Start()
    {
        // Start the routine to periodically find potential targets.
        if (_potentialTargetRefreshRate > 0)
        {
            _refreshPotentialTargetsCoroutine = StartCoroutine(RefreshPotentialTargetsRoutine());
        }
        else // If refresh rate is 0, refresh every frame.
        {
            FindPotentialTargets(); // Initial refresh
        }
    }

    private void Update()
    {
        HandleInput();

        if (_isLockedOn)
        {
            MaintainLockOn();
        }
        else // If not locked on, we might need to find targets more frequently
        {
            // If refresh rate is 0, ensure targets are found every frame
            if (_potentialTargetRefreshRate <= 0)
            {
                FindPotentialTargets();
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up coroutine if running
        if (_refreshPotentialTargetsCoroutine != null)
        {
            StopCoroutine(_refreshPotentialTargetsCoroutine);
        }

        // Destroy the indicator if it exists
        if (_currentIndicatorInstance != null)
        {
            Destroy(_currentIndicatorInstance);
        }
    }

    // Draw Gizmos in the editor for visualization
    private void OnDrawGizmosSelected()
    {
        if (_playerTransform == null) return;

        // Draw lock-on search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_playerTransform.position, _lockOnSearchRadius);

        // Draw lock-on view angle
        Gizmos.color = Color.cyan;
        Vector3 forward = _playerTransform.forward;
        Vector3 left = Quaternion.Euler(0, -_lockOnViewAngle / 2, 0) * forward;
        Vector3 right = Quaternion.Euler(0, _lockOnViewAngle / 2, 0) * forward;
        Gizmos.DrawRay(_playerTransform.position, left * _lockOnSearchRadius);
        Gizmos.DrawRay(_playerTransform.position, right * _lockOnSearchRadius);
        Gizmos.DrawLine(_playerTransform.position + left * _lockOnSearchRadius, _playerTransform.position + right * _lockOnSearchRadius);
    }

    // ====================================================================================
    // CORE FUNCTIONALITY
    // ====================================================================================

    /// <summary>
    /// Handles player input for lock-on and target cycling.
    /// </summary>
    private void HandleInput()
    {
        // Toggle Lock-On / Unlock
        if (Input.GetKeyDown(_toggleLockOnKey))
        {
            ToggleLockOn();
        }

        // Cycle Targets (only if locked on)
        if (_isLockedOn)
        {
            if (Input.GetKeyDown(_cycleTargetRightKey))
            {
                CycleTarget(1); // Cycle right
            }
            if (Input.GetKeyDown(_cycleTargetLeftKey))
            {
                CycleTarget(-1); // Cycle left
            }
        }
    }

    /// <summary>
    /// Toggles the lock-on state. If currently locked, unlocks.
    /// If not locked, attempts to find and lock onto a target.
    /// This method is the primary Facade entry point for external systems to initiate lock-on.
    /// </summary>
    public void ToggleLockOn()
    {
        if (_isLockedOn)
        {
            UnlockTarget();
        }
        else
        {
            // First, refresh potential targets to get the most up-to-date list
            FindPotentialTargets();

            // Try to find the best initial target
            GameObject initialTarget = GetClosestTarget();
            if (initialTarget != null)
            {
                LockOnTarget(initialTarget);
            }
            else
            {
                Debug.Log("CombatLockOnSystem: No valid targets found to lock on to.");
            }
        }
    }

    /// <summary>
    /// Locks onto the specified target.
    /// </summary>
    /// <param name="target">The GameObject to lock onto.</param>
    private void LockOnTarget(GameObject target)
    {
        if (target == null) return;

        _currentTarget = target;
        _isLockedOn = true;

        // Update the internal index to match the current target in the potential targets list.
        _currentPotentialTargetIndex = _potentialTargets.IndexOf(target);

        Debug.Log($"CombatLockOnSystem: Locked onto {target.name}");

        // Observer Pattern: Notify subscribers that a target has been locked.
        OnTargetLockedOn?.Invoke(_currentTarget);

        UpdateTargetIndicator();
    }

    /// <summary>
    /// Unlocks from the current target.
    /// This method is another primary Facade entry point.
    /// </summary>
    public void UnlockTarget()
    {
        if (!_isLockedOn) return;

        GameObject previouslyLockedTarget = _currentTarget; // Store for event
        _isLockedOn = false;
        _currentTarget = null;
        _currentPotentialTargetIndex = -1;

        Debug.Log($"CombatLockOnSystem: Unlocked from {previouslyLockedTarget?.name ?? "target"}.");

        // Observer Pattern: Notify subscribers that the target has been unlocked.
        OnTargetUnlocked?.Invoke(previouslyLockedTarget);

        HideTargetIndicator();
    }

    /// <summary>
    /// Checks if the current target is still valid and within range/view.
    /// If not, unlocks automatically. Also handles player rotation towards target.
    /// </summary>
    private void MaintainLockOn()
    {
        if (_currentTarget == null || !_currentTarget.activeInHierarchy || !IsTargetValid(_currentTarget))
        {
            Debug.Log("CombatLockOnSystem: Current target lost (null, inactive, out of range, or out of view).");
            UnlockTarget();
            return;
        }

        // Player rotation towards target
        FaceTarget(_currentTarget.transform.position);

        // Update indicator position
        UpdateTargetIndicator();
    }

    /// <summary>
    /// Finds all potential targets within the search radius and view angle,
    /// and updates the _potentialTargets list.
    /// </summary>
    private void FindPotentialTargets()
    {
        // Get all colliders in range on the target layer
        Collider[] hitColliders = Physics.OverlapSphere(_playerTransform.position, _lockOnSearchRadius, _targetLayer);

        _potentialTargets.Clear();

        foreach (var hitCollider in hitColliders)
        {
            // Ensure we don't target ourselves or already-dead targets
            if (hitCollider.gameObject == _playerTransform.gameObject) continue;
            if (!hitCollider.gameObject.activeInHierarchy) continue;

            // Check if target is within the view angle and has line of sight
            if (IsTargetInViewAngle(hitCollider.transform) && HasLineOfSight(hitCollider.transform))
            {
                _potentialTargets.Add(hitCollider.gameObject);
            }
        }

        // Sort potential targets by distance to the player for consistent cycling
        _potentialTargets = _potentialTargets
                            .OrderBy(target => Vector3.Distance(_playerTransform.position, target.transform.position))
                            .ToList();

        // If currently locked, ensure the current target is still in the list and update index
        if (_isLockedOn && _currentTarget != null)
        {
            _currentPotentialTargetIndex = _potentialTargets.IndexOf(_currentTarget);
            if (_currentPotentialTargetIndex == -1) // Current target is no longer viable
            {
                UnlockTarget(); // Unlock if current target vanished or moved out of range/view
            }
        }
        else if (!_isLockedOn && _potentialTargets.Count > 0)
        {
            _currentPotentialTargetIndex = 0; // If not locked, pre-select the first target
        }
        else
        {
            _currentPotentialTargetIndex = -1; // No potential targets
        }
    }

    /// <summary>
    /// Coroutine to periodically refresh the list of potential targets.
    /// </summary>
    private IEnumerator RefreshPotentialTargetsRoutine()
    {
        while (true)
        {
            FindPotentialTargets();
            yield return new WaitForSeconds(_potentialTargetRefreshRate);
        }
    }

    /// <summary>
    /// Gets the best initial target from the _potentialTargets list.
    /// Prioritizes the target closest to the center of the screen or closest overall.
    /// </summary>
    /// <returns>The most suitable GameObject to lock onto, or null if none found.</returns>
    private GameObject GetClosestTarget()
    {
        // Ensure _potentialTargets is up-to-date
        FindPotentialTargets();

        if (_potentialTargets.Count == 0)
        {
            return null;
        }

        // For simplicity, we'll just pick the first one from the sorted list (closest).
        // For a more advanced system, you might consider screen-space position to find the most central target.
        return _potentialTargets.FirstOrDefault();
    }

    /// <summary>
    /// Cycles through the _potentialTargets list.
    /// Implicitly uses an Iterator-like pattern to move through available targets.
    /// </summary>
    /// <param name="direction">1 for next target, -1 for previous target.</param>
    public void CycleTarget(int direction)
    {
        if (!_isLockedOn || _potentialTargets.Count <= 1)
        {
            return; // Can't cycle if not locked or only one/no potential targets
        }

        // Ensure current target is still valid and in the list before cycling
        if (_currentTarget != null && _potentialTargets.Contains(_currentTarget))
        {
            _currentPotentialTargetIndex = _potentialTargets.IndexOf(_currentTarget);
        }
        else
        {
            // If current target is invalid or not in list, find a new starting point
            FindPotentialTargets(); // Re-populate and sort
            if (_potentialTargets.Count == 0)
            {
                UnlockTarget();
                return;
            }
            _currentPotentialTargetIndex = 0; // Start from the beginning if the old target is gone
            LockOnTarget(_potentialTargets[_currentPotentialTargetIndex]);
            return;
        }

        int newIndex = _currentPotentialTargetIndex + direction;

        // Wrap around the list
        if (newIndex >= _potentialTargets.Count)
        {
            newIndex = 0;
        }
        else if (newIndex < 0)
        {
            newIndex = _potentialTargets.Count - 1;
        }

        // Ensure the new target is valid before locking onto it
        if (_potentialTargets[newIndex] != null && IsTargetValid(_potentialTargets[newIndex]))
        {
            LockOnTarget(_potentialTargets[newIndex]);
        }
        else
        {
            // If the next target in sequence is invalid, remove it and try again
            Debug.Log($"CombatLockOnSystem: Skipped invalid target at index {newIndex}. Re-evaluating.");
            _potentialTargets.RemoveAt(newIndex);
            // After removing, we might need to adjust newIndex or simply re-cycle
            // For simplicity, we'll just call CycleTarget again, but with caution to prevent infinite loops if all targets are invalid.
            // A more robust solution would be to re-run FindPotentialTargets and pick from the updated list.
            FindPotentialTargets(); // Re-populate and sort after removing
            if (_potentialTargets.Count > 0)
            {
                // If the list is now empty or only has the current target, don't try to cycle
                if (_potentialTargets.Count == 1 && _potentialTargets[0] == _currentTarget) return;

                // Try to find the new index of the current target
                int currentTargetNewIndex = _potentialTargets.IndexOf(_currentTarget);
                if (currentTargetNewIndex != -1)
                {
                    _currentPotentialTargetIndex = currentTargetNewIndex; // Update the index
                    CycleTarget(direction); // Recurse to find a valid one
                }
                else
                {
                    // Current target itself is gone, pick the first valid one if available
                    UnlockTarget();
                    ToggleLockOn(); // Try to lock onto a new target
                }
            }
            else
            {
                UnlockTarget(); // No more valid targets
            }
        }
    }

    /// <summary>
    /// Rotates the player character to face the specified target position.
    /// </summary>
    /// <param name="targetPosition">The world position to face.</param>
    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - _playerTransform.position).normalized;
        if (direction == Vector3.zero) return; // Avoid error if target is at player position

        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        _playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, lookRotation, Time.deltaTime * _playerRotationSpeed);
    }

    // ====================================================================================
    // TARGET VALIDATION HELPERS
    // ====================================================================================

    /// <summary>
    /// Checks if a target is valid for lock-on.
    /// Considers distance, active state, view angle, and line of sight.
    /// </summary>
    /// <param name="target">The GameObject to validate.</param>
    /// <returns>True if the target is valid, false otherwise.</returns>
    private bool IsTargetValid(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return false;
        }

        // Check distance
        if (Vector3.Distance(_playerTransform.position, target.transform.position) > _maxLockOnDistance)
        {
            return false;
        }

        // Check view angle and line of sight
        return IsTargetInViewAngle(target.transform) && HasLineOfSight(target.transform);
    }

    /// <summary>
    /// Checks if a target is within the player's view angle.
    /// </summary>
    /// <param name="target">The target's transform.</param>
    /// <returns>True if within view angle, false otherwise.</returns>
    private bool IsTargetInViewAngle(Transform target)
    {
        Vector3 directionToTarget = (target.position - _playerTransform.position).normalized;
        return Vector3.Angle(_playerTransform.forward, directionToTarget) < _lockOnViewAngle / 2;
    }

    /// <summary>
    /// Checks for line of sight to the target using a raycast.
    /// Ignores the player's own collider for the raycast.
    /// </summary>
    /// <param name="target">The target's transform.</param>
    /// <returns>True if there's an unobstructed line of sight, false otherwise.</returns>
    private bool HasLineOfSight(Transform target)
    {
        RaycastHit hit;
        Vector3 origin = _playerTransform.position + Vector3.up * 0.5f; // Slightly above ground to avoid hitting player's feet
        Vector3 direction = (target.position + Vector3.up * 0.5f - origin).normalized; // Target's center

        // We need to ignore the player's own collider in the raycast.
        // Option 1: Use a specific layer for obstacles that block LOS, and exclude player layer.
        // Option 2: Add 'this' collider to a temporary ignore list before raycast.
        // For simplicity, we'll cast to the target layer and exclude 'this' directly.
        // It's crucial that targets have colliders on the _targetLayer.

        // To make sure raycast doesn't hit player collider:
        // Create a temporary layer mask that includes everything EXCEPT the player's layer
        int layerMask = ~LayerMask.GetMask("Player"); // Assuming player is on "Player" layer

        if (Physics.Raycast(origin, direction, out hit, _maxLockOnDistance, layerMask))
        {
            // If the ray hits the target, we have line of sight
            return hit.collider.gameObject == target.gameObject;
        }
        return false; // Ray didn't hit anything or hit something else
    }

    // ====================================================================================
    // UI INDICATOR
    // ====================================================================================

    /// <summary>
    /// Updates the position and visibility of the lock-on indicator.
    /// </summary>
    private void UpdateTargetIndicator()
    {
        if (_currentIndicatorInstance == null || _currentTarget == null)
        {
            HideTargetIndicator();
            return;
        }

        // Set the indicator's parent to null or a UI Canvas that is set to World Space
        // or ensure it's positioned correctly in screen space.
        // For simplicity, let's assume a world-space Canvas or an object that follows.
        _currentIndicatorInstance.transform.position = _currentTarget.transform.position + Vector3.up * 1.5f; // Offset above target
        _currentIndicatorInstance.SetActive(true);

        // Optional: Make the indicator always face the camera if it's a 3D object
        if (Camera.main != null)
        {
            _currentIndicatorInstance.transform.LookAt(_currentIndicatorInstance.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                                      Camera.main.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// Hides the lock-on indicator.
    /// </summary>
    private void HideTargetIndicator()
    {
        if (_currentIndicatorInstance != null)
        {
            _currentIndicatorInstance.SetActive(false);
        }
    }
}
```

---

## Example Usage: How to integrate and use the `CombatLockOnSystem`

Here are examples of how other scripts (like a `PlayerCamera`, `PlayerCombat`, or `UIManager`) would interact with the `CombatLockOnSystem` using the **Observer Pattern**.

### 1. `PlayerCamera.cs` (Example Subscriber)

This script would adjust the camera to focus on the locked target.

```csharp
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _followSpeed = 5f;
    [SerializeField] private float _lookAtSpeed = 10f;
    [SerializeField] private Vector3 _offset = new Vector3(0, 3, -7); // Camera offset from player

    private Transform _lockedTargetTransform;

    void OnEnable()
    {
        // Observer Pattern: Subscribe to lock-on events
        CombatLockOnSystem.OnTargetLockedOn += HandleTargetLockedOn;
        CombatLockOnSystem.OnTargetUnlocked += HandleTargetUnlocked;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and unexpected behavior
        CombatLockOnSystem.OnTargetLockedOn -= HandleTargetLockedOn;
        CombatLockOnSystem.OnTargetUnlocked -= HandleTargetUnlocked;
    }

    void LateUpdate()
    {
        // Always follow the player
        Vector3 desiredPosition = _playerTransform.position + _playerTransform.TransformDirection(_offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, _followSpeed * Time.deltaTime);

        // If locked on, also look at the target (or a point between player and target)
        if (_lockedTargetTransform != null)
        {
            Vector3 lookAtPoint = (_playerTransform.position + _lockedTargetTransform.position) / 2f;
            lookAtPoint.y = _playerTransform.position.y + 1.5f; // Adjust height to look at player/target mid-body
            Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _lookAtSpeed * Time.deltaTime);
        }
        else
        {
            // If not locked, simply look at the player's forward direction (or an existing camera logic)
            Quaternion lookRotation = Quaternion.LookRotation(_playerTransform.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _lookAtSpeed * Time.deltaTime);
        }
    }

    private void HandleTargetLockedOn(GameObject target)
    {
        Debug.Log($"Camera received lock-on notification for {target.name}");
        _lockedTargetTransform = target.transform;
    }

    private void HandleTargetUnlocked(GameObject target)
    {
        Debug.Log($"Camera received unlock notification for {target?.name ?? "a target"}");
        _lockedTargetTransform = null;
    }
}
```

### 2. `PlayerCombat.cs` (Example Subscriber)

This script might enable specific combat abilities when a target is locked.

```csharp
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private GameObject _currentCombatTarget;

    void OnEnable()
    {
        // Observer Pattern: Subscribe to lock-on events
        CombatLockOnSystem.OnTargetLockedOn += HandleTargetLockedOn;
        CombatLockOnSystem.OnTargetUnlocked += HandleTargetUnlocked;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and unexpected behavior
        CombatLockOnSystem.OnTargetLockedOn -= HandleTargetLockedOn;
        CombatLockOnSystem.OnTargetUnlocked -= HandleTargetUnlocked;
    }

    void Update()
    {
        // Example: Only allow ranged attack if a target is locked
        if (_currentCombatTarget != null && Input.GetMouseButtonDown(0)) // Left mouse button
        {
            PerformRangedAttack(_currentCombatTarget);
        }
        else if (_currentCombatTarget == null && Input.GetMouseButtonDown(0))
        {
            // Maybe perform a different action if no target locked
            Debug.Log("No target locked, performing wide-area attack or missing.");
        }
    }

    private void HandleTargetLockedOn(GameObject target)
    {
        Debug.Log($"PlayerCombat received lock-on notification for {target.name}");
        _currentCombatTarget = target;
        // Optionally enable a UI element or highlight for combat abilities
    }

    private void HandleTargetUnlocked(GameObject target)
    {
        Debug.Log($"PlayerCombat received unlock notification for {target?.name ?? "a target"}");
        _currentCombatTarget = null;
        // Optionally disable combat ability UI
    }

    private void PerformRangedAttack(GameObject target)
    {
        Debug.Log($"Player attacks {target.name}!");
        // Implement actual attack logic here (e.g., instantiate projectile, deal damage)
    }
}
```

---

## Unity Setup Instructions:

1.  **Create your Player:**
    *   Create an empty GameObject (e.g., "Player").
    *   Add a `CharacterController` or `Rigidbody` and `Collider` to it.
    *   Attach the `CombatLockOnSystem.cs` script to this "Player" GameObject.
    *   Drag the "Player" GameObject's `Transform` component into the `_Player Transform` field of the `CombatLockOnSystem` script in the Inspector.

2.  **Create Enemy Targets:**
    *   Create several 3D objects (e.g., Cubes or Capsules) and rename them "Enemy_1", "Enemy_2", etc.
    *   **Crucially**, create a new Layer in Unity called "Enemy" (or whatever you set for `_Target Layer` in the script). Assign all enemy GameObjects to this "Enemy" layer.
    *   Add a `Collider` component (e.g., `BoxCollider`, `CapsuleCollider`) to each enemy GameObject. Ensure `Is Trigger` is *unchecked* for physics interactions.

3.  **Create a Lock-On Indicator (Optional but Recommended):**
    *   In your Unity project, right-click in the Project window -> Create -> UI -> Image. This will create a Canvas and an Image.
    *   Rename the Image to "LockOnIndicator".
    *   Adjust its size, color, and sprite as desired (e.g., a simple target reticle).
    *   **Crucially:** Set the Canvas's `Render Mode` to `World Space`. Adjust its `Rect Transform` (width, height, scale) and position (e.g., (0,0,0) with scale (0.01, 0.01, 0.01)) so it appears as a small indicator in the world.
    *   Make sure the "LockOnIndicator" Image is the *only* child of the Canvas. Drag the "LockOnIndicator" Image from the Hierarchy into your Project window to create a Prefab.
    *   Delete the "LockOnIndicator" and its Canvas from your Hierarchy.
    *   Drag this new "LockOnIndicator" Prefab into the `_Lock On Indicator Prefab` field of your `CombatLockOnSystem` script on the Player.

4.  **Configure `CombatLockOnSystem` in Inspector:**
    *   On your "Player" GameObject, select the `CombatLockOnSystem` component.
    *   `_Lock On Search Radius`: Set a reasonable range (e.g., 20-30).
    *   `_Max Lock On Distance`: Set a slightly larger range (e.g., 25-35).
    *   `_Lock On View Angle`: e.g., 90 degrees.
    *   `_Target Layer`: Select your "Enemy" layer from the dropdown.
    *   `_Player Rotation Speed`: e.g., 10-15.
    *   `_Potential Target Refresh Rate`: e.g., 0.5 (or 0 for every frame).
    *   `_Toggle Lock On Key`: `Tab` (default).
    *   `_Cycle Target Right Key`: `E` (default).
    *   `_Cycle Target Left Key`: `Q` (default).

5.  **Setup Player Camera (Optional but Recommended):**
    *   Create a Camera in your scene (or use the Main Camera).
    *   Attach the `PlayerCamera.cs` script (provided above) to your Camera.
    *   Drag your "Player" GameObject's `Transform` into the `_Player Transform` field of the `PlayerCamera` script.
    *   Adjust `_Offset` and `_Follow Speed` as needed.

6.  **Setup Player Combat (Optional for Testing Events):**
    *   Attach the `PlayerCombat.cs` script (provided above) to your "Player" GameObject (it can coexist with `CombatLockOnSystem`).

7.  **Play the Scene:**
    *   Press `Tab` to lock onto the closest enemy.
    *   Press `E` or `Q` to cycle through available enemies.
    *   Press `Tab` again to unlock.
    *   Observe how the camera and player combat (via `Debug.Log`s) react to the lock-on state changes.
    *   Move enemies out of range or behind obstacles to see the lock-on automatically break.

This complete setup provides a practical and educational example of the Combat Lock-On System design pattern in Unity.