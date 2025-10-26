// Unity Design Pattern Example: VRGestureSystem
// This script demonstrates the VRGestureSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a 'VR Gesture System' design pattern in Unity. The pattern aims to provide a flexible and extensible way to define, recognize, and react to specific hand movements or controller interactions in a VR environment.

**Core Design Patterns Used:**

1.  **Scriptable Object for Configuration:** `VRGestureDefinition` allows designers to create, configure, and manage different gestures as assets directly in the Unity Editor, promoting data-driven design and easy iteration.
2.  **Observer Pattern (via UnityEvents):** The `OnGestureRecognized` UnityEvent allows any other component to "subscribe" to gesture recognition notifications without needing to know the internal workings of the gesture system, promoting loose coupling.
3.  **Strategy Pattern (Implicit/Delineated):** While not using explicit `IGestureRecognizer` interfaces for each strategy (for simplicity in a single-file example), the `switch` statement in `ProcessInput` and the separate `HandleSwipeGesture`/`HandleHoldGesture` methods clearly delineate different recognition algorithms (strategies) for different `GestureType`s. This structure makes it easy to extend with new gesture types by adding new cases and handler methods.

---

**File 1: `VRGestureDefinition.cs` (ScriptableObject)**
This script defines what a gesture *is*. It's a ScriptableObject, allowing you to create instances as assets in your Unity project, each representing a unique gesture with its own parameters.

```csharp
// VRGestureDefinition.cs
using UnityEngine;

/// <summary>
/// Defines the different types of gestures our system can recognize.
/// Each type will have a specific recognition logic (strategy).
/// </summary>
public enum GestureType
{
    Swipe, // A directional movement over a certain distance
    Hold   // Holding a button/pose relatively still for a duration
    // Add more gesture types here (e.g., Circle, Pinch, PoseMatch, etc.)
}

/// <summary>
/// A ScriptableObject that defines the properties of a single VR gesture.
/// This allows designers to create and configure gestures as assets in the Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewVRGestureDefinition", menuName = "VR Gesture System/Gesture Definition")]
public class VRGestureDefinition : ScriptableObject
{
    [Header("General Gesture Properties")]
    [Tooltip("The unique name of the gesture. Used for identification when recognized.")]
    public string gestureName = "New Gesture";

    [Tooltip("The type of recognition logic this gesture uses.")]
    public GestureType type = GestureType.Swipe;

    [Tooltip("The threshold value for recognition. Its meaning depends on the gesture type.\n" +
             "E.g., for Swipe: minimum distance (meters); for Hold: minimum time (seconds).")]
    public float recognitionThreshold = 0.2f;

    [Header("Swipe Gesture Properties")]
    [Tooltip("For Swipe gestures: The normalized direction vector of the swipe.\n" +
             "E.g., (0, 1, 0) for an upward swipe, (0, 0, 1) for a forward swipe.")]
    public Vector3 swipeDirection = Vector3.forward;

    [Header("Hold Gesture Properties")]
    [Tooltip("For Hold gestures: Maximum allowed movement (in meters) of the hand during a hold " +
             "for it to still be considered 'stationary'.")]
    public float maxHoldMovementTolerance = 0.1f;
    
    // You can add more gesture-specific parameters here as you add new GestureTypes.
    // Example for a "Circle" gesture:
    // public float minCircleRadius = 0.1f;
    // public int minCircleSegments = 8;
}
```

---

**File 2: `VRGestureSystem.cs` (Core System MonoBehaviour)**
This is the main component that you'll add to a GameObject in your scene. It processes input from a VR controller (or simulates it) and uses the `VRGestureDefinition` assets to recognize gestures.

