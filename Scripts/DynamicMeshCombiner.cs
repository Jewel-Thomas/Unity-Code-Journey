// Unity Design Pattern Example: DynamicMeshCombiner
// This script demonstrates the DynamicMeshCombiner pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DynamicMeshCombiner' design pattern in Unity aims to reduce the number of draw calls by merging multiple smaller static meshes into a single, larger mesh. This is particularly beneficial for environment assets, modular buildings, or props that don't need individual movement or complex interactions. The "dynamic" aspect means the combination can be re-triggered at runtime (e.g., if objects are added or removed) or in the editor.

**Benefits of DynamicMeshCombiner:**
*   **Reduced Draw Calls:** Grouping meshes under a single `MeshRenderer` drastically lowers the number of commands sent to the GPU, improving CPU performance.
*   **Improved Culling:** A single large mesh might be more efficiently culled (frustum culling) than many small ones.
*   **Simplified Scene Hierarchy:** Clutter from many small objects can be reduced.

**Key Challenges & How This Script Addresses Them:**
1.  **Multiple Materials:** If source meshes use different materials, the combined mesh needs multiple "submeshes," each assigned a unique material. This script handles this by creating temporary meshes for each material type and then combining those into a final mesh with the correct submesh structure.
2.  **Transforms:** Source meshes are often at different positions, rotations, and scales. Their vertices must be transformed into the local space of the combined mesh object.
3.  **Dynamic Updates:** The script provides methods (`AddObjectAndRecombine`, `RemoveObjectAndRecombine`) and a `[ContextMenu]` option to re-combine meshes when the source objects change.
4.  **Editor vs. Runtime:** It works both in the editor (allowing you to bake combined meshes into assets) and at runtime.
5.  **Performance:** Uses `Mesh.CombineMeshes` efficiently and recalculates properties like normals, tangents, and bounds. It also uses `IndexFormat.UInt32` to support meshes with more than 65,535 vertices.

---

### C# Unity Script: `DynamicMeshCombiner.cs`

To use this script:

1.  **Create a New C# Script** in your Unity project, name it `DynamicMeshCombiner`.
2.  **Copy and Paste** the code below into the script.
3.  **Create an Empty GameObject** in your scene (e.g., "ModularBuildingCombiner").
4.  **Attach the `DynamicMeshCombiner` script** to this new GameObject.
5.  **Place all the static mesh GameObjects** you want to combine as **children** of "ModularBuildingCombiner". Make sure these children have `MeshFilter` and `MeshRenderer` components with valid meshes and materials.
6.  **Configure in Inspector:**
    *   **Combine On Start:** Check this if you want the combination to happen automatically when the scene starts playing.
    *   **Destroy Originals After Combine:** If checked, the original child GameObjects will be destroyed after their meshes are combined. Otherwise, they will be disabled. Destroying is usually better for performance if you don't need the original objects anymore.
    *   **Consider Inactive Objects:** If checked, inactive child objects will also be included in the combination process.
    *   **Source Parent:** By default, it uses the GameObject this script is attached to. You can assign another GameObject here if your meshes are children of a different parent.
    *   **Combined Mesh GameObject:** If left null, the script will create a new child GameObject named "CombinedMesh_Runtime" to hold the combined mesh.
