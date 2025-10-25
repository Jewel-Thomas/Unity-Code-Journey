// Unity Design Pattern Example: TrapPlacementSystem
// This script demonstrates the TrapPlacementSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `TrapPlacementSystem` pattern, as interpreted for Unity, focuses on creating a robust and flexible mechanism for players to select, preview, validate, and place interactive objects (like traps, buildings, or furniture) within the game world. It combines principles of state management, strategy-like behavior (for different item types), and clear separation of concerns.

Here's a breakdown of the pattern's components and how they work together:

1.  **`TrapData` (ScriptableObject)**:
    *   **Purpose**: Acts as a blueprint for each distinct trap type.
    *   **Pattern Relevance**: Uses the **Data-Driven Design** principle, allowing designers to create new trap types purely through assets in the Unity editor without writing new code. This promotes extensibility.
    *   **Contents**: Holds immutable data like the trap's `prefab`, `cost`, `name`, `icon`, and `placementRadius` (for collision checks).

2.  **`TrapPlacementSystem` (Core Manager MonoBehaviour)**:
    *   **Purpose**: The central orchestrator that handles the entire placement workflow.
    *   **Pattern Relevance**:
        *   **State Machine**: Manages the system's state (`Idle`, `Previewing`), transitioning between them based on player input and validation.
        *   **Input Handling**: Listens for player commands (select trap, confirm, cancel).
        *   **Validation Logic**: Checks if a chosen location is valid (e.g., on ground, no overlaps, sufficient resources).
        *   **Preview Management**: Instantiates and updates a "ghost" visual of the trap.
        *   **Placement Execution**: Instantiates the final trap and deducts resources.
        *   **Event-Driven**: Uses C# events (`OnTrapPlaced`, `OnPlacementCancelled`, etc.) to communicate with other systems (like UI, analytics, resource managers) without tight coupling (**Observer Pattern**).
    *   **Contents**:
        *   References to `TrapData` assets.
        *   `LayerMask`s for ground and obstacle detection.
        *   Materials for valid/invalid preview feedback.
        *   Internal state variables (`_currentState`, `_currentSelectedTrapData`, `_currentPreviewGameObject`).
        *   Methods for selection, preview updating, validation, placement, and cancellation.

3.  **`PlayerResources` (Static Class - Example)**:
    *   **Purpose**: A simplified system to manage player currency or build resources.
    *   **Pattern Relevance**: Demonstrates how the placement system interacts with other core game systems (like an inventory or resource manager) to enforce game rules (e.g., cost). By being a static class in this example, it's easily accessible but in a larger game, it might be a MonoBehaviour managed by a `GameManager` or accessed via a **Service Locator** pattern.

4.  **`TrapPreview` (Implicit, handled by `TrapPlacementSystem`)**:
    *   **Purpose**: Provides real-time visual feedback to the player about where and if a trap can be placed.
    *   **Pattern Relevance**: Crucial for user experience. The `TrapPlacementSystem` manipulates a temporary GameObject (often a transparent version of the trap prefab) to show the preview, changing its color or displaying indicators based on placement validity.

**Benefits of this Pattern:**

*   **Extensibility**: Adding new trap types is as simple as creating a new `TrapData` ScriptableObject asset and configuring it, without modifying core placement code.
*   **Maintainability**: Placement logic is centralized in `TrapPlacementSystem`, making it easier to modify or debug.
*   **User Experience**: Real-time visual feedback guides the player.
*   **Decoupling**: Events allow other game systems to react to trap placements without direct dependencies on the `TrapPlacementSystem`.
*   **Flexibility**: The validation logic can be easily expanded (e.g., checking for specific terrains, build zones, or player proximity) without affecting the selection or placement mechanics.

---

### Complete C# Unity Example

Below are the C# scripts. You'll need to set up corresponding assets and layers in the Unity Editor as explained in the example usage comments.

#### 1. `TrapData.cs`

