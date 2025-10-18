// Unity Design Pattern Example: SafeZoneSystem
// This script demonstrates the SafeZoneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SafeZoneSystem' design pattern in Unity provides a robust way to manage specific areas in your game world (SafeZones) and allow entities to react when they enter or exit these areas. This is incredibly useful for implementing various game mechanics such as healing zones, no-combat areas, shop zones, quest-giver zones, or any location-based behavior.

This example will create a complete system with three interconnected C# scripts:

1.  **`SafeZoneSystem.cs` (The Manager):** A singleton that centrally registers all `SafeZone` components, tracks which `SafeZoneEntity` objects are in which zones, and dispatches events when entities enter or exit zones.
2.  **`SafeZone.cs` (The Zone Definition):** A component placed on GameObjects with trigger colliders. It defines a specific safe area and its type (e.g., `HealingZone`, `NoCombatZone`). It notifies the `SafeZoneSystem` when a `SafeZoneEntity` enters or exits its bounds.
3.  **`SafeZoneEntity.cs` (The Interactable Entity):** A component placed on GameObjects (like players or NPCs) that should interact with safe zones. It listens to events from the `SafeZoneSystem` and updates its own state, triggering specific behaviors based on the zones it's currently in.

---

### **1. `SafeZoneSystem.cs`**

This script acts as the central hub for the entire system. It's a singleton, meaning only one instance of it can exist in your scene, ensuring a single point of truth for safe zone data.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // Required for LINQ extensions like Select, Join

namespace DesignPatterns.SafeZoneSystem // Encapsulates the pattern within a namespace
{
    // Enum to define different types of safe zones.
    // This allows for varied behaviors to be associated with different zones.
    public enum SafeZoneType
    {
        Generic,         // A basic safe zone without specific behavior
        HealingZone,     // Entities might regenerate health here
        NoCombatZone,    // Combat actions might be disabled here
        ShopZone,        // Triggers shop UI or NPC interaction
        QuestGiverZone   // Highlights quest givers or quest interactions
    }

    /// <summary>
    /// The central manager for the SafeZoneSystem design pattern.
    /// This is a singleton that keeps track of all active SafeZones and SafeZoneEntities.
    /// It processes enter/exit events and broadcasts status changes to interested parties.
    /// </summary>
    public class SafeZoneSystem : MonoBehaviour
    {
        // Singleton pattern: Ensures only one instance of the SafeZoneSystem exists.
        // It's publicly accessible static, but its setter is private to control instantiation.
        public static SafeZoneSystem Instance { get; private set; }

        // Stores all active SafeZones, mapped by their unique name for quick lookup.
        private Dictionary<string, SafeZone> _registeredZones = new Dictionary<string, SafeZone>();

        // Tracks which entities are currently inside which safe zones.
        // Key: SafeZoneEntity object, Value: HashSet of SafeZones that the entity is currently in.
        // Using a HashSet efficiently stores unique zones and allows quick addition/removal.
        private Dictionary<SafeZoneEntity, HashSet<SafeZone>> _entitiesInZones = new Dictionary<SafeZoneEntity, HashSet<SafeZone>>();

        // --- Events ---
        // These events allow other components (like SafeZoneEntity) to subscribe
        // and react to changes in the safe zone status.

        /// <summary>
        /// Event fired when ANY SafeZoneEntity enters a specific SafeZone.
        /// (Arguments: The entity that entered, the zone that was entered)
        /// </summary>
        public event Action<SafeZoneEntity, SafeZone> OnEntityEnteredSafeZone;

        /// <summary>
        /// Event fired when ANY SafeZoneEntity exits a specific SafeZone.
        /// (Arguments: The entity that exited, the zone that was exited)
        /// </summary>
        public event Action<SafeZoneEntity, SafeZone> OnEntityExitedSafeZone;

        /// <summary>
        /// Event fired when an entity's OVERALL safe zone status changes.
        /// This means going from "not in any zone" to "in at least one zone",
        /// or from "in at least one zone" to "not in any zone".
        /// (Arguments: The entity whose status changed, true if now in a zone, false if now out of all zones)
        /// </summary>
        public event Action<SafeZoneEntity, bool> OnEntitySafeZoneStatusChanged;

