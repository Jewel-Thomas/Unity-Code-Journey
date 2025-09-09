// Unity Design Pattern Example: CameraCinematicSystem
// This script demonstrates the CameraCinematicSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Camera Cinematic System** design pattern. This pattern provides a centralized manager for orchestrating different camera "shots" during cinematic sequences, allowing for flexible transitions and custom camera behaviors.

**Key Components of the Pattern:**

1.  **`ICinematicCamera` (Interface):** Defines a contract for any custom camera behavior script (e.g., a follow camera, an orbit camera, or a wrapper for a Cinemachine Virtual Camera) to be managed by the system. This promotes polymorphism, allowing the system to work with various camera types.
2.  **`CinematicShot` (Serializable Class):** Represents a single, distinct camera view or action within a cinematic. It holds configuration for a specific camera (e.g., its GameObject, duration, and events). Being `[System.Serializable]` allows you to configure these shots directly in the Unity Inspector.
3.  **`CameraCinematicSystem` (MonoBehaviour Singleton):** The core manager. It holds a sequence of `CinematicShot` objects and provides methods to start, stop, and manage the playback of the cinematic. It also handles enabling/disabling the main game camera during the sequence and orchestrates transitions between shots (though simple for this example, it can be extended for sophisticated blends).
4.  **`ExampleOrbitCamera` (Optional Example Implementation):** A concrete example of a custom camera script that implements `ICinematicCamera`, showcasing how different camera behaviors can be integrated.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent for custom callbacks

// --- 1. ICinematicCamera Interface ---
// This interface defines a contract for any custom camera behavior
// that should be managed by the CameraCinematicSystem.
// By implementing this interface, different camera scripts (e.g., custom follow,
// orbit, or even a wrapper for Cinemachine Virtual Cameras) can be
// generically enabled and disabled by the central system.
public interface ICinematicCamera
{
    /// <summary>
    /// Called when this specific camera shot should become active.
    /// Implementations should activate their camera component and any associated logic.
    /// </summary>
    void EnableCamera();

    /// <summary>
    /// Called when this specific camera shot should become inactive.
    /// Implementations should deactivate their camera component and any associated logic.
    /// </summary>
    void DisableCamera();

    /// <summary>
    /// Returns the GameObject associated with this camera.
    /// Useful for the CameraCinematicSystem to manage the root GameObject's active state.
    /// </summary>
    GameObject GetCameraGameObject();
}

// --- 2. CinematicShot Class ---
// This class represents a single "shot" or camera view within a cinematic sequence.
// It's marked [System.Serializable] so it can be configured directly in the Unity Inspector
// as part of a list within the CameraCinematicSystem.
[System.Serializable]
public class CinematicShot
{
    [Tooltip("A friendly name for this shot (e.g., 'Opening establishing shot', 'Character close-up').")]
    public string shotName = "New Shot";

    [Tooltip("The GameObject containing the camera component (or a custom ICinematicCamera script) " +
             "for this shot. This GameObject will be activated/deactivated.")]
    public GameObject cameraGameObject;

    [Tooltip("How long this shot should be active in seconds.")]
    [Min(0.1f)] public float shotDuration = 3.0f;

    [Tooltip("Optional: The duration for fading or blending into this shot. " +
             "Currently, this example only switches abruptly, but you could " +
             "extend the CameraCinematicSystem to support smooth transitions using this value.")]
    [Min(0f)] public float transitionDuration = 0.5f;

    [Header("Shot Events")]
    [Tooltip("Unity Events invoked when this specific shot begins. " +
             "Useful for playing sounds, triggering animations, showing UI, etc.")]
    public UnityEvent onShotStart;

    [Tooltip("Unity Events invoked when this specific shot ends. " +
             "Useful for stopping sounds, hiding UI, etc.")]
    public UnityEvent onShotEnd;

    // --- Internal Helpers for Shot Activation/Deactivation ---
    // These methods provide a unified way for the CameraCinematicSystem
    // to activate/deactivate the camera associated with this shot,
    // handling different types of camera setups (standard Camera or ICinematicCamera).

