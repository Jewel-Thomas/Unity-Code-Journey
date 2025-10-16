// Unity Design Pattern Example: PuzzleInteractionSystem
// This script demonstrates the PuzzleInteractionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The `PuzzleInteractionSystem` design pattern in Unity helps manage complex puzzles composed of multiple interactive elements. It centralizes the logic for determining overall puzzle completion, allowing individual puzzle parts to focus solely on their own state and interaction.

This pattern promotes:
*   **Modularity:** Each puzzle element is a self-contained unit.
*   **Extensibility:** Easily add new puzzle types by implementing a common interface.
*   **Decoupling:** Puzzle elements don't need to know about each other or the final outcome; they just report their state to a central manager.
*   **Scalability:** Manages any number of puzzle elements.

---

### Core Components of the Pattern:

1.  **`IPuzzleElement` (Interface):** Defines the contract for any object that can be part of a puzzle. It specifies properties like `IsSolved` and an event `OnPuzzleElementStateChanged` to notify listeners when its state changes. It also includes a `ResetElement` method.
2.  **`PuzzleManager` (Central Controller):** A singleton or globally accessible manager that:
    *   Keeps track of all `IPuzzleElement` instances registered with it.
    *   Subscribes to each element's `OnPuzzleElementStateChanged` event.
    *   Whenever an element's state changes, it checks if all registered elements are `IsSolved`.
    *   Invokes its own `OnPuzzleSolved` or `OnPuzzleUnsolved` event when the overall puzzle's state changes.
    *   Can trigger a reset for all elements.
3.  **Concrete `IPuzzleElement` Implementations:** These are actual scripts for individual puzzle parts (e.g., a button, a lever, a dial). They handle player interaction, update their internal `IsSolved` state, and invoke `OnPuzzleElementStateChanged` when appropriate.
4.  **`PuzzleConsequence` (e.g., `PuzzleDoor`):** A script that subscribes to the `PuzzleManager`'s `OnPuzzleSolved` event to perform an action (e.g., open a door, enable a new path, play an animation) when the entire puzzle is completed.

---

### Complete Unity Example

Below are the C# scripts that implement this pattern, along with instructions on how to set them up in a Unity project.

#### 1. `IPuzzleElement.cs` (Interface)

This interface defines what every puzzle piece must provide.

```csharp
// File: Assets/Scripts/PuzzleSystem/IPuzzleElement.cs
using UnityEngine;
using System;

/// <summary>
/// Interface for any object that wants to be considered a part of a larger puzzle.
/// Implementing classes will represent individual, interactive puzzle components.
/// </summary>
public interface IPuzzleElement
{
    /// <summary>
    /// Gets a value indicating whether this individual puzzle element is currently in its 'solved' state.
    /// </summary>
    bool IsSolved { get; }

    /// <summary>
    /// Event that concrete puzzle elements invoke whenever their 'IsSolved' state might have changed.
    /// The PuzzleManager subscribes to this event to re-evaluate the overall puzzle completion.
    /// </summary>
    event Action OnPuzzleElementStateChanged;

    /// <summary>
    /// Resets the puzzle element to its initial, unsolved state.
    /// This allows the PuzzleManager or other systems to reset the entire puzzle.
    /// </summary>
    void ResetElement();
}
```

#### 2. `PuzzleManager.cs` (Central Controller)

This script manages all puzzle elements and determines when the main puzzle is solved.

