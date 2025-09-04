// Unity Design Pattern Example: AISensingSystem
// This script demonstrates the AISensingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The AISensingSystem design pattern provides a structured way for AI agents to gather information about their environment through various "sensors." This pattern promotes modularity, allowing different types of detection (sight, hearing, proximity, damage, etc.) to be handled by distinct sensor components, while a central sensing system aggregates and provides access to this information for the AI's decision-making logic.

### Core Components of the AISensingSystem Pattern:

1.  **`SensorDetection` (Data Structure):** A lightweight structure to encapsulate the details of something detected by a sensor (e.g., the detected object, its position, detection strength, and the type of sensor that made the detection).
2.  **`AISensor` (Abstract Base Class):** Defines the common interface and behavior for all sensors. It handles enabling/disabling, detection frequency, and stores its own detected results. Concrete sensors will inherit from this.
3.  **Concrete `AISensor` Implementations:**
    *   **`SightSensor`:** Detects objects within a view cone, checking for line of sight.
    *   **`HearingSensor`:** Detects sound-emitting objects within a range. (Requires a simple `SoundSource` component for demonstration).
    *   **`ProximitySensor`:** Detects any object within a small radius.
    *   *(You can extend this with `DamageSensor`, `SmellSensor`, etc.)*
4.  **`AISensingSystem` (Central Manager):** Attached to the AI agent, this component discovers and manages all `AISensor` components on its GameObject. It orchestrates their updates and provides a unified API for the AI to query all aggregated sensor data.
5.  **`AIController` (Consumer):** An example AI script that utilizes the `AISensingSystem` to retrieve environmental information and make decisions based on it.

---

### Unity Implementation Walkthrough:

Here are the C# scripts for implementing the AISensingSystem pattern in Unity.

#### 1. `SensorDetection.cs`

This script defines the data structure for a single detection event.

```csharp
using UnityEngine;

// Enum to categorize different types of AI sensors.
// This helps in filtering and understanding sensor data.
public enum AISensorType {
    None,        // Default or unassigned type
    Sight,       // Visual detection
    Hearing,     // Auditory detection
    Proximity,   // Close-range physical detection
    Damage,      // Detection of incoming damage (e.g., for self-preservation)
    // Add more sensor types as needed for your game
}

/// <summary>
/// A struct representing a single detection event from an AI sensor.
/// It contains detailed information about what was detected.
/// </summary>
[System.Serializable]
public struct SensorDetection {
    [Tooltip("The GameObject that was detected.")]
    public GameObject detectedObject;

    [Tooltip("The world position where the detection occurred (e.g., center of detected object).")]
    public Vector3 detectedPosition;

    [Tooltip("A normalized value indicating the 'strength' or 'clarity' of the detection (0-1). " +
             "E.g., how visible, how loud, how close.")]
    [Range(0f, 1f)]
    public float detectionStrength;

    [Tooltip("The type of sensor that made this detection.")]
    public AISensorType sensorType;

    [Tooltip("A reference to the specific AISensor component that made this detection.")]
    public AISensor sourceSensor;

    [Tooltip("The Unity Time.time when this detection was made.")]
    public float detectionTime;

    /// <summary>
    /// Constructor for a new SensorDetection.
    /// </summary>
    public SensorDetection(GameObject obj, Vector3 pos, float strength, AISensorType type, AISensor sensor, float time) {
        detectedObject = obj;
        detectedPosition = pos;
        detectionStrength = Mathf.Clamp01(strength); // Ensure strength is between 0 and 1
        sensorType = type;
        sourceSensor = sensor;
        detectionTime = time;
    }

    /// <summary>
    /// Provides a string representation of the detection for debugging.
    /// </summary>
    public override string ToString() {
        return $"[{sensorType} Sensor] Detected: {detectedObject?.name ?? "NULL"} at {detectedPosition} (Strength: {detectionStrength:F2})";
    }
}
```

#### 2. `AISensor.cs`

This is the abstract base class for all specific sensors. It handles common properties and the detection frequency.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base class for all AI sensor components.
/// This class defines common properties and methods for sensors,
/// such as enabling/disabling, detection frequency, and storing detection results.
/// </summary>
public abstract class AISensor : MonoBehaviour {

    [Header("Base Sensor Settings")]
    [Tooltip("The type of this sensor.")]
    public AISensorType sensorType = AISensorType.None;

