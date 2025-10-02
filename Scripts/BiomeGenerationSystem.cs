// Unity Design Pattern Example: BiomeGenerationSystem
// This script demonstrates the BiomeGenerationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Biome Generation System** pattern in Unity. This pattern provides a flexible and data-driven way to define, organize, and apply different biome rules to procedurally generated terrain.

**Core Idea:**
The system uses `ScriptableObject`s to define `BiomeData` (the characteristics of a biome) and `[System.Serializable]` classes (`BiomeLayer`) to define rules for applying those biomes based on environmental factors (like height, temperature, humidity, and a general biome noise). A central `BiomeGenerationSystem` orchestrates the process, applying these rules to a Unity `Terrain`.

**Components of the Pattern:**

1.  **`BiomeData` (ScriptableObject):**
    *   Defines the properties of a specific biome (e.g., what textures it uses, what trees and detail objects grow there, its preferred height range, etc.).
    *   Allows designers to create and configure biomes as assets in the Project window.

2.  **`BiomeLayer` (Serializable Class):**
    *   Acts as a rule that links a `BiomeData` to specific environmental conditions.
    *   Defines thresholds (e.g., `minHeightThreshold`, `maxTemperatureThreshold`) that determine when this biome should be applied.
    *   This class is `[System.Serializable]` so it can be embedded directly into the `BiomeGenerationSystem`'s Inspector, allowing for easy configuration of generation rules.

3.  **`BiomeGenerationSystem` (MonoBehaviour):**
    *   The central orchestrator. It holds a list of `BiomeLayer`s.
    *   Takes a `Terrain` component as input.
    *   Iterates through the terrain's heightmap points, calculating various noise values (height, temperature, humidity, biome distribution).
    *   For each point, it evaluates the `BiomeLayer`s based on their criteria to determine the appropriate biome.
    *   Applies the chosen biome's properties (height, texture, trees, details) to the `TerrainData`.

This separation of concerns makes the system highly modular and extensible. You can add new biomes or new rules without modifying the core generation logic.

---

### **1. `BiomeData.cs` (ScriptableObject for Biome Properties)**

This asset defines what a specific biome *is*.

```csharp
// BiomeData.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject defining the properties and assets for a specific biome.
/// </summary>
[CreateAssetMenu(fileName = "Biome_", menuName = "Biome Generation/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Tooltip("A unique name for this biome.")]
    public string biomeName = "New Biome";

    [Header("Terrain Height Settings")]
    [Tooltip("Multiplier for the base height in this biome. Higher values create taller features.")]
    public float heightMultiplier = 1f;
    [Tooltip("Offset added to the base height. Useful for elevating or lowering entire biomes.")]
    public float heightOffset = 0f;

    [Header("Terrain Texture Settings")]
    [Tooltip("The index of the terrain layer (texture) to apply for this biome. Corresponds to the order in the Terrain's Paint Texture list.")]
    public int terrainLayerIndex = 0; // Index in TerrainData.terrainLayers
    [Tooltip("How smoothly to blend this biome's texture with adjacent biomes.")]
    [Range(0, 1)]
    public float textureBlendFactor = 0.5f;

    [Header("Tree Settings")]
    [Tooltip("A list of tree prefabs that can spawn in this biome.")]
    public List<GameObject> treePrefabs;
    [Tooltip("The density of trees in this biome (0-1).")]
    [Range(0, 1)]
    public float treeDensity = 0.05f;
    [Tooltip("Minimum scale of trees.")]
    public Vector3 minTreeScale = new Vector3(0.8f, 0.8f, 0.8f);
    [Tooltip("Maximum scale of trees.")]
    public Vector3 maxTreeScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Header("Detail (Grass/Rock) Settings")]
    [Tooltip("A list of detail prototypes (e.g., grass textures, rock prefabs) that can spawn in this biome.")]
    public List<GameObject> detailPrefabs; // Use GameObjects for detail prototypes
    [Tooltip("The density of details (grass/rocks) in this biome (0-1).")]
    [Range(0, 1)]
    public float detailDensity = 0.8f;
    [Tooltip("Minimum height of details.")]
    public float minDetailHeight = 0.5f;
    [Tooltip("Maximum height of details.")]
    public float maxDetailHeight = 1.5f;
    [Tooltip("Minimum width of details.")]
    public float minDetailWidth = 0.5f;
    [Tooltip("Maximum width of details.")]
    public float maxDetailWidth = 1.5f;

    // You can add more biome-specific properties here, e.g.,
    // public List<EnemySpawnTable> enemies;
    // public Color skyTint;
    // public ParticleSystem weatherEffect;
}
```

