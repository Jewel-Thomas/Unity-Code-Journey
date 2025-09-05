// Unity Design Pattern Example: AILocomotionSystem
// This script demonstrates the AILocomotionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **AILocomotionSystem** pattern in Unity, which is essentially an application of the **Strategy design pattern** to AI movement. It allows an AI character to switch its movement behavior (e.g., walk, run, fly, idle) dynamically at runtime without changing its core class.

**Key Concepts of the AILocomotionSystem (Strategy Pattern):**

1.  **`ILocomotionStrategy` (Strategy Interface):** Defines a common interface for all concrete locomotion behaviors. This ensures that all movement strategies have the same methods, allowing the AI character to interact with them uniformly.
2.  **Concrete Strategy Classes (e.g., `WalkStrategy`, `RunStrategy`, `FlyStrategy`, `IdleStrategy`):** These classes implement the `ILocomotionStrategy` interface. Each class encapsulates a specific movement algorithm (e.g., how to walk, how to run, how to fly). They know their own speed and how to apply it.
3.  **`AILocomotionSystem` (Context):** This class holds a reference to the currently selected `ILocomotionStrategy`. It provides a public interface for the AI character to request movement (e.g., `ExecuteMove`, `ExecuteStop`). When a movement request comes in, it delegates the actual work to its current strategy object. It also allows setting a new strategy at runtime.
4.  **`AICharacter` (Client):** This is the `MonoBehaviour` that represents the AI in the scene. It *uses* the `AILocomotionSystem` to perform its movement. The `AICharacter` is responsible for deciding *when* to switch locomotion strategies (e.g., based on distance to target, health, environmental factors, or a state machine). It *doesn't* implement the movement logic itself.

---

### **AILocomotionSystemExample.cs**

To use this:
1.  Create a new C# script named `AILocomotionSystemExample.cs` in your Unity project.
2.  Copy and paste the entire code below into the script.
3.  Create an empty GameObject in your scene (e.g., rename it "AICharacter").
4.  Attach the `AICharacter` component (which is part of this script) to your "AICharacter" GameObject.
5.  Run the scene. The AI Character will move back and forth, switching between walk, run, and idle based on its distance to the target. Press 'F' to toggle flying mode.
6.  You can adjust the `walkSpeed`, `runSpeed`, `flySpeed`, and distance thresholds directly in the Inspector.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Good practice, often used in complex systems

/// <summary>
/// ILocomotionStrategy Interface
/// Defines the common contract for all locomotion behaviors.
/// Each concrete strategy will implement these methods, providing its specific movement logic.
/// </summary>
public interface ILocomotionStrategy
{
    /// <summary>
    /// Implements the movement logic for the agent.
    /// The strategy defines its own speed and how it moves.
    /// </summary>
    /// <param name="agentTransform">The Transform component of the AI agent.</param>
    /// <param name="targetPosition">The target world position the agent should move towards.</param>
    void Move(Transform agentTransform, Vector3 targetPosition);

    /// <summary>
    /// Implements the stop logic for the agent.
    /// </summary>
    /// <param name="agentTransform">The Transform component of the AI agent.</param>
    void Stop(Transform agentTransform);
}

// --- Concrete Locomotion Strategy Implementations ---

/// <summary>
/// WalkStrategy
/// Implements walking behavior. Moves the agent at a specified walk speed.
/// </summary>
public class WalkStrategy : ILocomotionStrategy
{
    private float _speed;

    public WalkStrategy(float speed)
    {
        _speed = speed;
    }

    public void Move(Transform agentTransform, Vector3 targetPosition)
    {
        if (agentTransform == null) return;

        // Calculate direction and move towards target
        Vector3 direction = (targetPosition - agentTransform.position).normalized;
        agentTransform.position = Vector3.MoveTowards(agentTransform.position, targetPosition, _speed * Time.deltaTime);

        // Optional: Make the character look in the direction of movement
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, targetRotation, Time.deltaTime * _speed * 5f);
        }

        Debug.Log($"<color=green>Walking</color> towards {targetPosition} at {_speed} m/s.");
    }

    public void Stop(Transform agentTransform)
    {
        // This strategy specific stop behavior (e.g., play idle animation, reduce velocity)
        Debug.Log("Walk strategy: Stopping movement.");
    }
}