7.  **Run the Scene** or **Right-click on the `DynamicMeshCombiner` component in the Inspector** and choose:
    *   **"Combine Meshes Now"**: To trigger the combination in the editor.
    *   **"Clear Combined Mesh"**: To remove the generated combined mesh and its asset (if created in editor).

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Implements the 'DynamicMeshCombiner' design pattern in Unity.
/// This script combines multiple static child meshes into a single mesh at runtime or in the editor
/// to reduce draw calls and improve rendering performance. It handles multiple materials by creating
/// submeshes in the combined mesh.
/// </summary>
/// <remarks>
/// The 'Dynamic' aspect refers to the ability to re-combine meshes if the source children
/// are added, removed, or changed. This version provides editor context menu options and
/// basic runtime add/remove methods to demonstrate this.
///
/// Usage Steps:
/// 1. Create an empty GameObject in your scene (e.g., "MyMeshCombinerParent").
/// 2. Attach this `DynamicMeshCombiner` script to it.
/// 3. Place all static GameObjects with MeshFilters and MeshRenderers you want to combine
///    as children of "MyMeshCombinerParent". These can have different meshes and materials.
/// 4. Configure the script in the Inspector:
///    - `Combine On Start`: If true, combination happens automatically when the scene starts.
///    - `Destroy Originals After Combine`: If true, original child GameObjects are destroyed.
///      Otherwise, they are disabled. Destroying is generally better for performance if
///      original objects are no longer needed.
///    - `Consider Inactive Objects`: If true, inactive children will also be combined.
///    - `Source Parent`: (Optional) Assign a different GameObject whose children to combine.
///      Defaults to the GameObject this script is attached to.
///    - `Combined Mesh GameObject`: (Optional) Assign an existing GameObject to host the combined
///      mesh. If null, a new child GameObject ("CombinedMesh_Runtime") will be created.
/// 5. For editor-time combination: Right-click on the component in the Inspector and select
///    "Combine Meshes Now". The combined mesh will be saved as an asset if in editor.
/// 6. To clear: Right-click on the component and select "Clear Combined Mesh".
/// </remarks>
[DisallowMultipleComponent] // Ensures only one combiner instance per GameObject
public class DynamicMeshCombiner : MonoBehaviour
{
    [Tooltip("If true, the mesh combination will happen automatically when the scene starts.")]
    [SerializeField]
    private bool _combineOnStart = true;

    [Tooltip("If true, the original child GameObjects will be destroyed after combination. " +
             "Otherwise, they will be disabled. Destroying can free up memory faster.")]
    [SerializeField]
    private bool _destroyOriginalsAfterCombine = true;

    [Tooltip("If true, inactive child GameObjects will also be included in the combination process.")]
    [SerializeField]
    private bool _considerInactiveObjects = false;

    [Tooltip("The parent GameObject whose children will be combined. If null, this GameObject's own children will be used.")]
    [SerializeField]
    private GameObject _sourceParent;

    [Tooltip("The GameObject that will hold the combined mesh. If null, a new child GameObject will be created.")]
    [SerializeField]
    private GameObject _combinedMeshGameObject;

    // Internal references to the MeshFilter, MeshRenderer, and the Mesh itself for the combined object.
    private MeshFilter _combinedMeshFilter;
    private MeshRenderer _combinedMeshRenderer;
    private Mesh _combinedMesh;

    // Stores the unique materials found, in order, to define the submeshes of the combined mesh.
    private List<Material> _combinedMaterials = new List<Material>();

    private void Awake()
    {
        // Ensure _sourceParent is initialized; if not set in Inspector, use this GameObject.
        if (_sourceParent == null)
        {
            _sourceParent = gameObject;
        }

        // Prepare the GameObject that will display the combined mesh.
        InitializeCombinedGameObject();

        // Perform initial combination if configured.
        if (_combineOnStart)
        {
            CombineMeshes();
        }
    }

    /// <summary>
    /// Ensures a GameObject exists to host the combined mesh, and that it has
    /// a MeshFilter and MeshRenderer components. It also initializes or retrieves the Mesh object.
    /// </summary>
    private void InitializeCombinedGameObject()
    {
        // Create a new GameObject if _combinedMeshGameObject is not assigned.
        if (_combinedMeshGameObject == null)
        {
            _combinedMeshGameObject = new GameObject("CombinedMesh_Runtime");
            // Make it a child of the combiner GameObject and reset its transform.
            _combinedMeshGameObject.transform.SetParent(transform);
            _combinedMeshGameObject.transform.localPosition = Vector3.zero;
            _combinedMeshGameObject.transform.localRotation = Quaternion.identity;
            _combinedMeshGameObject.transform.localScale = Vector3.one;
        }

        // Get or add MeshFilter and MeshRenderer components to the combined mesh GameObject.
        _combinedMeshFilter = _combinedMeshGameObject.GetComponent<MeshFilter>();
        if (_combinedMeshFilter == null)
        {
            _combinedMeshFilter = _combinedMeshGameObject.AddComponent<MeshFilter>();
        }

        _combinedMeshRenderer = _combinedMeshGameObject.GetComponent<MeshRenderer>();
        if (_combinedMeshRenderer == null)
        {
            _combinedMeshRenderer = _combinedMeshGameObject.AddComponent<MeshRenderer>();
        }

        // If no mesh is assigned to the MeshFilter, create a new one.
        _combinedMesh = _combinedMeshFilter.sharedMesh;
        if (_combinedMesh == null)
        {
            _combinedMesh = new Mesh();
            _combinedMesh.name = "CombinedMesh_" + gameObject.name;
            _combinedMeshFilter.sharedMesh = _combinedMesh;
        }
    }

