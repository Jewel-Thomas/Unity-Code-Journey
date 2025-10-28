// Unity Design Pattern Example: WarpZoneSystem
// This script demonstrates the WarpZoneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the "WarpZoneSystem" design pattern in Unity. This pattern provides a flexible and decoupled way to manage teleportation or transitions between distinct areas within your game.

It consists of three main components:

1.  **`WarpZoneSystem` (The Manager - Singleton):** The central hub that keeps track of all available warp destinations and executes the actual teleportation logic.
2.  **`WarpDestination` (The Destination Marker):** A simple component placed at specific locations that can serve as targets for warping. It registers itself with the `WarpZoneSystem` using a unique ID.
3.  **`WarpZoneTrigger` (The Initiator/Portal):** A component attached to a trigger volume (e.g., a Box Collider). When an eligible entity enters this trigger, it requests the `WarpZoneSystem` to warp that entity to a specified `WarpDestination` ID.

---

### **WarpZoneSystem.cs**

This script acts as the central manager for all warp operations. It maintains a registry of `WarpDestination` transforms and handles the actual teleportation requests.

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System.Linq; // For LINQ operations if needed, currently not heavily used but often useful with collections.

/// <summary>
/// The central manager for the WarpZoneSystem pattern.
/// This MonoBehaviour acts as a Singleton, managing all registered WarpDestinations
/// and executing warp requests from WarpZoneTriggers.
/// </summary>
public class WarpZoneSystem : MonoBehaviour
{
    // Singleton instance. Provides easy global access to the system.
    public static WarpZoneSystem Instance { get; private set; }

    // Dictionary to store registered WarpDestinations.
    // Key: The unique ID of the destination (string).
    // Value: The Transform of the destination point.
    private Dictionary<string, Transform> _warpDestinations = new Dictionary<string, Transform>();

    [Header("System Settings")]
    [Tooltip("Minimum time (in seconds) an entity must wait before it can warp again " +
             "after a successful warp. Prevents rapid, accidental re-warps.")]
    [SerializeField] private float _entityWarpCooldown = 1.0f;

    // A dictionary to track cooldowns for individual GameObjects that have recently warped.
    // Key: The GameObject that warped.
    // Value: The Time.time when its cooldown will expire.
    private Dictionary<GameObject, float> _entityCooldownTimers = new Dictionary<GameObject, float>();


    private void Awake()
    {
        // Implement the Singleton pattern.
        // Ensures only one instance of WarpZoneSystem exists in the scene.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WarpZoneSystem: Found multiple instances of WarpZoneSystem. Destroying this one.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, uncomment the line below if this system should persist across scene loads.
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        // Update and clean up entity cooldowns.
        // This prevents the _entityCooldownTimers dictionary from growing indefinitely.
        if (_entityCooldownTimers.Count > 0)
        {
            List<GameObject> expiredEntities = new List<GameObject>();
            foreach (var entry in _entityCooldownTimers)
            {
                if (Time.time >= entry.Value)
                {
                    expiredEntities.Add(entry.Key);
                }
            }

            foreach (var entity in expiredEntities)
            {
                _entityCooldownTimers.Remove(entity);
            }
        }
    }

