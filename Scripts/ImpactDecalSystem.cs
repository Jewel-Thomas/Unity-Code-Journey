// Unity Design Pattern Example: ImpactDecalSystem
// This script demonstrates the ImpactDecalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Impact Decal System is a design pattern used in games to efficiently manage visual effects like bullet holes, scorch marks, or blood splatters that appear on surfaces after an impact.

**Why use the Impact Decal System?**

1.  **Decoupling:** The entity causing the impact (e.g., a bullet, an explosion) doesn't need to know how to create or manage a decal. It simply reports an impact event (position, normal, type).
2.  **Performance:** Decals are often numerous and short-lived. This system uses object pooling to recycle decals instead of constantly instantiating and destroying them, which reduces garbage collection and CPU overhead.
3.  **Centralized Management:** All decals are managed by a single system, making it easier to control their lifecycle, fade-out effects, and overall count.
4.  **Flexibility:** Easily add new decal types, modify their appearance, or change their behavior without altering game logic that causes impacts.

**Pattern Breakdown:**

*   **`ImpactDecalManager` (Singleton):** The core of the system. It's a singleton to provide global access. It holds pre-created pools of different decal types and handles requests to spawn new decals, retrieving them from a pool, activating them, and returning them when their lifetime expires.
*   **`DecalType` (Enum):** Defines the different categories of decals (e.g., BulletHole, BloodSplatter, ExplosionScorch).
*   **`DecalController` (Component):** A script attached to each decal prefab. It's responsible for managing its own individual state (position, rotation, scale) and its lifecycle (e.g., fading out, returning to the pool after a set time).
*   **`ObjectPool<T>` (Generic Utility):** A reusable component for pooling any type of `MonoBehaviour`. The `ImpactDecalManager` uses this internally for each decal type.
*   **Impact Source (Example: `ImpactGenerator`):** Any script that needs to create a decal (e.g., a bullet, a melee weapon, an explosion). It calls the `ImpactDecalManager.Instance.SpawnDecal()` method without needing to know the implementation details.

---

### **1. `ImpactDecalManager.cs`**