    [Tooltip("A unique name for this sensor within the AI system.")]
    public string sensorName = "Unnamed Sensor";

    [Tooltip("Is this sensor currently active and performing detections?")]
    public bool isEnabled = true;

    [Tooltip("How often, in seconds, should this sensor attempt to detect objects? " +
             "A value of 0 or less means it detects every frame.")]
    [Min(0f)]
    public float detectionFrequency = 0.2f; // Detect 5 times per second by default

    // Stores the time when the last detection cycle occurred for this sensor.
    protected float _lastDetectionTime;

    // A list to store the most recent objects detected by this sensor.
    // Cleared and repopulated on each detection cycle.
    protected List<SensorDetection> _currentDetections = new List<SensorDetection>();

    /// <summary>
    /// Provides read-only access to the list of currently detected objects by this sensor.
    /// </summary>
    public IReadOnlyList<SensorDetection> GetDetections() => _currentDetections;

    /// <summary>
    /// This method is called by the AISensingSystem to update the sensor's state.
    /// It handles the detection frequency logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    public void UpdateSensor(float deltaTime) {
        if (!isEnabled) {
            _currentDetections.Clear(); // Clear detections if disabled
            return;
        }

        // Only perform detection if enough time has passed since the last one.
        // Or if detectionFrequency is 0, perform every frame.
        if (detectionFrequency <= 0f || Time.time >= _lastDetectionTime + detectionFrequency) {
            _lastDetectionTime = Time.time;
            ClearCurrentDetections(); // Clear previous detections before performing new ones
            PerformDetection();       // Call the abstract method for specific detection logic
        }
    }

    /// <summary>
    /// Clears the internal list of current detections.
    /// </summary>
    protected void ClearCurrentDetections() {
        _currentDetections.Clear();
    }

    /// <summary>
    /// Adds a new SensorDetection to the internal list.
    /// </summary>
    /// <param name="detection">The SensorDetection object to add.</param>
    protected void AddDetection(SensorDetection detection) {
        _currentDetections.Add(detection);
    }

    /// <summary>
    /// Abstract method that concrete sensor implementations must override.
    /// This is where the actual detection logic specific to each sensor type resides.
    /// </summary>
    protected abstract void PerformDetection();

    /// <summary>
    /// Optional: Implement OnDrawGizmos or OnDrawGizmosSelected in derived classes
    /// to visualize the sensor's detection range and field in the Unity Editor.
    /// </summary>
    protected virtual void OnDrawGizmosSelected() {
        if (isEnabled) {
            Gizmos.color = Color.yellow;
            // Base class doesn't draw anything specific, derived classes will.
        }
    }
}
```

#### 3. `SightSensor.cs`

Detects objects within a specified range and view cone, checking for line of sight.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A concrete AI sensor that simulates visual detection (sight).
/// It detects objects within a specified range and angle, checking for line of sight.
/// </summary>
public class SightSensor : AISensor {

    [Header("Sight Sensor Settings")]
    [Tooltip("The maximum distance this sensor can detect objects.")]
    public float detectionRange = 10f;

    [Tooltip("The horizontal field of view angle in degrees. Objects outside this angle are not detected.")]
    [Range(0f, 360f)]
    public float viewAngle = 90f;

    [Tooltip("Which layers should be considered for detection?")]
    public LayerMask detectionLayers;

    [Tooltip("Which layers should block line of sight (e.g., walls, obstacles)?")]
    public LayerMask obstacleLayers;

    void Awake() {
        sensorType = AISensorType.Sight;
        if (string.IsNullOrEmpty(sensorName) || sensorName == "Unnamed Sensor") {
            sensorName = "Sight Sensor";
        }
    }

    protected override void PerformDetection() {
        // Find all colliders within the detection range using an overlap sphere.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, detectionLayers);

        foreach (Collider hitCollider in hitColliders) {
            // Don't detect self
            if (hitCollider.gameObject == gameObject) {
                continue;
            }

            Vector3 targetPosition = hitCollider.transform.position;
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;

            // 1. Check if target is within the view angle
            if (Vector3.Angle(transform.forward, directionToTarget) <= viewAngle / 2f) {

                // 2. Check for line of sight (no obstacles in between)
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToTarget, out hit, detectionRange, obstacleLayers | detectionLayers)) {
                    if (hit.collider.gameObject == hitCollider.gameObject) {
                        // Target is visible, add to detections
                        float distance = Vector3.Distance(transform.position, targetPosition);
                        // Strength decreases with distance
                        float strength = 1f - (distance / detectionRange); 
                        
                        AddDetection(new SensorDetection(
                            hitCollider.gameObject,
                            hit.point, // Use hit point for more accurate detected position
                            strength,
                            AISensorType.Sight,
                            this,
                            Time.time
                        ));
                    }
                }
            }
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        if (isEnabled) {
            Gizmos.color = Color.cyan;

            // Draw detection range sphere (wireframe)
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw view cone
            Vector3 forward = transform.forward * detectionRange;
            Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * forward;
            Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * forward;

            Gizmos.DrawLine(transform.position, transform.position + left);
            Gizmos.DrawLine(transform.position, transform.position + right);
            Gizmos.DrawLine(transform.position + left, transform.position + right); // Connect the arc ends (simplified)

            // Draw direction ray
            Gizmos.DrawRay(transform.position, transform.forward * detectionRange);
        }
    }
}
```