```csharp
using UnityEngine;

// --- 1. TrapData ScriptableObject ---
// This ScriptableObject defines a type of trap, holding its essential data.
// Using a ScriptableObject allows us to create multiple trap types as assets
// in the Unity editor without creating new C# classes for each.
// This is a key part of the 'data-driven' aspect of the TrapPlacementSystem pattern.
[CreateAssetMenu(fileName = "NewTrapData", menuName = "Trap System/Trap Data")]
public class TrapData : ScriptableObject
{
    [Tooltip("The actual GameObject prefab that will be instantiated when the trap is placed.")]
    public GameObject trapPrefab;

    [Tooltip("The cost to place this trap.")]
    public int placementCost;

    [Tooltip("The name of the trap, for UI display.")]
    public string trapName;

    [Tooltip("A visual icon for the trap, for UI display (optional).")]
    public Sprite trapIcon;

    [Tooltip("The radius used for Physics.OverlapSphere collision checking during placement " +
             "to prevent overlaps. Should roughly match the trap's physical size.")]
    public float placementRadius = 0.5f;

    [Tooltip("Optional: A custom material to use for the preview of THIS specific trap type. " +
             "If null, the system's default preview material will be used.")]
    public Material customPreviewMaterial;
}
```

#### 2. `TrapPlacementSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<T>
using System; // For Action event

// --- 2. TrapPlacementSystem (Core Manager MonoBehaviour) ---
// This is the heart of the TrapPlacementSystem pattern. It manages the entire
// process of selecting, previewing, validating, and placing traps.
// It acts as a state machine and orchestrator for the placement workflow.
public class TrapPlacementSystem : MonoBehaviour
{
    // --- Public Properties & Events ---
    // These events allow other systems (like UI, analytics, game managers) to react
    // to placement actions without the placement system needing direct references to them.
    // This demonstrates a loose coupling (Observer Pattern) crucial for good design.
    public static event Action<TrapData, Vector3> OnTrapPlaced;
    public static event Action OnPlacementCancelled;
    public static event Action<bool> OnPreviewValidityChanged; // bool indicates if current preview is valid

    // --- Editor-Configurable Fields ---
    [Header("Trap Settings")]
    [Tooltip("List of all available trap types that can be placed. Populate this in the Inspector.")]
    [SerializeField] private List<TrapData> _availableTraps = new List<TrapData>();

    [Tooltip("The layer(s) considered as ground where traps can be placed.")]
    [SerializeField] private LayerMask _groundLayer;

    [Tooltip("The layer(s) considered as obstacles (e.g., other traps, environmental objects) " +
             "that prevent placement if overlapped.")]
    [SerializeField] private LayerMask _obstacleLayer;

    [Tooltip("The maximum distance from the camera (or player) where a trap can be placed.")]
    [SerializeField] private float _maxPlacementDistance = 10f;

    [Tooltip("The default material for the preview object when placement is valid.")]
    [SerializeField] private Material _defaultPreviewMaterialValid;

    [Tooltip("The default material for the preview object when placement is invalid.")]
    [SerializeField] private Material _defaultPreviewMaterialInvalid;

    [Tooltip("Optional: A GameObject prefab to be used as a visual indicator for invalid placement " +
             "(e.g., a red X or a blocked icon). Will appear over the preview when invalid.")]
    [SerializeField] private GameObject _invalidPlacementIndicatorPrefab;

    // --- Internal State Variables ---
    // The placement system's state machine.
    private enum PlacementState { Idle, Previewing }
    private PlacementState _currentState = PlacementState.Idle;

    private TrapData _currentSelectedTrapData; // The trap type currently chosen for placement
    private GameObject _currentPreviewGameObject; // The ghost object shown during preview
    private GameObject _invalidIndicatorInstance; // Instance of the invalid placement indicator

