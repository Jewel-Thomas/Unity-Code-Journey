// Unity Design Pattern Example: ScriptableEvents
// This script demonstrates the ScriptableEvents pattern in Unity
// Generated automatically - ready to use in your Unity project

The ScriptableEvents design pattern in Unity is a powerful way to decouple game systems. It allows one part of your game to "broadcast" an event without needing to know which other parts are "listening." This significantly reduces direct dependencies between components, making your code more modular, flexible, and easier to maintain.

## ScriptableEvents Design Pattern Explained

At its core, the ScriptableEvents pattern in Unity consists of three main components:

1.  **The Event (ScriptableObject):**
    *   This is a `ScriptableObject` asset that represents a specific event (e.g., "OnPlayerDied," "OnScoreChanged," "OnLevelLoaded").
    *   It holds a list of all active listeners for that event.
    *   It has a `Raise()` method (or `Invoke()`, `Fire()`, etc.) that, when called, notifies all registered listeners.
    *   Since it's a `ScriptableObject`, it exists independently of any scene or GameObject and can be referenced by multiple GameObjects.

2.  **The Listener (MonoBehaviour):**
    *   This is a `MonoBehaviour` component attached to a GameObject.
    *   It has a public reference to a specific `GameEvent` asset.
    *   When the GameObject is enabled (`OnEnable`), it registers itself with the `GameEvent`.
    *   When the GameObject is disabled (`OnDisable`), it unregisters itself from the `GameEvent`.
    *   It exposes a `UnityEvent` (or `UnityEvent<T>`) in the Inspector, allowing developers to visually link responses to the event. When the `GameEvent` is raised, this `UnityEvent` is invoked, triggering its configured actions.

3.  **The Raiser/Invoker (MonoBehaviour):**
    *   This is a `MonoBehaviour` component that determines when an event should be raised.
    *   It has a public reference to a specific `GameEvent` asset.
    *   At the appropriate time (e.g., player takes damage, button is pressed, item is collected), it calls the `Raise()` method on its referenced `GameEvent` asset.

### Benefits:

*   **Decoupling:** Senders don't need to know who receives; receivers don't need to know who sends.
*   **Reusability:** Events and listeners can be reused across different scenes and contexts.
*   **Flexibility:** Easily add new listeners or change event responses without modifying existing code.
*   **Inspector Workflow:** UnityEvents allow wiring up responses directly in the Inspector, reducing code and improving visibility.
*   **Scalability:** Good for managing complex systems and interactions in larger projects.

### Example Scenario: Player Score System

We will create a system where:
*   A `ScoreManager` script manages the player's score.
*   When the score changes, it raises an `IntGameEvent` (an event that carries an integer payload).
*   A `ScoreDisplayUI` component listens for this `IntGameEvent` and updates a UI Text element.
*   A simple `ScoreAdder` component allows us to easily trigger score additions for demonstration purposes.

---

## Complete C# Unity Implementation

Here are the C# scripts. Place each script in its own `.cs` file in your Unity project. A good folder structure would be `Assets/ScriptableEvents/` for the core components and `Assets/ScriptableEvents/Examples/` for the demonstration scripts.

### 1. Core Event Definitions

#### `GameEvent.cs`
This is the base `ScriptableObject` for events *without* any data payload.

```csharp
// File: Assets/ScriptableEvents/GameEvent.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that represents a game event with no payload.
/// This acts as a central hub for game systems to communicate without direct dependencies.
/// Other GameObjects can 'Raise' this event, and registered 'GameEventListeners' will react.
/// </summary>
[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Scriptable Events/Game Event (No Payload)")]
public class GameEvent : ScriptableObject
{
    // A list of all GameEventListeners currently subscribed to this event.
    // The 'readonly' keyword ensures the list itself cannot be reassigned,
    // though its contents can still be modified.
    private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

    /// <summary>
    /// Call this method to raise the event. All registered listeners will be notified.
    /// </summary>
    /// <remarks>
    /// Iterates through the listeners in reverse to safely allow listeners to unregister
    /// themselves during the notification process without causing issues with collection modification.
    /// </remarks>
    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised();
        }
    }

    /// <summary>
    /// Register a listener with this event. This should typically be called in the listener's OnEnable().
    /// </summary>
    /// <param name="listener">The GameEventListener to register.</param>
    public void RegisterListener(GameEventListener listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    /// <summary>
    /// Unregister a listener from this event. This should typically be called in the listener's OnDisable().
    /// </summary>
    /// <param name="listener">The GameEventListener to unregister.</param>
    public void UnregisterListener(GameEventListener listener)
    {
        _listeners.Remove(listener);
    }
}
```

