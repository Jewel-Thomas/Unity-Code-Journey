// Unity Design Pattern Example: AuraEffectSystem
// This script demonstrates the AuraEffectSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The AuraEffectSystem is a common pattern in games, especially RPGs and MOBAs, where entities emit an area-of-effect (AoE) influence that applies various status effects to other entities within its radius. This example provides a complete, practical, and educational implementation in C# for Unity.

**Core Components of the AuraEffectSystem:**

1.  **`AuraEffectConfig` (ScriptableObject):** Defines the *blueprint* for an effect (what it does, its potency, duration).
2.  **`IAuraTarget` (Interface):** Defines the *contract* for any GameObject that can be affected by an aura.
3.  **`AuraTargetComponent` (MonoBehaviour):** Implements `IAuraTarget`, manages the target entity's stats, and actively applies/reverts effects based on their configuration and the aura's state.
4.  **`AuraSource` (MonoBehaviour):** The *emitter* of the aura. It detects targets, tells them when they enter/exit its range, and periodically instructs them to "tick" for ongoing effects.

---

### 1. `AuraEffectConfig.cs`

This ScriptableObject defines the properties of a single aura effect. You can create multiple instances of this asset in your project to represent different effects (e.g., Poison, Heal, Speed Buff).

```csharp
// AuraEffectSystem/AuraEffectConfig.cs
using UnityEngine;
using System.Collections.Generic;

namespace AuraEffectSystem
{
    /// <summary>
    /// Enum to categorize different types of effects.
    /// This allows the AuraTargetComponent to apply specific logic based on the effect type.
    /// </summary>
    public enum EffectType
    {
        None,
        DamageOverTime,      // Periodically deals damage.
        HealOverTime,        // Periodically heals.
        MovementSpeedBuff,   // Increases movement speed.
        MovementSpeedDebuff, // Decreases movement speed.
        // Add more effect types as needed (e.g., AttackBuff, DefenseDebuff, Stun, etc.)
    }

    /// <summary>
    /// ScriptableObject that defines the blueprint for an aura effect.
    /// This allows designers to create and configure different effects without writing code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAuraEffect", menuName = "Aura System/Aura Effect Configuration", order = 1)]
    public class AuraEffectConfig : ScriptableObject
    {
        [Header("Effect Identification")]
        public string effectName = "New Aura Effect";
        [TextArea]
        public string description = "A general description of this aura effect.";

        [Header("Effect Properties")]
        public EffectType effectType = EffectType.None;
        [Tooltip("e.g., damage per tick, heal per tick, speed modifier.")]
        public float potency = 1f; 
        
        [Tooltip("How often the effect is applied while target is in aura. 0 for continuous (only on enter/exit changes), >0 for periodic ticks.")]
        public float tickInterval = 1f; 

        [Tooltip("How long the effect persists AFTER the target leaves the aura radius. 0 for immediate removal.")]
        public float durationOnExit = 0f; 

        [Tooltip("Can multiple instances of this exact effect (from different sources or even the same source if logic allows) stack on the same target?")]
        public bool canStack = false; 

        public override string ToString()
        {
            return $"{effectName} ({effectType}, Potency: {potency})";
        }
    }
}
```

---

### 2. `IAuraTarget.cs`

This interface defines the methods that any GameObject capable of being affected by an aura must implement. It promotes loose coupling between the aura source and its potential targets.

```csharp
// AuraEffectSystem/IAuraTarget.cs
using UnityEngine;
using System.Collections.Generic;

namespace AuraEffectSystem
{
    /// <summary>
    /// Interface for any GameObject that can be affected by an aura.
    /// This allows AuraSource to interact with targets generically.
    /// </summary>
    public interface IAuraTarget
    {
        /// <summary>
        /// Reference to the GameObject this target script is attached to.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Called when this target enters an aura's radius.
        /// The source is the AuraSource component, and configs are the effects it applies.
        /// </summary>
        void OnAuraEnter(AuraSource source, List<AuraEffectConfig> configs);

        /// <summary>
        /// Called periodically by the AuraSource while this target is within its radius.
        /// This is where periodic effects (DoT, HoT) would be applied.
        /// </summary>
        void OnAuraTick(AuraSource source, List<AuraEffectConfig> configs);

        /// <summary>
        /// Called when this target leaves an aura's radius.
        /// The source is the AuraSource component, and configs are the effects it applied.
        /// </summary>
        void OnAuraExit(AuraSource source, List<AuraEffectConfig> configs);
    }
}
```