#### 4. `SoundSource.cs` (Helper for HearingSensor)

A simple component to mark GameObjects as sound sources, so `HearingSensor` can detect them.

```csharp
using UnityEngine;

/// <summary>
/// A simple component to mark a GameObject as a source of sound for the HearingSensor.
/// AI agents with a HearingSensor can detect GameObjects with this component.
/// </summary>
public class SoundSource : MonoBehaviour {
    [Tooltip("The 'volume' or 'loudness' of this sound source. " +
             "A higher value means it can be detected from further away.")]
    [Min(0.1f)]
    public float soundLoudness = 1.0f;

    [Tooltip("The GameObject that made the sound. Defaults to this GameObject.")]
    public GameObject sourceObject;

    void Awake() {
        if (sourceObject == null) {
            sourceObject = gameObject;
        }
    }

    // You could add methods here to "make a sound" that triggers effects or events
    // For this example, just the presence and loudness are enough.
    public void PlaySoundEffect() {
        Debug.Log($"{sourceObject.name} is making a sound with loudness {soundLoudness}");
        // Example: Play an actual audio clip
        // AudioSource audio = GetComponent<AudioSource>();
        // if (audio != null) audio.Play();
    }
}
```

#### 5. `HearingSensor.cs`

Detects `SoundSource` components within range.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A concrete AI sensor that simulates auditory detection (hearing).
/// It detects GameObjects that have a 'SoundSource' component within a specified range.
/// </summary>
public class HearingSensor : AISensor {

    [Header("Hearing Sensor Settings")]
    [Tooltip("The maximum distance this sensor can 'hear' sounds.")]
    public float hearingRange = 15f;

    [Tooltip("Which layers should be considered for detecting sound sources? " +
             "Only GameObjects with a SoundSource component on these layers will be detected.")]
    public LayerMask soundSourceLayers;

    void Awake() {
        sensorType = AISensorType.Hearing;
        if (string.IsNullOrEmpty(sensorName) || sensorName == "Unnamed Sensor") {
            sensorName = "Hearing Sensor";
        }
    }

