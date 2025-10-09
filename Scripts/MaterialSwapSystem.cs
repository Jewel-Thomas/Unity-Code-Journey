// Unity Design Pattern Example: MaterialSwapSystem
// This script demonstrates the MaterialSwapSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the **Material Swap System** design pattern. It centralizes material management, allowing you to define various material "states" for a GameObject and its children, and switch between them programmatically. This is highly beneficial for scenarios like highlighting, damage effects, visual feedback, or player customization.

---

### MaterialSwapSystem.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .Contains, .Any, .GroupBy

/// <summary>
/// Implements the Material Swap System design pattern in Unity.
/// This system allows you to define different sets of materials (states) for a GameObject
/// and its children, and switch between these states programmatically.
/// </summary>
/// <remarks>
/// The Material Swap System centralizes material management, making it easy to change the visual
/// appearance of complex objects without directly manipulating individual renderers.
/// This is particularly useful for highlighting objects on hover, showing damage states,
/// providing selection feedback, or enabling visual customization by the player.
///
/// <para>
/// **How the Material Swap System pattern works:**
/// 1.  **Renderer Discovery:** On `Awake`, the system automatically finds all `MeshRenderer`
///     and `SkinnedMeshRenderer` components on the GameObject this script is attached to,
///     and its active children. These are the "managed renderers."
/// 2.  **Original State Preservation:** It stores the original `sharedMaterials` of all
///     managed renderers. This allows the system to revert the object's appearance to its
///     initial state at any time.
/// 3.  **Defined States:** You define various `MaterialState` objects directly in the Unity
///     Inspector. Each `MaterialState` has a unique `stateName` (e.g., "Normal", "Highlighted",
///     "Damaged").
/// 4.  **Explicit Assignments:** Within each `MaterialState`, you create `RendererMaterialAssignment`
///     entries. Each assignment explicitly links a *specific target renderer* (one of the
///     managed renderers) with a `Material[]` array. This array defines the materials that
///     will be applied to that renderer's sub-meshes when this state is active. This explicit
///     mapping provides robust control, even for complex models with multiple renderers or
///     multiple sub-meshes per renderer.
/// 5.  **State Switching:** The public method `SwitchToState(string stateName)` is the core
///     API. When called, the system looks up the specified `MaterialState` and iterates
///     through its `RendererMaterialAssignment`s, applying the defined materials to their
///     respective renderers using `renderer.sharedMaterials`.
/// 6.  **Reverting:** The `RevertToOriginal()` method restores all managed renderers to
///     the materials they had when the `Awake` method was first called.
/// </para>
///
/// <para>
/// **Key Benefits:**
/// -   **Centralized Control:** All material states are managed from a single script, improving
///     organization and reducing scattershot material assignments.
/// -   **Decoupling:** Game logic doesn't need to know about individual renderers or their
///     sub-mesh structure; it just requests a state change by name.
/// -   **Flexibility:** Easily add new visual states without modifying existing code.
/// -   **Maintainability:** Changes to material assets or renderer structure can be updated
///     in the Inspector without code changes (as long as `Renderer` references remain valid).
/// -   **Performance:** By using `sharedMaterials`, the system avoids creating new material
///     instances on every state change, which can be a significant performance gain.
/// </para>
/// </remarks>
[DisallowMultipleComponent] // Prevents adding multiple instances of this script to the same GameObject.
public class MaterialSwapSystem : MonoBehaviour
{
    // --- Internal Serializable Classes for Inspector Configuration ---

    /// <summary>
    /// Represents a single assignment of materials to a specific renderer for a given state.
    /// This allows granular control over which materials go to which sub-mesh of which renderer.
    /// </summary>
    [System.Serializable]
    public class RendererMaterialAssignment
    {
        [Tooltip("The MeshRenderer or SkinnedMeshRenderer this assignment targets. Must be a child or on this GameObject.")]
        public Renderer targetRenderer;

        [Tooltip("The materials to apply to the targetRenderer. The array's length must match the targetRenderer's sub-mesh count (i.e., its current sharedMaterials.Length).")]
        public Material[] materials;
    }

    /// <summary>
    /// Defines a named collection of material assignments that constitute a visual state.
    /// </summary>
    [System.Serializable]
    public class MaterialState
    {
        [Tooltip("A unique name for this material state (e.g., 'Normal', 'Highlighted', 'Damaged', 'Selected').")]
        public string stateName;

