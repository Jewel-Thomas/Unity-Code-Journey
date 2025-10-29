// Unity Design Pattern Example: ZoneTransitionSystem
// This script demonstrates the ZoneTransitionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Zone Transition System** design pattern. This pattern is ideal for managing game state, triggering events, and optimizing resources based on the player's (or another entity's) location within defined areas of the game world.

**Core Idea of Zone Transition System:**

1.  **Zones:** Defined geographical areas in your game world.
2.  **Triggers:** Colliders that represent the boundaries of these zones.
3.  **Detector:** An entity (usually the player) that can enter or exit these triggers.
4.  **Manager:** A central system that keeps track of the current zone, processes trigger events from detectors, and dispatches events when zone transitions occur.
5.  **Listeners:** Other game systems (e.g., Audio Manager, UI Manager, Quest Manager) that subscribe to the manager's events to react to zone changes.

---

### **Setup Instructions in Unity:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create C# Scripts:** Create the following C# scripts in your `Assets` folder and copy the code into them.
    *   `ZoneType.cs`
    *   `ZoneTrigger.cs`
    *   `PlayerZoneDetector.cs`
    *   `ZoneTransitionManager.cs`
    *   `GameManagerExample.cs` (This script requires TextMeshPro. If you don't have it, go to `Window > TextMeshPro > Import TMP Essential Resources` in Unity).
3.  **Create the ZoneTransitionManager:**
    *   Create an empty GameObject in your scene (e.g., named "GameManagers").
    *   Attach the `ZoneTransitionManager.cs` script to it. This will be your global singleton manager.
4.  **Create Zones:**
    *   For each distinct zone in your game (e.g., "Forest," "Town," "Cave"):
        *   Create an empty GameObject (e.g., `ForestZone`, `TownZone`).
        *   Add a `Collider` component (e.g., `Box Collider`). **Crucially, check the `Is Trigger` box.**
        *   Adjust the collider's size and position to define the boundaries of your zone.
        *   Attach the `ZoneTrigger.cs` script to this GameObject.
        *   In the Inspector, select the appropriate `ZoneType` from the dropdown for this zone.
5.  **Prepare your Player (or entity):**
    *   On your player character GameObject (or any GameObject that should detect zones):
        *   Ensure it has a `Collider` component (e.g., `Capsule Collider` for a character).
        *   Ensure it has a `Rigidbody` component. **Set `Is Kinematic` to true** if you don't want physics interactions but still need trigger detection.
        *   Attach the `PlayerZoneDetector.cs` script to it.
6.  **Create a Listener Example:**
    *   Create another empty GameObject (e.g., named "GameManager").
    *   Attach the `GameManagerExample.cs` script to it.
    *   In the Inspector, assign an `AudioSource` component (add one to the GameObject if needed), and some `AudioClip` assets for music.
    *   Create a UI `Canvas` and a `TextMeshPro - Text` element. Drag this TextMeshPro element to the `Zone Name Display` field in the `GameManagerExample` script's Inspector.
7.  **Run the Scene:** Walk your player character between the defined zone triggers, and observe the debug logs, UI updates, and music changes based on the `GameManagerExample`.

---

### **1. `ZoneType.cs`**
*Defines the different types of zones in your game.*

```csharp
// ZoneType.cs
using UnityEngine; // Not strictly needed for enum, but often included for Unity context

/// <summary>
/// Defines the different types of zones that can exist in the game world.
/// This enum makes it easy to categorize and reference specific zones.
/// </summary>
public enum ZoneType
{
    /// <summary>
    /// Default state, typically indicates no specific zone or an unassigned zone.
    /// </summary>
    None,

    /// <summary>
    /// The initial starting area for the player.
    /// </summary>
    StartingArea,

    /// <summary>
    /// The outer parts of a forest area.
    /// </summary>
    ForestOutskirts,

    /// <summary>
    /// The inner, denser part of a forest.
    /// </summary>
    DeepForest,

    /// <summary>
    /// A treacherous mountain passage.
    /// </summary>
    MountainPass,

    /// <summary>
    /// The entrance area to a cave.
    /// </summary>
    CaveEntrance,

    /// <summary>
    /// The deeper, inner sections of a cave.
    /// </summary>
    InnerCave,

    /// <summary>
    /// The main communal area of a town.
    /// </summary>
    TownSquare,

    /// <summary>
    /// The shopping district of a town.
    /// </summary>
    MarketDistrict,

    /// <summary>
    /// The player's personal dwelling.
    /// </summary>
    PlayerHouse
    
    // Add more zone types as needed for your game
}
```

---

### **2. `ZoneTrigger.cs`**
*Attached to GameObjects that define zone boundaries in the world.*

```csharp
// ZoneTrigger.cs
using UnityEngine;

/// <summary>
/// This component marks a GameObject as a 'zone' boundary.
/// It must be attached to a GameObject with a Collider component set to 'Is Trigger'.
/// When a PlayerZoneDetector enters/exits this collider, it reports the associated ZoneType.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure a Collider is present
public class ZoneTrigger : MonoBehaviour
{
    [Tooltip("The type of zone this trigger represents.")]
    public ZoneType zoneType = ZoneType.None;

    // Optional: Visual helper for the editor
    private void OnDrawGizmos()
    {
        // Draw a semi-transparent colored cube/sphere representing the collider bounds
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, semi-transparent

        // Store current matrix to restore after drawing
        Matrix4x4 oldGizmoMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            // Add other collider types if needed (e.g., CapsuleCollider)
        }

        Gizmos.matrix = oldGizmoMatrix; // Restore matrix

        // Draw the zone type name above the trigger for easy identification
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (col != null ? col.bounds.extents.y : 0.5f), zoneType.ToString(), style);
    }
}
```
*Note: The `UnityEditor.Handles.Label` part is an editor-only feature. It will only compile and work when you are in the Unity Editor. If you build your game, this line will cause an error unless it's wrapped in `#if UNITY_EDITOR`.*
Let's make that correction:

```csharp
// ZoneTrigger.cs
using UnityEngine;
// No need for UnityEditor if only using Gizmos
#if UNITY_EDITOR
using UnityEditor; // Required for Handles.Label
#endif

/// <summary>
/// This component marks a GameObject as a 'zone' boundary.
/// It must be attached to a GameObject with a Collider component set to 'Is Trigger'.
/// When a PlayerZoneDetector enters/exits this collider, it reports the associated ZoneType.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure a Collider is present
public class ZoneTrigger : MonoBehaviour
{
    [Tooltip("The type of zone this trigger represents.")]
    public ZoneType zoneType = ZoneType.None;

    // Optional: Visual helper for the editor
    private void OnDrawGizmos()
    {
        // Draw a semi-transparent colored cube/sphere representing the collider bounds
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, semi-transparent

        // Store current matrix to restore after drawing
        Matrix4x4 oldGizmoMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
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
                // A simplified drawing for Capsule, actual capsule drawing is more complex.
                // Draw a wire sphere at the center, representing its radius.
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
            // Add other collider types if needed
        }

        Gizmos.matrix = oldGizmoMatrix; // Restore matrix

        #if UNITY_EDITOR
        // Draw the zone type name above the trigger for easy identification
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(transform.position + Vector3.up * (col != null ? col.bounds.extents.y + 0.5f : 0.5f), zoneType.ToString(), style);
        #endif
    }
}
```

---

### **3. `PlayerZoneDetector.cs`**
*Attached to the player character or any entity that needs to trigger zone events.*

```csharp
// PlayerZoneDetector.cs
using UnityEngine;

/// <summary>
/// This component is attached to the player or any entity that needs to detect zone transitions.
/// It requires a Collider (set as Trigger) and a Rigidbody (set as Kinematic) to work correctly.
/// It reports enter/exit events to the central ZoneTransitionManager.
/// </summary>
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PlayerZoneDetector : MonoBehaviour
{
    private void Start()
    {
        // Ensure the collider is a trigger to detect zone boundaries without physical collision.
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[PlayerZoneDetector] Collider on {gameObject.name} is not set to 'Is Trigger'. Setting it now. Please set it in the inspector for best practice.", this);
            col.isTrigger = true;
        }

        // Ensure the Rigidbody is kinematic. This allows trigger detection without being affected
        // by physics forces, ideal for a player character that moves via script.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!rb.isKinematic)
        {
            Debug.LogWarning($"[PlayerZoneDetector] Rigidbody on {gameObject.name} is not kinematic. Setting it now. Please set it in the inspector for best practice.", this);
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Called when this GameObject's collider enters another trigger collider.
    /// </summary>
    /// <param name="other">The collider that was entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Try to get a ZoneTrigger component from the other collider's GameObject.
        ZoneTrigger zoneTrigger = other.GetComponent<ZoneTrigger>();
        if (zoneTrigger != null)
        {
            // If it's a zone trigger, notify the central manager.
            // Using ?.Invoke() ensures that if the manager is null, no error occurs.
            ZoneTransitionManager.Instance?.OnPlayerEnterZone(zoneTrigger.zoneType);
        }
    }

    /// <summary>
    /// Called when this GameObject's collider exits another trigger collider.
    /// </summary>
    /// <param name="other">The collider that was exited.</param>
    private void OnTriggerExit(Collider other)
    {
        ZoneTrigger zoneTrigger = other.GetComponent<ZoneTrigger>();
        if (zoneTrigger != null)
        {
            // If it's a zone trigger, notify the central manager.
            ZoneTransitionManager.Instance?.OnPlayerExitZone(zoneTrigger.zoneType);
        }
    }
}
```

---

### **4. `ZoneTransitionManager.cs`**
*The heart of the system. A singleton that manages current zone state and dispatches events.*

```csharp
// ZoneTransitionManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For .FirstOrDefault() if complex logic needed

/// <summary>
/// The central manager for the Zone Transition System pattern.
/// It tracks the player's current zone and dispatches events when zone transitions occur.
/// This class follows the Singleton pattern for easy global access.
/// </summary>
public class ZoneTransitionManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // Provides global access to the single instance of this manager.
    public static ZoneTransitionManager Instance { get; private set; }

    [Header("Current Zone State")]
    [Tooltip("The zone the player is currently considered to be in. Read-only at runtime.")]
    [SerializeField] // Serialize for debugging in inspector
    private ZoneType _currentZone = ZoneType.None;
    public ZoneType CurrentZone => _currentZone; // Public getter for the current zone

    // --- Events ---
    // Actions are used for events to allow multiple subscribers (listeners).

    /// <summary>
    /// Event fired when the player enters any zone's trigger collider.
    /// Parameters: ZoneType of the entered zone.
    /// Use this for immediate, broad reactions like showing a subtle "area entered" UI effect.
    /// </summary>
    public event Action<ZoneType> OnAnyZoneEntered;

    /// <summary>
    /// Event fired when the player exits any zone's trigger collider.
    /// Parameters: ZoneType of the exited zone.
    /// Use this for immediate, broad reactions like hiding a subtle "area entered" UI effect.
    /// </summary>
    public event Action<ZoneType> OnAnyZoneExited;

    /// <summary>
    /// **The primary event for zone-specific logic.**
    /// Fired when the player transitions from one distinct primary zone to another.
    /// This is where you'd typically handle major game state changes, like loading/unloading
    /// assets, changing music, updating primary UI elements, or triggering story events.
    /// Parameters: ZoneType of the previous zone, ZoneType of the new zone.
    /// </summary>
    public event Action<ZoneType, ZoneType> OnZoneTransition;

    /// <summary>
    /// Tracks all zones the player is currently overlapping with.
    /// This HashSet is crucial for handling cases where zones overlap,
    /// ensuring that _currentZone_ is managed robustly.
    /// </summary>
    private HashSet<ZoneType> _overlappingZones = new HashSet<ZoneType>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern to ensure only one instance exists.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one to maintain uniqueness.
            Debug.LogWarning("[ZoneTransitionManager] Multiple instances detected! Destroying duplicate.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make this object persistent across scene loads.
            // Be cautious with this: if scenes load different ZoneTransitionManagers,
            // or if references to this manager are not handled correctly across scenes,
            // it can lead to issues. For simple games, it's often fine.
            // DontDestroyOnLoad(gameObject);
            Debug.Log("[ZoneTransitionManager] Initialized.", this);
        }
    }

    /// <summary>
    /// Called by a PlayerZoneDetector when it enters a ZoneTrigger's collider.
    /// This method updates the internal state and dispatches relevant events.
    /// </summary>
    /// <param name="zoneType">The type of the zone that was entered.</param>
    public void OnPlayerEnterZone(ZoneType zoneType)
    {
        if (zoneType == ZoneType.None) return; // Ignore unassigned zones

        // Add the zone to our set of currently overlapping zones.
        // HashSet.Add returns true if the element was added (i.e., it wasn't already present).
        if (_overlappingZones.Add(zoneType))
        {
            Debug.Log($"[ZoneTransitionManager] Player entered zone trigger: {zoneType}");
            OnAnyZoneEntered?.Invoke(zoneType); // Notify listeners of any zone entry

            // If the player is entering a new zone, and it's different from the _currentZone_,
            // then a significant transition might have occurred.
            if (_currentZone != zoneType)
            {
                ZoneType previousZone = _currentZone;
                _currentZone = zoneType; // Set the newly entered zone as the current primary zone.
                                         // (Simple logic: most recently entered is current. See GetHighestPriorityOverlappingZone for more complex ideas).
                Debug.Log($"[ZoneTransitionManager] Primary zone transition: {previousZone} -> {_currentZone}");
                OnZoneTransition?.Invoke(previousZone, _currentZone); // Notify listeners of primary transition
            }
        }
        else
        {
            // Player entered a zone trigger they were already overlapping with.
            // This can happen with overlapping colliders, or if the player briefly exited
            // and re-entered before OnTriggerExit could fire. No new "entry" event is needed.
            Debug.Log($"[ZoneTransitionManager] Player re-entered {zoneType}, already overlapping.", this);
        }
    }

    /// <summary>
    /// Called by a PlayerZoneDetector when it exits a ZoneTrigger's collider.
    /// This method updates the internal state and dispatches relevant events.
    /// </summary>
    /// <param name="zoneType">The type of the zone that was exited.</param>
    public void OnPlayerExitZone(ZoneType zoneType)
    {
        if (zoneType == ZoneType.None) return; // Ignore unassigned zones

        // Remove the zone from our set of currently overlapping zones.
        // HashSet.Remove returns true if the element was removed (i.e., it was present).
        if (_overlappingZones.Remove(zoneType))
        {
            Debug.Log($"[ZoneTransitionManager] Player exited zone trigger: {zoneType}");
            OnAnyZoneExited?.Invoke(zoneType); // Notify listeners of any zone exit

            // If the exited zone was our _currentZone_, we need to re-evaluate what the new _currentZone_ should be.
            if (_currentZone == zoneType)
            {
                ZoneType previousZone = _currentZone;

                // Determine the new primary current zone.
                // This is crucial for handling situations where zones overlap or a player exits to 'None'.
                _currentZone = GetHighestPriorityOverlappingZone();

                // If the current zone has actually changed (e.g., player moved to another overlapping zone,
                // or exited all zones to 'None'), then a transition has occurred.
                if (_currentZone != previousZone)
                {
                    Debug.Log($"[ZoneTransitionManager] Primary zone transition (due to exit): {previousZone} -> {_currentZone}");
                    OnZoneTransition?.Invoke(previousZone, _currentZone); // Notify listeners of primary transition
                }
            }
        }
        else
        {
            // Player exited a zone trigger they were not previously overlapping with.
            // This shouldn't happen under normal circumstances if OnTriggerEnter/Exit pairs are correct.
            Debug.LogWarning($"[ZoneTransitionManager] Player exited {zoneType}, but was not marked as overlapping with it.", this);
        }
    }

    /// <summary>
    /// Determines the "most important" zone among the currently overlapping ones.
    /// This is crucial for handling overlapping zones gracefully.
    ///
    /// **Example Priority Logic:**
    /// 1.  **Simple (Current Implementation):** Just returns the first element in the hash set, or `ZoneType.None`.
    ///     This works but offers no real "priority."
    /// 2.  **Explicit Priority:** Add a `priority` field to `ZoneTrigger.cs`. Iterate `_overlappingZones`
    ///     and find the `ZoneTrigger` instance with the highest priority.
    /// 3.  **Smallest Zone Wins:** If multiple zones overlap, the player is usually "more" in the smallest zone.
    ///     You would need references to the `ZoneTrigger` objects and their `Collider.bounds.size`.
    /// 4.  **Specific Zone Override:** Certain zones (e.g., 'PlayerHouse') might always take precedence.
    ///
    /// For this example, we'll keep it simple, just picking one from the set.
    /// </summary>
    /// <returns>The ZoneType of the highest priority overlapping zone, or ZoneType.None if no zones are overlapping.</returns>
    private ZoneType GetHighestPriorityOverlappingZone()
    {
        if (_overlappingZones.Count > 0)
        {
            // Simple approach: just pick the first one found.
            // For a real game, you'd implement more robust priority logic here.
            return _overlappingZones.FirstOrDefault(); // Requires System.Linq
        }
        return ZoneType.None; // No zones currently overlapping
    }

    // --- Editor-only Debug Functionality ---
    // Displays current zone information directly on the screen in the editor.
    private void OnGUI()
    {
        // Only show debug GUI in editor or development builds
        if (!Debug.isDebugBuild && !Application.isEditor) return;

        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, 10, 300, 25), $"Current Primary Zone: {_currentZone}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Overlapping Triggers: {string.Join(", ", _overlappingZones)}");
        GUI.color = Color.white;
    }
}
```

---

### **5. `GameManagerExample.cs`**
*An example of a listener that reacts to zone transition events.*

```csharp
// GameManagerExample.cs
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI

/// <summary>
/// An example MonoBehaviour that demonstrates how to subscribe to and react to
/// ZoneTransitionManager events. This would typically be part of your main
/// game manager, audio manager, UI manager, or any other system that needs
/// to know about zone changes.
/// </summary>
public class GameManagerExample : MonoBehaviour
{
    [Header("UI & Audio References (for example)")]
    public AudioSource backgroundMusicPlayer;
    public AudioClip defaultMusic;
    public AudioClip forestMusic;
    public AudioClip townMusic;
    public AudioClip caveMusic;
    [Tooltip("Requires a TextMeshProUGUI component on a Canvas.")]
    public TextMeshProUGUI zoneNameDisplay;

    /// <summary>
    /// Subscribes to ZoneTransitionManager events when this component becomes enabled.
    /// It's crucial to check for `Instance != null` as the manager might not be initialized yet.
    /// </summary>
    private void OnEnable()
    {
        if (ZoneTransitionManager.Instance != null)
        {
            ZoneTransitionManager.Instance.OnZoneTransition += HandleZoneTransition;
            ZoneTransitionManager.Instance.OnAnyZoneEntered += HandleAnyZoneEntered;
            ZoneTransitionManager.Instance.OnAnyZoneExited += HandleAnyZoneExited;
            Debug.Log("[GameManagerExample] Subscribed to ZoneTransitionManager events.");
        }
        else
        {
            Debug.LogError("[GameManagerExample] ZoneTransitionManager instance not found! Make sure it's in the scene and initialized before GameManagerExample.", this);
        }
    }

    /// <summary>
    /// Unsubscribes from ZoneTransitionManager events when this component is disabled or destroyed.
    /// This is vital to prevent memory leaks and null reference exceptions if the manager is destroyed.
    /// </summary>
    private void OnDisable()
    {
        if (ZoneTransitionManager.Instance != null)
        {
            ZoneTransitionManager.Instance.OnZoneTransition -= HandleZoneTransition;
            ZoneTransitionManager.Instance.OnAnyZoneEntered -= HandleAnyZoneEntered;
            ZoneTransitionManager.Instance.OnAnyZoneExited -= HandleAnyZoneExited;
            Debug.Log("[GameManagerExample] Unsubscribed from ZoneTransitionManager events.");
        }
    }

    /// <summary>
    /// Called once at the start. Sets up initial state based on the current zone
    /// (useful if the player starts directly inside a zone).
    /// </summary>
    private void Start()
    {
        if (ZoneTransitionManager.Instance != null)
        {
            // Simulate an initial transition to set up UI/music based on starting zone.
            HandleZoneTransition(ZoneType.None, ZoneTransitionManager.Instance.CurrentZone);
        }
    }

    /// <summary>
    /// This method is called when the player transitions from one distinct primary zone to another.
    /// It's the ideal place for zone-specific logic like:
    /// - Loading/unloading scene sections or assets
    /// - Changing background music or ambient sounds
    /// - Updating primary UI elements (e.g., zone name display, minimap)
    /// - Triggering story events, quests, or tutorials
    /// - Saving/loading game state specific to a zone
    /// </summary>
    /// <param name="oldZone">The zone the player just left (could be ZoneType.None).</param>
    /// <param name="newZone">The zone the player just entered (could be ZoneType.None if exiting all zones).</param>
    private void HandleZoneTransition(ZoneType oldZone, ZoneType newZone)
    {
        Debug.Log($"[GameManagerExample] Player transitioned from {oldZone} to {newZone}. Performing actions...");

        // --- Example: Update UI ---
        if (zoneNameDisplay != null)
        {
            zoneNameDisplay.text = $"Current Area: {newZone}";
            zoneNameDisplay.color = Color.yellow;
        }

        // --- Example: Change background music ---
        if (backgroundMusicPlayer != null)
        {
            AudioClip targetMusic = defaultMusic; // Fallback music
            switch (newZone)
            {
                case ZoneType.ForestOutskirts:
                case ZoneType.DeepForest:
                    targetMusic = forestMusic;
                    break;
                case ZoneType.TownSquare:
                case ZoneType.MarketDistrict:
                    targetMusic = townMusic;
                    break;
                case ZoneType.CaveEntrance:
                case ZoneType.InnerCave:
                    targetMusic = caveMusic;
                    break;
                case ZoneType.StartingArea:
                case ZoneType.PlayerHouse:
                    targetMusic = defaultMusic; // Or specific home music
                    break;
                case ZoneType.None:
                    targetMusic = defaultMusic; // No specific zone, play default or silence
                    break;
            }

            // Only change music if it's different from the current one to avoid restarting tracks unnecessarily.
            if (backgroundMusicPlayer.clip != targetMusic)
            {
                backgroundMusicPlayer.clip = targetMusic;
                backgroundMusicPlayer.Play();
                Debug.Log($"[GameManagerExample] Changed music to: {targetMusic?.name ?? "None"}");
            }
        }

        // --- Example: Trigger specific game events based on specific transitions ---
        if (newZone == ZoneType.DeepForest && oldZone == ZoneType.ForestOutskirts)
        {
            Debug.Log("[GameManagerExample] Player entered Deep Forest for the first time from Forest Outskirts! Triggering story event.");
            // Example: QuestManager.Instance.TriggerQuestEvent("EnteredDeepForest");
            // Example: Spawn a specific enemy type unique to DeepForest.
        }
        else if (newZone == ZoneType.TownSquare)
        {
            Debug.Log("[GameManagerExample] Player arrived at Town Square! Performing autosave.");
            // Example: SaveGameManager.Instance.AutoSave();
        }
        else if (oldZone == ZoneType.TownSquare && newZone == ZoneType.None)
        {
            Debug.Log("[GameManagerExample] Player left Town Square. Hiding merchant UI.");
            // Example: UIManager.Instance.HideMerchantPanel();
        }

        // Add other zone-specific logic here (e.g., character behavior changes, enemy spawns)
    }

    /// <summary>
    /// This method is called when the player's detector enters ANY zone's collider,
    /// regardless of whether it results in a *primary* zone transition.
    /// Useful for immediate, localized feedback or effects that don't depend on the overall game state.
    /// </summary>
    /// <param name="enteredZone">The type of the zone collider just entered.</param>
    private void HandleAnyZoneEntered(ZoneType enteredZone)
    {
        Debug.Log($"[GameManagerExample] Player detected entering {enteredZone} trigger (minor event).");
        // Example: Play a subtle "zone boundary crossed" sound effect.
        // Example: Temporarily show a mini-map region highlight.
        // Example: Update an internal counter for visited zones.
    }

    /// <summary>
    /// This method is called when the player's detector exits ANY zone's collider.
    /// Similar to `HandleAnyZoneEntered`, but for exiting triggers.
    /// </summary>
    /// <param name="exitedZone">The type of the zone collider just exited.</param>
    private void HandleAnyZoneExited(ZoneType exitedZone)
    {
        Debug.Log($"[GameManagerExample] Player detected exiting {exitedZone} trigger (minor event).");
        // Example: Clear a mini-map region highlight for the exited trigger.
    }
}
```