```csharp
// File: Assets/Scripts/PuzzleSystem/PuzzleManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // Required for .All() and .Count() extension methods

/// <summary>
/// The central manager for a multi-part puzzle system.
/// It tracks the state of multiple IPuzzleElement components and triggers events
/// when the entire puzzle is solved or unsolved.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    // Singleton pattern for easy global access to the PuzzleManager.
    public static PuzzleManager Instance { get; private set; }

    [Tooltip("Assign all GameObjects that implement IPuzzleElement here. " +
             "The manager will automatically find their IPuzzleElement components.")]
    [SerializeField] private List<GameObject> puzzleElementGameObjects = new List<GameObject>();

    private List<IPuzzleElement> _puzzleElements = new List<IPuzzleElement>();

    /// <summary>
    /// Event invoked when the entire puzzle (all registered elements) transitions to a solved state.
    /// Other systems (e.g., a door, a new area) can subscribe to this.
    /// </summary>
    public event Action OnPuzzleSolved;

    /// <summary>
    /// Event invoked when the entire puzzle transitions from a solved state back to an unsolved state.
    /// This can happen if a solved element is reset or interacted with incorrectly.
    /// </summary>
    public event Action OnPuzzleUnsolved;

    /// <summary>
    /// Gets a value indicating whether the entire puzzle is currently in its solved state.
    /// </summary>
    public bool IsPuzzleCurrentlySolved { get; private set; }

    private void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PuzzleManagers detected! Destroying the duplicate GameObject.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePuzzleElements();
    }

    /// <summary>
    /// Initializes the puzzle elements by finding the IPuzzleElement components
    /// on the assigned GameObjects and subscribing to their state change events.
    /// </summary>
    private void InitializePuzzleElements()
    {
        _puzzleElements.Clear(); // Clear any previous references
        foreach (GameObject go in puzzleElementGameObjects)
        {
            if (go == null)
            {
                Debug.LogWarning($"A null GameObject was assigned in PuzzleManager's 'Puzzle Element Game Objects' list. Skipping.", this);
                continue;
            }

            IPuzzleElement element = go.GetComponent<IPuzzleElement>();
            if (element != null)
            {
                RegisterPuzzleElement(element);
            }
            else
            {
                Debug.LogWarning($"GameObject '{go.name}' assigned to PuzzleManager does not have a component implementing IPuzzleElement. Please check its script.", go);
            }
        }

        // Perform an initial check in case all elements are already solved on scene start.
        CheckPuzzleCompletion();
    }

    /// <summary>
    /// Registers an individual puzzle element with the manager.
    /// This method is called internally during initialization, but can also be used
    /// by dynamically created puzzle elements.
    /// </summary>
    /// <param name="element">The IPuzzleElement to register.</param>
    public void RegisterPuzzleElement(IPuzzleElement element)
    {
        if (element == null || _puzzleElements.Contains(element)) return;

        _puzzleElements.Add(element);
        // Subscribe to the element's state change event.
        // This is crucial for the PuzzleManager to know when to re-evaluate the overall puzzle state.
        element.OnPuzzleElementStateChanged += OnElementStateChanged;
        Debug.Log($"PuzzleManager: Registered puzzle element: {((MonoBehaviour)element).name}");
    }

    /// <summary>
    /// Unregisters a puzzle element from the manager.
    /// Important for dynamically removed elements or scene cleanup.
    /// </summary>
    /// <param name="element">The IPuzzleElement to unregister.</param>
    public void UnregisterPuzzleElement(IPuzzleElement element)
    {
        if (element == null) return;

        if (_puzzleElements.Contains(element))
        {
            element.OnPuzzleElementStateChanged -= OnElementStateChanged;
            _puzzleElements.Remove(element);
            Debug.Log($"PuzzleManager: Unregistered puzzle element: {((MonoBehaviour)element).name}");
            // Re-check completion as removing an element might change the overall state if it was solved.
            CheckPuzzleCompletion();
        }
    }

    /// <summary>
    /// This callback is executed every time ANY registered puzzle element changes its solved state.
    /// It then triggers a re-evaluation of the entire puzzle's completion status.
    /// </summary>
    private void OnElementStateChanged()
    {
        CheckPuzzleCompletion();
    }

    /// <summary>
    /// Evaluates if all registered puzzle elements are currently in their 'solved' state.
    /// If the overall puzzle state changes, it invokes the appropriate events.
    /// </summary>
    private void CheckPuzzleCompletion()
    {
        if (_puzzleElements.Count == 0)
        {
            // If there are no elements, the puzzle cannot be solved.
            // If it was previously solved, trigger unsolved event.
            if (IsPuzzleCurrentlySolved)
            {
                IsPuzzleCurrentlySolved = false;
                OnPuzzleUnsolved?.Invoke();
                Debug.Log("PuzzleManager: Puzzle is now unsolved (no elements currently registered).");
            }
            return;
        }

        // Use LINQ's .All() method for a concise check if all elements are solved.
        bool allElementsAreSolved = _puzzleElements.All(element => element.IsSolved);

        // Check for state transition: Unsolved -> Solved
        if (allElementsAreSolved && !IsPuzzleCurrentlySolved)
        {
            IsPuzzleCurrentlySolved = true;
            OnPuzzleSolved?.Invoke(); // Trigger the global puzzle solved event
            Debug.Log("****** PuzzleManager: ENTIRE PUZZLE SOLVED! ******");
        }
        // Check for state transition: Solved -> Unsolved (e.g., an element was reset)
        else if (!allElementsAreSolved && IsPuzzleCurrentlySolved)
        {
            IsPuzzleCurrentlySolved = false;
            OnPuzzleUnsolved?.Invoke(); // Trigger the global puzzle unsolved event
            Debug.Log("PuzzleManager: Puzzle is no longer solved (an element became unsolved).");
        }
        else if (!IsPuzzleCurrentlySolved)
        {
            // If still unsolved, provide progress feedback.
            Debug.Log($"PuzzleManager: Puzzle still unsolved. Solved elements: {_puzzleElements.Count(e => e.IsSolved)}/{_puzzleElements.Count}");
        }
    }

    /// <summary>
    /// Resets all registered puzzle elements to their initial unsolved state.
    /// This is useful for replaying a puzzle or handling player mistakes.
    /// </summary>
    public void ResetAllPuzzleElements()
    {
        Debug.Log("PuzzleManager: Resetting all puzzle elements...");
        foreach (IPuzzleElement element in _puzzleElements)
        {
            element.ResetElement(); // Call the ResetElement method defined in the interface.
        }
        // The CheckPuzzleCompletion will be called by each element's OnPuzzleElementStateChanged event.
        // However, a final check here ensures consistency if some elements don't trigger the event on reset.
        CheckPuzzleCompletion();
    }

    private void OnDestroy()
    {
        // Important: Unsubscribe from all element events to prevent memory leaks
        // and potential null reference exceptions if elements are destroyed before the manager.
        foreach (IPuzzleElement element in _puzzleElements)
        {
            if (element != null) // Check if the element object itself hasn't been destroyed yet.
            {
                element.OnPuzzleElementStateChanged -= OnElementStateChanged;
            }
        }
        // Clear the static instance reference if this manager is being destroyed.
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

#### 3. Concrete Puzzle Elements (Examples)

##### `PuzzleButtonSequence.cs`

A puzzle where a series of buttons must be pressed in a specific order.

```csharp
// File: Assets/Scripts/PuzzleSystem/PuzzleButtonSequence.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq; // For .Where()

