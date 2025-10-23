// Unity Design Pattern Example: SurfaceMaterialSystem
// This script demonstrates the SurfaceMaterialSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Surface Material System is a design pattern used in game development, particularly in Unity, to decouple the visual appearance (Unity `Material`) of an object from its gameplay-relevant properties or behavior (its "surface type"). This allows different visual materials to share the same gameplay properties (e.g., several grass textures acting as "Grass" surface) or for objects without a visible material (like an invisible trigger) to still have a defined surface type.

This system is crucial for:
*   **Footstep sounds:** Playing the correct sound based on the ground type (wood, stone, grass).
*   **Projectile impacts:** Spawning appropriate particle effects and sounds when a bullet hits a surface.
*   **Player movement:** Modifying speed or friction based on the surface.
*   **Environmental interactions:** Water slowing down the player, ice being slippery.

## SurfaceMaterialSystem in Unity: A Complete Example

This example provides a robust and practical implementation of the Surface Material System in Unity.

### Core Components:

1.  **`SurfaceType` (Enum):** Defines the distinct categories of surfaces from a gameplay perspective.
2.  **`SurfaceTypeDefinition` (ScriptableObject):** Holds detailed gameplay properties for a specific `SurfaceType` (e.g., sound clips, particle prefabs, friction values).
3.  **`SurfaceMaterialComponent` (MonoBehaviour):** Allows explicit assignment of a `SurfaceTypeDefinition` to any GameObject. This takes precedence over material-based lookups.
4.  **`SurfaceMaterialManager` (Singleton MonoBehaviour):** The central registry and lookup service. It maps Unity `Material`s to `SurfaceTypeDefinition`s and provides methods to query the surface type of a `Renderer`, `Collider`, or `RaycastHit`.
5.  **Example Usage Scripts:** Demonstrate how to query and react to surface types for footstep sounds and projectile impacts.

---

### Step 1: Create the `SurfaceType` Enum

This enum provides a unique identifier for each distinct type of surface you want to differentiate in your game.

**File: `Assets/Scripts/SurfaceSystem/SurfaceType.cs`**

```csharp
using UnityEngine; // Not strictly needed for enum, but common practice in Unity scripts.

/// <summary>
/// Defines the distinct types of surfaces from a gameplay perspective.
/// These are used as identifiers for SurfaceTypeDefinitions.
/// </summary>
public enum SurfaceType
{
    // A default or unknown surface type. Always good to have a fallback.
    Default,
    
    // Example specific surface types
    Grass,
    Stone,
    Wood,
    Metal,
    Water,
    Sand,
    Dirt,
    Concrete,
    Glass
}

```

---

### Step 2: Create the `SurfaceTypeDefinition` ScriptableObject

This is where you define the *properties* associated with each `SurfaceType`. You'll create assets of this type in the Unity editor.

**File: `Assets/Scripts/SurfaceSystem/SurfaceTypeDefinition.cs`**

```csharp
using UnityEngine;

/// <summary>
/// A ScriptableObject that defines the gameplay properties for a specific SurfaceType.
/// This allows you to define reusable bundles of properties (sounds, particles, etc.)
/// that correspond to an abstract surface type like "Grass" or "Stone".
/// </summary>
[CreateAssetMenu(fileName = "SurfaceDef_", menuName = "Surface System/Surface Type Definition", order = 1)]
public class SurfaceTypeDefinition : ScriptableObject
{
    [Tooltip("The unique identifier for this surface type.")]
    public SurfaceType surfaceID = SurfaceType.Default;

    [Header("Audio Properties")]
    [Tooltip("AudioClip to play for footsteps on this surface.")]
    public AudioClip footstepSound;
    [Tooltip("AudioClip to play for projectile impacts on this surface.")]
    public AudioClip impactSound;
    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for sounds played on this surface.")]
    public float soundVolume = 1.0f;

    [Header("Visual Properties")]
    [Tooltip("Prefab for impact particles (e.g., dust, sparks) on this surface.")]
    public GameObject impactEffectPrefab;

    [Header("Physics Properties")]
    [Tooltip("Friction modifier for characters moving on this surface. (e.g., <1 for slippery, >1 for sticky)")]
    public float frictionModifier = 1.0f;
    [Tooltip("Multiplier for projectile bounce on this surface.")]
    public float bounceMultiplier = 0.5f;

    // You can add many more properties here as needed for your game:
    // public float damageMultiplier = 1.0f;
    // public bool isWater = false;
    // public Color bloodColor = Color.red;
    // public Material decalMaterial;
    // ... and so on.

    /// <summary>
    /// Returns the name of the ScriptableObject asset, which is useful for debugging.
    /// </summary>
    public string GetName()
    {
        return name;
    }
}
```