    /// <summary>
    /// Registers a <see cref="WarpDestination"/> with the system.
    /// This makes the destination available for <see cref="WarpZoneTrigger"/>s to use.
    /// Typically called by a WarpDestination's OnEnable method.
    /// </summary>
    /// <param name="id">The unique identifier for the destination.</param>
    /// <param name="destinationTransform">The Transform representing the destination's position and rotation.</param>
    public void RegisterDestination(string id, Transform destinationTransform)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("WarpZoneSystem: Attempted to register a destination with a null or empty ID.", destinationTransform);
            return;
        }
        if (destinationTransform == null)
        {
            Debug.LogError($"WarpZoneSystem: Attempted to register destination '{id}' with a null Transform.", destinationTransform);
            return;
        }

        if (_warpDestinations.ContainsKey(id))
        {
            // This can happen if a destination is disabled and re-enabled, or if there are duplicate IDs (which should be avoided).
            Debug.LogWarning($"WarpZoneSystem: A destination with ID '{id}' is already registered. Overwriting with new transform '{destinationTransform.name}'.", destinationTransform);
            _warpDestinations[id] = destinationTransform;
        }
        else
        {
            _warpDestinations.Add(id, destinationTransform);
            // Debug.Log($"WarpZoneSystem: Destination '{id}' registered.", destinationTransform);
        }
    }

    /// <summary>
    /// Unregisters a <see cref="WarpDestination"/> from the system.
    /// This removes it as a valid target for warp requests.
    /// Typically called by a WarpDestination's OnDisable method.
    /// </summary>
    /// <param name="id">The unique ID of the destination to unregister.</param>
    public void UnregisterDestination(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("WarpZoneSystem: Attempted to unregister a destination with a null or empty ID.");
            return;
        }

        if (_warpDestinations.Remove(id))
        {
            // Debug.Log($"WarpZoneSystem: Destination '{id}' unregistered.");
        }
        else
        {
            // Debug.LogWarning($"WarpZoneSystem: Attempted to unregister destination '{id}' but it was not found. " +
            //                  "This might indicate an ID mismatch or double unregistration.");
        }
    }

    /// <summary>
    /// Attempts to retrieve the Transform for a registered WarpDestination.
    /// Useful for editor visualization or external queries without initiating a warp.
    /// </summary>
    /// <param name="id">The unique ID of the destination.</param>
    /// <param name="destinationTransform">Output parameter: The Transform if found.</param>
    /// <returns>True if the destination was found, false otherwise.</returns>
    public bool TryGetDestinationTransform(string id, out Transform destinationTransform)
    {
        return _warpDestinations.TryGetValue(id, out destinationTransform);
    }

    /// <summary>
    /// Requests the WarpZoneSystem to warp an entity to a specified destination.
    /// This is the core method called by <see cref="WarpZoneTrigger"/>s.
    /// </summary>
    /// <param name="entityToWarp">The GameObject that should be warped (e.g., the Player).</param>
    /// <param name="targetDestinationID">The unique ID of the target WarpDestination.</param>
    /// <returns>True if the warp was successful, false otherwise.</returns>
    public bool RequestWarp(GameObject entityToWarp, string targetDestinationID)
    {
        if (entityToWarp == null)
        {
            Debug.LogError("WarpZoneSystem: Cannot warp a null entity.");
            return false;
        }

        if (string.IsNullOrEmpty(targetDestinationID))
        {
            Debug.LogError($"WarpZoneSystem: Entity '{entityToWarp.name}' tried to warp with a null or empty target destination ID.", entityToWarp);
            return false;
        }

        // Check if the entity is currently on cooldown.
        if (_entityCooldownTimers.ContainsKey(entityToWarp) && Time.time < _entityCooldownTimers[entityToWarp])
        {
            // Debug.Log($"WarpZoneSystem: Entity '{entityToWarp.name}' is on warp cooldown. Remaining: ({_entityCooldownTimers[entityToWarp] - Time.time:F2}s)", entityToWarp);
            return false; // Warp request ignored due to cooldown.
        }

        // Try to find the destination transform using the provided ID.
        if (_warpDestinations.TryGetValue(targetDestinationID, out Transform destinationTransform))
        {
            // Perform the actual warp: set position and rotation.
            entityToWarp.transform.position = destinationTransform.position;
            entityToWarp.transform.rotation = destinationTransform.rotation;

            // Optional: If the entity has a Rigidbody, reset its velocity to prevent unwanted momentum after warp.
            Rigidbody rb = entityToWarp.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"WarpZoneSystem: Entity '{entityToWarp.name}' successfully warped to destination '{targetDestinationID}'.", entityToWarp);

            // Apply cooldown to prevent immediate re-warping.
            _entityCooldownTimers[entityToWarp] = Time.time + _entityWarpCooldown;

            // Optional: Trigger a custom event here (e.g., UnityEvent, C# event)
            // Example: GameEvents.OnEntityWarped?.Invoke(entityToWarp, targetDestinationID);

            return true; // Warp successful.
        }
        else
        {
            // Destination not found. Log a warning.
            Debug.LogWarning($"WarpZoneSystem: Destination with ID '{targetDestinationID}' not found for entity '{entityToWarp.name}'. Warp failed.", entityToWarp);
            return false; // Warp failed.
        }
    }

    /// <summary>
    /// Editor-only visualization for registered destinations.
    /// </summary>
    void OnDrawGizmos()
    {
        if (_warpDestinations == null || !Application.isEditor) return;

        // Visualize all registered destinations
        foreach (var entry in _warpDestinations)
        {
            if (entry.Value != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(entry.Value.position, 0.5f);
                Gizmos.DrawRay(entry.Value.position, entry.Value.forward * 1f); // Show direction

                #if UNITY_EDITOR
                // Draw ID text in editor using UnityEditor.Handles for better readability.
                // Requires UnityEditor namespace, so wrap with #if UNITY_EDITOR.
                UnityEditor.Handles.Label(entry.Value.position + Vector3.up * 0.7f, $"Dest: {entry.Key}");
                #endif
            }
        }
    }
}
```

---

### **WarpDestination.cs**

This script marks a GameObject's transform as a valid destination point within the `WarpZoneSystem`.

```csharp
using UnityEngine;
// No System.Collections needed specifically for this script.

