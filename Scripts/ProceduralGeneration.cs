// Unity Design Pattern Example: ProceduralGeneration
// This script demonstrates the ProceduralGeneration pattern in Unity
// Generated automatically - ready to use in your Unity project

The Procedural Generation design pattern is about algorithms that create content programmatically rather than manually. In Unity, this often means generating terrains, levels, textures, objects, or even entire game worlds at runtime or in the editor.

This example demonstrates how to procedurally generate a simple 3D terrain using Perlin noise. The terrain will be a grid of cubes with varying heights, giving it a natural, undulating appearance.

---

### Key Concepts Demonstrated:

1.  **Seed-based Generation**: Using a numerical seed to ensure reproducible results. The same seed will always produce the exact same terrain.
2.  **Generation Parameters**: Exposing various settings (dimensions, noise scale, octaves) in the Unity Inspector to control the generation process.
3.  **Noise Functions**: Employing Perlin noise, a common technique for creating organic-looking textures and heightmaps.
4.  **Iteration and Instantiation**: Looping through a grid to place and configure individual GameObjects (cubes in this case).
5.  **Hierarchy Organization**: Parenting generated objects under a single container for a cleaner Unity Hierarchy.
6.  **Editor-Time Generation**: Using `[ExecuteInEditMode]` and `OnValidate()` to allow real-time regeneration in the Unity Editor when parameters are changed, providing immediate visual feedback.
7.  **Cleanup**: Safely destroying previous generations before creating new ones to prevent clutter and memory leaks.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For potential use of lists, though not strictly needed in this minimal example

