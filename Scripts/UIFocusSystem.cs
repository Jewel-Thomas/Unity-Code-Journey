// Unity Design Pattern Example: UIFocusSystem
// This script demonstrates the UIFocusSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `UIFocusSystem` design pattern provides a robust and flexible way to manage UI element focus, especially useful for keyboard and gamepad navigation. It centralizes focus logic, allowing individual UI elements to define their behavior when focused and how navigation works between them.

This example will create a complete `UIFocusSystem` that allows you to navigate between UI buttons using arrow keys or a gamepad D-pad, providing visual feedback for the focused element.

---

### **UIFocusSystem Pattern Explained**

1.  **`UIFocusSystem` (Manager - Singleton):**
    *   This is the central orchestrator. It's a singleton, meaning only one instance exists throughout the application.
    *   It tracks the `CurrentFocusedElement`.
    *   It listens for navigation input (e.g., Arrow Keys, D-Pad).
    *   When input occurs, it asks the `CurrentFocusedElement` which element is its neighbor in that direction.
    *   It then sets focus to the new element, triggering `OnFocusLost` on the old element and `OnFocusGained` on the new one.
    *   It can also trigger the "action" (e.g., button click) of the focused element when a "submit" input is detected.

2.  **`IFocusableUIElement` (Interface):**
    *   This interface defines the contract for any UI element that can participate in the focus system.
    *   It declares methods like `OnFocusGained()`, `OnFocusLost()`, and `GetNavigationTarget(FocusDirection direction)`.
    *   This promotes polymorphism: the `UIFocusSystem` doesn't care about the *concrete type* of the element, only that it adheres to this interface.

3.  **`FocusDirection` (Enum):**
    *   A simple enum to define the possible navigation directions (Up, Down, Left, Right).

4.  **`FocusableButton` (Concrete Implementation):**
    *   This is an example implementation of `IFocusableUIElement` specifically for a `UnityEngine.UI.Button`.
    *   It holds references to its "neighbors" in each direction (Up, Down, Left, Right) that are also `IFocusableUIElement`s.
    *   It implements `OnFocusGained()` and `OnFocusLost()` to provide visual feedback (e.g., changing color, scaling).
    *   It implements `GetNavigationTarget()` to return the pre-defined neighbor for a given direction.

---

### **Implementation Files**

Here are the C# scripts required for the `UIFocusSystem`. You should create them in your Unity project, preferably in a dedicated folder like `Assets/Scripts/UIFocusSystem`.

**1. `FocusDirection.cs`**
This enum defines the possible directions for UI navigation.

```csharp
// FocusDirection.cs
using System;

namespace UIFocusSystem
{
    /// <summary>
    /// Defines the possible directions for UI navigation within the Focus System.
    /// </summary>
    public enum FocusDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
```

**2. `IFocusableUIElement.cs`**
This interface defines the contract for any UI element that can participate in the `UIFocusSystem`.

```csharp
// IFocusableUIElement.cs
using UnityEngine; // Required for GameObject property

namespace UIFocusSystem
{
    /// <summary>
    /// Interface for any UI element that can gain or lose focus within the UIFocusSystem.
    /// </summary>
    public interface IFocusableUIElement
    {
        /// <summary>
        /// Gets the GameObject associated with this focusable UI element.
        /// Useful for external systems to interact with the GameObject directly (e.g., positioning, parenting).
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Called when this UI element gains focus.
        /// Implement visual feedback here (e.g., highlight, scale, change color).
        /// </summary>
        void OnFocusGained();

        /// <summary>
        /// Called when this UI element loses focus.
        /// Implement visual feedback reversal here (e.g., revert highlight, scale, color).
        /// </summary>
        void OnFocusLost();

        /// <summary>
        /// Returns the IFocusableUIElement that should be focused next based on the given direction.
        /// This is crucial for defining navigation paths.
        /// </summary>
        /// <param name="direction">The desired navigation direction.</param>
        /// <returns>The next IFocusableUIElement to focus, or null if no element is available in that direction.</returns>
        IFocusableUIElement GetNavigationTarget(FocusDirection direction);
    }
}
```

