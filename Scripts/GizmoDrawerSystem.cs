// Unity Design Pattern Example: GizmoDrawerSystem
// This script demonstrates the GizmoDrawerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The **GizmoDrawerSystem** design pattern in Unity provides a centralized, decoupled, and efficient way to manage and draw editor Gizmos. Instead of scattering `OnDrawGizmos()` and `OnDrawGizmosSelected()` calls across many MonoBehaviour scripts, this pattern funnels all Gizmo drawing requests through a single manager.

This offers several advantages:
1.  **Decoupling:** Game logic components don't need to know about Gizmo drawing. They simply provide the data.
2.  **Organization:** All Gizmo drawing logic is contained within dedicated 'drawer' classes, separate from game logic.
3.  **Performance:** The system can control when and which Gizmos are drawn, potentially batching drawing calls or only drawing certain types of Gizmos based on editor settings. It avoids unnecessary `OnDrawGizmos` calls on inactive or unselected objects if not needed.
4.  **Flexibility:** Easily enable/disable specific Gizmo types or even the entire system from one place.
5.  **Reusability:** Gizmo drawer classes can be reused across different components or even projects.

This example demonstrates a complete GizmoDrawerSystem. It consists of:
1.  **`GizmoDrawerSystem` (MonoBehaviour Singleton):** The central hub that registers and unregisters `IGizmoDrawer` instances and iterates through them to call their drawing methods during Unity's `OnDrawGizmos` and `OnDrawGizmosSelected` events.
2.  **`IGizmoDrawer` (Interface):** Defines the contract for any class that wishes to draw Gizmos through the system. It includes methods for drawing when the target object is unselected (`DrawGizmos()`) and when it is selected (`DrawGizmosSelected()`).
3.  **Concrete `IGizmoDrawer` Implementations:** Example classes that implement `IGizmoDrawer` for specific game objects (e.g., `PlayerCharacter`, `Waypoint`, `TriggerArea`). These are typically nested classes within the components they serve or standalone classes.
4.  **Game Components:** Simple `MonoBehaviour` scripts (`PlayerCharacter`, `Waypoint`, `TriggerArea`) that create an instance of their respective `IGizmoDrawer` and register/unregister it with the `GizmoDrawerSystem`.

---

**How to Use This Script:**

1.  **Create a C# Script:** Create a new C# script in your Unity project, name it `GizmoDrawerSystem.cs`, and copy-paste the entire code below into it.
2.  **Create the System GameObject:** In your Unity scene, create an empty GameObject (e.g., named `_GizmoDrawerSystem_`). Add the `GizmoDrawerSystem` component to it. This GameObject should persist throughout your scene(s).
3.  **Add Example Components:**
    *   Create an empty GameObject, name it `Player`, and add the `PlayerCharacter` component to it.
    *   Create a few empty GameObjects, name them `Waypoint_A`, `Waypoint_B`, etc., and add the `Waypoint` component to each.
    *   Create an empty GameObject, name it `Trigger`, and add the `TriggerArea` component to it.
4.  **Observe Gizmos:**
    *   Ensure "Gizmos" are enabled in your Scene view (button in the top right of the Scene view).
    *   You will see different Gizmos for players, waypoints, and trigger areas.
    *   Select any of these GameObjects to see their "selected" Gizmos appear (e.g., Player's forward direction, TriggerArea's solid box).
    *   You can toggle the "System Enabled" checkbox on the `_GizmoDrawerSystem_` GameObject to globally enable/disable all Gizmos managed by the system.
    *   Each component (Player, Waypoint, Trigger) also has a `Gizmos Enabled` toggle and `Gizmo Color` field to customize its specific drawing.

---

