// Unity Design Pattern Example: MeshBakingSystem
// This script demonstrates the MeshBakingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MeshBakingSystem' design pattern in Unity provides a structured way to combine, optimize, and process multiple source meshes into a single, new mesh asset. This is a common practice for performance optimization, as it reduces draw calls, simplifies the scene hierarchy, and can be used for things like creating level geometry, static environment colliders, or optimized character models.

This example focuses on combining multiple `MeshFilter` components (potentially from different GameObjects) into a single new `Mesh` object. It correctly handles multiple materials and submeshes, preserving the original material assignments across the combined mesh. The resulting mesh can optionally be saved as a `.asset` file in the project for persistent use.

---

### MeshBakingSystem Design Pattern Explained

1.  **Purpose:** To consolidate multiple individual mesh assets and their renderers into a single, optimized mesh and renderer. This "baking" process typically happens offline (in the Unity editor) to prepare assets for runtime.

2.  **Core Components:**
    *   **`MeshBakeOptions` (Configuration):** A data class/struct that defines all parameters for the baking process. This includes what meshes to bake, how to transform them (world vs. local space), where to save the output, etc. This makes the baking system flexible and reusable for different scenarios.
    *   **`MeshBakeResult` (Output):** A simple data class/struct to return the outcome of the baking operation, including the resulting mesh, materials, and any error messages.
    *   **`MeshBakingSystem` (The System/Facade):** The central static class that orchestrates the entire baking process. It takes the `MeshBakeOptions` and performs all the complex steps:
        *   Gathering source mesh data.
        *   Handling transformations to correctly position combined meshes.
        *   Managing multiple materials and submeshes to minimize draw calls while preserving visual fidelity.
        *   Creating the new `Mesh` asset.
        *   Assigning the new mesh and materials to a target `GameObject`.
        *   Optionally saving the new `Mesh` as a `.asset` file in the project.

3.  **Key Advantages:**
    *   **Reduced Draw Calls:** Multiple meshes are combined into one, meaning the GPU only needs to draw one object instead of many (for each material group).
    *   **Simplified Hierarchy:** Reduces the number of GameObjects in the scene, improving scene loading times and editor performance.
    *   **Optimized Performance:** Less CPU work for culling, transform calculations, etc.
    *   **Reusability:** The system is designed to be highly configurable and can be used for various baking needs.
    *   **Clean Separation of Concerns:** The baking logic is encapsulated in a dedicated system, separate from game logic or individual mesh components.

4.  **How it works (Mesh Combining Logic):**
    The most complex part is combining meshes while preserving their distinct materials. Unity's `Mesh.CombineMeshes()` function is powerful but requires careful usage for this scenario.
    *   **Material Grouping:** The system first identifies all unique materials across all source meshes.
    *   **Temporary Meshes per Material:** For each unique material, it collects all submeshes from all source objects that use that material. These submeshes are then combined into a *single temporary mesh*. This temporary mesh will have only one submesh, representing all geometry with that specific material.
    *   **Final Combine:** These temporary, material-specific meshes are then combined into the *final baked mesh*. By doing so, the final mesh will have multiple submeshes, where each submesh corresponds to one of the original unique materials. This allows the `MeshRenderer` to use an array of materials, each mapped to a specific submesh, maintaining the original appearance with reduced draw calls.

---

### Complete Unity C# Example

This example consists of two main parts:
1.  **`MeshBakingSystem.cs`**: The core static class implementing the pattern.
2.  **`MeshBakerEditorTool.cs`**: A `MonoBehaviour` (with an Editor script) that provides an easy way to trigger the baking process from the Unity Editor's Inspector.

#### 1. `MeshBakingSystem.cs`

This script contains the `MeshBakeOptions`, `MeshBakeResult`, and the `MeshBakingSystem` static class.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .OrderBy()

#if UNITY_EDITOR
using UnityEditor; // Required for AssetDatabase to save meshes
#endif

namespace MyCustomTools
{
    /// <summary>
    /// Configuration options for the MeshBakingSystem.
    /// Defines what to bake and how to process it.
    /// </summary>
    [System.Serializable]
    public class MeshBakeOptions
    {
        [Tooltip("The GameObject that will host the new MeshFilter and MeshRenderer components after baking. " +
                 "If 'Combine Children Of Target' is checked and this is null, the GameObject with MeshBakerEditorTool will be used.")]
        public GameObject targetGameObject;

