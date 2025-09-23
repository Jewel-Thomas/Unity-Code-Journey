// Unity Design Pattern Example: ProceduralMeshGeneration
// This script demonstrates the ProceduralMeshGeneration pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Procedural Mesh Generation' design pattern in Unity involves programmatically creating 3D meshes (vertices, triangles, UVs, normals, etc.) instead of importing them from external files. This is incredibly powerful for generating dynamic content like terrains, custom shapes, optimized collision meshes, or special effects at runtime or in the editor.

**Key Principles of the Pattern:**

1.  **Data-Driven:** The mesh is defined by arrays/lists of fundamental data (vertices, triangles, UVs).
2.  **Algorithmic Generation:** Logic is used to calculate these data points based on parameters (size, detail, shape).
3.  **Component Integration:** The generated data is applied to Unity's `Mesh` object, which is then rendered via `MeshFilter` and `MeshRenderer` components.
4.  **Separation of Concerns:**
    *   **Configuration:** Parameters that control the mesh generation.
    *   **Data Collection:** Methods to add vertices, triangles, UVs to temporary lists.
    *   **Generation Logic:** The core algorithms that populate these lists based on desired shape.
    *   **Mesh Application:** Taking the collected data and assigning it to a Unity `Mesh` component.

---

### ProceduralMeshGenerator.cs

This script demonstrates generating a customizable plane mesh procedurally.

**How to Use:**

1.  Create a new C# script named `ProceduralMeshGenerator` in your Unity project.
2.  Copy and paste the code below into the script.
3.  Create an empty GameObject in your scene (e.g., named "ProceduralPlane").
4.  Attach the `ProceduralMeshGenerator` script to this GameObject.
5.  In the Inspector, assign a `Material` to the "Mesh Material" slot (e.g., the default "Default-Material").
6.  Adjust the "Width", "Depth", "Width Segments", and "Depth Segments" parameters. The mesh will regenerate automatically in the editor due to `OnValidate()`.
7.  Run the scene!

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List<T>

/// <summary>
/// Demonstrates the 'Procedural Mesh Generation' design pattern in Unity.
/// This script generates a customizable 3D plane mesh programmatically.
/// </summary>
/// <remarks>
/// The pattern separates concerns:
/// 1. Configuration: Public fields for mesh parameters.
/// 2. Data Collection: Using Lists to store vertices, triangles, UVs, etc.
/// 3. Generation Logic: Methods like GeneratePlaneData() that populate these lists.
/// 4. Mesh Application: Taking the collected data and applying it to a Unity Mesh component.
/// </remarks>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // Ensure necessary components exist
public class ProceduralMeshGenerator : MonoBehaviour
{
    // --- Configuration Parameters (Exposed in Inspector) ---
    [Header("Mesh Dimensions")]
    [Tooltip("The total width of the plane.")]
    [SerializeField] private float meshWidth = 10f;
    [Tooltip("The total depth (Z-axis) of the plane.")]
    [SerializeField] private float meshDepth = 10f;

    [Header("Mesh Detail")]
    [Tooltip("Number of segments along the width (X-axis). More segments = more detail.")]
    [SerializeField] [Range(1, 200)] private int widthSegments = 10;
    [Tooltip("Number of segments along the depth (Z-axis). More segments = more detail.")]
    [SerializeField] [Range(1, 200)] private int depthSegments = 10;

    [Header("Mesh Appearance")]
    [Tooltip("The material to apply to the generated mesh.")]
    [SerializeField] private Material meshMaterial;

    [Header("Mesh Calculations")]
    [Tooltip("Recalculate normals automatically. Essential for correct lighting.")]
    [SerializeField] private bool recalculateNormals = true;
    [Tooltip("Recalculate tangents automatically. Needed for normal mapping.")]
    [SerializeField] private bool recalculateTangents = false;
    [Tooltip("Recalculate bounds automatically. Important for frustum culling and physics.")]
    [SerializeField] private bool recalculateBounds = true;


    // --- Mesh Data Storage (Data-Driven Aspect of the Pattern) ---
    // These lists temporarily hold the mesh data during generation.
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    // Normals and Colors can also be stored here if custom generation is needed,
    // otherwise Unity can often calculate them.
    // private List<Vector3> normals = new List<Vector3>();
    // private List<Color> colors = new List<Color>();