---

### 3. `AuraTargetComponent.cs`

This MonoBehaviour implements `IAuraTarget`. It manages the target's example stats (health, speed) and the complex logic of applying, ticking, persisting, and reverting aura effects.

```csharp
// AuraEffectSystem/AuraTargetComponent.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList() and other LINQ operations

namespace AuraEffectSystem
{
    /// <summary>
    /// Represents an active instance of an effect on this target.
    /// This allows tracking multiple instances of the same effect config from different sources,
    /// or managing duration after leaving an aura.
    /// </summary>
    [System.Serializable]
    public class ActiveAuraEffectInstance
    {
        public AuraEffectConfig config;      // The blueprint of the effect
        public AuraSource source;            // The source that applied this effect
        public float timeApplied;           // When this specific instance was applied
        public float timeExitedAura;        // When the target left the aura (0 if still in aura)
        public int stackCount = 1;          // How many stacks of this effect are present (if canStack is true)

        // Constructor
        public ActiveAuraEffectInstance(AuraEffectConfig config, AuraSource source, float currentTime)
        {
            this.config = config;
            this.source = source;
            this.timeApplied = currentTime;
            this.timeExitedAura = 0f; // 0 indicates still in aura
            this.stackCount = 1;
        }

        /// <summary>
        /// Checks if the effect has expired, considering its durationOnExit.
        /// </summary>
        public bool IsExpired(float currentTime)
        {
            // If timeExitedAura is set and durationOnExit is positive, check if persistence time is over.
            if (timeExitedAura > 0 && config.durationOnExit > 0)
            {
                return currentTime >= timeExitedAura + config.durationOnExit;
            }
            // If durationOnExit is 0 and it has exited, it's considered expired immediately upon exit.
            return timeExitedAura > 0 && config.durationOnExit <= 0;
        }

        /// <summary>
        /// Checks if this effect is currently active (either in aura or persisting).
        /// </summary>
        public bool IsActive(float currentTime)
        {
            return !IsExpired(currentTime);
        }

        public override string ToString()
        {
            return $"{config.effectName} (Source: {source.name}, Stack: {stackCount})";
        }
    }

    /// <summary>
    /// This component should be added to any GameObject that can be affected by Auras.
    /// It manages the entity's stats and the application/reversion of aura effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))] // Often entities have a Rigidbody for physics interactions
    public class AuraTargetComponent : MonoBehaviour, IAuraTarget
    {
        [Header("Target Stats (Example)")]
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _baseMovementSpeed = 5f;

        // Public properties for stats
        public float CurrentHealth { get => _currentHealth; private set => _currentHealth = Mathf.Clamp(value, 0, _maxHealth); }
        public float MaxHealth { get => _maxHealth; private set => _maxHealth = value; }
        public float BaseMovementSpeed { get => _baseMovementSpeed; private set => _baseMovementSpeed = value; }

        // Actual current movement speed, modified by effects
        public float CurrentMovementSpeed { get; private set; }

        // A list to track all active effect instances on this target.
        // Each entry represents an effect from a specific source, potentially stacked.
        private List<ActiveAuraEffectInstance> _activeEffectInstances = new List<ActiveAuraEffectInstance>();

        private void Awake()
        {
            CurrentMovementSpeed = BaseMovementSpeed;
        }

        private void Update()
        {
            // Update effects that persist after leaving an aura.
            // Also clean up expired effects.
            float currentTime = Time.time;
            List<ActiveAuraEffectInstance> expiredEffects = new List<ActiveAuraEffectInstance>();

            foreach (var instance in _activeEffectInstances)
            {
                if (instance.IsExpired(currentTime))
                {
                    expiredEffects.Add(instance);
                }
            }

            // Remove and revert expired effects
            foreach (var instance in expiredEffects)
            {
                RevertEffect(instance); // Revert any changes this effect made
                _activeEffectInstances.Remove(instance);
                Debug.Log($"<color=orange>{gameObject.name}</color> reverted/removed effect: {instance.config.effectName} from {instance.source.name}. Remaining: {_activeEffectInstances.Count} effects.");
            }

            // Recalculate dynamic stats (like speed) if effects have changed
            if (expiredEffects.Any())
            {
                RecalculateDynamicStats();
            }
        }

        // --- IAuraTarget Implementation ---

        /// <summary>
        /// Called when this target enters an aura's radius.
        /// Manages the initial application or refreshing of effects.
        /// </summary>
        public void OnAuraEnter(AuraSource source, List<AuraEffectConfig> configs)
        {
            float currentTime = Time.time;
            foreach (var config in configs)
            {
                // Find an existing instance of this specific effect config from this source
                var existingInstance = _activeEffectInstances.FirstOrDefault(
                    inst => inst.config == config && inst.source == source);

                if (existingInstance != null)
                {
                    // If effect is already active from this source, refresh its timers and potentially stack
                    existingInstance.timeApplied = currentTime;
                    existingInstance.timeExitedAura = 0f; // Re-entered aura
                    if (config.canStack)
                    {
                        existingInstance.stackCount++;
                        // Apply stack-specific logic if needed, e.g., if potency increases per stack
                        // For this example, potency is multiplied by stackCount during application/recalculation.
                    }
                    Debug.Log($"<color=lime>{gameObject.name}</color> refreshed/stacked effect: {existingInstance} from {source.name}");
                }
                else
                {
                    // Create a new instance and apply the effect for the first time
                    var newInstance = new ActiveAuraEffectInstance(config, source, currentTime);
                    _activeEffectInstances.Add(newInstance);
                    ApplyInitialEffect(newInstance);
                    Debug.Log($"<color=lime>{gameObject.name}</color> gained effect: {newInstance} from {source.name}");
                }
            }
            RecalculateDynamicStats(); // Recalculate stats as new effects might have been applied
        }

        /// <summary>
        /// Called periodically by the AuraSource while this target is within its radius.
        /// Processes periodic effects like Damage Over Time or Heal Over Time.
        /// </summary>
        public void OnAuraTick(AuraSource source, List<AuraEffectConfig> configs)
        {
            foreach (var config in configs)
            {
                // Only process effects that have a tick interval and are still considered "in aura"
                if (config.tickInterval > 0)
                {
                    // Find the active instance for this config from this source
                    var instance = _activeEffectInstances.FirstOrDefault(
                        inst => inst.config == config && inst.source == source && inst.timeExitedAura == 0f);

                    if (instance != null)
                    {
                        ApplyPeriodicEffect(instance);
                    }
                }
            }
        }

        /// <summary>
        /// Called when this target leaves an aura's radius.
        /// Manages the removal or persistence of effects.
        /// </summary>
        public void OnAuraExit(AuraSource source, List<AuraEffectConfig> configs)
        {
            float currentTime = Time.time;
            bool statChanged = false;

            foreach (var config in configs)
            {
                // Find the specific instance of this effect from this source that was active in the aura
                var instance = _activeEffectInstances.FirstOrDefault(
                    inst => inst.config == config && inst.source == source && inst.timeExitedAura == 0f); 

                if (instance != null)
                {
                    instance.timeExitedAura = currentTime; // Mark as exited

                    if (instance.config.durationOnExit <= 0)
                    {
                        // Immediately remove and revert if no durationOnExit
                        RevertEffect(instance);
                        _activeEffectInstances.Remove(instance);
                        Debug.Log($"<color=red>{gameObject.name}</color> immediately removed effect: {instance.config.effectName} from {source.name}. Remaining: {_activeEffectInstances.Count} effects.");
                        statChanged = true;
                    }
                    else
                    {
                        Debug.Log($"<color=red>{gameObject.name}</color> effect: {instance.config.effectName} from {source.name} will persist for {instance.config.durationOnExit}s.");
                    }
                }
            }

            if (statChanged) // Only recalculate if effects were immediately removed
            {
                RecalculateDynamicStats(); 
            }
        }

        // --- Effect Application Logic ---

        /// <summary>
        /// Applies an effect's initial impact or sets up continuous effects.
        /// </summary>
        private void ApplyInitialEffect(ActiveAuraEffectInstance instance)
        {
            if (instance == null) return;

            // Debug.Log($"Applying initial effect: {instance}");

            // For most effects, initial application might just mean tracking the instance.
            // For continuous buffs/debuffs (like speed), their impact is calculated in RecalculateDynamicStats.
            switch (instance.config.effectType)
            {
                case EffectType.DamageOverTime:
                case EffectType.HealOverTime:
                    // DoT/HoT primarily apply on periodic ticks, not usually an initial burst.
                    break;
                case EffectType.MovementSpeedBuff:
                case EffectType.MovementSpeedDebuff:
                    // These are handled by RecalculateDynamicStats
                    break;
                default:
                    Debug.LogWarning($"Unhandled initial application for EffectType: {instance.config.effectType}");
                    break;
            }
        }

        /// <summary>
        /// Applies a periodic effect (like DoT, HoT) during an aura tick.
        /// </summary>
        private void ApplyPeriodicEffect(ActiveAuraEffectInstance instance)
        {
            if (instance == null) return;

            // Debug.Log($"Applying periodic effect: {instance}");

            switch (instance.config.effectType)
            {
                case EffectType.DamageOverTime:
                    CurrentHealth -= instance.config.potency * instance.stackCount;
                    Debug.Log($"<color=magenta>{gameObject.name}</color> takes {instance.config.potency * instance.stackCount} DoT from {instance.source.name}. Health: {CurrentHealth:F1}");
                    break;
                case EffectType.HealOverTime:
                    CurrentHealth += instance.config.potency * instance.stackCount;
                    Debug.Log($"<color=cyan>{gameObject.name}</color> heals {instance.config.potency * instance.stackCount} HoT from {instance.source.name}. Health: {CurrentHealth:F1}");
                    break;
                case EffectType.MovementSpeedBuff:
                case EffectType.MovementSpeedDebuff:
                    // These effects are continuous, not periodic, so no action here.
                    break;
                default:
                    // Debug.LogWarning($"Unhandled periodic application for EffectType: {instance.config.effectType}");
                    break;
            }
        }

        /// <summary>
        /// Reverts an effect's changes when it's removed or expires.
        /// For dynamic stats, this primarily involves removing the instance from the list, then recalculating.
        /// </summary>
        private void RevertEffect(ActiveAuraEffectInstance instance)
        {
            if (instance == null) return;

            // Debug.Log($"Reverting effect: {instance}");

            switch (instance.config.effectType)
            {
                case EffectType.DamageOverTime:
                case EffectType.HealOverTime:
                    // DoT/HoT don't have direct "revert" logic, they just stop ticking.
                    break;
                case EffectType.MovementSpeedBuff:
                case EffectType.MovementSpeedDebuff:
                    // These are handled by RecalculateDynamicStats which will run after removal
                    break;
                default:
                    Debug.LogWarning($"Unhandled revert for EffectType: {instance.config.effectType}");
                    break;
            }
        }

        /// <summary>
        /// Recalculates dynamic stats (like movement speed) based on all currently active effects.
        /// This ensures that buffs/debuffs are correctly applied and removed.
        /// </summary>
        private void RecalculateDynamicStats()
        {
            float speedModifier = 0f; // Cumulative modifier for speed

            // Filter for effects that are currently active or persisting
            foreach (var instance in _activeEffectInstances.Where(inst => inst.IsActive(Time.time))) 
            {
                if (instance.config.effectType == EffectType.MovementSpeedBuff)
                {
                    speedModifier += instance.config.potency * instance.stackCount;
                }
                else if (instance.config.effectType == EffectType.MovementSpeedDebuff)
                {
                    speedModifier -= instance.config.potency * instance.stackCount;
                }
            }

            CurrentMovementSpeed = BaseMovementSpeed + speedModifier;
            // Ensure speed doesn't go below 0 (or some minimum threshold)
            CurrentMovementSpeed = Mathf.Max(0f, CurrentMovementSpeed);

            // You might want to update a character controller's speed or animator here
            // Example: GetComponent<PlayerMovement>().SetSpeed(CurrentMovementSpeed);
            // Debug.Log($"<color=yellow>{gameObject.name}</color> recalculated speed: {CurrentMovementSpeed:F1}");
        }

        // Optional: Draw debug information above the target GameObject in the game view.
        private void OnGUI()
        {
            // Only draw if a main camera exists
            if (Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screenPos.z < 0) return; // Don't draw if behind camera

            GUILayout.BeginArea(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 150));
            GUILayout.Box($"<b>{gameObject.name}</b>");
            GUILayout.Label($"Health: {CurrentHealth:F1}/{MaxHealth:F1}");
            GUILayout.Label($"Speed: {CurrentMovementSpeed:F1}");
            if (_activeEffectInstances.Any())
            {
                GUILayout.Label("Active Effects:");
                foreach (var instance in _activeEffectInstances.Where(inst => inst.IsActive(Time.time)))
                {
                    string status = instance.timeExitedAura > 0 ? 
                                    $"(Persisting for {(instance.timeExitedAura + instance.config.durationOnExit - Time.time):F1}s)" : 
                                    "(In Aura)";
                    GUILayout.Label($"- {instance.config.effectName} ({instance.source.name}) {status}");
                }
            }
            GUILayout.EndArea();
        }
    }
}
```

