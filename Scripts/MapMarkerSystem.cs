// Unity Design Pattern Example: MapMarkerSystem
// This script demonstrates the MapMarkerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The MapMarkerSystem design pattern provides a robust and scalable way to manage and display points of interest (POIs) in a game world. It decouples the data of a marker from its visual representation and its management logic, making the system flexible and easy to extend.

Here's how the pattern is typically structured in Unity:

1.  **MapMarkerType (Enum):** Defines categories for markers (e.g., Quest Giver, Enemy Camp, Resource Node).
2.  **MapMarkerData (Serializable Class):** Holds the essential information about a marker (position, type, name, icon, unique ID). This is pure data.
3.  **MapMarkerManager (Singleton MonoBehaviour):** The central hub. It maintains a collection of all active `MapMarkerData` objects and provides methods to register, unregister, and query markers. It also broadcasts events when markers are added or removed, allowing other systems to react.
4.  **MapMarker (MonoBehaviour):** A component attached to GameObjects in the world that represent a point of interest. It holds the specific `MapMarkerData` for that object and automatically registers/unregisters itself with the `MapMarkerManager` based on its GameObject's lifecycle.
5.  **MapMarkerDisplayUI (MonoBehaviour):** A display system (e.g., for a minimap or screen overlay). It subscribes to the `MapMarkerManager`'s events and is responsible for creating, updating, and destroying the visual UI elements (icons, text) for the markers.

---

### Complete C# Unity Example: MapMarkerSystem

This example includes five C# scripts that demonstrate the MapMarkerSystem pattern.

#### 1. `MapMarkerType.cs`

This enum categorizes your markers, allowing for different visual styles or behaviors based on type.

```csharp
using UnityEngine;

namespace MapMarkerSystem
{
    /// <summary>
    /// Defines different types of map markers.
    /// This allows for categorization and specific display rules (e.g., different icons for quest givers vs. enemies).
    /// </summary>
    public enum MapMarkerType
    {
        None,           // Default/undefined
        Player,         // The player's current position (useful for tracking the player on a map)
        QuestGiver,     // NPC that offers quests
        EnemyCamp,      // Location of an enemy group
        ResourceNode,   // Mining, logging, or harvesting point
        Waypoint,       // User-defined or pre-defined navigation point
        POI,            // General Point of Interest
        Shop,           // Merchant or vendor location
        DungeonEntrance // Entrance to an instance or dungeon
    }
}
```

#### 2. `MapMarkerData.cs`

This class holds all the data for a single marker, decoupled from any Unity components or display logic. It's `[Serializable]` so it can be used in the Inspector if needed within other scripts.

```csharp
using UnityEngine;
using System; // For Guid

namespace MapMarkerSystem
{
    /// <summary>
    /// Represents the data for a single map marker.
    /// This is a plain C# class, marked as [Serializable] so it can be exposed in the Unity Inspector
    /// if needed within other MonoBehaviours or ScriptableObjects.
    /// It decouples the *information* about a marker from its *visual representation* or *management logic*.
    /// </summary>
    [Serializable]
    public class MapMarkerData
    {
        [Tooltip("A unique identifier for this marker. Auto-generated if empty.")]
        public string id;

        [Tooltip("The world position of the marker.")]
        public Vector3 position;

        [Tooltip("The type of this marker, used for categorization and display rules.")]
        public MapMarkerType type;

        [Tooltip("A display name for the marker.")]
        public string markerName;

        [Tooltip("A short description for the marker.")]
        [TextArea(1, 3)]
        public string description;

        [Tooltip("The sprite to use for this marker's icon on the UI or minimap.")]
        public Sprite icon;

        /// <summary>
        /// Constructor for MapMarkerData.
        /// Automatically generates a GUID if no ID is provided, ensuring uniqueness.
        /// </summary>
        public MapMarkerData(Vector3 position, MapMarkerType type, string markerName, string description, Sprite icon, string id = "")
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.position = position;
            this.type = type;
            this.markerName = markerName;
            this.description = description;
            this.icon = icon;
        }

        // You might add more data here, like:
        // public bool isVisibleOnMinimap;
        // public float displayRadius;
        // public GameObject associatedGameObject; // Reference to the actual object in the world
    }
}
```

#### 3. `MapMarkerManager.cs`

