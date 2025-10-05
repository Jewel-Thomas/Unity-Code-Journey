// Unity Design Pattern Example: FriendInviteSystem
// This script demonstrates the FriendInviteSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'FriendInviteSystem' design pattern in Unity can be effectively implemented using a combination of the **Mediator** and **Observer** patterns.

Here's how it works in this context:

1.  **Mediator Pattern**:
    *   The `FriendInviteSystem` class acts as a **Mediator**. Instead of users directly managing their friend lists and sending requests to each other, all friend-related operations (sending invites, accepting, declining, removing friends, blocking) go through this central system.
    *   This decouples the `UserProfile` objects from each other. They don't need to know the intricate logic of how an invite is processed, how friend lists are updated, or how conflicts (like blocking or existing friendships) are resolved. They just interact with the `FriendInviteSystem`.
    *   The Mediator centralizes the complex business logic, making it easier to manage, test, and modify.

2.  **Observer Pattern**:
    *   The `FriendInviteSystem` uses C# events (`OnInviteSent`, `OnInviteAccepted`, etc.). These events allow other parts of your application (e.g., UI elements, notification systems, other game logic) to **observe** changes in the friend invite state.
    *   When an invite is sent, accepted, or declined, the Mediator broadcasts an event. Any interested "Observers" (like a UI manager displaying notifications, or a friend list panel refreshing) can subscribe to these events and react accordingly without needing to poll the system or having direct dependencies on the `FriendInviteSystem`'s internal workings.

This combination creates a robust, flexible, and maintainable system for managing friend interactions in your game.

---

## Complete C# Unity Example: FriendInviteSystem

This example includes three main parts:
1.  **`UserProfile.cs`**: A data class for user profiles.
2.  **`FriendInviteSystem.cs`**: The core Mediator/Observer system.
3.  **`FriendInviteSystemDemo.cs`**: A `MonoBehaviour` to demonstrate its usage.

### 1. `UserProfile.cs` (Data Model)

This script defines the data structure for a user in our system.

```csharp
using System;
using System.Collections.Generic;

namespace FriendInviteSystem
{
    /// <summary>
    /// Represents the status of a friend invite.
    /// </summary>
    public enum InviteStatus
    {
        Pending,
        Accepted,
        Declined,
        Cancelled // e.g., if one user blocks another
    }

    /// <summary>
    /// Represents a single friend invite between two users.
    /// </summary>
    [Serializable] // To make it visible in Unity's inspector if needed (though typically managed internally)
    public class FriendInvite
    {
        public string InviteId { get; private set; }
        public string SenderId { get; private set; }
        public string ReceiverId { get; private set; }
        public InviteStatus Status { get; set; } // Status can change
        public DateTime SentTime { get; private set; }

        public FriendInvite(string senderId, string receiverId)
        {
            SenderId = senderId;
            ReceiverId = receiverId;
            InviteId = Guid.NewGuid().ToString(); // Unique ID for each invite
            Status = InviteStatus.Pending;
            SentTime = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"InviteId: {InviteId}, Sender: {SenderId}, Receiver: {ReceiverId}, Status: {Status}, Sent: {SentTime.ToShortTimeString()}";
        }
    }

    /// <summary>
    /// Represents a user profile in the friend system.
    /// This is a plain C# class, not a MonoBehaviour.
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; private set; }
        public string UserName { get; private set; }

        // Friends list (stores UserId of friends)
        public HashSet<string> Friends { get; private set; }

        // Invites this user has sent that are pending
        public HashSet<string> PendingSentInvites { get; private set; } // Stores InviteId

        // Invites this user has received that are pending
        public HashSet<string> PendingReceivedInvites { get; private set; } // Stores InviteId

        // Users this user has blocked
        public HashSet<string> BlockedUsers { get; private set; } // Stores UserId of blocked users

        // Users who have blocked this user (useful for checks, but might be redundant depending on how BlockUser is implemented)
        // public HashSet<string> BlockedByUsers { get; private set; } 

        public UserProfile(string userId, string userName)
        {
            UserId = userId;
            UserName = userName;
            Friends = new HashSet<string>();
            PendingSentInvites = new HashSet<string>();
            PendingReceivedInvites = new HashSet<string>();
            BlockedUsers = new HashSet<string>();
            // BlockedByUsers = new HashSet<string>();
        }

        public override string ToString()
        {
            return $"{UserName} ({UserId})";
        }
    }
}
```