        [Tooltip("If true, all active MeshFilter components found under the 'Target GameObject's children " +
                 "(excluding the Target GameObject itself) will be combined.")]
        public bool combineChildrenOfTarget;

        [Tooltip("List of specific MeshFilter components to bake. This list is ignored if 'Combine Children Of Target' is true.")]
        public List<MeshFilter> sourceMeshFilters = new List<MeshFilter>();

        [Tooltip("If true, meshes are combined preserving their original world space positions. " +
                 "If false, meshes are combined relative to the 'Target GameObject's local space.")]
        public bool useWorldSpaceTransforms = false;

        [Tooltip("If true, the baked mesh will be saved as an asset in the Unity project. " +
                 "This allows the baked mesh to persist between editor sessions and builds.")]
        public bool saveBakedMeshAsAsset = true;

        [Tooltip("The asset path where the baked mesh will be saved (e.g., 'Assets/BakedMeshes/MyCombinedMesh.asset'). " +
                 "Folders will be created if they don't exist.")]
        public string assetPath = "Assets/BakedMeshes/BakedMesh.asset";

        /// <summary>
        /// Validates the options to ensure they are set up correctly before baking.
        /// </summary>
        public bool Validate()
        {
            if (targetGameObject == null)
            {
                Debug.LogError("MeshBakeOptions: Target GameObject cannot be null.");
                return false;
            }
            if (!combineChildrenOfTarget && (sourceMeshFilters == null || sourceMeshFilters.Count == 0))
            {
                Debug.LogError("MeshBakeOptions: No source MeshFilters specified and 'Combine Children Of Target' is false.");
                return false;
            }
            if (saveBakedMeshAsAsset && string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("MeshBakeOptions: Asset path cannot be empty if saving as asset.");
                return false;
            }
            if (saveBakedMeshAsAsset && !assetPath.EndsWith(".asset"))
            {
                assetPath += ".asset";
                Debug.LogWarning("MeshBakeOptions: Asset path was automatically appended with '.asset' extension.");
            }
            return true;
        }
    }

    /// <summary>
    /// Represents the result of a mesh baking operation.
    /// </summary>
    public class MeshBakeResult
    {
        public bool success;
        public string errorMessage;
        public Mesh bakedMesh;
        public Material[] bakedMaterials;

        public MeshBakeResult(bool success, string errorMessage = null, Mesh bakedMesh = null, Material[] bakedMaterials = null)
        {
            this.success = success;
            this.errorMessage = errorMessage;
            this.bakedMesh = bakedMesh;
            this.bakedMaterials = bakedMaterials;
        }
    }

    /// <summary>
    /// The MeshBakingSystem is a design pattern that encapsulates the complex process
    /// of combining multiple source meshes (from MeshFilters) into a single, new Mesh asset.
    /// This system aims to improve runtime performance by reducing draw calls and simplifying
    /// the scene hierarchy, especially useful for static geometry.
    /// </summary>
    /// <remarks>
    /// **Design Pattern Aspects:**
    /// -   **Facade:** Provides a simplified, high-level interface (`BakeMeshes`) to a complex
    ///     subsystem (mesh data manipulation, material grouping, Unity asset management).
    /// -   **Strategy (via Options):** The `MeshBakeOptions` class allows different baking strategies
    ///     (e.g., combining children vs. explicit list, world space vs. local space) to be
    ///     defined and passed to the core `BakeMeshes` method without altering its internal structure.
    /// -   **Builder/Aggregator:** Internally, it acts as a builder, constructing a new complex
    ///     `Mesh` object from various individual mesh parts and materials.
    /// </remarks>
    public static class MeshBakingSystem
    {
        /// <summary>
        /// Orchestrates the mesh baking process based on the provided options.
        /// This is the primary entry point for the MeshBakingSystem.
        /// </summary>
        /// <param name="options">Configuration for how the meshes should be baked.</param>
        /// <returns>A <see cref="MeshBakeResult"/> indicating success/failure and the baked mesh data.</returns>
        public static MeshBakeResult BakeMeshes(MeshBakeOptions options)
        {
            if (!options.Validate())
            {
                return new MeshBakeResult(false, "MeshBakeOptions validation failed. Check console for details.");
            }

            Debug.Log($"MeshBakingSystem: Starting bake for target '{options.targetGameObject.name}'...");

            // --- 1. Gather all relevant source MeshFilter and MeshRenderer components ---
            List<MeshFilter> activeSourceMeshFilters = new List<MeshFilter>();
            List<MeshRenderer> activeSourceMeshRenderers = new List<MeshRenderer>();

            if (options.combineChildrenOfTarget)
            {
                // Find all MeshFilters under the targetGameObject's children
                foreach (MeshFilter mf in options.targetGameObject.GetComponentsInChildren<MeshFilter>(true))
                {
                    // Exclude the targetGameObject itself if it has a MeshFilter
                    if (mf.gameObject == options.targetGameObject) continue;

                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    // Only consider valid meshes with active renderers
                    if (mf.sharedMesh != null && mr != null && mr.enabled)
                    {
                        activeSourceMeshFilters.Add(mf);
                        activeSourceMeshRenderers.Add(mr);
                    }
                }
            }
            else
            {
                // Use the explicitly provided sourceMeshFilters list
                foreach (MeshFilter mf in options.sourceMeshFilters)
                {
                    if (mf == null) continue; // Skip null entries in the list
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh != null && mr != null && mr.enabled)
                    {
                        activeSourceMeshFilters.Add(mf);
                        activeSourceMeshRenderers.Add(mr);
                    }
                }
            }

