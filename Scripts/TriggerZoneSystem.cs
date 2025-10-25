// Unity Design Pattern Example: TriggerZoneSystem
// This script demonstrates the TriggerZoneSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `TriggerZoneSystem` design pattern in Unity provides a robust and scalable way to manage interactive areas (trigger zones) in your game world. It typically involves:

1.  **A central Manager/System:** To register and provide access to all active trigger zones.
2.  **Individual TriggerZone Components:** Attached to game objects, defining the zone's area and handling physics trigger events.
3.  **Event-driven Communication:** Trigger zones emit events when objects enter or exit, allowing other scripts to react without direct coupling.

This setup decouples the trigger logic from the objects that react to it, making your game architecture cleaner and easier to maintain.

---

### **1. `TriggerZoneManager.cs`**
This script acts as the central registry for all `TriggerZone` instances in the scene. It ensures that any other script can easily find and interact with specific zones by their unique ID.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary

/// <summary>
/// TriggerZoneManager: A central singleton manager for all TriggerZone components in the scene.
/// It provides a registry for zones and allows other systems to find them by ID.
/// This acts as the 'System' part of the TriggerZoneSystem pattern.
/// </summary>
public class TriggerZoneManager : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static TriggerZoneManager Instance { get; private set; }

    // A dictionary to store active TriggerZones, keyed by their unique ID.
    private Dictionary<string, TriggerZone> _activeTriggerZones = new Dictionary<string, TriggerZone>();

    // Called when the script instance is being loaded.
    private void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TriggerZoneManager: Duplicate instance found, destroying this one. " +
                             "Only one TriggerZoneManager should exist per scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make this manager persist across scene loads.
        // If you want a scene-specific manager, remove Don'tDestroyOnLoad.
        // DontDestroyOnLoad(gameObject);

        Debug.Log("TriggerZoneManager initialized.");
    }

    // Called when the MonoBehaviour will be destroyed.
    private void OnDestroy()
    {
        // Clean up the singleton reference if this is the active instance.
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("TriggerZoneManager destroyed.");
        }
    }

    /// <summary>
    /// Registers a TriggerZone with the manager.
    /// Called by individual TriggerZone components when they become active.
    /// </summary>
    /// <param name="zone">The TriggerZone to register.</param>
    public void RegisterZone(TriggerZone zone)
    {
        if (string.IsNullOrWhiteSpace(zone.zoneID))
        {
            Debug.LogError($"TriggerZoneManager: Attempted to register a zone without a valid ID. " +
                           $"Zone on GameObject '{zone.gameObject.name}' will not be managed.", zone);
            return;
        }

        if (_activeTriggerZones.ContainsKey(zone.zoneID))
        {
            Debug.LogWarning($"TriggerZoneManager: A zone with ID '{zone.zoneID}' already exists. " +
                             $"The new zone on '{zone.gameObject.name}' will overwrite the existing one.", zone);
            _activeTriggerZones[zone.zoneID] = zone; // Overwrite or handle as an error, depending on desired behavior.
        }
        else
        {
            _activeTriggerZones.Add(zone.zoneID, zone);
            Debug.Log($"TriggerZoneManager: Registered zone '{zone.zoneID}' on GameObject '{zone.gameObject.name}'.");
        }
    }

    /// <summary>
    /// Unregisters a TriggerZone from the manager.
    /// Called by individual TriggerZone components when they are disabled or destroyed.
    /// </summary>
    /// <param name="zone">The TriggerZone to unregister.</param>
    public void UnregisterZone(TriggerZone zone)
    {
        if (string.IsNullOrWhiteSpace(zone.zoneID))
        {
            // This might happen if a zone was never registered properly due to missing ID
            return;
        }

        if (_activeTriggerZones.Remove(zone.zoneID))
        {
            Debug.Log($"TriggerZoneManager: Unregistered zone '{zone.zoneID}' from GameObject '{zone.gameObject.name}'.");
        }
        else
        {
            // This can happen if the zone was never registered or already removed.
            Debug.LogWarning($"TriggerZoneManager: Attempted to unregister zone '{zone.zoneID}' which was not found in the active zones list.", zone);
        }
    }

    /// <summary>
    /// Retrieves a registered TriggerZone by its unique ID.
    /// </summary>
    /// <param name="zoneID">The unique ID of the zone to retrieve.</param>
    /// <returns>The TriggerZone component if found, otherwise null.</returns>
    public TriggerZone GetZone(string zoneID)
    {
        if (_activeTriggerZones.TryGetValue(zoneID, out TriggerZone zone))
        {
            return zone;
        }
        Debug.LogWarning($"TriggerZoneManager: Zone with ID '{zoneID}' not found.");
        return null;
    }
}
```

---

### **2. `TriggerZone.cs`**
This script defines an individual triggerable area. It requires a Collider (set to `Is Trigger`) and handles `OnTriggerEnter`/`OnTriggerExit` events, then notifies listeners via `UnityEvent`s.

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.Collections.Generic; // Required for HashSet

/// <summary>
/// TriggerZone: A component that defines an interactive area in the game world.
/// It uses a Collider (set to 'Is Trigger') to detect objects entering and exiting.
/// It registers itself with the TriggerZoneManager and provides UnityEvents for easy hook-up.
/// This acts as the 'TriggerZone' part of the TriggerZoneSystem pattern.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensures a Collider component is present.
public class TriggerZone : MonoBehaviour
{
    [Tooltip("A unique identifier for this trigger zone. Used by the TriggerZoneManager.")]
    public string zoneID;

    [Tooltip("The tag of GameObjects that can trigger this zone (e.g., 'Player', 'Enemy').")]
    public string targetTag = "Player";

    [Tooltip("Event fired when an object with the target tag enters this zone.")]
    public UnityEvent OnZoneEnter;

    [Tooltip("Event fired when an object with the target tag exits this zone.")]
    public UnityEvent OnZoneExit;

    // A list to keep track of GameObjects currently inside the trigger zone,
    // filtered by the targetTag. This is useful if you need to know which specific objects are inside.
    private HashSet<GameObject> _objectsInZone = new HashSet<GameObject>();

    // Called in the editor when the script is loaded or a value is changed in the Inspector.
    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"TriggerZone on GameObject '{gameObject.name}': Collider must be set to 'Is Trigger'. " +
                             $"Setting it automatically.", this);
            col.isTrigger = true;
        }

        if (string.IsNullOrWhiteSpace(zoneID))
        {
            Debug.LogWarning($"TriggerZone on GameObject '{gameObject.name}': 'zoneID' is empty. " +
                             $"Please provide a unique ID.", this);
        }
    }

    // Called when the object becomes enabled and active.
    private void OnEnable()
    {
        // Register this zone with the central manager.
        // We check for Instance being null in case the manager hasn't initialized yet
        // or was destroyed. It's safer to check for null here.
        if (TriggerZoneManager.Instance != null)
        {
            TriggerZoneManager.Instance.RegisterZone(this);
        }
        else
        {
            Debug.LogError("TriggerZoneManager not found! Make sure there's an active TriggerZoneManager in the scene.", this);
        }
    }

    // Called when the object becomes disabled or inactive.
    private void OnDisable()
    {
        // Unregister this zone from the central manager.
        if (TriggerZoneManager.Instance != null)
        {
            TriggerZoneManager.Instance.UnregisterZone(this);
        }
        // Clear any objects tracked within the zone upon disable.
        _objectsInZone.Clear();
    }

    /// <summary>
    /// Unity's physics callback when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The Collider that entered the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Filter by tag to ensure only relevant objects trigger the zone.
        if (other.CompareTag(targetTag))
        {
            // Add the entering object to our tracking list.
            if (_objectsInZone.Add(other.gameObject)) // Add returns true if the item was added (wasn't already there)
            {
                Debug.Log($"'{other.gameObject.name}' (Tag: {other.tag}) entered zone '{zoneID}'.", other.gameObject);
                OnZoneEnter.Invoke(); // Invoke the UnityEvent for subscribers.
            }
        }
    }

    /// <summary>
    /// Unity's physics callback when another collider exits this trigger.
    /// </summary>
    /// <param name="other">The Collider that exited the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        // Filter by tag.
        if (other.CompareTag(targetTag))
        {
            // Remove the exiting object from our tracking list.
            if (_objectsInZone.Remove(other.gameObject)) // Remove returns true if the item was removed (was present)
            {
                Debug.Log($"'{other.gameObject.name}' (Tag: {other.tag}) exited zone '{zoneID}'.", other.gameObject);
                OnZoneExit.Invoke(); // Invoke the UnityEvent for subscribers.
            }
        }
    }

    /// <summary>
    /// Checks if a specific GameObject is currently inside this trigger zone.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if the object is inside and tracked, false otherwise.</returns>
    public bool IsObjectInZone(GameObject obj)
    {
        return _objectsInZone.Contains(obj);
    }

    /// <summary>
    /// Returns a list of all GameObjects currently inside this trigger zone.
    /// Note: This returns a copy of the list, so external modifications don't affect internal state.
    /// </summary>
    public List<GameObject> GetObjectsInZone()
    {
        return new List<GameObject>(_objectsInZone);
    }
}
```