```csharp
// VRGestureSystem.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System; // For basic System types like EventArgs if needed

/// <summary>
/// Internal class to track the ongoing state of a potential gesture recognition for a single definition.
/// </summary>
[System.Serializable] // Made serializable for potential debugging in Inspector, not strictly necessary
public class GestureRecognitionState
{
    public VRGestureDefinition gestureDefinition; // Reference to the gesture being tracked
    public bool isActive;                        // Is this gesture currently being formed?
    public Vector3 startPosition;               // Hand position when the gesture started
    public float startTime;                     // Time when the gesture started
    public float currentProgressValue;          // Current progress towards the threshold (e.g., distance, time)
    public Vector3 lastKnownPosition;           // Last position used for movement comparison (e.g., for hold stability)
    public float currentStationaryTime;         // Tracks how long the hand has been still for hold gestures

    public GestureRecognitionState(VRGestureDefinition definition)
    {
        gestureDefinition = definition;
        Reset();
    }

    /// <summary>
    /// Resets the state of this gesture recognition, stopping any active tracking.
    /// </summary>
    public void Reset()
    {
        isActive = false;
        startPosition = Vector3.zero;
        startTime = 0f;
        currentProgressValue = 0f;
        lastKnownPosition = Vector3.zero;
        currentStationaryTime = 0f;
    }
}

/// <summary>
/// The core VR Gesture System. This MonoBehaviour processes VR input (hand position, trigger state)
/// and compares it against a list of defined gestures to recognize them.
/// It uses a strategy-like approach to handle different gesture types and broadcasts
/// recognized gestures via a UnityEvent.
/// </summary>
public class VRGestureSystem : MonoBehaviour
{
    [Header("Gesture Definitions")]
    [Tooltip("Assign the ScriptableObject gesture definitions here that this system should try to recognize.")]
    public List<VRGestureDefinition> gesturesToRecognize = new List<VRGestureDefinition>();

    [Header("Input Source (for simulation or direct link)")]
    [Tooltip("The Transform representing the hand controller (e.g., LeftHandAnchor, RightHandAnchor). " +
             "If assigned, its position will be used for input. If null, external calls to SetHandPosition " +
             "are required for input.")]
    public Transform handControllerTransform;

    [Tooltip("Maximum time allowed for a gesture to complete from its start. If exceeded, the gesture attempt resets.")]
    public float gestureWindowTime = 1.5f;

    [Header("Events")]
    [Tooltip("Event fired when a gesture is successfully recognized. Passes the gesture's name as a string.")]
    public UnityEvent<string> OnGestureRecognized;

    // Internal state management for each gesture definition being tracked.
    private Dictionary<VRGestureDefinition, GestureRecognitionState> recognitionStates;

    // Last frame's input state, used when no handControllerTransform is provided
    // or for internal comparison (e.g., triggerJustPressed/Released).
    private Vector3 _lastInputPosition;
    private bool _lastInputTriggerState;

    /// <summary>
    /// Call this method from your VR input manager (e.g., XR Interaction Toolkit, OVRInput)
    /// to provide the current world position of the VR hand controller.
    /// This is used if `handControllerTransform` is not assigned.
    /// </summary>
    /// <param name="position">The current world position of the hand controller.</param>
    public void SetHandPosition(Vector3 position)
    {
        _lastInputPosition = position;
    }

    /// <summary>
    /// Call this method from your VR input manager to provide the current state of the
    /// primary trigger button on the VR hand controller.
    /// This is used if `handControllerTransform` is not assigned.
    /// </summary>
    /// <param name="pressed">True if the trigger button is currently pressed.</param>
    public void SetTriggerState(bool pressed)
    {
        _lastInputTriggerState = pressed;
    }

    private void Awake()
    {
        InitializeGestureSystem();
    }

    /// <summary>
    /// Initializes the internal state tracking for all defined gestures.
    /// </summary>
    private void InitializeGestureSystem()
    {
        recognitionStates = new Dictionary<VRGestureDefinition, GestureRecognitionState>();
        foreach (var gestureDef in gesturesToRecognize)
        {
            if (gestureDef == null)
            {
                Debug.LogWarning("VRGestureSystem: A null gesture definition found in gesturesToRecognize list. " +
                                 "Please ensure all gesture assets are properly assigned.", this);
                continue;
            }
            recognitionStates.Add(gestureDef, new GestureRecognitionState(gestureDef));
        }

        // Initialize last input position to current if transform exists, or default
        _lastInputPosition = (handControllerTransform != null) ? handControllerTransform.position : Vector3.zero;
        _lastInputTriggerState = false;
    }

    private void Update()
    {
        Vector3 currentFramePosition;
        bool currentFrameTriggerState;

        // --- Input Source Selection ---
        // Option A: Use a direct Transform reference for simulation or simpler setups.
        // This will also use Input.GetMouseButton(0) for trigger simulation.
        if (handControllerTransform != null)
        {
            currentFramePosition = handControllerTransform.position;
            currentFrameTriggerState = Input.GetMouseButton(0); // Simulate trigger with Left Mouse Button
        }
        // Option B: Rely on external scripts calling SetHandPosition/SetTriggerState.
        // This is the preferred method for robust VR integration (e.g., XR Interaction Toolkit, OVRInput).
        else
        {
            currentFramePosition = _lastInputPosition;
            currentFrameTriggerState = _lastInputTriggerState;
        }

        // Process the current frame's input
        ProcessInput(currentFramePosition, currentFrameTriggerState, Time.deltaTime);
    }

    /// <summary>
    /// The core method where VR input is processed to recognize gestures.
    /// This method is called every frame with the latest input data.
    /// </summary>
    /// <param name="currentHandPosition">The current world position of the hand controller.</param>
    /// <param name="currentTriggerState">True if the primary trigger button is currently pressed.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    public void ProcessInput(Vector3 currentHandPosition, bool currentTriggerState, float deltaTime)
    {
        // Determine if trigger was just pressed or just released this frame
        // This relies on _lastInputTriggerState being the state from the PREVIOUS frame.
        bool triggerJustPressed = currentTriggerState && !_lastInputTriggerState;
        bool triggerJustReleased = !currentTriggerState && _lastInputTriggerState;

        // Iterate through all defined gestures and update their recognition state
        foreach (var entry in recognitionStates)
        {
            VRGestureDefinition gestureDef = entry.Key;
            GestureRecognitionState state = entry.Value;

            // --- UNIVERSAL RESET/START CONDITIONS for any gesture ---

            // Reset if gesture window time has passed, or if trigger was released before recognition.
            // (Note: If a gesture is recognized on trigger RELEASE, this logic needs adjustment
            // to allow the recognition to complete before resetting.)
            if (state.isActive)
            {
                if (Time.time - state.startTime > gestureWindowTime)
                {
                    // Gesture took too long, reset.
                    state.Reset();
                }
                else if (triggerJustReleased)
                {
                    // Trigger released prematurely (before recognition completed based on current logic).
                    // In a more complex system, some gestures might complete on release,
                    // but for Swipe/Hold as defined, they complete while held/moving.
                    state.Reset();
                }
            }


            // Start tracking a new gesture attempt if the trigger was just pressed and no gesture is active
            if (!state.isActive && triggerJustPressed)
            {
                state.isActive = true;
                state.startPosition = currentHandPosition;
                state.startTime = Time.time;
                state.lastKnownPosition = currentHandPosition; // Initialize for hold gesture tracking
                state.currentProgressValue = 0f;
                state.currentStationaryTime = 0f;
            }

            // --- GESTURE-SPECIFIC RECOGNITION LOGIC (Strategy Pattern application) ---
            if (state.isActive)
            {
                switch (gestureDef.type)
                {
                    case GestureType.Swipe:
                        HandleSwipeGesture(gestureDef, state, currentHandPosition);
                        break;
                    case GestureType.Hold:
                        HandleHoldGesture(gestureDef, state, currentHandPosition, deltaTime);
                        break;
                    // Add more cases here for new GestureTypes.
                    // Each case should call a dedicated handler method for that gesture type.
                    default:
                        Debug.LogWarning($"VRGestureSystem: Unknown gesture type '{gestureDef.type}' for gesture '{gestureDef.gestureName}'. Resetting state.", this);
                        state.Reset(); // Invalid type, so reset
                        break;
                }
            }
        }

        // Update the _lastInputPosition and _lastInputTriggerState for the next frame's comparison.
        _lastInputPosition = currentHandPosition;
        _lastInputTriggerState = currentTriggerState;
    }


    // --- Gesture-specific recognition logic methods ---
    // These methods represent different "strategies" for recognizing distinct gesture types.

    /// <summary>
    /// Handles the recognition logic for a Swipe gesture.
    /// </summary>
    private void HandleSwipeGesture(VRGestureDefinition gestureDef, GestureRecognitionState state, Vector3 currentHandPosition)
    {
        // Calculate the total displacement from the start of the gesture
        Vector3 displacement = currentHandPosition - state.startPosition;

        // Project the displacement onto the defined swipe direction.
        // The dot product gives us the magnitude of the displacement along that direction.
        state.currentProgressValue = Vector3.Dot(displacement, gestureDef.swipeDirection.normalized);

        // Check if the swipe threshold has been met
        if (state.currentProgressValue >= gestureDef.recognitionThreshold)
        {
            RecognizeGesture(gestureDef.gestureName);
            state.Reset(); // Reset the state after successful recognition
        }
    }

    /// <summary>
    /// Handles the recognition logic for a Hold gesture.
    /// </summary>
    private void HandleHoldGesture(VRGestureDefinition gestureDef, GestureRecognitionState state, Vector3 currentHandPosition, float deltaTime)
    {
        // Check if the hand is relatively still within the defined tolerance
        float movementSinceLastFrame = Vector3.Distance(currentHandPosition, state.lastKnownPosition);
        float currentTolerance = gestureDef.maxHoldMovementTolerance; // Use gesture-specific tolerance

        if (movementSinceLastFrame <= currentTolerance)
        {
            // If still, accumulate stationary time
            state.currentStationaryTime += deltaTime;
        }
        else
        {
            // If hand moved too much, reset stationary time.
            // This means the hold is broken and must restart its stationary period.
            state.currentStationaryTime = 0f;
        }

        state.lastKnownPosition = currentHandPosition; // Update for next frame's comparison

        // Check if the hold duration threshold has been met
        if (state.currentStationaryTime >= gestureDef.recognitionThreshold)
        {
            RecognizeGesture(gestureDef.gestureName);
            state.Reset(); // Reset the state after successful recognition
        }
    }

    /// <summary>
    /// Invokes the OnGestureRecognized event and logs the recognized gesture.
    /// This is the "output" of the gesture system.
    /// </summary>
    /// <param name="gestureName">The name of the recognized gesture.</param>
    private void RecognizeGesture(string gestureName)
    {
        Debug.Log($"VRGestureSystem: Gesture Recognized: {gestureName}", this);
        OnGestureRecognized?.Invoke(gestureName);
    }
}
```

