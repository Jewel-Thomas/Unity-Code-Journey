// Unity Design Pattern Example: TimeFreezeSystem
// This script demonstrates the TimeFreezeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **TimeFreezeSystem** design pattern. This pattern allows for selectively pausing certain game elements (like enemies, projectiles, or physics) while others (like the player, UI, or background music) remain unaffected. This is more flexible than simply setting `Time.timeScale = 0`, which globally pauses everything.

## TimeFreezeSystem Pattern Explained

The TimeFreezeSystem pattern typically involves:

1.  **An `IFreezeable` Interface:** Any game object or component that wishes to respond to a "time freeze" event implements this interface. It defines methods like `OnFreeze()` and `OnUnfreeze()`.
2.  **A Central `TimeFreezeSystem`:** This is a static class (or a Singleton MonoBehaviour) that maintains a list of all registered `IFreezeable` objects. It provides global methods like `FreezeAll()` and `UnfreezeAll()` which iterate through the registered objects and call their respective interface methods.
3.  **Individual Component Implementations:** Specific MonoBehaviour scripts (e.g., `FreezeableMover`, `FreezeableRigidbody`, `FreezeableParticleSystem`) implement `IFreezeable` and define their unique behavior when `OnFreeze()` or `OnUnfreeze()` is called (e.g., stop movement, disable physics, pause animations).
4.  **Registration/Unregistration:** Components register themselves with the `TimeFreezeSystem` when they are enabled (`OnEnable()`) and unregister when disabled or destroyed (`OnDisable()`).

**Benefits:**

*   **Selective Control:** Freeze specific elements without affecting the entire game.
*   **Loose Coupling:** Components only need to know about the `IFreezeable` interface, not the specifics of the `TimeFreezeSystem` itself.
*   **Reusability:** The `IFreezeable` interface and `TimeFreezeSystem` can be reused across many different types of game logic.
*   **Clear Responsibility:** Each component is responsible for knowing how to freeze/unfreeze *itself*.

---

## 1. `TimeFreezeSystem.cs` (Includes Interface and Examples)

This script contains the `IFreezeable` interface, the static `TimeFreezeSystem` class, and several example `MonoBehaviour` implementations (`FreezeableMover`, `FreezeableRigidbody`, `FreezeableParticleSystem`, and `TimeFreezeActivator`) to show how to use it.

You can drop this entire code into a single C# file named `TimeFreezeSystem.cs` in your Unity project.