---

### **3. `ZoneEffectHandler.cs`**
This is an example script demonstrating how another component might react to a `TriggerZone`. It shows both dynamic subscription in code and how you would assign it in the Inspector.

```csharp
using UnityEngine;
using UnityEngine.Events; // Not strictly needed for this script, but good practice if using more UnityEvents.

/// <summary>
/// ZoneEffectHandler: An example script that subscribes to a TriggerZone's events
/// and performs an action when the zone is entered or exited.
/// This demonstrates how other components can react to the TriggerZoneSystem.
/// </summary>
public class ZoneEffectHandler : MonoBehaviour
{
    [Tooltip("The TriggerZone this handler will listen to. Assign in the Inspector.")]
    public TriggerZone targetZone;

    [Tooltip("Message to log when the zone is entered.")]
    public string enterMessage = "Zone Entered!";

    [Tooltip("Message to log when the zone is exited.")]
    public string exitMessage = "Zone Exited!";

    [Tooltip("Optional: An event to invoke when the zone is entered, for additional actions.")]
    public UnityEvent OnHandlerZoneEnter;

    [Tooltip("Optional: An event to invoke when the zone is exited, for additional actions.")]
    public UnityEvent OnHandlerZoneExit;


    // Called when the object becomes enabled and active.
    private void OnEnable()
    {
        // Ensure the targetZone is assigned.
        if (targetZone == null)
        {
            Debug.LogWarning($"ZoneEffectHandler on '{gameObject.name}': targetZone is not assigned. " +
                             "This handler will not function.", this);
            return;
        }

        // Dynamically subscribe to the target zone's events.
        // This is a common way to connect components in code.
        targetZone.OnZoneEnter.AddListener(HandleZoneEnter);
        targetZone.OnZoneExit.AddListener(HandleZoneExit);
        Debug.Log($"ZoneEffectHandler on '{gameObject.name}' is now listening to zone '{targetZone.zoneID}'.");
    }

    // Called when the object becomes disabled or inactive.
    private void OnDisable()
    {
        // Always unsubscribe from events to prevent memory leaks or calling null objects
        // if the targetZone outlives this handler.
        if (targetZone != null)
        {
            targetZone.OnZoneEnter.RemoveListener(HandleZoneEnter);
            targetZone.OnZoneExit.RemoveListener(HandleZoneExit);
            Debug.Log($"ZoneEffectHandler on '{gameObject.name}' stopped listening to zone '{targetZone.zoneID}'.");
        }
    }

    /// <summary>
    /// Callback method invoked when the target zone is entered.
    /// </summary>
    private void HandleZoneEnter()
    {
        Debug.Log($"<color=green>[ZoneEffectHandler] '{gameObject.name}' received ENTER from zone '{targetZone.zoneID}':</color> {enterMessage}", this);
        OnHandlerZoneEnter.Invoke(); // Invoke additional actions if set up in the Inspector.
    }

    /// <summary>
    /// Callback method invoked when the target zone is exited.
    /// </summary>
    private void HandleZoneExit()
    {
        Debug.Log($"<color=red>[ZoneEffectHandler] '{gameObject.name}' received EXIT from zone '{targetZone.zoneID}':</color> {exitMessage}", this);
        OnHandlerZoneExit.Invoke(); // Invoke additional actions if set up in the Inspector.
    }

    /// <summary>
    /// Example method to call from a UnityEvent directly in the Inspector.
    /// </summary>
    public void ActivateSomething()
    {
        Debug.Log($"<color=yellow>[ZoneEffectHandler] '{gameObject.name}' Activated Something via UnityEvent!</color>", this);
    }
}
```

