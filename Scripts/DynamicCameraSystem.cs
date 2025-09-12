// Unity Design Pattern Example: DynamicCameraSystem
// This script demonstrates the DynamicCameraSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Dynamic Camera System** design pattern in Unity. This pattern allows a game to dynamically switch between different camera behaviors (modes) based on the current game state or events. It's essentially an application of the **Strategy Pattern**, where the camera's behavior is encapsulated in interchangeable "mode" objects.

**Key Components:**

1.  **`CameraBrain` (MonoBehaviour, Singleton):** The central manager attached to the main camera. It holds the `DynamicCameraSystem` instance and provides public methods to switch camera modes from other scripts. It also drives the `Update` loop for the active camera mode.
2.  **`DynamicCameraSystem` (Core Logic):** This class manages the current camera mode. It's responsible for calling `Enter` on a new mode, `Exit` on the old mode, and delegating `UpdateMode` to the currently active mode.
3.  **`ICameraMode` (Interface):** Defines the common contract for all camera modes: `Enter`, `UpdateMode`, and `Exit`. This ensures all camera modes can be treated interchangeably.
4.  **Concrete Camera Modes (Implementations of `ICameraMode`):** These classes encapsulate specific camera logic.
    *   **`FollowTargetCameraMode`:** A classic third-person camera that smoothly follows a target.
    *   **`CinematicCameraMode`:** A camera mode that transitions to a specific position/rotation, looks at a point, and then can automatically switch back after a duration.
    *   **`FixedViewCameraMode`:** A camera that simply stays at a predefined static position and rotation.

---

### How the DynamicCameraSystem Pattern Works

1.  **Context (`DynamicCameraSystem`):** This is the core engine that holds a reference to the currently active `ICameraMode` strategy. It doesn't know *how* to follow, or be cinematic; it just knows it has a `currentMode` that *does* those things.
2.  **Strategy Interface (`ICameraMode`):** This interface declares the operations common to all concrete camera behaviors (`Enter`, `UpdateMode`, `Exit`).
3.  **Concrete Strategies (`FollowTargetCameraMode`, `CinematicCameraMode`, `FixedViewCameraMode`):** Each of these classes implements `ICameraMode` and contains the specific logic for its camera behavior.
    *   `Enter()`: Called when this mode becomes active. Used for initialization, setting up initial camera state, or starting transitions.
    *   `UpdateMode()`: Called every frame while this mode is active. Contains the actual camera movement, rotation, and logic.
    *   `Exit()`: Called when this mode is no longer active. Used for cleanup, resetting values, or preparing for the next mode.
4.  **Client (`CameraBrain` and other game scripts):** The `CameraBrain` acts as a facade, providing easy access to switch modes. Other game scripts (e.g., a player controller, a cutscene manager, a trigger) simply call `CameraBrain.Instance.SetFollowMode(playerTransform)` or `CameraBrain.Instance.SetCinematicMode(...)` without needing to know the internal details of how each mode works.

---

### Usage in Unity

1.  **Create a C# Script:** Create a new C# script named `CameraBrain.cs`.
2.  **Copy & Paste:** Copy the entire code below into `CameraBrain.cs`.
3.  **Attach to Main Camera:** Drag and drop the `CameraBrain.cs` script onto your main camera in the Unity Hierarchy.
4.  **Player/Target (Optional):** For the `FollowTargetCameraMode` to work, you'll need a GameObject in your scene to act as the target (e.g., your player character). Assign its `Transform` to the `CameraBrain`'s Inspector field named "Default Target".
5.  **Run:** Play your scene. The camera will start in the `FollowTargetCameraMode` (if a target is assigned).

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Not strictly needed for this example, but good practice

// --- NAMESPACE FOR ORGANIZATION ---
namespace DynamicCameraSystem
{
    // --- 1. ICameraMode INTERFACE (Strategy Interface) ---
    /// <summary>
    /// Interface for all camera modes. Defines the contract for how a camera mode
    /// should behave during its lifecycle (entering, updating, exiting).
    /// This is the 'Strategy' interface.
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>
        /// Called when this camera mode becomes active.
        /// Use for initialization, setting up initial camera state, or starting transitions.
        /// </summary>
        /// <param name="unityCamera">The actual Unity Camera component to control.</param>
        void Enter(Camera unityCamera);

        /// <summary>
        /// Called every frame while this camera mode is active.
        /// Contains the main camera movement, rotation, and logic for this specific mode.
        /// </summary>
        /// <param name="unityCamera">The actual Unity Camera component to control.</param>
        void UpdateMode(Camera unityCamera);

