// Unity Design Pattern Example: ParticleManager
// This script demonstrates the ParticleManager pattern in Unity
// Generated automatically - ready to use in your Unity project

Here's a complete, practical C# Unity example for implementing the 'ParticleManager' design pattern. This script includes object pooling for particle systems, detailed comments, and example usage instructions.

This solution consists of two classes (`ParticleManager` and `ParticleSystemAutoReturn`) within a single C# file, which is a common and convenient way to provide a self-contained, drop-in solution for Unity.

**`ParticleManager.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Serializable

// This script demonstrates the 'ParticleManager' design pattern using object pooling
// for efficient management and reuse of particle systems in Unity.
// It is designed to be a complete, drop-in solution.

namespace MyGame.VFX // Recommended: Use a namespace for better organization
{
    /// <summary>
    /// Configuration struct for a particle effect type.
    /// Used by the ParticleManager to define which particle systems to pool and their initial size.
    /// </summary>
    [Serializable]
    public struct ParticleEffectConfig
    {
        [Tooltip("A unique name to identify this particle effect type. Used when playing the effect.")]
        public string EffectName;
        
        [Tooltip("The prefab of the particle system. It MUST have a ParticleSystem component.")]
        public ParticleSystem Prefab;
        
        [Tooltip("How many instances of this particle system to create initially for the pool.")]
        [Range(1, 50)] // Clamp initial size for reasonable values in Inspector
        public int InitialPoolSize;
    }

    /// <summary>
    /// ParticleManager is a singleton responsible for object pooling and managing
    /// the lifecycle of various particle systems in the game.
    /// 
    /// **Pattern Explanation (ParticleManager):**
    /// The ParticleManager implements a **Singleton** pattern for global access and an **Object Pool** pattern
    /// to efficiently manage ParticleSystem instances.
    /// 
    /// - **Singleton:** Ensures there's only one instance of the manager throughout the game, providing a
    ///   centralized point for all particle effect requests. This makes it easy for any script to
    ///   `ParticleManager.Instance.PlayParticleEffect(...)`.
    ///   
    /// - **Object Pool:** Instead of constantly `Instantiate()`ing and `Destroy()`ing particle systems
    ///   (which are expensive operations, especially at runtime), the manager pre-instantiates a pool
    ///   of particle systems for each defined effect type. When an effect is requested, an idle
    ///   instance is taken from the pool, configured, and activated. When it finishes playing,
    ///   it's returned to the pool (deactivated) instead of being destroyed. This drastically reduces
    ///   garbage collection and CPU spikes.
    ///   
    /// - **Dynamic Expansion:** If all instances in a specific pool are currently in use, the manager
    ///   can dynamically create a new instance to fulfill the request, preventing the game from
    ///   stalling or failing to play an effect. This new instance is then added to the pool for future reuse.
    ///   
    /// - **Automatic Return:** The <see cref="ParticleSystemAutoReturn"/> helper script attached to
    ///   the particle system prefabs ensures that once an effect finishes playing, it automatically
    ///   notifies the ParticleManager to return it to the correct pool, completing the cycle.
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        // === Singleton Instance ===
        // Public static property to provide global access to the single instance of ParticleManager.
        public static ParticleManager Instance { get; private set; }

        // === Configuration Fields ===
        [Tooltip("List of all particle effect types to be managed and pooled.")]
        [SerializeField]
        private List<ParticleEffectConfig> particleEffects = new List<ParticleEffectConfig>();

        // === Internal Data Structures ===
        // A dictionary where keys are effect names (strings) and values are queues of ParticleSystem instances.
        // Each queue represents a pool for a specific type of particle effect.
        private Dictionary<string, Queue<ParticleSystem>> poolDictionary = new Dictionary<string, Queue<ParticleSystem>>();

        // A dictionary to quickly find the original prefab for a given effect name.
        // Used when the pool is exhausted and a new instance needs to be created.
        private Dictionary<string, ParticleSystem> prefabDictionary = new Dictionary<string, ParticleSystem>();

        // === Unity Lifecycle Methods ===

        private void Awake()
        {
            // --- Singleton Initialization ---
            // If another instance of ParticleManager already exists in the scene and it's not this one,
            // destroy this GameObject to ensure only one manager instance persists.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ParticleManager: Another instance of ParticleManager found, destroying this one to enforce singleton pattern.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this; // Assign this instance as the singleton.

            // Ensure this manager persists across scene loads.
            // This is typical for managers that provide global services throughout the game's lifetime.
            DontDestroyOnLoad(gameObject);

            // Initialize all particle effect pools based on the configuration set in the Inspector.
            InitializePools();
        }

        /// <summary>
        /// Initializes the object pools for all configured particle effects.
        /// This method is called once when the ParticleManager starts up in Awake().
        /// It populates the pools with a predefined number of instances for each effect type.
        /// </summary>
        private void InitializePools()
        {
            foreach (var config in particleEffects)
            {
                // --- Configuration Validation ---
                if (string.IsNullOrEmpty(config.EffectName))
                {
                    Debug.LogError($"ParticleManager: Configuration error - An effect entry has an empty 'Effect Name'. Skipping this entry.", this);
                    continue;
                }
                if (config.Prefab == null)
                {
                    Debug.LogError($"ParticleManager: Configuration error for '{config.EffectName}' - 'Prefab' field is null. Skipping this entry.", this);
                    continue;
                }
                if (poolDictionary.ContainsKey(config.EffectName))
                {
                    Debug.LogWarning($"ParticleManager: Duplicate 'Effect Name' '{config.EffectName}' found in configuration. Only the first entry will be used.", this);
                    continue;
                }
                
                // Store the prefab reference in the prefab dictionary for potential dynamic instantiation later.
                prefabDictionary.Add(config.EffectName, config.Prefab);

                // Create a new queue (pool) for this specific particle effect type.
                poolDictionary.Add(config.EffectName, new Queue<ParticleSystem>());

                // Pre-instantiate instances up to the initial pool size and add them to the queue.
                for (int i = 0; i < config.InitialPoolSize; i++)
                {
                    CreateParticleInstance(config.EffectName, config.Prefab, poolDictionary[config.EffectName]);
                }
            }
            Debug.Log($"ParticleManager: Initialized pools for {particleEffects.Count} effect types.");
        }

        /// <summary>
        /// Creates a new instance of a particle system prefab, configures it for pooling,
        /// and adds it to the specified queue. This method is used during initial pool setup
        /// and for dynamic expansion when a pool is empty.
        /// </summary>
        /// <param name="effectName">The unique name of the effect this instance belongs to.</param>
        /// <param name="prefab">The ParticleSystem prefab to instantiate.</param>
        /// <param name="targetPool">The queue to add the created instance to.</param>
        /// <returns>The newly created and configured ParticleSystem instance.</returns>
        private ParticleSystem CreateParticleInstance(string effectName, ParticleSystem prefab, Queue<ParticleSystem> targetPool)
        {
            // Instantiate the prefab. Parent it to the ParticleManager GameObject for scene organization,
            // keeping the Hierarchy clean from unmanaged root objects.
            ParticleSystem newParticleSystem = Instantiate(prefab, transform);
            newParticleSystem.gameObject.SetActive(false); // Deactivate it immediately; it's not playing yet.

            // --- Ensure ParticleSystemAutoReturn Component ---
            // This custom component (defined below) is crucial for automatically returning particle systems
            // to the manager's pool once they finish playing.
            // If it's not already attached to the prefab, we add it dynamically.
            ParticleSystemAutoReturn autoReturn = newParticleSystem.GetComponent<ParticleSystemAutoReturn>();
            if (autoReturn == null)
            {
                autoReturn = newParticleSystem.gameObject.AddComponent<ParticleSystemAutoReturn>();
            }
            // Inform the auto-return script which pool this instance belongs to.
            autoReturn.SetEffectName(effectName); 
            
            // --- Configure ParticleSystem for Auto-Return ---
            // Ensure the main module's 'Stop Action' is set to 'Disable'.
            // This is vital: it tells Unity to call the OnParticleSystemStopped() callback
            // when the particle system naturally finishes playing, which the ParticleSystemAutoReturn
            // script listens for. If this is not set, the particle system will not automatically
            // return to the pool.
            var mainModule = newParticleSystem.main;
            mainModule.stopAction = ParticleSystemStopAction.Disable;

            targetPool.Enqueue(newParticleSystem); // Add the newly created instance to its designated pool.
            return newParticleSystem;
        }

        // === Public API for Playing Particle Effects ===

        /// <summary>
        /// Plays a particle effect from the pool at a specified world position and rotation.
        /// This is the primary method for external scripts to request a particle effect.
        /// </summary>
        /// <param name="effectName">The unique name of the particle effect to play (as configured in the Inspector).</param>
        /// <param name="position">The world position where the effect should appear.</param>
        /// <param name="rotation">The rotation of the effect. Defaults to Quaternion.identity (no rotation) if not provided.</param>
        /// <param name="parent">Optional parent transform for the particle system. If null, it remains a child of the ParticleManager GameObject.</param>
        /// <returns>The activated ParticleSystem instance, or null if the effect name is not found.</returns>
        public ParticleSystem PlayParticleEffect(string effectName, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            // --- Input Validation ---
            if (string.IsNullOrEmpty(effectName))
            {
                Debug.LogWarning("ParticleManager: Attempted to play effect with an empty name. Cannot proceed.", this);
                return null;
            }

            // Try to retrieve the queue (pool) associated with the requested effect name.
            if (!poolDictionary.TryGetValue(effectName, out Queue<ParticleSystem> effectPool))
            {
                Debug.LogWarning($"ParticleManager: Effect '{effectName}' not found in pool dictionary. Check ParticleManager configuration.", this);
                return null;
            }

            ParticleSystem particleSystemToPlay;

            // --- Retrieve from Pool or Instantiate New ---
            if (effectPool.Count > 0)
            {
                // If the pool has available instances, dequeue one for reuse.
                particleSystemToPlay = effectPool.Dequeue();
            }
            else
            {
                // If the pool is empty, dynamically create a new instance to meet the demand.
                // This ensures that the game doesn't stop playing effects even if the initial pool size is too small.
                if (prefabDictionary.TryGetValue(effectName, out ParticleSystem prefab))
                {
                    Debug.LogWarning($"ParticleManager: Pool for '{effectName}' is empty. Instantiating a new particle system to meet demand.", this);
                    // Instantiate a new particle system directly (not through CreateParticleInstance as it queues it).
                    particleSystemToPlay = Instantiate(prefab); 
                    particleSystemToPlay.transform.SetParent(transform); // Parent to manager for organization.

                    // Ensure the auto-return component is present and configured for this new instance.
                    ParticleSystemAutoReturn autoReturn = particleSystemToPlay.GetComponent<ParticleSystemAutoReturn>();
                    if (autoReturn == null)
                    {
                        autoReturn = particleSystemToPlay.gameObject.AddComponent<ParticleSystemAutoReturn>();
                    }
                    autoReturn.SetEffectName(effectName);

                    // Re-assert the 'Stop Action' property, as it's crucial for auto-return.
                    var mainModule = particleSystemToPlay.main;
                    mainModule.stopAction = ParticleSystemStopAction.Disable;
                }
                else
                {
                    Debug.LogError($"ParticleManager: Prefab for '{effectName}' not found even after pool was exhausted. This indicates a serious configuration error.", this);
                    return null;
                }
            }
            
            // --- Configure and Activate the Particle System ---
            particleSystemToPlay.transform.position = position;
            // Handle the default Quaternion value (which is (0,0,0,0) and not Quaternion.identity).
            particleSystemToPlay.transform.rotation = (rotation == default) ? Quaternion.identity : rotation; 
            particleSystemToPlay.transform.SetParent(parent); // Set parent if provided, otherwise it remains child of manager.

            // Stop and clear any previous emissions/particles. This ensures the effect starts fresh every time
            // it's played, even if it was stopped prematurely or re-used quickly.
            particleSystemToPlay.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystemToPlay.gameObject.SetActive(true); // Activate the GameObject.
            
            particleSystemToPlay.Play(); // Start playing the particle system.

            return particleSystemToPlay; // Return the activated instance for potential further control (e.g., stopping a looping effect).
        }

        /// <summary>
        /// Returns a particle system instance back to its respective pool.
        /// This method is primarily called by the <see cref="ParticleSystemAutoReturn"/> script attached to the
        /// particle prefabs, once the particle effect has finished playing.
        /// </summary>
        /// <param name="particleSystem">The ParticleSystem instance to return to the pool.</param>
        /// <param name="effectName">The unique name of the effect type this particle system belongs to.
        ///                          This is provided by the ParticleSystemAutoReturn component.</param>
        public void ReturnParticleEffect(ParticleSystem particleSystem, string effectName)
        {
            // --- Input Validation ---
            if (particleSystem == null)
            {
                Debug.LogWarning("ParticleManager: Attempted to return a null particle system. Ignoring.", this);
                return;
            }
            if (string.IsNullOrEmpty(effectName))
            {
                Debug.LogWarning($"ParticleManager: Attempted to return particle system '{particleSystem.name}' without a specified effect name. Destroying it to prevent leaks.", this);
                Destroy(particleSystem.gameObject);
                return;
            }

            // Try to find the correct pool based on the provided effect name.
            if (poolDictionary.TryGetValue(effectName, out Queue<ParticleSystem> effectPool))
            {
                // Ensure the particle system is stopped and cleared before being returned to the pool.
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
                particleSystem.gameObject.SetActive(false); // Deactivate the GameObject.
                particleSystem.transform.SetParent(Instance.transform); // Re-parent to the manager for organization.

                effectPool.Enqueue(particleSystem); // Add the particle system back to its pool.
            }
            else
            {
                // This scenario means an effect was returned for a name that no longer exists in our configuration,
                // or the effectName was incorrect. It should be destroyed to prevent orphaned objects.
                Debug.LogWarning($"ParticleManager: Returned particle system '{particleSystem.name}' for unknown or removed effect '{effectName}'. Destroying it instead of pooling.", this);
                Destroy(particleSystem.gameObject);
            }
        }
    }

    /// <summary>
    /// Helper script attached to particle system prefabs.
    /// It automatically returns them to the ParticleManager's pool once they finish playing.
    /// 
    /// **Key Requirement:** The ParticleSystem's 'Stop Action' in the Main Module
    /// MUST be set to 'Disable' for `OnParticleSystemStopped()` to be called by Unity.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))] // Ensures a ParticleSystem component is always present.
    public class ParticleSystemAutoReturn : MonoBehaviour
    {
        private ParticleSystem ps;      // Reference to the ParticleSystem component.
        private string effectName;      // Stores the unique name of the effect this instance belongs to.
                                        // This is set by the ParticleManager when the effect is played.

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            // Just a redundant check/setting to ensure the particle system is configured correctly.
            // ParticleManager also sets this when creating/reusing instances.
            var mainModule = ps.main;
            mainModule.stopAction = ParticleSystemStopAction.Disable;
        }

        /// <summary>
        /// This Unity callback is invoked by the ParticleSystem when it finishes playing
        /// AND its 'Stop Action' in the Main Module is set to 'Disable'.
        /// This is the core mechanism for automatic return to the pool.
        /// </summary>
        private void OnParticleSystemStopped()
        {
            // Check if the ParticleManager instance is still active and if we know which effect this is.
            if (ParticleManager.Instance != null && !string.IsNullOrEmpty(effectName))
            {
                // If everything is in order, return this particle system instance to the manager's pool.
                ParticleManager.Instance.ReturnParticleEffect(ps, effectName);
            }
            else
            {
                // Fallback: If the manager is gone (e.g., scene unloaded without manager being DontDestroyOnLoad)
                // or the effectName wasn't properly set (a configuration/logic error),
                // destroy the GameObject to prevent orphaned objects in the scene.
                Debug.LogWarning($"ParticleSystemAutoReturn: Particle system '{gameObject.name}' finished but could not be returned to pool (Manager null or effectName empty). Destroying it to prevent resource leaks.", this);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Sets the unique effect name for this particle system instance.
        /// This method is called by the ParticleManager when an instance is activated
        /// from the pool or newly created, linking the instance back to its specific pool.
        /// </summary>
        /// <param name="name">The unique effect name (e.g., "Explosion", "Sparkle").</param>
        public void SetEffectName(string name)
        {
            this.effectName = name;
        }
    }
}


/*
// ===== HOW TO SET UP AND USE THE PARTICLE MANAGER IN UNITY (STEP-BY-STEP GUIDE) =====

// 1. Create the C# script file:
//    - In your Unity Project window, right-click -> `Create -> C# Script`.
//    - Name it `ParticleManager`.
//    - Open the newly created `ParticleManager.cs` file and paste the ENTIRE code block above
//      (including both the `ParticleManager` class and the `ParticleSystemAutoReturn` class) into this single file.
//      Ensure you replace any existing code in the file.

