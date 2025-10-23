// Unity Design Pattern Example: TeleportationSystem
// This script demonstrates the TeleportationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'TeleportationSystem' pattern in Unity, incorporating several common design patterns:

1.  **Singleton:** The `TeleportationSystem` manager is a Singleton, providing a global point of access.
2.  **Strategy:** Different teleportation effects (e.g., instant, fade) are implemented as strategies (`ITeleportationEffectStrategy`), allowing the `TeleportationSystem` to dynamically switch between them.
3.  **Observer/Event System:** The `TeleportationSystem` exposes C# events (`OnTeleportInitiated`, `OnTeleportCompleted`) that other game systems can subscribe to, decoupling teleportation logic from reaction logic.

This design makes the teleportation process centralized, extensible, and flexible.

---

### **1. Core System Components (C# Scripts)**

Create a folder named `TeleportationSystemPattern` in your Unity project's `Assets` folder, and subfolders like `Scripts` and `ScriptableObjects`. Place the following scripts in the `Scripts` folder.

#### `ITeleportationEffectStrategy.cs`

```csharp
using System;
using UnityEngine;
using System.Collections; // Needed for Coroutines in implementing strategies

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Strategy Interface
    /// Defines the contract for different teleportation effects or transitions.
    /// This allows the TeleportationSystem to use various methods without knowing
    /// the specific implementation details, promoting extensibility.
    /// </summary>
    public interface ITeleportationEffectStrategy
    {
        /// <summary>
        /// Executes the teleportation logic with a specific effect.
        /// </summary>
        /// <param name="subject">The GameObject to be teleported.</param>
        /// <param name="targetPosition">The world position to teleport to.</param>
        /// <param name="targetRotation">The world rotation to set after teleportation.</param>
        /// <param name="onComplete">An optional callback to invoke once the teleportation and its effect are finished.</param>
        /// <returns>An IEnumerator for potential coroutine-based effects (e.g., fading, animations).</returns>
        IEnumerator ExecuteTeleport(GameObject subject, Vector3 targetPosition, Quaternion targetRotation, Action onComplete = null);
    }
}
```

#### `InstantTeleportEffect.cs`

```csharp
using System;
using UnityEngine;
using System.Collections;

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Concrete Strategy (Instant Teleport)
    /// Implements an immediate teleportation effect without any transition.
    /// This is a ScriptableObject, allowing you to create and configure
    /// different instant teleport presets in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "InstantTeleportEffect", menuName = "Teleportation System/Effects/Instant Teleport", order = 0)]
    public class InstantTeleportEffect : ScriptableObject, ITeleportationEffectStrategy
    {
        [Header("Instant Teleport Effect Settings")]
        [Tooltip("Optional: Debug message to show when this effect is used.")]
        public string debugMessage = "Performing instant teleport.";

        /// <summary>
        /// Immediately moves the subject to the target position and rotation.
        /// </summary>
        public IEnumerator ExecuteTeleport(GameObject subject, Vector3 targetPosition, Quaternion targetRotation, Action onComplete = null)
        {
            if (subject == null)
            {
                Debug.LogError("InstantTeleportEffect: Subject GameObject is null. Cannot teleport.");
                onComplete?.Invoke();
                yield break;
            }

            Debug.Log($"<color=cyan>{debugMessage}</color> Teleporting '{subject.name}' instantly to {targetPosition}");

            subject.transform.position = targetPosition;
            subject.transform.rotation = targetRotation;

            onComplete?.Invoke();
            yield break; // No waiting required for instant effect
        }
    }
}
```

#### `FadeTeleportEffect.cs`

