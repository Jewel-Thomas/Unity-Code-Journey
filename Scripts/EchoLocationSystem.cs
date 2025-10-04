// Unity Design Pattern Example: EchoLocationSystem
// This script demonstrates the EchoLocationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'EchoLocationSystem' design pattern in Unity, as implied by its name, can be interpreted as a specialized form of the **Observer pattern** or **Publisher-Subscriber pattern** combined with spatial querying.

Here's how it works in this context:

1.  **Echo Emitter (Publisher/Queryer):** A central component (e.g., a "sonar device" or "sensor array") sends out an "echo" or a spatial query (like a sonar ping). This emitter doesn't know about specific "listeners."
2.  **Echo Listener (Subscriber/Detectable):** Components on other GameObjects act as "listeners." They are designed to "hear" or be affected by the echo. When detected, they provide a standardized "response" back to the emitter.
3.  **Echo Request/Response (Message/Data):** The "echo" itself is a set of parameters (e.g., origin point, range) and the "response" is data returned by the listeners (e.g., their position, type, distance).
4.  **Localization/Processing:** The emitter collects these responses, effectively "localizing" or gathering information about its surroundings. It might then process these results or broadcast them to other interested systems.

This pattern is highly practical for systems like:
*   **Sonar/Radar:** Detecting enemies, resources, or obstacles within a radius.
*   **Environmental Scanners:** Identifying objects with specific properties.
*   **Area-of-Effect Spells:** Finding targets within a spell's range.
*   **AI Perception:** An AI agent scanning its immediate environment for threats or points of interest.

---

## Complete C# Unity Example: EchoLocationSystem

This example provides three core components:

1.  **`IEchoResponse` & `EchoResponseData`**: Defines the data structure for information returned by detected objects.
2.  **`IEchoListener`**: An interface that any detectable object must implement.
3.  **`SonarEchoEmitter`**: The component that sends out the 'echo' (a physics query) and collects responses.
4.  **`EchoDetectable`**: A concrete implementation of `IEchoListener` to attach to objects you want to be detected.

**To use this script:**

1.  **Create a C# Script** named `EchoLocationSystem.cs` in your Unity project.
2.  **Copy and Paste** the entire code block below into the script.
3.  **Set up Layers:** In Unity, go to `Edit -> Project Settings -> Tags and Layers`. Add a new `User Layer` (e.g., `Detectable`).
4.  **Follow the `Example Usage` comments** at the bottom of the script to set up your Emitter and Detectable objects in a scene.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<T>
using System; // For Action event delegate
using System.Collections; // For IEnumerator

// --- 1. The Echo Location Response Data ---
// This interface defines what information an 'echo' listener can provide back to the emitter.
// It keeps the system flexible, allowing different types of listeners to return different,
// but standardized, sets of data.
public interface IEchoResponse
{
    GameObject ResponderGameObject { get; }
    Vector3 Position { get; }
    float DistanceFromOrigin { get; }
    string TypeTag { get; } // A custom tag for the type of detectable object
    // You can extend this interface with more properties as needed,
    // e.g., Health, SpecificProperties, etc.
}

// A concrete implementation of IEchoResponse.
// This struct is used to encapsulate the data returned by an Echo Listener.
// Using a struct for small, immutable data can be more performant as it avoids heap allocations.
[System.Serializable] // Make it serializable if you ever need to store it or display in Inspector
public struct EchoResponseData : IEchoResponse
{
    public GameObject ResponderGameObject { get; private set; }
    public Vector3 Position { get; private set; }
    public float DistanceFromOrigin { get; private set; }
    public string TypeTag { get; private set; }

    public EchoResponseData(GameObject go, Vector3 pos, float dist, string tag)
    {
        ResponderGameObject = go;
        Position = pos;
        DistanceFromOrigin = dist;
        TypeTag = tag;
    }
}

// --- 2. The Echo Listener Interface ---
// This interface defines the contract for any object that can "hear" an echo and respond to it.
// Objects implementing this interface are the "targets" or "detectables" in the system.
public interface IEchoListener
{
    /// <summary>
    /// This method is called by the Echo Emitter when an echo reaches this listener.
    /// It should return an IEchoResponse containing relevant data about the listener.
    /// </summary>
    /// <param name="echoOrigin">The world position from where the echo was emitted.</param>
    /// <returns>An IEchoResponse containing data about this detectable object.</returns>
    IEchoResponse OnEchoLocated(Vector3 echoOrigin);
}

