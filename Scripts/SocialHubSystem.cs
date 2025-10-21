// Unity Design Pattern Example: SocialHubSystem
// This script demonstrates the SocialHubSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'SocialHubSystem' design pattern, while not a standard GoF pattern, is a practical application of the **Mediator** and **Publisher-Subscriber (Event Bus)** patterns, specifically tailored for managing interconnected "social" features in a game (like friends, chat, profiles, guilds, etc.).

**Core Idea of the SocialHubSystem:**

The goal is to decouple various social modules from each other. Instead of a `FriendManager` directly calling methods on a `ChatManager` to update a friend's status, the `FriendManager` publishes a "friend status changed" event to a central `SocialHub`. Any module interested in friend status changes (like the `ChatManager` or `ProfileManager`) subscribes to this event via the `SocialHub` and reacts accordingly.

This central hub acts as a mediator, facilitating communication without direct dependencies between concrete social modules.

---

### Key Components of this Implementation:

1.  **`SocialHub` (Singleton MonoBehaviour):**
    *   The central orchestrator.
    *   Implements the **Singleton** pattern to provide a single, globally accessible instance.
    *   Acts as an **Event Bus**, holding a dictionary of event types to lists of subscribed callbacks.
    *   Provides `Subscribe<TEvent>`, `Unsubscribe<TEvent>`, and `Publish<TEvent>` methods.
2.  **Event Data Structures:**
    *   Simple C# classes (`FriendStatusChangedEvent`, `ChatMessageEvent`, `ProfileUpdatedEvent`).
    *   These carry all relevant data about a specific event. They make the event system strongly typed and self-descriptive.
3.  **Concrete Social Modules (MonoBehaviours):**
    *   `FriendManager`: Manages friend lists, adds/removes friends, sets online status. **Publishes** `FriendStatusChangedEvent`. **Subscribes** to `ProfileUpdatedEvent` (e.g., if it needs to react to profile updates).
    *   `ChatManager`: Handles sending/receiving messages. **Subscribes** to `FriendStatusChangedEvent` (to update friend online status in chat lists) and `ChatMessageEvent` (to display incoming messages). **Publishes** `ChatMessageEvent`.
    *   `ProfileManager`: Manages user profile data (name, level, friend count). **Publishes** `ProfileUpdatedEvent`. **Subscribes** to `FriendStatusChangedEvent` (to update internal friend count).

---

### Advantages of the SocialHubSystem:

*   **Decoupling:** Modules don't know about each other, only about the `SocialHub` and the event types. This minimizes dependencies.
*   **Modularity:** New social features can be added as new modules without modifying existing code. They just subscribe to or publish relevant events.
*   **Flexibility:** Event data can be extended easily.
*   **Testability:** Individual modules can be tested in isolation, mocking the `SocialHub` if necessary.
*   **Maintainability:** Changes in one module are less likely to break others.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For simple debugging in demo, not strictly needed for core pattern.

// --- 1. Define Event Data Structures ---
// These are simple classes that carry information about an event.
// They make the event system strongly typed and self-descriptive.
// We override ToString() for better debug logging.

/// <summary>
/// Event data for when a friend's status changes (e.g., added, removed, online, offline).
/// </summary>
public class FriendStatusChangedEvent
{
    public string FriendId { get; private set; }
    public string FriendName { get; private set; }
    public bool IsOnline { get; private set; }
    public FriendStatusChangeType ChangeType { get; private set; }

    public FriendStatusChangedEvent(string id, string name, bool isOnline, FriendStatusChangeType type)
    {
        FriendId = id;
        FriendName = name;
        IsOnline = isOnline;
        ChangeType = type;
    }

    public override string ToString()
    {
        return $"[FriendStatusChanged] ID: {FriendId}, Name: {FriendName}, Online: {IsOnline}, Type: {ChangeType}";
    }
}

public enum FriendStatusChangeType
{
    Added,
    Removed,
    Online,
    Offline
}

/// <summary>
/// Event data for a new chat message.
/// </summary>
public class ChatMessageEvent
{
    public string SenderId { get; private set; }
    public string ReceiverId { get; private set; } // Can be null for global chat
    public string Message { get; private set; }
    public DateTime Timestamp { get; private set; }