---

### Step 3: Create the `SurfaceMaterialComponent` MonoBehaviour

This component allows you to explicitly assign a `SurfaceTypeDefinition` to a GameObject, overriding any material-based lookup for that specific object. Useful for complex objects, terrains, or when the visual material doesn't perfectly match the gameplay surface.

**File: `Assets/Scripts/SurfaceSystem/SurfaceMaterialComponent.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Component that explicitly assigns a SurfaceTypeDefinition to a GameObject.
/// When queried, this component's definition takes precedence over any material-based lookup.
/// Useful for terrains, complex objects, or when the visual material doesn't match the
/// desired gameplay surface type.
/// </summary>
[AddComponentMenu("Surface System/Surface Material Component")]
public class SurfaceMaterialComponent : MonoBehaviour
{
    [Tooltip("The SurfaceTypeDefinition to assign to this GameObject.")]
    public SurfaceTypeDefinition surfaceDefinition;

    // Optional: Draw a small gizmo in the editor to indicate this component is present
    void OnDrawGizmos()
    {
        if (surfaceDefinition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawIcon(transform.position + Vector3.up * 0.5f, "UnityEditor.SceneView@2x", true);
        }
    }
}
```

---

### Step 4: Create the `SurfaceMaterialManager` Singleton

This is the central brain of the system. It's a singleton that maintains mappings from Unity `Material`s to `SurfaceTypeDefinition`s and provides methods to query surface types from various Unity objects.

