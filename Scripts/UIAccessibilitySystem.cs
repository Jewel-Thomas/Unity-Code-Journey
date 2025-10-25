// Unity Design Pattern Example: UIAccessibilitySystem
// This script demonstrates the UIAccessibilitySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'UIAccessibilitySystem' design pattern, while not a formally recognized Gang of Four pattern, refers to a common architectural approach for implementing accessibility features in a user interface. It draws inspiration from systems like Apple's UIAccessibility framework.

**Core Principles of the UIAccessibilitySystem Pattern:**

1.  **Centralized Management**: A single, dedicated system (the "Accessibility Manager") acts as the hub for all accessibility-related concerns. It doesn't directly implement accessibility features but orchestrates them.
2.  **Decoupled Elements**: Individual UI elements do not need to know about specific accessibility tools (like screen readers). Instead, they expose their accessibility properties and behaviors through a common interface.
3.  **Registration and Discovery**: UI elements "register" themselves with the Accessibility Manager upon activation (e.g., `OnEnable`) and "unregister" themselves upon deactivation (e.g., `OnDisable`). The manager maintains a registry of all currently available accessible elements.
4.  **Contextual Information**: The manager can query registered elements for their accessibility-specific information (name, role, value, hint, interactability).
5.  **Interaction Proxy**: The manager acts as an intermediary for accessibility tools. It provides methods for navigating between elements, activating them, or changing their state through an accessible interface.
6.  **Event-Driven Communication**: The manager notifies interested accessibility tools (e.g., a screen reader) about changes in focus or important system events (like alerts).

**Real-World Use Case in Unity:**

Imagine you're building a game or application in Unity and want to support basic screen reader functionality for visually impaired users. This system would allow them to navigate your UI using keyboard commands (e.g., Tab for next, Shift+Tab for previous, Enter to activate) and have a simulated screen reader announce the details of the currently focused element.

---

### Complete C# Unity Example: UIAccessibilitySystem

This example demonstrates the UIAccessibilitySystem pattern using a `AccessibilityManager` (the central system), an `IAccessibleElement` interface (what UI elements expose), concrete `AccessibleButton` and `AccessibleSlider` components, and an `AccessibilityScreenReaderSimulator` to show how an external tool would interact with the system.

**Project Setup Instructions:**

1.  Create a new Unity 3D project.
2.  Create an empty GameObject in your scene named `AccessibilityManager`.
3.  Create an empty GameObject in your scene named `AccessibilityScreenReader`.
4.  Create a `Canvas` (GameObject -> UI -> Canvas).
5.  Add a Unity `Button` (Right Click Canvas -> UI -> Button) to the Canvas.
6.  Add a Unity `Slider` (Right Click Canvas -> UI -> Slider) to the Canvas.
7.  Create a folder named `Scripts` in your Project window.
8.  Create the C# scripts listed below inside the `Scripts` folder.
9.  Attach `AccessibilityManager.cs` to the `AccessibilityManager` GameObject.
10. Attach `AccessibilityScreenReaderSimulator.cs` to the `AccessibilityScreenReader` GameObject.
11. Attach `AccessibleButton.cs` to the `Button` GameObject on your Canvas.
12. Attach `AccessibleSlider.cs` to the `Slider` GameObject on your Canvas.
13. **Configure the Accessible Components in the Inspector:**
    *   **AccessibleButton:**
        *   Drag your Canvas Button into the "Target Button" field.
        *   Set "Accessibility Name" (e.g., "Start Game Button").
        *   Set "Accessibility Hint" (e.g., "Press to begin the adventure.").
        *   Set "Tab Index" (e.g., 0).
    *   **AccessibleSlider:**
        *   Drag your Canvas Slider into the "Target Slider" field.
        *   Set "Accessibility Name" (e.g., "Volume Level").
        *   Set "Accessibility Hint" (e.g., "Use left/right arrow keys to adjust volume.").
        *   Set "Tab Index" (e.g., 1).
14. Run the scene.
15. Use **Tab** and **Shift+Tab** to navigate between elements. Use **Enter** to activate a button. Use **Left/Right Arrow** keys to adjust the slider. Observe the Console output simulating screen reader announcements.

