// Unity Design Pattern Example: PointOfInterestSystem
// This script demonstrates the PointOfInterestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the PointOfInterestSystem design pattern in Unity. This pattern provides a centralized registry for objects in your scene that can be discovered, queried, and interacted with by other systems (e.g., a player, UI, AI). It decouples the discoverer from the discovered, making your game more modular and scalable.

## PointOfInterestSystem Design Pattern Overview

The PointOfInterestSystem pattern typically involves:

1.  **PointOfInterest (POI) Component:** A script attached to game objects that represent a point of interest. It registers itself with the central system.
2.  **PointOfInterestSystem Manager:** A singleton or static class that maintains a collection of all registered POIs. It provides methods to register, unregister, and query POIs (e.g., find nearest, find all of a certain type, find all within a radius).
3.  **Client (e.g., Player, UI, AI):** Uses the PointOfInterestSystem Manager to discover and interact with POIs without needing to know their specific implementations or how they are located in the scene.

---

## **1. PoiType.cs (Enum Definition)**

This enum defines different categories or types for your Points of Interest. This allows for flexible querying.

```csharp
// PoiType.cs
using System;

/// <summary>
/// Defines the various types of Points of Interest that can exist in the game.
/// This enum allows for categorization and filtering when querying the PointOfInterestSystem.
/// 'Any' is a special value used for queries that don't need type-specific filtering.
/// </summary>
public enum PoiType
{
    Any = 0, // Used for queries where the type doesn't matter
    Generic = 1,
    QuestGiver = 2,
    Shop = 3,
    Collectible = 4,
    EnemySpawn = 5,
    DungeonEntrance = 6,
    HealingStation = 7,
    ResourceNode = 8
    // Add more types as your game needs them
}

```

---

## **2. PointOfInterest.cs (POI Component)**

This `MonoBehaviour` represents an actual point of interest in your Unity scene. It handles its own registration and unregistration with the `PointOfInterestSystem`.

```csharp
// PointOfInterest.cs
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System; // Required for Action

/// <summary>
/// Represents a single Point of Interest in the game world.
/// GameObjects with this component will automatically register themselves
/// with the PointOfInterestSystem when enabled, and unregister when disabled.
/// </summary>
public class PointOfInterest : MonoBehaviour
{
    [Tooltip("The type of this Point of Interest.")]
    [SerializeField] private PoiType poiType = PoiType.Generic;

    [Tooltip("Optional: A unique string ID for this specific POI. If left empty, a GUID will be assigned.")]
    [SerializeField] private string uniqueID = "";

    [Tooltip("UnityEvent triggered when this POI is interacted with.")]
    public UnityEvent OnInteract = new UnityEvent();

    /// <summary>
    /// Gets the type of this Point of Interest.
    /// </summary>
    public PoiType Type => poiType;

    /// <summary>
    /// Gets the world position of this Point of Interest.
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// Gets the unique ID of this Point of Interest.
    /// If no uniqueID is set in the inspector, a GUID is generated on Awake.
    /// </summary>
    public string UniqueID => uniqueID;

    private void Awake()
    {
        // Ensure a unique ID exists for this POI.
        // This is useful if you need to specifically reference a POI from a save file or quest log.
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
            // Optional: If you want to make it visible in editor even if auto-generated:
            // Debug.LogWarning($"PointOfInterest on {gameObject.name} has no unique ID. Assigning: {uniqueID}");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Registers this POI with the central PointOfInterestSystem.
    /// </summary>
    private void OnEnable()
    {
        PointOfInterestSystem.Instance.RegisterPOI(this);
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// Unregisters this POI from the central PointOfInterestSystem.
    /// </summary>
    private void OnDisable()
    {
        // Only unregister if the system instance still exists (e.g., not during application quit)
        if (PointOfInterestSystem.Instance != null)
        {
            PointOfInterestSystem.Instance.UnregisterPOI(this);
        }
    }

    /// <summary>
    /// Public method to trigger interaction with this POI.
    /// Other systems (like a Player Controller) can call this.
    /// </summary>
    public void Interact()
    {
        Debug.Log($"Interacting with POI: {gameObject.name} (Type: {Type}, ID: {UniqueID}) at {Position}");
        OnInteract?.Invoke(); // Trigger the UnityEvent
    }

    /// <summary>
    /// Optional: Visual representation in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // Draw a small sphere at POI position
    }
}
```

