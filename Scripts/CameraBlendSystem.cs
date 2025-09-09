// Unity Design Pattern Example: CameraBlendSystem
// This script demonstrates the CameraBlendSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'CameraBlendSystem' design pattern focuses on managing multiple potential camera views and smoothly transitioning between them using a single active camera component. This avoids the overhead and complexity of enabling/disabling numerous `Camera` components, and centralizes camera control logic.

Here's a complete C# Unity implementation that is practical, educational, and ready to use.

---

### 1. `CameraBlendSystem.cs`

This is the core manager script. It uses a single `Primary Camera` component and dynamically updates its properties (position, rotation, FOV, orthographic size) to match the desired camera states.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Used for dictionary initialization and checks

/// <summary>
/// Represents a snapshot of a camera's properties at a specific moment.
/// This struct is used internally by the CameraBlendSystem to store and interpolate
/// between different camera views without needing actual Camera components for each view.
/// </summary>
[System.Serializable] // Make it serializable for potential debugging or custom editor usage
public struct CameraStateSnapshot
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float FieldOfView;
    public bool IsOrthographic;
    public float OrthographicSize;

    /// <summary>
    /// Creates a CameraStateSnapshot by capturing the current properties of a Unity Camera component.
    /// </summary>
    /// <param name="camera">The Camera component to snapshot.</param>
    public CameraStateSnapshot(Camera camera)
    {
        Position = camera.transform.position;
        Rotation = camera.transform.rotation;
        FieldOfView = camera.fieldOfView;
        IsOrthographic = camera.orthographic;
        OrthographicSize = camera.orthographicSize;
    }

    /// <summary>
    /// Linearly interpolates between two CameraStateSnapshots.
    /// </summary>
    /// <param name="a">The starting camera state.</param>
    /// <param name="b">The ending camera state.</param>
    /// <param name="t">The interpolation factor (0.0 to 1.0).</param>
    /// <returns>A new CameraStateSnapshot representing the interpolated state.</returns>
    public static CameraStateSnapshot Lerp(CameraStateSnapshot a, CameraStateSnapshot b, float t)
    {
        CameraStateSnapshot result = new CameraStateSnapshot();
        result.Position = Vector3.Lerp(a.Position, b.Position, t);
        result.Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
        result.FieldOfView = Mathf.Lerp(a.FieldOfView, b.FieldOfView, t);
        result.OrthographicSize = Mathf.Lerp(a.OrthographicSize, b.OrthographicSize, t);

        // For orthographic, we apply the target's state directly.
        // A more advanced system might blend between perspective and orthographic effects,
        // but for typical use cases, a discrete switch during blend is sufficient.
        result.IsOrthographic = b.IsOrthographic; 
        
        return result;
    }
}

/// <summary>
/// A helper class for defining camera points in the Unity Editor.
/// This allows designers to easily set up various camera views using GameObjects
/// and specify properties like FOV or Orthographic Size directly in the Inspector.
/// </summary>
[System.Serializable]
public class CameraPoint
{
    [Tooltip("A unique identifier for this camera point. Used to reference it in code.")]
    public string ID;

    [Tooltip("The Transform that defines the position and rotation for this camera point.")]
    public Transform TargetTransform;

    [Tooltip("If true, the 'SpecificFOV' will be used. Otherwise, the primary camera's current FOV will be maintained or the default if not blending.")]
    public bool UseSpecificFOV = false;
    [Range(1, 179)]
    [Tooltip("The Field of View (in degrees) for this camera point, if 'UseSpecificFOV' is true.")]
    public float SpecificFOV = 60f;

    [Tooltip("If true, the 'SpecificOrthoSize' will be used. Otherwise, the primary camera's current Ortho Size will be maintained or the default.")]
    public bool UseSpecificOrthoSize = false;
    [Range(0.1f, 100)]
    [Tooltip("The Orthographic Size for this camera point, if 'UseSpecificOrthoSize' is true.")]
    public float SpecificOrthoSize = 5f;

