// Unity Design Pattern Example: CrowdSystem
// This script demonstrates the CrowdSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'CrowdSystem' design pattern in Unity is used for efficiently managing a large number of similar game entities (a "crowd") such as NPCs, projectiles, visual effects, or environmental elements. Its core principles revolve around:

1.  **Centralized Management**: A single manager object controls the lifecycle, state, and often the updates of all crowd members.
2.  **Object Pooling**: Instead of constantly instantiating and destroying objects (which causes performance spikes due to memory allocation and garbage collection), objects are pre-created and reused from a pool.
3.  **Simplified Member Logic**: Individual crowd members often have minimal, self-contained logic, delegating complex coordination or shared state management to the system manager.
4.  **Batch Processing**: The manager can iterate through active members and update them, allowing for optimizations like culling, level-of-detail changes, or job system integration for large-scale operations.

This example demonstrates a basic CrowdSystem where "CrowdMembers" (represented by simple spheres) move to random targets and are then recycled back into a pool.

---

### Unity Setup Instructions:

1.  **Create a 3D Object for CrowdMember:**
    *   In Unity, go to `GameObject -> 3D Object -> Sphere`.
    *   Rename it to `CrowdMemberPrefab`.
    *   You can scale it down (e.g., `Scale: 0.2, 0.2, 0.2`).
    *   Add a `Rigidbody` component (`Add Component -> Physics -> Rigidbody`), uncheck `Use Gravity` to keep them on the plane and check `Is Kinematic` if you want to control movement purely by transform. For this example, we move by `transform.position`, so `Is Kinematic` is appropriate.
    *   Ensure its `Box Collider` (or `Sphere Collider`) is present.
    *   Drag this `CrowdMemberPrefab` from the Hierarchy into your Project window to create a prefab.
    *   Delete the `CrowdMemberPrefab` instance from the Hierarchy.

2.  **Create an Empty GameObject for CrowdSystem:**
    *   In Unity, go to `GameObject -> Create Empty`.
    *   Rename it to `CrowdSystemManager`.

3.  **Apply Scripts:**
    *   Create two new C# scripts: `CrowdMember.cs` and `CrowdSystem.cs`.
    *   Copy the code for `CrowdMember.cs` into the `CrowdMember.cs` script file.
    *   Copy the code for `CrowdSystem.cs` into the `CrowdSystem.cs` script file.
    *   Attach the `CrowdSystem.cs` script to the `CrowdSystemManager` GameObject in your Hierarchy.

4.  **Configure CrowdSystemManager in Inspector:**
    *   Select the `CrowdSystemManager` GameObject.
    *   In the Inspector, drag your `CrowdMemberPrefab` from the Project window into the `Crowd Member Prefab` slot of the `Crowd System` component.
    *   Adjust `Pool Size`, `Spawn Area Min/Max`, `Spawn Height`, `Spawn Interval`, and `Max Active Members` as desired.
        *   **Recommended starting values:**
            *   `Pool Size`: 200
            *   `Spawn Area Min`: X=-50, Y=-50 (represents X and Z coordinates)
            *   `Spawn Area Max`: X=50, Y=50 (represents X and Z coordinates)
            *   `Spawn Height`: 0.5 (or whatever Y-level your ground is)
            *   `Spawn Interval`: 0.05
            *   `Max Active Members`: 1000

5.  **Run the Scene:**
    *   Press Play. You should see spheres (CrowdMembers) appearing and moving around, then disappearing and reappearing elsewhere, demonstrating the object pooling and centralized management.

---

### 1. `CrowdMember.cs` (Individual Crowd Agent)

This script defines the behavior and state for a single crowd member. It's designed to be lightweight and managed entirely by the `CrowdSystem`.