---

## **3. PointOfInterestSystem.cs (Manager Singleton)**

This is the core manager class. It's a `MonoBehaviour` singleton that maintains a list of all active POIs and provides methods for other systems to query them.

```csharp
// PointOfInterestSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System; // Required for Action delegate

/// <summary>
/// The central manager for all Points of Interest in the game.
/// This is a Singleton, meaning there will only be one instance of it throughout the game.
/// It provides methods to register, unregister, and query POIs based on various criteria.
/// </summary>
public class PointOfInterestSystem : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static PointOfInterestSystem _instance;
    public static PointOfInterestSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PointOfInterestSystem>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(PointOfInterestSystem).Name);
                    _instance = singletonObject.AddComponent<PointOfInterestSystem>();
                }
                DontDestroyOnLoad(_instance.gameObject); // Keep the system alive across scenes
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    // --- End Singleton Pattern Implementation ---


    // Stores all currently active Points of Interest.
    private List<PointOfInterest> _registeredPOIs = new List<PointOfInterest>();
    // Optional: Dictionary for fast lookup by unique ID if needed frequently.
    private Dictionary<string, PointOfInterest> _poiByID = new Dictionary<string, PointOfInterest>();

    /// <summary>
    /// Event triggered when a new PointOfInterest is registered.
    /// Subscribers can react to new POIs appearing in the world.
    /// </summary>
    public event Action<PointOfInterest> OnPoiRegistered;

    /// <summary>
    /// Event triggered when a PointOfInterest is unregistered.
    /// Subscribers can react to POIs disappearing from the world.
    /// </summary>
    public event Action<PointOfInterest> OnPoiUnregistered;


    /// <summary>
    /// Registers a PointOfInterest with the system.
    /// Called automatically by the PointOfInterest component's OnEnable method.
    /// </summary>
    /// <param name="poi">The PointOfInterest to register.</param>
    public void RegisterPOI(PointOfInterest poi)
    {
        if (poi == null)
        {
            Debug.LogError("Attempted to register a null PointOfInterest.");
            return;
        }
        if (_registeredPOIs.Contains(poi))
        {
            // Debug.LogWarning($"PointOfInterest '{poi.name}' already registered.");
            return;
        }

        _registeredPOIs.Add(poi);
        _poiByID[poi.UniqueID] = poi; // Add to dictionary for ID lookup
        OnPoiRegistered?.Invoke(poi);
        // Debug.Log($"Registered POI: {poi.gameObject.name} (Type: {poi.Type}, ID: {poi.UniqueID}). Total POIs: {_registeredPOIs.Count}");
    }

    /// <summary>
    /// Unregisters a PointOfInterest from the system.
    /// Called automatically by the PointOfInterest component's OnDisable method.
    /// </summary>
    /// <param name="poi">The PointOfInterest to unregister.</param>
    public void UnregisterPOI(PointOfInterest poi)
    {
        if (poi == null)
        {
            Debug.LogError("Attempted to unregister a null PointOfInterest.");
            return;
        }

        if (_registeredPOIs.Remove(poi))
        {
            _poiByID.Remove(poi.UniqueID); // Remove from dictionary
            OnPoiUnregistered?.Invoke(poi);
            // Debug.Log($"Unregistered POI: {poi.gameObject.name} (Type: {poi.Type}, ID: {poi.UniqueID}). Total POIs: {_registeredPOIs.Count}");
        }
        // else
        // {
        //     Debug.LogWarning($"PointOfInterest '{poi.name}' was not found in registered list, cannot unregister.");
        // }
    }


    /// <summary>
    /// Retrieves a PointOfInterest by its unique ID.
    /// </summary>
    /// <param name="uniqueID">The unique string ID of the POI.</param>
    /// <returns>The PointOfInterest with the matching ID, or null if not found.</returns>
    public PointOfInterest GetPOIByID(string uniqueID)
    {
        _poiByID.TryGetValue(uniqueID, out PointOfInterest poi);
        return poi;
    }


    /// <summary>
    /// Finds the nearest PointOfInterest to a given position.
    /// Can optionally filter by PoiType.
    /// </summary>
    /// <param name="currentPosition">The position to measure distance from.</param>
    /// <param name="type">The type of POI to search for. Use PoiType.Any to ignore type filtering.</param>
    /// <returns>The nearest PointOfInterest, or null if no POIs are found matching the criteria.</returns>
    public PointOfInterest GetNearestPOI(Vector3 currentPosition, PoiType type = PoiType.Any)
    {
        PointOfInterest nearestPOI = null;
        float minDistanceSqr = float.MaxValue; // Use squared distance for performance

        foreach (PointOfInterest poi in _registeredPOIs)
        {
            if (poi == null || (type != PoiType.Any && poi.Type != type))
            {
                continue; // Skip null or unmatched type POIs
            }

            float distanceSqr = (poi.Position - currentPosition).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                nearestPOI = poi;
            }
        }
        return nearestPOI;
    }

    /// <summary>
    /// Finds all PointsOfInterest of a specific type.
    /// </summary>
    /// <param name="type">The type of POI to search for. Use PoiType.Any to get all registered POIs.</param>
    /// <returns>A list of all PointsOfInterest matching the specified type.</returns>
    public List<PointOfInterest> GetAllPOIsByType(PoiType type)
    {
        List<PointOfInterest> result = new List<PointOfInterest>();
        if (type == PoiType.Any)
        {
            result.AddRange(_registeredPOIs); // Add all POIs
        }
        else
        {
            foreach (PointOfInterest poi in _registeredPOIs)
            {
                if (poi != null && poi.Type == type)
                {
                    result.Add(poi);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Finds all PointsOfInterest within a specified radius from a given position.
    /// Can optionally filter by PoiType.
    /// </summary>
    /// <param name="currentPosition">The center position for the radius check.</param>
    /// <param name="radius">The maximum distance from currentPosition.</param>
    /// <param name="type">The type of POI to search for. Use PoiType.Any to ignore type filtering.</param>
    /// <returns>A list of all PointsOfInterest within the radius matching the criteria.</returns>
    public List<PointOfInterest> GetAllPOIsWithinRadius(Vector3 currentPosition, float radius, PoiType type = PoiType.Any)
    {
        List<PointOfInterest> result = new List<PointOfInterest>();
        float radiusSqr = radius * radius; // Use squared distance for performance

        foreach (PointOfInterest poi in _registeredPOIs)
        {
            if (poi == null || (type != PoiType.Any && poi.Type != type))
            {
                continue; // Skip null or unmatched type POIs
            }

            if ((poi.Position - currentPosition).sqrMagnitude <= radiusSqr)
            {
                result.Add(poi);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets a read-only list of all currently registered Points of Interest.
    /// </summary>
    public IReadOnlyList<PointOfInterest> AllRegisteredPOIs => _registeredPOIs;
}
```

