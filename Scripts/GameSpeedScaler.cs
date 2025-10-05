// Unity Design Pattern Example: GameSpeedScaler
// This script demonstrates the GameSpeedScaler pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'GameSpeedScaler' design pattern in Unity provides a flexible, centralized way to control the speed of specific game elements, independent of Unity's global `Time.timeScale`. While `Time.timeScale` affects *everything* (physics, animations, particle systems, `Update`/`FixedUpdate` delta times), a `GameSpeedScaler` allows you to apply a custom speed multiplier to only those components that explicitly opt into it.

This is invaluable for scenarios like:
*   **Slow-motion effects:** Only specific gameplay elements (player movement, enemy projectiles) slow down, while UI animations or background particles remain at normal speed.
*   **Game pausing:** Pause only gameplay logic while still allowing UI elements or cinematics to continue.
*   **Speed-up/Fast-forward:** Accelerate game progression without affecting menu responsiveness or physics stability.
*   **Debugging/Testing:** Temporarily slow down complex interactions to observe them more clearly.

---

### **GameSpeedScaler Pattern Implementation**

This example consists of three C# scripts:

1.  **`GameSpeedScaler.cs`**: The core singleton responsible for managing the custom speed scale.
2.  **`ScalableObjectMovement.cs`**: An example component that uses the `GameSpeedScaler` to control its movement speed.
3.  **`GameSpeedController.cs`**: A simple MonoBehaviour to demonstrate how to control the `GameSpeedScaler` at runtime using input.

---

### **1. `GameSpeedScaler.cs` (Core Singleton)**

This script is the heart of the pattern. It's a singleton MonoBehaviour that provides the current custom speed scale and methods to modify it.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines if you want to scale them (though this example focuses on Update-driven logic)

/// <summary>
/// GameSpeedScaler: A singleton MonoBehaviour that provides a custom, independent speed scale for game elements.
///
/// This pattern allows for fine-grained control over game speed for specific components,
/// unlike Time.timeScale which affects everything globally (physics, animations, coroutines, etc.).
///
/// Components that want to be affected by this scaler should query `GameSpeedScaler.Instance.ScaledDeltaTime`
/// instead of `Time.deltaTime` in their Update methods.
/// </summary>
public class GameSpeedScaler : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static GameSpeedScaler _instance;

    /// <summary>
    /// Gets the singleton instance of the GameSpeedScaler.
    /// Ensures there is only one instance in the scene.
    /// </summary>
    public static GameSpeedScaler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameSpeedScaler>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(GameSpeedScaler).Name);
                    _instance = singletonObject.AddComponent<GameSpeedScaler>();
                    Debug.Log($"[GameSpeedScaler] A new instance was created: {singletonObject.name}");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Ensures that only one instance of GameSpeedScaler exists.
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[GameSpeedScaler] Duplicate instance detected, destroying this one.", this);
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // Optionally make it persist across scene loads
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[GameSpeedScaler] Initialized singleton: {gameObject.name}");
        }
    }

    // --- Game Speed Scaling Logic ---

    [Header("Speed Settings")]
    [Tooltip("The current custom speed scale applied to scalable game elements.")]
    [SerializeField]
    private float _currentSpeedScale = 1.0f;

    [Tooltip("Stores the speed scale before pausing, to restore it on unpause.")]
    [SerializeField]
    private float _previousSpeedScale = 1.0f; // Stores the scale before pausing

    /// <summary>
    /// Gets the current custom speed scale factor.
    /// </summary>
    public float CurrentScale => _currentSpeedScale;

    /// <summary>
    /// Provides the delta time multiplied by the current custom speed scale.
    /// Scalable components should use this instead of Time.deltaTime.
    /// </summary>
    public float ScaledDeltaTime => Time.deltaTime * _currentSpeedScale;

    /// <summary>
    /// Sets a new custom speed scale factor.
    /// </summary>
    /// <param name="newScale">The new speed scale. Clamped to a minimum of 0.</param>
    public void SetSpeedScale(float newScale)
    {
        if (newScale < 0f)
        {
            Debug.LogWarning($"[GameSpeedScaler] Attempted to set negative speed scale ({newScale}). Clamping to 0.");
            _currentSpeedScale = 0f;
        }
        else
        {
            _currentSpeedScale = newScale;
        }
        Debug.Log($"[GameSpeedScaler] Speed scale set to: {_currentSpeedScale}");
    }

    /// <summary>
    /// Pauses all scalable game elements by setting the speed scale to 0.
    /// Stores the previous speed scale to restore it later.
    /// </summary>
    public void Pause()
    {
        if (_currentSpeedScale > 0) // Only pause if not already paused
        {
            _previousSpeedScale = _currentSpeedScale;
            SetSpeedScale(0f);
            Debug.Log("[GameSpeedScaler] Game paused. Previous scale: " + _previousSpeedScale);
        }
        else
        {
            Debug.Log("[GameSpeedScaler] Game already paused.");
        }
    }

    /// <summary>
    /// Unpauses all scalable game elements by restoring the speed scale to its pre-pause value.
    /// If no previous scale was stored (e.g., if Pause was never called), it defaults to 1.0.
    /// </summary>
    public void Unpause()
    {
        if (_currentSpeedScale == 0) // Only unpause if currently paused
        {
            // Restore previous scale, or default to 1.0 if previous scale was 0 or not set
            SetSpeedScale(_previousSpeedScale > 0 ? _previousSpeedScale : 1.0f);
            Debug.Log("[GameSpeedScaler] Game unpaused. Restored scale: " + _currentSpeedScale);
        }
        else
        {
            Debug.Log("[GameSpeedScaler] Game not paused.");
        }
    }

    /// <summary>
    /// Toggles the paused state of scalable game elements.
    /// </summary>
    public void TogglePause()
    {
        if (_currentSpeedScale == 0)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    // --- Editor-only Debugging and Context Menu ---
    // These methods make it easy to test the scaler directly from the Unity Editor.
    #if UNITY_EDITOR
    [ContextMenu("Set Speed Scale to 0.1 (Slow-mo)")]
    private void Editor_SetSlowMotion() { SetSpeedScale(0.1f); }

    [ContextMenu("Set Speed Scale to 1.0 (Normal)")]
    private void Editor_SetNormalSpeed() { SetSpeedScale(1.0f); }

    [ContextMenu("Set Speed Scale to 2.0 (Fast-forward)")]
    private void Editor_SetFastForward() { SetSpeedScale(2.0f); }

    [ContextMenu("Pause Game Elements")]
    private void Editor_PauseGame() { Pause(); }

    [ContextMenu("Unpause Game Elements")]
    private void Editor_UnpauseGame() { Unpause(); }

    [ContextMenu("Toggle Pause Game Elements")]
    private void Editor_TogglePauseGame() { TogglePause(); }
    #endif
}

