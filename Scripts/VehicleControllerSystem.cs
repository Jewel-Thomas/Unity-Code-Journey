// Unity Design Pattern Example: VehicleControllerSystem
// This script demonstrates the VehicleControllerSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'VehicleControllerSystem' design pattern in Unity aims to decouple the high-level vehicle control logic (input processing, overall state management) from the low-level physics and specific implementations of vehicle components (engine, steering, braking). This promotes modularity, flexibility, and extensibility.

**Key Components of the Pattern:**

1.  **Interfaces:** Define contracts for each vehicle system (e.g., `IEngine`, `ISteering`, `IBrakeSystem`). These interfaces specify what actions a system can perform (e.g., `SetThrottle`, `SetSteerAngle`).
2.  **Concrete Implementations:** Create specific classes that implement these interfaces (e.g., `GasolineEngine`, `ElectricEngine`, `FrontWheelSteering`, `RearWheelSteering`, `ABS_BrakeSystem`). These classes handle the actual physics calculations and component interactions.
3.  **VehicleController (The Hub):** A central `MonoBehaviour` that ties everything together.
    *   It holds references to the various component *interfaces*.
    *   It receives input (player or AI).
    *   It translates this input into calls on the component interfaces, allowing the specific implementations to handle the physics.
    *   It often manages common vehicle aspects like wheel mesh synchronization.

**Benefits:**

*   **Modularity:** Easily swap out different engine types, steering mechanisms, or brake systems without changing the core `VehicleController` logic.
*   **Extensibility:** Add new vehicle systems (e.g., `IWeaponSystem`, `ISuspensionSystem`) by defining new interfaces and implementations without altering existing code.
*   **Maintainability:** Each component is responsible for its own behavior, making debugging and updates easier.
*   **Testability:** Individual components can be unit-tested in isolation.
*   **Design Flexibility:** Create a wide variety of vehicles (cars, tanks, trucks) using the same underlying system, just by combining different component implementations.

---

Here's a complete C# Unity example demonstrating this pattern.

### Project Setup in Unity:

1.  Create a new C# Script named `VehicleControllerSystem`.
2.  Copy and paste *all* the code provided below into this single script file.
3.  Create a 3D object (e.g., a Cube) to represent your vehicle's body. Add a `Rigidbody` component to it.
4.  Add four empty `GameObject`s as children to the vehicle body, name them `FrontLeftWheel`, `FrontRightWheel`, `RearLeftWheel`, `RearRightWheel`.
5.  Add `WheelCollider` components to these four empty GameObjects. Adjust their radii, suspensions, etc., as needed.
6.  Optionally, add a cylinder mesh as a child to each wheel `GameObject` and position it to visually represent the wheel.
7.  Attach the `VehicleControllerSystem` script to your vehicle's main body `GameObject`.
8.  In the Inspector of the `VehicleControllerSystem` script:
    *   Drag your vehicle's `Rigidbody` into the `Rigidbody` field.
    *   Drag all four `WheelCollider` GameObjects into the `All Wheels`, `Drive Wheels`, `Steering Wheels`, and `Brake Wheels` arrays.
    *   Drag your wheel mesh `Transform`s (if you created them) into the `Wheel Meshes` array, matching the order of `All Wheels`.
    *   Crucially, assign the `BasicEngine`, `FrontWheelSteering`, and `BasicBrakeSystem` components *which are automatically added to the same GameObject* when you put the `VehicleControllerSystem` script on it, to their respective `Engine Implementation`, `Steering Implementation`, and `Brake System Implementation` fields.

---

### `VehicleControllerSystem.cs` (Single File)

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- 1. INTERFACES: Define the contracts for vehicle components ---

/// <summary>
/// Defines the interface for any engine system in the vehicle.
/// </summary>
public interface IEngine
{
    /// <summary>
    /// Initializes the engine with the vehicle's Rigidbody and drive wheels.
    /// </summary>
    /// <param name="rb">The Rigidbody of the vehicle.</param>
    /// <param name="driveWheels">An array of WheelColliders that are driven by this engine.</param>
    void Initialize(Rigidbody rb, WheelCollider[] driveWheels);