/// <summary>
/// Marks a specific GameObject's Transform as a valid destination for warping.
/// It registers and unregisters itself with the central <see cref="WarpZoneSystem"/>.
/// </summary>
[DisallowMultipleComponent] // Ensures only one WarpDestination component per GameObject.
public class WarpDestination : MonoBehaviour
{
    [Tooltip("A unique identifier for this warp destination. " +
             "WarpZoneTriggers will use this ID to specify this location as their target.")]
    [SerializeField] private string _destinationID;

    // Public getter for the destination ID.
    public string DestinationID => _destinationID;

    #if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to validate and potentially auto-assign the ID.
    /// Ensures the ID is set and provides a simple default if left empty.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_destinationID))
        {
            // Simple auto-assignment: use the GameObject's name as a default ID.
            // In a larger project, consider a more robust ID generation or validation system.
            _destinationID = gameObject.name;
            UnityEditor.EditorUtility.SetDirty(this); // Mark as dirty to save the change.
        }
    }
    #endif

    /// <summary>
    /// Called when the GameObject becomes enabled or active.
    /// Registers this destination with the central <see cref="WarpZoneSystem"/>.
    /// </summary>
    private void OnEnable()
    {
        if (WarpZoneSystem.Instance != null)
        {
            WarpZoneSystem.Instance.RegisterDestination(_destinationID, this.transform);
        }
        else
        {
            Debug.LogError($"WarpDestination '{gameObject.name}': WarpZoneSystem not found! " +
                           "Ensure there's a 'WarpZoneSystem' GameObject in the scene with the script attached.", this);
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive, or is destroyed.
    /// Unregisters this destination from the central <see cref="WarpZoneSystem"/>.
    /// </summary>
    private void OnDisable()
    {
        if (WarpZoneSystem.Instance != null)
        {
            WarpZoneSystem.Instance.UnregisterDestination(_destinationID);
        }
        // No error log if WarpZoneSystem is null here, as it might be destroyed before destinations (e.g., scene unload).
    }

    /// <summary>
    /// Editor-only visualization for the destination point.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.4f); // Draw a wire sphere to mark the point.
        Gizmos.DrawRay(transform.position, transform.forward * 1f); // Draw a ray to indicate forward direction.

        #if UNITY_EDITOR
        // Draw ID text in editor using UnityEditor.Handles for better readability.
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"DEST: {_destinationID}");
        #endif
    }
}
```

---

### **WarpZoneTrigger.cs**

This script attaches to a trigger volume and initiates a warp request to the `WarpZoneSystem` when an eligible entity enters.

```csharp
using UnityEngine;
// No System.Collections needed specifically for this script.

/// <summary>
/// A component that defines a trigger volume. When an eligible entity enters this zone,
/// it requests the <see cref="WarpZoneSystem"/> to warp that entity to a specified destination.
/// </summary>
[RequireComponent(typeof(Collider))] // A Collider component set to 'Is Trigger' is essential.
[DisallowMultipleComponent] // Ensures only one WarpZoneTrigger component per GameObject.
public class WarpZoneTrigger : MonoBehaviour
{
    [Tooltip("The unique ID of the WarpDestination where entities entering this zone will be sent.")]
    [SerializeField] private string _targetDestinationID;