        [Tooltip("List of renderers and their corresponding materials for this state. Each entry specifies a renderer and the materials for its sub-meshes.")]
        public List<RendererMaterialAssignment> assignments = new List<RendererMaterialAssignment>();
    }

    // --- Public Inspector Fields ---

    [Header("Material States Configuration")]
    [Tooltip("Define different material states for this object. Each state is a collection of material assignments for specific renderers.")]
    [SerializeField]
    private List<MaterialState> materialStates = new List<MaterialState>();

    [Tooltip("The initial state to apply when the system starts. Leave empty if no default state should be applied (e.g., if another script sets the initial state).")]
    [SerializeField]
    private string defaultStateName = "Normal";

    // --- Private Internal State ---

    // A cached array of all MeshRenderer and SkinnedMeshRenderer components found on this GameObject and its children.
    private Renderer[] _allManagedRenderers;

    // A dictionary to store the original shared materials for each managed renderer, keyed by the renderer itself.
    // This allows reverting to the object's initial appearance.
    private Dictionary<Renderer, Material[]> _originalSharedMaterialsMap = new Dictionary<Renderer, Material[]>();

    // A dictionary to store the processed material states, keyed by state name.
    // Each value is another dictionary mapping a renderer to the materials it should use for that state.
    private Dictionary<string, Dictionary<Renderer, Material[]>> _stateMaterialMaps = new Dictionary<string, Dictionary<Renderer, Material[]>>();

    // The name of the currently active material state.
    private string _currentStateName = "Original";

    /// <summary>
    /// Gets the name of the currently active material state. Returns "Original" if reverted, or the last set state name.
    /// </summary>
    public string CurrentStateName => _currentStateName;

    // A flag to ensure the system initialization only happens once.
    private bool _isInitialized = false;

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Initialize the system when the GameObject wakes up.
        InitializeSystem();

