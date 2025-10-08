// Unity Design Pattern Example: LavaFlowSystem
// This script demonstrates the LavaFlowSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'LavaFlowSystem' isn't a universally recognized Gang of Four design pattern, but its name evokes a common game development strategy: a system that dynamically generates, spreads, or modifies features (like components or effects) across game objects or regions, often based on proximity, time, or state changes. It's about a central "source" that 'flows' functionality or state to 'receptive' targets.

In Unity, this often translates to:
1.  **Dynamic Component Injection:** A core system adds `MonoBehaviour` components to existing `GameObject`s at runtime, changing their behavior.
2.  **State Propagation:** A state (e.g., "corrupted," "powered-up," "infected") spreads from one object to others, activating specific behaviors.
3.  **Area-of-Effect with Lasting Changes:** Unlike a simple one-shot effect, the "flow" applies persistent or evolving changes to objects within its range.

---

### **LavaFlowSystem Design Pattern Example: "Corruption Spread System"**

This example demonstrates a "Corruption Spread System" where a central `LavaFlowSource` (the corruption core) spreads a "corruption" status to nearby `FlowableObject`s. When an object becomes corrupted, the `LavaFlowSource` dynamically *adds new `MonoBehaviour` components* to that object, granting it new behaviors (e.g., taking damage, showing visual effects). When the object leaves the corruption's range, these components are *removed*.

**Core Components:**

1.  **`LavaFlowSource.cs`**: The "source" of the flow. It defines the radius, spread rate, and *which components to inject* into affected objects. It continuously scans for `IFlowable` objects and manages the addition/removal of components.
2.  **`IFlowable.cs`**: An interface for objects that can be affected by the flow. It defines callbacks for when flow starts (`OnFlowAffected`) and stops (`OnFlowCeased`).
3.  **`FlowableObject.cs`**: A concrete `MonoBehaviour` implementation of `IFlowable`. It reacts to the flow by changing its material or performing other actions.
4.  **Injectable Components (e.g., `CorruptedDamageComponent.cs`, `CorruptedVisualEffect.cs`):** These are the `MonoBehaviour` scripts that the `LavaFlowSource` dynamically adds to `FlowableObject`s. They define the specific "corrupted" behaviors.

---

### **1. `IFlowable.cs` (Interface Definition)**

This interface defines the contract for any GameObject that can be affected by a `LavaFlowSource`.

```csharp
using UnityEngine;

namespace LavaFlowSystem
{
    /// <summary>
    /// Interface for objects that can be affected by a LavaFlowSource.
    /// Objects implementing this interface can react when a flow starts or ceases to affect them.
    /// </summary>
    public interface IFlowable
    {
        /// <summary>
        /// Called when a LavaFlowSource starts to affect this object.
        /// </summary>
        /// <param name="source">The LavaFlowSource that is now affecting this object.</param>
        void OnFlowAffected(LavaFlowSource source);

        /// <summary>
        /// Called when a LavaFlowSource ceases to affect this object.
        /// </summary>
        /// <param name="source">The LavaFlowSource that has stopped affecting this object.</param>
        void OnFlowCeased(LavaFlowSource source);
    }
}
```

### **2. `LavaFlowSource.cs` (The Core System)**

