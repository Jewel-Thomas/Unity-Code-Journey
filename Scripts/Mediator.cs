// Unity Design Pattern Example: Mediator
// This script demonstrates the Mediator pattern in Unity
// Generated automatically - ready to use in your Unity project

The Mediator design pattern is incredibly useful in Unity development, especially for managing complex UI interactions, game state changes, or orchestrating logic between loosely coupled game systems. It centralizes communication logic, preventing objects from referring to each other explicitly and thus reducing dependencies.

Here's a complete, practical C# Unity example demonstrating the Mediator pattern. This script is designed to be dropped into a Unity project and run immediately after setting up the scene as described in the comments.

**Scenario:** We'll simulate a simple game UI where player health, mana, an inventory button, and a notification system all need to interact. A `GameStatsUpdater` component will simulate changes in player stats. The `UIMediator` will orchestrate all these interactions.

---

```csharp
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text and Button
using System.Collections; // Required for Coroutines (IEnumerator)
using System.Collections.Generic; // Not strictly needed for this example, but often useful

// --- Mediator Design Pattern Example in Unity ---
//
// This script demonstrates the 'Mediator' design pattern, which defines an object
// that encapsulates how a set of objects interact. This pattern is useful for
// reducing coupling between components by preventing them from referring to each other
// explicitly. Instead, they communicate through a central Mediator.
//
// In this example, we have:
// 1.  IMediator: The interface that defines the contract for our Mediator.
// 2.  UIMediator: The concrete Mediator, responsible for routing messages between UI components.
// 3.  UIPanelComponent: A base class for all UI components (Colleagues) that will
//     interact via the Mediator.
// 4.  Concrete Colleagues:
//     - PlayerHealthDisplay: Shows player's health.
//     - PlayerManaDisplay: Shows player's mana.
//     - InventoryButton: A button that, when clicked, triggers an action.
//     - NotificationTextDisplay: Displays temporary messages.
//     - GameStatsUpdater: Simulates a game system that updates player stats
//       and sends general notifications.
//
// The UIMediator will connect a 'GameStatsUpdater' (simulating game logic) to various
// UI elements (health, mana, notifications) and handle interactions originating
// from UI elements (like an inventory button click), routing them to other relevant components.

// --- 1. Mediator Interface ---
/// <summary>
/// The IMediator interface declares a method for notifying the mediator about events.
/// This is the contract for how 'colleagues' (UI components, game logic components)
/// can communicate with the mediator.
/// </summary>
public interface IMediator
{
    // The Notify method takes the sender, an event type (string for flexibility),
    // and optional data. The Mediator uses this information to decide how to
    // route the message to other relevant colleagues.
    void Notify(UIPanelComponent sender, string eventType, object data = null);
}

// --- 2. Colleague Base Class ---
/// <summary>
/// The abstract base class for all UI components (Colleagues) that will interact
/// via the Mediator.
/// Each colleague holds a reference to its Mediator and provides a helper method
/// to send notifications *only* to the Mediator. Colleagues do not talk directly
/// to each other.
/// </summary>
public abstract class UIPanelComponent : MonoBehaviour
{
    // Protected reference to the mediator instance.
    protected IMediator _mediator;

    /// <summary>
    /// Sets the mediator for this component. The mediator is typically injected
    /// into each colleague during initialization (e.g., in the Mediator's Awake method).
    /// </summary>
    /// <param name="mediator">The mediator instance to set.</param>
    public void SetMediator(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Helper method for components to notify the mediator about an event.
    /// This is the only way colleagues should communicate their state changes
    /// or user interactions to the system.
    /// </summary>
    /// <param name="eventType">A string identifying the type of event (e.g., "PlayerStatsUpdate", "InventoryToggle").</param>
    /// <param name="data">Optional data associated with the event (e.g., new health value, a message string).</param>
    protected void SendNotification(string eventType, object data = null)
    {
        // Null check for the mediator to prevent errors if not set up correctly.
        _mediator?.Notify(this, eventType, data);
    }
}

// --- Data Structures (for type-safe data transfer between Colleagues via Mediator) ---
/// <summary>
/// A simple data structure to pass player stats updates in a type-safe manner.
/// Using specific classes/structs for data payload is better than just 'object'
/// for clarity and compile-time checking.
/// </summary>
public class PlayerStatsUpdateData
{
    public int Health;
    public int Mana;
}

// --- 3. Concrete Colleagues ---

/// <summary>
/// Concrete Colleague: Displays the player's current health.
/// It only knows how to update its own display (its internal responsibility)
/// and receives commands from the UIMediator. It has no knowledge of who
/// changed the health or what other UI elements exist.
/// </summary>
public class PlayerHealthDisplay : UIPanelComponent
{
    [SerializeField] private Text _healthText; // Reference to a Unity UI Text component

    /// <summary>
    /// Updates the displayed health value on the UI.
    /// </summary>
    /// <param name="health">The new health value to display.</param>
    public void UpdateHealth(int health)
    {
        if (_healthText != null)
        {
            _healthText.text = $"Health: {health}";
        }
        Debug.Log($"<color=cyan>[HealthDisplay]</color> Health updated to: {health}");
    }
}

/// <summary>
/// Concrete Colleague: Displays the player's current mana.
/// Similar to PlayerHealthDisplay, it receives updates exclusively via the UIMediator.
/// </summary>
public class PlayerManaDisplay : UIPanelComponent
{
    [SerializeField] private Text _manaText; // Reference to a Unity UI Text component

    /// <summary>
    /// Updates the displayed mana value on the UI.
    /// </summary>
    /// <param name="mana">The new mana value to display.</param>
    public void UpdateMana(int mana)
    {
        if (_manaText != null)
        {
            _manaText.text = $"Mana: {mana}";
        }
        Debug.Log($"<color=cyan>[ManaDisplay]</color> Mana updated to: {mana}");
    }
}

/// <summary>
/// Concrete Colleague: Represents an in-game inventory button.
/// When clicked, it notifies the mediator about the event ("InventoryToggle"),
/// but it doesn't know what action will actually be performed (e.g., opening an
/// inventory panel, playing a sound). The Mediator handles that decision.
/// </summary>
public class InventoryButton : UIPanelComponent
{
    [SerializeField] private Button _button; // Reference to a Unity UI Button component

    void Awake()
    {
        // Subscribe to the button's onClick event.
        if (_button != null)
        {
            _button.onClick.AddListener(OnInventoryButtonClick);
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks.
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnInventoryButtonClick);
        }
    }

    /// <summary>
    /// Callback for when the inventory button is clicked. It sends a notification
    /// to the mediator, signaling that an inventory toggle event has occurred.
    /// </summary>
    public void OnInventoryButtonClick()
    {
        SendNotification("InventoryToggle"); // Notify the mediator
        Debug.Log($"<color=magenta>[InventoryButton]</color> Inventory button clicked! Sending notification...");
    }
}

/// <summary>
/// Concrete Colleague: Displays temporary notifications to the player.
/// It receives messages (strings) from the UIMediator and handles the logic
/// for displaying them for a certain duration.
/// </summary>
public class NotificationTextDisplay : UIPanelComponent
{
    [SerializeField] private Text _notificationText; // Reference to a Unity UI Text component
    private Coroutine _displayRoutine; // Used to manage the message display duration

    /// <summary>
    /// Displays a message on the UI for a specified duration.
    /// If another message is already being displayed, it will be interrupted.
    /// </summary>
    /// <param name="message">The string message to display.</param>
    /// <param name="duration">How long (in seconds) the message should be visible.</param>
    public void DisplayMessage(string message, float duration = 3f)
    {
        if (_notificationText != null)
        {
            // Stop any existing display routine to show the new message immediately.
            if (_displayRoutine != null)
            {
                StopCoroutine(_displayRoutine);
            }
            _notificationText.text = message;
            _displayRoutine = StartCoroutine(ClearMessageAfterDelay(duration));
        }
        Debug.Log($"<color=yellow>[Notification]</color> Displaying: \"{message}\"");
    }

    /// <summary>
    /// Coroutine to clear the notification message after a delay.
    /// </summary>
    private IEnumerator ClearMessageAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (_notificationText != null)
        {
            _notificationText.text = ""; // Clear the text
        }
        _displayRoutine = null; // Reset the routine reference
    }
}

/// <summary>
/// Concrete Colleague: Simulates a game system (e.g., a PlayerController or GameManager)
/// that updates player statistics (health, mana) and needs to notify the UI about
/// these changes. It also generates general notification messages.
/// This component doesn't know specifically which UI elements will update; it only
/// knows that it needs to send notifications to the mediator.
/// </summary>
public class GameStatsUpdater : UIPanelComponent
{
    [SerializeField] private int _initialHealth = 100;
    [SerializeField] private int _initialMana = 50;

    private int _currentHealth;
    private int _currentMana;

    void Start()
    {
        _currentHealth = _initialHealth;
        _currentMana = _initialMana;

        // Immediately update UI with initial stats using the mediator.
        UpdateStatsAndNotify();

        // Start a coroutine to simulate various game events over time.
        StartCoroutine(SimulateGameEvents());
    }

    /// <summary>
    /// Simulates a sequence of game events like taking damage, recovering, and dying.
    /// Each event triggers a notification to the mediator.
    /// </summary>
    private IEnumerator SimulateGameEvents()
    {
        yield return new WaitForSeconds(2);
        _currentHealth -= 20;
        _currentMana -= 10;
        UpdateStatsAndNotify(); // Notify about stat changes
        SendNotification("NotificationMessage", "You took some damage!"); // Send a general notification

        yield return new WaitForSeconds(3);
        _currentHealth += 15;
        _currentMana += 5;
        UpdateStatsAndNotify();
        SendNotification("NotificationMessage", "You recovered some health and mana!");

        yield return new WaitForSeconds(4);
        _currentHealth = 0; // Simulate player death
        UpdateStatsAndNotify(); // Send one last stats update
        SendNotification("PlayerDied"); // Notify the mediator specifically about player death
    }

    /// <summary>
    /// Bundles current stats into a PlayerStatsUpdateData object and sends
    /// a "PlayerStatsUpdate" notification to the mediator.
    /// </summary>
    private void UpdateStatsAndNotify()
    {
        PlayerStatsUpdateData data = new PlayerStatsUpdateData
        {
            Health = _currentHealth,
            Mana = _currentMana
        };
        SendNotification("PlayerStatsUpdate", data); // Pass the data payload
        Debug.Log($"<color=green>[GameStatsUpdater]</color> Sent 'PlayerStatsUpdate' (HP:{_currentHealth}, MP:{_currentMana})");
    }

    /// <summary>
    /// An example of an external method that could trigger a stat change,
    /// demonstrating how game logic interacts with the mediator.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        if (_currentHealth < 0) _currentHealth = 0;
        SendNotification("NotificationMessage", $"You took {amount} damage!"); // Direct notification
        UpdateStatsAndNotify(); // Also update stats displays
        if (_currentHealth == 0)
        {
            SendNotification("PlayerDied");
        }
        Debug.Log($"<color=green>[GameStatsUpdater]</color> Player took {amount} damage.");
    }
}


// --- 4. Concrete Mediator ---
/// <summary>
/// The Concrete Mediator class. It centralizes all communication logic between its
/// registered colleagues. Instead of colleagues talking directly to each other,
/// they all notify the UIMediator, and the UIMediator decides how to interpret
/// the notification and which other colleagues should react.
/// </summary>
public class UIMediator : MonoBehaviour, IMediator
{
    // These SerializedFields allow us to link the various UI components and
    // game logic components (our "colleagues") from the Unity Inspector.
    // The UIMediator holds references to all components it manages.
    [Header("Mediated UI Components")]
    [SerializeField] private PlayerHealthDisplay _healthDisplay;
    [SerializeField] private PlayerManaDisplay _manaDisplay;
    [SerializeField] private InventoryButton _inventoryButton;
    [SerializeField] private NotificationTextDisplay _notificationTextDisplay;

    [Header("Mediated Game Logic Components (e.g., source of updates)")]
    [SerializeField] private GameStatsUpdater _gameStatsUpdater;

    void Awake()
    {
        // IMPORTANT: Inject this mediator instance into all its colleagues.
        // This establishes the vital communication link, allowing colleagues
        // to call _mediator.Notify() when events occur.
        _healthDisplay?.SetMediator(this);
        _manaDisplay?.SetMediator(this);
        _inventoryButton?.SetMediator(this);
        _notificationTextDisplay?.SetMediator(this);
        _gameStatsUpdater?.SetMediator(this);

        Debug.Log("<color=red>[UIMediator]</color> Initialized and injected itself into all registered colleagues.");
    }

    /// <summary>
    /// The core method of the Mediator. This is the central control point
    /// where all communication from colleagues is received.
    /// The mediator then decides which other colleagues (or itself) should react
    /// to the event based on the sender, event type, and data.
    /// </summary>
    /// <param name="sender">The component that sent the notification.</param>
    /// <param name="eventType">A string identifying the type of event (e.g., "PlayerStatsUpdate").</param>
    /// <param name="data">Optional data associated with the event (e.g., PlayerStatsUpdateData).</param>
    public void Notify(UIPanelComponent sender, string eventType, object data = null)
    {
        Debug.Log($"<color=red>[UIMediator]</color> Received '{eventType}' from {sender.name}");

        // --- Event Handling Logic ---
        // This is where the Mediator's intelligence and all interaction logic resides.
        // It defines how different components react to events from others.

        // Handle PlayerStatsUpdate event, typically from GameStatsUpdater
        if (eventType == "PlayerStatsUpdate")
        {
            if (sender == _gameStatsUpdater && data is PlayerStatsUpdateData stats)
            {
                // Update specific UI elements based on the received stats.
                _healthDisplay?.UpdateHealth(stats.Health);
                _manaDisplay?.UpdateMana(stats.Mana);
                // The mediator also decides to show a general notification for this event.
                _notificationTextDisplay?.DisplayMessage($"Stats updated: HP {stats.Health}, MP {stats.Mana}", 1.5f);
            }
        }
        // Handle InventoryToggle event, typically from InventoryButton
        else if (eventType == "InventoryToggle")
        {
            if (sender == _inventoryButton)
            {
                // Here, the mediator translates the button click into a notification
                // for the NotificationTextDisplay. In a real game, this might also
                // trigger an actual InventoryPanel to open/close (if we had one).
                _notificationTextDisplay?.DisplayMessage("Inventory panel toggled!", 1.5f);
                // Example: If we had an InventoryPanel colleague, we would call its method here:
                // _inventoryPanel?.ToggleVisibility();
            }
        }
        // Handle PlayerDied event, typically from GameStatsUpdater
        else if (eventType == "PlayerDied")
        {
            if (sender == _gameStatsUpdater)
            {
                // Display a critical message and potentially trigger other game over logic.
                _notificationTextDisplay?.DisplayMessage("You died! Game Over!", 5f);
                // Example: Activate a game over screen:
                // _gameOverScreen?.Show();
                // Example: Pause game, reset level, etc.
            }
        }
        // Handle general NotificationMessage event, which can come from any colleague
        else if (eventType == "NotificationMessage" && data is string message)
        {
            // This allows any colleague to send a general text notification,
            // and the mediator ensures it reaches the NotificationTextDisplay.
            _notificationTextDisplay?.DisplayMessage(message, 1.5f);
        }
        // Add more event handling as needed for other interactions and event types.
    }
}

/*
--- How to Use This Example in Unity ---

To get this Mediator pattern example up and running in your Unity project,
follow these steps:

1.  **Create the C# Script:**
    -   In your Unity project's `Assets` folder, right-click -> `Create` -> `C# Script`.
    -   Name it exactly `UIMediatorExample` (or whatever you name the main `UIMediator` class).
    -   Copy and paste the ENTIRE content of this file into the newly created script, overwriting any default content.