/// <summary>
/// RunStrategy
/// Implements running behavior. Moves the agent at a specified run speed.
/// </summary>
public class RunStrategy : ILocomotionStrategy
{
    private float _speed;

    public RunStrategy(float speed)
    {
        _speed = speed;
    }

    public void Move(Transform agentTransform, Vector3 targetPosition)
    {
        if (agentTransform == null) return;

        Vector3 direction = (targetPosition - agentTransform.position).normalized;
        agentTransform.position = Vector3.MoveTowards(agentTransform.position, targetPosition, _speed * Time.deltaTime);

        // Optional: Make the character look in the direction of movement
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, targetRotation, Time.deltaTime * _speed * 5f);
        }

        Debug.Log($"<color=orange>Running</color> towards {targetPosition} at {_speed} m/s.");
    }

    public void Stop(Transform agentTransform)
    {
        Debug.Log("Run strategy: Stopping movement.");
    }
}

/// <summary>
/// FlyStrategy
/// Implements flying behavior. Moves the agent at a specified fly speed, potentially
/// allowing movement on the Y-axis without ground constraints.
/// </summary>
public class FlyStrategy : ILocomotionStrategy
{
    private float _speed;

    public FlyStrategy(float speed)
    {
        _speed = speed;
    }

    public void Move(Transform agentTransform, Vector3 targetPosition)
    {
        if (agentTransform == null) return;

        Vector3 direction = (targetPosition - agentTransform.position).normalized;
        agentTransform.position = Vector3.MoveTowards(agentTransform.position, targetPosition, _speed * Time.deltaTime);

        // Optional: Make the character look in the direction of movement
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, targetRotation, Time.deltaTime * _speed * 5f);
        }

        Debug.Log($"<color=blue>Flying</color> towards {targetPosition} at {_speed} m/s.");
    }

    public void Stop(Transform agentTransform)
    {
        Debug.Log("Fly strategy: Stopping movement.");
    }
}

/// <summary>
/// IdleStrategy
/// Implements idle behavior. The agent does not move.
/// </summary>
public class IdleStrategy : ILocomotionStrategy
{
    public void Move(Transform agentTransform, Vector3 targetPosition)
    {
        // No movement for idle.
        // Can add idle animation trigger here.
        Debug.Log("<color=grey>Idle</color> strategy: Not moving.");
    }

    public void Stop(Transform agentTransform)
    {
        Debug.Log("Idle strategy: Already stopped/idle.");
    }
}

// --- AILocomotionSystem Context ---

/// <summary>
/// AILocomotionSystem
/// This class acts as the Context for the Strategy pattern.
/// It holds a reference to the current ILocomotionStrategy and delegates movement calls to it.
/// The AICharacter interacts with this system, not directly with concrete strategies.
/// </summary>
public class AILocomotionSystem
{
    private ILocomotionStrategy _currentStrategy;

    /// <summary>
    /// Constructor for AILocomotionSystem.
    /// Initializes the system with an initial locomotion strategy.
    /// </summary>
    /// <param name="initialStrategy">The strategy to start with.</param>
    public AILocomotionSystem(ILocomotionStrategy initialStrategy)
    {
        SetStrategy(initialStrategy);
    }

    /// <summary>
    /// Changes the current locomotion strategy.
    /// The AI character can call this method to dynamically change its movement behavior.
    /// </summary>
    /// <param name="newStrategy">The new strategy to use.</param>
    public void SetStrategy(ILocomotionStrategy newStrategy)
    {
        if (newStrategy == null)
        {
            Debug.LogWarning("Attempted to set a null locomotion strategy. Reverting to IdleStrategy.");
            _currentStrategy = new IdleStrategy(); // Fallback to a safe default
        }
        else if (_currentStrategy != newStrategy) // Only change and log if the strategy is actually different
        {
            Debug.Log($"<color=cyan>Locomotion strategy changed to: {newStrategy.GetType().Name}</color>");
            _currentStrategy = newStrategy;
        }
    }