// 2. Create the ParticleManager GameObject in your scene:
//    - In your Unity scene Hierarchy, right-click -> `Create Empty`.
//    - Rename the new GameObject to "ParticleManager".
//    - Drag and drop your `ParticleManager.cs` script asset from the Project window onto this
//      "ParticleManager" GameObject in the Hierarchy.

// 3. Create your Particle System Prefabs:
//    a. Go to `GameObject -> VFX -> Particle System`. This creates a default particle system in your scene.
//    b. Customize its appearance and behavior as desired using the Particle System component in the Inspector
//       (e.g., set color, lifetime, emission rate, shape, sub-emitters, etc.).
//    c. IMPORTANT CONFIGURATION STEP: In the Inspector for your Particle System, go to the `Main Module`.
//       Locate the `Stop Action` property and set it to `Disable`. This is absolutely crucial for the
//       `ParticleSystemAutoReturn` script to correctly detect when the particle system has finished playing
//       and return it to the pool.
//    d. Drag the configured Particle System GameObject from your Hierarchy into your Project window
//       (e.g., into a "Prefabs" folder) to create a reusable prefab.
//    e. After creating the prefab, delete the instance from the Hierarchy. The ParticleManager will
//       handle instantiating and managing them.
//    f. Repeat these steps for each unique particle effect you want to manage (e.g., create an "Explosion_VFX" prefab
//       and a "Sparkle_VFX" prefab).