// [ExecuteInEditMode] allows this script to run and update in the editor without pressing Play.
// This is incredibly useful for procedural generation as it provides real-time feedback when
// tweaking parameters in the Inspector, enabling a more iterative design process.
[ExecuteInEditMode]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    // --- Procedural Generation Parameters ---
    // These fields are marked [SerializeField] to expose them in the Unity Inspector,
    // allowing developers to easily customize the generation without modifying code.

    [Header("Generation Settings")]
    [Tooltip("Width of the generated terrain in units (number of blocks along X-axis).")]
    [SerializeField] private int terrainWidth = 50;
    [Tooltip("Depth of the generated terrain in units (number of blocks along Z-axis).")]
    [SerializeField] private int terrainDepth = 50;
    [Tooltip("Controls the vertical amplitude of the terrain features. Higher values mean taller mountains/deeper valleys.")]
    [SerializeField] private float terrainHeightScale = 10f;
    [Tooltip("The size of each individual block/unit that makes up the terrain.")]
    [SerializeField] private float blockSize = 1f;
    [Tooltip("Random seed for reproducible generation. Use 0 for truly random, or a specific number to get the same terrain every time.")]
    [SerializeField] private int seed = 0;
    [Tooltip("If true, a new random seed will be generated on each editor update or play, overriding the 'Seed' value.")]
    [SerializeField] private bool generateRandomSeed = true;

    [Header("Noise Settings")]
    [Tooltip("Scaling factor for the Perlin noise. Smaller values create smoother, larger features; larger values create more jagged, smaller features.")]
    [SerializeField] private float noiseScale = 10f;
    [Tooltip("Number of noise layers (octaves). More octaves add more fine-grained detail to the terrain.")]
    [SerializeField] private int octaves = 4;
    [Tooltip("Controls the amplitude of each successive octave. A value between 0 and 1. Higher persistence means later octaves have a stronger impact.")]
    [SerializeField] [Range(0f, 1f)] private float persistence = 0.5f;
    [Tooltip("Controls the frequency of each successive octave. A value greater than 1. Higher lacunarity means later octaves add more detail/roughness.")]
    [SerializeField] private float lacunarity = 2f;
    [Tooltip("Offset for the noise coordinates. Can be used to 'move' or 'scroll' the terrain landscape.")]
    [SerializeField] private Vector2 noiseOffset;

    [Header("Block Material")]
    [Tooltip("Material to apply to each generated terrain block. Assign a material asset from your Project window.")]
    [SerializeField] private Material blockMaterial;

    // --- Internal State ---
    private GameObject terrainContainer; // A parent GameObject to hold all generated blocks for organization.
    private System.Random prng;          // Pseudorandom number generator for seed control. System.Random is deterministic with a seed.

    // --- Core Procedural Generation Logic ---

    // OnValidate is called in the editor when the script is loaded or a value is changed in the Inspector.
    // This is the ideal place to trigger procedural generation for real-time feedback in the editor.
    private void OnValidate()
    {
        // Calling GenerateTerrain() here ensures that any change in the Inspector immediately
        // updates the generated terrain in the Scene view, making the development process very efficient.
        GenerateTerrain();
    }

    // Start is called once when the script instance is being loaded (when the game starts or script is enabled).
    // This ensures terrain is generated when entering Play Mode, or if ExecuteInEditMode is not used.
    private void Start()
    {
        GenerateTerrain();
    }

    /// <summary>
    /// This is the central method that orchestrates the procedural generation of the terrain.
    /// It encapsulates the entire process: cleanup, seeding, calculation, and instantiation.
    /// </summary>
    public void GenerateTerrain()
    {
        // 1. Cleanup Previous Generation (if any)
        // This is crucial to prevent multiple terrains from overlapping and to avoid memory leaks
        // when parameters are changed rapidly in the editor.
        ClearTerrain();

        // 2. Initialize Seed (for reproducibility and variation)
        // The seed ensures that the generation is deterministic. The same seed will always
        // produce the exact same terrain. If generateRandomSeed is true, a new seed is
        // picked, creating a different terrain each time.
        if (generateRandomSeed || seed == 0) // Generate a new random seed if specified, or if default 0
        {
            seed = Random.Range(1, 100000); // UnityEngine.Random for initial random seed
        }
        prng = new System.Random(seed); // Initialize System.Random for deterministic noise offsets.

        // Update noiseOffset based on the seed. This adds more variation when generating random seeds,
        // as it shifts the sampling point of the Perlin noise, producing a different "slice" of the noise.
        noiseOffset = new Vector2((float)prng.NextDouble() * 10000f, (float)prng.NextDouble() * 10000f);


        // 3. Create a Parent Container for Organization
        // Grouping all generated objects under a single parent keeps the Unity Hierarchy clean
        // and makes it easy to manipulate (move, scale, delete) the entire terrain as one unit.
        terrainContainer = new GameObject("ProceduralTerrain");
        // Make the container a child of the generator object itself.
        terrainContainer.transform.SetParent(this.transform);
        terrainContainer.transform.localPosition = Vector3.zero; // Reset its local position relative to generator.

        // 4. Main Generation Loop: Iterate through each point in our 2D grid.
        // For each (x, z) coordinate, we determine its height and then instantiate a block.
        for (int x = 0; x < terrainWidth; x++)
        {
            for (int z = 0; z < terrainDepth; z++)
            {
                // Calculate the height for the current block using fractal Perlin Noise.
                // This is the core of the procedural generation, determining the shape of the terrain.
                float height = CalculatePerlinNoiseHeight(x, z);

                // Instantiate a primitive cube. This represents a single 'block' or 'voxel' of our terrain.
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);

                // Apply the specified material to the block.
                if (blockMaterial != null)
                {
                    block.GetComponent<Renderer>().sharedMaterial = blockMaterial;
                }
                else
                {
                    Debug.LogWarning("No material assigned for terrain blocks. Using default Unity material. " +
                                     "Please assign a material to 'Block Material' in the Inspector.");
                }

                // Set the block's position based on its grid coordinates and the calculated height.
                // We multiply by blockSize to ensure proper spacing and sizing of the grid.
                block.transform.position = new Vector3(x * blockSize, height * terrainHeightScale, z * blockSize);

                // Set the block's scale. This ensures each block has the desired 'blockSize' dimension.
                block.transform.localScale = Vector3.one * blockSize;

                // Parent the newly created block to the terrainContainer for organization.
                block.transform.SetParent(terrainContainer.transform);
            }
        }

        Debug.Log($"Procedural Terrain Generated! Width: {terrainWidth}, Depth: {terrainDepth}, Seed: {seed}");
    }

    /// <summary>
    /// Calculates the height value for a given (x, z) coordinate using fractal Perlin noise.
    /// Fractal noise combines multiple layers (octaves) of Perlin noise to add detail and complexity.
    /// </summary>
    /// <param name="x">The x-coordinate in the grid.</param>
    /// <param name="z">The z-coordinate in the grid.</param>
    /// <returns>A float representing the normalized height at the given coordinates, typically between 0 and 1.</returns>
    private float CalculatePerlinNoiseHeight(int x, int z)
    {
        float noiseHeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxPossibleHeight = 0f; // Used to normalize the final noise value later

        // Loop through each octave to add layers of noise.
        for (int i = 0; i < octaves; i++)
        {
            // Calculate sample coordinates for Perlin noise.
            // We divide by noiseScale to control the overall 'zoom' of the noise.
            // Frequency is applied to each octave to make them progressively finer.
            // noiseOffset is added to shift the sampling position.
            float sampleX = (x / noiseScale * frequency) + noiseOffset.x;
            float sampleZ = (z / noiseScale * frequency) + noiseOffset.y;

            // Get Perlin noise value, which ranges from [0, 1].
            // We then remap it to [-1, 1] for symmetrical amplitude contribution.
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;

            // Add the current octave's contribution to the total noise height.
            noiseHeight += perlinValue * amplitude;

            // Accumulate the maximum possible amplitude to normalize the final height.
            maxPossibleHeight += amplitude;

            // Adjust amplitude and frequency for the next octave.
            // Persistence (gain) reduces the amplitude, making later octaves less impactful on overall height.
            // Lacunarity (roughness) increases the frequency, making later octaves add finer detail.
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize the noiseHeight.
        // (noiseHeight + maxPossibleHeight) maps the range [-maxPossibleHeight, maxPossibleHeight] to [0, 2*maxPossibleHeight].
        // Dividing by (2 * maxPossibleHeight) then maps it to a final range of [0, 1].
        // This ensures the height values are consistently scaled regardless of the octave parameters.
        return (noiseHeight + maxPossibleHeight) / (2f * maxPossibleHeight);
    }

    /// <summary>
    /// Clears any previously generated terrain blocks by safely destroying the terrain container GameObject.
    /// This is vital for managing resources and preventing clutter, especially during editor-time generation.
    /// </summary>
    private void ClearTerrain()
    {
        // Check if a terrain container already exists.
        if (terrainContainer != null)
        {
            // Use DestroyImmediate in editor mode to remove objects instantly.
            // Use Destroy in play mode for proper garbage collection.
            if (Application.isEditor && !Application.isPlaying)
            {
                DestroyImmediate(terrainContainer);
            }
            else
            {
                Destroy(terrainContainer);
            }
        }
    }
}