    /// <summary>
    /// Delegates the actual movement execution to the current strategy.
    /// </summary>
    /// <param name="agentTransform">The Transform component of the AI agent.</param>
    /// <param name="targetPosition">The target world position for movement.</param>
    public void ExecuteMove(Transform agentTransform, Vector3 targetPosition)
    {
        // Null-conditional operator ensures no error if _currentStrategy is unexpectedly null
        _currentStrategy?.Move(agentTransform, targetPosition);
    }

    /// <summary>
    /// Delegates the stop command to the current strategy.
    /// </summary>
    /// <param name="agentTransform">The Transform component of the AI agent.</param>
    public void ExecuteStop(Transform agentTransform)
    {
        _currentStrategy?.Stop(agentTransform);
    }

    /// <summary>
    /// Returns the type of the currently active strategy for debugging or state checks.
    /// </summary>
    public System.Type GetCurrentStrategyType()
    {
        return _currentStrategy?.GetType();
    }
}

// --- AICharacter (Client MonoBehaviour) ---

/// <summary>
/// AICharacter
/// This MonoBehaviour acts as the Client for the AILocomotionSystem.
/// It uses the AILocomotionSystem to control its movement based on internal logic
/// (e.g., distance to target, health, user input) or external events.
/// It decides *what* movement behavior to use, but delegates *how* to the strategies.
/// </summary>
public class AICharacter : MonoBehaviour
{
    [Header("Locomotion Speeds")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float flySpeed = 7.0f;

    [Header("Target & Distance Thresholds")]
    [Tooltip("Initial target position for the AI to move towards.")]
    [SerializeField] private Vector3 initialTargetPosition = new Vector3(10, 0, 0);
    [Tooltip("If target is further than this, the AI will Run.")]
    [SerializeField] private float runDistanceThreshold = 8.0f;
    [Tooltip("If target is further than this (but closer than run threshold), the AI will Walk.")]
    [SerializeField] private float walkDistanceThreshold = 3.0f;
    [Tooltip("If target is closer than this, the AI will Stop/Idle.")]
    [SerializeField] private float stopDistanceThreshold = 0.5f;

    // The core AILocomotionSystem instance that manages our strategies.
    private AILocomotionSystem _locomotionSystem;

    // Instances of concrete locomotion strategies.
    // We create them once and reuse them.
    private ILocomotionStrategy _walkStrategy;
    private ILocomotionStrategy _runStrategy;
    private ILocomotionStrategy _flyStrategy;
    private ILocomotionStrategy _idleStrategy;

    // Current target for the AI character.
    private Vector3 _currentTargetPosition;
    private bool _hasTarget = false;
    private bool _isFlying = false; // Example state variable to trigger flying behavior

    void Awake()
    {
        // 1. Initialize all concrete locomotion strategies with their respective speeds.
        _walkStrategy = new WalkStrategy(walkSpeed);
        _runStrategy = new RunStrategy(runSpeed);
        _flyStrategy = new FlyStrategy(flySpeed);
        _idleStrategy = new IdleStrategy();

        // 2. Initialize the AILocomotionSystem with an initial default strategy (e.g., Idle).
        _locomotionSystem = new AILocomotionSystem(_idleStrategy);

        // Set an initial target for demonstration purposes.
        SetTarget(initialTargetPosition);
    }

    void Update()
    {
        // Handles the core locomotion logic, deciding which strategy to use.
        HandleLocomotion();

        // Visual aid to see the target in the Scene view.
        DebugDrawTarget();

        // Example input to toggle flying mode.
        ToggleFlightInput();
    }

    /// <summary>
    /// Contains the AI's decision-making logic for choosing a locomotion strategy.
    /// This is where the "what to do" logic resides.
    /// </summary>
    private void HandleLocomotion()
    {
        // If there's no target, ensure the AI is idle and stopped.
        if (!_hasTarget)
        {
            _locomotionSystem.SetStrategy(_idleStrategy);
            _locomotionSystem.ExecuteStop(transform);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTargetPosition);

        // Determine the next strategy based on current state (e.g., distance, flying mode).
        ILocomotionStrategy nextStrategy = _idleStrategy; // Default to idle

        if (_isFlying)
        {
            nextStrategy = _flyStrategy;
        }
        else if (distanceToTarget > runDistanceThreshold)
        {
            nextStrategy = _runStrategy;
        }
        else if (distanceToTarget > walkDistanceThreshold)
        {
            nextStrategy = _walkStrategy;
        }
        else // Close to target
        {
            if (distanceToTarget <= stopDistanceThreshold)
            {
                // Target reached, clear target, stop, and set a new one after a delay.
                _hasTarget = false;
                Debug.Log("<color=red>Target reached! Stopping.</color>");
                _locomotionSystem.SetStrategy(_idleStrategy); // Explicitly set to idle when target reached
                _locomotionSystem.ExecuteStop(transform);
                StartCoroutine(SetNewTargetAfterDelay(2f)); // Set a new target after a 2-second delay
                return; // Exit early as movement is handled by the coroutine
            }
            else
            {
                // Still within walk threshold, but not yet at stop distance, so continue walking.
                nextStrategy = _walkStrategy;
            }
        }

        // Set the chosen strategy in the locomotion system.
        // The AILocomotionSystem itself will prevent redundant strategy changes/logs.
        _locomotionSystem.SetStrategy(nextStrategy);

        // Execute the movement using the currently active strategy.
        // This delegates the "how to do it" to the strategy.
        _locomotionSystem.ExecuteMove(transform, _currentTargetPosition);
    }

    /// <summary>
    /// Public method to set a new target position for the AI character.
    /// This can be called by other systems (e.g., a game manager, player input).
    /// </summary>
    /// <param name="newTarget">The new target world position.</param>
    public void SetTarget(Vector3 newTarget)
    {
        _currentTargetPosition = newTarget;
        _hasTarget = true;
        Debug.Log($"<color=magenta>AICharacter received new target: {_currentTargetPosition}</color>");
    }

    /// <summary>
    /// Coroutine to set a new target after a delay, demonstrating continuous movement.
    /// </summary>
    private IEnumerator SetNewTargetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Toggles the target between the initial position and its negative X-axis counterpart.
        Vector3 newTarget;
        if (Mathf.Approximately(_currentTargetPosition.x, initialTargetPosition.x) &&
            Mathf.Approximately(_currentTargetPosition.y, initialTargetPosition.y) &&
            Mathf.Approximately(_currentTargetPosition.z, initialTargetPosition.z))
        {
            newTarget = new Vector3(-initialTargetPosition.x, initialTargetPosition.y, initialTargetPosition.z);
        }
        else
        {
            newTarget = initialTargetPosition;
        }
        SetTarget(newTarget);
    }