    /// <summary>
    /// Sets the throttle input for the engine.
    /// </summary>
    /// <param name="throttleInput">A float between 0 and 1, where 1 is full throttle.</param>
    void SetThrottle(float throttleInput);

    /// <summary>
    /// Gets the current Revolutions Per Minute (RPM) of the engine.
    /// </summary>
    float GetCurrentRPM();

    /// <summary>
    /// Gets the maximum possible RPM of the engine.
    /// </summary>
    float GetMaxRPM();
}

/// <summary>
/// Defines the interface for any steering system in the vehicle.
/// </summary>
public interface ISteering
{
    /// <summary>
    /// Initializes the steering system with the wheels that it controls.
    /// </summary>
    /// <param name="steeringWheels">An array of WheelColliders that are steered.</param>
    void Initialize(WheelCollider[] steeringWheels);

    /// <summary>
    /// Sets the steering input for the vehicle.
    /// </summary>
    /// <param name="steerInput">A float between -1 and 1, where -1 is full left, 1 is full right.</param>
    void SetSteerAngle(float steerInput);
}

/// <summary>
/// Defines the interface for any brake system in the vehicle.
/// </summary>
public interface IBrakeSystem
{
    /// <summary>
    /// Initializes the brake system with all the vehicle's wheels.
    /// </summary>
    /// <param name="allWheels">An array of all WheelColliders on the vehicle.</param>
    void Initialize(WheelCollider[] allWheels);

    /// <summary>
    /// Sets the braking input for the system.
    /// </summary>
    /// <param name="brakeInput">A float between 0 and 1, where 1 is full brake.</param>
    void SetBrakeAmount(float brakeInput);

    /// <summary>
    /// Applies or releases the handbrake.
    /// </summary>
    /// <param name="apply">True to apply handbrake, false to release.</param>
    void ApplyHandbrake(bool apply);
}

// --- 2. CONCRETE IMPLEMENTATIONS: Specific component logic ---

/// <summary>
/// A basic engine implementation that applies torque to drive wheels.
/// </summary>
[RequireComponent(typeof(VehicleControllerSystem))] // Ensures this is added to a VehicleControllerSystem object
public class BasicEngine : MonoBehaviour, IEngine
{
    [Header("Engine Settings")]
    [Tooltip("Maximum torque the engine can produce.")]
    [SerializeField] private float _maxTorque = 200f;
    [Tooltip("Maximum RPM the engine can reach.")]
    [SerializeField] private float _maxRPM = 6000f;
    [Tooltip("How quickly RPM changes.")]
    [SerializeField] private float _rpmLerpSpeed = 2f;
    [Tooltip("Coefficient to simulate engine drag at idle.")]
    [SerializeField] private float _idleDragCoefficient = 0.05f;

    private Rigidbody _rigidbody;
    private WheelCollider[] _driveWheels;
    private float _currentRPM;
    private float _throttleInput;

    // Private lists to avoid GC alloc in FixedUpdate
    private List<WheelCollider> _activeDriveWheels = new List<WheelCollider>();

    public void Initialize(Rigidbody rb, WheelCollider[] driveWheels)
    {
        _rigidbody = rb;
        _driveWheels = driveWheels;
        _activeDriveWheels.Clear();
        if (_driveWheels != null)
        {
            _activeDriveWheels.AddRange(_driveWheels);
        }
    }

    public void SetThrottle(float throttleInput)
    {
        _throttleInput = Mathf.Clamp01(throttleInput);
    }

    public float GetCurrentRPM()
    {
        return _currentRPM;
    }

    public float GetMaxRPM()
    {
        return _maxRPM;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null || _activeDriveWheels.Count == 0) return;

        // Calculate average wheel RPM for the engine
        float totalWheelRPM = 0f;
        int activeWheels = 0;
        foreach (var wheel in _activeDriveWheels)
        {
            if (wheel.isGrounded)
            {
                totalWheelRPM += wheel.rpm;
                activeWheels++;
            }
        }
        float averageWheelRPM = activeWheels > 0 ? totalWheelRPM / activeWheels : 0f;

        // Simulate engine RPM based on throttle and wheel RPM
        float targetRPM = _throttleInput * _maxRPM;
        float desiredRPMFromWheels = averageWheelRPM * 60f / (2 * Mathf.PI * _activeDriveWheels[0].radius); // Approximate conversion
        
