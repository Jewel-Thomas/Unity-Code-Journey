// Unity Design Pattern Example: CameraOcclusionCulling
// This script demonstrates the CameraOcclusionCulling pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a script-driven approach to managing object visibility based on camera position and view frustum. While Unity has a highly optimized built-in **Occlusion Culling** system (which is GPU-driven and typically bakes static scene data), this custom "Camera Occlusion Culling" pattern provides a script-based alternative or supplement.

This pattern is useful for:
*   **Dynamic Objects:** When Unity's built-in occlusion culling (which primarily works for static geometry) is not suitable.
*   **Custom Culling Logic:** When you need more control over *when* and *how* objects are culled (e.g., based on custom conditions, specific object types, or unique distance metrics).
*   **Performance Optimization:** For scenes with many small objects that are frequently out of view or too far away, this can reduce the number of objects passed to the rendering pipeline, even if Unity's frustum culling would eventually handle them.
*   **Learning:** To understand the principles behind visibility management.

---

### **CameraOcclusionCulling Pattern Explanation**

The pattern is implemented using a **Manager-Component** structure:

1.  **`CameraVisibilityManager` (The Manager):**
    *   A central singleton responsible for orchestrating the culling process.
    *   It maintains a list of all objects that are registered to be culled.
    *   It periodically calculates the camera's frustum planes and position.
    *   It then iterates through all registered objects, instructing them to update their visibility.
    *   It can include global culling parameters like a maximum culling distance or a layer for simple occluders.

2.  **`CullableItem` (The Component):**
    *   Attached to any `GameObject` that needs to be managed by the `CameraVisibilityManager`.
    *   It registers itself with the manager when enabled and unregisters when disabled.
    *   It holds a reference to its `Renderer` component and its `Bounds`.
    *   It receives culling instructions (frustum planes, camera position) from the manager.
    *   It applies the culling logic (frustum check, distance check, optional simple occlusion check) to determine if its `Renderer` should be enabled or disabled.

**Key Culling Logic Demonstrated:**

*   **Frustum Culling:** Checks if an object's bounding box intersects the camera's view frustum. If not, it's hidden.
*   **Distance Culling:** Hides objects that are beyond a specified maximum distance from the camera.
*   **Simple "Occlusion" Heuristic (Optional):** This example includes a *very basic* form of occlusion by raycasting from the camera to the object's center. If it hits an object on a designated "Occluder" layer first, the object is considered occluded. **Important Note:** This is a simplistic heuristic and not a true, robust occlusion system like Unity's built-in one. Per-pixel occlusion in script is extremely performance-intensive and typically not practical.

---

