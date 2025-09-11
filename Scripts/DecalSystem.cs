// Unity Design Pattern Example: DecalSystem
// This script demonstrates the DecalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The DecalSystem design pattern in Unity provides an efficient way to render temporary or dynamic visual effects (like bullet holes, blood splatters, scorch marks, footprints, etc.) on surfaces. It primarily leverages **Object Pooling** to manage Decal GameObjects, reducing the overhead of instantiating and destroying them frequently.

Here's how this example implements the pattern:

1.  **`DecalSystemManager` (Manager/Controller):**
    *   A Singleton responsible for managing all decals.
    *   Maintains a pool of inactive `Decal` GameObjects for reuse.
    *   Handles requests to place new decals: retrieves from pool, positions, configures, and activates.
    *   Enforces limits on the total number of active decals, automatically deactivating the oldest ones when the limit is reached (FIFO - First-In, First-Out).
    *   Manages decal lifetime and triggers their fade-out process.

2.  **`Decal` (Individual Decal Component):**
    *   A `MonoBehaviour` attached to the decal prefab.
    *   Represents a single visual decal.
    *   Manages its own `MeshRenderer` and material instance (crucial for independent fading).
    *   Handles its activation, deactivation, and the fade-out logic via a Coroutine.
    *   Communicates back to the `DecalSystemManager` when its lifecycle is complete (to be returned to the pool).

This setup allows for a high volume of dynamic decals with minimal performance impact, as GameObjects are reused rather than constantly created and destroyed.

---

## DecalSystem Unity Example

You'll need two C# scripts and one Unity Prefab:

1.  **`Decal.cs`**: The component for an individual decal.
2.  **`DecalSystemManager.cs`**: The central manager for all decals.
3.  **A Decal Prefab**: A simple Quad GameObject with the `Decal.cs` component and a transparent material.

---

### 1. `Decal.cs`

