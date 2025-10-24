// Unity Design Pattern Example: TerrainBlendingSystem
// This script demonstrates the TerrainBlendingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'TerrainBlendingSystem' design pattern, as implemented here, uses the **Strategy pattern** to dynamically apply different blending rules (strategies) to a Unity Terrain. It centralizes the terrain modification logic in a `TerrainBlendingSystem` (the Context) and delegates specific blending calculations (e.g., blend by height, blend by slope, blend by noise) to separate `BlendingRule` ScriptableObjects (the Strategies). This makes the system highly extensible, allowing developers to add new blending behaviors without modifying the core system.

---

```csharp
using UnityEngine;
using System.Collections.Generic;

// ====================================================================================
// DESIGN PATTERN: TERRAIN BLENDING SYSTEM
//
// This implementation demonstrates the 'Terrain Blending System' pattern,
// which is a practical application of the Strategy design pattern in Unity.
//
// Pattern Components:
// 1.  TerrainBlendingSystem (Context):
//     -   A MonoBehaviour that orchestrates the blending process.
//     -   Holds a list of 'BlendingRule' ScriptableObjects.
//     -   Initializes the terrain's splatmap (alphamap).
//     -   Iterates through each rule, collects its texture contributions.
//     -   Normalizes the combined contributions to ensure proper blending.
//     -   Applies the final splatmap to the Unity Terrain.
//
// 2.  BlendingRule (Strategy Interface / Abstract Base Class):
//     -   An abstract ScriptableObject class that defines the common interface
//         for all blending rules.
//     -   Each concrete rule will inherit from this base class.
//     -   It includes properties common to all rules (e.g., target texture index, strength).
//     -   Defines an abstract method `GenerateContribution` that concrete rules must implement.
//
// 3.  ConcreteBlendingRules (Concrete Strategies):
//     -   Specific implementations of `BlendingRule` (e.g., HeightBlendingRule,
//         SlopeBlendingRule, PerlinNoiseBlendingRule).
//     -   Each concrete rule contains the unique logic to calculate
//         its texture's influence based on terrain properties.
//     -   They are ScriptableObjects, allowing them to be created and configured
//         independently in the Unity Editor without modifying code.
//
// Benefits:
// -   **Extensibility:** Easily add new blending rules (strategies) by creating new
//     ScriptableObject classes inheriting from `BlendingRule` without changing
//     the `TerrainBlendingSystem`.
// -   **Flexibility:** Combine multiple rules in various orders and configurations
//     to achieve complex terrain texturing.
// -   **Separation of Concerns:** Each rule focuses on a single blending logic,
//     keeping code clean and manageable.
// -   **Data-Driven:** Blending configurations are data assets (ScriptableObjects)
//     that designers can modify directly in the editor.
// ====================================================================================

// --- Core Blending Rule Design Pattern ---

/// <summary>
/// Base class for all terrain blending rules.
/// This acts as the 'Strategy' interface in the Strategy design pattern.
/// Each concrete rule defines a specific way to determine a texture's influence
/// based on terrain properties like height, slope, or noise.
/// </summary>
public abstract class BlendingRule : ScriptableObject
{
    [Tooltip("The index of the TerrainLayer (texture) this rule primarily affects. " +
             "Corresponds to the order of layers in your Terrain component.")]
    public int targetTextureIndex = 0;

    [Tooltip("A global multiplier for the strength of this rule's contribution. " +
             "A value of 0 means no contribution, 1 means full contribution.")]
    [Range(0f, 1f)]
    public float globalStrength = 1.0f;

    /// <summary>
    /// Generates a 2D map of contribution values for the target texture.
    /// The values represent how much this rule wants to paint 'targetTextureIndex' at each point.
    /// The TerrainBlendingSystem will combine and normalize these contributions.
    /// </summary>
    /// <param name="alphaMapWidth">The width of the alphamap (splatmap) resolution.</param>
    /// <param name="alphaMapHeight">The height of the alphamap (splatmap) resolution.</param>
    /// <param name="terrainData">The TerrainData object containing heightmap, size, etc.</param>
    /// <param name="terrain">The Terrain component itself, useful for world position queries.</param>
    /// <returns>A 2D float array where `[x, y]` stores the contribution at that point.</returns>
    public abstract float[,] GenerateContribution(int alphaMapWidth, int alphaMapHeight, TerrainData terrainData, Terrain terrain);

    /// <summary>
    /// Helper function to get normalized height (0-1) from alphamap coordinates.
    /// Maps alphamap coordinates to heightmap coordinates for accurate height sampling.
    /// </summary>
    protected float GetNormalizedHeight(int x, int y, int alphaMapWidth, int alphaMapHeight, TerrainData terrainData)
    {
        // Map alphamap coords (0 to alphaMapWidth/Height-1) to normalized heightmap coords (0-1)
        float u = (float)x / (alphaMapWidth - 1);
        float v = (float)y / (alphaMapHeight - 1);
        return terrainData.GetInterpolatedHeight(u, v) / terrainData.size.y; // Normalize to 0-1 range of terrain height
    }

    /// <summary>
    /// Helper function to get the slope (steepness in degrees) from alphamap coordinates.
    /// Calculates the world position corresponding to the alphamap coordinate to query steepness.
    /// </summary>
    protected float GetSlope(int x, int y, int alphaMapWidth, int alphaMapHeight, TerrainData terrainData, Terrain terrain)
    {
        // Calculate normalized position within the terrain's bounds
        float normalizedX = (float)x / (alphaMapWidth - 1);
        float normalizedY = (float)y / (alphaMapHeight - 1);

        // Convert normalized terrain position to world position
        Vector3 terrainLocalPos = new Vector3(normalizedX * terrainData.size.x, 0, normalizedY * terrainData.size.z);
        Vector3 worldPos = terrain.GetPosition() + terrainLocalPos;

        return terrainData.GetSteepness(worldPos.x, worldPos.z); // Steepness in degrees (0-90)
    }
}

// --- Concrete Blending Rule Implementations ---

/// <summary>
/// A concrete <see cref="BlendingRule"/> that applies a texture based on the height of the terrain.
/// Typically used for layering textures like water (low), grass (mid), rock/snow (high).
/// </summary>
[CreateAssetMenu(fileName = "NewHeightBlendingRule", menuName = "Terrain Blending/Height Rule", order = 1)]
public class HeightBlendingRule : BlendingRule
{
    [Tooltip("The normalized height (0-1) below which the texture will not be applied.")]
    [Range(0f, 1f)]
    public float minHeight = 0.0f;

    [Tooltip("The normalized height (0-1) above which the texture will be fully applied.")]
    [Range(0f, 1f)]
    public float maxHeight = 1.0f;

    [Tooltip("How smoothly the blend occurs between minHeight and maxHeight. " +
             "Higher values make the blend sharper, lower values make it smoother.")]
    [Range(0.1f, 10.0f)]
    public float blendFalloff = 1.0f;

    public override float[,] GenerateContribution(int alphaMapWidth, int alphaMapHeight, TerrainData terrainData, Terrain terrain)
    {
        float[,] contributionMap = new float[alphaMapWidth, alphaMapHeight];

        for (int y = 0; y < alphaMapHeight; y++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                float normalizedHeight = GetNormalizedHeight(x, y, alphaMapWidth, alphaMapHeight, terrainData);

                // Calculate blend factor based on height range
                float factor = Mathf.InverseLerp(minHeight, maxHeight, normalizedHeight);
                factor = Mathf.Pow(factor, blendFalloff); // Apply falloff for smoother/sharper blend

                contributionMap[x, y] = factor * globalStrength;
            }
        }
        return contributionMap;
    }
}

/// <summary>
/// A concrete <see cref="BlendingRule"/> that applies a texture based on the slope (steepness) of the terrain.
/// Typically used for applying rock/cliff textures to steep areas and grass/dirt to flatter areas.
/// </summary>
[CreateAssetMenu(fileName = "NewSlopeBlendingRule", menuName = "Terrain Blending/Slope Rule", order = 2)]
public class SlopeBlendingRule : BlendingRule
{
    [Tooltip("The slope in degrees below which the texture will not be applied.")]
    [Range(0f, 90f)]
    public float minSlope = 0.0f;

    [Tooltip("The slope in degrees above which the texture will be fully applied.")]
    [Range(0f, 90f)]
    public float maxSlope = 90.0f;

    [Tooltip("How smoothly the blend occurs between minSlope and maxSlope. " +
             "Higher values make the blend sharper, lower values make it smoother.")]
    [Range(0.1f, 10.0f)]
    public float blendFalloff = 1.0f;

    public override float[,] GenerateContribution(int alphaMapWidth, int alphaMapHeight, TerrainData terrainData, Terrain terrain)
    {
        float[,] contributionMap = new float[alphaMapWidth, alphaMapHeight];

        for (int y = 0; y < alphaMapHeight; y++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                float slope = GetSlope(x, y, alphaMapWidth, alphaMapHeight, terrainData, terrain);

                // Calculate blend factor based on slope range
                float factor = Mathf.InverseLerp(minSlope, maxSlope, slope);
                factor = Mathf.Pow(factor, blendFalloff);

                contributionMap[x, y] = factor * globalStrength;
            }
        }
        return contributionMap;
    }
}

/// <summary>
/// A concrete <see cref="BlendingRule"/> that applies a texture based on Perlin noise, adding natural variation.
/// Useful for breaking up uniform areas or adding patchy details.
/// </summary>
[CreateAssetMenu(fileName = "NewPerlinNoiseBlendingRule", menuName = "Terrain Blending/Perlin Noise Rule", order = 3)]
public class PerlinNoiseBlendingRule : BlendingRule
{
    [Tooltip("The scale of the Perlin noise. Smaller values create larger noise features.")]
    public float noiseScale = 0.1f;

    [Tooltip("Offset for the noise, useful for getting different patterns.")]
    public Vector2 noiseOffset = Vector2.zero;

    [Tooltip("The noise value (0-1) below which the texture won't be applied. " +
             "A 'fuzzy' threshold is used for smoother blending.")]
    [Range(0f, 1f)]
    public float noiseThreshold = 0.5f;

    [Tooltip("How smoothly the blend occurs around the noiseThreshold. " +
             "Higher values make the blend sharper, lower values make it smoother.")]
    [Range(0.1f, 10.0f)]
    public float blendFalloff = 1.0f;

    public override float[,] GenerateContribution(int alphaMapWidth, int alphaMapHeight, TerrainData terrainData, Terrain terrain)
    {
        float[,] contributionMap = new float[alphaMapWidth, alphaMapHeight];

        for (int y = 0; y < alphaMapHeight; y++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                // Sample Perlin noise, mapping alphamap coordinates to noise coordinates
                float u = (float)x / alphaMapWidth * noiseScale + noiseOffset.x;
                float v = (float)y / alphaMapHeight * noiseScale + noiseOffset.y;

                float noiseValue = Mathf.PerlinNoise(u, v);

                // Calculate blend factor based on noise threshold with a small fuzzy band
                float factor = Mathf.InverseLerp(noiseThreshold - 0.1f, noiseThreshold + 0.1f, noiseValue);
                factor = Mathf.Pow(factor, blendFalloff);

                contributionMap[x, y] = factor * globalStrength;
            }
        }
        return contributionMap;
    }
}

// --- The Terrain Blending System ---

/// <summary>
/// The main TerrainBlendingSystem MonoBehaviour.
/// This acts as the 'Context' in the Strategy design pattern.
/// It orchestrates the application of multiple BlendingRules to a Unity Terrain,
/// generating or modifying its splatmap (alphamap) based on these rules.
/// </summary>
[RequireComponent(typeof(Terrain))] // Ensures a Terrain component is present on the GameObject
public class TerrainBlendingSystem : MonoBehaviour
{
    [Tooltip("The Terrain component to apply blending to.")]
    public Terrain targetTerrain;

    [Tooltip("A list of BlendingRule ScriptableObjects that define how textures should be blended. " +
             "The order in this list can influence how contributions are combined before normalization, " +
             "though with the current 'sum and normalize' approach, order is less critical.")]
    public List<BlendingRule> blendingRules = new List<BlendingRule>();

    [Tooltip("When true, the system will apply blending automatically on Start(). " +
             "Useful for runtime terrain generation, otherwise use the 'Blend Terrain Now' button.")]
    public bool blendOnStart = false;

    private void Awake()
    {
        // Automatically get the Terrain component if not assigned
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }
        if (targetTerrain == null)
        {
            Debug.LogError("TerrainBlendingSystem requires a Terrain component, or one assigned in the inspector.", this);
            enabled = false; // Disable script if no terrain is found
        }
    }

    private void Start()
    {
        if (blendOnStart)
        {
            BlendTerrain();
        }
    }

    /// <summary>
    /// Initiates the terrain blending process.
    /// This method fetches all blending rules, computes their contributions,
    /// combines them, normalizes the results, and applies the new splatmap to the terrain.
    /// This method can be called manually from the Inspector via a context menu.
    /// </summary>
    [ContextMenu("Blend Terrain Now")] // Adds an option to the component's context menu in the Inspector
    public void BlendTerrain()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("Target Terrain is not assigned. Please assign a Terrain component.", this);
            return;
        }
        if (blendingRules == null || blendingRules.Count == 0)
        {
            Debug.LogWarning("No blending rules assigned to TerrainBlendingSystem. Add some rules to blend the terrain.", this);
            return;
        }
        if (targetTerrain.terrainData.terrainLayers == null || targetTerrain.terrainData.terrainLayers.Length == 0)
        {
            Debug.LogError("Terrain has no TerrainLayers (textures) assigned. Please add some to the Terrain component via 'Paint Texture' -> 'Edit Terrain Layers' -> 'Add Layer'.", this);
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;
        int numLayers = terrainData.terrainLayers.Length;

        // Initialize a 3D array to store the combined contributions for all layers.
        // Unity's SetAlphamaps expects dimensions [alphaMapY, alphaMapX, layerIndex].
        float[,,] finalAlphaMap = new float[alphaMapHeight, alphaMapWidth, numLayers];

        Debug.Log($"Starting terrain blend for '{targetTerrain.name}' with {blendingRules.Count} rules...");

        // Step 1: Execute each blending rule to get its contribution map.
        // This is where the 'Strategy' pattern comes into play, as each rule (concrete strategy)
        // independently calculates its specific influence for its target texture.
        foreach (BlendingRule rule in blendingRules)
        {
            if (rule == null)
            {
                Debug.LogWarning("A null blending rule was found in the list. Skipping.", this);
                continue;
            }
            if (rule.targetTextureIndex < 0 || rule.targetTextureIndex >= numLayers)
            {
                Debug.LogWarning($"Blending Rule '{rule.name}' targets an invalid texture index {rule.targetTextureIndex}. " +
                                 $"Max valid index is {numLayers - 1}. Skipping this rule.", this);
                continue;
            }

            // Generate the contribution map for the current rule.
            // The contribution map is typically [x, y].
            float[,] contribution = rule.GenerateContribution(alphaMapWidth, alphaMapHeight, terrainData, targetTerrain);

            // Add this rule's contribution to the finalAlphaMap at its target layer.
            // Note the transpose: contribution[x, y] is copied to finalAlphaMap[y, x, layerIndex]
            // to match Unity's expected format for SetAlphamaps.
            for (int y = 0; y < alphaMapHeight; y++)
            {
                for (int x = 0; x < alphaMapWidth; x++)
                {
                    finalAlphaMap[y, x, rule.targetTextureIndex] += contribution[x, y];
                }
            }
        }

        // Step 2: Normalize the contributions for each point.
        // For each pixel (x,y), the sum of all layer influences must equal 1.
        // Iterate through finalAlphaMap using Unity's [y, x] indexing.
        for (int y = 0; y < alphaMapHeight; y++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                // Calculate the sum of all layer contributions for this point.
                float sum = 0f;
                for (int i = 0; i < numLayers; i++)
                {
                    sum += finalAlphaMap[y, x, i];
                }

                // If sum is zero (no rules contributed to this point), default to the first layer (index 0).
                // Otherwise, normalize each layer's contribution to ensure the sum is 1.0.
                if (sum == 0f)
                {
                    // Fallback: If nothing was painted, paint with the first layer.
                    // This prevents untextured areas.
                    finalAlphaMap[y, x, 0] = 1.0f;
                }
                else
                {
                    for (int i = 0; i < numLayers; i++)
                    {
                        finalAlphaMap[y, x, i] /= sum;
                    }
                }
            }
        }

        // Step 3: Apply the new splatmap to the terrain.
        // The SetAlphamaps method expects the map to be indexed as [height, width, texture_layer].
        // Our finalAlphaMap is already in this format.
        terrainData.SetAlphamaps(0, 0, finalAlphaMap);

        // Refresh terrain shaders and rendering to display the changes immediately.
        targetTerrain.Flush();
        Debug.Log("Terrain blending completed!");
    }
}

/*
* ====================================================================================
*                          TERRAIN BLENDING SYSTEM - EXAMPLE USAGE
* ====================================================================================
*
* This section demonstrates how to set up and use the TerrainBlendingSystem in Unity.
*
* REQUIREMENTS:
* 1.  A Unity Project with a Scene.
* 2.  A 'Terrain' GameObject in your scene.
* 3.  The Terrain must have 'TerrainLayers' (textures) assigned to it.
*     (Select your Terrain -> In the Inspector, navigate to the 'Paint Texture' brush icon
*      -> Click 'Edit Terrain Layers' -> 'Add Layer...' and select or create some TerrainLayers.)
*     Note down their indices: The first layer added is index 0, the second is 1, and so on.
*
* SETUP STEPS:
*
* 1.  CREATE THE TERRAIN BLENDING SYSTEM:
*     a.  Create an empty GameObject in your scene (e.g., named "TerrainBlenderManager").
*     b.  Add the 'TerrainBlendingSystem' script to this GameObject.
*     c.  Drag your Terrain GameObject from the Hierarchy into the 'Target Terrain'
*         field of the 'TerrainBlendingSystem' component in the Inspector.
*
* 2.  CREATE BLENDING RULES (ScriptableObjects):
*     These are your individual strategies for painting the terrain.
*     a.  In your Project window, right-click anywhere (e.g., in an 'Assets/Rules' folder).
*     b.  Go to 'Create -> Terrain Blending'.
*     c.  You'll see options for "Height Rule", "Slope Rule", "Perlin Noise Rule".
*     d.  Create a few instances of these (e.g., "GrassOnLowHeight", "RockOnSteepSlope", "DirtNoise").
*         -   **Example 1: "GrassOnLowHeight" (Create a 'Height Rule')**
*             -   `Target Texture Index`: 0 (assuming your grass layer is index 0 on the Terrain)
*             -   `Min Height`: 0.0
*             -   `Max Height`: 0.3 (grass will be painted up to 30% of terrain height)
*             -   `Blend Falloff`: 2.0 (for a smoother, but not too wide, transition)
*             -   `Global Strength`: 1.0 (full strength for this rule)
*
*         -   **Example 2: "RockOnSteepSlope" (Create a 'Slope Rule')**
*             -   `Target Texture Index`: 1 (assuming your rock layer is index 1 on the Terrain)
*             -   `Min Slope`: 30.0 (rock starts appearing at 30 degrees)
*             -   `Max Slope`: 60.0 (fully rock at 60 degrees)
*             -   `Blend Falloff`: 3.0 (makes the rock appear more sharply on steep slopes)
*             -   `Global Strength`: 1.0
*
*         -   **Example 3: "DirtNoise" (Create a 'Perlin Noise Rule')**
*             -   `Target Texture Index`: 2 (assuming your dirt layer is index 2 on the Terrain)
*             -   `Noise Scale`: 0.1 (larger noise features)
*             -   `Noise Offset`: (random values like 12.3, 45.6 to get a unique pattern)
*             -   `Noise Threshold`: 0.5 (mid-range noise values will trigger the blend)
*             -   `Blend Falloff`: 1.0 (linear blend around the threshold)
*             -   `Global Strength`: 0.5 (to mix it in more subtly with other layers)
*
* 3.  ASSIGN RULES TO THE SYSTEM:
*     a.  Select your "TerrainBlenderManager" GameObject in the Hierarchy.
*     b.  In the 'TerrainBlendingSystem' component, expand the 'Blending Rules' list.
*     c.  Drag the BlendingRule ScriptableObjects you created (e.g., "GrassOnLowHeight",
*         "RockOnSteepSlope", "DirtNoise") from your Project window into this list.
*
* 4.  RUN THE BLENDING:
*     a.  With the "TerrainBlenderManager" GameObject selected, look at its
*         'TerrainBlendingSystem' component in the Inspector.
*     b.  Click the "Blend Terrain Now" button (this button is added via `[ContextMenu]`
*         and is visible only in the Editor).
*
*     Alternatively, if you set 'Blend On Start' to true, the blending will happen
*     automatically when you press Play in the Editor or run the built game.
*
*
* EXPECTED RESULT:
* Your terrain's textures will be automatically painted based on the rules you defined.
* For instance: Grass on low, flat areas; rock on steep slopes; and dirt appearing as
* noisy patches, all blended together seamlessly.
*
* This setup vividly demonstrates the TerrainBlendingSystem pattern, leveraging the
* Strategy pattern (each BlendingRule is a strategy) and ScriptableObjects
* for a highly flexible, extensible, and data-driven approach to terrain texture generation.
* New blending behaviors can be added by simply creating new BlendingRule
* ScriptableObject classes without ever modifying the core TerrainBlendingSystem code.
*/
```