---

### **How to Use the `VRGestureSystem` in Unity (Example Usage)**

Follow these steps to set up and test the `VRGestureSystem` in your Unity project:

1.  **Create Gesture Definitions:**
    *   In your Unity Project window, right-click -> Create -> **VR Gesture System -> Gesture Definition**.
    *   Create a few instances and configure their properties:

        *   **Gesture 1: "UpwardSwipe"**
            *   Name: `UpwardSwipe`
            *   Type: `Swipe`
            *   Recognition Threshold: `0.2` (meters)
            *   Swipe Direction: `(0, 1, 0)` (Y-axis for upward)

        *   **Gesture 2: "ForwardSwipe"**
            *   Name: `ForwardSwipe`
            *   Type: `Swipe`
            *   Recognition Threshold: `0.2`
            *   Swipe Direction: `(0, 0, 1)` (Z-axis for forward)

        *   **Gesture 3: "TriggerHold"**
            *   Name: `TriggerHold`
            *   Type: `Hold`
            *   Recognition Threshold: `1.5` (seconds)
            *   Max Hold Movement Tolerance: `0.1` (meters)

2.  **Create a `VRGestureSystem` GameObject:**
    *   Create an empty GameObject in your scene (e.g., "VRGestureManager").
    *   Attach the `VRGestureSystem.cs` script to it.