---

### **2. `BiomeLayer.cs` (Serializable Class for Biome Rules)**

This class defines a rule for when a `BiomeData` should be applied.

```csharp
// BiomeLayer.cs
using UnityEngine;
using System;

/// <summary>
/// Defines a rule for applying a specific BiomeData based on environmental conditions.
/// </summary>
[Serializable]
public class BiomeLayer
{
    [Tooltip("The BiomeData asset associated with this layer.")]
    public BiomeData biome;

    [Header("Condition Thresholds (0-1 normalized)")]
    [Tooltip("Minimum normalized height (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float minHeightThreshold = 0f;
    [Tooltip("Maximum normalized height (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float maxHeightThreshold = 1f;

    [Tooltip("Minimum normalized temperature (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float minTemperatureThreshold = 0f;
    [Tooltip("Maximum normalized temperature (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float maxTemperatureThreshold = 1f;

    [Tooltip("Minimum normalized humidity (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float minHumidityThreshold = 0f;
    [Tooltip("Maximum normalized humidity (0-1) for this biome to apply.")]
    [Range(0, 1)]
    public float maxHumidityThreshold = 1f;

    [Tooltip("A threshold based on a separate 'biome noise' to create patchy distribution within height/temp/humidity ranges.")]
    [Range(0, 1)]
    public float biomeNoiseThreshold = 0.5f;

    /// <summary>
    /// Checks if this biome layer applies to the given environmental conditions.
    /// </summary>
    /// <param name="normalizedHeight">The current normalized height (0-1).</param>
    /// <param name="normalizedTemperature">The current normalized temperature (0-1).</param>
    /// <param name="normalizedHumidity">The current normalized humidity (0-1).</param>
    /// <param name="biomeNoiseValue">A noise value used for patchy biome distribution (0-1).</param>
    /// <returns>True if this biome layer applies, false otherwise.</returns>
    public bool DoesApply(float normalizedHeight, float normalizedTemperature, float normalizedHumidity, float biomeNoiseValue)
    {
        if (biome == null)
        {
            Debug.LogWarning("BiomeLayer has no BiomeData assigned!");
            return false;
        }

        return normalizedHeight >= minHeightThreshold && normalizedHeight <= maxHeightThreshold &&
               normalizedTemperature >= minTemperatureThreshold && normalizedTemperature <= maxTemperatureThreshold &&
               normalizedHumidity >= minHumidityThreshold && normalizedHumidity <= maxHumidityThreshold &&
               biomeNoiseValue >= biomeNoiseThreshold; // This threshold makes biomes appear in patches
    }
}
```

---

### **3. `BiomeGenerationSystem.cs` (The Orchestrator)**

This `MonoBehaviour` ties everything together, generates noise, applies rules, and modifies the `Terrain`.