This script is the heart of the LavaFlowSystem. It represents the origin of the flow (e.g., a "corruption core"). It scans for `IFlowable` objects within its radius and dynamically adds or removes specified `MonoBehaviour` components to them.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LavaFlowSystem
{
    /// <summary>
    /// Represents a source of 'Lava Flow' that dynamically adds or removes components
    /// to 'IFlowable' GameObjects within its influence radius.
    /// This is the central piece of the LavaFlowSystem pattern demonstration.
    /// </summary>
    [DisallowMultipleComponent]
    public class LavaFlowSource : MonoBehaviour
    {
        [Header("Flow Settings")]
        [Tooltip("The radius within which this source affects Flowable objects.")]
        [SerializeField] private float spreadRadius = 10f;

        [Tooltip("How often (in seconds) the source checks for Flowable objects.")]
        [SerializeField] private float spreadCheckInterval = 1.0f;

        [Tooltip("LayerMask to filter which colliders are checked for IFlowable objects.")]
        [SerializeField] private LayerMask flowableLayers;

        [Header("Components to Inject")]
        [Tooltip("List of MonoBehaviour component type names (e.g., 'LavaFlowSystem.CorruptedDamageComponent') " +
                 "to dynamically add to Flowable objects when they are affected. " +
                 "Note: Using strings for Type.GetType() can be fragile. In a production environment, " +
                 "consider using ScriptableObjects, MonoScript references, or a custom editor for System.Type " +
                 "for more robust type management.")]
        [SerializeField] private List<string> componentTypeNamesToInject;

        // Internal tracking for currently affected objects and the components injected into them.
        private readonly Dictionary<IFlowable, List<MonoBehaviour>> _injectedComponentsTracker = new Dictionary<IFlowable, List<MonoBehaviour>>();
        private readonly HashSet<IFlowable> _currentlyAffectedFlowables = new HashSet<IFlowable>();
        private List<Type> _cachedComponentTypes; // To store resolved Types once for performance.

        private void OnEnable()
        {
            // Cache component types once to avoid repeated Type.GetType() calls.
            CacheComponentTypes();
            
            // Start the periodic check for flowable objects.
            InvokeRepeating(nameof(CheckAndApplyFlow), 0f, spreadCheckInterval);
        }

        private void OnDisable()
        {
            // Stop all ongoing flows and clean up injected components.
            CancelInvoke(nameof(CheckAndApplyFlow));
            StopAllFlows();
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize the spread radius in the editor.
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spreadRadius);
        }

        /// <summary>
        /// Resolves component type names to actual System.Type objects.
        /// </summary>
        private void CacheComponentTypes()
        {
            _cachedComponentTypes = new List<Type>();
            foreach (string typeName in componentTypeNamesToInject)
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    Debug.LogWarning($"LavaFlowSource: Could not find type '{typeName}'. Make sure the full namespace and assembly name are correct (e.g., 'MyNamespace.MyComponent, Assembly-CSharp').");
                    continue;
                }
                if (!type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    Debug.LogWarning($"LavaFlowSource: Type '{typeName}' is not a MonoBehaviour and cannot be injected.");
                    continue;
                }
                _cachedComponentTypes.Add(type);
            }
        }

        /// <summary>
        /// This is the core logic that periodically checks for flowable objects
        /// and applies/removes the flow (i.e., component injection/removal).
        /// </summary>
        private void CheckAndApplyFlow()
        {
            // 1. Find all potential flowable objects within the radius.
            Collider[] colliders = Physics.OverlapSphere(transform.position, spreadRadius, flowableLayers);
            HashSet<IFlowable> currentlyInRadius = new HashSet<IFlowable>();

            foreach (Collider col in colliders)
            {
                IFlowable flowable = col.GetComponentInParent<IFlowable>();
                if (flowable != null && (flowable as MonoBehaviour) != null) // Ensure it's a MonoBehaviour that implements IFlowable
                {
                    currentlyInRadius.Add(flowable);
                }
            }

            // 2. Identify objects that are no longer in the radius (flow ceased).
            // Use ToList() to avoid modifying collection while iterating.
            foreach (IFlowable flowable in _currentlyAffectedFlowables.Except(currentlyInRadius).ToList())
            {
                StopFlow(flowable);
            }

            // 3. Identify objects that are newly in the radius or still in the radius (flow applied/continued).
            foreach (IFlowable flowable in currentlyInRadius)
            {
                if (!_currentlyAffectedFlowables.Contains(flowable))
                {
                    ApplyFlow(flowable);
                }
            }

            // 4. Update the set of currently affected objects.
            _currentlyAffectedFlowables.Clear();
            foreach (IFlowable flowable in currentlyInRadius)
            {
                _currentlyAffectedFlowables.Add(flowable);
            }
        }

        /// <summary>
        /// Applies the flow to a specific IFlowable object by injecting components.
        /// </summary>
        /// <param name="flowable">The object to apply the flow to.</param>
        private void ApplyFlow(IFlowable flowable)
        {
            MonoBehaviour flowableMB = flowable as MonoBehaviour;
            if (flowableMB == null || flowableMB.gameObject == null)
            {
                Debug.LogWarning($"LavaFlowSource: Attempted to apply flow to a null or destroyed FlowableObject.");
                return;
            }

            Debug.Log($"LavaFlowSource: Applying flow to {flowableMB.name}. Injecting components...");

            List<MonoBehaviour> injected = new List<MonoBehaviour>();
            foreach (Type componentType in _cachedComponentTypes)
            {
                // Check if the component already exists to avoid duplicates.
                if (flowableMB.gameObject.GetComponent(componentType) == null)
                {
                    MonoBehaviour newComponent = flowableMB.gameObject.AddComponent(componentType) as MonoBehaviour;
                    if (newComponent != null)
                    {
                        injected.Add(newComponent);
                        Debug.Log($"  - Injected: {newComponent.GetType().Name}");
                    }
                    else
                    {
                        Debug.LogError($"  - Failed to inject component of type {componentType.Name} onto {flowableMB.name}.");
                    }
                }
                else
                {
                    Debug.Log($"  - Component {componentType.Name} already exists on {flowableMB.name}. Skipping injection.");
                }
            }

            _injectedComponentsTracker[flowable] = injected;
            flowable.OnFlowAffected(this); // Notify the flowable object.
        }

        /// <summary>
        /// Stops the flow for a specific IFlowable object by removing injected components.
        /// </summary>
        /// <param name="flowable">The object to stop the flow for.</param>
        private void StopFlow(IFlowable flowable)
        {
            MonoBehaviour flowableMB = flowable as MonoBehaviour;
            if (flowableMB == null || flowableMB.gameObject == null)
            {
                Debug.LogWarning($"LavaFlowSource: Attempted to stop flow for a null or destroyed FlowableObject.");
                // If the object is destroyed, it won't be in the tracker anyway, or Destroy will handle it.
                // Just remove from tracking.
                _injectedComponentsTracker.Remove(flowable);
                return;
            }

            Debug.Log($"LavaFlowSource: Stopping flow for {flowableMB.name}. Removing components...");

            if (_injectedComponentsTracker.TryGetValue(flowable, out List<MonoBehaviour> injected))
            {
                foreach (MonoBehaviour component in injected)
                {
                    if (component != null) // Ensure component wasn't already destroyed
                    {
                        Destroy(component);
                        Debug.Log($"  - Removed: {component.GetType().Name}");
                    }
                }
                _injectedComponentsTracker.Remove(flowable);
            }
            flowable.OnFlowCeased(this); // Notify the flowable object.
        }

        /// <summary>
        /// Stops the flow for all currently affected objects. Called on OnDisable/OnDestroy.
        /// </summary>
        private void StopAllFlows()
        {
            // Use ToList() to iterate over a copy, as StopFlow modifies _injectedComponentsTracker.
            foreach (IFlowable flowable in _injectedComponentsTracker.Keys.ToList())
            {
                StopFlow(flowable);
            }
            _currentlyAffectedFlowables.Clear();
        }
    }
}
```

### **3. `FlowableObject.cs` (An Object That Can Be Affected)**

This script implements `IFlowable` and represents an object that can be "corrupted" by the `LavaFlowSource`. It changes its visual appearance based on whether it's affected.

```csharp
using UnityEngine;

