// Unity Design Pattern Example: OcclusionPortalSystem
// This script demonstrates the OcclusionPortalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Occlusion Portal System** design pattern in Unity. This pattern is used to improve rendering performance by dividing a scene into discrete zones (rooms/sectors) and only rendering those zones that are visible through "portals" from the camera's current perspective.

Instead of relying on Unity's built-in Occlusion Culling (which is highly optimized and works at a lower level), this script-based implementation illustrates the *architectural principles* of the pattern by activating/deactivating `Renderer` components. This makes it a great educational tool for understanding the logic behind such systems.

**How it works:**

1.  **`OcclusionRoom`:** Represents a distinct area in your scene. It knows all the `Renderer` components within its boundaries and which `OcclusionPortal`s lead out of it. It also has a `Collider` to define its physical space.
2.  **`OcclusionPortal`:** Represents a connection point between two `OcclusionRoom`s (e.g., a doorway, an archway). It has a `Collider` (set as a trigger) that defines its "opening."
3.  **`OcclusionPortalSystemController`:** The central manager. It finds the `OcclusionRoom` the camera is currently in. From that room, it recursively traverses visible `OcclusionPortal`s to determine which other rooms should be activated (rendered). All other rooms are deactivated (their renderers disabled).

---

### **Setup Guide in Unity:**

1.  **Create your Scene:** Design an indoor environment with several distinct "rooms" (e.g., using cubes for walls, floors, ceilings).
2.  **Create `OcclusionRoom` GameObjects:**
    *   For each room, create an empty GameObject (e.g., `Room_A`, `Room_B`).
    *   Add the `OcclusionRoom` script to it.
    *   Add a `BoxCollider` to this GameObject. Adjust its size to encompass the *entire* room. **This collider is crucial for the system to detect if the camera is inside.**
    *   Place all your room's visual geometry (walls, props, lights, etc.) as children of this `Room_A` GameObject. The `OcclusionRoom` script will automatically find their `Renderer` components.
3.  **Create `OcclusionPortal` GameObjects:**
    *   Between any two rooms that connect, create an empty GameObject (e.g., `Portal_AB`).
    *   Position this GameObject in the doorway/opening between `Room_A` and `Room_B`.
    *   Add the `OcclusionPortal` script to it.
    *   Add a `BoxCollider` to this GameObject. Mark it as an **Is Trigger** and adjust its size to fit the *opening* of the portal.
    *   In the Inspector for `Portal_AB`, drag `Room_A` into the `Room A` slot and `Room_B` into the `Room B` slot.
    *   Adjust the `Activation Distance` (e.g., 5-10 units) for how close the camera needs to be to potentially "see through" this portal.
4.  **Create `OcclusionPortalSystemController`:**
    *   Create an empty GameObject in your scene (e.g., `OcclusionManager`).
    *   Add the `OcclusionPortalSystemController` script to it.
    *   Drag your main camera (`Main Camera`) into the `Main Camera` slot.
    *   Adjust the `Update Interval` (e.g., 0.25 seconds) to control how often the system re-calculates visibility. A higher interval saves performance but might be less reactive.
5.  **Test:** Run your scene. Move your camera around. You should observe rooms appearing and disappearing as you move between them and look through portals.

---

### **1. `OcclusionPortalSystemController.cs`**

This script manages the entire occlusion system, determining which rooms are visible based on the camera's position and view.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For HashSet