**File: `Assets/Scripts/SurfaceSystem/SurfaceMaterialManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The central manager for the Surface Material System.
/// This singleton class provides methods to query the SurfaceTypeDefinition
/// for a given Material, Renderer, Collider, or RaycastHit.
/// It maintains a mapping of Unity Materials to SurfaceTypeDefinitions.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensures this runs before other scripts that might query it.
[AddComponentMenu("Surface System/Surface Material Manager")]
public class SurfaceMaterialManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the SurfaceMaterialManager.
    /// </summary>
    public static SurfaceMaterialManager Instance { get; private set; }

    [Header("Default Settings")]
    [Tooltip("The SurfaceTypeDefinition to use if no specific mapping is found.")]
    public SurfaceTypeDefinition defaultSurfaceType;

    [Header("Initial Material Mappings")]
    [Tooltip("List of explicit Material-to-SurfaceTypeDefinition mappings.")]
    public List<MaterialSurfaceMapping> initialMappings = new List<MaterialSurfaceMapping>();

    /// <summary>
    /// Private dictionary to store the runtime mapping of Materials to SurfaceTypeDefinitions.
    /// </summary>
    private Dictionary<Material, SurfaceTypeDefinition> _materialMap = new Dictionary<Material, SurfaceTypeDefinition>();

    /// <summary>
    /// Helper struct for inspector-friendly material mappings.
    /// </summary>
    [System.Serializable]
    public struct MaterialSurfaceMapping
    {
        public Material material;
        public SurfaceTypeDefinition surfaceType;
    }

    void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SurfaceMaterialManagers found. Destroying duplicate.", this);
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (defaultSurfaceType == null)
        {
            Debug.LogError("Default Surface Type is not assigned in SurfaceMaterialManager!", this);
            // Attempt to create a basic default if none is assigned to prevent null refs.
            defaultSurfaceType = ScriptableObject.CreateInstance<SurfaceTypeDefinition>();
            defaultSurfaceType.name = "AutoGeneratedDefaultSurface";
            defaultSurfaceType.surfaceID = SurfaceType.Default;
        }

        // Populate the material map from inspector-defined initial mappings.
        PopulateMaterialMap();
    }

    /// <summary>
    /// Populates the internal material map from the initialMappings list.
    /// This should typically be called once at startup.
    /// </summary>
    private void PopulateMaterialMap()
    {
        _materialMap.Clear();
        foreach (var mapping in initialMappings)
        {
            if (mapping.material != null && mapping.surfaceType != null)
            {
                if (!_materialMap.ContainsKey(mapping.material))
                {
                    _materialMap.Add(mapping.material, mapping.surfaceType);
                }
                else
                {
                    Debug.LogWarning($"Duplicate material mapping found for '{mapping.material.name}'. Using the first one defined.", mapping.material);
                }
            }
            else
            {
                Debug.LogWarning("Skipping null material or surface type in initial mappings.", this);
            }
        }
    }

    /// <summary>
    /// Registers a new material-to-surface type mapping at runtime.
    /// Useful for dynamically created materials.
    /// </summary>
    /// <param name="material">The Unity Material to register.</param>
    /// <param name="surfaceDef">The SurfaceTypeDefinition associated with this material.</param>
    public void RegisterMaterial(Material material, SurfaceTypeDefinition surfaceDef)
    {
        if (material == null || surfaceDef == null)
        {
            Debug.LogWarning("Cannot register null material or surface definition.", this);
            return;
        }

        if (_materialMap.ContainsKey(material))
        {
            _materialMap[material] = surfaceDef; // Update existing mapping
        }
        else
        {
            _materialMap.Add(material, surfaceDef); // Add new mapping
        }
    }

    /// <summary>
    /// Unregisters a material mapping.
    /// </summary>
    /// <param name="material">The Material to unregister.</param>
    public void UnregisterMaterial(Material material)
    {
        if (material != null && _materialMap.ContainsKey(material))
        {
            _materialMap.Remove(material);
        }
    }

    /// <summary>
    /// Gets the SurfaceTypeDefinition associated with a given Material.
    /// </summary>
    /// <param name="material">The Material to look up.</param>
    /// <returns>The corresponding SurfaceTypeDefinition, or the defaultSurfaceType if not found.</returns>
    public SurfaceTypeDefinition GetSurfaceType(Material material)
    {
        if (material == null) return defaultSurfaceType;

        if (_materialMap.TryGetValue(material, out SurfaceTypeDefinition surfaceDef))
        {
            return surfaceDef;
        }
        return defaultSurfaceType;
    }

    /// <summary>
    /// Gets the SurfaceTypeDefinition for a given Renderer.
    /// Prioritizes SurfaceMaterialComponent, then checks the renderer's sharedMaterial.
    /// </summary>
    /// <param name="renderer">The Renderer to query.</param>
    /// <returns>The corresponding SurfaceTypeDefinition, or the defaultSurfaceType if not found.</returns>
    public SurfaceTypeDefinition GetSurfaceType(Renderer renderer)
    {
        if (renderer == null) return defaultSurfaceType;

        // 1. Check for an explicit SurfaceMaterialComponent on the GameObject
        SurfaceMaterialComponent surfaceComp = renderer.GetComponent<SurfaceMaterialComponent>();
        if (surfaceComp != null && surfaceComp.surfaceDefinition != null)
        {
            return surfaceComp.surfaceDefinition;
        }

        // 2. Fallback to material-based lookup
        // Using sharedMaterial for performance, as material creates a new instance.
        if (renderer.sharedMaterial != null)
        {
            return GetSurfaceType(renderer.sharedMaterial);
        }

        // 3. Fallback to default
        return defaultSurfaceType;
    }

    /// <summary>
    /// Gets the SurfaceTypeDefinition for a given Collider.
    /// This will attempt to find a Renderer on the same GameObject first.
    /// </summary>
    /// <param name="collider">The Collider to query.</param>
    /// <returns>The corresponding SurfaceTypeDefinition, or the defaultSurfaceType if not found.</returns>
    public SurfaceTypeDefinition GetSurfaceType(Collider collider)
    {
        if (collider == null) return defaultSurfaceType;

        // Try to get the renderer from the collider's GameObject
        Renderer renderer = collider.GetComponent<Renderer>();
        if (renderer != null)
        {
            return GetSurfaceType(renderer);
        }

        // If no renderer, check for SurfaceMaterialComponent directly on the collider's GameObject
        SurfaceMaterialComponent surfaceComp = collider.GetComponent<SurfaceMaterialComponent>();
        if (surfaceComp != null && surfaceComp.surfaceDefinition != null)
        {
            return surfaceComp.surfaceDefinition;
        }

        // Fallback to default
        return defaultSurfaceType;
    }

    /// <summary>
    /// Gets the SurfaceTypeDefinition for a given RaycastHit.
    /// This is the most common way to query surface types in gameplay.
    /// </summary>
    /// <param name="hit">The RaycastHit information.</param>
    /// <returns>The corresponding SurfaceTypeDefinition, or the defaultSurfaceType if not found.</returns>
    public SurfaceTypeDefinition GetSurfaceType(RaycastHit hit)
    {
        if (hit.collider == null) return defaultSurfaceType;

        return GetSurfaceType(hit.collider);
    }
}
```

