// Unity Design Pattern Example: SmokeEffectSystem
// This script demonstrates the SmokeEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements the 'SmokeEffectSystem' pattern by combining several established design patterns commonly used in Unity for managing visual effects:

1.  **Singleton Pattern**: Ensures a single, globally accessible instance of the `SmokeEffectManager`.
2.  **Object Pool Pattern**: Reuses `GameObject` instances of smoke effects for performance, avoiding constant `Instantiate` and `Destroy` calls.
3.  **Simplified Factory Pattern**: Provides a clean, centralized interface (`SpawnSmoke`) for creating and configuring smoke effects with various parameters.

This combined approach creates a robust and efficient "system" for handling smoke effects (or any similar short-lived visual effects) in a Unity game.

---

## Complete C# Unity Script: `SmokeEffectSystem.cs`

This single script contains two classes:
*   `SmokeEffect`: A MonoBehaviour attached to your smoke particle system prefab.
*   `SmokeEffectManager`: The singleton manager that handles pooling and spawning.

To use this, save the code below as `SmokeEffectSystem.cs` in your Unity project.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List and Queue

/// <summary>
/// Represents a single instance of a smoke effect.
/// This component is attached to the smoke effect prefab and manages its ParticleSystem.
/// </summary>
public class SmokeEffect : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Gets the ParticleSystem component reference and ensures it doesn't play on awake.
    /// </summary>
    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        if (_particleSystem == null)
        {
            Debug.LogError("SmokeEffect: No ParticleSystem found on this GameObject.", this);
            enabled = false; // Disable if no ParticleSystem is found
        }
        // Ensure the particle system doesn't play automatically on instantiation/activation.
        // The manager will explicitly call Play().
        _particleSystem.playOnAwake = false;
    }

    /// <summary>
    /// Configures the particle system's main module properties and plays the effect.
    /// </summary>
    /// <param name="position">The world position to spawn the effect.</param>
    /// <param name="startColor">The initial color of the particles.</param>
    /// <param name="startSize">The initial size of the particles.</param>
    /// <param name="particleLifetime">How long each individual particle lives.</param>
    public void SetupAndPlay(Vector3 position, Color startColor, float startSize, float particleLifetime)
    {
        transform.position = position; // Set the world position of the effect
        gameObject.SetActive(true);    // Activate the GameObject to make it visible

        // Access the main module of the particle system to modify properties
        var main = _particleSystem.main;
        main.startColor = startColor;
        main.startSize = new ParticleSystem.MinMaxCurve(startSize); // Use MinMaxCurve for startSize
        main.startLifetime = particleLifetime;
        // If your prefab's particle system is set to loop, you might want to force it off for one-shot effects.
        // main.loop = false; 

        _particleSystem.Clear(); // Clears any residual particles from previous plays
        _particleSystem.Play();  // Starts the particle system emission
    }

    /// <summary>
    /// Stops the particle system and prepares it for recycling.
    /// This is typically called by the SmokeEffectManager when the effect's duration is over.
    /// </summary>
    public void StopEffect()
    {
        // Stop all emissions and clear existing particles immediately
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false); // Deactivate the GameObject to return it to the pool
    }
}