#### `GameEventListener.cs`
This is the `MonoBehaviour` listener for events *without* any data payload.

```csharp
// File: Assets/ScriptableEvents/GameEventListener.cs
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// A MonoBehaviour that listens for a 'GameEvent' ScriptableObject.
/// When the associated GameEvent is raised, this listener invokes its 'Response' UnityEvent,
/// allowing methods to be hooked up in the Inspector.
/// </summary>
public class GameEventListener : MonoBehaviour
{
    [Tooltip("The GameEvent ScriptableObject to listen to.")]
    public GameEvent Event;

    [Tooltip("The actions to invoke when the event is raised (no arguments).")]
    public UnityEvent Response;

    /// <summary>
    /// Subscribes this listener to the GameEvent when the GameObject is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (Event != null)
        {
            Event.RegisterListener(this);
        }
    }

    /// <summary>
    /// Unsubscribes this listener from the GameEvent when the GameObject is disabled.
    /// This prevents memory leaks and null reference exceptions if the event is raised
    /// after this object has been destroyed or deactivated.
    /// </summary>
    private void OnDisable()
    {
        if (Event != null)
        {
            Event.UnregisterListener(this);
        }
    }

    /// <summary>
    /// This method is called by the GameEvent when it is raised.
    /// It triggers the UnityEvent 'Response'.
    /// </summary>
    public void OnEventRaised()
    {
        Response.Invoke();
    }
}
```

### 2. Typed Event Definitions (e.g., Integer Payload)

#### `IntGameEvent.cs`
This is a `ScriptableObject` for events that carry an `int` payload. You can create similar `FloatGameEvent`, `Vector3GameEvent`, `StringGameEvent`, etc.

```csharp
// File: Assets/ScriptableEvents/IntGameEvent.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that represents a game event with an integer payload.
/// Similar to GameEvent, but carries an 'int' value when raised.
/// </summary>
[CreateAssetMenu(fileName = "NewIntGameEvent", menuName = "Scriptable Events/Int Game Event")]
public class IntGameEvent : ScriptableObject
{
    // A list of all IntGameEventListeners currently subscribed to this event.
    private readonly List<IntGameEventListener> _listeners = new List<IntGameEventListener>();

    /// <summary>
    /// Call this method to raise the event, passing an integer payload.
    /// All registered listeners will be notified with the provided value.
    /// </summary>
    /// <param name="value">The integer value to send with the event.</param>
    public void Raise(int value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i].OnEventRaised(value);
        }
    }

    /// <summary>
    /// Register a listener with this event. Typically called in OnEnable().
    /// </summary>
    /// <param name="listener">The IntGameEventListener to register.</param>
    public void RegisterListener(IntGameEventListener listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    /// <summary>
    /// Unregister a listener from this event. Typically called in OnDisable().
    /// </summary>
    /// <param name="listener">The IntGameEventListener to unregister.</param>
    public void UnregisterListener(IntGameEventListener listener)
    {
        _listeners.Remove(listener);
    }
}
```

#### `IntGameEventListener.cs`
This is the `MonoBehaviour` listener for events that carry an `int` payload.

```csharp
// File: Assets/ScriptableEvents/IntGameEventListener.cs
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent<T>

/// <summary>
/// A MonoBehaviour that listens for an 'IntGameEvent' ScriptableObject.
/// When the associated IntGameEvent is raised, this listener invokes its 'Response' UnityEvent,
/// passing the integer payload to any hooked methods.
/// </summary>
public class IntGameEventListener : MonoBehaviour
{
    [Tooltip("The IntGameEvent ScriptableObject to listen to.")]
    public IntGameEvent Event;

    [Tooltip("The actions to invoke when the event is raised, passing the integer payload.")]
    public UnityEvent<int> Response; // This UnityEvent accepts an integer argument.

    /// <summary>
    /// Subscribes this listener to the IntGameEvent when the GameObject is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (Event != null)
        {
            Event.RegisterListener(this);
        }
    }

    /// <summary>
    /// Unsubscribes this listener from the IntGameEvent when the GameObject is disabled.
    /// </summary>
    private void OnDisable()
    {
        if (Event != null)
        {
            Event.UnregisterListener(this);
        }
    }

    /// <summary>
    /// This method is called by the IntGameEvent when it is raised, with the integer payload.
    /// It triggers the UnityEvent 'Response' and passes the received value.
    /// </summary>
    /// <param name="value">The integer payload received from the event.</param>
    public void OnEventRaised(int value)
    {
        Response.Invoke(value);
    }
}
```

