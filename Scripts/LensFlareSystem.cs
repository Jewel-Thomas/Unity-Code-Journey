// Unity Design Pattern Example: LensFlareSystem
// This script demonstrates the LensFlareSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a "LensFlareSystem" design pattern in Unity. While "LensFlareSystem" isn't a formally recognized GoF design pattern, it represents a common architectural pattern for managing a visual effect like lens flares. The core idea is to centralize the control and optimization of multiple instances of a visual effect (flares in this case), decoupling the visual behavior from the source of the effect (the light).

**The Pattern's Components:**

1.  **`LensFlareSystem` (The Manager/Singleton):**
    *   **Purpose:** Acts as a centralized controller for all lens flares in the scene. It's responsible for their creation, positioning, visibility checks (including occlusion), and destruction/pooling.
    *   **Pattern Role:** Singleton (ensures only one instance), Manager, Controller.
    *   **Key Responsibilities:**
        *   Maintain a collection of active and inactive flares.
        *   Update flare visual properties each frame based on camera view and light source.
        *   Perform raycasts for occlusion detection to make flares disappear behind objects.
        *   Optimize performance through object pooling and staggered occlusion checks.
        *   Provide methods for `LensFlareSource` components to register and unregister themselves.

2.  **`LensFlareSource` (The Integrator/Component):**
    *   **Purpose:** This component is attached to individual `Light` GameObjects. It declares that the associated light should have a lens flare and specifies its unique properties (e.g., color, size, fade speed).
    *   **Pattern Role:** Adapter, Component-based Integration.
    *   **Key Responsibilities:**
        *   Hold specific parameters for *its* flare.
        *   Register itself with the `LensFlareSystem` when enabled.
        *   Unregister itself from the `LensFlareSystem` when disabled or destroyed.
        *   Delegates all visual management to the `LensFlareSystem`.

3.  **`ManagedFlare` (Internal Data/Visual Representation):**
    *   **Purpose:** An internal class within `LensFlareSystem` that encapsulates the runtime data and visual GameObject for a single flare. It links a `LensFlareSource` to its dynamically created visual representation.
    *   **Pattern Role:** Data Holder, State Object.
    *   **Key Responsibilities:**
        *   Store references to the `LensFlareSource` and the visual `GameObject` (e.g., a `Quad` with a `SpriteRenderer`).
        *   Manage the `MaterialPropertyBlock` for efficient visual updates.
        *   Hold the current brightness state.
        *   Provide methods to update its visual properties (position, scale, color, alpha) and activate/deactivate its GameObject.

This structure promotes modularity, maintainability, and performance optimization for handling multiple visual effects of the same type.

---

### 1. `LensFlareSystem.cs`

This script manages all lens flares in your scene. It's a Singleton, meaning there's only one instance globally.

