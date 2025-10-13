// Unity Design Pattern Example: PartyChatSystem
// This script demonstrates the PartyChatSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a robust and practical implementation of a 'PartyChatSystem' in Unity, using several common design patterns:

1.  **Singleton Pattern:** For the `PartyChatManager` to ensure a single, globally accessible instance.
2.  **Publisher-Subscriber (or Observer) Pattern:** For individual `Party` objects to notify subscribing UI elements or other game logic whenever a new message is added.

The system allows for multiple independent chat parties, each with its own history and members.

## `PartyChatSystem.cs`

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
// No System.Text needed for this specific implementation, but often useful for complex string operations.

/// <summary>
/// Defines the type of a chat message.
/// </summary>
public enum MessageType
{
    Text,        // Regular chat message
    System,      // Automated system message (e.g., "Player joined", "Party disbanded")
    Emote,       // An action or emote message (e.g., "PlayerX waves hello!")
    Warning      // A warning message
}

/// <summary>
/// Represents a single chat message within a party.
/// Marked [System.Serializable] so it can be viewed in the Unity Inspector if it were
/// part of a MonoBehaviour's serialized list (though not directly used that way here).
/// </summary>
[System.Serializable]
public struct ChatMessage
{
    public string SenderPlayerId;   // Unique ID of the sender (e.g., database ID)
    public string SenderPlayerName; // Display name of the sender
    public string Content;          // The actual message text
    public DateTime Timestamp;      // When the message was sent
    public MessageType Type;         // Type of message

    public ChatMessage(string senderId, string senderName, string content, MessageType type = MessageType.Text)
    {
        SenderPlayerId = senderId;
        SenderPlayerName = senderName;
        Content = content;
        Timestamp = DateTime.Now;
        Type = type;
    }

    /// <summary>
    /// Provides a formatted string representation of the message, useful for display.
    /// Includes basic rich text formatting for Unity UI if needed.
    /// </summary>
    public override string ToString()
    {
        string prefix = $"[{Timestamp:HH:mm:ss}]";
        switch (Type)
        {
            case MessageType.System:
                return $"{prefix} <color=yellow>[SYSTEM]</color> {Content}";
            case MessageType.Warning:
                return $"{prefix} <color=red>[WARNING]</color> {Content}";
            case MessageType.Emote:
                return $"{prefix} * {SenderPlayerName} {Content} *";
            default:
                return $"{prefix} <color=cyan>{SenderPlayerName}</color>: {Content}";
        }
    }
}

/// <summary>
/// Represents a single chat party. It maintains its own chat history,
/// a list of members, and acts as a 'Publisher' in the Publisher-Subscriber pattern
/// by providing an event that notifies subscribers (like UI elements) whenever a new message is added.
/// </summary>
public class Party
{
    public string PartyId { get; private set; }                       // Unique identifier for this party
    public List<string> MemberPlayerIds { get; private set; }         // List of player IDs currently in this party
    public List<ChatMessage> MessageHistory { get; private set; }     // Full history of messages for this party

    /// <summary>
    /// Event triggered when a new message is added to this party's chat.
    /// Any UI component or game logic interested in displaying messages for this party
    /// should subscribe to this event. This is the core of the Observer/Publisher-Subscriber pattern.
    /// </summary>
    public event Action<ChatMessage> OnNewMessage;

    public Party(string partyId)
    {
        PartyId = partyId;
        MemberPlayerIds = new List<string>();
        MessageHistory = new List<ChatMessage>();
    }

    /// <summary>
    /// Adds a player to this party's member list and sends a system message.
    /// </summary>
    public void AddMember(string playerId)
    {
        if (!MemberPlayerIds.Contains(playerId))
        {
            MemberPlayerIds.Add(playerId);
            // Optionally, we could try to get the player's name from a global player service here
            // For simplicity, we'll use the ID or assume the name is passed.
            AddSystemMessage($"{playerId} has joined the party.");
        }
    }