---

## **4. PlayerInteractionExample.cs (Client Example)**

This script demonstrates how a "player" might use the `PointOfInterestSystem` to discover and interact with nearby POIs.

```csharp
// PlayerInteractionExample.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// An example client script (e.g., attached to a player character) that
/// demonstrates how to use the PointOfInterestSystem to find and interact with POIs.
/// </summary>
public class PlayerInteractionExample : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The radius within which the player can interact with a Point of Interest.")]
    [SerializeField] private float interactionRadius = 3.0f;

    [Tooltip("The input key to trigger interaction with the nearest POI.")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Debugging")]
    [Tooltip("Show the interaction radius gizmo in the editor.")]
    [SerializeField] private bool showInteractionGizmo = true;
    [SerializeField] private Color gizmoColor = Color.green;

    private PointOfInterest _nearestInteractablePOI;

    void Update()
    {
        // 1. Find the nearest POI within the interaction radius.
        // We query for any type of POI, then check if it's within range.
        PointOfInterest potentialPOI = PointOfInterestSystem.Instance.GetNearestPOI(transform.position, PoiType.Any);

        _nearestInteractablePOI = null; // Reset
        if (potentialPOI != null)
        {
            float distanceToPOI = Vector3.Distance(transform.position, potentialPOI.Position);
            if (distanceToPOI <= interactionRadius)
            {
                _nearestInteractablePOI = potentialPOI;
            }
        }

        // 2. Provide feedback to the player (e.g., highlight, UI prompt).
        // In a real game, you'd likely update a UI element here.
        if (_nearestInteractablePOI != null)
        {
            Debug.DrawLine(transform.position, _nearestInteractablePOI.Position, Color.yellow); // Visual cue
            // Example of showing a UI prompt:
            // UIManager.Instance.ShowInteractionPrompt($"Press '{interactKey}' to interact with {_nearestInteractablePOI.name}");
        }
        // else
        // {
        //     // UIManager.Instance.HideInteractionPrompt();
        // }


        // 3. Handle player input for interaction.
        if (_nearestInteractablePOI != null && Input.GetKeyDown(interactKey))
        {
            _nearestInteractablePOI.Interact(); // Trigger the POI's interaction logic
        }

        // Example: Find all Quest Givers within a larger radius (e.g., for a minimap)
        // This could run less frequently than Update()
        if (Time.frameCount % 60 == 0) // Every 60 frames (approx. 1 second at 60 FPS)
        {
            List<PointOfInterest> nearbyQuestGivers = PointOfInterestSystem.Instance.GetAllPOIsWithinRadius(transform.position, 20f, PoiType.QuestGiver);
            // if (nearbyQuestGivers.Count > 0)
            // {
            //     Debug.Log($"Found {nearbyQuestGivers.Count} nearby quest givers.");
            // }
        }
    }

    /// <summary>
    /// Draws gizmos in the editor for visualization.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showInteractionGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);

            if (_nearestInteractablePOI != null)
            {
                // Highlight the currently interactable POI
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _nearestInteractablePOI.Position);
                Gizmos.DrawSphere(_nearestInteractablePOI.Position, 0.6f);
            }
        }
    }
}
```

