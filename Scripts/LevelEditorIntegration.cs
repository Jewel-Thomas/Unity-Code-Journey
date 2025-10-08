// Unity Design Pattern Example: LevelEditorIntegration
// This script demonstrates the LevelEditorIntegration pattern in Unity
// Generated automatically - ready to use in your Unity project

The **LevelEditorIntegration** design pattern in Unity focuses on creating components and systems that seamlessly blend editor-time configuration and visualization with runtime game logic. It allows level designers to intuitively place, configure, and visualize game elements directly within the Unity editor, with these configurations being automatically picked up and utilized by runtime systems without requiring manual setup or data export.

This pattern leverages Unity's editor scripting features like `[ExecuteInEditMode]`, `OnDrawGizmos()`, `OnValidate()`, and `[ContextMenu]` to provide a rich, interactive, and error-checking experience for designers, while ensuring the runtime code remains clean and efficient.

---

### Real-World Use Case: Configurable Spawn Points

We'll demonstrate this pattern with a common scenario: managing various types of spawn points in a level.

*   **Editor-Time:** Designers place `SpawnPoint` GameObjects in the scene. Each `SpawnPoint` can be configured with a `spawnGroup` (e.g., "Player", "Enemy", "Item"), a `spawnRadius`, and a visual `gizmoColor`. The `SpawnPoint` will draw visual gizmos in the editor to show its radius and group.
*   **Runtime:** A central `SpawnManager` script automatically finds all `SpawnPoint` instances in the scene on `Awake()`, organizes them by their `spawnGroup`, and provides methods for other game systems to request a random spawn position from a specific group.

---

### Complete C# Unity Example: LevelEditorIntegration

Here are two scripts: `SpawnPoint.cs` and `SpawnManager.cs`.

**1. `SpawnPoint.cs` - The Editor-Integrated Element**

This script represents an individual spawn point. It's designed to be placed directly in the scene by level designers.

```csharp
using UnityEngine;
using System.Collections.Generic;

// [ExecuteInEditMode]
// This attribute is crucial for the LevelEditorIntegration pattern.
// It allows this script's Update, OnEnable, OnDisable, OnDrawGizmos, and OnValidate
// methods to run both in Edit Mode and Play Mode.
// This enables real-time visualization and validation within the editor.
[ExecuteInEditMode]
public class SpawnPoint : MonoBehaviour
{
    // --- Editor-Configurable Properties ---
    // These fields are public or [SerializeField] to be editable in the Unity Inspector.
    [Tooltip("The logical group this spawn point belongs to (e.g., 'Player', 'EnemyA', 'Item').")]
    public string spawnGroup = "Default";

    [Tooltip("The radius around this point where spawning can occur.")]
    [Range(0.1f, 10f)] // Provides a slider in the Inspector for easy editing
    public float spawnRadius = 1.0f;

    [Tooltip("The color of the gizmo drawn in the editor.")]
    public Color gizmoColor = Color.green;

    // --- Runtime Properties (optional, for specific spawn logic) ---
    [Tooltip("Maximum number of entities that can use this specific spawn point concurrently.")]
    public int maxConcurrentSpawns = 1;
    private int currentSpawns = 0; // Runtime tracking

    // --- LevelEditorIntegration: Editor-Time Validation & Feedback ---

    // OnValidate() is called in the editor when a script is loaded or a value is changed in the Inspector.
    // This is perfect for enforcing constraints and providing immediate feedback to designers.
    void OnValidate()
    {
        // Ensure spawnGroup is not empty. If it is, default it.
        if (string.IsNullOrWhiteSpace(spawnGroup))
        {
            spawnGroup = "Default";
            // Debug.LogWarning("SpawnPoint on " + gameObject.name + " has an empty spawn group. Defaulted to 'Default'.", this);
        }

        // Clamp spawnRadius to ensure it's always positive.
        spawnRadius = Mathf.Max(0.1f, spawnRadius);

        // You could also update gizmoColor based on group here if desired.
        // E.g., if (spawnGroup == "Player") gizmoColor = Color.blue;
    }

    // OnDrawGizmos() is called in the editor for rendering gizmos.
    // This provides crucial visual feedback to designers without affecting runtime visuals.
    void OnDrawGizmos()
    {
        // Check if the game is not playing. This ensures gizmos are drawn only in the editor's scene view
        // and not during actual gameplay render, unless specifically desired for debugging runtime.
        // For LevelEditorIntegration, we primarily care about editor visualization.
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Set the gizmo color
            Gizmos.color = gizmoColor;

            // Draw a wire sphere to visualize the spawn radius
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // Draw a solid sphere at the center
            Gizmos.DrawSphere(transform.position, spawnRadius * 0.1f); // Smaller sphere for the exact center

            // Draw a label with the spawn group name
            GUIStyle style = new GUIStyle();
            style.normal.textColor = gizmoColor;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.HandleUtility.ApplyWireMaterial(0);
            UnityEditor.Handles.Label(transform.position + Vector3.up * (spawnRadius + 0.5f), spawnGroup, style);

            // Draw an arrow indicating forward direction (optional, but useful)
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * spawnRadius * 0.7f);
            Gizmos.DrawSphere(transform.position + transform.forward * spawnRadius * 0.7f, spawnRadius * 0.05f);
        }
        #endif
    }

    // --- Editor-Specific Context Menu Items ---

    // [ContextMenu] allows adding custom menu items to the component's context menu
    // in the Inspector. This is great for editor-time actions or debugging.
    [ContextMenu("Debug: Get Random Spawn Position")]
    private void DebugGetRandomSpawnPosition()
    {
        Vector3 randomPos = GetRandomSpawnPosition();
        Debug.Log($"Debug Spawn Position for '{spawnGroup}' at {gameObject.name}: {randomPos}", this);
        // You could also draw a temporary gizmo here in editor via a Gizmos.DrawSphere in OnDrawGizmos
        // and set a flag in this method.
    }

    // --- Runtime Methods ---

    // Returns a random position within this spawn point's radius.
    public Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    // Can be used to check if this spawn point is available for use.
    public bool IsAvailableForSpawn()
    {
        return currentSpawns < maxConcurrentSpawns;
    }

    // Call this when an entity spawns here.
    public void IncrementCurrentSpawns()
    {
        currentSpawns++;
    }

    // Call this when an entity despawns or leaves this point.
    public void DecrementCurrentSpawns()
    {
        currentSpawns = Mathf.Max(0, currentSpawns - 1);
    }
}
```