This is the central manager for all markers. It's a `MonoBehaviour` singleton, allowing global access. It uses events to notify other systems about marker changes.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System; // For Action events
using System.Linq; // For LINQ queries

namespace MapMarkerSystem
{
    /// <summary>
    /// The core of the MapMarkerSystem pattern.
    /// This class acts as a central repository and manager for all active map markers in the scene.
    /// It's implemented as a Singleton MonoBehaviour for easy global access.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensures this manager initializes before other scripts try to register markers.
    public class MapMarkerManager : MonoBehaviour
    {
        // Singleton instance for global access.
        public static MapMarkerManager Instance { get; private set; }

        // Dictionary to store all active markers, keyed by their unique ID.
        // This allows for quick lookup, addition, and removal of markers.
        private Dictionary<string, MapMarkerData> _markers = new Dictionary<string, MapMarkerData>();

        // --- Events ---
        // Events allow other systems (like UI displayers) to react when markers are added or removed
        // without the manager needing to know about or directly control them. This promotes loose coupling.
        public static event Action<MapMarkerData> OnMarkerRegistered;
        public static event Action<string> OnMarkerUnregistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("MapMarkerManager: Multiple instances found! Destroying duplicate.", this);
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                // Optional: Make it persist across scene loads if desired
                // DontDestroyOnLoad(this.gameObject);
            }
        }

        /// <summary>
        /// Registers a new map marker with the system.
        /// If a marker with the same ID already exists, its data will be updated.
        /// This is useful for dynamic markers whose properties (e.g., position, icon) change.
        /// </summary>
        /// <param name="markerData">The data of the marker to register.</param>
        public void RegisterMarker(MapMarkerData markerData)
        {
            if (markerData == null || string.IsNullOrEmpty(markerData.id))
            {
                Debug.LogError("MapMarkerManager: Attempted to register a null or ID-less marker data.");
                return;
            }

            // Update existing marker data if ID matches, otherwise add new.
            _markers[markerData.id] = markerData;
            
            // Invoke the event, notifying any subscribers that a marker has been registered/updated.
            OnMarkerRegistered?.Invoke(markerData);
            // Debug.Log($"MapMarkerManager: Marker '{markerData.markerName}' (ID: {markerData.id}) registered/updated.");
        }

        /// <summary>
        /// Unregisters a map marker from the system using its unique ID.
        /// </summary>
        /// <param name="markerID">The unique ID of the marker to unregister.</param>
        public void UnregisterMarker(string markerID)
        {
            if (string.IsNullOrEmpty(markerID))
            {
                Debug.LogError("MapMarkerManager: Attempted to unregister a marker with an empty ID.");
                return;
            }

            if (_markers.Remove(markerID))
            {
                // Invoke the event, notifying any subscribers that a marker has been unregistered.
                OnMarkerUnregistered?.Invoke(markerID);
                // Debug.Log($"MapMarkerManager: Marker (ID: {markerID}) unregistered.");
            }
            else
            {
                Debug.LogWarning($"MapMarkerManager: Attempted to unregister marker with ID '{markerID}', but it was not found.", this);
            }
        }

        /// <summary>
        /// Retrieves a specific marker's data by its ID.
        /// </summary>
        /// <param name="markerID">The ID of the marker to retrieve.</param>
        /// <returns>The MapMarkerData if found, otherwise null.</returns>
        public MapMarkerData GetMarker(string markerID)
        {
            _markers.TryGetValue(markerID, out MapMarkerData marker);
            return marker;
        }

        /// <summary>
        /// Retrieves all currently registered map markers.
        /// Returns a new list to prevent external modification of the internal dictionary.
        /// </summary>
        /// <returns>A list of all MapMarkerData.</returns>
        public List<MapMarkerData> GetAllMarkers()
        {
            return _markers.Values.ToList();
        }

        /// <summary>
        /// Retrieves all markers of a specific type.
        /// </summary>
        /// <param name="type">The type of markers to retrieve.</param>
        /// <returns>A list of MapMarkerData matching the specified type.</returns>
        public List<MapMarkerData> GetMarkersByType(MapMarkerType type)
        {
            return _markers.Values.Where(m => m.type == type).ToList();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // Important: Unsubscribe all listeners when the manager is destroyed to prevent memory leaks
                OnMarkerRegistered = null;
                OnMarkerUnregistered = null;
            }
        }
    }
}
```

#### 4. `MapMarker.cs`

This component is attached to GameObjects in your world (e.g., an NPC, a resource node). It defines the `MapMarkerData` for that specific object and automatically registers/unregisters itself with the `MapMarkerManager`.

```csharp
using UnityEngine;
using System; // For Guid

