// Unity Design Pattern Example: ClothSimulationSystem
// This script demonstrates the ClothSimulationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **ClothSimulationSystem** design pattern in Unity. This pattern, while not one of the classic Gang of Four patterns, is a practical approach for managing and orchestrating multiple, independent simulations (like cloth, fluid, or particle systems) within a game engine.

**The Core Idea:**
The `ClothSimulationSystem` pattern involves a central manager (`ClothSimulationSystem`) that knows *when* to update various cloth simulations, and individual cloth components (`SimpleClothSimulator`) that know *how* to simulate their specific piece of cloth. This separates the concerns of global orchestration from local simulation logic.

---

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Used for cleaner list operations, though not strictly required for this simple example

// --- 1. IClothSimulator Interface ---
// This interface defines the contract for any component that wants to be
// recognized and managed by the ClothSimulationSystem.
// By depending on an interface, the system achieves loose coupling:
// it doesn't need to know the concrete type of cloth, only that it can
// be initialized and simulated.
public interface IClothSimulator
{
    /// <summary>
    /// Initializes the cloth's internal state (mesh, particles, springs).
    /// Called by the ClothSimulationSystem when the cloth is registered.
    /// </summary>
    void InitializeCloth();

    /// <summary>
    /// Performs one step of the cloth simulation.
    /// Called by the ClothSimulationSystem during its fixed update loop.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last simulation step.</param>
    /// <param name="globalGravity">A global gravity vector applied to all cloths.</param>
    /// <param name="simulationIterations">Number of internal iterations for constraint solving.</param>
    void SimulateCloth(float deltaTime, Vector3 globalGravity, int simulationIterations);

    // You could add other methods here if the system needs to interact more deeply,
    // e.g., SetWindForce(Vector3 wind), GetClothState(), PauseSimulation().
}

// --- 2. ClothSimulationSystem (The Manager/System) ---
// This is the central component of the pattern. It's a MonoBehaviour that:
// - Acts as a singleton for easy global access.
// - Maintains a list of all registered IClothSimulator instances.
// - Iterates through the registered cloths in its FixedUpdate and triggers their simulation.
// - Provides global parameters (like gravity) that apply to all managed cloths.
public class ClothSimulationSystem : MonoBehaviour
{
    // Singleton instance to allow easy access from other parts of the application.
    public static ClothSimulationSystem Instance { get; private set; }

    [Header("Global Simulation Settings")]
    [Tooltip("Global gravity vector applied to all cloths managed by this system.")]
    public Vector3 globalGravity = new Vector3(0, -9.81f, 0);
    [Tooltip("Number of iterations per FixedUpdate step for spring constraints. " +
             "Higher value means more accurate constraint satisfaction but higher computational cost.")]
    [Range(1, 20)]
    public int simulationIterations = 8;
    [Tooltip("If true, this system will persist across scene loads.")]
    public bool dontDestroyOnLoad = false;

    // A list to hold all active IClothSimulator components.
    private readonly List<IClothSimulator> _registeredCloths = new List<IClothSimulator>();
    // A HashSet for efficient O(1) checking if a cloth is already registered, preventing duplicates.
    private readonly HashSet<IClothSimulator> _registeredClothSet = new HashSet<IClothSimulator>();