```csharp
using System;
using UnityEngine;
using System.Collections;
// Potentially needed for a real UI fade, but for this example, we'll simulate with delays.
// using UnityEngine.UI;

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Concrete Strategy (Fade Teleport)
    /// Implements a teleportation effect that simulates a fade-out, teleport, and fade-in sequence.
    /// For this example, we use simple delays to represent the fade, as a full UI fade setup
    /// is beyond the scope of just demonstrating the pattern.
    /// This is a ScriptableObject, allowing you to create and configure
    /// different fade teleport presets in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "FadeTeleportEffect", menuName = "Teleportation System/Effects/Fade Teleport", order = 1)]
    public class FadeTeleportEffect : ScriptableObject, ITeleportationEffectStrategy
    {
        [Header("Fade Teleport Effect Settings")]
        [Tooltip("Duration of the fade-out and fade-in segments.")]
        [Range(0.1f, 2.0f)]
        public float fadeDuration = 0.5f;

        [Tooltip("Delay in seconds during which the player is 'blacked out' or transitioning.")]
        [Range(0.1f, 1.0f)]
        public float blackScreenDelay = 0.2f;

        /// <summary>
        /// Executes a fade-out, teleports the subject, then fades back in.
        /// Uses Unity Coroutines to manage the timed steps.
        /// </summary>
        public IEnumerator ExecuteTeleport(GameObject subject, Vector3 targetPosition, Quaternion targetRotation, Action onComplete = null)
        {
            if (subject == null)
            {
                Debug.LogError("FadeTeleportEffect: Subject GameObject is null. Cannot teleport.");
                onComplete?.Invoke();
                yield break;
            }

            Debug.Log($"<color=yellow>Performing fade teleport.</color> Teleporting '{subject.name}' to {targetPosition}");

            // --- Simulate Fade Out ---
            Debug.Log($"<color=yellow>Fading out...</color> (Duration: {fadeDuration}s)");
            // In a real game, you would activate a full-screen black image/panel here
            // and animate its alpha from 0 to 1 over fadeDuration using a CanvasGroup.
            yield return new WaitForSeconds(fadeDuration);

            // --- Black Screen Delay / Transition ---
            Debug.Log($"<color=yellow>Teleporting during black screen...</color> (Delay: {blackScreenDelay}s)");
            subject.transform.position = targetPosition;
            subject.transform.rotation = targetRotation;
            yield return new WaitForSeconds(blackScreenDelay);

            // --- Simulate Fade In ---
            Debug.Log($"<color=yellow>Fading in...</color> (Duration: {fadeDuration}s)");
            // In a real game, you would animate the black image/panel's alpha from 1 to 0 over fadeDuration
            // and then deactivate it.
            yield return new WaitForSeconds(fadeDuration);

            Debug.Log($"<color=yellow>Fade teleport complete for '{subject.name}'!</color>");
            onComplete?.Invoke();
        }
    }
}
```

#### `TeleportationSystem.cs`

