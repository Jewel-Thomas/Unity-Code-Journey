// Unity Design Pattern Example: FakeLagSimulator
// This script demonstrates the FakeLagSimulator pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'FakeLagSimulator' design pattern, while not a classical GoF pattern, is a practical utility pattern often used in game development, especially in Unity, to simulate network latency, processing delays, or input lag. This is invaluable for:

1.  **Testing and Debugging**: Understanding how your game behaves under various network conditions without needing an actual high-latency network.
2.  **User Experience (UX) Research**: Observing how players react to delayed feedback.
3.  **Balancing and Design**: Fine-tuning game mechanics that might be affected by lag.
4.  **Educational Purposes**: Demonstrating the impact of latency on client-side prediction, server authoritative models, etc.

The core idea is to introduce an artificial, configurable delay before an action, event, or data processing truly occurs. Instead of executing an action immediately, it's queued and dispatched only after a specified time has passed.

---

### FakeLagSimulator Unity Example

This example provides a `FakeLagSimulator` class implemented as a Unity `MonoBehaviour` singleton. It allows any other script to easily enqueue actions that will be executed after a simulated delay.

**How it works:**
1.  **Singleton Access**: It uses the standard Unity singleton pattern for easy global access.
2.  **Configurable Delay**: You can set minimum and maximum delay times in the Inspector, allowing for randomizing lag.
3.  **Action Queue**: It maintains an internal queue of `DelayedAction` structs. Each `DelayedAction` holds the actual `System.Action` to be performed and the `Time.time` at which it should be dispatched.
4.  **`Update` Loop Processing**: In its `Update` method, the simulator continuously checks if any queued actions have passed their `DispatchTime`. If so, it dequeues and executes them.
5.  **Public API**: The `SimulateLaggedAction` method is the primary way other scripts interact with the simulator, passing in an `Action` to be delayed.

---

### `FakeLagSimulator.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// The FakeLagSimulator design pattern is used to intentionally introduce an artificial delay
/// before executing certain actions, simulating network latency, processing lag, or input delay.
///
/// This is highly useful for:
/// 1.  Testing how a game behaves under laggy conditions.
/// 2.  Debugging network code or client-side prediction.
/// 3.  Analyzing user experience with delayed feedback.
/// 4.  Training players to handle lag.
/// 5.  Creating specific game mechanics (e.g., "bullet time" with delayed effects).
///
/// This implementation provides a singleton MonoBehaviour that other scripts can
/// easily use to wrap actions that should be affected by simulated lag.
/// </summary>
public class FakeLagSimulator : MonoBehaviour
{
    // --- Singleton Setup ---
    /// <summary>
    /// Static instance for global access to the FakeLagSimulator.
    /// This follows the Singleton pattern, ensuring only one instance exists.
    /// </summary>
    public static FakeLagSimulator Instance { get; private set; }

    [Header("Simulation Settings")]
    [Tooltip("Enable or disable the lag simulation.")]
    public bool isEnabled = true;

    [Range(0f, 5f)]
    [Tooltip("The minimum amount of simulated delay in seconds.")]
    public float minDelaySeconds = 0.1f;

    [Range(0f, 5f)]
    [Tooltip("The maximum amount of simulated delay in seconds.")]
    public float maxDelaySeconds = 0.5f;

    /// <summary>
    /// Internal struct to hold an action and its scheduled dispatch time.
    /// </summary>
    private struct DelayedAction
    {
        public Action ActionToExecute; // The actual method/action to be called.
        public float DispatchTime;    // The Time.time (Unity's game time) when this action should be executed.
    }

