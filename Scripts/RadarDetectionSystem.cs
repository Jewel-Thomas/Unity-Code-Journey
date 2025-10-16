// Unity Design Pattern Example: RadarDetectionSystem
// This script demonstrates the RadarDetectionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Radar Detection System** design pattern in Unity. This pattern involves a central "radar" component that periodically scans for "detectable" objects within a defined area, notifying other systems when objects enter or leave its range.

**Key Components of the Pattern:**

1.  **`IDetectable` Interface:** Defines a common contract for any object that can be detected by the radar. This ensures loose coupling.
2.  **`DetectableTarget` Component:** A concrete implementation of `IDetectable`. You attach this to objects you want the radar to find.
3.  **`RadarDetectionSystem` Component:** The core "radar" itself. It performs the detection logic (using physics queries), manages the list of detected objects, and provides events for others to subscribe to.
4.  **`RadarListenerExample` Component:** An example of how another script (e.g., a player AI, a UI system) would subscribe to and react to the `RadarDetectionSystem`'s events.

---

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for HashSet
using System; // Required for Action

namespace DesignPatterns.RadarDetectionSystem
{
    // ---------------------------------------------------------------------------------------------
    // 1. IDetectable Interface
    //    Defines the contract for any object that can be detected by the RadarDetectionSystem.
    //    This is crucial for loose coupling: the Radar doesn't need to know the concrete type
    //    of the detected object, only that it implements IDetectable. This allows different
    //    types of objects (e.g., enemies, allies, resources) to be detected by the same radar.
    // ---------------------------------------------------------------------------------------------
    public interface IDetectable
    {
        // Provides the GameObject associated with the detectable object.
        GameObject GetGameObject();
        // Provides the Transform associated with the detectable object.
        Transform GetTransform();
        // A property to get a display name or identifier for the detectable object.
        string detectableName { get; }

        // You can extend this interface with more properties/methods,
        // e.g., GetTeamID(), GetDetectableType(), IsHostile(), etc.,
        // depending on the information your radar needs to gather.
    }

    // ---------------------------------------------------------------------------------------------
    // 2. DetectableTarget Component
    //    An example concrete implementation of the IDetectable interface.
    //    Attach this script to any GameObject you want the RadarDetectionSystem to detect.
    //    It ensures the GameObject has a Collider, which is necessary for physics-based detection.
    // ---------------------------------------------------------------------------------------------
    [RequireComponent(typeof(Collider))] // Ensures any object with this script also has a Collider.
                                         // Physics.OverlapSphere needs a collider on the target.
    public class DetectableTarget : MonoBehaviour, IDetectable
    {
        [Tooltip("A unique name or identifier for this detectable target.")]
        [SerializeField] private string _targetName = "Detectable Object";

        // Implement the IDetectable interface methods and property
        public GameObject GetGameObject() => gameObject;
        public Transform GetTransform() => transform;
        public string detectableName => _targetName; // Read-only property for the name