---

### 1. `IAccessibleElement.cs`

This interface defines the contract for any UI element that wishes to expose itself to the accessibility system.

```csharp
using UnityEngine;

namespace UIAccessibilitySystem
{
    /// <summary>
    /// Defines the roles an accessible UI element can have, helping accessibility tools
    /// interpret its purpose (e.g., a "Button" is interactive, "StaticText" is purely informative).
    /// </summary>
    public enum AccessibilityRole
    {
        Button,
        Toggle,
        Slider,
        Input,
        StaticText,
        Custom,
        // Add more roles as needed for your application (e.g., Dropdown, ScrollView, ProgressBar)
    }

    /// <summary>
    /// The core interface for the UIAccessibilitySystem pattern.
    /// Any UI component that needs to be accessible must implement this interface.
    /// It defines the standard properties and methods that the AccessibilityManager
    /// and any consuming accessibility tools will use to interact with the element.
    /// </summary>
    public interface IAccessibleElement
    {
        /// <summary>
        /// A concise, localized name for the element, announced by a screen reader.
        /// Example: "Play button", "Volume slider", "Username input field".
        /// </summary>
        string AccessibilityName { get; }

        /// <summary>
        /// The current value of the element, relevant for sliders, input fields, toggles.
        /// Example: "50 percent", "On", "John Doe".
        /// </summary>
        string AccessibilityValue { get; }

        /// <summary>
        /// Additional descriptive text or instructions for the user.
        /// Example: "Double tap to activate", "Use left/right arrow keys to adjust".
        /// </summary>
        string AccessibilityHint { get; }

        /// <summary>
        /// The role or type of the UI element, indicating its primary function.
        /// </summary>
        AccessibilityRole AccessibilityRole { get; }

        /// <summary>
        /// Indicates whether the element can currently be interacted with by the user.
        /// (e.g., a disabled button should return false).
        /// </summary>
        bool IsInteractable { get; }

        /// <summary>
        /// A numerical index used to determine the sequential navigation order (e.g., with Tab key).
        /// Lower values typically mean earlier in the navigation order.
        /// </summary>
        int TabIndex { get; }

        /// <summary>
        /// Provides a reference to the GameObject associated with this accessible element.
        /// Useful for visual focus indication or spatial sorting.
        /// </summary>
        GameObject GetGameObject();

        /// <summary>
        /// Called when an accessibility tool requests to activate the element
        /// (e.g., a screen reader "click" or an assistive switch activation).
        /// This should trigger the same action as a normal user interaction.
        /// </summary>
        void OnAccessibilityActivate();

        /// <summary>
        /// Called when an accessibility tool requests to increase the element's value.
        /// Relevant for sliders, steppers, or other adjustable elements.
        /// </summary>
        void OnAccessibilityIncrease();

        /// <summary>
        /// Called when an accessibility tool requests to decrease the element's value.
        /// Relevant for sliders, steppers, or other adjustable elements.
        /// </summary>
        void OnAccessibilityDecrease();

        /// <summary>
        /// Called by the AccessibilityManager when this element gains accessibility focus.
        /// Useful for providing visual feedback (e.g., a highlight) to the user.
        /// </summary>
        void OnAccessibilityFocusGained();

        /// <summary>
        /// Called by the AccessibilityManager when this element loses accessibility focus.
        /// Useful for removing visual feedback.
        /// </summary>
        void OnAccessibilityFocusLost();
    }
}
```

### 2. `AccessibilityManager.cs`