This is the central service that manages all decal pools and handles requests to spawn decals.

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace GameSystems.Decals
{
    /// <summary>
    /// Defines the different types of decals that can be spawned.
    /// This enum can be extended to include more specific decal types.
    /// </summary>
    public enum DecalType
    {
        None = 0,
        BulletHole,
        BloodSplatter,
        ExplosionScorch,
        ArrowImpact,
        // Add more decal types as needed
    }

    /// <summary>
    /// Settings for a specific decal type's object pool.
    /// Used by the ImpactDecalManager to set up its pools in the inspector.
    /// </summary>
    [System.Serializable]
    public class DecalPoolSettings
    {
        public DecalType decalType;          // The type of decal this setting applies to
        public DecalController decalPrefab;  // The prefab to use for this decal type
        public int initialPoolSize = 10;     // How many decals to pre-instantiate
        public float defaultLifetime = 5f;   // Default duration before decal fades/disappears
    }

    /// <summary>
    /// The core singleton manager for all impact decals.
    /// It provides a centralized point to spawn decals and uses object pooling
    /// for efficient management and performance.
    /// </summary>
    public class ImpactDecalManager : MonoBehaviour
    {
        // Singleton instance for global access
        public static ImpactDecalManager Instance { get; private set; }

        [Header("Decal Pool Settings")]
        [Tooltip("Configure pools for different decal types.")]
        public List<DecalPoolSettings> decalPoolSettings = new List<DecalPoolSettings>();

        private Dictionary<DecalType, ObjectPool<DecalController>> _decalPools;

        private void Awake()
        {
            // Enforce singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple ImpactDecalManager instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        /// <summary>
        /// Initializes all decal object pools based on the inspector settings.
        /// </summary>
        private void InitializePools()
        {
            _decalPools = new Dictionary<DecalType, ObjectPool<DecalController>>();

            foreach (var settings in decalPoolSettings)
            {
                if (settings.decalType == DecalType.None || settings.decalPrefab == null)
                {
                    Debug.LogWarning($"ImpactDecalManager: DecalPoolSettings for type '{settings.decalType}' is incomplete. Skipping.");
                    continue;
                }

                if (_decalPools.ContainsKey(settings.decalType))
                {
                    Debug.LogWarning($"ImpactDecalManager: Duplicate DecalType '{settings.decalType}' found in settings. Only the first will be used.");
                    continue;
                }

                // Create a new ObjectPool for each decal type
                var pool = new ObjectPool<DecalController>(
                    settings.decalPrefab,
                    settings.initialPoolSize,
                    transform // Parent the pooled objects under the manager for organization
                );
                _decalPools.Add(settings.decalType, pool);

                Debug.Log($"ImpactDecalManager: Initialized pool for '{settings.decalType}' with size {settings.initialPoolSize}.");
            }
        }

        /// <summary>
        /// Spawns a decal at a given position and rotation.
        /// This is the primary method for other game systems to request a decal.
        /// </summary>
        /// <param name="type">The type of decal to spawn (e.g., BulletHole, BloodSplatter).</param>
        /// <param name="position">The world position where the decal should appear.</param>
        /// <param name="normal">The surface normal at the impact point, used for decal orientation.</param>
        /// <param name="parent">Optional parent transform for the decal (e.g., the hit object).</param>
        /// <param name="scale">Optional scale multiplier for the decal.</param>
        /// <param name="customLifetime">Optional custom lifetime, overrides the default pool setting.</param>
        public void SpawnDecal(DecalType type, Vector3 position, Vector3 normal,
                               Transform parent = null, float scale = 1f, float? customLifetime = null)
        {
            if (!_decalPools.TryGetValue(type, out var pool))
            {
                Debug.LogWarning($"ImpactDecalManager: No pool configured for DecalType '{type}'. Cannot spawn decal.");
                return;
            }

            // Get a decal from the pool
            DecalController decal = pool.Get();

            if (decal == null)
            {
                Debug.LogError($"ImpactDecalManager: Failed to get a decal from pool for type '{type}'. Pool might be exhausted or prefab is missing.");
                return;
            }

            // Set decal's properties
            decal.transform.position = position;

            // Orient the decal to face away from the surface normal
            // Adding a random rotation around the normal for visual variety
            Quaternion surfaceRotation = Quaternion.LookRotation(normal);
            Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), normal);
            decal.transform.rotation = surfaceRotation * randomRotation;
            
            decal.transform.localScale = Vector3.one * scale;
            decal.transform.SetParent(parent); // Parent it to the hit object if provided

            // Determine lifetime
            float actualLifetime = customLifetime ?? GetDefaultLifetimeForType(type);

            // Initialize and activate the decal
            decal.Initialize(type, actualLifetime, this);
            decal.gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns a DecalController instance to its appropriate pool.
        /// This method is called by the DecalController when its lifetime expires.
        /// </summary>
        /// <param name="decal">The decal to return to the pool.</param>
        public void ReturnDecalToPool(DecalController decal)
        {
            if (decal == null || !_decalPools.TryGetValue(decal.DecalType, out var pool))
            {
                Debug.LogWarning($"ImpactDecalManager: Attempted to return an invalid or unmanaged decal to pool.");
                return;
            }
            
            // Deactivate and return to pool
            decal.gameObject.SetActive(false);
            decal.transform.SetParent(this.transform); // Re-parent back to manager for organization
            pool.Return(decal);
        }

        /// <summary>
        /// Helper to get the default lifetime configured for a specific decal type.
        /// </summary>
        private float GetDefaultLifetimeForType(DecalType type)
        {
            foreach (var settings in decalPoolSettings)
            {
                if (settings.decalType == type)
                {
                    return settings.defaultLifetime;
                }
            }
            return 5f; // Fallback default lifetime
        }
    }
}
```

---

### **2. `DecalController.cs`**

This script is attached to each decal prefab and manages its individual behavior, such as lifetime and fading.

```csharp
using UnityEngine;
using System.Collections;

namespace GameSystems.Decals
{
    /// <summary>
    /// Base class for a decal component that manages its own lifetime and interacts with the ImpactDecalManager.
    /// This is attached to decal prefabs.
    /// </summary>
    public class DecalController : MonoBehaviour
    {
        [Tooltip("Assign the type of this decal prefab. Used by the manager.")]
        [SerializeField] private DecalType _decalType;
        public DecalType DecalType => _decalType;