### **1. `CameraVisibilityManager.cs`**

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CameraVisibilityManager: The central manager for the CameraOcclusionCulling pattern.
/// It orchestrates visibility checks for all registered CullableItem components.
/// This acts as a 'Manager' in a Manager-Component design pattern.
/// </summary>
public class CameraVisibilityManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a globally accessible instance of the manager.
    public static CameraVisibilityManager Instance { get; private set; }

    [Header("Culling Settings")]
    [Tooltip("Maximum distance from the camera beyond which objects will be culled.")]
    [SerializeField]
    private float cullingDistance = 100f;

    [Tooltip("How often (in seconds) the culling logic runs. A higher value means less frequent checks but better performance.")]
    [SerializeField]
    private float cullingIntervalSeconds = 0.5f;

    [Tooltip("Layer mask for objects that should act as occluders for the simple occlusion check. " +
             "Leave 'Nothing' for no simple occlusion check.")]
    [SerializeField]
    private LayerMask occluderLayer; // For basic raycast occlusion

    [Tooltip("Offset applied to the raycast origin from the camera to prevent self-occlusion artifacts.")]
    [SerializeField]
    private float raycastOriginOffset = 0.5f; // Small offset for raycast origin

    // --- Private Members ---
    private Camera mainCamera; // Reference to the main camera
    private Plane[] frustumPlanes; // Stores the current camera frustum planes
    private readonly List<CullableItem> registeredCullables = new List<CullableItem>(); // List of all items to be culled
    private Coroutine cullingCoroutine; // Reference to the running culling coroutine

    // --- Properties for CullableItems to access ---
    public float CullingDistance => cullingDistance;
    public LayerMask OccluderLayer => occluderLayer;
    public float RaycastOriginOffset => raycastOriginOffset;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Implements the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CameraVisibilityManager: Another instance of CameraVisibilityManager found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure the main camera is set. If not, try to find it.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("CameraVisibilityManager: No 'MainCamera' tag found. Please tag your main camera as 'MainCamera'.");
            }
        }
    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active.
    /// Starts the periodic culling coroutine.
    /// </summary>
    private void OnEnable()
    {
        // Start the culling loop only if a camera is present
        if (mainCamera != null)
        {
            if (cullingCoroutine != null)
            {
                StopCoroutine(cullingCoroutine);
            }
            cullingCoroutine = StartCoroutine(CullingLoop());
        }
    }

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive.
    /// Stops the culling coroutine to prevent errors when the manager is inactive.
    /// </summary>
    private void OnDisable()
    {
        if (cullingCoroutine != null)
        {
            StopCoroutine(cullingCoroutine);
            cullingCoroutine = null;
        }
    }

    /// <summary>
    /// Registers a CullableItem with the manager.
    /// This allows the manager to include the item in its visibility checks.
    /// </summary>
    /// <param name="item">The CullableItem to register.</param>
    public void RegisterCullable(CullableItem item)
    {
        if (!registeredCullables.Contains(item))
        {
            registeredCullables.Add(item);
            // Immediately update visibility for newly registered items
            if (mainCamera != null)
            {
                item.UpdateVisibility(frustumPlanes, mainCamera.transform.position, mainCamera.transform.forward);
            }
        }
    }

    /// <summary>
    /// Unregisters a CullableItem from the manager.
    /// This removes the item from future visibility checks.
    /// </summary>
    /// <param name="item">The CullableItem to unregister.</param>
    public void UnregisterCullable(CullableItem item)
    {
        if (registeredCullables.Contains(item))
        {
            registeredCullables.Remove(item);
            // Ensure the item is visible if it's no longer managed (or hide it, depending on desired behavior)
            if (item.TryGetComponent(out Renderer renderer) && !renderer.enabled)
            {
                renderer.enabled = true; // Make it visible by default when unmanaged
            }
        }
    }

    /// <summary>
    /// The main culling loop, run as a coroutine for performance.
    /// It periodically updates camera data and instructs registered items to update their visibility.
    /// </summary>
    private IEnumerator CullingLoop()
    {
        while (true)
        {
            // Wait for the specified interval before the next culling pass
            yield return new WaitForSeconds(cullingIntervalSeconds);

            // Re-find camera if it was destroyed or not found initially
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("CameraVisibilityManager: Main camera not found for culling. Skipping this pass.");
                    continue; // Skip this iteration if no camera
                }
            }

            // 1. Update camera frustum planes for frustum culling
            // GeometryUtility.CalculateFrustumPlanes is efficient.
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            Vector3 camPosition = mainCamera.transform.position;
            Vector3 camForward = mainCamera.transform.forward; // For simple occlusion raycast direction

            // 2. Iterate through all registered cullable items and update their visibility
            for (int i = 0; i < registeredCullables.Count; i++)
            {
                CullableItem item = registeredCullables[i];
                if (item != null) // Check if the item hasn't been destroyed
                {
                    item.UpdateVisibility(frustumPlanes, camPosition, camForward);
                }
                else
                {
                    // Clean up null references from the list (e.g., destroyed game objects)
                    registeredCullables.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
        }
    }
}
```

---

### **2. `CullableItem.cs`**

```csharp
using UnityEngine;

/// <summary>
/// CullableItem: A component that marks a GameObject to be managed by the CameraVisibilityManager.
/// It represents the 'Component' part of the Manager-Component design pattern.
/// </summary>
[RequireComponent(typeof(Renderer))] // Ensures the GameObject has a Renderer to enable/disable
public class CullableItem : MonoBehaviour
{
    private Renderer itemRenderer; // Reference to the GameObject's renderer
    private Bounds itemBounds; // The world-space bounding box of the renderer
    private bool isCurrentlyVisible = true; // Tracks the current visibility state to avoid redundant updates

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Gets component references and calculates initial bounds.
    /// </summary>
    private void Awake()
    {
        itemRenderer = GetComponent<Renderer>();
        // Cache the world-space bounds of the renderer.
        // For static objects, this only needs to be calculated once.
        // For moving objects, consider recalculating if bounds change significantly.
        itemBounds = itemRenderer.bounds;
    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active.
    /// Registers this item with the CameraVisibilityManager.
    /// </summary>
    private void OnEnable()
    {
        if (CameraVisibilityManager.Instance != null)
        {
            CameraVisibilityManager.Instance.RegisterCullable(this);
        }
        else
        {
            Debug.LogWarning("CullableItem on " + gameObject.name + ": CameraVisibilityManager not found. Object will not be culled.", this);
            itemRenderer.enabled = true; // Ensure visible if not managed
        }
    }

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive.
    /// Unregisters this item from the CameraVisibilityManager.
    /// </summary>
    private void OnDisable()
    {
        if (CameraVisibilityManager.Instance != null)
        {
            CameraVisibilityManager.Instance.UnregisterCullable(this);
        }
    }

    /// <summary>
    /// Updates the visibility of this item based on camera parameters.
    /// This method is called by the CameraVisibilityManager.
    /// </summary>
    /// <param name="frustumPlanes">The current camera frustum planes.</param>
    /// <param name="cameraPosition">The current position of the camera.</param>
    /// <param name="cameraForward">The forward direction of the camera.</param>
    public void UpdateVisibility(Plane[] frustumPlanes, Vector3 cameraPosition, Vector3 cameraForward)
    {
        // If the renderer is somehow null (e.g., component removed dynamically), do nothing.
        if (itemRenderer == null) return;

        bool shouldBeVisible = true;

        // --- 1. Frustum Culling Check ---
        // GeometryUtility.TestPlanesAABB is an efficient way to check if a bounding box
        // intersects or is contained within the camera's view frustum.
        // If it doesn't intersect, the object is outside the camera's view.
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, itemBounds))
        {
            shouldBeVisible = false;
        }