    /// <summary>
    /// Activates the camera associated with this shot.
    /// It first activates the root GameObject, then prioritizes ICinematicCamera implementations,
    /// falls back to standard Unity Cameras, and finally invokes custom start events.
    /// </summary>
    public void ActivateShot()
    {
        if (cameraGameObject == null)
        {
            Debug.LogWarning($"CinematicShot '{shotName}' has no cameraGameObject assigned. Cannot activate.", null);
            return;
        }

        // Always activate the root GameObject first to ensure components are accessible and active.
        cameraGameObject.SetActive(true);

        ICinematicCamera cinematicCam = cameraGameObject.GetComponent<ICinematicCamera>();
        if (cinematicCam != null)
        {
            cinematicCam.EnableCamera();
        }
        else
        {
            Camera standardCam = cameraGameObject.GetComponent<Camera>();
            if (standardCam != null)
            {
                standardCam.enabled = true;
            }
            else
            {
                Debug.LogWarning($"CinematicShot '{shotName}' cameraGameObject has neither an " +
                                 $"ICinematicCamera nor a standard Camera component. " +
                                 $"Only the GameObject itself was activated.", cameraGameObject);
            }
        }
        onShotStart?.Invoke(); // Invoke custom start events
    }

    /// <summary>
    /// Deactivates the camera associated with this shot.
    /// It deactivates ICinematicCamera implementations or standard Unity Cameras,
    /// then deactivates the root GameObject, and finally invokes custom end events.
    /// </summary>
    public void DeactivateShot()
    {
        if (cameraGameObject == null) return;

        ICinematicCamera cinematicCam = cameraGameObject.GetComponent<ICinematicCamera>();
        if (cinematicCam != null)
        {
            cinematicCam.DisableCamera();
        }
        else
        {
            Camera standardCam = cameraGameObject.GetComponent<Camera>();
            if (standardCam != null)
            {
                standardCam.enabled = false;
            }
        }
        // Always deactivate the root GameObject last to ensure it's fully off.
        cameraGameObject.SetActive(false);
        onShotEnd?.Invoke(); // Invoke custom end events
    }
}