        [Tooltip("Optional: Renderer for the decal, used for fading effects.")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Fading Settings (Optional)")]
        [Tooltip("If true, the decal will fade out before disappearing.")]
        [SerializeField] private bool _enableFade = true;
        [Tooltip("The duration of the fade-out effect.")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private ImpactDecalManager _manager;
        private Coroutine _lifetimeRoutine;
        private Material _fadeMaterialInstance; // To prevent modifying the shared material asset

        private void Awake()
        {
            // Get renderer if not assigned. Prioritize MeshRenderer then SpriteRenderer.
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_spriteRenderer == null && _meshRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_enableFade && (_meshRenderer != null || _spriteRenderer != null))
            {
                // Create a material instance to avoid modifying the shared asset
                // This is crucial if multiple decals use the same material
                if (_meshRenderer != null)
                {
                    _fadeMaterialInstance = _meshRenderer.material; // Gets an instance
                }
                else if (_spriteRenderer != null)
                {                    
                    _fadeMaterialInstance = _spriteRenderer.material; // Gets an instance
                }

                // Ensure the material supports fading (e.g., render mode is Fade in URP/HDRP or a custom shader)
                if (_fadeMaterialInstance != null && !_fadeMaterialInstance.HasProperty("_Color"))
                {
                    Debug.LogWarning($"DecalController on {name}: Material {_fadeMaterialInstance.name} does not have a '_Color' property for fading. Fading disabled.", this);
                    _enableFade = false;
                }
            } else if (_enableFade) {
                 Debug.LogWarning($"DecalController on {name}: No MeshRenderer or SpriteRenderer found for fading. Fading disabled.", this);
                 _enableFade = false;
            }
        }

        /// <summary>
        /// Initializes the decal with its type, lifetime, and a reference to the manager.
        /// This is called by the ImpactDecalManager when a decal is spawned from the pool.
        /// </summary>
        /// <param name="decalType">The type of this decal.</param>
        /// <param name="lifetime">How long the decal should remain visible.</param>
        /// <param name="manager">Reference to the ImpactDecalManager to return itself to the pool.</param>
        public void Initialize(DecalType decalType, float lifetime, ImpactDecalManager manager)
        {
            _decalType = decalType; // Ensure the type is correctly set when initialized
            _manager = manager;

            // Stop any existing lifetime routine to prevent conflicts
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
            }

            // Ensure decal is fully opaque at the start if fading is enabled
            if (_enableFade && _fadeMaterialInstance != null)
            {
                Color currentColor = _fadeMaterialInstance.color;
                _fadeMaterialInstance.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            }

            // Start the new lifetime routine
            _lifetimeRoutine = StartCoroutine(LifetimeRoutine(lifetime));
        }

        /// <summary>
        /// Coroutine that manages the decal's lifetime, including optional fading.
        /// </summary>
        /// <param name="lifetime">Total duration for the decal to be active.</param>
        private IEnumerator LifetimeRoutine(float lifetime)
        {
            yield return new WaitForSeconds(lifetime - (_enableFade ? _fadeDuration : 0f));

            if (_enableFade)
            {
                yield return FadeOutRoutine();
            }

            // Once lifetime is over (and fade completed), return to pool
            _manager.ReturnDecalToPool(this);
        }

        /// <summary>
        /// Coroutine to fade out the decal's alpha over time.
        /// Requires the decal's material to support transparency via _Color property.
        /// </summary>
        private IEnumerator FadeOutRoutine()
        {
            if (_fadeMaterialInstance == null) yield break; // No material to fade

            float timer = 0f;
            Color startColor = _fadeMaterialInstance.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / _fadeDuration;
                _fadeMaterialInstance.color = Color.Lerp(startColor, endColor, progress);
                yield return null;
            }

            _fadeMaterialInstance.color = endColor; // Ensure it's fully transparent
        }