---

### 4. `AuraSource.cs`

This MonoBehaviour represents the emitter of the aura. It uses a `SphereCollider` to detect `IAuraTarget` components within its radius and manages the `OnAuraEnter`, `OnAuraTick`, and `OnAuraExit` events.

```csharp
// AuraEffectSystem/AuraSource.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList()

namespace AuraEffectSystem
{
    /// <summary>
    /// This component should be added to any GameObject that emits an Aura.
    /// It's responsible for detecting targets, applying initial effects, and periodically ticking them.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))] // Used for detecting targets in range
    public class AuraSource : MonoBehaviour
    {
        [Header("Aura Configuration")]
        [Tooltip("Radius of the aura effect.")]
        public float auraRadius = 5f;

        [Tooltip("Which layers contain potential targets for this aura.")]
        public LayerMask targetLayers;

        [Tooltip("How often the aura's effects will 'tick' on targets within its radius.")]
        public float effectTickRate = 1f; // How often targets are instructed to tick effects

        [Header("Aura Effects")]
        [Tooltip("The list of effects that this aura will apply to targets.")]
        public List<AuraEffectConfig> auraEffectsToApply = new List<AuraEffectConfig>();

        // Internal tracking of targets currently within the aura's radius.
        private HashSet<IAuraTarget> _targetsInAura = new HashSet<IAuraTarget>();

        // Tracks when each target should receive its next periodic effect tick from this source.
        private Dictionary<IAuraTarget, float> _nextTickTimes = new Dictionary<IAuraTarget, float>();

        private SphereCollider _auraCollider;

        private void Awake()
        {
            _auraCollider = GetComponent<SphereCollider>();
            _auraCollider.isTrigger = true;
            _auraCollider.radius = auraRadius;
        }

        private void OnValidate()
        {
            // Update collider radius in editor if auraRadius changes.
            // Also ensure the collider is a trigger.
            if (_auraCollider == null) _auraCollider = GetComponent<SphereCollider>();
            if (_auraCollider != null)
            {
                _auraCollider.isTrigger = true;
                _auraCollider.radius = auraRadius;
            }
        }

        private void FixedUpdate()
        {
            // Use FixedUpdate for physics-related checks or consistent ticking,
            // as this is where we manage periodic application of effects.
            float currentTime = Time.time;
            
            // Create a temporary list to avoid modifying _targetsInAura while iterating,
            // in case a target leaves the aura during the tick processing.
            List<IAuraTarget> targetsToTick = _targetsInAura.ToList();

            foreach (var target in targetsToTick)
            {
                // Ensure the target is still valid and being tracked
                if (target != null && _nextTickTimes.ContainsKey(target))
                {
                    if (currentTime >= _nextTickTimes[target])
                    {
                        target.OnAuraTick(this, auraEffectsToApply);
                        _nextTickTimes[target] = currentTime + effectTickRate; // Schedule next tick
                    }
                }
                else if (target == null) // Cleanup for potentially destroyed targets
                {
                    _targetsInAura.Remove(target);
                    _nextTickTimes.Remove(target);
                }
            }
        }

        /// <summary>
        /// Called when another collider enters the trigger collider.
        /// Detects new targets and informs them they've entered the aura.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Check if the entering object is on one of the target layers.
            if ((targetLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                IAuraTarget target = other.GetComponent<IAuraTarget>();
                if (target != null && !_targetsInAura.Contains(target))
                {
                    _targetsInAura.Add(target);
                    // Schedule the first tick immediately or after a delay
                    _nextTickTimes[target] = Time.time + effectTickRate; 
                    target.OnAuraEnter(this, auraEffectsToApply);
                    Debug.Log($"<color=blue>{target.gameObject.name}</color> entered aura of <color=blue>{gameObject.name}</color>.");
                }
            }
        }

        /// <summary>
        /// Called when another collider exits the trigger collider.
        /// Informs targets they've left the aura.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            IAuraTarget target = other.GetComponent<IAuraTarget>();
            if (target != null && _targetsInAura.Contains(target))
            {
                _targetsInAura.Remove(target);
                _nextTickTimes.Remove(target); // No longer need to track ticks for this target
                target.OnAuraExit(this, auraEffectsToApply);
                Debug.Log($"<color=blue>{target.gameObject.name}</color> exited aura of <color=blue>{gameObject.name}</color>.");
            }
        }

        // Optional: Draw the aura radius in the editor for visualization.
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, auraRadius);
        }
    }
}
```