    /// <summary>
    /// The primary method to perform the mesh combination.
    /// It iterates through all child meshes, groups their submeshes by material,
    /// combines each material group into a temporary mesh, and then combines these
    /// temporary meshes into the final `_combinedMesh` with appropriate submeshes.
    /// </summary>
    [ContextMenu("Combine Meshes Now")]
    public void CombineMeshes()
    {
        // Re-initialize components if they became null (e.g., after clearing).
        if (_combinedMeshFilter == null || _combinedMeshRenderer == null || _combinedMesh == null)
        {
            InitializeCombinedGameObject();
            if (_combinedMeshFilter == null || _combinedMeshRenderer == null || _combinedMesh == null)
            {
                Debug.LogError("Failed to initialize combined mesh GameObject components. Combination aborted.", this);
                return;
            }
        }

        // Clear existing mesh data and material list for a fresh combination.
        _combinedMesh.Clear();
        _combinedMaterials.Clear();

        // Dictionary to store `CombineInstance` lists, keyed by material.
        // Each list will contain all submeshes from source objects that use a specific material.
        Dictionary<Material, List<CombineInstance>> materialCombinations = new Dictionary<Material, List<CombineInstance>>();

        // Find all MeshFilters in the source parent's hierarchy.
        // `_considerInactiveObjects` determines if inactive GameObjects are included.
        MeshFilter[] sourceMeshFilters = _sourceParent.GetComponentsInChildren<MeshFilter>(_considerInactiveObjects);

        // --- Stage 1: Gather and Group Source Meshes by Material ---
        foreach (MeshFilter mf in sourceMeshFilters)
        {
            // Skip the MeshFilter belonging to the combiner or the combined mesh GameObject itself.
            if (mf == _combinedMeshFilter || mf.gameObject == _sourceParent || mf.gameObject == _combinedMeshGameObject)
            {
                continue;
            }

            // Skip if the GameObject is inactive and we're not considering inactive objects.
            if (!mf.gameObject.activeInHierarchy && !_considerInactiveObjects)
            {
                continue;
            }

            // Validate source mesh and renderer.
            if (mf.sharedMesh == null)
            {
                Debug.LogWarning($"Skipping MeshFilter on {mf.name} as it has no shared mesh.", mf);
                continue;
            }

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                Debug.LogWarning($"Skipping MeshFilter on {mf.name} as it has no MeshRenderer.", mf);
                continue;
            }
            if (mr.sharedMaterials == null || mr.sharedMaterials.Length == 0)
            {
                Debug.LogWarning($"Skipping MeshFilter on {mf.name} as its MeshRenderer has no shared materials.", mf);
                continue;
            }

            // Iterate through each submesh of the current source object.
            // A single source object can have multiple submeshes, each with a different material.
            for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
            {
                // Ensure material index is valid for the renderer.
                if (sub >= mr.sharedMaterials.Length)
                {
                    Debug.LogWarning($"Material index {sub} out of bounds for {mr.name}'s sharedMaterials. Skipping submesh.", mr);
                    continue;
                }

                Material sourceMaterial = mr.sharedMaterials[sub];
                if (sourceMaterial == null)
                {
                    Debug.LogWarning($"Null material found for submesh {sub} on {mf.name}. Skipping this submesh.", mf);
                    continue;
                }

                // If this material is new, add it to our tracking lists.
                if (!materialCombinations.ContainsKey(sourceMaterial))
                {
                    materialCombinations[sourceMaterial] = new List<CombineInstance>();
                    _combinedMaterials.Add(sourceMaterial); // Store for final combined materials array
                }

                // Create a CombineInstance for this specific submesh.
                // The `transform` converts the source mesh's local vertices into the
                // `_combinedMeshGameObject`'s local space.
                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    subMeshIndex = sub, // Specifies which submesh of the source mesh to use.
                    transform = _combinedMeshGameObject.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
                };
                materialCombinations[sourceMaterial].Add(ci);
            }

