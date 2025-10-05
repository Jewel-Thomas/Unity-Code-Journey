// Unity Design Pattern Example: GestureRecognitionSystem
// This script demonstrates the GestureRecognitionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example provides a complete, practical implementation of the 'Gesture Recognition System' design pattern in Unity. It's designed to be educational, showing how to separate concerns and make the system extensible.

**Key Design Pattern Elements Demonstrated:**

1.  **Strategy Pattern:** Each `IGestureRecognizer` (e.g., `TapGestureRecognizer`, `SwipeGestureRecognizer`) encapsulates a specific gesture recognition algorithm. The `GestureRecognitionSystem` acts as the context, using different strategies to try and recognize a gesture.
2.  **Observer Pattern:** The `GestureRecognitionSystem` acts as the 'Subject' by exposing an `OnGestureRecognized` event. Other scripts (like `GestureHandlerExample`) act as 'Observers' by subscribing to this event, allowing them to react to recognized gestures without tight coupling.
3.  **Abstract Factory/Factory Method (Implied):** While not a full factory, the `GestureRecognitionSystem` creates instances of `Gesture` objects based on the successful recognition by a specific `IGestureRecognizer`.
4.  **Singleton Pattern:** The `GestureRecognitionSystem` uses a basic singleton to provide a globally accessible entry point for registering recognizers and subscribing to events.

---

### **1. Core Gesture Definitions (`Gesture.cs`)**

This script defines the base `Gesture` class and concrete implementations for `Tap` and `Swipe` gestures. These are data classes that hold information about a recognized gesture.

```csharp
// File: Assets/Scripts/Gestures/Gesture.cs
using UnityEngine;

/// <summary>
/// Abstract base class for all gestures.
/// This class defines common properties that all gestures might share,
/// making it easy to pass around a generic 'Gesture' object
/// and then cast it to a specific type if needed.
/// </summary>
public abstract class Gesture
{
    public string Name { get; protected set; }                  // The name of the gesture (e.g., "Tap", "Swipe")
    public Vector2 StartPosition { get; protected set; }        // The screen position where the gesture started
    public Vector2 EndPosition { get; protected set; }          // The screen position where the gesture ended
    public float Duration { get; protected set; }               // How long the gesture took
    public float TotalMovementDistance { get; protected set; }  // Total distance moved during the gesture

    public Gesture(string name, Vector2 startPosition, Vector2 endPosition, float duration, float totalMovementDistance)
    {
        Name = name;
        StartPosition = startPosition;
        EndPosition = endPosition;
        Duration = duration;
        TotalMovementDistance = totalMovementDistance;
    }

    /// <summary>
    /// Provides a user-friendly string representation of the gesture.
    /// </summary>
    public override string ToString()
    {
        return $"[{Name}] Start: {StartPosition}, End: {EndPosition}, Duration: {Duration:F2}s, Distance: {TotalMovementDistance:F2}";
    }
}

/// <summary>
/// Concrete implementation for a Tap gesture.
/// Taps are typically short, quick presses with minimal movement.
/// </summary>
public class TapGesture : Gesture
{
    public TapGesture(Vector2 position, float duration, float totalMovementDistance)
        : base("Tap", position, position, duration, totalMovementDistance) { }
}

/// <summary>
/// Concrete implementation for a Swipe gesture.
/// Swipes involve movement over a distance in a particular direction.
/// </summary>
public class SwipeGesture : Gesture
{
    public Vector2 Direction { get; private set; } // The normalized direction of the swipe

    public SwipeGesture(Vector2 startPosition, Vector2 endPosition, float duration, float totalMovementDistance, Vector2 direction)
        : base("Swipe", startPosition, endPosition, duration, totalMovementDistance)
    {
        Direction = direction.normalized; // Ensure direction is always normalized
    }

    /// <summary>
    /// Provides a user-friendly string representation of the Swipe gesture, including its direction.
    /// </summary>
    public override string ToString()
    {
        return base.ToString() + $", Direction: {Direction}";
    }
}
```