// --- 3. CameraCinematicSystem MonoBehaviour ---
// This is the core manager for cinematic camera sequences.
// It acts as a central point to define, start, and stop cinematics,
// managing transitions between different camera shots. It uses a singleton
// pattern for easy global access.
public class CameraCinematicSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Allows easy access to the CameraCinematicSystem from any other script
    // (e.g., CameraCinematicSystem.Instance.StartCinematic()).
    public static CameraCinematicSystem Instance { get; private set; }

    [Header("Cinematic Settings")]
    [Tooltip("The main game camera that should be active when no cinematic is running. " +
             "It will be disabled during the cinematic and re-enabled afterward.")]
    [SerializeField] private Camera mainGameCamera;

    [Tooltip("If true, the mainGameCamera will be disabled when a cinematic starts " +
             "and re-enabled when it ends. If false, the main camera's state " +
             "is left as-is (e.g., if you're using a separate camera manager or Cinemachine brain).")]
    [SerializeField] private bool disableMainCameraDuringCinematic = true;

    [Tooltip("A list of cinematic shots that make up this sequence. " +
             "Define the order, duration, and camera GameObject for each shot.")]
    [SerializeField] private List<CinematicShot> cinematicShots = new List<CinematicShot>();

    [Header("System Events")]
    [Tooltip("Unity Events invoked when the entire cinematic sequence starts.")]
    public UnityEvent onCinematicStart;

    [Tooltip("Unity Events invoked when the entire cinematic sequence ends.")]
    public UnityEvent onCinematicEnd;

    // --- Internal State ---
    private int _currentShotIndex = -1; // Tracks the currently active shot in the sequence
    private Coroutine _cinematicRoutine; // Reference to the running coroutine for stopping it
    
    /// <summary>
    /// True if a cinematic sequence is currently active, false otherwise.
    /// </summary>
    public bool IsCinematicActive { get; private set; } = false;

    // --- MonoBehaviour Lifecycle Methods ---
    private void Awake()
    {
        // Implement the singleton pattern to ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this; // Set this instance as the singleton
            // Optionally, make this object persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }

        // Initially ensure all cinematic cameras are off to avoid conflicts
        // if they were accidentally left active in the scene.
        foreach (var shot in cinematicShots)
        {
            shot.DeactivateShot(); // Use DeactivateShot to ensure cleanup
        }
    }

    private void OnDestroy()
    {
        // Clean up the singleton reference if this object is destroyed.
        if (Instance == this)
        {
            Instance = null;
        }
        // Ensure any running cinematic is stopped if the manager is destroyed.
        if (IsCinematicActive)
        {
            StopCinematic();
        }
    }

    // --- Public API for Controlling Cinematics ---

    /// <summary>
    /// Starts the cinematic sequence.
    /// If a cinematic is already running, it will be stopped and restarted.
    /// </summary>
    public void StartCinematic()
    {
        if (cinematicShots.Count == 0)
        {
            Debug.LogWarning("CameraCinematicSystem: No cinematic shots defined. Cannot start cinematic.", this);
            return;
        }

        if (IsCinematicActive)
        {
            Debug.Log("CameraCinematicSystem: Cinematic already active, stopping and restarting.", this);
            StopCinematic(); // Stop any currently running cinematic before starting a new one
        }

        IsCinematicActive = true;
        onCinematicStart?.Invoke(); // Trigger overall cinematic start event

        // Disable the main game camera if configured
        if (disableMainCameraDuringCinematic && mainGameCamera != null)
        {
            mainGameCamera.enabled = false;
        }
        else if (disableMainCameraDuringCinematic && mainGameCamera == null)
        {
            Debug.LogWarning("CameraCinematicSystem: 'Disable Main Camera During Cinematic' is true, " +
                             "but no Main Game Camera is assigned! The main camera will remain active.", this);
        }

        _currentShotIndex = -1; // Reset index for the new sequence
        _cinematicRoutine = StartCoroutine(CinematicSequenceCoroutine());
        Debug.Log("CameraCinematicSystem: Cinematic sequence started.", this);
    }

    /// <summary>
    /// Stops the currently running cinematic sequence immediately.
    /// Deactivates the current shot and re-enables the main game camera.
    /// </summary>
    public void StopCinematic()
    {
        if (!IsCinematicActive)
        {
            Debug.Log("CameraCinematicSystem: No cinematic is currently active to stop.", this);
            return;
        }

        if (_cinematicRoutine != null)
        {
            StopCoroutine(_cinematicRoutine);
            _cinematicRoutine = null;
        }

        // Deactivate the currently active shot (if any)
        if (_currentShotIndex >= 0 && _currentShotIndex < cinematicShots.Count)
        {
            cinematicShots[_currentShotIndex].DeactivateShot();
        }

        // Re-enable the main game camera
        if (disableMainCameraDuringCinematic && mainGameCamera != null)
        {
            mainGameCamera.enabled = true;
        }

        IsCinematicActive = false;
        onCinematicEnd?.Invoke(); // Trigger overall cinematic end event
        Debug.Log("CameraCinematicSystem: Cinematic stopped.", this);
    }

    /// <summary>
    /// Coroutine that handles the sequential playback of cinematic shots.
    /// It iterates through the defined shots, activates each for its duration,
    /// and then deactivates it before moving to the next.
    /// </summary>
    private IEnumerator CinematicSequenceCoroutine()
    {
        for (_currentShotIndex = 0; _currentShotIndex < cinematicShots.Count; _currentShotIndex++)
        {
            CinematicShot currentShot = cinematicShots[_currentShotIndex];

            // Safety check for assigned camera GameObject
            if (currentShot.cameraGameObject == null)
            {
                Debug.LogWarning($"CinematicSystem: Shot '{currentShot.shotName}' at index {_currentShotIndex} " +
                                 $"has no cameraGameObject assigned. Skipping this shot.", this);
                continue;
            }

            Debug.Log($"CameraCinematicSystem: Activating shot '{currentShot.shotName}' for {currentShot.shotDuration}s.", this);

            // Deactivate the *previous* shot before activating the current one.
            // This ensures only one cinematic camera is active at a time.
            if (_currentShotIndex > 0)
            {
                cinematicShots[_currentShotIndex - 1].DeactivateShot();
            }

            // Activate the current shot using its defined activation logic.
            currentShot.ActivateShot();

            // Wait for the shot's defined duration.
            yield return new WaitForSeconds(currentShot.shotDuration);
        }

        // If the loop completes, the cinematic sequence finished naturally.
        StopCinematic();
    }
}