        // When the decal is disabled (e.g., returned to pool), stop its routines
        private void OnDisable()
        {
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
                _lifetimeRoutine = null;
            }
            // Reset parent to null or manager's transform when returning to pool if it was parented to another object
            // This is handled by ImpactDecalManager.ReturnDecalToPool, but good to keep in mind.
        }

        // Clean up the instantiated material on destruction
        private void OnDestroy()
        {
            if (_fadeMaterialInstance != null && _fadeMaterialInstance != (_meshRenderer?.sharedMaterial ?? _spriteRenderer?.sharedMaterial))
            {
                Destroy(_fadeMaterialInstance);
            }
        }
    }
}
```

---

### **3. `ObjectPool.cs`**

A generic object pooling utility used by the `ImpactDecalManager`.

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace GameSystems.Decals
{
    /// <summary>
    /// A generic object pool for MonoBehaviour instances.
    /// Useful for reusing game objects instead of constantly instantiating and destroying them.
    /// </summary>
    /// <typeparam name="T">The type of MonoBehaviour to pool.</typeparam>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private T _prefab;
        private Stack<T> _pool;
        private Transform _parentTransform;

        /// <summary>
        /// Constructor for the ObjectPool.
        /// </summary>
        /// <param name="prefab">The prefab to use for creating new instances.</param>
        /// <param name="initialSize">The number of instances to pre-populate the pool with.</param>
        /// <param name="parentTransform">The transform to parent pooled objects under (for organization).</param>
        public ObjectPool(T prefab, int initialSize, Transform parentTransform)
        {
            _prefab = prefab;
            _parentTransform = parentTransform;
            _pool = new Stack<T>(initialSize);

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNewInstance();
                obj.gameObject.SetActive(false); // Start inactive
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Creates a new instance of the prefab, parenting it to the designated transform.
        /// </summary>
        /// <returns>A new instance of the pooled type.</returns>
        private T CreateNewInstance()
        {
            T newObj = GameObject.Instantiate(_prefab, _parentTransform);
            newObj.name = _prefab.name + " (Pooled)";
            return newObj;
        }

        /// <summary>
        /// Retrieves an object from the pool. If the pool is empty, a new instance is created.
        /// </summary>
        /// <returns>An active instance of the pooled type.</returns>
        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                // Pool is empty, create a new one (expands pool dynamically)
                Debug.LogWarning($"ObjectPool: Pool for '{_prefab.name}' exhausted. Creating new instance.");
                obj = CreateNewInstance();
            }
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool, deactivating it.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        public void Return(T obj)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Returns the current number of available (inactive) objects in the pool.
        /// </summary>
        public int Count => _pool.Count;
    }
}
```

---

### **4. `ImpactGenerator.cs` (Example Usage)**

This script simulates an impact (like a weapon firing) and demonstrates how to use the `ImpactDecalManager`.

```csharp
using UnityEngine;
using GameSystems.Decals; // Make sure to include the namespace for your decal system

/// <summary>
/// Example script demonstrating how to use the ImpactDecalManager.
/// Simulates firing a raycast and spawning a decal at the hit point.
/// </summary>
public class ImpactGenerator : MonoBehaviour
{
    [Header("Impact Settings")]
    [Tooltip("The type of decal to spawn on impact.")]
    public DecalType decalTypeToSpawn = DecalType.BulletHole;
    [Tooltip("The max distance for the raycast.")]
    public float maxImpactDistance = 100f;
    [Tooltip("The scale of the spawned decal.")]
    public float decalScale = 1f;
    [Tooltip("Optional: Custom lifetime for the decal, overrides pool setting.")]
    public float? customDecalLifetime = null; // Set to a value (e.g., 2f) to enable custom lifetime

    [Header("Visuals")]
    [Tooltip("Optional: Particle system to play on impact.")]
    public GameObject impactEffectPrefab;

    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("ImpactGenerator requires a main camera in the scene!");
            enabled = false;
        }
    }

    void Update()
    {
        // Simulate a "fire" action (e.g., left mouse button click)
        if (Input.GetMouseButtonDown(0))
        {
            SimulateImpact();
        }
    }

    /// <summary>
    /// Performs a raycast from the camera/center of screen and processes the impact.
    /// </summary>
    void SimulateImpact()
    {
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Ray from center of screen
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxImpactDistance))
        {
            Debug.Log($"Hit {hit.collider.name} at {hit.point} with normal {hit.normal}");

            // --- THE CORE OF THE IMPACT DECAL SYSTEM USAGE ---
            // Request the ImpactDecalManager to spawn a decal.
            // The manager handles finding a pooled decal, setting it up, and managing its lifetime.
            if (ImpactDecalManager.Instance != null)
            {
                ImpactDecalManager.Instance.SpawnDecal(
                    decalTypeToSpawn,
                    hit.point,
                    hit.normal,
                    hit.collider.transform, // Parent decal to the hit object for moving surfaces
                    decalScale,
                    customDecalLifetime
                );
            }
            else
            {
                Debug.LogError("ImpactDecalManager.Instance is null. Make sure it's present in the scene and initialized.");
            }

            // Optional: Play a particle effect at the impact point
            if (impactEffectPrefab != null)
            {
                // Instantiate a particle effect (consider pooling particle systems too for performance)
                GameObject effect = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f); // Destroy the effect after a short duration
            }
        }
        else
        {
            Debug.Log("No hit.");
        }
    }
}
```