```csharp
using UnityEngine;
using System.Collections.Generic; // For HashSet
using System.Linq; // For .ToList() to safely iterate collections while modifying them

/// <summary>
///     Interface for objects that can be frozen and unfrozen by the TimeFreezeSystem.
///     Implementing this interface allows a GameObject's component to respond to global
///     freeze/unfreeze events, customizing its behavior during a 'time freeze'.
/// </summary>
public interface IFreezeable
{
    /// <summary>
    ///     Called by the TimeFreezeSystem when a global freeze event occurs.
    ///     Implement this method to pause or halt the component's specific logic.
    /// </summary>
    void OnFreeze();

    /// <summary>
    ///     Called by the TimeFreezeSystem when a global unfreeze event occurs.
    ///     Implement this method to resume or restart the component's specific logic
    ///     from where it left off, or to reset its state as needed.
    /// </summary>
    void OnUnfreeze();
}

/// <summary>
///     The central TimeFreezeSystem pattern implementation.
///     This static class manages all registered IFreezeable objects and provides
///     global methods to freeze and unfreeze them. It does not use Time.timeScale,
///     allowing for selective pausing of game elements while others (like UI, music)
///     can continue unaffected.
/// </summary>
/// <remarks>
///     This system operates on the principle of a 'soft pause' or 'selective freeze'.
///     Instead of stopping the entire game engine (like Time.timeScale = 0), it
///     notifies individual game components (that implement IFreezeable) to pause
///     their own specific behaviors. This is ideal for scenarios like:
///     - A special ability that freezes enemies but not the player.
///     - Pausing game elements during an in-game cinematic while UI still functions.
///     - Bullet-time effects where some elements slow down/stop while others move normally.
/// </remarks>
public static class TimeFreezeSystem
{
    // A HashSet is used to store registered IFreezeable objects.
    // It efficiently prevents duplicate registrations and allows for quick additions/removals.
    private static readonly HashSet<IFreezeable> _freezeableObjects = new HashSet<IFreezeable>();

    /// <summary>
    ///     Gets a value indicating whether the TimeFreezeSystem is currently in a frozen state.
    /// </summary>
    public static bool IsFrozen { get; private set; }

    /// <summary>
    ///     Registers an IFreezeable object with the system.
    ///     Registered objects will receive OnFreeze() and OnUnfreeze() calls
    ///     when the system's state changes.
    /// </summary>
    /// <param name="obj">The IFreezeable object to register.</param>
    public static void Register(IFreezeable obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[TimeFreezeSystem] Attempted to register a null IFreezeable object.");
            return;
        }

        if (_freezeableObjects.Add(obj)) // Add returns true if the element was added (i.e., not already present)
        {
            // If the system is already frozen, newly registered objects should immediately freeze
            // to ensure they are in the correct state from the moment they become active.
            if (IsFrozen)
            {
                obj.OnFreeze();
            }
            // Debug.Log($"[TimeFreezeSystem] Registered: {obj.GetType().Name}"); // Uncomment for detailed debug logs
        }
        else
        {
            // Debug.LogWarning($"[TimeFreezeSystem] Attempted to register an object that is already registered: {obj.GetType().Name}");
        }
    }

    /// <summary>
    ///     Unregisters an IFreezeable object from the system.
    ///     The object will no longer receive freeze/unfreeze notifications.
    /// </summary>
    /// <param name="obj">The IFreezeable object to unregister.</param>
    public static void Unregister(IFreezeable obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[TimeFreezeSystem] Attempted to unregister a null IFreezeable object.");
            return;
        }

        if (_freezeableObjects.Remove(obj))
        {
            // Debug.Log($"[TimeFreezeSystem] Unregistered: {obj.GetType().Name}"); // Uncomment for detailed debug logs
        }
        else
        {
            // Debug.LogWarning($"[TimeFreezeSystem] Attempted to unregister an object that was not registered: {obj.GetType().Name}");
        }
    }

    /// <summary>
    ///     Initiates a global freeze. All registered IFreezeable objects will
    ///     receive an OnFreeze() call.
    /// </summary>
    public static void FreezeAll()
    {
        if (IsFrozen)
        {
            Debug.LogWarning("[TimeFreezeSystem] Already frozen, ignoring FreezeAll request.");
            return;
        }

        IsFrozen = true;
        // Iterate through a copy of the collection using ToList() to safely remove destroyed objects
        // or objects that might unregister themselves during the iteration.
        foreach (var obj in _freezeableObjects.ToList())
        {
            // Check if the underlying Unity object has been destroyed.
            // This is important for robustness if GameObjects are destroyed without properly unregistering.
            if (obj is MonoBehaviour monoObj && monoObj == null)
            {
                _freezeableObjects.Remove(obj); // Clean up the destroyed reference
                continue;
            }
            obj.OnFreeze();
        }
        Debug.Log("[TimeFreezeSystem] All registered objects frozen.");
    }

    /// <summary>
    ///     Initiates a global unfreeze. All registered IFreezeable objects will
    ///     receive an OnUnfreeze() call.
    /// </summary>
    public static void UnfreezeAll()
    {
        if (!IsFrozen)
        {
            Debug.LogWarning("[TimeFreezeSystem] Not currently frozen, ignoring UnfreezeAll request.");
            return;
        }

        IsFrozen = false;
        // Iterate through a copy of the collection using ToList() for safety.
        foreach (var obj in _freezeableObjects.ToList())
        {
            if (obj is MonoBehaviour monoObj && monoObj == null)
            {
                _freezeableObjects.Remove(obj); // Clean up the destroyed reference
                continue;
            }
            obj.OnUnfreeze();
        }
        Debug.Log("[TimeFreezeSystem] All registered objects unfrozen.");
    }

    /// <summary>
    ///     Clears all registered IFreezeable objects from the system.
    ///     Useful for scene transitions, game resets, or when shutting down the system.
    ///     Also sets the system to an unfrozen state.
    /// </summary>
    public static void ClearAll()
    {
        _freezeableObjects.Clear();
        IsFrozen = false;
        Debug.Log("[TimeFreezeSystem] All registered objects cleared and system unfrozen.");
    }
}

// --- EXAMPLE USAGE COMPONENTS ---
// These MonoBehaviour scripts demonstrate how to implement the IFreezeable interface
// for various Unity components and respond to freeze/unfreeze events.
// To use them, create Empty GameObjects in your scene, add the relevant components
// (e.g., Rigidbody, ParticleSystem), and then add these Freezeable scripts.

/// <summary>
///     Example 1: Freezing a simple moving character/object.
///     This component stops its movement logic when frozen and resumes when unfrozen.
/// </summary>
public class FreezeableMover : MonoBehaviour, IFreezeable
{
    [Tooltip("The speed at which the object moves when unfrozen.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("The direction of movement (e.g., Vector3.right for X-axis).")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    private float _currentMoveSpeed; // Stores the active move speed (0 when frozen, original when unfrozen)
    private bool _isMovementEnabled = true; // Tracks if movement logic is allowed to run

    void Awake()
    {
        _currentMoveSpeed = moveSpeed; // Initialize with original speed
    }

    void OnEnable()
    {
        // Important: Register with the TimeFreezeSystem when this component becomes active.
        TimeFreezeSystem.Register(this);
        // Ensure initial state matches the system's current state immediately upon enabling.
        if (TimeFreezeSystem.IsFrozen)
        {
            OnFreeze();
        }
        else
        {
            OnUnfreeze();
        }
    }

    void OnDisable()
    {
        // Important: Unregister from the TimeFreezeSystem when this component becomes inactive or destroyed.
        TimeFreezeSystem.Unregister(this);
    }

    void Update()
    {
        // Only move if movement is enabled (i.e., not frozen)
        if (_isMovementEnabled)
        {
            transform.Translate(moveDirection.normalized * _currentMoveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    ///     Implements IFreezeable.OnFreeze().
    ///     Sets movement speed to zero and disables update logic.
    /// </summary>
    public void OnFreeze()
    {
        _currentMoveSpeed = 0f;
        _isMovementEnabled = false;
        Debug.Log($"{gameObject.name} FreezeableMover: Frozen!");
    }

    /// <summary>
    ///     Implements IFreezeable.OnUnfreeze().
    ///     Restores original movement speed and enables update logic.
    /// </summary>
    public void OnUnfreeze()
    {
        _currentMoveSpeed = moveSpeed;
        _isMovementEnabled = true;
        Debug.Log($"{gameObject.name} FreezeableMover: Unfrozen!");
    }
}

/// <summary>
///     Example 2: Freezing a Rigidbody's physics simulation.
///     This component sets the Rigidbody to kinematic when frozen to stop physics,
///     and restores it when unfrozen.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody component is present on the GameObject
public class FreezeableRigidbody : MonoBehaviour, IFreezeable
{
    private Rigidbody _rigidbody;
    private bool _wasKinematic; // Stores original kinematic state of the Rigidbody
    private Vector3 _storedVelocity;
    private Vector3 _storedAngularVelocity;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null) // Should not happen with RequireComponent, but good practice.
        {
            Debug.LogError($"[FreezeableRigidbody] Rigidbody not found on {gameObject.name}. This component requires a Rigidbody.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        TimeFreezeSystem.Register(this);
        // Ensure initial state matches the system's current state.
        if (TimeFreezeSystem.IsFrozen)
        {
            OnFreeze();
        }
        else
        {
            OnUnfreeze();
        }
    }

    void OnDisable()
    {
        TimeFreezeSystem.Unregister(this);
        // Important: If this component is disabled while the system is frozen,
        // the Rigidbody would be left in a kinematic state. This restores it
        // to its pre-freeze state to prevent unexpected behavior.
        if (_rigidbody != null && _rigidbody.isKinematic && !_wasKinematic)
        {
             _rigidbody.isKinematic = _wasKinematic;
             _rigidbody.velocity = _storedVelocity;
             _rigidbody.angularVelocity = _storedAngularVelocity;
        }
    }

    /// <summary>
    ///     Implements IFreezeable.OnFreeze().
    ///     Makes the Rigidbody kinematic to stop all physics interactions and stores its current velocities.
    /// </summary>
    public void OnFreeze()
    {
        if (_rigidbody == null) return;

        // Store current state and velocities before freezing.
        _wasKinematic = _rigidbody.isKinematic;
        _storedVelocity = _rigidbody.velocity;
        _storedAngularVelocity = _rigidbody.angularVelocity;

        _rigidbody.isKinematic = true; // Stop physics simulation
        _rigidbody.velocity = Vector3.zero; // Explicitly halt any residual movement
        _rigidbody.angularVelocity = Vector3.zero;
        Debug.Log($"{gameObject.name} FreezeableRigidbody: Frozen!");
    }

    /// <summary>
    ///     Implements IFreezeable.OnUnfreeze().
    ///     Restores the Rigidbody's original kinematic state and previous velocities.
    /// </summary>
    public void OnUnfreeze()
    {
        if (_rigidbody == null) return;

        _rigidbody.isKinematic = _wasKinematic; // Restore original state
        _rigidbody.velocity = _storedVelocity;
        _rigidbody.angularVelocity = _storedAngularVelocity;
        Debug.Log($"{gameObject.name} FreezeableRigidbody: Unfrozen!");
    }
}

/// <summary>
///     Example 3: Freezing a ParticleSystem.
///     This component pauses a particle system when frozen and resumes it when unfrozen.
/// </summary>
[RequireComponent(typeof(ParticleSystem))] // Ensures a ParticleSystem component is present on the GameObject
public class FreezeableParticleSystem : MonoBehaviour, IFreezeable
{
    private ParticleSystem _particleSystem;
    private bool _wasPlaying; // Stores whether particle system was playing before freeze

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        if (_particleSystem == null) // Should not happen with RequireComponent.
        {
            Debug.LogError($"[FreezeableParticleSystem] ParticleSystem not found on {gameObject.name}. This component requires a ParticleSystem.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        TimeFreezeSystem.Register(this);
        // Ensure initial state matches the system's current state.
        if (TimeFreezeSystem.IsFrozen)
        {
            OnFreeze();
        }
        else
        {
            OnUnfreeze();
        }
    }

    void OnDisable()
    {
        TimeFreezeSystem.Unregister(this);
        // If the component is disabled while the particle system was playing and is now paused,
        // ensure it's played back so it's not left in a paused state unexpectedly.
        if (_particleSystem != null && _wasPlaying && _particleSystem.isPaused)
        {
            _particleSystem.Play(true); // Play with 'true' to include children
        }
    }

    /// <summary>
    ///     Implements IFreezeable.OnFreeze().
    ///     Pauses the particle system.
    /// </summary>
    public void OnFreeze()
    {
        if (_particleSystem == null) return;

        _wasPlaying = _particleSystem.isPlaying; // Store whether it was playing
        if (_wasPlaying)
        {
            _particleSystem.Pause(true); // Pause with 'true' to include child particle systems
        }
        Debug.Log($"{gameObject.name} FreezeableParticleSystem: Frozen!");
    }

    /// <summary>
    ///     Implements IFreezeable.OnUnfreeze().
    ///     Resumes the particle system if it was playing before the freeze.
    /// </summary>
    public void OnUnfreeze()
    {
        if (_particleSystem == null) return;

        if (_wasPlaying && _particleSystem.isPaused) // Only resume if it was playing and is currently paused
        {
            _particleSystem.Play(true); // Play with 'true' to include child particle systems
        }
        Debug.Log($"{gameObject.name} FreezeableParticleSystem: Unfrozen!");
    }
}


/// <summary>
///     Example 4: A simple controller to activate/deactivate the TimeFreezeSystem
///     for demonstration purposes. This component can be attached to any GameObject
///     in the scene (e.g., a "GameManager" object).
/// </summary>
public class TimeFreezeActivator : MonoBehaviour
{
    [Tooltip("Key to press to freeze all registered objects.")]
    [SerializeField] private KeyCode freezeKey = KeyCode.F;
    [Tooltip("Key to press to unfreeze all registered objects.")]
    [SerializeField] private KeyCode unfreezeKey = KeyCode.U;
    [Tooltip("Key to press to toggle freeze state.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;


    void Update()
    {
        if (Input.GetKeyDown(freezeKey))
        {
            TimeFreezeSystem.FreezeAll();
        }
        else if (Input.GetKeyDown(unfreezeKey))
        {
            TimeFreezeSystem.UnfreezeAll();
        }
        else if (Input.GetKeyDown(toggleKey))
        {
            if (TimeFreezeSystem.IsFrozen)
            {
                TimeFreezeSystem.UnfreezeAll();
            }
            else
            {
                TimeFreezeSystem.FreezeAll();
            }
        }
    }

    void OnDestroy()
    {
        // It's good practice to clear the system when the scene unloads or
        // the managing object is destroyed, to prevent references to destroyed objects.
        TimeFreezeSystem.ClearAll();
    }
}
```