3.  **Configure the `VRGestureSystem` Component:**
    *   In the Inspector of your "VRGestureManager" GameObject:
        *   Drag and drop your created `VRGestureDefinition` assets into the `Gestures To Recognize` list.
        *   **For Simulation/Basic Testing:** Create an empty GameObject (e.g., "HandProxy") and move it around to simulate hand movement. Drag this "HandProxy" GameObject into the `Hand Controller Transform` field. The system will now use its position and `Input.GetMouseButton(0)` (left mouse button) as the trigger.
        *   (Optional) Adjust `Gesture Window Time` as needed.

4.  **Connect to Real VR Input (If not using simulation):**
    *   If you're integrating with an actual VR SDK (like Unity's XR Interaction Toolkit, Oculus Integration, SteamVR, etc.), you'll need a small helper script on your hand controller GameObject.
    *   **Disable/Remove `handControllerTransform` from the `VRGestureSystem` component** or ensure `handControllerTransform` is null if you want to use `SetHandPosition`/`SetTriggerState`.
    *   Example for Oculus Integration (place this on your `RightHandAnchor` or `LeftHandAnchor`):

        ```csharp
        using UnityEngine;
        // using OVR; // Uncomment if OVRInput is used directly

        /// <summary>
        /// Example script to feed real VR input from an Oculus controller to the VRGestureSystem.
        /// Attach this to your OVRHandPrefab or equivalent hand controller GameObject.
        /// </summary>
        public class OculusInputToGestureSystem : MonoBehaviour
        {
            [Tooltip("Reference to the VRGestureSystem in your scene.")]
            public VRGestureSystem gestureSystem; // Assign in Inspector

            [Tooltip("Specify which Oculus controller this script is attached to.")]
            public OVRInput.Controller controller = OVRInput.Controller.RTouch; // Or OVRInput.Controller.LTouch

            void Start()
            {
                if (gestureSystem == null)
                {
                    // Try to find it if not assigned (less efficient but convenient)
                    gestureSystem = FindObjectOfType<VRGestureSystem>();
                    if (gestureSystem == null)
                    {
                        Debug.LogError("OculusInputToGestureSystem: VRGestureSystem not found in scene or not assigned!", this);
                        enabled = false; // Disable this script if no system to send input to
                    }
                }
            }

            void Update()
            {
                if (gestureSystem != null)
                {
                    // Update hand position (transform.position is the world position of the hand controller)
                    gestureSystem.SetHandPosition(transform.position);

                    // Update trigger state (e.g., PrimaryIndexTrigger)
                    bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller);
                    gestureSystem.SetTriggerState(triggerPressed);
                }
            }
        }
        ```
    *   For XR Interaction Toolkit, you'd hook into `XRBaseController.position` and `XRBaseController.selectInteractionState.activated`.