/// <summary>
/// A concrete implementation of IPuzzleElement.
/// This puzzle requires a specific sequence of buttons to be pressed in order to be solved.
/// </summary>
public class PuzzleButtonSequence : MonoBehaviour, IPuzzleElement
{
    [Header("Puzzle Element Settings")]
    [Tooltip("Is this individual element currently in its solved state?")]
    [SerializeField] private bool _isSolved = false;
    public bool IsSolved => _isSolved; // IPuzzleElement implementation for checking solved status

    // Event required by IPuzzleElement. Invoked when _isSolved changes.
    public event Action OnPuzzleElementStateChanged;

    [Header("Button Sequence Settings")]
    [Tooltip("The GameObjects representing the buttons. Each needs a Collider for interaction.")]
    [SerializeField] private List<GameObject> buttons = new List<GameObject>();
    [Tooltip("The correct 0-based index sequence of buttons to press (e.g., 0, 1, 2).")]
    [SerializeField] private int[] correctSequence = { 0, 1, 2 };

    private List<int> _currentInputSequence = new List<int>();

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material pressedMaterial;
    [SerializeField] private Material correctMaterial;
    [SerializeField] private Material incorrectMaterial;

    private List<MeshRenderer> _buttonRenderers = new List<MeshRenderer>();

    private void Awake()
    {
        if (buttons.Count == 0)
        {
            Debug.LogWarning($"PuzzleButtonSequence on {gameObject.name} has no buttons assigned. This puzzle element will not function.", this);
            return;
        }

        // Initialize each button for interaction and visual feedback.
        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject buttonGO = buttons[i];
            if (buttonGO == null)
            {
                Debug.LogWarning($"Button at index {i} is null in '{gameObject.name}'. Skipping.", this);
                continue;
            }

            // Ensure a collider for mouse interaction (OnMouseDown).
            if (buttonGO.GetComponent<Collider>() == null)
            {
                buttonGO.AddComponent<BoxCollider>().isTrigger = true; // Use trigger for easier detection
            }

            // Attach a helper script to each button to handle click events.
            PuzzleButtonTrigger buttonTrigger = buttonGO.GetComponent<PuzzleButtonTrigger>();
            if (buttonTrigger == null)
            {
                buttonTrigger = buttonGO.AddComponent<PuzzleButtonTrigger>();
            }
            // Initialize the trigger, passing a reference to this puzzle and its unique index.
            buttonTrigger.Initialize(this, i);

            // Get renderer for visual feedback.
            MeshRenderer renderer = buttonGO.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = buttonGO.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                _buttonRenderers.Add(renderer);
            }
            else
            {
                _buttonRenderers.Add(null); // Keep list aligned even if no renderer
                Debug.LogWarning($"Button '{buttonGO.name}' at index {i} has no MeshRenderer. Visual feedback will be limited.", buttonGO);
            }
        }

        ResetElement(); // Ensure the puzzle starts in an unsolved state.
    }

    /// <summary>
    /// Called by an individual PuzzleButtonTrigger when its button is pressed.
    /// This method processes the player's input sequence.
    /// </summary>
    /// <param name="buttonIndex">The 0-based index of the button that was pressed.</param>
    public void OnButtonPressed(int buttonIndex)
    {
        if (_isSolved) return; // Ignore input if the puzzle is already solved.

        Debug.Log($"Button {buttonIndex} pressed on '{gameObject.name}'.");
        _currentInputSequence.Add(buttonIndex);
        ApplyFeedback(buttonIndex, pressedMaterial); // Show immediate feedback for the pressed button.

        // Check if the current input sequence is a valid prefix of the correct sequence.
        bool currentInputValid = true;
        for (int i = 0; i < _currentInputSequence.Count; i++)
        {
            if (i >= correctSequence.Length || _currentInputSequence[i] != correctSequence[i])
            {
                currentInputValid = false;
                break;
            }
        }

        if (!currentInputValid)
        {
            // Incorrect button pressed or sequence too long.
            Debug.Log("Sequence incorrect: Wrong button or too many buttons pressed.");
            ApplyIncorrectFeedback();
            Invoke(nameof(ResetInput), 0.5f); // Reset input after a short delay for visual feedback.
        }
        else if (_currentInputSequence.Count == correctSequence.Length)
        {
            // Sequence is complete and correct!
            Debug.Log("Correct sequence entered!");
            SetSolved(true); // Mark this element as solved.
            ApplyCorrectFeedback();
        }
        else
        {
            // Sequence is a correct prefix, but not yet complete.
            Debug.Log("Sequence partially correct. Continue...");
        }
    }

    /// <summary>
    /// Sets the solved state of this puzzle element and invokes the state change event if the state changed.
    /// </summary>
    /// <param name="solved">The new solved state.</param>
    private void SetSolved(bool solved)
    {
        if (_isSolved != solved)
        {
            _isSolved = solved;
            OnPuzzleElementStateChanged?.Invoke(); // Notify the PuzzleManager.
        }
    }

    /// <summary>
    /// Applies a material to a specific button's renderer.
    /// </summary>
    /// <param name="buttonIndex">The index of the button.</param>
    /// <param name="material">The material to apply.</param>
    private void ApplyFeedback(int buttonIndex, Material material)
    {
        if (_buttonRenderers.Count > buttonIndex && _buttonRenderers[buttonIndex] != null && material != null)
        {
            _buttonRenderers[buttonIndex].material = material;
        }
    }

    /// <summary>
    /// Applies incorrect feedback to all buttons and schedules a visual reset.
    /// </summary>
    private void ApplyIncorrectFeedback()
    {
        foreach (MeshRenderer renderer in _buttonRenderers.Where(r => r != null))
        {
            if (incorrectMaterial != null) renderer.material = incorrectMaterial;
        }
        Invoke(nameof(ResetVisuals), 0.5f); // Briefly show incorrect state, then reset visuals.
    }

    /// <summary>
    /// Applies correct feedback to all buttons.
    /// </summary>
    private void ApplyCorrectFeedback()
    {
        foreach (MeshRenderer renderer in _buttonRenderers.Where(r => r != null))
        {
            if (correctMaterial != null) renderer.material = correctMaterial;
        }
    }

    /// <summary>
    /// Clears the current input sequence, effectively resetting the player's progress on this element.
    /// </summary>
    private void ResetInput()
    {
        _currentInputSequence.Clear();
        ResetVisuals(); // Ensure visuals are back to default.
        // If the puzzle was solved and then an incorrect button was pressed, it should become unsolved.
        if (_isSolved) {
            SetSolved(false); // Only do this if we want to immediately unsolve if sequence messed up
        }
    }

    /// <summary>
    /// Resets this puzzle element to its initial unsolved state.
    /// (Required by IPuzzleElement interface)
    /// </summary>
    public void ResetElement()
    {
        SetSolved(false); // Mark as unsolved.
        _currentInputSequence.Clear(); // Clear any partial input.
        ResetVisuals(); // Reset visual state.
        Debug.Log($"PuzzleButtonSequence '{gameObject.name}' has been reset.");
    }

    /// <summary>
    /// Resets all button visuals to their default material.
    /// </summary>
    private void ResetVisuals()
    {
        foreach (MeshRenderer renderer in _buttonRenderers.Where(r => r != null))
        {
            if (defaultMaterial != null) renderer.material = defaultMaterial;
        }
    }
}