    private Camera _mainCamera; // Cached camera reference for raycasting
    private bool _isCurrentPreviewValid = false; // Tracks the validity of the current preview position

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        _mainCamera = Camera.main; // Cache the main camera for performance
        if (_mainCamera == null)
        {
            Debug.LogError("TrapPlacementSystem: No main camera found! Please tag your camera as 'MainCamera'.");
            enabled = false; // Disable script if no camera is found
        }
    }

    private void Update()
    {
        HandleTrapSelectionInput(); // Always check for trap selection
        
        if (_currentState == PlacementState.Previewing)
        {
            HandlePreviewModeInput();   // Check for confirmation/cancellation
            UpdatePlacementPreview();   // Update ghost object position and validity
        }
    }

    // --- Core Logic Methods ---

    // Handles player input for selecting different trap types using number keys.
    // This could be replaced by UI buttons or a more sophisticated hotkey system.
    private void HandleTrapSelectionInput()
    {
        for (int i = 0; i < _availableTraps.Count; i++)
        {
            // Input.GetKeyDown(KeyCode.Alpha1 + i) maps to '1', '2', '3', etc.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                // Ensure the index is valid for our list of traps
                if (i < _availableTraps.Count)
                {
                    SelectTrap(_availableTraps[i]);
                    return; // Only select one trap per frame
                }
            }
        }
    }

    // Handles input specific to the preview mode (confirm/cancel placement).
    private void HandlePreviewModeInput()
    {
        // Left mouse button (0) to confirm placement
        if (Input.GetMouseButtonDown(0))
        {
            if (_isCurrentPreviewValid)
            {
                PlaceTrap();
            }
            else
            {
                Debug.Log("Placement not valid. Check location, overlaps, or resources.");
            }
        }

        // Right mouse button (1) or Escape key to cancel placement
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    // Initiates the trap placement process by selecting a specific trap type.
    // This is the entry point into the 'Previewing' state.
    public void SelectTrap(TrapData trap)
    {
        if (trap == null)
        {
            Debug.LogWarning("Attempted to select a null TrapData. Please check your _availableTraps list.");
            return;
        }

        // If a different trap is already being previewed, cancel it first to clean up.
        if (_currentState == PlacementState.Previewing && _currentSelectedTrapData != trap)
        {
            CancelPlacement();
        }

        _currentSelectedTrapData = trap;
        _currentState = PlacementState.Previewing;
        StartPreviewVisual(); // Prepare the visual ghost object

        Debug.Log($"Selected trap: {_currentSelectedTrapData.trapName}. Entering preview mode. Press LMB to place, RMB/ESC to cancel.");
    }

    // Prepares and displays the visual preview object (the "ghost" trap).
    private void StartPreviewVisual()
    {
        // Clean up any existing preview objects from previous selections/cancellations.
        if (_currentPreviewGameObject != null) Destroy(_currentPreviewGameObject);
        if (_invalidIndicatorInstance != null) Destroy(_invalidIndicatorInstance);

        // Instantiate the trap's prefab to use as the preview.
        _currentPreviewGameObject = Instantiate(_currentSelectedTrapData.trapPrefab);
        _currentPreviewGameObject.name = "Trap_Preview_" + _currentSelectedTrapData.trapName;
        
        // It's crucial to set the preview object to a layer that won't interfere with
        // actual game physics or obstacle detection. "Ignore Raycast" or a custom "Preview" layer works well.
        SetLayerRecursively(_currentPreviewGameObject, LayerMask.NameToLayer("Ignore Raycast"));

        // Disable colliders and any active scripts on the preview object to prevent interaction
        // during the preview phase. These will be re-enabled or replaced on the actual placed trap.
        foreach (Collider col in _currentPreviewGameObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        foreach (MonoBehaviour script in _currentPreviewGameObject.GetComponentsInChildren<MonoBehaviour>())
        {
            // Only disable scripts that are not the TrapPlacementSystem itself (if it were to be on the prefab)
            if (script != this) { script.enabled = false; }
        }

        // Instantiate the optional invalid placement indicator.
        if (_invalidPlacementIndicatorPrefab != null)
        {
            _invalidIndicatorInstance = Instantiate(_invalidPlacementIndicatorPrefab);
            _invalidIndicatorInstance.SetActive(false); // Start hidden
            SetLayerRecursively(_invalidIndicatorInstance, LayerMask.NameToLayer("Ignore Raycast"));
        }

        // Hide the preview initially until a valid ground position is found.
        _currentPreviewGameObject.SetActive(false);
    }

    // Updates the position, rotation, and visual feedback (material, indicator) of the preview object.
    private void UpdatePlacementPreview()
    {
        // Raycast from the camera to the mouse position to find a ground hit.
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Only consider hits on the specified ground layer within max placement distance.
        if (Physics.Raycast(ray, out hit, _maxPlacementDistance, _groundLayer))
        {
            // A ground position was found. Update preview's transform.
            Vector3 placementPosition = hit.point;
            // Align the trap's forward with the camera's forward, projected onto the plane (optional).
            Quaternion placementRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up), Vector3.up);

            _currentPreviewGameObject.SetActive(true);
            _currentPreviewGameObject.transform.position = placementPosition;
            _currentPreviewGameObject.transform.rotation = placementRotation;

            // Validate all placement conditions at this exact position.
            _isCurrentPreviewValid = CheckPlacementValidity(placementPosition, _currentSelectedTrapData);
            UpdatePreviewVisuals(_isCurrentPreviewValid); // Update color and indicator

            // Notify any UI or other systems about the current validity.
            OnPreviewValidityChanged?.Invoke(_isCurrentPreviewValid);
        }
        else
        {
            // No valid ground hit. Hide preview and indicate invalidity.
            _currentPreviewGameObject.SetActive(false);
            if (_invalidIndicatorInstance != null) { _invalidIndicatorInstance.SetActive(false); }
            _isCurrentPreviewValid = false;
            OnPreviewValidityChanged?.Invoke(false);
        }
    }

    // Checks all conditions for a valid trap placement at a given position.
    // This is where custom game rules for placement are enforced.
    private bool CheckPlacementValidity(Vector3 position, TrapData trapData)
    {
        // 1. Resource Check: Can the player afford this trap?
        if (!PlayerResources.CanAfford(trapData.placementCost))
        {
            return false;
        }

        // 2. Overlap Check: Use Physics.OverlapSphere to detect if the trap would overlap with obstacles.
        // The overlap check should ignore the preview itself, as it's not a real obstacle.
        Collider[] hitColliders = Physics.OverlapSphere(position, trapData.placementRadius, _obstacleLayer);
        foreach (Collider col in hitColliders)
        {
            // If the collider belongs to the preview object or the invalid indicator, ignore it.
            if (col.gameObject == _currentPreviewGameObject || (_invalidIndicatorInstance != null && col.gameObject == _invalidIndicatorInstance))
            {
                continue; // It's our own preview/indicator, not a real obstruction.
            }
            return false; // Found an actual obstacle.
        }
        
        // --- Add more complex validation logic here if needed: ---
        // Examples:
        // - Is the ground flat enough? (e.g., check hit.normal against Vector3.up)
        // - Is it within a specific "buildable zone"? (e.g., another trigger collider)
        // - Does it intersect with the player character or other crucial entities?
        // - Raycast upwards to ensure no ceiling/overhang blocking placement.

        return true; // All checks passed, placement is valid.
    }

    // Applies the correct visual feedback (material and indicator visibility) based on validity.
    private void UpdatePreviewVisuals(bool isValid)
    {
        // Choose between the custom preview material (if defined in TrapData) or the default ones.
        Material targetMaterial = isValid ?
            (_currentSelectedTrapData.customPreviewMaterial != null ? _currentSelectedTrapData.customPreviewMaterial : _defaultPreviewMaterialValid) :
            _defaultPreviewMaterialInvalid;

        // Apply material to all renderers in the preview object.
        foreach (Renderer renderer in _currentPreviewGameObject.GetComponentsInChildren<Renderer>())
        {
            renderer.material = targetMaterial;
        }

        // Show/hide the invalid placement indicator.
        if (_invalidIndicatorInstance != null)
        {
            _invalidIndicatorInstance.SetActive(!isValid);
            if (!isValid)
            {
                // Position the indicator slightly above the preview object for visibility.
                _invalidIndicatorInstance.transform.position = _currentPreviewGameObject.transform.position + Vector3.up * 0.1f;
                _invalidIndicatorInstance.transform.rotation = _currentPreviewGameObject.transform.rotation;
            }
        }
    }

    // Confirms and places the trap in the game world.
    // This transitions from 'Previewing' to 'Idle' state.
    private void PlaceTrap()
    {
        if (_currentSelectedTrapData == null || _currentPreviewGameObject == null)
        {
            Debug.LogError("Attempted to place trap but no trap data or preview object is available. Cancelling.");
            CancelPlacement();
            return;
        }

        Vector3 placementPosition = _currentPreviewGameObject.transform.position;
        Quaternion placementRotation = _currentPreviewGameObject.transform.rotation;

        // Critical: Re-check validity one last time before placing.
        // This handles cases where conditions might have changed between frames (e.g., another player placed something, resource changed).
        if (!CheckPlacementValidity(placementPosition, _currentSelectedTrapData))
        {
            Debug.LogWarning("Placement became invalid just before confirmation. Cancelling placement.");
            CancelPlacement();
            return;
        }

        // Deduct cost from player resources.
        PlayerResources.Deduct(_currentSelectedTrapData.placementCost);

        // Instantiate the actual trap prefab.
        GameObject placedTrap = Instantiate(_currentSelectedTrapData.trapPrefab, placementPosition, placementRotation);
        placedTrap.name = _currentSelectedTrapData.trapName; // Name for clarity in Hierarchy

        // Optional: Add a specific component to the placed trap for its game logic (e.g., an "ITrap" interface implementation).
        // Example: PlacedTrapComponent trapLogic = placedTrap.AddComponent<PlacedTrapComponent>();
        // trapLogic.Initialize(_currentSelectedTrapData); // Pass data for trap's specific behavior

        Debug.Log($"Successfully placed '{_currentSelectedTrapData.trapName}' at {placementPosition}. Cost: {_currentSelectedTrapData.placementCost}. Remaining Gold: {PlayerResources.Gold}");

        // Notify other systems that a trap was placed.
        OnTrapPlaced?.Invoke(_currentSelectedTrapData, placementPosition);

        // End placement mode and clean up.
        ExitPlacementMode();
    }

    // Cancels the current trap placement operation.
    // This transitions from 'Previewing' to 'Idle' state.
    private void CancelPlacement()
    {
        Debug.Log("Trap placement cancelled.");
        OnPlacementCancelled?.Invoke(); // Notify listeners
        ExitPlacementMode(); // Clean up
    }

    // Cleans up all preview objects and resets the system's state.
    private void ExitPlacementMode()
    {
        if (_currentPreviewGameObject != null)
        {
            Destroy(_currentPreviewGameObject);
            _currentPreviewGameObject = null;
        }
        if (_invalidIndicatorInstance != null)
        {
            Destroy(_invalidIndicatorInstance);
            _invalidIndicatorInstance = null;
        }

        _currentSelectedTrapData = null;
        _currentState = PlacementState.Idle;
        _isCurrentPreviewValid = false;
        OnPreviewValidityChanged?.Invoke(false); // Notify that preview is no longer active
    }

    // Helper method to set the layer for a GameObject and all its children.
    // Useful for ensuring preview objects are on an "Ignore Raycast" or "Preview" layer.
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // --- Visualization in Editor (Optional) ---
    // Draws a gizmo sphere in the editor to visualize the trap's placement radius.
    private void OnDrawGizmos()
    {
        if (_currentState == PlacementState.Previewing && _currentPreviewGameObject != null && _currentSelectedTrapData != null)
        {
            Gizmos.color = _isCurrentPreviewValid ? Color.green : Color.red;
            // Draw a wire sphere at the preview's position with its defined placement radius.
            Gizmos.DrawWireSphere(_currentPreviewGameObject.transform.position, _currentSelectedTrapData.placementRadius);
        }
    }
}
```

#### 3. `PlayerResources.cs`

```csharp
using UnityEngine;

