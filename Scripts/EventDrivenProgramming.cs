// Unity Design Pattern Example: EventDrivenProgramming
// This script demonstrates the EventDrivenProgramming pattern in Unity
// Generated automatically - ready to use in your Unity project

The Event-Driven Programming (EDP) pattern is fundamental for building decoupled and scalable systems in Unity. It allows different parts of your application to communicate without having direct references to each other. This is achieved through a central `EventManager` that acts as a "message bus," where components can "publish" events and other components can "subscribe" to them.

---

### Key Concepts Explained:

1.  **EventManager (The Hub):**
    *   A central, static class responsible for managing event subscriptions and publications.
    *   It holds a collection of delegates (event handlers), typically stored in a `Dictionary`.
    *   It provides methods to `Subscribe`, `Unsubscribe`, and `Publish` events.
    *   Using generics (`<TEventData>`) makes it highly flexible, allowing any type of data payload to be passed with an event.

2.  **Event Data Classes (The Message):**
    *   Simple C# classes that encapsulate the data associated with a specific event.
    *   E.g., `CoinCollectedEventData` would contain `int Amount`.
    *   Using distinct event data classes provides strong typing, clarity, and prevents errors compared to passing generic `object` or `params object[]`.

3.  **Publishers (The Broadcaster):**
    *   Components that detect an action or state change and then notify other systems by "publishing" an event through the `EventManager`.
    *   They don't know who is listening or what those listeners will do with the event. They just announce that something happened.
    *   E.g., `PlayerCoinCollector` publishes `CoinCollectedEventData` when the player picks up a coin.

4.  **Subscribers (The Listener):**
    *   Components that are interested in specific events. They "subscribe" to these events via the `EventManager`.
    *   When an event they are subscribed to is published, their registered callback method is invoked.
    *   They don't know who published the event. They just react to it.
    *   E.g., `UICoinDisplay`, `CoinSoundPlayer`, and `AchievementTracker` subscribe to `CoinCollectedEventData` to update UI, play sound, and check achievements, respectively.

5.  **Decoupling:**
    *   The core benefit! Publishers and subscribers don't have direct references to each other. This means:
        *   **Reduced Dependencies:** Changing one system has minimal impact on others.
        *   **Increased Reusability:** Components can be easily swapped or used in different contexts.
        *   **Easier Maintenance:** Code becomes simpler to understand and debug.
        *   **Scalability:** Adding new features (new subscribers) doesn't require modifying existing publishers.

6.  **`OnEnable` & `OnDisable`:**
    *   In Unity, it is *critical* to subscribe to events in `OnEnable()` and unsubscribe in `OnDisable()`.
    *   **Subscribe in `OnEnable()`:** Ensures the component only receives events when it's active.
    *   **Unsubscribe in `OnDisable()`:** Prevents memory leaks and `NullReferenceException` errors if the GameObject is disabled or destroyed, but the `EventManager` still tries to invoke its method.

---

### Real-World Use Case: Player Collects Coins

Imagine a scenario where a player collects coins in a game. Several systems need to react to this:

1.  **UI System:** Updates the coin count displayed on the screen.
2.  **Sound System:** Plays a "coin collected" sound effect.
3.  **Achievement System:** Checks if any coin-related achievements have been unlocked.

Without EDP, the `Player` script would need direct references to the `UIManager`, `SoundManager`, and `AchievementManager`, leading to tight coupling. With EDP, the `Player` simply publishes an event, and the other systems react independently.

---

### Setup in Unity:

1.  **Create a new Unity Project.**
2.  **Install TextMeshPro:** Go to `Window > TextMeshPro > Import TMP Essential Resources` (if you plan to use `TextMeshProUGUI` for UI).
3.  **Create `Scripts` Folder:** In your Project window, create a folder named `Scripts`.
4.  **Create C# Scripts:** Create the following C# scripts in the `Scripts` folder and copy the respective code into them:
    *   `EventManager.cs`
    *   `CoinCollectedEventData.cs`
    *   `PlayerCoinCollector.cs`
    *   `UICoinDisplay.cs`
    *   `CoinSoundPlayer.cs`
    *   `AchievementTracker.cs`