    protected override void PerformDetection() {
        // Find all colliders within the hearing range.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRange, soundSourceLayers);

        foreach (Collider hitCollider in hitColliders) {
            // Don't detect self
            if (hitCollider.gameObject == gameObject) {
                continue;
            }

            // Check if the detected collider has a SoundSource component
            SoundSource soundSource = hitCollider.GetComponent<SoundSource>();
            if (soundSource != null && soundSource.sourceObject != null) {
                float distance = Vector3.Distance(transform.position, soundSource.sourceObject.transform.position);

                // Calculate detection strength based on distance and sound loudness
                // Strength decreases with distance, increases with loudness
                float baseStrength = soundSource.soundLoudness / 10f; // Normalize loudness somewhat
                float distanceFactor = 1f - (distance / hearingRange);
                float strength = Mathf.Clamp01(baseStrength + distanceFactor);

                AddDetection(new SensorDetection(
                    soundSource.sourceObject,
                    soundSource.sourceObject.transform.position,
                    strength,
                    AISensorType.Hearing,
                    this,
                    Time.time
                ));
            }
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        if (isEnabled) {
            Gizmos.color = Color.magenta;
            // Draw hearing range sphere (wireframe)
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }
    }
}
```

#### 6. `ProximitySensor.cs`

Detects any object within a small, spherical radius.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A concrete AI sensor that detects objects within a small, spherical proximity.
/// This is useful for detecting things very close to the AI agent.
/// </summary>
public class ProximitySensor : AISensor {

    [Header("Proximity Sensor Settings")]
    [Tooltip("The radius of the proximity detection sphere.")]
    public float detectionRadius = 1.5f;

    [Tooltip("Which layers should be considered for proximity detection?")]
    public LayerMask detectionLayers;

    void Awake() {
        sensorType = AISensorType.Proximity;
        if (string.IsNullOrEmpty(sensorName) || sensorName == "Unnamed Sensor") {
            sensorName = "Proximity Sensor";
        }
    }

    protected override void PerformDetection() {
        // Find all colliders within the proximity radius.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayers);

        foreach (Collider hitCollider in hitColliders) {
            // Don't detect self
            if (hitCollider.gameObject == gameObject) {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            // Strength is inversely proportional to distance (closer = stronger)
            float strength = 1f - (distance / detectionRadius);

            AddDetection(new SensorDetection(
                hitCollider.gameObject,
                hitCollider.transform.position,
                strength,
                AISensorType.Proximity,
                this,
                Time.time
            ));
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        if (isEnabled) {
            Gizmos.color = Color.red;
            // Draw proximity sphere (wireframe)
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
```

#### 7. `AISensingSystem.cs`

This is the central manager that holds and updates all sensors, providing an aggregated view of the environment.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like .Where() and .Select()

/// <summary>
/// The central AI Sensing System that manages multiple AISensor components.
/// It aggregates detections from all attached sensors and provides a unified interface
/// for AI controllers to query environmental information.
/// </summary>
public class AISensingSystem : MonoBehaviour {

    [Tooltip("A list of all AISensor components managed by this system. " +
             "Automatically populated from components on this GameObject in Awake.")]
    [SerializeField]
    private List<AISensor> _sensors = new List<AISensor>();

    // A cached list of all current detections aggregated from all active sensors.
    private List<SensorDetection> _allCurrentDetections = new List<SensorDetection>();

    /// <summary>
    /// Provides read-only access to all detections aggregated from all active sensors
    /// during the last update cycle.
    /// </summary>
    public IReadOnlyList<SensorDetection> AllCurrentDetections => _allCurrentDetections;

    // --- Events (Optional, but very useful for reactive AI) ---
    // Event fired when a new object is detected by any sensor.
    public event System.Action<SensorDetection> OnNewDetection;
    // Event fired when a previously detected object is no longer detected.
    public event System.Action<GameObject> OnLostDetection;

    // Keep track of previously detected objects to fire OnLostDetection
    private Dictionary<GameObject, List<AISensor>> _previouslyDetectedObjects = new Dictionary<GameObject, List<AISensor>>();


    void Awake() {
        // Automatically find all AISensor components attached to this GameObject.
        _sensors = GetComponents<AISensor>().ToList();

        if (_sensors.Count == 0) {
            Debug.LogWarning($"AISensingSystem on {gameObject.name} found no AISensor components.", this);
        }
    }