5.  **Listen to Gesture Events:**
    *   Create a new C# script (e.g., `GestureReaction.cs`) to react to recognized gestures.
    *   Add this script to any GameObject in your scene (e.g., Main Camera, or an empty "GestureListener").
    *   Example `GestureReaction.cs` script:

        ```csharp
        using UnityEngine;
        using TMPro; // Required if using TextMeshPro for UI display

        /// <summary>
        /// Example script to demonstrate listening and reacting to recognized gestures.
        /// Attach this to any GameObject in your scene and set up the UI.
        /// </summary>
        public class GestureReaction : MonoBehaviour
        {
            [Tooltip("Optional: Assign a TextMeshProUGUI element to display recognized gestures.")]
            public TextMeshProUGUI uiTextDisplay; 

            void Start()
            {
                // Find the VRGestureSystem in the scene if not assigned
                VRGestureSystem gestureSystem = FindObjectOfType<VRGestureSystem>();
                if (gestureSystem != null)
                {
                    // Subscribe to the OnGestureRecognized event
                    gestureSystem.OnGestureRecognized.AddListener(OnGestureRecognized);
                    Debug.Log("GestureReaction: Subscribed to VRGestureSystem's OnGestureRecognized event.");
                }
                else
                {
                    Debug.LogError("GestureReaction: VRGestureSystem not found in scene! Please ensure one exists.", this);
                    enabled = false; // Disable this script if no system to listen to
                }

                if (uiTextDisplay != null)
                {
                    uiTextDisplay.text = "Perform a Gesture!";
                    uiTextDisplay.color = Color.white;
                }
            }

            /// <summary>
            /// This method is called whenever a gesture is recognized by the VRGestureSystem.
            /// </summary>
            /// <param name="gestureName">The name of the gesture that was recognized.</param>
            public void OnGestureRecognized(string gestureName)
            {
                Debug.Log($"GestureReaction: Received recognized gesture: {gestureName}");

                // Update UI display
                if (uiTextDisplay != null)
                {
                    uiTextDisplay.text = $"Gesture: {gestureName}!";
                    uiTextDisplay.color = Color.green;
                    CancelInvoke("ResetUIText"); // Cancel any pending reset
                    Invoke("ResetUIText", 2.0f); // Schedule text to clear after 2 seconds
                }

                // --- Implement specific actions based on the recognized gesture's name ---
                switch (gestureName)
                {
                    case "UpwardSwipe":
                        Debug.Log("Action: Launching projectile upwards!");
                        // Example: Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                        break;
                    case "ForwardSwipe":
                        Debug.Log("Action: Moving player forward!");
                        // Example: FindObjectOfType<PlayerMovement>().Move(Vector3.forward);
                        break;
                    case "TriggerHold":
                        Debug.Log("Action: Activating continuous power/charging effect!");
                        // Example: StartCoroutine(ActivatePowerBeam());
                        break;
                    default:
                        Debug.Log($"GestureReaction: Unhandled gesture: {gestureName}");
                        break;
                }
            }

            private void ResetUIText()
            {
                if (uiTextDisplay != null)
                {
                    uiTextDisplay.text = "Perform a Gesture!";
                    uiTextDisplay.color = Color.white;
                }
            }
        }
        ```
    *   If using `TextMeshProUGUI`, you'll need to import TextMeshPro Essentials (Window -> TextMeshPro -> Import TMP Essential Resources).
    *   Create a UI Canvas and add a TextMeshPro - Text element, then assign it to the `UI Text Display` field of your `GestureReaction` script.

This setup provides a complete, flexible, and extensible VR Gesture System, ready for integration into your Unity VR projects.