    public ChatMessageEvent(string senderId, string receiverId, string message)
    {
        SenderId = senderId;
        ReceiverId = receiverId;
        Message = message;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        string receiverInfo = string.IsNullOrEmpty(ReceiverId) ? "(Global)" : $"(To: {ReceiverId})";
        return $"[ChatMessage] From: {SenderId} {receiverInfo}, Msg: '{Message}'";
    }
}

/// <summary>
/// Event data for when a user's profile information is updated.
/// </summary>
public class ProfileUpdatedEvent
{
    public string UserId { get; private set; }
    public string NewDisplayName { get; private set; }
    public int NewLevel { get; private set; }
    // ... other profile data

    public ProfileUpdatedEvent(string userId, string newDisplayName, int newLevel)
    {
        UserId = userId;
        NewDisplayName = newDisplayName;
        NewLevel = newLevel;
    }

    public override string ToString()
    {
        return $"[ProfileUpdated] User: {UserId}, DisplayName: {NewDisplayName}, Level: {NewLevel}";
    }
}


// --- 2. The SocialHub System (Mediator/Event Bus) ---
// This is the core of the SocialHubSystem pattern. It acts as a central mediator
// for all social-related modules, allowing them to communicate without direct dependencies.

/// <summary>
/// The central Social Hub System.
/// Implements the Singleton pattern to ensure only one instance exists and provides global access.
/// It acts as an Event Bus/Mediator, enabling various social modules to publish and subscribe to events.
/// This decouples modules, making the system more modular, testable, and maintainable.
/// </summary>
public class SocialHub : MonoBehaviour
{
    // Singleton instance
    public static SocialHub Instance { get; private set; }