```

### **2. `ScalableObjectMovement.cs` (Example Consumer)**

This script demonstrates how a regular MonoBehaviour component "opts in" to the `GameSpeedScaler` pattern by using `GameSpeedScaler.Instance.ScaledDeltaTime` for its updates.

```csharp
using UnityEngine;

/// <summary>
/// ScalableObjectMovement: An example MonoBehaviour that moves an object
/// and demonstrates how to integrate with the GameSpeedScaler pattern.
///
/// This script explicitly uses `GameSpeedScaler.Instance.ScaledDeltaTime`
/// instead of `Time.deltaTime` to ensure its movement speed is affected
/// by the custom game speed scale.
/// </summary>
public class ScalableObjectMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The base movement speed of the object.")]
    [SerializeField]
    private float _moveSpeed = 5.0f; // Units per second

    [Tooltip("The direction in which the object will move.")]
    [SerializeField]
    private Vector3 _moveDirection = Vector3.right;

    void Update()
    {
        // Check if the GameSpeedScaler instance is available.
        // This check is good practice, especially during initialization or if the scaler might not always be present.
        if (GameSpeedScaler.Instance == null)
        {
            Debug.LogWarning("[ScalableObjectMovement] GameSpeedScaler instance not found. Movement will not be scaled.", this);
            // Fallback to normal Time.deltaTime if the scaler isn't available
            transform.Translate(_moveDirection.normalized * _moveSpeed * Time.deltaTime);
            return;
        }

        // --- Core integration with GameSpeedScaler ---
        // Multiply movement by GameSpeedScaler.Instance.ScaledDeltaTime
        // This ensures the movement is automatically adjusted based on the current custom speed scale.
        transform.Translate(_moveDirection.normalized * _moveSpeed * GameSpeedScaler.Instance.ScaledDeltaTime);
    }
}
```

### **3. `GameSpeedController.cs` (Example Controller)**

This script provides a basic user interface (using `OnGUI`) and input handling to control the `GameSpeedScaler` at runtime, making the demonstration interactive.

```csharp
using UnityEngine;

