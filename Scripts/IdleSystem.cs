// Unity Design Pattern Example: IdleSystem
// This script demonstrates the IdleSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'IdleSystem' design pattern is used to detect periods of user inactivity within an application and trigger specific actions when the application transitions into or out of an "idle" state. This can be incredibly useful in games and interactive applications for things like:

*   **Resource Management:** Pausing computationally intensive background tasks.
*   **UI Behavior:** Dimming the screen, fading out UI elements, or showing "screensaver" content.
*   **Game State:** Automatically saving the game, or logging out a user after extended inactivity.
*   **User Experience:** Providing visual cues or prompts to re-engage the user.

This example provides a robust, commented `IdleSystem` script and demonstrates how other scripts can subscribe to its events to react to idle/active state changes.

---

### `IdleSystem.cs`

This is the core script. Create a new C# script named `IdleSystem.cs` and paste the following code into it.

```csharp
using UnityEngine;
using System; // Required for Action delegate

/// <summary>
/// Implements the 'IdleSystem' design pattern in Unity.
/// This system tracks user input activity (keyboard, mouse) and raises events
/// when the application transitions between 'active' and 'idle' states.
/// </summary>
/// <remarks>
/// Other scripts can subscribe to `OnBecameIdle` and `OnBecameActive` static events
/// to react to these state changes.
/// </remarks>
[DisallowMultipleComponent] // Ensures only one IdleSystem can exist on a GameObject
public class IdleSystem : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("The time in seconds after which the system is considered 'idle' if no activity is detected.")]
    [SerializeField]
    private float idleTimeoutSeconds = 30f; // Default idle timeout of 30 seconds

    // --- State Variables ---
    private float _timeSinceLastActivity; // Tracks the elapsed time since the last detected user activity
    private bool _isIdle;                 // The current idle state of the system (true if idle, false if active)
    private Vector3 _lastMousePosition;   // Stores the mouse position from the previous frame to detect movement
    private float _lastMouseScrollY;      // Stores the mouse scroll delta Y from the previous frame to detect scrolling

    // --- Events ---
    // These static events provide a way for other parts of the application to
    // be notified when the system's idle state changes. They are static so
    // any script can subscribe without needing a direct reference to the IdleSystem instance.
    public static event Action OnBecameIdle;     // Fired when the system transitions from active to idle
    public static event Action OnBecameActive;   // Fired when the system transitions from idle to active

    // --- Singleton-like Access ---
    // Provides a convenient, globally accessible reference to the single IdleSystem instance.
    // This makes it easy for other scripts to check the current state or manually reset the timer.
    public static IdleSystem Instance { get; private set; }

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Enforce a singleton-like pattern.
        // If an instance already exists and it's not this one, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple IdleSystem instances found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this; // Set this instance as the singleton reference

        // Initialize state variables
        _timeSinceLastActivity = 0f;
        _isIdle = false;
        _lastMousePosition = Input.mousePosition;       // Capture initial mouse position
        _lastMouseScrollY = Input.mouseScrollDelta.y;   // Capture initial scroll state
    }

    private void Update()
    {
        // 1. Detect User Activity
        bool wasActiveThisFrame = DetectActivity();

        // 2. Update Idle Timer and State based on activity
        if (wasActiveThisFrame)
        {
            // If activity was detected, reset the idle timer
            _timeSinceLastActivity = 0f;

            // If the system was previously idle, transition to active and invoke the event
            if (_isIdle)
            {
                _isIdle = false;
                Debug.Log("IdleSystem: Became Active!");
                // The '?' is the null-conditional operator, ensuring the event is only invoked if there are subscribers.
                OnBecameActive?.Invoke(); 
            }
        }
        else
        {
            // No activity detected, increment the idle timer
            _timeSinceLastActivity += Time.deltaTime;

            // If the timer exceeds the timeout and the system is not already idle,
            // transition to idle and invoke the event.
            if (!_isIdle && _timeSinceLastActivity >= idleTimeoutSeconds)
            {
                _isIdle = true;
                Debug.Log("IdleSystem: Became Idle!");
                OnBecameIdle?.Invoke();
            }
        }

        // 3. Update last known input states for the next frame's detection
        _lastMousePosition = Input.mousePosition;
        _lastMouseScrollY = Input.mouseScrollDelta.y;
    }

    private void OnDestroy()
    {
        // Important for proper cleanup: if this instance is destroyed, clear the static reference.
        // This prevents other scripts from holding onto a reference to a destroyed object.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Helper Methods ---

    /// <summary>
    /// Checks for various types of user input activity during the current frame.
    /// </summary>
    /// <returns>True if any user activity (keyboard, mouse movement, mouse click, mouse scroll) was detected, false otherwise.</returns>
    private bool DetectActivity()
    {
        // Keyboard input: Check if any key was pressed down this frame
        if (Input.anyKeyDown)
        {
            return true;
        }

        // Mouse movement: Check if the mouse position has changed since the last frame
        if (Input.mousePosition != _lastMousePosition)
        {
            return true;
        }

        // Mouse clicks: Check if any mouse button was pressed down this frame
        // (Input.anyKeyDown usually covers this, but an explicit check can be more robust)
        for (int i = 0; i < 3; i++) // Check for left (0), right (1), and middle (2) mouse buttons
        {
            if (Input.GetMouseButtonDown(i))
            {
                return true;
            }
        }
        
        // Mouse scroll: Check if the scroll wheel was used
        if (Input.mouseScrollDelta.y != 0f)
        {
            return true;
        }

        // No activity detected this frame
        return false;
    }

    /// <summary>
    /// Manually resets the idle timer and forces the system into an 'active' state.
    /// This is useful for game events (e.g., a cutscene starting, a new level loading)
    /// that should prevent the system from becoming idle, even without direct user input.
    /// </summary>
    public void ResetIdleTimer()
    {
        _timeSinceLastActivity = 0f;
        if (_isIdle)
        {
            _isIdle = false;
            Debug.Log("IdleSystem: Timer manually reset, became Active!");
            OnBecameActive?.Invoke();
        }
    }

    /// <summary>
    /// Public property to get the current idle state of the system.
    /// </summary>
    public bool IsIdle => _isIdle;

    /// <summary>
    /// Public property to get the time elapsed since the last detected user activity.
    /// </summary>
    public float TimeSinceLastActivity => _timeSinceLastActivity;
}

```