    [Tooltip("Optional: A tag that identifies objects capable of being warped (e.g., 'Player'). " +
             "Leave empty to allow any object with a collider to trigger the warp.")]
    [SerializeField] private string _warpableTag = "Player";

    [Header("Trigger Settings")]
    [Tooltip("If true, this trigger can be activated multiple times (subject to trigger cooldown). " +
             "If false, it will deactivate itself after one successful warp.")]
    [SerializeField] private bool _canWarpMultipleTimes = true;

    [Tooltip("Time in seconds to wait before this trigger can be activated again by any entity " +
             "(only applies if 'Can Warp Multiple Times' is true).")]
    [SerializeField] private float _triggerCooldown = 0.5f;

    // Internal timer to manage the trigger's cooldown.
    private float _nextReadyTime = 0f;

    // Reference to the Collider component, automatically assigned in Awake.
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError($"WarpZoneTrigger on '{gameObject.name}' requires a Collider component!", this);
            enabled = false; // Disable this script if no collider is found.
            return;
        }

        // Ensure the collider is set as a trigger for OnTriggerEnter to work.
        if (!_collider.isTrigger)
        {
            Debug.LogWarning($"WarpZoneTrigger on '{gameObject.name}' has a non-trigger collider. Setting it to 'Is Trigger'.", this);
            _collider.isTrigger = true;
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to validate settings.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_targetDestinationID))
        {
            Debug.LogWarning($"WarpZoneTrigger on '{gameObject.name}' has an empty Target Destination ID. " +
                             "Please set it to a valid WarpDestination ID.", this);
        }
    }
    #endif

    /// <summary>
    /// Called when another collider enters this trigger volume.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Basic checks before attempting a warp.
        if (string.IsNullOrEmpty(_targetDestinationID))
        {
            Debug.LogWarning($"WarpZoneTrigger '{gameObject.name}' has no target destination ID set. Cannot warp.", this);
            return;
        }

        // Check if the entering object has the required tag, if _warpableTag is specified.
        if (!string.IsNullOrEmpty(_warpableTag) && !other.CompareTag(_warpableTag))
        {
            // Debug.Log($"WarpZoneTrigger: Object '{other.name}' does not have the required tag '{_warpableTag}'.", other.gameObject);
            return; // Ignore objects without the specified tag.
        }

        // Check if the trigger itself is on cooldown.
        if (Time.time < _nextReadyTime)
        {
            // Debug.Log($"WarpZoneTrigger: Trigger for '{gameObject.name}' is on cooldown.", this);
            return; // Ignore if trigger is still on cooldown.
        }

        // Request the warp from the central WarpZoneSystem.
        if (WarpZoneSystem.Instance != null)
        {
            bool warped = WarpZoneSystem.Instance.RequestWarp(other.gameObject, _targetDestinationID);

            if (warped)
            {
                // If the warp was successful:
                if (!_canWarpMultipleTimes)
                {
                    // If this trigger is single-use, deactivate it.
                    Debug.Log($"WarpZoneTrigger '{gameObject.name}' successfully used once. Deactivating.", this);
                    gameObject.SetActive(false); // Disable the GameObject containing this trigger.
                }
                else
                {
                    // If re-usable, apply the trigger-specific cooldown.
                    _nextReadyTime = Time.time + _triggerCooldown;
                }
            }
            // If warped is false, it means the entity was on cooldown or the destination didn't exist,
            // so we don't apply _triggerCooldown or deactivate.
        }
        else
        {
            Debug.LogError($"WarpZoneTrigger '{gameObject.name}': WarpZoneSystem not found! " +
                           "Ensure there's a 'WarpZoneSystem' GameObject in the scene with the script attached.", this);
        }
    }

    /// <summary>
    /// Editor-only visualization for the trigger volume and its target.
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw the trigger collider volume for visual reference.
        if (_collider != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
            if (_collider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (_collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            else if (_collider is CapsuleCollider capsule)
            {
                // Gizmos.DrawWireSphere is approximate for capsule, can draw more complex shapes for accuracy.
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
            // Add other collider types as needed (e.g., MeshCollider).

            Gizmos.color = new Color(0, 1, 1, 0.8f); // Opaque cyan for wireframe
            if (_collider is BoxCollider boxWire)
            {
                Gizmos.DrawWireCube(boxWire.center, boxWire.size);
            }
            else if (_collider is SphereCollider sphereWire)
            {
                Gizmos.DrawWireSphere(sphereWire.center, sphereWire.radius);
            }
            else if (_collider is CapsuleCollider capsuleWire)
            {
                Gizmos.DrawWireSphere(capsuleWire.center, capsuleWire.radius);
            }
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix after drawing transformed gizmo.
        }

        // Draw a line connecting the trigger to its target destination (if found).
        if (!string.IsNullOrEmpty(_targetDestinationID))
        {
            // Only try to get WarpZoneSystem.Instance if it's already initialized to avoid creating it in editor.
            if (WarpZoneSystem.Instance != null && WarpZoneSystem.Instance.TryGetDestinationTransform(_targetDestinationID, out Transform targetTransform))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetTransform.position); // Line from trigger to destination.
                Gizmos.DrawSphere(transform.position, 0.2f); // Mark trigger origin.
                Gizmos.DrawSphere(targetTransform.position, 0.2f); // Mark destination.
            }
        }

        #if UNITY_EDITOR
        // Draw the target ID text above the trigger in the editor.
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"WARP TO: {_targetDestinationID}");
        #endif
    }
}
```

---

### **How to Use the WarpZoneSystem in Unity:**

1.  **Create the WarpZoneSystem Manager:**
    *   Create an Empty GameObject in your scene (e.g., `_GameManagers`).
    *   Add another Empty GameObject as a child (e.g., `WarpZoneSystem`).
    *   Drag and drop the `WarpZoneSystem.cs` script onto this `WarpZoneSystem` GameObject. This creates your global manager.
    *   You can adjust the `Entity Warp Cooldown` in its Inspector.

2.  **Define Warp Destinations:**
    *   For each unique location you want to be a possible warp *target*, create an Empty GameObject (e.g., `WarpDestination_HubEntry`, `WarpDestination_Level1_Spawn`).
    *   Position and rotate these GameObjects exactly where you want the player (or other entities) to appear and which way they should face after warping.
    *   Drag and drop the `WarpDestination.cs` script onto each of these GameObjects.
    *   In the Inspector of each `WarpDestination`, assign a **unique and descriptive `Destination ID`** (e.g., "HubEntry", "Level1Spawn"). These IDs are crucial for linking triggers to destinations.

3.  **Create Warp Zone Triggers:**
    *   For each area you want to act as a *portal* or *teleporter*, create an Empty GameObject (e.g., `WarpTrigger_ToLevel1`, `WarpTrigger_ReturnToHub`).
    *   Add a `Collider` component to it (e.g., `BoxCollider` for a rectangular area, `SphereCollider` for a spherical area).
    *   **Crucially, ensure the `Is Trigger` checkbox on the Collider component is ENABLED.**
    *   Scale and position the collider to define the exact trigger volume.
    *   Drag and drop the `WarpZoneTrigger.cs` script onto this GameObject.
    *   In the Inspector of each `WarpZoneTrigger`:
        *   Set the `Target Destination ID` to one of the IDs you defined in step 2 (e.g., "Level1Spawn" for `WarpTrigger_ToLevel1`).
        *   Optionally, specify a `Warpable Tag` (e.g., "Player"). Only GameObjects with this tag will trigger the warp. Leave it empty to allow any collider to trigger.
        *   Adjust `Can Warp Multiple Times` and `Trigger Cooldown` as desired for reusability.

4.  **Tag Your Warpable Entities:**
    *   Make sure your player character (or any GameObject you intend to warp) has the `Tag` specified in the `WarpZoneTrigger`'s `Warpable Tag` field (e.g., if `_warpableTag` is "Player", your player GameObject must have the "Player" tag).

**That's it!** When an entity with the specified tag enters a `WarpZoneTrigger`'s volume, it will be instantly transported to the `WarpDestination` with the matching ID. The editor gizmos (magenta spheres for destinations, cyan volumes for triggers, and yellow lines connecting triggers to their targets) will help you visualize your warp network.