namespace MapMarkerSystem
{
    /// <summary>
    /// This component represents an actual map marker in the Unity scene.
    /// It holds the MapMarkerData and automatically registers/unregisters itself
    /// with the MapMarkerManager when its GameObject becomes active/inactive or is destroyed.
    /// </summary>
    public class MapMarker : MonoBehaviour
    {
        [Tooltip("The unique ID for this marker. If left empty, a GUID will be generated.")]
        [SerializeField] private string markerID = "";

        [Tooltip("The type of this marker.")]
        [SerializeField] private MapMarkerType markerType = MapMarkerType.POI;

        [Tooltip("The display name for this marker.")]
        [SerializeField] private string markerName = "New Marker";

        [Tooltip("A short description for this marker.")]
        [TextArea(1, 3)]
        [SerializeField] private string markerDescription = "A point of interest.";

        [Tooltip("The sprite to use for this marker's icon on the UI or minimap.")]
        [SerializeField] private Sprite markerIcon;

        // The actual data object that will be registered with the manager
        private MapMarkerData _markerData;

        /// <summary>
        /// Gets the unique ID of this marker.
        /// </summary>
        public string ID => _markerData?.id ?? markerID;

        private void Awake()
        {
            // Ensure we have a unique ID. Generate one if not provided in the Inspector.
            if (string.IsNullOrEmpty(markerID))
            {
                markerID = Guid.NewGuid().ToString();
            }

            // Create the MapMarkerData object using the inspector-set properties and current position.
            _markerData = new MapMarkerData(
                transform.position, // The marker's position is its GameObject's transform position
                markerType,
                markerName,
                markerDescription,
                markerIcon,
                markerID
            );
        }

        private void OnEnable()
        {
            // Register the marker with the manager when this component becomes active.
            // This makes the marker discoverable by other systems (e.g., UI displayers).
            if (MapMarkerManager.Instance != null)
            {
                _markerData.position = transform.position; // Ensure position is up-to-date at registration
                MapMarkerManager.Instance.RegisterMarker(_markerData);
            }
            else
            {
                Debug.LogWarning($"MapMarker: Manager not found when trying to register '{markerName}'. Is MapMarkerManager in the scene?", this);
            }
        }

        private void OnDisable()
        {
            // Unregister the marker when this component becomes inactive.
            // This removes it from the manager's active list.
            if (MapMarkerManager.Instance != null && _markerData != null)
            {
                MapMarkerManager.Instance.UnregisterMarker(_markerData.id);
            }
        }

        private void OnDestroy()
        {
            // Ensure cleanup if the GameObject is destroyed directly without OnDisable being called.
            if (MapMarkerManager.Instance != null && _markerData != null)
            {
                MapMarkerManager.Instance.UnregisterMarker(_markerData.id);
            }
        }

        /// <summary>
        /// Call this method if the marker's properties (position, type, name, etc.) change
        /// dynamically at runtime and you want the manager and subscribers to be updated.
        /// </summary>
        public void UpdateMarkerData()
        {
            if (_markerData == null) return;

            // Update the data object with current values from the Inspector/GameObject
            _markerData.position = transform.position;
            _markerData.type = markerType;
            _markerData.markerName = markerName;
            _markerData.description = markerDescription;
            _markerData.icon = markerIcon;

            if (MapMarkerManager.Instance != null)
            {
                // Re-register to update its data in the manager's dictionary and trigger the OnMarkerRegistered event.
                MapMarkerManager.Instance.RegisterMarker(_markerData); 
            }
        }

        // --- Editor Helpers (Optional) ---
        // These methods make it easier to visualize the marker in the scene view.
        private void OnDrawGizmos()
        {
            Gizmos.color = GetGizmoColor(markerType);
            Gizmos.DrawSphere(transform.position, 0.5f);
            // Gizmos.DrawIcon requires a 'map_marker.png' in Assets/Gizmos folder for custom icons
            // Gizmos.DrawIcon(transform.position + Vector3.up * 1f, "map_marker.png", true); 
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            // Draw marker name in editor for easy identification
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, markerName);
            #endif
        }