    /// <summary>
    /// Removes a player from this party's member list and sends a system message.
    /// </summary>
    public void RemoveMember(string playerId)
    {
        if (MemberPlayerIds.Contains(playerId))
        {
            MemberPlayerIds.Remove(playerId);
            AddSystemMessage($"{playerId} has left the party.");
        }
    }

    /// <summary>
    /// Adds a new chat message to the party's history and notifies all subscribers
    /// via the `OnNewMessage` event. This is the 'Publish' action.
    /// </summary>
    /// <param name="message">The ChatMessage to add and publish.</param>
    public void AddMessage(ChatMessage message)
    {
        MessageHistory.Add(message);
        OnNewMessage?.Invoke(message); // The '?' checks if there are any subscribers before invoking.
    }

    /// <summary>
    /// Convenience method to add a system-generated message to the chat.
    /// </summary>
    /// <param name="content">The content of the system message.</param>
    public void AddSystemMessage(string content)
    {
        // System messages are sent by "SYSTEM" with "System" as name.
        AddMessage(new ChatMessage("SYSTEM", "System", content, MessageType.System));
    }
}

/// <summary>
/// The central manager for all chat parties in the game.
/// This class uses the **Singleton pattern** to ensure there's only one instance
/// accessible globally. It orchestrates party creation, joining, leaving,
/// and message sending, acting as the primary interface for all chat operations.
/// </summary>
public class PartyChatManager : MonoBehaviour
{
    // Singleton instance for global access.
    // 'static' means it belongs to the class itself, not any specific object.
    public static PartyChatManager Instance { get; private set; }

    // Stores all active parties, mapped by their unique PartyId.
    private Dictionary<string, Party> activeParties = new Dictionary<string, Party>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used here to set up the Singleton.
    /// </summary>
    void Awake()
    {
        // Singleton enforcement:
        // If an instance already exists and it's not THIS instance, destroy this one.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PartyChatManager: Duplicate instance found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        // Set this instance as the Singleton.
        Instance = this;
        // Make sure this GameObject persists across scene changes.
        DontDestroyOnLoad(gameObject);
        Debug.Log("PartyChatManager: Initialized.");
    }

    /// <summary>
    /// Creates a new chat party with a given ID and an initial member.
    /// </summary>
    /// <param name="partyId">Unique identifier for the new party.</param>
    /// <param name="initialMemberPlayerId">The unique ID of the player creating/initializing the party.</param>
    /// <param name="initialMemberPlayerName">The display name of the player creating the party.</param>
    /// <returns>The newly created Party object, or null if a party with this ID already exists.</returns>
    public Party CreateParty(string partyId, string initialMemberPlayerId, string initialMemberPlayerName)
    {
        if (activeParties.ContainsKey(partyId))
        {
            Debug.LogWarning($"PartyChatManager: Party with ID '{partyId}' already exists. Cannot create.");
            return null;
        }

        Party newParty = new Party(partyId);
        newParty.AddMember(initialMemberPlayerId); // Add the creator as the first member
        activeParties.Add(partyId, newParty);
        Debug.Log($"PartyChatManager: Party '{partyId}' created by {initialMemberPlayerId}.");
        newParty.AddSystemMessage($"{initialMemberPlayerName} ({initialMemberPlayerId}) created the party.");
        return newParty;
    }

    /// <summary>
    /// Attempts to join an existing party.
    /// </summary>
    /// <param name="partyId">The ID of the party to join.</param>
    /// <param name="playerId">The unique ID of the player joining.</param>
    /// <param name="playerName">The display name of the player joining.</param>
    /// <returns>True if joined successfully, false otherwise (e.g., party not found, already a member).</returns>
    public bool JoinParty(string partyId, string playerId, string playerName)
    {
        if (activeParties.TryGetValue(partyId, out Party party))
        {
            if (!party.MemberPlayerIds.Contains(playerId))
            {
                party.AddMember(playerId);
                Debug.Log($"PartyChatManager: Player {playerName} ({playerId}) joined party '{partyId}'.");
                return true;
            }
            Debug.LogWarning($"PartyChatManager: Player {playerId} is already a member of party '{partyId}'.");
            return false;
        }
        Debug.LogWarning($"PartyChatManager: Party with ID '{partyId}' not found for player {playerId} to join.");
        return false;
    }