This is the central component of the UIAccessibilitySystem. It manages the registration of accessible elements, handles focus, and dispatches events. It's implemented as a Singleton for easy global access.

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UIAccessibilitySystem
{
    /// <summary>
    /// The core component of the UIAccessibilitySystem design pattern.
    /// This manager acts as a central hub for all accessible UI elements in the scene.
    /// It's responsible for:
    /// 1. Registering and unregistering IAccessibleElements.
    /// 2. Managing the current accessibility focus.
    /// 3. Providing methods for navigating between accessible elements.
    /// 4. Dispatching events to notify accessibility tools (like a screen reader)
    ///    about focus changes or system alerts.
    ///
    /// This class follows the Singleton pattern to ensure only one instance exists
    /// and is easily accessible throughout the application.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        // --- Singleton Implementation ---
        public static AccessibilityManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("AccessibilityManager: Multiple instances found! Destroying duplicate.", this);
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                // Optional: Make the manager persist across scenes if needed.
                // DontDestroyOnLoad(this.gameObject);
                Debug.Log("AccessibilityManager: Initialized.");
            }
        }

        // --- Accessible Element Management ---
        private readonly List<IAccessibleElement> _accessibleElements = new List<IAccessibleElement>();
        private int _focusedElementIndex = -1; // -1 indicates no element is currently focused.

        /// <summary>
        /// Event fired when the accessibility focus changes to a new element.
        /// An AccessibilityScreenReaderSimulator would subscribe to this to announce the new element.
        /// </summary>
        public UnityEvent<IAccessibleElement> OnFocusedElementChanged = new UnityEvent<IAccessibleElement>();

        /// <summary>
        /// Event fired for general accessibility alerts or announcements that are not tied
        /// to a specific UI element (e.g., "Game Paused", "New Message Received").
        /// </summary>
        public UnityEvent<string> OnAccessibilityAlert = new UnityEvent<string>();

        /// <summary>
        /// Gets the currently focused accessible element.
        /// Returns null if no element is focused.
        /// </summary>
        public IAccessibleElement CurrentlyFocusedElement
        {
            get
            {
                if (_focusedElementIndex >= 0 && _focusedElementIndex < _accessibleElements.Count)
                {
                    return _accessibleElements[_focusedElementIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// Registers an IAccessibleElement with the AccessibilityManager.
        /// Elements are automatically sorted by their TabIndex.
        /// </summary>
        /// <param name="element">The element to register.</param>
        public void RegisterElement(IAccessibleElement element)
        {
            if (element == null || _accessibleElements.Contains(element)) return;

            _accessibleElements.Add(element);
            _accessibleElements.Sort((a, b) => a.TabIndex.CompareTo(b.TabIndex)); // Keep elements sorted by TabIndex

            Debug.Log($"AccessibilityManager: Registered element: {element.AccessibilityName} (TabIndex: {element.TabIndex})");

            // If this is the first element, or if no element was focused,
            // set focus to this new element (or the first element).
            if (_focusedElementIndex == -1 || _accessibleElements.Count == 1)
            {
                SetFocusedElement(0);
            }
            // If the currently focused element was removed and this new element took its spot.
            else if (_accessibleElements.IndexOf(element) <= _focusedElementIndex && _focusedElementIndex > 0)
            {
                // Adjust focused index if an element was inserted before the current focus
                _focusedElementIndex = _accessibleElements.IndexOf(CurrentlyFocusedElement);
            }
        }

        /// <summary>
        /// Unregisters an IAccessibleElement from the AccessibilityManager.
        /// If the unregistered element was focused, focus shifts to the next available element.
        /// </summary>
        /// <param name="element">The element to unregister.</param>
        public void UnregisterElement(IAccessibleElement element)
        {
            if (element == null || !_accessibleElements.Contains(element)) return;

            int removedIndex = _accessibleElements.IndexOf(element);
            _accessibleElements.Remove(element);

            Debug.Log($"AccessibilityManager: Unregistered element: {element.AccessibilityName}");

            // If the removed element was the focused one
            if (removedIndex == _focusedElementIndex)
            {
                element.OnAccessibilityFocusLost(); // Ensure focus lost is called
                _focusedElementIndex = -1; // Reset focus temporarily

                if (_accessibleElements.Count > 0)
                {
                    // Try to focus the next element, or the first if it was the last.
                    SetFocusedElement(Mathf.Min(removedIndex, _accessibleElements.Count - 1));
                }
                else
                {
                    OnFocusedElementChanged.Invoke(null); // No elements left
                }
            }
            // If an element *before* the focused one was removed, adjust the focus index.
            else if (removedIndex < _focusedElementIndex)
            {
                _focusedElementIndex--;
            }
        }

        /// <summary>
        /// Sets the accessibility focus to a specific element by its index in the sorted list.
        /// This method handles calling OnAccessibilityFocusLost and OnAccessibilityFocusGained.
        /// </summary>
        /// <param name="index">The index of the element to focus.</param>
        public void SetFocusedElement(int index)
        {
            if (_accessibleElements.Count == 0)
            {
                _focusedElementIndex = -1;
                OnFocusedElementChanged.Invoke(null);
                return;
            }

            index = Mathf.Clamp(index, 0, _accessibleElements.Count - 1);

            if (_focusedElementIndex != index)
            {
                // Notify the previously focused element that it lost focus.
                if (_focusedElementIndex >= 0 && _focusedElementIndex < _accessibleElements.Count)
                {
                    _accessibleElements[_focusedElementIndex]?.OnAccessibilityFocusLost();
                }

                _focusedElementIndex = index;
                IAccessibleElement newFocus = _accessibleElements[_focusedElementIndex];

                // Notify the new element that it gained focus.
                newFocus.OnAccessibilityFocusGained();

                // Notify any subscribers (e.g., screen reader) that focus has changed.
                OnFocusedElementChanged.Invoke(newFocus);
                Debug.Log($"AccessibilityManager: Focus changed to: {newFocus.AccessibilityName}");
            }
        }

        /// <summary>
        /// Moves accessibility focus to the next element in the sorted list.
        /// Wraps around to the beginning if at the end.
        /// </summary>
        public void FocusNextElement()
        {
            if (_accessibleElements.Count == 0) return;
            SetFocusedElement((_focusedElementIndex + 1) % _accessibleElements.Count);
        }

        /// <summary>
        /// Moves accessibility focus to the previous element in the sorted list.
        /// Wraps around to the end if at the beginning.
        /// </summary>
        public void FocusPreviousElement()
        {
            if (_accessibleElements.Count == 0) return;
            int newIndex = _focusedElementIndex - 1;
            if (newIndex < 0) newIndex = _accessibleElements.Count - 1;
            SetFocusedElement(newIndex);
        }

        /// <summary>
        /// Activates the currently focused accessible element.
        /// This typically calls the OnAccessibilityActivate method of the focused element.
        /// </summary>
        public void ActivateFocusedElement()
        {
            CurrentlyFocusedElement?.OnAccessibilityActivate();
        }

        /// <summary>
        /// Increases the value of the currently focused accessible element (if applicable).
        /// </summary>
        public void IncreaseFocusedElementValue()
        {
            CurrentlyFocusedElement?.OnAccessibilityIncrease();
        }

        /// <summary>
        /// Decreases the value of the currently focused accessible element (if applicable).
        /// </summary>
        public void DecreaseFocusedElementValue()
        {
            CurrentlyFocusedElement?.OnAccessibilityDecrease();
        }

        /// <summary>
        /// Triggers an accessibility alert to be announced by any listening accessibility tools.
        /// </summary>
        /// <param name="message">The message to announce.</param>
        public void AnnounceAccessibilityAlert(string message)
        {
            OnAccessibilityAlert.Invoke(message);
            Debug.Log($"AccessibilityManager: Alert: {message}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Debug.Log("AccessibilityManager: Destroyed.");
            }
        }
    }
}
```

### 3. `AccessibleButton.cs`

A concrete implementation of `IAccessibleElement` for a standard Unity UI Button.

```csharp
using UnityEngine;
using UnityEngine.UI;
using UIAccessibilitySystem; // Ensure this using statement is present

/// <summary>
/// This component makes a standard Unity UI Button accessible to the UIAccessibilitySystem.
/// It implements the IAccessibleElement interface, exposing button-specific properties
/// and behaviors to the AccessibilityManager.
/// </summary>
[RequireComponent(typeof(Button))] // Ensures this script is always on a GameObject with a Button component.
public class AccessibleButton : MonoBehaviour, IAccessibleElement
{
    [Tooltip("The actual Unity UI Button component this script will control.")]
    [SerializeField] private Button targetButton;

    [Tooltip("A short, descriptive name for this button (e.g., 'Play Game', 'Settings').")]
    [SerializeField] private string accessibilityName = "Untitled Button";

    [Tooltip("Additional instructions or context for the user (e.g., 'Starts the game').")]
    [SerializeField] private string accessibilityHint = "";

    [Tooltip("The order in which this element will be focused during sequential navigation (e.g., Tab key).")]
    [SerializeField] private int tabIndex;

    private Color originalColor; // To restore button color after focus.
    private Image buttonImage;   // To apply visual focus indicator.

    // --- IAccessibleElement Implementation ---
    public string AccessibilityName => accessibilityName;
    public string AccessibilityValue => ""; // Buttons don't typically have a 'value'.
    public string AccessibilityHint => accessibilityHint;
    public AccessibilityRole AccessibilityRole => AccessibilityRole.Button;
    public bool IsInteractable => targetButton != null && targetButton.interactable;
    public int TabIndex => tabIndex;

    public GameObject GetGameObject() => gameObject;

    /// <summary>
    /// Called when the accessibility system requests to activate this button.
    /// This effectively simulates a click on the button.
    /// </summary>
    public void OnAccessibilityActivate()
    {
        if (IsInteractable)
        {
            Debug.Log($"<color=cyan>AccessibleButton: '{AccessibilityName}' activated by accessibility system.</color>");
            targetButton.onClick.Invoke(); // Trigger the button's normal OnClick event.
            // Example: Play a sound or show a visual feedback here.
        }
        else
        {
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert($"{AccessibilityName} is currently disabled.");
        }
    }

    // Buttons don't typically have increase/decrease functionality.
    public void OnAccessibilityIncrease() { /* Do nothing */ }
    public void OnAccessibilityDecrease() { /* Do nothing */ }

    /// <summary>
    /// Called when this button gains accessibility focus.
    /// Provides a visual highlight to indicate focus.
    /// </summary>
    public void OnAccessibilityFocusGained()
    {
        Debug.Log($"<color=green>AccessibleButton: '{AccessibilityName}' gained focus.</color>");
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
            buttonImage.color = Color.yellow; // Highlight in yellow
        }
    }

    /// <summary>
    /// Called when this button loses accessibility focus.
    /// Removes the visual highlight.
    /// </summary>
    public void OnAccessibilityFocusLost()
    {
        Debug.Log($"<color=red>AccessibleButton: '{AccessibilityName}' lost focus.</color>");
        if (buttonImage != null)
        {
            buttonImage.color = originalColor; // Restore original color
        }
    }

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
        buttonImage = targetButton.GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Register this accessible element with the central AccessibilityManager.
        AccessibilityManager.Instance?.RegisterElement(this);
    }

    private void OnDisable()
    {
        // Unregister this accessible element when it's disabled or destroyed.
        AccessibilityManager.Instance?.UnregisterElement(this);
        // Ensure focus state is reset if it was focused when disabled.
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
    }

    // --- Editor-related methods ---
    private void OnValidate()
    {
        // Auto-assign the button if not set in editor.
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
    }
}
```

### 4. `AccessibleSlider.cs`

A concrete implementation of `IAccessibleElement` for a standard Unity UI Slider.

```csharp
using UnityEngine;
using UnityEngine.UI;
using UIAccessibilitySystem; // Ensure this using statement is present