        private Color GetGizmoColor(MapMarkerType type)
        {
            switch (type)
            {
                case MapMarkerType.Player: return Color.blue;
                case MapMarkerType.QuestGiver: return Color.green;
                case MapMarkerType.EnemyCamp: return Color.red;
                case MapMarkerType.ResourceNode: return Color.cyan;
                case MapMarkerType.Waypoint: return Color.white;
                case MapMarkerType.Shop: return Color.magenta;
                case MapMarkerType.DungeonEntrance: return new Color(1f, 0.5f, 0f); // Orange
                case MapMarkerType.POI:
                default: return Color.grey;
            }
        }
    }
}
```

#### 5. `MapMarkerDisplayUI.cs`

This component handles the visual display of markers on a UI Canvas. It subscribes to the `MapMarkerManager` events to dynamically create, update, and destroy UI elements for markers.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Assuming TextMeshPro is used for text display
using System.Linq; // For Clear method

namespace MapMarkerSystem
{
    /// <summary>
    /// This component is responsible for visualizing map markers on a UI Canvas.
    /// It subscribes to the MapMarkerManager's events and dynamically creates/destroys
    /// UI elements (e.g., icons) for each registered marker.
    ///
    /// This demonstrates how the MapMarkerSystem decouples marker data management
    /// from its display, allowing multiple display systems (e.g., minimap, world-space UI, full-screen map)
    /// to use the same underlying marker data.
    /// </summary>
    public class MapMarkerDisplayUI : MonoBehaviour
    {
        [Header("UI Setup")]
        [Tooltip("The RectTransform under which marker UI elements will be instantiated.")]
        [SerializeField] private RectTransform parentCanvasRect;

        [Tooltip("Prefab for a single map marker UI element. Must contain an Image component for the icon.")]
        [SerializeField] private GameObject markerUIPrefab;

        [Header("Display Settings")]
        [Tooltip("The main camera used for world-to-screen point conversions.")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("The Transform of the player or observer. Used to determine relative distance and culling.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Maximum distance from the player a marker will be displayed on the UI.")]
        [SerializeField] private float maxDisplayDistance = 100f;

        [Tooltip("Should markers off-screen show an indicator at the edge of the screen?")]
        [SerializeField] private bool showOffscreenIndicators = true;

        [Tooltip("Prefab for an off-screen directional indicator (e.g., an arrow).")]
        [SerializeField] private GameObject offscreenIndicatorPrefab;

        // Dictionary to keep track of instantiated UI elements for each marker ID.
        private Dictionary<string, GameObject> _activeMarkerUIs = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (parentCanvasRect == null)
            {
                // Try to find a Canvas in parents if not explicitly set
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null) parentCanvasRect = canvas.GetComponent<RectTransform>();
                else Debug.LogError("MapMarkerDisplayUI: parentCanvasRect not set and no parent Canvas found! Marker UI won't function.", this);
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("MapMarkerDisplayUI: mainCamera not set and Camera.main not found! Please assign one in the Inspector or ensure your main camera is tagged 'MainCamera'.", this);
                }
            }

            if (playerTransform == null)
            {
                // Try to find an object tagged "Player"
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                else Debug.LogWarning("MapMarkerDisplayUI: playerTransform not set and no object with tag 'Player' found. Markers will always be relative to (0,0,0) or won't cull.", this);
            }

            if (markerUIPrefab == null)
            {
                Debug.LogError("MapMarkerDisplayUI: markerUIPrefab is not assigned! Marker UI will not be displayed.", this);
            }
            if (showOffscreenIndicators && offscreenIndicatorPrefab == null)
            {
                Debug.LogWarning("MapMarkerDisplayUI: showOffscreenIndicators is true but offscreenIndicatorPrefab is not assigned. Off-screen indicators will not be shown.", this);
            }
        }

        private void OnEnable()
        {
            // Subscribe to the manager's events. This is how the display system
            // learns about new markers and marker removals.
            MapMarkerManager.OnMarkerRegistered += CreateOrUpdateMarkerUI;
            MapMarkerManager.OnMarkerUnregistered += RemoveMarkerUI;

            // When enabled, ensure all existing markers are displayed (e.g., after scene load or disabling/enabling)
            if (MapMarkerManager.Instance != null)
            {
                foreach (MapMarkerData markerData in MapMarkerManager.Instance.GetAllMarkers())
                {
                    CreateOrUpdateMarkerUI(markerData);
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks when this component is disabled or destroyed.
            MapMarkerManager.OnMarkerRegistered -= CreateOrUpdateMarkerUI;
            MapMarkerManager.OnMarkerUnregistered -= RemoveMarkerUI;

            // Clear all displayed UI elements when disabled
            ClearAllMarkerUIs();
        }

        private void LateUpdate()
        {
            if (mainCamera == null || parentCanvasRect == null || playerTransform == null) return;

            // Update positions and visibility of all active marker UIs.
            // Using a temporary list to avoid modifying the dictionary during iteration.
            List<string> markersToUpdate = _activeMarkerUIs.Keys.ToList();
            foreach (string markerID in markersToUpdate)
            {
                GameObject markerUI = _activeMarkerUIs[markerID];
                MapMarkerData markerData = MapMarkerManager.Instance.GetMarker(markerID);

                // If markerData is null, it means the marker was unregistered, but UI removal might not have happened yet.
                // We double-check and remove if necessary.
                if (markerData == null)
                {
                    RemoveMarkerUI(markerID);
                    continue;
                }

                UpdateMarkerUIPosition(markerUI, markerData);
            }
        }

        /// <summary>
        /// Creates a new UI element for a marker or updates an existing one.
        /// Called when MapMarkerManager.OnMarkerRegistered is invoked.
        /// </summary>
        private void CreateOrUpdateMarkerUI(MapMarkerData markerData)
        {
            if (markerData == null || markerUIPrefab == null || parentCanvasRect == null) return;

            GameObject markerUI;
            if (_activeMarkerUIs.TryGetValue(markerData.id, out markerUI))
            {
                // Update existing marker's properties if it already exists (e.g., icon, name)
                UpdateMarkerUIProperties(markerUI, markerData);
            }
            else
            {
                // Instantiate a new UI element for the marker
                markerUI = Instantiate(markerUIPrefab, parentCanvasRect);
                markerUI.name = $"MarkerUI_{markerData.markerName}_{markerData.id.Substring(0, 4)}"; // Add a unique suffix
                _activeMarkerUIs.Add(markerData.id, markerUI);
                UpdateMarkerUIProperties(markerUI, markerData);
            }

            // Ensure the UI element is active and its position is updated immediately.
            markerUI.SetActive(true);
            UpdateMarkerUIPosition(markerUI, markerData);
        }

        /// <summary>
        /// Removes a marker's UI element.
        /// Called when MapMarkerManager.OnMarkerUnregistered is invoked.
        /// </summary>
        private void RemoveMarkerUI(string markerID)
        {
            if (_activeMarkerUIs.TryGetValue(markerID, out GameObject markerUI))
            {
                Destroy(markerUI);
                _activeMarkerUIs.Remove(markerID);
            }
        }

        /// <summary>
        /// Clears all currently displayed marker UIs.
        /// </summary>
        private void ClearAllMarkerUIs()
        {
            foreach (var uiObject in _activeMarkerUIs.Values)
            {
                if (uiObject != null)
                {
                    Destroy(uiObject);
                }
            }
            _activeMarkerUIs.Clear();
        }

        /// <summary>
        /// Updates the visual properties (icon, name, etc.) of a marker UI element.
        /// </summary>
        private void UpdateMarkerUIProperties(GameObject markerUI, MapMarkerData markerData)
        {
            // Find and set the icon (Image component on the marker UI prefab itself or its children)
            Image iconImage = markerUI.GetComponent<Image>();
            if (iconImage == null) iconImage = markerUI.GetComponentInChildren<Image>(); 
            if (iconImage != null)
            {
                iconImage.sprite = markerData.icon;
                iconImage.enabled = markerData.icon != null; // Hide the image component if no icon sprite is assigned
            }

            // Find and set the name text (TextMeshProUGUI component in children)
            TMP_Text nameText = markerUI.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = markerData.markerName;
            }

            // You can add more logic here, e.g., set color based on type, enable/disable specific elements,
            // or apply type-specific animations.
        }

        /// <summary>
        /// Calculates and sets the screen position of a marker UI element.
        /// Handles off-screen markers by clamping them to the screen edge and showing an indicator.
        /// </summary>
        private void UpdateMarkerUIPosition(GameObject markerUI, MapMarkerData markerData)
        {
            Vector3 worldPosition = markerData.position;
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;

            // Cull markers beyond maxDisplayDistance
            float distance = Vector3.Distance(worldPosition, playerPosition);
            if (distance > maxDisplayDistance)
            {
                markerUI.SetActive(false);
                return;
            }
            
            // Convert world position to screen position
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Check if the marker is behind the camera or out of bounds (viewport point Z is negative or outside 0-1 range)
            bool isOffScreen = screenPosition.z < 0 || 
                               screenPosition.x < 0 || screenPosition.x > Screen.width ||
                               screenPosition.y < 0 || screenPosition.y > Screen.height;
            
            RectTransform markerRect = markerUI.GetComponent<RectTransform>();
            if (markerRect == null) return;

            // Manage off-screen indicator visibility
            GameObject indicator = null;
            Transform indicatorTransform = markerUI.transform.Find("OffscreenIndicator");

            if (showOffscreenIndicators && offscreenIndicatorPrefab != null)
            {
                if (isOffScreen && indicatorTransform == null)
                {
                    indicator = Instantiate(offscreenIndicatorPrefab, markerUI.transform);
                    indicator.name = "OffscreenIndicator";
                    indicatorTransform = indicator.transform; // Update reference
                }
                else if (!isOffScreen && indicatorTransform != null)
                {
                    Destroy(indicatorTransform.gameObject); // Destroy indicator if no longer off-screen
                }

                if (indicatorTransform != null)
                {
                    indicator = indicatorTransform.gameObject;
                }
            }


            if (isOffScreen && showOffscreenIndicators && indicator != null)
            {
                indicator.SetActive(true);
                // Calculate the direction from the center of the screen to the marker
                Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                Vector3 directionToMarker = (screenPosition - screenCenter);
                directionToMarker.z = 0; // Ignore Z for 2D UI direction

                // Clamp the position to the edge of the screen
                Vector2 clampedPosition = screenPosition;
                float borderOffset = 50f; // Padding from the screen edge
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, borderOffset, Screen.width - borderOffset);
                clampedPosition.y = Mathf.Clamp(clampedPosition.y, borderOffset, Screen.height - borderOffset);
                
                // Get rotation for the indicator
                float angle = Mathf.Atan2(directionToMarker.y, directionToMarker.x) * Mathf.Rad2Deg - 90; // Adjust for arrow pointing up by default
                indicator.transform.localEulerAngles = new Vector3(0, 0, angle);

                // Convert clamped screen position to canvas local position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvasRect,
                    clampedPosition,
                    mainCamera,
                    out Vector2 localPointer);

                markerRect.anchoredPosition = localPointer;
                markerUI.SetActive(true); // Always show main marker icon at the clamped position
            }
            else
            {
                // Convert world position to canvas local position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvasRect,
                    screenPosition,
                    mainCamera,
                    out Vector2 localPointer);

                markerRect.anchoredPosition = localPointer;
                markerUI.SetActive(true); // Ensure active if within bounds
                if (indicator != null) indicator.SetActive(false); // Hide off-screen indicator
            }
            
            // Optional: Scale marker based on distance
            // float normalizedDistance = Mathf.Clamp01(distance / maxDisplayDistance);
            // float scale = Mathf.Lerp(1.0f, 0.5f, normalizedDistance); // Smaller further away
            // markerRect.localScale = Vector3.one * scale;
        }
    }
}
```

