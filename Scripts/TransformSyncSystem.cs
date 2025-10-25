// Unity Design Pattern Example: TransformSyncSystem
// This script demonstrates the TransformSyncSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive example demonstrates the 'TransformSyncSystem' design pattern in Unity. This pattern centralizes the management and updating of `Transform` properties (position, rotation, scale) for multiple game objects. It's particularly useful in scenarios like networking (synchronizing remote player movements), AI following, or any system where a group of objects needs to consistently track or interpolate towards other transforms.

**Core Concepts:**

1.  **`ITransformSyncable` Interface:** Defines the contract for any object that wants its transform to be managed by the system. It exposes the `TargetTransform` (its own transform to be updated), the `SourceTransform` (the transform it should follow), and a `SyncSpeed`.
2.  **`TransformSyncSystem` (Manager):** A centralized `MonoBehaviour` (implemented as a singleton) that holds a collection of `ITransformSyncable` objects. In its `LateUpdate` method, it iterates through all registered syncables and performs the actual transform interpolation (e.g., `Lerp` for position, `Slerp` for rotation) from the `SourceTransform` to the `TargetTransform`.
3.  **`ExampleTransformSyncable` (Client):** A concrete `MonoBehaviour` that implements `ITransformSyncable`. It registers itself with the `TransformSyncSystem` in `Awake()` and unregisters in `OnDestroy()`, effectively opting into the system's management.

---

### File Structure:

You'll create three C# scripts:

1.  `ITransformSyncable.cs` (Interface definition)
2.  `TransformSyncSystem.cs` (The core manager system)
3.  `ExampleTransformSyncable.cs` (An example client component)

---

### 1. `ITransformSyncable.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.TransformSyncSystem
{
    /// <summary>
    /// ITransformSyncable Interface
    ///
    /// This interface defines the contract for any object that wants its transform
    /// to be managed and synchronized by the TransformSyncSystem.
    ///
    /// The 'TransformSyncSystem' pattern centralizes the logic for updating
    /// the transforms of multiple game objects, rather than each object
    /// managing its own transform update logic independently.
    ///
    /// Implementers of this interface provide:
    /// 1. TargetTransform: The transform *this object* owns and wants to be updated.
    /// 2. SourceTransform: The transform *this object* wants to follow or sync with.
    /// 3. SyncSpeed: How fast the TargetTransform should move/rotate/scale towards the SourceTransform.
    /// </summary>
    public interface ITransformSyncable
    {
        /// <summary>
        /// Gets the Transform that will be updated by the TransformSyncSystem.
        /// This is typically the transform of the implementing MonoBehaviour itself.
        /// </summary>
        Transform TargetTransform { get; }

        /// <summary>
        /// Gets the Transform that the TargetTransform should follow or synchronize with.
        /// This is the "leader" or reference transform.
        /// </summary>
        Transform SourceTransform { get; }

        /// <summary>
        /// Gets the speed at which the TargetTransform interpolates towards the SourceTransform.
        /// A higher value means faster synchronization. This value is used in a Lerp/Slerp function.
        /// </summary>
        float SyncSpeed { get; }
    }
}