---

### Step 5: Example Usage Scripts

These scripts demonstrate how other parts of your game would interact with the `SurfaceMaterialManager`.

#### Example 5.1: `PlayerFootsteps` Script

This script simulates a player character playing footstep sounds based on the surface they are walking on.

**File: `Assets/Scripts/SurfaceSystem/Examples/PlayerFootsteps.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the SurfaceMaterialSystem for footstep sounds.
/// Attach this to your player character or a character controller.
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
    [Tooltip("The AudioSource component to play footstep sounds.")]
    public AudioSource footstepAudioSource;
    
    [Tooltip("LayerMask to use for ground detection (e.g., 'Ground').")]
    public LayerMask groundLayer;

    [Tooltip("Distance below the player's pivot to check for ground.")]
    public float groundCheckDistance = 0.2f;

    [Tooltip("Minimum time between footsteps.")]
    public float footstepDelay = 0.4f;

    private float _lastFootstepTime;
    private SurfaceTypeDefinition _currentSurface;

    void Awake()
    {
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
            footstepAudioSource.spatialBlend = 1.0f; // 3D sound
            footstepAudioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Simple movement check (replace with your actual character movement logic)
        // If the player is moving and it's time for a footstep...
        if (IsMoving() && Time.time > _lastFootstepTime + footstepDelay)
        {
            CheckAndPlayFootstep();
            _lastFootstepTime = Time.time;
        }
    }

    private bool IsMoving()
    {
        // Example: Check if character controller or rigidbody is moving.
        // For demonstration, let's just assume we are moving if any input is pressed.
        // In a real game, you'd check character velocity.
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }

    private void CheckAndPlayFootstep()
    {
        RaycastHit hit;
        // Raycast down from the player to detect the surface.
        // Adjust origin and direction based on your character's setup.
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            // Query the SurfaceMaterialManager for the SurfaceTypeDefinition at the hit point.
            _currentSurface = SurfaceMaterialManager.Instance.GetSurfaceType(hit);

            if (_currentSurface != null && _currentSurface.footstepSound != null)
            {
                footstepAudioSource.PlayOneShot(_currentSurface.footstepSound, _currentSurface.soundVolume);
                // Debug.Log($"Walking on {_currentSurface.surfaceID}: Playing {_currentSurface.footstepSound.name}");
            }
            else
            {
                // Debug.Log($"Walking on default/unknown surface: {_currentSurface?.surfaceID}");
            }
        }
        else
        {
            // Debug.Log("Not on ground.");
            _currentSurface = SurfaceMaterialManager.Instance.defaultSurfaceType; // Assume default if not on ground
        }
    }

    // Optional: Visualize the ground check in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance);
        if (_currentSurface != null)
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (groundCheckDistance - 0.05f), 0.1f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Surface: {_currentSurface.surfaceID}");
        }
    }
}

```