    void Update() {
        // Store objects detected in the current frame
        HashSet<GameObject> currentFrameDetectedObjects = new HashSet<GameObject>();
        // Store current detections for event firing
        List<SensorDetection> currentFrameDetections = new List<SensorDetection>();

        // Clear the aggregated list before populating with fresh data
        _allCurrentDetections.Clear();

        foreach (AISensor sensor in _sensors) {
            if (sensor.isEnabled) {
                sensor.UpdateSensor(Time.deltaTime); // Each sensor performs its specific detection logic
                var sensorDetections = sensor.GetDetections();
                
                // Aggregate all detections from this sensor
                _allCurrentDetections.AddRange(sensorDetections);
                currentFrameDetections.AddRange(sensorDetections);

                foreach (var detection in sensorDetections) {
                    currentFrameDetectedObjects.Add(detection.detectedObject);
                }
            }
        }

        // --- Event Firing Logic (Optional but recommended) ---
        // Identify new detections and lost detections
        List<GameObject> lostObjects = new List<GameObject>();
        foreach (var entry in _previouslyDetectedObjects) {
            GameObject prevDetectedObj = entry.Key;
            List<AISensor> prevDetectingSensors = entry.Value;

            bool stillDetected = false;
            foreach (AISensor sensor in prevDetectingSensors) {
                if (sensor.isEnabled && sensor.GetDetections().Any(d => d.detectedObject == prevDetectedObj)) {
                    stillDetected = true;
                    break;
                }
            }

            if (!stillDetected) {
                lostObjects.Add(prevDetectedObj);
            }
        }

        foreach (GameObject obj in lostObjects) {
            OnLostDetection?.Invoke(obj);
            _previouslyDetectedObjects.Remove(obj);
        }

        // Handle OnNewDetection for newly detected objects
        foreach (var detection in currentFrameDetections) {
            if (!_previouslyDetectedObjects.ContainsKey(detection.detectedObject)) {
                OnNewDetection?.Invoke(detection);
                _previouslyDetectedObjects[detection.detectedObject] = new List<AISensor>();
            }
            // Update which sensors are currently detecting this object
            if (!_previouslyDetectedObjects[detection.detectedObject].Contains(detection.sourceSensor)) {
                _previouslyDetectedObjects[detection.detectedObject].Add(detection.sourceSensor);
            }
        }

        // Remove sensors from _previouslyDetectedObjects that are no longer detecting
        foreach (GameObject obj in _previouslyDetectedObjects.Keys.ToList()) { // Use ToList to modify while iterating
            _previouslyDetectedObjects[obj].RemoveAll(s => !s.GetDetections().Any(d => d.detectedObject == obj));
            if (_previouslyDetectedObjects[obj].Count == 0) {
                 // Object is truly lost across all sensors if list is empty
                 // This case is already covered by the 'lostObjects' logic above.
                 // This part ensures that if an object is only detected by a specific sensor
                 // that sensor gets removed when it stops detecting it.
                 // The 'lostObjects' logic is superior for overall "lost" status.
            }
        }
    }