    [Tooltip("If true, the camera will be orthographic at this point. If false, it will be perspective.")]
    public bool IsOrthographic = false;

    /// <summary>
    /// Generates a CameraStateSnapshot from this CameraPoint's settings.
    /// It captures the position and rotation from TargetTransform and applies
    /// any specific FOV/OrthoSize overrides.
    /// </summary>
    /// <param name="defaultFOV">The default FOV to use if not overridden.</param>
    /// <param name="defaultOrthoSize">The default Orthographic Size to use if not overridden.</param>
    /// <returns>A CameraStateSnapshot representing this CameraPoint.</returns>
    public CameraStateSnapshot GetStateSnapshot(float defaultFOV, float defaultOrthoSize)
    {
        if (TargetTransform == null)
        {
            Debug.LogError($"CameraPoint '{ID}' has no TargetTransform assigned. Cannot create snapshot. Returning default.");
            // Return a default snapshot to prevent null reference issues, though an error is logged.
            return new CameraStateSnapshot
            {
                Position = Vector3.zero, 
                Rotation = Quaternion.identity, 
                FieldOfView = defaultFOV, 
                IsOrthographic = IsOrthographic, 
                OrthographicSize = defaultOrthoSize
            }; 
        }

        return new CameraStateSnapshot
        {
            Position = TargetTransform.position,
            Rotation = TargetTransform.rotation,
            FieldOfView = UseSpecificFOV ? SpecificFOV : defaultFOV,
            OrthographicSize = UseSpecificOrthoSize ? SpecificOrthoSize : defaultOrthoSize,
            IsOrthographic = IsOrthographic
        };
    }
}