---

### **2. Gesture Recognizer Interface (`IGestureRecognizer.cs`)**

This interface defines the contract for all gesture recognizers. Each specific gesture type will have its own recognizer implementation.

```csharp
// File: Assets/Scripts/Gestures/IGestureRecognizer.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for all gesture recognizers.
/// This allows the GestureRecognitionSystem to interact with different
/// gesture recognition logics polymorphically (Strategy Pattern).
/// </summary>
public interface IGestureRecognizer
{
    /// <summary>
    /// A unique name for the gesture this recognizer handles.
    /// </summary>
    string GestureName { get; }

    /// <summary>
    /// Attempts to recognize a gesture from the given input points.
    /// </summary>
    /// <param name="inputPoints">A list of screen positions captured during the potential gesture.</param>
    /// <param name="gestureStartTime">The Time.realtimeSinceStartup when the input tracking began.</param>
    /// <param name="currentTime">The current Time.realtimeSinceStartup.</param>
    /// <param name="recognizedGesture">Output parameter for the recognized gesture object if successful.</param>
    /// <returns>True if a gesture was recognized, false otherwise.</returns>
    bool TryRecognize(List<Vector2> inputPoints, float gestureStartTime, float currentTime, out Gesture recognizedGesture);

    /// <summary>
    /// Resets the internal state of the recognizer.
    /// This is crucial for multi-frame gestures or when a new gesture sequence begins.
    /// For simple gestures like Tap/Swipe, it might be empty.
    /// </summary>
    void ResetRecognitionState();
}
```

---

### **3. Concrete Gesture Recognizers (`TapGestureRecognizer.cs`, `SwipeGestureRecognizer.cs`)**

These scripts provide the specific logic for detecting a tap and a swipe gesture, respectively. They implement the `IGestureRecognizer` interface.

**3.1 `TapGestureRecognizer.cs`**

```csharp
// File: Assets/Scripts/Gestures/TapGestureRecognizer.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recognizer for a Tap gesture.
/// A tap is defined by a short duration press and minimal movement.
/// </summary>
[System.Serializable] // Make it serializable so thresholds can be set in Inspector
public class TapGestureRecognizer : IGestureRecognizer
{
    public string GestureName => "Tap"; // Name of the gesture this recognizer handles

    [Header("Tap Settings")]
    [Tooltip("Maximum duration for a press to be considered a tap (in seconds).")]
    [SerializeField] private float maxTapDuration = 0.2f;
    [Tooltip("Maximum movement allowed for a press to be considered a tap (in pixels).")]
    [SerializeField] private float maxTapMovementDistance = 10f;

    /// <summary>
    /// For a simple tap, no complex internal state is maintained across frames,
    /// so this method is empty.
    /// </summary>
    public void ResetRecognitionState() { }

    /// <summary>
    /// Attempts to recognize a tap gesture based on duration and movement thresholds.
    /// </summary>
    public bool TryRecognize(List<Vector2> inputPoints, float gestureStartTime, float currentTime, out Gesture recognizedGesture)
    {
        recognizedGesture = null;

        // A tap requires at least one point (the start/end point).
        if (inputPoints == null || inputPoints.Count == 0)
        {
            return false;
        }

        // Calculate gesture duration.
        float duration = currentTime - gestureStartTime;

        // Calculate total movement distance.
        float totalMovementDistance = 0f;
        if (inputPoints.Count > 1)
        {
            for (int i = 0; i < inputPoints.Count - 1; i++)
            {
                totalMovementDistance += Vector2.Distance(inputPoints[i], inputPoints[i + 1]);
            }
        }

        // Check against tap criteria.
        if (duration <= maxTapDuration && totalMovementDistance <= maxTapMovementDistance)
        {
            // If criteria met, create a TapGesture object.
            recognizedGesture = new TapGesture(inputPoints[0], duration, totalMovementDistance);
            return true;
        }

        return false;
    }
}
```