---

### `IdleReactionExample.cs` (Example Usage)

This script demonstrates how another component in your game would subscribe to the `IdleSystem`'s events and react to state changes. Create a new C# script named `IdleReactionExample.cs` and paste the following code.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Text component

/// <summary>
/// An example script demonstrating how to subscribe to the IdleSystem's events
/// and react to the application becoming idle or active.
/// </summary>
public class IdleReactionExample : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Reference to a UI Text element to display the current idle status.")]
    [SerializeField] private Text statusText;
    [Tooltip("Reference to a UI Panel GameObject to act as a 'screensaver' when idle.")]
    [SerializeField] private GameObject screensaverPanel;

    [Header("Status Text Colors")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color idleColor = Color.red;

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// This is where we subscribe to the IdleSystem's static events.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to the events. When IdleSystem invokes these, our methods will be called.
        IdleSystem.OnBecameIdle += HandleBecameIdle;
        IdleSystem.OnBecameActive += HandleBecameActive;

        // Immediately check the current state of the IdleSystem to set the initial UI.
        // This handles cases where IdleSystem might have already initialized before this script.
        if (IdleSystem.Instance != null)
        {
            if (IdleSystem.Instance.IsIdle)
            {
                HandleBecameIdle();
            }
            else
            {
                HandleBecameActive();
            }
        }
        else
        {
            // Log a warning if the IdleSystem is not found in the scene.
            Debug.LogWarning("IdleSystem instance not found. Make sure an IdleSystem GameObject is in the scene.", this);
            if (statusText != null)
            {
                statusText.text = "IdleSystem NOT FOUND!";
                statusText.color = Color.gray;
            }
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// It's crucial to unsubscribe from events here to prevent memory leaks and
    /// ensure that this object doesn't try to react after it's been destroyed or disabled.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from the events.
        IdleSystem.OnBecameIdle -= HandleBecameIdle;
        IdleSystem.OnBecameActive -= HandleBecameActive;
    }

    /// <summary>
    /// This method is called when the IdleSystem detects the application has become idle.
    /// </summary>
    private void HandleBecameIdle()
    {
        if (statusText != null)
        {
            statusText.text = "STATUS: IDLE";
            statusText.color = idleColor;
        }
        // Example reaction: Activate a "screensaver" panel or dim other UI elements.
        if (screensaverPanel != null)
        {
            screensaverPanel.SetActive(true);
        }
        Debug.Log("IdleReactionExample: System is now IDLE! (Reacting)");
    }

    /// <summary>
    /// This method is called when the IdleSystem detects the application has become active again.
    /// </summary>
    private void HandleBecameActive()
    {
        if (statusText != null)
        {
            statusText.text = "STATUS: ACTIVE";
            statusText.color = activeColor;
        }
        // Example reaction: Deactivate the "screensaver" and restore normal UI.
        if (screensaverPanel != null)
        {
            screensaverPanel.SetActive(false);
        }
        Debug.Log("IdleReactionExample: System is now ACTIVE! (Reacting)");
    }

    /// <summary>
    /// Optional: Example of manually triggering activity from another script.
    /// Pressing 'Space' will reset the idle timer regardless of other input.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IdleSystem.Instance != null)
            {
                IdleSystem.Instance.ResetIdleTimer(); // Manually declare activity
                Debug.Log("IdleReactionExample: Spacebar pressed, manually resetting idle timer.");
            }
        }
    }
}
```

---

### Setup in Unity Editor:

1.  **Create IdleSystem GameObject:**
    *   In your Unity scene, create an empty GameObject (Right-click in Hierarchy -> Create Empty).
    *   Rename it to `GameManager` or `IdleSystemManager`.
    *   Attach the `IdleSystem.cs` script to this new GameObject.
    *   In the Inspector for the `IdleSystem` component, you can adjust the `Idle Timeout Seconds` (e.g., set it to 5 seconds for easier testing).

2.  **Create UI Elements for Reaction (Optional, but Recommended for Visualization):**
    *   Create a UI Text element: Right-click in Hierarchy -> UI -> Text (Legacy) or Text - TextMeshPro (if you've imported TMP essentials). Name it `StatusText`. Position it clearly on your screen.
    *   Create a UI Panel element (optional "screensaver"): Right-click in Hierarchy -> UI -> Panel. Name it `ScreensaverPanel`. Change its color and alpha to make it noticeable. Ensure it covers most of the screen. Initially, you can leave it active or inactive, the `IdleReactionExample` script will handle its visibility.

3.  **Create IdleReactionExample GameObject:**
    *   Create another empty GameObject (e.g., `IdleReactor`).
    *   Attach the `IdleReactionExample.cs` script to this GameObject.
    *   In the Inspector for `IdleReactionExample`:
        *   Drag your `StatusText` GameObject from the Hierarchy into the `Status Text` field.
        *   Drag your `ScreensaverPanel` GameObject from the Hierarchy into the `Screensaver Panel` field.
        *   You can adjust `Active Color` and `Idle Color` if desired.

4.  **Run the Scene:**
    *   Play your Unity scene.
    *   Observe the `StatusText` and the Console.
    *   Initially, it should show "STATUS: ACTIVE".
    *   Stop interacting with your mouse or keyboard for the `Idle Timeout Seconds` you set.
    *   You should see the `StatusText` change to "STATUS: IDLE", its color change, and the `ScreensaverPanel` become active.
    *   Move your mouse, click, or press a key, and the system should immediately become "ACTIVE" again.
    *   Pressing the `Space` key will also manually trigger an active state via `IdleSystem.Instance.ResetIdleTimer()`.

This setup provides a complete, practical, and easy-to-understand example of the IdleSystem design pattern in Unity.