/// <summary>
/// Helper script to attach to each individual button GameObject within a sequence.
/// It forwards mouse click events to the parent PuzzleButtonSequence.
/// </summary>
[RequireComponent(typeof(Collider))] // Ensure there's a collider to detect clicks
public class PuzzleButtonTrigger : MonoBehaviour
{
    private PuzzleButtonSequence _puzzleSequence;
    private int _buttonIndex;

    /// <summary>
    /// Initializes this button trigger with a reference to its parent sequence and its index.
    /// </summary>
    /// <param name="sequence">The PuzzleButtonSequence this button belongs to.</param>
    /// <param name="index">The 0-based index of this button within the sequence.</param>
    public void Initialize(PuzzleButtonSequence sequence, int index)
    {
        _puzzleSequence = sequence;
        _buttonIndex = index;
    }

    /// <summary>
    /// Called when the mouse button is pressed over this collider.
    /// </summary>
    private void OnMouseDown()
    {
        if (_puzzleSequence != null)
        {
            _puzzleSequence.OnButtonPressed(_buttonIndex);
        }
    }
}
```

##### `PuzzleLever.cs`

A puzzle where a lever needs to be in a specific `Up` or `Down` position.

```csharp
// File: Assets/Scripts/PuzzleSystem/PuzzleLever.cs
using UnityEngine;
using System;

