// Unity Design Pattern Example: UIModularWindowSystem
// This script demonstrates the UIModularWindowSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The UIModularWindowSystem is a design pattern used in Unity (and other UI frameworks) to manage user interface windows in a flexible, scalable, and maintainable way. It promotes decoupling between the window management logic and the specific implementation of individual windows.

**Core Principles:**

1.  **Centralized Manager:** A single manager component is responsible for opening, closing, and keeping track of all UI windows. It acts as the "orchestrator."
2.  **Window Interface/Base Class:** All individual windows adhere to a common interface or inherit from a base class. This ensures the manager can interact with any window polymorphically, without knowing its specific type.
3.  **Modular Windows:** Each window is a self-contained module, typically a prefab, with its own logic, UI elements, and data handling. They don't directly know or interact with other windows or the manager, promoting loose coupling.
4.  **Data Agnostic:** The manager doesn't care about the specific data each window displays. It simply passes an `object` (or a generic type) to the window, and the window casts and interprets it.
5.  **Prefab-Based Instantiation:** Windows are typically prefabs instantiated by the manager when needed and destroyed (or pooled) when closed.

---

### **UIModularWindowSystem Example in Unity**

This example demonstrates a complete UIModularWindowSystem with:
*   `IWindow` interface: Defines the contract for all windows.
*   `BaseWindow`: An abstract MonoBehaviour implementing `IWindow` and providing common functionality.
*   `WindowManager`: The central manager responsible for registering, opening, and closing windows.
*   `ExampleInfoWindow` and `ExampleSettingsWindow`: Concrete implementations of windows.
*   `WindowOpener`: A simple script to trigger window actions for demonstration.

**Setup in Unity (Step-by-Step):**

1.  **Create a New Unity Project** (or use an existing one).
2.  **Import TextMeshPro:** Go to `Window > TextMeshPro > Import TMP Essential Resources`.
3.  **Create UI Canvas:**
    *   Right-click in the Hierarchy `UI > Canvas`. Rename it to `MainCanvas`.
    *   Set its `Render Mode` to `Screen Space - Overlay`.
    *   Set `UI Scale Mode` to `Scale With Screen Size` and `Reference Resolution` to `1920x1080` (or your preferred resolution).