// 4. Configure the ParticleManager in the Unity Inspector:
//    a. Select your "ParticleManager" GameObject in the scene Hierarchy.
//    b. In the Inspector, you will see the `Particle Effects` list under the `Particle Manager` component.
//    c. Expand this list and increase its `Size` property to add new entries for each of your particle effects.
//    d. For each element you add:
//       - `Effect Name`: Provide a unique string identifier (e.g., "Explosion", "Sparkle", "HealEffect").
//         This is the exact string name you will use in your game code to request this specific particle effect.
//       - `Prefab`: Drag your corresponding Particle System prefab (e.g., "Explosion_VFX") from your Project window
//         into this field.
//       - `Initial Pool Size`: Set an integer value for how many instances of this particle system should be
//         pre-instantiated when the game starts. A good starting point is the maximum number of this specific effect
//         you expect to see simultaneously. The pool will dynamically expand if more instances are needed at runtime.

// 5. Example Usage from another script (e.g., a "ParticleTrigger" script):

//    Now, to actually play particle effects, you'll call the `ParticleManager` from any other script in your game.
//    Create a new C# script (e.g., `ParticleTrigger.cs`) and attach it to any GameObject
//    in your scene (e.g., a player character, an enemy, a button, or an empty GameObject for simple testing).

using UnityEngine;
using MyGame.VFX; // Make sure to include the namespace where ParticleManager resides