    // Dictionary to store event subscriptions.
    // Key: Type of the event (e.g., typeof(FriendStatusChangedEvent)).
    // Value: List of delegates (callbacks) subscribed to that event type.
    private Dictionary<Type, List<Delegate>> _eventCallbacks = new Dictionary<Type, List<Delegate>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SocialHub: Another instance already exists. Destroying this one.", this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene loads
            Debug.Log("<color=cyan>SocialHub: Initialized.</color>");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            _eventCallbacks.Clear(); // Clear all subscriptions
            Debug.Log("<color=cyan>SocialHub: Destroyed. All subscriptions cleared.</color>");
        }
    }

    /// <summary>
    /// Subscribes a callback method to a specific event type.
    /// When an event of type TEvent is published, the provided callback will be invoked.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
    /// <param name="callback">The method to call when the event is published.</param>
    public void Subscribe<TEvent>(Action<TEvent> callback)
    {
        Type eventType = typeof(TEvent);

        if (!_eventCallbacks.ContainsKey(eventType))
        {
            _eventCallbacks[eventType] = new List<Delegate>();
        }

        // Add the callback to the list if it's not already present
        if (!_eventCallbacks[eventType].Contains(callback))
        {
            _eventCallbacks[eventType].Add(callback);
            Debug.Log($"<color=green>SocialHub: Subscribed {callback.Method.Name} to {eventType.Name}. Total subscribers: {_eventCallbacks[eventType].Count}</color>");
        }
        else
        {
            Debug.LogWarning($"SocialHub: Attempted to subscribe same callback ({callback.Method.Name}) to {eventType.Name} multiple times.");
        }
    }

    /// <summary>
    /// Unsubscribes a callback method from a specific event type.
    /// This should be called when a module no longer needs to listen for an event (e.g., OnDisable or OnDestroy).
    /// </summary>
    /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
    /// <param name="callback">The method that was previously subscribed.</param>
    public void Unsubscribe<TEvent>(Action<TEvent> callback)
    {
        Type eventType = typeof(TEvent);

        if (_eventCallbacks.ContainsKey(eventType))
        {
            // Remove the specific callback
            if (_eventCallbacks[eventType].Remove(callback))
            {
                Debug.Log($"<color=orange>SocialHub: Unsubscribed {callback.Method.Name} from {eventType.Name}. Remaining subscribers: {_eventCallbacks[eventType].Count}</color>");
            }
            else
            {
                Debug.LogWarning($"SocialHub: Attempted to unsubscribe a callback ({callback.Method.Name}) that was not subscribed to {eventType.Name}.");
            }

            // Clean up the list if no more subscribers
            if (_eventCallbacks[eventType].Count == 0)
            {
                _eventCallbacks.Remove(eventType);
                Debug.Log($"SocialHub: No more subscribers for {eventType.Name}, list removed.");
            }
        }
        else
        {
            Debug.LogWarning($"SocialHub: Attempted to unsubscribe from {eventType.Name}, but no subscribers were found for this event type.");
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed listeners.
    /// The eventData object carries all the relevant information for the event.
    /// </summary>
    /// <typeparam name="TEvent">The type of event being published.</typeparam>
    /// <param name="eventData">The data object associated with the event.</param>
    public void Publish<TEvent>(TEvent eventData)
    {
        Type eventType = typeof(TEvent);

        if (_eventCallbacks.ContainsKey(eventType))
        {
            // Iterate through a copy of the list to prevent issues if subscribers unsubscribe during iteration
            List<Delegate> callbacksToInvoke = new List<Delegate>(_eventCallbacks[eventType]);

            Debug.Log($"<color=yellow>SocialHub: Publishing {eventType.Name}. Data: {eventData.ToString()} to {callbacksToInvoke.Count} subscribers.</color>");

            foreach (Delegate callback in callbacksToInvoke)
            {
                // Each delegate needs to be cast back to its specific Action<TEvent> type
                // for invocation. This is safe because only Action<TEvent> delegates are stored
                // for a given TEvent key.
                (callback as Action<TEvent>)?.Invoke(eventData);
            }
        }
        else
        {
            Debug.Log($"SocialHub: No subscribers for event type: {eventType.Name}. Event was not processed. Data: {eventData.ToString()}");
        }
    }
}


// --- 3. Concrete Social Modules ---
// These are individual components responsible for specific social features.
// They interact with the SocialHub to communicate with other modules, without knowing them directly.

/// <summary>
/// Manages friend lists and related actions.
/// It publishes events when friend status changes and can subscribe to other events
/// if it needs to react to global social updates (e.g., profile changes).
/// </summary>
public class FriendManager : MonoBehaviour
{
    private List<string> _friends = new List<string>();
    private string _myUserId = "Player123";
    private Dictionary<string, bool> _onlineStatus = new Dictionary<string, bool>(); // Tracks online status internally

    void OnEnable()
    {
        // Subscribe to events that FriendManager is interested in.
        SocialHub.Instance?.Subscribe<ProfileUpdatedEvent>(OnProfileUpdated);
        Debug.Log($"FriendManager: Enabled. Subscribed to ProfileUpdatedEvent.");
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks and ensure clean shutdown.
        SocialHub.Instance?.Unsubscribe<ProfileUpdatedEvent>(OnProfileUpdated);
        Debug.Log($"FriendManager: Disabled. Unsubscribed from ProfileUpdatedEvent.");
    }

    void Start()
    {
        // Simulate initial friends
        AddFriend("Friend_Bob", true);
        AddFriend("Friend_Alice", true);
        AddFriend("Friend_Charlie", false); // Offline friend
    }

    /// <summary>
    /// Simulates adding a new friend.
    /// Publishes a FriendStatusChangedEvent to inform other modules.
    /// </summary>
    public void AddFriend(string friendId, bool isOnline)
    {
        if (!_friends.Contains(friendId))
        {
            _friends.Add(friendId);
            _onlineStatus[friendId] = isOnline;
            Debug.Log($"FriendManager: Added friend '{friendId}'.");
            SocialHub.Instance?.Publish(new FriendStatusChangedEvent(friendId, friendId, isOnline, FriendStatusChangeType.Added));
        }
        else
        {
            Debug.Log($"FriendManager: '{friendId}' is already a friend.");
        }
    }

    /// <summary>
    /// Simulates removing a friend.
    /// Publishes a FriendStatusChangedEvent.
    /// </summary>
    public void RemoveFriend(string friendId)
    {
        if (_friends.Remove(friendId))
        {
            _onlineStatus.Remove(friendId);
            Debug.Log($"FriendManager: Removed friend '{friendId}'.");
            SocialHub.Instance?.Publish(new FriendStatusChangedEvent(friendId, friendId, false, FriendStatusChangeType.Removed));
        }
        else
        {
            Debug.Log($"FriendManager: '{friendId}' is not in friend list.");
        }
    }

    /// <summary>
    /// Simulates a friend going online or offline.
    /// Publishes a FriendStatusChangedEvent.
    /// </summary>
    public void SetFriendOnlineStatus(string friendId, bool isOnline)
    {
        if (_friends.Contains(friendId))
        {
            if (_onlineStatus.TryGetValue(friendId, out bool currentStatus) && currentStatus == isOnline)
            {
                Debug.Log($"FriendManager: Friend '{friendId}' is already {(isOnline ? "Online" : "Offline")}. No change.");
                return;
            }

            _onlineStatus[friendId] = isOnline;
            Debug.Log($"FriendManager: Friend '{friendId}' set to {(isOnline ? "Online" : "Offline")}.");
            SocialHub.Instance?.Publish(new FriendStatusChangedEvent(friendId, friendId, isOnline, isOnline ? FriendStatusChangeType.Online : FriendStatusChangeType.Offline));
        }
        else
        {
            Debug.LogWarning($"FriendManager: Cannot set status for non-friend '{friendId}'.");
        }
    }

    /// <summary>
    /// Callback for when a profile is updated.
    /// FriendManager might react by refreshing friend displays, etc.
    /// </summary>
    /// <param name="e">The ProfileUpdatedEvent data.</param>
    private void OnProfileUpdated(ProfileUpdatedEvent e)
    {
        Debug.Log($"FriendManager: Received ProfileUpdatedEvent for User '{e.UserId}'. New Display Name: '{e.NewDisplayName}', Level: {e.NewLevel}.");
        // Example reaction: if e.UserId is our own, perhaps update our display.
        // Or if it's a friend's profile update, refresh their info in our list.
    }

    // Example UI/Debug interactions for FriendManager
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            AddFriend("NewbieFriend", true);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            RemoveFriend("Friend_Bob");
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            bool aliceIsOnline = _onlineStatus.ContainsKey("Friend_Alice") && _onlineStatus["Friend_Alice"];
            SetFriendOnlineStatus("Friend_Alice", !aliceIsOnline); // Toggle Alice's status
        }
    }
}