This script should be attached to your decal prefab. It manages the visual properties and lifecycle of a single decal instance.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
///     DecalSystem Pattern: Decal Component
///     
///     This component represents a single decal in the scene. It manages its
///     own visual representation, activation, deactivation, and fading lifecycle.
///     It works in conjunction with the DecalSystemManager to provide a performant
///     and organized way to handle dynamic decals.
/// </summary>
[RequireComponent(typeof(MeshRenderer))] // A decal always needs a MeshRenderer to display
public class Decal : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material decalMaterialInstance; // An instance of the material to allow individual property changes (like alpha)
    private Color startColor;
    private Coroutine fadeCoroutine;

    // Public properties to be set by the DecalSystemManager
    public float maxLifeTime { get; private set; }
    public float fadeDuration { get; private set; }
    public float currentLifeTime { get; private set; }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches the MeshRenderer and creates a material instance.
    /// This is crucial for each decal to fade independently without
    /// affecting the shared material asset.
    /// </summary>
    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        // Get the material from the renderer and make an instance of it.
        // This is crucial to avoid modifying the shared material asset.
        decalMaterialInstance = new Material(meshRenderer.material);
        meshRenderer.material = decalMaterialInstance;
        
        // IMPORTANT: Ensure your decal material's shader supports transparency (e.g., URP/Lit with Render Mode: Fade)
        // If your material doesn't already have its render mode set to Fade/Transparent, 
        // the alpha property will have no visual effect.
        // The following lines can programmatically set it, but it's best configured in the Inspector:
        // decalMaterialInstance.SetOverrideTag("RenderType", "Transparent");
        // decalMaterialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        // decalMaterialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // decalMaterialInstance.SetInt("_ZWrite", 0);
        // decalMaterialInstance.DisableKeyword("_ALPHATEST_ON");
        // decalMaterialInstance.EnableKeyword("_ALPHABLEND_ON");
        // decalMaterialInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    /// <summary>
    /// Initializes the decal with its specific material, lifetime, and fade duration.
    /// Called by DecalSystemManager when a decal is activated from the pool.
    /// </summary>
    /// <param name="material">The material to use for this decal.</param>
    /// <param name="lifeTime">The total time this decal will be active before fading starts.</param>
    /// <param name="fadeDur">The duration of the fade-out process.</param>
    public void Setup(Material material, float lifeTime, float fadeDur)
    {
        // Copy properties from the provided material to our instance
        // This effectively assigns the new texture/color etc.
        decalMaterialInstance.CopyPropertiesFrom(material);
        
        maxLifeTime = lifeTime;
        fadeDuration = fadeDur;
        currentLifeTime = 0f; // Reset life time for reuse
        
        startColor = decalMaterialInstance.color; // Store initial color for fading
        startColor.a = 1f; // Ensure full opacity at start
        decalMaterialInstance.color = startColor;
    }

    /// <summary>
    /// Activates and positions the decal in the scene.
    /// Called by DecalSystemManager when placing a new decal.
    /// </summary>
    /// <param name="position">World position for the decal.</param>
    /// <param name="rotation">World rotation for the decal.</param>
    /// <param name="scale">Local scale for the decal.</param>
    public void Activate(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
        
        gameObject.SetActive(true); // Make the decal visible
        
        // Stop any previous fade coroutine if this decal is being reused quickly
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        currentLifeTime = 0f; // Reset life time for tracking
        startColor.a = 1f; // Ensure full opacity on activation
        decalMaterialInstance.color = startColor; // Reset material alpha to full
    }

    /// <summary>
    /// Deactivates the decal, hiding it and preparing it for reuse in the pool.
    /// </summary>
    public void Deactivate()
    {
        gameObject.SetActive(false); // Hide the decal
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    /// <summary>
    /// Updates the decal's lifecycle, tracking its age and initiating fade-out
    /// when its max life time is reached.
    /// </summary>
    void Update()
    {
        if (!gameObject.activeSelf) return;

        currentLifeTime += Time.deltaTime;

        // If lifetime exceeded and not already fading, start fading
        if (currentLifeTime >= maxLifeTime && fadeCoroutine == null)
        {
            FadeOutAndDeactivate();
        }
    }

    /// <summary>
    /// Initiates the fade-out process for the decal.
    /// </summary>
    private void FadeOutAndDeactivate()
    {
        if (fadeDuration > 0)
        {
            fadeCoroutine = StartCoroutine(FadeCoroutine());
        }
        else // No fade duration, deactivate immediately
        {
            DecalSystemManager.Instance.ReturnDecalToPool(this);
        }
    }

    /// <summary>
    /// Coroutine to gradually fade out the decal's material over time.
    /// </summary>
    private IEnumerator FadeCoroutine()
    {
        float timer = 0f;
        Color currentColor = startColor; // Start from the decal's initial color

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            currentColor.a = alpha;
            decalMaterialInstance.color = currentColor;
            yield return null;
        }

        // Ensure it's fully transparent at the end
        currentColor.a = 0f;
        decalMaterialInstance.color = currentColor;
        
        // Return to pool after fading
        DecalSystemManager.Instance.ReturnDecalToPool(this);
    }
}
```

---

### 2. `DecalSystemManager.cs`

This script should be placed on an empty GameObject in your scene. It acts as the central hub for the decal system.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List and Queue

/// <summary>
///     DecalSystem Pattern: DecalSystemManager
///     
///     This class implements the DecalSystem design pattern. It acts as a centralized
///     manager for creating, reusing, and managing decals in a Unity scene.
///     
///     Key features of the DecalSystem pattern demonstrated here:
///     1.  **Object Pooling:** Reuses `Decal` GameObjects to reduce instantiation/destruction
///         overhead, improving performance.
///     2.  **Centralized Control (Singleton):** Provides a single point of access (`Instance`)
///         for placing and managing decals, simplifying client code.
///     3.  **Lifecycle Management:** Handles decal activation, deactivation, and automatic
///         removal (e.g., after a certain lifetime or when max decals limit is reached).
///     4.  **Configurability:** Exposes various parameters in the Inspector for easy customization
///         of decal appearance, behavior, and limits.
/// </summary>
public class DecalSystemManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global access point to the DecalSystemManager instance.
    public static DecalSystemManager Instance { get; private set; }

    [Header("Decal Prefab & Pooling")]
    [Tooltip("The prefab for a single decal. It should have a MeshRenderer and a Decal component.")]
    [SerializeField] private Decal decalPrefab;
    [Tooltip("The initial number of decals to create in the pool at startup.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("The maximum number of active decals allowed in the scene at any time. Oldest decals are removed first.")]
    [SerializeField] private int maxActiveDecals = 30;

    [Header("Decal Appearance & Behavior")]
    [Tooltip("The default material to use for decals if no specific material is provided during placement.")]
    [SerializeField] private Material defaultDecalMaterial;
    [Tooltip("The uniform scale of the placed decals.")]
    [SerializeField] private float decalSize = 1.0f;
    [Tooltip("The offset from the surface normal to prevent Z-fighting with the underlying geometry.")]
    [SerializeField] private float decalOffsetFromSurface = 0.01f;
    [Tooltip("The total time a decal remains visible before it starts fading out.")]
    [SerializeField] private float decalLifeTime = 5.0f;
    [Tooltip("The duration of the fade-out effect for a decal.")]
    [SerializeField] private float decalFadeDuration = 1.0f;

    [Header("Placement Settings")]
    [Tooltip("The LayerMask used for raycasting when placing decals. Only layers in this mask will be hit.")]
    [SerializeField] private LayerMask placementLayerMask = ~0; // Default to everything

    // --- Internal State ---
    private Queue<Decal> activeDecals; // Stores currently active decals in order of placement (oldest first)
    private List<Decal> decalPool;     // Stores inactive decals ready for reuse

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the singleton pattern and initializes the object pool.
    /// </summary>
    void Awake()
    {
        // Singleton enforcement: Ensures only one instance of DecalSystemManager exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DecalSystemManager: Another instance found, destroying this one. Only one DecalSystemManager should exist per scene.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    /// <summary>
    /// Initializes the decal object pool by instantiating 'initialPoolSize' decals.
    /// </summary>
    private void InitializePool()
    {
        activeDecals = new Queue<Decal>();
        decalPool = new List<Decal>();

        if (decalPrefab == null)
        {
            Debug.LogError("DecalSystemManager: Decal Prefab is not assigned! Decals cannot be placed.");
            return;
        }

        // Pre-instantiate decals to populate the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPooledDecal();
        }
    }

    /// <summary>
    /// Creates a new Decal GameObject, adds it to the pool, and deactivates it.
    /// </summary>
    /// <returns>The newly created Decal component.</returns>
    private Decal CreateNewPooledDecal()
    {
        // Instantiate the prefab and parent it under the manager for scene organization
        Decal newDecal = Instantiate(decalPrefab, transform); 
        newDecal.name = "Decal_" + decalPool.Count;
        newDecal.Deactivate(); // Start inactive
        decalPool.Add(newDecal);
        return newDecal;
    }

    /// <summary>
    /// Gets a Decal component from the pool. If no inactive decals are available,
    /// a new one is created (dynamic pool expansion).
    /// </summary>
    /// <returns>An available Decal component ready for activation.</returns>
    private Decal GetDecalFromPool()
    {
        Decal decal = null;

        // Try to find an inactive decal in the pool
        for (int i = 0; i < decalPool.Count; i++)
        {
            if (!decalPool[i].gameObject.activeSelf)
            {
                decal = decalPool[i];
                break;
            }
        }

        // If no inactive decal found, create a new one (expands pool dynamically)
        if (decal == null)
        {
            Debug.LogWarning("DecalSystemManager: Pool exhausted, creating new decal. Consider increasing initialPoolSize.");
            decal = CreateNewPooledDecal();
        }

        return decal;
    }

    /// <summary>
    /// Returns a Decal component to the pool, deactivating it.
    /// This method is typically called by the Decal itself when its lifecycle ends.
    /// Note: This does NOT remove the decal from the activeDecals queue directly.
    /// It relies on the queue's natural FIFO behavior or the `ClearAllActiveDecals` method
    /// to eventually remove the reference from the queue when it naturally reaches the front.
    /// </summary>
    /// <param name="decal">The Decal to return to the pool.</param>
    public void ReturnDecalToPool(Decal decal)
    {
        decal.Deactivate();
        // The decal is now inactive and available for reuse by GetDecalFromPool().
        // It remains in the 'activeDecals' queue (as a reference) until it's naturally
        // dequeued when 'maxActiveDecals' is exceeded or 'ClearAllActiveDecals' is called.
    }

    /// <summary>
    /// Public method to place a decal at a specific world position, oriented to a normal.
    /// This is the primary method client scripts should call to create decals.
    /// </summary>
    /// <param name="position">The world position where the decal should be placed.</param>
    /// <param name="normal">The surface normal at the placement position (decal will face away from this normal).</param>
    /// <param name="overrideMaterial">Optional material to use for this specific decal. If null, defaultDecalMaterial is used.</param>
    /// <param name="randomRotation">If true, applies a random rotation around the normal axis to add variety.</param>
    public Decal PlaceDecal(Vector3 position, Vector3 normal, Material overrideMaterial = null, bool randomRotation = true)
    {
        if (defaultDecalMaterial == null && overrideMaterial == null)
        {
            Debug.LogError("DecalSystemManager: Cannot place decal. No default material and no override material provided.");
            return null;
        }

        // Enforce max active decals limit: remove the oldest decal if the limit is reached.
        if (activeDecals.Count >= maxActiveDecals)
        {
            Decal oldestDecal = activeDecals.Dequeue(); // Get the oldest decal from the front of the queue
            oldestDecal.Deactivate(); // Immediately deactivate and effectively remove from scene/stop any fading
        }

        Decal newDecal = GetDecalFromPool(); // Get an available decal from the pool
        if (newDecal == null) return null; // Should not happen with dynamic pooling

        // Calculate rotation: Align decal's forward (+Z for Unity Quad) with the surface normal
        Quaternion rotation = Quaternion.LookRotation(normal); 
        if (randomRotation)
        {
            // Apply a random Z-axis rotation to the decal (on the surface plane) for visual variety
            rotation *= Quaternion.Euler(0, 0, Random.Range(0f, 360f)); 
        }

        // Apply slight offset to prevent Z-fighting with the underlying surface
        Vector3 finalPosition = position + normal * decalOffsetFromSurface;
        
        // Prepare decal with the chosen material, lifetime, and fade duration
        newDecal.Setup(overrideMaterial != null ? overrideMaterial : defaultDecalMaterial, decalLifeTime, decalFadeDuration);
        // Activate decal at the calculated position, rotation, and scale
        newDecal.Activate(finalPosition, rotation, Vector3.one * decalSize);

        activeDecals.Enqueue(newDecal); // Add to active decals queue (becomes the newest decal)
        return newDecal;
    }

    /// <summary>
    /// Example usage in Update for demonstration purposes:
    /// Places decals on mouse click and clears them on 'C' key press.
    /// This part demonstrates how a client script would interact with the DecalSystemManager.
    /// </summary>
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform a raycast to detect where the mouse clicked on a surface
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, placementLayerMask))
            {
                // Place a decal at the hit point, facing away from the surface normal
                PlaceDecal(hit.point, hit.normal); 
                
                // --- Example of placing a decal with a custom material ---
                // You could pass a specific material based on the hit object or other logic:
                // if (hit.collider.CompareTag("Wood"))
                // {
                //     DecalSystemManager.Instance.PlaceDecal(hit.point, hit.normal, woodDecalMaterial);
                // }
                // else 
                // {
                //     DecalSystemManager.Instance.PlaceDecal(hit.point, hit.normal); // Uses default material
                // }
            }
        }
        
        // Example for clearing all decals (e.g., for level reset, or cleanup)
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllActiveDecals();
        }
    }

    /// <summary>
    /// Clears all currently active decals by returning them to the pool.
    /// This is useful for resetting the scene or cleaning up decals.
    /// </summary>
    public void ClearAllActiveDecals()
    {
        while (activeDecals.Count > 0)
        {
            Decal decal = activeDecals.Dequeue(); // Get and remove decal from queue
            decal.Deactivate(); // Deactivate stops fade coroutines and sets inactive
        }
        Debug.Log("All decals cleared from scene.");
    }
}
```