5.  **Unity Scene Setup:**
    *   **Player GameObject:** Create an empty GameObject named `Player`. Add the `PlayerCoinCollector` component to it.
    *   **UI Canvas & Text:**
        *   Create a UI Canvas (`GameObject > UI > Canvas`).
        *   Inside the Canvas, create a `Text - TextMeshPro` object (`GameObject > UI > Text - TextMeshPro`). Name it `CoinCountText`.
        *   Position it somewhere visible (e.g., top-left).
        *   Add the `UICoinDisplay` component to the `CoinCountText` GameObject.
        *   In the Inspector for `UICoinDisplay`, drag the `CoinCountText` TextMeshProUGUI component itself into the `_coinText` field.
    *   **Managers GameObject:** Create an empty GameObject named `GameManagers`.
        *   Add the `CoinSoundPlayer` component to it.
            *   Add an `AudioSource` component to `GameManagers`.
            *   In `CoinSoundPlayer`, assign an `AudioClip` to the `_coinCollectSound` field (you can use a placeholder, or a simple sound effect from the Asset Store or your own collection).
        *   Add the `AchievementTracker` component to it.
6.  **Run the Scene:**
    *   Press the `Play` button in Unity.
    *   Press the **Spacebar** (default key configured in `PlayerCoinCollector`) repeatedly.
    *   Observe the following:
        *   The **UI Text** for "Coins" updates.
        *   A **sound effect** plays (if an AudioClip was assigned).
        *   **Debug Logs** appear in the Console from `PlayerCoinCollector`, `UICoinDisplay`, `CoinSoundPlayer`, and `AchievementTracker`, showing how each system reacts.
        *   **Achievement messages** will appear when coin thresholds are met.

---

### 1. `EventManager.cs`

This is the central static class responsible for managing event subscriptions and publications.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central static EventManager for Event-Driven Programming in Unity.
/// It provides a mechanism for different systems to communicate without direct dependencies.
/// Events are strongly typed using generics, ensuring type safety and clarity.
/// </summary>
public static class EventManager
{
    // A dictionary to store different types of events.
    // Key: The Type of the event data (e.g., typeof(CoinCollectedEventData)).
    // Value: A Delegate, which is the base type for all event handlers (Action<T>).
    private static readonly Dictionary<Type, Delegate> s_eventListeners = new Dictionary<Type, Delegate>();

    /// <summary>
    /// Subscribes a listener (an Action method) to an event of a specific type.
    /// When an event of TEventData type is published, the listener will be invoked.
    /// </summary>
    /// <typeparam name="TEventData">The type of the event data payload.</typeparam>
    /// <param name="listener">The action method to be invoked when the event occurs.</param>
    public static void Subscribe<TEventData>(Action<TEventData> listener)
    {
        Type eventType = typeof(TEventData);

        // If the event type doesn't exist in our dictionary, add it with a null delegate initially.
        if (!s_eventListeners.ContainsKey(eventType))
        {
            s_eventListeners.Add(eventType, null);
        }

        // Use Delegate.Combine to safely add the new listener to the existing delegate chain.
        // This handles cases where there are no existing listeners or multiple existing ones.
        s_eventListeners[eventType] = Delegate.Combine(s_eventListeners[eventType], listener);

        // Debug.Log($"[EventManager] Subscribed listener to event: {eventType.Name}");
    }

    /// <summary>
    /// Unsubscribes a listener from an event of a specific type.
    /// It's crucial to unsubscribe when an object is destroyed or disabled to prevent memory leaks
    /// and errors (e.g., trying to call a method on a destroyed object).
    /// </summary>
    /// <typeparam name="TEventData">The type of the event data payload.</typeparam>
    /// <param name="listener">The action method to be removed from the event.</param>
    public static void Unsubscribe<TEventData>(Action<TEventData> listener)
    {
        Type eventType = typeof(TEventData);

        // Only attempt to unsubscribe if the event type exists in our dictionary.
        if (s_eventListeners.TryGetValue(eventType, out Delegate existingDelegate))
        {
            // Use Delegate.Remove to safely remove the listener from the delegate chain.
            s_eventListeners[eventType] = Delegate.Remove(existingDelegate, listener);

            // If no listeners remain for this event type, remove its entry from the dictionary.
            // This helps clean up unused event entries and optimize memory.
            if (s_eventListeners[eventType] == null)
            {
                s_eventListeners.Remove(eventType);
            }

            // Debug.Log($"[EventManager] Unsubscribed listener from event: {eventType.Name}");
        }
    }