/// <summary>
/// Manages chat functionality (sending/receiving messages).
/// Subscribes to friend status changes to update chat lists and publishes new messages.
/// </summary>
public class ChatManager : MonoBehaviour
{
    private string _myUserId = "Player123";
    private string _currentChatTarget = "Friend_Bob"; // Simulate who we are currently chatting with.

    // Internal state to keep track of friend's online status (mirrored from FriendManager via SocialHub)
    private Dictionary<string, bool> _friendOnlineStatus = new Dictionary<string, bool>();

    void OnEnable()
    {
        // Subscribe to events that ChatManager needs to react to.
        SocialHub.Instance?.Subscribe<FriendStatusChangedEvent>(OnFriendStatusChanged);
        SocialHub.Instance?.Subscribe<ChatMessageEvent>(OnChatMessageReceived);
        Debug.Log($"ChatManager: Enabled. Subscribed to FriendStatusChangedEvent and ChatMessageEvent.");
    }

    void OnDisable()
    {
        // Unsubscribe from events.
        SocialHub.Instance?.Unsubscribe<FriendStatusChangedEvent>(OnFriendStatusChanged);
        SocialHub.Instance?.Unsubscribe<ChatMessageEvent>(OnChatMessageReceived);
        Debug.Log($"ChatManager: Disabled. Unsubscribed from FriendStatusChangedEvent and ChatMessageEvent.");
    }

    /// <summary>
    /// Handles incoming FriendStatusChangedEvents.
    /// Updates the chat UI/friend list based on friend activity.
    /// </summary>
    /// <param name="e">The FriendStatusChangedEvent data.</param>
    private void OnFriendStatusChanged(FriendStatusChangedEvent e)
    {
        switch (e.ChangeType)
        {
            case FriendStatusChangeType.Added:
                Debug.Log($"ChatManager: Friend '{e.FriendName}' ({e.FriendId}) was added. Is online: {e.IsOnline}.");
                _friendOnlineStatus[e.FriendId] = e.IsOnline;
                // Add friend to chat list UI.
                break;
            case FriendStatusChangeType.Removed:
                Debug.Log($"ChatManager: Friend '{e.FriendName}' ({e.FriendId}) was removed.");
                _friendOnlineStatus.Remove(e.FriendId);
                // Remove friend from chat list UI.
                break;
            case FriendStatusChangeType.Online:
                Debug.Log($"ChatManager: Friend '{e.FriendName}' ({e.FriendId}) is now ONLINE.");
                _friendOnlineStatus[e.FriendId] = true;
                // Update friend's status in chat list UI.
                break;
            case FriendStatusChangeType.Offline:
                Debug.Log($"ChatManager: Friend '{e.FriendName}' ({e.FriendId}) is now OFFLINE.");
                _friendOnlineStatus[e.FriendId] = false;
                // Update friend's status in chat list UI.
                break;
        }
        DisplayFriendStatus();
    }