---

### How to Implement and Use in Unity

1.  **Project Setup:**
    *   Create a new Unity project or open an existing one.
    *   Create a folder named `MapMarkerSystem` (or similar) inside your `Assets/Scripts` folder.
    *   Copy all five C# scripts (`MapMarkerType.cs`, `MapMarkerData.cs`, `MapMarkerManager.cs`, `MapMarker.cs`, `MapMarkerDisplayUI.cs`) into this new folder.

2.  **Create Sprites for Icons:**
    *   Import some simple square or circular sprites to use as marker icons (e.g., a question mark for quest givers, a sword for enemy camps, an arrow for off-screen indicators).
    *   Set their Texture Type to `Sprite (2D and UI)` and apply.

3.  **Setup the `MapMarkerManager`:**
    *   In your scene, create an empty GameObject (right-click in Hierarchy -> Create Empty).
    *   Rename it to `MapMarkerManager`.
    *   Add the `MapMarkerSystem.MapMarkerManager` component to this GameObject.
    *   This object should typically be at the root of your scene and remain active throughout gameplay.

4.  **Setup the `MapMarkerDisplayUI`:**
    *   Create a UI Canvas (right-click in Hierarchy -> UI -> Canvas).
        *   Set its `Render Mode` to `Screen Space - Overlay` for a standard overlay UI.
        *   If using `Screen Space - Camera`, drag your `Main Camera` into the `Render Camera` slot.
    *   As a child of the Canvas, create an empty GameObject and name it `MapMarkerUIContainer`.
    *   Add the `MapMarkerSystem.MapMarkerDisplayUI` component to `MapMarkerUIContainer`.
    *   In the `MapMarkerDisplayUI` Inspector:
        *   Drag your **Canvas** GameObject's `RectTransform` to the `Parent Canvas Rect` field.
        *   Drag your **Main Camera** GameObject to the `Main Camera` field.
        *   Drag your **Player** GameObject's `Transform` to the `Player Transform` field. (Ensure your player has the tag "Player" or explicitly assign it).
        *   **Create Prefabs for UI Elements:**
            *   **`MarkerUIPrefab`:**
                *   Create an empty GameObject as a child of `MapMarkerUIContainer`. Name it `_MarkerUIPrefab`.
                *   Add an `Image` component to it. This will display the marker's icon.
                *   (Optional, but recommended) Add a `TextMeshPro - Text (UI)` component as a child to `_MarkerUIPrefab`. This will display the marker's name. Adjust its `Rect Transform` and font settings.
                *   Adjust the `Rect Transform` of `_MarkerUIPrefab` (e.g., width/height 32x32).
                *   Drag `_MarkerUIPrefab` from the Hierarchy into your Project window to create a prefab.
                *   Delete `_MarkerUIPrefab` from the Hierarchy.
                *   Assign this new prefab to the `Marker UI Prefab` field on `MapMarkerDisplayUI`.
            *   **`OffscreenIndicatorPrefab` (Optional):**
                *   Create another empty GameObject. Name it `_OffscreenIndicatorPrefab`.
                *   Add an `Image` component and assign an arrow sprite to it.
                *   Adjust its `Rect Transform` (e.g., 20x20) and `Pivot` (e.g., X=0.5, Y=0.0) so it points towards the marker.
                *   Drag `_OffscreenIndicatorPrefab` from the Hierarchy into your Project window to create a prefab.
                *   Delete `_OffscreenIndicatorPrefab` from the Hierarchy.
                *   Assign this new prefab to the `Offscreen Indicator Prefab` field on `MapMarkerDisplayUI`.

