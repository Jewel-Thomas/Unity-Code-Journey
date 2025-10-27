// Unity Design Pattern Example: VRPresenceSystem
// This script demonstrates the VRPresenceSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script provides a complete and practical implementation of the 'VRPresenceSystem' design pattern. This pattern is particularly useful in VR development to centralize access to the player's core VR components (HMD, controllers, player root) and manage their presence state.

**VRPresenceSystem.cs**

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System;             // Required for Action, if using event Action instead of UnityEvent

/// <summary>
///     *** The VRPresenceSystem Design Pattern ***
///
///     Purpose:
///     The VRPresenceSystem pattern establishes a centralized, globally accessible system
///     (often implemented as a Singleton) responsible for managing and providing access
///     to the core components and state of the VR player's presence in the scene.
///     It acts as a single point of truth for the VR player's current position,
///     orientation (HMD), hand controllers, and potentially their avatar.
///
///     Why use it?
///     1.  **Single Point of Access:** Any part of your game (UI, game logic, interactions,
///         VFX) that needs to know where the player's head is, where their hands are,
///         or if their HMD is mounted, can get this information easily from one place.
///     2.  **Decoupling:** Game systems don't need direct references to the VR camera,
///         specific controller game objects, or the XR Origin. They interact with the
///         VRPresenceSystem, which abstracts away the specifics of the VR setup.
///     3.  **Event-Driven Reactions:** Provides events for significant VR presence changes
///         (e.g., HMD mounted/unmounted), allowing other systems to react without
///         constantly polling.
///     4.  **Maintainability:** If your VR setup changes (e.g., switching XR frameworks,
///         modifying player hierarchy), only the VRPresenceSystem needs to be updated,
///         not every script that uses player data.
///     5.  **Testability:** Easier to mock or simulate VR presence for testing purposes.
///
///     How it works:
///     1.  **Singleton:** A static 'Instance' property ensures there's only one active
///         VRPresenceSystem throughout the application, providing global access.
///     2.  **References:** It holds references to key VR player transforms: the overall
///         player root (e.g., XR Origin), the Head Mounted Display (HMD) camera, and
///         the left and right hand controllers. These are typically set up in the Unity Inspector.
///     3.  **Properties:** Provides public read-only properties to easily retrieve these
///         transforms.
///     4.  **Events:** Exposes `UnityEvent`s (or C# `event Action`s) that other scripts
///         can subscribe to, notifying them of changes in VR presence state (e.g., HMD status).
///     5.  **State Management:** Internally tracks and updates VR-specific states, like
///         whether the HMD is currently mounted.
///
///     Typical Use Cases:
///     -   A UI system needs to position a menu relative to the player's head.
///     -   An interaction system needs the exact location of the player's hands to perform raycasts.
///     -   Game logic needs to pause or change difficulty when the HMD is unmounted.
///     -   An avatar system needs to attach body parts to the HMD and hand transforms.
///     -   VFX systems need to spawn effects at the player's location or gaze direction.
///
///     This script is designed to be a complete, practical example. Drop it onto an
///     empty GameObject in your scene (e.g., named "VRPresenceManager"), and assign
///     the required Transforms in the Inspector.
/// </summary>
[DisallowMultipleComponent] // Ensures only one VRPresenceSystem can exist on a GameObject
public class VRPresenceSystem : MonoBehaviour
{
    // --- Singleton Instance ---
    // Public static property to provide global access to the single instance of VRPresenceSystem.
    public static VRPresenceSystem Instance { get; private set; }

    // --- Configuration (Inspector-visible properties for setting up VR player transforms) ---
    [Header("VR Player Transforms")]
    [Tooltip("The root transform of the VR player's GameObject hierarchy (e.g., the XR Origin).")]
    [SerializeField]
    private Transform playerRootTransform;

    [Tooltip("The transform representing the VR Head Mounted Display (HMD), typically the main camera.")]
    [SerializeField]
    private Transform hmdTransform;

    [Tooltip("The transform representing the VR Left Hand controller.")]
    [SerializeField]
    private Transform leftHandTransform;

    [Tooltip("The transform representing the VR Right Hand controller.")]
    [SerializeField]
    private Transform rightHandTransform;

    // --- Presence Events ---
    // UnityEvents allow other scripts to subscribe via the Inspector or code.
    [Header("Presence Events")]
    [Tooltip("Invoked when the VR HMD is detected as mounted (worn) by the player.")]
    public UnityEvent OnHMDMounted = new UnityEvent();