```csharp
using System;
using UnityEngine;
using System.Collections; // Needed for Coroutines

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Singleton Manager (Context)
    /// This central manager handles all teleportation requests in the game.
    /// It uses a Singleton pattern for easy global access and a Strategy pattern
    /// to allow different teleportation effects to be plugged in dynamically.
    /// It also offers an event system for global pre/post-teleport notifications.
    /// </summary>
    public class TeleportationSystem : MonoBehaviour
    {
        // --- Singleton Instance ---
        private static TeleportationSystem _instance;
        public static TeleportationSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Attempt to find an existing instance in the scene
                    _instance = FindObjectOfType<TeleportationSystem>();

                    if (_instance == null)
                    {
                        // If none found, create a new GameObject and add the component
                        GameObject singletonObject = new GameObject(typeof(TeleportationSystem).Name);
                        _instance = singletonObject.AddComponent<TeleportationSystem>();
                        Debug.LogWarning($"TeleportationSystem created at runtime. Consider adding it to your scene manually for better control.");
                    }
                }
                return _instance;
            }
        }

        [Header("Default Teleportation Strategy")]
        [Tooltip("Assign a default teleportation effect strategy (e.g., InstantTeleportEffect, FadeTeleportEffect ScriptableObject).")]
        [SerializeField]
        private ScriptableObject _defaultTeleportationEffect; // Use ScriptableObject for inspector assignment

        private ITeleportationEffectStrategy _currentEffectStrategy;

        // --- Event System (Observer Pattern Element) ---
        // Events that other systems can subscribe to for pre/post-teleport actions.
        public static event Action<GameObject, Vector3, Quaternion> OnTeleportInitiated;
        public static event Action<GameObject, Vector3, Quaternion> OnTeleportCompleted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Enforce Singleton: destroy duplicate instances
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // Keep the system alive across scene loads

            // Initialize with the default strategy from the Inspector
            if (_defaultTeleportationEffect is ITeleportationEffectStrategy defaultStrategy)
            {
                _currentEffectStrategy = defaultStrategy;
            }
            else
            {
                Debug.LogError("TeleportationSystem: Default teleportation effect is not assigned or is not a valid ITeleportationEffectStrategy. Assign an InstantTeleportEffect or FadeTeleportEffect ScriptableObject.");
                // Fallback to a basic instant strategy if default is invalid or not set
                _currentEffectStrategy = ScriptableObject.CreateInstance<InstantTeleportEffect>(); // Create a runtime instance
            }
            Debug.Log($"TeleportationSystem initialized with default effect: {_currentEffectStrategy.GetType().Name}");
        }

        /// <summary>
        /// TeleportationSystem Pattern: Set Strategy
        /// Allows dynamically changing the teleportation effect at runtime.
        /// This strategy will be used for all subsequent teleports until changed again.
        /// </summary>
        /// <param name="newStrategy">The new strategy to use for subsequent teleports.</param>
        public void SetEffectStrategy(ITeleportationEffectStrategy newStrategy)
        {
            if (newStrategy == null)
            {
                Debug.LogWarning("Attempted to set a null teleportation strategy. Reverting to current strategy.");
                return;
            }
            _currentEffectStrategy = newStrategy;
            Debug.Log($"TeleportationSystem effect strategy changed to: {_currentEffectStrategy.GetType().Name}");
        }

        /// <summary>
        /// TeleportationSystem Pattern: Public API for Teleportation
        /// Initiates a teleportation request using the currently active effect strategy.
        /// </summary>
        /// <param name="subject">The GameObject to be teleported (e.g., player character).</param>
        /// <param name="targetPosition">The world position where the subject will land.</param>
        /// <param name="targetRotation">The world rotation the subject will have after landing.</param>
        /// <param name="onComplete">An optional action to call once the teleportation (and its effect) is fully finished.</param>
        public void Teleport(GameObject subject, Vector3 targetPosition, Quaternion targetRotation, Action onComplete = null)
        {
            if (subject == null)
            {
                Debug.LogError("TeleportationSystem: Teleport subject is null. Aborting teleport.");
                onComplete?.Invoke();
                return;
            }

            if (_currentEffectStrategy == null)
            {
                Debug.LogError("TeleportationSystem: No teleportation effect strategy is set! Cannot teleport.");
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"TeleportationSystem: Teleport request for '{subject.name}' to {targetPosition} using strategy: {_currentEffectStrategy.GetType().Name}");

            // Notify listeners that a teleportation is about to begin
            OnTeleportInitiated?.Invoke(subject, targetPosition, targetRotation);

            // Execute the teleport using the chosen strategy.
            // The StartCoroutine must be called on a MonoBehaviour instance,
            // which is why TeleportationSystem itself executes the strategy's IEnumerator.
            StartCoroutine(_currentEffectStrategy.ExecuteTeleport(subject, targetPosition, targetRotation, () =>
            {
                // This callback ensures OnTeleportCompleted is invoked AFTER the strategy's effect finishes
                OnTeleportCompleted?.Invoke(subject, targetPosition, targetRotation);
                onComplete?.Invoke(); // Also call the original onComplete callback from the requester
            }));
        }

        /// <summary>
        /// Overload for convenience: Teleport to a specific Transform's position and rotation.
        /// </summary>
        public void Teleport(GameObject subject, Transform targetTransform, Action onComplete = null)
        {
            if (targetTransform == null)
            {
                Debug.LogError("TeleportationSystem: Target Transform is null. Aborting teleport.");
                onComplete?.Invoke();
                return;
            }
            Teleport(subject, targetTransform.position, targetTransform.rotation, onComplete);
        }

        /// <summary>
        /// Overload for convenience: Teleport to a specific Vector3 position, keeping current rotation.
        /// </summary>
        public void Teleport(GameObject subject, Vector3 targetPosition, Action onComplete = null)
        {
            if (subject == null)
            {
                Debug.LogError("TeleportationSystem: Teleport subject is null. Aborting teleport.");
                onComplete?.Invoke();
                return;
            }
            Teleport(subject, targetPosition, subject.transform.rotation, onComplete);
        }
    }
}
```

