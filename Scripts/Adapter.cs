// Unity Design Pattern Example: Adapter
// This script demonstrates the Adapter pattern in Unity
// Generated automatically - ready to use in your Unity project

The Adapter design pattern allows objects with incompatible interfaces to collaborate. It acts as a wrapper between two objects, catching calls for one object and transforming them into a format recognizable by the second.

**Real-world Use Case in Unity:**
Imagine you have a legacy enemy AI system (`LegacyEnemyAI`) that has a method like `ReceiveDamage(int hitPoints)`. Now, you're building a new, more robust damage system that uses an `IDamageable` interface with a method `ApplyDamage(float amount, DamageType type)`. You don't want to rewrite the entire `LegacyEnemyAI`, but you need your new damage system to be able to interact with it.

This is where the Adapter pattern comes in. We create a `LegacyEnemyAIAdapter` that implements `IDamageable`. When `ApplyDamage` is called on the adapter, it translates the new interface's parameters (`float amount`, `DamageType type`) into the old interface's parameters (`int hitPoints`) and calls `ReceiveDamage` on the `LegacyEnemyAI` component.

---

### `AdapterPatternExample.cs`

Save this complete script as `AdapterPatternExample.cs` in your Unity project.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for List

namespace DesignPatterns.Adapter
{
    // =========================================================================
    // 1. Target Interface: The new interface your client code expects.
    //    This is the interface we want our legacy system to conform to.
    // =========================================================================

    /// <summary>
    /// Represents different types of damage that can be applied.
    /// </summary>
    public enum DamageType { Physical, Magical, Fire, Ice, Pure }