---

### 3. Setup in Unity (Example Usage)

Follow these steps to get the DecalSystem running in your Unity project:

#### A. Create the Decal Prefab

1.  **Create an Empty GameObject:** In your Unity Hierarchy, right-click -> `Create Empty`. Name it `DecalPrefab`.
2.  **Add a Quad Mesh:** Right-click on `DecalPrefab` -> `3D Object` -> `Quad`.
    *   Ensure the `Quad` child's `Transform` has `Position: (0,0,0)`, `Rotation: (0,0,0)`, `Scale: (1,1,1)`.
    *   *(A standard Unity Quad faces its local +Z axis).*
3.  **Create a Decal Material:**
    *   In your Project window, right-click -> `Create` -> `Material`. Name it `BulletHole_Material` (or similar).
    *   **Set Shader:** Change its shader to `Universal Render Pipeline/Lit` (if you're using URP) or `Standard` (if using Built-in RP).
    *   **Set Rendering Mode:** Change the `Rendering Mode` to `Fade` or `Transparent`. This is crucial for the decal to be able to fade out.
    *   **Assign Texture:** Drag a transparent texture (e.g., a bullet hole, blood splatter, or crack with an alpha channel) to the `Albedo (Color)` slot.
    *   **Adjust Color:** Optionally adjust the `Color` tint if needed.
4.  **Apply Material to Quad:** Drag your `BulletHole_Material` onto the `Mesh Renderer` component of the `Quad` child of `DecalPrefab`.
5.  **Add `Decal` Component:** Select the `DecalPrefab` (the parent GameObject), and click `Add Component`. Search for and add the `Decal` script.
6.  **Make it a Prefab:** Drag the `DecalPrefab` from your Hierarchy into your Project window (e.g., into a `Prefabs` folder) to create a prefab.
7.  **Delete from Hierarchy:** You can now delete `DecalPrefab` from your Hierarchy, as it exists as a Project asset.

#### B. Setup the DecalSystemManager

1.  **Create an Empty GameObject:** In your Hierarchy, right-click -> `Create Empty`. Name it `DecalSystem`.
2.  **Add `DecalSystemManager` Component:** Select `DecalSystem`, click `Add Component`, and search for and add the `DecalSystemManager` script.
3.  **Configure in Inspector:**
    *   **`Decal Prefab`**: Drag your `DecalPrefab` (from the Project window) into this slot.
    *   **`Default Decal Material`**: Drag your `BulletHole_Material` (or another general-purpose decal material) into this slot.
    *   **Adjust Settings:** Modify `Initial Pool Size`, `Max Active Decals`, `Decal Size`, `Decal Offset From Surface`, `Decal Life Time`, `Decal Fade Duration` to suit your project's needs.
    *   **`Placement Layer Mask`**: Set this to the layers your decals should stick to (e.g., "Default", "Ground", "Walls"). **Uncheck layers like "UI" or "Ignore Raycast"** to prevent accidental decal placement.

#### C. Ensure Main Camera Tag

*   Make sure your main camera in the scene (the one you're looking through) has its `Tag` set to `MainCamera`. The `DecalSystemManager` uses `Camera.main` for raycasting, which relies on this tag.

#### D. Run the Scene!

*   Play your Unity scene.
*   **Left-click** anywhere on a valid surface (within your `Placement Layer Mask`) to place a decal.
*   Observe how older decals fade out and disappear as new ones are placed (when `Max Active Decals` is reached) or when their `Decal Life Time` expires.
*   Press the **'C' key** to clear all active decals immediately.

---

This complete example provides a robust, performant, and easily configurable DecalSystem following the specified design pattern, ready for use and extension in your Unity projects.