### 3. Example Usage: Player Score System

#### `ScoreManager.cs` (Event Raiser)
This script manages the player's score and raises an `IntGameEvent` whenever the score changes.

```csharp
// File: Assets/ScriptableEvents/Examples/ScoreManager.cs
using UnityEngine;

/// <summary>
/// Manages the player's score and raises an IntGameEvent whenever the score changes.
/// This acts as the central point for modifying the player's score.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Tooltip("The IntGameEvent to raise when the score changes.")]
    public IntGameEvent ScoreChangedEvent;

    [Tooltip("The current score of the player.")]
    public int currentScore = 0;

    /// <summary>
    /// Adds a specified amount to the current score and raises the ScoreChangedEvent.
    /// </summary>
    /// <param name="amount">The value to add to the score.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"Score increased to: {currentScore}");

        // Raise the event, passing the new current score as payload.
        if (ScoreChangedEvent != null)
        {
            ScoreChangedEvent.Raise(currentScore);
        }
        else
        {
            Debug.LogWarning("ScoreChangedEvent is not assigned in ScoreManager! No listeners will be notified.", this);
        }
    }

    /// <summary>
    /// Sets the current score to a new value and raises the ScoreChangedEvent.
    /// </summary>
    /// <param name="newScore">The new score value.</param>
    public void SetScore(int newScore)
    {
        currentScore = newScore;
        Debug.Log($"Score set to: {currentScore}");

        if (ScoreChangedEvent != null)
        {
            ScoreChangedEvent.Raise(currentScore);
        }
        else
        {
            Debug.LogWarning("ScoreChangedEvent is not assigned in ScoreManager! No listeners will be notified.", this);
        }
    }

    /// <summary>
    /// Called when the script starts. Raises the event once to ensure UI elements
    /// display the initial score correctly.
    /// </summary>
    void Start()
    {
        if (ScoreChangedEvent != null)
        {
            ScoreChangedEvent.Raise(currentScore);
        }
    }
}
```

#### `ScoreDisplayUI.cs` (Event Listener's Responder)
This script updates a TextMeshProUGUI component with the current score. It doesn't listen directly, but provides the method that an `IntGameEventListener` will call.

```csharp
// File: Assets/ScriptableEvents/Examples/ScoreDisplayUI.cs
using UnityEngine;
using TMPro; // Required for TextMeshProUGUI

/// <summary>
/// Updates a TextMeshProUGUI component to display the current score.
/// This script provides the method that an 'IntGameEventListener' will invoke.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))] // Ensure there's a TextMeshProUGUI component
public class ScoreDisplayUI : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component to update with the score.")]
    public TextMeshProUGUI scoreText;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the scoreText reference if not already set.
    /// </summary>
    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<TextMeshProUGUI>();
            if (scoreText == null)
            {
                Debug.LogError("ScoreDisplayUI requires a TextMeshProUGUI component on this GameObject or a reference set in the Inspector.", this);
                enabled = false; // Disable script if no text component is found.
            }
        }
    }

    /// <summary>
    /// This method is designed to be called by an IntGameEventListener's Response UnityEvent.
    /// It updates the UI Text with the new score value.
    /// </summary>
    /// <param name="newScore">The score value received from the IntGameEvent.</param>
    public void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
    }
}
```

#### `ScoreAdder.cs` (Example Trigger for Raiser)
A simple component to trigger score additions, demonstrating how other parts of the game can easily interact with the `ScoreManager`.