2.  **Set Up the UI Canvas:**
    -   Right-click in the Hierarchy panel -> `UI` -> `Canvas`.
    -   Select the `Canvas` GameObject. In its Inspector:
        -   Set `Render Mode` to "Screen Space - Camera" and drag your `Main Camera` into the `Render Camera` slot.
        -   Set `UI Scale Mode` to "Scale With Screen Size" (e.g., `Reference Resolution` 1920x1080 for consistency, `Screen Match Mode` to "Match Width Or Height", `Match` 0.5).

3.  **Create UI Elements (Colleagues) and Corresponding GameObjects:**

    For each UI element below, create an empty GameObject as a parent, then add the UI component and the custom script component to it. This helps organize your scene.

    -   **Player Health Display:**
        -   Right-click on `Canvas` -> `Create Empty`. Rename it `PlayerHealthDisplayObj`.
        -   Add Component to `PlayerHealthDisplayObj` -> search for `PlayerHealthDisplay`.
        -   Right-click on `PlayerHealthDisplayObj` -> `UI` -> `Text (Legacy - Text)`. Rename it `HealthTextUI`.
        -   Adjust `HealthTextUI`'s Rect Transform (e.g., top-left corner, size 200x50). Set its font size, color, and initial text (e.g., "Health: 100").
        -   Select `PlayerHealthDisplayObj` again. Drag `HealthTextUI` from the Hierarchy into the `_healthText` field of its `PlayerHealthDisplay` component in the Inspector.

    -   **Player Mana Display:**
        -   Right-click on `Canvas` -> `Create Empty`. Rename it `PlayerManaDisplayObj`.
        -   Add Component to `PlayerManaDisplayObj` -> search for `PlayerManaDisplay`.
        -   Right-click on `PlayerManaDisplayObj` -> `UI` -> `Text (Legacy - Text)`. Rename it `ManaTextUI`.
        -   Adjust `ManaTextUI`'s Rect Transform (e.g., below Health, size 200x50). Set its font size, color, and initial text (e.g., "Mana: 50").
        -   Select `PlayerManaDisplayObj`. Drag `ManaTextUI` into the `_manaText` field of its `PlayerManaDisplay` component.

    -   **Inventory Button:**
        -   Right-click on `Canvas` -> `Create Empty`. Rename it `InventoryButtonObj`.
        -   Add Component to `InventoryButtonObj` -> search for `InventoryButton`.
        -   Right-click on `InventoryButtonObj` -> `UI` -> `Button (Legacy - Button)`. Rename it `InventoryButtonUI`.
        -   Adjust `InventoryButtonUI`'s Rect Transform (e.g., bottom-left corner, size 160x60). Double-click `InventoryButtonUI` to select its child `Text` and change the text to "Inventory".
        -   Select `InventoryButtonObj`. Drag `InventoryButtonUI` into the `_button` field of its `InventoryButton` component.

    -   **Notification Text Display:**
        -   Right-click on `Canvas` -> `Create Empty`. Rename it `NotificationTextDisplayObj`.
        -   Add Component to `NotificationTextDisplayObj` -> search for `NotificationTextDisplay`.
        -   Right-click on `NotificationTextDisplayObj` -> `UI` -> `Text (Legacy - Text)`. Rename it `NotificationTextUI`.
        -   Adjust `NotificationTextUI`'s Rect Transform (e.g., center-bottom, large width/height like 800x100). Set its font size (e.g., 30), alignment (center), and color.
        -   Select `NotificationTextDisplayObj`. Drag `NotificationTextUI` into the `_notificationText` field of its `NotificationTextDisplay` component.