/// <summary>
/// The central manager for the Occlusion Portal System.
/// This script orchestrates the visibility updates for all rooms in the scene.
/// </summary>
public class OcclusionPortalSystemController : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("The main camera used for visibility determination.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("How often (in seconds) the system updates room visibility.")]
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Debug Information")]
    [Tooltip("All Occlusion Rooms found in the scene.")]
    [SerializeField] private OcclusionRoom[] allRooms;

    [Tooltip("The room the camera is currently determined to be inside.")]
    [SerializeField] private OcclusionRoom currentCameraRoom;

    // Coroutine reference for stopping and starting the update loop
    private Coroutine updateVisibilityCoroutine;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Finds all Occlusion Rooms and assigns the main camera if not set.
    /// </summary>
    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("OcclusionPortalSystemController: No main camera found or assigned. Please assign one in the Inspector.", this);
                enabled = false; // Disable the component if no camera
                return;
            }
        }

        FindAllRoomsInScene();
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Starts the visibility update coroutine.
    /// </summary>
    private void OnEnable()
    {
        if (mainCamera == null) return; // Don't start if no camera
        if (updateVisibilityCoroutine != null)
        {
            StopCoroutine(updateVisibilityCoroutine);
        }
        updateVisibilityCoroutine = StartCoroutine(VisibilityUpdateLoop());
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Stops the visibility update coroutine.
    /// </summary>
    private void OnDisable()
    {
        if (updateVisibilityCoroutine != null)
        {
            StopCoroutine(updateVisibilityCoroutine);
            updateVisibilityCoroutine = null;
        }

        // Ensure all rooms are visible when the system is disabled, or hidden.
        // For this demo, let's make them all visible when disabled, so you can see them.
        foreach (var room in allRooms)
        {
            if (room != null)
            {
                room.SetVisibility(true);
            }
        }
    }

    /// <summary>
    /// Periodically updates the visibility of rooms.
    /// </summary>
    private IEnumerator VisibilityUpdateLoop()
    {
        while (true)
        {
            UpdateVisibility();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    /// <summary>
    /// The main logic for updating room visibility.
    /// 1. Determines which room the camera is currently inside.
    /// 2. Hides all rooms initially.
    /// 3. Recursively activates visible rooms starting from the current camera room.
    /// </summary>
    private void UpdateVisibility()
    {
        if (mainCamera == null) return;

        // 1. Determine the current room the camera is in
        DetermineCurrentCameraRoom();

        // 2. Hide all rooms first
        foreach (var room in allRooms)
        {
            if (room != null)
            {
                room.SetVisibility(false);
            }
        }

        // 3. If a current room is found, start the recursive visibility check
        if (currentCameraRoom != null)
        {
            HashSet<OcclusionRoom> visibleRooms = new HashSet<OcclusionRoom>();
            currentCameraRoom.DetermineVisibleRooms(mainCamera, visibleRooms);

            // Activate all determined visible rooms
            foreach (var room in visibleRooms)
            {
                if (room != null)
                {
                    room.SetVisibility(true);
                }
            }
        }
    }

    /// <summary>
    /// Iterates through all rooms to find the one the camera is currently inside.
    /// </summary>
    private void DetermineCurrentCameraRoom()
    {
        currentCameraRoom = null;
        foreach (var room in allRooms)
        {
            if (room != null && room.IsCameraInside(mainCamera.transform.position))
            {
                currentCameraRoom = room;
                return; // Found the room, no need to check further
            }
        }
    }

    /// <summary>
    /// Finds all `OcclusionRoom` components in the scene and populates the `allRooms` array.
    /// </summary>
    [ContextMenu("Find All Rooms In Scene")]
    private void FindAllRoomsInScene()
    {
        allRooms = FindObjectsOfType<OcclusionRoom>();
        Debug.Log($"OcclusionPortalSystemController: Found {allRooms.Length} Occlusion Rooms in the scene.", this);
    }

    // Optional: Editor helper for refreshing rooms during design time
    private void OnValidate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
#if UNITY_EDITOR
        // Ensure rooms are updated in editor
        if (!Application.isPlaying && allRooms != null && allRooms.Length == 0)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && allRooms != null && allRooms.Length == 0) // Double check after delay
                {
                    FindAllRoomsInScene();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            };
        }
#endif
    }
}
```

---

### **2. `OcclusionRoom.cs`**

This script defines a single room or sector in your occlusion system. It manages its own renderers and connections to other rooms via portals.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List and HashSet

/// <summary>
/// Represents a single room or sector in the occlusion portal system.
/// Manages the visibility of its contained renderers and identifies adjacent portals.
/// </summary>
[RequireComponent(typeof(Collider))] // A collider is needed to define the room bounds.
public class OcclusionRoom : MonoBehaviour
{
    [Header("Room Settings")]
    [Tooltip("The collider defining the boundaries of this room. Used to check if the camera is inside.")]
    [SerializeField] private Collider roomCollider;

    [Header("Debug Information")]
    [Tooltip("All renderers that are children of this room's GameObject. Their visibility will be toggled.")]
    [SerializeField] private Renderer[] childRenderers;

    [Tooltip("All Occlusion Portals connected to this room.")]
    [SerializeField] private OcclusionPortal[] adjacentPortals;

    [Tooltip("Current visibility state of the room.")]
    [SerializeField] private bool isCurrentlyVisible = false;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes components and finds child renderers/adjacent portals.
    /// </summary>
    private void Awake()
    {
        EnsureRoomCollider();
        FindChildRenderers();
        FindAdjacentPortals();
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed.
    /// Helps to set up the room collider and find components easily.
    /// </summary>
    private void OnValidate()
    {
        EnsureRoomCollider();
        if (Application.isPlaying) return; // Only run in editor outside play mode

#if UNITY_EDITOR
        // Using delayCall to avoid issues with modifying scene during OnValidate
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && Application.isPlaying == false) // Ensure object still exists and not in play mode
            {
                // Only re-find if array is null or empty to prevent constant updates
                if (childRenderers == null || childRenderers.Length == 0)
                {
                    FindChildRenderers();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                if (adjacentPortals == null || adjacentPortals.Length == 0)
                {
                    FindAdjacentPortals();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        };
#endif
    }

    /// <summary>
    /// Ensures the roomCollider field is populated with the GameObject's collider.
    /// Also ensures the collider is not marked as a trigger (it defines solid bounds).
    /// </summary>
    private void EnsureRoomCollider()
    {
        if (roomCollider == null)
        {
            roomCollider = GetComponent<Collider>();
            if (roomCollider == null)
            {
                Debug.LogError($"OcclusionRoom: No Collider found on {gameObject.name}. A collider is required to define room boundaries. Please add one.", this);
                enabled = false;
                return;
            }
            // Ensure the room collider is not a trigger, as it defines physical space
            roomCollider.isTrigger = false;
        }
    }

    /// <summary>
    /// Finds all Renderer components that are children of this room's GameObject.
    /// </summary>
    [ContextMenu("Find Child Renderers")]
    private void FindChildRenderers()
    {
        // Get all renderers, but exclude this GameObject's own renderer if it has one
        // and exclude renderers that are part of the portal objects themselves.
        List<Renderer> foundRenderers = new List<Renderer>();
        Renderer[] allRenderersInChildren = GetComponentsInChildren<Renderer>(true); // include inactive

        foreach (Renderer rend in allRenderersInChildren)
        {
            // Exclude the collider used for room boundaries itself if it has a renderer
            if (rend.gameObject == gameObject) continue;

            // Exclude renderers that are part of an OcclusionPortal
            if (rend.GetComponent<OcclusionPortal>() != null) continue;

            foundRenderers.Add(rend);
        }
        childRenderers = foundRenderers.ToArray();
        Debug.Log($"OcclusionRoom '{gameObject.name}': Found {childRenderers.Length} child renderers.", this);
    }

    /// <summary>
    /// Finds all `OcclusionPortal` components in the scene that reference this room.
    /// </summary>
    [ContextMenu("Find Adjacent Portals")]
    private void FindAdjacentPortals()
    {
        List<OcclusionPortal> foundPortals = new List<OcclusionPortal>();
        OcclusionPortal[] allPortals = FindObjectsOfType<OcclusionPortal>(); // Find all portals in scene

        foreach (OcclusionPortal portal in allPortals)
        {
            if (portal.RoomA == this || portal.RoomB == this)
            {
                foundPortals.Add(portal);
            }
        }
        adjacentPortals = foundPortals.ToArray();
        Debug.Log($"OcclusionRoom '{gameObject.name}': Found {adjacentPortals.Length} adjacent portals.", this);
    }


    /// <summary>
    /// Sets the visibility of all child renderers in this room.
    /// </summary>
    /// <param name="visible">True to enable renderers, false to disable.</param>
    public void SetVisibility(bool visible)
    {
        if (isCurrentlyVisible == visible) return; // No change needed

        foreach (Renderer rend in childRenderers)
        {
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
        isCurrentlyVisible = visible;
        // Debug.Log($"Room '{gameObject.name}' visibility set to: {visible}");
    }

    /// <summary>
    /// Checks if the given camera position is inside this room's collider.
    /// </summary>
    /// <param name="cameraPosition">The world position of the camera.</param>
    /// <returns>True if the camera is inside the room, false otherwise.</returns>
    public bool IsCameraInside(Vector3 cameraPosition)
    {
        if (roomCollider == null) return false;
        return roomCollider.bounds.Contains(cameraPosition);
    }

    /// <summary>
    /// Recursively determines all rooms visible from the current room through open portals.
    /// Uses a HashSet to prevent infinite recursion and redundant processing.
    /// </summary>
    /// <param name="camera">The camera used for visibility checks.</param>
    /// <param name="visibleRooms">A HashSet to store all rooms determined to be visible.</param>
    public void DetermineVisibleRooms(Camera camera, HashSet<OcclusionRoom> visibleRooms)
    {
        // If this room has already been processed or added to visibleRooms, stop recursion.
        if (visibleRooms.Contains(this))
        {
            return;
        }

        // Add this room to the set of visible rooms.
        visibleRooms.Add(this);

        // Iterate through all adjacent portals
        foreach (OcclusionPortal portal in adjacentPortals)
        {
            if (portal == null) continue;

            // Get the room on the other side of this portal
            OcclusionRoom connectedRoom = portal.GetConnectedRoom(this);

            if (connectedRoom != null)
            {
                // Check if the portal itself is visible from the camera's perspective
                if (portal.IsPortalVisibleFromCamera(camera))
                {
                    // If the portal is visible, recursively check the connected room
                    connectedRoom.DetermineVisibleRooms(camera, visibleRooms);
                }
            }
        }
    }

    /// <summary>
    /// Gets the current visibility status of the room.
    /// </summary>
    public bool IsCurrentlyVisible => isCurrentlyVisible;
}
```