// --- 4. Example Custom Cinematic Camera (Optional but Recommended) ---
// This is an example of a custom camera behavior that implements ICinematicCamera.
// You can attach this script to a GameObject, ensure it also has a standard
// Camera component (it will add one if missing), and then assign this GameObject
// to a CinematicShot's 'Camera Game Object' field in the CameraCinematicSystem.
public class ExampleOrbitCamera : MonoBehaviour, ICinematicCamera
{
    [Tooltip("The target GameObject this camera should orbit around.")]
    public Transform target;

    [Tooltip("Distance from the target.")]
    public float distance = 5.0f;

    [Tooltip("Orbit speed in degrees per second.")]
    public float orbitSpeed = 30.0f;

    [Tooltip("Vertical offset from the target.")]
    public float heightOffset = 1.0f;

    private bool _isActive = false; // Internal flag to control LateUpdate logic
    private Camera _cameraComponent; // Reference to the standard Camera component

    void Awake()
    {
        _cameraComponent = GetComponent<Camera>();
        if (_cameraComponent == null)
        {
            _cameraComponent = gameObject.AddComponent<Camera>();
            Debug.LogWarning("ExampleOrbitCamera: No Camera component found, adding one.", this);
        }
        // Ensure the camera and its GameObject are initially off.
        // The CameraCinematicSystem will activate them when needed.
        _cameraComponent.enabled = false;
        gameObject.SetActive(false);
    }

    // LateUpdate is typically used for camera logic to ensure it runs after
    // all other object movements in the frame.
    void LateUpdate()
    {
        if (!_isActive || target == null) return;

        // --- Orbit Logic ---
        // Rotate the camera around the target.
        transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);

        // Maintain desired distance and height from the target.
        Vector3 desiredPosition = target.position + (transform.position - target.position).normalized * distance;
        desiredPosition.y = target.position.y + heightOffset; // Apply vertical offset
        transform.position = desiredPosition;

        // Always look at the target.
        transform.LookAt(target.position);
    }

    /// <summary>
    /// Implements ICinematicCamera.EnableCamera(). Activates this camera.
    /// </summary>
    public void EnableCamera()
    {
        _isActive = true;
        _cameraComponent.enabled = true;
        gameObject.SetActive(true); // Ensure GameObject is active
        Debug.Log($"ExampleOrbitCamera: {gameObject.name} Enabled.", this);
    }

    /// <summary>
    /// Implements ICinematicCamera.DisableCamera(). Deactivates this camera.
    /// </summary>
    public void DisableCamera()
    {
        _isActive = false;
        _cameraComponent.enabled = false;
        gameObject.SetActive(false); // Ensure GameObject is inactive
        Debug.Log($"ExampleOrbitCamera: {gameObject.name} Disabled.", this);
    }

    /// <summary>
    /// Implements ICinematicCamera.GetCameraGameObject().
    /// </summary>
    public GameObject GetCameraGameObject()
    {
        return gameObject;
    }
}