#### Example 5.2: `ProjectileImpact` Script

This script simulates a projectile hitting a surface and spawning appropriate effects and sounds.

**File: `Assets/Scripts/SurfaceSystem/Examples/ProjectileImpact.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the SurfaceMaterialSystem for projectile impacts.
/// Attach this to a projectile prefab that should handle collision.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectileImpact : MonoBehaviour
{
    [Tooltip("Prefab to spawn when the projectile hits something.")]
    public GameObject projectileImpactEffect;
    [Tooltip("How long after impact the projectile should self-destruct.")]
    public float destroyDelay = 0.1f;
    [Tooltip("Initial speed of the projectile.")]
    public float initialSpeed = 20f;

    private Rigidbody _rb;
    private bool _hasImpacted = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false; // For a simple projectile
    }

    void Start()
    {
        // Give it an initial push
        _rb.velocity = transform.forward * initialSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hasImpacted) return;

        _hasImpacted = true;

        // Get the SurfaceTypeDefinition at the point of impact.
        // We can use the contact point or directly query the collider.
        SurfaceTypeDefinition surface = SurfaceMaterialManager.Instance.GetSurfaceType(collision.collider);

        if (surface != null)
        {
            Debug.Log($"Projectile hit {collision.gameObject.name} (Surface: {surface.surfaceID})");

            // Play impact sound
            if (surface.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(surface.impactSound, collision.contacts[0].point, surface.soundVolume);
            }

            // Spawn impact effect
            if (surface.impactEffectPrefab != null)
            {
                // Instantiate the effect prefab at the collision point, rotated to face away from the surface.
                Quaternion hitRotation = Quaternion.LookRotation(collision.contacts[0].normal);
                Instantiate(surface.impactEffectPrefab, collision.contacts[0].point, hitRotation);
            }

            // Apply friction/bounce effects (example: adjust collider properties, or apply force)
            // For example, if you wanted the projectile to bounce more on certain surfaces:
            // _rb.velocity = Vector3.Reflect(transform.forward * initialSpeed, collision.contacts[0].normal) * surface.bounceMultiplier;
        }
        else
        {
            Debug.Log("Projectile hit something with an unknown surface type.");
        }

        // Destroy the projectile after a short delay
        Destroy(gameObject, destroyDelay);
    }
}
```

---

### How to Use in Unity Editor:

1.  **Create Folders:**
    *   `Assets/Scripts/SurfaceSystem`
    *   `Assets/Scripts/SurfaceSystem/Examples`
    *   `Assets/SurfaceDefinitions` (or `Assets/Data/SurfaceDefinitions`)
    *   `Assets/Materials`
    *   `Assets/Prefabs/Effects`
    *   `Assets/Audio`

2.  **Place Scripts:** Drop the C# files into their respective folders.

3.  **Create `SurfaceMaterialManager` GameObject:**
    *   Create an empty GameObject in your scene (e.g., `_Managers/SurfaceMaterialManager`).
    *   Add the `SurfaceMaterialManager.cs` component to it.