4.  **Create WindowManager GameObject:**
    *   Create an empty GameObject as a child of `MainCanvas`. Rename it to `_WindowManager`.
    *   Attach the `WindowManager.cs` script (which you'll create below) to it.
    *   In the Inspector, assign `MainCanvas` to the `Window Parent` field of the `WindowManager` script.
5.  **Create Window Prefabs:**
    *   **Info Window:**
        *   Right-click `_WindowManager` in Hierarchy `UI > Panel`. Rename it `InfoWindowPanel`.
        *   Add a `TextMeshProUGUI` child to `InfoWindowPanel` for `Title`.
        *   Add another `TextMeshProUGUI` child for `Content`.
        *   Adjust their positions and text sizes as needed.
        *   Attach the `ExampleInfoWindow.cs` script (you'll create it below) to `InfoWindowPanel`.
        *   In the `ExampleInfoWindow` script component, drag the `InfoWindowPanel` itself into the `Content Panel` field.
        *   Drag the `Title` TextMeshProUGUI to the `Title Text` field.
        *   Drag the `Content` TextMeshProUGUI to the `Content Text` field.
        *   **Crucially:** Drag the `InfoWindowPanel` GameObject from the Hierarchy into your Project window (e.g., into an `Assets/Prefabs` folder) to create a prefab.
        *   **Delete `InfoWindowPanel` from the Hierarchy.**
    *   **Settings Window:**
        *   Repeat the panel creation process for `SettingsWindowPanel`.
        *   Add a `TextMeshProUGUI` for `Title` and a `Slider` (Right-click `SettingsWindowPanel > UI > Slider (Legacy)` or `UI > Slider - TextMeshPro`) for volume control.
        *   Attach the `ExampleSettingsWindow.cs` script (create it below) to `SettingsWindowPanel`.
        *   Assign `SettingsWindowPanel` to `Content Panel`.
        *   Assign the `Slider` to `Volume Slider`.
        *   Create a prefab of `SettingsWindowPanel` and **delete it from the Hierarchy.**
6.  **Register Window Prefabs:**
    *   Select `_WindowManager` in the Hierarchy.
    *   In the `WindowManager` script component, expand the `Registered Window Prefabs` list.
    *   Drag your `InfoWindowPanel` prefab and `SettingsWindowPanel` prefab from your Project window into this list.
7.  **Create UI Buttons:**
    *   Right-click `MainCanvas` `UI > Button - TextMeshPro`. Rename it `Open Info Button`.
    *   Change its text to "Open Info".
    *   Repeat for "Open Settings Button" and "Close All Windows Button".
    *   Arrange these buttons on your Canvas.
8.  **Create WindowOpener GameObject:**
    *   Create an empty GameObject as a child of `MainCanvas`. Rename it to `_WindowActivator`.
    *   Attach the `WindowOpener.cs` script to it.
    *   In the Inspector, drag your three buttons to their respective fields (`Open Info Button`, `Open Settings Button`, `Close All Button`).
    *   For each button, in its `Button (Script)` component, add an `OnClick` event:
        *   Drag `_WindowActivator` to the runtime object slot.
        *   Select `WindowOpener > OnOpenInfoButtonClicked()` for the `Open Info Button`.
        *   Select `WindowOpener > OnOpenSettingsButtonClicked()` for the `Open Settings Button`.
        *   Select `WindowOpener > OnCloseAllButtonClicked()` for the `Close All Windows Button`.

Now, you're ready to run the scene!

---

### **C# Scripts:**

Create these scripts in your Unity Project (e.g., in an `Assets/Scripts` folder).

#### 1. `IWindow.cs`

```csharp
using System;
using UnityEngine;

namespace UIModularWindowSystem
{
    /// <summary>
    /// Defines the contract for any window in the modular window system.
    /// This interface allows the WindowManager to interact with different window types polymorphically.
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// Gets the unique identifier for this window type.
        /// Using Type allows for strong typing when opening/closing windows.
        /// </summary>
        Type WindowID { get; }

        /// <summary>
        /// Initializes and displays the window.
        /// </summary>
        /// <param name="data">Optional data to pass to the window for display or initial setup.</param>
        void Open(object data = null);

        /// <summary>
        /// Hides or deactivates the window.
        /// </summary>
        void Close();

        /// <summary>
        /// Updates the window's content with new data if it's already open.
        /// </summary>
        /// <param name="data">The new data to update the window with.</param>
        void SetData(object data);

        /// <summary>
        /// Checks if the window is currently active and visible.
        /// </summary>
        /// <returns>True if the window is active, false otherwise.</returns>
        bool IsActive();

        /// <summary>
        /// Gets the RectTransform of the window's root GameObject.
        /// Useful for bringing windows to front or positioning.
        /// </summary>
        RectTransform GetRectTransform();
    }
}
```

#### 2. `BaseWindow.cs`

```csharp
using System;
using UnityEngine;

namespace UIModularWindowSystem
{
    /// <summary>
    /// Abstract base class for all modular UI windows.
    /// It provides common functionality and ensures all concrete windows adhere to the IWindow interface.
    /// </summary>
    public abstract class BaseWindow : MonoBehaviour, IWindow
    {
        [Tooltip("The root GameObject that contains all the window's UI elements. This will be enabled/disabled.")]
        [SerializeField]
        protected GameObject _contentPanel;

        private RectTransform _rectTransform;

        /// <summary>
        /// Initializes the window, typically by hiding its content panel.
        /// </summary>
        protected virtual void Awake()
        {
            if (_contentPanel == null)
            {
                Debug.LogError($"'{name}' BaseWindow: Content Panel is not assigned!", this);
                enabled = false; // Disable if critical component is missing
                return;
            }
            _contentPanel.SetActive(false); // Ensure window is hidden initially
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                Debug.LogError($"'{name}' BaseWindow: RectTransform component not found!", this);
            }
        }

        /// <summary>
        /// Implementation of IWindow.Open(). Activates the content panel and calls OnOpen.
        /// </summary>
        /// <param name="data">Optional data to pass to the window.</param>
        public void Open(object data = null)
        {
            if (!IsActive())
            {
                _contentPanel.SetActive(true);
            }
            OnOpen(data);
        }

        /// <summary>
        /// Implementation of IWindow.Close(). Deactivates the content panel and calls OnClose.
        /// </summary>
        public void Close()
        {
            if (IsActive())
            {
                _contentPanel.SetActive(false);
            }
            OnClose();
        }

        /// <summary>
        /// Implementation of IWindow.SetData(). Calls OnSetData to update content.
        /// </summary>
        /// <param name="data">The new data to update the window with.</param>
        public void SetData(object data)
        {
            OnSetData(data);
        }

        /// <summary>
        /// Implementation of IWindow.IsActive(). Checks if the content panel is active.
        /// </summary>
        /// <returns>True if the content panel is active, false otherwise.</returns>
        public bool IsActive()
        {
            return _contentPanel != null && _contentPanel.activeSelf;
        }

        /// <summary>
        /// Implementation of IWindow.GetRectTransform().
        /// </summary>
        /// <returns>The RectTransform of this window's GameObject.</returns>
        public RectTransform GetRectTransform()
        {
            return _rectTransform;
        }

        /// <summary>
        /// Abstract property for the unique ID of this window type.
        /// Concrete implementations should return their Type (e.g., 'GetType()').
        /// </summary>
        public abstract Type WindowID { get; }

        /// <summary>
        /// Abstract method called when the window is opened.
        /// Concrete windows should implement this to initialize their UI based on the provided data.
        /// </summary>
        /// <param name="data">Optional data passed during window opening.</param>
        protected abstract void OnOpen(object data);

        /// <summary>
        /// Abstract method called when the window is closed.
        /// Concrete windows should implement this for cleanup or saving data.
        /// </summary>
        protected abstract void OnClose();

        /// <summary>
        /// Virtual method called when new data is set on an already open window.
        /// Concrete windows can override this to update their UI.
        /// </summary>
        /// <param name="data">The new data to apply.</param>
        protected virtual void OnSetData(object data)
        {
            // Default implementation: if window is open, re-apply the data as if it was just opened.
            // Concrete windows can override for more specific behavior.
            if (IsActive())
            {
                OnOpen(data); // Re-initialize with new data
            }
        }
    }
}
```

#### 3. `WindowManager.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UIModularWindowSystem
{
    /// <summary>
    /// The central manager for the UIModularWindowSystem.
    /// It handles registration, instantiation, and management of all UI windows.
    /// This uses a basic singleton pattern for easy global access.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }

        [Tooltip("The parent transform under which all instantiated windows will be placed (e.g., a Canvas).")]
        [SerializeField]
        private Transform _windowParent;

        [Tooltip("List of all BaseWindow prefabs that can be opened by the system.")]
        [SerializeField]
        private List<BaseWindow> _registeredWindowPrefabs = new List<BaseWindow>();

        // Dictionary to quickly look up window prefabs by their Type.
        private Dictionary<Type, BaseWindow> _windowPrefabsMap = new Dictionary<Type, BaseWindow>();
        
        // Dictionary to keep track of currently open (instantiated) window instances.
        private Dictionary<Type, BaseWindow> _openWindows = new Dictionary<Type, BaseWindow>();

        /// <summary>
        /// Initializes the WindowManager, sets up the singleton, and registers window prefabs.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple WindowManager instances found. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure the window parent is set. If not, try to find a Canvas.
            if (_windowParent == null)
            {
                Canvas mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas != null)
                {
                    _windowParent = mainCanvas.transform;
                    Debug.LogWarning($"WindowManager: No Window Parent assigned. Using {mainCanvas.name} as default.", mainCanvas);
                }
                else
                {
                    Debug.LogError("WindowManager: No Window Parent assigned and no Canvas found in scene! Windows cannot be instantiated.", this);
                    enabled = false;
                    return;
                }
            }

            RegisterPrefabs();
        }

        /// <summary>
        /// Registers all window prefabs from the Inspector list into the internal map.
        /// Ensures only one prefab per WindowID type is registered.
        /// </summary>
        private void RegisterPrefabs()
        {
            _windowPrefabsMap.Clear();
            foreach (var windowPrefab in _registeredWindowPrefabs)
            {
                if (windowPrefab == null)
                {
                    Debug.LogWarning("WindowManager: A null reference was found in Registered Window Prefabs list. Please check your setup.", this);
                    continue;
                }

                if (_windowPrefabsMap.ContainsKey(windowPrefab.WindowID))
                {
                    Debug.LogWarning($"WindowManager: Duplicate window prefab registered for ID '{windowPrefab.WindowID}'. " +
                                     $"Only the first instance of type '{windowPrefab.name}' will be used.", windowPrefab);
                    continue;
                }
                _windowPrefabsMap.Add(windowPrefab.WindowID, windowPrefab);
            }
            Debug.Log($"WindowManager: Registered {_windowPrefabsMap.Count} unique window types.");
        }

        /// <summary>
        /// Opens a window of the specified type. If the window is already open, it brings it to front
        /// (optional) and updates its data. If not, it instantiates and opens it.
        /// </summary>
        /// <typeparam name="T">The type of the window to open (must derive from BaseWindow).</typeparam>
        /// <param name="data">Optional data to pass to the window.</param>
        /// <returns>The opened window instance, or null if the window could not be opened.</returns>
        public T OpenWindow<T>(object data = null) where T : BaseWindow
        {
            Type windowType = typeof(T);

            // Check if the window is already open
            if (_openWindows.TryGetValue(windowType, out BaseWindow existingWindow))
            {
                // Window is already open, just update its data and bring it to front
                Debug.Log($"WindowManager: Window of type '{windowType.Name}' is already open. Updating data.");
                existingWindow.SetData(data);
                BringToFront(existingWindow.GetRectTransform()); // Optional: bring existing window to front
                return existingWindow as T;
            }

            // Window is not open, try to instantiate it from prefab
            if (!_windowPrefabsMap.TryGetValue(windowType, out BaseWindow prefab))
            {
                Debug.LogError($"WindowManager: No prefab registered for window type '{windowType.Name}'. " +
                               $"Please ensure it's in the '_registeredWindowPrefabs' list.", this);
                return null;
            }

            // Instantiate the window prefab
            T newWindowInstance = Instantiate(prefab, _windowParent).GetComponent<T>();
            if (newWindowInstance == null)
            {
                Debug.LogError($"WindowManager: Instantiated prefab for '{windowType.Name}' does not contain a component of type '{typeof(T).Name}'.", prefab);
                Destroy(newWindowInstance.gameObject);
                return null;
            }

            // Add to open windows and open it
            _openWindows.Add(windowType, newWindowInstance);
            newWindowInstance.Open(data);
            Debug.Log($"WindowManager: Opened window of type '{windowType.Name}'.");

            return newWindowInstance;
        }

        /// <summary>
        /// Closes a window of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the window to close.</typeparam>
        public void CloseWindow<T>() where T : BaseWindow
        {
            Type windowType = typeof(T);

            if (_openWindows.TryGetValue(windowType, out BaseWindow windowToClose))
            {
                windowToClose.Close();
                _openWindows.Remove(windowType);
                Destroy(windowToClose.gameObject);
                Debug.Log($"WindowManager: Closed and destroyed window of type '{windowType.Name}'.");
            }
            else
            {
                Debug.LogWarning($"WindowManager: Attempted to close window of type '{windowType.Name}', but it was not found in open windows.", this);
            }
        }

        /// <summary>
        /// Closes all currently open windows managed by this system.
        /// </summary>
        public void CloseAllOpenWindows()
        {
            // Create a copy of the keys to avoid modifying the dictionary while iterating
            List<Type> typesToClose = new List<Type>(_openWindows.Keys);

            foreach (Type windowType in typesToClose)
            {
                // Retrieve the actual window instance (it might have been removed if an error occurred)
                if (_openWindows.TryGetValue(windowType, out BaseWindow windowInstance))
                {
                    windowInstance.Close();
                    Destroy(windowInstance.gameObject);
                    Debug.Log($"WindowManager: Closed all windows - Destroyed '{windowType.Name}'.");
                }
            }
            _openWindows.Clear(); // Ensure the dictionary is completely clear
            Debug.Log("WindowManager: All open windows closed.");
        }

        /// <summary>
        /// Checks if a window of the specified type is currently open.
        /// </summary>
        /// <typeparam name="T">The type of the window to check.</typeparam>
        /// <returns>True if the window is open, false otherwise.</returns>
        public bool IsWindowOpen<T>() where T : BaseWindow
        {
            return _openWindows.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Gets an open window instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the window to retrieve.</typeparam>
        /// <returns>The window instance if open, otherwise null.</returns>
        public T GetOpenWindow<T>() where T : BaseWindow
        {
            _openWindows.TryGetValue(typeof(T), out BaseWindow window);
            return window as T;
        }

        /// <summary>
        /// Brings a RectTransform to the front of its siblings in the UI hierarchy.
        /// </summary>
        /// <param name="rectTransform">The RectTransform of the window to bring to front.</param>
        private void BringToFront(RectTransform rectTransform)
        {
            if (rectTransform != null)
            {
                rectTransform.SetAsLastSibling();
            }
        }
    }
}
```

#### 4. `ExampleInfoWindow.cs`

```csharp
using System;
using UnityEngine;
using TMPro; // Required for TextMeshPro

