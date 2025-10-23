// Unity Design Pattern Example: SummoningSystem
// This script demonstrates the SummoningSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SummoningSystem' design pattern, while not a classical GoF pattern, is a highly practical and common pattern in game development, especially in Unity. It acts as a central manager for creating, initializing, and often recycling (pooling) game entities that are "summoned" or spawned during gameplay (e.g., minions, spells, projectiles, VFX).

This pattern typically combines elements of:
*   **Factory Method/Abstract Factory**: The system acts as a factory for various types of summonable objects.
*   **Object Pool**: Crucial for performance, especially when summoning many transient objects.
*   **Service Locator/Singleton**: The SummoningSystem itself is often a globally accessible service.

---

### **Core Components of the SummoningSystem Pattern:**

1.  **`SummoningSystem` (The Manager)**:
    *   A central `MonoBehaviour` responsible for the entire process.
    *   Manages object pools for different summonable types.
    *   Provides methods to `Summon()` and `Despawn()` entities.
    *   Often implemented as a Singleton for easy global access.

2.  **`SummonableData` (ScriptableObject)**:
    *   Defines the blueprint for a summonable entity.
    *   Holds references to the prefab, initial pool size, auto-despawn settings, etc.
    *   Allows designers to configure new summonable types without writing code.

3.  **`ISummonable` (Interface)**:
    *   An interface implemented by components on the actual game objects that can be summoned.
    *   Provides methods like `Initialize()` (when activated from the pool) and `OnDespawned()` (when returned to the pool).
    *   Ensures that summoned objects conform to a standard lifecycle managed by the system.

4.  **`SummoningRequest` (Data Structure)**:
    *   A class or struct that encapsulates all necessary information for a summoning operation (what to summon, where, who summoned it, duration, custom data).

5.  **`PooledObject` (Helper Component)**:
    *   A small component attached to any pooled object to store a reference to its original `SummonableData`. This helps the `SummoningSystem` efficiently return the object to the correct pool.

---

### **Project Setup & Usage Instructions:**

1.  **Create C# Scripts**: Create the following C# scripts in your Unity project (e.g., in a `Scripts` folder):
    *   `SummoningSystem.cs`
    *   `SummonableData.cs`
    *   `ISummonable.cs`
    *   `SummoningRequest.cs`
    *   `PooledObject.cs`
    *   `ExampleSummoner.cs`
    *   `ExampleSummonableMinion.cs`
    *   `ExampleSummonableSpellEffect.cs`

2.  **Create SummoningSystem GameObject**:
    *   In an empty scene, create an empty GameObject and name it `SummoningSystem`.
    *   Attach the `SummoningSystem.cs` script to it.

3.  **Create Prefabs**:
    *   Create two basic prefabs (e.g., a red Capsule named "MinionPrefab" and a blue Sphere named "SpellEffectPrefab").
    *   **For "MinionPrefab"**:
        *   Add `ExampleSummonableMinion.cs` component.
        *   Add `PooledObject.cs` component.
    *   **For "SpellEffectPrefab"**:
        *   Add `ExampleSummonableSpellEffect.cs` component.
        *   Add `PooledObject.cs` component.

4.  **Create SummonableData ScriptableObjects**:
    *   Right-click in your Project window -> Create -> SummoningSystem -> Summonable Data.
    *   Create one and name it `MinionSummonableData`.
        *   Drag your "MinionPrefab" into its `Prefab` slot.
        *   Set `Summonable ID` to "Minion".
        *   Set `Initial Pool Size` to `5`.
        *   Set `Can Auto Despawn` to `false` (minions live until killed).
    *   Create another and name it `SpellEffectSummonableData`.
        *   Drag your "SpellEffectPrefab" into its `Prefab` slot.
        *   Set `Summonable ID` to "SpellEffect".
        *   Set `Initial Pool Size` to `10`.
        *   Set `Can Auto Despawn` to `true`.
        *   Set `Default Despawn Duration` to `3` seconds.