/// <summary>
/// This component makes a standard Unity UI Slider accessible to the UIAccessibilitySystem.
/// It implements the IAccessibleElement interface, exposing slider-specific properties
/// and behaviors (like increasing/decreasing value) to the AccessibilityManager.
/// </summary>
[RequireComponent(typeof(Slider))] // Ensures this script is always on a GameObject with a Slider component.
public class AccessibleSlider : MonoBehaviour, IAccessibleElement
{
    [Tooltip("The actual Unity UI Slider component this script will control.")]
    [SerializeField] private Slider targetSlider;

    [Tooltip("A short, descriptive name for this slider (e.g., 'Volume', 'Brightness').")]
    [SerializeField] private string accessibilityName = "Untitled Slider";

    [Tooltip("Additional instructions or context for the user (e.g., 'Use left/right arrow keys to adjust').")]
    [SerializeField] private string accessibilityHint = "";

    [Tooltip("The step size to use when increasing or decreasing the slider's value via accessibility commands.")]
    [SerializeField] private float stepIncrement = 0.1f;

    [Tooltip("The order in which this element will be focused during sequential navigation (e.g., Tab key).")]
    [SerializeField] private int tabIndex;

    private Color originalHandleColor; // To restore slider handle color after focus.
    private Image sliderHandleImage;   // To apply visual focus indicator.