    /// <summary>
    /// Publishes an event, triggering all subscribed listeners for that event type.
    /// </summary>
    /// <typeparam name="TEventData">The type of the event data payload.</typeparam>
    /// <param name="eventData">The data payload associated with the event.</param>
    public static void Publish<TEventData>(TEventData eventData)
    {
        Type eventType = typeof(TEventData);

        // Check if there are any listeners registered for this specific event type.
        if (s_eventListeners.TryGetValue(eventType, out Delegate rawDelegate))
        {
            // Cast the raw Delegate back to the specific Action<TEventData> type.
            // This cast is safe because we ensured the delegate was added with this type.
            Action<TEventData> typedDelegate = rawDelegate as Action<TEventData>;

            // Invoke all subscribed listeners with the provided event data.
            // The null check 'typedDelegate?' is important because it's possible
            // that a delegate was removed, leaving it null.
            typedDelegate?.Invoke(eventData);

            // Debug.Log($"[EventManager] Published event: {eventType.Name} with data: {eventData}");
        }
        // else
        // {
        //     Debug.LogWarning($"[EventManager] Attempted to publish event '{eventType.Name}' but no listeners were subscribed.");
        // }
    }
}
```

### 2. `CoinCollectedEventData.cs`

This class defines the data payload for when a coin is collected.

```csharp
using UnityEngine; // Included for general Unity context, though not strictly needed here.

/// <summary>
/// Represents the data payload for a 'Coin Collected' event.
/// Using specific classes for event data provides strong typing and clarity,
/// allowing you to pass structured information with each event.
/// </summary>
public class CoinCollectedEventData
{
    public int Amount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoinCollectedEventData"/> class.
    /// </summary>
    /// <param name="amount">The number of coins collected in this event.</param>
    public CoinCollectedEventData(int amount)
    {
        Amount = amount;
    }

    // Optional: Override ToString for easier debugging in console logs.
    public override string ToString()
    {
        return $"CoinCollectedEventData [Amount: {Amount}]";
    }
}
```

### 3. `PlayerCoinCollector.cs` (Publisher)

This script simulates a player collecting coins and publishes the event.

```csharp
using UnityEngine;

/// <summary>
/// Simulates a player collecting coins and publishes a 'CoinCollectedEventData' event.
/// This acts as a 'Publisher' in the Event-Driven Programming pattern.
/// It doesn't know which systems (subscribers) will react to this event,
/// demonstrating loose coupling.
/// </summary>
public class PlayerCoinCollector : MonoBehaviour
{
    [Tooltip("The amount of coins to collect with each press.")]
    [SerializeField] private int _coinAmountPerCollection = 1;
    [Tooltip("The key to press to simulate collecting a coin.")]
    [SerializeField] private KeyCode _collectCoinKey = KeyCode.Space;

    // A simple internal counter, mainly for demonstration of total collected by this player.
    private int _totalCoinsCollectedByPlayer = 0;

    void Update()
    {
        // Simulate collecting a coin when the specified key is pressed.
        if (Input.GetKeyDown(_collectCoinKey))
        {
            CollectCoin(_coinAmountPerCollection);
        }
    }

    /// <summary>
    /// Simulates the act of collecting coins.
    /// After internal processing (if any), it publishes an event to notify other systems.
    /// </summary>
    /// <param name="amount">The number of coins collected.</param>
    public void CollectCoin(int amount)
    {
        // Update internal state
        _totalCoinsCollectedByPlayer += amount;
        Debug.Log($"[PlayerCoinCollector] Player collected {amount} coins. Total collected by player: {_totalCoinsCollectedByPlayer}");

        // Create an instance of the event data, packaging the relevant information.
        CoinCollectedEventData eventData = new CoinCollectedEventData(amount);

        // Publish the event through the static EventManager.
        // Any system subscribed to 'CoinCollectedEventData' will now receive this event.
        // The PlayerCoinCollector doesn't need to know who is listening or what they will do.
        EventManager.Publish(eventData);

        Debug.Log($"[PlayerCoinCollector] Event Published: {eventData}");
    }
}
```

### 4. `UICoinDisplay.cs` (Subscriber)

This script updates a UI Text element based on coin collection events.

```csharp
using UnityEngine;
using TMPro; // Required for TextMeshPro. Ensure you've imported TMP Essential Resources.