/// <summary>
/// The CameraBlendSystem is a central manager for handling multiple camera views
/// and smoothly blending between them. It uses a single 'Primary Camera' component
/// and manipulates its properties (position, rotation, FOV, etc.) to achieve
/// different camera perspectives. This avoids the overhead of enabling/disabling
/// multiple full Camera components and simplifies audio listeners.
/// </summary>
/// <remarks>
/// Design Pattern: This implements a variation of the 'State' and 'Manager' patterns.
/// It manages different camera states and provides a controlled way to transition between them.
/// It also uses the 'Singleton' pattern for easy global access from any script.
/// </remarks>
public class CameraBlendSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a global access point to the CameraBlendSystem instance.
    public static CameraBlendSystem Instance { get; private set; }

    // --- Editor-Configurable Properties ---
    [Header("Primary Camera Settings")]
    [Tooltip("The actual Unity Camera component that this system will control. If left unassigned, it will try to find Camera.main.")]
    [SerializeField] private Camera _primaryCamera;

    [Header("Predefined Camera Points")]
    [Tooltip("A list of camera points defined in the editor. Create empty GameObjects for their transforms.")]
    [SerializeField] private List<CameraPoint> _predefinedCameraPoints = new List<CameraPoint>();

    [Header("Blending Settings")]
    [Tooltip("The animation curve used for blending (e.g., EaseInOut for smooth transitions).")]
    [SerializeField] private AnimationCurve _blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("The default duration (in seconds) for camera blends, if not specified when calling BlendToCamera.")]
    [SerializeField] private float _defaultBlendDuration = 1.0f;

    // --- Internal State ---
    // A dictionary to store all registered camera states, keyed by their unique ID.
    private Dictionary<string, CameraStateSnapshot> _registeredCameraStates = new Dictionary<string, CameraStateSnapshot>();
    
    private CameraStateSnapshot _startBlendState; // The camera state at the beginning of a blend.
    private CameraStateSnapshot _targetBlendState; // The camera state at the end of a blend.
    private float _currentBlendTime; // How much time has passed since the blend started.
    private float _blendDuration; // The total duration for the current blend.
    private bool _isBlending = false; // Is a blend currently in progress?

    // The state the primary camera is currently displaying (even when not blending).
    // This is continuously updated to reflect the camera's actual properties.
    private CameraStateSnapshot _currentActiveState; 

    /// <summary>
    /// Public property to check if the camera is currently blending.
    /// </summary>
    public bool IsBlending => _isBlending;

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement Singleton pattern to ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CameraBlendSystem: Multiple CameraBlendSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make the CameraBlendSystem persist across scene loads.
            // Comment out if you want a new manager per scene.
            DontDestroyOnLoad(gameObject); 
            InitializeSystem();
        }
    }

    /// <summary>
    /// Initializes the CameraBlendSystem: finds the primary camera, captures its initial state,
    /// and registers all predefined camera points from the Inspector.
    /// </summary>
    private void InitializeSystem()
    {
        // If _primaryCamera is not set in the inspector, try to find Camera.main
        if (_primaryCamera == null)
        {
            _primaryCamera = Camera.main;
            if (_primaryCamera == null)
            {
                Debug.LogError("CameraBlendSystem: No primary camera assigned and Camera.main not found! Please assign a camera or ensure one is tagged 'MainCamera'. Disabling script.");
                enabled = false; // Disable the script if no camera to control
                return;
            }
        }

        // Initialize _currentActiveState with the primary camera's initial state
        // This ensures blending always starts from the camera's current visible state.
        _currentActiveState = new CameraStateSnapshot(_primaryCamera);
        ApplyStateToPrimaryCamera(_currentActiveState); // Ensure primary camera matches the initially captured state

        // Register predefined camera points from the Inspector list
        foreach (var camPoint in _predefinedCameraPoints)
        {
            if (string.IsNullOrEmpty(camPoint.ID))
            {
                Debug.LogWarning($"CameraBlendSystem: Skipping CameraPoint with unassigned ID. Please ensure all predefined camera points have unique IDs.");
                continue;
            }
            if (_registeredCameraStates.ContainsKey(camPoint.ID))
            {
                Debug.LogWarning($"CameraBlendSystem: Duplicate CameraPoint ID '{camPoint.ID}' found in predefined list. Overwriting previous entry.");
            }
            // Generate snapshot using primary camera's current FOV/OrthoSize as defaults if not overridden
            _registeredCameraStates[camPoint.ID] = camPoint.GetStateSnapshot(_primaryCamera.fieldOfView, _primaryCamera.orthographicSize);
        }
        
        Debug.Log($"CameraBlendSystem initialized with {_registeredCameraStates.Count} predefined camera states.");
    }

    /// <summary>
    /// Called once per frame. Handles the ongoing camera blending process.
    /// </summary>
    private void Update()
    {
        if (_isBlending)
        {
            _currentBlendTime += Time.deltaTime;
            // Calculate normalized time (0 to 1) for the blend, clamping to prevent overshoot
            float normalizedTime = Mathf.Clamp01(_currentBlendTime / _blendDuration);
            // Evaluate the animation curve to get a non-linear interpolation factor
            float curveValue = _blendCurve.Evaluate(normalizedTime);

            // Interpolate between the start and target states using the curved value
            CameraStateSnapshot blendedState = CameraStateSnapshot.Lerp(_startBlendState, _targetBlendState, curveValue);
            
            // Apply the interpolated state to the actual primary camera
            ApplyStateToPrimaryCamera(blendedState);

            // Update _currentActiveState to reflect the camera's current blended properties
            _currentActiveState = blendedState;

            if (normalizedTime >= 1.0f)
            {
                // Blending has finished
                _isBlending = false;
                // Ensure the final state is exactly the target state to avoid floating point inaccuracies
                _currentActiveState = _targetBlendState; 
                ApplyStateToPrimaryCamera(_currentActiveState); // Apply final state precisely
                Debug.Log($"Camera blend finished. Current view: {_targetBlendState.Position}");
            }
        }
    }

    // --- Public API for Camera Control ---

    /// <summary>
    /// Registers a new camera state at runtime. This allows for dynamic camera points
    /// (e.g., following a new enemy, or a player character when they enter a specific zone).
    /// </summary>
    /// <param name="id">A unique identifier for this camera state.</param>
    /// <param name="targetTransform">The Transform whose position and rotation define the camera state.</param>
    /// <param name="isOrthographic">Whether the camera should be orthographic at this state.</param>
    /// <param name="fov">Optional: Specific Field of View. If null, uses the current primary camera's FOV.</param>
    /// <param name="orthoSize">Optional: Specific Orthographic Size. If null, uses the current primary camera's Orthographic Size.</param>
    public void RegisterCameraPoint(string id, Transform targetTransform, bool isOrthographic = false, float? fov = null, float? orthoSize = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("CameraBlendSystem: Cannot register camera point with an empty ID.");
            return;
        }
        if (targetTransform == null)
        {
            Debug.LogError($"CameraBlendSystem: Cannot register camera point '{id}' with a null TargetTransform.");
            return;
        }

        CameraStateSnapshot newSnapshot = new CameraStateSnapshot
        {
            Position = targetTransform.position,
            Rotation = targetTransform.rotation,
            IsOrthographic = isOrthographic,
            FieldOfView = fov ?? _primaryCamera.fieldOfView, // Use provided FOV or primary camera's current FOV
            OrthographicSize = orthoSize ?? _primaryCamera.orthographicSize // Use provided Ortho Size or primary camera's current
        };

        if (_registeredCameraStates.ContainsKey(id))
        {
            Debug.LogWarning($"CameraBlendSystem: Overwriting existing camera point with ID '{id}'.");
        }
        _registeredCameraStates[id] = newSnapshot;
        Debug.Log($"Camera point '{id}' registered successfully at {newSnapshot.Position}.");
    }

    /// <summary>
    /// Initiates a smooth blend transition to a previously registered camera state.
    /// </summary>
    /// <param name="id">The ID of the target camera state.</param>
    /// <param name="duration">Optional: The duration of the blend in seconds. If null, uses the default blend duration.</param>
    public void BlendToCamera(string id, float? duration = null)
    {
        if (!_registeredCameraStates.TryGetValue(id, out CameraStateSnapshot targetState))
        {
            Debug.LogWarning($"CameraBlendSystem: Camera point with ID '{id}' not found. Cannot blend.");
            return;
        }

        // Capture the current camera state as the blend's starting point.
        // This makes transitions smooth even if the camera was manually moved or set instantly before.
        _startBlendState = _currentActiveState; 
        _targetBlendState = targetState;
        _blendDuration = duration ?? _defaultBlendDuration; // Use provided duration or default
        _currentBlendTime = 0f;
        _isBlending = true;
        
        Debug.Log($"Camera blend initiated to '{id}' for {_blendDuration} seconds.");
    }

    /// <summary>
    /// Instantly switches the primary camera to a previously registered camera state without blending.
    /// </summary>
    /// <param name="id">The ID of the target camera state.</param>
    public void SetCameraInstant(string id)
    {
        if (!_registeredCameraStates.TryGetValue(id, out CameraStateSnapshot targetState))
        {
            Debug.LogWarning($"CameraBlendSystem: Camera point with ID '{id}' not found. Cannot set instantly.");
            return;
        }

        _isBlending = false; // Stop any ongoing blend immediately
        _currentActiveState = targetState; // Update the current active state
        ApplyStateToPrimaryCamera(_currentActiveState); // Apply the target state directly to the camera
        Debug.Log($"Camera instantly set to '{id}'.");
    }

    /// <summary>
    /// Retrieves a snapshot of the primary camera's current properties.
    /// This can be useful for saving a temporary camera state or debugging.
    /// </summary>
    /// <returns>A CameraStateSnapshot representing the primary camera's current state.</returns>
    public CameraStateSnapshot GetCurrentCameraStateSnapshot()
    {
        return new CameraStateSnapshot(_primaryCamera);
    }

    /// <summary>
    /// Applies a given CameraStateSnapshot to the primary camera component.
    /// This is the core helper method that manipulates the actual Unity Camera's transform and properties.
    /// </summary>
    /// <param name="state">The CameraStateSnapshot to apply.</param>
    private void ApplyStateToPrimaryCamera(CameraStateSnapshot state)
    {
        if (_primaryCamera == null) return; // Defensive check

        _primaryCamera.transform.position = state.Position;
        _primaryCamera.transform.rotation = state.Rotation;
        _primaryCamera.fieldOfView = state.FieldOfView;
        _primaryCamera.orthographic = state.IsOrthographic;
        _primaryCamera.orthographicSize = state.OrthographicSize;
    }

    // --- Editor-only Visualization (Gizmos) ---
    // This method draws visual helpers in the editor scene view to show camera points.
    private void OnDrawGizmos()
    {
        // Require UnityEditor namespace for Handles.Label
#if UNITY_EDITOR
        if (_primaryCamera == null) return;

        // Draw gizmos for all registered camera states (both predefined and runtime registered)
        if (_registeredCameraStates != null)
        {
            foreach (var entry in _registeredCameraStates)
            {
                CameraStateSnapshot state = entry.Value;
                
                Gizmos.color = Color.cyan; // Color for registered camera points
                // Set the gizmo matrix to the camera point's transform for drawing local shapes
                Gizmos.matrix = Matrix4x4.TRS(state.Position, state.Rotation, Vector3.one);
                
                if (state.IsOrthographic)
                {
                    // Draw a wire cube representing the orthographic view frustum's near plane
                    float aspect = _primaryCamera.aspect; // Use primary camera's aspect for correct visualization
                    float height = state.OrthographicSize * 2f;
                    float width = height * aspect;
                    // Draw a box that represents the view plane at a fixed distance (e.g., 0.5 units forward)
                    Gizmos.DrawWireCube(Vector3.forward * 0.5f, new Vector3(width, height, 1f)); 
                }
                else
                {
                    // Draw the frustum for a perspective camera
                    Gizmos.DrawFrustum(Vector3.zero, state.FieldOfView, 1f, 0.1f, _primaryCamera.aspect);
                }

                // Draw a small sphere and line to indicate camera position and forward direction
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(Vector3.zero, 0.2f); // Camera 'eye' position
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f); // Camera 'look' direction
                
                // Draw ID text label above the camera point
                UnityEditor.Handles.Label(state.Position + state.Rotation * Vector3.up * 0.5f, entry.Key);
            }
        }

        // Draw the current primary camera's frustum in a different color to distinguish it
        if (_primaryCamera != null)
        {
            Gizmos.color = _isBlending ? Color.yellow : Color.red; // Yellow when blending, red otherwise
            Gizmos.matrix = Matrix4x4.TRS(_primaryCamera.transform.position, _primaryCamera.transform.rotation, Vector3.one);
            if (_primaryCamera.orthographic)
            {
                float aspect = _primaryCamera.aspect;
                float height = _primaryCamera.orthographicSize * 2f;
                float width = height * aspect;
                Gizmos.DrawWireCube(Vector3.forward * 0.5f, new Vector3(width, height, 1f));
            }
            else
            {
                Gizmos.DrawFrustum(Vector3.zero, _primaryCamera.fieldOfView, 1f, 0.1f, _primaryCamera.aspect);
            }
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, 0.25f);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.75f);
        }
