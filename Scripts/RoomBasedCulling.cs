// Unity Design Pattern Example: RoomBasedCulling
// This script demonstrates the RoomBasedCulling pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Room-Based Culling** design pattern. This pattern optimizes performance by activating and deactivating game objects based on the player's current location within defined "rooms" in a scene. Objects outside the current room (and possibly adjacent rooms) are disabled, reducing rendering and update overhead.

The example consists of two main scripts:
1.  **`Room.cs`**: Defines a single room, its boundaries (using a Trigger Collider), and the game objects that belong to it.
2.  **`RoomManager.cs`**: A singleton that orchestrates the culling. It tracks the player's current room and tells rooms to activate or deactivate their contents.

---

### Room.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor; // Required for Handles.Label in OnDrawGizmos
#endif

namespace RoomBasedCulling
{
    /// <summary>
    /// Represents a single 'room' in the Room-Based Culling system.
    /// This script should be attached to a GameObject that has a Collider
    /// marked as 'Is Trigger'. This collider defines the boundaries of the room.
    /// </summary>
    [RequireComponent(typeof(Collider))] // Ensures a Collider is present on the GameObject
    public class Room : MonoBehaviour
    {
        [Header("Room Configuration")]
        [Tooltip("A unique name for this room, useful for debugging.")]
        public string roomName = "New Room";

        [Tooltip("List of all GameObjects that belong to this room. " +
                 "These objects will be activated when the player enters this room " +
                 "and deactivated when the player leaves.")]
        [SerializeField]
        private List<GameObject> roomContents = new List<GameObject>();

        [Tooltip("If true, this room's contents will always remain active, " +
                 "regardless of the player's current room. Useful for global UI, " +
                 "persistent elements, or 'outside' areas that should never be culled.")]
        [SerializeField]
        private bool alwaysActive = false;

        private Collider roomTrigger; // Reference to the room's trigger collider.

        /// <summary>
        /// Gets whether this room is configured to always be active.
        /// </summary>
        public bool IsAlwaysActive => alwaysActive;

        private void Awake()
        {
            // Get the collider component and perform essential checks.
            roomTrigger = GetComponent<Collider>();
            if (roomTrigger == null)
            {
                Debug.LogError($"Room '{roomName}' on GameObject '{gameObject.name}' requires a Collider component to function.", this);
                enabled = false; // Disable script if no collider to prevent further errors.
                return;
            }

            // Ensure the collider is set as a trigger for detection.
            if (!roomTrigger.isTrigger)
            {
                Debug.LogWarning($"Room '{roomName}' on GameObject '{gameObject.name}'s collider is not marked as 'Is Trigger'. " +
                                 "Room-based culling will not function correctly without it.", this);
            }

            // Initially deactivate contents if it's not an 'alwaysActive' room.
            // The RoomManager will handle the specific initial activation for the starting room.
            if (!alwaysActive)
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Activates all game objects within this room by setting them active.
        /// Does nothing if the room is marked as 'alwaysActive'.
        /// </summary>
        public void Activate()
        {
            if (alwaysActive) return; // Always active rooms are never explicitly deactivated or activated by manager.

            foreach (GameObject obj in roomContents)
            {
                // Only activate if the object exists and is not already active.
                if (obj != null && !obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }
            // Uncomment for verbose debugging:
            // Debug.Log($"Activated contents of room: {roomName}");
        }

        /// <summary>
        /// Deactivates all game objects within this room by setting them inactive.
        /// Does nothing if the room is marked as 'alwaysActive'.
        /// </summary>
        public void Deactivate()
        {
            if (alwaysActive) return; // Always active rooms are never explicitly deactivated or activated by manager.

            foreach (GameObject obj in roomContents)
            {
                // Only deactivate if the object exists and is currently active.
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                }
            }
            // Uncomment for verbose debugging:
            // Debug.Log($"Deactivated contents of room: {roomName}");
        }

        /// <summary>
        /// Unity's callback for when another collider enters this room's trigger.
        /// This is where the player's entry into a room is detected.
        /// </summary>
        /// <param name="other">The collider that entered this trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            // Check if the entering collider belongs to the player.
            // RoomManager uses a configurable player tag for this check.
            if (RoomManager.Instance != null && other.CompareTag(RoomManager.Instance.PlayerTag))
            {
                // Inform the RoomManager that the player has entered this room.
                RoomManager.Instance.PlayerEnteredRoom(this);
            }
        }