namespace LavaFlowSystem
{
    /// <summary>
    /// An example MonoBehaviour that implements IFlowable.
    /// It changes its material to visually indicate whether it's affected by a LavaFlowSource.
    /// This object must have a Collider component to be detected by Physics.OverlapSphere.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Ensure a collider exists for detection
    public class FlowableObject : MonoBehaviour, IFlowable
    {
        [SerializeField] private Renderer objectRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material affectedMaterial;

        private void Start()
        {
            if (objectRenderer == null)
            {
                objectRenderer = GetComponent<Renderer>();
                if (objectRenderer == null)
                {
                    Debug.LogError($"FlowableObject '{name}' requires a Renderer component.", this);
                    enabled = false;
                    return;
                }
            }
            // Ensure initial state is normal
            if (normalMaterial != null)
            {
                objectRenderer.material = normalMaterial;
            }
            else
            {
                Debug.LogWarning($"FlowableObject '{name}' is missing a Normal Material. Visual feedback may be limited.", this);
            }
        }

        /// <summary>
        /// Callback when a LavaFlowSource starts affecting this object.
        /// Changes the object's material to indicate corruption.
        /// </summary>
        /// <param name="source">The LavaFlowSource that affected this object.</param>
        public void OnFlowAffected(LavaFlowSource source)
        {
            Debug.Log($"{name} is now affected by Lava Flow from {source.name}!");
            if (affectedMaterial != null)
            {
                objectRenderer.material = affectedMaterial;
            }
        }