/// <summary>
/// Updates a UI TextMeshProUGUI element to display the current coin count.
/// This acts as a 'Subscriber' to the 'CoinCollectedEventData' event.
/// It reacts to the event without knowing who published it.
/// </summary>
public class UICoinDisplay : MonoBehaviour
{
    [Tooltip("The TextMeshProUGUI component to display the coin count.")]
    [SerializeField] private TextMeshProUGUI _coinText;

    private int _currentCoinCount = 0;

    void Awake()
    {
        // Basic validation: Ensure the TextMeshProUGUI component is assigned.
        if (_coinText == null)
        {
            _coinText = GetComponent<TextMeshProUGUI>();
            if (_coinText == null)
            {
                Debug.LogError("[UICoinDisplay] requires a TextMeshProUGUI component or one assigned in the inspector. Disabling script.", this);
                enabled = false; // Disable this script if essential component is missing
                return;
            }
        }
        UpdateCoinDisplayText(); // Initialize display with 0 coins.
    }

    /// <summary>
    /// Subscribes to the 'CoinCollectedEventData' when this GameObject is enabled.
    /// It's crucial to subscribe here (or in Start) to ensure it's active when the object is.
    /// </summary>
    void OnEnable()
    {
        // Register the 'OnCoinCollected' method to be called when a CoinCollectedEventData is published.
        EventManager.Subscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[UICoinDisplay] Subscribed to CoinCollectedEventData.");
    }

    /// <summary>
    /// Unsubscribes from the 'CoinCollectedEventData' when this GameObject is disabled or destroyed.
    /// This is CRITICAL to prevent memory leaks and 'NullReferenceException' errors
    /// if the GameObject is destroyed but the EventManager still tries to invoke its method.
    /// </summary>
    void OnDisable()
    {
        // Unregister the 'OnCoinCollected' method.
        EventManager.Unsubscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[UICoinDisplay] Unsubscribed from CoinCollectedEventData.");
    }

    /// <summary>
    /// This method is the event handler. It's called by the EventManager
    /// whenever a 'CoinCollectedEventData' event is published.
    /// </summary>
    /// <param name="eventData">The data payload associated with the coin collection event.</param>
    private void OnCoinCollected(CoinCollectedEventData eventData)
    {
        _currentCoinCount += eventData.Amount;
        UpdateCoinDisplayText();
        Debug.Log($"[UICoinDisplay] Received CoinCollectedEvent. Current coins: {_currentCoinCount}");
    }

    /// <summary>
    /// Updates the text component with the current coin count.
    /// </summary>
    private void UpdateCoinDisplayText()
    {
        if (_coinText != null)
        {
            _coinText.text = $"Coins: {_currentCoinCount}";
        }
    }
}
```

### 5. `CoinSoundPlayer.cs` (Subscriber)

This script plays a sound effect when a coin is collected.

```csharp
using UnityEngine;

/// <summary>
/// Plays an audio clip when a 'CoinCollectedEventData' event is published.
/// This acts as another 'Subscriber', reacting to the event without direct knowledge
/// of the publisher.
/// </summary>
public class CoinSoundPlayer : MonoBehaviour
{
    [Tooltip("Audio clip to play when a coin is collected.")]
    [SerializeField] private AudioClip _coinCollectSound;
    [Tooltip("AudioSource component to play the sound.")]
    [SerializeField] private AudioSource _audioSource;