---

## **How to Use This in Unity:**

1.  **Create the Scripts:**
    *   Create a new C# script named `PoiType.cs` and paste the `PoiType` enum code.
    *   Create a new C# script named `PointOfInterest.cs` and paste its code.
    *   Create a new C# script named `PointOfInterestSystem.cs` and paste its code.
    *   Create a new C# script named `PlayerInteractionExample.cs` and paste its code.

2.  **Setup the PointOfInterestSystem Manager:**
    *   In your Unity scene, create an empty GameObject (e.g., name it `_Managers` or `PointOfInterestManager`).
    *   Attach the `PointOfInterestSystem.cs` script to this GameObject. The singleton will handle itself.

3.  **Setup the Player:**
    *   Create a simple Player GameObject (e.g., a Sphere or Cube).
    *   Attach the `PlayerInteractionExample.cs` script to your Player. Adjust the `Interaction Radius` as needed in the Inspector.
    *   Add a `CharacterController` or a `Rigidbody` and appropriate movement script if you want to move the player around. For simple testing, you can just manually move the player in the Scene view.

4.  **Create Points of Interest:**
    *   Create several empty GameObjects in your scene (e.g., `QuestGiver`, `Shop`, `Collectible_01`, `HealingStation`).
    *   Position them around your Player.
    *   Attach the `PointOfInterest.cs` script to each of these GameObjects.
    *   In the Inspector for each `PointOfInterest` component:
        *   Set its `PoiType` (e.g., `QuestGiver` for the `QuestGiver` object, `Shop` for `Shop`, etc.).
        *   (Optional) Give it a `Unique ID` if you need to reference it specifically by a string identifier.
        *   Add a callback to the `On Interact` UnityEvent: Click the `+` button, drag the GameObject itself into the object slot, and select `Debug.Log(string)` from `No Function` dropdown. In the string field, enter a message like `"Quest Giver interacted!"` or similar. This will log a message to the console when you interact with it.

5.  **Run the Scene:**
    *   Move your Player character close to one of the POIs. You should see a yellow line drawn from the player to the nearest interactable POI.
    *   Press the 'E' key (or your assigned `Interact Key`).
    *   You should see the debug message from the POI's `OnInteract` event in your console, confirming the interaction.

This setup provides a complete, practical, and well-commented example of the PointOfInterestSystem design pattern in Unity, ready to be expanded upon for real game development.