namespace UIModularWindowSystem
{
    // A simple struct to define the data for the Info Window.
    public struct PlayerInfo
    {
        public string Name;
        public int Level;
        public string Class;
    }

    /// <summary>
    /// A concrete implementation of a BaseWindow for displaying player information.
    /// </summary>
    public class ExampleInfoWindow : BaseWindow
    {
        [Header("Info Window Specific UI")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _contentText;

        // Returns the Type of this class as its unique ID.
        public override Type WindowID => GetType();

        /// <summary>
        /// Called when the Info Window is opened.
        /// Initializes UI elements with player data.
        /// </summary>
        /// <param name="data">Expected to be a PlayerInfo struct.</param>
        protected override void OnOpen(object data)
        {
            if (_titleText == null || _contentText == null)
            {
                Debug.LogError($"ExampleInfoWindow: UI elements not assigned on '{name}'!", this);
                return;
            }

            if (data is PlayerInfo playerInfo)
            {
                _titleText.text = $"{playerInfo.Name}'s Profile";
                _contentText.text = $"Level: {playerInfo.Level}\nClass: {playerInfo.Class}";
                Debug.Log($"ExampleInfoWindow: Opened with Player Info for {playerInfo.Name}");
            }
            else
            {
                _titleText.text = "Player Profile";
                _contentText.text = "No player data provided.";
                Debug.LogWarning($"ExampleInfoWindow: Opened without valid PlayerInfo data on '{name}'.", this);
            }
        }

        /// <summary>
        /// Called when the Info Window is closed.
        /// Clears UI elements.
        /// </summary>
        protected override void OnClose()
        {
            if (_titleText != null) _titleText.text = "";
            if (_contentText != null) _contentText.text = "";
            Debug.Log($"ExampleInfoWindow: Closed '{name}'.");
        }
    }
}
```

#### 5. `ExampleSettingsWindow.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.UI; // Required for Slider
using TMPro; // Required for TextMeshPro

namespace UIModularWindowSystem
{
    // A simple struct to define the data for the Settings Window.
    public struct SettingsData
    {
        public float MasterVolume;
        public bool Fullscreen;
    }