// --- 3. The Echo Emitter Component (Publisher/Queryer) ---
// This MonoBehaviour acts as the "sonar" or "ping" source.
// It sends out an echo (a physics query) and collects responses from IEchoListener components.
[RequireComponent(typeof(SphereCollider))] // Optional: For visual gizmo or if you extend to trigger-based detection
public class SonarEchoEmitter : MonoBehaviour
{
    [Header("Echo Settings")]
    [Tooltip("The radius of the echo ping.")]
    [SerializeField] private float pingRadius = 10f;
    [Tooltip("How often the echo ping is sent (in seconds).")]
    [SerializeField] private float pingInterval = 2f;
    [Tooltip("The layers that the echo will detect. Only objects on these layers will be considered.")]
    [SerializeField] private LayerMask detectableLayers;

    [Header("Debug Visualization")]
    [Tooltip("Show the ping radius as a Gizmo in the editor.")]
    [SerializeField] private bool showPingRadius = true;
    [Tooltip("Draw lines from emitter to detected objects when pinged.")]
    [SerializeField] private bool drawDebugLines = true;
    [Tooltip("Color of the debug lines.")]
    [SerializeField] private Color debugLineColor = Color.yellow;
    [Tooltip("Duration for which debug lines are visible.")]
    [SerializeField] private float debugLineDuration = 0.1f;

    // A public list to store the most recent detected objects.
    // Other scripts can access this list to get the latest detection results.
    public List<IEchoResponse> LastDetectedObjects { get; private set; } = new List<IEchoResponse>();

    // An event that other systems can subscribe to.
    // This allows other parts of your game to react when an echo ping completes and results are available,
    // without the emitter needing to know about those specific systems (decoupling).
    public event Action<List<IEchoResponse>> OnEchoPingCompleted;

    private float _nextPingTime;
    private SphereCollider _sphereCollider; // Used mainly for setting Gizmo radius and potential future trigger use

    void Awake()
    {
        _nextPingTime = Time.time;
        _sphereCollider = GetComponent<SphereCollider>();
        if (_sphereCollider != null)
        {
            _sphereCollider.isTrigger = true; // Ensure it's a trigger for OverlapSphere/Enter/Exit if used
            _sphereCollider.radius = pingRadius; // Keep the collider radius in sync with pingRadius
        }
    }

    void Update()
    {
        if (Time.time >= _nextPingTime)
        {
            EmitEchoLocationPing();
            _nextPingTime = Time.time + pingInterval;
        }
    }

    /// <summary>
    /// Emits a single echo location ping, querying for nearby IEchoListener components.
    /// This is the core of the EchoLocationSystem: sending out the 'ping' and collecting 'responses'.
    /// </summary>
    public void EmitEchoLocationPing()
    {
        LastDetectedObjects.Clear(); // Clear previous results

        // Use Physics.OverlapSphere to find all colliders within the pingRadius on the specified layers.
        // This is the "echo" propagation mechanism.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pingRadius, detectableLayers);

        foreach (Collider hitCollider in hitColliders)
        {
            // Attempt to get an IEchoListener component from the hit GameObject or its parents.
            // This allows the actual listener component to be on a child or parent of the collider.
            IEchoListener listener = hitCollider.GetComponentInParent<IEchoListener>();
            
            if (listener != null)
            {
                // If a listener is found, request its echo response data.
                // This is where the "localization" or information gathering happens.
                IEchoResponse response = listener.OnEchoLocated(transform.position);
                LastDetectedObjects.Add(response);

                // Debug visualization: Draw a line to the detected object.
                if (drawDebugLines)
                {
                    Debug.DrawLine(transform.position, response.Position, debugLineColor, debugLineDuration);
                }
            }
        }

        // Notify any subscribers that the ping has completed and results are available.
        // This makes the emitter highly reusable, as other systems can just listen for results.
        OnEchoPingCompleted?.Invoke(LastDetectedObjects);

        // Optional: Log results for debugging directly from the emitter
        // Debug.Log($"Echo Ping from {gameObject.name} completed. Detected {LastDetectedObjects.Count} objects.");
        // foreach (var response in LastDetectedObjects)
        // {
        //     Debug.Log($" - Detected: {response.ResponderGameObject.name} (Tag: {response.TypeTag}, Dist: {response.DistanceFromOrigin:F2})");
        // }
    }