// Enum to define the two possible states of the lever.
public enum LeverState { Down, Up }

/// <summary>
/// A concrete implementation of IPuzzleElement.
/// This puzzle requires a lever to be in a specific 'Up' or 'Down' position.
/// </summary>
public class PuzzleLever : MonoBehaviour, IPuzzleElement
{
    [Header("Puzzle Element Settings")]
    [Tooltip("Is this individual element currently in its solved state?")]
    [SerializeField] private bool _isSolved = false;
    public bool IsSolved => _isSolved; // IPuzzleElement implementation

    // Event required by IPuzzleElement. Invoked when _isSolved changes.
    public event Action OnPuzzleElementStateChanged;

    [Header("Lever Settings")]
    [Tooltip("The correct state (Up or Down) for this lever to be in to solve this element.")]
    [SerializeField] private LeverState correctState = LeverState.Up;
    [Tooltip("The current operational state of the lever.")]
    [SerializeField] private LeverState currentState = LeverState.Down;

    [Header("Visuals (Optional)")]
    [Tooltip("The GameObject's transform representing the lever's visual part that rotates.")]
    [SerializeField] private Transform leverVisual;
    [Tooltip("The local Euler angles for the 'Down' state of the lever.")]
    [SerializeField] private Vector3 downRotation = new Vector3(0, 0, 45); // Example: 45 degrees Z rotation
    [Tooltip("The local Euler angles for the 'Up' state of the lever.")]
    [SerializeField] private Vector3 upRotation = new Vector3(0, 0, -45); // Example: -45 degrees Z rotation
    [Tooltip("Material to apply when the lever is in its correct (solved) state.")]
    [SerializeField] private Material solvedMaterial;
    [Tooltip("Material to apply when the lever is in an incorrect (unsolved) state.")]
    [SerializeField] private Material unsolvedMaterial;

    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        // Ensure there's a collider to detect mouse clicks.
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        // Get the MeshRenderer for applying visual feedback.
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Apply the initial state visually and logically.
        UpdateVisualState();
        CheckState(); // Check if the initial state is already correct.
    }

    /// <summary>
    /// Player interaction: Called when the mouse button is pressed over this lever's collider.
    /// Toggles the lever's state.
    /// </summary>
    private void OnMouseDown()
    {
        ToggleLever();
    }

    /// <summary>
    /// Toggles the current state of the lever (Up to Down, or Down to Up).
    /// </summary>
    public void ToggleLever()
    {
        // If already solved and the lever is in the correct state,
        // we might want to prevent further interaction or change.
        // For this example, we'll allow toggling even if solved,
        // which would then unsolve the puzzle.
        // if (_isSolved && currentState == correctState) return;

        currentState = (currentState == LeverState.Up) ? LeverState.Down : LeverState.Up;
        UpdateVisualState(); // Update visuals to reflect the new state.
        CheckState();       // Re-evaluate if this element is now solved.
        Debug.Log($"Lever '{gameObject.name}' toggled to: {currentState}");
    }

    /// <summary>
    /// Updates the visual representation of the lever based on its current state.
    /// </summary>
    private void UpdateVisualState()
    {
        if (leverVisual != null)
        {
            leverVisual.localEulerAngles = (currentState == LeverState.Up) ? upRotation : downRotation;
        }
        if (_meshRenderer != null)
        {
            // Apply solved/unsolved material based on the _isSolved status.
            _meshRenderer.material = _isSolved ? solvedMaterial : unsolvedMaterial;
        }
    }

    /// <summary>
    /// Checks if the current state of the lever matches the correct state.
    /// Updates _isSolved and invokes the event if the solved status changes.
    /// </summary>
    private void CheckState()
    {
        bool newIsSolved = (currentState == correctState);
        if (newIsSolved != _isSolved)
        {
            _isSolved = newIsSolved;
            UpdateVisualState(); // Update material based on new _isSolved state.
            OnPuzzleElementStateChanged?.Invoke(); // Notify the PuzzleManager.
            Debug.Log($"Lever '{gameObject.name}' is now: {(_isSolved ? "SOLVED" : "UNSOLVED")}");
        }
    }

    /// <summary>
    /// Resets the lever to its default (unsolved) state.
    /// (Required by IPuzzleElement interface)
    /// </summary>
    public void ResetElement()
    {
        currentState = LeverState.Down; // Default to 'Down' for reset.
        _isSolved = false;              // Explicitly set to unsolved.
        UpdateVisualState();            // Apply reset visuals.
        OnPuzzleElementStateChanged?.Invoke(); // Notify PuzzleManager about the state change.
        Debug.Log($"PuzzleLever '{gameObject.name}' has been reset.");
    }
}
```

##### `PuzzleDial.cs`

A puzzle where a dial needs to be rotated to a specific numerical value.

```csharp
// File: Assets/Scripts/PuzzleSystem/PuzzleDial.cs
using UnityEngine;
using System;