/// <summary>
/// The central manager for all smoke effects in the game.
/// This class implements the 'SmokeEffectSystem' pattern by combining:
/// 1.  Singleton Pattern: Ensures a single, globally accessible instance.
/// 2.  Object Pool Pattern: Reuses smoke effect GameObjects for performance.
/// 3.  Simplified Factory Pattern: Provides a clean interface to 'spawn' effects with custom parameters.
/// </summary>
public class SmokeEffectManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    /// <summary>
    /// The static instance of the SmokeEffectManager, ensuring only one exists.
    /// Other scripts can access this manager via `SmokeEffectManager.Instance`.
    /// </summary>
    public static SmokeEffectManager Instance { get; private set; }

    [Header("Effect Settings")]
    [Tooltip("The prefab for the smoke effect. It MUST have a ParticleSystem and a SmokeEffect component.")]
    [SerializeField]
    private GameObject smokeEffectPrefab;

    [Tooltip("The initial number of smoke effects to pre-instantiate for the pool. Increase for more concurrent effects.")]
    [SerializeField]
    private int initialPoolSize = 10;

    // --- Object Pool Pattern ---
    private Queue<SmokeEffect> _availableEffects; // Stores effects that are currently inactive and ready for reuse
    private List<SmokeEffect> _allEffects;       // Keeps track of all created effects (for potential cleanup or debugging)

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton and the Object Pool.
    /// </summary>
    void Awake()
    {
        // Implement Singleton pattern to ensure only one instance of the manager exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SmokeEffectManager: An instance already exists. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, uncomment the line below to keep the manager alive across scene changes.
        // DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    /// <summary>
    /// Pre-instantiates the smoke effect prefabs and adds them to the object pool.
    /// This prevents performance spikes during gameplay from creating new GameObjects.
    /// </summary>
    private void InitializePool()
    {
        if (smokeEffectPrefab == null)
        {
            Debug.LogError("SmokeEffectManager: Smoke Effect Prefab is not assigned! Please assign it in the Inspector.", this);
            enabled = false; // Disable the manager if essential prefab is missing
            return;
        }

        // Ensure the prefab has the required SmokeEffect component for our system to work.
        if (smokeEffectPrefab.GetComponent<SmokeEffect>() == null)
        {
            Debug.LogError("SmokeEffectManager: The assigned 'Smoke Effect Prefab' MUST have a 'SmokeEffect' component attached!", this);
            enabled = false;
            return;
        }

        _availableEffects = new Queue<SmokeEffect>(initialPoolSize);
        _allEffects = new List<SmokeEffect>(initialPoolSize);

        // Instantiate effects and add them to the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            SmokeEffect newEffect = CreateNewEffectInstance();
            newEffect.StopEffect(); // Deactivate and stop it immediately upon creation
            _availableEffects.Enqueue(newEffect);
        }

        Debug.Log($"SmokeEffectManager: Initial pool created with {initialPoolSize} effects.");
    }

    /// <summary>
    /// Creates a new instance of the smoke effect prefab.
    /// This method is called by InitializePool or when the pool runs out of available effects.
    /// </summary>
    /// <returns>The newly created SmokeEffect component.</returns>
    private SmokeEffect CreateNewEffectInstance()
    {
        // Instantiate the prefab as a child of this manager for better hierarchy organization
        GameObject newGO = Instantiate(smokeEffectPrefab, transform);
        SmokeEffect effect = newGO.GetComponent<SmokeEffect>();
        _allEffects.Add(effect); // Keep track of all created effects for potential global management/cleanup
        return effect;
    }

    /// <summary>
    /// Retrieves a smoke effect from the pool. If the pool is empty, a new effect is created.
    /// </summary>
    /// <returns>An available (or newly created) SmokeEffect instance.</returns>
    private SmokeEffect GetEffectFromPool()
    {
        if (_availableEffects.Count > 0)
        {
            return _availableEffects.Dequeue(); // Reuse an existing effect
        }
        else
        {
            // If the pool is exhausted, create a new one. This ensures effects are always available
            // but might indicate that initialPoolSize needs to be increased.
            Debug.LogWarning("SmokeEffectManager: Pool exhausted. Creating a new smoke effect instance. Consider increasing initialPoolSize.", this);
            return CreateNewEffectInstance();
        }
    }

    /// <summary>
    /// Returns a smoke effect to the pool, making it available for reuse.
    /// The effect is stopped and deactivated here.
    /// </summary>
    /// <param name="effect">The SmokeEffect instance to return.</param>
    private void ReturnEffectToPool(SmokeEffect effect)
    {
        // Ensure the effect is valid and not already in the pool to prevent duplicates
        if (effect != null && !_availableEffects.Contains(effect))
        {
            effect.StopEffect(); // Stop and deactivate the effect
            _availableEffects.Enqueue(effect); // Add it back to the queue of available effects
        }
    }

    // --- Simplified Factory Pattern (public interface) ---
    /// <summary>
    /// Spawns a smoke effect at a given position with customizable properties.
    /// This is the primary method for external scripts to request a smoke effect.
    /// </summary>
    /// <param name="position">The world position to spawn the smoke effect.</param>
    /// <param name="color">The start color of the smoke particles.</param>
    /// <param name="scale">The overall scale of the smoke effect (applies to the GameObject's localScale).</param>
    /// <param name="effectDuration">How long the entire effect will be visible before being returned to the pool (in seconds).</param>
    /// <param name="particleLifetime">How long each individual particle within the system lives (in seconds).</param>
    /// <returns>The spawned SmokeEffect instance, or null if the manager is not initialized or disabled.</returns>
    public SmokeEffect SpawnSmoke(Vector3 position, Color color, float scale = 1f, float effectDuration = 2f, float particleLifetime = 1f)
    {
        // Safety check to ensure the manager is ready
        if (Instance == null || smokeEffectPrefab == null || !enabled)
        {
            Debug.LogError("SmokeEffectManager is not initialized or disabled. Cannot spawn smoke.", this);
            return null;
        }

        SmokeEffect effect = GetEffectFromPool();
        if (effect != null)
        {
            effect.transform.localScale = Vector3.one * scale; // Apply overall scale to the effect GameObject
            effect.SetupAndPlay(position, color, scale, particleLifetime); // Configure and play the particle system
            StartCoroutine(ReturnEffectAfterDelay(effect, effectDuration)); // Schedule its return to the pool after the specified duration
        }
        return effect;
    }

    /// <summary>
    /// Coroutine to return an effect to the pool after a specified delay.
    /// This handles the lifecycle of the effect by making it available for reuse.
    /// </summary>
    /// <param name="effect">The SmokeEffect instance to return.</param>
    /// <param name="delay">The time in seconds to wait before returning the effect.</param>
    private IEnumerator ReturnEffectAfterDelay(SmokeEffect effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only return if the effect is still valid (hasn't been destroyed externally)
        if (effect != null)
        {
            ReturnEffectToPool(effect);
        }
    }

    /// <summary>
    /// Cleans up all created effect GameObjects when the manager is destroyed.
    /// This is good practice for memory management, especially when scenes unload.
    /// </summary>
    void OnDestroy()
    {
        // Clear the static instance reference
        if (Instance == this)
        {
            Instance = null;
        }

        // Destroy all GameObjects created by the pool
        foreach (SmokeEffect effect in _allEffects)
        {
            if (effect != null) // Check if it hasn't been destroyed already
            {
                Destroy(effect.gameObject);
            }
        }
        _allEffects.Clear();
        _availableEffects.Clear();
    }
}