#### `TeleportTarget.cs`

```csharp
using UnityEngine;

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Target Marker
    /// A simple MonoBehaviour to mark a GameObject as a potential teleport destination.
    /// It provides a specific Transform as the spawn point. This decouples the visual
    /// target object from the exact landing spot.
    /// </summary>
    public class TeleportTarget : MonoBehaviour
    {
        [Header("Teleport Target Settings")]
        [Tooltip("The actual Transform where the player or object will be placed after teleporting.")]
        public Transform spawnPoint;

        [Tooltip("Optional: A unique identifier for this teleport target. Can be used to find specific targets.")]
        public string targetId = "";

        void Awake()
        {
            // If no specific spawn point is set, use the GameObject's own transform.
            if (spawnPoint == null)
            {
                spawnPoint = this.transform;
                Debug.LogWarning($"TeleportTarget '{gameObject.name}' has no specific spawn point assigned. Using its own transform as spawn point.");
            }
        }

        /// <summary>
        /// Returns the Transform where the teleported object should be placed.
        /// </summary>
        /// <returns>The spawn point Transform.</returns>
        public Transform GetSpawnPoint()
        {
            return spawnPoint;
        }

        // Optional: Draw a gizmo to easily visualize the spawn point in the editor
        void OnDrawGizmos()
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 1.0f); // Show forward direction
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(spawnPoint.position + Vector3.up * 0.1f, 0.15f); // Small sphere slightly above for clarity
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, Vector3.one);
                Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
            }
        }
    }
}
```

#### `TeleportRequester.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .ToList()

namespace TeleportationSystemPattern
{
    /// <summary>
    /// TeleportationSystem Pattern: Example Usage (Client)
    /// This MonoBehaviour demonstrates how other parts of your game would interact
    /// with the TeleportationSystem. It acts as a client that requests teleports
    /// and can dynamically change the system's teleportation strategy.
    /// </summary>
    public class TeleportRequester : MonoBehaviour
    {
        [Header("Teleport Requester Settings")]
        [Tooltip("The GameObject that will be teleported (e.g., your player character).")]
        public GameObject subjectToTeleport;

        [Tooltip("List of available teleport targets in the scene. Assign these from your scene.")]
        public List<TeleportTarget> availableTargets;

        [Tooltip("Reference to an InstantTeleportEffect ScriptableObject for use by this requester.")]
        public InstantTeleportEffect instantEffectSO; 

        [Tooltip("Reference to a FadeTeleportEffect ScriptableObject for use by this requester.")]
        public FadeTeleportEffect fadeEffectSO; 

        private int _currentTeleportTargetIndex = 0;

        void Awake()
        {
            if (subjectToTeleport == null)
            {
                subjectToTeleport = this.gameObject; // Default to self if not assigned
                Debug.LogWarning($"TeleportRequester on '{gameObject.name}' has no subjectToTeleport assigned. Defaulting to self.");
            }

            // Ensure targets are populated if empty (e.g. at runtime if not set in inspector)
            if (availableTargets == null || availableTargets.Count == 0)
            {
                availableTargets = FindObjectsOfType<TeleportTarget>().ToList();
                if (availableTargets.Count == 0)
                {
                    Debug.LogWarning("No TeleportTargets found in scene for TeleportRequester. Please add some TeleportTarget components.");
                }
            }

            // Example of subscribing to system events.
            // These handlers will be called for ALL teleports initiated through TeleportationSystem.Instance.
            TeleportationSystem.OnTeleportInitiated += HandleTeleportInitiated;
            TeleportationSystem.OnTeleportCompleted += HandleTeleportCompleted;
        }