---

### **How to Use This in Unity (Example Setup):**

1.  **Create a `TriggerZoneManager` GameObject:**
    *   Create an empty GameObject in your scene (e.g., `_Managers`).
    *   Attach the `TriggerZoneManager.cs` script to it. This will ensure the manager is active.

2.  **Create a `Player` GameObject:**
    *   Create a 3D Capsule (or any suitable object).
    *   Rename it to `Player`.
    *   **Add a Tag:** Select the `Player` GameObject, in the Inspector, click the "Tag" dropdown, select "Add Tag...", add a new tag named `Player`, then re-select your `Player` GameObject and assign the `Player` tag to it.
    *   **Add a Collider:** Ensure it has a Collider (e.g., `CapsuleCollider`). This collider *should NOT* be set to `Is Trigger`. It will be the "moving object" that interacts.
    *   **Add a Rigidbody:** Add a `Rigidbody` component. Check `Is Kinematic` on the Rigidbody if you want to control its movement manually without physics affecting it, but still have it trigger collisions. This is crucial for `OnTriggerEnter`/`OnTriggerExit` callbacks to work correctly when one of the objects is a trigger. If both are non-kinematic rigidbodies, physics will handle it normally. For simple trigger detection, one needs a Rigidbody (can be kinematic) and the other a trigger collider.