    /// <summary>
    /// Attempts to leave an existing party.
    /// If the party becomes empty after a player leaves, it is disbanded.
    /// </summary>
    /// <param name="partyId">The ID of the party to leave.</param>
    /// <param name="playerId">The unique ID of the player leaving.</param>
    /// <returns>True if left successfully, false otherwise (e.g., party not found, not a member).</returns>
    public bool LeaveParty(string partyId, string playerId)
    {
        if (activeParties.TryGetValue(partyId, out Party party))
        {
            if (party.MemberPlayerIds.Contains(playerId))
            {
                party.RemoveMember(playerId);
                Debug.Log($"PartyChatManager: Player {playerId} left party '{partyId}'.");
                
                // Disband the party if no members are left
                if (party.MemberPlayerIds.Count == 0)
                {
                    activeParties.Remove(partyId);
                    Debug.Log($"PartyChatManager: Party '{partyId}' is now empty and has been disbanded.");
                    // Note: Subscribers to this specific Party object will no longer receive updates
                    // as the party itself is no longer managed. Their OnDestroy should handle unsubscription.
                }
                return true;
            }
            Debug.LogWarning($"PartyChatManager: Player {playerId} is not a member of party '{partyId}'.");
            return false;
        }
        Debug.LogWarning($"PartyChatManager: Party with ID '{partyId}' not found for player {playerId} to leave.");
        return false;
    }

    /// <summary>
    /// Sends a chat message to a specific party. The sender must be a member of the party.
    /// </summary>
    /// <param name="partyId">The ID of the target party.</param>
    /// <param name="senderPlayerId">The unique ID of the player sending the message.</param>
    /// <param name="senderPlayerName">The display name of the player sending the message.</param>
    /// <param name="messageContent">The actual text content of the message.</param>
    /// <param name="type">The type of the message (default: Text).</param>
    /// <returns>True if the message was sent successfully, false otherwise.</returns>
    public bool SendMessage(string partyId, string senderPlayerId, string senderPlayerName, string messageContent, MessageType type = MessageType.Text)
    {
        if (activeParties.TryGetValue(partyId, out Party party))
        {
            // Allow system messages to be sent even if the sender isn't explicitly a "member"
            if (party.MemberPlayerIds.Contains(senderPlayerId) || type == MessageType.System)
            {
                ChatMessage message = new ChatMessage(senderPlayerId, senderPlayerName, messageContent, type);
                party.AddMessage(message); // Add message to party and trigger OnNewMessage event
                return true;
            }
            Debug.LogWarning($"PartyChatManager: Player {senderPlayerId} is not a member of party '{partyId}' and cannot send messages.");
            return false;
        }
        Debug.LogWarning($"PartyChatManager: Party with ID '{partyId}' not found to send message from {senderPlayerId}.");
        return false;
    }

    /// <summary>
    /// Retrieves a Party object by its ID. This method is crucial for UI components
    /// or other game logic to get a reference to a specific party and then subscribe
    /// to its `OnNewMessage` event.
    /// </summary>
    /// <param name="partyId">The ID of the party to retrieve.</param>
    /// <returns>The Party object if found, otherwise null.</returns>
    public Party GetParty(string partyId)
    {
        activeParties.TryGetValue(partyId, out Party party);
        return party;
    }
}

// --- EXAMPLE USAGE COMPONENTS ---