**3. `UIFocusSystem.cs`**
This is the core manager (a singleton) that handles input, focus transitions, and delegates to the `IFocusableUIElement`s.

```csharp
// UIFocusSystem.cs
using System;
using UnityEngine;
using UnityEngine.EventSystems; // Potentially useful if integrating with Unity's EventSystem, but not strictly required for focus management itself.

namespace UIFocusSystem
{
    /// <summary>
    /// The central manager for the UIFocusSystem design pattern.
    /// This singleton MonoBehaviour orchestrates focus changes, handles input for navigation,
    /// and provides an API for other scripts to interact with the focus state.
    /// </summary>
    public class UIFocusSystem : MonoBehaviour
    {
        // Singleton pattern: Ensures only one instance of UIFocusSystem exists.
        public static UIFocusSystem Instance { get; private set; }

        // Event for external listeners to react to focus changes.
        // Parameters: (oldFocusedElement, newFocusedElement)
        public event Action<IFocusableUIElement, IFocusableUIElement> OnFocusChanged;

        [Tooltip("The initial UI element that will receive focus when the system starts.")]
        [SerializeField]
        private MonoBehaviour _initialFocusElement; // Use MonoBehaviour to allow drag-and-drop IFocusableUIElement in Inspector.

        private IFocusableUIElement _currentFocusedElement;
        public IFocusableUIElement CurrentFocusedElement => _currentFocusedElement;

        private void Awake()
        {
            // Singleton enforcement
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple UIFocusSystem instances found. Destroying duplicate.", this);
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Set initial focus if specified.
            if (_initialFocusElement != null && _initialFocusElement is IFocusableUIElement initialFocus)
            {
                SetFocus(initialFocus);
            }
            else if (_initialFocusElement == null)
            {
                Debug.LogWarning("No initial focus element set for UIFocusSystem. Focus must be set manually or via another script.", this);
            }
            else
            {
                Debug.LogError($"_initialFocusElement '{_initialFocusElement.name}' does not implement IFocusableUIElement!", _initialFocusElement);
            }
        }

        private void Update()
        {
            HandleNavigationInput();
            HandleSubmitInput();
        }

        /// <summary>
        /// Sets the focus to a new IFocusableUIElement.
        /// Manages calling OnFocusLost on the old element and OnFocusGained on the new element.
        /// </summary>
        /// <param name="newFocusElement">The element to set focus to.</param>
        public void SetFocus(IFocusableUIElement newFocusElement)
        {
            if (newFocusElement == null)
            {
                Debug.LogWarning("Attempted to set focus to a null element. Clearing current focus instead.");
                ClearFocus();
                return;
            }

            if (_currentFocusedElement == newFocusElement)
            {
                // Already focused on this element, no change needed.
                return;
            }

            IFocusableUIElement oldFocusedElement = _currentFocusedElement;

            // Notify the old element that it has lost focus.
            if (oldFocusedElement != null)
            {
                oldFocusedElement.OnFocusLost();
            }

            _currentFocusedElement = newFocusElement;

            // Notify the new element that it has gained focus.
            _currentFocusedElement.OnFocusGained();

            // Optionally integrate with Unity's EventSystem for some compatibility
            // EventSystem.current?.SetSelectedGameObject(_currentFocusedElement.GameObject);

            // Invoke the focus changed event.
            OnFocusChanged?.Invoke(oldFocusedElement, _currentFocusedElement);

            // Debug log for clarity
            Debug.Log($"Focus changed from {(oldFocusedElement?.GameObject.name ?? "None")} to {_currentFocusedElement.GameObject.name}", _currentFocusedElement.GameObject);
        }

        /// <summary>
        /// Clears the current focus, notifying the previously focused element.
        /// </summary>
        public void ClearFocus()
        {
            if (_currentFocusedElement != null)
            {
                IFocusableUIElement oldFocusedElement = _currentFocusedElement;
                _currentFocusedElement.OnFocusLost();
                _currentFocusedElement = null;
                OnFocusChanged?.Invoke(oldFocusedElement, null);
                // EventSystem.current?.SetSelectedGameObject(null);
                Debug.Log("Focus cleared.");
            }
        }

        /// <summary>
        /// Handles input for navigation (Up, Down, Left, Right).
        /// </summary>
        private void HandleNavigationInput()
        {
            FocusDirection? direction = null;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetAxisRaw("Vertical") > 0.5f)
            {
                direction = FocusDirection.Up;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetAxisRaw("Vertical") < -0.5f)
            {
                direction = FocusDirection.Down;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetAxisRaw("Horizontal") < -0.5f)
            {
                direction = FocusDirection.Left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetAxisRaw("Horizontal") > 0.5f)
            {
                direction = FocusDirection.Right;
            }

            // Only attempt navigation if a direction was detected and the axis input wasn't held down (to prevent rapid navigation)
            if (direction.HasValue && !IsAxisHeld(direction.Value))
            {
                Navigate(direction.Value);
            }
        }

        // Helper to prevent rapid navigation when an axis is held
        private float _lastVerticalInput = 0f;
        private float _lastHorizontalInput = 0f;
        private bool IsAxisHeld(FocusDirection direction)
        {
            const float threshold = 0.5f;
            float currentVertical = Input.GetAxisRaw("Vertical");
            float currentHorizontal = Input.GetAxisRaw("Horizontal");

            bool held = false;
            if (direction == FocusDirection.Up || direction == FocusDirection.Down)
            {
                if (Mathf.Abs(currentVertical) > threshold && Mathf.Abs(_lastVerticalInput) > threshold && Mathf.Sign(currentVertical) == Mathf.Sign(_lastVerticalInput))
                {
                    held = true;
                }
                _lastVerticalInput = currentVertical;
            }
            else if (direction == FocusDirection.Left || direction == FocusDirection.Right)
            {
                if (Mathf.Abs(currentHorizontal) > threshold && Mathf.Abs(_lastHorizontalInput) > threshold && Mathf.Sign(currentHorizontal) == Mathf.Sign(_lastHorizontalInput))
                {
                    held = true;
                }
                _lastHorizontalInput = currentHorizontal;
            }
            
            return held; // This simplistic check might not be robust enough for all gamepads. A timer-based cooldown is often better.
        }


        /// <summary>
        /// Attempts to navigate focus in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to navigate.</param>
        private void Navigate(FocusDirection direction)
        {
            if (_currentFocusedElement == null)
            {
                // If nothing is currently focused, try to set focus to the initial element.
                if (_initialFocusElement != null && _initialFocusElement is IFocusableUIElement initialFocus)
                {
                    SetFocus(initialFocus);
                }
                return;
            }

            IFocusableUIElement nextElement = _currentFocusedElement.GetNavigationTarget(direction);

            if (nextElement != null)
            {
                SetFocus(nextElement);
            }
            else
            {
                Debug.Log($"No navigation target found for {_currentFocusedElement.GameObject.name} in direction {direction}.", _currentFocusedElement.GameObject);
            }
        }

        /// <summary>
        /// Handles input for submitting (activating) the currently focused element.
        /// </summary>
        private void HandleSubmitInput()
        {
            // Check for 'Submit' button (Enter/Space on keyboard, 'A' on Xbox, 'X' on PlayStation)
            if (Input.GetButtonDown("Submit"))
            {
                // If the current focused element is a FocusableButton, trigger its onClick event.
                // This demonstrates how the UIFocusSystem can interact with the focused element's specific functionality.
                if (_currentFocusedElement is FocusableButton focusableButton)
                {
                    focusableButton.SimulateClick();
                    Debug.Log($"Submit pressed on {focusableButton.GameObject.name}", focusableButton.GameObject);
                }
                else if (_currentFocusedElement != null)
                {
                    Debug.Log($"Submit pressed on {_currentFocusedElement.GameObject.name}, but it's not a FocusableButton. No click action taken.", _currentFocusedElement.GameObject);
                }
            }
        }
    }
}
```