        // Blend target RPM with wheel-driven RPM
        if (_throttleInput > 0.1f) // When accelerating, engine RPM leads wheel RPM
        {
             _currentRPM = Mathf.Lerp(_currentRPM, targetRPM + (desiredRPMFromWheels * 0.5f), Time.fixedDeltaTime * _rpmLerpSpeed);
        }
        else // When decelerating or idle, engine RPM follows wheel RPM more closely
        {
            _currentRPM = Mathf.Lerp(_currentRPM, Mathf.Max(desiredRPMFromWheels, targetRPM), Time.fixedDeltaTime * _rpmLerpSpeed * 2);
        }
        _currentRPM = Mathf.Clamp(_currentRPM, 0f, _maxRPM);

        // Calculate and apply motor torque
        float motorTorque = _throttleInput * _maxTorque * (1 - (_currentRPM / _maxRPM)); // Torque decreases with RPM
        
        // Add some engine drag when not throttling, to simulate engine braking
        if (_throttleInput < 0.01f)
        {
            motorTorque -= _currentRPM * _idleDragCoefficient;
        }

        foreach (var wheel in _activeDriveWheels)
        {
            wheel.motorTorque = motorTorque;
        }
    }
}

/// <summary>
/// A basic steering implementation for front wheels.
/// </summary>
[RequireComponent(typeof(VehicleControllerSystem))]
public class FrontWheelSteering : MonoBehaviour, ISteering
{
    [Header("Steering Settings")]
    [Tooltip("Maximum angle the steering wheels can turn.")]
    [SerializeField] private float _maxSteerAngle = 30f;
    [Tooltip("Speed at which the steering angle changes.")]
    [SerializeField] private float _steerSpeed = 5f;

    private WheelCollider[] _steeringWheels;
    private float _targetSteerAngle;

    public void Initialize(WheelCollider[] steeringWheels)
    {
        _steeringWheels = steeringWheels;
    }

    public void SetSteerAngle(float steerInput)
    {
        _targetSteerAngle = steerInput * _maxSteerAngle;
    }

    private void FixedUpdate()
    {
        if (_steeringWheels == null) return;

        foreach (var wheel in _steeringWheels)
        {
            wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, _targetSteerAngle, Time.fixedDeltaTime * _steerSpeed);
        }
    }
}

/// <summary>
/// A basic brake system implementation that applies brake torque to all wheels.
/// </summary>
[RequireComponent(typeof(VehicleControllerSystem))]
public class BasicBrakeSystem : MonoBehaviour, IBrakeSystem
{
    [Header("Brake Settings")]
    [Tooltip("Maximum brake torque applied when brake input is 1.")]
    [SerializeField] private float _maxBrakeTorque = 300f;
    [Tooltip("Brake torque applied when handbrake is active.")]
    [SerializeField] private float _handbrakeTorque = 2000f;

    private WheelCollider[] _allWheels;
    private float _brakeInput;
    private bool _handbrakeActive;

    public void Initialize(WheelCollider[] allWheels)
    {
        _allWheels = allWheels;
    }

    public void SetBrakeAmount(float brakeInput)
    {
        _brakeInput = Mathf.Clamp01(brakeInput);
    }

    public void ApplyHandbrake(bool apply)
    {
        _handbrakeActive = apply;
    }

    private void FixedUpdate()
    {
        if (_allWheels == null) return;

        float currentBrakeTorque = _handbrakeActive ? _handbrakeTorque : _brakeInput * _maxBrakeTorque;

        foreach (var wheel in _allWheels)
        {
            wheel.brakeTorque = currentBrakeTorque;
        }
    }
}

// --- 3. VEHICLE CONTROLLER: The central hub managing components and input ---