        /// <summary>
        /// Callback when a LavaFlowSource stops affecting this object.
        /// Reverts the object's material to its normal state.
        /// </summary>
        /// <param name="source">The LavaFlowSource that stopped affecting this object.</param>
        public void OnFlowCeased(LavaFlowSource source)
        {
            Debug.Log($"{name} is no longer affected by Lava Flow from {source.name}.");
            if (normalMaterial != null)
            {
                objectRenderer.material = normalMaterial;
            }
        }

        // It's good practice to handle potential cleanup if the FlowableObject is destroyed
        // before the LavaFlowSource gets a chance to call OnFlowCeased.
        private void OnDestroy()
        {
            // In a more complex system, you might want to notify all active LavaFlowSources
            // that this object is being destroyed, so they can clean up their trackers.
            // For this example, LavaFlowSource's periodic check handles objects disappearing.
        }
    }
}
```

### **4. `CorruptedDamageComponent.cs` (Injectable Behavior)**

This is an example `MonoBehaviour` that will be dynamically added to `FlowableObject`s when they are "corrupted."

```csharp
using UnityEngine;

namespace LavaFlowSystem
{
    /// <summary>
    /// An example MonoBehaviour component dynamically added by LavaFlowSource.
    /// Simulates damage over time when attached to a GameObject.
    /// </summary>
    public class CorruptedDamageComponent : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 5f;
        [SerializeField] private float damageInterval = 1f;

        private float _timer;

        private void OnEnable()
        {
            Debug.Log($"{gameObject.name}: CorruptedDamageComponent enabled. Taking {damagePerSecond} DPS!");
            _timer = 0f;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= damageInterval)
            {
                Debug.LogWarning($"{gameObject.name}: Taking {damagePerSecond * damageInterval} corruption damage!");
                // In a real game, this would interface with a Health component.
                _timer -= damageInterval;
            }
        }

        private void OnDisable()
        {
            Debug.Log($"{gameObject.name}: CorruptedDamageComponent disabled. No longer taking damage.");
        }
    }
}
```

### **5. `CorruptedVisualEffect.cs` (Injectable Behavior)**

Another example `MonoBehaviour` for visual feedback when corrupted.

```csharp
using UnityEngine;

namespace LavaFlowSystem
{
    /// <summary>
    /// An example MonoBehaviour component dynamically added by LavaFlowSource.
    /// Plays a particle system effect when attached to a GameObject.
    /// </summary>
    public class CorruptedVisualEffect : MonoBehaviour
    {
        [SerializeField] private GameObject corruptionEffectPrefab;
        private GameObject _instantiatedEffect;

        private void OnEnable()
        {
            if (corruptionEffectPrefab != null)
            {
                _instantiatedEffect = Instantiate(corruptionEffectPrefab, transform.position, Quaternion.identity, transform);
                Debug.Log($"{gameObject.name}: CorruptedVisualEffect enabled. Playing corruption effect.");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: CorruptedVisualEffect is missing its effect prefab!", this);
            }
        }

