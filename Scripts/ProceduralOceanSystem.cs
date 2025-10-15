// Unity Design Pattern Example: ProceduralOceanSystem
// This script demonstrates the ProceduralOceanSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ProceduralOceanSystem' pattern, while not a GoF (Gang of Four) design pattern, represents a common *system design* or *architectural pattern* in game development. It describes a central component responsible for programmatically generating, simulating, and rendering a dynamic ocean surface.

This system encapsulates:
1.  **Mesh Generation:** Creating the geometric structure (vertices, triangles, UVs) for the ocean plane from scratch.
2.  **Wave Simulation:** Applying mathematical models (e.g., sine waves, Gerstner waves) to deform the mesh vertices over time, creating the illusion of moving waves.
3.  **Rendering Integration:** Managing Unity's `MeshFilter` and `MeshRenderer` components to display the generated and deformed mesh with an appropriate material.
4.  **Configuration:** Exposing parameters in the Unity Inspector to control the ocean's size, resolution, and wave characteristics.

This pattern is highly practical for creating dynamic environments, allowing for custom wave behavior, real-time adjustments, and efficient resource management compared to pre-modeled static oceans.

---

Here's a complete, commented C# Unity script demonstrating the `ProceduralOceanSystem` pattern:

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

/// <summary>
///     The ProceduralOceanSystem MonoBehaviour class.
///     This class encapsulates the entire logic for generating a procedural ocean mesh,
///     simulating waves, and rendering the ocean surface in Unity.
///
///     It acts as a central manager for all ocean-related functionality, adhering to
///     the 'ProceduralOceanSystem' design pattern.
/// </summary>
/// <remarks>
///     **ProceduralOceanSystem Design Pattern Explanation:**
///     This pattern is about centralizing the creation and management of a dynamic,
///     algorithmically generated ocean. It combines several responsibilities:
///     1.  **Mesh Generation:** Creating the initial grid of vertices, triangles, and UVs.
///     2.  **Wave Simulation Logic:** Implementing mathematical models (like sine waves
///         or Gerstner waves) to calculate vertex displacement over time.
///     3.  **Mesh Deformation:** Applying the calculated wave displacements to the mesh's
///         vertices each frame to animate the waves.
///     4.  **Rendering Integration:** Ensuring the mesh is correctly displayed by managing
///         the `MeshFilter` and `MeshRenderer` components.
///     5.  **Configuration:** Exposing parameters in the Inspector for easy customization
///         of ocean size, detail, and wave properties without code changes.
///
///     By using this pattern, all ocean-related logic is self-contained and easily
///     configurable, making it practical for real-world Unity projects.
/// </remarks>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralOceanSystem : MonoBehaviour
{
    [Header("Ocean Dimensions")]
    [Tooltip("The width and depth of the square ocean plane in Unity units.")]
    [SerializeField] private float oceanSize = 100f;

    [Tooltip("The number of segments along one side of the ocean plane. Higher values mean more detail but higher performance cost.")]
    [Range(16, 256)] // Limit resolution for practical usage and performance
    [SerializeField] private int resolution = 64; // (resolution + 1) vertices per side

    [Header("Wave Parameters")]
    [Tooltip("An array of wave definitions. Multiple waves can be combined for complex effects.")]
    [SerializeField] private WaveData[] waves;

    /// <summary>
    ///     A serializable struct to define properties for a single wave.
    ///     Using `[System.Serializable]` makes this struct editable in the Unity Inspector.
    /// </summary>
    [System.Serializable]
    public struct WaveData
    {
        [Tooltip("The maximum height of the wave.")]
        public float amplitude;
        [Tooltip("How many wave cycles occur over a given distance. Higher frequency means shorter, more frequent waves.")]
        public float frequency;
        [Tooltip("How fast the wave moves across the ocean surface.")]
        public float speed;
        [Tooltip("The direction vector of the wave (normalized for consistent behavior).")]
        public Vector2 direction;
    }

    // --- Internal State ---
    private MeshFilter meshFilter;
    private Mesh mesh;

    // Stores the original, flat positions of the vertices. Used as a base for wave calculations.
    private Vector3[] baseVertices;
    // Stores the current, deformed positions of the vertices due to waves. Updated every frame.
    private Vector3[] currentVertices;
    private int[] triangles;
    private Vector2[] uvs;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    ///     Called when the script instance is being loaded.
    ///     Used to get references to required components.
    /// </summary>
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    /// <summary>
    ///     Called on the frame when a script is enabled just before any Update methods are called the first time.
    ///     Used to generate the initial ocean mesh and apply a default material.
    /// </summary>
    void Start()
    {
        GenerateOceanMesh();
        ApplyOceanMaterial();
    }

    /// <summary>
    ///     Called every frame.
    ///     Used to update the ocean waves by deforming the mesh vertices.
    /// </summary>
    void Update()
    {
        UpdateOceanWaves();
    }

    // --- Core Ocean Generation and Simulation Methods ---

    /// <summary>
    ///     Generates the initial flat ocean mesh.
    ///     This involves creating vertices, UVs, and triangles based on `oceanSize` and `resolution`.
    /// </summary>
    private void GenerateOceanMesh()
    {
        // 1. Initialize or reset the Mesh object
        // Creating a new mesh is fine for initial generation. For updates, we modify existing mesh data.
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Procedural Ocean Mesh";
        }
        else
        {
            mesh.Clear(); // Clear existing mesh data if regenerating
        }

        // Assign the mesh to the MeshFilter component
        meshFilter.mesh = mesh;

        // 2. Create the grid data (vertices, UVs, triangles)
        CreateGridData();

        // 3. Apply the generated data to the mesh
        mesh.vertices = currentVertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // 4. Recalculate mesh properties for correct rendering
        mesh.RecalculateNormals();  // Important for lighting
        mesh.RecalculateTangents(); // Required if using normal maps in the material
        mesh.RecalculateBounds();   // Important for frustum culling and performance
    }

    /// <summary>
    ///     Calculates and populates the `baseVertices`, `currentVertices`, `triangles`, and `uvs` arrays
    ///     for a flat grid based on the specified `resolution` and `oceanSize`.
    /// </summary>
    private void CreateGridData()
    {
        // Using Lists first for easy dynamic additions, then convert to arrays.
        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesList = new List<int>();
        List<Vector2> uvsList = new List<Vector2>();

        // Calculate the size of each grid segment
        float segmentSize = oceanSize / resolution;

        // --- Create Vertices and UVs ---
        // Loop through each point in the grid to create a vertex
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                // Position the vertex, centering the ocean around (0,0)
                float xPos = x * segmentSize - oceanSize / 2f;
                float zPos = z * segmentSize - oceanSize / 2f;
                verticesList.Add(new Vector3(xPos, 0, zPos));

                // Calculate UV coordinates (0 to 1 range across the plane)
                uvsList.Add(new Vector2((float)x / resolution, (float)z / resolution));
            }
        }

        // Store the base (flat) vertices and create a copy for current deformation
        baseVertices = verticesList.ToArray();
        currentVertices = (Vector3[])baseVertices.Clone(); // Start with a copy for deformation

        // --- Create Triangles ---
        // A grid square is made of two triangles. We iterate through each square.
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Get the base index for the bottom-left vertex of the current square
                int vertIndex = z * (resolution + 1) + x;

                // Define the two triangles for this square:
                // Triangle 1: (bottom-left, top-left, bottom-right)
                trianglesList.Add(vertIndex);             // 0
                trianglesList.Add(vertIndex + resolution + 1); // 1
                trianglesList.Add(vertIndex + 1);         // 2

                // Triangle 2: (top-left, top-right, bottom-right)
                trianglesList.Add(vertIndex + resolution + 1); // 1
                trianglesList.Add(vertIndex + resolution + 2); // 3
                trianglesList.Add(vertIndex + 1);         // 2
            }
        }

        triangles = trianglesList.ToArray();
        uvs = uvsList.ToArray();
    }

    /// <summary>
    ///     Updates the `currentVertices` array by applying wave displacements based on
    ///     `Time.time` and the configured `waves`.
    ///     The updated vertices are then assigned back to the `mesh`.
    /// </summary>
    private void UpdateOceanWaves()
    {
        // Ensure mesh data is initialized before attempting to update
        if (mesh == null || baseVertices == null || waves == null || waves.Length == 0) return;

        // Reset current vertices to their base (flat) positions before applying new displacements.
        // This prevents waves from cumulatively growing or getting stuck.
        System.Array.Copy(baseVertices, currentVertices, baseVertices.Length);

        float time = Time.time; // Get current time for wave animation

        // Iterate through all vertices and apply wave deformation
        for (int i = 0; i < currentVertices.Length; i++)
        {
            // Use the base (original flat) vertex position for wave calculations.
            // This ensures waves are consistent regardless of previous frame's deformation.
            Vector3 vertex = baseVertices[i];

            // Accumulate displacement from all defined waves
            float totalDisplacementY = 0f;
            Vector3 totalDisplacementXZ = Vector3.zero; // For more advanced wave types like Gerstner

            foreach (var wave in waves)
            {
                // Skip waves with negligible amplitude to save computation
                if (wave.amplitude <= 0.001f) continue;

                // Normalize wave direction to prevent issues with non-unit vectors
                Vector2 normalizedDirection = wave.direction.normalized;

                // Calculate the dot product of the vertex position and wave direction.
                // This gives the "progress" of the wave across the surface.
                float dotProduct = Vector2.Dot(new Vector2(vertex.x, vertex.z), normalizedDirection);

                // Simple Sine Wave: Only displaces vertically (Y-axis)
                // waveHeight = Amplitude * sin( (position . direction) * frequency + time * speed )
                totalDisplacementY += wave.amplitude * Mathf.Sin(dotProduct * wave.frequency + time * wave.speed);

                /*
                /// --- Advanced Wave Example: Gerstner Waves (More Realistic) ---
                /// Gerstner waves also displace vertices horizontally (X, Z) to create
                /// realistic sharp crests and flat troughs. This is more computationally
                /// intensive but looks better.
                /// To implement, uncomment and replace the simple sine wave line above.

                float Q = 0.7f; // Steepness factor (0 to 1, 0 = pure sine, 1 = sharp crests)

                // Vertical displacement
                totalDisplacementY += wave.amplitude * Mathf.Sin(dotProduct * wave.frequency + time * wave.speed);

                // Horizontal displacement (creates crests/troughs and pushes points)
                float cosTerm = Mathf.Cos(dotProduct * wave.frequency + time * wave.speed);
                totalDisplacementXZ.x += Q * normalizedDirection.x * wave.amplitude * cosTerm;
                totalDisplacementXZ.z += Q * normalizedDirection.y * wave.amplitude * cosTerm;

                */
            }

            // Apply accumulated displacement to the current vertex
            currentVertices[i].y += totalDisplacementY;
            // currentVertices[i].x += totalDisplacementXZ.x; // Uncomment for Gerstner X
            // currentVertices[i].z += totalDisplacementXZ.z; // Uncomment for Gerstner Z
        }

        // Assign the updated vertices back to the mesh. This is an efficient way to deform a mesh.
        mesh.vertices = currentVertices;

        // Recalculate normals and bounds after vertex deformation for correct lighting and culling.
        // NOTE: RecalculateNormals() can be performance-intensive for very high resolutions.
        // For extreme optimization, custom normal calculation based on neighbor vertices would be faster.
        mesh.RecalculateNormals();
        mesh.RecalculateTangents(); // Useful if your shader uses tangent space normal maps
        mesh.RecalculateBounds();
    }

    /// <summary>
    ///     Applies a material to the MeshRenderer component.
    ///     If no material is assigned in the Inspector, a default transparent blue material is created and used.
    /// </summary>
    private void ApplyOceanMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found on ProceduralOceanSystem. This should not happen due to RequireComponent.", this);
            return;
        }

        // If no material is assigned to the MeshRenderer in the Inspector, create a default one.
        if (meshRenderer.sharedMaterial == null)
        {
            Debug.LogWarning("No material assigned to the ProceduralOceanSystem's MeshRenderer. Assigning a default transparent blue material.", this);

            // Attempt to find the "Standard" shader, which is versatile.
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            if (defaultMaterial != null)
            {
                // Configure the material for transparent blue water
                defaultMaterial.SetColor("_Color", new Color(0.1f, 0.3f, 0.7f, 0.6f)); // Light blue, semi-transparent
                defaultMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.1f, 0.2f, 1f)); // Subtle emissive glow
                defaultMaterial.EnableKeyword("_EMISSION");

                // Set render mode to Fade (for transparency)
                defaultMaterial.SetFloat("_Mode", 2); // 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
                defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                defaultMaterial.SetInt("_ZWrite", 0); // Don't write to Z-buffer for transparent objects
                defaultMaterial.DisableKeyword("_ALPHATEST_ON");
                defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
                defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                defaultMaterial.renderQueue = 3000; // Render after opaque objects

                meshRenderer.material = defaultMaterial;
            }
            else
            {
                Debug.LogError("Failed to find 'Standard' shader for default material. Please ensure the 'Standard' shader is available in your project or assign a material manually.", this);
            }
        }
    }

    /// <summary>
    ///     Draws a wireframe cube in the editor to visualize the ocean's bounds.
    /// </summary>
    void OnDrawGizmos()
    {
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix; // Apply object's transform to Gizmos
            Gizmos.color = Color.cyan;
            // Draw a wireframe cube representing the ocean's dimensions
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(oceanSize, 0.1f, oceanSize));
        }
    }
}