    /// <summary>
    /// Toggles the flying state of the AI character via keyboard input.
    /// </summary>
    private void ToggleFlightInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            _isFlying = !_isFlying;
            Debug.Log($"<color=yellow>Flight mode toggled: {_isFlying}</color>");
            if (_isFlying)
            {
                // In a real game, you might want to adjust the target's Y position
                // or initiate a special flying animation/transition here.
            }
            else
            {
                // If landing, you might want to force the character to ground level.
                // For simplicity, this example just switches the movement strategy.
            }
        }
    }

    /// <summary>
    /// Draws a line to the current target and a sphere at the target position in the Scene view.
    /// </summary>
    private void DebugDrawTarget()
    {
        if (_hasTarget)
        {
            // Draw a line from the AI to its target.
            Debug.DrawLine(transform.position, _currentTargetPosition, Color.cyan);
            // Draw a small sphere at the target position.
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_currentTargetPosition, 0.2f);
        }
    }

    /// <summary>
    /// Draws Gizmos in the Scene view to visualize the distance thresholds.
    /// </summary>
    void OnDrawGizmos()
    {
        DebugDrawTarget(); // Also draw target in Gizmos for persistence

        // Draw spheres representing the run, walk, and stop distance thresholds.
        // This helps visualize when the AI will switch behaviors.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, runDistanceThreshold); // Outer ring: Run
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, walkDistanceThreshold); // Middle ring: Walk
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, stopDistanceThreshold); // Inner ring: Stop
    }
}
```