/// <summary>
/// The main VehicleController that orchestrates different vehicle components
/// based on input. It acts as the central hub of the VehicleControllerSystem pattern.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BasicEngine))]        // Automatically adds BasicEngine
[RequireComponent(typeof(FrontWheelSteering))] // Automatically adds FrontWheelSteering
[RequireComponent(typeof(BasicBrakeSystem))]   // Automatically adds BasicBrakeSystem
public class VehicleControllerSystem : MonoBehaviour
{
    [Header("Core Vehicle Components")]
    [Tooltip("The Rigidbody component of the vehicle.")]
    [SerializeField] private Rigidbody _rigidbody;
    [Tooltip("All WheelColliders on the vehicle.")]
    [SerializeField] private WheelCollider[] _allWheels;
    [Tooltip("WheelColliders that receive power from the engine.")]
    [SerializeField] private WheelCollider[] _driveWheels;
    [Tooltip("WheelColliders that are steered.")]
    [SerializeField] private WheelCollider[] _steeringWheels;
    [Tooltip("WheelColliders that apply brake force (can be all wheels or specific ones).")]
    [SerializeField] private WheelCollider[] _brakeWheels; // For this example, assuming all wheels, but could be specific

    [Header("Visuals")]
    [Tooltip("Corresponding Transform for each WheelCollider to update its position and rotation.")]
    [SerializeField] private Transform[] _wheelMeshes;

    [Header("Component Implementations (Drag Components from this GameObject)")]
    [Tooltip("Reference to the MonoBehaviour implementing IEngine.")]
    [SerializeField] private MonoBehaviour _engineImplementation;
    [Tooltip("Reference to the MonoBehaviour implementing ISteering.")]
    [SerializeField] private MonoBehaviour _steeringImplementation;
    [Tooltip("Reference to the MonoBehaviour implementing IBrakeSystem.")]
    [SerializeField] private MonoBehaviour _brakeSystemImplementation;

    // Private references to the interfaces
    private IEngine _engine;
    private ISteering _steering;
    private IBrakeSystem _brakeSystem;

    // Input values
    private float _throttleInput;
    private float _steerInput;
    private float _brakeInput;
    private bool _handbrakeInput;

    private void Awake()
    {
        // Get Rigidbody if not assigned
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
        if (_rigidbody == null)
        {
            Debug.LogError("VehicleControllerSystem requires a Rigidbody component.", this);
            enabled = false;
            return;
        }

        // Validate and assign component interfaces
        _engine = _engineImplementation as IEngine;
        if (_engine == null)
        {
            Debug.LogError("Engine Implementation does not implement IEngine interface or is not assigned!", this);
            enabled = false;
            return;
        }

        _steering = _steeringImplementation as ISteering;
        if (_steering == null)
        {
            Debug.LogError("Steering Implementation does not implement ISteering interface or is not assigned!", this);
            enabled = false;
            return;
        }

        _brakeSystem = _brakeSystemImplementation as IBrakeSystem;
        if (_brakeSystem == null)
        {
            Debug.LogError("Brake System Implementation does not implement IBrakeSystem interface or is not assigned!", this);
            enabled = false;
            return;
        }

        // Initialize components
        _engine.Initialize(_rigidbody, _driveWheels);
        _steering.Initialize(_steeringWheels);
        _brakeSystem.Initialize(_brakeWheels); // Using brakeWheels for clarity, could be allWheels if desired
    }

    private void Update()
    {
        // --- Input Handling ---
        _throttleInput = Input.GetAxis("Vertical"); // W/S or Up/Down arrows
        _steerInput = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        _handbrakeInput = Input.GetKey(KeyCode.Space);

        // Separate brake input from throttle (e.g., using 'S' or 'Down arrow' for reverse and brake)
        if (_throttleInput < 0) // If pressing 'S' or 'Down arrow'
        {
            _brakeInput = -_throttleInput; // Use the magnitude as brake input
            _throttleInput = 0; // No forward throttle
        }
        else
        {
            _brakeInput = 0; // No separate brake if moving forward
        }

        // Optionally, if you want a dedicated brake button (e.g., 'K' for brake pedal)
        // if (Input.GetKey(KeyCode.K)) _brakeInput = 1f;
    }

    private void FixedUpdate()
    {
        // --- Apply Input to Components ---
        _engine.SetThrottle(_throttleInput);
        _steering.SetSteerAngle(_steerInput);
        _brakeSystem.SetBrakeAmount(_brakeInput);
        _brakeSystem.ApplyHandbrake(_handbrakeInput);

        // --- Synchronize Wheel Meshes (Visuals) ---
        UpdateWheelMeshes();
    }

