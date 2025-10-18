// Unity Design Pattern Example: SceneLayerSystem
// This script demonstrates the SceneLayerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **SceneLayerSystem** design pattern. This pattern helps organize GameObjects in a scene into logical "layers," allowing for batch operations like toggling visibility, applying effects, or processing updates across entire groups of related objects.

This script is designed to be dropped directly into a Unity project.

---

### **How to Use This Example in Unity:**

1.  **Create a C# Script:** In your Unity project, create a new C# script (e.g., named `SceneLayerSystem.cs`) and copy all the code below into it.
2.  **Create Manager GameObject:** In your scene, create an empty GameObject and name it `SceneLayerManager`. Attach the `SceneLayerManager` component from the script to it.
3.  **Create Demo GameObject:** Create another empty GameObject and name it `GameController` (or anything else). Attach the `SceneLayerDemo` component from the script to it.
4.  **Assign Layers to Your Objects:**
    *   For any GameObject you want to belong to a specific layer (e.g., your player, UI elements, particle effects), attach the `SceneLayerMember` component to it.
    *   In the Inspector for `SceneLayerMember`, select the desired `Layer Type` from the dropdown (e.g., `Gameplay`, `UI`, `Effects`, `Debug`, `Environment`).
    *   **Example Setup:**
        *   Create a 3D Cube: Add `SceneLayerMember`, set `Layer Type` to `Gameplay`.
        *   Create a UI Canvas: Add `SceneLayerMember`, set `Layer Type` to `UI`. Add a Button as a child of the Canvas.
        *   Create a Particle System: Add `SceneLayerMember`, set `Layer Type` to `Effects`.
        *   Create a 3D Sphere: Add `SceneLayerMember`, set `Layer Type` to `Debug`. (This will be hidden by default by the demo script).
        *   Create a 3D Plane: Add `SceneLayerMember`, set `Layer Type` to `Environment`.
5.  **Run the Scene:**
    *   The `SceneLayerDemo` script will display instructions on the screen.
    *   Press `1`, `2`, `3`, `4`, `5` to toggle the visibility of the `Gameplay`, `UI`, `Effects`, `Debug`, and `Environment` layers respectively.
    *   Press `F1` to toggle the `Canvas` component's `enabled` state on all UI elements.
    *   Press `F2` to toggle a green highlight on all `Gameplay` objects.

---

### **SceneLayerSystem.cs**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() and .ToArray()

namespace YourGame.SceneManagement
{
    // =====================================================================================
    // 1. SceneLayerType Enum: Defines the distinct logical layers in your scene.
    //    Use an enum for compile-time safety, clarity, and easy dropdown selection in Unity Inspector.
    // =====================================================================================
    public enum SceneLayerType
    {
        Default,        // Objects not explicitly assigned to another layer.
        Gameplay,       // Core game elements: player, enemies, interactables, level geometry.
        UI,             // User interface elements: menus, HUD, popups.
        Effects,        // Visual effects: particle systems, temporary decals, post-processing triggers.
        Debug,          // Debugging visualizations, editor-only objects, performance stats.
        Environment,    // Static background elements, distant scenery, non-interactive environment.
        Cinematics,     // Objects specific to cutscenes or narrative sequences.
        // Add more specific layers as your game's needs evolve (e.g., "Audio", "Cameras", "Networking").
    }

    // =====================================================================================
    // 2. SceneLayer Class: Represents a single logical layer.
    //    It manages the collection of GameObjects associated with it and its overall visibility state.
    // =====================================================================================
    public class SceneLayer
    {
        public SceneLayerType LayerType { get; private set; } // The type/name of this layer.
        public bool IsVisible { get; private set; }           // The current visibility state of this layer.

        // Using HashSet for efficient (O(1) average time complexity) addition, removal,
        // and checking for existence of GameObjects within the layer.
        private HashSet<GameObject> _members = new HashSet<GameObject>();

        /// <summary>
        /// Constructor for a SceneLayer.
        /// </summary>
        /// <param name="type">The SceneLayerType this instance will represent.</param>
        public SceneLayer(SceneLayerType type)
        {
            LayerType = type;
            IsVisible = true; // Layers are visible by default upon creation.
        }

