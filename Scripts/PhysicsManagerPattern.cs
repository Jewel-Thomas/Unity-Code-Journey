// Unity Design Pattern Example: PhysicsManagerPattern
// This script demonstrates the PhysicsManagerPattern pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **PhysicsManagerPattern** in Unity. This pattern centralizes and encapsulates all physics-related operations within a single, globally accessible manager. This decouples game objects from direct interaction with Unity's Physics engine or Rigidbody components.

## PhysicsManagerPattern Benefits:

1.  **Decoupling:** Game objects don't need to know the specifics of how physics actions are performed. They simply request an action from the manager. This makes client code cleaner and less dependent on Unity's specific physics API.
2.  **Centralized Control:** All physics configurations and actions are managed in one place. This simplifies modifications (e.g., changing global gravity, default raycast layers, or even swapping physics engines).
3.  **Flexibility & Extensibility:** Allows for custom physics behaviors, logging, debugging visualizations, or even switching between different physics implementations (e.g., Unity's built-in vs. a custom solution) without altering client code.
4.  **Maintainability:** Easier to identify and fix physics-related issues when all logic is consolidated.
5.  **Testability:** A manager can be mocked or faked for unit testing purposes more easily than individual Rigidbody components.

---

### Step 1: Create the `PhysicsManager.cs` Script

This script will be the core of our pattern, acting as a singleton that handles all physics requests.

```csharp
using UnityEngine;
using System.Collections.Generic; // Not strictly needed for basic ops, but good practice for patterns

/// <summary>
/// PhysicsManager: The central hub for all physics-related operations in the game.
/// This class implements the Singleton pattern to ensure a single, globally accessible
/// instance that manages interactions with Unity's physics engine.
///
/// It provides a decoupled API for game objects to perform physics actions
/// (e.g., applying forces, raycasting, overlap checks) without directly interacting
/// with Unity's Rigidbody or Physics static class.
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    // --- Singleton Instance ---
    private static PhysicsManager _instance;

    /// <summary>
    /// Gets the singleton instance of the PhysicsManager.
    /// If an instance doesn't exist in the scene, it creates one dynamically.
    /// </summary>
    public static PhysicsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<PhysicsManager>();

                // If no instance exists, create a new GameObject and add the component
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(PhysicsManager).Name);
                    _instance = singletonObject.AddComponent<PhysicsManager>();
                    Debug.Log($"[PhysicsManager] Singleton instance created: {singletonObject.name}");
                }
            }
            return _instance;
        }
    }

    // --- Configuration Parameters (Exposed to Inspector) ---
    // These parameters allow designers/developers to tweak physics behavior
    // globally from a single location without modifying individual game object scripts.

    [Header("General Physics Settings")]
    [Tooltip("A global multiplier applied to all forces when using PhysicsManager.ApplyForce().")]
    [SerializeField]
    private float globalForceMultiplier = 1.0f;

    [Header("Raycast Settings")]
    [Tooltip("The default layer mask to use for raycasting if not specified by the caller.")]
    [SerializeField]
    private LayerMask defaultRaycastLayerMask = -1; // -1 means 'Everything'

    [Tooltip("The default maximum distance for raycasts if not specified by the caller.")]
    [SerializeField]
    private float defaultRaycastDistance = 100f;

    [Tooltip("Enable or disable debug drawing for raycasts performed through the manager.")]
    [SerializeField]
    private bool enableRaycastDebugDraw = true;

    [Tooltip("Color for successful raycast debug lines.")]
    [SerializeField]
    private Color raycastHitColor = Color.green;

    [Tooltip("Color for missed raycast debug lines.")]
    [SerializeField]
    private Color raycastMissColor = Color.red;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures only one instance of the PhysicsManager exists and persists across scenes.
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If a duplicate instance is found, destroy this one to maintain the singleton pattern.
            Debug.LogWarning($"[PhysicsManager] Duplicate instance found on '{gameObject.name}', destroying it. " +
                             "Ensure only one PhysicsManager exists in the scene.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // Keep the PhysicsManager alive across scene changes.
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[PhysicsManager] Initialized: {gameObject.name}");
    }

    /// <summary>
    /// This is where physics calculations should ideally occur, even if just delegating.
    /// For this example, we are primarily delegating to Unity's built-in physics,
    /// so no complex global logic is strictly needed here.
    /// However, this provides a hook for future custom physics updates or global force applications.
    /// </summary>
    private void FixedUpdate()
    {
        // Example: If you wanted to apply a custom global drag force to all managed rigidbodies:
        // foreach (var rb in activeManagedRigidbodies) { rb.AddForce(-rb.velocity * customGlobalDrag, ForceMode.Acceleration); }

        // This method can also be used for advanced debug visualizations that rely on physics frames.
    }

    /// <summary>
    /// Called when the MonoBehaviour is enabled.
    /// </summary>
    private void OnEnable()
    {
        // Any setup or subscriptions needed when the manager becomes active.
    }

    /// <summary>
    /// Called when the MonoBehaviour is disabled.
    /// </summary>
    private void OnDisable()
    {
        // Any cleanup or unsubscribing needed when the manager becomes inactive.
    }

    /// <summary>
    /// Used for drawing gizmos in the editor, useful for debugging physics areas.
    /// Note: Gizmos are drawn even if the GameObject is not selected.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Can be used to visualize default raycast distance, debug areas, etc.
        // if (Application.isPlaying && enableRaycastDebugDraw)
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f); // Simple indicator for manager's position
        // }
    }

    // --- Public Physics Operations ---
    // These methods provide the API for client scripts to interact with physics.

    /// <summary>
    /// Applies a force to a given Rigidbody. The force vector will be multiplied
    /// by the `globalForceMultiplier` defined in the PhysicsManager's settings.
    /// </summary>
    /// <param name="rigidbody">The Rigidbody component to apply force to.</param>
    /// <param name="force">The base force vector to apply.</param>
    /// <param name="mode">The force mode to use (e.g., Force, Impulse, VelocityChange). Defaults to Force.</param>
    public void ApplyForce(Rigidbody rigidbody, Vector3 force, ForceMode mode = ForceMode.Force)
    {
        if (rigidbody == null)
        {
            Debug.LogWarning("[PhysicsManager] Attempted to apply force to a null Rigidbody.");
            return;
        }
        rigidbody.AddForce(force * globalForceMultiplier, mode);
        // Can add logging or other custom logic here.
    }

    /// <summary>
    /// Applies a force at a specific position on a Rigidbody. The force vector
    /// will be multiplied by the `globalForceMultiplier`.
    /// </summary>
    /// <param name="rigidbody">The Rigidbody component to apply force to.</param>
    /// <param name="force">The base force vector to apply.</param>
    /// <param name="position">The position in world coordinates to apply the force.</param>
    /// <param name="mode">The force mode to use (e.g., Force, Impulse, VelocityChange). Defaults to Force.</param>
    public void ApplyForceAtPosition(Rigidbody rigidbody, Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
    {
        if (rigidbody == null)
        {
            Debug.LogWarning("[PhysicsManager] Attempted to apply force at position to a null Rigidbody.");
            return;
        }
        rigidbody.AddForceAtPosition(force * globalForceMultiplier, position, mode);
    }

    /// <summary>
    /// Sets the velocity of a Rigidbody directly. This operation bypasses the
    /// `globalForceMultiplier` as it's a direct velocity assignment, not a force application.
    /// </summary>
    /// <param name="rigidbody">The Rigidbody component to set velocity for.</param>
    /// <param name="velocity">The new velocity vector.</param>
    public void SetRigidbodyVelocity(Rigidbody rigidbody, Vector3 velocity)
    {
        if (rigidbody == null)
        {
            Debug.LogWarning("[PhysicsManager] Attempted to set velocity for a null Rigidbody.");
            return;
        }
        rigidbody.velocity = velocity;
    }

    /// <summary>
    /// Performs a raycast from an origin in a given direction.
    /// Uses `defaultRaycastDistance` and `defaultRaycastLayerMask` if not specified.
    /// Includes optional debug drawing based on `enableRaycastDebugDraw`.
    /// </summary>
    /// <param name="origin">The starting point of the ray in world coordinates.</param>
    /// <param name="direction">The direction vector of the ray.</param>
    /// <param name="hitInfo">Output parameter for detailed RaycastHit information.</param>
    /// <param name="distance">Maximum distance the ray should check. If null, `defaultRaycastDistance` is used.</param>
    /// <param name="layerMask">A LayerMask to filter which layers the ray should hit. If null, `defaultRaycastLayerMask` is used.</param>
    /// <param name="queryTriggerInteraction">Specifies whether to hit Triggers or not. Defaults to `QueryTriggerInteraction.UseGlobal`.</param>
    /// <returns>True if the ray hits a collider within the specified distance and layer mask, false otherwise.</returns>
    public bool PerformRaycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo,
                               float? distance = null, LayerMask? layerMask = null,
                               QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        // Use default values if optional parameters are not provided
        float actualDistance = distance ?? defaultRaycastDistance;
        LayerMask actualLayerMask = layerMask ?? defaultRaycastLayerMask;

        bool hit = Physics.Raycast(origin, direction, out hitInfo, actualDistance, actualLayerMask, queryTriggerInteraction);

        // Debug drawing for visualization in the editor
        if (enableRaycastDebugDraw)
        {
            Color drawColor = hit ? raycastHitColor : raycastMissColor;
            Debug.DrawRay(origin, direction * actualDistance, drawColor, 0.1f); // Draws for a short duration
            if (hit)
            {
                Debug.DrawLine(origin, hitInfo.point, drawColor, 0.1f);
            }
        }
        return hit;
    }

    /// <summary>
    /// Performs an OverlapSphere check to find colliders within a specified radius.
    /// Uses `defaultRaycastLayerMask` if not specified.
    /// </summary>
    /// <param name="position">The center of the sphere in world coordinates.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="layerMask">A LayerMask to filter which layers the sphere should hit. If null, `defaultRaycastLayerMask` is used.</param>
    /// <param name="queryTriggerInteraction">Specifies whether to hit Triggers or not. Defaults to `QueryTriggerInteraction.UseGlobal`.</param>
    /// <returns>An array of Colliders that overlap the sphere.</returns>
    public Collider[] PerformOverlapSphere(Vector3 position, float radius,
                                           LayerMask? layerMask = null,
                                           QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        LayerMask actualLayerMask = layerMask ?? defaultRaycastLayerMask;
        Collider[] colliders = Physics.OverlapSphere(position, radius, actualLayerMask, queryTriggerInteraction);

        // Minimal debug drawing for OverlapSphere. For a proper wire sphere, OnDrawGizmos is better.
        if (enableRaycastDebugDraw)
        {
            // Debug.DrawRay(position, Vector3.up * radius, colliders.Length > 0 ? raycastHitColor : raycastMissColor, 0.1f);
        }
        return colliders;
    }

    /// <summary>
    /// Performs an OverlapBox check to find colliders within a specified box volume.
    /// Uses `defaultRaycastLayerMask` if not specified.
    /// </summary>
    /// <param name="center">The center of the box in world coordinates.</param>
    /// <param name="halfExtents">Half the size of the box along each axis (X, Y, Z).</param>
    /// <param name="orientation">The rotation of the box. Defaults to identity (no rotation).</param>
    /// <param name="layerMask">A LayerMask to filter which layers the box should hit. If null, `defaultRaycastLayerMask` is used.</param>
    /// <param name="queryTriggerInteraction">Specifies whether to hit Triggers or not. Defaults to `QueryTriggerInteraction.UseGlobal`.</param>
    /// <returns>An array of Colliders that overlap the box.</returns>
    public Collider[] PerformOverlapBox(Vector3 center, Vector3 halfExtents,
                                        Quaternion orientation = default(Quaternion), LayerMask? layerMask = null,
                                        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        LayerMask actualLayerMask = layerMask ?? defaultRaycastLayerMask;
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, orientation, actualLayerMask, queryTriggerInteraction);

        if (enableRaycastDebugDraw)
        {
            // Similar to OverlapSphere, detailed debug drawing for OverlapBox is better in OnDrawGizmos.
        }
        return colliders;
    }

    // --- Example of Custom Physics Logic Hook (Illustrative) ---

    /// <summary>
    /// This method illustrates how the PhysicsManager can encapsulate and manage
    /// more complex, global physics behaviors beyond simple delegation.
    /// It could, for instance, switch between Unity's default gravity and a custom one.
    /// </summary>
    /// <param name="enabled">True to enable custom gravity, false to revert to default.</param>
    public void SetCustomGravityEnabled(bool enabled)
    {
        if (enabled)
        {
            // In a real scenario, this would set a custom gravity vector or
            // activate a system that applies custom gravity forces to registered rigidbodies.
            Physics.gravity = new Vector3(0, -20f, 0); // Directly changing Unity's global gravity
            Debug.Log("[PhysicsManager] Custom gravity enabled: (0, -20, 0). All physics objects are affected.");
        }
        else
        {
            Physics.gravity = new Vector3(0, -9.81f, 0); // Reset to Unity's default gravity
            Debug.Log("[PhysicsManager] Custom gravity disabled. Returning to Unity's default gravity: (0, -9.81, 0).");
        }
    }

    // Further extensions could include:
    // - Registering/unregistering specific Rigidbodies for custom force applications (e.g., wind zones).
    // - Managing custom collision response logic.
    // - Centralizing object pooling for physics effects (e.g., impact particles).
}
```

---

### Step 2: Create the `ExamplePhysicsUser.cs` Script

This script will demonstrate how a typical game object (e.g., a player character) would interact with the `PhysicsManager` instead of directly with Unity's physics API.

```csharp
using UnityEngine;

/// <summary>
/// ExamplePhysicsUser: A client script that demonstrates how to interact with the PhysicsManager.
/// Instead of directly calling Unity's Physics or Rigidbody methods, this script delegates
/// all physics-related operations (movement, jumping, ground checks, object detection)
/// to the centralized PhysicsManager.
///
/// This maintains decoupling and allows the PhysicsManager to manage global physics rules
/// and configurations transparently to the client.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures the GameObject has a Rigidbody component
public class ExamplePhysicsUser : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the player moves horizontally.")]
    [SerializeField]
    private float moveSpeed = 5f;
    [Tooltip("The upward force applied when the player jumps.")]
    [SerializeField]
    private float jumpForce = 8f;

    [Header("Ground Check Settings")]
    [Tooltip("The Transform representing the origin point for ground checks (e.g., bottom of character).")]
    [SerializeField]
    private Transform groundCheckOrigin;
    [Tooltip("The maximum distance to check downwards for ground.")]
    [SerializeField]
    private float groundCheckDistance = 0.6f;
    [Tooltip("The LayerMask for objects considered 'ground'.")]
    [SerializeField]
    private LayerMask groundLayer;

    [Header("Interaction Detection Settings")]
    [Tooltip("The Transform representing the origin point for interaction detection (e.g., character's center).")]
    [SerializeField]
    private Transform sphereCheckOrigin;
    [Tooltip("The radius for the OverlapSphere to detect nearby interactable objects.")]
    [SerializeField]
    private float sphereCheckRadius = 1.0f;
    [Tooltip("The LayerMask for objects considered 'interactable'.")]
    [SerializeField]
    private LayerMask interactionLayer;

    private Rigidbody _rigidbody;
    private bool _isGrounded;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Retrieves the Rigidbody component and performs initial checks.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("[ExamplePhysicsUser] Rigidbody component not found. PhysicsUser requires a Rigidbody.", this);
        }
        if (groundCheckOrigin == null)
        {
            Debug.LogWarning("[ExamplePhysicsUser] groundCheckOrigin is not set. Using GameObject's transform as fallback.", this);
            groundCheckOrigin = transform;
        }
        if (sphereCheckOrigin == null)
        {
            Debug.LogWarning("[ExamplePhysicsUser] sphereCheckOrigin is not set. Using GameObject's transform as fallback.", this);
            sphereCheckOrigin = transform;
        }
    }

    /// <summary>
    /// Update is called once per frame. Handles input and general logic.
    /// </summary>
    private void Update()
    {
        HandleMovementInput();
        CheckGroundStatus();
        DetectNearbyObjects();

        // Example: Toggling custom gravity via the manager
        if (Input.GetKeyDown(KeyCode.G))
        {
            // This demonstrates how a game object can request a global physics change.
            // The PhysicsManager handles the implementation details of changing gravity.
            // We check the current gravity to determine the toggle state.
            bool currentGravityIsCustom = Physics.gravity.y < -10f; // Simple check, adjust as needed
            PhysicsManager.Instance.SetCustomGravityEnabled(!currentGravityIsCustom);
        }
    }

    // --- Physics-Related Actions (Delegated to PhysicsManager) ---

    /// <summary>
    /// Handles player input for horizontal movement and jumping.
    /// All physics operations are delegated to the PhysicsManager.
    /// </summary>
    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate desired horizontal velocity
        Vector3 targetHorizontalVelocity = (transform.right * horizontal + transform.forward * vertical).normalized * moveSpeed;

        // Maintain current Y velocity for gravity/jumps, and set the new horizontal velocity.
        // We use PhysicsManager.Instance.SetRigidbodyVelocity to apply this.
        Vector3 newVelocity = new Vector3(targetHorizontalVelocity.x, _rigidbody.velocity.y, targetHorizontalVelocity.z);
        PhysicsManager.Instance.SetRigidbodyVelocity(_rigidbody, newVelocity);

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            // Instead of _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // Delegate to PhysicsManager for force application.
            // The manager's globalForceMultiplier would affect this force.
            PhysicsManager.Instance.ApplyForce(_rigidbody, Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("[ExamplePhysicsUser] Jump initiated via PhysicsManager.");
        }
    }

    /// <summary>
    /// Performs a raycast to determine if the player is currently grounded.
    /// Delegates the raycast operation to the PhysicsManager.
    /// </summary>
    private void CheckGroundStatus()
    {
        // Instead of Physics.Raycast(groundCheckOrigin.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
        // Delegate to PhysicsManager for raycasting. This benefits from manager-defined defaults and debug drawing.
        RaycastHit hit;
        _isGrounded = PhysicsManager.Instance.PerformRaycast(
            groundCheckOrigin.position,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer // Use the specific ground layer mask for this check
        );

        // Optional debug logging
        // if (_isGrounded)
        // {
        //     Debug.Log($"[ExamplePhysicsUser] Grounded on: {hit.collider.name}");
        // }
    }

    /// <summary>
    /// Performs an overlap sphere check to detect nearby interactable objects.
    /// Delegates the overlap operation to the PhysicsManager.
    /// </summary>
    private void DetectNearbyObjects()
    {
        // Instead of Physics.OverlapSphere(sphereCheckOrigin.position, sphereCheckRadius, interactionLayer);
        // Delegate to PhysicsManager for overlap checks.
        Collider[] hitColliders = PhysicsManager.Instance.PerformOverlapSphere(
            sphereCheckOrigin.position,
            sphereCheckRadius,
            interactionLayer // Use the specific interaction layer mask for this check
        );

        if (hitColliders.Length > 0)
        {
            // Debug.Log($"[ExamplePhysicsUser] Found {hitColliders.Length} interactable objects via OverlapSphere.");
            // Example: Iterate through detected objects and perform interaction logic
            // foreach (var collider in hitColliders)
            // {
            //     Debug.Log($"[ExamplePhysicsUser] Nearby interactable: {collider.name}");
            //     // collider.GetComponent<IInteractable>()?.Highlight();
            // }
        }
    }

    /// <summary>
    /// Draws debug gizmos in the editor to visualize ground checks and interaction spheres.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Visualize the ground check ray
        if (groundCheckOrigin != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundCheckOrigin.position, groundCheckOrigin.position + Vector3.down * groundCheckDistance);
        }

        // Visualize the interaction detection sphere
        if (sphereCheckOrigin != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(sphereCheckOrigin.position, sphereCheckRadius);
        }
    }
}
```

---

### How to Implement and Test in Unity:

1.  **Create PhysicsManager GameObject:**
    *   In your Unity project, create an empty GameObject in your scene (e.g., named `PhysicsManager`).
    *   Attach the `PhysicsManager.cs` script to this GameObject.
    *   **Configure:** In the Inspector for `PhysicsManager`, adjust the `Global Force Multiplier`, `Default Raycast Layer Mask` (e.g., select "Everything"), `Default Raycast Distance`, and `Enable Raycast Debug Draw` as needed. Enabling debug draw is highly recommended for visualization.

2.  **Create Player GameObject:**
    *   Create a 3D object (e.g., Capsule or Cube) in your scene (e.g., named `Player`).
    *   Add a `Rigidbody` component to the `Player` GameObject.
        *   Ensure "Use Gravity" is checked.
        *   Set "Drag" and "Angular Drag" to values like 0 or 1 for basic movement.
    *   Attach the `ExamplePhysicsUser.cs` script to the `Player` GameObject.
    *   **Configure:**
        *   Drag the `Player` GameObject itself into the `Ground Check Origin` and `Sphere Check Origin` fields (or create child empty GameObjects for more precise control, e.g., an empty at the bottom of the capsule for ground check).
        *   **Create Layers:** In Unity, go to `Layers` (top right of Inspector) -> `Add Layer...`. Create layers like "Ground" and "Interactable".
        *   Set the `Ground Layer` and `Interaction Layer` fields in `ExamplePhysicsUser` to these newly created layers.
        *   Adjust `Move Speed`, `Jump Force`, `Ground Check Distance`, and `Sphere Check Radius`.

3.  **Create Ground and Interactable Objects:**
    *   **Ground:** Create a 3D Plane or Cube named `Ground`. Assign it to the "Ground" layer.
    *   **Interactable Object (Optional):** Create a 3D Cube or Sphere. Assign it to the "Interactable" layer. Place it near your player.

4.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Use WASD or arrow keys to move your player.
    *   Press Space to jump.
    *   Observe the debug rays for ground checks (green if grounded, red if not).
    *   Press 'G' to toggle custom gravity (watch your player's fall speed change).
    *   Move near an "Interactable" object to see if it's detected by the overlap sphere (visible as a blue wire sphere in Gizmos).

This setup provides a complete, working example of the PhysicsManagerPattern, ready to be dropped into a Unity project, offering clear benefits for managing physics in a robust and flexible way.