    // --- Component References ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize components and generate the mesh on play mode start.
    /// </summary>
    void Awake()
    {
        InitializeComponents();
        GenerateMesh();
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed in the Inspector.
    /// This allows for real-time preview of mesh changes without entering Play mode.
    /// </summary>
    void OnValidate()
    {
        // Check if the application is playing or in edit mode.
        // We only want to regenerate in edit mode for inspector changes,
        // and in play mode if Awake() hasn't run yet or a value changes dynamically.
        if (Application.isPlaying)
        {
            InitializeComponents(); // Ensure components are ready
            GenerateMesh();
        }
        else if (!Application.isPlaying && meshFilter == null) // For first-time setup in editor
        {
             InitializeComponents(); // Ensure components are ready
             // Only generate if we have a meshFilter and it's not an empty object
             if (meshFilter != null && meshFilter.gameObject != null)
             {
                 GenerateMesh();
             }
        }
        else if (meshFilter != null) // If components are already set up in editor
        {
            GenerateMesh();
        }
    }

    /// <summary>
    /// Provides a context menu option in the Inspector to manually regenerate the mesh.
    /// Useful for debugging or when OnValidate might not trigger (e.g., after script compilation).
    /// </summary>
    [ContextMenu("Regenerate Mesh")]
    private void RegenerateMeshContextMenu()
    {
        InitializeComponents();
        GenerateMesh();
        Debug.Log("Mesh regenerated via context menu.");
    }

    // --- Core Procedural Mesh Generation Methods ---

    /// <summary>
    /// Ensures the GameObject has a MeshFilter and MeshRenderer, and gets their references.
    /// Initializes the Mesh object itself.
    /// </summary>
    private void InitializeComponents()
    {
        // Get or add MeshFilter component
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Get or add MeshRenderer component
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Ensure the MeshFilter has a Mesh assigned. Create a new one if not.
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }
        mesh = meshFilter.sharedMesh;

        // Apply the chosen material to the MeshRenderer
        if (meshMaterial != null)
        {
            meshRenderer.sharedMaterial = meshMaterial;
        }
        else
        {
            Debug.LogWarning("ProceduralMeshGenerator: No material assigned! Mesh might not be visible. Please assign a material.", this);
            // Fallback to a default material if none is assigned, though it might not look good.
            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            }
        }
    }

    /// <summary>
    /// Orchestrates the entire mesh generation process.
    /// This is the core method demonstrating the pattern's workflow.
    /// </summary>
    private void GenerateMesh()
    {
        // 1. Clear existing mesh data from previous generations
        mesh.Clear();
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        // normals.Clear(); // Clear if custom normals are used

        // 2. Generate the geometric data (vertices, triangles, UVs) for the plane
        GeneratePlaneData(meshWidth, meshDepth, widthSegments, depthSegments);

        // 3. Apply the collected data to the Unity Mesh object
        ApplyMeshData();
    }

    /// <summary>
    /// Generates the raw vertex, triangle, and UV data for a plane mesh.
    /// This is the 'Generation Logic' part of the pattern.
    /// </summary>
    /// <param name="width">Total width of the plane.</param>
    /// <param name="depth">Total depth of the plane.</param>
    /// <param name="wSegments">Number of subdivisions along the width.</param>
    /// <param name="dSegments">Number of subdivisions along the depth.</param>
    private void GeneratePlaneData(float width, float depth, int wSegments, int dSegments)
    {
        // Calculate the step size for each segment
        float segmentWidth = width / wSegments;
        float segmentDepth = depth / dSegments;

        // Calculate offset to center the mesh around the GameObject's origin
        float xOffset = -width / 2f;
        float zOffset = -depth / 2f;

        // Loop through each segment to create vertices and UVs
        for (int z = 0; z <= dSegments; z++) // Iterate over depth (rows)
        {
            for (int x = 0; x <= wSegments; x++) // Iterate over width (columns)
            {
                // Calculate vertex position
                Vector3 vertexPosition = new Vector3(
                    x * segmentWidth + xOffset, // X-coordinate
                    0f,                           // Y-coordinate (flat plane)
                    z * segmentDepth + zOffset    // Z-coordinate
                );
                vertices.Add(vertexPosition);

                // Calculate UV coordinate (maps texture across the plane)
                // UVs range from (0,0) to (1,1) across the entire plane.
                Vector2 uvCoordinate = new Vector2(
                    (float)x / wSegments,
                    (float)z / dSegments
                );
                uvs.Add(uvCoordinate);

                // Note: Normals could be generated here if custom normals are needed.
                // For a flat plane, (0,1,0) for all vertices would be sufficient.
                // normals.Add(Vector3.up);
            }
        }

        // Loop through each segment to create triangles
        // A quad (two triangles) is formed by four adjacent vertices.
        for (int z = 0; z < dSegments; z++)
        {
            for (int x = 0; x < wSegments; x++)
            {
                // Get the base index of the current quad's bottom-left vertex
                // (x, z) corresponds to index (z * (wSegments + 1) + x)
                int bl = z * (wSegments + 1) + x;               // Bottom-left
                int br = bl + 1;                                 // Bottom-right
                int tl = (z + 1) * (wSegments + 1) + x;          // Top-left
                int tr = tl + 1;                                 // Top-right

                // Add the two triangles that form this quad
                // Triangle 1: Bottom-left, Top-left, Bottom-right (Clockwise winding for front face)
                triangles.Add(bl);
                triangles.Add(tl);
                triangles.Add(br);

                // Triangle 2: Bottom-right, Top-left, Top-right (Clockwise winding for front face)
                triangles.Add(br);
                triangles.Add(tl);
                triangles.Add(tr);
            }
        }
    }

    /// <summary>
    /// Applies the collected vertex, triangle, and UV data to the Unity Mesh object.
    /// This is the 'Mesh Application' part of the pattern.
    /// </summary>
    private void ApplyMeshData()
    {
        // Assign the generated data to the mesh
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0); // Submesh index 0
        mesh.SetUVs(0, uvs); // UV channel 0

        // Optional: Recalculate mesh properties if enabled
        if (recalculateNormals)
        {
            mesh.RecalculateNormals(); // Essential for proper lighting
        }
        // If custom normals were generated, assign them directly:
        // mesh.SetNormals(normals);

        if (recalculateTangents)
        {
            mesh.RecalculateTangents(); // Needed for normal maps
        }
        if (recalculateBounds)
        {
            mesh.RecalculateBounds();   // Important for frustum culling and physics
        }

        // Optimize the mesh for rendering performance (optional, but good practice)
        mesh.Optimize();
    }
}
```