```

### 2. `TransformSyncSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace DesignPatterns.TransformSyncSystem
{
    /// <summary>
    /// TransformSyncSystem
    ///
    /// This is the core 'System' responsible for managing and performing
    /// transform synchronization for all registered ITransformSyncable objects.
    ///
    /// Pattern Role: Centralized Manager / Singleton.
    /// - It maintains a collection of all objects that need transform synchronization.
    /// - It iterates through these objects in its LateUpdate loop and applies the
    ///   synchronization logic (e.g., Lerp for smooth following).
    /// - Using LateUpdate ensures that all other object updates (movement, physics, AI)
    ///   have already occurred, preventing "jitter" or out-of-order updates.
    /// - It decouples the synchronization logic from individual syncable objects,
    ///   making the system more maintainable and extensible.
    /// </summary>
    [DefaultExecutionOrder(100)] // Ensures this runs after most other scripts that might move SourceTransforms.
    public class TransformSyncSystem : MonoBehaviour
    {
        // Singleton instance to allow easy access from other components.
        public static TransformSyncSystem Instance { get; private set; }

        // Using a HashSet for efficient addition, removal, and prevention of duplicate registrations.
        // It stores references to all ITransformSyncable objects that need to be synced.
        private readonly HashSet<ITransformSyncable> _syncables = new HashSet<ITransformSyncable>();

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the singleton instance.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("TransformSyncSystem: Multiple instances found. Destroying duplicate.", this);
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                // Optional: Make the system persistent across scene loads if needed.
                // DontDestroyOnLoad(this.gameObject); 
            }
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed.
        /// Clears the singleton instance reference.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Registers an ITransformSyncable object with the system.
        /// Once registered, the system will manage its transform updates.
        /// </summary>
        /// <param name="syncable">The object implementing ITransformSyncable to register.</param>
        public void Register(ITransformSyncable syncable)
        {
            if (syncable == null)
            {
                Debug.LogWarning("TransformSyncSystem: Attempted to register a null syncable object.");
                return;
            }
            if (_syncables.Add(syncable))
            {
                // Debug.Log($"TransformSyncSystem: Registered {syncable.TargetTransform.name}");
            }
            else
            {
                // Debug.LogWarning($"TransformSyncSystem: {syncable.TargetTransform.name} already registered.");
            }
        }

        /// <summary>
        /// Unregisters an ITransformSyncable object from the system.
        /// The system will no longer manage its transform updates.
        /// </summary>
        /// <param name="syncable">The object implementing ITransformSyncable to unregister.</param>
        public void Unregister(ITransformSyncable syncable)
        {
            if (syncable == null)
            {
                Debug.LogWarning("TransformSyncSystem: Attempted to unregister a null syncable object.");
                return;
            }
            if (_syncables.Remove(syncable))
            {
                // Debug.Log($"TransformSyncSystem: Unregistered {syncable.TargetTransform.name}");
            }
            else
            {
                // Debug.LogWarning($"TransformSyncSystem: {syncable.TargetTransform.name} was not registered.");
            }
        }

        /// <summary>
        /// LateUpdate is called once per frame, after all Update functions have been called.
        /// This is the ideal place for transform synchronization to ensure that
        /// all movement logic for the source objects has already been computed.
        /// </summary>
        private void LateUpdate()
        {
            if (_syncables.Count == 0) return;

            // To be robust against modifications to _syncables during iteration (e.g., if a syncable
            // object gets destroyed during another LateUpdate), we iterate over a temporary copy.
            // This creates a small amount of garbage, but is safer for dynamic object lifecycles.
            // For systems with very high-frequency updates or strict GC constraints, a more
            // sophisticated pending add/remove list management might be used.
            List<ITransformSyncable> currentSyncables = new List<ITransformSyncable>(_syncables);

            foreach (var syncable in currentSyncables)
            {
                // Robustness check: Ensure the syncable object and its transforms are still valid.
                // Objects might be destroyed between registration and this LateUpdate call.
                if (syncable == null || syncable.TargetTransform == null || syncable.SourceTransform == null)
                {
                    Debug.LogWarning($"TransformSyncSystem: Syncable object or its transforms are null. Unregistering potentially invalid entry.", this);
                    _syncables.Remove(syncable); // Clean up the invalid entry from the original HashSet
                    continue; 
                }

                Transform target = syncable.TargetTransform;
                Transform source = syncable.SourceTransform;
                float speed = syncable.SyncSpeed;

                // Calculate interpolation factor. Clamp01 prevents overshooting if speed * Time.deltaTime is large.
                float t = Mathf.Clamp01(speed * Time.deltaTime);

                // Lerp (Linear Interpolation) for position
                target.position = Vector3.Lerp(target.position, source.position, t);

                // Slerp (Spherical Linear Interpolation) for rotation, for smooth rotation transitions
                target.rotation = Quaternion.Slerp(target.rotation, source.rotation, t);

                // Optional: Lerp scale. Often, scale is not synced or handled differently depending on the use case.
                // target.localScale = Vector3.Lerp(target.localScale, source.localScale, t);
            }
        }
    }
}
```

### 3. `ExampleTransformSyncable.cs`

```csharp
using UnityEngine;

namespace DesignPatterns.TransformSyncSystem
{
    /// <summary>
    /// ExampleTransformSyncable
    ///
    /// This is a concrete implementation of ITransformSyncable.
    /// It represents a game object that wants to follow another object's transform.
    ///
    /// Pattern Role: Client / Implementer.
    /// - Implements the ITransformSyncable interface, providing its own transform
    ///   as the target and a specified 'source' transform to follow.
    /// - Registers itself with the TransformSyncSystem in Awake().
    /// - Unregisters itself in OnDestroy() to clean up references in the system.
    /// </summary>
    public class ExampleTransformSyncable : MonoBehaviour, ITransformSyncable
    {
        [Tooltip("The Transform this object will follow. Drag the 'leader' object here.")]
        [SerializeField] private Transform _sourceTransform;

        [Tooltip("The speed at which this object will follow the Source Transform. Higher value means faster sync.")]
        [SerializeField] private float _syncSpeed = 5f; // Interpolation speed