    [Tooltip("Invoked when the VR HMD is detected as unmounted (taken off) by the player.")]
    public UnityEvent OnHMDUnmounted = new UnityEvent();

    // Example of using C# events (alternative to UnityEvents for code-only subscriptions):
    // public event Action OnLeftControllerConnected;
    // public event Action OnRightControllerConnected;

    // --- Public Read-Only Properties for easy access to Transforms ---
    public Transform PlayerRoot => playerRootTransform;
    public Transform HMD => hmdTransform;
    public Transform LeftHand => leftHandTransform;
    public Transform RightHand => rightHandTransform;

    // --- Internal State ---
    private bool _isHmdMounted = false; // Tracks the current HMD mounted state.

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used for Singleton enforcement and initial setup.
    /// </summary>
    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance of VRPresenceSystem exists.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("VRPresenceSystem: Found multiple instances. Destroying this duplicate.", this);
            Destroy(this); // Destroy the new instance if one already exists.
            return;
        }
        Instance = this; // Assign this instance as the singleton.

        // Optional: If you want the VRPresenceSystem to persist across scene loads, uncomment this:
        // DontDestroyOnLoad(gameObject);

        ValidateTransforms(); // Check if essential transforms are assigned.
    }

    /// <summary>
    /// Called once per frame.
    /// Used here to continuously check and update the HMD mounted state.
    /// </summary>
    private void Update()
    {
        // --- HMD Mounted State Detection ---
        // In a real VR project using Unity's XR Input System, you would typically use:
        // InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        // bool currentHmdMounted = false;
        // if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.hmdMounted, out bool mounted))
        // {
        //     currentHmdMounted = mounted;
        // }
        //
        // For this example, we use a simple check: if the HMD transform is active in the hierarchy.
        // This is a simplification to avoid mandatory XR package dependencies, making the example
        // readily usable without specific XR setup. Adjust this logic for your actual VR platform.
        bool currentHmdMounted = hmdTransform != null && hmdTransform.gameObject.activeInHierarchy;

        // Check if the HMD mounted state has changed.
        if (currentHmdMounted != _isHmdMounted)
        {
            _isHmdMounted = currentHmdMounted; // Update the internal state.

            if (_isHmdMounted)
            {
                OnHMDMounted.Invoke(); // Trigger the HMD mounted event.
                Debug.Log("VRPresenceSystem: HMD Mounted event fired.");
            }
            else
            {
                OnHMDUnmounted.Invoke(); // Trigger the HMD unmounted event.
                Debug.Log("VRPresenceSystem: HMD Unmounted event fired.");
            }
        }
    }

    /// <summary>
    /// Called when the script is enabled or the object is reset in the editor.
    /// Provides convenience for auto-assigning common VR transforms.
    /// </summary>
    private void Reset()
    {
        // Attempt to auto-assign Player Root Transform
        // If this script is on the root of the VR player, assign itself.
        // Otherwise, you might search for an XROrigin or a specific player tag.
        if (playerRootTransform == null)
        {
            // Simple heuristic: If this script is at the scene root, assume it's the player root.
            if (transform.parent == null)
            {
                playerRootTransform = transform;
            }
            else
            {
                // If using XR Interaction Toolkit, you might try:
                // var xrOrigin = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XROrigin>();
                // if (xrOrigin != null) playerRootTransform = xrOrigin.transform;
            }
            if (playerRootTransform == null)
            {
                Debug.LogWarning("VRPresenceSystem: Could not auto-assign Player Root Transform. Please assign it manually in the Inspector.", this);
            }
        }

        // Attempt to auto-assign HMD Transform (usually the Main Camera)
        if (hmdTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                hmdTransform = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("VRPresenceSystem: Could not auto-assign HMD Transform (no 'MainCamera' tag found). Please assign it manually.", this);
            }
        }

        // Hand transforms are harder to auto-assign reliably without specific conventions
        // or XR system knowledge. It's usually best to assign them manually or through a
        // more sophisticated VR setup script.
    }

    // --- Public Methods (Utility functions for accessing VR player state) ---

    /// <summary>
    /// Checks if the HMD is currently considered mounted (worn) by the player.
    /// </summary>
    /// <returns>True if the HMD is mounted, false otherwise.</returns>
    public bool IsHMDMounted()
    {
        return _isHmdMounted;
    }

    /// <summary>
    /// Gets the current gaze origin of the VR player (position of the HMD).
    /// </summary>
    /// <returns>The world position of the HMD.</returns>
    public Vector3 GetGazeOrigin()
    {
        if (hmdTransform != null)
        {
            return hmdTransform.position;
        }
        Debug.LogWarning("VRPresenceSystem: HMD Transform not assigned, returning Vector3.zero for gaze origin.");
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the current gaze direction of the VR player (forward vector of the HMD).
    /// </summary>
    /// <returns>The forward direction vector of the HMD.</returns>
    public Vector3 GetGazeDirection()
    {
        if (hmdTransform != null)
        {
            return hmdTransform.forward;
        }
        Debug.LogWarning("VRPresenceSystem: HMD Transform not assigned, returning Vector3.forward for gaze direction.");
        return Vector3.forward;
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Validates that essential transforms are assigned and logs warnings if they are missing.
    /// </summary>
    private void ValidateTransforms()
    {
        if (playerRootTransform == null)
        {
            Debug.LogWarning("VRPresenceSystem: Player Root Transform is not assigned. Please assign it in the Inspector for full functionality.", this);
        }
        if (hmdTransform == null)
        {
            Debug.LogError("VRPresenceSystem: HMD Transform is not assigned. This is critical for VR presence. Please assign it in the Inspector.", this);
        }
        // Hand transforms are less critical for core presence, so warnings are sufficient.
        if (leftHandTransform == null)
        {
            Debug.LogWarning("VRPresenceSystem: Left Hand Transform is not assigned. Some systems might not function correctly.", this);
        }
        if (rightHandTransform == null)
        {
            Debug.LogWarning("VRPresenceSystem: Right Hand Transform is not assigned. Some systems might not function correctly.", this);
        }
    }
}


// --- EXAMPLE USAGE SCENARIOS ---
// These examples demonstrate how other scripts would interact with the VRPresenceSystem.
// In a real project, each example below would typically be in its own C# script file.

/*
// --------------------------------------------------------------------------------------------------------------------
// EXAMPLE 1: UIManager for positioning a floating UI panel in front of the player's gaze.
// Attach this script to a UI Canvas or UI Manager GameObject.
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject floatingPanel;
    [SerializeField] private float distanceFromHMD = 1.5f; // Distance from HMD to place UI.
    [SerializeField] private float smoothSpeed = 5f;     // How smoothly the UI follows.

    void Start()
    {
        if (floatingPanel == null)
        {
            Debug.LogError("UIManager: floatingPanel is not assigned! Please assign it in the Inspector.", this);
            enabled = false; // Disable script if essential reference is missing.
            return;
        }

        // You could also subscribe to VRPresenceSystem.Instance.OnHMDMounted to show/hide UI.
    }

    void Update()
    {
        // Always check if VRPresenceSystem.Instance is available before using it.
        // It might not be initialized yet, or might have been destroyed.
        if (VRPresenceSystem.Instance != null && VRPresenceSystem.Instance.IsHMDMounted())
        {
            // Get current HMD position and forward direction from VRPresenceSystem
            Vector3 targetPosition = VRPresenceSystem.Instance.GetGazeOrigin() +
                                     VRPresenceSystem.Instance.GetGazeDirection() * distanceFromHMD;

            // Get HMD rotation, often only interested in horizontal gaze for UI panels.
            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(VRPresenceSystem.Instance.GetGazeDirection().x, 0, VRPresenceSystem.Instance.GetGazeDirection().z));

            // Smoothly move and rotate the UI panel
            floatingPanel.transform.position = Vector3.Lerp(floatingPanel.transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            floatingPanel.transform.rotation = Quaternion.Slerp(floatingPanel.transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);

            if (!floatingPanel.activeSelf) floatingPanel.SetActive(true);
        }
        else
        {
            // If HMD is not mounted or system not ready, hide the UI for better immersion/performance.
            if (floatingPanel.activeSelf) floatingPanel.SetActive(false);
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// EXAMPLE 2: GameManager reacting to HMD mounted/unmounted events.
// Attach this script to your main Game Manager GameObject.
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject gameWorldEnvironment; // E.g., disable/enable for performance/pause.
    [SerializeField] private GameObject pauseMenuCanvas;      // Show a pause menu when HMD is off.

    // Using OnEnable and OnDisable to manage event subscriptions is a best practice
    // to prevent memory leaks and ensure subscriptions are active only when needed.
    void OnEnable()
    {
        // Subscribe to VRPresenceSystem events when this script is enabled.
        if (VRPresenceSystem.Instance != null)
        {
            VRPresenceSystem.Instance.OnHMDMounted.AddListener(OnHMDMounted);
            VRPresenceSystem.Instance.OnHMDUnmounted.AddListener(OnHMDUnmounted);
        }
    }

    void OnDisable()
    {
        // Unsubscribe from VRPresenceSystem events when this script is disabled to prevent memory leaks.
        if (VRPresenceSystem.Instance != null)
        {
            VRPresenceSystem.Instance.OnHMDMounted.RemoveListener(OnHMDMounted);
            VRPresenceSystem.Instance.OnHMDUnmounted.RemoveListener(OnHMDUnmounted);
        }
    }

    private void OnHMDMounted()
    {
        Debug.Log("GameManager: Player has mounted HMD. Resuming game!");
        // Example actions:
        if (gameWorldEnvironment != null) gameWorldEnvironment.SetActive(true);
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1.0f; // Resume game time.
    }

    private void OnHMDUnmounted()
    {
        Debug.Log("GameManager: Player has unmounted HMD. Pausing game!");
        // Example actions:
        if (gameWorldEnvironment != null) gameWorldEnvironment.SetActive(false);
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(true);
        Time.timeScale = 0.0f; // Pause game time.
    }
}

// --------------------------------------------------------------------------------------------------------------------
// EXAMPLE 3: VR Interaction System using hand transforms.
// Attach this script to an interaction manager GameObject.
// This is a simplified example; a real interaction system would be more complex.
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

public class VRInteractionManager : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 0.1f; // Max distance for an object to be considered "grabbed".
    [SerializeField] private LayerMask interactableLayer; // Define a layer for interactable objects.

    private GameObject _grabbedObject = null;
    private Transform _grabbingHand = null;

    void Update()
    {
        if (VRPresenceSystem.Instance == null) return;

        // Simplified input for demonstration. In a real VR project, use XR Input Actions or direct Input Devices.
        // Example: InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.GripButton, out bool pressed);
        bool leftGrabInput = Input.GetKey(KeyCode.A); // Placeholder for Left hand grab input (e.g., 'A' key)
        bool rightGrabInput = Input.GetKey(KeyCode.D); // Placeholder for Right hand grab input (e.g., 'D' key)

        if (_grabbedObject == null)
        {
            // Try to grab with left hand if input is pressed and left hand transform is available
            if (leftGrabInput && VRPresenceSystem.Instance.LeftHand != null)
            {
                TryGrab(VRPresenceSystem.Instance.LeftHand);
            }
            // Try to grab with right hand if input is pressed and right hand transform is available
            else if (rightGrabInput && VRPresenceSystem.Instance.RightHand != null)
            {
                TryGrab(VRPresenceSystem.Instance.RightHand);
            }
        }
        else
        {
            // If an object is grabbed, keep it attached to the grabbing hand.
            _grabbedObject.transform.position = _grabbingHand.position;
            _grabbedObject.transform.rotation = _grabbingHand.rotation;

            // Check for release input.
            bool releaseLeft = Input.GetKeyUp(KeyCode.A);
            bool releaseRight = Input.GetKeyUp(KeyCode.D);

            if (_grabbingHand == VRPresenceSystem.Instance.LeftHand && releaseLeft)
            {
                ReleaseGrab();
            }
            else if (_grabbingHand == VRPresenceSystem.Instance.RightHand && releaseRight)
            {
                ReleaseGrab();
            }
        }
    }

    private void TryGrab(Transform handTransform)
    {
        RaycastHit hit;
        // Cast a small sphere from the hand to detect nearby interactables.
        // Ensure your interactable objects are on the 'interactableLayer'.
        if (Physics.SphereCast(handTransform.position, 0.05f, handTransform.forward, out hit, interactionDistance, interactableLayer))
        {
            Debug.Log($"Grabbed {hit.collider.name} with {handTransform.name}");
            _grabbedObject = hit.collider.gameObject;
            _grabbingHand = handTransform;

            // Parent the grabbed object to the hand for direct following.
            _grabbedObject.transform.SetParent(handTransform);

            // Make the grabbed object kinematic to prevent physics interactions while held.
            Rigidbody rb = _grabbedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    private void ReleaseGrab()
    {
        if (_grabbedObject == null) return;

        Debug.Log($"Released {_grabbedObject.name}");

        // Remove parenting from the hand.
        _grabbedObject.transform.SetParent(null);

        // Restore rigid body settings to allow physics interactions again.
        Rigidbody rb = _grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        _grabbedObject = null;
        _grabbingHand = null;
    }
}
*/
```