    // Visualize the ping radius and detected objects in the editor.
    void OnDrawGizmos()
    {
        if (showPingRadius)
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan with transparency for the sphere
            Gizmos.DrawSphere(transform.position, pingRadius);

            if (Application.isPlaying && drawDebugLines)
            {
                // Re-draw debug lines for currently detected objects while in play mode if desired
                Gizmos.color = debugLineColor;
                foreach (var response in LastDetectedObjects)
                {
                    // Ensure the detected object still exists (might have been destroyed)
                    if (response.ResponderGameObject != null)
                    {
                        Gizmos.DrawLine(transform.position, response.Position);
                    }
                }
            }
        }
    }
}

// --- 4. The Echo Detectable Component (Listener Implementation) ---
// This MonoBehaviour implements the IEchoListener interface.
// Attach this to any GameObject you want to be detectable by a SonarEchoEmitter.
[RequireComponent(typeof(Collider))] // Requires a collider to be hit by Physics.OverlapSphere
public class EchoDetectable : MonoBehaviour, IEchoListener
{
    [Header("Echo Detectable Settings")]
    [Tooltip("A unique tag identifying the type of this detectable object.")]
    [SerializeField] private string typeTag = "GenericDetectable";
    [Tooltip("Color to temporarily show when this object is detected (requires a Renderer).")]
    [SerializeField] private Color detectionColor = Color.cyan;
    [Tooltip("How long the detection color lasts after being pinged.")]
    [SerializeField] private float detectionColorDuration = 0.5f;

    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _resetColorCoroutine;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        else
        {
            // Warn if no renderer is found, as visual feedback won't work.
            Debug.LogWarning($"EchoDetectable on {gameObject.name} requires a Renderer component for visual feedback.", this);
        }

        // Ensure a collider is present, as it's essential for detection by Physics.OverlapSphere.
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"EchoDetectable on {gameObject.name} requires a Collider component to be detectable. Disabling script.", this);
            enabled = false; // Disable the script if it can't function
        }
        else if (col.isTrigger && !col.attachedRigidbody)
        {
            // Important note for Unity Physics:
            // For Physics.OverlapSphere (and other query functions) to detect a trigger collider,
            // the collider usually needs to be attached to a Rigidbody (even a kinematic one).
            // Static trigger colliders without a Rigidbody might not be detected.
            Debug.LogWarning($"EchoDetectable on {gameObject.name} has a trigger collider but no Rigidbody. " +
                             "It might not be detected by Physics.OverlapSphere without a Rigidbody set to Kinematic.", this);
        }
    }

    /// <summary>
    /// Called by an Echo Emitter when this object is detected.
    /// This is the "response" part of the EchoLocationSystem.
    /// </summary>
    /// <param name="echoOrigin">The position from where the echo was emitted.</param>
    /// <returns>A struct containing data about this detectable object.</returns>
    public IEchoResponse OnEchoLocated(Vector3 echoOrigin)
    {
        // Provide visual feedback (if a renderer exists)
        if (_renderer != null)
        {
            if (_resetColorCoroutine != null)
            {
                StopCoroutine(_resetColorCoroutine); // Stop previous reset if a new ping comes in quickly
            }
            _renderer.material.color = detectionColor;
            _resetColorCoroutine = StartCoroutine(ResetColorAfterDelay(detectionColorDuration));
        }

        // Calculate distance from the echo origin
        float distance = Vector3.Distance(transform.position, echoOrigin);

        // Return the response data, encapsulating this object's relevant information.
        return new EchoResponseData(gameObject, transform.position, distance, typeTag);
    }

    private IEnumerator ResetColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_renderer != null)
        {
            _renderer.material.color = _originalColor;
        }
        _resetColorCoroutine = null; // Clear the coroutine reference
    }
}