/// <summary>
/// GameSpeedController: A simple MonoBehaviour to demonstrate runtime control
/// of the GameSpeedScaler using keyboard input and a basic OnGUI display.
///
/// This script shows how an external system (e.g., UI, player input, game state manager)
/// can interact with the GameSpeedScaler to change the custom game speed.
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Header("Control Settings")]
    [Tooltip("The amount by which to adjust the speed scale with each key press.")]
    [SerializeField]
    private float _speedChangeAmount = 0.1f;

    [Tooltip("The maximum allowable speed scale.")]
    [SerializeField]
    private float _maxSpeed = 5.0f;

    [Tooltip("The minimum allowable speed scale (above 0 for non-paused states).")]
    [SerializeField]
    private float _minSpeed = 0.1f;

    [Header("UI Settings")]
    [Tooltip("The font size for the OnGUI display.")]
    [SerializeField]
    private int _fontSize = 24;

    private GUIStyle _textStyle;

    void Start()
    {
        // Ensure the GameSpeedScaler instance exists, creating it if necessary.
        // This is important if you manually place this controller but forget the scaler.
        if (GameSpeedScaler.Instance == null)
        {
            Debug.LogWarning("[GameSpeedController] GameSpeedScaler not found. Attempting to create one.");
            // Accessing the Instance property will automatically create it if it doesn't exist.
            GameSpeedScaler.Instance.SetSpeedScale(1.0f);
        }

        _textStyle = new GUIStyle();
        _textStyle.fontSize = _fontSize;
        _textStyle.normal.textColor = Color.white;
        _textStyle.alignment = TextAnchor.UpperLeft;
    }

    void Update()
    {
        if (GameSpeedScaler.Instance == null)
        {
            return; // Cannot control if scaler is not present
        }

        // --- Keyboard Input Control ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameSpeedScaler.Instance.TogglePause();
        }
        else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            float newScale = Mathf.Min(GameSpeedScaler.Instance.CurrentScale + _speedChangeAmount, _maxSpeed);
            GameSpeedScaler.Instance.SetSpeedScale(newScale);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            // Only allow decreasing if not paused and not at the minimum speed
            if (GameSpeedScaler.Instance.CurrentScale > 0)
            {
                float newScale = Mathf.Max(GameSpeedScaler.Instance.CurrentScale - _speedChangeAmount, _minSpeed);
                GameSpeedScaler.Instance.SetSpeedScale(newScale);
            }
            else
            {
                Debug.Log("[GameSpeedController] Cannot decrease speed while paused. Unpause first.");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0)) // Set to normal speed
        {
            GameSpeedScaler.Instance.SetSpeedScale(1.0f);
        }
    }

    void OnGUI()
    {
        // Display current speed scale
        if (GameSpeedScaler.Instance != null)
        {
            GUI.Label(new Rect(10, 10, 300, 50), $"Game Speed Scale: {GameSpeedScaler.Instance.CurrentScale:F2}", _textStyle);
            string status = GameSpeedScaler.Instance.CurrentScale == 0 ? "PAUSED" : "RUNNING";
            GUI.Label(new Rect(10, 40, 300, 50), $"Status: {status}", _textStyle);

            GUI.Label(new Rect(10, 100, 500, 100),
                "Controls:\n" +
                "  Space: Toggle Pause/Unpause\n" +
                "  '+'/'-': Adjust Speed Scale\n" +
                "  '0': Set Speed Scale to 1.0 (Normal)", _textStyle);
        }
    }
}
```

---

### **How to Use in Unity**

1.  **Create Scripts:** Save each of the three code blocks above into separate C# files named `GameSpeedScaler.cs`, `ScalableObjectMovement.cs`, and `GameSpeedController.cs` in your Unity project's `Assets` folder.
2.  **Setup GameSpeedScaler:**
    *   Create an empty GameObject in your scene (e.g., name it `_GameManagers`).
    *   Add the `GameSpeedScaler.cs` component to this GameObject. Since it's a singleton, it will create itself if not found, but it's good practice to place it explicitly.
3.  **Setup Scalable Objects:**
    *   Create a 3D Cube (or any primitive) in your scene.
    *   Add the `ScalableObjectMovement.cs` component to this Cube.
    *   Adjust the `Move Speed` and `Move Direction` in the Inspector if desired.
    *   Duplicate this Cube a few times to see multiple objects moving.
4.  **Setup Controller:**
    *   Create another empty GameObject (e.g., name it `GameSpeedControl`).
    *   Add the `GameSpeedController.cs` component to this GameObject.
    *   Adjust `Speed Change Amount`, `Max Speed`, `Min Speed`, and `Font Size` in the Inspector if desired.
5.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   You will see the Cubes moving.
    *   Use the keyboard controls (Space, +, -, 0) to change the custom game speed. Observe how only the cubes are affected, while `Time.timeScale` remains at 1.0 (you can verify this by looking at the Unity Editor's `Time` window while playing).

This setup provides a complete, working, and educational example of the GameSpeedScaler design pattern in Unity.