```csharp
using UnityEngine;
using System.Collections.Generic;

/*
    HOW TO USE THE LENSFLARESYSTEM:

    1.  SETUP THE LENSFLARESYSTEM MANAGER:
        a.  Create an empty GameObject in your scene (e.g., named "LensFlareManager").
        b.  Attach the `LensFlareSystem.cs` script to it.
        c.  Create a visual prefab for your flares:
            i.   Create a new Material (e.g., "FlareMaterial").
                 - Set its Shader to `Unlit/Transparent` or `Legacy Shaders/Particles/Additive`.
                 - Set Render Mode to `Fade` or `Additive`.
                 - Assign a transparent texture (e.g., a soft white circle with feathered edges) to the Base Map.
            ii.  Create an empty GameObject (e.g., "FlareVisualPrefab").
                 - Add a `SpriteRenderer` component to it.
                 - (Optional but recommended) Assign a default `Sprite` to the `SpriteRenderer` (e.g., a white circle sprite).
                 - Drag your "FlareMaterial" onto the `Material` slot of the `SpriteRenderer`.
                 - Ensure its Layer is set to something appropriate for rendering (e.g., "Default" or a specific "Flares" layer).
                 - Remove any collider components if present.
                 - Set its initial localScale to (1,1,1). The system will adjust its size.
            iii. Drag the "FlareVisualPrefab" GameObject from your Hierarchy into your Project window to create a Prefab.
            iv. Delete the "FlareVisualPrefab" GameObject from your scene. (It's now a prefab).
        d.  In the Inspector of your "LensFlareManager" GameObject, drag your "FlareVisualPrefab" into the "Flare Visual Prefab" slot.
        e.  Optionally, configure the "Occlusion Layers" to specify which layers should block lens flares (e.g., your "Environment" layer).
        f.  Ensure "Game Camera" is assigned (usually Camera.main is found automatically).

    2.  ADD LENS FLARES TO YOUR LIGHTS:
        a.  Select any GameObject in your scene that has a `Light` component (e.g., a Point Light, Directional Light, Spot Light).
        b.  Add the `LensFlareSource.cs` script component to it.
            (The `RequireComponent(typeof(Light))` attribute will ensure a Light component is present).
        c.  In the Inspector for your `LensFlareSource` component:
            i.   Assign a `Sprite` for the `Flare Sprite` (this will override the default sprite in your visual prefab for *this* flare). If left null, the prefab's default sprite will be used.
            ii.  Adjust `Base Size`, `Base Alpha`, `Flare Color`, `Fade Speed`, and `Render Distance` to customize the appearance and behavior of *this* specific flare.

    3.  RUN YOUR SCENE:
        The `LensFlareSystem` will automatically detect and manage all `LensFlareSource` components in the scene.
        Flares will appear when their associated light is on-screen and not occluded, and fade out otherwise.

    TIPS:
    - For realistic occlusion, ensure your `occlusionLayers` in `LensFlareSystem` accurately reflect your scene's geometry.
    - Experiment with different flare textures and shader properties for varied visual effects.
    - If you have many flares, consider optimizing `occlusionChecksPerFrame` for performance.
    - The `DefaultExecutionOrder` attributes ensure the system initializes before its sources.
*/

// Design Pattern: LensFlareSystem (Manager/Singleton)
// This pattern centralizes the management and rendering of all lens flares in the scene.
// It acts as a single point of control, optimizing performance by handling occlusion,
// visibility, and visual updates for all flares from various light sources.
// It decouples the visual representation and complex logic from individual light sources,
// allowing lights to simply declare their desire for a flare via the LensFlareSource component.

[DefaultExecutionOrder(-100)] // Ensures this system updates before most other scripts
public class LensFlareSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    // Provides a global access point to the LensFlareSystem.
    // This ensures there's only one manager coordinating all flares,
    // which is crucial for centralized control and optimization.
    public static LensFlareSystem Instance { get; private set; }

    [Header("Flare Visuals")]
    [Tooltip("Prefab for the visual representation of a single flare (e.g., a Quad with a SpriteRenderer and transparent material).")]
    [SerializeField] private GameObject flareVisualPrefab;

    [Tooltip("The camera used to calculate flare visibility and position. Defaults to Camera.main.")]
    [SerializeField] private Camera gameCamera;

    [Header("Occlusion Settings")]
    [Tooltip("Layers that can block lens flares. Flares will fade out if an object on these layers is between the camera and the light source.")]
    [SerializeField] private LayerMask occlusionLayers = ~0; // All layers by default
    [Tooltip("How many lens flares to raycast for occlusion per frame. Optimizes performance for scenes with many flares.")]
    [SerializeField] private int occlusionChecksPerFrame = 5;

    // --- Internal Data Structures ---
    // A list to hold all active flare instances currently being managed.
    private List<ManagedFlare> activeFlares = new List<ManagedFlare>();
    // A queue for flares that are currently invisible or inactive, ready for reuse (simple object pooling).
    private Queue<ManagedFlare> inactiveFlares = new Queue<ManagedFlare>();

    private int currentOcclusionCheckStartIndex = 0; // To stagger occlusion checks across multiple frames

    // --- ManagedFlare Inner Class ---
    // This internal class encapsulates the data and logic for a single lens flare instance.
    // It links a LensFlareSource (the light wanting a flare) to its visual representation
    // and manages its current state (brightness, position, etc.).
    private class ManagedFlare
    {
        public LensFlareSource source;         // The original component requesting this flare
        public GameObject visualGameObject;     // The actual GameObject displaying the flare
        public SpriteRenderer spriteRenderer;   // Component to render the flare texture
        public MaterialPropertyBlock materialPropertyBlock; // For efficient material property changes without instantiating materials
        public float currentBrightness = 0f;    // Current rendered brightness (0 to 1)

        // Constructor for ManagedFlare. Creates and initializes the visual GameObject.
        public ManagedFlare(GameObject visualPrefab, Transform parent, LensFlareSource flareSource, Sprite customSprite)
        {
            source = flareSource;
            visualGameObject = Instantiate(visualPrefab, parent);
            visualGameObject.name = "Flare Visual - " + source.gameObject.name;
            spriteRenderer = visualGameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"Flare visual prefab '{visualPrefab.name}' is missing a SpriteRenderer!", visualGameObject);
                return;
            }
            if (customSprite != null) // Apply custom sprite from source if provided
            {
                spriteRenderer.sprite = customSprite;
            }
            materialPropertyBlock = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(materialPropertyBlock); // Initialize property block from existing material properties
            visualGameObject.SetActive(false); // Start inactive
        }

        // Updates the visual properties of the flare based on calculated brightness and screen position.
        public void UpdateVisuals(Vector3 screenPos, float calculatedBrightness, Camera cam)
        {
            // Position the flare visual in world space.
            // Convert viewport coordinates (0-1 range for X/Y) to world coordinates at a specified Z-distance from the camera.
            Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(screenPos.x, screenPos.y, source.renderDistance));
            visualGameObject.transform.position = worldPos;

            // Make the flare always face the camera (billboard effect).
            // LookAt makes the Z-axis of the visual point at the camera.
            visualGameObject.transform.LookAt(cam.transform.position, Vector3.up);
            // SpriteRenderers often render "facing away" from the camera if not rotated 180 on Y.
            visualGameObject.transform.Rotate(0, 180, 0);

            // Apply size and color based on brightness and source properties.
            float scale = source.baseSize * calculatedBrightness;
            visualGameObject.transform.localScale = Vector3.one * scale;

            Color targetColor = source.flareColor;
            // The alpha channel of the color is adjusted by the calculated brightness.
            targetColor.a = calculatedBrightness * source.baseAlpha;
            materialPropertyBlock.SetColor("_Color", targetColor);
            spriteRenderer.SetPropertyBlock(materialPropertyBlock); // Apply property block to the renderer
        }

        // Sets the active state of the visual GameObject.
        public void SetVisualActive(bool active)
        {
            visualGameObject.SetActive(active);
        }

        // Resets the flare for reuse in the object pool.
        public void ResetFlare()
        {
            source = null;
            currentBrightness = 0f;
            SetVisualActive(false);
            // Note: spriteRenderer.sprite is reset during RegisterFlareSource for the new flare.
        }
    }

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Enforce Singleton pattern: ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("LensFlareSystem: Multiple instances found! Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally: DontDestroyOnLoad(gameObject); if flares should persist across scenes.
            // For a system tied to a specific scene's lights, it's often better to not make it persistent.
        }

        // Auto-assign Camera.main if not set in inspector.
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError("LensFlareSystem: No main camera found! Please ensure your main camera is tagged 'MainCamera' or assign one manually.", this);
            }
        }

        // Critical check: Ensure the flare visual prefab is assigned and correctly configured.
        if (flareVisualPrefab == null)
        {
            Debug.LogError("LensFlareSystem: Flare Visual Prefab is not assigned! Please assign a Quad/Sprite with a transparent material.", this);
            enabled = false; // Disable the system if critical prefab is missing
        }
        else if (flareVisualPrefab.GetComponent<SpriteRenderer>() == null)
        {
             Debug.LogWarning($"LensFlareSystem: Flare Visual Prefab '{flareVisualPrefab.name}' is missing a SpriteRenderer. Ensure it has one for proper rendering.", flareVisualPrefab);
        }
    }

    private void Update()
    {
        // Skip processing if critical components are missing or no flares are active.
        if (gameCamera == null || flareVisualPrefab == null || activeFlares.Count == 0)
        {
            return;
        }

        ProcessFlares();
    }

    // --- Public Methods for Flare Management ---

    // Design Pattern: Registration
    // LensFlareSource components call this to register themselves with the central system.
    public void RegisterFlareSource(LensFlareSource source)
    {
        if (source == null || source.lightSource == null)
        {
            Debug.LogWarning($"Attempted to register null LensFlareSource or light for {source?.gameObject.name ?? "unknown"}");
            return;
        }

        // Check if this source is already registered to prevent duplicates.
        foreach (var flare in activeFlares)
        {
            if (flare.source == source)
            {
                //Debug.LogWarning($"LensFlareSource '{source.gameObject.name}' already registered.", source);
                return;
            }
        }

        ManagedFlare newFlare;
        if (inactiveFlares.Count > 0)
        {
            // Reuse an inactive flare from the pool
            newFlare = inactiveFlares.Dequeue();
            newFlare.source = source; // Re-assign the source
            // Apply the custom sprite from the source component, or clear if none
            newFlare.spriteRenderer.sprite = source.flareSprite;
            newFlare.spriteRenderer.GetPropertyBlock(newFlare.materialPropertyBlock); // Refresh property block state
        }
        else
        {
            // Create a new flare visual if no inactive ones are available
            newFlare = new ManagedFlare(flareVisualPrefab, transform, source, source.flareSprite);
        }

        activeFlares.Add(newFlare);
        // Debug.Log($"Registered flare for light: {source.gameObject.name}. Total active flares: {activeFlares.Count}");
    }

    // Design Pattern: Deregistration
    // LensFlareSource components call this when they are disabled or destroyed.
    public void UnregisterFlareSource(LensFlareSource source)
    {
        if (source == null) return;

        ManagedFlare flareToRemove = null;
        foreach (var flare in activeFlares)
        {
            if (flare.source == source)
            {
                flareToRemove = flare;
                break;
            }
        }

        if (flareToRemove != null)
        {
            activeFlares.Remove(flareToRemove);
            flareToRemove.ResetFlare(); // Hide the visual and reset its state
            inactiveFlares.Enqueue(flareToRemove); // Add back to pool for future reuse
            // Debug.Log($"Unregistered flare for light: {source.gameObject.name}. Total active flares: {activeFlares.Count}");
        }
    }

    // --- Core Flare Processing Logic ---
    private void ProcessFlares()
    {
        int flaresCount = activeFlares.Count;

        // Iterate through a batch of flares for occlusion checks, then update all visuals.
        // This distributes expensive raycasts over several frames.
        for (int i = 0; i < flaresCount; i++)
        {
            ManagedFlare flare = activeFlares[i];

            // Handle cases where source or light might have been destroyed externally
            if (flare == null || flare.source == null || flare.source.lightSource == null)
            {
                Debug.LogWarning($"Orphaned flare found for {(flare?.source?.gameObject.name ?? "unknown")}. Removing.", this);
                activeFlares.RemoveAt(i);
                i--; // Adjust index due to removal
                flaresCount--;
                if (flare != null) inactiveFlares.Enqueue(flare); // Pool the orphaned flare visual
                continue;
            }

            // 1. Calculate screen position of the light source
            Vector3 lightViewportPoint = gameCamera.WorldToViewportPoint(flare.source.lightSource.transform.position);

            // 2. Check if light is on screen and in front of camera
            bool onScreen = lightViewportPoint.z > 0 &&
                            lightViewportPoint.x >= 0 && lightViewportPoint.x <= 1 &&
                            lightViewportPoint.y >= 0 && lightViewportPoint.y <= 1;

            float targetBrightness = 0f;
            if (onScreen)
            {
                // 3. Perform occlusion check (staggered for performance)
                bool isOccluded = false;
                // Only perform occlusion check if this flare is within the current batch to check.
                if (i >= currentOcclusionCheckStartIndex && i < currentOcclusionCheckStartIndex + occlusionChecksPerFrame)
                {
                    isOccluded = CheckOcclusion(flare.source.lightSource.transform.position);
                }
                else if (i < currentOcclusionCheckStartIndex)
                {
                    // For flares already checked in this cycle, assume their last occlusion state or don't re-check.
                    // For simplicity, we assume if not checked this frame, it's not occluded for this frame,
                    // relying on the fade out to handle eventual occlusion.
                    // A more robust system might cache occlusion status.
                }

                if (!isOccluded)
                {
                    targetBrightness = 1f; // Fully visible if on screen and not occluded
                }
            }

            // 4. Smoothly interpolate brightness towards the target (0 or 1)
            float fadeSpeed = flare.source.fadeSpeed * Time.deltaTime;
            flare.currentBrightness = Mathf.MoveTowards(flare.currentBrightness, targetBrightness, fadeSpeed);

            // 5. Update visual state (activate/deactivate GameObject and update its properties)
            if (flare.currentBrightness > 0.001f) // Only show if sufficiently bright to avoid flickering with low alpha
            {
                if (!flare.visualGameObject.activeSelf)
                {
                    flare.SetVisualActive(true);
                }
                flare.UpdateVisuals(lightViewportPoint, flare.currentBrightness, gameCamera);
            }
            else
            {
                if (flare.visualGameObject.activeSelf)
                {
                    flare.SetVisualActive(false);
                }
            }
        }

        // Advance the start index for the next batch of occlusion checks.
        currentOcclusionCheckStartIndex += occlusionChecksPerFrame;
        if (currentOcclusionCheckStartIndex >= flaresCount)
        {
            currentOcclusionCheckStartIndex = 0; // Loop back to the beginning
        }
    }

    // Checks if the light source is blocked by an object between it and the camera.
    private bool CheckOcclusion(Vector3 lightPosition)
    {
        if (gameCamera == null) return false;

        Vector3 cameraPos = gameCamera.transform.position;
        Vector3 direction = (lightPosition - cameraPos).normalized;
        float distance = Vector3.Distance(cameraPos, lightPosition);

        // Raycast from camera to light source, ignoring triggers.
        return Physics.Raycast(cameraPos, direction, distance, occlusionLayers, QueryTriggerInteraction.Ignore);
    }

    // --- Gizmos for Debugging ---
    private void OnDrawGizmos()
    {
        if (gameCamera == null || activeFlares == null) return;

        foreach (var flare in activeFlares)
        {
            if (flare.source != null && flare.source.lightSource != null && flare.visualGameObject != null && flare.visualGameObject.activeSelf)
            {
                Gizmos.color = Color.yellow;
                // Draw a line from camera to the light source if the flare is active
                Gizmos.DrawLine(gameCamera.transform.position, flare.source.lightSource.transform.position);
                // Draw a small sphere at the light source position
                Gizmos.DrawWireSphere(flare.source.lightSource.transform.position, 0.2f);
            }
        }
    }
}
```