    /// <summary>
    /// The 'Target' interface that the client code (e.g., your new weapon system)
    /// expects to interact with.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies damage to the object.
        /// </summary>
        /// <param name="amount">The base amount of damage.</param>
        /// <param name="type">The type of damage.</param>
        void ApplyDamage(float amount, DamageType type);
    }

    // =========================================================================
    // 2. Adaptee: The existing (legacy or third-party) class with an incompatible interface.
    //    This is the system we cannot (or do not want to) modify directly.
    // =========================================================================

    /// <summary>
    /// A legacy enemy AI component that only understands damage in integer hit points.
    /// This represents an existing system that our new damage system needs to interact with.
    /// We assume we cannot modify this class directly.
    /// </summary>
    public class LegacyEnemyAI : MonoBehaviour
    {
        [Tooltip("Current health of the legacy enemy.")]
        public int Health = 100;
        [Tooltip("Optional: Visual feedback for damage taken.")]
        public GameObject HitEffectPrefab;

        /// <summary>
        /// The legacy method for receiving damage, expecting an integer.
        /// </summary>
        /// <param name="hitPoints">The integer amount of damage to subtract from health.</param>
        public void ReceiveDamage(int hitPoints)
        {
            Health -= hitPoints;
            Debug.Log($"<color=orange>[Adaptee: LegacyEnemyAI]</color> took <color=red>{hitPoints}</color> damage. Current Health: {Health}");

            // Simulate some legacy effect
            if (HitEffectPrefab != null)
            {
                Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
            }

            if (Health <= 0)
            {
                Debug.Log($"<color=red>Legacy Enemy '{gameObject.name}' destroyed!</color>");
                // For demonstration, disable or destroy
                Destroy(gameObject); 
            }
        }
    }

    // =========================================================================
    // 3. Adapter: Implements the Target interface and holds a reference to the Adaptee.
    //    It translates calls from the Target interface to the Adaptee's interface.
    // =========================================================================

    /// <summary>
    /// The 'Adapter' component. It implements the new <see cref="IDamageable"/> interface
    /// and wraps an instance of the <see cref="LegacyEnemyAI"/> (the Adaptee).
    /// Its responsibility is to translate calls from the new interface to the old one.
    /// </summary>
    public class LegacyEnemyAIAdapter : MonoBehaviour, IDamageable
    {
        [Tooltip("Reference to the LegacyEnemyAI component this adapter will wrap.")]
        [SerializeField] private LegacyEnemyAI _legacyEnemy; // Reference to the adaptee

        private void Awake()
        {
            // Ensure the adapter has a reference to the adaptee.
            // If not set in the Inspector, try to find it on the same GameObject.
            if (_legacyEnemy == null)
            {
                _legacyEnemy = GetComponent<LegacyEnemyAI>();
                if (_legacyEnemy == null)
                {
                    Debug.LogError($"<color=red>[Adapter: LegacyEnemyAIAdapter]</color> requires a LegacyEnemyAI component to function. Disabling.", this);
                    enabled = false; // Disable if no adaptee is found
                }
            }
        }

        /// <summary>
        /// Implements the <see cref="IDamageable"/> interface.
        /// This method is called by client code expecting the new interface.
        /// </summary>
        /// <param name="amount">The base amount of damage (float).</param>
        /// <param name="type">The type of damage.</param>
        public void ApplyDamage(float amount, DamageType type)
        {
            if (_legacyEnemy == null || !enabled)
            {
                Debug.LogWarning($"<color=red>[Adapter: LegacyEnemyAIAdapter]</color> is not active or has no LegacyEnemyAI reference.", this);
                return;
            }

            Debug.Log($"<color=blue>[Adapter: LegacyEnemyAIAdapter]</color> received <color=green>{amount} ({type})</color> damage call.");

            // --- Adapter's core logic: Translating the new interface call to the old one ---
            // The LegacyEnemyAI only understands integer hit points, not float amount or damage type.
            // We convert the float amount to an integer.
            int hitPoints = Mathf.RoundToInt(amount);

            // We can also apply type-specific logic here before passing to the legacy system.
            // For example, if 'Fire' damage should be more effective on this legacy enemy:
            if (type == DamageType.Fire)
            {
                hitPoints += 5; // Add bonus fire damage for this legacy enemy
                Debug.Log($"<color=blue>[Adapter: LegacyEnemyAIAdapter]</color> applied +5 bonus damage for Fire type. Total adapted hit points: {hitPoints}.");
            }
            // Or if Magical damage should be reduced:
            if (type == DamageType.Magical)
            {
                hitPoints = Mathf.Max(1, hitPoints - 3); // Reduce magic damage, ensure at least 1 damage
                Debug.Log($"<color=blue>[Adapter: LegacyEnemyAIAdapter]</color> applied -3 reduction for Magical type. Total adapted hit points: {hitPoints}.");
            }


            // Finally, call the Adaptee's method with the translated parameters.
            Debug.Log($"<color=blue>[Adapter: LegacyEnemyAIAdapter]</color> translating <color=green>{amount} ({type})</color> damage to <color=red>{hitPoints}</color> hit points for LegacyEnemyAI.");
            _legacyEnemy.ReceiveDamage(hitPoints);
        }
    }
    
    // =========================================================================
    // Optional: A 'New' component that natively implements the IDamageable interface
    //           to show how the client interacts with both new and adapted objects seamlessly.
    // =========================================================================

    /// <summary>
    /// A new enemy component that natively implements the IDamageable interface.
    /// This shows that the client can interact with both legacy (via adapter)
    /// and new systems using the same interface.
    /// </summary>
    public class NewEnemyDamageable : MonoBehaviour, IDamageable
    {
        [Tooltip("Current health of the new enemy.")]
        public float CurrentHealth = 150f;
        [Tooltip("Optional: Visual feedback for damage taken.")]
        public GameObject ExplodeEffectPrefab;

        /// <summary>
        /// Natively applies damage according to the new IDamageable interface.
        /// </summary>
        public void ApplyDamage(float amount, DamageType type)
        {
            CurrentHealth -= amount;
            Debug.Log($"<color=green>[NewEnemyDamageable]</color> took <color=red>{amount} ({type})</color> damage. Current Health: {CurrentHealth}");

            if (ExplodeEffectPrefab != null && type == DamageType.Fire)
            {
                // Special effect for fire damage
                Instantiate(ExplodeEffectPrefab, transform.position, Quaternion.identity);
            }
            
            if (CurrentHealth <= 0)
            {
                Debug.Log($"<color=red>New Enemy '{gameObject.name}' destroyed!</color>");
                Destroy(gameObject);
            }
        }
    }


    // =========================================================================
    // 4. Client: The code that uses the Target interface.
    //    It doesn't know (or care) if it's dealing with an Adapter or a native implementation.
    // =========================================================================

    /// <summary>
    /// The 'Client' MonoBehaviour that demonstrates using the <see cref="IDamageable"/> interface.
    /// It interacts with objects through this common interface, unaware if they are
    /// natively <see cref="IDamageable"/> or if an <see cref="LegacyEnemyAIAdapter"/> is used.
    /// </summary>
    public class AdapterPatternExample : MonoBehaviour
    {
        [Tooltip("List of GameObjects that the client will try to damage.")]
        [SerializeField] private List<GameObject> _targetObjects;

        void Start()
        {
            Debug.Log("--- Adapter Pattern Demonstration Start ---");
            Debug.Log("The client code (this script) will attempt to damage various objects via the IDamageable interface.");
            Debug.Log("It does not know if the target is a legacy system (via Adapter) or a new system (native IDamageable).");
            Debug.Log("-------------------------------------------");

            // Define some damage payloads
            var damagePayloads = new (float amount, DamageType type)[]
            {
                (20.0f, DamageType.Physical),
                (15.0f, DamageType.Magical),
                (10.0f, DamageType.Fire)
            };
            int payloadIndex = 0;

            foreach (var targetObject in _targetObjects)
            {
                if (targetObject == null)
                {
                    Debug.LogWarning("One of the target objects in the list is null. Skipping.");
                    continue;
                }

                // The client requests the IDamageable interface.
                IDamageable damageable = targetObject.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    Debug.Log($"\n<color=cyan>Client:</color> Found IDamageable on '{targetObject.name}'. Applying damage...");

                    // Get the next damage payload in a循环 fashion
                    var currentDamage = damagePayloads[payloadIndex % damagePayloads.Length];
                    payloadIndex++;

                    // The client simply calls ApplyDamage, completely unaware of the underlying implementation
                    // (LegacyEnemyAI via adapter, or NewEnemyDamageable natively).
                    damageable.ApplyDamage(currentDamage.amount, currentDamage.type);
                }
                else
                {
                    Debug.LogWarning($"<color=cyan>Client:</color> Object '{targetObject.name}' does not implement IDamageable. Cannot apply damage.");
                }
            }

            Debug.Log("\n--- Adapter Pattern Demonstration Complete ---");
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create a New C# Script:** In your Unity project, create a new C# script named `AdapterPatternExample.cs` and copy-paste the entire code above into it.
2.  **Create a Legacy Enemy:**
    *   Right-click in the Hierarchy -> `Create Empty`. Name it `LegacyEnemy_AdapterDemo`.
    *   Select `LegacyEnemy_AdapterDemo`. In the Inspector, click `Add Component`.
    *   Search for `Legacy Enemy AI` and add it.
    *   Search for `Legacy Enemy AI Adapter` and add it.
    *   (Optional): In `LegacyEnemyAI`, drag any simple `GameObject` prefab (e.g., a Cube or Sphere scaled down) into the `Hit Effect Prefab` slot to see a visual.
    *   **Crucially**: The `Legacy Enemy AI Adapter` automatically tries to find the `LegacyEnemyAI` on the same GameObject in its `Awake()` method. If you place them on separate GameObjects, you'd drag the `LegacyEnemyAI` GameObject into the `_legacyEnemy` slot on the adapter. For this example, having them on the same GameObject works out of the box.
3.  **Create a New Enemy:**
    *   Right-click in the Hierarchy -> `Create Empty`. Name it `NewEnemy_NativeIDamageable`.
    *   Select `NewEnemy_NativeIDamageable`. In the Inspector, click `Add Component`.
    *   Search for `New Enemy Damageable` and add it.
    *   (Optional): In `NewEnemyDamageable`, drag any simple `GameObject` prefab (e.g., a Cube or Sphere scaled down) into the `Explode Effect Prefab` slot.
4.  **Create the Client:**
    *   Right-click in the Hierarchy -> `Create Empty`. Name it `DamageSystemClient`.
    *   Select `DamageSystemClient`. In the Inspector, click `Add Component`.
    *   Search for `Adapter Pattern Example` and add it.
    *   In the Inspector for `Adapter Pattern Example`, you'll see a `_targetObjects` list. Increase its size to `2`.
    *   Drag `LegacyEnemy_AdapterDemo` from the Hierarchy into the first slot (`Element 0`).
    *   Drag `NewEnemy_NativeIDamageable` from the Hierarchy into the second slot (`Element 1`).
5.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   Observe the Console output. You will see how the `AdapterPatternExample` client uniformly applies damage to both the `LegacyEnemy_AdapterDemo` (via the adapter) and `NewEnemy_NativeIDamageable` (natively) using the `IDamageable` interface. The adapter translates the damage calls for the legacy system, including type-specific adjustments.

This setup clearly demonstrates how the `IDamageable` interface allows the `AdapterPatternExample` (Client) to interact seamlessly with both the `NewEnemyDamageable` (which natively implements `IDamageable`) and the `LegacyEnemyAI` (which becomes compatible via the `LegacyEnemyAIAdapter`).