        // If initialization was successful and a default state is specified, switch to it.
        if (_isInitialized && !string.IsNullOrEmpty(defaultStateName))
        {
            SwitchToState(defaultStateName);
        }
    }

    // --- Public API Methods ---

    /// <summary>
    /// Switches the materials of all managed renderers to a predefined state.
    /// Materials are applied using `renderer.sharedMaterials` for performance and consistency
    /// across prefab instances using the same material assets.
    /// </summary>
    /// <param name="stateName">The unique name of the material state to switch to (e.g., "Highlighted", "Normal").</param>
    public void SwitchToState(string stateName)
    {
        // Ensure the system is initialized before attempting to switch states.
        if (!_isInitialized)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' is not initialized. Attempting to initialize now.", this);
            InitializeSystem(); // Try to initialize if not already
            if (!_isInitialized) return; // If initialization still fails, exit
        }

        // Check if the component is enabled.
        if (!enabled)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' is disabled. Cannot switch state to '{stateName}'.", this);
            return;
        }

        // Attempt to retrieve the material assignments for the requested state.
        if (!_stateMaterialMaps.TryGetValue(stateName, out Dictionary<Renderer, Material[]> targetStateAssignments))
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' does not contain a state named '{stateName}'. No material change applied.", this);
            return;
        }

        // Apply materials only for renderers that have an explicit assignment in this state.
        // Renderers not included in this state's assignments will retain their current materials.
        foreach (var assignment in targetStateAssignments)
        {
            Renderer targetRenderer = assignment.Key;
            Material[] materialsToApply = assignment.Value;

            // Ensure the renderer still exists (it might have been destroyed runtime).
            if (targetRenderer != null)
            {
                // Assign the new materials.
                // Using .sharedMaterials modifies the material asset directly, which is generally
                // desired for a 'swap' pattern as it's performant and ensures consistency
                // across all objects using that material asset.
                //
                // Alternative: If you need to modify material properties uniquely per object
                // at runtime (e.g., setting a unique color for one specific instance without
                // affecting others), you would use `targetRenderer.materials = materialsToApply;`.
                // Be aware that this creates a *new instance* of the materials array (and potentially
                // new material instances themselves) which can have performance implications
                // and might require careful memory management. For this 'swap' pattern,
                // `sharedMaterials` is usually the appropriate choice.
                targetRenderer.sharedMaterials = materialsToApply;
            }
        }
        _currentStateName = stateName;
        // Debug.Log($"MaterialSwapSystem on '{gameObject.name}' switched to state: '{stateName}'", this);
    }

    /// <summary>
    /// Reverts all managed renderers back to their original materials recorded at `Awake`.
    /// </summary>
    public void RevertToOriginal()
    {
        // Ensure the system is initialized before attempting to revert.
        if (!_isInitialized)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' is not initialized. Attempting to initialize now.", this);
            InitializeSystem();
            if (!_isInitialized) return;
        }

        // Check if the component is enabled.
        if (!enabled)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' is disabled. Cannot revert to original materials.", this);
            return;
        }

        // Iterate through the map of original materials and reapply them to their respective renderers.
        foreach (var entry in _originalSharedMaterialsMap)
        {
            Renderer targetRenderer = entry.Key;
            Material[] originalMaterials = entry.Value;

            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterials = originalMaterials;
            }
        }
        _currentStateName = "Original"; // Indicate that the object is back to its original state.
        // Debug.Log($"MaterialSwapSystem on '{gameObject.name}' reverted to original materials.", this);
    }

    /// <summary>
    /// Checks if a material state with the given name exists within the system's configuration.
    /// </summary>
    /// <param name="stateName">The name of the state to check for.</param>
    /// <returns>True if a state with the specified name exists, false otherwise.</returns>
    public bool HasState(string stateName)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' is not initialized. Calling InitializeSystem() first.", this);
            InitializeSystem();
            if (!_isInitialized) return false;
        }
        return _stateMaterialMaps.ContainsKey(stateName);
    }

    // --- Private Initialization Logic ---

    /// <summary>
    /// Initializes the MaterialSwapSystem by finding renderers, storing original materials,
    /// and processing the user-defined material states. This method is called once during Awake.
    /// </summary>
    private void InitializeSystem()
    {
        if (_isInitialized) return; // Prevent re-initialization

        // 1. Discover all relevant renderers (MeshRenderer and SkinnedMeshRenderer)
        //    on this GameObject and its children. 'true' includes inactive children,
        //    allowing configuration of inactive parts of a prefab.
        _allManagedRenderers = GetComponentsInChildren<Renderer>(true);

        if (_allManagedRenderers == null || _allManagedRenderers.Length == 0)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' found no MeshRenderer or SkinnedMeshRenderer components. Disabling system as it has nothing to manage.", this);
            enabled = false; // Disable the component if there's nothing to manage.
            return;
        }

        // 2. Store the original shared materials for each found renderer.
        //    This allows reverting to the initial visual state.
        foreach (var renderer in _allManagedRenderers)
        {
            if (renderer == null) continue; // Safety check if a renderer was somehow null.
            _originalSharedMaterialsMap.Add(renderer, renderer.sharedMaterials);
        }

        // 3. Process and store the user-defined material states from the Inspector.
        foreach (var state in materialStates)
        {
            // Validate the state name.
            if (string.IsNullOrWhiteSpace(state.stateName))
            {
                Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' has a MaterialState with an empty or whitespace name. This state will be skipped.", this);
                continue;
            }

            // Check for duplicate state names.
            if (_stateMaterialMaps.ContainsKey(state.stateName))
            {
                Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' has duplicate Material State name: '{state.stateName}'. Only the first definition will be used.", this);
                continue;
            }

            // Create a temporary map for this specific state to store its validated renderer-material assignments.
            var currentStateAssignments = new Dictionary<Renderer, Material[]>();
            bool stateContainsErrors = false;

            // Process each renderer material assignment defined within this state.
            foreach (var assignment in state.assignments)
            {
                // Validate that a target renderer is assigned.
                if (assignment.targetRenderer == null)
                {
                    Debug.LogError($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}': An assignment has an unassigned 'Target Renderer'. Please assign a renderer. This state will be marked invalid.", this);
                    stateContainsErrors = true;
                    break; // Stop processing this state if a critical error is found.
                }

                // Validate that the target renderer is one of the renderers managed by this system.
                // (i.e., it's a child or on the same GameObject).
                if (!_allManagedRenderers.Contains(assignment.targetRenderer))
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' targets renderer '{assignment.targetRenderer.name}' which is not part of this system's managed renderers. This assignment will be ignored.", this);
                    continue; // Skip this specific assignment but continue processing others in the state.
                }

                // Validate that materials are provided for the assignment.
                if (assignment.materials == null || assignment.materials.Length == 0)
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' assignment for '{assignment.targetRenderer.name}' has no materials defined. This assignment will be ignored.", this);
                    continue; // Skip this specific assignment.
                }

                // Validate that the number of provided materials matches the number of sub-meshes (sharedMaterials.Length)
                // of the target renderer. A mismatch here is a common error and can lead to incorrect rendering.
                if (assignment.targetRenderer.sharedMaterials.Length != assignment.materials.Length)
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' assignment for '{assignment.targetRenderer.name}': Material count mismatch! Expected {assignment.targetRenderer.sharedMaterials.Length} materials (for its sub-meshes), but {assignment.materials.Length} were provided. This assignment will be ignored.", this);
                    continue; // Skip this specific assignment.
                }

                // Check for duplicate assignments to the same renderer within a single state.
                if (currentStateAssignments.ContainsKey(assignment.targetRenderer))
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' has multiple assignments for renderer '{assignment.targetRenderer.name}'. Only the first one defined will be used.", this);
                    continue; // Skip duplicate assignments.
                }

                // If all validations pass, add the assignment to the current state's map.
                currentStateAssignments.Add(assignment.targetRenderer, assignment.materials);
            }

            // Only add the state to the main map if it contains no critical errors.
            if (!stateContainsErrors)
            {
                _stateMaterialMaps.Add(state.stateName, currentStateAssignments);
            }
            else
            {
                Debug.LogError($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' contains errors and will not be available for use.", this);
            }
        }
        _isInitialized = true; // Mark initialization as complete.
    }

    // --- Editor-time Functionality for Better User Experience ---

    /// <summary>
    /// `OnValidate` is called in the editor when the script is loaded or a value is changed
    /// in the Inspector. This is used to provide immediate feedback to the developer regarding
    /// potential configuration issues without needing to run the game.
    /// </summary>
    void OnValidate()
    {
        // Warn if the default state name is set but doesn't exist.
        if (!string.IsNullOrEmpty(defaultStateName) && materialStates != null)
        {
            bool defaultStateExists = materialStates.Any(s => s.stateName == defaultStateName);
            if (!defaultStateExists)
            {
                Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}': The 'Default State Name' '{defaultStateName}' does not match any of the defined Material States. Please correct this in the Inspector.", this);
            }
        }

        // Warn about duplicate state names, as only the first one will be used at runtime.
        var duplicateStates = materialStates
            .Where(s => !string.IsNullOrWhiteSpace(s.stateName)) // Only consider non-empty names.
            .GroupBy(s => s.stateName) // Group states by their name.
            .Where(g => g.Count() > 1) // Find groups with more than one entry (duplicates).
            .Select(g => g.Key) // Select the name of the duplicate states.
            .ToList();

        foreach (var duplicateName in duplicateStates)
        {
            Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}': Duplicate Material State name found: '{duplicateName}'. Only the first definition will be used at runtime.", this);
        }

        // Validate individual RendererMaterialAssignments within each state.
        // This provides real-time feedback as the user configures the states.
        foreach (var state in materialStates)
        {
            if (string.IsNullOrWhiteSpace(state.stateName)) continue; // Skip states with no name.

            var assignedRenderersInState = new HashSet<Renderer>(); // Track renderers assigned in THIS state.
            foreach (var assignment in state.assignments)
            {
                if (assignment.targetRenderer == null)
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}': An assignment has an unassigned 'Target Renderer'. Please drag a Renderer component here.", this);
                    continue;
                }

                // Check for duplicate assignments to the same renderer within a single state.
                if (!assignedRenderersInState.Add(assignment.targetRenderer))
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}': Multiple assignments for renderer '{assignment.targetRenderer.name}'. Only the first one will be used at runtime.", this);
                }

                // Check for material count mismatch between the target renderer's sub-meshes and provided materials.
                // This is a common setup error and crucial to catch early.
                if (assignment.materials != null && assignment.targetRenderer.sharedMaterials.Length != assignment.materials.Length)
                {
                    Debug.LogWarning($"MaterialSwapSystem on '{gameObject.name}' - State '{state.stateName}' for renderer '{assignment.targetRenderer.name}': Material count mismatch! Expected {assignment.targetRenderer.sharedMaterials.Length} materials (for its sub-meshes), but {assignment.materials.Length} were provided. Please ensure the material array size matches the renderer's sub-mesh count. This assignment will be ignored or partially applied at runtime.", this);
                }
            }
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create the Script:**
    *   Save the code above as `MaterialSwapSystem.cs` in your Unity project's Assets folder.