5.  **Configure SummoningSystem Manager**:
    *   Select the `SummoningSystem` GameObject in your scene.
    *   In its Inspector, expand the `Registered Summonables` list.
    *   Add two elements. Drag your `MinionSummonableData` and `SpellEffectSummonableData` ScriptableObjects into these slots.

6.  **Create Example Summoner**:
    *   Create an empty GameObject (e.g., named "Player").
    *   Attach the `ExampleSummoner.cs` script to it.
    *   In its Inspector, drag your `MinionSummonableData` into the `Minion Data` slot and `SpellEffectSummonableData` into the `Spell Effect Data` slot.

7.  **Run the Scene**:
    *   Press Play.
    *   Press `M` to summon minions.
    *   Press `S` to summon spell effects.
    *   Observe minions staying on screen until you manually despawn them (not implemented in this example for minions, but for spell effects).
    *   Observe spell effects appearing and disappearing after 3 seconds, demonstrating pooling. Check the Hierarchy during runtime to see objects being activated/deactivated.

---

### **The Code:**

Here are the complete C# scripts.

#### 1. `ISummonable.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// Interface for any game object that can be "summoned" or pooled by the SummoningSystem.
    /// Components implementing this interface will be managed by the system.
    /// </summary>
    public interface ISummonable
    {
        /// <summary>
        /// Gets the root GameObject of this summonable entity.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Called when the summonable object is activated from the pool and initialized.
        /// This is where the summoned object should set up its state based on the request.
        /// </summary>
        /// <param name="request">The request data that initiated the summon.</param>
        /// <param name="system">Reference to the SummoningSystem for despawning or other interactions.</param>
        void Initialize(SummoningRequest request, SummoningSystem system);

        /// <summary>
        /// Called when the summonable object is returned to the pool (despawned).
        /// This is where the summoned object should reset its state to be ready for reuse.
        /// </summary>
        void OnDespawned();
    }
}
```

#### 2. `SummonableData.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// ScriptableObject defining the properties of a type of summonable entity.
    /// This allows designers to easily create and configure different summonable types
    /// without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSummonableData", menuName = "SummoningSystem/Summonable Data", order = 1)]
    public class SummonableData : ScriptableObject
    {
        [Tooltip("A unique identifier for this summonable type.")]
        public string summonableID;

        [Tooltip("The prefab GameObject that will be instantiated/pooled.")]
        public GameObject prefab;

        [Tooltip("The initial number of objects to pre-populate in the pool for this type.")]
        public int initialPoolSize = 5;

        [Tooltip("If true, the SummoningSystem will automatically despawn this object after a set duration.")]
        public bool canAutoDespawn = false;

        [Tooltip("The default duration in seconds after which this object will be automatically despawned " +
                 "(if canAutoDespawn is true and no duration is specified in SummoningRequest).")]
        public float defaultDespawnDuration = 5f;

        // Validation for unique ID
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(summonableID))
            {
                summonableID = name; // Use asset name as default ID
                Debug.LogWarning($"SummonableData '{name}' has no ID. Defaulting to asset name.", this);
            }
        }
    }
}
```

#### 3. `SummoningRequest.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// Data structure to encapsulate a request for the SummoningSystem to summon an entity.
    /// This makes the 'Summon' method cleaner and more extensible.
    /// </summary>
    public class SummoningRequest
    {
        [Tooltip("The SummonableData defining what type of object to summon.")]
        public SummonableData summonableData;

        [Tooltip("The world position where the object should be spawned.")]
        public Vector3 position;

        [Tooltip("The rotation of the spawned object.")]
        public Quaternion rotation;

        [Tooltip("Optional: The parent Transform for the spawned object.")]
        public Transform parent;

        [Tooltip("Optional: Override for the auto-despawn duration. If null, uses SummonableData's default.")]
        public float? duration;

        [Tooltip("Optional: Custom data/context to pass to the spawned object's Initialize method.")]
        public object context;

        public SummoningRequest(SummonableData data, Vector3 pos, Quaternion rot, Transform parent = null, float? dur = null, object ctx = null)
        {
            summonableData = data;
            position = pos;
            rotation = rot;
            this.parent = parent;
            duration = dur;
            context = ctx;
        }
    }
}
```

#### 4. `PooledObject.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// A helper component attached to any GameObject managed by the SummoningSystem's object pool.
    /// It holds a reference to the SummonableData that originally created it,
    /// allowing the SummoningSystem to quickly identify which pool to return it to.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public SummonableData originData;
    }
}
```

#### 5. `SummoningSystem.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// The core SummoningSystem manager. This is a Singleton MonoBehaviour that handles
    /// pooling, spawning, and despawning of all ISummonable entities.
    /// </summary>
    public class SummoningSystem : MonoBehaviour
    {
        // -----------------------------------------------------------
        // Singleton Implementation
        // Ensures only one instance of SummoningSystem exists and is globally accessible.
        // -----------------------------------------------------------
        public static SummoningSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SummoningSystem instances found! Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePools();
        }

        // -----------------------------------------------------------
        // Configuration
        // -----------------------------------------------------------
        [Tooltip("List of all SummonableData assets this system should manage and pre-pool.")]
        public List<SummonableData> registeredSummonables = new List<SummonableData>();

        // -----------------------------------------------------------
        // Internal Pooling Data Structures
        // -----------------------------------------------------------
        // Dictionary to hold queues of pooled GameObjects for each SummonableData type.
        private Dictionary<SummonableData, Queue<GameObject>> _objectPools = new Dictionary<SummonableData, Queue<GameObject>>();

        // Dictionary to quickly look up the SummonableData for a currently active pooled GameObject.
        // This is used when an object is despawned to know which pool to return it to.
        private Dictionary<GameObject, SummonableData> _activeSummonableMap = new Dictionary<GameObject, SummonableData>();

        // -----------------------------------------------------------
        // Initialization
        // -----------------------------------------------------------
        private void InitializePools()
        {
            foreach (SummonableData data in registeredSummonables)
            {
                CreatePoolForSummonable(data);
            }
        }

        private void CreatePoolForSummonable(SummonableData data)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogError($"SummonableData '{data?.name}' or its prefab is null. Cannot create pool.", data);
                return;
            }

            if (_objectPools.ContainsKey(data))
            {
                Debug.LogWarning($"Pool for '{data.summonableID}' already exists. Skipping re-initialization.", data);
                return;
            }

            Queue<GameObject> pool = new Queue<GameObject>();
            for (int i = 0; i < data.initialPoolSize; i++)
            {
                GameObject obj = Instantiate(data.prefab, transform); // Instantiate as child of SummoningSystem
                obj.name = $"{data.summonableID}_Pooled_{i}";
                obj.SetActive(false); // Objects start inactive
                AttachPooledObjectComponent(obj, data);
                pool.Enqueue(obj);
            }
            _objectPools.Add(data, pool);
            Debug.Log($"Initialized pool for '{data.summonableID}' with {data.initialPoolSize} objects.");
        }

        private void AttachPooledObjectComponent(GameObject obj, SummonableData data)
        {
            PooledObject pooledObjComp = obj.GetComponent<PooledObject>();
            if (pooledObjComp == null)
            {
                pooledObjComp = obj.AddComponent<PooledObject>();
            }
            pooledObjComp.originData = data;
        }

        // -----------------------------------------------------------
        // Public Summoning Methods
        // -----------------------------------------------------------

        /// <summary>
        /// Summons an entity based on the provided request.
        /// </summary>
        /// <param name="request">The SummoningRequest containing all necessary data.</param>
        /// <returns>The summoned GameObject, or null if summoning failed.</returns>
        public GameObject Summon(SummoningRequest request)
        {
            if (request == null || request.summonableData == null)
            {
                Debug.LogError("Invalid SummoningRequest: SummonableData is null.");
                return null;
            }

            SummonableData data = request.summonableData;
            GameObject summonedObject = GetPooledObject(data);

            if (summonedObject == null)
            {
                Debug.LogWarning($"Could not summon '{data.summonableID}'. Check pool configuration or increase initial pool size.", data);
                return null;
            }

            // Set up basic transform properties
            summonedObject.transform.position = request.position;
            summonedObject.transform.rotation = request.rotation;
            summonedObject.transform.parent = request.parent; // Parent can be null for world space

            // Activate the object
            summonedObject.SetActive(true);

            // Notify the ISummonable component
            ISummonable summonableComponent = summonedObject.GetComponent<ISummonable>();
            if (summonableComponent != null)
            {
                summonableComponent.Initialize(request, this);
            }
            else
            {
                Debug.LogWarning($"Summoned object '{summonedObject.name}' does not implement ISummonable. Initialization skipped.", summonedObject);
            }

            // Register in the active map for despawning
            _activeSummonableMap.Add(summonedObject, data);

            // Start auto-despawn timer if configured
            if (data.canAutoDespawn)
            {
                float despawnDuration = request.duration ?? data.defaultDespawnDuration;
                if (despawnDuration > 0)
                {
                    StartCoroutine(AutoDespawnRoutine(summonedObject, despawnDuration));
                }
            }

            return summonedObject;
        }

        /// <summary>
        /// Despawns a GameObject (returns it to its respective pool).
        /// </summary>
        /// <param name="objToDespawn">The GameObject to despawn.</param>
        public void Despawn(GameObject objToDespawn)
        {
            if (objToDespawn == null) return;

            if (!_activeSummonableMap.TryGetValue(objToDespawn, out SummonableData originData))
            {
                Debug.LogWarning($"Attempted to despawn object '{objToDespawn.name}' that was not spawned by SummoningSystem or already despawned.", objToDespawn);
                Destroy(objToDespawn); // Destroy if not managed by the system
                return;
            }

            _activeSummonableMap.Remove(objToDespawn);

            // Notify the ISummonable component before deactivation
            ISummonable summonableComponent = objToDespawn.GetComponent<ISummonable>();
            summonableComponent?.OnDespawned();

            // Reset transform and deactivate
            objToDespawn.transform.SetParent(transform); // Re-parent to SummoningSystem
            objToDespawn.SetActive(false);

            // Return to pool
            if (_objectPools.TryGetValue(originData, out Queue<GameObject> pool))
            {
                pool.Enqueue(objToDespawn);
            }
            else
            {
                // This should theoretically not happen if _activeSummonableMap is consistent
                Debug.LogError($"Pool for SummonableData '{originData.summonableID}' not found during despawn! Destroying object.", objToDespawn);
                Destroy(objToDespawn);
            }
        }

        /// <summary>
        /// Despawns an ISummonable instance (returns its GameObject to its respective pool).
        /// </summary>
        /// <param name="summonable">The ISummonable instance to despawn.</param>
        public void Despawn(ISummonable summonable)
        {
            if (summonable != null)
            {
                Despawn(summonable.GameObject);
            }
        }

        // -----------------------------------------------------------
        // Internal Pooling Logic
        // -----------------------------------------------------------

        private GameObject GetPooledObject(SummonableData data)
        {
            if (!_objectPools.TryGetValue(data, out Queue<GameObject> pool))
            {
                Debug.LogWarning($"Pool for '{data.summonableID}' not found. Creating a new one dynamically.", data);
                CreatePoolForSummonable(data); // Create pool on demand if not pre-registered
                pool = _objectPools[data];
            }

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            else
            {
                // Pool is empty, instantiate a new one (expands the pool)
                GameObject newObj = Instantiate(data.prefab, transform);
                newObj.name = $"{data.summonableID}_Pooled_Expanded";
                AttachPooledObjectComponent(newObj, data);
                Debug.LogWarning($"Pool for '{data.summonableID}' was empty. Instantiated a new object. Consider increasing initial pool size.", data);
                return newObj;
            }
        }

        private IEnumerator AutoDespawnRoutine(GameObject obj, float duration)
        {
            yield return new WaitForSeconds(duration);

            // Only despawn if the object is still active and managed by the system
            // (could have been despawned manually or destroyed before timer ended)
            if (obj != null && obj.activeInHierarchy && _activeSummonableMap.ContainsKey(obj))
            {
                Despawn(obj);
            }
        }
    }
}
```

#### 6. `ExampleSummoner.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// An example script demonstrating how a 'Summoner' (e.g., player, enemy)
    /// would interact with the SummoningSystem to create entities.
    /// </summary>
    public class ExampleSummoner : MonoBehaviour
    {
        [Header("Summonable Data References")]
        [Tooltip("The SummonableData for the minion type to be summoned.")]
        public SummonableData minionData;

        [Tooltip("The SummonableData for the spell effect type to be summoned.")]
        public SummonableData spellEffectData;

        [Header("Summoning Configuration")]
        [Tooltip("Offset from the summoner's position where minions will appear.")]
        public Vector3 minionSummonOffset = new Vector3(0, 0, 2f); // In front of the summoner

        [Tooltip("Offset from the summoner's position where spell effects will appear.")]
        public Vector3 spellEffectSummonOffset = new Vector3(0, 0.5f, 1f); // Slightly above and in front

        [Tooltip("Custom message to pass to summoned minions.")]
        public string minionMessage = "Hello, I am a minion!";

        [Tooltip("Custom value to pass to summoned spell effects.")]
        public float spellEffectPower = 100f;

        void Update()
        {
            // Example: Summon a minion on 'M' key press
            if (Input.GetKeyDown(KeyCode.M))
            {
                SummonMinion();
            }

            // Example: Summon a spell effect on 'S' key press
            if (Input.GetKeyDown(KeyCode.S))
            {
                SummonSpellEffect();
            }
        }

        private void SummonMinion()
        {
            if (minionData == null)
            {
                Debug.LogError("Minion SummonableData is not assigned in ExampleSummoner.", this);
                return;
            }

            if (SummoningSystem.Instance == null)
            {
                Debug.LogError("SummoningSystem.Instance is null. Make sure it's in the scene and active.", this);
                return;
            }

            // Create a SummoningRequest for the minion
            SummoningRequest request = new SummoningRequest(
                summonableData: minionData,
                position: transform.position + transform.TransformDirection(minionSummonOffset),
                rotation: transform.rotation,
                parent: null, // Minions are typically root level or child of a game manager
                duration: null, // Use default from SummonableData (false for minions)
                context: minionMessage // Pass custom data
            );

            GameObject summonedMinion = SummoningSystem.Instance.Summon(request);

            if (summonedMinion != null)
            {
                Debug.Log($"Summoned a {minionData.summonableID} at {summonedMinion.transform.position}.");
            }
        }

        private void SummonSpellEffect()
        {
            if (spellEffectData == null)
            {
                Debug.LogError("Spell Effect SummonableData is not assigned in ExampleSummoner.", this);
                return;
            }

            if (SummoningSystem.Instance == null)
            {
                Debug.LogError("SummoningSystem.Instance is null. Make sure it's in the scene and active.", this);
                return;
            }

            // Create a SummoningRequest for the spell effect
            SummoningRequest request = new SummoningRequest(
                summonableData: spellEffectData,
                position: transform.position + transform.TransformDirection(spellEffectSummonOffset),
                rotation: Quaternion.identity, // Spell effects might not need specific rotation
                parent: null,
                duration: 2.0f, // Override default duration for this specific spell effect
                context: spellEffectPower // Pass custom data
            );

            GameObject summonedEffect = SummoningSystem.Instance.Summon(request);

            if (summonedEffect != null)
            {
                Debug.Log($"Summoned a {spellEffectData.summonableID} at {summonedEffect.transform.position}. It will despawn in {request.duration} seconds.");
            }
        }
    }
}
```

#### 7. `ExampleSummonableMinion.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// An example implementation of an ISummonable for a minion.
    /// This script handles the behavior and lifecycle of a summoned minion.
    /// </summary>
    [RequireComponent(typeof(PooledObject))] // Ensure PooledObject is attached for SummoningSystem
    public class ExampleSummonableMinion : MonoBehaviour, ISummonable
    {
        public GameObject GameObject => gameObject;

        [Header("Minion Properties")]
        public float moveSpeed = 1f;
        public Renderer bodyRenderer; // Assign a renderer to change color

        private SummoningSystem _summoningSystem; // Reference to the system for despawning
        private string _minionGreeting; // Custom data passed during summoning

        /// <summary>
        /// Called when this minion is activated from the pool.
        /// Use the request data to initialize its state.
        /// </summary>
        public void Initialize(SummoningRequest request, SummoningSystem system)
        {
            _summoningSystem = system;

            // Example: Set initial position and rotation (already done by SummoningSystem)
            // Example: Apply specific behaviors based on context
            if (request.context is string greeting)
            {
                _minionGreeting = greeting;
                Debug.Log($"Minion '{gameObject.name}' initialized! Message: {_minionGreeting}");
            }
            else
            {
                _minionGreeting = "No special message.";
                Debug.Log($"Minion '{gameObject.name}' initialized with no specific message.");
            }

            // Example: Change color to indicate it's a new minion
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
            }
        }

        /// <summary>
        /// Called when this minion is returned to the pool (despawned).
        /// Reset its state to be ready for reuse.
        /// </summary>
        public void OnDespawned()
        {
            // Example: Reset any ongoing effects, stop movement, clear references
            Debug.Log($"Minion '{gameObject.name}' despawned. Returning to pool.");
            // For a minion, you might not auto-despawn but rather despawn when it dies.
            // For this example, we're just logging its return to pool.
        }

        void Update()
        {
            // Example: Simple movement for the minion
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

            // Example: If a minion goes too far, despawn it (simulate death/leaving bounds)
            if (transform.position.magnitude > 50f)
            {
                // In a real game, this might be triggered by health reaching zero,
                // or a "destroy" animation completing.
                Debug.Log($"Minion '{gameObject.name}' travelled too far. Despawning.");
                _summoningSystem?.Despawn(this);
            }
        }
    }
}
```

#### 8. `ExampleSummonableSpellEffect.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.SummoningSystem
{
    /// <summary>
    /// An example implementation of an ISummonable for a spell effect.
    /// This script handles the behavior and lifecycle of a summoned spell effect.
    /// </summary>
    [RequireComponent(typeof(PooledObject))] // Ensure PooledObject is attached for SummoningSystem
    public class ExampleSummonableSpellEffect : MonoBehaviour, ISummonable
    {
        public GameObject GameObject => gameObject;

        [Header("Effect Properties")]
        public float rotationSpeed = 100f;
        public Renderer bodyRenderer; // Assign a renderer to change color

        private SummoningSystem _summoningSystem;
        private float _effectPower; // Custom data passed during summoning

        /// <summary>
        /// Called when this spell effect is activated from the pool.
        /// Use the request data to initialize its state.
        /// </summary>
        public void Initialize(SummoningRequest request, SummoningSystem system)
        {
            _summoningSystem = system;

            // Example: Set initial scale, color, or start particle effects
            if (request.context is float powerValue)
            {
                _effectPower = powerValue;
                Debug.Log($"Spell Effect '{gameObject.name}' initialized with power: {_effectPower}");
            }
            else
            {
                _effectPower = 0;
                Debug.Log($"Spell Effect '{gameObject.name}' initialized with no specific power.");
            }

            // Example: Animate color based on effect power
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = Color.Lerp(Color.blue, Color.red, _effectPower / 200f);
            }

            // You might start a particle system here:
            // GetComponent<ParticleSystem>()?.Play();
        }

        /// <summary>
        /// Called when this spell effect is returned to the pool (despawned).
        /// Reset its state to be ready for reuse.
        /// </summary>
        public void OnDespawned()
        {
            // Example: Stop particle systems, reset visual state
            Debug.Log($"Spell Effect '{gameObject.name}' despawned. Returning to pool.");
            // GetComponent<ParticleSystem>()?.Stop();
        }

        void Update()
        {
            // Example: Simple visual effect, rotate the object
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
```