// Unity Design Pattern Example: ShipNavigationSystem
// This script demonstrates the ShipNavigationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `ShipNavigationSystem` pattern, as interpreted here, combines aspects of the **Strategy Pattern** and a **Centralized Manager/Facade**. It aims to decouple a ship's movement logic from the ship itself, allowing various navigation behaviors (strategies) to be easily swapped at runtime, all orchestrated by a central system.

Here's how it works:

1.  **`INavigationStrategy` (Strategy Interface):** Defines the common interface for all navigation algorithms (e.g., `Navigate`, `GetStrategyName`). This ensures all concrete strategies can be used interchangeably.
2.  **`NavigableShip` (Context):** This `MonoBehaviour` represents the entity that needs to be navigated. It holds its current state (position, speed, destination, waypoints) and a reference to its currently active `INavigationStrategy`. It delegates the actual movement logic to this strategy in its `Update()` method. It doesn't know *how* to navigate, only *that* it needs to delegate to its strategy.
3.  **Concrete `NavigationStrategy` Implementations (ScriptableObjects):** These are `ScriptableObject` assets (e.g., `DirectPathStrategySO`, `WaypointPathStrategySO`, `PatrolStrategySO`, `StandStillStrategy`). Each implements `INavigationStrategy` and encapsulates a specific movement algorithm. Using `ScriptableObject`s makes these strategies reusable assets configurable directly in the Unity Editor. They are stateless; all necessary data (destination, waypoints, etc.) is held by the `NavigableShip` context.
4.  **`ShipNavigationSystem` (Manager/Orchestrator):** This `MonoBehaviour` is the central hub. It keeps track of all `NavigableShip` instances it manages and holds references to the various `INavigationStrategy` `ScriptableObject` assets. It provides a simple API (e.g., `SetShipDirectPath`, `SetShipWaypointPath`) for other parts of your game to tell ships *what* to do. Internally, it configures the target `NavigableShip`'s data (destination, waypoints) and then assigns the appropriate `INavigationStrategy` to it.

This design allows:
*   **Flexibility:** Easily switch navigation behaviors for a ship at runtime.
*   **Reusability:** Strategies (as `ScriptableObject`s) can be shared across multiple ships.
*   **Maintainability:** Navigation logic is separated into distinct, testable classes.
*   **Clarity:** The `ShipNavigationSystem` provides a clear, high-level interface for commanding ships.

---

### Project Setup Instructions:

1.  **Create C# Scripts:**
    *   Create a new C# Script named `INavigationStrategy`. Copy the content for `INavigationStrategy.cs`.
    *   Create a new C# Script named `NavigableShip`. Copy the content for `NavigableShip.cs`.
    *   Create new C# Scripts for each strategy: `StandStillStrategy`, `DirectPathStrategySO`, `WaypointPathStrategySO`, `PatrolStrategySO`. Copy their respective contents.
    *   Create a new C# Script named `ShipNavigationSystem`. Copy the content for `ShipNavigationSystem.cs`.

2.  **Create Strategy ScriptableObjects:**
    *   In your Unity Project window, right-click -> Create -> Ship Navigation -> Strategies.
    *   Create one of each: `Direct Path`, `Waypoint Path`, `Patrol Path`, `Stand Still`. These will appear as assets in your project.