    // --- IAccessibleElement Implementation ---
    public string AccessibilityName => accessibilityName;
    public string AccessibilityValue => targetSlider != null ? $"{Mathf.RoundToInt(targetSlider.normalizedValue * 100)}%" : "N/A";
    public string AccessibilityHint => accessibilityHint;
    public AccessibilityRole AccessibilityRole => AccessibilityRole.Slider;
    public bool IsInteractable => targetSlider != null && targetSlider.interactable;
    public int TabIndex => tabIndex;

    public GameObject GetGameObject() => gameObject;

    /// <summary>
    /// Sliders don't typically have a direct "activate" action like a button,
    /// but this could be used to toggle a mute state or open a detailed settings panel.
    /// For this example, it will announce its current value.
    /// </summary>
    public void OnAccessibilityActivate()
    {
        if (IsInteractable)
        {
            Debug.Log($"<color=cyan>AccessibleSlider: '{AccessibilityName}' activated. Current value: {AccessibilityValue}.</color>");
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert($"{AccessibilityName}, current value {AccessibilityValue}");
        }
        else
        {
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert($"{AccessibilityName} is currently disabled.");
        }
    }

    /// <summary>
    /// Increases the slider's value by the step increment.
    /// </summary>
    public void OnAccessibilityIncrease()
    {
        if (IsInteractable && targetSlider != null)
        {
            targetSlider.value = Mathf.Min(targetSlider.maxValue, targetSlider.value + stepIncrement * (targetSlider.maxValue - targetSlider.minValue));
            Debug.Log($"<color=green>AccessibleSlider: '{AccessibilityName}' increased to {AccessibilityValue}.</color>");
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert($"{AccessibilityName}, {AccessibilityValue}");
        }
    }