/// <summary>
/// A concrete implementation of IPuzzleElement.
/// This puzzle requires a rotatable dial to be set to a specific numerical value.
/// </summary>
public class PuzzleDial : MonoBehaviour, IPuzzleElement
{
    [Header("Puzzle Element Settings")]
    [Tooltip("Is this individual element currently in its solved state?")]
    [SerializeField] private bool _isSolved = false;
    public bool IsSolved => _isSolved; // IPuzzleElement implementation

    // Event required by IPuzzleElement. Invoked when _isSolved changes.
    public event Action OnPuzzleElementStateChanged;

    [Header("Dial Settings")]
    [Tooltip("The correct numerical value the dial must be set to for this element to be solved.")]
    [SerializeField] private int correctValue = 5;
    [Tooltip("The minimum possible value for the dial (e.g., 0).")]
    [SerializeField] private int minValue = 0;
    [Tooltip("The maximum possible value for the dial (e.g., 9).")]
    [SerializeField] private int maxValue = 9;
    [Tooltip("The current numerical value displayed by the dial.")]
    [SerializeField] private int currentValue = 0;

    [Header("Visuals (Optional)")]
    [Tooltip("The GameObject's transform representing the dial's rotatable part.")]
    [SerializeField] private Transform dialVisual;
    [Tooltip("The amount of degrees to rotate for each value increment. (e.g., 36 degrees for a 0-9 dial = 360/10).")]
    [SerializeField] private float degreesPerUnit = 36f;
    [Tooltip("Material to apply when the dial is in its correct (solved) state.")]
    [SerializeField] private Material solvedMaterial;
    [Tooltip("Material to apply when the dial is in an incorrect (unsolved) state.")]
    [SerializeField] private Material unsolvedMaterial;

    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        // Ensure there's a collider to detect mouse clicks.
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        // Get the MeshRenderer for applying visual feedback.
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Apply initial state visually and logically.
        UpdateVisualState();
        CheckState(); // Check if the initial state is already correct.
    }

    /// <summary>
    /// Player interaction: Called when the mouse button is pressed over this dial's collider.
    /// For simplicity, a click increments the dial's value.
    /// </summary>
    private void OnMouseDown()
    {
        RotateDial(1); // Increment value on click.
    }

    /// <summary>
    /// Rotates the dial by a certain amount, updating its value.
    /// </summary>
    /// <param name="direction">Positive to increment, negative to decrement the value.</param>
    public void RotateDial(int direction)
    {
        // If already solved and locked, prevent further interaction.
        // For this example, we'll allow interaction even if solved, which would unsolve the puzzle.
        // if (_isSolved && currentValue == correctValue) return;

        currentValue += direction;
        // Clamp and wrap the value around min/max to simulate a continuous dial.
        if (currentValue > maxValue) currentValue = minValue;
        if (currentValue < minValue) currentValue = maxValue;

        UpdateVisualState(); // Update visuals to reflect the new value.
        CheckState();       // Re-evaluate if this element is now solved.
        Debug.Log($"Dial '{gameObject.name}' rotated to: {currentValue}");
    }

    /// <summary>
    /// Updates the visual representation of the dial based on its current value.
    /// </summary>
    private void UpdateVisualState()
    {
        if (dialVisual != null)
        {
            // Calculate Z-axis rotation based on current value and degrees per unit.
            float targetRotationZ = currentValue * degreesPerUnit;
            // Maintain existing X and Y rotation if desired.
            dialVisual.localEulerAngles = new Vector3(dialVisual.localEulerAngles.x, dialVisual.localEulerAngles.y, targetRotationZ);
        }
        if (_meshRenderer != null)
        {
            // Apply solved/unsolved material based on the _isSolved status.
            _meshRenderer.material = _isSolved ? solvedMaterial : unsolvedMaterial;
        }
    }

    /// <summary>
    /// Checks if the current value of the dial matches the correct value.
    /// Updates _isSolved and invokes the event if the solved status changes.
    /// </summary>
    private void CheckState()
    {
        bool newIsSolved = (currentValue == correctValue);
        if (newIsSolved != _isSolved)
        {
            _isSolved = newIsSolved;
            UpdateVisualState(); // Update material based on new _isSolved state.
            OnPuzzleElementStateChanged?.Invoke(); // Notify the PuzzleManager.
            Debug.Log($"Dial '{gameObject.name}' is now: {(_isSolved ? "SOLVED" : "UNSOLVED")}");
        }
    }

    /// <summary>
    /// Resets the dial to its default (unsolved) state.
    /// (Required by IPuzzleElement interface)
    /// </summary>
    public void ResetElement()
    {
        currentValue = minValue; // Default to the minimum value for reset.
        _isSolved = false;       // Explicitly set to unsolved.
        UpdateVisualState();     // Apply reset visuals.
        OnPuzzleElementStateChanged?.Invoke(); // Notify PuzzleManager about the state change.
        Debug.Log($"PuzzleDial '{gameObject.name}' has been reset.");
    }
}
```

#### 4. `PuzzleDoor.cs` (Consequence)

This script acts as a receiver, performing an action (opening/closing a door) when the overall puzzle is solved or unsolved.

```csharp
// File: Assets/Scripts/PuzzleSystem/PuzzleDoor.cs
using UnityEngine;