            if (activeSourceMeshFilters.Count == 0)
            {
                return new MeshBakeResult(false, "No active source MeshFilters found to bake based on the provided options.");
            }

            Debug.Log($"MeshBakingSystem: Found {activeSourceMeshFilters.Count} valid source meshes to combine.");

            // --- 2. Group CombineInstances by material to handle submeshes properly ---
            // This is crucial for creating a single combined mesh that still uses multiple materials
            // (one for each logical submesh group) to minimize draw calls.
            Dictionary<Material, List<CombineInstance>> combineInstancesByMaterial =
                new Dictionary<Material, List<CombineInstance>>();
            List<Material> uniqueBakedMaterials = new List<Material>(); // To maintain order for the final materials array

            // Pre-calculate target transforms for efficiency
            Matrix4x4 targetLocalToWorld = options.targetGameObject.transform.localToWorldMatrix;
            Matrix4x4 targetWorldToLocal = options.targetGameObject.transform.worldToLocalMatrix;

            for (int i = 0; i < activeSourceMeshFilters.Count; i++)
            {
                MeshFilter mf = activeSourceMeshFilters[i];
                MeshRenderer mr = activeSourceMeshRenderers[i];
                Mesh sourceMesh = mf.sharedMesh;
                Material[] sourceMaterials = mr.sharedMaterials;

                // Iterate through each submesh of the current source mesh
                for (int sub = 0; sub < sourceMesh.subMeshCount; sub++)
                {
                    if (sub >= sourceMaterials.Length || sourceMaterials[sub] == null)
                    {
                        Debug.LogWarning($"MeshBakingSystem: Mesh '{mf.name}' (GameObject: '{mf.gameObject.name}') " +
                                         $"has submesh {sub} but no material assigned in its MeshRenderer. Skipping this submesh.");
                        continue;
                    }

                    Material currentMaterial = sourceMaterials[sub];

                    // Determine the transformation matrix for this specific submesh
                    Matrix4x4 combineTransform;
                    if (options.useWorldSpaceTransforms)
                    {
                        // Transform source mesh from its local space directly to world space
                        combineTransform = mf.transform.localToWorldMatrix;
                    }
                    else
                    {
                        // Transform source mesh from its local space, then to world space,
                        // and finally into the targetGameObject's local space.
                        combineTransform = targetWorldToLocal * mf.transform.localToWorldMatrix;
                    }

                    // Create a CombineInstance for the current submesh
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = sourceMesh,
                        subMeshIndex = sub, // Crucial: combine *this specific submesh* from the source mesh
                        transform = combineTransform
                    };

                    // Add this CombineInstance to the list corresponding to its material
                    if (!combineInstancesByMaterial.ContainsKey(currentMaterial))
                    {
                        combineInstancesByMaterial.Add(currentMaterial, new List<CombineInstance>());
                        uniqueBakedMaterials.Add(currentMaterial); // Add to ordered list of unique materials
                    }
                    combineInstancesByMaterial[currentMaterial].Add(ci);
                }
            }