**3.2 `SwipeGestureRecognizer.cs`**

```csharp
// File: Assets/Scripts/Gestures/SwipeGestureRecognizer.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recognizer for a Swipe gesture.
/// A swipe is defined by significant movement over a certain distance and duration.
/// </summary>
[System.Serializable] // Make it serializable so thresholds can be set in Inspector
public class SwipeGestureRecognizer : IGestureRecognizer
{
    public string GestureName => "Swipe"; // Name of the gesture this recognizer handles

    [Header("Swipe Settings")]
    [Tooltip("Minimum distance the input must travel to be considered a swipe (in pixels).")]
    [SerializeField] private float minSwipeDistance = 50f;
    [Tooltip("Minimum duration for a swipe (to distinguish from a quick tap-like drag).")]
    [SerializeField] private float minSwipeDuration = 0.05f;
    [Tooltip("Maximum duration for a swipe (to distinguish from a long drag/hold).")]
    [SerializeField] private float maxSwipeDuration = 1.0f;
    [Tooltip("Maximum deviation from a straight line path, as a percentage of total distance.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxPathDeviation = 0.3f; // 0.3 means 30% deviation allowed

    /// <summary>
    /// For a simple swipe, no complex internal state is maintained across frames,
    /// so this method is empty.
    /// </summary>
    public void ResetRecognitionState() { }

    /// <summary>
    /// Attempts to recognize a swipe gesture based on distance, duration, and path linearity thresholds.
    /// </summary>
    public bool TryRecognize(List<Vector2> inputPoints, float gestureStartTime, float currentTime, out Gesture recognizedGesture)
    {
        recognizedGesture = null;

        // A swipe requires at least two points (start and end).
        if (inputPoints == null || inputPoints.Count < 2)
        {
            return false;
        }

        Vector2 startPos = inputPoints[0];
        Vector2 endPos = inputPoints[inputPoints.Count - 1];
        float duration = currentTime - gestureStartTime;

        // Calculate total movement distance (path length).
        float totalMovementDistance = 0f;
        for (int i = 0; i < inputPoints.Count - 1; i++)
        {
            totalMovementDistance += Vector2.Distance(inputPoints[i], inputPoints[i + 1]);
        }

        // Calculate straight line distance between start and end.
        float straightLineDistance = Vector2.Distance(startPos, endPos);

        // Check swipe criteria:
        // 1. Duration within limits.
        // 2. Total movement distance meets minimum.
        // 3. Straight line distance is significant relative to total movement (path not too wiggly).
        if (duration >= minSwipeDuration &&
            duration <= maxSwipeDuration &&
            totalMovementDistance >= minSwipeDistance &&
            straightLineDistance >= minSwipeDistance && // Ensure the swipe actually went somewhere
            (straightLineDistance / totalMovementDistance) >= (1f - maxPathDeviation)) // Check for linearity
        {
            // Calculate direction of the swipe.
            Vector2 direction = (endPos - startPos);

            // If criteria met, create a SwipeGesture object.
            recognizedGesture = new SwipeGesture(startPos, endPos, duration, totalMovementDistance, direction);
            return true;
        }

        return false;
    }
}
```

---

### **4. Gesture Recognition System (`GestureRecognitionSystem.cs`)**

This is the central manager script. It captures input, registers all `IGestureRecognizer` instances, and dispatches events when a gesture is recognized.