---

## How to Set Up and Test in Unity:

1.  **Create a C# Script:** In your Unity project, create a new C# script named `TimeFreezeSystem.cs` and copy all the code above into it.
2.  **Create a GameManager Object:** In your scene, create an empty GameObject (e.g., `Right-click > Create Empty`) and name it `GameManager`.
3.  **Add the Activator:** Drag and drop the `TimeFreezeActivator` component onto your `GameManager` GameObject. You'll see `Freeze Key`, `Unfreeze Key`, and `Toggle Key` in the Inspector, which you can customize.
4.  **Create Freezeable Objects:**
    *   **Moving Cube:**
        *   Create a 3D Cube (`Right-click > 3D Object > Cube`).
        *   Add the `FreezeableMover` component to it. You can adjust its `Move Speed` and `Move Direction` in the Inspector.
    *   **Falling Sphere (Physics):**
        *   Create a 3D Sphere.
        *   Add a `Rigidbody` component to it (`Add Component > Physics > Rigidbody`).
        *   Add the `FreezeableRigidbody` component to it.
    *   **Particle Effect:**
        *   Create a Particle System (`Right-click > Effects > Particle System`).
        *   Add the `FreezeableParticleSystem` component to it.
        *   (Optional) Make sure the Particle System is set to "Looping" or has a long duration so you can observe it.
5.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   You should see the cube moving, the sphere falling (if you positioned it above a plane/ground), and particles emitting.
    *   Press **'F'** (default Freeze Key): All objects with `FreezeableMover`, `FreezeableRigidbody`, and `FreezeableParticleSystem` should stop their respective behaviors. The cube halts, the sphere freezes mid-air, and the particles stop emitting.
    *   Press **'U'** (default Unfreeze Key): All objects should resume their behaviors from where they left off.
    *   Press **'T'** (default Toggle Key): Toggles the freeze state.
    *   Observe the `Debug.Log` messages in the Console window for confirmation.

This setup provides a clear, practical, and educational demonstration of the TimeFreezeSystem pattern in Unity.