public class ParticleTrigger : MonoBehaviour
{
    // Define public fields to assign keys in the Inspector for easy testing
    public KeyCode explodeKey = KeyCode.Space;
    public KeyCode sparkleKey = KeyCode.E;
    public KeyCode fireTrailKey = KeyCode.F; // Assuming you have a "FireTrail" effect configured

    [Header("Testing for Parented Effects")]
    [Tooltip("Assign another Transform (e.g., your Player GameObject) here to test parented effects.")]
    public Transform objectToAttachEffectTo; 
    
    void Update()
    {
        // Example 1: Play an explosion effect at this GameObject's current position when Space is pressed.
        if (Input.GetKeyDown(explodeKey))
        {
            // Call the static `Instance` of ParticleManager to play an effect.
            // Arguments: effectName (string), position (Vector3), rotation (Quaternion, optional), parent (Transform, optional).
            ParticleManager.Instance.PlayParticleEffect("Explosion", transform.position, Quaternion.identity);
            Debug.Log("Played 'Explosion' effect!");
        }

        // Example 2: Play a sparkle effect at a random position near this GameObject when 'E' is pressed.
        // Rotation is omitted here, so it defaults to Quaternion.identity (no specific rotation).
        if (Input.GetKeyDown(sparkleKey))
        {
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-1f, 1f), 0);
            ParticleManager.Instance.PlayParticleEffect("Sparkle", transform.position + randomOffset);
            Debug.Log("Played 'Sparkle' effect!");
        }

        // Example 3: Play a particle effect parented to another object (e.g., a player character) when 'F' is pressed.
        // This is useful for effects that should move with a specific entity (like a trail, a buff effect, etc.).
        if (Input.GetKeyDown(fireTrailKey) && objectToAttachEffectTo != null)
        {
            // Note: For truly continuous effects (like a long-lasting trail), you might want to call
            // PlayParticleEffect once, store the returned ParticleSystem reference, and then explicitly
            // call `particleSystemReference.Stop()` when the effect should end, rather than relying
            // solely on `OnParticleSystemStopped`. However, for most common uses where effects have
            // a finite duration, the auto-return system works perfectly.
            ParticleManager.Instance.PlayParticleEffect("FireTrail", objectToAttachEffectTo.position, Quaternion.identity, objectToAttachEffectTo);
            Debug.Log($"Played 'FireTrail' effect parented to '{objectToAttachEffectTo.name}'!");
        }
    }
}

// END OF USAGE INSTRUCTIONS
*/
```