---

### Example Usage and Setup in Unity Editor:

To get this system running in your Unity project:

1.  **Create a folder** named `AuraEffectSystem` (or similar) in your Assets folder and place all four `.cs` files inside it.

2.  **Create AuraEffectConfig ScriptableObjects:**
    *   In your Project window, right-click -> Create -> Aura System -> Aura Effect Configuration.
    *   Create a few instances with different properties:
        *   **"PoisonAura"**:
            *   `Effect Type`: `DamageOverTime`
            *   `Potency`: 5
            *   `Tick Interval`: 1
            *   `Duration On Exit`: 0
            *   `Can Stack`: false
        *   **"HealingAura"**:
            *   `Effect Type`: `HealOverTime`
            *   `Potency`: 3
            *   `Tick Interval`: 1
            *   `Duration On Exit`: 2 (persists for 2 seconds after leaving)
            *   `Can Stack`: false
        *   **"SpeedBoostAura"**:
            *   `Effect Type`: `MovementSpeedBuff`
            *   `Potency`: 2
            *   `Tick Interval`: 0 (continuous effect, no periodic tick needed)
            *   `Duration On Exit`: 3
            *   `Can Stack`: true (multiple speed auras could stack)
        *   **"SlowAura"**:
            *   `Effect Type`: `MovementSpeedDebuff`
            *   `Potency`: 1.5
            *   `Tick Interval`: 0
            *   `Duration On Exit`: 1
            *   `Can Stack`: true