            // If no materials or instances were collected, something went wrong or no valid meshes were processed.
            if (uniqueBakedMaterials.Count == 0)
            {
                return new MeshBakeResult(false, "No materials or valid submeshes found after grouping from source meshes.");
            }

            // --- 3. Create a temporary mesh for each material group, then combine them ---
            // This two-step combining process is necessary to correctly group geometry by material
            // and still have a single Mesh with multiple submeshes in the end.
            List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
            List<Mesh> temporaryMeshesToCleanUp = new List<Mesh>(); // Track temp meshes for destruction

            // Sort materials to ensure a consistent submesh order in the final baked mesh
            // (e.g., if you bake twice, submesh 0 always corresponds to the same material).
            uniqueBakedMaterials = uniqueBakedMaterials.OrderBy(m => m.name).ToList();

            foreach (Material mat in uniqueBakedMaterials)
            {
                if (combineInstancesByMaterial.TryGetValue(mat, out List<CombineInstance> instancesForThisMaterial))
                {
                    Mesh tempMesh = new Mesh();
                    tempMesh.indexFormat = MeshIndexFormat.UInt32; // Use UInt32 for potentially large combined meshes
                    tempMesh.name = $"TempBakedMesh_{mat.name}";

                    // Combine all submeshes that use this specific material into one temporary mesh.
                    // 'true' for mergeSubMeshes: All instances in 'instancesForThisMaterial' (which share the same material)
                    // will be merged into a SINGLE submesh within this temporary mesh.
                    // 'options.useWorldSpaceTransforms' is passed to apply the transforms correctly within this temp mesh.
                    tempMesh.CombineMeshes(instancesForThisMaterial.ToArray(), true, options.useWorldSpaceTransforms, true);

                    // Add this temporary mesh as a single CombineInstance to the final list.
                    // Its transform is identity because its vertices are already in the correct space
                    // (either world space or targetGameObject's local space) due to the previous combine.
                    finalCombineInstances.Add(new CombineInstance
                    {
                        mesh = tempMesh,
                        subMeshIndex = 0, // This temporary mesh now only has one submesh (index 0)
                        transform = Matrix4x4.identity // Transforms were applied in the previous step
                    });
                    temporaryMeshesToCleanUp.Add(tempMesh); // Add to cleanup list
                }
            }

            // --- 4. Create the final baked Mesh object ---
            Mesh bakedMesh = new Mesh();
            bakedMesh.indexFormat = MeshIndexFormat.UInt32; // Ensure support for large meshes
            bakedMesh.name = options.targetGameObject.name + "_BakedMesh";

            // Combine all the temporary meshes (each representing a unique material group)
            // into the final baked mesh.
            // 'false' for mergeSubMeshes: This means each 'tempMesh' (which already has its
            // transform baked in and represents a single material group) will become its
            // OWN SEPARATE SUBMESH in the 'bakedMesh'. The 'bakedMesh' will then have
            // uniqueBakedMaterials.Count submeshes.
            // 'false' for useMatrices: The transforms were already applied when creating the tempMeshes.
            bakedMesh.CombineMeshes(finalCombineInstances.ToArray(), false, false, true);

            // Ensure normals and tangents are correct for lighting
            bakedMesh.RecalculateNormals();
            bakedMesh.RecalculateTangents();
            bakedMesh.RecalculateBounds(); // Update bounds for culling

            // --- 5. Clean up temporary meshes to prevent memory leaks in the editor ---
            foreach (Mesh temp in temporaryMeshesToCleanUp)
            {
                // In Editor, DestroyImmediate is preferred for immediate cleanup.
                // In runtime builds, Object.Destroy is sufficient.
                if (Application.isEditor)
                {
                    Object.DestroyImmediate(temp);
                }
                else
                {
                    Object.Destroy(temp);
                }
            }

            // --- 6. Assign the baked mesh and materials to the target GameObject ---
            MeshFilter targetMeshFilter = options.targetGameObject.GetComponent<MeshFilter>();
            if (targetMeshFilter == null)
            {
                targetMeshFilter = options.targetGameObject.AddComponent<MeshFilter>();
            }
            targetMeshFilter.sharedMesh = bakedMesh; // Assign the newly baked mesh

