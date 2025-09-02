// Unity Design Pattern Example: CustomPhysicsPatterns
// This script demonstrates the CustomPhysicsPatterns pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Custom Physics Patterns** design pattern in Unity, primarily using the **Strategy Pattern** to allow GameObjects to dynamically change their custom physics behaviors at runtime. This is incredibly useful for games with different environmental zones, character abilities, or special object interactions where Unity's built-in physics might need augmentation or complete replacement.

### Core Idea

The **Custom Physics Patterns** design pattern, as implemented here with the Strategy Pattern, revolves around:

1.  **Defining an Interface (`IPhysicsBehavior`):** This interface declares a common method for applying custom physics.
2.  **Creating Concrete Strategies:** Multiple classes implement this interface, each providing a different custom physics behavior (e.g., normal gravity, anti-gravity, magnetic pull, zero gravity).
3.  **A Context (`CustomPhysicsController`):** This MonoBehaviour holds a reference to the current `IPhysicsBehavior` strategy. It delegates the physics application to this strategy in `FixedUpdate`. It also provides a method to change the active strategy.
4.  **Dynamic Swapping:** At runtime, the `CustomPhysicsController` can be instructed to use a different strategy, effectively changing the object's physics behavior on the fly.

This setup makes it easy to add new physics behaviors without modifying existing code, promoting modularity and extensibility.

---

### Project Setup and Usage

To use this example in your Unity project:

1.  **Create C# Scripts:** Create new C# scripts in your Unity project (e.g., in a "Scripts" folder) with the exact names provided below.
2.  **Create 3D Objects:** In your scene, create several 3D Cube GameObjects (or any other primitive).
    *   For each cube, add a **`Rigidbody`** component.
    *   Add the **`CustomPhysicsController`** component to each cube.
3.  **Configure `CustomPhysicsController` on Cubes:**
    *   On each `CustomPhysicsController`, you can set the `Initial Behavior Type` in the Inspector.
    *   Adjust the settings for `Normal Gravity Settings`, `Anti Gravity Settings`, and `Magnetic Pull Settings` as desired. These are *per-object* settings.
4.  **Create a Magnetic Target (Optional):** Create an empty GameObject (e.g., named "MagneticTarget") and position it somewhere in your scene. This will serve as the target for `MagneticPullBehavior`.
5.  **Create a Physics Driver:** Create an empty GameObject (e.g., "PhysicsDriver") and attach the **`PhysicsBehaviorDriver`** script to it.
6.  **Configure `PhysicsBehaviorDriver`:**
    *   Drag all your cubes (the ones with `CustomPhysicsController`) into the `Controllable Objects` list on the `PhysicsBehaviorDriver`.
    *   Drag your "MagneticTarget" GameObject into the `Magnetic Pull Target` field on the `PhysicsBehaviorDriver`.