```csharp
// File: Assets/Scripts/GestureRecognitionSystem.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For .ToList() and other LINQ operations

/// <summary>
/// The GestureRecognitionSystem is the central component that manages input capture,
/// registers different gesture recognizers, and dispatches events when gestures are detected.
/// This acts as the 'Context' in the Strategy pattern (for gesture recognition logic)
/// and the 'Subject' in the Observer pattern (for gesture events).
/// It follows the Singleton pattern for easy global access.
/// </summary>
public class GestureRecognitionSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    /// <summary>
    /// Provides a globally accessible instance of the GestureRecognitionSystem.
    /// </summary>
    public static GestureRecognitionSystem Instance { get; private set; }

    // --- Observer Pattern (Events) ---
    /// <summary>
    /// Event that clients can subscribe to receive notifications when a gesture is recognized.
    /// The event carries the recognized 'Gesture' object.
    /// </summary>
    public static event Action<Gesture> OnGestureRecognized;

    // --- Strategy Pattern (Gesture Recognizers) ---
    /// <summary>
    /// A list of all active gesture recognizers.
    /// Each recognizer implements IGestureRecognizer and contains the specific logic for a gesture.
    /// [SerializeReference] allows derived classes of an interface/abstract class to be serialized,
    /// enabling configuration of different IGestureRecognizer types directly in the Inspector.
    /// </summary>
    [SerializeReference]
    private List<IGestureRecognizer> _recognizers = new List<IGestureRecognizer>();

    [Header("Input Tracking Settings")]
    [Tooltip("How frequently (in seconds) to sample input positions when tracking.")]
    [SerializeField] private float _samplingRate = 0.02f; // ~50 samples per second
    // Note: _maxInputPointAge is currently not fully utilized for clearing old points mid-gesture,
    // as tap/swipe recognizers typically use all points from start to end.
    // It's more relevant for continuous recognition or complex shape detection over time.
    [Tooltip("Maximum age (in seconds) of input points to keep for recognition (currently not fully implemented for all recognizers).")]
    [SerializeField] private float _maxInputPointAge = 1.0f; 

    private List<Vector2> _currentInputPoints = new List<Vector2>(); // Stores captured input positions (screen space).
    private float _gestureStartTime;         // Time.realtimeSinceStartup when the current input sequence began.
    private float _lastSampleTime;           // Last time an input point was sampled.
    private bool _isTrackingInput = false;   // Flag to indicate if input is currently being tracked.

    // --- MonoBehaviour Lifecycle ---

    private void Awake()
    {
        // Implement the singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GestureRecognitionSystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize default recognizers if none are set in the Inspector.
        // This makes the system ready to use out-of-the-box.
        if (_recognizers == null || _recognizers.Count == 0)
        {
            Debug.Log("No gesture recognizers configured in Inspector. Adding default Tap and Swipe recognizers.");
            _recognizers = new List<IGestureRecognizer>
            {
                new TapGestureRecognizer(),
                new SwipeGestureRecognizer()
            };
        }
    }

    private void Update()
    {
        // Capture mouse input for demonstration.
        // This can be easily extended to touch input (Input.touches), VR controllers, etc.
        HandleMouseInput();
    }

    // --- Public API ---

    /// <summary>
    /// Registers a new gesture recognizer with the system.
    /// New custom gesture types can be added at runtime.
    /// </summary>
    /// <param name="recognizer">The IGestureRecognizer implementation to add.</param>
    public void RegisterRecognizer(IGestureRecognizer recognizer)
    {
        if (recognizer == null)
        {
            Debug.LogError("Attempted to register a null gesture recognizer.");
            return;
        }
        if (_recognizers.Any(r => r.GestureName == recognizer.GestureName))
        {
            Debug.LogWarning($"Gesture recognizer with name '{recognizer.GestureName}' already exists. Skipping registration.");
            return;
        }
        _recognizers.Add(recognizer);
        Debug.Log($"Registered new gesture recognizer: {recognizer.GestureName}");
    }

    /// <summary>
    /// Unregisters an existing gesture recognizer from the system.
    /// </summary>
    /// <param name="recognizerName">The name of the gesture recognizer to remove.</param>
    public void UnregisterRecognizer(string recognizerName)
    {
        var recognizerToRemove = _recognizers.FirstOrDefault(r => r.GestureName == recognizerName);
        if (recognizerToRemove != null)
        {
            _recognizers.Remove(recognizerToRemove);
            Debug.Log($"Unregistered gesture recognizer: {recognizerName}");
        }
        else
        {
            Debug.LogWarning($"Gesture recognizer with name '{recognizerName}' not found.");
        }
    }

    // --- Internal Input Handling ---

    /// <summary>
    /// Manages the mouse input state (press, hold, release) and triggers
    /// input tracking and gesture recognition.
    /// </summary>
    private void HandleMouseInput()
    {
        // Start tracking input when mouse button 0 (left click) is pressed down.
        if (Input.GetMouseButtonDown(0))
        {
            StartTrackingInput();
        }

        // Continue tracking input as long as mouse button 0 is held down.
        if (Input.GetMouseButton(0))
        {
            TrackInput();
        }

        // Stop tracking and attempt recognition when mouse button 0 is released.
        if (Input.GetMouseButtonUp(0))
        {
            StopTrackingInputAndRecognize();
        }

        // Edge case: If tracking was active but mouse button isn't held (e.g., missed MouseUp,
        // or alt-tabbed out of focus causing MouseUp to be missed), finalize recognition.
        if (_isTrackingInput && !Input.GetMouseButton(0))
        {
            StopTrackingInputAndRecognize();
        }
    }

    /// <summary>
    /// Initializes input tracking state when a new potential gesture begins.
    /// </summary>
    private void StartTrackingInput()
    {
        _isTrackingInput = true;
        _gestureStartTime = Time.realtimeSinceStartup;
        _lastSampleTime = _gestureStartTime;
        _currentInputPoints.Clear(); // Clear any previous points
        foreach (var recognizer in _recognizers)
        {
            recognizer.ResetRecognitionState(); // Reset each recognizer's internal state
        }
        _currentInputPoints.Add(Input.mousePosition); // Add the initial point
    }

    /// <summary>
    /// Continuously samples and stores input positions while a potential gesture is in progress.
    /// </summary>
    private void TrackInput()
    {
        if (!_isTrackingInput) return;

        // Sample input position at a defined rate.
        if (Time.realtimeSinceStartup - _lastSampleTime >= _samplingRate)
        {
            // Only add if the position has actually changed to avoid redundant points for stationary input
            if (_currentInputPoints.Count == 0 || Vector2.Distance(_currentInputPoints.Last(), Input.mousePosition) > 1f) // Small threshold
            {
                _currentInputPoints.Add(Input.mousePosition);
            }
            _lastSampleTime = Time.realtimeSinceStartup;
        }

        // Optional: Remove old points. Currently not critical for Tap/Swipe but useful for
        // gestures that analyze a 'window' of recent input, or to prevent memory issues for
        // extremely long drag gestures.
        // For this example, we keep all points until release, as Tap/Swipe use the full sequence.
    }

    /// <summary>
    /// Stops input tracking and attempts to recognize a gesture using all registered recognizers.
    /// Dispatches an event if a gesture is successfully recognized.
    /// </summary>
    private void StopTrackingInputAndRecognize()
    {
        if (!_isTrackingInput) return;

        _isTrackingInput = false; // Stop tracking immediately.

        // Add the final input point if it's different from the last sampled point.
        if (_currentInputPoints.Count == 0 || Vector2.Distance(_currentInputPoints.Last(), Input.mousePosition) > 1f)
        {
             _currentInputPoints.Add(Input.mousePosition);
        }
        
        // Ensure there's at least one point to process if input was very brief.
        if (_currentInputPoints.Count == 0 && Input.GetMouseButtonUp(0)) {
            _currentInputPoints.Add(Input.mousePosition);
        }

        // Attempt to recognize a gesture using all registered recognizers.
        // Recognizers are tried in the order they are in the list.
        // The first recognizer that successfully identifies a gesture "wins".
        Gesture recognizedGesture = null;
        foreach (var recognizer in _recognizers)
        {
            if (recognizer.TryRecognize(_currentInputPoints, _gestureStartTime, Time.realtimeSinceStartup, out recognizedGesture))
            {
                // Gesture recognized! Fire the event to notify all subscribers.
                OnGestureRecognized?.Invoke(recognizedGesture);
                Debug.Log($"Recognized: {recognizedGesture}");
                break; // Stop after the first successful recognition (important if multiple could match)
            }
        }

        // Reset input tracking state, regardless of whether a gesture was recognized or not,
        // to prepare for the next potential gesture.
        _currentInputPoints.Clear();
    }
}
```