        /*
        /// OnTriggerExit is generally not strictly needed for the core Room-Based Culling logic
        /// as entering a new room implicitly deactivates the old one via the RoomManager.
        /// However, it can be useful for debugging or more complex scenarios where a player might
        /// leave all defined rooms, or for implementing an 'outside' default state.
        /// For this basic example, we rely solely on OnTriggerEnter.
        private void OnTriggerExit(Collider other)
        {
            if (RoomManager.Instance != null && other.CompareTag(RoomManager.Instance.PlayerTag))
            {
                // Example: If player exited the currently active room without entering a new one,
                // you might want to activate a default 'outside' room or clear the current active room.
                // RoomManager.Instance.PlayerExitedRoom(this); // Optional callback
            }
        }
        */

        /// <summary>
        /// Draws a gizmo in the editor to visualize the room's trigger bounds.
        /// This helps in setting up and debugging room boundaries.
        /// </summary>
        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                // Set matrix to apply transform (position, rotation, scale) to gizmo.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan, semi-transparent for fill.

                // Draw specific collider types.
                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.color = new Color(0, 1, 1, 0.5f); // Slightly more opaque for wireframe.
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    Gizmos.color = new Color(0, 1, 1, 0.5f);
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                // Add support for other collider types (e.g., CapsuleCollider) if needed.