7.  **Run the Scene:**
    *   Press `1` to apply Normal Gravity.
    *   Press `2` to apply Anti-Gravity.
    *   Press `3` to apply Magnetic Pull towards the designated target.
    *   Press `4` to apply Zero Gravity (no custom forces).
    *   Press `0` to apply no custom physics (objects will behave according to Unity's default Rigidbody properties, or remain static if `useGravity` is off and no forces are applied).

---

### 1. `IPhysicsBehavior.cs` (Interface)

This interface defines the contract for all custom physics behaviors.

```csharp
using UnityEngine;

/// <summary>
/// Interface for defining a custom physics behavior strategy.
/// This is the core of the CustomPhysicsPatterns (Strategy Pattern) implementation.
/// </summary>
public interface IPhysicsBehavior
{
    /// <summary>
    /// Initializes the physics behavior. Called when the behavior is set on an object.
    /// Useful for caching references or setting up initial state specific to the owner.
    /// </summary>
    /// <param name="owner">The GameObject that this behavior is applied to.</param>
    void Initialize(GameObject owner);

    /// <summary>
    /// Applies custom physics forces or modifications to the Rigidbody.
    /// This method is typically called in `FixedUpdate` by the `CustomPhysicsController`.
    /// </summary>
    /// <param name="rb">The Rigidbody component of the object.</param>
    /// <param name="deltaTime">The fixed delta time (Time.fixedDeltaTime) for frame-rate independent physics.</param>
    void ApplyPhysics(Rigidbody rb, float deltaTime);
}
```

### 2. Concrete Strategy Implementations

These classes implement `IPhysicsBehavior`, providing specific custom physics logic. They are marked `[System.Serializable]` so their settings can be configured directly in the Unity Inspector via the `CustomPhysicsController`.

#### `NormalGravityBehavior.cs`

Applies a custom downward force, simulating gravity.

```csharp
using UnityEngine;

/// <summary>
/// A concrete physics strategy that applies a custom normal gravity force.
/// This can replace or augment Unity's built-in gravity.
/// </summary>
[System.Serializable] // Makes this class's fields editable in the Inspector when used in a MonoBehaviour
public class NormalGravityBehavior : IPhysicsBehavior
{
    [Tooltip("The strength of the downward gravity force.")]
    [SerializeField] private float gravityStrength = 9.81f;

    // The Initialize method is optional if no specific setup is needed per instance.
    public void Initialize(GameObject owner)
    {
        // No specific initialization needed for this simple gravity behavior.
    }

    /// <summary>
    /// Applies a downward force to the Rigidbody based on gravityStrength.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply force to.</param>
    /// <param name="deltaTime">Fixed delta time.</param>
    public void ApplyPhysics(Rigidbody rb, float deltaTime)
    {
        // Apply a force downwards. Using ForceMode.Acceleration makes it mass-independent (applies an acceleration).
        // If rb.useGravity is false, this effectively becomes the object's gravity.
        rb.AddForce(Vector3.down * gravityStrength * rb.mass, ForceMode.Acceleration);
    }
}
```

#### `AntiGravityBehavior.cs`

Applies an upward force, simulating anti-gravity.

```csharp
using UnityEngine;

/// <summary>
/// A concrete physics strategy that applies an upward anti-gravity force.
/// </summary>
[System.Serializable]
public class AntiGravityBehavior : IPhysicsBehavior
{
    [Tooltip("The strength of the upward anti-gravity force.")]
    [SerializeField] private float antiGravityStrength = 5.0f;

    public void Initialize(GameObject owner)
    {
        // No specific initialization needed.
    }

    /// <summary>
    /// Applies an upward force to the Rigidbody.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply force to.</param>
    /// <param name="deltaTime">Fixed delta time.</param>
    public void ApplyPhysics(Rigidbody rb, float deltaTime)
    {
        rb.AddForce(Vector3.up * antiGravityStrength * rb.mass, ForceMode.Acceleration);
    }
}
```

#### `MagneticPullBehavior.cs`

Pulls the object towards a specified target.

```csharp
using UnityEngine;

/// <summary>
/// A concrete physics strategy that pulls the Rigidbody towards a target.
/// The pull strength decreases with distance.
/// </summary>
[System.Serializable]
public class MagneticPullBehavior : IPhysicsBehavior
{
    [Tooltip("The Transform of the object to pull towards.")]
    [SerializeField] private Transform target;
    [Tooltip("The maximum strength of the pull force.")]
    [SerializeField] private float pullStrength = 10.0f;
    [Tooltip("The maximum distance at which the magnetic pull is active.")]
    [SerializeField] private float maxDistance = 10.0f;

    public void Initialize(GameObject owner)
    {
        // No specific initialization needed other than target being set.
    }

    /// <summary>
    /// Sets the target for the magnetic pull.
    /// </summary>
    /// <param name="newTarget">The new Transform to pull towards.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Applies a force to pull the Rigidbody towards the target.
    /// Force magnitude scales with distance.
    /// </summary>
    /// <param name="rb">The Rigidbody to apply force to.</param>
    /// <param name="deltaTime">Fixed delta time.</param>
    public void ApplyPhysics(Rigidbody rb, float deltaTime)
    {
        if (target == null) return;

        Vector3 direction = (target.position - rb.position).normalized;
        float distance = Vector3.Distance(target.position, rb.position);

        if (distance < maxDistance)
        {
            // Calculate force that gets stronger closer to the target
            float forceMagnitude = pullStrength * (1 - (distance / maxDistance));
            rb.AddForce(direction * forceMagnitude * rb.mass, ForceMode.Acceleration);
        }
    }
}
```

#### `ZeroGravityBehavior.cs`

Applies no custom forces, essentially mimicking zero gravity if Unity's default gravity is also off.

```csharp
using UnityEngine;

/// <summary>
/// A concrete physics strategy that applies no custom forces.
/// If Rigidbody.useGravity is also false, this results in an object behaving in zero gravity.
/// </summary>
[System.Serializable]
public class ZeroGravityBehavior : IPhysicsBehavior
{
    public void Initialize(GameObject owner)
    {
        // No initialization needed as this behavior has no state to manage.
    }

    /// <summary>
    /// Applies no custom physics forces.
    /// </summary>
    /// <param name="rb">The Rigidbody (not used by this behavior).</param>
    /// <param name="deltaTime">Fixed delta time (not used by this behavior).</param>
    public void ApplyPhysics(Rigidbody rb, float deltaTime)
    {
        // This behavior does nothing, effectively "zeroing" out custom forces.
        // For actual zero-G, ensure rb.useGravity is also false in CustomPhysicsController.
    }
}
```

### 3. `CustomPhysicsController.cs` (Context)

This MonoBehaviour orchestrates the physics behavior, holding the current strategy and executing it.

```csharp
using UnityEngine;
using System.Collections.Generic; // For collections if needed

/// <summary>
/// The CustomPhysicsController acts as the 'Context' in the Strategy Pattern.
/// It holds a reference to an IPhysicsBehavior and delegates the physics calculations
/// to the currently assigned behavior. This allows for dynamic swapping of physics logic.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures the GameObject has a Rigidbody
public class CustomPhysicsController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private IPhysicsBehavior _currentPhysicsBehavior;

    /// <summary>
    /// Defines the types of physics behaviors available for selection.
    /// </summary>
    public enum PhysicsBehaviorType
    {
        None,           // No custom physics applied by this controller
        NormalGravity,  // Custom downward gravity
        AntiGravity,    // Custom upward force
        MagneticPull,   // Pull towards a target object
        ZeroGravity     // No custom forces applied
    }

    [Header("Behavior Settings")]
    [Tooltip("The initial physics behavior to apply when the game starts.")]
    [SerializeField] private PhysicsBehaviorType initialBehaviorType = PhysicsBehaviorType.NormalGravity;

    // Serialized instances of each behavior strategy. Their fields will appear in the Inspector.
    [Tooltip("Settings for the Normal Gravity behavior.")]
    [SerializeField] private NormalGravityBehavior normalGravitySettings = new NormalGravityBehavior();
    [Tooltip("Settings for the Anti-Gravity behavior.")]
    [SerializeField] private AntiGravityBehavior antiGravitySettings = new AntiGravityBehavior();
    [Tooltip("Settings for the Magnetic Pull behavior. Target must be set via SetMagneticPullTarget or driver.")]
    [SerializeField] private MagneticPullBehavior magneticPullSettings = new MagneticPullBehavior();
    // ZeroGravityBehavior has no settings, so no direct serialized field for its instance.

    [Header("Shared Behavior Properties")]
    [Tooltip("The default target for Magnetic Pull behavior. Can be overridden dynamically.")]
    [SerializeField] private Transform magneticPullTarget; // Shared target, or can be per-object

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        // Crucial for custom physics: disable Unity's default gravity if you want your
        // IPhysicsBehavior strategies to have full control over gravity effects.
        // If you want your custom behaviors to *add* to Unity's gravity, remove this line.
        _rigidbody.useGravity = false;

        // Set the initial physics behavior based on Inspector selection.
        SetPhysicsBehavior(initialBehaviorType);
    }

    void FixedUpdate()
    {
        // Physics calculations should always happen in FixedUpdate for consistency.
        if (_currentPhysicsBehavior != null)
        {
            _currentPhysicsBehavior.ApplyPhysics(_rigidbody, Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Sets the current physics behavior for this object dynamically.
    /// This is the key method for swapping strategies at runtime.
    /// </summary>
    /// <param name="behaviorType">The type of physics behavior to apply.</param>
    public void SetPhysicsBehavior(PhysicsBehaviorType behaviorType)
    {
        switch (behaviorType)
        {
            case PhysicsBehaviorType.NormalGravity:
                _currentPhysicsBehavior = normalGravitySettings;
                break;
            case PhysicsBehaviorType.AntiGravity:
                _currentPhysicsBehavior = antiGravitySettings;
                break;
            case PhysicsBehaviorType.MagneticPull:
                _currentPhysicsBehavior = magneticPullSettings;
                // Ensure the magnetic pull behavior has its target set.
                if (magneticPullTarget != null)
                {
                    magneticPullSettings.SetTarget(magneticPullTarget);
                }
                else
                {
                    Debug.LogWarning("MagneticPullBehavior requires a target, but none is set on " + gameObject.name, this);
                }
                break;
            case PhysicsBehaviorType.ZeroGravity:
                _currentPhysicsBehavior = new ZeroGravityBehavior(); // Instantiate directly as it has no state
                break;
            case PhysicsBehaviorType.None:
            default:
                _currentPhysicsBehavior = null; // No custom physics applied by this controller
                break;
        }

        // Initialize the new behavior if one was set.
        if (_currentPhysicsBehavior != null)
        {
            _currentPhysicsBehavior.Initialize(gameObject);
        }
        Debug.Log($"Physics behavior for {gameObject.name} set to: {behaviorType}", this);
    }

    /// <summary>
    /// Allows setting the magnetic pull target dynamically from an external script.
    /// </summary>
    /// <param name="newTarget">The new Transform to pull towards.</param>
    public void SetMagneticPullTarget(Transform newTarget)
    {
        magneticPullTarget = newTarget;
        // If the current behavior is MagneticPull, update its target immediately.
        if (_currentPhysicsBehavior is MagneticPullBehavior magneticBehavior)
        {
            magneticBehavior.SetTarget(newTarget);
        }
    }
}
```

### 4. `PhysicsBehaviorDriver.cs` (Example Usage / Demonstration Script)

This script manages multiple `CustomPhysicsController` objects and allows you to change their behaviors using key presses.

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script demonstrates how to interact with CustomPhysicsController objects
/// to dynamically change their physics behaviors at runtime.
/// Attach this to an empty GameObject in your scene.
/// </summary>
public class PhysicsBehaviorDriver : MonoBehaviour
{
    [Tooltip("List of CustomPhysicsController objects whose behavior can be controlled.")]
    [SerializeField] private List<CustomPhysicsController> controllableObjects;

    [Tooltip("The global target for all Magnetic Pull behaviors.")]
    [SerializeField] private Transform magneticPullTarget;

    void Update()
    {
        // Check for key presses to switch physics behaviors
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType.NormalGravity);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType.AntiGravity);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType.MagneticPull);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType.ZeroGravity);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType.None);
        }
    }

    /// <summary>
    /// Applies a specific physics behavior type to all objects in the controllableObjects list.
    /// </summary>
    /// <param name="type">The CustomPhysicsController.PhysicsBehaviorType to apply.</param>
    void ApplyBehaviorToAll(CustomPhysicsController.PhysicsBehaviorType type)
    {
        foreach (var controller in controllableObjects)
        {
            if (controller != null)
            {
                // If setting magnetic pull, ensure the target is passed to the controller.
                if (type == CustomPhysicsController.PhysicsBehaviorType.MagneticPull)
                {
                    controller.SetMagneticPullTarget(magneticPullTarget);
                }
                controller.SetPhysicsBehavior(type);
            }
        }
        Debug.Log($"Applied {type} behavior to all controlled objects.", this);
    }
}
```