**2. `SpawnManager.cs` - The Runtime System**

This script acts as the central orchestrator for all `SpawnPoint` objects in the scene. It discovers them at runtime and provides an interface for other game systems.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For Linq extensions like ToList()

public class SpawnManager : MonoBehaviour
{
    // A dictionary to store SpawnPoints, organized by their 'spawnGroup'.
    // This allows efficient lookup of spawn points for a specific type of entity.
    private Dictionary<string, List<SpawnPoint>> spawnPointsByGroup;

    [Tooltip("If true, the manager will automatically find and populate spawn points on Awake.")]
    [SerializeField] private bool autoPopulateOnAwake = true;

    [Tooltip("If true, gizmos will be drawn in editor to show found spawn points.")]
    [SerializeField] private bool drawDebugGizmosInEditor = true;

    // --- Runtime Initialization ---

    void Awake()
    {
        spawnPointsByGroup = new Dictionary<string, List<SpawnPoint>>();

        if (autoPopulateOnAwake)
        {
            PopulateSpawnPoints();
        }
    }

    // --- LevelEditorIntegration: Editor-Triggered Actions ---

    // [ContextMenu] allows a designer to manually trigger the population process
    // from the Inspector, e.g., after adding new spawn points.
    [ContextMenu("Editor: Populate Spawn Points")]
    public void PopulateSpawnPoints()
    {
        Debug.Log("Populating Spawn Points...", this);
        spawnPointsByGroup.Clear(); // Clear existing data to re-populate

        // Find all SpawnPoint components in the current scene.
        // This is the core of the integration: the manager *discovers* what the designer placed.
        SpawnPoint[] allSpawnPoints = FindObjectsOfType<SpawnPoint>();

        if (allSpawnPoints.Length == 0)
        {
            Debug.LogWarning("No SpawnPoint objects found in the scene.", this);
            return;
        }

        foreach (SpawnPoint sp in allSpawnPoints)
        {
            // Group spawn points by their designated 'spawnGroup'.
            if (!spawnPointsByGroup.ContainsKey(sp.spawnGroup))
            {
                spawnPointsByGroup.Add(sp.spawnGroup, new List<SpawnPoint>());
            }
            spawnPointsByGroup[sp.spawnGroup].Add(sp);
        }

        Debug.Log($"Found and organized {allSpawnPoints.Length} spawn points across {spawnPointsByGroup.Count} groups.");
        foreach (var group in spawnPointsByGroup)
        {
            Debug.Log($"- Group '{group.Key}': {group.Value.Count} spawn points.");
        }
    }