3.  **Set up a Navigable Ship:**
    *   Create a 3D Object (e.g., a Cube) in your scene. Rename it "Ship_01".
    *   Add a `Rigidbody` component to "Ship_01". Set `Is Kinematic` to `true` (important for this example's direct movement control).
    *   Add the `NavigableShip` component to "Ship_01".
    *   Optionally, adjust its `Speed`, `Turn Rate`, and `Arrival Threshold`.

4.  **Set up the Ship Navigation System:**
    *   Create an Empty GameObject in your scene. Rename it "NavigationManager".
    *   Add the `ShipNavigationSystem` component to "NavigationManager".
    *   In the Inspector for "NavigationManager":
        *   Drag your "Ship_01" (the GameObject) into the `Registered Ships` list.
        *   Drag each of the `ScriptableObject` strategy assets you created (DirectPathStrategy, WaypointPathStrategy, PatrolStrategy, StandStillStrategy) into their corresponding slots under `Available Navigation Strategies`.
        *   For the `PatrolStrategySO` asset, you might want to open it in the Inspector and pre-define some `Patrol Points` there if you want to use it as a shared patrol definition (though the example code now passes points dynamically).

5.  **Run and Test:**
    *   Run the scene.
    *   Select "NavigationManager" in the Hierarchy.
    *   In the Inspector, you'll see "Example" context menu buttons (e.g., "Example: Ship 0 Direct Path to (50,0,0)"). Click these to see "Ship_01" move according to the assigned strategy.
    *   You can also call these methods from your own scripts (e.g., a UI button, another game event).

---

### 1. `INavigationStrategy.cs`

```csharp
using UnityEngine;

/// <summary>
/// Defines the contract for all navigation behaviors.
/// Ships will use an instance of this interface to perform their movement.
/// This is the 'Strategy' interface in the Strategy Design Pattern.
/// </summary>
public interface INavigationStrategy
{
    /// <summary>
    /// The core method that encapsulates the navigation logic.
    /// </summary>
    /// <param name="ship">The NavigableShip instance that this strategy should navigate (the 'Context').</param>
    /// <param name="deltaTime">The time elapsed since the last frame, used for frame-rate independent movement.</param>
    void Navigate(NavigableShip ship, float deltaTime);

    /// <summary>
    /// Provides a human-readable name for the strategy, useful for debugging or UI.
    /// </summary>
    /// <returns>A string representing the name of the strategy.</returns>
    string GetStrategyName();
}
```

### 2. `NavigableShip.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Any(), .ToList()

/// <summary>
/// This MonoBehaviour represents a ship that can be navigated.
/// It acts as the 'Context' in the Strategy pattern, holding its current state
/// and delegating the actual movement logic to its currently assigned strategy.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ships usually have physics
public class NavigableShip : MonoBehaviour
{
    [Header("Ship Properties")]
    [Tooltip("The speed at which the ship moves.")]
    [SerializeField] private float _speed = 10f;
    [Tooltip("The rate at which the ship turns towards its target (degrees per second).")]
    [SerializeField] private float _turnRate = 90f;
    [Tooltip("How close the ship needs to be to a waypoint/destination to consider it reached.")]
    [SerializeField] private float _arrivalThreshold = 1f;

    // The currently active navigation strategy.
    // This is where the strategy pattern allows swapping behaviors.
    private INavigationStrategy _currentStrategy;

    // Internal state variables for various navigation types.
    // These are managed by the ShipNavigationSystem and accessed by strategies.
    private Vector3 _currentDestination;
    private Queue<Vector3> _waypoints = new Queue<Vector3>();
    private List<Vector3> _patrolPoints = new List<Vector3>();
    private int _currentPatrolIndex = 0;

    // Public properties to allow strategies to access ship state and parameters.
    public float Speed => _speed;
    public float TurnRate => _turnRate;
    public float ArrivalThreshold => _arrivalThreshold;
    public Vector3 CurrentDestination => _currentDestination;
    public bool HasDestination => _currentDestination != Vector3.zero; // Or use a separate flag
    public bool HasWaypoints => _waypoints.Any();
    public List<Vector3> PatrolPoints => _patrolPoints;
    public int CurrentPatrolIndex => _currentPatrolIndex;

    private Rigidbody _rigidbody;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("NavigableShip requires a Rigidbody component!");
            enabled = false; // Disable if no Rigidbody
            return;
        }
        _rigidbody.isKinematic = true; // For demonstration, we'll control movement directly.
                                       // In a real project, consider physics-based movement using AddForce/AddTorque.

        // A ship starts with no active strategy. The ShipNavigationSystem will assign one.
        // Update() method will safely handle a null strategy.
    }

    void Update()
    {
        // Delegate the actual navigation logic to the current strategy.
        // This is the core of the Strategy Pattern: behavior is delegated.
        _currentStrategy?.Navigate(this, Time.deltaTime);

        // Optional: Debug visualization for paths in the editor
        if (Application.isEditor && _currentStrategy != null)
        {
            if (_currentDestination != Vector3.zero)
            {
                Debug.DrawLine(transform.position, _currentDestination, Color.blue);
            }

            Vector3 prevWaypoint = transform.position;
            foreach (var wp in _waypoints)
            {
                Debug.DrawLine(prevWaypoint, wp, Color.cyan);
                prevWaypoint = wp;
            }

            if (_patrolPoints.Any())
            {
                for (int i = 0; i < _patrolPoints.Count; i++)
                {
                    Debug.DrawLine(_patrolPoints[i], _patrolPoints[(i + 1) % _patrolPoints.Count], Color.green);
                }
            }
        }
    }

    /// <summary>
    /// Sets the navigation strategy for this ship. This method resets previous navigation data.
    /// </summary>
    /// <param name="newStrategy">The new strategy to use for navigation.</param>
    public void SetStrategy(INavigationStrategy newStrategy)
    {
        if (newStrategy == null)
        {
            Debug.LogError("Attempted to set a null navigation strategy for ship: " + name);
            return;
        }
        _currentStrategy = newStrategy;
        Debug.Log($"{name} changed strategy to: {_currentStrategy.GetStrategyName()}");

        // Reset internal navigation state when strategy changes to avoid conflicts
        _currentDestination = Vector3.zero;
        _waypoints.Clear();
        _patrolPoints.Clear();
        _currentPatrolIndex = 0;
    }

    /// <summary>
    /// Sets a single destination for the ship. This prepares the ship for a direct path strategy.
    /// </summary>
    /// <param name="destination">The target position.</param>
    public void SetDestination(Vector3 destination)
    {
        _currentDestination = destination;
        _waypoints.Clear(); // Clear any existing waypoints
        _patrolPoints.Clear(); // Clear any existing patrol points
        _currentPatrolIndex = 0;
    }

    /// <summary>
    /// Sets a series of waypoints for the ship to follow. This prepares the ship for a waypoint strategy.
    /// </summary>
    /// <param name="waypoints">An enumerable collection of waypoints.</param>
    public void SetWaypoints(IEnumerable<Vector3> waypoints)
    {
        _waypoints = new Queue<Vector3>(waypoints);
        if (_waypoints.Any())
        {
            _currentDestination = _waypoints.Peek(); // Set first waypoint as initial destination
        }
        else
        {
            _currentDestination = Vector3.zero;
        }
        _patrolPoints.Clear(); // Clear any existing patrol points
        _currentPatrolIndex = 0;
    }

    /// <summary>
    /// Retrieves the next waypoint from the queue.
    /// </summary>
    /// <returns>The next waypoint, or Vector3.zero if no more waypoints.</returns>
    public Vector3 GetNextWaypoint()
    {
        if (_waypoints.Any())
        {
            _waypoints.Dequeue(); // Remove the waypoint just reached
            if (_waypoints.Any())
            {
                _currentDestination = _waypoints.Peek(); // Set next waypoint as new destination
                return _currentDestination;
            }
        }
        _currentDestination = Vector3.zero; // No more waypoints
        return Vector3.zero;
    }

    /// <summary>
    /// Sets a list of points for the ship to patrol in a loop.
    /// This prepares the ship for a patrol strategy.
    /// </summary>
    /// <param name="patrolPoints">A list of Vector3 points forming the patrol route.</param>
    public void SetPatrolPoints(IEnumerable<Vector3> patrolPoints)
    {
        _patrolPoints = patrolPoints?.ToList() ?? new List<Vector3>();
        _currentPatrolIndex = 0;
        if (_patrolPoints.Any())
        {
            _currentDestination = _patrolPoints[_currentPatrolIndex];
        }
        else
        {
            _currentDestination = Vector3.zero;
        }
        _waypoints.Clear(); // Clear any existing waypoints
    }

    /// <summary>
    /// Advances the patrol index to the next point in the loop.
    /// </summary>
    /// <returns>The next patrol point, or Vector3.zero if no patrol points are set.</returns>
    public Vector3 GetNextPatrolPoint()
    {
        if (!_patrolPoints.Any()) return Vector3.zero;

        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
        _currentDestination = _patrolPoints[_currentPatrolIndex];
        return _currentDestination;
    }

    /// <summary>
    /// Moves and rotates the ship towards a target position. This is a helper method
    /// that concrete strategies can use to perform the physical movement.
    /// </summary>
    /// <param name="targetPosition">The position the ship should move towards.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    public void MoveTowards(Vector3 targetPosition, float deltaTime)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction == Vector3.zero) return; // Avoid issues if already at target

        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnRate * deltaTime);

        // Move forward
        // Using Rigidbody.MovePosition for kinematic Rigidbody control.
        _rigidbody.MovePosition(transform.position + transform.forward * _speed * deltaTime);
        // Alternative for non-physics objects: transform.Translate(transform.forward * _speed * deltaTime, Space.World);
    }
}
```

### 3. Concrete Strategy Implementations (`ScriptableObject`s)

#### `StandStillStrategy.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete navigation strategy where the ship simply does not move.
/// This is useful as a default state or when explicitly stopping a ship.
/// </summary>
[CreateAssetMenu(fileName = "StandStillStrategy", menuName = "Ship Navigation/Strategies/Stand Still")]
public class StandStillStrategy : ScriptableObject, INavigationStrategy
{
    public void Navigate(NavigableShip ship, float deltaTime)
    {
        // The ship stands still. No movement logic needed here.
    }