        /// <summary>
        /// Adds a GameObject to this layer.
        /// When added, its active state is adjusted to match the layer's current visibility.
        /// </summary>
        /// <param name="obj">The GameObject to add.</param>
        public void AddObject(GameObject obj)
        {
            if (obj == null) return;

            // If the object is successfully added (wasn't already present)...
            if (_members.Add(obj))
            {
                // If the layer is currently invisible, deactivate the added object.
                // We don't force activate if the layer is visible, as the object
                // might have been intentionally deactivated by other game logic.
                if (!IsVisible)
                {
                    obj.SetActive(false);
                }
                //Debug.Log($"[SceneLayer {LayerType}] Added: {obj.name}. Current visibility: {obj.activeSelf}");
            }
        }

        /// <summary>
        /// Removes a GameObject from this layer.
        /// When removed, its active state is restored to visible if it was only hidden by this layer system.
        /// </summary>
        /// <param name="obj">The GameObject to remove.</param>
        public void RemoveObject(GameObject obj)
        {
            if (obj == null) return;

            // If the object is successfully removed...
            if (_members.Remove(obj))
            {
                // If the layer was previously invisible, and this object was deactivated by it,
                // reactivate it to ensure it doesn't remain hidden artificially.
                if (!IsVisible && !obj.activeSelf)
                {
                    obj.SetActive(true);
                }
                //Debug.Log($"[SceneLayer {LayerType}] Removed: {obj.name}. Object restored active state.");
            }
        }

        /// <summary>
        /// Sets the visibility of the entire layer. All member GameObjects are activated or deactivated accordingly.
        /// </summary>
        /// <param name="visible">True to make the layer visible, false to hide it.</param>
        public void SetVisibility(bool visible)
        {
            if (IsVisible == visible) return; // No change needed if already in the desired state.

            IsVisible = visible;
            foreach (var obj in _members)
            {
                // Important: Check for null in case an object was destroyed but not yet unregistered.
                if (obj != null)
                {
                    obj.SetActive(visible);
                }
            }
            Debug.Log($"[SceneLayer {LayerType}] Layer visibility set to: {visible}. Affecting {_members.Count} objects.");
        }

        /// <summary>
        /// Returns a copy of the collection of all GameObjects currently in this layer.
        /// Using ToList() ensures the internal HashSet cannot be modified externally.
        /// </summary>
        public IEnumerable<GameObject> GetMembers()
        {
            return _members.ToList();
        }

        /// <summary>
        /// Executes a specified action for each GameObject currently in this layer.
        /// This is useful for batch operations that are not just about visibility.
        /// </summary>
        /// <param name="action">The action (a delegate taking a GameObject) to perform.</param>
        public void ForEach(System.Action<GameObject> action)
        {
            if (action == null) return;

            // Iterate over a copy (.ToArray()) to prevent issues if the collection is modified
            // (e.g., an object removes itself from the layer) during the iteration.
            foreach (var obj in _members.ToArray())
            {
                if (obj != null)
                {
                    action.Invoke(obj);
                }
            }
        }

        /// <summary>
        /// Clears all GameObjects from this layer.
        /// </summary>
        public void Clear()
        {
            _members.Clear();
        }
    }

    // =====================================================================================
    // 3. SceneLayerManager (The central singleton orchestrator)
    //    This MonoBehaviour acts as the public interface for the Scene Layer System.
    //    It's a singleton to provide global access and manages all individual SceneLayer instances.
    // =====================================================================================
    public class SceneLayerManager : MonoBehaviour
    {
        // Singleton instance, allowing easy global access (e.g., SceneLayerManager.Instance.RegisterObject(...)).
        public static SceneLayerManager Instance { get; private set; }

        // Dictionary to hold all SceneLayer instances, indexed by their SceneLayerType.
        private Dictionary<SceneLayerType, SceneLayer> _layers = new Dictionary<SceneLayerType, SceneLayer>();

        // --- MonoBehaviour Lifecycle ---

        private void Awake()
        {
            // Enforce singleton pattern.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("SceneLayerManager: Another instance of SceneLayerManager detected. Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Ensure the manager persists across scene loads. Remove if you want a new manager per scene.
            DontDestroyOnLoad(gameObject);

            InitializeLayers();
            Debug.Log($"SceneLayerManager initialized with {_layers.Count} defined layers.");
        }

        private void OnDestroy()
        {
            // Clear the singleton reference when this manager is destroyed.
            if (Instance == this)
            {
                Instance = null;
            }
            // Optional: Clear all layers, though individual objects should unregister themselves.
            foreach (var layer in _layers.Values)
            {
                layer.Clear();
            }
            _layers.Clear();
        }