2.  **Attach to a GameObject:**
    *   Create a new empty GameObject (e.g., "MySelectableObject") or select an existing one that has `MeshRenderer` or `SkinnedMeshRenderer` components (either directly on it or on its children).
    *   Drag and drop the `MaterialSwapSystem.cs` script onto this GameObject.

3.  **Configure in the Inspector:**
    *   **Default State Name:** Enter the name of the state you want to apply when the game starts (e.g., "Normal"). Leave empty if you don't want a default.
    *   **Material States:**
        *   Expand the `Material States` list.
        *   Click `+` to add a new `Material State`.
        *   **State Name:** Give it a unique name (e.g., "Normal", "Highlighted", "Damaged").
        *   **Assignments:** Expand the `Assignments` list within your new state.
            *   Click `+` to add a `Renderer Material Assignment`.
            *   **Target Renderer:** Drag and drop a `MeshRenderer` or `SkinnedMeshRenderer` component from the current GameObject or one of its children into this slot.
            *   **Materials:** Expand the `Materials` array. The size of this array **must match** the `Target Renderer`'s `sharedMaterials.Length` (i.e., the number of sub-meshes it has). Drag and drop your desired `Material` assets into these slots.
        *   Repeat this for all renderers you want to control within this state, and for all other desired states (e.g., create a "Highlighted" state with glowing materials).