/*
====================================================================================================
                        HOW TO USE THE SMOKEEFFECTSYSTEM IN YOUR UNITY PROJECT
====================================================================================================

This setup provides a highly efficient and flexible way to manage and trigger visual effects
like smoke, explosions, magic spells, or impact effects.

STEP 1: Create a Smoke Effect Prefab
-------------------------------------
1.  In Unity, right-click in the Hierarchy -> "Effects" -> "Particle System".
2.  Rename it (e.g., "Smoke_VFX_Prefab").
3.  Adjust the Particle System settings to achieve your desired smoke appearance.
    -   **Crucial Settings for a good smoke effect:**
        -   **Main Module**:
            -   `Duration`: How long one emission cycle lasts (e.g., 1-2 seconds).
            -   `Looping`: **UNCHECK** this if you want one-shot puffs.
            -   `Start Lifetime`: How long individual particles live (e.g., 1-3 seconds).
            -   `Start Speed`: Initial speed of particles (e.g., 0.5-2).
            -   `Start Size`: Initial size of particles (e.g., 0.5-1.5).
            -   `Start Color`: Default color (can be overridden by manager).
        -   **Emission Module**:
            -   `Rate over Time`: How many particles per second (e.g., 10-50).
        -   **Shape Module**:
            -   Choose a shape (e.g., Sphere, Cone, Hemisphere) and adjust its properties.
        -   **Size over Lifetime Module**:
            -   Enable it. Set a curve to make particles grow then shrink (e.g., starting small, peaking, then fading).
        -   **Color over Lifetime Module**:
            -   Enable it. Set a gradient to fade particles to transparent towards their end of life, or change color.
        -   **Renderer Module**:
            -   Set `Render Mode` to `Billboard`.
            -   Assign a `Material` that supports transparency (e.g., "Particles/Standard Unlit" or a custom shader with a soft, smoky texture).
4.  **Add the `SmokeEffect` script**:
    -   With your "Smoke_VFX_Prefab" GameObject selected, click "Add Component" in the Inspector.
    -   Search for "SmokeEffect" and add it.
5.  Drag this configured GameObject from the Hierarchy into your Project window (e.g., into a "Prefabs" folder) to create a prefab.
6.  Delete the original GameObject from the Hierarchy.

STEP 2: Set Up the SmokeEffectManager
-------------------------------------
1.  Create an Empty GameObject in your Hierarchy (e.g., name it "SmokeEffectManager").
2.  Add the `SmokeEffectManager` script to this GameObject.
    -   With "SmokeEffectManager" selected, click "Add Component".
    -   Search for "SmokeEffectManager" and add it.
3.  Select the "SmokeEffectManager" GameObject.
4.  In the Inspector, drag your "Smoke_VFX_Prefab" (from Step 1) into the "Smoke Effect Prefab" slot.
5.  You can adjust the "Initial Pool Size" (e.g., 5-20 depending on your game's needs). This determines how many smoke effects are pre-made at start.

STEP 3: Use the SmokeEffectSystem from another script
------------------------------------------------------
Now you can easily trigger smoke effects from any other script in your game by calling the `SpawnSmoke` method on the `SmokeEffectManager.Instance`.

Example Usage Script: `ExampleSmokeTrigger.cs` (Create a new C# script and attach it to any GameObject in your scene)

```csharp
using UnityEngine;

public class ExampleSmokeTrigger : MonoBehaviour
{
    // Public fields to easily adjust smoke parameters in the Inspector
    [Header("Smoke Presets")]
    public Color fireSmokeColor = new Color(1f, 0.5f, 0f, 0.7f); // Orangey
    public Color magicSmokeColor = new Color(0.5f, 0f, 1f, 0.7f); // Purplish
    public Color normalSmokeColor = new Color(0.7f, 0.7f, 0.7f, 0.7f); // Greyish-white

    public float smallSmokeScale = 0.5f;
    public float largeSmokeScale = 2.0f;
    public float shortDuration = 1.0f;
    public float mediumDuration = 3.0f;
    public float longDuration = 5.0f;

    void Update()
    {
        // Example 1: Spawn a small, short, normal smoke puff
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SmokeEffectManager.Instance.SpawnSmoke(
                transform.position + Vector3.up * 0.5f, // Position slightly above this GameObject
                normalSmokeColor,
                smallSmokeScale,
                shortDuration,
                0.7f // particleLifetime: individual particles live for 0.7s
            );
            Debug.Log("Spawned small normal smoke (Press '1').");
        }

        // Example 2: Spawn a large, medium duration, fiery smoke effect
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SmokeEffectManager.Instance.SpawnSmoke(
                transform.position + Vector3.forward * 1.5f, // Position in front of this GameObject
                fireSmokeColor,
                largeSmokeScale,
                mediumDuration,
                1.5f // particleLifetime: individual particles live for 1.5s
            );
            Debug.Log("Spawned large fire smoke (Press '2').");
        }

        // Example 3: Spawn a medium-sized, long duration, magic smoke effect at a random offset
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), Random.Range(-1f, 1f));
            SmokeEffectManager.Instance.SpawnSmoke(
                transform.position + randomOffset,
                magicSmokeColor,
                1.0f, // Medium scale
                longDuration,
                2.0f // particleLifetime: individual particles live for 2.0s
            );
            Debug.Log("Spawned magic smoke with random offset (Press '3').");
        }

        // Example 4: Spawn smoke at the mouse click position (requires a Camera and Colliders)
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // Ensure your scene has colliders for raycasting to hit
            if (Physics.Raycast(ray, out hit, 100f))
            {
                SmokeEffectManager.Instance.SpawnSmoke(
                    hit.point + Vector3.up * 0.1f, // Position slightly above the hit point
                    new Color(Random.value, Random.value, Random.value, 0.8f), // Random color
                    Random.Range(0.8f, 1.5f), // Random scale
                    Random.Range(2f, 4f),     // Random effect duration
                    Random.Range(1f, 2f)      // Random particle lifetime
                );
                Debug.Log($"Spawned random smoke at {hit.point} (Left Mouse Click).");
            }
        }
    }
}
```

**To use `ExampleSmokeTrigger`:**
1.  Create an Empty GameObject in your scene (e.g., "SmokeTrigger").
2.  Add the `ExampleSmokeTrigger` script to it.
3.  Run the scene. Press '1', '2', '3' or click the left mouse button (on a surface with a collider) to spawn different types of smoke effects.

This "SmokeEffectSystem" provides a powerful and organized way to manage and optimize visual effects, crucial for performance and flexibility in Unity game development.