        private void OnDisable()
        {
            if (_instantiatedEffect != null)
            {
                Destroy(_instantiatedEffect);
                Debug.Log($"{gameObject.name}: CorruptedVisualEffect disabled. Stopping corruption effect.");
            }
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create C# Scripts:**
    *   Create a folder named `LavaFlowSystem` under `Assets`.
    *   Create the five C# scripts listed above (`IFlowable.cs`, `LavaFlowSource.cs`, `FlowableObject.cs`, `CorruptedDamageComponent.cs`, `CorruptedVisualEffect.cs`) and place them in the `LavaFlowSystem` folder. Ensure the namespace `LavaFlowSystem` is correctly used in all scripts.

2.  **Create Materials:**
    *   Right-click in Project window -> `Create` -> `Material`.
    *   Name one `NormalMaterial` (e.g., set Base Map to white).
    *   Name another `AffectedMaterial` (e.g., set Base Map to red or purple).

3.  **Create a Particle System Prefab (for `CorruptedVisualEffect`):**
    *   Right-click in Hierarchy -> `Effects` -> `Particle System`.
    *   Customize it to look like a "corruption" effect (e.g., dark, smoky, reddish particles).
    *   Drag this Particle System GameObject from the Hierarchy into your `Assets` folder to create a prefab (e.g., `CorruptionParticles`).

4.  **Create a `FlowableObject` Prefab:**
    *   Right-click in Hierarchy -> `3D Object` -> `Cube`.
    *   Rename it `FlowableCube`.
    *   Add the `FlowableObject.cs` script to it.
    *   Drag the `NormalMaterial` to its `Normal Material` slot and `AffectedMaterial` to its `Affected Material` slot in the Inspector.
    *   Make sure it has a `Box Collider` (Unity adds one to a Cube by default).
    *   Set its `Layer` (e.g., create a new Layer called "Flowable" and assign it).
    *   Drag `FlowableCube` from the Hierarchy into your `Assets` folder to create a prefab. Delete the instance from the Hierarchy.

5.  **Create the `LavaFlowSource` GameObject:**
    *   Right-click in Hierarchy -> `Create Empty`.
    *   Rename it `CorruptionCore`.
    *   Add the `LavaFlowSource.cs` script to it.
    *   **Configure `LavaFlowSource` in the Inspector:**
        *   `Spread Radius`: Set to `10` (or desired range).
        *   `Spread Check Interval`: Set to `0.5` (or desired frequency).
        *   `Flowable Layers`: Select the "Flowable" layer you created.
        *   **`Component Type Names To Inject`:**
            *   Click the `+` to add two elements.
            *   For Element 0, type: `LavaFlowSystem.CorruptedDamageComponent, Assembly-CSharp`
            *   For Element 1, type: `LavaFlowSystem.CorruptedVisualEffect, Assembly-CSharp`
            *   *(Note: The `, Assembly-CSharp` part is often necessary for `Type.GetType()` to find types defined in your project's main script assembly. If your scripts are in a different assembly, you'll need its name.)*

6.  **Configure `CorruptedVisualEffect` Prefab:**
    *   Select `CorruptedVisualEffect.cs` in the Project window.
    *   Drag your `CorruptionParticles` prefab into the `Corruption Effect Prefab` slot of the script asset. This sets the default for any dynamically added `CorruptedVisualEffect` components.

7.  **Populate the Scene:**
    *   Drag multiple `FlowableCube` prefabs into your scene, placing some inside and some outside the `CorruptionCore`'s radius.
    *   Run the game!

---

### **Expected Behavior:**

*   `FlowableCube`s within the `CorruptionCore`'s `Spread Radius` will change to the `AffectedMaterial`.
*   These affected `FlowableCube`s will dynamically gain `CorruptedDamageComponent` and `CorruptedVisualEffect` scripts.
*   You'll see particle effects playing on them, and the console will log damage messages.
*   When a `FlowableCube` moves out of the `CorruptionCore`'s radius, its material will revert, and the injected components will be destroyed.
*   When a `FlowableCube` moves back into the radius, the process repeats.

This example provides a clear and practical demonstration of the 'LavaFlowSystem' pattern by dynamically altering GameObject behavior and appearance based on a spreading influence.