        void Awake()
        {
            // Optional: Log a warning if no collider is found, even with RequireComponent,
            // as some colliders might be disabled or misconfigured.
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"DetectableTarget '{gameObject.name}' is missing a Collider component. " +
                                 "Physics queries might not work as expected.", this);
            }
        }

        // Optional: Draw a small sphere in the editor to visualize this target
        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            // Only draw gizmos in the editor to avoid runtime overhead in builds
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.75f, _targetName);
#endif
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }


    // ---------------------------------------------------------------------------------------------
    // 3. RadarDetectionSystem Component
    //    This is the core "radar" component. It performs periodic scans, manages detected objects,
    //    and notifies subscribers via C# events when objects enter or leave its range.
    // ---------------------------------------------------------------------------------------------
    public class RadarDetectionSystem : MonoBehaviour
    {
        [Header("Radar Settings")]
        [Tooltip("The radius within which the radar will detect objects.")]
        [SerializeField] private float detectionRange = 10f;

        [Tooltip("How often (in seconds) the radar will perform a detection scan.")]
        [SerializeField] private float detectionInterval = 0.5f;

        [Tooltip("The layers on which the radar will look for detectable objects.")]
        [SerializeField] private LayerMask detectableLayers;

        [Tooltip("If specified, the radar will only detect objects with this specific tag.")]
        [SerializeField] private string detectableTag = "";

        [Tooltip("Visualize the detection range and detected objects in the editor.")]
        [SerializeField] private bool drawGizmos = true;

        // Private fields to manage the state of detected objects between scans.
        // Using HashSets for efficient O(1) average time complexity for add, remove, and contains operations.
        private HashSet<IDetectable> _currentlyDetectedObjects = new HashSet<IDetectable>();
        private HashSet<IDetectable> _previouslyDetectedObjects = new HashSet<IDetectable>();
        private Coroutine _detectionCoroutine; // Reference to the running coroutine

        // -----------------------------------------------------------------------------------------
        // Public Events:
        // These events allow other scripts to subscribe and react when objects
        // enter or leave the radar's detection range. Using System.Action<T> for simplicity.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Event fired when a new IDetectable object *enters* the radar's range.
        /// Subscribers receive the IDetectable object that just came into range.
        /// </summary>
        public event Action<IDetectable> OnObjectEnteredRange;

        /// <summary>
        /// Event fired when an IDetectable object *leaves* the radar's range.
        /// Subscribers receive the IDetectable object that just went out of range.
        /// </summary>
        public event Action<IDetectable> OnObjectLeftRange;

        /// <summary>
        /// Provides a read-only collection of all IDetectable objects currently within the radar's range.
        /// This allows subscribers to query the current state without modifying the internal list.
        /// </summary>
        public IReadOnlyCollection<IDetectable> DetectedObjects => _currentlyDetectedObjects;

        // -----------------------------------------------------------------------------------------
        // MonoBehaviour Lifecycle Methods
        // -----------------------------------------------------------------------------------------

        void OnEnable()
        {
            // Start the detection coroutine when the GameObject is enabled.
            // Using a coroutine with a WaitForSeconds interval is more performant than
            // checking every frame in Update() if the detection doesn't need to be constant.
            if (_detectionCoroutine != null)
            {
                StopCoroutine(_detectionCoroutine); // Stop any existing coroutine to prevent duplicates
            }
            _detectionCoroutine = StartCoroutine(DetectionRoutine());
            Debug.Log($"Radar '{gameObject.name}' enabled and started scanning.");
        }

        void OnDisable()
        {
            // Stop the detection coroutine when the GameObject is disabled.
            // This is crucial for cleaning up resources and preventing errors if the object is destroyed.
            if (_detectionCoroutine != null)
            {
                StopCoroutine(_detectionCoroutine);
                _detectionCoroutine = null;
            }
            // Clear lists to prevent stale references if radar is re-enabled later.
            _currentlyDetectedObjects.Clear();
            _previouslyDetectedObjects.Clear();
            Debug.Log($"Radar '{gameObject.name}' disabled and stopped scanning.");
        }

        // -----------------------------------------------------------------------------------------
        // Core Detection Logic
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Coroutine that periodically performs a detection scan at the specified interval.
        /// </summary>
        private IEnumerator DetectionRoutine()
        {
            while (true) // Loop indefinitely while the coroutine is running
            {
                PerformDetectionScan();
                yield return new WaitForSeconds(detectionInterval); // Wait before the next scan
            }
        }

        /// <summary>
        /// Executes a single detection scan using Physics.OverlapSphere.
        /// It updates the list of currently detected objects and fires events
        /// for objects entering or leaving range.
        /// </summary>
        private void PerformDetectionScan()
        {
            // Step 1: Prepare for the new scan.
            // Swap references to efficiently manage current and previous state.
            // 'previouslyDetectedObjects' now holds the objects from the *last* scan.
            // 'currentlyDetectedObjects' is cleared to be filled with fresh data.
            var temp = _previouslyDetectedObjects;
            _previouslyDetectedObjects = _currentlyDetectedObjects;
            _currentlyDetectedObjects = temp;
            _currentlyDetectedObjects.Clear();

            // Step 2: Perform the physics query.
            // Physics.OverlapSphere finds all colliders within a sphere.
            // It's efficient for checking "what's in this area?".
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, detectableLayers);

            // Step 3: Process detected colliders.
            foreach (var hitCollider in hitColliders)
            {
                // Try to get the IDetectable component. Use GetComponentInParent
                // to handle cases where the Collider might be on a child GameObject,
                // but the IDetectable script is on the parent (e.g., a character root).
                IDetectable detectable = hitCollider.GetComponentInParent<IDetectable>();

                if (detectable != null && detectable.GetGameObject().activeInHierarchy) // Check if the target is active
                {
                    // Optional: Filter by tag if a tag is specified in the Inspector.
                    if (!string.IsNullOrEmpty(detectableTag) && !detectable.GetGameObject().CompareTag(detectableTag))
                    {
                        continue; // Skip this object if its tag doesn't match the filter.
                    }

                    // Add the valid, filtered detectable object to our current list.
                    _currentlyDetectedObjects.Add(detectable);

                    // Check if this object was *not* in the previous scan.
                    // If so, it means it just entered the radar's range.
                    if (!_previouslyDetectedObjects.Contains(detectable))
                    {
                        OnObjectEnteredRange?.Invoke(detectable); // Fire the "entered range" event.
                    }
                }
            }

            // Step 4: Identify objects that have left the range.
            // Iterate through the objects that *were* detected in the previous scan.
            foreach (var detectable in _previouslyDetectedObjects)
            {
                // If an object from the previous scan is *not* in the current scan,
                // it means it has left the radar's range or been destroyed.
                // We also check for null, as objects might have been destroyed between scans.
                if (detectable == null || !currentlyDetectedObjects.Contains(detectable))
                {
                    OnObjectLeftRange?.Invoke(detectable); // Fire the "left range" event.
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        // Debugging and Visualization (Gizmos)
        // -----------------------------------------------------------------------------------------

        void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            // Draw the detection range sphere in the editor.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw a smaller, filled sphere for the radar's position.
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f); // Orange, semi-transparent
            Gizmos.DrawSphere(transform.position, 0.2f);

            // Draw lines to currently detected objects
            if (_currentlyDetectedObjects != null)
            {
                Gizmos.color = Color.red;
                foreach (var detectable in _currentlyDetectedObjects)
                {
                    // Ensure the detected object still exists and its transform is valid.
                    if (detectable != null && detectable.GetTransform() != null)
                    {
                        Gizmos.DrawLine(transform.position, detectable.GetTransform().position);
                        // Optional: Draw a small cube at the target's position for better visibility
                        Gizmos.DrawCube(detectable.GetTransform().position, Vector3.one * 0.3f);
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // 4. RadarListenerExample Component
    //    This script demonstrates how another component would interact with the RadarDetectionSystem.
    //    It subscribes to the radar's events to react when objects enter or leave range,
    //    and also shows how to query the current list of detected objects.
    // ---------------------------------------------------------------------------------------------
    public class RadarListenerExample : MonoBehaviour
    {
        [Tooltip("Reference to the RadarDetectionSystem component in the scene.")]
        [SerializeField] private RadarDetectionSystem radarSystem;

        void OnEnable()
        {
            // IMPORTANT: Subscribe to the radar's events when this listener is enabled.
            // This ensures our methods are called when detection events occur.
            if (radarSystem != null)
            {
                radarSystem.OnObjectEnteredRange += HandleObjectEnteredRange;
                radarSystem.OnObjectLeftRange += HandleObjectLeftRange;
                Debug.Log($"RadarListenerExample subscribed to radar events on {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning("RadarSystem reference is missing in RadarListenerExample. " +
                                 "Please assign it in the Inspector.", this);
            }
        }

        void OnDisable()
        {
            // IMPORTANT: Unsubscribe from the radar's events when this listener is disabled.
            // This prevents memory leaks and ensures our methods aren't called on a destroyed object
            // or if the listener itself is destroyed.
            if (radarSystem != null)
            {
                radarSystem.OnObjectEnteredRange -= HandleObjectEnteredRange;
                radarSystem.OnObjectLeftRange -= HandleObjectLeftRange;
                Debug.Log($"RadarListenerExample unsubscribed from radar events on {gameObject.name}.");
            }
        }

        /// <summary>
        /// Callback method for when an object enters the radar's range.
        /// This method is invoked by the RadarDetectionSystem via the OnObjectEnteredRange event.
        /// </summary>
        /// <param name="detectable">The IDetectable object that just entered range.</param>
        private void HandleObjectEnteredRange(IDetectable detectable)
        {
            Debug.Log($"<color=green>RADAR ALERT: {detectable.detectableName} ENTERED range at {detectable.GetTransform().position}!</color>");
            // Example action: Change the detected object's material color to red.
            Renderer renderer = detectable.GetGameObject().GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
        }

        /// <summary>
        /// Callback method for when an object leaves the radar's range.
        /// This method is invoked by the RadarDetectionSystem via the OnObjectLeftRange event.
        /// </summary>
        /// <param name="detectable">The IDetectable object that just left range.</param>
        private void HandleObjectLeftRange(IDetectable detectable)
        {
            // It's good practice to check if the detectable object is still valid (not destroyed)
            // before trying to access its components or properties.
            if (detectable == null || detectable.GetGameObject() == null)
            {
                Debug.Log($"<color=blue>RADAR INFO: A previously detected object (now destroyed or null) LEFT range.</color>");
                return;
            }
            
            Debug.Log($"<color=blue>RADAR INFO: {detectable.detectableName} LEFT range.</color>");
            // Example action: Revert the detected object's material color to white.
            Renderer renderer = detectable.GetGameObject().GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white; // Or revert to its original color
            }
        }

        void Update()
        {
            // Example of how to access currently detected objects on demand.
            if (Input.GetKeyDown(KeyCode.Space) && radarSystem != null)
            {
                Debug.Log("--- Current Radar Scan Snapshot (Press Space) ---");
                if (radarSystem.DetectedObjects.Count > 0)
                {
                    foreach (var detectable in radarSystem.DetectedObjects)
                    {
                        // Make sure the object is still valid before accessing its properties.
                        if (detectable != null && detectable.GetGameObject() != null)
                        {
                            Debug.Log($"Currently tracking: {detectable.detectableName} at {detectable.GetTransform().position}");
                        }
                    }
                }
                else
                {
                    Debug.Log("No objects currently in radar range.");
                }
                Debug.Log("-------------------------------------------------");
            }
        }
    }
}
```

---

### How to Use in Unity:

1.  **Create C# Scripts:**
    *   Save the `IDetectable` interface and `DetectableTarget` class into a file named `DetectableTarget.cs` in your Unity project.
    *   Save the `RadarDetectionSystem` and `RadarListenerExample` classes into a file named `RadarDetectionSystem.cs` (or `Radar.cs` and `RadarListener.cs` if you prefer separate files for clarity) in your Unity project. Ensure all classes are within the `DesignPatterns.RadarDetectionSystem` namespace.

2.  **Create the Radar Emitter:**
    *   In your Unity scene, create an empty GameObject (e.g., named "PlayerRadar").
    *   Add the `RadarDetectionSystem` component to this GameObject.
    *   In the Inspector, configure its properties:
        *   **Detection Range:** Set a value (e.g., `15`). This is the radius for detection.
        *   **Detection Interval:** Set how often the radar scans (e.g., `0.3` seconds).
        *   **Detectable Layers:** Use the dropdown to select which Unity Layers your detectable objects will be on (e.g., check "Default" or create a new "Targets" layer). This is important for performance.
        *   **Detectable Tag:** (Optional) If you want to *only* detect objects with a specific tag (e.g., "Enemy"), type that tag here. Leave blank to detect all objects on the specified layers.
        *   **Draw Gizmos:** Keep checked to see the radar's range and detected objects in the Scene view.

3.  **Create Detectable Objects:**
    *   Create several 3D objects in your scene (e.g., `Cube`, `Sphere`, `Capsule`). Position them randomly.
    *   For *each* of these objects:
        *   Ensure it has a `Collider` component (e.g., `BoxCollider`, `SphereCollider`). This is critical for `Physics.OverlapSphere`.
        *   Add the `DetectableTarget` component to it.
        *   In the Inspector for `DetectableTarget`:
            *   Set a unique `Target Name` (e.g., "Enemy Ship 1", "Cargo Drone", "Resource Node").
        *   Set the GameObject's **Layer** to one you selected in the `RadarDetectionSystem`'s `Detectable Layers`.
        *   (Optional) If you specified a `Detectable Tag` on the `RadarDetectionSystem`, assign that tag to these objects.
        *   To see the `RadarListenerExample`'s color-changing effect, ensure the object also has a `MeshRenderer` (which 3D objects usually do by default).

4.  **Create a Radar Listener (for demonstration):**
    *   Create another empty GameObject in your scene (e.g., "RadarDebugger").
    *   Add the `RadarListenerExample` component to this GameObject.
    *   In the Inspector for `RadarListenerExample`:
        *   Drag your "PlayerRadar" GameObject (the one with the `RadarDetectionSystem`) into the `Radar System` field.

5.  **Run the Scene:**
    *   Enter Play Mode.
    *   Observe the Console window for messages.
    *   As you move the `DetectableTarget` objects into and out of the `PlayerRadar`'s detection range (visible as a yellow wire sphere in the Scene view), you will see "Radar detected" and "Radar lost" messages in the console.
    *   The `DetectableTarget` objects should turn red when detected and revert to white when they leave the range, demonstrating the `RadarListenerExample`'s reaction.
    *   Press the **Spacebar** during play mode to get an immediate snapshot of all objects currently within the radar's range.

This complete setup demonstrates a practical and extensible Radar Detection System in Unity, following good design pattern principles and Unity best practices.