    /// <summary>
    /// Synchronizes the position and rotation of visual wheel meshes with their WheelColliders.
    /// </summary>
    private void UpdateWheelMeshes()
    {
        if (_allWheels == null || _wheelMeshes == null || _allWheels.Length != _wheelMeshes.Length)
        {
            // Debug.LogWarning("Wheel Colliders and/or Wheel Meshes not properly set up.", this);
            return;
        }

        for (int i = 0; i < _allWheels.Length; i++)
        {
            if (_allWheels[i] == null || _wheelMeshes[i] == null) continue;

            Vector3 position;
            Quaternion rotation;
            _allWheels[i].GetWorldPose(out position, out rotation);

            _wheelMeshes[i].position = position;
            _wheelMeshes[i].rotation = rotation;
        }
    }

    /// <summary>
    /// Example of how to swap an engine at runtime (for demonstration purposes).
    /// You might use this in a garage system or for special abilities.
    /// </summary>
    public void SwapEngine(IEngine newEngine)
    {
        // Important: If newEngine is a MonoBehaviour, ensure it's on the GameObject
        // or instantiated properly. For this example, assuming it's a MonoBehaviour
        // and has been added to the scene already or will be.
        MonoBehaviour newEngineMono = newEngine as MonoBehaviour;
        if (newEngineMono == null)
        {
            Debug.LogError("Cannot swap engine: newEngine must be a MonoBehaviour.");
            return;
        }

        // Clean up old engine (if needed, e.g., unsubscribe from events)
        // For simple components like these, no explicit cleanup is needed.

        _engine = newEngine;
        _engineImplementation = newEngineMono; // Update the serialized field for inspection
        _engine.Initialize(_rigidbody, _driveWheels); // Re-initialize with current vehicle data
        Debug.Log($"Engine swapped to: {newEngine.GetType().Name}");
    }
}

/*
// --- EXAMPLE USAGE IN COMMENTS ---

/// <summary>
/// To extend the system, let's create an "Electric Engine" and a "Tank Steering" system.
/// </summary>

// --- New Engine Implementation (Example) ---
// You would put this in a new C# script file named "ElectricEngine.cs"
/*
using UnityEngine;

public class ElectricEngine : MonoBehaviour, IEngine
{
    [Header("Electric Engine Settings")]
    [SerializeField] private float _maxPower = 300f; // More like 'horsepower' in electric motors
    [SerializeField] private float _maxRPM = 10000f;
    [SerializeField] private float _regenBrakeFactor = 0.1f; // Regenerative braking
    [SerializeField] private float _batteryCharge = 100f; // Simple battery model

    private Rigidbody _rigidbody;
    private WheelCollider[] _driveWheels;
    private float _currentRPM;
    private float _throttleInput;

    private List<WheelCollider> _activeDriveWheels = new List<WheelCollider>();

    public void Initialize(Rigidbody rb, WheelCollider[] driveWheels)
    {
        _rigidbody = rb;
        _driveWheels = driveWheels;
        _activeDriveWheels.Clear();
        if (_driveWheels != null)
        {
            _activeDriveWheels.AddRange(_driveWheels);
        }
        _currentRPM = 0f;
        _batteryCharge = 100f; // Start fully charged
    }

    public void SetThrottle(float throttleInput)
    {
        _throttleInput = Mathf.Clamp(throttleInput, -1f, 1f); // Electric motors can have reverse torque
    }

    public float GetCurrentRPM()
    {
        return _currentRPM;
    }

    public float GetMaxRPM()
    {
        return _maxRPM;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null || _activeDriveWheels.Count == 0) return;

        float totalWheelAngularVelocity = 0f;
        int activeWheels = 0;
        foreach (var wheel in _activeDriveWheels)
        {
            totalWheelAngularVelocity += wheel.rpm * (2 * Mathf.PI / 60f); // Convert RPM to rad/s
            activeWheels++;
        }
        float averageWheelAngularVelocity = activeWheels > 0 ? totalWheelAngularVelocity / activeWheels : 0f;

        // Simple RPM update based on throttle and wheel speed
        _currentRPM = Mathf.Abs(averageWheelAngularVelocity) * 60f / (2 * Mathf.PI) * 2; // Rough conversion back to RPM, scaling up
        _currentRPM = Mathf.Lerp(_currentRPM, _throttleInput * _maxRPM, Time.fixedDeltaTime * 3f);
        _currentRPM = Mathf.Clamp(Mathf.Abs(_currentRPM), 0f, _maxRPM);

        float motorTorque = _throttleInput * _maxPower * (1 - (_currentRPM / _maxRPM));
        
        // Regenerative braking when coasting or braking
        if (_throttleInput < 0.05f && _throttleInput > -0.05f && _rigidbody.velocity.magnitude > 0.1f)
        {
            motorTorque -= _rigidbody.velocity.magnitude * _regenBrakeFactor;
            _batteryCharge = Mathf.Min(100f, _batteryCharge + (_rigidbody.velocity.magnitude * Time.fixedDeltaTime * 0.1f));
        }
        // Apply reverse torque if throttle is negative
        else if (_throttleInput < -0.05f)
        {
             motorTorque = _throttleInput * _maxPower * (1 - (_currentRPM / _maxRPM));
             _batteryCharge = Mathf.Max(0f, _batteryCharge - Mathf.Abs(motorTorque) * Time.fixedDeltaTime * 0.01f);
        }
        else if (_throttleInput > 0.05f)
        {
            _batteryCharge = Mathf.Max(0f, _batteryCharge - motorTorque * Time.fixedDeltaTime * 0.01f);
        }
        
        if (_batteryCharge <= 0f) motorTorque = 0f; // No power if battery is dead

        foreach (var wheel in _activeDriveWheels)
        {
            wheel.motorTorque = motorTorque;
        }
    }
}
*/