            MeshRenderer targetMeshRenderer = options.targetGameObject.GetComponent<MeshRenderer>();
            if (targetMeshRenderer == null)
            {
                targetMeshRenderer = options.targetGameObject.AddComponent<MeshRenderer>();
            }
            targetMeshRenderer.sharedMaterials = uniqueBakedMaterials.ToArray(); // Assign the collected unique materials

            // --- 7. Optionally save the baked mesh as an asset in the project ---
            if (options.saveBakedMeshAsAsset)
            {
#if UNITY_EDITOR
                // Ensure the target directory exists before saving
                string directory = System.IO.Path.GetDirectoryName(options.assetPath);
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    // Recursively create folders if necessary
                    string[] folders = directory.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
                    string currentPath = "Assets";
                    for (int j = 0; j < folders.Length; j++)
                    {
                        string nextPath = System.IO.Path.Combine(currentPath, folders[j]);
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[j]);
                        }
                        currentPath = nextPath;
                    }
                }

                AssetDatabase.CreateAsset(bakedMesh, options.assetPath);
                AssetDatabase.SaveAssets(); // Save all pending changes to disk
                Debug.Log($"MeshBakingSystem: Baked mesh saved to asset: {options.assetPath}");
#else
                Debug.LogWarning("MeshBakingSystem: Saving mesh as an asset is only supported in the Unity Editor.");
#endif
            }

            Debug.Log($"MeshBakingSystem: Baking completed successfully for '{options.targetGameObject.name}'. " +
                      $"Baked Mesh has {bakedMesh.vertexCount} vertices and {bakedMesh.subMeshCount} submeshes.");

            return new MeshBakeResult(true, null, bakedMesh, uniqueBakedMaterials.ToArray());
        }
    }
}
```

#### 2. `MeshBakerEditorTool.cs` (and its Custom Editor)

This script provides an editor-friendly way to use the `MeshBakingSystem`. It's a MonoBehaviour you can attach to an empty GameObject, set up its options in the Inspector, and click a button to perform the bake.

```csharp
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor; // Required for CustomEditor and AssetDatabase functions
#endif

namespace MyCustomTools
{
    /// <summary>
    /// This MonoBehaviour acts as an editor-time utility to trigger the MeshBakingSystem.
    /// It provides a simple inspector interface to configure and execute the baking process
    /// on a target GameObject, either by combining its children or explicit MeshFilters.
    /// </summary>
    public class MeshBakerEditorTool : MonoBehaviour
    {
        [Tooltip("Configuration options for the mesh baking process.")]
        public MeshBakeOptions bakeOptions = new MeshBakeOptions();

        // No need for a public bool 'bakeMeshesNow' when using a CustomEditor with a button.
        // We will add the button directly in the custom editor's OnInspectorGUI.

        /// <summary>
        /// Initiates the mesh baking process using the MeshBakingSystem.
        /// This method is typically called from the Unity Editor.
        /// </summary>
        public void PerformBake()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Mesh baking is primarily intended for editor-time use. " +
                                 "It can run in play mode, but asset saving is editor-only. " +
                                 "Consider using it before entering play mode for performance benefits.");
            }

            Debug.Log("MeshBakerEditorTool: Initiating mesh baking...");

            // Ensure the targetGameObject is set to this GameObject if not specified
            // and the option is to combine its children.
            if (bakeOptions.targetGameObject == null && bakeOptions.combineChildrenOfTarget)
            {
                bakeOptions.targetGameObject = this.gameObject;
                Debug.Log($"MeshBakerEditorTool: Setting Target GameObject to '{this.gameObject.name}' " +
                          $"because 'Combine Children Of Target' is enabled and it was not set.");
            }

            // Perform the bake operation using the MeshBakingSystem
            MeshBakeResult result = MeshBakingSystem.BakeMeshes(bakeOptions);

            if (result.success)
            {
                Debug.Log($"MeshBakerEditorTool: Mesh baking successful! New mesh created on '{bakeOptions.targetGameObject.name}'.");

                // Optionally, disable source renderers after baking to clearly see the baked result.
                // This helps verify that the new baked mesh is rendering correctly.
                if (bakeOptions.combineChildrenOfTarget)
                {
                    // Disable renderers of all children that were combined
                    foreach (MeshFilter mf in bakeOptions.targetGameObject.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.gameObject == bakeOptions.targetGameObject) continue; // Don't disable self
                        MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = false;
                    }
                }
                else
                {
                    // Disable renderers of explicitly listed source MeshFilters
                    foreach (MeshFilter mf in bakeOptions.sourceMeshFilters)
                    {
                        if (mf != null)
                        {
                            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                            if (mr != null) mr.enabled = false;
                        }
                    }
                }

                // If in editor and an asset was saved, select and ping the new asset for convenience.