```csharp
using UnityEngine;

/// <summary>
/// Represents an individual member of the crowd managed by the CrowdSystem.
/// This script contains the basic behavior and state for a single crowd agent.
/// </summary>
/// <remarks>
/// Crowd members are typically lightweight, focusing on their individual task.
/// Their lifecycle (spawning, despawning) and often their updates are controlled
/// by the central CrowdSystem manager.
/// </remarks>
[RequireComponent(typeof(Collider))] // Ensures a collider is present for basic interaction (e.g., ground detection)
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present for physics (set to Kinematic if moving by transform)
public class CrowdMember : MonoBehaviour
{
    [Tooltip("The movement speed of this crowd member.")]
    public float Speed = 2f;

    private Vector3 _targetPosition; // The current target position this member is moving towards.
    private bool _hasTarget;         // True if the member has an active target and is moving.

    /// <summary>
    /// Initializes the crowd member for active use.
    /// Called by the CrowdSystem when a member is taken from the pool.
    /// </summary>
    /// <param name="startPosition">The initial position for the member.</param>
    /// <param name="targetPosition">The target position the member should move towards.</param>
    public void Initialize(Vector3 startPosition, Vector3 targetPosition)
    {
        transform.position = startPosition; // Set initial position
        _targetPosition = targetPosition;   // Set the new target
        _hasTarget = true;                  // Mark as having a target
        gameObject.SetActive(true);         // Ensure the GameObject is active in the scene
        // Optional: Reset other member-specific states (e.g., health, animation state, color)
        // For example, if it had health, you'd reset it here.
    }

    /// <summary>
    /// Resets the crowd member to its inactive state, preparing it to be returned to the pool.
    /// Called by the CrowdSystem when a member is despawned.
    /// </summary>
    public void ResetMember()
    {
        _hasTarget = false;                 // No longer has a target
        gameObject.SetActive(false);        // Deactivate the GameObject to hide it and stop updates
        // Optional: Clear references, stop coroutines, reset visual properties, etc.
        // For example, if it had a trail renderer, you might clear it here.
    }

    /// <summary>
    /// Updates the state and behavior of the individual crowd member.
    /// This method is called by the CrowdSystem each frame for active members,
    /// demonstrating centralized control over updates.
    /// </summary>
    /// <returns>True if the member is still active/has a target, false if it has completed its task.</returns>
    public bool AgentUpdate()
    {
        if (!_hasTarget)
        {
            // If the member has no target, it's considered "done" or inactive for this cycle.
            return false;
        }

        // Move towards the target position.
        // We use MoveTowards for simplicity, other movement types like NavMeshAgent could be used.
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, Speed * Time.deltaTime);

        // Check if the member has reached its target (within a small threshold).
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            _hasTarget = false; // Member has reached its destination, no longer needs to move.
            return false;       // Indicate that this member has completed its current task.
        }

        return true; // Member is still active and moving towards its target.
    }
}
```

### 2. `CrowdSystem.cs` (The Central Manager)

This script embodies the 'CrowdSystem' pattern, handling object pooling, spawning, despawning, and updating all individual `CrowdMember` instances.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Stack and List

/// <summary>
/// Implements the 'CrowdSystem' design pattern in Unity.
/// This system efficiently manages a large number of similar entities (crowd members)
/// using object pooling, centralized updates, and simplified member behaviors.
/// </summary>
/// <remarks>
/// **Key Components of the CrowdSystem Pattern:**
/// 1.  **Centralized Manager (CrowdSystem):** Handles creation, destruction (via pooling),
///     and updating of all crowd members. It acts as the single point of control.
///     This decouples the lifecycle management from individual members.
/// 2.  **Crowd Member (CrowdMember):** A simple, lightweight entity representing an
///     individual in the crowd. Its behavior is often simplified, with complex logic
///     residing in the manager or a separate controller.
/// 3.  **Object Pooling:** Crucial for performance. Instead of instantiating and
///     destroying objects, they are reused from a pool of pre-allocated objects.
///     This avoids costly memory allocations and garbage collection spikes, especially
///     with frequent object creation/destruction.
/// 4.  **Batch Processing/Updates:** The manager iterates through active members
///     and calls their update logic. This allows for potential optimizations, like
///     only updating members within a certain range, or applying global behaviors.
/// 5.  **Scalability:** Designed to handle hundreds or thousands of entities efficiently.
/// </remarks>
public class CrowdSystem : MonoBehaviour
{
    [Header("Crowd Member Settings")]
    [Tooltip("The prefab for the individual crowd members.")]
    public CrowdMember CrowdMemberPrefab;

    [Tooltip("The initial number of crowd members to pre-instantiate and add to the pool.")]
    public int PoolSize = 100;