### 2. `FriendInviteSystem.cs` (The Mediator & Observer)

This is the core of the pattern. It manages all user profiles and friend invites, and broadcasts events when changes occur.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FriendInviteSystem
{
    /// <summary>
    /// The central Mediator and Observer for managing friend invites and relationships.
    /// It handles all logic related to sending, accepting, declining invites,
    /// adding/removing friends, and blocking users.
    /// </summary>
    public class FriendInviteSystem : MonoBehaviour
    {
        // Singleton pattern for easy global access
        public static FriendInviteSystem Instance { get; private set; }

        // --- Internal Data Stores ---
        private Dictionary<string, UserProfile> userProfiles; // Key: UserId
        private Dictionary<string, FriendInvite> allInvites;   // Key: InviteId

        // --- Events (Observer Pattern) ---
        // Other systems can subscribe to these events to react to changes.
        public event Action<FriendInvite> OnInviteSent;
        public event Action<FriendInvite> OnInviteAccepted;
        public event Action<FriendInvite> OnInviteDeclined;
        public event Action<FriendInvite> OnInviteCancelled; // e.g., due to blocking
        public event Action<UserProfile, UserProfile> OnFriendRemoved;
        public event Action<UserProfile, UserProfile> OnUserBlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FriendInviteSystem already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the system alive across scenes

            Initialize();
        }

        private void Initialize()
        {
            userProfiles = new Dictionary<string, UserProfile>();
            allInvites = new Dictionary<string, FriendInvite>();
            Debug.Log("<color=cyan>FriendInviteSystem initialized.</color>");
        }

        /// <summary>
        /// Registers a new user with the system.
        /// </summary>
        /// <param name="userId">Unique identifier for the user.</param>
        /// <param name="userName">Display name for the user.</param>
        /// <returns>The created UserProfile, or null if a user with the ID already exists.</returns>
        public UserProfile RegisterUser(string userId, string userName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                Debug.LogError("User ID and User Name cannot be empty.");
                return null;
            }
            if (userProfiles.ContainsKey(userId))
            {
                Debug.LogWarning($"User with ID '{userId}' already registered.");
                return null;
            }

            UserProfile newUser = new UserProfile(userId, userName);
            userProfiles.Add(userId, newUser);
            Debug.Log($"User '{userName}' ({userId}) registered.");
            return newUser;
        }

        /// <summary>
        /// Retrieves a user profile by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>The UserProfile if found, otherwise null.</returns>
        public UserProfile GetUserProfile(string userId)
        {
            userProfiles.TryGetValue(userId, out UserProfile profile);
            return profile;
        }

        /// <summary>
        /// Sends a friend invite from one user to another.
        /// Performs various checks before sending.
        /// </summary>
        /// <param name="senderId">The ID of the user sending the invite.</param>
        /// <param name="receiverId">The ID of the user receiving the invite.</param>
        /// <returns>The created FriendInvite if successful, otherwise null.</returns>
        public FriendInvite SendInvite(string senderId, string receiverId)
        {
            // --- Basic validation ---
            if (senderId == receiverId)
            {
                Debug.LogWarning("Cannot send invite to self.");
                return null;
            }
            if (!userProfiles.TryGetValue(senderId, out UserProfile senderProfile) ||
                !userProfiles.TryGetValue(receiverId, out UserProfile receiverProfile))
            {
                Debug.LogError("One or both users not found.");
                return null;
            }

            // --- Business logic checks ---
            if (senderProfile.Friends.Contains(receiverId))
            {
                Debug.Log($"'{senderProfile.UserName}' and '{receiverProfile.UserName}' are already friends.");
                return null;
            }
            if (senderProfile.BlockedUsers.Contains(receiverId))
            {
                Debug.LogWarning($"'{senderProfile.UserName}' has blocked '{receiverProfile.UserName}'. Invite cannot be sent.");
                return null;
            }
            if (receiverProfile.BlockedUsers.Contains(senderId))
            {
                Debug.LogWarning($"'{receiverProfile.UserName}' has blocked '{senderProfile.UserName}'. Invite cannot be sent.");
                return null;
            }
            if (senderProfile.PendingSentInvites.Any(inviteId => allInvites[inviteId].ReceiverId == receiverId))
            {
                Debug.Log($"'{senderProfile.UserName}' already has a pending invite to '{receiverProfile.UserName}'.");
                return null;
            }
            // Optional: Prevent if receiver has already sent an invite to sender
            if (receiverProfile.PendingSentInvites.Any(inviteId => allInvites[inviteId].ReceiverId == senderId))
            {
                 Debug.Log($"'{receiverProfile.UserName}' has already sent an invite to '{senderProfile.UserName}'. Consider accepting their invite instead.");
                 return null;
            }


            // --- Create and store invite ---
            FriendInvite newInvite = new FriendInvite(senderId, receiverId);
            allInvites.Add(newInvite.InviteId, newInvite);

            senderProfile.PendingSentInvites.Add(newInvite.InviteId);
            receiverProfile.PendingReceivedInvites.Add(newInvite.InviteId);

            Debug.Log($"<color=green>Invite sent from {senderProfile.UserName} to {receiverProfile.UserName}.</color> (ID: {newInvite.InviteId})");
            OnInviteSent?.Invoke(newInvite); // Notify observers
            return newInvite;
        }

        /// <summary>
        /// Accepts a pending friend invite.
        /// </summary>
        /// <param name="inviteId">The ID of the invite to accept.</param>
        /// <param name="accepterId">The ID of the user accepting the invite (must be the receiver).</param>
        /// <returns>True if accepted, false otherwise.</returns>
        public bool AcceptInvite(string inviteId, string accepterId)
        {
            if (!allInvites.TryGetValue(inviteId, out FriendInvite invite))
            {
                Debug.LogError($"Invite with ID '{inviteId}' not found.");
                return false;
            }

            if (invite.ReceiverId != accepterId)
            {
                Debug.LogWarning($"User '{accepterId}' is not the receiver of invite '{inviteId}'.");
                return false;
            }

            if (invite.Status != InviteStatus.Pending)
            {
                Debug.LogWarning($"Invite '{inviteId}' is not pending (current status: {invite.Status}). Cannot accept.");
                return false;
            }

            if (!userProfiles.TryGetValue(invite.SenderId, out UserProfile senderProfile) ||
                !userProfiles.TryGetValue(invite.ReceiverId, out UserProfile receiverProfile))
            {
                Debug.LogError("Sender or receiver user profile not found for invite.");
                return false;
            }
            
            // Check if one user blocked the other while the invite was pending
            if (senderProfile.BlockedUsers.Contains(receiverProfile.UserId) || receiverProfile.BlockedUsers.Contains(senderProfile.UserId))
            {
                Debug.LogWarning($"Cannot accept invite. One of the users has blocked the other since the invite was sent. Invite {invite.InviteId} cancelled.");
                CleanupInvite(invite);
                invite.Status = InviteStatus.Cancelled;
                OnInviteCancelled?.Invoke(invite);
                return false;
            }

            // Add each other to friend lists
            senderProfile.Friends.Add(receiverProfile.UserId);
            receiverProfile.Friends.Add(senderProfile.UserId);

            // Remove invite from pending lists
            CleanupInvite(invite);

            invite.Status = InviteStatus.Accepted;
            Debug.Log($"<color=green>Invite '{invite.InviteId}' accepted by {receiverProfile.UserName}. {senderProfile.UserName} and {receiverProfile.UserName} are now friends!</color>");
            OnInviteAccepted?.Invoke(invite); // Notify observers
            return true;
        }

        /// <summary>
        /// Declines a pending friend invite.
        /// </summary>
        /// <param name="inviteId">The ID of the invite to decline.</param>
        /// <param name="declinerId">The ID of the user declining the invite (must be the receiver).</param>
        /// <returns>True if declined, false otherwise.</returns>
        public bool DeclineInvite(string inviteId, string declinerId)
        {
            if (!allInvites.TryGetValue(inviteId, out FriendInvite invite))
            {
                Debug.LogError($"Invite with ID '{inviteId}' not found.");
                return false;
            }

            if (invite.ReceiverId != declinerId)
            {
                Debug.LogWarning($"User '{declinerId}' is not the receiver of invite '{inviteId}'.");
                return false;
            }

            if (invite.Status != InviteStatus.Pending)
            {
                Debug.LogWarning($"Invite '{inviteId}' is not pending (current status: {invite.Status}). Cannot decline.");
                return false;
            }

            // Remove invite from pending lists
            CleanupInvite(invite);

            invite.Status = InviteStatus.Declined;
            Debug.Log($"<color=yellow>Invite '{inviteId}' declined by {GetUserProfile(declinerId).UserName}.</color>");
            OnInviteDeclined?.Invoke(invite); // Notify observers
            return true;
        }

        /// <summary>
        /// Removes an existing friendship between two users.
        /// </summary>
        /// <param name="userId1">ID of the first user.</param>
        /// <param name="userId2">ID of the second user.</param>
        /// <returns>True if friendship was removed, false otherwise.</returns>
        public bool RemoveFriend(string userId1, string userId2)
        {
            if (!userProfiles.TryGetValue(userId1, out UserProfile user1Profile) ||
                !userProfiles.TryGetValue(userId2, out UserProfile user2Profile))
            {
                Debug.LogError("One or both users not found.");
                return false;
            }

            if (!user1Profile.Friends.Contains(userId2))
            {
                Debug.LogWarning($"'{user1Profile.UserName}' and '{user2Profile.UserName}' are not friends.");
                return false;
            }

            user1Profile.Friends.Remove(userId2);
            user2Profile.Friends.Remove(userId1);

            Debug.Log($"<color=red>'{user1Profile.UserName}' and '{user2Profile.UserName}' are no longer friends.</color>");
            OnFriendRemoved?.Invoke(user1Profile, user2Profile); // Notify observers
            return true;
        }

        /// <summary>
        /// Blocks a user, preventing any future interaction and cancelling existing friendships/invites.
        /// </summary>
        /// <param name="blockerId">The ID of the user doing the blocking.</param>
        /// <param name="blockedId">The ID of the user being blocked.</param>
        /// <returns>True if blocked, false otherwise.</returns>
        public bool BlockUser(string blockerId, string blockedId)
        {
            if (blockerId == blockedId)
            {
                Debug.LogWarning("Cannot block self.");
                return false;
            }
            if (!userProfiles.TryGetValue(blockerId, out UserProfile blockerProfile) ||
                !userProfiles.TryGetValue(blockedId, out UserProfile blockedProfile))
            {
                Debug.LogError("One or both users not found for blocking operation.");
                return false;
            }

            if (blockerProfile.BlockedUsers.Contains(blockedId))
            {
                Debug.LogWarning($"'{blockerProfile.UserName}' has already blocked '{blockedProfile.UserName}'.");
                return false;
            }

            blockerProfile.BlockedUsers.Add(blockedId);
            Debug.Log($"<color=red>'{blockerProfile.UserName}' has blocked '{blockedProfile.UserName}'.</color>");

            // --- Enforce blocking rules ---
            // 1. Remove friendship if exists
            if (blockerProfile.Friends.Contains(blockedId))
            {
                RemoveFriend(blockerId, blockedId); // This will trigger OnFriendRemoved event
            }

            // 2. Cancel any pending invites between them
            // Invites sent by blocker to blocked
            var sentInvitesToBlocked = blockerProfile.PendingSentInvites
                .Where(id => allInvites.ContainsKey(id) && allInvites[id].ReceiverId == blockedId && allInvites[id].Status == InviteStatus.Pending)
                .ToList();
            foreach (var inviteId in sentInvitesToBlocked)
            {
                CancelInviteInternal(allInvites[inviteId]);
            }

            // Invites sent by blocked to blocker
            var receivedInvitesFromBlocked = blockerProfile.PendingReceivedInvites
                .Where(id => allInvites.ContainsKey(id) && allInvites[id].SenderId == blockedId && allInvites[id].Status == InviteStatus.Pending)
                .ToList();
            foreach (var inviteId in receivedInvitesFromBlocked)
            {
                CancelInviteInternal(allInvites[inviteId]);
            }

            OnUserBlocked?.Invoke(blockerProfile, blockedProfile); // Notify observers
            return true;
        }

        /// <summary>
        /// Checks if two users are currently friends.
        /// </summary>
        public bool AreFriends(string userId1, string userId2)
        {
            if (!userProfiles.TryGetValue(userId1, out UserProfile user1Profile) ||
                !userProfiles.TryGetValue(userId2, out UserProfile user2Profile))
            {
                return false;
            }
            return user1Profile.Friends.Contains(userId2) && user2Profile.Friends.Contains(userId1);
        }

        /// <summary>
        /// Gets all pending friend invites received by a specific user.
        /// </summary>
        public IEnumerable<FriendInvite> GetReceivedPendingInvites(string userId)
        {
            if (userProfiles.TryGetValue(userId, out UserProfile profile))
            {
                return profile.PendingReceivedInvites
                    .Where(id => allInvites.ContainsKey(id) && allInvites[id].Status == InviteStatus.Pending)
                    .Select(id => allInvites[id]);
            }
            return Enumerable.Empty<FriendInvite>();
        }
        
        /// <summary>
        /// Gets all pending friend invites sent by a specific user.
        /// </summary>
        public IEnumerable<FriendInvite> GetSentPendingInvites(string userId)
        {
            if (userProfiles.TryGetValue(userId, out UserProfile profile))
            {
                return profile.PendingSentInvites
                    .Where(id => allInvites.ContainsKey(id) && allInvites[id].Status == InviteStatus.Pending)
                    .Select(id => allInvites[id]);
            }
            return Enumerable.Empty<FriendInvite>();
        }

        // --- Helper Methods ---

        /// <summary>
        /// Internal method to clean up an invite from user pending lists.
        /// </summary>
        private void CleanupInvite(FriendInvite invite)
        {
            if (userProfiles.TryGetValue(invite.SenderId, out UserProfile senderProfile))
            {
                senderProfile.PendingSentInvites.Remove(invite.InviteId);
            }
            if (userProfiles.TryGetValue(invite.ReceiverId, out UserProfile receiverProfile))
            {
                receiverProfile.PendingReceivedInvites.Remove(invite.InviteId);
            }
            // Note: The invite itself is kept in 'allInvites' with its updated status (Accepted/Declined/Cancelled)
            // for historical purposes, but is no longer "pending".
        }

        /// <summary>
        /// Internal method to cancel an invite (e.g., when a user is blocked).
        /// </summary>
        private void CancelInviteInternal(FriendInvite invite)
        {
            if (invite.Status == InviteStatus.Pending)
            {
                invite.Status = InviteStatus.Cancelled;
                CleanupInvite(invite);
                Debug.LogWarning($"Invite {invite.InviteId} from {GetUserProfile(invite.SenderId).UserName} to {GetUserProfile(invite.ReceiverId).UserName} cancelled.");
                OnInviteCancelled?.Invoke(invite);
            }
        }
    }
}
```

### 3. `FriendInviteSystemDemo.cs` (Example Usage)

This `MonoBehaviour` shows how to interact with the `FriendInviteSystem` and subscribe to its events. Attach this script to any GameObject in your scene.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FriendInviteSystem
{
    /// <summary>
    /// Demonstrates the usage of the FriendInviteSystem.
    /// Attach this script to an empty GameObject in your scene.
    /// </summary>
    public class FriendInviteSystemDemo : MonoBehaviour
    {
        private FriendInviteSystem _friendSystem;

        void Start()
        {
            // Ensure the FriendInviteSystem exists in the scene
            // Or create it if it's not a DontDestroyOnLoad singleton already present
            _friendSystem = FriendInviteSystem.Instance;
            if (_friendSystem == null)
            {
                GameObject systemGO = new GameObject("FriendInviteSystem");
                _friendSystem = systemGO.AddComponent<FriendInviteSystem>();
            }

            Debug.Log("\n--- FriendInviteSystem Demo Started ---");

            // --- Subscribe to events (Observer Pattern in action!) ---
            _friendSystem.OnInviteSent += HandleInviteSent;
            _friendSystem.OnInviteAccepted += HandleInviteAccepted;
            _friendSystem.OnInviteDeclined += HandleInviteDeclined;
            _friendSystem.OnInviteCancelled += HandleInviteCancelled;
            _friendSystem.OnFriendRemoved += HandleFriendRemoved;
            _friendSystem.OnUserBlocked += HandleUserBlocked;

            StartCoroutine(RunDemoSequence());
        }

        void OnDestroy()
        {
            // Always unsubscribe from events to prevent memory leaks and null reference exceptions
            if (_friendSystem != null)
            {
                _friendSystem.OnInviteSent -= HandleInviteSent;
                _friendSystem.OnInviteAccepted -= HandleInviteAccepted;
                _friendSystem.OnInviteDeclined -= HandleInviteDeclined;
                _friendSystem.OnInviteCancelled -= HandleInviteCancelled;
                _friendSystem.OnFriendRemoved -= HandleFriendRemoved;
                _friendSystem.OnUserBlocked -= HandleUserBlocked;
            }
        }

        // --- Event Handlers ---
        private void HandleInviteSent(FriendInvite invite)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: {_friendSystem.GetUserProfile(invite.SenderId).UserName} sent an invite to {_friendSystem.GetUserProfile(invite.ReceiverId).UserName}.</color>");
        }

        private void HandleInviteAccepted(FriendInvite invite)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: {_friendSystem.GetUserProfile(invite.ReceiverId).UserName} accepted {_friendSystem.GetUserProfile(invite.SenderId).UserName}'s invite! You are now friends.</color>");
            RefreshFriendListUI(invite.SenderId); // Simulate UI update
            RefreshFriendListUI(invite.ReceiverId); // Simulate UI update
        }

        private void HandleInviteDeclined(FriendInvite invite)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: {_friendSystem.GetUserProfile(invite.ReceiverId).UserName} declined {_friendSystem.GetUserProfile(invite.SenderId).UserName}'s invite.</color>");
        }

        private void HandleInviteCancelled(FriendInvite invite)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: Invite from {_friendSystem.GetUserProfile(invite.SenderId).UserName} to {_friendSystem.GetUserProfile(invite.ReceiverId).UserName} was cancelled.</color>");
        }

        private void HandleFriendRemoved(UserProfile user1, UserProfile user2)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: {user1.UserName} and {user2.UserName} are no longer friends.</color>");
            RefreshFriendListUI(user1.UserId); // Simulate UI update
            RefreshFriendListUI(user2.UserId); // Simulate UI update
        }

        private void HandleUserBlocked(UserProfile blocker, UserProfile blocked)
        {
            Debug.Log($"<color=blue>[UI/NOTIF] Notification: {blocker.UserName} blocked {blocked.UserName}.</color>");
            RefreshFriendListUI(blocker.UserId); // Simulate UI update (blocked users might not appear)
        }

        // Simulate UI refresh for a specific user's friend list
        private void RefreshFriendListUI(string userId)
        {
            UserProfile profile = _friendSystem.GetUserProfile(userId);
            if (profile != null)
            {
                string friends = profile.Friends.Count > 0 ? string.Join(", ", profile.Friends.Select(id => _friendSystem.GetUserProfile(id).UserName)) : "No friends";
                string pendingReceived = profile.PendingReceivedInvites.Count > 0 ? string.Join(", ", _friendSystem.GetReceivedPendingInvites(userId).Select(i => _friendSystem.GetUserProfile(i.SenderId).UserName + "(P)")) : "No pending received";
                Debug.Log($"<color=magenta>[UI] {profile.UserName}'s Friend List Refresh: Friends: [{friends}] | Pending Received: [{pendingReceived}]</color>");
            }
        }

        // --- Demo Sequence ---
        IEnumerator RunDemoSequence()
        {
            yield return new WaitForSeconds(1f);

            // 1. Register users
            UserProfile alice = _friendSystem.RegisterUser("user_A", "Alice");
            UserProfile bob = _friendSystem.RegisterUser("user_B", "Bob");
            UserProfile charlie = _friendSystem.RegisterUser("user_C", "Charlie");
            UserProfile david = _friendSystem.RegisterUser("user_D", "David");

            yield return new WaitForSeconds(1f);
            Debug.Log("\n--- Scenario 1: Alice invites Bob, Bob accepts ---");
            _friendSystem.SendInvite(alice.UserId, bob.UserId); // Alice invites Bob
            yield return new WaitForSeconds(1.5f);
            _friendSystem.AcceptInvite(bob.PendingReceivedInvites.First(), bob.UserId); // Bob accepts

            yield return new WaitForSeconds(2f);
            Debug.Log("\n--- Scenario 2: Alice invites Charlie, Charlie declines ---");
            _friendSystem.SendInvite(alice.UserId, charlie.UserId); // Alice invites Charlie
            yield return new WaitForSeconds(1.5f);
            _friendSystem.DeclineInvite(charlie.PendingReceivedInvites.First(), charlie.UserId); // Charlie declines

            yield return new WaitForSeconds(2f);
            Debug.Log("\n--- Scenario 3: Bob invites David, David blocks Bob while invite is pending ---");
            _friendSystem.SendInvite(bob.UserId, david.UserId); // Bob invites David
            yield return new WaitForSeconds(1.5f);
            _friendSystem.BlockUser(david.UserId, bob.UserId); // David blocks Bob
            // The invite from Bob to David should now be cancelled.
            // If David tries to accept it later, it should fail.
            yield return new WaitForSeconds(0.5f);
            // Try to accept a cancelled invite
            if (david.PendingReceivedInvites.Any())
            {
                 Debug.Log($"<color=yellow>David tries to accept Bob's invite after blocking Bob:</color>");
                 _friendSystem.AcceptInvite(david.PendingReceivedInvites.First(), david.UserId);
            }
            else
            {
                Debug.Log("<color=green>Invite was correctly removed from David's pending list after blocking Bob.</color>");
            }


            yield return new WaitForSeconds(2f);
            Debug.Log("\n--- Scenario 4: Alice removes Bob as a friend ---");
            Debug.Log($"Are Alice and Bob friends before removal? {_friendSystem.AreFriends(alice.UserId, bob.UserId)}");
            _friendSystem.RemoveFriend(alice.UserId, bob.UserId);
            yield return new WaitForSeconds(1f);
            Debug.Log($"Are Alice and Bob friends after removal? {_friendSystem.AreFriends(alice.UserId, bob.UserId)}");

            yield return new WaitForSeconds(2f);
            Debug.Log("\n--- Scenario 5: Alice tries to invite someone she has blocked (Bob) ---");
            _friendSystem.BlockUser(alice.UserId, bob.UserId); // Alice blocks Bob
            yield return new WaitForSeconds(1f);
            _friendSystem.SendInvite(alice.UserId, bob.UserId); // Alice tries to invite Bob (should fail)

            yield return new WaitForSeconds(2f);
            Debug.Log("\n--- Scenario 6: Cleanup and final status ---");
            RefreshFriendListUI(alice.UserId);
            RefreshFriendListUI(bob.UserId);
            RefreshFriendListUI(charlie.UserId);
            RefreshFriendListUI(david.UserId);

            Debug.Log("\n--- FriendInviteSystem Demo Finished ---");
        }
    }
}
```

---

### How to Use in Unity:

1.  Create a new C# script named `UserProfile.cs` and paste the content of the first code block into it.
2.  Create a new C# script named `FriendInviteSystem.cs` and paste the content of the second code block into it.
3.  Create a new C# script named `FriendInviteSystemDemo.cs` and paste the content of the third code block into it.
4.  In your Unity scene, create an empty GameObject (e.g., name it `DemoManager`).
5.  Drag the `FriendInviteSystemDemo.cs` script onto the `DemoManager` GameObject.
6.  Run the scene.

You will see detailed logs in the Unity Console explaining each step of the demo, demonstrating how users are registered, invites are sent, accepted, declined, friends are removed, and users are blocked, all managed by the central `FriendInviteSystem`. The `[UI/NOTIF]` messages simulate how other parts of your game (like UI or a notification center) would react to these events.