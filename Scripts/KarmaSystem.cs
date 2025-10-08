// Unity Design Pattern Example: KarmaSystem
// This script demonstrates the KarmaSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The KarmaSystem design pattern tracks a moral or reputational value for an entity (like a player, an NPC, or even a faction) within a game. This "karma" value changes based on actions, and its current state can influence various aspects of the game, such as dialogue options, NPC reactions, quest availability, visual effects, and overall game progression.

This example provides a robust, configurable, and event-driven KarmaSystem that can be easily integrated into any Unity project.

---

### **KarmaSystem Design Pattern: Unity Implementation**

**Purpose:**
*   **Track Morality:** Maintain a quantifiable value representing an entity's good or bad deeds.
*   **Influence Game State:** Allow other game systems to react dynamically to changes in karma.
*   **Provide Feedback:** Give players clear indicators of their moral standing.

**Key Components:**
1.  **`KarmaSystem` (MonoBehaviour):** The core script managing the karma value, its limits, and defining discrete karma states. It provides methods to modify karma and an event to notify listeners of changes.
2.  **`KarmaState` (Enum):** Defines named categories for karma (e.g., Evil, Neutral, Good).
3.  **`KarmaStateThreshold` (Struct):** Configurable thresholds linking a `KarmaState` to a specific karma value.
4.  **Events:** Crucial for decoupling. Other systems don't need direct references to `KarmaSystem` but can subscribe to its `OnKarmaChanged` event.

---

### **KarmaSystem.cs**

This script should be attached to the GameObject that represents the entity whose karma you want to track (e.g., the Player GameObject).