---

### **5. Example Gesture Handler (`GestureHandlerExample.cs`)**

This script demonstrates how a client (any other script in your game) can subscribe to the `GestureRecognitionSystem`'s event and react to different recognized gestures.

```csharp
// File: Assets/Scripts/GestureHandlerExample.cs
using UnityEngine;

/// <summary>
/// This is an example client script that demonstrates how to subscribe to
/// the GestureRecognitionSystem's events and react to recognized gestures.
/// This acts as an 'Observer' in the Observer pattern.
/// </summary>
public class GestureHandlerExample : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to the OnGestureRecognized event.
        // This method will be called whenever a gesture is successfully recognized by the system.
        GestureRecognitionSystem.OnGestureRecognized += HandleGestureRecognized;
        Debug.Log("GestureHandlerExample enabled and subscribed to gestures.");
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when this GameObject is disabled or destroyed.
        // This is crucial to prevent memory leaks and unexpected behavior (e.g., calling a method
        // on a destroyed object).
        GestureRecognitionSystem.OnGestureRecognized -= HandleGestureRecognized;
        Debug.Log("GestureHandlerExample disabled and unsubscribed from gestures.");
    }

    /// <summary>
    /// This method is the event handler for recognized gestures.
    /// It receives the base 'Gesture' object, which can then be cast to
    /// specific gesture types to access their unique properties (e.g., direction for a swipe).
    /// </summary>
    /// <param name="gesture">The recognized gesture object.</param>
    private void HandleGestureRecognized(Gesture gesture)
    {
        // Log the base gesture information.
        Debug.Log($"Gesture Handled: {gesture.Name}");

        // Use 'is' and 'as' to safely check the specific type of gesture
        // and access its unique properties for specialized handling.

        if (gesture is TapGesture tapGesture)
        {
            // Example: Do something specific for a tap.
            Debug.Log($"  > It's a Tap! Position: {tapGesture.StartPosition}, Duration: {tapGesture.Duration:F2}s");
            // Practical use cases: select an object, open a menu, fire a weapon (quick tap), etc.
        }
        else if (gesture is SwipeGesture swipeGesture)
        {
            // Example: Do something specific for a swipe.
            Debug.Log($"  > It's a Swipe! Direction: {swipeGesture.Direction}, Distance: {swipeGesture.TotalMovementDistance:F2}");
            // Practical use cases: move a camera, navigate a UI, trigger a character ability, scroll lists, etc.

            // You can also check for specific swipe directions if needed:
            if (Mathf.Abs(swipeGesture.Direction.x) > Mathf.Abs(swipeGesture.Direction.y))
            {
                // Horizontal swipe
                if (swipeGesture.Direction.x > 0)
                {
                    Debug.Log("    > Swiped Right!");
                }
                else
                {
                    Debug.Log("    > Swiped Left!");
                }
            }
            else
            {
                // Vertical swipe
                if (swipeGesture.Direction.y > 0)
                {
                    Debug.Log("    > Swiped Up!");
                }
                else
                {
                    Debug.Log("    > Swiped Down!");
                }
            }
        }
        // Add more 'else if' blocks here for any other custom gesture types you implement
        // (e.g., CircleGesture, PinchGesture, LongPressGesture, etc.).
        else
        {
            Debug.Log($"  > Unknown gesture type: {gesture.GetType().Name}");
        }
    }

    /// <summary>
    /// Example of how a custom gesture recognizer could be added at runtime.
    /// In a real project, you would define your custom `IGestureRecognizer` class
    /// (e.g., `CircleGestureRecognizer`) and then instantiate and register it.
    /// </summary>
    public void AddCustomRecognizerDynamicallyExample()
    {
        if (GestureRecognitionSystem.Instance != null)
        {
            // For a true custom recognizer, you would define a new class like:
            // public class CircleGestureRecognizer : IGestureRecognizer { ... }
            // Then instantiate it:
            // var circleRecognizer = new CircleGestureRecognizer();
            // GestureRecognitionSystem.Instance.RegisterRecognizer(circleRecognizer);

            // For this example, we'll demonstrate adding a *new instance* of an existing type.
            // Note: The system prevents adding recognizers with duplicate names.
            // So, for a real "new" type, you need a distinct GestureName.
            // Let's imagine a "DoubleTap" recognizer for this example:
            // var doubleTapRecognizer = new DoubleTapGestureRecognizer(); // New class required
            // GestureRecognitionSystem.Instance.RegisterRecognizer(doubleTapRecognizer);

            Debug.Log("Called AddCustomRecognizerDynamicallyExample. " +
                      "In a real scenario, this would register a *new* IGestureRecognizer implementation.");
        }
        else
        {
            Debug.LogError("GestureRecognitionSystem.Instance is not available. Is the GestureRecognitionSystem GameObject in the scene?");
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create Folders:**
    *   In your Unity Project window, create a folder named `Scripts`.
    *   Inside `Scripts`, create another folder named `Gestures`.

2.  **Create C# Scripts:**
    *   Create `Gesture.cs` in `Assets/Scripts/Gestures/` and paste the code from **Section 1**.
    *   Create `IGestureRecognizer.cs` in `Assets/Scripts/Gestures/` and paste the code from **Section 2**.
    *   Create `TapGestureRecognizer.cs` in `Assets/Scripts/Gestures/` and paste the code from **Section 3.1**.
    *   Create `SwipeGestureRecognizer.cs` in `Assets/Scripts/Gestures/` and paste the code from **Section 3.2**.
    *   Create `GestureRecognitionSystem.cs` in `Assets/Scripts/` and paste the code from **Section 4**.
    *   Create `GestureHandlerExample.cs` in `Assets/Scripts/` and paste the code from **Section 5**.

3.  **Create GameObjects in Scene:**
    *   In your Unity scene (Hierarchy window), right-click -> `Create Empty`. Rename this GameObject to `GestureSystem`.
    *   Right-click again -> `Create Empty`. Rename this GameObject to `GestureEventHandler`.

4.  **Attach Components:**
    *   Drag and drop the `GestureRecognitionSystem.cs` script onto the `GestureSystem` GameObject.
    *   Drag and drop the `GestureHandlerExample.cs` script onto the `GestureEventHandler` GameObject.

5.  **Configure `GestureRecognitionSystem` (Optional):**
    *   Select the `GestureSystem` GameObject.
    *   In the Inspector, you'll see the `GestureRecognitionSystem` component.
    *   Expand the "Recognizers" list. By default, it will automatically populate with `TapGestureRecognizer` and `SwipeGestureRecognizer` on `Awake()` if the list is empty.
    *   You can expand each recognizer to adjust its settings (e.g., `maxTapDuration`, `minSwipeDistance`) directly in the Inspector.
    *   You can manually add more `IGestureRecognizer` types here if you create new ones.

6.  **Run the Scene:**
    *   Play your Unity scene.
    *   In the Game view, try interacting with your mouse:
        *   **Quickly click and release:** This should trigger a "Tap" gesture.
        *   **Click and drag the mouse a noticeable distance:** This should trigger a "Swipe" gesture.
    *   Observe the Unity Console window. You will see messages indicating when gestures are recognized and handled, along with their specific details.

This setup provides a robust and extensible foundation for building advanced gesture-driven interactions in your Unity projects.