4.  **Create Game Logic Component (another Colleague):**
    -   Create an empty `GameObject` in the Hierarchy, name it `GameStatsUpdaterObj`.
    -   Add Component to `GameStatsUpdaterObj` -> search for `GameStatsUpdater`.
    -   You can leave its `_initialHealth` and `_initialMana` at default values (100, 50).

5.  **Create the Mediator GameObject:**
    -   Create an empty `GameObject` in the Hierarchy, name it `UIMediatorObj`.
    -   Add Component to `UIMediatorObj` -> search for `UIMediator`.

6.  **Connect All Colleagues to the Mediator:**
    -   Select `UIMediatorObj` in the Hierarchy.
    -   In its `UIMediator` component in the Inspector, you will see several empty fields (`_healthDisplay`, `_manaDisplay`, etc.).
    -   Drag the respective parent GameObjects you created in step 3 and 4 into these fields:
        -   Drag `PlayerHealthDisplayObj` into the `_healthDisplay` slot.
        -   Drag `PlayerManaDisplayObj` into the `_manaDisplay` slot.
        -   Drag `InventoryButtonObj` into the `_inventoryButton` slot.
        -   Drag `NotificationTextDisplayObj` into the `_notificationTextDisplay` slot.
        -   Drag `GameStatsUpdaterObj` into the `_gameStatsUpdater` slot.

7.  **Run the Scene:**
    -   Save your scene (Ctrl+S or Cmd+S).
    -   Press the Play button in the Unity Editor.
    -   You should observe:
        -   The health and mana displays updating over time, driven by `GameStatsUpdater`.
        -   Notification messages appearing from both `GameStatsUpdater`'s events and direct messages.
        -   Clicking the "Inventory" button will trigger a notification.
    -   Open the Console window to see the detailed debug logs from each component and the Mediator, illustrating the communication flow.

This setup vividly demonstrates how the Mediator pattern centralizes control and communication, significantly reducing direct dependencies between objects. Each colleague only knows about the mediator, not about other specific colleagues, making the system more modular, manageable, and easier to extend or modify.
*/
```