            // --- Optional: Handle Original GameObjects ---
            // After processing, either destroy or disable the original objects.
            if (_destroyOriginalsAfterCombine)
            {
                #if UNITY_EDITOR
                if (EditorApplication.isPlaying) { Destroy(mf.gameObject); }
                else { DestroyImmediate(mf.gameObject); }
                #else
                Destroy(mf.gameObject);
                #endif
            }
            else
            {
                mf.gameObject.SetActive(false); // Disable the entire GameObject.
            }
        }

        // Handle case where no meshes were found for combination.
        if (_combinedMaterials.Count == 0)
        {
            Debug.LogWarning("No meshes found to combine. Combined mesh cleared and reset.", this);
            _combinedMesh.Clear();
            _combinedMeshFilter.sharedMesh = null;
            _combinedMeshRenderer.sharedMaterials = new Material[0];
            // If the mesh asset was created, remove it.
            #if UNITY_EDITOR
            if (!EditorApplication.isPlaying && _combinedMesh != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_combinedMesh);
                if (!string.IsNullOrEmpty(assetPath)) { AssetDatabase.DeleteAsset(assetPath); }
            }
            #endif
            // Destroy the runtime mesh object and re-initialize.
            if (_combinedMesh != null) DestroyImmediate(_combinedMesh);
            _combinedMesh = null; // Clear reference
            InitializeCombinedGameObject(); // Re-prepare for next combination
            return;
        }

        // --- Stage 2: Combine Each Material Group into Temporary Meshes ---
        // Each temporary mesh will contain all geometry that uses a specific material type.
        List<Mesh> temporaryMeshes = new List<Mesh>();
        foreach (Material mat in _combinedMaterials) // Iterate through materials in a consistent order
        {
            if (materialCombinations.TryGetValue(mat, out List<CombineInstance> instancesForMat))
            {
                Mesh tempMesh = new Mesh();
                tempMesh.name = "TempCombined_" + mat.name;
                tempMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Essential for large meshes (>65k vertices)
                
                // Combine all instances for this material into a single temporary mesh.
                // `mergeSubMeshes: true` ensures all source submeshes for this material are
                // merged into one single submesh within `tempMesh`.
                // `useWorldSpace: true` applies the transforms correctly.
                tempMesh.CombineMeshes(instancesForMat.ToArray(), true, true);
                temporaryMeshes.Add(tempMesh);
            }
        }

        // --- Stage 3: Combine Temporary Meshes into the Final Combined Mesh ---
        // Each temporary mesh (representing a unique material) becomes a distinct submesh
        // in the final `_combinedMesh`.
        _combinedMesh.subMeshCount = temporaryMeshes.Count; // Set the number of submeshes in the final mesh.
        
        List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
        for (int i = 0; i < temporaryMeshes.Count; i++)
        {
            if (temporaryMeshes[i] != null)
            {
                finalCombineInstances.Add(new CombineInstance
                {
                    mesh = temporaryMeshes[i],
                    subMeshIndex = 0, // We want to use the *first* (and only) submesh of the temporary mesh.
                    transform = Matrix4x4.identity // Temporary meshes are already in the combined object's local space.
                });
            }
        }

        // Combine the temporary meshes into the final _combinedMesh.
        // `mergeSubMeshes: false` is critical here: it tells `CombineMeshes` to treat each
        // `CombineInstance` (each of our temporary meshes) as a separate submesh in the target.
        _combinedMesh.CombineMeshes(finalCombineInstances.ToArray(), false, false);

        // --- Stage 4: Finalize Combined Mesh Properties and Materials ---
        _combinedMeshRenderer.sharedMaterials = _combinedMaterials.ToArray();
        _combinedMeshFilter.sharedMesh = _combinedMesh; // Assign the final combined mesh

        // Recalculate mesh properties for correct rendering, lighting, and physics.
        _combinedMesh.RecalculateNormals();
        _combinedMesh.RecalculateTangents(); // Important for normal mapping
        _combinedMesh.RecalculateBounds();
        _combinedMesh.Optimize(); // Optimize mesh for rendering performance

        Debug.Log($"Successfully combined {_combinedMaterials.Count} submeshes into '{_combinedMeshGameObject.name}'. Total vertices: {_combinedMesh.vertexCount}", this);

        // --- Stage 5: Clean up Temporary Meshes ---
        // Destroy temporary meshes to free up memory.
        foreach (Mesh tempM in temporaryMeshes)
        {
            if (tempM != null)
            {
                #if UNITY_EDITOR
                if (EditorApplication.isPlaying) { Destroy(tempM); }
                else { DestroyImmediate(tempM); }
                #else
                Destroy(tempM);
                #endif
            }
        }

        // --- Editor-specific: Save Combined Mesh as Asset ---
        #if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            // Ensure a folder exists to save assets.
            string path = "Assets/_CombinedMeshes";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets", "_CombinedMeshes");
            }

            // Generate a unique asset path based on name and timestamp.
            string assetPath = $"{path}/{_combinedMesh.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            AssetDatabase.CreateAsset(_combinedMesh, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Combined mesh saved as asset: {assetPath}");
            
            // Assign materials properly in the editor for persistence.
            _combinedMeshRenderer.sharedMaterials = _combinedMaterials.ToArray();
        }
        #endif
    }

    /// <summary>
    /// Clears the current combined mesh, detaches it from the MeshFilter, and resets materials.
    /// In the editor, it also attempts to delete the mesh asset from the project.
    /// </summary>
    [ContextMenu("Clear Combined Mesh")]
    public void ClearCombinedMesh()
    {
        if (_combinedMeshFilter != null && _combinedMeshFilter.sharedMesh != null)
        {
            // Get a reference to the mesh before clearing its sharedMesh reference
            Mesh meshToClear = _combinedMeshFilter.sharedMesh;

            _combinedMeshFilter.sharedMesh.Clear(); // Clear the mesh data
            _combinedMeshFilter.sharedMesh = null;  // Detach the mesh from the MeshFilter
            _combinedMeshRenderer.sharedMaterials = new Material[0]; // Clear materials
            _combinedMaterials.Clear(); // Clear internal material list
            _combinedMesh = null; // Invalidate internal mesh reference
            Debug.Log("Combined mesh cleared.", this);

            // --- Editor-specific: Delete Mesh Asset ---
            #if UNITY_EDITOR
            if (meshToClear != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(meshToClear);
                if (!string.IsNullOrEmpty(assetPath)) // Only delete if it's a saved asset
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Deleted combined mesh asset: {assetPath}");
                }
            }
            #endif
            // Re-initialize to prepare for potential future combinations.
            InitializeCombinedGameObject();
        }
        else
        {
            Debug.Log("No combined mesh to clear or already cleared.", this);
        }
    }

    /// <summary>
    /// Adds a new GameObject to the source parent's children and triggers a full re-combination.
    /// This demonstrates the "Dynamic" aspect of the pattern.
    /// </summary>
    /// <param name="newObject">The GameObject to add to the combination.</param>
    public void AddObjectAndRecombine(GameObject newObject)
    {
        if (newObject == null)
        {
            Debug.LogWarning("Attempted to add a null object for combination.", this);
            return;
        }

        // Make the new object a child of the source parent.
        newObject.transform.SetParent(_sourceParent.transform);
        Debug.Log($"Added {newObject.name} to combine. Re-combining meshes...", this);
        CombineMeshes(); // Re-trigger the combination.
    }

    /// <summary>
    /// Removes a GameObject from the source parent's children and triggers a full re-combination.
    /// This demonstrates the "Dynamic" aspect of the pattern. The object is destroyed.
    /// </summary>
    /// <param name="objectToRemove">The GameObject to remove from the combination.</param>
    public void RemoveObjectAndRecombine(GameObject objectToRemove)
    {
        if (objectToRemove == null)
        {
            Debug.LogWarning("Attempted to remove a null object from combination.", this);
            return;
        }

        // Ensure the object is indeed a child of the source parent.
        if (objectToRemove.transform.parent == _sourceParent.transform)
        {
            Debug.Log($"Removing {objectToRemove.name} from combine. Re-combining meshes...", this);
            #if UNITY_EDITOR
            if (EditorApplication.isPlaying) { Destroy(objectToRemove); }
            else { DestroyImmediate(objectToRemove); }
            #else
            Destroy(objectToRemove);
            #endif
            CombineMeshes(); // Re-trigger the combination.
        }
        else
        {
            Debug.LogWarning($"{objectToRemove.name} is not a child of {_sourceParent.name}. Cannot remove from combination.", objectToRemove);
        }
    }
}
```