        // Called when the script instance is being loaded.
        private void Awake()
        {
            // Implement the singleton pattern.
            // If another instance already exists, destroy this one to maintain uniqueness.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SafeZoneSystem instances found. Destroying duplicate.", this);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                // Optional: Make the system persistent across scene loads if desired.
                // DontDestroyOnLoad(gameObject);
            }
        }

        // --- Public Methods for Zone Management ---

        /// <summary>
        /// Registers a SafeZone with the system. Typically called by SafeZone.OnEnable().
        /// </summary>
        /// <param name="zone">The SafeZone component to register.</param>
        public void RegisterZone(SafeZone zone)
        {
            // Only add if a zone with this name doesn't already exist.
            if (!_registeredZones.ContainsKey(zone.ZoneName))
            {
                _registeredZones.Add(zone.ZoneName, zone);
                Debug.Log($"SafeZone '{zone.ZoneName}' ({zone.ZoneType}) registered with the system.", zone);
            }
            else
            {
                Debug.LogWarning($"Attempted to register SafeZone with duplicate name: '{zone.ZoneName}'. Zone not registered. Please ensure zone names are unique.", zone);
            }
        }

        /// <summary>
        /// Unregisters a SafeZone from the system. Typically called by SafeZone.OnDisable().
        /// Also ensures any entities currently associated with this zone are updated correctly.
        /// </summary>
        /// <param name="zone">The SafeZone component to unregister.</param>
        public void UnregisterZone(SafeZone zone)
        {
            // Only remove if the zone is actually registered.
            if (_registeredZones.Remove(zone.ZoneName))
            {
                Debug.Log($"SafeZone '{zone.ZoneName}' ({zone.ZoneType}) unregistered from the system.", zone);

                // Iterate through all entities currently in any zone.
                // We need to check if any of them were specifically in the zone being unregistered.
                foreach (var entry in _entitiesInZones)
                {
                    SafeZoneEntity entity = entry.Key;
                    HashSet<SafeZone> zonesOfEntity = entry.Value;

                    // If the entity was in this specific zone, remove it from their list.
                    if (zonesOfEntity.Remove(zone))
                    {
                        // Notify subscribers that this entity exited the zone (due to unregistration).
                        OnEntityExitedSafeZone?.Invoke(entity, zone);

                        // If, after removing this zone, the entity is no longer in ANY safe zone,
                        // then its overall status has changed.
                        if (zonesOfEntity.Count == 0)
                        {
                            // Remove the entity entirely from tracking if it's no longer in any zone.
                            _entitiesInZones.Remove(entity); 
                            OnEntitySafeZoneStatusChanged?.Invoke(entity, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called by a SafeZone when a SafeZoneEntity enters its trigger.
        /// Updates the internal state tracking which entities are in which zones and broadcasts events.
        /// </summary>
        /// <param name="zone">The SafeZone that was entered.</param>
        /// <param name="entity">The SafeZoneEntity that entered.</param>
        public void EntityEnteredZone(SafeZone zone, SafeZoneEntity entity)
        {
            // Ensure there's an entry for this entity in our tracking dictionary.
            // If not, create a new HashSet for it.
            if (!_entitiesInZones.ContainsKey(entity))
            {
                _entitiesInZones[entity] = new HashSet<SafeZone>();
            }

            // Check if the entity was already in *any* safe zone before this new zone was added.
            bool wasInAnyZone = _entitiesInZones[entity].Count > 0;

            // Attempt to add the zone to the entity's set of current zones.
            // The .Add() method returns true if the element was added (i.e., it wasn't already present).
            if (_entitiesInZones[entity].Add(zone))
            {
                Debug.Log($"Entity '{entity.EntityName}' entered zone '{zone.ZoneName}'.", entity);
                // Broadcast that this entity entered this specific zone.
                OnEntityEnteredSafeZone?.Invoke(entity, zone);

                // If the entity was NOT in any zone before and now IS (because we just added one),
                // then its overall safe zone status has changed to 'in a zone'.
                if (!wasInAnyZone && _entitiesInZones[entity].Count == 1)
                {
                    OnEntitySafeZoneStatusChanged?.Invoke(entity, true);
                }
            }
        }

        /// <summary>
        /// Called by a SafeZone when a SafeZoneEntity exits its trigger.
        /// Updates the internal state and broadcasts events.
        /// </summary>
        /// <param name="zone">The SafeZone that was exited.</param>
        /// <param name="entity">The SafeZoneEntity that exited.</param>
        public void EntityExitedZone(SafeZone zone, SafeZoneEntity entity)
        {
            // Ensure the entity is actually being tracked and was in the specified zone.
            if (_entitiesInZones.ContainsKey(entity) && _entitiesInZones[entity].Contains(zone))
            {
                // Remove the zone from the entity's set of current zones.
                _entitiesInZones[entity].Remove(zone);
                Debug.Log($"Entity '{entity.EntityName}' exited zone '{zone.ZoneName}'.", entity);

                // Broadcast that this entity exited this specific zone.
                OnEntityExitedSafeZone?.Invoke(entity, zone);

                // If, after removing this zone, the entity is now no longer in ANY safe zone,
                // its overall status has changed.
                if (_entitiesInZones[entity].Count == 0)
                {
                    // Clean up the dictionary entry for the entity if it's no longer in any zone.
                    _entitiesInZones.Remove(entity);
                    OnEntitySafeZoneStatusChanged?.Invoke(entity, false);
                }
            }
        }

        // --- Public Query Methods ---

        /// <summary>
        /// Checks if a given SafeZoneEntity is currently inside any registered safe zone.
        /// </summary>
        /// <param name="entity">The SafeZoneEntity to check.</param>
        /// <returns>True if the entity is in at least one safe zone, false otherwise.</returns>
        public bool IsEntityInAnySafeZone(SafeZoneEntity entity)
        {
            // Check if the entity is tracked AND if it's associated with any zones.
            return _entitiesInZones.ContainsKey(entity) && _entitiesInZones[entity].Count > 0;
        }

        /// <summary>
        /// Gets a read-only collection of all SafeZones a specific entity is currently inside.
        /// </summary>
        /// <param name="entity">The SafeZoneEntity to query.</param>
        /// <returns>A read-only collection of SafeZones, or an empty list if the entity is not in any.</returns>
        public IReadOnlyCollection<SafeZone> GetZonesForEntity(SafeZoneEntity entity)
        {
            // Use TryGetValue for safe access.
            if (_entitiesInZones.TryGetValue(entity, out HashSet<SafeZone> zones))
            {
                // Return a copy of the HashSet as a List to prevent external modification of the internal state.
                return new List<SafeZone>(zones); 
            }
            return new List<SafeZone>(); // Return an empty list if the entity is not in any zone.
        }

        /// <summary>
        /// Gets a registered SafeZone by its unique name.
        /// </summary>
        /// <param name="zoneName">The unique name of the SafeZone to find.</param>
        /// <returns>The SafeZone object if found, otherwise null.</returns>
        public SafeZone GetSafeZoneByName(string zoneName)
        {
            _registeredZones.TryGetValue(zoneName, out SafeZone zone);
            return zone;
        }

        /// <summary>
        /// Returns a read-only collection of all currently registered SafeZones in the system.
        /// </summary>
        public IReadOnlyCollection<SafeZone> GetAllRegisteredZones()
        {
            return new List<SafeZone>(_registeredZones.Values); // Return a copy.
        }
    }
}
```

---

### **2. `SafeZone.cs`**

This script defines a safe zone area in your scene. Attach it to any GameObject with a trigger collider.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for IReadOnlyCollection in SafeZoneEntity example
// The namespace must match the one defined in SafeZoneSystem.cs
namespace DesignPatterns.SafeZoneSystem 
{
    /// <summary>
    /// Represents a specific safe area in the game world.
    /// This component requires a Collider set to 'Is Trigger' to function correctly.
    /// It registers itself with the SafeZoneSystem and detects when SafeZoneEntities enter or exit its bounds.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Ensures a Collider component is always present.
    public class SafeZone : MonoBehaviour
    {
        [Tooltip("A unique identifier for this safe zone. Used for registration and lookup.")]
        public string ZoneName = "New Safe Zone";

        [Tooltip("The type of this safe zone, dictating its specific behavior (e.g., healing, no-combat).")]
        public SafeZoneType ZoneType = SafeZoneType.Generic;

        [Tooltip("An optional description of the zone's purpose, for designer reference.")]
        [TextArea] // Makes the string field multi-line in the Inspector.
        public string ZoneDescription = "A general safe area.";

        // Called when the script instance is being loaded.
        private void Awake()
        {
            // Ensure the attached Collider is a trigger.
            // If it's not, Unity's trigger events (OnTriggerEnter/Exit) won't fire.
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"Collider on SafeZone '{ZoneName}' is not set to 'Is Trigger'. Setting it automatically.", this);
                col.isTrigger = true;
            }
            else if (col == null)
            {
                Debug.LogError($"SafeZone '{ZoneName}' requires a Collider component to define its area! Disabling this component.", this);
                enabled = false; // Disable the component if no collider is found, as it cannot function.
            }
        }

        // Called when the object becomes enabled and active.
        private void OnEnable()
        {
            // Attempt to register this safe zone with the central SafeZoneSystem.
            // Null check ensures we don't try to access a non-existent system (e.g., if scene loads out of order).
            SafeZoneSystem.Instance?.RegisterZone(this);
        }

        // Called when the behaviour becomes disabled or inactive.
        private void OnDisable()
        {
            // Attempt to unregister this safe zone from the SafeZoneSystem.
            // This is crucial to prevent the system from referencing destroyed or inactive zones
            // and helps with memory management.
            SafeZoneSystem.Instance?.UnregisterZone(this);
        }

        /// <summary>
        /// Called when another collider enters this trigger collider.
        /// </summary>
        /// <param name="other">The collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            // Try to get a SafeZoneEntity component from the entering GameObject.
            // This ensures we only react to entities explicitly designed to interact with safe zones.
            SafeZoneEntity entity = other.GetComponent<SafeZoneEntity>();
            if (entity != null)
            {
                // If a SafeZoneEntity is found, notify the central SafeZoneSystem.
                SafeZoneSystem.Instance?.EntityEnteredZone(this, entity);
            }
        }

        /// <summary>
        /// Called when another collider exits this trigger collider.
        /// </summary>
        /// <param name="other">The collider that exited the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            // Try to get a SafeZoneEntity component from the exiting GameObject.
            SafeZoneEntity entity = other.GetComponent<SafeZoneEntity>();
            if (entity != null)
            {
                // If a SafeZoneEntity is found, notify the central SafeZoneSystem.
                SafeZoneSystem.Instance?.EntityExitedZone(this, entity);
            }
        }

        // Optional: Draw a visual representation of the safe zone in the editor.
        // This helps designers visualize the zone's boundaries without running the game.
        private void OnDrawGizmos()
        {
            // Retrieve the collider to get its properties (center, size, etc.).
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // Set Gizmo color to green with some transparency for visibility.
                Gizmos.color = new Color(0, 1, 0, 0.3f); 
                // Store the current Gizmos matrix to restore it later.
                Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
                // Set Gizmos matrix to match the object's position, rotation, and scale for accurate drawing.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

                // Draw based on the type of collider.
                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    // Capsule drawing in Gizmos is more complex.
                    // For simplicity, we draw a sphere at its center, or a box.
                    // For a more accurate visual, you might draw multiple spheres or use custom handles.
                    Gizmos.DrawSphere(capsule.center, capsule.radius); 
                }
                // Add more collider types (e.g., MeshCollider) as needed.

                Gizmos.matrix = oldGizmosMatrix; // Restore the original Gizmos matrix.
            }
        }
    }
}
```

---

### **3. `SafeZoneEntity.cs`**

This script identifies an object (like a player character or an NPC) as something that interacts with safe zones. It includes example logic for reacting to zone entry/exit events.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like Select, Join

// The namespace must match the one defined in SafeZoneSystem.cs
namespace DesignPatterns.SafeZoneSystem
{
    /// <summary>
    /// Represents an entity that can enter and exit SafeZones.
    /// This component marks a GameObject as interactable with the SafeZoneSystem.
    /// It requires a Rigidbody to ensure trigger events are correctly fired,
    /// and listens to the SafeZoneSystem for updates on its status.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))] // Rigidbody is essential for reliable trigger detection.
    public class SafeZoneEntity : MonoBehaviour
    {
        [Tooltip("Optional: A friendly name for this entity, useful for debugging messages.")]
        public string EntityName = "Unnamed Entity";

        // Public property to check if the entity is currently inside any safe zone.
        // This property is updated by the SafeZoneSystem via event subscription.
        public bool IsInAnySafeZone { get; private set; }

        // Public property to get a read-only collection of safe zones the entity is currently in.
        // This queries the SafeZoneSystem directly, providing up-to-date information.
        public IReadOnlyCollection<SafeZone> CurrentSafeZones
        {
            get
            {
                // Ensure the SafeZoneSystem instance exists before querying.
                // If not, return an empty collection to prevent NullReferenceException.
                return SafeZoneSystem.Instance?.GetZonesForEntity(this) ?? new List<SafeZone>();
            }
        }

        // Called when the script instance is being loaded.
        private void Awake()
        {
            // Ensure the Rigidbody is configured for trigger detection without physics simulation.
            // If the entity is not meant to be physics-driven, set its Rigidbody to kinematic.
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Prevents physics forces from moving the entity.
                rb.useGravity = false; // Optional: Disables gravity on the Rigidbody.
                // It's good practice to place entities on a specific layer that SafeZones interact with,
                // and configure the physics matrix (Edit -> Project Settings -> Physics) to only allow
                // interactions between SafeZone layers and Entity layers.
            }
        }

        // Called when the object becomes enabled and active.
        private void OnEnable()
        {
            // Subscribe to the SafeZoneSystem's events for status changes.
            // This allows the entity to react whenever its safe zone status changes globally or specifically.
            if (SafeZoneSystem.Instance != null)
            {
                SafeZoneSystem.Instance.OnEntitySafeZoneStatusChanged += HandleSafeZoneStatusChanged;
                SafeZoneSystem.Instance.OnEntityEnteredSafeZone += HandleEntityEnteredSpecificZone;
                SafeZoneSystem.Instance.OnEntityExitedSafeZone += HandleEntityExitedSpecificZone;
            }
            else
            {
                Debug.LogWarning("SafeZoneSystem.Instance is null. Please ensure a SafeZoneSystem GameObject is present in the scene.", this);
            }
        }

        // Called when the behaviour becomes disabled or inactive.
        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks and ensure event handlers
            // are not called on a disabled or destroyed object.
            if (SafeZoneSystem.Instance != null)
            {
                SafeZoneSystem.Instance.OnEntitySafeZoneStatusChanged -= HandleSafeZoneStatusChanged;
                SafeZoneSystem.Instance.OnEntityEnteredSafeZone -= HandleEntityEnteredSpecificZone;
                SafeZoneSystem.Instance.OnEntityExitedSafeZone -= HandleEntityExitedSpecificZone;
            }
        }

        /// <summary>
        /// Event handler for when the entity's overall safe zone status changes (enters ANY zone or exits ALL zones).
        /// </summary>
        /// <param name="entity">The entity whose status changed.</param>
        /// <param name="isInZone">True if the entity is now in at least one safe zone, false otherwise.</param>
        private void HandleSafeZoneStatusChanged(SafeZoneEntity entity, bool isInZone)
        {
            // Only update and react if the event pertains to THIS specific entity instance.
            if (entity == this)
            {
                IsInAnySafeZone = isInZone;
                Debug.Log($"<color=cyan>{EntityName} overall status: {(IsInAnySafeZone ? "Entered ANY safe zone." : "Exited ALL safe zones.")}</color>", this);

                // --- Practical Example: Triggering general effects ---
                if (IsInAnySafeZone)
                {
                    // Potentially start a general "safe mode" effect here (e.g., UI glow, passive XP gain).
                }
                else
                {
                    // Potentially end general "safe mode" effects here.
                }
            }
        }

        /// <summary>
        /// Event handler for when this entity enters a specific safe zone.
        /// </summary>
        /// <param name="entity">The entity that entered.</param>
        /// <param name="zone">The specific SafeZone entered.</param>
        private void HandleEntityEnteredSpecificZone(SafeZoneEntity entity, SafeZone zone)
        {
            if (entity == this)
            {
                Debug.Log($"<color=green>{EntityName} entered specific zone: {zone.ZoneName} (Type: {zone.ZoneType})</color>", this);

                // --- Practical Example: Perform actions based on specific zone types ---
                switch (zone.ZoneType)
                {
                    case SafeZoneType.HealingZone:
                        Debug.Log($"<color=lime>{EntityName}: Initiating health regeneration in {zone.ZoneName}!</color>");
                        // TODO: Trigger actual health regeneration logic (e.g., start a Coroutine).
                        break;
                    case SafeZoneType.NoCombatZone:
                        Debug.Log($"<color=orange>{EntityName}: Combat disabled in {zone.ZoneName}.</color>");
                        // TODO: Trigger combat disabling logic (e.g., notify a CombatManager).
                        break;
                    case SafeZoneType.ShopZone:
                        Debug.Log($"<color=yellow>{EntityName}: Welcome to the shop in {zone.ZoneName}! Press 'E' to open shop.</color>");
                        // TODO: Trigger shop UI display or NPC interaction prompt.
                        break;
                    case SafeZoneType.QuestGiverZone:
                        Debug.Log($"<color=blue>{EntityName}: A quest awaits in {zone.ZoneName}! Look for NPCs with '!' above their heads.</color>");
                        // TODO: Highlight quest givers, update quest log, etc.
                        break;
                    default:
                        Debug.Log($"{EntityName}: Entered a generic safe zone: {zone.ZoneName}.");
                        break;
                }
            }
        }

        /// <summary>
        /// Event handler for when this entity exits a specific safe zone.
        /// </summary>
        /// <param name="entity">The entity that exited.</param>
        /// <param name="zone">The specific SafeZone exited.</param>
        private void HandleEntityExitedSpecificZone(SafeZoneEntity entity, SafeZone zone)
        {
            if (entity == this)
            {
                Debug.Log($"<color=red>{EntityName} exited specific zone: {zone.ZoneName} (Type: {zone.ZoneType})</color>", this);

                // --- Practical Example: Perform actions based on zone type when exiting ---
                switch (zone.ZoneType)
                {
                    case SafeZoneType.HealingZone:
                        Debug.Log($"<color=red>{EntityName}: Stopping health regeneration from {zone.ZoneName}.</color>");
                        // TODO: Stop actual health regeneration logic.
                        break;
                    case SafeZoneType.NoCombatZone:
                        Debug.Log($"<color=orange>{EntityName}: Combat enabled outside {zone.ZoneName}.</color>");
                        // TODO: Re-enable combat logic.
                        break;
                    case SafeZoneType.ShopZone:
                        Debug.Log($"<color=yellow>{EntityName}: Leaving the shop in {zone.ZoneName}. Goodbye!</color>");
                        // TODO: Close shop UI or end NPC interaction.
                        break;
                    case SafeZoneType.QuestGiverZone:
                        Debug.Log($"<color=blue>{EntityName}: Left the quest zone. Quests might be harder to find now.</color>");
                        // TODO: Remove quest highlights, etc.
                        break;
                    default:
                        Debug.Log($"{EntityName}: Exited a generic safe zone: {zone.ZoneName}.");
                        break;
                }
            }
        }

        // Example method to demonstrate querying the system directly (e.g., when a player presses a button)
        public void PerformActionBasedOnZoneStatus()
        {
            if (IsInAnySafeZone)
            {
                // Build a comma-separated string of current zone names.
                string zoneNames = string.Join(", ", CurrentSafeZones.Select(z => z.ZoneName));
                Debug.Log($"<color=magenta>{EntityName} is currently in safe zone(s): {zoneNames}. Feeling safe!</color>", this);
            }
            else
            {
                Debug.Log($"<color=grey>{EntityName} is not in any safe zone. Stay vigilant!</color>", this);
            }
        }
    }
}
```

---

### **Example Usage in Unity (Step-by-Step Implementation):**

1.  **Create a New Unity Project (or open an existing one).**
    *   Open Unity Hub, create a new 3D project.

2.  **Organize Scripts:**
    *   In your Unity Project window, create a folder structure: `Assets/Scripts/DesignPatterns/SafeZoneSystem`.
    *   Create three C# scripts inside `Assets/Scripts/DesignPatterns/SafeZoneSystem`:
        *   `SafeZoneSystem.cs`
        *   `SafeZone.cs`
        *   `SafeZoneEntity.cs`
    *   Copy the respective code blocks provided above into each file. Ensure the `namespace DesignPatterns.SafeZoneSystem` is consistent across all three.

3.  **Setup the `SafeZoneSystem` Manager:**
    *   In your Unity scene (e.g., `SampleScene`), create an empty GameObject.
    *   Rename it to `_SafeZoneSystemManager`.
    *   Drag and drop the `SafeZoneSystem.cs` script onto this `_SafeZoneSystemManager` GameObject in the Inspector.
        *   *Explanation:* This initializes the singleton instance, making the system available for other scripts.

4.  **Create Safe Zones:**
    *   **Healing Zone:**
        *   Create a new 3D Object -> `Cube`. Rename it `HealingZone_01`.
        *   Reset its Transform (right-click Transform component -> Reset).
        *   Scale it up (e.g., X=10, Y=2, Z=10) to make a visible area.
        *   In the `Box Collider` component, **check `Is Trigger`**.
        *   Drag and drop the `SafeZone.cs` script onto `HealingZone_01`.
        *   In the `SafeZone` component's Inspector:
            *   Set `Zone Name` to `HealingZone_01`.
            *   Set `Zone Type` to `HealingZone`.
            *   Set `Zone Description` to `A zone where entities regenerate health.`
    *   **No Combat Zone:**
        *   Duplicate `HealingZone_01` (Ctrl+D or Cmd+D). Rename the duplicate to `NoCombatZone_01`.
        *   Move `NoCombatZone_01` to a different position (e.g., change its Z position to 15) so it doesn't overlap.
        *   In its `SafeZone` component's Inspector:
            *   Set `Zone Name` to `NoCombatZone_01`.
            *   Set `Zone Type` to `NoCombatZone`.
            *   Set `Zone Description` to `A zone where combat is disabled.`

5.  **Create a Player (SafeZoneEntity):**
    *   Create a new 3D Object -> `Capsule`. Rename it `Player`.
    *   Reset its Transform and move it slightly up (e.g., Y=1) so it sits above the ground.
    *   Add a `Rigidbody` component to the `Player` GameObject (Component -> Physics -> Rigidbody).
        *   **Crucially, check `Is Kinematic` on the Rigidbody.** This allows trigger events to fire without the player being affected by gravity or physics forces.
        *   You can also check `Freeze Rotation` for X, Y, Z if you only want to control rotation programmatically.
    *   Drag and drop the `SafeZoneEntity.cs` script onto `Player`.
    *   In the `SafeZoneEntity` component's Inspector:
        *   Set `Entity Name` to `Player`.

6.  **Add a Simple Player Movement Script (Optional but Recommended for Testing):**
    *   Create a new C# script `PlayerMovement.cs` in `Assets/Scripts`.
    *   Add the following code:
        ```csharp
        using UnityEngine;

        public class PlayerMovement : MonoBehaviour
        {
            public float moveSpeed = 5f;
            public float rotationSpeed = 180f; // Degrees per second

            void Update()
            {
                // Basic movement (forward/backward)
                float verticalInput = Input.GetAxis("Vertical");
                transform.Translate(Vector3.forward * verticalInput * moveSpeed * Time.deltaTime);

                // Basic rotation (left/right)
                float horizontalInput = Input.GetAxis("Horizontal");
                transform.Rotate(Vector3.up * horizontalInput * rotationSpeed * Time.deltaTime);

                // Optional: Check current safe zone status by pressing Space (for demonstration)
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // Access the SafeZoneEntity component and call its example method
                    GetComponent<DesignPatterns.SafeZoneSystem.SafeZoneEntity>()?.PerformActionBasedOnZoneStatus();
                }
            }
        }
        ```
    *   Drag and drop `PlayerMovement.cs` onto your `Player` GameObject.

7.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Open the Console window (Window -> General -> Console).
    *   Use W/S to move the `Player` forward/backward and A/D to rotate (if you added the `PlayerMovement.cs`).
    *   **Observe the Console:**
        *   Move the `Player` into `HealingZone_01`. You should see `Debug.Log` messages indicating the player entered the zone, its type, and specific "healing" messages.
        *   Move the `Player` out of `HealingZone_01`. Messages should show the player exiting and any associated "stop healing" logic.
        *   Move the `Player` into `NoCombatZone_01`. Messages should show entry, its type, and "combat disabled" actions.
        *   Move the `Player` from one zone directly into another (e.g., from `HealingZone_01` to `NoCombatZone_01` without fully exiting all zones in between). Observe the sequence of exit/enter events. The `OnEntitySafeZoneStatusChanged` event should only fire when the entity *first* enters *any* zone, and *last* exits *all* zones.
        *   Press `Space` (if using `PlayerMovement.cs`) to query the player's current safe zone status via `PerformActionBasedOnZoneStatus()`.

This setup provides a complete, practical, and educational example of the SafeZoneSystem design pattern in Unity. You can extend the `SafeZoneType` enum and the `SafeZoneEntity`'s `HandleEntityEnteredSpecificZone`/`HandleEntityExitedSpecificZone` methods to implement any game-specific behavior you need.