/// <summary>
/// Simulates a UI component that displays chat messages for a specific party.
/// This class acts as a 'Subscriber' to a Party's `OnNewMessage` event.
/// </summary>
public class ChatUI : MonoBehaviour
{
    [Header("Chat UI Settings")]
    [SerializeField] private string targetPartyId = "GlobalParty"; // The party this UI is configured to display
    [SerializeField] private int maxMessagesToDisplay = 10;        // How many messages to keep in history for display

    private Party _subscribedParty; // Reference to the specific Party object we are observing
    private List<ChatMessage> _displayMessages = new List<ChatMessage>(); // Local cache of messages to display

    void Start()
    {
        // 1. Ensure the PartyChatManager exists in the scene.
        if (PartyChatManager.Instance == null)
        {
            Debug.LogError("ChatUI: PartyChatManager not found in scene. Please add it to a GameObject.");
            enabled = false; // Disable this component if manager is missing
            return;
        }

        // 2. Try to get the target party from the manager.
        // In a real game, party creation might be server-driven or handled by a LobbyManager.
        // Here, we assume a PlayerChatSimulator will create it first if needed.
        _subscribedParty = PartyChatManager.Instance.GetParty(targetPartyId);

        if (_subscribedParty == null)
        {
            Debug.LogWarning($"ChatUI: Party '{targetPartyId}' not found when ChatUI started. Will not subscribe. Ensure a player creates/joins it.");
            // We could add a retry mechanism or an event from PartyChatManager to notify of new parties.
            return;
        }

        // 3. Subscribe to the OnNewMessage event of the target party.
        // This is where this ChatUI 'observes' the party, adhering to the Observer pattern.
        _subscribedParty.OnNewMessage += OnNewPartyMessage;
        Debug.Log($"ChatUI: Successfully subscribed to party '{targetPartyId}'.");

        // 4. Optionally, display existing message history upon subscription.
        foreach (var msg in _subscribedParty.MessageHistory)
        {
            AddMessageToDisplay(msg);
        }
    }

    /// <summary>
    /// IMPORTANT: Unsubscribe when the GameObject this script is on is destroyed
    /// to prevent memory leaks. If we don't unsubscribe, the `_subscribedParty`
    /// would still hold a reference to this `OnNewPartyMessage` method, preventing
    /// this `ChatUI` object from being garbage collected even after destruction.
    /// </summary>
    void OnDestroy()
    {
        if (_subscribedParty != null)
        {
            _subscribedParty.OnNewMessage -= OnNewPartyMessage;
            Debug.Log($"ChatUI: Unsubscribed from party '{targetPartyId}'.");
        }
    }

    /// <summary>
    /// This is the callback method that gets invoked whenever the `OnNewMessage`
    /// event of the subscribed party is triggered.
    /// This is the 'Update' method in the Observer pattern, receiving the new data.
    /// </summary>
    /// <param name="message">The new chat message received from the party.</param>
    private void OnNewPartyMessage(ChatMessage message)
    {
        AddMessageToDisplay(message);
    }

    /// <summary>
    /// Adds a message to the internal display list and manages its size.
    /// In a real Unity UI, this would update TextMeshPro or other UI elements.
    /// For this example, it logs to the console and adds to an OnGUI list.
    /// </summary>
    /// <param name="message">The message to add to the display.</param>
    private void AddMessageToDisplay(ChatMessage message)
    {
        _displayMessages.Add(message);
        if (_displayMessages.Count > maxMessagesToDisplay)
        {
            _displayMessages.RemoveAt(0); // Remove the oldest message if exceeding limit
        }
        // Log to Unity console for immediate feedback
        Debug.Log($"[UI for {targetPartyId}] {message}");
    }