    [Tooltip("The minimum X and Z coordinates for spawning and target positions.")]
    public Vector2 SpawnAreaMin = new Vector2(-50, -50);

    [Tooltip("The maximum X and Z coordinates for spawning and target positions.")]
    public Vector2 SpawnAreaMax = new Vector2(50, 50);

    [Tooltip("The fixed Y-coordinate (height) for all spawned crowd members.")]
    public float SpawnHeight = 0.5f;

    [Header("Crowd Behavior Settings")]
    [Tooltip("The time interval (in seconds) between attempts to spawn a new crowd member.")]
    public float SpawnInterval = 0.1f;

    [Tooltip("The maximum number of crowd members that can be active in the scene at any given time.")]
    public int MaxActiveMembers = 500;

    // The object pool for inactive crowd members, using a Stack for efficient LIFO (Last-In, First-Out) access.
    // Stack is good here because we don't care about the order, just getting an available object quickly.
    private Stack<CrowdMember> _objectPool = new Stack<CrowdMember>();

    // A list of currently active crowd members in the scene.
    // Using a List allows for easy iteration and removal.
    private List<CrowdMember> _activeMembers = new List<CrowdMember>();

    // Timer to control the rate of spawning new members.
    private float _nextSpawnTime;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is the ideal place to initialize the object pool by pre-instantiating crowd members.
    /// </summary>
    void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// Initializes the object pool by creating `PoolSize` instances of the CrowdMemberPrefab.
    /// All instantiated members are immediately reset and added to the pool.
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            // Instantiate as a child of this CrowdSystem GameObject for organizational purposes in the Hierarchy.
            CrowdMember member = Instantiate(CrowdMemberPrefab, transform);
            member.ResetMember(); // Set to inactive state and add to pool.
            _objectPool.Push(member);
        }
        Debug.Log($"CrowdSystem initialized with a pool of {PoolSize} members.");
    }

    /// <summary>
    /// Called once per frame.
    /// Manages spawning new members and updating existing active members.
    /// </summary>
    void Update()
    {
        // --- 1. Spawning Logic ---
        // Attempt to spawn a new crowd member if:
        // a) Enough time has passed since the last spawn.
        // b) The maximum number of active members hasn't been reached.
        // c) There are available members in the pool.
        if (Time.time >= _nextSpawnTime && _activeMembers.Count < MaxActiveMembers && _objectPool.Count > 0)
        {
            SpawnCrowdMember();
            _nextSpawnTime = Time.time + SpawnInterval; // Schedule next spawn attempt.
        }

        // --- 2. Update Active Members Logic ---
        // Iterate and update all currently active crowd members.
        // We iterate backwards to safely remove elements from the list while iterating.
        for (int i = _activeMembers.Count - 1; i >= 0; i--)
        {
            CrowdMember member = _activeMembers[i];

            // Call the member's update logic.
            // The member returns true if it's still active/has a task,
            // false if it has completed its current task (e.g., reached target).
            bool stillActive = member.AgentUpdate();

            if (!stillActive)
            {
                // If the member has completed its task, despawn it (return to pool).
                DespawnCrowdMember(member);
            }
        }

        // --- Optional: Debugging information ---
        // Periodically log the active count to observe the system's performance and state.
        // if (Time.frameCount % 300 == 0) // Log every ~5 seconds (at 60fps)
        // {
        //     Debug.Log($"Active crowd members: {_activeMembers.Count} | Available in pool: {_objectPool.Count}");
        // }
    }

    /// <summary>
    /// Retrieves a crowd member from the object pool, initializes it with a
    /// random position and target, and adds it to the list of active members.
    /// This is the primary method for 'creating' a new crowd member in the scene.
    /// </summary>
    /// <returns>The newly spawned CrowdMember, or null if the pool is empty.</returns>
    private CrowdMember SpawnCrowdMember()
    {
        if (_objectPool.Count == 0)
        {
            Debug.LogWarning("CrowdSystem pool is empty! Consider increasing PoolSize or MaxActiveMembers to prevent shortages.");
            return null; // No available members in the pool.
        }

        CrowdMember member = _objectPool.Pop(); // Get an inactive member from the pool.
        _activeMembers.Add(member);             // Add it to the list of currently active members.

        // Assign a random start and target position within the defined area.
        Vector3 startPos = GetRandomAreaPosition();
        Vector3 targetPos = GetRandomAreaPosition();
        member.Initialize(startPos, targetPos); // Initialize its state with new parameters.

        return member;
    }

    /// <summary>
    /// Returns an active crowd member to the object pool, resetting its state
    /// and removing it from the list of active members.
    /// This is the primary method for 'destroying' a crowd member from the scene.
    /// </summary>
    /// <param name="member">The CrowdMember to despawn.</param>
    private void DespawnCrowdMember(CrowdMember member)
    {
        // Attempt to remove the member from the active list.
        // If it's not found, it means there was an error in tracking or it was already removed.
        if (!_activeMembers.Remove(member))
        {
            Debug.LogWarning($"Attempted to despawn CrowdMember {member.name} but it was not found in the active list. This might indicate a logic error.");
            return;
        }

        member.ResetMember(); // Reset its state and deactivate its GameObject.
        _objectPool.Push(member); // Return it to the pool for reuse.
    }

    /// <summary>
    /// Generates a random position within the defined SpawnAreaMin/Max at the specified SpawnHeight.
    /// The Vector2 min/max are treated as X and Z coordinates for a 2D plane.
    /// </summary>
    /// <returns>A random Vector3 position.</returns>
    private Vector3 GetRandomAreaPosition()
    {
        float x = Random.Range(SpawnAreaMin.x, SpawnAreaMax.x);
        float z = Random.Range(SpawnAreaMin.y, SpawnAreaMax.y); // Note: using y component of Vector2 for Z-axis
        return new Vector3(x, SpawnHeight, z);
    }

    /// <summary>
    /// Public method to despawn all currently active crowd members, returning them to the pool.
    /// This can be called from other scripts (e.g., when changing levels or game states).
    /// </summary>
    public void DespawnAllCrowdMembers()
    {
        // Iterate backwards to safely remove elements while modifying the list.
        int countBeforeDespawn = _activeMembers.Count;
        for (int i = _activeMembers.Count - 1; i >= 0; i--)
        {
            DespawnCrowdMember(_activeMembers[i]); // This will remove from active list and add to pool.
        }
        Debug.Log($"Despawned {countBeforeDespawn} active crowd members and returned them to pool.");
    }

    /// <summary>
    /// Gets the current number of active crowd members in the scene.
    /// </summary>
    /// <returns>The count of currently active members.</returns>
    public int GetActiveCrowdMemberCount()
    {
        return _activeMembers.Count;
    }

    /// <summary>
    /// Gets the current number of available crowd members in the pool.
    /// </summary>
    /// <returns>The count of available (inactive) members in the pool.</returns>
    public int GetPoolCount()
    {
        return _objectPool.Count;
    }

    // --- Example Usage in another script ---
    /*
    // Example of how another MonoBehaviour script could interact with the CrowdSystem:
    public class GameController : MonoBehaviour
    {
        public CrowdSystem myCrowdSystem; // Drag your CrowdSystemManager here in the Inspector

        void Start()
        {
            // You can dynamically get the CrowdSystem if it's a singleton or known globally
            // myCrowdSystem = FindObjectOfType<CrowdSystem>();
            if (myCrowdSystem == null)
            {
                Debug.LogError("CrowdSystem reference not set on GameController!");
                return;
            }

            Debug.Log($"Current active crowd members: {myCrowdSystem.GetActiveCrowdMemberCount()}");
        }

        // Example: Despawn all crowd members when a specific key is pressed
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Space key pressed! Despawning all crowd members...");
                myCrowdSystem.DespawnAllCrowdMembers();
            }

            // You could also trigger custom spawns if needed, though the CrowdSystem manages default spawning.
            // if (Input.GetKeyDown(KeyCode.S))
            // {
            //     // Note: The CrowdSystem itself manages random spawning, but you could add
            //     // a public method to trigger a single spawn with specific parameters if desired.
            //     // Example: myCrowdSystem.SpawnCrowdMemberAtLocation(new Vector3(0, 0, 0), new Vector3(10, 0, 10));
            // }
        }
    }
    */
}
```