/*
/// --- EXAMPLE USAGE IN UNITY ---
///
/// To implement and experiment with this ProceduralTerrainGenerator in your Unity project:
///
/// 1.  **Create the Script**:
///     - In your Unity Project window, navigate to a desired folder (e.g., 'Assets/Scripts').
///     - Right-click -> Create -> C# Script. Name it `ProceduralTerrainGenerator`.
///     - Copy and paste the complete code above into this new script, overwriting its default content.
///
/// 2.  **Create a Generator GameObject**:
///     - In your Unity Hierarchy window, right-click -> Create Empty.
///     - Rename this new GameObject to `TerrainGenerator`.
///
/// 3.  **Attach the Script**:
///     - Drag and Drop the `ProceduralTerrainGenerator` script from your Project window
///       onto the `TerrainGenerator` GameObject in your Hierarchy.
///
/// 4.  **Assign a Material**:
///     - The generated blocks need a material to render correctly.
///     - In your Project window, right-click -> Create -> Material. Name it `TerrainBlockMaterial`.
///     - You can adjust its color (e.g., green for grass, brown for dirt) in the Inspector.
///     - Drag this `TerrainBlockMaterial` from your Project window to the 'Block Material' slot
///       in the Inspector of your `TerrainGenerator` GameObject.
///
/// 5.  **Observe and Tweak in Editor**:
///     - With the `TerrainGenerator` GameObject selected, you'll see all the parameters in the Inspector.
///     - Due to `[ExecuteInEditMode]` and `OnValidate()`, as you adjust parameters like
///       'Terrain Width', 'Terrain Depth', 'Noise Scale', 'Octaves', or 'Terrain Height Scale',
///       the terrain will *automatically regenerate and update* live in your Scene View.
///     - Experiment with different values to understand their effect on the terrain's appearance.
///     - Set `Generate Random Seed` to `true` and then change any other parameter to get a completely new, random terrain.
///       Otherwise, enter a specific number in `Seed` for reproducible results.
///
/// 6.  **Run in Play Mode**:
///     - Press the Play button in Unity. The terrain will be generated again based on the current
///       Inspector parameters. If `generateRandomSeed` is true, a new unique terrain will appear.
///
/// This setup provides a powerful and interactive way to design and iterate on procedural content
/// directly within the Unity editor, making the learning and development process highly efficient.
*/
```