                Gizmos.matrix = Matrix4x4.identity; // Reset matrix to default.
            }
            else
            {
                // If no trigger collider or not a trigger, draw a warning cube.
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, Vector3.one);
                #if UNITY_EDITOR
                // Draw a visual error icon in the editor.
                Handles.Label(transform.position + Vector3.up * 0.5f, "Room: No Trigger Collider!");
                #endif
            }

            // Draw room name as text in the editor for easy identification.
            if (!string.IsNullOrEmpty(roomName))
            {
                #if UNITY_EDITOR // Handles are Unity Editor specific.
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                Handles.Label(transform.position + Vector3.up * 1f, roomName, style);
                #endif
            }
        }
    }
}
```

---

### RoomManager.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() extension method

namespace RoomBasedCulling
{
    /// <summary>
    /// The central manager for the Room-Based Culling system.
    /// This script orchestrates the activation and deactivation of rooms
    /// based on the player's current location. It implements the Singleton pattern
    /// to ensure easy and global access from other scripts (like Room.cs).
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the RoomManager.
        /// Provides a global access point to the manager.
        /// </summary>
        public static RoomManager Instance { get; private set; }

        [Header("Player Settings")]
        [Tooltip("The tag assigned to the player GameObject. " +
                 "Ensure your player GameObject has this tag set in the Inspector.")]
        [SerializeField]
        private string playerTag = "Player";

        [Tooltip("The room the player starts in when the scene loads. " +
                 "Its contents will be activated immediately. If left null, " +
                 "no room will be active initially, and the player will need " +
                 "to enter a room trigger to activate any content.")]
        [SerializeField]
        private Room initialRoom;

        /// <summary>
        /// Gets the player tag used by the RoomManager for identification.
        /// </summary>
        public string PlayerTag => playerTag;

        private Room currentActiveRoom; // The room whose contents are currently active.
        private List<Room> allRooms;    // A cached list of all Room components found in the scene.

        private void Awake()
        {
            // Implement the Singleton pattern.
            // Ensures only one instance of RoomManager exists.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("RoomManager: Duplicate instance found, destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find all Room components in the scene.
            // The 'true' argument ensures inactive GameObjects are also searched.
            allRooms = FindObjectsOfType<Room>(true).ToList();

            // Deactivate the contents of all rooms initially, except for those marked as 'alwaysActive'.
            DeactivateAllRooms();

            // Activate the initial room if one is specified.
            if (initialRoom != null)
            {
                PlayerEnteredRoom(initialRoom);
            }
            else
            {
                Debug.LogWarning("RoomManager: No initial room set. Make sure the player enters a room trigger to activate content.", this);
            }
        }

        /// <summary>
        /// Deactivates the contents of all rooms in the scene that are not marked as 'alwaysActive'.
        /// This is typically called once at scene startup.
        /// </summary>
        private void DeactivateAllRooms()
        {
            foreach (Room room in allRooms)
            {
                if (room != null && !room.IsAlwaysActive)
                {
                    room.Deactivate();
                }
            }
            Debug.Log("RoomManager: All non-always-active rooms initially deactivated.");
        }

        /// <summary>
        /// Called by a Room script when the player enters its trigger.
        /// This method orchestrates the room transition: deactivating the previous room
        /// and activating the new one.
        /// </summary>
        /// <param name="newRoom">The room the player has just entered.</param>
        public void PlayerEnteredRoom(Room newRoom)
        {
            if (newRoom == null)
            {
                Debug.LogError("RoomManager: PlayerEnteredRoom called with a null room.", this);
                return;
            }

            // If the player is already in this room, or it's an always-active room
            // which doesn't need explicit state changes, do nothing.
            if (newRoom == currentActiveRoom || newRoom.IsAlwaysActive)
            {
                //Debug.Log($"RoomManager: Player already in {newRoom.roomName} or it's always active.");
                return;
            }

            Debug.Log($"RoomManager: Player entered new room: {newRoom.roomName}.");

            // Deactivate the old room's contents, but only if it was a valid room
            // and not an 'alwaysActive' room (which should never be deactivated).
            if (currentActiveRoom != null && !currentActiveRoom.IsAlwaysActive)
            {
                currentActiveRoom.Deactivate();
                Debug.Log($"RoomManager: Deactivated old room: {currentActiveRoom.roomName}.");
            }

            // Activate the new room's contents.
            newRoom.Activate();
            currentActiveRoom = newRoom; // Set the new room as the currently active one.

            // OPTIONAL ADVANCED: To prevent objects from "popping in", you could activate
            // also adjacent rooms here, based on a list defined in the Room script.
            // This would require Room objects to have a list of 'adjacentRooms'.
        }

        /*
        // Optional: PlayerExitedRoom could be used for more complex logic,
        // e.g., if a player leaves all rooms, or if you want to keep
        // adjacent rooms active for a short duration. For this basic
        // example, we rely on PlayerEnteredRoom to handle all transitions.
        public void PlayerExitedRoom(Room room)
        {
            // Example: If the player truly leaves the 'currentActiveRoom' and doesn't enter a new one,
            // you might want to deactivate it or activate a default 'outside' state.
            // if (room == currentActiveRoom && !room.IsAlwaysActive)
            // {
            //     Debug.Log($"RoomManager: Player exited {room.roomName}. No new room entered yet.");
            //     room.Deactivate();
            //     currentActiveRoom = null; // Player is now in no defined room.
            // }
        }
        */

        /// <summary>
        /// Returns the room that is currently active and whose contents are visible.
        /// </summary>
        public Room GetCurrentActiveRoom()
        {
            return currentActiveRoom;
        }
    }
}
```

---

### EXAMPLE USAGE AND SETUP IN UNITY:

This guide will walk you through setting up the Room-Based Culling system in your Unity project.