5.  **Create World Markers:**
    *   For any GameObject in your scene that should be a map marker (e.g., an NPC, a resource vein, a quest item):
        *   Select the GameObject.
        *   Add the `MapMarkerSystem.MapMarker` component to it.
        *   In the Inspector for `MapMarker`:
            *   Set `Marker Type` (e.g., `QuestGiver`, `EnemyCamp`).
            *   Set `Marker Name` (e.g., "Bob the Quest Giver").
            *   Assign a `Marker Icon` sprite.
            *   (Optional) Add a `Marker ID` (if left empty, a unique ID will be generated).
    *   Position these GameObjects in your world.

6.  **Run the Scene:**
    *   You should now see UI icons for your `MapMarker` GameObjects appearing on your Canvas.
    *   As you move your player/camera, the UI icons should update their positions.
    *   Markers that go off-screen (if `Show Offscreen Indicators` is enabled) will show directional arrows at the edge of the UI.

---

### Example: Dynamic Marker Creation and Interaction Script

You can create and manage markers entirely through code. Here's an example script to demonstrate this:

```csharp
using UnityEngine;
using MapMarkerSystem; // Important to include your namespace
using System.Collections.Generic;

/// <summary>
/// This script demonstrates how to dynamically create, query, update,
/// and remove map markers at runtime using the MapMarkerManager.
/// </summary>
public class DynamicMarkerExample : MonoBehaviour
{
    [Tooltip("Assign your player's Transform here for dynamic marker positioning.")]
    public Transform playerTransform;

    [Header("Marker Icons")]
    public Sprite questGiverIcon;
    public Sprite resourceIcon;
    public Sprite playerIcon; // For the dynamic player marker

    private string dynamicQuestMarkerID; // To keep track of a dynamically created marker

    void Start()
    {
        if (MapMarkerManager.Instance == null)
        {
            Debug.LogError("MapMarkerManager not found in the scene! Dynamic markers won't work.");
            return;
        }

        // --- Example 1: Creating a temporary marker dynamically ---
        // This marker appears 10 units in front of the player at game start.
        if (playerTransform != null)
        {
            Vector3 dynamicMarkerPos = playerTransform.position + playerTransform.forward * 10f;
            MapMarkerData dynamicQuestGiver = new MapMarkerData(
                dynamicMarkerPos,
                MapMarkerType.QuestGiver,
                "Dynamic Quest",
                "A new quest has appeared!",
                questGiverIcon
            );
            MapMarkerManager.Instance.RegisterMarker(dynamicQuestGiver);
            dynamicQuestMarkerID = dynamicQuestGiver.id; // Store ID to potentially remove later
            Debug.Log($"Dynamically created temporary quest marker with ID: {dynamicQuestMarkerID}");
        }

        // --- Example 2: Creating a persistent marker with a known ID ---
        // This marker is always at a fixed location unless explicitly moved/removed.
        MapMarkerManager.Instance.RegisterMarker(new MapMarkerData(
            new Vector3(50, 0, 50),
            MapMarkerType.ResourceNode,
            "Rare Ore Vein",
            "Contains valuable resources.",
            resourceIcon,
            "PersistentOreVein001" // Assign a specific ID for known markers
        ));
        Debug.Log("Registered persistent ore vein marker.");
    }

    void Update()
    {
        if (MapMarkerManager.Instance == null) return;

        // --- Example 3: Updating the player's position as a marker ---
        // This demonstrates how to constantly update a marker's data.
        if (playerTransform != null && playerIcon != null)
        {
            MapMarkerManager.Instance.RegisterMarker(new MapMarkerData(
                playerTransform.position,
                MapMarkerType.Player,
                "Player",
                "Your current location.",
                playerIcon,
                "PlayerMarkerID" // A consistent ID for the player
            ));
        }

        // --- Example 4: Querying markers ---
        // You can get markers based on type, ID, or all markers.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            List<MapMarkerData> questGivers = MapMarkerManager.Instance.GetMarkersByType(MapMarkerType.QuestGiver);
            Debug.Log($"Found {questGivers.Count} active quest givers.");
            foreach (var marker in questGivers)
            {
                Debug.Log($"  - Quest Giver: {marker.markerName} at {marker.position}");
            }
        }

        // --- Example 5: Removing a dynamic marker ---
        // Press 'R' to remove the dynamically created quest marker.
        if (Input.GetKeyDown(KeyCode.R) && !string.IsNullOrEmpty(dynamicQuestMarkerID))
        {
            MapMarkerManager.Instance.UnregisterMarker(dynamicQuestMarkerID);
            Debug.Log($"Removed dynamic quest marker with ID: {dynamicQuestMarkerID}");
            dynamicQuestMarkerID = null; // Clear the ID as it's no longer valid
        }

        // --- Example 6: Updating a marker's position through code ---
        // Press 'T' to make the "Rare Ore Vein" marker move.
        if (Input.GetKey(KeyCode.T))
        {
            MapMarkerData oreVein = MapMarkerManager.Instance.GetMarker("PersistentOreVein001");
            if (oreVein != null)
            {
                oreVein.position += Vector3.right * Time.deltaTime * 5f; // Make it 'move'
                // Re-register to update its data in the manager and trigger the UI to update its position.
                MapMarkerManager.Instance.RegisterMarker(oreVein); 
                Debug.Log($"Moving 'Rare Ore Vein' marker to {oreVein.position}");
            }
        }
    }
}
```

**To use `DynamicMarkerExample`:**
1.  Create an empty GameObject in your scene, name it `_DynamicMarkerExample`.
2.  Attach the `DynamicMarkerExample.cs` script to it.
3.  Assign your Player's `Transform` to the `Player Transform` field.
4.  Assign the `questGiverIcon`, `resourceIcon`, and `playerIcon` sprites in the Inspector.
5.  Run the game and press 'Q', 'R', 'T' to see the dynamic behavior.

This complete example provides a solid foundation for implementing a robust MapMarkerSystem in your Unity projects, focusing on clarity, best practices, and practical application.