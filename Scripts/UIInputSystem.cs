// Unity Design Pattern Example: UIInputSystem
// This script demonstrates the UIInputSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **UIInputSystem** design pattern in Unity using the new Input System package. This pattern centralizes input handling for UI elements, decoupling the raw input from the UI logic. This makes your UI more robust, scalable, and easier to manage across different input devices (keyboard, mouse, gamepad, touch).

**Key Principles of the UIInputSystem Pattern:**

1.  **Centralized Input Handling:** A single `UIInputSystem` script manages all relevant UI input.
2.  **Event-Driven Communication:** Instead of UI elements polling for input directly, the `UIInputSystem` translates raw input into higher-level UI events (e.g., `OnSubmitPressed`, `OnNavigateInput`). UI elements then subscribe to these events.
3.  **Decoupling:** UI components don't need to know *how* an action (like "Submit") is triggered (keyboard 'Enter', gamepad 'South Button', etc.). They only react to the `OnSubmitPressed` event.
4.  **Context-Awareness (Optional but common):** The system can enable/disable different input "action maps" based on the current UI state (e.g., "MainMenu" map active, "InGameOverlay" map active, "DialogBox" map active).
5.  **Extensibility:** Easily add new input actions or change bindings without modifying individual UI components.
6.  **New Input System Integration:** Leverages Unity's powerful `Input Actions` asset for flexible and device-agnostic input definitions.

---

### **Setup Instructions (Before using the script):**

1.  **Install Input System Package:**
    *   In Unity, go to `Window` -> `Package Manager`.
    *   Select `Unity Registry`.
    *   Search for "Input System" and install it.
    *   When prompted to enable the new input backend, click "Yes". Unity might restart.

2.  **Create an Input Actions Asset:**
    *   In your Unity Project window, right-click -> `Create` -> `Input Actions`.
    *   Name it `PlayerInputActions`.
    *   **This asset is crucial for defining your input bindings.** While the provided C# class `PlayerInputActions` is embedded for self-containment, in a real project, you would *generate* this C# class from the asset. For this example, you still need the *asset* to drag into the `UIInputSystem` component in the Inspector.

3.  **Configure `PlayerInputActions` Asset:**
    *   Double-click the `PlayerInputActions` asset to open the Input Action Editor.
    *   Click the `+` button next to `Action Maps` and name it `UI`.
    *   Inside the `UI` map, click the `+` button next to `Actions` to add the following actions with their bindings:

        *   **Action: `Submit` (Type: Button)**
            *   Binding 1: `<Keyboard>/enter`
            *   Binding 2: `<Gamepad>/buttonSouth` (e.g., 'A' on Xbox, 'X' on PlayStation)

        *   **Action: `Cancel` (Type: Button)**
            *   Binding 1: `<Keyboard>/escape`
            *   Binding 2: `<Gamepad>/buttonEast` (e.g., 'B' on Xbox, 'Circle' on PlayStation)

        *   **Action: `Navigate` (Type: Value, Control Type: Vector2)**
            *   Binding 1: `<Keyboard>/w`, `<Keyboard>/upArrow` (Up)
            *   Binding 2: `<Keyboard>/s`, `<Keyboard>/downArrow` (Down)
            *   Binding 3: `<Keyboard>/a`, `<Keyboard>/leftArrow` (Left)
            *   Binding 4: `<Keyboard>/d`, `<Keyboard>/rightArrow` (Right)
            *   Binding 5: `<Gamepad>/leftStick` (Add `Stick Deadzone` processor if desired)
            *   Binding 6: `<Gamepad>/dpad` (Add `Stick Deadzone` processor if desired)

    *   Click `Save Asset` in the top left of the Input Action Editor.

---

### **The Code:**

Here are three scripts. Create them in your Unity project:

1.  **`UIInputSystem.cs`**: The core of the pattern, a singleton that manages input actions and dispatches events.
2.  **`MenuButtonResponder.cs`**: An example script for a UI button that subscribes to the `Submit` event.
3.  **`MenuNavigationController.cs`**: An example script for a menu panel that handles navigation using `Navigate` events.