    /// <summary>
    /// Decreases the slider's value by the step increment.
    /// </summary>
    public void OnAccessibilityDecrease()
    {
        if (IsInteractable && targetSlider != null)
        {
            targetSlider.value = Mathf.Max(targetSlider.minValue, targetSlider.value - stepIncrement * (targetSlider.maxValue - targetSlider.minValue));
            Debug.Log($"<color=red>AccessibleSlider: '{AccessibilityName}' decreased to {AccessibilityValue}.</color>");
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert($"{AccessibilityName}, {AccessibilityValue}");
        }
    }

    /// <summary>
    /// Called when this slider gains accessibility focus.
    /// Provides a visual highlight to indicate focus (e.g., on the slider handle).
    /// </summary>
    public void OnAccessibilityFocusGained()
    {
        Debug.Log($"<color=green>AccessibleSlider: '{AccessibilityName}' gained focus.</color>");
        if (sliderHandleImage != null)
        {
            originalHandleColor = sliderHandleImage.color;
            sliderHandleImage.color = Color.yellow; // Highlight handle in yellow
        }
    }

    /// <summary>
    /// Called when this slider loses accessibility focus.
    /// Removes the visual highlight.
    /// </summary>
    public void OnAccessibilityFocusLost()
    {
        Debug.Log($"<color=red>AccessibleSlider: '{AccessibilityName}' lost focus.</color>");
        if (sliderHandleImage != null)
        {
            sliderHandleImage.color = originalHandleColor; // Restore original color
        }
    }

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        if (targetSlider == null)
        {
            targetSlider = GetComponent<Slider>();
        }

        // Try to find the handle image to apply focus indication.
        // Assuming the handle is usually a child with an Image component.
        Transform handle = targetSlider.handleRect;
        if (handle != null)
        {
            sliderHandleImage = handle.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        // Register this accessible element with the central AccessibilityManager.
        AccessibilityManager.Instance?.RegisterElement(this);
    }