    /// <summary>
    /// Handles incoming ChatMessageEvents.
    /// Displays the message in the chat UI.
    /// </summary>
    /// <param name="e">The ChatMessageEvent data.</param>
    private void OnChatMessageReceived(ChatMessageEvent e)
    {
        // Only display messages if we are the intended receiver or it's a global message
        if (e.ReceiverId == _myUserId || string.IsNullOrEmpty(e.ReceiverId))
        {
            string receiverInfo = string.IsNullOrEmpty(e.ReceiverId) ? "(Global)" : $"(To: {e.ReceiverId})";
            Debug.Log($"ChatManager: Message from '{e.SenderId}' {receiverInfo}: '{e.Message}' (Time: {e.Timestamp})");
            // Display message in chat window.
        } else {
            Debug.Log($"ChatManager: Ignoring message from '{e.SenderId}' to '{e.ReceiverId}' (not for me).");
        }
    }

    /// <summary>
    /// Simulates sending a chat message to the currently selected friend.
    /// Publishes a ChatMessageEvent.
    /// </summary>
    public void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(_currentChatTarget))
        {
            Debug.LogWarning("ChatManager: No chat target selected. Cannot send message.");
            return;
        }
        if (!_friendOnlineStatus.ContainsKey(_currentChatTarget) || !_friendOnlineStatus[_currentChatTarget])
        {
            Debug.LogWarning($"ChatManager: Cannot send message to '{_currentChatTarget}'. They might be offline or not a friend.");
            // In a real app, you might queue it or send it anyway for offline messaging.
            return;
        }

        Debug.Log($"ChatManager: Sending message to '{_currentChatTarget}': '{message}'.");
        SocialHub.Instance?.Publish(new ChatMessageEvent(_myUserId, _currentChatTarget, message));
    }

    private void DisplayFriendStatus()
    {
        string status = "ChatManager Friend Status:\n";
        foreach (var entry in _friendOnlineStatus)
        {
            status += $"- {entry.Key}: {(entry.Value ? "Online" : "Offline")}\n";
        }
        Debug.Log(status);
    }

    // Example UI/Debug interactions for ChatManager
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SendChatMessage("Hey, what's up?");
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Switch target to Bob
        {
            _currentChatTarget = "Friend_Bob";
            Debug.Log($"ChatManager: Switched chat target to '{_currentChatTarget}'.");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Switch target to Alice
        {
            _currentChatTarget = "Friend_Alice";
            Debug.Log($"ChatManager: Switched chat target to '{_currentChatTarget}'.");
        }
        if (Input.GetKeyDown(KeyCode.G)) // Send global chat (no receiverId)
        {
            Debug.Log($"ChatManager: Sending global message.");
            SocialHub.Instance?.Publish(new ChatMessageEvent(_myUserId, null, "Hello everyone in global chat!"));
        }
    }
}


/// <summary>
/// Manages user profile information.
/// It can publish events when the profile changes and might subscribe to events
/// if it needs to react to other social activities (e.g., update friend count).
/// </summary>
public class ProfileManager : MonoBehaviour
{
    private string _myUserId = "Player123";
    private string _displayName = "HeroPlayer";
    private int _level = 1;
    private int _friendCount = 0; // This will be updated by FriendStatusChangedEvent

    void OnEnable()
    {
        // Subscribe to events that ProfileManager needs to react to.
        SocialHub.Instance?.Subscribe<FriendStatusChangedEvent>(OnFriendStatusChanged);
        Debug.Log($"ProfileManager: Enabled. Subscribed to FriendStatusChangedEvent.");
    }