    // Optional: Draw a simple debug UI box in the game view to visualize chat history.
    void OnGUI()
    {
        // Position each UI independently based on its party ID to avoid overlap.
        // Assumes party IDs are simple integers for positioning.
        int partyIndex = 0;
        if (targetPartyId.Contains("Party"))
        {
            string numStr = targetPartyId.Replace("Party", "").Trim();
            if (int.TryParse(numStr, out int index))
            {
                partyIndex = index;
            } else if (numStr == "Global") { // Special case for GlobalParty
                partyIndex = 0;
            }
        }
        
        Rect uiRect = new Rect(10, 10 + (partyIndex * 150), 300, 140);
        GUILayout.BeginArea(uiRect, GUI.skin.box);
        GUILayout.Label($"<b>{targetPartyId} Chat History</b>"); // Bold title
        GUILayout.Space(5);

        foreach (var msg in _displayMessages)
        {
            GUILayout.Label(msg.ToString()); // Display each message
        }
        GUILayout.EndArea();
    }
}

/// <summary>
/// Simulates a player's behavior: joining a party, sending messages, and leaving.
/// This acts as a client interacting with the `PartyChatManager`.
/// </summary>
public class PlayerChatSimulator : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private string playerId = "Player1";     // Unique ID for this simulated player
    [SerializeField] private string playerName = "Hero_One";  // Display name for this player

    [Header("Party Settings")]
    [SerializeField] private string partyToJoin = "GlobalParty";    // The ID of the party this player wants to join
    [SerializeField] private bool createPartyIfNotFound = true;    // If true, this player will create the party if it doesn't exist
    [SerializeField] private float messageInterval = 5f;            // How often this player sends a message (in seconds)

    private float _nextMessageTime; // Timestamp for when the next message should be sent

    void Start()
    {
        // 1. Ensure the PartyChatManager is present in the scene.
        if (PartyChatManager.Instance == null)
        {
            Debug.LogError("PlayerChatSimulator: PartyChatManager not found in scene. Please add it to a GameObject.");
            enabled = false;
            return;
        }

        // 2. Attempt to create or join the specified party.
        Party party = PartyChatManager.Instance.GetParty(partyToJoin);
        if (party == null && createPartyIfNotFound)
        {
            // If party doesn't exist and we're allowed to create it
            PartyChatManager.Instance.CreateParty(partyToJoin, playerId, playerName);
            Debug.Log($"PlayerChatSimulator {playerId}: Created party '{partyToJoin}'.");
        }
        else if (party != null)
        {
            // If party exists, try to join it
            PartyChatManager.Instance.JoinParty(partyToJoin, playerId, playerName);
        }
        else
        {
            // If party not found and cannot create
            Debug.LogError($"PlayerChatSimulator {playerId}: Party '{partyToJoin}' not found and createPartyIfNotFound is false. Cannot join.");
            enabled = false;
            return;
        }

        // Initialize the time for the first message. Add some randomness.
        _nextMessageTime = Time.time + UnityEngine.Random.Range(1f, messageInterval);
    }

    /// <summary>
    /// Update is called once per frame. Used here to periodically send messages.
    /// </summary>
    void Update()
    {
        if (Time.time >= _nextMessageTime)
        {
            SendMessageToParty();
            // Schedule the next message with some variation
            _nextMessageTime = Time.time + messageInterval + UnityEngine.Random.Range(-messageInterval / 2f, messageInterval / 2f);
        }
    }

    /// <summary>
    /// Simulates the player sending a message to their joined party.
    /// </summary>
    private void SendMessageToParty()
    {
        // A few example messages for variety
        string[] messages = {
            "Hello everyone!",
            "Any quests available?",
            "What's up in here?",
            "Having fun!",
            "Need some help at the crossroads.",
            "Great chat, folks!"
        };

        string messageContent = messages[UnityEngine.Random.Range(0, messages.Length)];

        // Randomly decide if it's a regular text message or an emote
        MessageType type = MessageType.Text;
        if (UnityEngine.Random.value < 0.2f) // 20% chance for an emote message
        {
            type = MessageType.Emote;
            messageContent = UnityEngine.Random.value < 0.5f ? "waves hello!" : "cheers loudly!";
        }

        PartyChatManager.Instance.SendMessage(partyToJoin, playerId, playerName, messageContent, type);
        // We log locally that we tried to send, the ChatUI will confirm if it was actually received.
        Debug.Log($"PlayerChatSimulator {playerId}: Sent message to '{partyToJoin}': \"{messageContent}\"");
    }

    /// <summary>
    /// Called when the script's GameObject is destroyed.
    /// Ensures the player leaves the party cleanly.
    /// </summary>
    void OnDestroy()
    {
        // Only attempt to leave if the manager still exists (might be destroyed first on app quit)
        if (PartyChatManager.Instance != null)
        {
            PartyChatManager.Instance.LeaveParty(partyToJoin, playerId);
        }
    }
}
```

---

### HOW TO USE THIS EXAMPLE IN UNITY:

This setup is designed to be easily runnable in a new Unity project.

1.  **Create a New C# Script:**
    *   In your Unity Project window, right-click -> `Create` -> `C# Script`.
    *   Name it `PartyChatSystem`.
    *   Copy and paste all the code above into this new script. Save it.