    private void OnDisable()
    {
        // Unregister this accessible element when it's disabled or destroyed.
        AccessibilityManager.Instance?.UnregisterElement(this);
        // Ensure focus state is reset if it was focused when disabled.
        if (sliderHandleImage != null)
        {
            sliderHandleImage.color = originalHandleColor;
        }
    }

    // --- Editor-related methods ---
    private void OnValidate()
    {
        // Auto-assign the slider if not set in editor.
        if (targetSlider == null)
        {
            targetSlider = GetComponent<Slider>();
        }
    }
}
```

### 5. `AccessibilityScreenReaderSimulator.cs`

This component acts as a consumer of the `UIAccessibilitySystem`. It simulates a screen reader by listening to events from the `AccessibilityManager` and logging relevant information to the console. It also handles keyboard input to send commands to the `AccessibilityManager`.

```csharp
using UnityEngine;
using UIAccessibilitySystem; // Ensure this using statement is present

/// <summary>
/// This component simulates a basic screen reader that interacts with the UIAccessibilitySystem.
/// It demonstrates how an external accessibility tool would:
/// 1. Listen for focus changes from the AccessibilityManager.
/// 2. Announce the details of the currently focused accessible element.
/// 3. Provide input (e.g., keyboard commands) to navigate and interact with the UI
///    through the AccessibilityManager.
///
/// This script should be attached to a GameObject in your scene.
/// </summary>
public class AccessibilityScreenReaderSimulator : MonoBehaviour
{
    private IAccessibleElement _lastAnnouncedElement;