    // --- Singleton Initialization ---
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple ClothSimulationSystem instances found! Destroying duplicate.");
            Destroy(gameObject); // Ensures only one instance exists.
        }
    }

    // FixedUpdate is the ideal place for physics calculations, as it runs at a fixed timestep.
    void FixedUpdate()
    {
        // The core orchestration logic: iterate and trigger simulation for each registered cloth.
        // The ClothSimulationSystem doesn't know *how* each cloth simulates, only *that* it can.
        foreach (IClothSimulator cloth in _registeredCloths)
        {
            cloth.SimulateCloth(Time.fixedDeltaTime, globalGravity, simulationIterations);
        }
    }

    // --- Public API for Cloth Registration ---

    /// <summary>
    /// Registers a cloth simulator with the system.
    /// The registered cloth will then be updated automatically by the system during FixedUpdate.
    /// </summary>
    /// <param name="cloth">The IClothSimulator instance to register.</param>
    public void RegisterCloth(IClothSimulator cloth)
    {
        if (cloth == null)
        {
            Debug.LogWarning("Attempted to register a null cloth simulator.");
            return;
        }

        if (!_registeredClothSet.Contains(cloth))
        {
            _registeredCloths.Add(cloth);
            _registeredClothSet.Add(cloth); // Add to hash set for quick duplicate checking
            Debug.Log($"Cloth registered: {cloth.GetType().Name} (Total: {_registeredCloths.Count})");
            // Important: Initialize the cloth immediately upon registration.
            // This ensures it's ready for its first simulation step.
            cloth.InitializeCloth();
        }
        else
        {
            Debug.LogWarning($"Attempted to register an already registered cloth: {cloth.GetType().Name}");
        }
    }

    /// <summary>
    /// Deregisters a cloth simulator from the system.
    /// It will no longer be updated by the system.
    /// </summary>
    /// <param name="cloth">The IClothSimulator instance to deregister.</param>
    public void DeregisterCloth(IClothSimulator cloth)
    {
        if (cloth == null)
        {
            Debug.LogWarning("Attempted to deregister a null cloth simulator.");
            return;
        }

        if (_registeredClothSet.Contains(cloth))
        {
            _registeredCloths.Remove(cloth);
            _registeredClothSet.Remove(cloth); // Remove from hash set
            Debug.Log($"Cloth deregistered: {cloth.GetType().Name} (Total: {_registeredCloths.Count})");
        }
        else
        {
            Debug.LogWarning($"Attempted to deregister an unknown cloth: {cloth.GetType().Name}");
        }
    }

    // --- Cleanup ---
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}


// --- 3. SimpleClothSimulator (Concrete Cloth Component) ---
// This class is a concrete implementation of IClothSimulator.
// Each instance of this MonoBehaviour represents a single, independent piece of cloth
// with its own mesh, physics properties, and simulation logic.
// It manages its own state and how it interacts with the simulation parameters
// provided by the ClothSimulationSystem.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // Ensures essential components are present
public class SimpleClothSimulator : MonoBehaviour, IClothSimulator
{
    [Header("Cloth Mesh Settings")]
    [Tooltip("Number of subdivisions along the width (horizontal) of the cloth.")]
    [Range(2, 50)] public int widthSegments = 10;
    [Tooltip("Number of subdivisions along the height (vertical) of the cloth.")]
    [Range(2, 50)] public int heightSegments = 10;
    public float clothWidth = 2f;
    public float clothHeight = 2f;

    [Header("Physics Settings")]
    [Tooltip("Mass assigned to each individual particle (vertex) of the cloth.")]
    public float massPerPoint = 0.1f;
    [Tooltip("Stiffness for springs connecting immediately adjacent particles (horizontal and vertical).")]
    public float structuralStiffness = 100f;
    [Tooltip("Stiffness for springs connecting diagonally adjacent particles.")]
    public float shearStiffness = 80f;
    [Tooltip("Stiffness for springs connecting particles two segments apart (helps resist bending).")]
    public float bendingStiffness = 50f;
    [Tooltip("A factor to reduce particle velocity each step, simulating drag/friction.")]
    [Range(0f, 0.99f)] public float dampingFactor = 0.05f;
    [Tooltip("Drag coefficient for air resistance. Higher values cause more resistance.")]
    [Range(0f, 0.1f)] public float airDrag = 0.01f;

    [Header("Pinning Settings")]
    [Tooltip("Indices of particles (vertices) to pin in place. These will not move.")]
    public int[] pinnedParticles;

    // --- Internal Simulation Data ---
    private Mesh _mesh;              // The Unity Mesh object to be deformed
    private MeshFilter _meshFilter;  // Reference to the MeshFilter component

    // Particle data for Verlet Integration
    private Vector3[] _currentPositions;    // Current positions of all particles
    private Vector3[] _previousPositions;   // Positions from the previous step (for calculating velocity)
    private Vector3[] _velocities;          // Explicit velocities (can be influenced by forces, then used in Verlet)
    private float[] _invMasses;             // Inverse mass for each particle (0 if pinned, 1/mass if not)

    // Springs define connections and constraints between particles
    private List<Spring> _springs = new List<Spring>();