```csharp
// BiomeGenerationSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Distinct() and .ToArray()

/// <summary>
/// The central system that orchestrates biome generation on a Unity Terrain.
/// It uses BiomeData assets and BiomeLayer rules to procedurally generate terrain features.
/// </summary>
[RequireComponent(typeof(Terrain))] // Ensures there's a Terrain component
public class BiomeGenerationSystem : MonoBehaviour
{
    [Tooltip("The target Unity Terrain component to generate biomes on.")]
    public Terrain targetTerrain;

    [Header("Terrain Dimensions")]
    [Tooltip("The resolution of the heightmap. Must be (2^n) + 1 (e.g., 513, 1025).")]
    public int heightmapResolution = 513;
    [Tooltip("The overall size of the terrain in world units (x, z).")]
    public float terrainSize = 1000f;
    [Tooltip("The maximum possible height of the terrain in world units.")]
    public float terrainMaxHeight = 600f;

    [Header("Noise Settings")]
    [Tooltip("Scale for the global height (Perlin) noise. Smaller values create larger features.")]
    public float globalHeightNoiseScale = 0.01f;
    [Tooltip("Offset for the global height noise, useful for varying generation with same seed.")]
    public Vector2 globalHeightNoiseOffset = new Vector2(0f, 0f);

    [Tooltip("Scale for temperature (Perlin) noise.")]
    public float temperatureNoiseScale = 0.015f;
    [Tooltip("Offset for temperature noise.")]
    public Vector2 temperatureNoiseOffset = new Vector2(100f, 100f);

    [Tooltip("Scale for humidity (Perlin) noise.")]
    public float humidityNoiseScale = 0.012f;
    [Tooltip("Offset for humidity noise.")]
    public Vector2 humidityNoiseOffset = new Vector2(200f, 200f);

    [Tooltip("Scale for biome distribution (Perlin) noise. Used to create patchy biomes.")]
    public float biomeNoiseScale = 0.02f;
    [Tooltip("Offset for biome distribution noise.")]
    public Vector2 biomeNoiseOffset = new Vector2(300f, 300f);

    [Header("Biome Layers")]
    [Tooltip("Define the order and rules for your biomes. The first matching layer will be applied.")]
    public List<BiomeLayer> biomeLayers = new List<BiomeLayer>();

    // Private working data for terrain generation
    private float[,] heightMap;         // Stores normalized height values (0-1)
    private float[,,] splatMap;         // Stores texture weights for each terrain layer
    private List<TreeInstance> treeInstances; // Collects all tree instances to be placed
    private int[,] detailMap;           // Stores detail (grass/rock) density for a specific detail layer

    // Cached TerrainData for efficient access
    private TerrainData terrainData;

    void Awake()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null)
        {
            Debug.LogError("BiomeGenerationSystem requires a Terrain component or a target terrain to be assigned.");
            enabled = false;
            return;
        }

        terrainData = targetTerrain.terrainData;

        // Ensure terrain data is set up if it's a new terrain
        if (terrainData.heightmapResolution != heightmapResolution)
        {
            Debug.Log($"Adjusting terrain heightmap resolution from {terrainData.heightmapResolution} to {heightmapResolution}");
            terrainData.heightmapResolution = heightmapResolution;
        }

        terrainData.size = new Vector3(terrainSize, terrainMaxHeight, terrainSize);
    }

    /// <summary>
    /// Entry point to start the biome generation process.
    /// Can be called from the Inspector (via a button) or from other scripts.
    /// </summary>
    [ContextMenu("Generate Biomes")] // Add a button to the Inspector
    public void GenerateTerrainBiomes()
    {
        Debug.Log("Starting Biome Generation...");

        if (targetTerrain == null || terrainData == null)
        {
            Debug.LogError("Terrain or TerrainData is not initialized.");
            return;
        }
        if (biomeLayers.Count == 0)
        {
            Debug.LogWarning("No biome layers defined. Terrain will be flat and empty.");
        }

        // 1. Initialize Terrain Layers (textures), Tree Prototypes, and Detail Prototypes
        SetupTerrainPrototypes();

        // 2. Initialize working arrays
        InitializeGenerationArrays();

        // 3. Generate Heightmap, Splatmap, and collect Tree/Detail data
        ProcessTerrainPoints();

        // 4. Apply all collected data to the Terrain
        ApplyDataToTerrain();

        Debug.Log("Biome Generation Complete!");
    }

    /// <summary>
    /// Sets up the TerrainData's terrain layers (textures), tree prototypes, and detail prototypes
    /// based on the BiomeData assets used in the BiomeLayers.
    /// </summary>
    private void SetupTerrainPrototypes()
    {
        // Collect all unique TerrainLayers required by BiomeData assets
        List<TerrainLayer> uniqueTerrainLayers = new List<TerrainLayer>();
        // Collect all unique Tree Prefabs required by BiomeData assets
        List<GameObject> uniqueTreePrefabs = new List<GameObject>();
        // Collect all unique Detail Prefabs required by BiomeData assets
        List<GameObject> uniqueDetailPrefabs = new List<GameObject>();

        foreach (BiomeLayer layer in biomeLayers)
        {
            if (layer.biome == null) continue;

            // Texture layers are assumed to be set up manually on the Terrain for this example
            // In a more complex system, you might create TerrainLayer assets dynamically.
            // For now, we just ensure the BiomeData's terrainLayerIndex is valid.
            // We'll rely on the user to ensure the Terrain has enough layers for the indices used.

            if (layer.biome.treePrefabs != null)
            {
                uniqueTreePrefabs.AddRange(layer.biome.treePrefabs);
            }
            if (layer.biome.detailPrefabs != null)
            {
                uniqueDetailPrefabs.AddRange(layer.biome.detailPrefabs);
            }
        }

        // Apply unique tree prefabs to TerrainData
        terrainData.treePrototypes = uniqueTreePrefabs
            .Distinct() // Ensure no duplicates
            .Where(p => p != null)
            .Select(p => new TreePrototype { prefab = p })
            .ToArray();

        // Apply unique detail prefabs to TerrainData
        terrainData.detailPrototypes = uniqueDetailPrefabs
            .Distinct() // Ensure no duplicates
            .Where(p => p != null)
            .Select(p => new DetailPrototype
            {
                prototype = p,
                renderMode = DetailRenderMode.GrassBillboard, // Or Grass, for meshes
                minHeight = 1,
                maxHeight = 2, // These will be overwritten by biome-specific values
                minWidth = 1,
                maxWidth = 2,
                dryColor = Color.white,
                healthyColor = Color.white
            })
            .ToArray();

        // Clear existing trees and details on the terrain
        terrainData.SetTreeInstances(new List<TreeInstance>().ToArray(), true);
        terrainData.SetDetailResolution(heightmapResolution, 8); // Set detail resolution (must match heightmap for 1:1 mapping)
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            terrainData.SetDetailLayer(0, 0, i, new int[heightmapResolution, heightmapResolution]); // Clear all detail layers
        }
    }


    /// <summary>
    /// Initializes all necessary arrays for heightmap, splatmap, trees, and details.
    /// </summary>
    private void InitializeGenerationArrays()
    {
        int splatMapResolution = terrainData.alphamapResolution;
        int numTerrainLayers = terrainData.alphamapLayers;

        heightMap = new float[heightmapResolution, heightmapResolution];
        splatMap = new float[splatMapResolution, splatMapResolution, numTerrainLayers];
        treeInstances = new List<TreeInstance>();

        // Initialize detailMap only for the first detail prototype as an example.
        // In a full system, you'd manage a detailMap for each DetailPrototype index.
        detailMap = new int[terrainData.detailResolution, terrainData.detailResolution];
    }

    /// <summary>
    /// Iterates through each point on the terrain, calculates environmental factors,
    /// finds the appropriate biome, and populates the generation arrays.
    /// </summary>
    private void ProcessTerrainPoints()
    {
        // Get the resolution for splatmap and detailmap, which can differ from heightmap
        int splatMapRes = terrainData.alphamapResolution;
        int detailRes = terrainData.detailResolution;

        for (int z = 0; z < heightmapResolution; z++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                // Calculate normalized coordinates (0-1)
                float normX = (float)x / (heightmapResolution - 1);
                float normZ = (float)z / (heightmapResolution - 1);

                // --- 1. Calculate environmental factors using Perlin noise ---
                // Global Height Noise
                float rawHeightNoise = Mathf.PerlinNoise(
                    (normX * globalHeightNoiseScale) + globalHeightNoiseOffset.x,
                    (normZ * globalHeightNoiseScale) + globalHeightNoiseOffset.y
                );

                // Temperature Noise
                float temperature = Mathf.PerlinNoise(
                    (normX * temperatureNoiseScale) + temperatureNoiseOffset.x,
                    (normZ * temperatureNoiseScale) + temperatureNoiseOffset.y
                );

                // Humidity Noise
                float humidity = Mathf.PerlinNoise(
                    (normX * humidityNoiseScale) + humidityNoiseOffset.x,
                    (normZ * humidityNoiseScale) + humidityNoiseOffset.y
                );

                // Biome Distribution Noise (for patchy biomes within ranges)
                float biomeDistNoise = Mathf.PerlinNoise(
                    (normX * biomeNoiseScale) + biomeNoiseOffset.x,
                    (normZ * biomeNoiseScale) + biomeNoiseOffset.y
                );

                // --- 2. Find the appropriate BiomeLayer ---
                BiomeData activeBiome = null;
                foreach (BiomeLayer layer in biomeLayers)
                {
                    if (layer.DoesApply(rawHeightNoise, temperature, humidity, biomeDistNoise))
                    {
                        activeBiome = layer.biome;
                        break; // Found the first matching biome, apply it. (Order matters!)
                    }
                }

                if (activeBiome == null)
                {
                    // Fallback biome or default behavior if no biome matches.
                    // For this example, if no biome matches, it will default to minimal height and first texture.
                    activeBiome = biomeLayers.Count > 0 ? biomeLayers[0].biome : null; // Fallback to first biome if available
                    if (activeBiome == null) continue; // Really no biomes, skip this point.
                }

                // --- 3. Apply biome properties ---
                // Heightmap
                heightMap[x, z] = (rawHeightNoise * activeBiome.heightMultiplier) + activeBiome.heightOffset;
                heightMap[x, z] = Mathf.Clamp01(heightMap[x, z]); // Ensure height is 0-1

                // Splatmap (Terrain Textures)
                // Convert heightmap coordinates to splatmap coordinates
                int splatX = Mathf.FloorToInt(normX * (splatMapRes - 1));
                int splatZ = Mathf.FloorToInt(normZ * (splatMapRes - 1));

                // Clear previous texture weights for this point
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    splatMap[splatZ, splatX, i] = 0;
                }
                // Apply active biome's texture weight
                if (activeBiome.terrainLayerIndex < terrainData.alphamapLayers)
                {
                    splatMap[splatZ, splatX, activeBiome.terrainLayerIndex] = 1; // Purely set, no blending here
                }
                // Note: For smooth texture blending, you would calculate weights for 
                // multiple biomes/layers based on height and blend factor.
                // This example uses a "first match wins" approach for simplicity.

                // Trees and Details (Sparse placement for performance)
                if (Random.value < activeBiome.treeDensity)
                {
                    // Convert normalized heightmap coordinate to world position for trees
                    Vector3 worldPos = GetWorldPosition(normX, normZ, heightMap[x, z]);
                    AddTreeToQueue(activeBiome, worldPos, x, z);
                }

                // Details (Grass/Rocks)
                // Convert heightmap coordinates to detailmap coordinates
                int detailX = Mathf.FloorToInt(normX * (detailRes - 1));
                int detailZ = Mathf.FloorToInt(normZ * (detailRes - 1));

                // For simplicity, we'll only assign density to the first detail prototype
                // A full system would iterate through all details and assign per detail prototype index.
                if (terrainData.detailPrototypes != null && terrainData.detailPrototypes.Length > 0)
                {
                    detailMap[detailZ, detailX] = Random.value < activeBiome.detailDensity ?
                                                 Mathf.RoundToInt(activeBiome.detailDensity * 16) : 0; // Up to 16 for grass density
                }
            }
        }
    }

    /// <summary>
    /// Helper to convert normalized heightmap coords to world position.
    /// </summary>
    private Vector3 GetWorldPosition(float normX, float normZ, float heightValue)
    {
        float worldX = normX * terrainSize;
        float worldZ = normZ * terrainSize;
        float worldY = heightValue * terrainMaxHeight;
        return new Vector3(worldX, worldY, worldZ);
    }

    /// <summary>
    /// Adds a tree instance to the queue, finding its corresponding prototype index.
    /// </summary>
    private void AddTreeToQueue(BiomeData biome, Vector3 worldPos, int x, int z)
    {
        if (biome.treePrefabs == null || biome.treePrefabs.Count == 0) return;

        // Randomly pick a tree prefab from the biome's list
        GameObject treePrefab = biome.treePrefabs[Random.Range(0, biome.treePrefabs.Count)];
        if (treePrefab == null) return;

        // Find the index of this prefab in the terrainData.treePrototypes array
        int prototypeIndex = -1;
        for (int i = 0; i < terrainData.treePrototypes.Length; i++)
        {
            if (terrainData.treePrototypes[i].prefab == treePrefab)
            {
                prototypeIndex = i;
                break;
            }
        }

        if (prototypeIndex != -1)
        {
            TreeInstance tree = new TreeInstance
            {
                position = new Vector3(
                    worldPos.x / terrainSize,  // Normalized X (0-1)
                    worldPos.y / terrainMaxHeight, // Normalized Y (0-1)
                    worldPos.z / terrainSize   // Normalized Z (0-1)
                ),
                rotation = Random.Range(0f, 360f),
                widthScale = Random.Range(biome.minTreeScale.x, biome.maxTreeScale.x),
                heightScale = Random.Range(biome.minTreeScale.y, biome.maxTreeScale.y),
                prototypeIndex = prototypeIndex
            };
            treeInstances.Add(tree);
        }
        else
        {
            Debug.LogWarning($"Tree prefab {treePrefab.name} used in biome {biome.biomeName} not found in TerrainData.treePrototypes.");
        }
    }


    /// <summary>
    /// Applies all generated data (heightmap, splatmap, trees, details) to the actual Unity Terrain.
    /// </summary>
    private void ApplyDataToTerrain()
    {
        terrainData.SetHeights(0, 0, heightMap);
        terrainData.SetAlphamaps(0, 0, splatMap); // Set splatmap for texturing

        // Apply trees
        terrainData.SetTreeInstances(treeInstances.ToArray(), true);

        // Apply details (grass/rocks). This example only sets for the first prototype index.
        if (terrainData.detailPrototypes != null && terrainData.detailPrototypes.Length > 0)
        {
            // For a full system, you would manage a separate detailMap for each detail prototype.
            // This example uses a single detailMap and applies it to the first prototype.
            terrainData.SetDetailLayer(0, 0, 0, detailMap);

            // Update prototype properties based on active biome data (can be done per biome, but here global for simplicity)
            DetailPrototype[] prototypes = terrainData.detailPrototypes;
            for(int i = 0; i < prototypes.Length; i++)
            {
                // This is a simplification. Ideally, each detail should have its biome-specific properties applied.
                // For a dynamic system, you might create new prototypes or update existing ones based on the active biome.
                prototypes[i].minHeight = biomeLayers.FirstOrDefault()?.biome?.minDetailHeight ?? 0.5f;
                prototypes[i].maxHeight = biomeLayers.FirstOrDefault()?.biome?.maxDetailHeight ?? 1.5f;
                prototypes[i].minWidth = biomeLayers.FirstOrDefault()?.biome?.minDetailWidth ?? 0.5f;
                prototypes[i].maxWidth = biomeLayers.FirstOrDefault()?.biome?.maxDetailWidth ?? 1.5f;
            }
            terrainData.detailPrototypes = prototypes;
        }

        // Refresh terrain
        targetTerrain.Flush();
        targetTerrain.heightmapMaximumLOD = 0; // Ensure full resolution heightmap is used
        targetTerrain.drawTreesAndFoliage = true; // Ensure trees and foliage are drawn
    }
}
```