    void OnDisable()
    {
        // Unsubscribe from events.
        SocialHub.Instance?.Unsubscribe<FriendStatusChangedEvent>(OnFriendStatusChanged);
        Debug.Log($"ProfileManager: Disabled. Unsubscribed from FriendStatusChangedEvent.");
    }

    void Start()
    {
        // Publish initial profile data (or load from persistent storage)
        PublishProfileUpdate();
    }

    /// <summary>
    /// Updates the user's display name and publishes a ProfileUpdatedEvent.
    /// </summary>
    public void UpdateDisplayName(string newName)
    {
        _displayName = newName;
        Debug.Log($"ProfileManager: Display name updated to '{newName}'.");
        PublishProfileUpdate();
    }

    /// <summary>
    /// Updates the user's level and publishes a ProfileUpdatedEvent.
    /// </summary>
    public void LevelUp()
    {
        _level++;
        Debug.Log($"ProfileManager: Leveled up to {_level}.");
        PublishProfileUpdate();
    }

    /// <summary>
    /// Internal helper to publish the current profile state.
    /// </summary>
    private void PublishProfileUpdate()
    {
        SocialHub.Instance?.Publish(new ProfileUpdatedEvent(_myUserId, _displayName, _level));
    }

    /// <summary>
    /// Callback for when friend status changes.
    /// ProfileManager updates its internal friend count.
    /// </summary>
    /// <param name="e">The FriendStatusChangedEvent data.</param>
    private void OnFriendStatusChanged(FriendStatusChangedEvent e)
    {
        switch (e.ChangeType)
        {
            case FriendStatusChangeType.Added:
                _friendCount++;
                Debug.Log($"ProfileManager: Friend '{e.FriendName}' added. Total friends: {_friendCount}.");
                break;
            case FriendStatusChangeType.Removed:
                _friendCount--;
                // Ensure friend count doesn't go below zero
                _friendCount = Mathf.Max(0, _friendCount);
                Debug.Log($"ProfileManager: Friend '{e.FriendName}' removed. Total friends: {_friendCount}.");
                break;
            // Online/Offline changes don't affect total friend count, but could update an "online friends" count if desired.
        }
    }

    // Example UI/Debug interactions for ProfileManager
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UpdateDisplayName("Super" + _myUserId + "_V" + UnityEngine.Random.Range(1, 10));
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            LevelUp();
        }
        if (Input.GetKeyDown(KeyCode.I)) // Info
        {
            Debug.Log($"ProfileManager Info: UserID: {_myUserId}, Display Name: {_displayName}, Level: {_level}, Friends: {_friendCount}");
        }
    }
}


/*
--- How to Use the SocialHubSystem in Unity ---

1.  **Create an Empty GameObject:** In your Unity scene, create an empty GameObject (e.g., "SocialSystem").
2.  **Add the SocialHub Script:** Attach the `SocialHub.cs` script to this "SocialSystem" GameObject. This will make it your central hub.
3.  **Create Social Module GameObjects:** Create other empty GameObjects for your specific social modules (e.g., "FriendManager", "ChatManager", "ProfileManager").
4.  **Add Social Module Scripts:** Attach the `FriendManager.cs`, `ChatManager.cs`, and `ProfileManager.cs` scripts to their respective GameObjects.
5.  **Run the Scene:**
    *   Observe the Debug.Log messages in the console. You'll see modules subscribing and events being published/received, color-coded for clarity.
    *   **Interact with the system using keyboard inputs (as defined in the example modules):**
        *   **FriendManager:**
            *   `F1`: Add a new friend ("NewbieFriend").
            *   `F2`: Remove "Friend_Bob".
            *   `F3`: Toggle "Friend_Alice"'s online status.
        *   **ChatManager:**
            *   `1`: Set chat target to "Friend_Bob".
            *   `2`: Set chat target to "Friend_Alice".
            *   `C`: Send a private chat message to the current target.
            *   `G`: Send a global chat message (received by all ChatManagers).
        *   **ProfileManager:**
            *   `P`: Update your display name (randomly changes).
            *   `L`: Level up your player.
            *   `I`: Log current profile information.

This setup creates a robust, decoupled, and scalable architecture for managing social features in your Unity projects.
*/
```