/*
// --- Example Usage: How to Set Up and Trigger the Camera Cinematic System ---

// 1. Create a new C# script named "CameraCinematicSystem.cs" and paste ALL the code above into it.
//    (This includes ICinematicCamera, CinematicShot, CameraCinematicSystem, and ExampleOrbitCamera).

// 2. Create an Empty GameObject in your Unity scene, name it "CinematicManager".
//    Add the "CameraCinematicSystem" component to it.

// 3. Assign your main game camera:
//    - Drag your primary game camera (e.g., "Main Camera" in a new scene) from the Hierarchy
//      to the "Main Game Camera" slot in the "CinematicManager" Inspector.
//    - Decide if you want "Disable Main Camera During Cinematic" checked (usually yes).

// 4. Create several new Empty GameObjects for your individual cinematic shots:
//    - Right-click in Hierarchy -> Create Empty. Name it "Shot1_StaticView".
//    - Add a "Camera" component to "Shot1_StaticView" if it doesn't have one.
//    - Position and rotate "Shot1_StaticView" to your desired first shot's perspective.
//    - Repeat for "Shot2_CloseUp", "Shot3_Panorama", etc., creating as many shots as you need.

//    - For an advanced example using the custom camera script:
//      - Create an Empty GameObject, name it "Shot4_OrbitCamera".
//      - Add the "ExampleOrbitCamera.cs" script to it.
//      - (It will automatically add a Camera component if one isn't present).
//      - Assign a `Transform` (e.g., your player character, or any object you want to orbit)
//        to its "Target" field in the Inspector. Adjust "Distance", "Orbit Speed", "Height Offset" as desired.

// 5. Configure the Cinematic Shots in the "CinematicManager" Inspector:
//    - Expand the "Cinematic Shots" list.
//    - Increase its "Size" to match the number of cinematic shots you've created.
//    - For each element in the list:
//      - Drag one of your "ShotX" GameObjects (e.g., "Shot1_StaticView", "Shot2_CloseUp", "Shot4_OrbitCamera")
//        to the "Camera Game Object" slot.
//      - Set a "Shot Duration" (e.g., 4 seconds for a wide shot, 2 seconds for a quick close-up).
//      - Optionally, add UnityEvents for "On Shot Start" / "On Shot End" to trigger custom logic.
//        (e.g., play a sound, show narrative text, trigger an animation on a character).

// 6. To trigger the cinematic, you can use a simple UI Button or a game trigger script:

//    --- Example Trigger Script (e.g., attach to a UI Button or a physical collider trigger) ---
//    // Create a new C# script named "CinematicTrigger.cs"
//    using UnityEngine;

//    public class CinematicTrigger : MonoBehaviour
//    {
//        [Tooltip("Reference to the CameraCinematicSystem. Can be left null if using singleton.")]
//        public CameraCinematicSystem cinematicSystem;

//        void Start()
//        {
//            // If not assigned in Inspector, try to get the singleton instance
//            if (cinematicSystem == null)
//            {
//                cinematicSystem = CameraCinematicSystem.Instance;
//            }
//            if (cinematicSystem == null)
//            {
//                Debug.LogError("CinematicTrigger: No CameraCinematicSystem found in scene or assigned. " +
//                               "Please ensure 'CinematicManager' GameObject exists and has the component.", this);
//            }
//        }

//        // Public method to be called by a UI Button's OnClick event
//        public void TriggerCinematic()
//        {
//            if (cinematicSystem != null && !cinematicSystem.IsCinematicActive)
//            {
//                Debug.Log("Triggering cinematic from UI Button.");
//                cinematicSystem.StartCinematic();
//            }
//        }

//        // Example for a physical trigger (e.g., player walks into a collider)
//        void OnTriggerEnter(Collider other)
//        {
//            // Assuming your player has the tag "Player"
//            if (other.CompareTag("Player") && cinematicSystem != null && !cinematicSystem.IsCinematicActive)
//            {
//                Debug.Log($"Triggering cinematic by {other.name} entering collider.");
//                cinematicSystem.StartCinematic();
//            }
//        }
//    }
//    ---------------------------------------------------------------------------------

// 7. Run the scene. When you trigger the cinematic (e.g., by clicking a UI button with
//    the `CinematicTrigger.TriggerCinematic()` method assigned), you should see the
//    camera sequence play out. The main game camera will be disabled, and each cinematic
//    shot will become active for its defined duration before the next one starts.
//    Finally, the main game camera will be re-enabled.

*/
```