    public string GetStrategyName() => "Stand Still";
}
```

#### `DirectPathStrategySO.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete navigation strategy that moves the ship directly towards a single destination.
/// This strategy assumes the NavigableShip's CurrentDestination property is set.
/// </summary>
[CreateAssetMenu(fileName = "DirectPathStrategy", menuName = "Ship Navigation/Strategies/Direct Path")]
public class DirectPathStrategySO : ScriptableObject, INavigationStrategy
{
    public void Navigate(NavigableShip ship, float deltaTime)
    {
        // If there's no destination set or it's been cleared, stop moving.
        if (!ship.HasDestination || ship.CurrentDestination == Vector3.zero)
        {
            return;
        }

        // Check if the ship has reached its current destination.
        if (Vector3.Distance(ship.transform.position, ship.CurrentDestination) < ship.ArrivalThreshold)
        {
            Debug.Log($"{ship.name} arrived at destination: {ship.CurrentDestination}.");
            // Clear the destination once reached. The ShipNavigationSystem can then assign a new strategy or target.
            ship.SetDestination(Vector3.zero); 
            return;
        }

        // Move the ship towards the destination using the helper method from NavigableShip.
        ship.MoveTowards(ship.CurrentDestination, deltaTime);
    }

    public string GetStrategyName() => "Direct Path";
}
```

#### `WaypointPathStrategySO.cs`

```csharp
using UnityEngine;