#if UNITY_EDITOR
                if (result.bakedMesh != null && bakeOptions.saveBakedMeshAsAsset)
                {
                    // Load the asset to ensure it's in the AssetDatabase and can be selected
                    Object bakedAsset = AssetDatabase.LoadAssetAtPath<Mesh>(bakeOptions.assetPath);
                    if (bakedAsset != null)
                    {
                        Selection.activeObject = bakedAsset; // Select the asset in the project window
                        EditorGUIUtility.PingObject(bakedAsset); // Highlight it
                    }
                }
#endif
            }
            else
            {
                Debug.LogError($"MeshBakerEditorTool: Mesh baking failed! Reason: {result.errorMessage}");
            }
        }

        /// <summary>
        /// This method demonstrates how to programmatically use the MeshBakingSystem
        /// from another script (e.g., a custom tool, a different MonoBehaviour's method).
        /// </summary>
        /// <param name="rootObject">The GameObject whose children meshes should be baked.</param>
        public static void ExampleProgrammaticBake(GameObject rootObject)
        {
            if (rootObject == null)
            {
                Debug.LogError("ExampleProgrammaticBake: Root object for baking is null.");
                return;
            }

            // Configure the baking options programmatically
            MeshBakeOptions options = new MeshBakeOptions
            {
                targetGameObject = rootObject,
                combineChildrenOfTarget = true, // Bake all meshes under the rootObject's children
                useWorldSpaceTransforms = false, // Combine relative to rootObject's local space
                saveBakedMeshAsAsset = true,
                assetPath = $"Assets/BakedMeshes/{rootObject.name}_ProgrammaticBakedMesh.asset"
            };

            Debug.Log($"ExampleProgrammaticBake: Initiating programmatic bake for '{rootObject.name}'...");

            // Execute the baking process
            MeshBakeResult result = MeshBakingSystem.BakeMeshes(options);

            if (result.success)
            {
                Debug.Log($"Programmatic Bake successful for '{rootObject.name}'. Baked mesh: {result.bakedMesh.name}");
                // You could add more programmatic actions here, e.g.,
                // foreach (Transform child in rootObject.transform) { child.gameObject.SetActive(false); }
            }
            else
            {
                Debug.LogError($"Programmatic Bake failed for '{rootObject.name}': {result.errorMessage}");
            }
        }
    }

    // --- Custom Editor for MeshBakerEditorTool ---
    // This allows us to display a button in the Inspector to trigger the baking process.
#if UNITY_EDITOR
    [CustomEditor(typeof(MeshBakerEditorTool))]
    public class MeshBakerEditorToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw all default serialized properties (your bakeOptions struct)
            DrawDefaultInspector();

            MeshBakerEditorTool myScript = (MeshBakerEditorTool)target;

            GUILayout.Space(15);

            // Create a button in the Inspector that calls PerformBake() when clicked
            if (GUILayout.Button("Bake Meshes Now", GUILayout.Height(30)))
            {
                myScript.PerformBake();
            }

            GUILayout.Space(15);
            EditorGUILayout.HelpBox(
                "**How to Use:**\n\n" +
                "1.  **Attach** this script to an empty GameObject (e.g., 'SceneBaker').\n" +
                "2.  **Target GameObject:** Drag the GameObject that will receive the baked mesh here. " +
                "    If 'Combine Children Of Target' is checked and this is empty, this GameObject ('SceneBaker') will be used.\n" +
                "3.  **Combine Children Of Target:** Check this to combine all MeshFilters under the 'Target GameObject's children. " +
                "    Uncheck to manually specify 'Source Mesh Filters'.\n" +
                "4.  **Source Mesh Filters:** (If 'Combine Children Of Target' is unchecked) Drag individual MeshFilter GameObjects here.\n" +
                "5.  **Use World Space Transforms:** " +
                "    - True: Meshes combine at their current world positions.\n" +
                "    - False: Meshes combine relative to the 'Target GameObject's local space.\n" +
                "6.  **Save Baked Mesh As Asset:** Recommended to save the generated mesh as a project asset.\n" +
                "7.  **Asset Path:** Specify where to save the .asset file (e.g., 'Assets/BakedMeshes/MyLevel.asset').\n" +
                "8.  Click **'Bake Meshes Now'**.\n\n" +
                "After baking, the original MeshRenderers of the source objects will be disabled, " +
                "and the 'Target GameObject' will display the combined mesh.", MessageType.Info);
        }
    }
