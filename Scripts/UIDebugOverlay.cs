// Unity Design Pattern Example: UIDebugOverlay
// This script demonstrates the UIDebugOverlay pattern in Unity
// Generated automatically - ready to use in your Unity project

The `UIDebugOverlay` design pattern provides a centralized, easy-to-use system for displaying real-time debug information directly on the game screen. It allows various parts of your application to register and update debug data without needing direct access to UI elements or complex logging systems. This is particularly useful for quickly inspecting game states, performance metrics, and custom variable values during development.

### Key Benefits of the UIDebugOverlay Pattern:
1.  **Centralized Control:** All debug information is managed by a single system.
2.  **Decoupling:** Game logic can contribute debug info without knowing how or where it's displayed, promoting cleaner code.
3.  **Real-time Feedback:** Instantly see changes in values or states directly in-game.
4.  **Toggleable Visibility:** Easily show/hide the overlay with a hotkey, keeping the game screen clean when not debugging.
5.  **Easy Integration:** Other scripts simply call a public method to add or update debug entries.
6.  **Performance Insights:** Can include built-in metrics like FPS.

---

### Complete C# Unity Example: `UIDebugOverlayManager`

This script creates a robust and customizable debug overlay that can be dropped into any Unity project. It automatically sets up its own Canvas and Text element, provides an easy API for other scripts to add/update debug information, and includes a configurable toggle key.

**How to Use:**
1.  Create an empty GameObject in your scene (e.g., named "DebugOverlay").
2.  Attach this `UIDebugOverlayManager.cs` script to that GameObject.
3.  Ensure the "Unity UI" package is installed in your project (Window -> Package Manager -> Unity Registry -> UI).
4.  Run your game. Press the configured `toggleKey` (default `F12`) to show/hide the overlay.
5.  See "Example Usage" in the comments below for how other scripts can interact with it.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Unity UI Text component
using System.Collections.Generic; // Required for Dictionary
using System.Text; // Required for StringBuilder for efficient string concatenation