    /// <summary>
    /// A concrete implementation of a BaseWindow for displaying game settings.
    /// </summary>
    public class ExampleSettingsWindow : BaseWindow
    {
        [Header("Settings Window Specific UI")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TextMeshProUGUI _volumeValueText; // To display current volume

        // Returns the Type of this class as its unique ID.
        public override Type WindowID => GetType();

        protected override void Awake()
        {
            base.Awake(); // Call BaseWindow's Awake to initialize content panel
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void OnDestroy()
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            }
        }

        /// <summary>
        /// Called when the Settings Window is opened.
        /// Initializes UI elements with settings data.
        /// </summary>
        /// <param name="data">Expected to be a SettingsData struct.</param>
        protected override void OnOpen(object data)
        {
            if (_titleText == null || _volumeSlider == null || _fullscreenToggle == null || _volumeValueText == null)
            {
                Debug.LogError($"ExampleSettingsWindow: UI elements not assigned on '{name}'!", this);
                return;
            }

            _titleText.text = "Game Settings";

            SettingsData settings = new SettingsData { MasterVolume = 0.5f, Fullscreen = true }; // Default values

            if (data is SettingsData receivedSettings)
            {
                settings = receivedSettings;
                Debug.Log($"ExampleSettingsWindow: Opened with SettingsData: Volume={settings.MasterVolume}, Fullscreen={settings.Fullscreen}");
            }
            else
            {
                Debug.LogWarning($"ExampleSettingsWindow: Opened without valid SettingsData on '{name}'. Using defaults.", this);
            }

            _volumeSlider.value = settings.MasterVolume;
            _fullscreenToggle.isOn = settings.Fullscreen;
            UpdateVolumeText(settings.MasterVolume); // Update text immediately
        }