// --- New Steering Implementation (Example) ---
// You would put this in a new C# script file named "TankSteering.cs"
/*
using UnityEngine;

public class TankSteering : MonoBehaviour, ISteering
{
    [Header("Tank Steering Settings")]
    [SerializeField] private float _turnSpeed = 100f; // How fast the tank pivots
    
    private WheelCollider[] _leftWheels;
    private WheelCollider[] _rightWheels;
    private float _steerInput;

    // Tank steering needs separate control over left and right tracks/wheels.
    // So, it would require a different Initialize signature or the VehicleControllerSystem
    // would need to provide separate Left/Right wheel arrays.
    // For simplicity, let's assume _steeringWheels[0] and [1] are left, and [2] and [3] are right.
    public void Initialize(WheelCollider[] allWheels) // Renaming parameter for clarity
    {
        // This is a simplified example. In a real tank, you'd likely define specific
        // left and right drive wheels and pass them.
        // For demonstration, let's just use all provided wheels.
        _leftWheels = new WheelCollider[allWheels.Length / 2];
        _rightWheels = new WheelCollider[allWheels.Length / 2];

        for (int i = 0; i < allWheels.Length; i++)
        {
            if (i < allWheels.Length / 2)
            {
                _leftWheels[i] = allWheels[i];
            }
            else
            {
                _rightWheels[i - allWheels.Length / 2] = allWheels[i];
            }
        }
    }

    public void SetSteerAngle(float steerInput)
    {
        _steerInput = steerInput; // SteerInput directly controls differential speed
    }

    private void FixedUpdate()
    {
        // Tank steering typically applies differential torque to wheels
        // rather than changing steer angle. This would ideally be an Engine responsibility
        // or handled by a combined 'DriveAndSteer' component.
        // For this ISteering example, we'll just modify the _rigidbody's angular velocity directly
        // for pivot steering, or send signals to an engine if it were combined.
        
        // This is a placeholder as ISteering is designed for steerAngle.
        // A true TankSteering might require the IEngine interface to also provide torque for individual wheels.
        // For demonstration, let's simulate a pivot turn.
        if (Mathf.Abs(_steerInput) > 0.1f)
        {
             // This would typically be handled by modifying wheel motorTorque, not directly Rigidbody
             // However, for pure ISteering demo of tank pivot, it shows a different control
             // This assumes the engine is off or provides neutral torque.
             // If combined with BasicEngine, you'd need a more complex interaction.
            GetComponent<Rigidbody>().angularVelocity = new Vector3(0, _steerInput * _turnSpeed * Mathf.Deg2Rad, 0);
        }
    }
}
*/
*/
```