// Unity Design Pattern Example: SoftBodyPhysicsSystem
// This script demonstrates the SoftBodyPhysicsSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SoftBodyPhysicsSystem' design pattern, as interpreted here, focuses on creating a modular and extensible system for simulating soft bodies in Unity. It separates the global management of physics updates from the individual soft body components, allowing for:

1.  **Centralized Control (System):** A single manager component (`SoftBodyPhysicsSystem`) that orchestrates the physics updates for all soft bodies. This ensures consistent timesteps and global parameters (like gravity).
2.  **Decoupled Components (SoftBody):** Each soft body (`SoftBody` component) is responsible for its own structure (nodes, springs) and physics logic, but it *registers* with the central system. It doesn't need to manage its own `FixedUpdate` or know about other soft bodies directly.
3.  **Scalability:** Easily add or remove soft bodies without changing the core system logic.
4.  **Maintainability:** Changes to the global physics loop or parameters are confined to the system, while changes to a specific soft body's behavior are contained within its component.

This approach combines elements of the **Manager Pattern**, **Component Pattern**, and **Observer Pattern** (where soft bodies "observe" the system's call to update).

---

## SoftBodyPhysicsSystem Design Pattern Implementation in Unity

This example provides two C# scripts:

1.  **`SoftBodyPhysicsSystem.cs`**: The central manager. It's a `MonoBehaviour` singleton (for easy access) that keeps track of all active `SoftBody` components and calls their `Simulate` method during `FixedUpdate`.
2.  **`SoftBody.cs`**: The individual soft body component. It's a `MonoBehaviour` that manages its own nodes (vertices), springs (connections), and updates its mesh based on physics calculations. It registers and deregisters itself with the `SoftBodyPhysicsSystem`.

### How to Use This Example

1.  **Create the Physics System Manager:**
    *   Create an empty GameObject in your Unity scene (e.g., named "SoftBodySystemManager").
    *   Attach the `SoftBodyPhysicsSystem.cs` script to it.
    *   Adjust `Global Gravity` in its Inspector if desired.

2.  **Create a Soft Body Object:**
    *   Create any 3D object in your scene (e.g., a "Cube", "Sphere", or a custom mesh). Name it appropriately (e.g., "MySoftCube").
    *   Ensure it has a `MeshFilter` and `MeshRenderer` component (Unity's default 3D objects already have these).
    *   Attach the `SoftBody.cs` script to it.
    *   **Crucially:** The `SoftBody` script will clone the assigned mesh in `Awake` to avoid modifying the original asset.
    *   Adjust the parameters in the `SoftBody`'s Inspector:
        *   `Mass Multiplier`: Affects the overall weight.
        *   `Spring Stiffness`: How rigid or squishy the soft body is.
        *   `Spring Damping`: Reduces oscillations in the springs.
        *   `Global Damping`: Reduces overall node velocity to prevent infinite motion.
        *   `Ground Height`: A simple Y-coordinate for an infinite ground plane collision.
        *   `Bounciness`: How much the soft body bounces off the ground.
        *   `Ground Friction`: Reduces horizontal velocity on ground contact.

3.  **Run the Scene:**
    *   Press Play. You should see your object deform under gravity and react to collisions with the defined ground plane.
    *   You can duplicate `SoftBody` objects, and they will all be managed by the same `SoftBodyPhysicsSystem`.
    *   For debugging, enable `OnDrawGizmos` in the `SoftBody.cs` script to visualize nodes and springs in the Scene view.

---

### `SoftBodyPhysicsSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The SoftBodyPhysicsSystem acts as the central manager (System/Facade pattern)
/// for all SoftBody components in the scene. It orchestrates their simulation
/// updates in a consistent and centralized manner.
/// </summary>
/// <remarks>
/// This class embodies the 'System' part of the SoftBodyPhysicsSystem pattern.
/// It provides a single point of access and control for the soft body simulations,
/// promoting loose coupling between individual soft bodies and the global physics loop.
/// SoftBody components register themselves here, allowing the system to manage them.
/// </remarks>
public class SoftBodyPhysicsSystem : MonoBehaviour
{
    // Singleton-like access: Provides a global point of access to the system.
    // This allows SoftBody components to easily find and register with the system.
    public static SoftBodyPhysicsSystem Instance { get; private set; }

    [Header("Global Physics Settings")]
    [Tooltip("The global acceleration due to gravity applied to all soft bodies.")]
    public Vector3 globalGravity = new Vector3(0, -9.81f, 0);

    // A list of all active SoftBody components currently managed by this system.
    private List<SoftBody> registeredSoftBodies = new List<SoftBody>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to enforce the singleton pattern.
    /// </summary>
    private void Awake()
    {
        // Ensure only one instance of SoftBodyPhysicsSystem exists in the scene.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple SoftBodyPhysicsSystem instances found. Destroying duplicate.", this);
            Destroy(gameObject); // Destroy any subsequent instances.
        }
    }

    /// <summary>
    /// Called once per physics step. This is the core of the system,
    /// where it drives the simulation for all registered soft bodies.
    /// </summary>
    private void FixedUpdate()
    {
        // Unity's FixedUpdate runs at a fixed timestep, ideal for physics calculations.
        float deltaTime = Time.fixedDeltaTime;

        // Iterate through all registered soft bodies and instruct them to update their physics.
        // This centralizes the simulation loop, ensuring all soft bodies are updated
        // consistently and in the correct phase of the game loop. Each SoftBody then
        // performs its own internal calculations.
        foreach (SoftBody softBody in registeredSoftBodies)
        {
            softBody.Simulate(deltaTime, globalGravity);
        }
    }

    /// <summary>
    /// Registers a SoftBody component with the system.
    /// SoftBody components call this when they become active (e.g., in OnEnable).
    /// </summary>
    /// <param name="softBody">The SoftBody component to register.</param>
    public void RegisterSoftBody(SoftBody softBody)
    {
        if (!registeredSoftBodies.Contains(softBody))
        {
            registeredSoftBodies.Add(softBody);
            Debug.Log($"SoftBody '{softBody.name}' registered with the system.");
        }
    }

    /// <summary>
    /// Deregisters a SoftBody component from the system.
    /// SoftBody components call this when they become inactive or are destroyed (e.g., in OnDisable).
    /// </summary>
    /// <param name="softBody">The SoftBody component to deregister.</param>
    public void DeregisterSoftBody(SoftBody softBody)
    {
        if (registeredSoftBodies.Remove(softBody))
        {
            Debug.Log($"SoftBody '{softBody.name}' deregistered from the system.");
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Cleans up the singleton instance.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

### `SoftBody.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single soft body object in the Unity scene.
/// This component stores the state and logic for its own soft body simulation.
/// </summary>
/// <remarks>
/// This class embodies the 'SoftBody' part of the SoftBodyPhysicsSystem pattern.
/// It acts as a specialized component that manages its own nodes, springs, and mesh.
/// It delegates its physics update calls to the central <see cref="SoftBodyPhysicsSystem"/>,
/// promoting a clean separation of concerns: the system manages *when* to update,
/// and the soft body knows *how* to update itself.
/// </remarks>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // Soft bodies need a mesh to deform and render.
// [ExecuteInEditMode] // Uncomment for live mesh updates in editor (useful for setup), but be cautious with performance for complex meshes.
public class SoftBody : MonoBehaviour
{
    [Header("Soft Body Physics Properties")]
    [Tooltip("Overall mass multiplier for the soft body nodes. Higher values mean more inertia.")]
    [SerializeField] private float massMultiplier = 1.0f;
    [Tooltip("The stiffness of the springs connecting the nodes. Higher values mean less deformation.")]
    [Range(0.1f, 200f)][SerializeField] private float springStiffness = 50.0f;
    [Tooltip("Damping applied to spring forces. Reduces oscillation between connected nodes.")]
    [Range(0.01f, 10f)][SerializeField] private float springDamping = 0.5f;
    [Tooltip("Overall damping applied to node velocities. Prevents infinite motion and stabilizes the system.")]
    [Range(0.0f, 0.99f)][SerializeField] private float globalDamping = 0.95f;

    [Header("Collision Properties")]
    [Tooltip("The Y-coordinate of an infinite ground plane for basic collision detection.")]
    [SerializeField] private float groundHeight = 0f;
    [Tooltip("How 'bouncy' the soft body is on collision. 0 for no bounce, 1 for full bounce.")]
    [Range(0.0f, 1.0f)][SerializeField] private float bounciness = 0.2f;
    [Tooltip("Friction applied to nodes when colliding with the ground, reducing horizontal velocity.")]
    [Range(0.0f, 1.0f)][SerializeField] private float groundFriction = 0.5f;

    // --- Internal Data Structures for the Soft Body ---

    /// <summary>
    /// Represents a single node (or particle) in the soft body's physics simulation.
    /// Each node has position, mass, and accumulates forces.
    /// </summary>
    private class SoftBodyNode
    {
        public Vector3 currentPosition;
        public Vector3 previousPosition; // Essential for Verlet integration.
        public Vector3 forceAccumulator; // Sum of all forces acting on this node.
        public float mass;
        public int[] originalMeshVertexIndices; // Stores which original mesh vertices this node corresponds to.

        public SoftBodyNode(Vector3 initialPosition, float nodeMass, params int[] vertexIndices)
        {
            currentPosition = initialPosition;
            previousPosition = initialPosition; // Start with no initial velocity for Verlet.
            forceAccumulator = Vector3.zero;
            mass = nodeMass;
            originalMeshVertexIndices = vertexIndices;
        }

        public void AddForce(Vector3 force)
        {
            forceAccumulator += force;
        }

        public void ResetForces()
        {
            forceAccumulator = Vector3.zero;
        }
    }

    /// <summary>
    /// Represents a spring connection between two <see cref="SoftBodyNode"/>s.
    /// Springs exert forces to maintain a 'rest length', simulating elasticity.
    /// </summary>
    private class SoftBodySpring
    {
        public SoftBodyNode nodeA;
        public SoftBodyNode nodeB;
        public float restLength; // The desired length of the spring.
        public float stiffness;  // How strong the spring force is.
        public float damping;    // How much the spring's oscillation is reduced.

        public SoftBodySpring(SoftBodyNode a, SoftBodyNode b, float initialLength, float springStiffness, float springDamping)
        {
            nodeA = a;
            nodeB = b;
            restLength = initialLength;
            stiffness = springStiffness;
            damping = springDamping;
        }
    }

    // --- Soft Body Component References ---
    private SoftBodyNode[] nodes;         // All physics nodes of this soft body.
    private SoftBodySpring[] springs;     // All spring connections.
    private Mesh mesh;                    // The runtime-editable mesh for deformation.
    private MeshFilter meshFilter;        // Component to get and set the mesh.
    private Vector3[] initialVertices;    // Stores the original vertex positions of the mesh.

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the mesh, nodes, and springs of the soft body.
    /// </summary>
    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("SoftBody requires a MeshFilter with a mesh assigned.", this);
            enabled = false; // Disable component if requirements are not met.
            return;
        }

        // It's crucial to create an *instance* of the mesh.
        // Directly modifying meshFilter.sharedMesh would modify the original asset!
        mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = mesh; // Assign the new instance to the MeshFilter.

        initialVertices = mesh.vertices; // Store original vertex positions.

        // Build the internal structure of nodes and springs from the mesh.
        InitializeSoftBodyStructure();
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Registers this soft body with the central physics system.
    /// </summary>
    private void OnEnable()
    {
        // This is a key part of the 'SoftBodyPhysicsSystem' pattern.
        // The soft body self-registers, informing the system it's ready for simulation.
        if (SoftBodyPhysicsSystem.Instance != null)
        {
            SoftBodyPhysicsSystem.Instance.RegisterSoftBody(this);
        }
        else
        {
            Debug.LogWarning("SoftBodyPhysicsSystem not found in scene. SoftBody will not be simulated.", this);
        }
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Deregisters this soft body from the central physics system.
    /// </summary>
    private void OnDisable()
    {
        // When the soft body is no longer active, it notifies the system to stop managing it.
        if (SoftBodyPhysicsSystem.Instance != null)
        {
            SoftBodyPhysicsSystem.Instance.DeregisterSoftBody(this);
        }
    }

    /// <summary>
    /// Initializes the nodes and springs of the soft body based on its mesh vertices and triangles.
    /// This implementation treats each mesh vertex as a physics node and connects adjacent vertices.
    /// </summary>
    private void InitializeSoftBodyStructure()
    {
        // 1. Node Creation: Each original mesh vertex becomes a physics node.
        nodes = new SoftBodyNode[initialVertices.Length];
        // Distribute the total mass across all nodes.
        float nodeMass = massMultiplier / initialVertices.Length;

        for (int i = 0; i < initialVertices.Length; i++)
        {
            // Convert local mesh vertex position to world position for simulation start.
            // Physics calculations are often simpler in world space.
            nodes[i] = new SoftBodyNode(transform.TransformPoint(initialVertices[i]), nodeMass, i);
        }

        // 2. Spring Creation: Connect nodes based on the mesh's triangles.
        // This creates a network of internal springs.
        List<SoftBodySpring> springList = new List<SoftBodySpring>();
        // Use a dictionary to avoid creating duplicate springs between the same two nodes.
        Dictionary<Vector2Int, SoftBodySpring> existingSprings = new Dictionary<Vector2Int, SoftBodySpring>();

        int[] triangles = mesh.triangles;
        // Iterate through all triangles (each triangle has 3 vertex indices).
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int idx1 = triangles[i];
            int idx2 = triangles[i + 1];
            int idx3 = triangles[i + 2];

            // Add springs for each edge of the triangle.
            AddSpringIfNotExists(springList, existingSprings, nodes[idx1], nodes[idx2]);
            AddSpringIfNotExists(springList, existingSprings, nodes[idx2], nodes[idx3]);
            AddSpringIfNotExists(springList, existingSprings, nodes[idx3], nodes[idx1]);
        }

        springs = springList.ToArray(); // Convert the list of springs to an array.
        Debug.Log($"Initialized Soft Body '{name}' with {nodes.Length} nodes and {springs.Length} springs.");
    }

    /// <summary>
    /// Helper method to add a spring between two nodes if it doesn't already exist.
    /// This prevents redundant springs, which can lead to instability or performance issues.
    /// </summary>
    private void AddSpringIfNotExists(List<SoftBodySpring> springList, Dictionary<Vector2Int, SoftBodySpring> existingSprings, SoftBodyNode n1, SoftBodyNode n2)
    {
        // Create a unique key for the pair of nodes, regardless of order (e.g., nodeA-nodeB is same as nodeB-nodeA).
        // Using array indices as a simple identifier for the nodes.
        int nodeIndex1 = System.Array.IndexOf(nodes, n1);
        int nodeIndex2 = System.Array.IndexOf(nodes, n2);
        Vector2Int key = new Vector2Int(Mathf.Min(nodeIndex1, nodeIndex2), Mathf.Max(nodeIndex1, nodeIndex2));

        if (!existingSprings.ContainsKey(key))
        {
            // Calculate the initial distance between nodes to set the spring's rest length.
            float initialDistance = Vector3.Distance(n1.currentPosition, n2.currentPosition);
            SoftBodySpring newSpring = new SoftBodySpring(n1, n2, initialDistance, springStiffness, springDamping);
            springList.Add(newSpring);
            existingSprings.Add(key, newSpring);
        }
    }


    /// <summary>
    /// The core simulation method for this soft body, called by the <see cref="SoftBodyPhysicsSystem"/>.
    /// This method performs one step of the soft body physics simulation.
    /// </summary>
    /// <param name="deltaTime">The fixed time step for the simulation (from Time.fixedDeltaTime).</param>
    /// <param name="gravity">The global gravity vector (from SoftBodyPhysicsSystem).</param>
    public void Simulate(float deltaTime, Vector3 gravity)
    {
        // 1. Reset forces for all nodes at the beginning of each simulation step.
        foreach (SoftBodyNode node in nodes)
        {
            node.ResetForces();
        }

        // 2. Apply external forces (e.g., gravity) to each node.
        foreach (SoftBodyNode node in nodes)
        {
            node.AddForce(gravity * node.mass);
            // Additional external forces (wind, explosions, etc.) could be added here.
        }

        // 3. Apply internal forces (spring forces) between connected nodes.
        foreach (SoftBodySpring spring in springs)
        {
            Vector3 direction = spring.nodeB.currentPosition - spring.nodeA.currentPosition;
            float currentLength = direction.magnitude;

            if (currentLength == 0) continue; // Avoid division by zero if nodes are perfectly superimposed.

            Vector3 normalizedDirection = direction / currentLength; // Unit vector from A to B.

            // Hooke's Law for spring force: F = -k * x
            // x is the displacement (currentLength - restLength)
            float displacement = currentLength - spring.restLength;
            Vector3 springForce = normalizedDirection * (spring.stiffness * displacement);

            // Damping force to reduce oscillations. Proportional to relative velocity.
            // For Verlet, we approximate velocity from (current - previous) / dt.
            Vector3 velocityA = (spring.nodeA.currentPosition - spring.nodeA.previousPosition) / deltaTime;
            Vector3 velocityB = (spring.nodeB.currentPosition - spring.nodeB.previousPosition) / deltaTime;
            Vector3 relativeVelocity = velocityB - velocityA;

            // Damping acts opposite to the relative velocity component along the spring direction.
            Vector3 dampingForce = normalizedDirection * (Vector3.Dot(relativeVelocity, normalizedDirection) * spring.damping);

            // Apply forces: Spring pulls/pushes, damping resists relative motion.
            // NodeA gets force in direction of B, NodeB gets force in direction of A.
            spring.nodeA.AddForce(springForce + dampingForce);
            spring.nodeB.AddForce(-springForce - dampingForce); // Equal and opposite force.
        }

        // 4. Integrate (update positions and velocities using Verlet Integration).
        // Verlet integration is often preferred for spring-mass systems due to its stability.
        foreach (SoftBodyNode node in nodes)
        {
            if (node.mass <= 0) continue; // Skip massless nodes to prevent division by zero.

            // Calculate acceleration: a = F/m
            Vector3 acceleration = node.forceAccumulator / node.mass;

            // Verlet integration formula: next_pos = 2 * current_pos - prev_pos + acceleration * dt^2
            // Global damping is incorporated here to reduce velocity over time.
            Vector3 nextPosition = node.currentPosition * (2f - globalDamping) - node.previousPosition * (1f - globalDamping) + acceleration * (deltaTime * deltaTime);

            // Update positions for the next step.
            node.previousPosition = node.currentPosition;
            node.currentPosition = nextPosition;
        }

        // 5. Apply constraints and handle collisions.
        HandleCollisions();

        // 6. Update the mesh visually to reflect the new node positions.
        UpdateMeshRepresentation();
    }

    /// <summary>
    /// Handles basic collision detection and response for the soft body nodes.
    /// Currently implements a simple collision with an infinite ground plane.
    /// </summary>
    private void HandleCollisions()
    {
        foreach (SoftBodyNode node in nodes)
        {
            // Simple ground plane collision: if node is below groundHeight, push it back.
            if (node.currentPosition.y < groundHeight)
            {
                // Calculate approximate velocity for collision response.
                // Velocity is needed to determine bounce and friction.
                Vector3 velocity = (node.currentPosition - node.previousPosition) / Time.fixedDeltaTime;

                // Move node exactly to the ground height.
                node.currentPosition.y = groundHeight;

                // Reflect vertical velocity and apply bounciness.
                velocity.y *= -bounciness;

                // Apply friction in the horizontal (XZ) plane.
                velocity.x *= (1.0f - groundFriction);
                velocity.z *= (1.0f - groundFriction);

                // Update previous position based on the new (collided) velocity for Verlet integration.
                // This correctly transfers the collision response into the Verlet system.
                node.previousPosition = node.currentPosition - velocity * Time.fixedDeltaTime;
            }
        }
    }

    /// <summary>
    /// Updates the MeshFilter's mesh with the new positions of the physics nodes.
    /// This visually reflects the deformation of the soft body in the scene.
    /// </summary>
    private void UpdateMeshRepresentation()
    {
        if (mesh == null || nodes == null || nodes.Length == 0) return;

        // Get the current vertices array from the mesh.
        Vector3[] vertices = mesh.vertices;

        // Iterate through all physics nodes.
        for (int i = 0; i < nodes.Length; i++)
        {
            // For each mesh vertex associated with this node, update its position.
            // Convert the world-space node position back to the mesh's local space.
            foreach (int vertexIndex in nodes[i].originalMeshVertexIndices)
            {
                vertices[vertexIndex] = transform.InverseTransformPoint(nodes[i].currentPosition);
            }
        }

        // Assign the updated vertices back to the mesh.
        mesh.vertices = vertices;

        // Recalculate normals to ensure correct lighting on the deformed mesh.
        mesh.RecalculateNormals();
        // Recalculate bounds for efficient culling and other Unity systems.
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Draws debugging gizmos in the editor to visualize the soft body's nodes and springs.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Visualize nodes as blue spheres.
        if (nodes != null)
        {
            Gizmos.color = Color.blue;
            foreach (SoftBodyNode node in nodes)
            {
                Gizmos.DrawSphere(node.currentPosition, 0.05f);
            }
        }

        // Visualize springs as green lines.
        if (springs != null)
        {
            Gizmos.color = Color.green;
            foreach (SoftBodySpring spring in springs)
            {
                Gizmos.DrawLine(spring.nodeA.currentPosition, spring.nodeB.currentPosition);
            }
        }
    }
}
```