/*
// --- Example Usage (How to implement in your Unity Project) ---

// 1. Setup the Emitter:
//    a. Create an empty GameObject in your scene (e.g., rename it "SonarEmitter").
//    b. Add the 'SonarEchoEmitter' script to this GameObject.
//    c. In the Inspector for "SonarEmitter":
//       - Adjust 'Ping Radius' (e.g., 15).
//       - Adjust 'Ping Interval' (e.g., 1.5).
//       - For 'Detectable Layers', select the 'Detectable' layer you created earlier.
//       - Optionally, enable/disable debug visualizations.
//    d. A SphereCollider will be added automatically due to `RequireComponent`. It's used for Gizmo visualization and can be for trigger-based detection if you extend the system.

// 2. Setup Detectable Objects:
//    a. Create several 3D objects in your scene (e.g., Cube, Sphere, Capsule).
//    b. For EACH detectable object:
//       - Ensure it has a Collider component (e.g., BoxCollider for a Cube).
//       - Add the 'EchoDetectable' script to it.
//       - In the Inspector for 'EchoDetectable':
//         - Set the 'Type Tag' (e.g., "Enemy", "Resource", "Obstacle").
//         - Optionally, change 'Detection Color' to see visual feedback when detected.
//       - Set the GameObject's Layer to the 'Detectable' layer you created earlier.
//         (This is crucial for the Emitter's `detectableLayers` to work).

// 3. (Optional) Reacting to Pings from another script:
//    You can subscribe to the 'OnEchoPingCompleted' event on the SonarEchoEmitter to process results.
//    This allows other parts of your game logic to react to detections without the emitter knowing about them directly.

//    Example Script:
//    public class EchoResultProcessor : MonoBehaviour
//    {
//        [Tooltip("Drag your SonarEmitter GameObject here.")]
//        public SonarEchoEmitter emitter;
//
//        void OnEnable()
//        {
//            // Subscribe to the emitter's event when this script becomes enabled.
//            if (emitter != null)
//            {
//                emitter.OnEchoPingCompleted += HandleEchoResults;
//                Debug.Log($"EchoResultProcessor subscribed to {emitter.name}'s OnEchoPingCompleted event.");
//            }
//            else
//            {
//                Debug.LogWarning("EchoResultProcessor: Emitter is not assigned!", this);
//            }
//        }
//
//        void OnDisable()
//        {
//            // Unsubscribe from the event when this script becomes disabled to prevent memory leaks.
//            if (emitter != null)
//            {
//                emitter.OnEchoPingCompleted -= HandleEchoResults;
//                Debug.Log($"EchoResultProcessor unsubscribed from {emitter.name}'s OnEchoPingCompleted event.");
//            }
//        }
//
//        /// <summary>
//        /// This method is called by the SonarEchoEmitter when an echo ping completes.
//        /// It receives a list of all detected objects.
//        /// </summary>
//        /// <param name="results">A list of IEchoResponse data from all detected objects.</param>
//        private void HandleEchoResults(List<IEchoResponse> results)
//        {
//            Debug.Log($"<color=lime>EchoResultProcessor:</color> Received {results.Count} detection results!");
//            foreach (var response in results)
//            {
//                Debug.Log($"  - Detected GameObject: {response.ResponderGameObject.name}, " +
//                          $"Type: '{response.TypeTag}', " +
//                          $"Position: {response.Position}, " +
//                          $"Distance: {response.DistanceFromOrigin:F2} units.");
//
//                // Example: Perform specific actions based on the detected object's type tag.
//                if (response.TypeTag == "Enemy")
//                {
//                    Debug.Log($"    -> Identified an <color=red>Enemy</color>! Consider targeting.");
//                    // Add to a list of potential targets, trigger an alert, etc.
//                }
//                else if (response.TypeTag == "Resource")
//                {
//                    Debug.Log($"    -> Found a <color=blue>Resource</color>! Consider collecting.");
//                }
//            }
//        }
//    }
//
//    - Create another empty GameObject (e.g., "GameLogic").
//    - Add the 'EchoResultProcessor' script to it.
//    - Drag your "SonarEmitter" GameObject from the Hierarchy into the 'Emitter' slot of the EchoResultProcessor in the Inspector.

// 4. Run the Scene:
//    - Observe the "SonarEmitter" sending out pings (Gizmo sphere).
//    - Watch your detectable objects briefly change color when detected.
//    - Check the Console for detailed debug logs from both the emitter and the optional result processor.
*/
```