// --- 3. PlayerResources (A simple placeholder for player currency/resources) ---
// This class simulates a player's resource system. In a real game, this would
// likely be part of a larger PlayerController, GameManager, or a persistent
// data system. Using a static class here simplifies the example, but be mindful
// of static class limitations in larger, more complex architectures.
public static class PlayerResources
{
    private static int _gold = 100; // Starting gold for demonstration

    public static int Gold
    {
        get { return _gold; }
        private set { _gold = Mathf.Max(0, value); } // Ensure gold never goes below zero
    }

    // Checks if the player has enough resources to afford a certain cost.
    public static bool CanAfford(int cost)
    {
        return Gold >= cost;
    }

    // Deducts resources after an action (e.g., placing a trap).
    public static void Deduct(int amount)
    {
        if (CanAfford(amount))
        {
            Gold -= amount;
            // Debug.Log($"Deducted {amount} gold. Current gold: {Gold}"); // Uncomment for more verbose logging
        }
        else
        {
            Debug.LogWarning($"Attempted to deduct {amount} gold, but only {Gold} available. Action failed.");
        }
    }

    // Adds resources to the player (e.g., collecting loot).
    public static void Add(int amount)
    {
        Gold += amount;
        // Debug.Log($"Added {amount} gold. Current gold: {Gold}"); // Uncomment for more verbose logging
    }