**1. Create Scripts:**
*   Create a new C# script named `Room.cs` and paste the content of the `Room` class into it.
*   Create another C# script named `RoomManager.cs` and paste the content of the `RoomManager` class into it.
    *(Make sure both scripts are inside your Unity project's `Assets` folder.)*

**2. Scene Setup: `RoomManager`**
*   Create an empty GameObject in your scene and name it **"RoomManager"**.
*   Attach the `RoomManager.cs` script to this GameObject. This will be your central culling controller.

**3. Scene Setup: Player**
*   Create your 'Player' GameObject (e.g., a Capsule).
    *   **Collider:** Ensure it has a Collider component (e.g., Capsule Collider, Box Collider).
        *   **IMPORTANT:** The player's collider **must NOT** be set to 'Is Trigger'.
    *   **Rigidbody:** For trigger events to fire correctly, the player GameObject needs a `Rigidbody` component.
        *   If your player character controller handles movement without physics, check `Is Kinematic` on the Rigidbody.
    *   **Tag:** Set its 'Tag' to **"Player"**. (Go to Inspector -> Tag dropdown -> Add Tag... -> type "Player" -> select "Player" tag for your Player GameObject). This tag is used by `Room.cs` to identify the player.
    *   **Movement:** Add a simple movement script to your player (e.g., for W/A/S/D movement) so you can move it around the scene.

**4. Scene Setup: Rooms**
*   For each distinct area you want to define as a room:
    *   Create an empty GameObject (e.g., **"Room1_MainHall"**, **"Room2_Corridor"**, **"Room3_Bedroom"**).
    *   Attach the `Room.cs` script to each of these Room GameObjects.
    *   Add a **Collider** component (e.g., Box Collider, Capsule Collider) to each Room GameObject.
        *   **IMPORTANT:** Mark the Collider as **'Is Trigger'** (checkbox in Inspector).
        *   Resize and position the collider to accurately define the boundaries of your room in the scene view.
    *   In the `Room` script's Inspector (on your Room GameObject):
        *   Give it a descriptive **'Room Name'** (e.g., "Main Hall", "Corridor A").
        *   **Drag and drop all GameObjects** that should be visible/active *only* when the player is in this specific room into the **'Room Contents'** list. These can be props, lights, enemies, specific walls/floors, particle systems, etc.
        *   If a room's content should *always* be active (e.g., global skybox, persistent UI elements, a general "outside" area), check the **'Always Active'** checkbox. These rooms will never have their contents deactivated by the `RoomManager`.

**5. Configure `RoomManager`:**
*   Select your **"RoomManager"** GameObject in the Hierarchy.
*   In its Inspector, drag one of your `Room` GameObjects (e.g., "Room1_MainHall") into the **'Initial Room'** slot. This room's contents will be active when the game starts.

**6. Test Your Setup:**
*   Run the scene.
*   **Observe in the Hierarchy:** Only the contents of your 'Initial Room' (and any rooms marked 'Always Active') should be active. All other room contents should be inactive.
*   Move your player character into another room's trigger collider.
*   You should see the previous room's contents deactivate and the new room's contents activate in the Hierarchy.
*   Check the Unity Console for debug logs showing room transitions.

---

### Best Practices & Considerations:

*   **Content Granularity:** Keep your 'Room Contents' lists focused on objects that genuinely benefit from culling. Static geometry that is part of the scene (e.g., large static room walls, floors, ceilings) can often just remain active if they are properly culled by Unity's built-in frustum culling. Room-based culling is most effective for dynamic objects, lights, heavy prefabs, enemies, particle systems, and anything that incurs significant overhead when active but out of view.
*   **Player Physics:** Ensure your player has a `Rigidbody` (set to `Is Kinematic` if you don't want physics, but need trigger detection) or a `CharacterController` for `OnTriggerEnter` events to register correctly.
*   **Room Collider Design:** Make sure your room trigger colliders are large enough to fully encompass the area and provide smooth transitions. Overlapping slightly with adjacent rooms can help prevent objects from "popping" in or out prematurely, though the current implementation does a hard switch.
*   **Visual Debugging:** The `OnDrawGizmos` functionality in the `Room` script provides clear visual representation of room boundaries and names directly in the editor, which is invaluable for setup and debugging.
*   **Advanced Scenarios:**
    *   **Adjacent Room Activation:** For smoother transitions, you could extend `RoomManager.PlayerEnteredRoom` to also activate rooms adjacent to the `newRoom`. This would require each `Room` script to have a `List<Room> adjacentRooms` property.
    *   **Fade Transitions:** Instead of `SetActive(true/false)`, you could implement a fading mechanism (e.g., gradually changing material transparency or light intensity) for objects when transitioning between rooms.
    *   **Multiple Players:** For multiplayer games, each player would typically need its own "current room" state, and the culling logic would become more complex (e.g., showing rooms relevant to *any* nearby player, or culling differently based on individual player's views). This example is designed for a single player.