/*
/// --- EXAMPLE USAGE IN UNITY ---

1.  **Create an Empty GameObject:**
    -   In your Unity project, right-click in the Hierarchy window.
    -   Select "Create Empty".
    -   Rename it to "ProceduralOcean".

2.  **Attach the Script:**
    -   Drag and drop this `ProceduralOceanSystem.cs` script onto the "ProceduralOcean" GameObject in the Hierarchy or Inspector.
    -   Unity will automatically add a `MeshFilter` and `MeshRenderer` component because of `[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]`.

3.  **Configure in the Inspector:**
    -   Select the "ProceduralOcean" GameObject.
    -   In the Inspector, adjust the parameters under the `ProceduralOceanSystem` component:
        -   **Ocean Size:** e.g., `100` (for a 100x100 unit ocean).
        -   **Resolution:** e.g., `64` (for a 64x64 grid, resulting in 65x65 vertices). Higher resolution means more detailed waves but more computational cost.

    -   **Add Waves:**
        -   Expand the "Waves" array.
        -   Set "Size" to `1` (or more for multiple combined waves).
        -   **Wave 0 (Example):**
            -   **Amplitude:** `1.5` (makes waves 1.5 units high)
            -   **Frequency:** `0.1` (makes waves repeat every 10 units)
            -   **Speed:** `2.0` (makes waves move at 2 units/second)
            -   **Direction:** `(1, 0)` (moves along the X-axis) or `(1, 1)` (moves diagonally, will be normalized by script)

        -   **Add another wave (Example for combined effect):**
            -   Set "Size" to `2`.
            -   **Wave 1:**
                -   **Amplitude:** `0.5`
                -   **Frequency:** `0.3`
                -   **Speed:** `3.0`
                -   **Direction:** `(0, 1)` (moves along the Z-axis)

4.  **Material (Optional but Recommended):**
    -   By default, the script will apply a simple transparent blue material if none is assigned.
    -   For a more realistic ocean, create your own water material (e.g., using a custom water shader or a modified Standard shader with reflection/refraction).
    -   Drag your custom material onto the "Material" slot of the `MeshRenderer` component on the "ProceduralOcean" GameObject.

5.  **Run the Scene:**
    -   Press Play in the Unity editor. You should see a dynamic, waving ocean surface.

*/
```