    // --- Runtime Query Methods ---

    // Returns a random SpawnPoint object from a specified group.
    public SpawnPoint GetRandomSpawnPoint(string groupName)
    {
        if (spawnPointsByGroup.ContainsKey(groupName))
        {
            List<SpawnPoint> groupPoints = spawnPointsByGroup[groupName];
            if (groupPoints.Count > 0)
            {
                // Optionally filter for available spawn points
                List<SpawnPoint> availablePoints = groupPoints.Where(sp => sp.IsAvailableForSpawn()).ToList();
                if (availablePoints.Count > 0)
                {
                    return availablePoints[Random.Range(0, availablePoints.Count)];
                }
                Debug.LogWarning($"No available spawn points in group '{groupName}'. All currently occupied?", this);
                return null; // All points occupied
            }
        }
        Debug.LogWarning($"Spawn group '{groupName}' not found or has no spawn points.", this);
        return null;
    }

    // Returns a random spawn position from a specified group.
    public Vector3? GetRandomSpawnPosition(string groupName)
    {
        SpawnPoint sp = GetRandomSpawnPoint(groupName);
        if (sp != null)
        {
            sp.IncrementCurrentSpawns(); // Mark this point as used
            return sp.GetRandomSpawnPosition();
        }
        return null; // Return nullable Vector3 if no spawn point found
    }

    // --- Editor-Time Debugging Gizmos (Optional but helpful for integration) ---