3.  **Create a `TriggerZone` GameObject:**
    *   Create a 3D Cube (or any suitable object).
    *   Rename it to `MyQuestTriggerZone`.
    *   **Add `TriggerZone.cs` script:** Attach the script.
    *   **Configure `TriggerZone` component:**
        *   Set `Zone ID` to `QuestStartZone`. (This is how you'll refer to it.)
        *   Set `Target Tag` to `Player`.
        *   **Collider:** Ensure its `BoxCollider` (or chosen collider type) has `Is Trigger` **checked**. (The `OnValidate` in the script will try to do this automatically if you forget).
        *   **Rigidbody:** For consistency and reliable trigger callbacks, add a `Rigidbody` component. Check `Is Kinematic` on it. (Even though it's stationary, Unity's physics sometimes requires a Rigidbody on at least one of the interacting objects for trigger events to fire).
        *   **UnityEvents (`OnZoneEnter`, `OnZoneExit`):** You can add direct actions here. For instance, add a listener to `OnZoneEnter`, drag the `Player` GameObject into the slot, and select `Debug.Log(string)` to log a message like "Player entered quest zone via Inspector!".

4.  **Create a `ZoneEffectHandler` GameObject:**
    *   Create an empty GameObject (e.g., `QuestLogger`).
    *   Attach the `ZoneEffectHandler.cs` script to it.
    *   **Configure `ZoneEffectHandler` component:**
        *   Drag your `MyQuestTriggerZone` GameObject from the Hierarchy into the `Target Zone` slot in the Inspector.
        *   You can customize `Enter Message` and `Exit Message`.
        *   **Optional `OnHandlerZoneEnter`/`OnHandlerZoneExit`:** You can add additional UnityEvents here that will fire when the handler's events are called. For example, you could drag the `QuestLogger` GameObject itself into the `OnHandlerZoneEnter` slot and select `ZoneEffectHandler.ActivateSomething()` to see it in action.

**Run the Scene:**
Move your `Player` GameObject (either manually in the Scene view or with a simple movement script) into and out of the `MyQuestTriggerZone`. Observe the Debug.Log messages from `TriggerZoneManager`, `TriggerZone`, and `ZoneEffectHandler` indicating when the player enters and exits the zone.

This complete example provides a robust and extensible `TriggerZoneSystem` ready for use in various game scenarios, such as quest triggers, area-specific effects, enemy spawns, and more.