```csharp
using UnityEngine;
using System;
using System.Linq; // For ordering thresholds

/// <summary>
/// Represents the discrete moral states an entity can be in based on their karma.
/// </summary>
public enum KarmaState
{
    VeryEvil,
    Evil,
    Neutral,
    Good,
    VeryGood
}

/// <summary>
/// A serializable struct to define a specific KarmaState and the karma value
/// at or above which that state is considered active.
/// This allows for easy configuration in the Unity Inspector.
/// </summary>
[Serializable]
public struct KarmaStateThreshold
{
    [Tooltip("The KarmaState associated with this threshold.")]
    public KarmaState state;
    [Tooltip("The minimum karma value required to reach this state. " +
             "States are evaluated in ascending order of this value.")]
    public float thresholdValue;
}

/// <summary>
/// The core KarmaSystem MonoBehaviour responsible for tracking, managing,
/// and notifying other systems about an entity's karma.
/// </summary>
public class KarmaSystem : MonoBehaviour
{
    [Header("Karma Settings")]
    [Tooltip("The initial karma value when the game starts or system is reset.")]
    [SerializeField]
    private float _initialKarma = 0f;

    [Tooltip("The current karma value of this entity.")]
    [SerializeField]
    private float _currentKarma;

    [Tooltip("The minimum possible karma value.")]
    [SerializeField]
    private float _minKarma = -100f;

    [Tooltip("The maximum possible karma value.")]
    [SerializeField]
    private float _maxKarma = 100f;

    [Header("Karma State Thresholds")]
    [Tooltip("Define the different karma states and their corresponding threshold values. " +
             "The system will automatically sort these by threshold value internally.")]
    [SerializeField]
    private KarmaStateThreshold[] _karmaStateThresholds = new KarmaStateThreshold[]
    {
        new KarmaStateThreshold { state = KarmaState.VeryEvil, thresholdValue = -100f },
        new KarmaStateThreshold { state = KarmaState.Evil, thresholdValue = -50f },
        new KarmaStateThreshold { state = KarmaState.Neutral, thresholdValue = 0f },
        new KarmaStateThreshold { state = KarmaState.Good, thresholdValue = 50f },
        new KarmaStateThreshold { state = KarmaState.VeryGood, thresholdValue = 75f }
    };

    // Private field to store sorted thresholds for efficient lookup
    private KarmaStateThreshold[] _sortedThresholds;

    /// <summary>
    /// Event fired whenever the karma value changes.
    /// Subscribers receive the new karma value and the new KarmaState.
    /// This is the primary way other systems react to karma changes.
    /// </summary>
    public event Action<float, KarmaState> OnKarmaChanged;

    /// <summary>
    /// Event fired specifically when the KarmaState changes.
    /// Subscribers receive the new KarmaState.
    /// </summary>
    public event Action<KarmaState> OnKarmaStateChanged;

    private KarmaState _previousKarmaState;

    // ====================================================================================
    // Unity Lifecycle Methods
    // ====================================================================================

    private void Awake()
    {
        // Ensure thresholds are sorted by value for correct state determination.
        // This is done once at Awake to avoid repeated sorting during gameplay.
        _sortedThresholds = _karmaStateThresholds.OrderBy(t => t.thresholdValue).ToArray();

        // Initialize karma and notify listeners if it's not at the default initial state
        // (e.g., if it was manually set in the inspector and not 0).
        _currentKarma = Mathf.Clamp(_initialKarma, _minKarma, _maxKarma);
        _previousKarmaState = GetCurrentKarmaState(); // Initialize previous state
        NotifyKarmaChanged();
    }

    // ====================================================================================
    // Public Methods to Modify Karma
    // ====================================================================================

    /// <summary>
    /// Adds a specified amount to the current karma value.
    /// The value will be clamped between _minKarma and _maxKarma.
    /// </summary>
    /// <param name="amount">The positive amount of karma to add (e.g., 10f for a good deed).</param>
    public void AddKarma(float amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("AddKarma was called with a negative amount. Use SubtractKarma instead.");
            return;
        }
        SetKarma(_currentKarma + amount);
    }

    /// <summary>
    /// Subtracts a specified amount from the current karma value.
    /// The value will be clamped between _minKarma and _maxKarma.
    /// </summary>
    /// <param name="amount">The positive amount of karma to subtract (e.g., 10f for a bad deed).</param>
    public void SubtractKarma(float amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("SubtractKarma was called with a negative amount. Use AddKarma instead.");
            return;
        }
        SetKarma(_currentKarma - amount);
    }

    /// <summary>
    /// Sets the current karma value directly.
    /// The value will be clamped between _minKarma and _maxKarma.
    /// Use this for specific scenarios like loading game progress or resetting karma.
    /// </summary>
    /// <param name="newKarmaValue">The new karma value to set.</param>
    public void SetKarma(float newKarmaValue)
    {
        float clampedKarma = Mathf.Clamp(newKarmaValue, _minKarma, _maxKarma);

        // Only update and notify if the karma value has actually changed
        if (Mathf.Approximately(_currentKarma, clampedKarma))
        {
            return; // No change, no need to proceed
        }

        _currentKarma = clampedKarma;
        NotifyKarmaChanged();
    }

    /// <summary>
    /// Resets the karma to its initial value.
    /// </summary>
    public void ResetKarma()
    {
        SetKarma(_initialKarma);
    }

    // ====================================================================================
    // Public Methods to Query Karma
    // ====================================================================================

    /// <summary>
    /// Gets the current raw karma value.
    /// </summary>
    public float GetCurrentKarma()
    {
        return _currentKarma;
    }

    /// <summary>
    /// Gets the current karma value normalized to a 0-1 range.
    /// Useful for UI elements like progress bars or sliders.
    /// </summary>
    public float GetKarmaNormalized()
    {
        if (_maxKarma == _minKarma) return 0.5f; // Prevent division by zero if min and max are the same
        return (_currentKarma - _minKarma) / (_maxKarma - _minKarma);
    }

    /// <summary>
    /// Determines and returns the current KarmaState based on the karma value
    /// and the defined thresholds.
    /// </summary>
    public KarmaState GetCurrentKarmaState()
    {
        // Iterate through sorted thresholds to find the highest state achieved.
        KarmaState currentState = KarmaState.Neutral; // Default state

        foreach (var threshold in _sortedThresholds)
        {
            if (_currentKarma >= threshold.thresholdValue)
            {
                currentState = threshold.state;
            }
            else
            {
                // Since thresholds are sorted, once we pass a threshold,
                // all subsequent thresholds will also be higher.
                // The current currentState is the highest one met so far.
                break;
            }
        }
        return currentState;
    }

    /// <summary>
    /// Retrieves the minimum possible karma value.
    /// </summary>
    public float GetMinKarma()
    {
        return _minKarma;
    }

    /// <summary>
    /// Retrieves the maximum possible karma value.
    /// </summary>
    public float GetMaxKarma()
    {
        return _maxKarma;
    }

    // ====================================================================================
    // Private Helper Methods
    // ====================================================================================

    /// <summary>
    /// Notifies all subscribed listeners that the karma has changed.
    /// Checks if the KarmaState has also changed to fire a separate event.
    /// </summary>
    private void NotifyKarmaChanged()
    {
        KarmaState currentKarmaState = GetCurrentKarmaState();

        // Invoke the general karma changed event
        OnKarmaChanged?.Invoke(_currentKarma, currentKarmaState);

        // Check if the state itself has changed
        if (_previousKarmaState != currentKarmaState)
        {
            OnKarmaStateChanged?.Invoke(currentKarmaState);
            _previousKarmaState = currentKarmaState; // Update previous state
        }
    }

    // ====================================================================================
    // Inspector Validation (Optional but Recommended)
    // ====================================================================================
    private void OnValidate()
    {
        // Ensure minKarma is less than or equal to maxKarma
        if (_minKarma > _maxKarma)
        {
            Debug.LogWarning("KarmaSystem: _minKarma cannot be greater than _maxKarma. Adjusting _maxKarma.");
            _maxKarma = _minKarma;
        }

        // Ensure _initialKarma is within bounds during editor changes
        _initialKarma = Mathf.Clamp(_initialKarma, _minKarma, _maxKarma);

        // Clamp current karma in editor if bounds change
        _currentKarma = Mathf.Clamp(_currentKarma, _minKarma, _maxKarma);

        // Sort thresholds in editor for visual consistency, but the runtime Awake will re-sort for safety.
        _karmaStateThresholds = _karmaStateThresholds.OrderBy(t => t.thresholdValue).ToArray();
    }
}
```