/// <summary>
/// A concrete navigation strategy that guides the ship through a series of waypoints.
/// This strategy uses the NavigableShip's internal waypoint queue.
/// </summary>
[CreateAssetMenu(fileName = "WaypointPathStrategy", menuName = "Ship Navigation/Strategies/Waypoint Path")]
public class WaypointPathStrategySO : ScriptableObject, INavigationStrategy
{
    public void Navigate(NavigableShip ship, float deltaTime)
    {
        // If there are no more waypoints to follow, stop this strategy.
        if (!ship.HasWaypoints)
        {
            Debug.Log($"{ship.name} completed waypoint path.");
            // Optionally, you could automatically switch to a StandStillStrategy here:
            // ship.SetStrategy(FindObjectOfType<ShipNavigationSystem>()?.GetStandStillStrategy());
            return;
        }

        // Check if the ship has reached the current waypoint (which is also its CurrentDestination).
        if (Vector3.Distance(ship.transform.position, ship.CurrentDestination) < ship.ArrivalThreshold)
        {
            Debug.Log($"{ship.name} reached waypoint: {ship.CurrentDestination}");
            ship.GetNextWaypoint(); // Advance to the next waypoint and update ship's destination.

            // If advancing to the next waypoint resulted in no more waypoints, exit.
            if (!ship.HasWaypoints)
            {
                Debug.Log($"{ship.name} completed waypoint path after advancing.");
                return;
            }
        }

        // Move the ship towards the current waypoint.
        ship.MoveTowards(ship.CurrentDestination, deltaTime);
    }

    public string GetStrategyName() => "Waypoint Path";
}
```

#### `PatrolStrategySO.cs`

```csharp
using UnityEngine;
using System.Linq; // For .Any()