---

### **How to Use in Unity (Example Setup):**

1.  **Create a new Unity Project** or open an existing one.
2.  **Create a new 3D Object -> Terrain** in your scene. Name it `MyTerrain`.
3.  **Attach the `BiomeGenerationSystem.cs` script** to `MyTerrain`.
4.  **Create BiomeData Assets:**
    *   In your Project window, right-click -> `Create` -> `Biome Generation` -> `Biome Data`.
    *   Create a few: e.g., `Biome_Grassland`, `Biome_Desert`, `Biome_Mountain`, `Biome_Water`.
    *   **Configure each `BiomeData`:**
        *   **`Biome_Grassland`:**
            *   `Biome Name`: Grassland
            *   `Height Multiplier`: 0.3
            *   `Height Offset`: 0.1
            *   `Terrain Layer Index`: `0` (Assumes your first terrain texture is grass)
            *   `Tree Prefabs`: Add some tree prefabs (e.g., Unity's default 'Tree' or your own).
            *   `Detail Prefabs`: Add some grass/bush prefabs.
            *   `Tree Density`: 0.05
            *   `Detail Density`: 0.8
        *   **`Biome_Desert`:**
            *   `Biome Name`: Desert
            *   `Height Multiplier`: 0.1
            *   `Height Offset`: 0.05
            *   `Terrain Layer Index`: `1` (Assumes your second terrain texture is sand)
            *   `Tree Prefabs`: Add some cactus prefabs if you have any.
            *   `Detail Prefabs`: Add some sparse dry grass/rock prefabs.
            *   `Tree Density`: 0.01
            *   `Detail Density`: 0.2
        *   **`Biome_Mountain`:**
            *   `Biome Name`: Mountain
            *   `Height Multiplier`: 0.8
            *   `Height Offset`: 0.2
            *   `Terrain Layer Index`: `2` (Assumes your third terrain texture is rock/mountain)
            *   `Tree Prefabs`: Add some pine tree prefabs.
            *   `Detail Prefabs`: Add some sparse rocks.
            *   `Tree Density`: 0.02
            *   `Detail Density`: 0.1
        *   **`Biome_Water` (for shores/shallow water):**
            *   `Biome Name`: Water
            *   `Height Multiplier`: 0.05 (keep it low)
            *   `Height Offset`: 0.0 (or slightly negative)
            *   `Terrain Layer Index`: `3` (Assumes a water/wet sand texture)
            *   `Tree Prefabs`: (leave empty or add reeds)
            *   `Detail Prefabs`: (leave empty or add water plants)
            *   `Tree Density`: 0.0
            *   `Detail Density`: 0.0
5.  **Configure Terrain Layers (Textures) on `MyTerrain`:**
    *   Select `MyTerrain` in the Hierarchy.
    *   In the Inspector, go to the `Paint Terrain` tab (brush icon).
    *   Click `Add Layer` -> `Create New Layer`.
    *   Add 4-5 `TerrainLayer` assets (e.g., `Grass_Layer`, `Sand_Layer`, `Rock_Layer`, `Water_Layer`). You can use Unity's default grass/rock textures or import your own. The order here directly corresponds to the `terrainLayerIndex` you set in `BiomeData`.
6.  **Configure Tree and Detail Prefabs:**
    *   For trees, drag some `GameObject` prefabs into your `BiomeData`'s `Tree Prefabs` list. You can use assets from the Unity Standard Assets (Environments/SpeedTree) or your own models.
    *   For details, drag some small grass or rock `GameObject` prefabs into your `BiomeData`'s `Detail Prefabs` list. For detail prototypes, Unity expects either a texture (for billboard grass) or a small mesh prefab. If using a mesh, ensure the `DetailPrototype` `renderMode` in `SetupTerrainPrototypes` is `DetailRenderMode.VertexLit` or `Grass`. (The provided code uses `GrassBillboard` by default, which works best with simple textured quads for grass).
7.  **Configure Biome Layers on `BiomeGenerationSystem`:**
    *   Select `MyTerrain` in the Hierarchy.
    *   In the Inspector, expand the `Biome Generation System` component.
    *   Expand `Biome Layers` and set the `Size` to 4 (or how many biomes you have).
    *   **Drag your `BiomeData` assets into the `Biome` field for each layer.**
    *   **Set the Thresholds for each `BiomeLayer` (Order matters!):**
        *   **Layer 0 (Water):**
            *   `Biome`: `Biome_Water`
            *   `Min Height Threshold`: 0.0
            *   `Max Height Threshold`: 0.1 (low height)
            *   `Min Temperature Threshold`: 0.3
            *   `Max Temperature Threshold`: 0.7
            *   `Biome Noise Threshold`: 0.0 (always applies if height/temp/humidity matches)
        *   **Layer 1 (Desert):**
            *   `Biome`: `Biome_Desert`
            *   `Min Height Threshold`: 0.05
            *   `Max Height Threshold`: 0.3
            *   `Min Temperature Threshold`: 0.7 (hot)
            *   `Max Temperature Threshold`: 1.0
            *   `Biome Noise Threshold`: 0.0
        *   **Layer 2 (Grassland):**
            *   `Biome`: `Biome_Grassland`
            *   `Min Height Threshold`: 0.1
            *   `Max Height Threshold`: 0.6
            *   `Min Temperature Threshold`: 0.2
            *   `Max Temperature Threshold`: 0.8
            *   `Biome Noise Threshold`: 0.0
        *   **Layer 3 (Mountain):**
            *   `Biome`: `Biome_Mountain`
            *   `Min Height Threshold`: 0.5 (high height)
            *   `Max Height Threshold`: 1.0
            *   `Min Temperature Threshold`: 0.0
            *   `Max Temperature Threshold`: 0.5 (cold)
            *   `Biome Noise Threshold`: 0.0
        *   *(You can adjust `Biome Noise Threshold` to make biomes patchy. E.g., for a "Forest" biome, you might set `Min Height: 0.2`, `Max Height: 0.5`, `Biome Noise Threshold: 0.5` to make forests only appear in certain noise patches within the valid height range.)*
8.  **Adjust Global Noise Settings:** Play with `globalHeightNoiseScale`, `temperatureNoiseScale`, `humidityNoiseScale`, and `biomeNoiseScale` on the `BiomeGenerationSystem` component to get different terrain shapes and biome distributions.
9.  **Generate!**
    *   Right-click on the `Biome Generation System` component in the Inspector.
    *   Select `Generate Biomes`.

You should now see your terrain generated with different biomes, textures, trees, and details based on your configured rules! You can adjust the `Noise Offset` values to generate different terrains with the same settings.