#endif
}
```

---

### How to Use in a Unity Project:

1.  **Create Scripts:**
    *   Create a folder `Assets/MyCustomTools` (or any name you prefer).
    *   Inside that folder, create a C# script named `MeshBakingSystem.cs` and copy the code for the `MeshBakingSystem` and its related classes (`MeshBakeOptions`, `MeshBakeResult`) into it.
    *   Create another C# script named `MeshBakerEditorTool.cs` in the same folder and copy the code for the `MeshBakerEditorTool` and its `MeshBakerEditorToolEditor` into it.

2.  **Prepare Your Scene:**
    *   Create a few 3D objects in your scene (e.g., using `GameObject -> 3D Object -> Cube`, `Sphere`, `Cylinder`). Position them uniquely.
    *   Optionally, apply different materials to these objects (or even to different submeshes of a single object) to see how the system handles material grouping.
    *   Group these objects under an empty GameObject if you plan to use the "Combine Children Of Target" option (e.g., create an empty GameObject named "SceneGeometry" and make your cubes, spheres, etc., children of "SceneGeometry").

3.  **Set Up the Baker:**
    *   Create a new empty GameObject in your scene (e.g., `GameObject -> Create Empty`) and name it "MySceneBaker".
    *   Select "MySceneBaker" in the Hierarchy.
    *   In the Inspector, click "Add Component" and search for "Mesh Baker Editor Tool". Add the component.

4.  **Configure Baking Options:**
    *   On the "MySceneBaker" GameObject's Inspector, you'll see the "Mesh Baker Editor Tool" component.
    *   **Target GameObject:** Drag "MySceneBaker" (the current GameObject) into this field. This is where the final baked mesh and its renderer will be attached.
    *   **Combine Children Of Target:**
        *   If you grouped your objects under "SceneGeometry", **check this box**. Then drag "SceneGeometry" into the **Target GameObject** field. The system will find all meshes under "SceneGeometry" (excluding "SceneGeometry" itself if it has a mesh).
        *   If you want to pick specific individual meshes, **uncheck this box**. Then expand the "Source Mesh Filters" list and drag the `MeshFilter` components from your individual 3D objects into this list.
    *   **Use World Space Transforms:**
        *   **Check this** if you want the combined mesh to appear exactly where the original objects were in world space.
        *   **Uncheck this** if you want the combined mesh's origin to be at the `Target GameObject`'s pivot, and the combined geometry to be transformed relative to that pivot.
    *   **Save Baked Mesh As Asset:** Keep this checked (recommended).
    *   **Asset Path:** Set a path like `Assets/BakedMeshes/MySceneCombined.asset`. The `BakedMeshes` folder will be created if it doesn't exist.

5.  **Perform the Bake:**
    *   Click the **"Bake Meshes Now"** button in the Inspector of "MySceneBaker".
    *   Observe the Console for logs, success messages, or errors.

6.  **Verify Results:**
    *   A new `.asset` file (e.g., `MySceneCombined.asset`) should appear in your Project window at the specified `Asset Path`.
    *   The `Target GameObject` ("MySceneBaker" or "SceneGeometry" in our example) will now have a `MeshFilter` and `MeshRenderer` components, displaying the combined mesh.
    *   The `MeshRenderer` on the `Target GameObject` will have an array of materials, corresponding to the unique materials used by the original source objects.
    *   The original source objects will have their `MeshRenderer` components disabled, showing only the baked result.

This setup provides a practical, robust, and educational example of implementing a MeshBakingSystem in Unity using C#.