    void Awake()
    {
        // Ensure an AudioSource is assigned or try to get one.
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                Debug.LogWarning("[CoinSoundPlayer] No AudioSource assigned or found on GameObject. Adding one automatically.", this);
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    /// <summary>
    /// Subscribes to the 'CoinCollectedEventData' when this GameObject is enabled.
    /// </summary>
    void OnEnable()
    {
        EventManager.Subscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[CoinSoundPlayer] Subscribed to CoinCollectedEventData.");
    }

    /// <summary>
    /// Unsubscribes from the 'CoinCollectedEventData' when this GameObject is disabled or destroyed.
    /// </summary>
    void OnDisable()
    {
        EventManager.Unsubscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[CoinSoundPlayer] Unsubscribed from CoinCollectedEventData.");
    }

    /// <summary>
    /// This method is the event handler. It's called when a 'CoinCollectedEventData' is published.
    /// It plays the assigned coin collection sound.
    /// </summary>
    /// <param name="eventData">The data payload associated with the coin collection event.</param>
    private void OnCoinCollected(CoinCollectedEventData eventData)
    {
        if (_coinCollectSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_coinCollectSound);
            Debug.Log($"[CoinSoundPlayer] Received CoinCollectedEvent. Playing sound for {eventData.Amount} coins.");
        }
        else
        {
            Debug.LogWarning("[CoinSoundPlayer] Received CoinCollectedEvent but sound clip or audio source is missing.", this);
        }
    }
}
```

### 6. `AchievementTracker.cs` (Subscriber)

This script tracks coin collection and unlocks achievements.

```csharp
using UnityEngine;

/// <summary>
/// Tracks total coins collected and checks for achievement unlocks.
/// This acts as another 'Subscriber' to the 'CoinCollectedEventData' event.
/// </summary>
public class AchievementTracker : MonoBehaviour
{
    [Tooltip("The number of coins required for the 'Coin Hoarder' achievement.")]
    [SerializeField] private int _hoarderAchievementThreshold = 10;
    [Tooltip("The number of coins required for the 'Wealthy Tycoon' achievement.")]
    [SerializeField] private int _tycoonAchievementThreshold = 50;

    private int _totalCoinsEverCollected = 0;
    private bool _hoarderAchievementUnlocked = false;
    private bool _tycoonAchievementUnlocked = false;

    /// <summary>
    /// Subscribes to the 'CoinCollectedEventData' when this GameObject is enabled.
    /// </summary>
    void OnEnable()
    {
        EventManager.Subscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[AchievementTracker] Subscribed to CoinCollectedEventData.");
    }

    /// <summary>
    /// Unsubscribes from the 'CoinCollectedEventData' when this GameObject is disabled or destroyed.
    /// </summary>
    void OnDisable()
    {
        EventManager.Unsubscribe<CoinCollectedEventData>(OnCoinCollected);
        Debug.Log("[AchievementTracker] Unsubscribed from CoinCollectedEventData.");
    }

    /// <summary>
    /// This method is the event handler. It's called when a 'CoinCollectedEventData' is published.
    /// It updates the total coin count and checks for achievement unlocks.
    /// </summary>
    /// <param name="eventData">The data payload associated with the coin collection event.</param>
    private void OnCoinCollected(CoinCollectedEventData eventData)
    {
        _totalCoinsEverCollected += eventData.Amount;
        Debug.Log($"[AchievementTracker] Received CoinCollectedEvent. Total coins ever collected: {_totalCoinsEverCollected}");

        CheckAchievements();
    }

    /// <summary>
    /// Checks if any achievements have been unlocked based on the total coins collected.
    /// This method demonstrates how a subscriber performs its specific logic in response to an event.
    /// </summary>
    private void CheckAchievements()
    {
        if (!_hoarderAchievementUnlocked && _totalCoinsEverCollected >= _hoarderAchievementThreshold)
        {
            _hoarderAchievementUnlocked = true;
            Debug.Log($"--- ACHIEVEMENT UNLOCKED: Coin Hoarder! (Collected {_hoarderAchievementThreshold} coins) ---");
            // Optionally, this could also publish an 'AchievementUnlockedEventData'
            // for other systems (e.g., a UI Achievement Pop-up) to react to.
            // EventManager.Publish(new AchievementUnlockedEventData("Coin Hoarder", _hoarderAchievementThreshold));
        }

        if (!_tycoonAchievementUnlocked && _totalCoinsEverCollected >= _tycoonAchievementThreshold)
        {
            _tycoonAchievementUnlocked = true;
            Debug.Log($"--- ACHIEVEMENT UNLOCKED: Wealthy Tycoon! (Collected {_tycoonAchievementThreshold} coins) ---");
        }
    }
}
```