    // For debugging or editor tools to easily set initial gold.
    public static void SetGold(int amount)
    {
        Gold = amount;
        Debug.Log($"PlayerResources: Gold set to {Gold}.");
    }
}
```

---

### Example Usage: Setting up in Unity Editor

To make this example work, follow these steps in your Unity project:

1.  **Create a TrapPlacementSystem GameObject:**
    *   In the Hierarchy window, right-click -> `Create Empty`. Name it "TrapPlacementSystem".
    *   Drag and drop the `TrapPlacementSystem.cs` script onto this new GameObject in the Inspector.

2.  **Configure Layers:**
    *   Go to `Edit` -> `Project Settings` -> `Tags and Layers`.
    *   Under "Layers", add at least two new layers: "Ground" (if you don't have one) and "Obstacle".
    *   Ensure your ground objects (e.g., `Terrain`, floor planes) in your scene are set to the "Ground" layer.
    *   Ensure any objects that should block trap placement (e.g., other traps, large rocks, walls, enemy characters) are set to the "Obstacle" layer.
    *   **Important**: The `TrapPlacementSystem` script uses `LayerMask.NameToLayer("Ignore Raycast")` for preview objects. This is a built-in Unity layer and usually safe, but you could create a custom "Preview" layer if you prefer.
    *   In the `TrapPlacementSystem` GameObject's Inspector:
        *   Set the `_Ground Layer` field to your "Ground" layer.
        *   Set the `_Obstacle Layer` field to your "Obstacle" layer.
        *   Adjust `_Max Placement Distance` as desired (e.g., 10 for 10 units).

3.  **Create Trap Prefabs:**
    *   Create some simple 3D models (e.g., a `Cube` for a "Spike Trap", a `Sphere` for a "Mine") by right-clicking in Hierarchy -> `3D Object`.
    *   Add a `Collider` component (e.g., `Box Collider`, `Sphere Collider`) to each of these objects.
    *   **Crucial**: Set the layer of these prefab objects to your "Obstacle" layer so the `TrapPlacementSystem` can detect them.
    *   Drag these configured objects from the Hierarchy into your Project window (e.g., into a "Prefabs" folder) to create reusable prefabs.

4.  **Create Preview Materials:**
    *   In your Project window, right-click -> `Create` -> `Material`.
    *   Create two materials:
        *   "PreviewValidMaterial": Set its color to a semi-transparent green (e.g., `RGBA(0, 1, 0, 0.5)`). Change `Render Mode` to "Fade" or "Transparent" in the Inspector.
        *   "PreviewInvalidMaterial": Set its color to a semi-transparent red (e.g., `RGBA(1, 0, 0, 0.5)`). Change `Render Mode` to "Fade" or "Transparent".
    *   Assign these materials to the `_Default Preview Material Valid` and `_Default Preview Material Invalid` fields respectively in the `TrapPlacementSystem` Inspector.

5.  **Create Invalid Placement Indicator (Optional):**
    *   Create a simple prefab that visually indicates "cannot place" (e.g., a `Quad` with a red "X" texture, or a simple red cross 3D model).
    *   Assign this prefab to the `_Invalid Placement Indicator Prefab` field in the `TrapPlacementSystem` Inspector.

6.  **Create TrapData ScriptableObjects:**
    *   In your Project window, right-click -> `Create` -> `Trap System` -> `Trap Data`.
    *   Create several of these (e.g., "SpikeTrapData", "BearTrapData", "BombTrapData").
    *   For each `TrapData` asset:
        *   Drag your corresponding trap prefab (from step 3) into the `Trap Prefab` field.
        *   Set a `Placement Cost` (e.g., 10, 20, 30).
        *   Give it a unique `Trap Name`.
        *   Adjust the `Placement Radius` to accurately reflect the size of your trap for overlap detection.
        *   (Optional) Assign a `Custom Preview Material` if you want a specific look for that trap type that overrides the default.

7.  **Populate Available Traps:**
    *   In the `TrapPlacementSystem` Inspector, locate the `_Available Traps` list.
    *   Drag your created `TrapData` ScriptableObjects (from step 6) into this list. The order in the list will correspond to the number keys (1, 2, 3...) used for selection.

8.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   You should see messages in the console about `PlayerResources` (starting gold).
    *   Press '1', '2', '3' (or whichever number corresponds to your trap count) to select a trap.
    *   Move your mouse cursor over the game world (over your "Ground" layer objects).
        *   You should see a "ghost" image of your selected trap.
        *   If the ghost is green (and no invalid indicator), placement is valid.
        *   If it's red (and shows the invalid indicator if provided), placement is invalid (check overlaps, cost, placement range).
    *   **Left-click** to place the trap if the preview is valid.
    *   **Right-click** or press `Escape` to cancel placement and return to idle.
    *   Observe the console for messages confirming placement, cost deduction, or cancellation.

This setup provides a complete, functional, and easily extensible trap placement system ready for integration into your Unity projects!