/// <summary>
/// Implements the UIDebugOverlay design pattern in Unity.
/// Provides a centralized, toggleable overlay for displaying real-time debug information.
/// </summary>
public class UIDebugOverlayManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Ensures only one instance of the Debug Overlay Manager exists and provides easy global access.
    public static UIDebugOverlayManager Instance { get; private set; }

    // --- Inspector Settings ---
    [Header("Overlay Settings")]
    [Tooltip("Key to toggle the visibility of the debug overlay.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;

    [Tooltip("Should the debug overlay be enabled by default when the game starts?")]
    [SerializeField] private bool startEnabled = false;

    [Tooltip("Font size for the debug text.")]
    [SerializeField] private int fontSize = 16;

    [Tooltip("Color of the debug text.")]
    [SerializeField] private Color textColor = Color.white;

    [Tooltip("Padding percentage from the screen edges for the debug text.")]
    [SerializeField] [Range(0.01f, 0.1f)] private float padding = 0.02f; // 2% padding

    // --- Private Internal References ---
    private GameObject _overlayRoot; // The root GameObject containing the Canvas
    private Text _debugText;         // The UI Text component that displays all debug info
    
    // Stores debug information using a dictionary: Key = debug item name, Value = debug item string.
    private Dictionary<string, string> _debugItems = new Dictionary<string, string>();

    // For calculating FPS
    private float _deltaTime = 0.0f; 

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
        Instance = this; // Set this instance as the singleton
        
        // Make the manager persistent across scene loads, typical for debug tools.
        DontDestroyOnLoad(gameObject); 

        InitializeOverlay(); // Setup the UI elements programmatically
    }

    private void Update()
    {
        // Handle toggling the overlay visibility
        if (Input.GetKeyDown(toggleKey))
        {
            _overlayRoot.SetActive(!_overlayRoot.activeSelf);
        }

        // Only update the text content if the overlay is currently visible.
        if (_overlayRoot.activeSelf)
        {
            UpdateDebugTextContent();
        }
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Programmatically creates the Canvas and Text UI elements for the debug overlay.
    /// This makes the component truly "drop-in" without manual UI setup.
    /// </summary>
    private void InitializeOverlay()
    {
        // 1. Create the Canvas GameObject
        _overlayRoot = new GameObject("DebugOverlayCanvas");
        Canvas canvas = _overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Render on top of everything
        _overlayRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _overlayRoot.AddComponent<GraphicRaycaster>(); // Required for canvas interactions, though not used here.
        _overlayRoot.transform.SetParent(this.transform); // Make it a child of the manager GameObject

        // 2. Create the Text GameObject
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(_overlayRoot.transform);
        _debugText = textGO.AddComponent<Text>();

        // 3. Configure RectTransform for padded top-left text
        RectTransform rt = _debugText.rectTransform;
        rt.anchorMin = new Vector2(0, 1); // Anchor to top-left of parent
        rt.anchorMax = new Vector2(0, 1); // Anchor to top-left of parent
        rt.pivot = new Vector2(0, 1);     // Pivot at top-left of the text itself

        // Calculate padded position and size based on screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float horizontalPaddingPx = padding * screenWidth;
        float verticalPaddingPx = padding * screenHeight;

        rt.anchoredPosition = new Vector2(horizontalPaddingPx, -verticalPaddingPx); // Offset from top-left for padding

        // Set text area size, making it span most of the screen to accommodate many lines
        float textWidth = screenWidth * (1f - 2f * padding);
        float textHeight = screenHeight * (1f - 2f * padding); 
        rt.sizeDelta = new Vector2(textWidth, textHeight);

        // 4. Configure Text component properties
        // Attempt to load Arial, fallback to engine default if not found (e.g., on some platforms)
        _debugText.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        if (_debugText.font == null) 
        {
            Debug.LogWarning("Arial font not found or could not be loaded. Falling back to default built-in font.");
            _debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Common fallback
        }
        _debugText.fontSize = fontSize;
        _debugText.color = textColor;
        _debugText.alignment = TextAnchor.UpperLeft; // Align text to the upper-left corner
        _debugText.horizontalOverflow = HorizontalWrapMode.Wrap; // Allow text to wrap to the next line
        _debugText.verticalOverflow = VerticalWrapMode.Overflow; // Allow text to extend beyond bounds (for debug, usually fine)

        // Ensure the overlay starts in the desired active state
        _overlayRoot.SetActive(startEnabled);
    }

    /// <summary>
    /// Gathers all registered debug items and built-in metrics (like FPS)
    /// and updates the UI Text component.
    /// </summary>
    private void UpdateDebugTextContent()
    {
        // 1. Calculate and add FPS
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f; // Smoothed delta time
        float fps = 1.0f / _deltaTime;
        
        // Using StringBuilder for efficient string concatenation, especially with many debug items.
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"FPS: {Mathf.RoundToInt(fps)}");
        sb.AppendLine("--------------------");

        // 2. Add all custom registered debug items
        foreach (var item in _debugItems)
        {
            sb.AppendLine($"{item.Key}: {item.Value}");
        }

        // 3. Update the UI Text component
        _debugText.text = sb.ToString();
    }

    // --- Public API for Other Scripts ---

    /// <summary>
    /// Adds a new debug item or updates an existing one.
    /// </summary>
    /// <param name="key">A unique identifier for the debug item (e.g., "PlayerPosition").</param>
    /// <param name="value">The string value to display for this debug item.</param>
    public void AddOrUpdateDebugItem(string key, string value)
    {
        if (_debugItems.ContainsKey(key))
        {
            _debugItems[key] = value;
        }
        else
        {
            _debugItems.Add(key, value);
        }
    }

    /// <summary>
    /// Removes a debug item from the overlay.
    /// </summary>
    /// <param name="key">The unique identifier of the debug item to remove.</param>
    public void RemoveDebugItem(string key)
    {
        if (_debugItems.ContainsKey(key))
        {
            _debugItems.Remove(key);
        }
    }

    /// <summary>
    /// Sets the visibility of the debug overlay.
    /// </summary>
    /// <param name="visible">True to show the overlay, false to hide it.</param>
    public void SetOverlayVisibility(bool visible)
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(visible);
        }
    }
}