    /// <summary>
    /// A queue to store actions that are waiting to be dispatched after their delay.
    /// Using a Queue ensures actions are processed in the order they were scheduled.
    /// </summary>
    private Queue<DelayedAction> _delayedActions = new Queue<DelayedAction>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Sets up the singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("FakeLagSimulator: Multiple instances found! Destroying duplicate. " +
                             "Ensure only one FakeLagSimulator exists in your scene.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make it persist across scene loads if it's a core system.
            // DontDestroyOnLoad(gameObject); 
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// This is where the queued actions are checked and dispatched if their delay has passed.
    /// </summary>
    private void Update()
    {
        if (!isEnabled || _delayedActions.Count == 0)
        {
            return; // No actions to process or simulation is disabled.
        }

        // Process all actions that are due to be executed.
        // We use a while loop with Peek() and Dequeue() to safely modify the queue
        // while iterating over eligible items.
        while (_delayedActions.Count > 0 && _delayedActions.Peek().DispatchTime <= Time.time)
        {
            DelayedAction actionToExecute = _delayedActions.Dequeue();
            
            // Execute the action. Use ?.Invoke() for null safety in case the action was cleared.
            actionToExecute.ActionToExecute?.Invoke();
        }
    }

    /// <summary>
    /// Schedules an action to be executed after a simulated random delay.
    /// The delay will be a random value between minDelaySeconds and maxDelaySeconds.
    /// This is the primary method other scripts will use to simulate lag.
    /// </summary>
    /// <param name="action">The Action (a parameterless method) to be executed after the delay.</param>
    public void SimulateLaggedAction(Action action)
    {
        if (!isEnabled)
        {
            action?.Invoke(); // If simulation is disabled, execute immediately.
            return;
        }

        float delay = UnityEngine.Random.Range(minDelaySeconds, maxDelaySeconds);
        ScheduleAction(action, delay);
    }

    /// <summary>
    /// Schedules an action to be executed after a custom, specified delay.
    /// </summary>
    /// <param name="action">The Action to be executed after the delay.</param>
    /// <param name="customDelaySeconds">The specific delay in seconds for this action.</param>
    public void SimulateLaggedAction(Action action, float customDelaySeconds)
    {
        if (!isEnabled)
        {
            action?.Invoke(); // If simulation is disabled, execute immediately.
            return;
        }

        ScheduleAction(action, customDelaySeconds);
    }

    /// <summary>
    /// Internal helper method to create and enqueue a DelayedAction.
    /// </summary>
    /// <param name="action">The action to enqueue.</param>
    /// <param name="delay">The delay before execution.</param>
    private void ScheduleAction(Action action, float delay)
    {
        if (action == null)
        {
            Debug.LogWarning("FakeLagSimulator: Attempted to schedule a null action.");
            return;
        }

        DelayedAction newDelayedAction = new DelayedAction
        {
            ActionToExecute = action,
            DispatchTime = Time.time + delay // Calculate the future time for dispatch.
        };

        _delayedActions.Enqueue(newDelayedAction);
    }

    /// <summary>
    /// Clears all pending actions from the queue. Useful when resetting or disabling the simulator.
    /// </summary>
    public void ClearAllPendingActions()
    {
        _delayedActions.Clear();
        Debug.Log("FakeLagSimulator: All pending actions cleared.");
    }

    /// <summary>
    /// Called when the GameObject is destroyed.
    /// Cleans up the singleton instance if this was the active instance.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

---

### Example Usage in Another Script (`PlayerControllerWithLag.cs`)

To demonstrate how to use `FakeLagSimulator`, let's imagine a simple player controller script where movement or firing actions can be delayed.

1.  **Create a new C# script** in Unity named `PlayerControllerWithLag.cs`.
2.  **Paste the following code** into it.
3.  **Attach this script** to a `GameObject` in your scene (e.g., your player character).
4.  **Create an Empty GameObject** in your scene, name it `FakeLagSimulator`, and **attach the `FakeLagSimulator.cs` script** to it.
5.  **Run the scene.** Observe the Debug.Log messages and how player actions might feel delayed. Try enabling/disabling the `FakeLagSimulator` component or adjusting its `minDelaySeconds`/`maxDelaySeconds` in the Inspector.

```csharp
using UnityEngine;

/// <summary>
/// This script demonstrates how to integrate the FakeLagSimulator into a typical
/// Unity player controller to simulate input lag or network latency for actions.
/// </summary>
public class PlayerControllerWithLag : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dashForce = 10f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("PlayerControllerWithLag requires a Rigidbody component on its GameObject.");
            enabled = false; // Disable script if no Rigidbody
        }

        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerControllerWithLag: bulletPrefab is not assigned. Fire action will not instantiate anything.");
        }
        if (firePoint == null)
        {
            // Default to the player's position if no firePoint is specified
            firePoint = transform; 
            Debug.LogWarning("PlayerControllerWithLag: firePoint is not assigned. Using player's transform for firing.");
        }
    }

    void Update()
    {
        HandleMovementInput();
        HandleActionInput();
    }

    private void HandleMovementInput()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // --- Demonstrate movement with simulated lag ---
        // Instead of directly applying movement, we queue it.
        // This simulates a scenario where movement commands are sent to a server
        // and processed after network latency.
        if (movement.magnitude > 0.01f) // Only simulate if there's actual input
        {
            // It's crucial to capture the 'movement' vector *at the time of input*
            // Otherwise, if 'movement' is directly accessed inside the lambda,
            // it might refer to the value from a later frame.
            Vector3 laggedMovement = movement; 

            FakeLagSimulator.Instance.SimulateLaggedAction(() =>
            {
                // This code executes after the simulated lag.
                // For direct movement, you might want client-side prediction
                // and then correct with the lagged authoritative movement.
                // For this example, we're simply applying it late.
                if (_rb != null)
                {
                    _rb.MovePosition(transform.position + laggedMovement * moveSpeed * Time.deltaTime);
                    Debug.Log($"[Lagged Movement] Player moved to: {transform.position} after lag. (Input: {laggedMovement})");
                }
            }, 0.2f); // Using a fixed 0.2s delay for movement for consistency
        }
    }

    private void HandleActionInput()
    {
        // --- Simulate Firing an ability with random lag ---
        if (Input.GetButtonDown("Fire1")) // Left Mouse Click
        {
            Debug.Log($"[Input Detected] Fire button pressed at {Time.time}");

            // Wrap the firing logic in a lagged action.
            // This simulates server processing delay or weapon cooldowns with lag.
            FakeLagSimulator.Instance.SimulateLaggedAction(() =>
            {
                PerformFireAction();
            });
        }

        // --- Simulate a Dash/Ability with custom lag ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[Input Detected] Spacebar pressed for dash at {Time.time}");

            // The dash itself will happen after a specific, custom delay.
            FakeLagSimulator.Instance.SimulateLaggedAction(() =>
            {
                PerformDashAction();
            }, 0.8f); // This dash will always have an 0.8 second simulated lag
        }
    }

    private void PerformFireAction()
    {
        // This code executes after the simulated lag for the "Fire" action.
        Debug.Log($"[Lagged Action] Firing weapon at {Time.time} after simulated lag!");
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            Debug.LogWarning("Cannot fire: bulletPrefab or firePoint not set.");
        }
    }

    private void PerformDashAction()
    {
        // This code executes after the simulated lag for the "Dash" action.
        Debug.Log($"[Lagged Action] Player dashed forward at {Time.time} after 0.8s lag!");
        if (_rb != null)
        {
            // Apply a force or move the character
            _rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
        }
    }

    // Optional: Add some visual feedback for the player.
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), "WASD: Move (0.2s Lag)");
        GUI.Label(new Rect(10, 40, 300, 30), "Left Click: Fire (Random Lag)");
        GUI.Label(new Rect(10, 70, 300, 30), "Space: Dash (0.8s Lag)");
        GUI.Label(new Rect(10, 100, 300, 30), $"FakeLagSimulator {(FakeLagSimulator.Instance != null && FakeLagSimulator.Instance.isEnabled ? "ENABLED" : "DISABLED")}");
    }
}
```