---

### 1. `UIInputSystem.cs`

This script is the central hub. It uses the `PlayerInputActions` generated class to read input and then exposes C# events that other UI components can subscribe to.

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// The UIInputSystem is a singleton that centralizes all UI-related input handling.
/// It uses Unity's new Input System to abstract input sources (keyboard, gamepad, touch)
/// and dispatches higher-level UI events that other components can subscribe to.
/// This decouples UI logic from raw input polling, making the UI more robust and flexible.
/// </summary>
public class UIInputSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static UIInputSystem Instance { get; private set; }

    // --- Input Actions Asset Reference ---
    // Assign your generated 'PlayerInputActions' asset here in the Inspector.
    [SerializeField] private PlayerInputActions inputActionsAsset;

    // --- Public Events for UI Actions ---
    // UI elements subscribe to these events to react to specific user input.
    public event Action OnSubmitPressed;
    public event Action OnCancelPressed;
    public event Action<Vector2> OnNavigateInput; // Vector2 for directional input (e.g., D-pad, joystick)

    // --- Private State for Navigation Debounce ---
    // Used to prevent rapid navigation when a key/stick is held down.
    private float navigationDebounceTimer = 0f;
    [SerializeField] private float navigationDebounceTime = 0.15f; // Time in seconds between navigation events.
    private Vector2 lastNavigationInput; // To track if the input direction has changed.

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the Singleton pattern to ensure only one instance exists.
    /// Initializes the Input Actions and subscribes to their performed events.
    /// </summary>
    private void Awake()
    {
        // Enforce Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple UIInputSystem instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads

        // Instantiate Input Actions if not already assigned (e.g., in editor)
        // In a real project, you would create the 'PlayerInputActions' asset and drag it here.
        // For this self-contained example, we ensure it's instantiated.
        if (inputActionsAsset == null)
        {
            Debug.LogWarning("UIInputSystem: 'PlayerInputActions' asset not assigned. Instantiating default. " +
                             "Please create an Input Actions asset in Unity and assign it for full control.");
            inputActionsAsset = new PlayerInputActions();
        }

        // --- Subscribe to Input Action Events ---
        // 'performed' callback triggers once when the action is completed (e.g., key pressed).
        inputActionsAsset.UI.Submit.performed += ctx => OnSubmitPressed?.Invoke();
        inputActionsAsset.UI.Cancel.performed += ctx => OnCancelPressed?.Invoke();

        // For navigation, we use both 'performed' and 'canceled' to detect sustained input
        // and also when the input is released. We'll handle debounce in Update.
        inputActionsAsset.UI.Navigate.performed += ctx => lastNavigationInput = ctx.ReadValue<Vector2>();
        inputActionsAsset.UI.Navigate.canceled += ctx => lastNavigationInput = Vector2.zero; // Clear input when released
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Enables the UI action map to start processing UI input.
    /// </summary>
    private void OnEnable()
    {
        inputActionsAsset.Enable();
        EnableUIMap(); // Ensure the UI action map is active
    }

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Disables the UI action map to stop processing UI input.
    /// </summary>
    private void OnDisable()
    {
        DisableUIMap(); // Disable the UI action map
        inputActionsAsset.Disable();
    }

    /// <summary>
    /// Called once per frame.
    /// Handles debouncing for continuous navigation input to prevent rapid menu selections.
    /// </summary>
    private void Update()
    {
        HandleNavigationDebounce();
    }

    /// <summary>
    /// Manages the debouncing logic for continuous navigation input.
    /// It ensures that `OnNavigateInput` is invoked at a controlled rate,
    /// rather than every frame the input is held down.
    /// </summary>
    private void HandleNavigationDebounce()
    {
        if (lastNavigationInput == Vector2.zero)
        {
            navigationDebounceTimer = 0f; // Reset timer when no input
            return;
        }

        // If a new distinct input direction is pressed, trigger immediately
        // and reset the timer for subsequent continuous input.
        if (navigationDebounceTimer == 0f ||
            (Mathf.Abs(lastNavigationInput.x) > 0.5f && Mathf.Abs(lastNavigationInput.x - inputActionsAsset.UI.Navigate.ReadValue<Vector2>().x) > 0.5f) ||
            (Mathf.Abs(lastNavigationInput.y) > 0.5f && Mathf.Abs(lastNavigationInput.y - inputActionsAsset.UI.Navigate.ReadValue<Vector2>().y) > 0.5f))
        {
            OnNavigateInput?.Invoke(lastNavigationInput);
            navigationDebounceTimer = navigationDebounceTime; // Start debounce timer
        }
        else
        {
            navigationDebounceTimer -= Time.deltaTime;
            if (navigationDebounceTimer <= 0f)
            {
                OnNavigateInput?.Invoke(lastNavigationInput);
                navigationDebounceTimer = navigationDebounceTime; // Reset timer for next continuous input
            }
        }
    }


    /// <summary>
    /// Enables the 'UI' action map. This is useful for context switching,
    /// e.g., when a menu is open, you might want UI input enabled but gameplay input disabled.
    /// </summary>
    public void EnableUIMap()
    {
        inputActionsAsset.UI.Enable();
        Debug.Log("UI Input Map Enabled.");
    }

    /// <summary>
    /// Disables the 'UI' action map.
    /// </summary>
    public void DisableUIMap()
    {
        inputActionsAsset.UI.Disable();
        Debug.Log("UI Input Map Disabled.");
    }

    /// <summary>
    /// Cleans up resources when the GameObject is destroyed.
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks, especially important for singletons
        inputActionsAsset.UI.Submit.performed -= ctx => OnSubmitPressed?.Invoke();
        inputActionsAsset.UI.Cancel.performed -= ctx => OnCancelPressed?.Invoke();
        inputActionsAsset.UI.Navigate.performed -= ctx => lastNavigationInput = ctx.ReadValue<Vector2>();
        inputActionsAsset.UI.Navigate.canceled -= ctx => lastNavigationInput = Vector2.zero;

        // Dispose of the Input Actions asset
        inputActionsAsset?.Dispose();
        inputActionsAsset = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Embedded PlayerInputActions Class ---
    // This is the C# class generated by Unity's Input System from your 'PlayerInputActions' asset.
    // It's included here directly to make this example self-contained and "drop-in ready".
    // In a real project, you would usually let Unity generate this into a separate file
    // and reference it, but for educational purposes, having it here simplifies setup.

    // To generate this yourself: Select your 'PlayerInputActions' asset in Project window,
    // check 'Generate C# Class' in the Inspector, and set 'Class Name' to PlayerInputActions.
    // Then copy the generated code into this section, replacing this placeholder.
    // For this example, a minimal version is provided.
    [Serializable]
    public class PlayerInputActions : IInputActionCollection2, IDisposable
    {
        public InputActionAsset asset { get; }
        public PlayerInputActions()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerInputActions"",
    ""maps"": [
        {
            ""name"": ""UI"",
            ""id"": ""e745f49d-3c2d-4277-96a8-6f68e4c760a9"",
            ""actions"": [
                {
                    ""name"": ""Submit"",
                    ""type"": ""Button"",
                    ""id"": ""2df1b9e0-2586-4e58-9cfd-1579d554a7ea"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Cancel"",
                    ""type"": ""Button"",
                    ""id"": ""52701e51-87a4-436f-8706-e7ddb508f7aa"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Navigate"",
                    ""type"": ""Value"",
                    ""id"": ""761f238a-0d85-4089-8d7d-5a6ae590632a"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""a132915b-e9b4-4b55-a0d0-e37456d956f7"",
                    ""path"": ""<Keyboard>/enter"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""96e838f3-85d0-4966-896c-b3b0d2679c37"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f6cf7057-010e-473d-82d2-8b1e4e3f3286"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a4457e3f-6323-45c1-ad03-9d8a5736783d"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""WASD"",
                    ""id"": ""57b282ed-38e2-4934-9333-e5170d767174"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""up"",
                    ""id"": ""93ee3099-2a9e-4e42-9993-97992437ae7f"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""down"",
                    ""id"": ""696ce751-c000-47b7-810a-372070c7e2c9"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""left"",
                    ""id"": ""25c341ce-6a6c-4977-8321-4f8021d157a4"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""right"",
                    ""id"": ""7619420f-b44e-4f7f-a63b-010260429f55"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Arrow Keys"",
                    ""id"": ""e0c5d57d-a2d0-4c17-a068-d0169d25e0c8"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""up"",
                    ""id"": ""27c44e99-f2e1-4543-98fe-c07a90b62e49"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""down"",
                    ""id"": ""96c21e7d-c9ea-47c1-a20c-7b9dd756b17c"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""left"",
                    ""id"": ""69006b52-2fb4-40ce-85fb-5b4d7ee76746"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""right"",
                    ""id"": ""a3f912e9-4467-4638-a159-86641b65e714"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KeyboardMouse"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""95066c1f-998f-4315-99d8-7b952a21e0dd"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": ""StickDeadzone"",
                    ""groups"": ""Gamepad"",
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e302ec24-d2e3-490c-80a5-83e9b119b932"",
                    ""path"": ""<Gamepad>/dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""KeyboardMouse"",
            ""bindingGroup"": ""KeyboardMouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Gamepad"",
            ""bindingGroup"": ""Gamepad"",
            ""devices"": [
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
            // Define Action Maps and Actions (minimal setup to match above JSON)
            m_UI = asset.FindActionMap("UI", throwIfNotFound: true);
            m_UI_Submit = m_UI.FindAction("Submit", throwIfNotFound: true);
            m_UI_Cancel = m_UI.FindAction("Cancel", throwIfNotFound: true);
            m_UI_Navigate = m_UI.FindAction("Navigate", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        public IEnumerable<InputBinding> bindings => asset.bindings;

        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        // UI Action Map
        private readonly InputActionMap m_UI;
        private IUIActions m_UIActionsCallbackInterface;
        private readonly InputAction m_UI_Submit;
        private readonly InputAction m_UI_Cancel;
        private readonly InputAction m_UI_Navigate;

        public struct UIActions
        {
            private @PlayerInputActions m_Wrapper;
            public UIActions(@PlayerInputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @Submit => m_Wrapper.m_UI_Submit;
            public InputAction @Cancel => m_Wrapper.m_UI_Cancel;
            public InputAction @Navigate => m_Wrapper.m_UI_Navigate;
            public InputActionMap Get() { return m_Wrapper.m_UI; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(UIActions set) { return set.Get(); }
            public void SetCallbacks(IUIActions instance)
            {
                if (m_Wrapper.m_UIActionsCallbackInterface != null)
                {
                    @Submit.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnSubmit;
                    @Cancel.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnCancel;
                    @Navigate.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnNavigate;
                }
                m_Wrapper.m_UIActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @Submit.performed += instance.OnSubmit;
                    @Cancel.performed += instance.OnCancel;
                    @Navigate.performed += instance.OnNavigate;
                }
            }
        }
        public UIActions @UI => new UIActions(this);
        private int m_KeyboardMouseSchemeIndex = -1;
        public InputControlScheme KeyboardMouseScheme
        {
            get
            {
                if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("KeyboardMouse");
                return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
            }
        }
        private int m_GamepadSchemeIndex = -1;
        public InputControlScheme GamepadScheme
        {
            get
            {
                if (m_GamepadSchemeIndex == -1) m_GamepadSchemeIndex = asset.FindControlSchemeIndex("Gamepad");
                return asset.controlSchemes[m_GamepadSchemeIndex];
            }
        }
        public interface IUIActions
        {
            void OnSubmit(InputAction.CallbackContext context);
            void OnCancel(InputAction.CallbackContext context);
            void OnNavigate(InputAction.CallbackContext context);
        }
    }
}
```

---

### 2. `MenuButtonResponder.cs`

This script is an example of a UI element that listens for the `OnSubmitPressed` event from `UIInputSystem`.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button and Image components

/// <summary>
/// Example script demonstrating how a UI element (like a button) subscribes
/// to events from the centralized UIInputSystem.
/// When 'Submit' is pressed, this button reacts without knowing the input source.
/// </summary>
[RequireComponent(typeof(Button))] // Ensures there's a Button component on the GameObject
public class MenuButtonResponder : MonoBehaviour
{
    [Tooltip("The name of this button for logging purposes.")]
    [SerializeField] private string buttonName = "Default Button";

    [Tooltip("Color to show when the button is 'pressed' via input system.")]
    [SerializeField] private Color highlightColor = Color.yellow;

    [Tooltip("Original color of the button.")]
    [SerializeField] private Color originalColor = Color.white;

    private Button uiButton;
    private Image buttonImage;

    private void Awake()
    {
        uiButton = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage.color; // Store the initial color
    }

    /// <summary>
    /// Subscribes to the UIInputSystem's OnSubmitPressed event when enabled.
    /// This is where the decoupling happens: the button doesn't poll input,
    /// it just reacts to the system's event.
    /// </summary>
    private void OnEnable()
    {
        // Ensure the UIInputSystem exists before subscribing
        if (UIInputSystem.Instance != null)
        {
            UIInputSystem.Instance.OnSubmitPressed += HandleSubmit;
            Debug.Log($"'{buttonName}' subscribed to UIInputSystem.OnSubmitPressed.");
        }
        else
        {
            Debug.LogError("UIInputSystem.Instance is null. Make sure it exists in the scene and is initialized.");
        }
    }

    /// <summary>
    /// Unsubscribes from the event when disabled to prevent memory leaks.
    /// Crucial for proper event management.
    /// </summary>
    private void OnDisable()
    {
        if (UIInputSystem.Instance != null)
        {
            UIInputSystem.Instance.OnSubmitPressed -= HandleSubmit;
            Debug.Log($"'{buttonName}' unsubscribed from UIInputSystem.OnSubmitPressed.");
        }
    }

    /// <summary>
    /// This method is called when the UIInputSystem dispatches the OnSubmitPressed event.
    /// It performs the button's action (invokes its OnClick and provides visual feedback).
    /// </summary>
    private void HandleSubmit()
    {
        // Only react if this button is interactable and currently selected/focused by the navigation system.
        // For simplicity, this example just checks interactable and shows visual feedback.
        // In a full system, you'd check if this is the currently "focused" UI element.
        if (uiButton.interactable)
        {
            Debug.Log($"'{buttonName}' received Submit event. Triggering OnClick action.");
            uiButton.onClick.Invoke(); // Trigger the standard Unity Button's action

            // Provide visual feedback
            FlashColor(highlightColor);
        }
    }

    /// <summary>
    /// Flashes the button's color briefly.
    /// </summary>
    private void FlashColor(Color color)
    {
        if (buttonImage != null)
        {
            buttonImage.color = color;
            Invoke(nameof(ResetColor), 0.15f); // Reset after a short delay
        }
    }

    /// <summary>
    /// Resets the button's color to its original state.
    /// </summary>
    private void ResetColor()
    {
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
    }

    /// <summary>
    /// Example method that would be assigned to the Unity Button's OnClick() event,
    /// showing what happens when the button is "pressed".
    /// </summary>
    public void OnButtonPressed()
    {
        Debug.Log($"Action for '{buttonName}' was performed!");
        // Add specific logic for this button here (e.g., LoadScene, OpenPanel, etc.)
    }
}
```

---

### 3. `MenuNavigationController.cs`

This script demonstrates how to handle menu navigation using `OnNavigateInput` events, allowing selection of different UI elements.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for Selectable (Button, Toggle, Slider, etc.)

/// <summary>
/// Example script for a menu panel that handles navigation between selectable UI elements
/// using events from the UIInputSystem. This demonstrates more complex event subscription
/// and state management (current selection).
/// </summary>
public class MenuNavigationController : MonoBehaviour
{
    [Tooltip("List of selectable UI elements in this menu.")]
    [SerializeField] private List<Selectable> menuItems;

    [Tooltip("Visual color for the currently selected item.")]
    [SerializeField] private Color selectedColor = Color.cyan;

    private int currentSelectionIndex = 0;
    private Selectable currentSelectedItem;

    private void OnEnable()
    {
        // Ensure UIInputSystem is available before subscribing
        if (UIInputSystem.Instance != null)
        {
            UIInputSystem.Instance.OnNavigateInput += HandleNavigationInput;
            UIInputSystem.Instance.OnSubmitPressed += HandleSubmitSelection; // Also listen for submit to activate selected item
            Debug.Log("MenuNavigationController subscribed to UIInputSystem events.");

            // Initialize selection if there are items
            if (menuItems != null && menuItems.Count > 0)
            {
                currentSelectionIndex = 0;
                SelectMenuItem(currentSelectionIndex);
            }
        }
        else
        {
            Debug.LogError("UIInputSystem.Instance is null. Make sure it exists in the scene and is initialized.");
        }
    }

    private void OnDisable()
    {
        if (UIInputSystem.Instance != null)
        {
            UIInputSystem.Instance.OnNavigateInput -= HandleNavigationInput;
            UIInputSystem.Instance.OnSubmitPressed -= HandleSubmitSelection;
            Debug.Log("MenuNavigationController unsubscribed from UIInputSystem events.");
        }

        // Clear selection visual when menu is disabled
        if (currentSelectedItem != null && currentSelectedItem.TryGetComponent(out Image img))
        {
            img.color = Color.white; // Reset to default color
        }
    }

    /// <summary>
    /// Handles navigation input received from the UIInputSystem.
    /// Moves the selection up/down/left/right based on the input vector.
    /// </summary>
    /// <param name="input">The normalized 2D input vector (e.g., from D-pad or stick).</param>
    private void HandleNavigationInput(Vector2 input)
    {
        if (menuItems == null || menuItems.Count == 0) return;

        int newIndex = currentSelectionIndex;

        // Vertical Navigation
        if (input.y > 0.5f) // Up
        {
            newIndex--;
        }
        else if (input.y < -0.5f) // Down
        {
            newIndex++;
        }
        // Horizontal Navigation (for more complex grids, you'd use a 2D array or more advanced logic)
        // For a simple list, horizontal usually isn't handled or wraps.
        else if (input.x > 0.5f) // Right
        {
            // For a linear list, maybe move to the next item or wrap
            newIndex++;
        }
        else if (input.x < -0.5f) // Left
        {
            // For a linear list, maybe move to the previous item or wrap
            newIndex--;
        }
        else
        {
            // No significant directional input for navigation, do nothing.
            return;
        }

        // Clamp and wrap the index
        if (newIndex < 0) newIndex = menuItems.Count - 1;
        else if (newIndex >= menuItems.Count) newIndex = 0;

        // Only update if the selection has actually changed
        if (newIndex != currentSelectionIndex)
        {
            SelectMenuItem(newIndex);
        }
    }

    /// <summary>
    /// Updates the currently selected menu item, providing visual feedback.
    /// </summary>
    /// <param name="index">The index of the item to select.</param>
    private void SelectMenuItem(int index)
    {
        if (menuItems == null || index < 0 || index >= menuItems.Count) return;

        // Reset previous item's color if it was an Image
        if (currentSelectedItem != null && currentSelectedItem.TryGetComponent(out Image prevImage))
        {
            prevImage.color = Color.white; // Or originalColor if stored
        }

        currentSelectionIndex = index;
        currentSelectedItem = menuItems[currentSelectionIndex];

        // Highlight new item if it has an Image component
        if (currentSelectedItem != null && currentSelectedItem.TryGetComponent(out Image newImage))
        {
            newImage.color = selectedColor;
        }

        Debug.Log($"Menu selection changed to: {currentSelectedItem.gameObject.name}");
        // You might want to play an audio cue here
    }

    /// <summary>
    /// Handles the 'Submit' event from the UIInputSystem to activate the currently selected item.
    /// </summary>
    private void HandleSubmitSelection()
    {
        if (currentSelectedItem != null && currentSelectedItem.interactable)
        {
            // Simulate a click on the selected item
            currentSelectedItem.Select(); // Makes the UI element "selected" by Unity's EventSystem
            if (currentSelectedItem is Button button)
            {
                button.onClick.Invoke(); // Trigger the button's action
            }
            else if (currentSelectedItem is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn; // Toggle its state
            }
            // Add more types of Selectable if needed (Slider, Dropdown, etc.)

            Debug.Log($"Submit pressed on selected item: {currentSelectedItem.gameObject.name}");
        }
    }
}
```

---

### **Unity Scene Setup:**

1.  **Create an Empty GameObject for the UI Input System:**
    *   In your scene, right-click -> `Create Empty`.
    *   Rename it `_UIInputSystem`.
    *   Attach the `UIInputSystem.cs` script to it.
    *   Drag your `PlayerInputActions` asset (created in Step 2 of setup) into the `Input Actions Asset` field of the `UIInputSystem` component in the Inspector.

2.  **Create a UI Canvas:**
    *   Right-click in Hierarchy -> `UI` -> `Canvas`.
    *   Set its `Render Mode` to `Screen Space - Camera` and assign your `Main Camera`.
    *   Set `UI Scale Mode` to `Scale With Screen Size` (recommended for responsive UI).

3.  **Create UI Buttons:**
    *   Inside the Canvas, right-click -> `UI` -> `Button - Text (TMP)` (Requires TextMeshPro package import).
    *   Duplicate this button a few times (e.g., Button1, Button2, Button3).
    *   Position them nicely (e.g., vertically stacked).
    *   For each Button:
        *   Attach the `MenuButtonResponder.cs` script.
        *   In the Inspector, set `Button Name` (e.g., "Start Game Button", "Options Button", "Quit Button").
        *   In the `Button` component (Unity's built-in), under `On Click()`, add a new event.
        *   Drag the button's GameObject itself into the object slot.
        *   Select `MenuButtonResponder` -> `OnButtonPressed()` from the function dropdown.

4.  **Create a Menu Manager GameObject:**
    *   In the Canvas (or a new empty GameObject), right-click -> `Create Empty`.
    *   Rename it `Menu Panel` (or `MainMenu`).
    *   Attach the `MenuNavigationController.cs` script to it.
    *   In the `Menu Navigation Controller` component's `Menu Items` list, drag your `Button1`, `Button2`, `Button3` GameObjects from the Hierarchy into the slots. The order matters for navigation!

5.  **Test the Scene:**
    *   Run the scene.
    *   You should see your buttons.
    *   **Press `Enter` or Gamepad `South Button` (A/X) to simulate clicking the currently selected button.** You'll see logs from `MenuButtonResponder`.
    *   **Use `W`/`S`/`Up Arrow`/`Down Arrow` or Gamepad `Left Stick`/`D-Pad` Up/Down to navigate between buttons.** The `MenuNavigationController` will change the highlight color and log the selection.
    *   **Press `Escape` or Gamepad `East Button` (B/Circle) to trigger the `Cancel` event** (you'd normally have a component listening to this for closing menus, etc.).

---

### **How this demonstrates the UIInputSystem Pattern:**

*   **`UIInputSystem.cs`**: This is the 'System' part. It's a central authority for UI input. It doesn't care *what* UI elements exist, only that it needs to listen for "Submit", "Cancel", and "Navigate" actions and notify anyone interested.
*   **`MenuButtonResponder.cs`**: This is a 'UI Element' part. It doesn't poll `Input.GetKeyDown(KeyCode.Return)`. Instead, it says, "Hey `UIInputSystem`, let me know when someone 'Submits'!" This makes it highly decoupled. If you change the 'Submit' key from `Enter` to `Space`, this script doesn't need to change at all.
*   **`MenuNavigationController.cs`**: This is a more complex 'UI Element/Controller' part. It reacts to navigation input and manages the state of which item is currently selected, providing a full navigation experience without directly touching raw input.
*   **New Input System**: The `PlayerInputActions` asset and generated class allow you to easily configure bindings for keyboard, mouse, gamepad, and touch, and the `UIInputSystem` automatically benefits from this flexibility.

This structure allows for a clean separation of concerns, making your UI input logic maintainable, testable, and adaptable to future changes in input requirements or device support.