```csharp
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR // UnityEditor namespace is only available in the editor
using UnityEditor;
#endif

/// <summary>
/// The central system responsible for managing and drawing all registered Gizmos.
/// This acts as a Singleton to be easily accessible from any part of the application.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensures this system initializes before other scripts try to register drawers.
public class GizmoDrawerSystem : MonoBehaviour
{
    // =============================================================================================================
    // GIZMODRAWERSYSTEM - CORE MANAGER
    // =============================================================================================================

    #region Singleton Setup
    public static GizmoDrawerSystem Instance { get; private set; }

    [Tooltip("Globally enable or disable all Gizmos managed by this system.")]
    [SerializeField] private bool m_SystemEnabled = true;

    /// <summary>
    /// Gets or sets whether the entire GizmoDrawerSystem is enabled.
    /// Setting this to false will prevent any registered Gizmos from being drawn.
    /// </summary>
    public bool SystemEnabled
    {
        get => m_SystemEnabled;
        set => m_SystemEnabled = value;
    }

    private readonly List<IGizmoDrawer> m_registeredDrawers = new List<IGizmoDrawer>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GizmoDrawerSystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("GizmoDrawerSystem initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        m_registeredDrawers.Clear(); // Clear references to prevent memory leaks.
    }
    #endregion

    #region Drawer Registration
    /// <summary>
    /// Registers an IGizmoDrawer with the system.
    /// The system will then be responsible for calling its DrawGizmos methods.
    /// </summary>
    /// <param name="drawer">The IGizmoDrawer instance to register.</param>
    public void RegisterDrawer(IGizmoDrawer drawer)
    {
        if (drawer == null)
        {
            Debug.LogWarning("Attempted to register a null GizmoDrawer.");
            return;
        }
        if (!m_registeredDrawers.Contains(drawer))
        {
            m_registeredDrawers.Add(drawer);
            // Debug.Log($"Registered GizmoDrawer for: {drawer.TargetObject?.name}");
        }
    }

    /// <summary>
    /// Unregisters an IGizmoDrawer from the system.
    /// This should be called when the associated game object or component is disabled or destroyed.
    /// </summary>
    /// <param name="drawer">The IGizmoDrawer instance to unregister.</param>
    public void UnregisterDrawer(IGizmoDrawer drawer)
    {
        if (drawer == null)
        {
            // Debug.LogWarning("Attempted to unregister a null GizmoDrawer. This can happen if a drawer is destroyed before its managing component.");
            return;
        }
        if (m_registeredDrawers.Remove(drawer))
        {
            // Debug.Log($"Unregistered GizmoDrawer for: {drawer.TargetObject?.name}");
        }
    }
    #endregion

    #region Gizmo Drawing Logic
    /// <summary>
    /// Unity's callback for drawing Gizmos when no object is selected, or for drawing unselected Gizmos
    /// of selected objects.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!m_SystemEnabled || m_registeredDrawers == null) return;

        // Iterate through all registered drawers.
        foreach (IGizmoDrawer drawer in m_registeredDrawers)
        {
            // Ensure the drawer itself is enabled and has a valid target object.
            if (drawer.Enabled && drawer.TargetObject != null)
            {
#if UNITY_EDITOR
                // In the Unity Editor, OnDrawGizmos is typically used for drawing
                // gizmos that appear even when the object is not selected.
                // If the object IS selected, its 'selected' gizmos often take precedence.
                // We prevent drawing 'general' gizmos if the object is currently selected,
                // delegating to OnDrawGizmosSelected for those.
                if (!Selection.Contains(drawer.TargetObject))
#endif
                {
                    drawer.DrawGizmos();
                }
            }
        }
    }

    /// <summary>
    /// Unity's callback for drawing Gizmos when the current GameObject (this system or any drawer's target)
    /// is selected in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!m_SystemEnabled || m_registeredDrawers == null) return;

        // Iterate through all registered drawers.
        foreach (IGizmoDrawer drawer in m_registeredDrawers)
        {
            // Ensure the drawer itself is enabled and has a valid target object.
            if (drawer.Enabled && drawer.TargetObject != null)
            {
#if UNITY_EDITOR
                // Only call DrawGizmosSelected if the target object is actually selected in the editor.
                if (Selection.Contains(drawer.TargetObject))
#endif
                {
                    drawer.DrawGizmosSelected();
                }
            }
        }
    }
    #endregion
}

/// <summary>
/// Interface defining the contract for any class that wants to draw Gizmos through the GizmoDrawerSystem.
/// </summary>
public interface IGizmoDrawer
{
    /// <summary>
    /// Gets the GameObject that this drawer is associated with.
    /// Used by the system for selection checks and context.
    /// </summary>
    GameObject TargetObject { get; }

    /// <summary>
    /// Gets or sets whether this specific GizmoDrawer is enabled.
    /// If false, this drawer will not draw any gizmos.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Implement this method to draw Gizmos when the TargetObject is NOT selected.
    /// Use Gizmos.Draw methods here for general visual cues.
    /// </summary>
    void DrawGizmos();

    /// <summary>
    /// Implement this method to draw Gizmos when the TargetObject IS selected.
    /// Use Gizmos.Draw methods or potentially Handles (within #if UNITY_EDITOR) here for detailed debugging.
    /// </summary>
    void DrawGizmosSelected();
}


// =============================================================================================================
// EXAMPLE USAGE: GAME COMPONENTS & THEIR GIZMO DRAWERS
// =============================================================================================================

/// <summary>
/// An example component representing a player character.
/// It uses the GizmoDrawerSystem to visualize its position and forward direction.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCharacter : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private bool m_drawGizmos = true;
    [SerializeField] private Color m_gizmoColor = Color.green;

    // The actual GizmoDrawer instance for this PlayerCharacter.
    private PlayerGizmoDrawer m_drawer;

    private void OnEnable()
    {
        // Create an instance of our specific drawer, passing 'this' (the PlayerCharacter component)
        // so the drawer has access to its data (transform, color, etc.).
        m_drawer = new PlayerGizmoDrawer(this);
        m_drawer.Enabled = m_drawGizmos; // Set initial state
        
        // Register the drawer with the central system.
        // This ensures the system will call our drawer's OnDrawGizmos methods.
        if (GizmoDrawerSystem.Instance != null)
        {
            GizmoDrawerSystem.Instance.RegisterDrawer(m_drawer);
        }
        else
        {
            Debug.LogError("GizmoDrawerSystem not found! Please add the GizmoDrawerSystem component to a GameObject in your scene.", this);
        }
    }

    private void OnDisable()
    {
        // Unregister the drawer when this component is disabled or destroyed.
        // This is crucial to prevent the system from trying to draw for invalid objects
        // and to allow garbage collection of the drawer.
        if (GizmoDrawerSystem.Instance != null && m_drawer != null)
        {
            GizmoDrawerSystem.Instance.UnregisterDrawer(m_drawer);
            m_drawer = null; // Clear reference
        }
    }

    // Update the drawer's enabled state if it changes in the editor.
    private void OnValidate()
    {
        if (m_drawer != null)
        {
            m_drawer.Enabled = m_drawGizmos;
        }
    }

    /// <summary>
    /// Internal class that implements IGizmoDrawer specifically for the PlayerCharacter.
    /// </summary>
    private class PlayerGizmoDrawer : IGizmoDrawer
    {
        private PlayerCharacter m_player; // Reference to the component this drawer serves.
        public GameObject TargetObject => m_player.gameObject;
        public bool Enabled { get; set; }

        public PlayerGizmoDrawer(PlayerCharacter player)
        {
            m_player = player;
        }

        public void DrawGizmos()
        {
            if (!Enabled || m_player == null) return;

            // Set Gizmo color for this drawing operation.
            Gizmos.color = m_player.m_gizmoColor * 0.7f; // Slightly dimmer when unselected.
            
            // Draw a sphere to represent the player's position.
            Gizmos.DrawSphere(m_player.transform.position + Vector3.up * 0.1f, 0.4f);
        }

        public void DrawGizmosSelected()
        {
            if (!Enabled || m_player == null) return;

            // Set Gizmo color for this drawing operation (brighter when selected).
            Gizmos.color = m_player.m_gizmoColor;

            // Draw a sphere for position.
            Gizmos.DrawSphere(m_player.transform.position + Vector3.up * 0.1f, 0.5f);

            // Draw an arrow indicating the player's forward direction.
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(m_player.transform.position, m_player.transform.forward * 1.5f);

#if UNITY_EDITOR
            // Example of using UnityEditor.Handles for more advanced drawing (only in editor).
            // Requires `using UnityEditor;` at the top of the file.
            // Handles.Label(m_player.transform.position + Vector3.up * 1f, m_player.gameObject.name);
#endif
        }
    }
}

/// <summary>
/// An example component representing a Waypoint.
/// It uses the GizmoDrawerSystem to visualize its position.
/// </summary>
[DisallowMultipleComponent]
public class Waypoint : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private bool m_drawGizmos = true;
    [SerializeField] private Color m_gizmoColor = Color.blue;
    [SerializeField] private float m_gizmoSize = 0.8f;

    // The actual GizmoDrawer instance for this Waypoint.
    private WaypointGizmoDrawer m_drawer;

    private void OnEnable()
    {
        m_drawer = new WaypointGizmoDrawer(this);
        m_drawer.Enabled = m_drawGizmos;
        if (GizmoDrawerSystem.Instance != null)
        {
            GizmoDrawerSystem.Instance.RegisterDrawer(m_drawer);
        }
        else
        {
            Debug.LogError("GizmoDrawerSystem not found! Please add the GizmoDrawerSystem component to a GameObject in your scene.", this);
        }
    }

    private void OnDisable()
    {
        if (GizmoDrawerSystem.Instance != null && m_drawer != null)
        {
            GizmoDrawerSystem.Instance.UnregisterDrawer(m_drawer);
            m_drawer = null;
        }
    }

    private void OnValidate()
    {
        if (m_drawer != null)
        {
            m_drawer.Enabled = m_drawGizmos;
        }
    }

    /// <summary>
    /// Internal class that implements IGizmoDrawer specifically for the Waypoint.
    /// </summary>
    private class WaypointGizmoDrawer : IGizmoDrawer
    {
        private Waypoint m_waypoint;
        public GameObject TargetObject => m_waypoint.gameObject;
        public bool Enabled { get; set; }

        public WaypointGizmoDrawer(Waypoint waypoint)
        {
            m_waypoint = waypoint;
        }

        public void DrawGizmos()
        {
            if (!Enabled || m_waypoint == null) return;

            Gizmos.color = m_waypoint.m_gizmoColor;
            Gizmos.DrawWireCube(m_waypoint.transform.position, Vector3.one * m_waypoint.m_gizmoSize);
        }

        public void DrawGizmosSelected()
        {
            if (!Enabled || m_waypoint == null) return;

            Gizmos.color = m_waypoint.m_gizmoColor;
            Gizmos.DrawCube(m_waypoint.transform.position, Vector3.one * m_waypoint.m_gizmoSize * 1.1f);

            // Optional: Draw text label (Editor-only)
#if UNITY_EDITOR
            // Handles.Label(m_waypoint.transform.position + Vector3.up * 0.7f, m_waypoint.gameObject.name);
#endif
        }
    }
}

/// <summary>
/// An example component representing a Trigger Area.
/// It uses the GizmoDrawerSystem to visualize its bounds.
/// </summary>
[DisallowMultipleComponent]
public class TriggerArea : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private bool m_drawGizmos = true;
    [SerializeField] private Color m_gizmoColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange with transparency
    [SerializeField] private Vector3 m_triggerSize = Vector3.one;

    public Vector3 Size => m_triggerSize; // Expose for drawer

    // The actual GizmoDrawer instance for this TriggerArea.
    private TriggerAreaGizmoDrawer m_drawer;

    private void OnEnable()
    {
        m_drawer = new TriggerAreaGizmoDrawer(this);
        m_drawer.Enabled = m_drawGizmos;
        if (GizmoDrawerSystem.Instance != null)
        {
            GizmoDrawerSystem.Instance.RegisterDrawer(m_drawer);
        }
        else
        {
            Debug.LogError("GizmoDrawerSystem not found! Please add the GizmoDrawerSystem component to a GameObject in your scene.", this);
        }
    }

    private void OnDisable()
    {
        if (GizmoDrawerSystem.Instance != null && m_drawer != null)
        {
            GizmoDrawerSystem.Instance.UnregisterDrawer(m_drawer);
            m_drawer = null;
        }
    }

    private void OnValidate()
    {
        if (m_drawer != null)
        {
            m_drawer.Enabled = m_drawGizmos;
        }
    }

    /// <summary>
    /// Internal class that implements IGizmoDrawer specifically for the TriggerArea.
    /// </summary>
    private class TriggerAreaGizmoDrawer : IGizmoDrawer
    {
        private TriggerArea m_triggerArea;
        public GameObject TargetObject => m_triggerArea.gameObject;
        public bool Enabled { get; set; }

        public TriggerAreaGizmoDrawer(TriggerArea triggerArea)
        {
            m_triggerArea = triggerArea;
        }

        public void DrawGizmos()
        {
            if (!Enabled || m_triggerArea == null) return;

            // Use Gizmos.matrix to draw in local space of the trigger area.
            // This is crucial if the trigger area object is rotated or scaled.
            Gizmos.matrix = Matrix4x4.TRS(
                m_triggerArea.transform.position,
                m_triggerArea.transform.rotation,
                m_triggerArea.transform.lossyScale
            );

            Gizmos.color = m_triggerArea.m_gizmoColor;
            Gizmos.DrawWireCube(Vector3.zero, m_triggerArea.m_triggerSize); // Draw at local origin

            // Reset matrix to avoid affecting other gizmo drawings.
            Gizmos.matrix = Matrix4x4.identity;
        }

        public void DrawGizmosSelected()
        {
            if (!Enabled || m_triggerArea == null) return;

            Gizmos.matrix = Matrix4x4.TRS(
                m_triggerArea.transform.position,
                m_triggerArea.transform.rotation,
                m_triggerArea.transform.lossyScale
            );

            // When selected, draw a solid (transparent) cube.
            Gizmos.color = m_triggerArea.m_gizmoColor;
            Gizmos.DrawCube(Vector3.zero, m_triggerArea.m_triggerSize);

            // Draw a wireframe on top for better visibility of edges.
            Gizmos.color = m_triggerArea.m_gizmoColor * 0.7f; // Slightly darker outline
            Gizmos.DrawWireCube(Vector3.zero, m_triggerArea.m_triggerSize);

            Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
            // Handles.Label(m_triggerArea.transform.position + Vector3.up * (m_triggerArea.m_triggerSize.y / 2f + 0.5f), "Trigger Area");
#endif
        }
    }
}
```