---

### 2. `LensFlareSource.cs`

This script is attached to a `Light` GameObject to indicate that it should have a lens flare and define its specific properties.

```csharp
using UnityEngine;

// Design Pattern: LensFlareSource (Component-based Integration)
// This component acts as an adapter, allowing individual Light GameObjects
// to integrate with the centralized LensFlareSystem. It holds specific
// properties for *this* flare and registers/unregisters itself with the system.
// It delegates the actual rendering and management to the LensFlareSystem,
// following the principle of separation of concerns.

[RequireComponent(typeof(Light))] // Ensures this component is always attached to a GameObject with a Light component
[DefaultExecutionOrder(100)] // Ensures this component initializes AFTER the LensFlareSystem
public class LensFlareSource : MonoBehaviour
{
    [Header("Flare Properties")]
    [Tooltip("The sprite texture to use for this flare. If null, the visual prefab's default sprite is used.")]
    [SerializeField] public Sprite flareSprite;
    [Tooltip("Base size multiplier for the flare visual.")]
    [SerializeField] public float baseSize = 1f;
    [Tooltip("Base alpha (opacity) multiplier for the flare visual.")]
    [Range(0f, 1f)][SerializeField] public float baseAlpha = 1f;
    [Tooltip("Color tint for the flare visual.")]
    [SerializeField] public Color flareColor = Color.white;
    [Tooltip("Speed at which the flare fades in/out.")]
    [SerializeField] public float fadeSpeed = 5f;
    [Tooltip("The distance from the camera at which the flare visual will be rendered in world space. Higher values make it appear further away.")]
    [SerializeField] public float renderDistance = 10f;


    // Reference to the Light component on this GameObject.
    // Automatically assigned by RequireComponent.
    public Light lightSource { get; private set; }

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        if (lightSource == null)
        {
            Debug.LogError("LensFlareSource requires a Light component on the same GameObject.", this);
            enabled = false; // Disable if no light is found
        }
    }

    private void OnEnable()
    {
        // When this component is enabled, register with the global LensFlareSystem.
        // This allows the system to start managing and rendering this light's flare.
        if (LensFlareSystem.Instance != null && lightSource != null)
        {
            LensFlareSystem.Instance.RegisterFlareSource(this);
        }
        else if (lightSource != null)
        {
            // This warning might indicate that LensFlareSystem isn't in the scene,
            // or there's an execution order issue (DefaultExecutionOrder should help).
            Debug.LogWarning("LensFlareSystem not found or LightSource is null. Cannot register flare for " + gameObject.name, this);
        }
    }

    private void OnDisable()
    {
        // When this component is disabled or destroyed, unregister from the system.
        // This cleans up resources and stops the system from trying to manage a non-existent flare.
        if (LensFlareSystem.Instance != null && lightSource != null)
        {
            LensFlareSystem.Instance.UnregisterFlareSource(this);
        }
    }
}
```