---

### **Unity Project Setup:**

1.  **Create C# Scripts:**
    *   Create a folder `Scripts/GameSystems/Decals`.
    *   Place `ImpactDecalManager.cs`, `DecalType.cs`, `DecalController.cs`, and `ObjectPool.cs` into this folder.
    *   Place `ImpactGenerator.cs` in a convenient location (e.g., `Scripts/Examples`).

2.  **Create Decal Prefabs:**
    *   For each `DecalType` you want to use (e.g., `BulletHole`, `BloodSplatter`), create a simple 3D Quad or a 2D Sprite.
    *   **Quad Decal Example:**
        *   Create `3D Object -> Quad`.
        *   Rename it (e.g., `Decal_BulletHole`).
        *   Create a new Material (e.g., `Mat_BulletHole`). Assign a bullet hole texture to its Albedo. Set its render mode to `Fade` (for URP/HDRP) or `Transparent` (for Standard RP) to allow for fading.
        *   Assign `Mat_BulletHole` to the `Decal_BulletHole` Quad's `MeshRenderer`.
        *   Add `DecalController.cs` component to `Decal_BulletHole`.
        *   In `DecalController`, set `_decalType` to `BulletHole`. Ensure the `Mesh Renderer` field is correctly assigned, and configure `Enable Fade` and `Fade Duration` as desired.
        *   Drag `Decal_BulletHole` from the Hierarchy into your Project window to create a prefab. Delete it from the Hierarchy.
    *   Repeat for `Decal_BloodSplatter`, `Decal_ExplosionScorch`, etc.

3.  **Setup `ImpactDecalManager` in Scene:**
    *   Create an empty GameObject in your scene, name it `ImpactDecalManager`.
    *   Add the `ImpactDecalManager.cs` component to it.
    *   In the Inspector for `ImpactDecalManager`, expand the `Decal Pool Settings` list.
    *   For each `DecalType` you created a prefab for:
        *   Add a new element to the list.
        *   Set `Decal Type` (e.g., `BulletHole`).
        *   Drag your corresponding decal prefab (e.g., `Decal_BulletHole`) into the `Decal Prefab` slot.
        *   Adjust `Initial Pool Size` and `Default Lifetime` as needed.

4.  **Setup `ImpactGenerator` in Scene:**
    *   Create an empty GameObject, name it `PlayerWeapon` or similar.
    *   Add the `ImpactGenerator.cs` component to it.
    *   In the Inspector for `ImpactGenerator`:
        *   Set `Decal Type To Spawn` (e.g., `BulletHole`).
        *   Adjust `Max Impact Distance` and `Decal Scale`.
        *   (Optional) If you have a particle system prefab for impacts, drag it into the `Impact Effect Prefab` slot.
    *   Ensure your scene has a `Main Camera` tagged as such.
    *   Add some colliders to your scene (e.g., `3D Object -> Cube`, `Plane`) for the raycast to hit.

**How to Use in Practice:**

Once set up, any game object (e.g., a bullet, a player's weapon, an explosion script) that needs to create an impact decal simply makes a call like this:

```csharp
using GameSystems.Decals; // Essential to access DecalType and ImpactDecalManager

// ... inside your bullet script's OnCollisionEnter or on a raycast hit ...
void OnHit(Vector3 hitPoint, Vector3 hitNormal, Transform hitObjectTransform)
{
    // Spawn a bullet hole decal
    ImpactDecalManager.Instance.SpawnDecal(
        DecalType.BulletHole,
        hitPoint,
        hitNormal,
        hitObjectTransform, // Optional: parent to the object that was hit
        0.5f // Optional: custom scale
    );

    // If it was a blood-related impact
    // ImpactDecalManager.Instance.SpawnDecal(
    //     DecalType.BloodSplatter,
    //     hitPoint,
    //     hitNormal,
    //     hitObjectTransform,
    //     1.2f, // Larger scale
    //     3f    // Shorter custom lifetime
    // );
}
```

This complete setup provides a robust, performant, and flexible Impact Decal System ready for use in a real Unity project.