        /// <summary>
        /// Called when the Settings Window is closed.
        /// Saves current settings (demonstration).
        /// </summary>
        protected override void OnClose()
        {
            // In a real application, you would save these settings to PlayerPrefs, a file, etc.
            Debug.Log($"ExampleSettingsWindow: Closing '{name}'. Saving current settings: " +
                      $"Volume={_volumeSlider.value:F2}, Fullscreen={_fullscreenToggle.isOn}");

            // Clear UI (optional)
            if (_titleText != null) _titleText.text = "";
            if (_volumeValueText != null) _volumeValueText.text = "";
        }

        /// <summary>
        /// Handles volume slider value changes.
        /// </summary>
        /// <param name="value">The new slider value.</param>
        private void OnVolumeChanged(float value)
        {
            UpdateVolumeText(value);
            // In a real game, you'd update audio mixer volume here
            // Debug.Log($"Volume changed to: {value:F2}");
        }

        /// <summary>
        /// Handles fullscreen toggle value changes.
        /// </summary>
        /// <param name="isOn">The new toggle state.</param>
        private void OnFullscreenChanged(bool isOn)
        {
            // In a real game, you'd update screen settings here
            // Debug.Log($"Fullscreen changed to: {isOn}");
        }

        /// <summary>
        /// Updates the TextMeshProUGUI with the current volume value.
        /// </summary>
        /// <param name="volume">The current volume float.</param>
        private void UpdateVolumeText(float volume)
        {
            if (_volumeValueText != null)
            {
                _volumeValueText.text = $"Volume: {(volume * 100):F0}%";
            }
        }
    }
}
```

#### 6. `WindowOpener.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro; // For potential TextMeshPro buttons if used

namespace UIModularWindowSystem
{
    /// <summary>
    /// A simple utility script to demonstrate opening and closing windows
    /// via UI buttons. This would typically be attached to some control panel
    /// or individual button game objects.
    /// </summary>
    public class WindowOpener : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button _openInfoButton;
        [SerializeField] private Button _openSettingsButton;
        [SerializeField] private Button _closeAllButton;