        void OnDestroy()
        {
            // IMPORTANT: Unsubscribe from static events to prevent memory leaks
            TeleportationSystem.OnTeleportInitiated -= HandleTeleportInitiated;
            TeleportationSystem.OnTeleportCompleted -= HandleTeleportCompleted;
        }

        void Update()
        {
            // Example: Press 'T' to teleport to the next target using the system's current strategy
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (availableTargets.Count > 0)
                {
                    TeleportTarget target = availableTargets[_currentTeleportTargetIndex];
                    if (target != null)
                    {
                        Debug.Log($"<color=green>TeleportRequester:</color> Requesting teleport for '{subjectToTeleport.name}' to '{target.name}'...");

                        // --- How to use the TeleportationSystem ---
                        // 1. You can just call Teleport, and it will use the system's currently set strategy.
                        //    TeleportationSystem.Instance.Teleport(subjectToTeleport, target.GetSpawnPoint(), OnMyTeleportComplete);

                        // 2. You can *dynamically change* the system's strategy before calling Teleport.
                        //    This will affect all subsequent teleports until the strategy is changed again.
                        if (instantEffectSO != null)
                        {
                            // Set the global system strategy to instant before teleporting
                            TeleportationSystem.Instance.SetEffectStrategy(instantEffectSO);
                            TeleportationSystem.Instance.Teleport(subjectToTeleport, target.GetSpawnPoint(), OnMyTeleportComplete);
                        }
                        else
                        {
                            Debug.LogWarning("Instant Teleport Effect ScriptableObject not assigned to TeleportRequester. Cannot set as system strategy.");
                            // Fallback to system's default if requester's preferred strategy is missing
                            TeleportationSystem.Instance.Teleport(subjectToTeleport, target.GetSpawnPoint(), OnMyTeleportComplete);
                        }
                        
                        _currentTeleportTargetIndex = (_currentTeleportTargetIndex + 1) % availableTargets.Count;
                    }
                    else
                    {
                        Debug.LogError($"TeleportRequester: Target at index {_currentTeleportTargetIndex} is null!");
                    }
                }
                else
                {
                    Debug.LogWarning("TeleportRequester: No teleport targets assigned or found.");
                }
            }