/// <summary>
/// A concrete example of a 'Puzzle Consequence'.
/// This script subscribes to the PuzzleManager's events and performs an action
/// (e.g., opens or closes a door) when the entire puzzle is solved or unsolved.
/// </summary>
public class PuzzleDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The GameObject representing the door to be moved/animated.")]
    [SerializeField] private GameObject doorVisual;
    [Tooltip("The position offset from the closed position when the door is open.")]
    [SerializeField] private Vector3 openPositionOffset = new Vector3(0, 5, 0); // Example: moves up 5 units
    [Tooltip("How fast the door moves between open and closed states.")]
    [SerializeField] private float openSpeed = 2f;

    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private bool _isOpen = false;

    private void Awake()
    {
        if (doorVisual == null)
        {
            Debug.LogWarning($"PuzzleDoor on {gameObject.name} has no 'Door Visual' assigned. Assigning self as visual target.", this);
            doorVisual = this.gameObject;
        }
        _closedPosition = doorVisual.transform.position;
        _openPosition = _closedPosition + openPositionOffset;
    }

    private void OnEnable()
    {
        // Subscribe to the PuzzleManager's events when this component is enabled.
        // It's good practice to ensure the PuzzleManager instance exists.
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleSolved += OpenDoor;
            PuzzleManager.Instance.OnPuzzleUnsolved += CloseDoor;
            // Optionally, check initial state in case the puzzle is already solved
            if (PuzzleManager.Instance.IsPuzzleCurrentlySolved) OpenDoor();
        }
        else
        {
            Debug.LogError("PuzzleManager instance not found! Make sure a GameObject with PuzzleManager.cs is present in the scene.", this);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events when this component is disabled or destroyed.
        // This prevents memory leaks and ensures methods aren't called on a destroyed object.
        if (PuzzleManager.Instance != null)
        {
            PuzzleManager.Instance.OnPuzzleSolved -= OpenDoor;
            PuzzleManager.Instance.OnPuzzleUnsolved -= CloseDoor;
        }
    }

    private void Update()
    {
        // Smoothly move the door towards its target position (open or closed).
        Vector3 targetPosition = _isOpen ? _openPosition : _closedPosition;
        if (doorVisual.transform.position != targetPosition)
        {
            doorVisual.transform.position = Vector3.MoveTowards(
                doorVisual.transform.position,
                targetPosition,
                openSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// This method is called when the PuzzleManager's OnPuzzleSolved event is triggered.
    /// It sets the door's state to 'open' and initiates movement.
    /// </summary>
    private void OpenDoor()
    {
        if (!_isOpen)
        {
            _isOpen = true;
            Debug.Log($"PuzzleDoor '{gameObject.name}': Puzzle solved! Opening door...");
            // Add animations, sound effects, particle effects here.
        }
    }

    /// <summary>
    /// This method is called when the PuzzleManager's OnPuzzleUnsolved event is triggered.
    /// It sets the door's state to 'closed' and initiates movement.
    /// </summary>
    private void CloseDoor()
    {
        if (_isOpen)
        {
            _isOpen = false;
            Debug.Log($"PuzzleDoor '{gameObject.name}': Puzzle unsolved! Closing door...");
            // Add reverse animations, sound effects, etc.
        }
    }

    /// <summary>
    /// Optional: A public method to manually toggle the door's state (e.g., for testing or other interactions).
    /// </summary>
    public void ToggleDoorManual()
    {
        if (_isOpen) CloseDoor();
        else OpenDoor();
    }
}
```

---

### Unity Setup Instructions (Example Scene)

To get this system running in your Unity project:

1.  **Create a Folder Structure:**
    *   Create a folder `Assets/Scripts/PuzzleSystem`.
    *   Place all the C# scripts (`IPuzzleElement.cs`, `PuzzleManager.cs`, `PuzzleButtonSequence.cs`, `PuzzleButtonTrigger.cs`, `PuzzleLever.cs`, `PuzzleDial.cs`, `PuzzleDoor.cs`) into this folder.

2.  **Create Materials (Optional but Recommended for Visual Feedback):**
    *   In your Project window, create a new folder `Assets/Materials`.
    *   Create several new Materials (Right-click -> Create -> Material):
        *   `DefaultMat` (e.g., light grey)
        *   `PressedMat` (e.g., blue)
        *   `CorrectMat` (e.g., green)
        *   `IncorrectMat` (e.g., red)
        *   `SolvedMat` (e.g., bright green)
        *   `UnsolvedMat` (e.g., dark grey)

3.  **Setup the `PuzzleManager`:**
    *   Create an Empty GameObject in your scene and rename it to `PuzzleManager`.
    *   Add the `PuzzleManager.cs` script to this GameObject.

4.  **Setup Puzzle Elements:**

    *   **Puzzle Button Sequence:**
        *   Create an Empty GameObject, name it `ButtonSequencePuzzle`.
        *   Add the `PuzzleButtonSequence.cs` script to it.
        *   As children of `ButtonSequencePuzzle`, create three 3D Cube objects. Name them `Button 0`, `Button 1`, `Button 2`. Position them clearly in the scene.
        *   Select `ButtonSequencePuzzle`. In its Inspector:
            *   Drag `Button 0`, `Button 1`, `Button 2` into the `Buttons` list.
            *   Set `Correct Sequence` (e.g., `0, 1, 2` for pressing in order).
            *   Assign `DefaultMat`, `PressedMat`, `CorrectMat`, `IncorrectMat` to their respective fields.
        *   **Important:** `PuzzleButtonTrigger` script will be added automatically to the button GameObjects in Awake().

    *   **Puzzle Lever:**
        *   Create an Empty GameObject, name it `LeverPuzzle`.
        *   Add the `PuzzleLever.cs` script to it.
        *   As a child of `LeverPuzzle`, create a 3D Cylinder object. Name it `Lever Visual`. Adjust its scale and position relative to `LeverPuzzle` to look like a lever. You might need to rotate its pivot.
        *   Select `LeverPuzzle`. In its Inspector:
            *   Assign `Lever Visual` to the `Lever Visual` field.
            *   Set `Correct State` (e.g., `Up`).
            *   Adjust `Down Rotation` and `Up Rotation` (e.g., `(0,0,45)` and `(0,0,-45)`) to visually represent the lever states.
            *   Assign `SolvedMat` and `UnsolvedMat`.

    *   **Puzzle Dial:**
        *   Create an Empty GameObject, name it `DialPuzzle`.
        *   Add the `PuzzleDial.cs` script to it.
        *   As a child of `DialPuzzle`, create a 3D Cylinder or Quad object. Name it `Dial Visual`. Make it look like a rotary dial.
        *   Select `DialPuzzle`. In its Inspector:
            *   Assign `Dial Visual` to the `Dial Visual` field.
            *   Set `Correct Value` (e.g., `5`).
            *   Adjust `Min Value` (e.g., `0`) and `Max Value` (e.g., `9`).
            *   Set `Degrees Per Unit` (e.g., `36f` for 10 values * 36 degrees = 360 degrees).
            *   Assign `SolvedMat` and `UnsolvedMat`.

5.  **Register Elements with the `PuzzleManager`:**
    *   Select the `PuzzleManager` GameObject in your scene.
    *   In its Inspector, expand the `Puzzle Element Game Objects` list.
    *   Drag and drop `ButtonSequencePuzzle`, `LeverPuzzle`, and `DialPuzzle` GameObjects from your Hierarchy into this list.

6.  **Setup the `PuzzleDoor` (Consequence):**
    *   Create a 3D Cube object, name it `DoorVisual`. Position it as a door blocking a path.
    *   Create an Empty GameObject, name it `PuzzleDoorController`.
    *   Add the `PuzzleDoor.cs` script to it.
    *   Select `PuzzleDoorController`. In its Inspector:
        *   Assign `DoorVisual` to the `Door Visual` field.
        *   Adjust `Open Position Offset` (e.g., `(0, 5, 0)` to make it slide up) and `Open Speed`.

7.  **Run and Test:**
    *   Run your Unity scene.
    *   Click on the buttons, lever, and dial to interact with them.
    *   Observe the Debug Logs (Console window) for feedback from the `PuzzleManager` and individual elements.
    *   When all three puzzle elements are in their correct 'solved' state, the `PuzzleDoor` should automatically open.
    *   If you then interact with any puzzle element to make it 'unsolved' again, the door should close.

This setup provides a robust and extensible framework for creating multi-part puzzles in your Unity projects.