**Example Configuration Screenshot (Mental Model):**

```
MySelectableObject (GameObject)
  - MaterialSwapSystem (Script)
    [Header] Material States Configuration
    Default State Name: Normal
    Material States:
      - Size: 2
      - Element 0:
          State Name: Normal
          Assignments:
            - Size: 1
            - Element 0:
                Target Renderer: MySelectableObject (Mesh Renderer)
                Materials:
                  - Size: 1
                  - Element 0: DefaultMaterial
      - Element 1:
          State Name: Highlighted
          Assignments:
            - Size: 1
            - Element 0:
                Target Renderer: MySelectableObject (Mesh Renderer)
                Materials:
                  - Size: 1
                  - Element 0: HighlightedMaterial
```

4.  **Calling from another script:**

    ```csharp
    using UnityEngine;

    public class MyInteractionScript : MonoBehaviour
    {
        [Tooltip("Reference to the MaterialSwapSystem on the object to interact with.")]
        [SerializeField] private MaterialSwapSystem targetMaterialSwapSystem;

        void Start()
        {
            if (targetMaterialSwapSystem == null)
            {
                Debug.LogError("MyInteractionScript: targetMaterialSwapSystem is not assigned!", this);
                enabled = false;
            }
        }

        // Example: Call this when the mouse enters the object's collider
        void OnMouseEnter()
        {
            if (targetMaterialSwapSystem != null && targetMaterialSwapSystem.HasState("Highlighted"))
            {
                targetMaterialSwapSystem.SwitchToState("Highlighted");
                Debug.Log("Object highlighted!");
            }
        }

        // Example: Call this when the mouse exits the object's collider
        void OnMouseExit()
        {
            if (targetMaterialSwapSystem != null)
            {
                targetMaterialSwapSystem.RevertToOriginal(); // Revert to what it was before being highlighted
                // Or switch to a specific "Normal" state:
                // targetMaterialSwapSystem.SwitchToState("Normal");
                Debug.Log("Object unhighlighted!");
            }
        }

        // Example: Call this when the object is selected
        public void SelectObject()
        {
            if (targetMaterialSwapSystem != null && targetMaterialSwapSystem.HasState("Selected"))
            {
                targetMaterialSwapSystem.SwitchToState("Selected");
                Debug.Log("Object selected!");
            }
        }

        // Example: Call this to simulate damage
        public void ApplyDamageVisual()
        {
            if (targetMaterialSwapSystem != null && targetMaterialSwapSystem.HasState("Damaged"))
            {
                targetMaterialSwapSystem.SwitchToState("Damaged");
                Debug.Log("Object damaged!");
            }
        }

        // Example: Call this to reset to default/normal appearance
        public void ResetAppearance()
        {
            if (targetMaterialSwapSystem != null && targetMaterialSwapSystem.HasState("Normal"))
            {
                targetMaterialSwapSystem.SwitchToState("Normal");
                Debug.Log("Object appearance reset to normal!");
            }
        }
    }
    ```

    *   Create `MyInteractionScript.cs`, attach it to another GameObject (e.g., your Player or a UI Manager), and drag "MySelectableObject" into its `Target Material Swap System` slot in the Inspector.
    *   Ensure "MySelectableObject" has a `Collider` component for `OnMouseEnter`/`OnMouseExit` events to work.

This `MaterialSwapSystem` provides a robust, flexible, and educational example of managing visual states in Unity, adhering to common design pattern principles and best practices.