```csharp
// File: Assets/ScriptableEvents/Examples/ScoreAdder.cs
using UnityEngine;

/// <summary>
/// A simple component to trigger score additions. For demonstration purposes,
/// this can add a predefined amount of score to the ScoreManager.
/// Can be hooked up to UI buttons, collision events, etc.
/// </summary>
public class ScoreAdder : MonoBehaviour
{
    [Tooltip("Reference to the ScoreManager that manages and raises score events.")]
    public ScoreManager scoreManager;

    [Tooltip("The amount of score to add when 'AddScore' is called.")]
    public int scoreToAdd = 10;

    [Tooltip("If true, score will be added once on Start. For initial setup/testing.")]
    public bool addOnStart = false;

    /// <summary>
    /// Called when the script starts. Tries to find the ScoreManager if not assigned
    /// and optionally adds score.
    /// </summary>
    private void Start()
    {
        if (scoreManager == null)
        {
            // Attempt to find the ScoreManager in the scene if not explicitly assigned.
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogError("ScoreManager not found or assigned to ScoreAdder. Please ensure a ScoreManager exists in the scene.", this);
                enabled = false; // Disable this component if no ScoreManager is found.
                return;
            }
        }

        if (addOnStart)
        {
            AddScore();
        }
    }

    /// <summary>
    /// Adds the predefined 'scoreToAdd' to the ScoreManager.
    /// This method can be called by UI Button clicks, animation events,
    /// collision triggers, or any other game logic.
    /// </summary>
    public void AddScore()
    {
        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreToAdd);
        }
        else
        {
            Debug.LogWarning("Cannot add score: ScoreManager is not set on ScoreAdder.", this);
        }
    }
}
```

---

## How to Set Up in Unity

Follow these steps to get the example working in your Unity project:

1.  **Create Folders:**
    *   In your Project window, create a folder named `ScriptableEvents`.
    *   Inside `ScriptableEvents`, create `Examples`.

2.  **Add Scripts:**
    *   Copy each `.cs` file into its respective folder (e.g., `GameEvent.cs` into `ScriptableEvents`, `ScoreManager.cs` into `ScriptableEvents/Examples`).

3.  **Create ScriptableObject Event Assets:**
    *   Go to `Assets/ScriptableEvents` (or any convenient location).
    *   Right-click in the Project window -> `Create` -> `Scriptable Events` -> `Int Game Event`.
    *   Name it `OnScoreChangedEvent`. This is your global score event asset.

4.  **Create a UI Canvas and Text (for Score Display):**
    *   In your scene, right-click in the Hierarchy -> `UI` -> `Canvas`.
    *   Right-click on the `Canvas` -> `UI` -> `TextMeshPro - Text`. (If prompted, import TMP Essentials).
    *   Name the Text object `ScoreDisplay`.
    *   Adjust its size and position on the Canvas so it's visible. You might want to set its text to "Score: 0" initially.

5.  **Set up the Score Display Listener:**
    *   Select your `ScoreDisplay` TextMeshPro object in the Hierarchy.
    *   Add Component -> `Int Game Event Listener`.
    *   In the `Int Game Event Listener` component:
        *   Drag your `OnScoreChangedEvent` asset (from step 3) into the `Event` slot.
        *   In the `Response` section, click the `+` button.
        *   Drag the `ScoreDisplay` GameObject itself into the Runtime Only object slot.
        *   From the dropdown, select `ScoreDisplayUI` -> `UpdateScoreDisplay (int)`. (If `ScoreDisplayUI` is not available, ensure you've added the `ScoreDisplayUI.cs` script to this GameObject first).

6.  **Set up the Score Manager (Event Raiser):**
    *   Create an empty GameObject in your scene (e.g., `_GameManager`).
    *   Add Component -> `Score Manager`.
    *   In the `Score Manager` component:
        *   Drag your `OnScoreChangedEvent` asset into the `Score Changed Event` slot.
        *   You can set an `Initial Score` if you like.

7.  **Set up the Score Adder (Trigger):**
    *   Create an empty GameObject in your scene (e.g., `ScoreTrigger`).
    *   Add Component -> `Score Adder`.
    *   In the `Score Adder` component:
        *   Drag your `_GameManager` GameObject (from step 6) into the `Score Manager` slot.
        *   Set `Score To Add` (e.g., `10`).
        *   Check `Add On Start` for an immediate demonstration, or create a UI Button and hook its `OnClick()` event to `ScoreTrigger.AddScore()`.

8.  **Run the Scene:**
    *   Press Play.
    *   If `Add On Start` was checked on `ScoreTrigger`, you should immediately see the score update on your UI Text.
    *   If you added a button, click the button to see the score increase.
    *   Observe the Debug.Log messages from `ScoreManager`.

Now you have a fully functional ScriptableEvents system! You can extend this by adding more listeners (e.g., an achievement system that listens for `OnScoreChangedEvent` to unlock achievements) or more raisers (e.g., an enemy script that calls `ScoreManager.AddScore()` when defeated).