**4. `FocusableButton.cs`**
This script implements `IFocusableUIElement` for standard Unity UI Buttons. It provides visual feedback and defines navigation neighbors in the Inspector.

```csharp
// FocusableButton.cs
using UnityEngine;
using UnityEngine.UI; // Required for Button and Graphic components
using UnityEngine.EventSystems; // Required for IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

namespace UIFocusSystem
{
    /// <summary>
    /// A concrete implementation of IFocusableUIElement specifically for a Unity UI Button.
    /// Manages its own visual state (highlight/normal) based on focus, defines navigation targets,
    /// and provides its GameObject for the UIFocusSystem.
    /// </summary>
    [RequireComponent(typeof(Button))] // Ensures this script is on a GameObject with a Button component
    public class FocusableButton : MonoBehaviour, IFocusableUIElement, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Navigation Neighbors")]
        [Tooltip("The element to focus when navigating Up from this button.")]
        [SerializeField] private MonoBehaviour _upNeighbor; // Using MonoBehaviour to allow drag-and-drop IFocusableUIElement
        [Tooltip("The element to focus when navigating Down from this button.")]
        [SerializeField] private MonoBehaviour _downNeighbor;
        [Tooltip("The element to focus when navigating Left from this button.")]
        [SerializeField] private MonoBehaviour _leftNeighbor;
        [Tooltip("The element to focus when navigating Right from this button.")]
        [SerializeField] private MonoBehaviour _rightNeighbor;

        [Header("Visual Feedback")]
        [Tooltip("The Graphic component (e.g., Image or Text) to change color for visual feedback.")]
        [SerializeField] private Graphic _targetGraphic;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _focusedColor = Color.yellow;
        [SerializeField] private Vector3 _normalScale = new Vector3(1f, 1f, 1f);
        [SerializeField] private Vector3 _focusedScale = new Vector3(1.1f, 1.1f, 1.1f);
        [SerializeField] private float _animationDuration = 0.1f;

        // Private fields
        private Button _buttonComponent;
        private Coroutine _scaleRoutine;
        private Coroutine _colorRoutine;

        // IFocusableUIElement implementation
        public GameObject GameObject => gameObject; // Simple getter for the associated GameObject

        public Button ButtonComponent => _buttonComponent; // Public getter for the Button component

        private void Awake()
        {
            _buttonComponent = GetComponent<Button>();
            if (_targetGraphic == null)
            {
                _targetGraphic = GetComponent<Graphic>(); // Try to get any Graphic if not explicitly set
            }
            // Ensure initial state is normal
            ApplyNormalState();
        }

        private void OnEnable()
        {
            // If this button is meant to be the initial focus, tell the system
            // This is one way to set initial focus if UIFocusSystem.Instance._initialFocusElement isn't used
            // Or you can have a separate script call UIFocusSystem.Instance.SetFocus(this) in Start()
            // For this example, _initialFocusElement on UIFocusSystem is the preferred way.
        }

        /// <summary>
        /// Called when this button gains focus from the UIFocusSystem.
        /// Applies focused visual state.
        /// </summary>
        public void OnFocusGained()
        {
            if (!_buttonComponent.interactable) return; // Don't highlight if not interactable

            // Stop any ongoing animation routines
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            if (_colorRoutine != null) StopCoroutine(_colorRoutine);

            // Start new animation routines for focused state
            _scaleRoutine = StartCoroutine(AnimateScale(transform.localScale, _focusedScale, _animationDuration));
            if (_targetGraphic != null)
            {
                _colorRoutine = StartCoroutine(AnimateColor(_targetGraphic.color, _focusedColor, _animationDuration));
            }
            
            // Optionally: Play a focus sound or animation
            // Debug.Log($"{name} gained focus!");
        }

        /// <summary>
        /// Called when this button loses focus from the UIFocusSystem.
        /// Reverts to normal visual state.
        /// </summary>
        public void OnFocusLost()
        {
            // Stop any ongoing animation routines
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            if (_colorRoutine != null) StopCoroutine(_colorRoutine);

            // Start new animation routines for normal state
            _scaleRoutine = StartCoroutine(AnimateScale(transform.localScale, _normalScale, _animationDuration));
            if (_targetGraphic != null)
            {
                _colorRoutine = StartCoroutine(AnimateColor(_targetGraphic.color, _normalColor, _animationDuration));
            }

            // Optionally: Play a lose focus sound or animation
            // Debug.Log($"{name} lost focus!");
        }

        /// <summary>
        /// Returns the appropriate neighbor element based on the requested direction.
        /// </summary>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>The IFocusableUIElement neighbor, or null if none is defined for that direction.</returns>
        public IFocusableUIElement GetNavigationTarget(FocusDirection direction)
        {
            MonoBehaviour targetMono = null;
            switch (direction)
            {
                case FocusDirection.Up: targetMono = _upNeighbor; break;
                case FocusDirection.Down: targetMono = _downNeighbor; break;
                case FocusDirection.Left: targetMono = _leftNeighbor; break;
                case FocusDirection.Right: targetMono = _rightNeighbor; break;
            }

            if (targetMono != null && targetMono is IFocusableUIElement focusableTarget && (targetMono as Component).gameObject.activeInHierarchy)
            {
                return focusableTarget;
            }
            return null;
        }

        /// <summary>
        /// Triggers the Button's onClick event. Called by UIFocusSystem when 'Submit' is pressed.
        /// </summary>
        public void SimulateClick()
        {
            if (_buttonComponent != null && _buttonComponent.interactable)
            {
                _buttonComponent.onClick.Invoke();
            }
        }

        // --- Visual Animation Helpers ---
        private System.Collections.IEnumerator AnimateScale(Vector3 startScale, Vector3 endScale, float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale;
            _scaleRoutine = null;
        }

        private System.Collections.IEnumerator AnimateColor(Color startColor, Color endColor, float duration)
        {
            if (_targetGraphic == null) yield break;

            float timer = 0f;
            while (timer < duration)
            {
                _targetGraphic.color = Color.Lerp(startColor, endColor, timer / duration);
                timer += Time.deltaTime;
                yield return null;
            }
            _targetGraphic.color = endColor;
            _colorRoutine = null;
        }

        private void ApplyNormalState()
        {
            transform.localScale = _normalScale;
            if (_targetGraphic != null)
            {
                _targetGraphic.color = _normalColor;
            }
        }
        
        // --- Input System Integration (Optional, for mouse/touch) ---
        // These methods ensure that if a user clicks or hovers with a mouse,
        // the UIFocusSystem updates its internal state accordingly.
        public void OnPointerEnter(PointerEventData eventData)
        {
            // If mouse hovers, set focus to this element
            if (UIFocusSystem.Instance != null && UIFocusSystem.Instance.CurrentFocusedElement != this)
            {
                UIFocusSystem.Instance.SetFocus(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // If mouse exits, only clear focus if this was the focused element
            // and no other element immediately takes focus (more complex to handle perfectly)
            // For simplicity, we just ensure it goes back to normal state.
            // UIFocusSystem.Instance.ClearFocus() might be too aggressive here.
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // If clicked with mouse, ensure it has focus and trigger its action
            if (UIFocusSystem.Instance != null)
            {
                UIFocusSystem.Instance.SetFocus(this);
                SimulateClick(); // Triggers the Unity Button's onClick event
            }
        }

        private void OnDisable()
        {
            // If this element becomes disabled, remove focus if it's currently focused.
            if (UIFocusSystem.Instance != null && UIFocusSystem.Instance.CurrentFocusedElement == this)
            {
                UIFocusSystem.Instance.ClearFocus();
            }
        }
    }
}
```