---

### **KarmaDisplayUI.cs (Example Usage - Subscribing to Karma Changes)**

This script demonstrates how another system (e.g., a UI element) would react to karma changes. Create a new C# script named `KarmaDisplayUI.cs` and attach it to a UI Text or Slider element in your Unity scene.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Text and Slider
using System;

/// <summary>
/// An example MonoBehaviour that displays the current karma value and state
/// on UI elements and reacts to karma changes.
/// </summary>
public class KarmaDisplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Text component to display the current raw karma value.")]
    [SerializeField]
    private Text _karmaValueText;

    [Tooltip("Text component to display the current KarmaState (e.g., Neutral, Good).")]
    [SerializeField]
    private Text _karmaStateText;

    [Tooltip("Slider component to visually represent karma (normalized 0-1).")]
    [SerializeField]
    private Slider _karmaSlider;

    private KarmaSystem _playerKarmaSystem; // Reference to the KarmaSystem we're observing

    private void Start()
    {
        // Find the KarmaSystem in the scene.
        // For a player, it's usually on the player GameObject.
        // Consider using dependency injection or a GameManager singleton for more robust references.
        _playerKarmaSystem = FindObjectOfType<KarmaSystem>();

        if (_playerKarmaSystem == null)
        {
            Debug.LogError("KarmaDisplayUI: No KarmaSystem found in the scene. Please ensure one exists.", this);
            enabled = false; // Disable this component if no KarmaSystem is found
            return;
        }

        // Subscribe to the OnKarmaChanged event.
        // This method will be called whenever the karma value is modified.
        _playerKarmaSystem.OnKarmaChanged += UpdateKarmaDisplay;

        // Optionally, subscribe to OnKarmaStateChanged for specific reactions
        _playerKarmaSystem.OnKarmaStateChanged += ReactToKarmaStateChange;

        // Immediately update the display with the current karma on start
        UpdateKarmaDisplay(_playerKarmaSystem.GetCurrentKarma(), _playerKarmaSystem.GetCurrentKarmaState());
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks or errors
        // if the observed object (KarmaSystem) is destroyed before this one.
        if (_playerKarmaSystem != null)
        {
            _playerKarmaSystem.OnKarmaChanged -= UpdateKarmaDisplay;
            _playerKarmaSystem.OnKarmaStateChanged -= ReactToKarmaStateChange;
        }
    }

    /// <summary>
    /// Callback method for the OnKarmaChanged event. Updates UI elements.
    /// </summary>
    /// <param name="newKarmaValue">The new raw karma value.</param>
    /// <param name="newKarmaState">The new KarmaState.</param>
    private void UpdateKarmaDisplay(float newKarmaValue, KarmaState newKarmaState)
    {
        if (_karmaValueText != null)
        {
            _karmaValueText.text = $"Karma: {newKarmaValue:F1}"; // Display with one decimal place
        }

        if (_karmaStateText != null)
        {
            _karmaStateText.text = $"State: {newKarmaState}";
            _karmaStateText.color = GetColorForKarmaState(newKarmaState); // Example: change color based on state
        }

        if (_karmaSlider != null)
        {
            _karmaSlider.value = _playerKarmaSystem.GetKarmaNormalized();
        }

        Debug.Log($"Karma Updated: Value={newKarmaValue:F1}, State={newKarmaState}");
    }

    /// <summary>
    /// Callback method for the OnKarmaStateChanged event.
    /// Demonstrates reacting specifically to a state change.
    /// </summary>
    /// <param name="newState">The new KarmaState.</param>
    private void ReactToKarmaStateChange(KarmaState newState)
    {
        Debug.Log($"KARMA STATE CHANGED! New State: {newState}");
        // Example: Play a sound, show a special effect, trigger a dialogue event, etc.
        switch (newState)
        {
            case KarmaState.VeryEvil:
                Debug.Log("You are an abomination!");
                // Trigger major evil-aligned game events
                break;
            case KarmaState.Evil:
                Debug.Log("NPCs now fear you.");
                // Change NPC dialogue
                break;
            case KarmaState.Neutral:
                Debug.Log("You are balanced. The world waits for your choices.");
                break;
            case KarmaState.Good:
                Debug.Log("NPCs look upon you favorably.");
                // Offer good-aligned quests
                break;
            case KarmaState.VeryGood:
                Debug.Log("You are a true hero!");
                // Unlock special hero content
                break;
        }
    }

    /// <summary>
    /// Helper method to return a color based on KarmaState for UI display.
    /// </summary>
    private Color GetColorForKarmaState(KarmaState state)
    {
        switch (state)
        {
            case KarmaState.VeryEvil: return Color.red;
            case KarmaState.Evil: return new Color(1f, 0.5f, 0f); // Orange-red
            case KarmaState.Neutral: return Color.gray;
            case KarmaState.Good: return Color.green;
            case KarmaState.VeryGood: return Color.cyan;
            default: return Color.white;
        }
    }
}
```

---

### **KarmaActionTrigger.cs (Example Usage - Modifying Karma)**

This script simulates game actions that would modify karma. Create a new C# script named `KarmaActionTrigger.cs` and attach it to an empty GameObject in your scene. Add a Button component to this GameObject (or any other interactive element) and hook up its `onClick` event to call the public methods in this script.

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for Button if you use UI buttons
using System.Collections; // For coroutines

/// <summary>
/// An example MonoBehaviour that simulates game actions which modify the KarmaSystem.
/// This could be attached to a button, a trigger collider, or called by other game logic.
/// </summary>
public class KarmaActionTrigger : MonoBehaviour
{
    [Tooltip("Reference to the KarmaSystem to modify. If null, it will try to find one.")]
    [SerializeField]
    private KarmaSystem _targetKarmaSystem;

    [Header("Action Values")]
    [Tooltip("Amount of karma to add for a 'good' action.")]
    [SerializeField]
    private float _goodDeedAmount = 10f;

    [Tooltip("Amount of karma to subtract for a 'bad' action.")]
    [SerializeField]
    private float _badDeedAmount = 10f;

    [Tooltip("Amount of karma to add for a 'heroic' action (more significant).")]
    [SerializeField]
    private float _heroicDeedAmount = 25f;

    [Tooltip("Amount of karma to subtract for a 'villainous' action (more significant).")]
    [SerializeField]
    private float _villainousDeedAmount = 25f;

    [Tooltip("Optional: Text element to show feedback messages.")]
    [SerializeField]
    private Text _feedbackText;

    private Coroutine _feedbackCoroutine;

    private void Start()
    {
        // If no KarmaSystem is explicitly assigned in the Inspector, try to find one.
        if (_targetKarmaSystem == null)
        {
            _targetKarmaSystem = FindObjectOfType<KarmaSystem>();
            if (_targetKarmaSystem == null)
            {
                Debug.LogError("KarmaActionTrigger: No KarmaSystem found in the scene. Please assign one or ensure one exists.", this);
                enabled = false; // Disable this component if no KarmaSystem is found
                return;
            }
        }
    }

    /// <summary>
    /// Simulates a good action, adding karma.
    /// </summary>
    public void PerformGoodDeed()
    {
        if (_targetKarmaSystem != null)
        {
            _targetKarmaSystem.AddKarma(_goodDeedAmount);
            ShowFeedback($"Performed a good deed! Karma +{_goodDeedAmount}");
        }
    }

    /// <summary>
    /// Simulates a bad action, subtracting karma.
    /// </summary>
    public void PerformBadDeed()
    {
        if (_targetKarmaSystem != null)
        {
            _targetKarmaSystem.SubtractKarma(_badDeedAmount);
            ShowFeedback($"Performed a bad deed! Karma -{_badDeedAmount}");
        }
    }

    /// <summary>
    /// Simulates a heroic action, adding a larger amount of karma.
    /// </summary>
    public void PerformHeroicDeed()
    {
        if (_targetKarmaSystem != null)
        {
            _targetKarmaSystem.AddKarma(_heroicDeedAmount);
            ShowFeedback($"Performed a heroic deed! Karma +{_heroicDeedAmount}");
        }
    }

    /// <summary>
    /// Simulates a villainous action, subtracting a larger amount of karma.
    /// </summary>
    public void PerformVillainousDeed()
    {
        if (_targetKarmaSystem != null)
        {
            _targetKarmaSystem.SubtractKarma(_villainousDeedAmount);
            ShowFeedback($"Performed a villainous deed! Karma -{_villainousDeedAmount}");
        }
    }

    /// <summary>
    /// Resets the karma to its initial value.
    /// </summary>
    public void ResetKarma()
    {
        if (_targetKarmaSystem != null)
        {
            _targetKarmaSystem.ResetKarma();
            ShowFeedback("Karma has been reset!");
        }
    }

    /// <summary>
    /// Displays a temporary feedback message on the UI.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private void ShowFeedback(string message)
    {
        if (_feedbackText != null)
        {
            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }
            _feedbackText.text = message;
            _feedbackCoroutine = StartCoroutine(ClearFeedbackAfterDelay(3f));
        }
    }

    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_feedbackText != null)
        {
            _feedbackText.text = "";
        }
        _feedbackCoroutine = null;
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create a Player GameObject:**
    *   In your Unity scene, create an empty GameObject and name it, for example, `Player`.
    *   Attach the `KarmaSystem.cs` script to this `Player` GameObject.
    *   In the Inspector for the `KarmaSystem` component:
        *   Adjust `Initial Karma`, `Min Karma`, `Max Karma` as needed.
        *   Configure the `Karma State Thresholds`. You can add more states or modify the `thresholdValue` for existing ones. Ensure the `thresholdValue` generally increases for higher karma states.

2.  **Create UI Elements:**
    *   Right-click in the Hierarchy -> UI -> Canvas.
    *   Inside the Canvas, create:
        *   UI -> Text (for Karma Value, rename to `KarmaValueText`).
        *   UI -> Text (for Karma State, rename to `KarmaStateText`).
        *   UI -> Slider (for Karma Slider, rename to `KarmaSlider`).
        *   UI -> Text (for Feedback, rename to `FeedbackText`).
    *   Adjust their positions and sizes on the Canvas.

3.  **Attach `KarmaDisplayUI`:**
    *   Create an empty GameObject (e.g., `UIManager`).
    *   Attach the `KarmaDisplayUI.cs` script to this `UIManager` GameObject.
    *   In the Inspector for `KarmaDisplayUI`:
        *   Drag your `KarmaValueText` to the `_karmaValueText` field.
        *   Drag your `KarmaStateText` to the `_karmaStateText` field.
        *   Drag your `KarmaSlider` to the `_karmaSlider` field.

4.  **Attach `KarmaActionTrigger`:**
    *   Create an empty GameObject (e.g., `GameActions`).
    *   Attach the `KarmaActionTrigger.cs` script to this `GameActions` GameObject.
    *   In the Inspector for `KarmaActionTrigger`:
        *   Drag your `Player` GameObject (which has the `KarmaSystem`) to the `Target Karma System` field.
        *   (Optional) Drag your `FeedbackText` to the `_feedbackText` field.
        *   Adjust `Good Deed Amount`, `Bad Deed Amount`, etc.

5.  **Create Action Buttons (Optional, for easy testing):**
    *   Inside your Canvas, create several UI -> Button elements.
    *   For each button:
        *   Change its text to "Good Deed", "Bad Deed", "Heroic Deed", "Villainous Deed", "Reset Karma".
        *   In the Inspector, find the "On Click ()" event.
        *   Click the "+" to add a new event.
        *   Drag your `GameActions` GameObject to the "Runtime Only" object slot.
        *   From the function dropdown, select `KarmaActionTrigger` and then the corresponding method (e.g., `PerformGoodDeed` for the "Good Deed" button).

6.  **Run the Scene:**
    *   Play your Unity scene.
    *   Interact with the buttons you created.
    *   Observe how the Karma Value, Karma State, and Slider on your UI update in real-time.
    *   Watch the Console for debug messages about karma state changes.

This complete setup provides a fully functional and highly customizable KarmaSystem, ready for use and extension in your Unity projects.