/// <summary>
/// A concrete navigation strategy that makes the ship continuously patrol a predefined loop of points.
/// This strategy relies on the NavigableShip's internal patrol points list and index.
/// </summary>
[CreateAssetMenu(fileName = "PatrolStrategy", menuName = "Ship Navigation/Strategies/Patrol Path")]
public class PatrolStrategySO : ScriptableObject, INavigationStrategy
{
    public void Navigate(NavigableShip ship, float deltaTime)
    {
        // If the ship has no patrol points assigned, it cannot patrol.
        if (ship.PatrolPoints == null || !ship.PatrolPoints.Any())
        {
            // Optionally, you might log a warning or automatically switch to StandStill here.
            return;
        }

        // Check if the ship has reached its current patrol point.
        if (Vector3.Distance(ship.transform.position, ship.CurrentDestination) < ship.ArrivalThreshold)
        {
            Debug.Log($"{ship.name} reached patrol point: {ship.CurrentDestination}");
            ship.GetNextPatrolPoint(); // Advance to the next patrol point in the loop.
            Debug.Log($"{ship.name} moving to next patrol point: {ship.CurrentDestination}");
        }

        // Move the ship towards the current patrol point.
        ship.MoveTowards(ship.CurrentDestination, deltaTime);
    }

    public string GetStrategyName() => "Patrol Path";
}
```

### 4. `ShipNavigationSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Any()

/// <summary>
/// This MonoBehaviour acts as the central manager for ship navigation.
/// It embodies the 'ShipNavigationSystem' design pattern by orchestrating
/// the application of various navigation strategies to multiple NavigableShips.
/// It provides a simplified interface for game logic to control ships' movements.
/// </summary>
public class ShipNavigationSystem : MonoBehaviour
{
    [Header("Registered Ships")]
    [Tooltip("Drag all NavigableShip instances you want this system to manage here.")]
    [SerializeField] private List<NavigableShip> _registeredShips = new List<NavigableShip>();

    [Header("Available Navigation Strategies (ScriptableObjects)")]
    [Tooltip("Assign your ScriptableObject strategy assets here. Create them via 'Create -> Ship Navigation -> Strategies'.")]
    [SerializeField] private DirectPathStrategySO _directPathStrategy;
    [SerializeField] private WaypointPathStrategySO _waypointPathStrategy;
    [SerializeField] private PatrolStrategySO _patrolStrategy;
    [SerializeField] private StandStillStrategy _standStillStrategy;

    void Awake()
    {
        // Basic validation: ensure all required strategy assets are assigned.
        // In a real project, you might want more robust handling (e.g., loading from Resources or AssetBundles).
        if (_directPathStrategy == null) Debug.LogError("DirectPathStrategySO not assigned in ShipNavigationSystem!", this);
        if (_waypointPathStrategy == null) Debug.LogError("WaypointPathStrategySO not assigned in ShipNavigationSystem!", this);
        if (_patrolStrategy == null) Debug.LogError("PatrolStrategySO not assigned in ShipNavigationSystem!", this);
        if (_standStillStrategy == null) Debug.LogError("StandStillStrategy not assigned in ShipNavigationSystem!", this);

        // Optionally, ensure all registered ships start with a default 'Stand Still' strategy.
        foreach (var ship in _registeredShips)
        {
            if (ship != null && ship.enabled)
            {
                SetShipStandStill(ship);
            }
        }
    }

    /// <summary>
    /// Assigns a direct path navigation strategy to a specific ship.
    /// The ship will move directly from its current position to the given destination.
    /// </summary>
    /// <param name="ship">The NavigableShip to control.</param>
    /// <param name="destination">The target position for the ship.</param>
    public void SetShipDirectPath(NavigableShip ship, Vector3 destination)
    {
        if (!IsValidShipAndStrategy(ship, _directPathStrategy, "Direct Path")) return;

        ship.SetDestination(destination);
        ship.SetStrategy(_directPathStrategy);
    }

    /// <summary>
    /// Assigns a waypoint path navigation strategy to a specific ship.
    /// The ship will follow the given waypoints in sequence.
    /// </summary>
    /// <param name="ship">The NavigableShip to control.</param>
    /// <param name="waypoints">A list of Vector3 points for the ship to follow.</param>
    public void SetShipWaypointPath(NavigableShip ship, List<Vector3> waypoints)
    {
        if (!IsValidShipAndStrategy(ship, _waypointPathStrategy, "Waypoint Path")) return;
        if (waypoints == null || !waypoints.Any())
        {
            Debug.LogWarning($"No waypoints provided for {ship.name}. Assigning Stand Still strategy.");
            SetShipStandStill(ship);
            return;
        }

        ship.SetWaypoints(waypoints);
        ship.SetStrategy(_waypointPathStrategy);
    }