---

### **How to Use in Unity**

1.  **Create Scripts:**
    *   Save the four code blocks above into separate `.cs` files (e.g., `FocusDirection.cs`, `IFocusableUIElement.cs`, `UIFocusSystem.cs`, `FocusableButton.cs`) in your Unity project, preferably in a folder like `Assets/Scripts/UIFocusSystem`.

2.  **Setup the `UIFocusSystem` GameObject:**
    *   Create an empty GameObject in your scene (e.g., named "UIFocusSystemManager").
    *   Attach the `UIFocusSystem.cs` script to this GameObject.

3.  **Create a UI Canvas and Buttons:**
    *   In your scene, create a UI Canvas (`GameObject -> UI -> Canvas`).
    *   Add several UI Buttons to this Canvas (`GameObject -> UI -> Button`). Arrange them vertically or horizontally. For example, create "Button Up", "Button Middle", "Button Down".

4.  **Add `FocusableButton` to your UI Buttons:**
    *   For each Button you created, select it in the Hierarchy.
    *   Click "Add Component" in the Inspector and add `FocusableButton.cs`.
    *   You will see new sections: "Navigation Neighbors" and "Visual Feedback".

5.  **Configure `FocusableButton` Neighbors:**
    *   **Crucially**, for each `FocusableButton`, drag and drop other `FocusableButton` GameObjects into its `_upNeighbor`, `_downNeighbor`, `_leftNeighbor`, `_rightNeighbor` fields in the Inspector.
        *   Example: For "Button Middle":
            *   `_upNeighbor`: Drag "Button Up" here.
            *   `_downNeighbor`: Drag "Button Down" here.
        *   For "Button Up":
            *   `_downNeighbor`: Drag "Button Middle" here.
        *   For "Button Down":
            *   `_upNeighbor`: Drag "Button Middle" here.
    *   If a button doesn't have a neighbor in a certain direction, leave that field empty.