            // Example: Press 'F' to teleport using the Fade effect strategy
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (availableTargets.Count > 0 && fadeEffectSO != null)
                {
                    TeleportTarget target = availableTargets[_currentTeleportTargetIndex];
                    if (target != null)
                    {
                        Debug.Log($"<color=green>TeleportRequester:</color> Requesting fade teleport for '{subjectToTeleport.name}' to '{target.name}'...");
                        
                        // Set the global system strategy to fade before teleporting
                        TeleportationSystem.Instance.SetEffectStrategy(fadeEffectSO);
                        TeleportationSystem.Instance.Teleport(subjectToTeleport, target.GetSpawnPoint(), OnMyTeleportComplete);
                        
                        _currentTeleportTargetIndex = (_currentTeleportTargetIndex + 1) % availableTargets.Count;
                    }
                }
                else if (fadeEffectSO == null)
                {
                    Debug.LogWarning("Fade Teleport Effect ScriptableObject not assigned to TeleportRequester. Cannot use fade effect.");
                }
            }
        }

        // --- Custom callback for a specific teleport request ---
        void OnMyTeleportComplete()
        {
            Debug.Log($"<color=magenta>TeleportRequester:</color> My specific teleport callback for '{subjectToTeleport.name}' invoked!");
            // Perform any actions specific to THIS teleport request (e.g., enable/disable UI, play a sound).
        }

        // --- Global event handlers for ALL teleports ---
        void HandleTeleportInitiated(GameObject subject, Vector3 pos, Quaternion rot)
        {
            Debug.Log($"<color=orange>Global Teleport Event:</color> '{subject.name}' is about to teleport to {pos}.");
            // Example: Disable player input, show a loading screen, play a global transition sound for any teleport.
            // You can check if 'subject == this.subjectToTeleport' to perform player-specific actions.
        }

        void HandleTeleportCompleted(GameObject subject, Vector3 pos, Quaternion rot)
        {
            Debug.Log($"<color=orange>Global Teleport Event:</color> '{subject.name}' has completed teleporting to {pos}.");
            // Example: Re-enable player input, hide loading screen, update camera, log analytics.
        }
    }
}
```

---

### **2. Setup in Unity Editor**

Follow these steps to get the example working in your Unity project:

1.  **Project Structure:**
    *   In your `Assets` folder, create `TeleportationSystemPattern`.
    *   Inside `TeleportationSystemPattern`, create `Scripts` and `ScriptableObjects`.
    *   Place all the `.cs` files (from section 1) into the `Scripts` folder.

2.  **Create ScriptableObject Assets:**
    *   Go to `Assets/TeleportationSystemPattern/ScriptableObjects`.
    *   Right-click in the Project window -> `Create` -> `Teleportation System` -> `Effects` -> `Instant Teleport`. Name this asset `DefaultInstantEffect`.
    *   Right-click -> `Create` -> `Teleportation System` -> `Effects` -> `Fade Teleport`. Name this asset `DefaultFadeEffect`.

3.  **Scene Setup:**

    *   **a) Create TeleportationSystem Manager:**
        *   Create an empty GameObject in your scene (Right-click in Hierarchy -> `Create Empty`).
        *   Rename it `TeleportationSystemManager`.
        *   Add the `TeleportationSystem` component to it (`Add Component` -> search for `TeleportationSystem`).
        *   In the Inspector, drag the `DefaultInstantEffect` ScriptableObject (from `Assets/TeleportationSystemPattern/ScriptableObjects`) into the `Default Teleportation Effect` slot.

    *   **b) Create Player (Subject to Teleport):**
        *   Create a 3D Object -> `Cube`. Rename it `PlayerCube`.
        *   Add a `Rigidbody` component to `PlayerCube`. (Optional: uncheck "Use Gravity" or freeze rotation in Constraints for easier observation).
        *   Position `PlayerCube` at `(0, 1, 0)`.

    *   **c) Create Teleport Targets:**
        *   Create three more empty GameObjects or 3D Objects (e.g., Spheres) in your scene.
        *   Rename them `TeleportTarget_A`, `TeleportTarget_B`, `TeleportTarget_C`.
        *   Position them at distinct locations (e.g., `(5, 1, 0)`, `(0, 1, 5)`, `(-5, 1, 0)`).
        *   Add the `TeleportTarget` component to each of them.
        *   (Optional, but recommended for clarity): For each `TeleportTarget` object, create an empty child GameObject called `SpawnPoint`. Position this `SpawnPoint` slightly above the parent object (e.g., `Y=0.5`). Then, drag this `SpawnPoint` child into the `Spawn Point` slot of the `TeleportTarget` component. This visualizes the *exact* landing spot.

    *   **d) Create Teleport Requester (Client):**
        *   Select your `PlayerCube` GameObject.
        *   Add the `TeleportRequester` component to it.
        *   In the Inspector of `PlayerCube`'s `TeleportRequester` component:
            *   Drag `PlayerCube` itself into the `Subject To Teleport` slot.
            *   Drag `TeleportTarget_A`, `TeleportTarget_B`, `TeleportTarget_C` from your Hierarchy into the `Available Targets` list.
            *   Drag the `DefaultInstantEffect` ScriptableObject into the `Instant Effect SO` slot.
            *   Drag the `DefaultFadeEffect` ScriptableObject into the `Fade Effect SO` slot.

4.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe the Console window for debug messages.
    *   **Press the `T` key:** Your `PlayerCube` will instantly teleport to the next `TeleportTarget` in the list, cycling through them. You'll see messages indicating the "Instant Teleport Effect" strategy is being used.
    *   **Press the `F` key:** Your `PlayerCube` will perform a simulated fade-out, teleport, and fade-in, using the "Fade Teleport Effect" strategy.
    *   Notice how global event handlers (`HandleTeleportInitiated`, `HandleTeleportCompleted`) are triggered for every teleport, along with the specific `OnMyTeleportComplete` callback for the requester.

---

This example provides a robust and flexible teleportation system for Unity, demonstrating how to combine several design patterns for a practical and extensible solution.