2.  **Create the `PartyChatManager` GameObject:**
    *   In your Unity `Hierarchy` window, right-click -> `Create Empty`.
    *   Rename this new GameObject to `PartyChatManager`.
    *   Drag and drop the `PartyChatSystem` script onto this `PartyChatManager` GameObject in the Inspector. This will attach the `PartyChatManager` component.

3.  **Create Player 1 Simulator:**
    *   In the `Hierarchy`, right-click -> `Create Empty`.
    *   Rename it to `Player_One_Simulator`.
    *   Drag and drop the `PartyChatSystem` script onto this GameObject. (It will attach the `PlayerChatSimulator` component).
    *   In the Inspector for `Player_One_Simulator` (ensure the `PlayerChatSimulator` component is selected):
        *   `Player Id`: `Player1`
        *   `Player Name`: `Alice`
        *   `Party To Join`: `GlobalParty`
        *   `Create Party If Not Found`: **Check** this box. (Alice will be the first to create the "GlobalParty").
        *   `Message Interval`: `3` (seconds)

4.  **Create Player 2 Simulator:**
    *   Repeat step 3 for a new GameObject named `Player_Two_Simulator`.
    *   In the Inspector for `Player_Two_Simulator`:
        *   `Player Id`: `Player2`
        *   `Player Name`: `Bob`
        *   `Party To Join`: `GlobalParty` (Bob will join Alice's existing party).
        *   `Create Party If Not Found`: **Uncheck** this box.
        *   `Message Interval`: `4` (seconds)

5.  **Create Global Chat UI:**
    *   In the `Hierarchy`, right-click -> `Create Empty`.
    *   Rename it to `GlobalChatUI`.
    *   Drag and drop the `PartyChatSystem` script onto this GameObject. (It will attach the `ChatUI` component).
    *   In the Inspector for `GlobalChatUI`:
        *   `Target Party Id`: `GlobalParty`
        *   `Max Messages To Display`: `10`

6.  **(Optional) Create a Second Party (e.g., "TeamParty"):**
    *   **Player 3 Simulator:**
        *   Create `Player_Three_Simulator` GameObject. Attach `PartyChatSystem` script.
        *   `Player Id`: `Player3`
        *   `Player Name`: `Charlie`
        *   `Party To Join`: `TeamParty`
        *   `Create Party If Not Found`: **Check** this box.
        *   `Message Interval`: `5`
    *   **Team Chat UI:**
        *   Create `TeamChatUI` GameObject. Attach `PartyChatSystem` script.
        *   `Target Party Id`: `TeamParty`
        *   `Max Messages To Display`: `10`

7.  **Run the Scene:**
    *   Press the `Play` button in Unity.
    *   Observe the Unity Console for detailed logs of party creation, players joining/leaving, and messages being sent and received.
    *   You will also see simple UI boxes drawn in the top-left of your Game View, showing the last 10 messages for each `ChatUI` you set up.

---

This example provides a clear, segmented approach to building a complex system, emphasizing modularity and common design patterns to make it robust and maintainable.