#endif
    }
}

```

---

### 2. Example Usage Script (`CameraTestController.cs`)

This script demonstrates how to interact with the `CameraBlendSystem` from other parts of your game code, both with predefined camera points and runtime-registered ones.

```csharp
using UnityEngine;

/// <summary>
/// This script demonstrates how to use the CameraBlendSystem to switch between
/// different camera views, both predefined in the editor and registered at runtime.
/// </summary>
public class CameraTestController : MonoBehaviour
{
    [Header("Predefined Camera IDs")]
    [Tooltip("ID for the player's typical view. Must match an ID in CameraBlendSystem's Predefined Camera Points.")]
    public string playerViewID = "PlayerView";
    [Tooltip("ID for an overhead view. Must match an ID in CameraBlendSystem's Predefined Camera Points.")]
    public string overheadViewID = "Overhead";
    [Tooltip("ID for a cinematic view. Must match an ID in CameraBlendSystem's Predefined Camera Points.")]
    public string cinematicViewID = "Cutscene1"; // Example for a specific cutscene view

    [Header("Blending Options")]
    [Tooltip("The duration (in seconds) for the camera blends triggered by this script.")]
    public float blendTime = 1.5f;

    [Header("Runtime Camera Point Example")]
    [Tooltip("Prefab for a new enemy to spawn and track with a runtime-registered camera point.")]
    public GameObject newEnemyPrefab;
    private Transform _newEnemyCamPointTransform; // Stores the transform for a runtime-registered camera point