    private void OnEnable()
    {
        // Subscribe to the AccessibilityManager's events.
        // This is how the screen reader receives updates from the UIAccessibilitySystem.
        if (AccessibilityManager.Instance != null)
        {
            AccessibilityManager.Instance.OnFocusedElementChanged.AddListener(AnnounceFocusedElement);
            AccessibilityManager.Instance.OnAccessibilityAlert.AddListener(AnnounceAlert);
            Debug.Log("AccessibilityScreenReaderSimulator: Subscribed to AccessibilityManager events.");
        }
        else
        {
            Debug.LogError("AccessibilityScreenReaderSimulator: AccessibilityManager instance not found. Make sure it's in the scene and initialized.");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks and ensure proper cleanup.
        if (AccessibilityManager.Instance != null)
        {
            AccessibilityManager.Instance.OnFocusedElementChanged.RemoveListener(AnnounceFocusedElement);
            AccessibilityManager.Instance.OnAccessibilityAlert.RemoveListener(AnnounceAlert);
            Debug.Log("AccessibilityScreenReaderSimulator: Unsubscribed from AccessibilityManager events.");
        }
    }

    private void Update()
    {
        // --- Input Handling for Accessibility Navigation ---
        // This simulates how a screen reader or switch control might send commands
        // to the accessibility system based on user input.

        // Tab: Focus next element
        if (Input.GetKeyDown(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            AccessibilityManager.Instance?.FocusNextElement();
        }
        // Shift+Tab: Focus previous element
        else if (Input.GetKeyDown(KeyCode.Tab) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            AccessibilityManager.Instance?.FocusPreviousElement();
        }
        // Enter: Activate focused element
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            AccessibilityManager.Instance?.ActivateFocusedElement();
        }
        // Left Arrow: Decrease focused element value (e.g., slider)
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AccessibilityManager.Instance?.DecreaseFocusedElementValue();
        }
        // Right Arrow: Increase focused element value (e.g., slider)
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AccessibilityManager.Instance?.IncreaseFocusedElementValue();
        }
        // Example: Escape key for a general alert
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            AccessibilityManager.Instance?.AnnounceAccessibilityAlert("Menu closed.");
        }
    }

    /// <summary>
    /// This method is called by the AccessibilityManager when the accessibility focus changes.
    /// It simulates a screen reader announcing the details of the new element.
    /// </summary>
    /// <param name="element">The newly focused IAccessibleElement, or null if focus is lost from all elements.</param>
    private void AnnounceFocusedElement(IAccessibleElement element)
    {
        if (element != null && element != _lastAnnouncedElement)
        {
            string announcement = $"<color=orange>Screen Reader:</color> ";
            announcement += $"{element.AccessibilityName}";

            if (!string.IsNullOrEmpty(element.AccessibilityValue))
            {
                announcement += $", {element.AccessibilityValue}";
            }

            announcement += $", {element.AccessibilityRole}";

            if (!string.IsNullOrEmpty(element.AccessibilityHint))
            {
                announcement += $", Hint: {element.AccessibilityHint}";
            }

            if (!element.IsInteractable)
            {
                announcement += ", Disabled";
            }

            Debug.Log(announcement);
            _lastAnnouncedElement = element;
        }
        else if (element == null)
        {
            Debug.Log("<color=orange>Screen Reader:</color> Focus cleared.");
            _lastAnnouncedElement = null;
        }
    }

    /// <summary>
    /// This method is called by the AccessibilityManager when a general system alert needs to be announced.
    /// </summary>
    /// <param name="message">The alert message to announce.</param>
    private void AnnounceAlert(string message)
    {
        Debug.Log($"<color=orange>Screen Reader (Alert):</color> {message}");
    }
}
```

---

### How the UIAccessibilitySystem Pattern Works in this Example:

1.  **`IAccessibleElement`**: This interface defines *what* an accessible UI element needs to provide. It's a contract. Any UI component that wants to participate in the accessibility system must implement this. This promotes **decoupling**; the UI element doesn't care *how* its information is used, only that it provides it.

2.  **`AccessibilityManager`**:
    *   **Singleton**: `AccessibilityManager.Instance` provides a global, easy-to-access point, making it a **Service Locator** for accessibility services.
    *   **Central Registry**: `_accessibleElements` is a `List` that stores all currently active `IAccessibleElement`s. Components call `RegisterElement` in `OnEnable` and `UnregisterElement` in `OnDisable`. This centralizes the management of accessible UI.
    *   **Focus Management**: It maintains `_focusedElementIndex` and methods like `FocusNextElement()`, `FocusPreviousElement()`, `SetFocusedElement()`. This encapsulates the logic for navigating the accessible UI.
    *   **Event Dispatcher**: `OnFocusedElementChanged` and `OnAccessibilityAlert` are `UnityEvent`s. These are key for **event-driven communication** and **Observer pattern** integration. Any accessibility tool (like our `AccessibilityScreenReaderSimulator`) can subscribe to these events to be notified of changes without needing to directly query the UI.

3.  **`AccessibleButton` / `AccessibleSlider` (Concrete Implementations)**:
    *   These scripts are attached to standard Unity UI components (`Button`, `Slider`).
    *   They implement `IAccessibleElement`, providing specific details (Name, Role, Value, Hint) and behaviors (`OnAccessibilityActivate`, `OnAccessibilityIncrease`/`Decrease`).
    *   They visually indicate focus change (`OnAccessibilityFocusGained`/`Lost`) by changing their color, demonstrating how to provide feedback.
    *   They interact with the `AccessibilityManager` by calling `RegisterElement` and `UnregisterElement` in their `OnEnable`/`OnDisable` methods, ensuring they are always known to the system when active.

4.  **`AccessibilityScreenReaderSimulator` (Consumer)**:
    *   This component doesn't directly interact with the `Button` or `Slider` components.
    *   It subscribes to `AccessibilityManager.Instance.OnFocusedElementChanged` and `OnAccessibilityAlert`. This demonstrates how an external system consumes information from the `UIAccessibilitySystem` without direct knowledge of the underlying UI implementation.
    *   It translates keyboard input (Tab, Enter, Arrows) into generic commands like `FocusNextElement()` or `ActivateFocusedElement()`, which are then processed by the `AccessibilityManager`. This shows the manager as an **Interaction Proxy**.

This structured approach makes your UI more accessible, maintainable, and extensible. You can easily add new types of accessible UI elements by implementing `IAccessibleElement`, and new accessibility tools can interact with the `AccessibilityManager` without modifying your UI logic.