        private void Awake()
        {
            // Add listeners for button clicks
            _openInfoButton?.onClick.AddListener(OnOpenInfoButtonClicked);
            _openSettingsButton?.onClick.AddListener(OnOpenSettingsButtonClicked);
            _closeAllButton?.onClick.AddListener(OnCloseAllButtonClicked);
        }

        private void OnDestroy()
        {
            // Remove listeners to prevent memory leaks
            _openInfoButton?.onClick.RemoveListener(OnOpenInfoButtonClicked);
            _openSettingsButton?.onClick.RemoveListener(OnOpenSettingsButtonClicked);
            _closeAllButton?.onClick.RemoveListener(OnCloseAllButtonClicked);
        }

        /// <summary>
        /// Callback for when the 'Open Info' button is clicked.
        /// Demonstrates opening a window and passing data.
        /// </summary>
        public void OnOpenInfoButtonClicked()
        {
            // Prepare data to pass to the Info Window
            PlayerInfo player = new PlayerInfo
            {
                Name = "Unity Hero",
                Level = 42,
                Class = "Pattern Weaver"
            };

            // Open the ExampleInfoWindow via the WindowManager, passing the player data
            WindowManager.Instance.OpenWindow<ExampleInfoWindow>(player);
            Debug.Log("WindowOpener: Requesting ExampleInfoWindow to open.");
        }

        /// <summary>
        /// Callback for when the 'Open Settings' button is clicked.
        /// Demonstrates opening a window with settings data.
        /// </summary>
        public void OnOpenSettingsButtonClicked()
        {
            // Prepare data for the Settings Window
            SettingsData currentSettings = new SettingsData
            {
                MasterVolume = 0.65f, // Example current volume
                Fullscreen = true    // Example current fullscreen state
            };

            // Open the ExampleSettingsWindow, passing the settings data
            WindowManager.Instance.OpenWindow<ExampleSettingsWindow>(currentSettings);
            Debug.Log("WindowOpener: Requesting ExampleSettingsWindow to open.");
        }

        /// <summary>
        /// Callback for when the 'Close All' button is clicked.
        /// Demonstrates closing all open windows.
        /// </summary>
        public void OnCloseAllButtonClicked()
        {
            // Request the WindowManager to close all currently open windows
            WindowManager.Instance.CloseAllOpenWindows();
            Debug.Log("WindowOpener: Requesting all open windows to close.");
        }
    }
}
```

---

This complete set of scripts provides a robust and educational foundation for implementing the UIModularWindowSystem pattern in your Unity projects. Remember to follow the Unity setup instructions carefully to get it working immediately.