    /// <summary>
    /// Assigns a patrol path navigation strategy to a specific ship.
    /// The ship will continuously loop through the provided patrol points.
    /// </summary>
    /// <param name="ship">The NavigableShip to control.</param>
    /// <param name="patrolPoints">A list of Vector3 points for the ship to patrol.</param>
    public void SetShipPatrolPath(NavigableShip ship, List<Vector3> patrolPoints)
    {
        if (!IsValidShipAndStrategy(ship, _patrolStrategy, "Patrol Path")) return;
        if (patrolPoints == null || !patrolPoints.Any())
        {
            Debug.LogWarning($"No patrol points provided for {ship.name}. Assigning Stand Still strategy.");
            SetShipStandStill(ship);
            return;
        }

        ship.SetPatrolPoints(patrolPoints);
        ship.SetStrategy(_patrolStrategy);
    }

    /// <summary>
    /// Instructs a ship to stop moving and stand still.
    /// </summary>
    /// <param name="ship">The NavigableShip to stop.</param>
    public void SetShipStandStill(NavigableShip ship)
    {
        if (!IsValidShipAndStrategy(ship, _standStillStrategy, "Stand Still")) return;

        ship.SetDestination(Vector3.zero); // Clear any target
        ship.SetWaypoints(new List<Vector3>()); // Clear any waypoints
        ship.SetPatrolPoints(new List<Vector3>()); // Clear any patrol points
        ship.SetStrategy(_standStillStrategy);
    }

    /// <summary>
    /// Helper method to validate ship and strategy references.
    /// </summary>
    private bool IsValidShipAndStrategy(NavigableShip ship, INavigationStrategy strategy, string strategyName)
    {
        if (ship == null)
        {
            Debug.LogError($"Attempted to assign '{strategyName}' strategy to a null ship reference.");
            return false;
        }
        if (strategy == null)
        {
            Debug.LogError($"'{strategyName}' Strategy is not assigned in the ShipNavigationSystem Inspector. Please assign the ScriptableObject asset.");
            return false;
        }
        return true;
    }

    // --- Example Usage Methods (for demonstration purposes, accessed via Context Menu in Inspector) ---

    [ContextMenu("Example: Ship 0 Direct Path to (50,0,0)")]
    private void ExampleDirectPath()
    {
        if (_registeredShips.Any() && _registeredShips[0] != null)
        {
            SetShipDirectPath(_registeredShips[0], new Vector3(50, 0, 0));
        }
        else
        {
            Debug.LogWarning("No ship registered at index 0 for example usage.");
        }
    }

    [ContextMenu("Example: Ship 0 Waypoint Path")]
    private void ExampleWaypointPath()
    {
        if (_registeredShips.Any() && _registeredShips[0] != null)
        {
            List<Vector3> waypoints = new List<Vector3>()
            {
                new Vector3(20, 0, 20),
                new Vector3(0, 0, 40),
                new Vector3(-20, 0, 20),
                new Vector3(0, 0, 0)
            };
            SetShipWaypointPath(_registeredShips[0], waypoints);
        }
        else
        {
            Debug.LogWarning("No ship registered at index 0 for example usage.");
        }
    }

    [ContextMenu("Example: Ship 0 Patrol Path")]
    private void ExamplePatrolPath()
    {
        if (_registeredShips.Any() && _registeredShips[0] != null)
        {
            List<Vector3> patrolPoints = new List<Vector3>()
            {
                new Vector3(30, 0, 30),
                new Vector3(50, 0, 10),
                new Vector3(10, 0, 50)
            };
            SetShipPatrolPath(_registeredShips[0], patrolPoints);
        }
        else
        {
            Debug.LogWarning("No ship registered at index 0 for example usage.");
        }
    }

    [ContextMenu("Example: Ship 0 Stand Still")]
    private void ExampleStandStill()
    {
        if (_registeredShips.Any() && _registeredShips[0] != null)
        {
            SetShipStandStill(_registeredShips[0]);
        }
        else
        {
            Debug.LogWarning("No ship registered at index 0 for example usage.");
        }
    }
}
```