3.  **Setup a Target GameObject (e.g., "Player" or "Enemy"):**
    *   Create a new 3D Object (e.g., a **Cube**). Name it "Target".
    *   Add the `AuraTargetComponent` script to it.
    *   Add a **Rigidbody** component (required by `AuraTargetComponent`).
    *   Set its **Layer** to something distinct, e.g., "Targets". (Go to Layers -> Add Layer..., create "Targets", then select the "Target" GameObject and assign it to this layer).
    *   Adjust `Base Movement Speed` in the inspector if desired.

4.  **Setup an Aura Source GameObject (e.g., "AuraEmitter"):**
    *   Create a new 3D Object (e.g., a **Sphere** or an Empty GameObject). Name it "AuraEmitter".
    *   Add the `AuraSource` script to it.
    *   Adjust `Aura Radius` (e.g., 5). You'll see a cyan wire sphere in the Scene view indicating its range.
    *   Set `Target Layers` to the layer you created for targets (e.g., "Targets").
    *   Drag the `AuraEffectConfig` ScriptableObjects you created in step 2 into the `Aura Effects To Apply` list in the Inspector. For example, add "PoisonAura" and "SlowAura".
    *   Adjust `Effect Tick Rate` (e.g., 1).

5.  **Test:**
    *   Run the scene.
    *   Move the "Target" GameObject into the "AuraEmitter"'s radius. You can do this by moving it directly in the scene view during play mode or by adding a simple movement script to the "Target" (e.g., `transform.Translate(Vector3.forward * CurrentMovementSpeed * Time.deltaTime)` in its `Update` method).
    *   Observe the debug logs in the Console and the `OnGUI` display floating above the "Target" GameObject, showing its health, speed, and active effects.
    *   Move the "Target" out of the "AuraEmitter"'s radius and observe if effects persist or are immediately removed based on their `durationOnExit` settings.
    *   Try creating another `AuraSource` with different effects (e.g., "HealingAura", "SpeedBoostAura") and see how effects interact, persist, and stack.

This setup provides a complete and functional example of the AuraEffectSystem, ready for extension and integration into your Unity projects.