---

### **3. `OcclusionPortal.cs`**

This script defines a portal, which acts as a connection point between two `OcclusionRoom`s. It determines if the camera can "see through" it.

```csharp
using UnityEngine;

/// <summary>
/// Represents a portal connecting two OcclusionRooms.
/// It defines an opening through which visibility can pass.
/// </summary>
[RequireComponent(typeof(Collider))] // A collider is needed to define the portal's opening.
public class OcclusionPortal : MonoBehaviour
{
    [Header("Portal Connections")]
    [Tooltip("The first room connected by this portal.")]
    [SerializeField] private OcclusionRoom roomA;
    [Tooltip("The second room connected by this portal.")]
    [SerializeField] private OcclusionRoom roomB;

    [Header("Portal Settings")]
    [Tooltip("The collider representing the portal's opening. Must be a trigger.")]
    [SerializeField] private Collider portalTrigger;

    [Tooltip("The maximum distance from the camera to the portal for it to be considered potentially visible.")]
    [SerializeField] private float activationDistance = 15f; // Example distance

    private Plane[] cameraFrustumPlanes; // Cached frustum planes for performance

    /// <summary>
    /// Gets the first connected room.
    /// </summary>
    public OcclusionRoom RoomA => roomA;

    /// <summary>
    /// Gets the second connected room.
    /// </summary>
    public OcclusionRoom RoomB => roomB;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures the portal has a trigger collider.
    /// </summary>
    private void Awake()
    {
        EnsurePortalTrigger();
    }

    /// <summary>
    /// Called in the editor when the script is loaded or a value is changed.
    /// Helps to set up the portal collider easily.
    /// </summary>
    private void OnValidate()
    {
        EnsurePortalTrigger();
    }

    /// <summary>
    /// Ensures the portalTrigger field is populated and set as a trigger.
    /// </summary>
    private void EnsurePortalTrigger()
    {
        if (portalTrigger == null)
        {
            portalTrigger = GetComponent<Collider>();
            if (portalTrigger == null)
            {
                Debug.LogError($"OcclusionPortal: No Collider found on {gameObject.name}. A collider is required to define the portal opening. Please add one.", this);
                enabled = false;
                return;
            }
        }
        // Ensure the portal collider is always a trigger
        portalTrigger.isTrigger = true;
    }

    /// <summary>
    /// Returns the room connected to this portal that is NOT the specified 'fromRoom'.
    /// </summary>
    /// <param name="fromRoom">The room from which we are checking.</param>
    /// <returns>The connected room, or null if 'fromRoom' is not one of the portal's connected rooms.</returns>
    public OcclusionRoom GetConnectedRoom(OcclusionRoom fromRoom)
    {
        if (fromRoom == roomA)
        {
            return roomB;
        }
        else if (fromRoom == roomB)
        {
            return roomA;
        }
        return null; // The provided room is not connected to this portal
    }

    /// <summary>
    /// Determines if this portal is potentially visible from the camera's perspective.
    /// This check involves:
    /// 1. Is the camera within a certain distance of the portal?
    /// 2. Is the portal's collider within the camera's viewing frustum?
    /// </summary>
    /// <param name="camera">The camera to check visibility from.</param>
    /// <returns>True if the portal is considered visible, false otherwise.</returns>
    public bool IsPortalVisibleFromCamera(Camera camera)
    {
        if (portalTrigger == null || camera == null) return false;

        // 1. Distance check: Is the camera close enough to potentially see through this portal?
        float distToPortal = Vector3.Distance(camera.transform.position, portalTrigger.bounds.center);
        if (distToPortal > activationDistance)
        {
            return false;
        }

        // 2. Frustum check: Is the portal's bounds within the camera's view frustum?
        // This is a common and efficient way to perform basic visibility culling.
        cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, portalTrigger.bounds);
    }
}
```