/*
// --- Example Usage in another script (e.g., PlayerController.cs, GameManager.cs) ---

// Attach this example script to any GameObject in your scene.
// Make sure a UIDebugOverlayManager is also present in the scene.

using UnityEngine;

public class MyDebugReporter : MonoBehaviour
{
    private float _health = 100f;
    private Vector3 _playerPosition;
    private int _enemyCount = 5;
    private bool _isGamePaused = false;

    // Optional: a timer to update less frequently for less critical info
    private float _updateTimer = 0f;
    private float _updateInterval = 0.25f; // Update every 0.25 seconds

    void Start()
    {
        // Ensure the DebugOverlayManager exists in the scene.
        if (UIDebugOverlayManager.Instance == null)
        {
            Debug.LogWarning("UIDebugOverlayManager not found in scene. Debug overlay will not function.");
            enabled = false; // Disable this script if no manager.
            return;
        }

        // Register initial debug items.
        // The first parameter is a unique key, the second is the value to display.
        // You can update these values in Update() or any other method.
        UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Player Health", _health.ToString());
        UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Player Position", _playerPosition.ToString());
        UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Enemy Count", _enemyCount.ToString());
        UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Game Paused", _isGamePaused.ToString());
        UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("My Custom Message", "This is a test message.");
    }

    void Update()
    {
        // Simulate changes to game state
        _health = Mathf.Max(0, _health - Time.deltaTime); // Health slowly decreases
        _playerPosition = transform.position; // Get current GameObject's position

        if (Input.GetKeyDown(KeyCode.P))
        {
            _isGamePaused = !_isGamePaused;
            Debug.Log($"Game Paused: {_isGamePaused}");
        }

        // Update debug items.
        // It's efficient to only update items that have actually changed or
        // update less critical items on a timer.

        _updateTimer += Time.deltaTime;
        if (_updateTimer >= _updateInterval)
        {
            _updateTimer = 0f;

            // Always update frequently changing values like position or health
            UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Player Health", Mathf.RoundToInt(_health).ToString());
            UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Player Position", $"X:{_playerPosition.x:F2} Y:{_playerPosition.y:F2} Z:{_playerPosition.z:F2}");
            
            // Update less frequently changing values or state flags
            UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Game Paused", _isGamePaused.ToString());

            // Example of dynamic update: If enemy count changes, update it here.
            // For this example, let's just decrement it for demonstration purposes.
            if (_enemyCount > 0 && Random.Range(0, 100) < 1) // Small chance to decrement
            {
                _enemyCount--;
                UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Enemy Count", _enemyCount.ToString());
            }
        }

        // Example: Remove an item when it's no longer relevant
        if (_health <= 0 && UIDebugOverlayManager.Instance != null)
        {
            UIDebugOverlayManager.Instance.RemoveDebugItem("Player Health");
            UIDebugOverlayManager.Instance.AddOrUpdateDebugItem("Player Status", "DEAD!");
        }
    }

    void OnDestroy()
    {
        // Clean up: remove all debug items registered by this script when it's destroyed or scene changes.
        // This is good practice to prevent stale debug info.
        if (UIDebugOverlayManager.Instance != null)
        {
            UIDebugOverlayManager.Instance.RemoveDebugItem("Player Health");
            UIDebugOverlayManager.Instance.RemoveDebugItem("Player Position");
            UIDebugOverlayManager.Instance.RemoveDebugItem("Enemy Count");
            UIDebugOverlayManager.Instance.RemoveDebugItem("Game Paused");
            UIDebugOverlayManager.Instance.RemoveDebugItem("My Custom Message");
            UIDebugOverlayManager.Instance.RemoveDebugItem("Player Status"); // In case it was added
        }
    }
}
*/
```