4.  **Create `SurfaceTypeDefinition` Assets:**
    *   Go to `Assets/SurfaceDefinitions` folder.
    *   Right-click -> Create -> Surface System -> Surface Type Definition.
    *   Create several, e.g., `SurfaceDef_Grass`, `SurfaceDef_Stone`, `SurfaceDef_Wood`, `SurfaceDef_Default`.
    *   For each, select its `surfaceID` from the dropdown (e.g., `Grass` for `SurfaceDef_Grass`).
    *   **Assign Audio/Prefabs:** Drag appropriate `AudioClip`s (you'll need to import or create some) and `GameObject` prefabs (e.g., simple particle systems for impacts) into their respective fields. For `SurfaceDef_Default`, make sure it has the `SurfaceType.Default` ID and default assets.

5.  **Configure `SurfaceMaterialManager` in Scene:**
    *   Select the `SurfaceMaterialManager` GameObject.
    *   Drag `SurfaceDef_Default` into the `Default Surface Type` slot.
    *   **Add Material Mappings:**
        *   In the `Initial Material Mappings` list, add new elements.
        *   For each element, drag a Unity `Material` from your project (e.g., a "Grass_Material") into the `Material` slot.
        *   Then, drag the corresponding `SurfaceTypeDefinition` (e.g., `SurfaceDef_Grass`) into the `Surface Type` slot.
        *   Repeat for other materials (Stone, Wood, etc.).

6.  **Setup Scene Objects:**
    *   **Ground/World Objects:**
        *   For a simple cube: Create a Cube. Assign a material (e.g., `Grass_Material`) to its `MeshRenderer`. This material should be mapped in `SurfaceMaterialManager`.
        *   For a terrain: You can map your terrain layers' materials, or, if a specific section of terrain needs a distinct `SurfaceType` regardless of its texture, add a `SurfaceMaterialComponent` to the terrain GameObject itself and assign a `SurfaceTypeDefinition`.
    *   **Player Character:**
        *   Create a simple cube for a player.
        *   Add a `Rigidbody` (optional, but good for physics-based movement).
        *   Add an `AudioSource` component.
        *   Add the `PlayerFootsteps.cs` script. Drag its `AudioSource` to the `Footstep Audio Source` slot. Set `Ground Layer` to something appropriate (e.g., "Default" or a custom "Ground" layer for your environment).
    *   **Projectile:**
        *   Create a small sphere (or another shape).
        *   Add a `Rigidbody` (set gravity to false if it's a fast-moving projectile).
        *   Add the `ProjectileImpact.cs` script.
        *   Create a simple particle system for a generic impact effect and assign it to `Projectile Impact Effect`.
        *   Make this sphere a Prefab.

7.  **Test:**
    *   Run the scene.
    *   Move your "player" cube around. You should hear different footstep sounds depending on the material it's walking on (if mapped in the manager) or if it has an explicit `SurfaceMaterialComponent`.
    *   You can create a simple script to spawn the `Projectile` prefab on mouse click to test impacts.

---

### Benefits of this Pattern:

*   **Decoupling:** Gameplay logic (footstep sounds, impact effects) is separated from visual representation (Unity Materials).
*   **Flexibility:**
    *   Multiple visual materials can map to the same gameplay `SurfaceType` (e.g., "Rough Grass" and "Smooth Grass" materials both map to `SurfaceType.Grass`).
    *   An object's `SurfaceType` can be explicitly defined (`SurfaceMaterialComponent`) regardless of its visual material, useful for invisible triggers or complex layered objects.
    *   New `SurfaceTypeDefinition` assets can be created easily without modifying code.
*   **Maintainability:** Changes to gameplay properties for a surface (e.g., changing the footstep sound for "Stone") only require editing one `SurfaceTypeDefinition` asset, not every material or object using that surface.
*   **Performance:** The `SurfaceMaterialManager` uses a `Dictionary` for fast lookups.
*   **Extensibility:** Easily add new properties to `SurfaceTypeDefinition` (e.g., `isSlippery`, `damageReduction`, `decalMaterial`) without breaking existing code.

This `SurfaceMaterialSystem` provides a robust, scalable, and easy-to-manage solution for handling environment interactions in your Unity projects.