    void Update()
    {
        // Ensure the CameraBlendSystem is initialized and available
        if (CameraBlendSystem.Instance == null)
        {
            Debug.LogError("CameraTestController: CameraBlendSystem.Instance is null. Is the CameraBlendSystem in your scene and initialized?");
            return;
        }

        // --- Demonstrate blending to predefined camera points ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log($"Requesting blend to '{playerViewID}'...");
            CameraBlendSystem.Instance.BlendToCamera(playerViewID, blendTime);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log($"Requesting blend to '{overheadViewID}'...");
            CameraBlendSystem.Instance.BlendToCamera(overheadViewID, blendTime);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log($"Requesting blend to '{cinematicViewID}'...");
            CameraBlendSystem.Instance.BlendToCamera(cinematicViewID, blendTime);
        }
        // --- Demonstrate instant camera switch ---
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Requesting instant switch to '{playerViewID}'...");
            CameraBlendSystem.Instance.SetCameraInstant(playerViewID);
        }
        // --- Demonstrate registering and blending to a runtime camera point ---
        else if (Input.GetKeyDown(KeyCode.E)) // 'E' for Enemy
        {
            Debug.Log("Spawning enemy and attempting to switch camera to it...");
            SpawnAndTrackEnemy();
        }
    }

    /// <summary>
    /// Spawns a new enemy and dynamically registers a camera point to follow it,
    /// then blends the camera to this new point.
    /// This showcases registering camera points at runtime.
    /// </summary>
    public void SpawnAndTrackEnemy()
    {
        if (newEnemyPrefab == null)
        {
            Debug.LogError("CameraTestController: newEnemyPrefab is not assigned for spawning enemy.");
            return;
        }

        GameObject enemy = Instantiate(newEnemyPrefab, new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15)), Quaternion.identity);
        enemy.name = $"RuntimeEnemy_{enemy.GetInstanceID()}";

        // Create a temporary GameObject to define the camera view point for the enemy.
        // This transform will define the position and rotation of our runtime camera point.
        GameObject enemyCamGO = new GameObject($"CamPoint_Enemy_{enemy.GetInstanceID()}");
        // Position the camera behind and slightly above the enemy, looking at it.
        enemyCamGO.transform.position = enemy.transform.position + new Vector3(0, 5, -8);
        enemyCamGO.transform.LookAt(enemy.transform);
        _newEnemyCamPointTransform = enemyCamGO.transform;

        // Register this new camera point with the CameraBlendSystem
        string enemyCamID = "EnemyView_" + enemy.GetInstanceID(); // Create a unique ID
        // Register with specific FOV (e.g., 45 degrees) and set to perspective (false)
        CameraBlendSystem.Instance.RegisterCameraPoint(enemyCamID, _newEnemyCamPointTransform, false, 45f);
        
        // Now blend to this newly registered camera point
        CameraBlendSystem.Instance.BlendToCamera(enemyCamID, 2.0f); // Longer blend for this dramatic switch
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "Press 1: Player View");
        GUI.Label(new Rect(10, 30, 300, 20), "Press 2: Overhead View");
        GUI.Label(new Rect(10, 50, 300, 20), "Press 3: Cinematic View");
        GUI.Label(new Rect(10, 70, 300, 20), "Press Space: Instant Player View");
        GUI.Label(new Rect(10, 90, 300, 20), "Press E: Spawn Enemy & Track");
    }
}
```

---

### How to Implement and Use in Unity:

1.  **Create Scripts:**
    *   Save the first code block as `CameraBlendSystem.cs` in your Unity project (e.g., in `Assets/Scripts/Camera/`).
    *   Save the second code block as `CameraTestController.cs` (e.g., in `Assets/Scripts/Game/`).

2.  **Setup the Camera Blend System:**
    *   In a new or existing Unity scene, create an empty GameObject. Name it something like `_CameraManager`.
    *   Attach the `CameraBlendSystem.cs` script to this `_CameraManager` GameObject.
    *   **Assign Primary Camera:** Drag your main scene camera (usually named `Main Camera`) from the Hierarchy to the `Primary Camera` field in the `CameraBlendSystem` component in the Inspector. (If you leave it blank, the script will automatically try to find `Camera.main` at runtime).

3.  **Define Predefined Camera Points:**
    *   Create several empty GameObjects in your scene. These will serve as your "camera points" (e.g., `CamPoint_PlayerView`, `CamPoint_Overhead`, `CamPoint_Cutscene1`).
    *   Position and rotate these GameObjects exactly where you want a camera to be and what it should look at.
    *   Select your `_CameraManager` GameObject. In the Inspector, expand the `Predefined Camera Points` list.
    *   For each desired view:
        *   Click the `+` button to add a new `Camera Point`.
        *   **ID:** Enter a unique string ID (e.g., "PlayerView", "Overhead", "Cutscene1"). This is what you'll use to reference it in code.
        *   **Target Transform:** Drag the corresponding empty GameObject (e.g., `CamPoint_PlayerView`) from your Hierarchy to this field.
        *   **Optional Overrides:** Adjust `Use Specific FOV`, `Specific FOV`, `Use Specific Ortho Size`, `Specific Ortho Size`, and `Is Orthographic` as needed for that specific view. If unchecked, the primary camera's current FOV/Ortho Size will be used for that property when blending.

4.  **Setup the Test Controller (or your own Game Manager):**
    *   Create another empty GameObject in your scene. Name it `_GameManager` or `_TestInput`.
    *   Attach the `CameraTestController.cs` script to this GameObject.
    *   In the Inspector for `_GameManager`:
        *   **Player View ID:** Set to "PlayerView" (or whatever you named your player camera point).
        *   **Overhead View ID:** Set to "Overhead".
        *   **Cinematic View ID:** Set to "Cutscene1".
        *   **Blend Time:** Adjust the default blend duration.
        *   **New Enemy Prefab:** (For the runtime example) Create a simple cube prefab and drag it here.

5.  **Run the Scene:**
    *   Play your scene.
    *   Observe the `_CameraManager` and `Main Camera` in the Hierarchy. You'll see the `Main Camera`'s transform and properties change.
    *   **In the Game view (or Scene view with Gizmos enabled):**
        *   Press `1` to blend to the "PlayerView".
        *   Press `2` to blend to the "Overhead" view.
        *   Press `3` to blend to the "Cutscene1" view.
        *   Press `Space` to instantly switch to the "PlayerView".
        *   Press `E` to spawn a new enemy and blend the camera to a dynamically created camera point following it.
    *   You'll see gizmos in the Scene view indicating your predefined camera points (cyan frustums/cubes) and the current position of your primary camera (red/yellow frustum/cube).

This `CameraBlendSystem` provides a robust and flexible way to manage all your in-game camera needs, making it easy to create engaging visual transitions without complex scripting for each camera interaction.