6.  **Configure `FocusableButton` Visual Feedback (Optional but Recommended):**
    *   On each `FocusableButton` component, assign an `Image` or `Text` component to the `_targetGraphic` field. (Usually, the Button's own `Image` component is suitable).
    *   Adjust `_normalColor`, `_focusedColor`, `_normalScale`, `_focusedScale` to your liking.

7.  **Set Initial Focus:**
    *   Select your "UIFocusSystemManager" GameObject.
    *   In its `UIFocusSystem` component, drag one of your `FocusableButton` GameObjects (e.g., "Button Middle") into the `_initialFocusElement` field. This button will be focused when the scene starts.

8.  **Add `EventSystem` (if not already present):**
    *   If you don't have one, create an `EventSystem` in your scene (`GameObject -> UI -> Event System`). The `FocusableButton` uses its `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerClickHandler` for mouse/touch interaction, which relies on the `EventSystem`.

9.  **Test:**
    *   Run the scene.
    *   The `_initialFocusElement` should instantly get a highlight/scale change.
    *   Use the **Arrow Keys** (Up, Down, Left, Right) or **WASD** to navigate between your buttons. Observe the focus changing visually.
    *   Press **Spacebar** or **Enter** (or Gamepad 'A'/'X' button) to "click" the focused button. You should see its `onClick` event trigger (e.g., a debug message if you've added one to the Button's `onClick` list).
    *   You can also use your mouse to click on a button, and it will immediately gain focus and trigger its action.

This comprehensive example provides a solid foundation for building complex, accessible UI navigation in your Unity projects using the `UIFocusSystem` pattern. You can extend it to support different types of focusable UI elements (sliders, toggles, custom controls) by creating new classes that implement the `IFocusableUIElement` interface.