        /// <summary>
        /// Initializes all defined SceneLayerType enums into actual SceneLayer instances.
        /// This ensures every layer type has a corresponding manager object.
        /// </summary>
        private void InitializeLayers()
        {
            foreach (SceneLayerType type in System.Enum.GetValues(typeof(SceneLayerType)))
            {
                if (!_layers.ContainsKey(type))
                {
                    _layers.Add(type, new SceneLayer(type));
                    //Debug.Log($"Initialized SceneLayer: {type}");
                }
            }
        }

        // --- Public API for Layer Interaction ---

        /// <summary>
        /// Registers a GameObject with a specified SceneLayer.
        /// This should primarily be called by SceneLayerMember.OnEnable().
        /// </summary>
        /// <param name="obj">The GameObject to register.</param>
        /// <param name="layerType">The SceneLayerType to register the object with.</param>
        public void RegisterObject(GameObject obj, SceneLayerType layerType)
        {
            if (obj == null) return;

            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                layer.AddObject(obj);
            }
            else
            {
                Debug.LogWarning($"SceneLayerManager: Attempted to register '{obj.name}' to unknown layer type: {layerType}.");
            }
        }

        /// <summary>
        /// Unregisters a GameObject from a specified SceneLayer.
        /// This should primarily be called by SceneLayerMember.OnDisable() or OnDestroy().
        /// </summary>
        /// <param name="obj">The GameObject to unregister.</param>
        /// <param name="layerType">The SceneLayerType to unregister the object from.</param>
        public void UnregisterObject(GameObject obj, SceneLayerType layerType)
        {
            if (obj == null) return;

            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                layer.RemoveObject(obj);
            }
            // No warning here, as an object might try to unregister from a layer it wasn't registered to,
            // or the layer might have been cleared already if the manager is shutting down.
        }

        /// <summary>
        /// Sets the visibility for an entire SceneLayer. All GameObjects in that layer
        /// will be activated or deactivated accordingly.
        /// </summary>
        /// <param name="layerType">The type of layer to modify visibility for.</param>
        /// <param name="visible">True to make the layer visible, false to hide it.</param>
        public void SetLayerVisibility(SceneLayerType layerType, bool visible)
        {
            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                layer.SetVisibility(visible);
            }
            else
            {
                Debug.LogWarning($"SceneLayerManager: Attempted to set visibility for unknown layer type: {layerType}.");
            }
        }

        /// <summary>
        /// Toggles the current visibility state of a specific SceneLayer.
        /// </summary>
        /// <param name="layerType">The type of layer to toggle.</param>
        public void ToggleLayerVisibility(SceneLayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                SetLayerVisibility(layerType, !layer.IsVisible);
            }
            else
            {
                Debug.LogWarning($"SceneLayerManager: Attempted to toggle visibility for unknown layer type: {layerType}.");
            }
        }

        /// <summary>
        /// Gets a read-only collection of GameObjects currently registered to a specific layer.
        /// </summary>
        /// <param name="layerType">The type of layer to query.</param>
        /// <returns>An IEnumerable of GameObjects in the specified layer. Returns an empty enumerable if the layer doesn't exist.</returns>
        public IEnumerable<GameObject> GetLayerObjects(SceneLayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                return layer.GetMembers();
            }
            Debug.LogWarning($"SceneLayerManager: Attempted to get objects from unknown layer type: {layerType}. Returning empty list.");
            return Enumerable.Empty<GameObject>(); // Return an empty collection safely.
        }

        /// <summary>
        /// Executes a given action on each GameObject within a specified layer.
        /// This is useful for batch operations that don't just involve visibility (e.g., apply a shader, update a component).
        /// </summary>
        /// <param name="layerType">The type of layer to iterate through.</param>
        /// <param name="action">The action (a delegate taking a GameObject) to perform on each GameObject.</param>
        public void ForEachInLayer(SceneLayerType layerType, System.Action<GameObject> action)
        {
            if (action == null) return;

            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                layer.ForEach(action);
            }
            else
            {
                Debug.LogWarning($"SceneLayerManager: Attempted to iterate unknown layer type: {layerType}.");
            }
        }

        /// <summary>
        /// Checks if a given layer is currently visible.
        /// </summary>
        /// <param name="layerType">The type of layer to check.</param>
        /// <returns>True if the layer exists and is visible, false otherwise (or if the layer doesn't exist).</returns>
        public bool IsLayerVisible(SceneLayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out SceneLayer layer))
            {
                return layer.IsVisible;
            }
            return false; // Layer doesn't exist, treat as not visible.
        }
    }

    // =====================================================================================
    // 4. SceneLayerMember (Component for GameObjects to declare their layer)
    //    Attach this script to any GameObject that you want to be part of a SceneLayer.
    //    It automatically registers and unregisters the GameObject with the SceneLayerManager
    //    based on its active state in the hierarchy.
    // =====================================================================================
    public class SceneLayerMember : MonoBehaviour
    {
        [Tooltip("The logical layer this GameObject belongs to.")]
        public SceneLayerType layerType = SceneLayerType.Default;

        private void OnEnable()
        {
            // It's crucial that SceneLayerManager.Instance is ready when this runs.
            // If objects enable before the Manager's Awake (e.g., if Manager is in a different scene
            // or if initialization order is tricky), you might need a more robust delayed registration.
            // For typical setups where the manager is in the same scene and its Awake runs first, this is fine.
            if (SceneLayerManager.Instance != null)
            {
                SceneLayerManager.Instance.RegisterObject(gameObject, layerType);
            }
            else
            {
                Debug.LogWarning($"SceneLayerMember on '{gameObject.name}': SceneLayerManager not yet initialized. " +
                                 "Registration will be skipped or may require manual retry if not persistent.");
                // A robust solution for delayed registration could involve an event or a coroutine.
                // For this example, we assume manager is ready.
            }
        }

        private void OnDisable()
        {
            // Unregister when the object is disabled or destroyed.
            // This ensures the layer system doesn't hold references to inactive/destroyed objects.
            if (SceneLayerManager.Instance != null)
            {
                SceneLayerManager.Instance.UnregisterObject(gameObject, layerType);
            }
        }

        // OnDestroy is also included to handle cases where an object might be destroyed directly
        // without OnDisable being called (e.g., if it was already inactive).
        private void OnDestroy()
        {
            if (SceneLayerManager.Instance != null)
            {
                SceneLayerManager.Instance.UnregisterObject(gameObject, layerType);
            }
        }
    }


    // =====================================================================================
    // 5. SceneLayerDemo (Example Usage Demonstration)
    //    This script showcases how other parts of your game (e.g., a UI manager, a debug console)
    //    would interact with the SceneLayerManager to control layers.
    //    Attach this to an empty GameObject in your scene (e.g., "GameController").
    // =====================================================================================
    public class SceneLayerDemo : MonoBehaviour
    {
        [Header("Layer Visibility Control (Press keys)")]
        [Tooltip("Key to toggle visibility of the Gameplay layer.")]
        public KeyCode toggleGameplay = KeyCode.Alpha1;
        [Tooltip("Key to toggle visibility of the UI layer.")]
        public KeyCode toggleUI = KeyCode.Alpha2;
        [Tooltip("Key to toggle visibility of the Effects layer.")]
        public KeyCode toggleEffects = KeyCode.Alpha3;
        [Tooltip("Key to toggle visibility of the Debug layer.")]
        public KeyCode toggleDebug = KeyCode.Alpha4;
        [Tooltip("Key to toggle visibility of the Environment layer.")]
        public KeyCode toggleEnvironment = KeyCode.Alpha5;

        [Header("Batch Operations (Press keys)")]
        [Tooltip("Key to toggle the 'Canvas' component on all UI layer objects.")]
        public KeyCode toggleUIComponent = KeyCode.F1;
        [Tooltip("Key to toggle a green highlight color on all Gameplay layer objects.")]
        public KeyCode highlightGameplay = KeyCode.F2;

        private void Start()
        {
            // Ensure the manager exists and is initialized.
            if (SceneLayerManager.Instance == null)
            {
                Debug.LogError("SceneLayerDemo: SceneLayerManager not found in scene! Please ensure it's present and active.");
                enabled = false; // Disable this demo script if manager is missing.
                return;
            }

            Debug.Log("SceneLayerDemo started. Use assigned keys to control layers.");

            // Example initial setup: Hide the Debug layer when the game starts.
            // This is useful for editor-only objects or diagnostic tools that shouldn't be visible in-game.
            SceneLayerManager.Instance.SetLayerVisibility(SceneLayerType.Debug, false);
        }

        private void Update()
        {
            // --- Visibility Toggling Examples ---
            // These demonstrate how easy it is to hide/show entire logical groups of objects.
            if (Input.GetKeyDown(toggleGameplay))
            {
                SceneLayerManager.Instance.ToggleLayerVisibility(SceneLayerType.Gameplay);
            }
            if (Input.GetKeyDown(toggleUI))
            {
                SceneLayerManager.Instance.ToggleLayerVisibility(SceneLayerType.UI);
            }
            if (Input.GetKeyDown(toggleEffects))
            {
                SceneLayerManager.Instance.ToggleLayerVisibility(SceneLayerType.Effects);
            }
            if (Input.GetKeyDown(toggleDebug))
            {
                SceneLayerManager.Instance.ToggleLayerVisibility(SceneLayerType.Debug);
            }
            if (Input.GetKeyDown(toggleEnvironment))
            {
                SceneLayerManager.Instance.ToggleLayerVisibility(SceneLayerType.Environment);
            }

            // --- Batch Operations / Iteration Examples ---
            // These show how to perform more granular actions on objects within a layer,
            // beyond just toggling their active state.

            if (Input.GetKeyDown(toggleUIComponent))
            {
                Debug.Log($"Toggling Canvas component on all UI elements in '{SceneLayerType.UI}' layer.");
                // Use ForEachInLayer to apply an action to every GameObject in the UI layer.
                SceneLayerManager.Instance.ForEachInLayer(SceneLayerType.UI, uiObject =>
                {
                    // Find and toggle a specific component (e.g., a Canvas) on UI objects.
                    // This allows for granular control without deactivating the entire GameObject.
                    Canvas canvas = uiObject.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.enabled = !canvas.enabled; // Toggle the Canvas component's enabled state.
                        Debug.Log($"- Toggled Canvas on {uiObject.name}: {canvas.enabled}");
                    }
                });
            }

            if (Input.GetKeyDown(highlightGameplay))
            {
                Debug.Log($"Toggling green highlight on all GameObjects in '{SceneLayerType.Gameplay}' layer.");
                // Use GetLayerObjects to iterate through objects and apply a specific effect.
                foreach (var gameplayObject in SceneLayerManager.Instance.GetLayerObjects(SceneLayerType.Gameplay))
                {
                    // Find a Renderer component and change its material color.
                    // This demonstrates interacting with specific visual aspects of layer members.
                    Renderer renderer = gameplayObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Accessing .material creates an instance if not already unique.
                        Material material = renderer.material;
                        if (material != null)
                        {
                            if (material.color == Color.green)
                            {
                                material.color = Color.white; // Revert to default color.
                            }
                            else
                            {
                                material.color = Color.green; // Apply highlight color.
                            }
                        }
                    }
                }
            }
        }

        // --- OnGUI for displaying instructions in the game view ---
        private void OnGUI()
        {
            // Custom GUIStyle for better readability.
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(5, 5, 5, 5);

            // Create a background box for the text.
            GUI.Box(new Rect(10, 10, 350, 420), "", GUI.skin.box);

            GUILayout.BeginArea(new Rect(20, 20, 330, 400));
            GUILayout.Label("<b>Scene Layer System Demo</b>", style);
            GUILayout.Space(15);

            GUILayout.Label($"Press '<b>{toggleGameplay}</b>' to toggle <b>Gameplay</b> (Visible: {GetLayerVisibilityStatus(SceneLayerType.Gameplay)})", style);
            GUILayout.Label($"Press '<b>{toggleUI}</b>' to toggle <b>UI</b> (Visible: {GetLayerVisibilityStatus(SceneLayerType.UI)})", style);
            GUILayout.Label($"Press '<b>{toggleEffects}</b>' to toggle <b>Effects</b> (Visible: {GetLayerVisibilityStatus(SceneLayerType.Effects)})", style);
            GUILayout.Label($"Press '<b>{toggleDebug}</b>' to toggle <b>Debug</b> (Visible: {GetLayerVisibilityStatus(SceneLayerType.Debug)})", style);
            GUILayout.Label($"Press '<b>{toggleEnvironment}</b>' to toggle <b>Environment</b> (Visible: {GetLayerVisibilityStatus(SceneLayerType.Environment)})", style);
            GUILayout.Space(20);
            GUILayout.Label($"Press '<b>{toggleUIComponent}</b>' to toggle Canvas on UI objects", style);
            GUILayout.Label($"Press '<b>{highlightGameplay}</b>' to toggle green highlight on Gameplay objects", style);
            GUILayout.EndArea();
        }

        // Helper to get layer visibility status for OnGUI display.
        private string GetLayerVisibilityStatus(SceneLayerType layerType)
        {
            if (SceneLayerManager.Instance == null) return "N/A";
            return SceneLayerManager.Instance.IsLayerVisible(layerType) ? "<color=green>TRUE</color>" : "<color=red>FALSE</color>";
        }
    }
}
```