    void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        if (drawDebugGizmosInEditor && spawnPointsByGroup != null)
        {
            Gizmos.color = Color.yellow;
            // Draw a sphere to indicate the manager's presence
            Gizmos.DrawWireSphere(transform.position, 1f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "Spawn Manager");

            // You could also draw lines from the manager to each found spawn point
            // This is especially useful for visualizing connections or ranges.
            foreach (var group in spawnPointsByGroup)
            {
                foreach (SpawnPoint sp in group.Value)
                {
                    if (sp != null)
                    {
                        Gizmos.color = group.Key == "Player" ? Color.blue : Color.red; // Example: different color for player group
                        Gizmos.DrawLine(transform.position, sp.transform.position);
                    }
                }
            }
        }
        #endif
    }
}
```

---

### How to Use These Scripts in Unity

1.  **Create the Scripts:**
    *   Create a new C# script named `SpawnPoint` and copy the first code block into it.
    *   Create another C# script named `SpawnManager` and copy the second code block into it.

2.  **Setup the `SpawnManager`:**
    *   Create an empty GameObject in your scene (e.g., named `_GameManager` or `SpawnSystem`).
    *   Attach the `SpawnManager` script to this GameObject.
    *   In the Inspector for `SpawnManager`, ensure `Auto Populate On Awake` is checked if you want it to find spawn points automatically when the game starts.

3.  **Setup `SpawnPoints`:**
    *   Create several empty GameObjects in your scene (e.g., `PlayerSpawn1`, `EnemySpawnZoneA`, `ItemDropPoint`).
    *   Attach the `SpawnPoint` script to each of these GameObjects.
    *   **Crucially:** Select each `SpawnPoint` GameObject. In its Inspector, you'll see the properties:
        *   **Spawn Group:** Enter a unique name like "Player", "EnemyA", "Item".
        *   **Spawn Radius:** Adjust the radius to define the area.
        *   **Gizmo Color:** Pick a color for easy identification in the editor.
    *   **Observe the Editor Integration:** As you adjust `spawnRadius` or `gizmoColor`, you will immediately see the sphere gizmo in the Scene view update. This live feedback is a core aspect of LevelEditorIntegration.
    *   **Context Menu:** Right-click on the `SpawnPoint` component in the Inspector. You'll see "Debug: Get Random Spawn Position". Click it to see the debug output.

4.  **Example Runtime Usage (in another script):**

    ```csharp
    using UnityEngine;

    public class GameManager : MonoBehaviour
    {
        public SpawnManager spawnManager;
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject itemPrefab;

        void Start()
        {
            if (spawnManager == null)
            {
                spawnManager = FindObjectOfType<SpawnManager>();
                if (spawnManager == null)
                {
                    Debug.LogError("No SpawnManager found in the scene!");
                    return;
                }
            }

            // Spawn Player
            Vector3? playerSpawnPos = spawnManager.GetRandomSpawnPosition("Player");
            if (playerSpawnPos.HasValue)
            {
                Instantiate(playerPrefab, playerSpawnPos.Value, Quaternion.identity);
                Debug.Log("Player spawned!");
            }
            else
            {
                Debug.LogError("Failed to get Player spawn position.");
            }

            // Spawn Enemies
            for (int i = 0; i < 3; i++)
            {
                Vector3? enemySpawnPos = spawnManager.GetRandomSpawnPosition("EnemyA");
                if (enemySpawnPos.HasValue)
                {
                    Instantiate(enemyPrefab, enemySpawnPos.Value, Quaternion.identity);
                    Debug.Log($"Enemy {i+1} spawned!");
                }
            }

            // Spawn an Item
            Vector3? itemSpawnPos = spawnManager.GetRandomSpawnPosition("Item");
            if (itemSpawnPos.HasValue)
            {
                Instantiate(itemPrefab, itemSpawnPos.Value, Quaternion.identity);
                Debug.Log("Item spawned!");
            }
        }
    }
    ```
    *   Attach this `GameManager` script to another GameObject (e.g., the `_GameManager` where `SpawnManager` is).
    *   Drag your `playerPrefab`, `enemyPrefab`, and `itemPrefab` (simple cubes will do) into the `GameManager`'s Inspector fields.
    *   Press Play. You will see objects spawn at the locations you defined with the `SpawnPoint` GameObjects in the editor!

---

### Explanation of the LevelEditorIntegration Pattern

1.  **`[ExecuteInEditMode]` on `SpawnPoint`:**
    *   This attribute is fundamental. It tells Unity to run `SpawnPoint`'s lifecycle methods (like `OnValidate`, `OnDrawGizmos`, `Update`) even when the editor is not in Play mode.
    *   **Benefit:** Allows designers to see changes and get feedback *instantly* as they configure the `SpawnPoint` in the Inspector or move its GameObject in the Scene view.

2.  **`OnValidate()` for Editor-Time Validation:**
    *   Called whenever the script is loaded or a value is changed in the Inspector.
    *   **Benefit:** Enables enforcing data integrity (e.g., `spawnRadius` must be positive), providing immediate warnings, or updating other properties based on input, all before runtime.

3.  **`OnDrawGizmos()` for Editor-Time Visualization:**
    *   Called to draw editor-only graphical helpers.
    *   **Benefit:** Visually represents the properties and behavior of the `SpawnPoint` (e.g., the spawn radius, the group label) directly in the Scene view. This drastically improves the level designer's workflow by making the "invisible" game logic "visible." We use `#if UNITY_EDITOR` to ensure this code is stripped out of builds, preventing runtime overhead.

4.  **`[ContextMenu]` for Editor-Time Actions:**
    *   Adds custom commands to the component's context menu in the Inspector.
    *   **Benefit:** Provides quick access to editor-specific debugging or utility functions directly on the component itself (e.g., "Debug: Get Random Spawn Position" on `SpawnPoint`, or "Editor: Populate Spawn Points" on `SpawnManager`).

5.  **Runtime System (`SpawnManager`) Discovering Editor-Configured Data:**
    *   The `SpawnManager` uses `FindObjectsOfType<SpawnPoint>()` at runtime (`Awake()` or a manual trigger) to automatically collect all `SpawnPoint` components that the designer has placed and configured in the scene.
    *   **Benefit:** Decouples level design from runtime logic. Designers just place and configure objects; the manager finds them. No need to manually drag-and-drop each `SpawnPoint` into a list on the `SpawnManager`. This scales very well for large levels.

6.  **Decoupling and Flexibility:**
    *   `SpawnPoint` components don't need to know about `SpawnManager`. They are self-contained.
    *   `SpawnManager` doesn't need to know *how* `SpawnPoint`s were created, only that they exist and have certain properties.
    *   This separation makes the system robust and easy to extend. You can add new `SpawnPoint` types or new ways to manage spawns without breaking existing code.

This example clearly demonstrates how the LevelEditorIntegration pattern streamlines level design by bringing game logic visualization and interaction directly into the Unity editor, making the level creation process more intuitive, less error-prone, and highly efficient.