        // Public properties implementing ITransformSyncable
        public Transform TargetTransform => this.transform; // The transform of this GameObject
        public Transform SourceTransform => _sourceTransform; // The transform it will follow
        public float SyncSpeed => _syncSpeed; // The speed of synchronization

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Registers this object with the TransformSyncSystem.
        /// </summary>
        private void Awake()
        {
            // Ensure the TransformSyncSystem exists in the scene before trying to register.
            if (TransformSyncSystem.Instance == null)
            {
                Debug.LogError($"TransformSyncSystem: No instance found in the scene for {gameObject.name}. " +
                               "Make sure you have an active GameObject with TransformSyncSystem attached.", this);
                enabled = false; // Disable this component if the system isn't available
                return;
            }

            if (_sourceTransform == null)
            {
                Debug.LogWarning($"ExampleTransformSyncable on {gameObject.name}: _sourceTransform is not assigned. This object will not sync.", this);
                enabled = false;
                return;
            }

            TransformSyncSystem.Instance.Register(this);
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed.
        /// Unregisters this object from the TransformSyncSystem to clean up references.
        /// </summary>
        private void OnDestroy()
        {
            // Only unregister if the system still exists (e.g., not quitting application).
            // When quitting, the singleton might be destroyed before individual objects.
            if (TransformSyncSystem.Instance != null)
            {
                TransformSyncSystem.Instance.Unregister(this);
            }
        }

        /// <summary>
        /// Optional: For visual debugging in the editor. Draws a line to the source.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && _sourceTransform != null && TargetTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(TargetTransform.position, _sourceTransform.position);
                Gizmos.DrawSphere(_sourceTransform.position, 0.2f); // Mark the source
            }
        }
    }
}
```

---

### Example Usage in a Unity Project:

To implement this in your Unity scene:

1.  **Create the TransformSyncSystem Manager:**
    *   In your Unity scene, create an empty GameObject (e.g., rename it to "TransformSyncManager").
    *   Drag and drop the `TransformSyncSystem.cs` script onto this GameObject. This will create the single instance of your synchronization system.

2.  **Create a Source Object (the "Leader"):**
    *   Create any GameObject (e.g., a 3D Sphere, Cube, or an empty GameObject). Rename it "LeaderObject".
    *   To make it move, you can add a simple script. Create a new C# script named `SimpleMover.cs` and paste the following:

    ```csharp
    using UnityEngine;

    namespace DesignPatterns.TransformSyncSystem
    {
        public class SimpleMover : MonoBehaviour
        {
            [SerializeField] private float _moveSpeed = 1f;
            [SerializeField] private float _rotationSpeed = 50f;
            [SerializeField] private Vector3 _moveDirection = Vector3.forward;

            void Update()
            {
                // Move in a local direction
                transform.Translate(_moveDirection * _moveSpeed * Time.deltaTime, Space.Self);
                // Rotate around its up axis
                transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
    ```
    *   Attach `SimpleMover.cs` to your "LeaderObject".

3.  **Create a Syncable Object (the "Follower"):**
    *   Create another GameObject (e.g., a 3D Cube or a different shape). Rename it "FollowerObject".
    *   Drag and drop the `ExampleTransformSyncable.cs` script onto this GameObject.
    *   In the Inspector for "FollowerObject", you will see the `Example Transform Syncable` component.
    *   Drag your "LeaderObject" from the Hierarchy into the `Source Transform` field of the `Example Transform Syncable` component.
    *   Adjust the `Sync Speed` as desired (e.g., values between 5 and 15 usually provide a smooth following effect).

4.  **Run the Scene:**
    *   You should now see the "FollowerObject" smoothly tracking the position and rotation of the "LeaderObject". The synchronization logic is entirely managed by the `TransformSyncSystem`.

---

### Benefits of the TransformSyncSystem Pattern:

*   **Decoupling:**
    *   `ExampleTransformSyncable` doesn't need to know *how* it's being synced; it just provides the necessary information (`TargetTransform`, `SourceTransform`, `SyncSpeed`).
    *   `TransformSyncSystem` doesn't need to know the specific class of each `ITransformSyncable`; it only interacts with the interface. This allows for diverse types of syncable objects.
*   **Centralized Control:** All synchronization logic resides in one place (`TransformSyncSystem`), making it easier to modify, debug, optimize, or even add features like network prediction or custom easing functions without touching individual game objects.
*   **Performance & Order of Execution:** By using a single `LateUpdate` loop, the system can efficiently process all synchronization requests after all other game logic (movement, physics, AI) has completed in `Update`. This prevents visual "jitter" and ensures a consistent update order.
*   **Extensibility:** New types of `ITransformSyncable` objects can be easily added without modifying the core `TransformSyncSystem`. Just implement the interface and register.
*   **Maintainability:** Changes to the synchronization algorithm only need to be made in one place.