    // Helper struct to define a spring connection between two particles
    private struct Spring
    {
        public int p1Index;        // Index of the first particle
        public int p2Index;        // Index of the second particle
        public float restLength;   // The desired distance between p1 and p2
        public float stiffness;    // How strongly the spring tries to return to its rest length
    }

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter.mesh == null)
        {
            _meshFilter.mesh = new Mesh(); // Create a new mesh if none exists
        }
        _mesh = _meshFilter.mesh;
        _mesh.name = "GeneratedClothMesh"; // Give the mesh a descriptive name
    }

    void OnEnable()
    {
        // When this component becomes active, it registers itself with the global system.
        // This is how the ClothSimulationSystem becomes aware of this specific cloth.
        if (ClothSimulationSystem.Instance != null)
        {
            ClothSimulationSystem.Instance.RegisterCloth(this);
        }
        else
        {
            Debug.LogError("ClothSimulationSystem not found! Please ensure it exists in the scene and is active.", this);
            enabled = false; // Disable this component if the system is missing to prevent errors.
        }
    }

    void OnDisable()
    {
        // When this component becomes inactive, it deregisters itself.
        // This stops the system from trying to update a disabled or destroyed cloth.
        if (ClothSimulationSystem.Instance != null)
        {
            ClothSimulationSystem.Instance.DeregisterCloth(this);
        }
    }

    // --- IClothSimulator Interface Implementations ---

    /// <inheritdoc/>
    public void InitializeCloth()
    {
        GenerateClothMesh();          // Create the visual mesh geometry
        InitializeParticlesAndSprings(); // Set up physics particles and their connections
    }

    /// <inheritdoc/>
    public void SimulateCloth(float deltaTime, Vector3 globalGravity, int simulationIterations)
    {
        // Only simulate if we have a valid mesh and particles
        if (_currentPositions == null || _currentPositions.Length == 0) return;

        // 1. Apply all external forces (gravity, air resistance, wind, etc.)
        ApplyExternalForces(deltaTime, globalGravity);

        // 2. Perform Verlet integration step to update particle positions based on forces.
        IntegrateVerlet(deltaTime);

        // 3. Iteratively satisfy constraints (springs and pinning).
        // Multiple iterations help ensure constraints are met more accurately,
        // especially when springs are stiff or interactions are complex.
        for (int i = 0; i < simulationIterations; i++)
        {
            SatisfySpringConstraints();
            ApplyPinningConstraints(); // Pinning needs to be re-applied after spring adjustments
        }

        // 4. Update the actual Unity mesh to reflect the new particle positions.
        UpdateMeshVertices();
    }

    // --- Private Helper Methods for Cloth Simulation Logic ---

    /// <summary>
    /// Generates a grid mesh for the cloth based on specified dimensions and segments.
    /// </summary>
    private void GenerateClothMesh()
    {
        int numVertices = (widthSegments + 1) * (heightSegments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uv = new Vector2[numVertices];
        int[] triangles = new int[widthSegments * heightSegments * 6]; // 2 triangles per quad * 3 vertices each

        float segmentWidth = clothWidth / widthSegments;
        float segmentHeight = clothHeight / heightSegments;

        // Populate vertices and UVs
        int vertIndex = 0;
        for (int j = 0; j <= heightSegments; j++) // Rows (height)
        {
            for (int i = 0; i <= widthSegments; i++) // Columns (width)
            {
                // Position calculation: centered at origin, top-left is (-(width/2), height, 0)
                float x = i * segmentWidth - clothWidth / 2f;
                float y = clothHeight - j * segmentHeight; // Start at top of desired height, move down
                vertices[vertIndex] = new Vector3(x, y, 0); // Z-axis can be modified for initial curve
                uv[vertIndex] = new Vector2((float)i / widthSegments, (float)j / heightSegments);
                vertIndex++;
            }
        }

        // Populate triangles (two triangles per quad)
        int triIndex = 0;
        for (int j = 0; j < heightSegments; j++)
        {
            for (int i = 0; i < widthSegments; i++)
            {
                int bottomLeft = j * (widthSegments + 1) + i;
                int bottomRight = bottomLeft + 1;
                int topLeft = (j + 1) * (widthSegments + 1) + i;
                int topRight = topLeft + 1;

                // First triangle (bottom-left, top-left, bottom-right)
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                // Second triangle (bottom-right, top-left, top-right)
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }

        // Apply mesh data
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals(); // Essential for lighting
        _mesh.RecalculateBounds();  // Essential for culling and interactions

        // Store initial positions for simulation
        _currentPositions = (Vector3[])vertices.Clone();
    }

    /// <summary>
    /// Initializes particle properties (mass, previous positions) and creates springs.
    /// </summary>
    private void InitializeParticlesAndSprings()
    {
        int numParticles = _currentPositions.Length;
        _previousPositions = (Vector3[])_currentPositions.Clone(); // Start with current positions
        _velocities = new Vector3[numParticles];
        _invMasses = new float[numParticles];

        // Assign inverse mass for unpinned particles
        for (int i = 0; i < numParticles; i++)
        {
            _invMasses[i] = 1f / massPerPoint;
        }

        // Apply pinning: set inverse mass to 0, effectively making mass infinite.
        // Pinned particles will not move from their initial positions.
        foreach (int pinnedIndex in pinnedParticles)
        {
            if (pinnedIndex >= 0 && pinnedIndex < numParticles)
            {
                _invMasses[pinnedIndex] = 0f;
            }
            else
            {
                Debug.LogWarning($"Pinned particle index {pinnedIndex} is out of bounds (0 - {numParticles - 1}) for cloth {gameObject.name}.");
            }
        }

        _springs.Clear(); // Clear existing springs before re-initializing

        // --- Create Springs ---
        // Structural Springs: Connect immediate neighbors (horizontal & vertical)
        for (int j = 0; j <= heightSegments; j++)
        {
            for (int i = 0; i <= widthSegments; i++)
            {
                int p1 = j * (widthSegments + 1) + i; // Current particle index

                // Horizontal spring (to the right)
                if (i < widthSegments)
                {
                    int p2 = p1 + 1;
                    AddSpring(p1, p2, structuralStiffness);
                }
                // Vertical spring (downwards)
                if (j < heightSegments)
                {
                    int p2 = p1 + (widthSegments + 1);
                    AddSpring(p1, p2, structuralStiffness);
                }
            }
        }

        // Shear Springs: Connect diagonal neighbors
        for (int j = 0; j < heightSegments; j++)
        {
            for (int i = 0; i < widthSegments; i++)
            {
                int pBL = j * (widthSegments + 1) + i;         // Bottom-left
                int pBR = pBL + 1;                              // Bottom-right
                int pTL = (j + 1) * (widthSegments + 1) + i;    // Top-left
                int pTR = pTL + 1;                              // Top-right

                AddSpring(pTL, pBR, shearStiffness); // Diagonal: Top-left to Bottom-right
                AddSpring(pTR, pBL, shearStiffness); // Diagonal: Top-right to Bottom-left
            }
        }

        // Bending Springs: Connect particles two segments apart (resists sharp folds)
        for (int j = 0; j <= heightSegments; j++)
        {
            for (int i = 0; i <= widthSegments; i++)
            {
                int p1 = j * (widthSegments + 1) + i;

                // Horizontal bending spring (two steps right)
                if (i < widthSegments - 1)
                {
                    int p2 = p1 + 2;
                    AddSpring(p1, p2, bendingStiffness);
                }
                // Vertical bending spring (two steps down)
                if (j < heightSegments - 1)
                {
                    int p2 = p1 + 2 * (widthSegments + 1);
                    AddSpring(p1, p2, bendingStiffness);
                }
            }
        }
    }

    /// <summary>
    /// Helper to add a spring to the internal list.
    /// </summary>
    private void AddSpring(int p1, int p2, float stiffness)
    {
        Spring spring = new Spring
        {
            p1Index = p1,
            p2Index = p2,
            restLength = Vector3.Distance(_currentPositions[p1], _currentPositions[p2]),
            stiffness = stiffness
        };
        _springs.Add(spring);
    }

    /// <summary>
    /// Applies forces like gravity and air resistance to particles.
    /// These forces modify the particles' velocities.
    /// </summary>
    private void ApplyExternalForces(float deltaTime, Vector3 globalGravity)
    {
        for (int i = 0; i < _currentPositions.Length; i++)
        {
            if (_invMasses[i] == 0) continue; // Skip pinned particles

            Vector3 force = Vector3.zero;

            // Gravity
            force += globalGravity * (1f / _invMasses[i]);

            // Air Resistance: simple model where resistance is proportional to velocity magnitude squared
            // and acts opposite to the current velocity.
            Vector3 currentVelocity = (_currentPositions[i] - _previousPositions[i]) / deltaTime;
            force -= currentVelocity * currentVelocity.magnitude * airDrag;

            // Update velocity based on applied forces
            _velocities[i] += force * _invMasses[i] * deltaTime;
        }
    }

    /// <summary>
    /// Updates particle positions using Verlet integration.
    /// This method is stable and good for cloth simulation.
    /// `X_new = X_current + (X_current - X_previous) + A * dt^2`
    /// We use an explicit `_velocities` array to simplify force application.
    /// </summary>
    private void IntegrateVerlet(float deltaTime)
    {
        for (int i = 0; i < _currentPositions.Length; i++)
        {
            if (_invMasses[i] == 0) continue; // Skip pinned particles

            Vector3 tempCurrentPos = _currentPositions[i]; // Store current position for the next step's "previous"

            // Apply damping directly to velocity
            _velocities[i] *= (1f - dampingFactor);

            // Update current position based on velocity
            _currentPositions[i] += _velocities[i] * deltaTime;

            // Update previous position for the next integration step
            _previousPositions[i] = tempCurrentPos;
        }
    }


    /// <summary>
    /// Iteratively adjusts particle positions to satisfy spring length constraints.
    /// </summary>
    private void SatisfySpringConstraints()
    {
        foreach (Spring spring in _springs)
        {
            int p1Idx = spring.p1Index;
            int p2Idx = spring.p2Index;

            Vector3 p1 = _currentPositions[p1Idx];
            Vector3 p2 = _currentPositions[p2Idx];

            Vector3 delta = p2 - p1;
            float currentDistance = delta.magnitude;

            // Avoid division by zero if particles are exactly at the same spot
            if (currentDistance == 0) continue;

            float difference = (currentDistance - spring.restLength) / currentDistance;

            // Calculate total inverse mass to distribute the correction proportionally
            float totalInvMass = _invMasses[p1Idx] + _invMasses[p2Idx];
            if (totalInvMass == 0) continue; // Both particles are pinned, no need to adjust

            // Calculate how much each particle should move
            // Correction is stronger for stiffer springs and lighter particles
            float p1CorrectionFactor = (difference * spring.stiffness / totalInvMass) * _invMasses[p1Idx];
            float p2CorrectionFactor = (difference * spring.stiffness / totalInvMass) * _invMasses[p2Idx];

            // Apply corrections: push p1 away from p2, pull p2 towards p1 (or vice versa)
            _currentPositions[p1Idx] += delta * p1CorrectionFactor;
            _currentPositions[p2Idx] -= delta * p2CorrectionFactor;
        }
    }

    /// <summary>
    /// Re-enforces pinning constraints. This is necessary after spring adjustments
    /// which might have moved a supposedly pinned particle slightly.
    /// </summary>
    private void ApplyPinningConstraints()
    {
        // For each pinned particle, reset its position and velocity to its original fixed state.
        foreach (int pinnedIndex in pinnedParticles)
        {
            if (pinnedIndex >= 0 && pinnedIndex < _currentPositions.Length)
            {
                // This assumes the initial mesh vertex position is the desired pinned location.
                _currentPositions[pinnedIndex] = _mesh.vertices[pinnedIndex];
                _previousPositions[pinnedIndex] = _mesh.vertices[pinnedIndex]; // Ensure velocity is zero
                _velocities[pinnedIndex] = Vector3.zero; // Explicitly zero out velocity
            }
        }
    }

    /// <summary>
    /// Updates the Unity Mesh's vertex positions with the new simulated particle positions.
    /// Recalculates normals and bounds for correct rendering.
    /// </summary>
    private void UpdateMeshVertices()
    {
        _mesh.vertices = _currentPositions;
        _mesh.RecalculateNormals(); // Normals must be updated as the mesh deforms
        _mesh.RecalculateBounds();  // Bounds must be updated for proper culling
    }

    // --- Editor-time functionality ---

    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Used here to provide real-time feedback in the Unity Editor.
    /// </summary>
    void OnValidate()
    {
        // Don't run this logic during play mode, as Awake/Start will handle initialization.
        if (Application.isPlaying) return;

        // Ensure MeshFilter and MeshRenderer components are attached.
        if (!GetComponent<MeshFilter>()) gameObject.AddComponent<MeshFilter>();
        if (!GetComponent<MeshRenderer>()) gameObject.AddComponent<MeshRenderer>();

        // If the mesh is null or parameters change, regenerate the cloth mesh and physics data.
        // This allows developers to see the cloth's initial shape directly in the editor.
        if (_meshFilter == null || _meshFilter.mesh == null)
        {
             _meshFilter = GetComponent<MeshFilter>();
             _meshFilter.mesh = new Mesh();
             _mesh = _meshFilter.mesh;
             _mesh.name = "GeneratedClothMesh";
        }
        GenerateClothMesh();
        InitializeParticlesAndSprings();
    }
}


/*
// --- Example Usage in a Unity Scene ---

// Step 1: Create the ClothSimulationSystem Manager
//   a. Create an empty GameObject in your scene (e.g., named "PhysicsManager").
//   b. Add the 'ClothSimulationSystem' component to this GameObject.
//   c. In the Inspector, adjust globalGravity (e.g., Y = -9.81 for Earth-like gravity)
//      and simulationIterations (higher = more stable but slower).

// Step 2: Create a Piece of Cloth
//   a. Create another empty GameObject (e.g., named "FlagCloth" or "CapeCloth").
//   b. Add the 'SimpleClothSimulator' component to this GameObject.
//      (Unity will automatically add a MeshFilter and MeshRenderer due to `RequireComponent`).

// Step 3: Assign a Material
//   a. In your Project window, create a new Material (e.g., "ClothMaterial").
//      You can use a basic Standard shader and assign a color or texture.
//   b. Drag and drop "ClothMaterial" onto the "FlagCloth" GameObject's MeshRenderer component
//      in the Inspector.

// Step 4: Configure the Cloth
//   a. Select your "FlagCloth" GameObject.
//   b. In the 'SimpleClothSimulator' component in the Inspector:
//      - Adjust `widthSegments` and `heightSegments` to control the cloth's resolution.
//      - Set `clothWidth` and `clothHeight` for its physical dimensions.
//      - Tune `massPerPoint`, `structuralStiffness`, `shearStiffness`, `bendingStiffness`,
//        `dampingFactor`, and `airDrag` to achieve desired cloth behavior.
//      - Define `pinnedParticles`: These are the vertex indices that will remain fixed.
//        For a flag, you might pin the top-left and top-right corners.
//        If `widthSegments = 10`, then:
//          - The top-left corner is index `0`.
//          - The top-right corner is index `widthSegments` (e.g., `10`).
//        So, set the `Pinned Particles` array size to `2`, and set `Element 0` to `0`, `Element 1` to `10`.
//        (The `OnValidate` method will update the mesh in the editor, allowing you to preview changes.)

// Step 5: Run the Scene!
//   Press Play. You should see your cloth object now simulating, reacting to gravity
//   and its internal spring forces.

// --- Benefits of the ClothSimulationSystem Pattern ---

// 1. Centralized Control and Configuration:
//    - The `ClothSimulationSystem` acts as a single point of truth for global simulation parameters
//      (like gravity, global wind, overall quality settings).
//    - You can easily pause/resume all cloth simulations or modify their behavior globally.

// 2. Scalability:
//    - Adding more cloth objects to your scene is straightforward. Each new cloth only needs to
//      implement `IClothSimulator` and will automatically be registered and managed by the system.
//    - The system can manage hundreds or thousands of cloths without each one needing its own
//      `FixedUpdate` loop, preventing potential performance issues from too many Update calls.

// 3. Decoupling and Modularity:
//    - The `ClothSimulationSystem` is decoupled from the specific implementation details of any
//      individual cloth. It only cares that a component implements `IClothSimulator`.
//    - This allows for different types of cloth simulators (e.g., a simple Verlet-based one,
//      an advanced Position-Based Dynamics one, or a tearable cloth) to coexist and be managed
//      by the same system, promoting code reusability and maintainability.

// 4. Performance Optimization Opportunities:
//    - A central system can intelligently manage update order, prioritize nearby cloths,
//      implement LOD (Level of Detail) based on distance, or even distribute complex
//      simulations across multiple threads or GPU compute shaders (though not implemented here).

// 5. Easier Testing:
//    - You can more easily test the `ClothSimulationSystem` in isolation or test individual
//      `IClothSimulator` implementations by providing mock `ClothSimulationSystem` instances.
*/
```