        /// <summary>
        /// Called when this camera mode is no longer active.
        /// Use for cleanup, resetting values, or preparing for the next mode.
        /// </summary>
        /// <param name="unityCamera">The actual Unity Camera component to control.</param>
        void Exit(Camera unityCamera);
    }

    // --- 2. DYNAMICCAMERASYSTEM CLASS (Context) ---
    /// <summary>
    /// The core system that manages the current camera mode.
    /// It's responsible for switching between modes and delegating update calls
    /// to the currently active mode. This is the 'Context' that uses the 'Strategy'.
    /// </summary>
    public class DynamicCameraSystem
    {
        private Camera _unityCamera;
        private ICameraMode _currentMode;

        public ICameraMode CurrentMode => _currentMode;

        /// <summary>
        /// Initializes the DynamicCameraSystem with the Unity Camera it will control.
        /// </summary>
        /// <param name="camera">The main Unity Camera component.</param>
        public DynamicCameraSystem(Camera camera)
        {
            _unityCamera = camera;
        }

        /// <summary>
        /// Switches the camera to a new mode.
        /// It calls Exit on the old mode (if any), sets the new mode, and then calls Enter on the new mode.
        /// </summary>
        /// <param name="newMode">The new camera mode to activate.</param>
        public void SwitchMode(ICameraMode newMode)
        {
            if (newMode == null)
            {
                Debug.LogError("Attempted to switch to a null camera mode.");
                return;
            }

            if (_currentMode != null)
            {
                _currentMode.Exit(_unityCamera);
                // Debug.Log($"Exiting {_currentMode.GetType().Name}");
            }

            _currentMode = newMode;
            _currentMode.Enter(_unityCamera);
            // Debug.Log($"Entering {_currentMode.GetType().Name}");
        }

        /// <summary>
        /// Called every frame from the CameraBrain. Delegates the update call to the current active mode.
        /// </summary>
        public void UpdateSystem()
        {
            _currentMode?.UpdateMode(_unityCamera);
        }
    }

    // --- 3. CONCRETE CAMERA MODES (Concrete Strategies) ---

    /// <summary>
    /// A camera mode that smoothly follows a target GameObject with an offset.
    /// </summary>
    public class FollowTargetCameraMode : ICameraMode
    {
        private Transform _target;
        private Vector3 _offset;
        private float _smoothSpeed;
        private Vector3 _lookAtOffset;

        /// <param name="target">The Transform to follow.</param>
        /// <param name="offset">The position offset from the target.</param>
        /// <param name="smoothSpeed">How smoothly the camera moves to the target (0-1, 1 is instant).</param>
        /// <param name="lookAtOffset">Offset from the target's position for the camera to look at.</param>
        public FollowTargetCameraMode(Transform target, Vector3 offset, float smoothSpeed = 0.125f, Vector3? lookAtOffset = null)
        {
            _target = target;
            _offset = offset;
            _smoothSpeed = Mathf.Clamp01(smoothSpeed);
            _lookAtOffset = lookAtOffset ?? Vector3.zero;
        }

        public void Enter(Camera unityCamera)
        {
            if (_target == null)
            {
                Debug.LogWarning("FollowTargetCameraMode entered without a target. Switching to fixed mode.");
                // As a fallback, maybe switch to a fixed mode or just default position
                unityCamera.transform.position = unityCamera.transform.position; // Stay put
                unityCamera.transform.rotation = unityCamera.transform.rotation;
                return;
            }

            // Optional: Snap to initial position or smoothly transition here
            // For simplicity, we'll let UpdateMode handle the initial smooth move.
        }

        public void UpdateMode(Camera unityCamera)
        {
            if (_target == null)
            {
                // Target might have been destroyed. This mode can't function.
                Debug.LogWarning("FollowTargetCameraMode target is null during UpdateMode. Consider switching camera modes.");
                return;
            }

            Vector3 desiredPosition = _target.position + _offset;
            Vector3 smoothedPosition = Vector3.Lerp(unityCamera.transform.position, desiredPosition, _smoothSpeed);
            unityCamera.transform.position = smoothedPosition;

            // Optional: Make camera look at the target (or a point slightly above/in front)
            unityCamera.transform.LookAt(_target.position + _lookAtOffset);
        }

        public void Exit(Camera unityCamera)
        {
            // No specific cleanup needed for this simple follow mode.
        }
    }

    /// <summary>
    /// A camera mode that moves to a specific position and looks at a point for a duration,
    /// then can automatically signal a switch back.
    /// </summary>
    public class CinematicCameraMode : ICameraMode
    {
        private Vector3 _cinematicPosition;
        private Vector3 _lookAtPoint;
        private float _duration;
        private float _startTime;
        private bool _isFinished;

        private Vector3 _initialCamPosition;
        private Quaternion _initialCamRotation;

        public bool IsFinished => _isFinished;

        /// <param name="cinematicPosition">The target position for the camera.</param>
        /// <param name="lookAtPoint">The point the camera should look at.</param>
        /// <param name="duration">How long the cinematic shot should last.</param>
        public CinematicCameraMode(Vector3 cinematicPosition, Vector3 lookAtPoint, float duration)
        {
            _cinematicPosition = cinematicPosition;
            _lookAtPoint = lookAtPoint;
            _duration = duration;
            _isFinished = false;
        }

        public void Enter(Camera unityCamera)
        {
            _startTime = Time.time;
            _isFinished = false;

            // Store current camera state to potentially return to it later
            _initialCamPosition = unityCamera.transform.position;
            _initialCamRotation = unityCamera.transform.rotation;

            // For simplicity, snap immediately to cinematic position
            unityCamera.transform.position = _cinematicPosition;
            unityCamera.transform.LookAt(_lookAtPoint);
            // For a smoother entry, you'd lerp towards cinematicPosition over a short time here.
        }

        public void UpdateMode(Camera unityCamera)
        {
            if (_isFinished) return; // Already finished, do nothing.

            unityCamera.transform.LookAt(_lookAtPoint); // Keep looking at the point

            if (Time.time >= _startTime + _duration)
            {
                _isFinished = true;
                // In a real scenario, you'd likely signal CameraBrain to switch back to a default mode.
                // For this example, we'll let CameraBrain check `IsFinished` or implicitly
                // switch after the duration.
                Debug.Log("Cinematic Camera Mode finished!");
            }
        }

        public void Exit(Camera unityCamera)
        {
            // Optional: Restore camera to where it was before the cinematic,
            // or perform a smooth transition back to the next mode.
            // For this example, we don't explicitly restore, as the next mode will take over.
        }
    }

    /// <summary>
    /// A camera mode that keeps the camera at a fixed position and rotation.
    /// </summary>
    public class FixedViewCameraMode : ICameraMode
    {
        private Vector3 _fixedPosition;
        private Quaternion _fixedRotation;

        public FixedViewCameraMode(Vector3 position, Quaternion rotation)
        {
            _fixedPosition = position;
            _fixedRotation = rotation;
        }

        public void Enter(Camera unityCamera)
        {
            // Snap the camera immediately to the fixed position and rotation
            unityCamera.transform.position = _fixedPosition;
            unityCamera.transform.rotation = _fixedRotation;
        }

        public void UpdateMode(Camera unityCamera)
        {
            // Ensure the camera stays fixed in case something tries to move it
            unityCamera.transform.position = _fixedPosition;
            unityCamera.transform.rotation = _fixedRotation;
        }

        public void Exit(Camera unityCamera)
        {
            // No specific cleanup needed.
        }
    }

    // --- 4. CAMERABRAIN MONOBEHAVIOUR (Client / Facade) ---
    /// <summary>
    /// The main MonoBehaviour attached to the Unity Camera.
    /// It acts as the client for the DynamicCameraSystem, providing an easy
    /// way for other scripts to interact with the camera system.
    /// Implements the Singleton pattern for easy global access.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraBrain : MonoBehaviour
    {
        public static CameraBrain Instance { get; private set; }

        [Header("Camera Settings")]
        [Tooltip("The default target for the FollowPlayerCameraMode.")]
        public Transform defaultTarget;
        [Tooltip("The offset from the target for the FollowPlayerCameraMode.")]
        public Vector3 followOffset = new Vector3(0, 5, -10);
        [Tooltip("How smoothly the camera follows the target (0-1, 1 is instant).")]
        [Range(0.01f, 1f)]
        public float followSmoothSpeed = 0.125f;
        [Tooltip("Offset for the point the camera looks at, relative to the target.")]
        public Vector3 followLookAtOffset = new Vector3(0, 1.5f, 0);

        private Camera _mainCamera;
        private DynamicCameraSystem _cameraSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _mainCamera = GetComponent<Camera>();
            _cameraSystem = new DynamicCameraSystem(_mainCamera);
        }

        private void Start()
        {
            // Set an initial camera mode when the game starts
            if (defaultTarget != null)
            {
                SetFollowMode(defaultTarget);
            }
            else
            {
                Debug.LogWarning("No default target assigned to CameraBrain. Camera will remain in its initial fixed position.");
                SetFixedMode(_mainCamera.transform.position, _mainCamera.transform.rotation);
            }
        }

        private void Update()
        {
            _cameraSystem.UpdateSystem();

            // Example of a cinematic mode automatically switching back
            if (_cameraSystem.CurrentMode is CinematicCameraMode cinematicMode)
            {
                if (cinematicMode.IsFinished)
                {
                    Debug.Log("Cinematic mode detected as finished. Switching back to follow mode (or a default).");
                    if (defaultTarget != null)
                    {
                        SetFollowMode(defaultTarget);
                    }
                    else // Fallback if no target is available
                    {
                        SetFixedMode(_mainCamera.transform.position, _mainCamera.transform.rotation);
                    }
                }
            }

            // --- EXAMPLE: Manual Mode Switching with Input ---
            // These are for demonstration. In a real game, these would be triggered
            // by game events, triggers, UI, or specific player actions.
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (defaultTarget != null)
                {
                    SetFollowMode(defaultTarget);
                    Debug.Log("Switched to Follow Camera Mode (Press 1)");
                }
                else
                {
                    Debug.LogWarning("Cannot switch to Follow Mode, defaultTarget is null.");
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                // Example cinematic view: look from above at a specific point
                Vector3 cinematicPos = new Vector3(0, 15, -10);
                Vector3 lookAtPoint = new Vector3(0, 0, 0);
                float cinematicDuration = 5f;
                SetCinematicMode(cinematicPos, lookAtPoint, cinematicDuration);
                Debug.Log($"Switched to Cinematic Camera Mode for {cinematicDuration}s (Press 2)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                // Example fixed view: good for menus or specific scene angles
                Vector3 fixedPos = new Vector3(10, 5, 10);
                Quaternion fixedRot = Quaternion.Euler(20, -135, 0);
                SetFixedMode(fixedPos, fixedRot);
                Debug.Log("Switched to Fixed Camera Mode (Press 3)");
            }
        }

        // --- PUBLIC METHODS TO SWITCH MODES ---

        /// <summary>
        /// Activates the FollowTargetCameraMode with the specified target and settings.
        /// </summary>
        public void SetFollowMode(Transform target)
        {
            _cameraSystem.SwitchMode(new FollowTargetCameraMode(target, followOffset, followSmoothSpeed, followLookAtOffset));
        }

        /// <summary>
        /// Activates the CinematicCameraMode.
        /// </summary>
        public void SetCinematicMode(Vector3 position, Vector3 lookAtPoint, float duration)
        {
            _cameraSystem.SwitchMode(new CinematicCameraMode(position, lookAtPoint, duration));
        }

        /// <summary>
        /// Activates the FixedViewCameraMode.
        /// </summary>
        public void SetFixedMode(Vector3 position, Quaternion rotation)
        {
            _cameraSystem.SwitchMode(new FixedViewCameraMode(position, rotation));
        }

        /// <summary>
        /// Gets the currently active camera mode.
        /// </summary>
        public ICameraMode GetCurrentCameraMode()
        {
            return _cameraSystem.CurrentMode;
        }

        // --- VISUALIZATION (Gizmos) ---
        private void OnDrawGizmos()
        {
            if (Instance == null) return;

            // Draw current follow target offset
            if (Application.isPlaying && _cameraSystem?.CurrentMode is FollowTargetCameraMode followMode)
            {
                // In play mode, we don't have direct access to internal fields of `followMode`
                // because it's an instance created at runtime. We'd need to expose them
                // or rely on the `defaultTarget` and `followOffset` from the Inspector for gizmos.
                if (defaultTarget != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(defaultTarget.position + followOffset, 0.5f);
                    Gizmos.DrawLine(defaultTarget.position, defaultTarget.position + followOffset);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(defaultTarget.position + followLookAtOffset, 0.2f);
                }
            }
            else if (defaultTarget != null) // Draw in edit mode based on inspector settings
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(defaultTarget.position + followOffset, 0.5f);
                Gizmos.DrawLine(defaultTarget.position, defaultTarget.position + followOffset);

                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(defaultTarget.position + followLookAtOffset, 0.2f);
            }
        }
    }
}
```