    /// <summary>
    /// Retrieves all detected objects by all active sensors, optionally filtered by type.
    /// </summary>
    /// <param name="type">The specific type of sensor detections to retrieve. Use AISensorType.None for all types.</param>
    /// <returns>A list of GameObjects that are currently detected.</returns>
    public List<GameObject> GetDetectedObjects(AISensorType type = AISensorType.None) {
        if (type == AISensorType.None) {
            return _allCurrentDetections.Select(d => d.detectedObject).Distinct().ToList();
        } else {
            return _allCurrentDetections
                .Where(d => d.sensorType == type)
                .Select(d => d.detectedObject)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>
    /// Checks if a specific GameObject is currently detected by any active sensor.
    /// </summary>
    /// <param name="target">The GameObject to check for.</param>
    /// <returns>True if the target is detected, false otherwise.</returns>
    public bool HasDetected(GameObject target) {
        return _allCurrentDetections.Any(d => d.detectedObject == target);
    }

    /// <summary>
    /// Checks if a specific GameObject is currently detected by a specific type of sensor.
    /// </summary>
    /// <param name="target">The GameObject to check for.</param>
    /// <param name="type">The type of sensor to check.</param>
    /// <returns>True if the target is detected by the specified sensor type, false otherwise.</returns>
    public bool HasDetected(GameObject target, AISensorType type) {
        return _allCurrentDetections.Any(d => d.detectedObject == target && d.sensorType == type);
    }

    /// <summary>
    /// Retrieves the most recent detection for a specific GameObject, regardless of sensor type.
    /// </summary>
    /// <param name="target">The GameObject whose detection to retrieve.</param>
    /// <returns>The most recent SensorDetection struct, or a default/invalid one if not found.</returns>
    public SensorDetection GetLatestDetectionFor(GameObject target) {
        return _allCurrentDetections
            .Where(d => d.detectedObject == target)
            .OrderByDescending(d => d.detectionTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Retrieves a specific sensor by its name.
    /// </summary>
    /// <param name="name">The name of the sensor to find.</param>
    /// <returns>The AISensor component, or null if not found.</returns>
    public AISensor GetSensor(string name) {
        return _sensors.Find(s => s.sensorName == name);
    }

    /// <summary>
    /// Retrieves a specific sensor by its type. Returns the first one found if multiple exist.
    /// </summary>
    /// <param name="type">The type of the sensor to find.</param>
    /// <returns>The AISensor component, or null if not found.</returns>
    public AISensor GetSensor(AISensorType type) {
        return _sensors.Find(s => s.sensorType == type);
    }

    /// <summary>
    /// Retrieves all sensors of a specific type.
    /// </summary>
    /// <param name="type">The type of sensors to find.</param>
    /// <returns>A list of AISensor components matching the type.</returns>
    public List<AISensor> GetSensors(AISensorType type) {
        return _sensors.Where(s => s.sensorType == type).ToList();
    }
}
```

#### 8. `AIController.cs` (Example Usage)

This script demonstrates how an AI agent would use the `AISensingSystem` to make decisions.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// An example AI Controller that utilizes the AISensingSystem to react to environmental cues.
/// This script demonstrates how to query the sensing system and respond to detections.
/// </summary>
[RequireComponent(typeof(AISensingSystem))] // Ensure the AI has a sensing system
public class AIController : MonoBehaviour {

    [Header("AI Behavior Settings")]
    [Tooltip("The speed at which the AI agent moves.")]
    public float moveSpeed = 3f;

    [Tooltip("The GameObject to follow if detected.")]
    private GameObject _targetToFollow;

    // Reference to the AISensingSystem on this GameObject
    private AISensingSystem _sensingSystem;

    void Awake() {
        _sensingSystem = GetComponent<AISensingSystem>();
    }

    void OnEnable() {
        // Subscribe to events from the sensing system for immediate reactions
        _sensingSystem.OnNewDetection += HandleNewDetection;
        _sensingSystem.OnLostDetection += HandleLostDetection;
    }

    void OnDisable() {
        // Unsubscribe from events to prevent memory leaks
        _sensingSystem.OnNewDetection -= HandleNewDetection;
        _sensingSystem.OnLostDetection -= HandleLostDetection;
    }

    void Update() {
        // --- Decision Making Based on Aggregated Sensor Data ---

        // Example 1: Prioritize sight detections
        if (_targetToFollow != null) {
            // If we have a target from a previous detection, try to follow it
            FollowTarget(_targetToFollow);
            Debug.Log($"{gameObject.name} is following {_targetToFollow.name}.");
        } else {
            // If no immediate target, check all current detections
            IReadOnlyList<SensorDetection> allDetections = _sensingSystem.AllCurrentDetections;

            if (allDetections.Count > 0) {
                // Find the "most important" detection. This logic can vary greatly.
                // For example, prioritize sight, then hearing, then proximity.
                SensorDetection? primaryDetection = FindPrimaryDetection(allDetections);

                if (primaryDetection.HasValue) {
                    _targetToFollow = primaryDetection.Value.detectedObject;
                    Debug.Log($"{gameObject.name} identified a new primary target: {_targetToFollow.name} via {primaryDetection.Value.sensorType} sensor.");
                } else {
                    Wander();
                }
            } else {
                Wander(); // No detections, just wander around
            }
        }

        // Example 2: React specifically to proximity detections
        List<GameObject> proximityDetections = _sensingSystem.GetDetectedObjects(AISensorType.Proximity);
        if (proximityDetections.Any()) {
            Debug.LogWarning($"{gameObject.name} is too close to something: {proximityDetections.First().name}!");
            // Perform evasive maneuvers or an attack here
        }
    }

    /// <summary>
    /// Determines the most relevant detection from a list of all current detections.
    /// This is where AI prioritization logic would live.
    /// </summary>
    private SensorDetection? FindPrimaryDetection(IReadOnlyList<SensorDetection> detections) {
        // Example priority: Sight > Hearing > Proximity
        var sightDetections = detections.Where(d => d.sensorType == AISensorType.Sight).OrderByDescending(d => d.detectionStrength);
        if (sightDetections.Any()) return sightDetections.First();

        var hearingDetections = detections.Where(d => d.sensorType == AISensorType.Hearing).OrderByDescending(d => d.detectionStrength);
        if (hearingDetections.Any()) return hearingDetections.First();

        var proximityDetections = detections.Where(d => d.sensorType == AISensorType.Proximity).OrderByDescending(d => d.detectionStrength);
        if (proximityDetections.Any()) return proximityDetections.First();

        return null; // No primary detection found
    }

    // --- Reaction Methods (Triggered by SensingSystem Events) ---

    private void HandleNewDetection(SensorDetection detection) {
        Debug.Log($"<color=green>{gameObject.name}</color> **EVENT**: New detection by {detection.sourceSensor.sensorName} of {detection.detectedObject.name}!");
        // If we don't have a target, assign the first new detection as target
        if (_targetToFollow == null) {
            _targetToFollow = detection.detectedObject;
        }
        // Specific reactions can be triggered here
        // E.g., if (detection.sensorType == AISensorType.Hearing) { InvestigateSound(detection.detectedPosition); }
    }

    private void HandleLostDetection(GameObject lostObject) {
        Debug.Log($"<color=red>{gameObject.name}</color> **EVENT**: Lost detection of {lostObject.name}!");
        if (_targetToFollow == lostObject) {
            _targetToFollow = null; // Clear target if it was the one we lost
            Debug.Log($"{gameObject.name} lost its target, now wandering.");
        }
    }

    // --- AI Action Methods ---

    void FollowTarget(GameObject target) {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.LookAt(target.transform);
    }

    void Wander() {
        // Simple placeholder for wandering
        // In a real game, this would involve pathfinding, random movement, etc.
        // Debug.Log($"{gameObject.name} is wandering...");
        // Example: just slowly rotate
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
    }

    void OnDrawGizmos() {
        if (_targetToFollow != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetToFollow.transform.position);
            Gizmos.DrawSphere(_targetToFollow.transform.position, 0.5f);
        }
    }
}
```

---

### How to Use in Unity (Example Setup):

1.  **Create an Empty Project** or open an existing one.
2.  **Create C# Scripts:**
    *   Create `SensorDetection.cs` and paste its code.
    *   Create `AISensor.cs` and paste its code.
    *   Create `SightSensor.cs` and paste its code.
    *   Create `SoundSource.cs` and paste its code.
    *   Create `HearingSensor.cs` and paste its code.
    *   Create `ProximitySensor.cs` and paste its code.
    *   Create `AISensingSystem.cs` and paste its code.
    *   Create `AIController.cs` and paste its code.
3.  **Setup the AI Agent:**
    *   Create an empty GameObject in your scene, name it `AI_Agent`.
    *   Add a `CharacterController` component to `AI_Agent` (or a Rigidbody and Collider) for physical interaction.
    *   Add `AISensingSystem.cs` to `AI_Agent`.
    *   Add `SightSensor.cs` to `AI_Agent`. Configure its `Detection Range`, `View Angle`, `Detection Layers`, and `Obstacle Layers`.
    *   Add `HearingSensor.cs` to `AI_Agent`. Configure its `Hearing Range` and `Sound Source Layers`.
    *   Add `ProximitySensor.cs` to `AI_Agent`. Configure its `Detection Radius` and `Detection Layers`.
    *   Add `AIController.cs` to `AI_Agent`. Configure its `Move Speed`.
    *   (Optional but recommended for visualization): Add a simple mesh (e.g., a `Cube`) as a child of `AI_Agent` to represent its body.
4.  **Setup Target Objects:**
    *   Create a 3D Object (e.g., a `Capsule`) in your scene, name it `Player_Target`.
    *   Add a `Rigidbody` component to `Player_Target` (uncheck `Use Gravity` or make it kinematic if you don't want it to fall).
    *   Add a `Collider` (e.g., `Capsule Collider`) to `Player_Target`.
    *   Add `SoundSource.cs` to `Player_Target`. Adjust its `Sound Loudness`.
    *   Place `Player_Target` on a layer that your sensors can detect (e.g., "Player" layer for Sight and Hearing, "Default" for Proximity, configured in each sensor's Inspector).
    *   Create another 3D Object (e.g., a `Sphere`), name it `Obstacle`. Add a `Collider` to it. Place it on a layer specified as `Obstacle Layers` in your `SightSensor`.
5.  **Configure Layers:**
    *   Go to `Edit > Project Settings > Tags and Layers`.
    *   Add new layers, e.g., "Player" and "Obstacle".
    *   Assign your `Player_Target` to the "Player" layer.
    *   Assign your `Obstacle` to the "Obstacle" layer.
    *   In the Inspector for `AI_Agent`'s sensors, select the appropriate layers for `Detection Layers`, `Obstacle Layers`, and `Sound Source Layers` using the dropdown menus.
6.  **Run the Scene:**
    *   Move the `Player_Target` around the `AI_Agent`.
    *   Observe the `AI_Agent` reacting to the `Player_Target` based on its sensors (following, getting warned about proximity).
    *   Place an `Obstacle` between the `AI_Agent` and `Player_Target` to block line of sight for the `SightSensor`.
    *   Check the console for `Debug.Log` messages from the `AIController` and `AISensingSystem` events.

This setup creates a functional and educational example of the AISensingSystem pattern, ready for expansion with more complex AI behaviors and additional sensor types.