        // --- 2. Distance Culling Check (if still visible) ---
        if (shouldBeVisible)
        {
            // Calculate squared distance for performance (avoids square root)
            float sqrDistance = (transform.position - cameraPosition).sqrMagnitude;
            float sqrCullingDistance = CameraVisibilityManager.Instance.CullingDistance * CameraVisibilityManager.Instance.CullingDistance;

            if (sqrDistance > sqrCullingDistance)
            {
                shouldBeVisible = false;
            }
        }

        // --- 3. Simple Occlusion Check (if still visible and occluder layer is set) ---
        // This is a basic heuristic: cast a ray from the camera to the object's center.
        // If the ray hits something on the 'occluderLayer' before hitting the object, it's considered occluded.
        // This is NOT a robust occlusion system but can work for large, simple occluders (e.g., walls).
        if (shouldBeVisible && CameraVisibilityManager.Instance.OccluderLayer.value != 0) // Check if occluder layer is configured
        {
            Vector3 rayOrigin = cameraPosition + cameraForward * CameraVisibilityManagerManager.Instance.RaycastOriginOffset;
            Vector3 direction = (transform.position - rayOrigin).normalized;
            float distance = Vector3.Distance(rayOrigin, transform.position);

            RaycastHit hit;
            // Raycast only against the specified occluder layer.
            if (Physics.Raycast(rayOrigin, direction, out hit, distance, CameraVisibilityManager.Instance.OccluderLayer))
            {
                // If the ray hits an occluder before reaching the object's center, it's occluded.
                // We add a small epsilon to distance to ensure we're not hitting the object itself.
                if (hit.collider.gameObject != gameObject && hit.distance < distance - 0.1f) // Ensure it's not self-occlusion
                {
                    shouldBeVisible = false;
                }
            }
        }

        // --- Update Renderer Visibility ---
        // Only update if the visibility state has actually changed to minimize component access.
        if (isCurrentlyVisible != shouldBeVisible)
        {
            itemRenderer.enabled = shouldBeVisible;
            isCurrentlyVisible = shouldBeVisible;
            // Optionally, log changes for debugging
            // Debug.Log($"{gameObject.name} visibility changed to: {shouldBeVisible}");
        }
    }

    /// <summary>
    /// OnDrawGizmos is called for rendering and selecting gizmos.
    /// Draws the object's bounds for debugging purposes.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (itemRenderer != null)
        {
            Gizmos.color = isCurrentlyVisible ? Color.green : Color.red;
            Gizmos.DrawWireCube(itemBounds.center, itemBounds.size);
        }
    }
}
```

---

### **Example Usage in Unity:**

1.  **Create Manager:**
    *   Create an empty GameObject in your scene (e.g., named `VisibilityManager`).
    *   Attach the `CameraVisibilityManager.cs` script to it.
    *   **Configure Manager:**
        *   Adjust `Culling Distance` (e.g., 50-200 units).
        *   Adjust `Culling Interval Seconds` (e.g., 0.2 to 1.0 seconds for performance, lower for more responsiveness).
        *   (Optional) For simple occlusion:
            *   Go to `Edit -> Project Settings -> Layers`.
            *   Add a new layer (e.g., `Occluder`).
            *   Select this `Occluder` layer in the `Occluder Layer` dropdown on your `CameraVisibilityManager` component.

2.  **Prepare Camera:**
    *   Ensure your main camera is tagged `MainCamera` in the Inspector.

3.  **Add Cullable Items:**
    *   Create some GameObjects (e.g., 3D Cubes, Spheres).
    *   For each GameObject you want to be managed by the system, attach the `CullableItem.cs` script.
    *   Ensure these GameObjects have a `Renderer` component (e.g., `MeshRenderer` for default shapes).

4.  **(Optional) Add Simple Occluders:**
    *   Create a large plane or cube (e.g., a "Wall").
    *   Set its `Layer` to the `Occluder` layer you created earlier. This object will now potentially block the view of `CullableItem`s.

5.  **Run the Scene:**
    *   As you move your camera, objects with `CullableItem` will automatically be enabled/disabled based on distance, frustum, and (if configured) simple occlusion.
    *   You can observe the `Renderer.enabled` state in the Inspector for your `CullableItem` objects.
    *   The `OnDrawGizmos` in `CullableItem` will show green wireframes for visible items and red for culled items, helping visualize the culling.

This complete setup provides a practical and educational example of the `CameraOcclusionCulling` pattern implemented via a custom Manager-Component system in Unity.