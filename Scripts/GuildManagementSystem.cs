// Unity Design Pattern Example: GuildManagementSystem
// This script demonstrates the GuildManagementSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements a robust 'GuildManagementSystem' pattern for Unity, focusing on practical use, clear separation of concerns, and common game development practices.

The core idea of a 'GuildManagementSystem' is to centralize all logic related to guilds (creation, joining, leaving, member management, roles, permissions) into a single, accessible service. This pattern is not one of the GoF design patterns but rather a common architectural pattern for managing a specific game feature (guilds) using principles from various patterns like Singleton, Service Locator, and Observer.

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like FirstOrDefault, Any

// =======================================================================================
// GUILD MANAGEMENT SYSTEM DESIGN PATTERN
// =======================================================================================
// This C# Unity script demonstrates a comprehensive Guild Management System.
// It is designed to be a central authority for all guild-related operations in a game.
//
// Key Components:
// 1.  GuildRole & GuildPermission Enums: Define member ranks and what actions they can perform.
// 2.  GuildPermissionsConfig: A static helper to map roles to their specific permissions,
//     making the system flexible and configurable.
// 3.  GuildMember Class: Represents a player's membership within a specific guild,
//     holding their role, join date, and a reference to their player ID and guild ID.
//     It encapsulates player-specific guild data.
// 4.  Guild Class: Represents a single guild, containing its core properties (ID, Name,
//     Description) and a collection of its GuildMember objects. It manages its internal
//     member list.
// 5.  GuildManager (Singleton MonoBehaviour): The heart of the system.
//     -   Implemented as a Singleton to provide a global, easy-to-access point for
//         all guild operations (Service Locator pattern).
//     -   Manages a collection of all active guilds.
//     -   Provides public methods for creating, joining, leaving, kicking, promoting,
//         and disbanding guilds/members.
//     -   Uses C# events to notify other parts of the game (UI, other systems) about
//         guild-related changes (Observer pattern), promoting loose coupling.
//
// Design Principles Applied:
// -   Encapsulation: Guild and GuildMember classes hide their internal data structures
//     and expose controlled access via public methods.
// -   Separation of Concerns: The GuildManager focuses solely on guild logic,
//     while UI or network layers would interact with it without knowing its internal workings.
// -   Event-Driven: Decouples the GuildManager from systems that need to react to guild events.
// -   Robustness: Includes validation and error handling for common scenarios.
// -   Scalability: Designed to be extensible for future features like guild quests,
//     guild bank, or more complex permission structures.
//
// How to Use:
// 1. Create an empty GameObject in your Unity scene (e.g., "GameManagers").
// 2. Attach the `GuildManager` script to this GameObject. It will automatically set itself
//    up as a singleton and persist across scene loads.
// 3. Attach the `GuildSystemExample` script to any GameObject in your scene (or the same
//    "GameManagers" object) to see a demonstration of its functionality in the Console.
// 4. Interact with `GuildManager.Instance` from any other script in your game to perform
//    guild operations. Subscribe to its events to react to changes.
// =======================================================================================


// --- 1. Enums and Data Structures ---

/// <summary>
/// Defines the different roles a player can have within a guild.
/// These roles determine the permissions a player has.
/// </summary>
public enum GuildRole
{
    Recruit,    // Lowest tier, typically new members with minimal permissions.
    Member,     // Standard member with basic permissions.
    Officer,    // Can manage some aspects of the guild (invites, kicks, promotions of lower ranks).
    Leader      // Has full control over the guild (disband, transfer leadership, all permissions).
}

/// <summary>
/// Defines granular permissions that can be assigned to guild roles.
/// Using [Flags] allows combining multiple permissions into a single value,
/// enabling flexible role-based access control (RBAC).
/// </summary>
[Flags]
public enum GuildPermission
{
    None            = 0,
    InviteMembers   = 1 << 0, // Can send guild invitations.
    KickMembers     = 1 << 1, // Can remove members of lower rank.
    PromoteDemote   = 1 << 2, // Can change member roles (up to their own role's limit).
    ManageGuildInfo = 1 << 3, // Can change guild name, description.
    AccessGuildBank = 1 << 4, // Can withdraw/deposit items/currency from guild bank.
    StartGuildQuest = 1 << 5, // Can initiate guild-wide quests or events.
    DisbandGuild    = 1 << 6, // Can permanently delete the guild (typically Leader only).
    All             = ~0      // Represents all possible permissions.
}

/// <summary>
/// A static helper class to define and retrieve the permissions for each GuildRole.
/// This centralizes permission configuration and makes it easy to check permissions.
/// </summary>
public static class GuildPermissionsConfig
{
    private static readonly Dictionary<GuildRole, GuildPermission> _rolePermissions =
        new Dictionary<GuildRole, GuildPermission>
        {
            { GuildRole.Recruit,    GuildPermission.None },
            { GuildRole.Member,     GuildPermission.AccessGuildBank | GuildPermission.StartGuildQuest },
            { GuildRole.Officer,    GuildPermission.InviteMembers | GuildPermission.KickMembers | GuildPermission.PromoteDemote |
                                    GuildPermission.ManageGuildInfo | GuildPermission.AccessGuildBank | GuildPermission.StartGuildQuest },
            { GuildRole.Leader,     GuildPermission.All } // The leader has all permissions.
        };

    /// <summary>
    /// Gets the combined permissions associated with a specific guild role.
    /// </summary>
    /// <param name="role">The GuildRole to query.</param>
    /// <returns>A GuildPermission flag enum representing all permissions for that role.</returns>
    public static GuildPermission GetPermissions(GuildRole role)
    {
        if (_rolePermissions.TryGetValue(role, out GuildPermission permissions))
        {
            return permissions;
        }
        return GuildPermission.None; // Default to no permissions if role is not found.
    }

    /// <summary>
    /// Checks if a given role has a specific permission.
    /// </summary>
    /// <param name="role">The GuildRole to check.</param>
    /// <param name="permission">The specific GuildPermission to verify.</param>
    /// <returns>True if the role has the permission, false otherwise.</returns>
    public static bool HasPermission(GuildRole role, GuildPermission permission)
    {
        return (GetPermissions(role) & permission) == permission;
    }
}


/// <summary>
/// Represents a player's individual membership details within a specific guild.
/// This is distinct from the player's core game data.
/// Marked [System.Serializable] to allow it to be saved/loaded by Unity's default
/// serialization or for debugging in the Inspector if part of a MonoBehaviour.
/// </summary>
[System.Serializable]
public class GuildMember
{
    public string PlayerId { get; private set; } // Unique identifier for the player.
    public GuildRole Role { get; private set; }   // The member's current role in the guild.
    public DateTime JoinDate { get; private set; } // The date and time the player joined.
    public string GuildId { get; private set; }   // The ID of the guild this member belongs to.

    public GuildMember(string playerId, string guildId, GuildRole role = GuildRole.Recruit)
    {
        if (string.IsNullOrEmpty(playerId)) throw new ArgumentNullException(nameof(playerId), "Player ID cannot be null or empty.");
        if (string.IsNullOrEmpty(guildId)) throw new ArgumentNullException(nameof(guildId), "Guild ID cannot be null or empty.");

        PlayerId = playerId;
        GuildId = guildId;
        Role = role;
        JoinDate = DateTime.Now;
    }

    /// <summary>
    /// Changes the role of this guild member. This method is typically called by the
    /// GuildManager after permission checks.
    /// </summary>
    /// <param name="newRole">The new role to assign to the member.</param>
    public void SetRole(GuildRole newRole)
    {
        Role = newRole;
    }

    /// <summary>
    /// Checks if this specific guild member has a required permission based on their current role.
    /// </summary>
    /// <param name="permission">The GuildPermission to check.</param>
    /// <returns>True if the member's role grants the permission, false otherwise.</returns>
    public bool HasPermission(GuildPermission permission)
    {
        return GuildPermissionsConfig.HasPermission(Role, permission);
    }
}

/// <summary>
/// Represents a single guild entity. It holds the guild's general information
/// and manages its collection of members.
/// Marked [System.Serializable] for potential save/load capabilities or Inspector debugging.
/// </summary>
[System.Serializable]
public class Guild
{
    public string Id { get; private set; } // Unique identifier for the guild.
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreationDate { get; private set; }

    // Using a Dictionary for members for efficient O(1) average-time lookup by PlayerId.
    // IReadOnlyDictionary provides safe, read-only access to the collection from outside.
    public IReadOnlyDictionary<string, GuildMember> Members => _members;
    private Dictionary<string, GuildMember> _members;

    /// <summary>
    /// Gets the PlayerId of the current guild leader.
    /// Uses LINQ to find the first member with the Leader role.
    /// </summary>
    public string LeaderPlayerId
    {
        get { return _members.Values.FirstOrDefault(m => m.Role == GuildRole.Leader)?.PlayerId; }
    }

    public Guild(string name, string description, string founderPlayerId)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name), "Guild name cannot be null or empty.");
        if (string.IsNullOrEmpty(founderPlayerId)) throw new ArgumentNullException(nameof(founderPlayerId), "Founder Player ID cannot be null or empty.");

        Id = System.Guid.NewGuid().ToString(); // Generate a unique, globally-unique ID.
        Name = name;
        Description = description;
        CreationDate = DateTime.Now;
        _members = new Dictionary<string, GuildMember>();

        // The founder is automatically assigned the Leader role upon guild creation.
        AddMember(new GuildMember(founderPlayerId, Id, GuildRole.Leader));
    }

    /// <summary>
    /// Adds a new member to the guild's internal member list.
    /// This method performs basic internal validation. External permission checks
    /// should be handled by the GuildManager.
    /// </summary>
    /// <param name="member">The GuildMember object to add.</param>
    public void AddMember(GuildMember member)
    {
        if (member == null) return;
        if (_members.ContainsKey(member.PlayerId))
        {
            Debug.LogWarning($"Player {member.PlayerId} is already a member of guild {Name}.");
            return;
        }
        if (member.GuildId != Id)
        {
            Debug.LogError($"Attempted to add member to wrong guild! Member's GuildId: {member.GuildId}, Current GuildId: {Id}");
            return;
        }
        _members.Add(member.PlayerId, member);
        Debug.Log($"Player {member.PlayerId} joined guild {Name} as {member.Role}.");
    }

    /// <summary>
    /// Removes a member from the guild's internal member list.
    /// </summary>
    /// <param name="playerId">The unique ID of the player to remove.</param>
    public void RemoveMember(string playerId)
    {
        if (!_members.ContainsKey(playerId))
        {
            Debug.LogWarning($"Player {playerId} is not a member of guild {Name}.");
            return;
        }
        _members.Remove(playerId);
        Debug.Log($"Player {playerId} left guild {Name}.");
    }

    /// <summary>
    /// Retrieves a GuildMember object by their PlayerId.
    /// </summary>
    /// <param name="playerId">The unique ID of the player.</param>
    /// <returns>The GuildMember object if found, otherwise null.</returns>
    public GuildMember GetMember(string playerId)
    {
        _members.TryGetValue(playerId, out GuildMember member);
        return member;
    }

    /// <summary>
    /// Updates the guild's name and description.
    /// </summary>
    /// <param name="newName">The new name for the guild.</param>
    /// <param name="newDescription">The new description for the guild.</param>
    public void UpdateInfo(string newName, string newDescription)
    {
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("Guild name cannot be empty, not updating name.");
            return;
        }
        Name = newName;
        Description = newDescription;
        Debug.Log($"Guild {Id} info updated. New Name: {Name}, New Description: {Description}");
    }

    /// <summary>
    /// Returns the current number of members in the guild.
    /// </summary>
    public int GetMemberCount()
    {
        return _members.Count;
    }
}

// --- 2. The Guild Management System (Singleton MonoBehaviour) ---

/// <summary>
/// The central manager for all guild-related operations in the game.
/// Implemented as a Singleton MonoBehaviour to ensure there's only one instance
/// and it's globally accessible. It also persists across scene loads.
/// </summary>
public class GuildManager : MonoBehaviour
{
    // --- Singleton Pattern Implementation ---
    private static GuildManager _instance;
    public static GuildManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<GuildManager>();

                // If no instance exists, create a new GameObject and add the component
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(GuildManager).Name);
                    _instance = singletonObject.AddComponent<GuildManager>();
                }
                // Ensure the manager persists across scene loads
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // --- Guild Data Storage ---
    // A dictionary to store all active guilds, keyed by their unique Id for quick lookup.
    private Dictionary<string, Guild> _guilds = new Dictionary<string, Guild>();
    // A dictionary to quickly find which guild a specific player belongs to (PlayerId -> GuildId).
    // This allows O(1) checks for player's guild status.
    private Dictionary<string, string> _playerToGuildIdMap = new Dictionary<string, string>();

    // --- Events for Notifying Other Systems (Observer Pattern) ---
    // These events allow UI, other game systems, or network layers to react to guild changes
    // without being tightly coupled to the GuildManager's internal logic.
    public event Action<Guild> OnGuildCreated;
    public event Action<Guild> OnGuildDisbanded;
    public event Action<string, GuildMember> OnMemberJoinedGuild; // Args: PlayerId, GuildMember
    public event Action<string, string> OnMemberLeftGuild;       // Args: PlayerId, GuildId
    public event Action<string, GuildMember> OnMemberRoleChanged; // Args: PlayerId, GuildMember with new role
    public event Action<string, string, string> OnGuildInfoUpdated; // Args: GuildId, NewName, NewDescription

    private void Awake()
    {
        // Enforce the singleton pattern: if another instance already exists, destroy this one.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Make sure the manager persists.

        Debug.Log("GuildManager Initialized. Ready for operations.");
        // In a real project, this is the place to load existing guild data from
        // a persistent storage (e.g., JSON, database, network data).
    }

    // --- Public API for Guild Operations ---

    /// <summary>
    /// Creates a new guild. The player who creates it automatically becomes the leader.
    /// </summary>
    /// <param name="guildName">The desired name for the new guild.</param>
    /// <param name="description">A short description for the guild.</param>
    /// <param name="founderPlayerId">The unique ID of the player initiating the guild creation.</param>
    /// <returns>The newly created Guild object, or null if creation failed (e.g., name taken, player already in guild).</returns>
    public Guild CreateGuild(string guildName, string description, string founderPlayerId)
    {
        if (string.IsNullOrWhiteSpace(guildName))
        {
            Debug.LogError("Guild name cannot be empty or just whitespace.");
            return null;
        }
        // Check if a guild with the proposed name already exists (case-insensitive).
        if (_guilds.Values.Any(g => g.Name.Equals(guildName, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.LogError($"Guild with name '{guildName}' already exists. Please choose a different name.");
            return null;
        }
        // Check if the founder is already in a guild.
        if (_playerToGuildIdMap.ContainsKey(founderPlayerId))
        {
            Debug.LogError($"Player '{founderPlayerId}' is already in a guild and cannot create a new one.");
            return null;
        }

        // Create the new guild instance.
        Guild newGuild = new Guild(guildName, description, founderPlayerId);
        _guilds.Add(newGuild.Id, newGuild); // Add to the main guild storage.
        _playerToGuildIdMap.Add(founderPlayerId, newGuild.Id); // Map the founder to the new guild.

        Debug.Log($"Guild '{newGuild.Name}' (ID: {newGuild.Id}) successfully created by '{founderPlayerId}'.");
        OnGuildCreated?.Invoke(newGuild); // Notify subscribers.

        return newGuild;
    }

    /// <summary>
    /// Disbands an existing guild. This operation requires the initiating player to be the guild leader.
    /// All members will be removed from the guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to disband.</param>
    /// <param name="initiatorPlayerId">The ID of the player attempting to disband the guild.</param>
    /// <returns>True if the guild was successfully disbanded, false otherwise.</returns>
    public bool DisbandGuild(string guildId, string initiatorPlayerId)
    {
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Disband Failed: Guild with ID '{guildId}' not found.");
            return false;
        }

        // Verify that the initiator is the guild leader and has permission.
        GuildMember initiatorMember = guild.GetMember(initiatorPlayerId);
        if (initiatorMember == null || initiatorMember.PlayerId != guild.LeaderPlayerId || !initiatorMember.HasPermission(GuildPermission.DisbandGuild))
        {
            Debug.LogError($"Disband Failed: Player '{initiatorPlayerId}' does not have permission to disband guild '{guild.Name}'. Only the leader can do this.");
            return false;
        }

        // Remove all members from the player-to-guild map and notify their departure.
        List<string> membersToRemove = new List<string>(guild.Members.Keys);
        foreach (var memberId in membersToRemove)
        {
            _playerToGuildIdMap.Remove(memberId);
            OnMemberLeftGuild?.Invoke(memberId, guild.Id); // Notify each member that they left.
        }

        _guilds.Remove(guildId); // Remove the guild from the main storage.
        Debug.Log($"Guild '{guild.Name}' (ID: {guildId}) has been successfully disbanded by '{initiatorPlayerId}'.");
        OnGuildDisbanded?.Invoke(guild); // Notify subscribers.
        return true;
    }

    /// <summary>
    /// Invites a player to a guild. This operation requires the inviter to have 'InviteMembers' permission.
    /// In a real game, this would likely trigger a UI notification and a pending invite state.
    /// For this example, it only logs the invitation. Use `AcceptInvite` to complete the join.
    /// </summary>
    /// <param name="guildId">The ID of the guild sending the invitation.</param>
    /// <param name="inviterPlayerId">The ID of the player sending the invite.</param>
    /// <param name="invitedPlayerId">The ID of the player being invited.</param>
    /// <returns>True if the invitation was conceptually sent (passed initial checks), false otherwise.</returns>
    public bool InvitePlayer(string guildId, string inviterPlayerId, string invitedPlayerId)
    {
        if (invitedPlayerId == inviterPlayerId)
        {
            Debug.LogError("Invite Failed: Cannot invite self to guild.");
            return false;
        }
        if (_playerToGuildIdMap.ContainsKey(invitedPlayerId))
        {
            Debug.LogError($"Invite Failed: Player '{invitedPlayerId}' is already in a guild.");
            return false;
        }
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Invite Failed: Guild with ID '{guildId}' not found.");
            return false;
        }

        // Check if the inviter has the necessary permission.
        GuildMember inviterMember = guild.GetMember(inviterPlayerId);
        if (inviterMember == null || !inviterMember.HasPermission(GuildPermission.InviteMembers))
        {
            Debug.LogError($"Invite Failed: Player '{inviterPlayerId}' does not have permission to invite members to guild '{guild.Name}'.");
            return false;
        }

        Debug.Log($"Invitation Sent: Player '{inviterPlayerId}' invited '{invitedPlayerId}' to guild '{guild.Name}'.");
        // In a real game, you would now store this invitation (e.g., in a list of pending invites
        // on the invitedPlayerId's data) and show a UI notification.
        return true;
    }

    /// <summary>
    /// Accepts a guild invitation, adding the player to the specified guild.
    /// </summary>
    /// <param name="playerToJoinId">The ID of the player accepting the invite.</param>
    /// <param name="guildId">The ID of the guild the player is joining.</param>
    /// <returns>True if the player successfully joined the guild, false otherwise.</returns>
    public bool AcceptInvite(string playerToJoinId, string guildId)
    {
        if (_playerToGuildIdMap.ContainsKey(playerToJoinId))
        {
            Debug.LogError($"Join Failed: Player '{playerToJoinId}' is already in a guild.");
            return false;
        }
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Join Failed: Guild with ID '{guildId}' not found.");
            return false;
        }
        // In a real game, you'd also check if a valid invitation exists for this player/guild combo.

        // Create a new GuildMember object (default role is Recruit) and add it to the guild.
        GuildMember newMember = new GuildMember(playerToJoinId, guildId);
        guild.AddMember(newMember);
        _playerToGuildIdMap.Add(playerToJoinId, guildId); // Update the player-to-guild map.

        Debug.Log($"Player '{playerToJoinId}' successfully joined guild '{guild.Name}'.");
        OnMemberJoinedGuild?.Invoke(playerToJoinId, newMember); // Notify subscribers.
        return true;
    }

    /// <summary>
    /// A player leaves their current guild.
    /// Special handling for leaders: a leader cannot leave if there are other members
    /// unless they first transfer leadership or disband the guild. If the leader is the
    /// only member, the guild is automatically disbanded.
    /// </summary>
    /// <param name="playerToLeaveId">The ID of the player who wants to leave.</param>
    /// <returns>True if the player successfully left, false otherwise.</returns>
    public bool LeaveGuild(string playerToLeaveId)
    {
        if (!_playerToGuildIdMap.TryGetValue(playerToLeaveId, out string guildId))
        {
            Debug.LogError($"Leave Failed: Player '{playerToLeaveId}' is not in any guild.");
            return false;
        }
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Leave Failed: Guild with ID '{guildId}' not found, despite player '{playerToLeaveId}' being mapped to it. Data inconsistency corrected.");
            _playerToGuildIdMap.Remove(playerToLeaveId); // Correct inconsistent data.
            return false;
        }

        // Leader leaving logic:
        if (guild.LeaderPlayerId == playerToLeaveId)
        {
            if (guild.GetMemberCount() > 1)
            {
                Debug.LogError($"Leave Failed: Leader '{playerToLeaveId}' cannot leave guild '{guild.Name}' while other members exist. Leadership must be transferred or the guild must be disbanded first.");
                return false;
            }
            else // If leader is the only member, the guild disbands automatically.
            {
                Debug.Log($"Leader '{playerToLeaveId}' is the only member. Guild '{guild.Name}' will be disbanded.");
                return DisbandGuild(guild.Id, playerToLeaveId); // Disband the guild.
            }
        }

        // Standard member leaving process.
        guild.RemoveMember(playerToLeaveId);
        _playerToGuildIdMap.Remove(playerToLeaveId);

        Debug.Log($"Player '{playerToLeaveId}' successfully left guild '{guild.Name}'.");
        OnMemberLeftGuild?.Invoke(playerToLeaveId, guildId); // Notify subscribers.
        return true;
    }

    /// <summary>
    /// Kicks a member from a guild. The kicker must have 'KickMembers' permission
    /// and cannot kick members of equal or higher rank, nor the leader.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="kickerPlayerId">The ID of the player initiating the kick.</param>
    /// <param name="playerToKickId">The ID of the player to be kicked.</param>
    /// <returns>True if the player was successfully kicked, false otherwise.</returns>
    public bool KickMember(string guildId, string kickerPlayerId, string playerToKickId)
    {
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Kick Failed: Guild with ID '{guildId}' not found.");
            return false;
        }
        if (kickerPlayerId == playerToKickId)
        {
            Debug.LogError($"Kick Failed: Player '{kickerPlayerId}' cannot kick themselves. Use 'LeaveGuild' to leave.");
            return false;
        }

        GuildMember kickerMember = guild.GetMember(kickerPlayerId);
        GuildMember kickedMember = guild.GetMember(playerToKickId);

        if (kickerMember == null || !kickerMember.HasPermission(GuildPermission.KickMembers))
        {
            Debug.LogError($"Kick Failed: Player '{kickerPlayerId}' does not have permission to kick members from guild '{guild.Name}'.");
            return false;
        }
        if (kickedMember == null)
        {
            Debug.LogError($"Kick Failed: Player '{playerToKickId}' is not a member of guild '{guild.Name}'.");
            return false;
        }
        // A member cannot kick someone of equal or higher rank.
        if (kickedMember.Role >= kickerMember.Role)
        {
            Debug.LogError($"Kick Failed: Player '{kickerPlayerId}' (Role: {kickerMember.Role}) cannot kick '{playerToKickId}' (Role: {kickedMember.Role}) as their rank is not high enough.");
            return false;
        }
        // The guild leader cannot be kicked; they must transfer leadership or disband.
        if (kickedMember.PlayerId == guild.LeaderPlayerId)
        {
            Debug.LogError($"Kick Failed: Cannot kick the guild leader '{playerToKickId}'. The leader must transfer leadership or disband the guild.");
            return false;
        }

        guild.RemoveMember(playerToKickId);
        _playerToGuildIdMap.Remove(playerToKickId);

        Debug.Log($"Player '{playerToKickId}' was successfully kicked from guild '{guild.Name}' by '{kickerPlayerId}'.");
        OnMemberLeftGuild?.Invoke(playerToKickId, guildId); // Notify subscribers.
        return true;
    }

    /// <summary>
    /// Changes a member's role within a guild. The promoter must have 'PromoteDemote' permission.
    /// A promoter cannot promote/demote someone to a role higher than or equal to their own,
    /// and special rules apply for the leader's role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="promoterPlayerId">The ID of the player initiating the role change.</param>
    /// <param name="targetPlayerId">The ID of the player whose role is being changed.</param>
    /// <param name="newRole">The new role to assign to the target member.</param>
    /// <returns>True if the role was successfully changed, false otherwise.</returns>
    public bool ChangeMemberRole(string guildId, string promoterPlayerId, string targetPlayerId, GuildRole newRole)
    {
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Role Change Failed: Guild with ID '{guildId}' not found.");
            return false;
        }
        if (promoterPlayerId == targetPlayerId)
        {
            Debug.LogError($"Role Change Failed: Player '{promoterPlayerId}' cannot change their own role using this method. Use 'TransferLeadership' for leader changes.");
            return false;
        }

        GuildMember promoterMember = guild.GetMember(promoterPlayerId);
        GuildMember targetMember = guild.GetMember(targetPlayerId);

        if (promoterMember == null || !promoterMember.HasPermission(GuildPermission.PromoteDemote))
        {
            Debug.LogError($"Role Change Failed: Player '{promoterPlayerId}' does not have permission to change roles in guild '{guild.Name}'.");
            return false;
        }
        if (targetMember == null)
        {
            Debug.LogError($"Role Change Failed: Player '{targetPlayerId}' is not a member of guild '{guild.Name}'.");
            return false;
        }
        if (targetMember.Role == newRole)
        {
            Debug.LogWarning($"Role Change Failed: Player '{targetPlayerId}' is already a '{newRole}'. No change needed.");
            return false;
        }

        // Specific rules for leader roles and promotions:
        if (newRole == GuildRole.Leader)
        {
            Debug.LogError($"Role Change Failed: Cannot directly set '{newRole}'. Use 'TransferLeadership' to appoint a new leader.");
            return false;
        }
        if (targetMember.PlayerId == guild.LeaderPlayerId)
        {
            Debug.LogError($"Role Change Failed: Cannot change the role of the current guild leader '{targetPlayerId}'. Leadership must be transferred first.");
            return false;
        }
        // A promoter cannot promote/demote someone to a role that is higher than or equal to their own.
        // E.g., an Officer cannot promote another member to Officer or demote a Leader.
        if (newRole > promoterMember.Role || targetMember.Role >= promoterMember.Role)
        {
            Debug.LogError($"Role Change Failed: Player '{promoterPlayerId}' (Role: {promoterMember.Role}) cannot change the role of '{targetPlayerId}' (Current Role: {targetMember.Role}) to '{newRole}' as their rank is not high enough or target rank is invalid.");
            return false;
        }


        targetMember.SetRole(newRole); // Update the role.
        Debug.Log($"Player '{targetPlayerId}' in guild '{guild.Name}' had role successfully changed to '{newRole}' by '{promoterPlayerId}'.");
        OnMemberRoleChanged?.Invoke(targetPlayerId, targetMember); // Notify subscribers.
        return true;
    }

    /// <summary>
    /// Transfers guild leadership from the current leader to another existing guild member.
    /// Only the current leader can initiate this.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="currentLeaderPlayerId">The ID of the current leader.</param>
    /// <param name="newLeaderPlayerId">The ID of the player who will become the new leader.</param>
    /// <returns>True if leadership was successfully transferred, false otherwise.</returns>
    public bool TransferLeadership(string guildId, string currentLeaderPlayerId, string newLeaderPlayerId)
    {
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Transfer Leadership Failed: Guild with ID '{guildId}' not found.");
            return false;
        }
        if (guild.LeaderPlayerId != currentLeaderPlayerId)
        {
            Debug.LogError($"Transfer Leadership Failed: Player '{currentLeaderPlayerId}' is not the current leader of guild '{guild.Name}'.");
            return false;
        }
        if (currentLeaderPlayerId == newLeaderPlayerId)
        {
            Debug.LogError("Transfer Leadership Failed: Cannot transfer leadership to self.");
            return false;
        }

        GuildMember currentLeaderMember = guild.GetMember(currentLeaderPlayerId);
        GuildMember newLeaderMember = guild.GetMember(newLeaderPlayerId);

        if (newLeaderMember == null)
        {
            Debug.LogError($"Transfer Leadership Failed: Player '{newLeaderPlayerId}' is not a member of guild '{guild.Name}'. Cannot transfer leadership.");
            return false;
        }

        // Demote the current leader to an Officer (or a suitable default role).
        currentLeaderMember.SetRole(GuildRole.Officer);
        OnMemberRoleChanged?.Invoke(currentLeaderPlayerId, currentLeaderMember); // Notify subscribers.

        // Promote the target player to the new leader.
        newLeaderMember.SetRole(GuildRole.Leader);
        OnMemberRoleChanged?.Invoke(newLeaderPlayerId, newLeaderMember); // Notify subscribers.

        Debug.Log($"Leadership of guild '{guild.Name}' successfully transferred from '{currentLeaderPlayerId}' to '{newLeaderPlayerId}'.");
        return true;
    }

    /// <summary>
    /// Updates the guild's name and/or description. The updater must have 'ManageGuildInfo' permission.
    /// A new name must be unique.
    /// </summary>
    /// <param name="guildId">The ID of the guild to update.</param>
    /// <param name="updaterPlayerId">The ID of the player initiating the update.</param>
    /// <param name="newName">The new name for the guild. If null or empty, the name won't change.</param>
    /// <param name="newDescription">The new description for the guild. If null, the description won't change.</param>
    /// <returns>True if the guild information was updated, false otherwise.</returns>
    public bool UpdateGuildInfo(string guildId, string updaterPlayerId, string newName, string newDescription)
    {
        if (!_guilds.TryGetValue(guildId, out Guild guild))
        {
            Debug.LogError($"Update Info Failed: Guild with ID '{guildId}' not found.");
            return false;
        }

        GuildMember updaterMember = guild.GetMember(updaterPlayerId);
        if (updaterMember == null || !updaterMember.HasPermission(GuildPermission.ManageGuildInfo))
        {
            Debug.LogError($"Update Info Failed: Player '{updaterPlayerId}' does not have permission to manage guild info for '{guild.Name}'.");
            return false;
        }

        string effectiveNewName = string.IsNullOrEmpty(newName) ? guild.Name : newName;
        string effectiveNewDescription = newDescription ?? guild.Description; // Use null-coalescing for description

        // Check for duplicate name if the name is actually changing.
        if (effectiveNewName != guild.Name && _guilds.Values.Any(g => g.Name.Equals(effectiveNewName, StringComparison.OrdinalIgnoreCase) && g.Id != guild.Id))
        {
            Debug.LogError($"Update Info Failed: Guild with name '{effectiveNewName}' already exists.");
            return false;
        }

        guild.UpdateInfo(effectiveNewName, effectiveNewDescription); // Call the Guild's internal update method.
        OnGuildInfoUpdated?.Invoke(guildId, guild.Name, guild.Description); // Notify subscribers.
        return true;
    }


    // --- Public Getters (Read-Only Access) ---

    /// <summary>
    /// Retrieves a Guild object by its unique ID.
    /// </summary>
    /// <param name="guildId">The unique ID of the guild.</param>
    /// <returns>The Guild object if found, otherwise null.</returns>
    public Guild GetGuild(string guildId)
    {
        _guilds.TryGetValue(guildId, out Guild guild);
        return guild;
    }

    /// <summary>
    /// Retrieves a Guild object by its name (case-insensitive).
    /// </summary>
    /// <param name="guildName">The name of the guild.</param>
    /// <returns>The Guild object if found, otherwise null.</returns>
    public Guild GetGuildByName(string guildName)
    {
        return _guilds.Values.FirstOrDefault(g => g.Name.Equals(guildName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieves the Guild object that a specific player belongs to.
    /// </summary>
    /// <param name="playerId">The unique ID of the player.</param>
    /// <returns>The Guild object if the player is in a guild, otherwise null.</returns>
    public Guild GetPlayerGuild(string playerId)
    {
        if (_playerToGuildIdMap.TryGetValue(playerId, out string guildId))
        {
            return GetGuild(guildId);
        }
        return null;
    }

    /// <summary>
    /// Checks if a player is currently a member of any guild.
    /// </summary>
    /// <param name="playerId">The unique ID of the player.</param>
    /// <returns>True if the player is in a guild, false otherwise.</returns>
    public bool IsPlayerInGuild(string playerId)
    {
        return _playerToGuildIdMap.ContainsKey(playerId);
    }

    /// <summary>
    /// Returns a read-only collection of all active guilds managed by the system.
    /// </summary>
    public IReadOnlyCollection<Guild> GetAllGuilds()
    {
        return _guilds.Values;
    }
}


// =======================================================================================
// EXAMPLE USAGE
// =======================================================================================
/// <summary>
/// This class demonstrates how to interact with the GuildManager.
/// Attach this script to any GameObject in your scene to run the example scenario
/// and see the output in the Unity Console.
/// </summary>
public class GuildSystemExample : MonoBehaviour
{
    // Define some constant player IDs for easy testing.
    private const string PLAYER_ALICE = "Player_Alice";
    private const string PLAYER_BOB = "Player_Bob";
    private const string PLAYER_CHARLIE = "Player_Charlie";
    private const string PLAYER_DAVE = "Player_Dave";
    private const string PLAYER_EVE = "Player_Eve";

    private string myGuildId;   // To store the ID of the guild created in the example.
    private string myGuildName = "Unity Devs Guild";

    void Start()
    {
        Debug.Log("--- Guild System Example Start ---");

        // --- Subscribe to GuildManager events ---
        // This is how other parts of your game (e.g., UI elements, other game systems)
        // would react to changes in the guild system.
        GuildManager.Instance.OnGuildCreated += OnGuildCreated;
        GuildManager.Instance.OnGuildDisbanded += OnGuildDisbanded;
        GuildManager.Instance.OnMemberJoinedGuild += OnMemberJoinedGuild;
        GuildManager.Instance.OnMemberLeftGuild += OnMemberLeftGuild;
        GuildManager.Instance.OnMemberRoleChanged += OnMemberRoleChanged;
        GuildManager.Instance.OnGuildInfoUpdated += OnGuildInfoUpdated;

        // Run the demonstration scenario.
        RunExampleScenario();

        Debug.Log("--- Guild System Example End ---");
    }

    void OnDestroy()
    {
        // --- Unsubscribe from events to prevent memory leaks ---
        // Crucial when a GameObject that subscribed might be destroyed while the singleton persists.
        if (GuildManager.Instance != null)
        {
            GuildManager.Instance.OnGuildCreated -= OnGuildCreated;
            GuildManager.Instance.OnGuildDisbanded -= OnGuildDisbanded;
            GuildManager.Instance.OnMemberJoinedGuild -= OnMemberJoinedGuild;
            GuildManager.Instance.OnMemberLeftGuild -= OnMemberLeftGuild;
            GuildManager.Instance.OnMemberRoleChanged -= OnMemberRoleChanged;
            GuildManager.Instance.OnGuildInfoUpdated -= OnGuildInfoUpdated;
        }
    }

    /// <summary>
    /// This method outlines a series of guild operations to demonstrate the system's functionality.
    /// Follow the console output to understand the flow and how permissions are enforced.
    /// </summary>
    void RunExampleScenario()
    {
        // --- Scenario 1: Guild Creation and Joining ---
        Debug.Log("\n--- Scenario 1: Guild Creation and Joining ---");
        // Alice creates a new guild.
        Guild createdGuild = GuildManager.Instance.CreateGuild(myGuildName, "A guild for dedicated Unity developers.", PLAYER_ALICE);
        if (createdGuild != null)
        {
            myGuildId = createdGuild.Id; // Store guild ID for later operations.
            Debug.Log($"SUCCESS: Alice created guild '{myGuildName}'. Guild ID: {myGuildId}");
        }

        // Bob tries to create a guild with the same name (should fail).
        GuildManager.Instance.CreateGuild(myGuildName, "Another guild by Bob.", PLAYER_BOB); // EXPECT FAIL

        // Bob successfully creates a different guild.
        string bobGuildName = "Coder's Haven";
        Guild bobGuild = GuildManager.Instance.CreateGuild(bobGuildName, "A peaceful place for coders to collaborate.", PLAYER_BOB);
        string bobGuildId = bobGuild?.Id;
        if (bobGuild != null)
        {
            Debug.Log($"SUCCESS: Bob created guild '{bobGuildName}'. Guild ID: {bobGuildId}");
        }

        // Alice tries to join Bob's guild (should fail, she's already in 'Unity Devs Guild').
        GuildManager.Instance.AcceptInvite(PLAYER_ALICE, bobGuildId); // EXPECT FAIL

        // Alice (Leader) invites Charlie to her guild, then Charlie accepts.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) invites {PLAYER_CHARLIE} to {myGuildName} ---");
        GuildManager.Instance.InvitePlayer(myGuildId, PLAYER_ALICE, PLAYER_CHARLIE);
        GuildManager.Instance.AcceptInvite(PLAYER_CHARLIE, myGuildId);

        // Alice (Leader) invites Dave, then Dave accepts.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) invites {PLAYER_DAVE} to {myGuildName} ---");
        GuildManager.Instance.InvitePlayer(myGuildId, PLAYER_ALICE, PLAYER_DAVE);
        GuildManager.Instance.AcceptInvite(PLAYER_DAVE, myGuildId);

        // Display current members of Alice's guild.
        Guild unityDevs = GuildManager.Instance.GetGuild(myGuildId);
        Debug.Log($"\n--- Current Members of '{unityDevs?.Name}' ({unityDevs?.GetMemberCount()} members) ---");
        foreach (var member in unityDevs.Members.Values)
        {
            Debug.Log($"- {member.PlayerId} (Role: {member.Role}, Join Date: {member.JoinDate.ToShortDateString()})");
        }


        // --- Scenario 2: Role Changes and Permissions ---
        Debug.Log("\n--- Scenario 2: Role Changes and Permissions ---");

        // Alice (Leader) promotes Charlie to Officer.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) promotes {PLAYER_CHARLIE} to Officer ---");
        GuildManager.Instance.ChangeMemberRole(myGuildId, PLAYER_ALICE, PLAYER_CHARLIE, GuildRole.Officer);

        // Charlie (now Officer) tries to promote Dave to Leader (should fail, only leader can transfer leadership).
        Debug.Log($"\n--- {PLAYER_CHARLIE} (Officer) tries to promote {PLAYER_DAVE} to Leader ---");
        GuildManager.Instance.ChangeMemberRole(myGuildId, PLAYER_CHARLIE, PLAYER_DAVE, GuildRole.Leader); // EXPECT FAIL

        // Charlie (Officer) tries to kick Alice (Leader) (should fail, cannot kick higher rank).
        Debug.Log($"\n--- {PLAYER_CHARLIE} (Officer) tries to kick {PLAYER_ALICE} (Leader) ---");
        GuildManager.Instance.KickMember(myGuildId, PLAYER_CHARLIE, PLAYER_ALICE); // EXPECT FAIL

        // Charlie (Officer) kicks Dave (Recruit) (should succeed).
        Debug.Log($"\n--- {PLAYER_CHARLIE} (Officer) kicks {PLAYER_DAVE} (Recruit) ---");
        GuildManager.Instance.KickMember(myGuildId, PLAYER_CHARLIE, PLAYER_DAVE); // EXPECT SUCCESS

        // Dave (now no guild) tries to invite Eve to Alice's guild (should fail, not in guild).
        Debug.Log($"\n--- {PLAYER_DAVE} (no guild) tries to invite {PLAYER_EVE} to {myGuildName} ---");
        GuildManager.Instance.InvitePlayer(myGuildId, PLAYER_DAVE, PLAYER_EVE); // EXPECT FAIL

        // Alice (Leader) updates guild info.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) updates guild info ---");
        GuildManager.Instance.UpdateGuildInfo(myGuildId, PLAYER_ALICE, "Unity Master Devs Guild", "The ultimate guild for master Unity developers!");

        // Charlie (Officer) updates guild description (should succeed as Officers have ManageGuildInfo).
        Debug.Log($"\n--- {PLAYER_CHARLIE} (Officer) updates guild description ---");
        GuildManager.Instance.UpdateGuildInfo(myGuildId, PLAYER_CHARLIE, null, "The ultimate guild for master Unity developers, now with more features!");


        // --- Scenario 3: Leaving and Disbanding ---
        Debug.Log("\n--- Scenario 3: Leaving and Disbanding ---");

        // Charlie leaves the guild.
        Debug.Log($"\n--- {PLAYER_CHARLIE} (Officer) leaves {myGuildName} ---");
        GuildManager.Instance.LeaveGuild(PLAYER_CHARLIE);

        // Alice tries to leave the guild (should fail because she is the leader and there are other members).
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) tries to leave {myGuildName} ---");
        GuildManager.Instance.LeaveGuild(PLAYER_ALICE); // EXPECT FAIL

        // Invite Eve to the guild first so Alice can transfer leadership.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) invites {PLAYER_EVE} to {myGuildName} ---");
        GuildManager.Instance.InvitePlayer(myGuildId, PLAYER_ALICE, PLAYER_EVE);
        GuildManager.Instance.AcceptInvite(PLAYER_EVE, myGuildId);

        // Alice (current Leader) transfers leadership to Eve.
        Debug.Log($"\n--- {PLAYER_ALICE} (Leader) transfers leadership to {PLAYER_EVE} ---");
        GuildManager.Instance.TransferLeadership(myGuildId, PLAYER_ALICE, PLAYER_EVE);

        // Now Alice (demoted to Officer) leaves her guild.
        Debug.Log($"\n--- {PLAYER_ALICE} (now Officer) leaves {myGuildName} ---");
        GuildManager.Instance.LeaveGuild(PLAYER_ALICE);

        // Eve (new Leader) disbands the guild.
        Debug.Log($"\n--- {PLAYER_EVE} (Leader) disbands {myGuildName} ---");
        GuildManager.Instance.DisbandGuild(myGuildId, PLAYER_EVE);

        // Bob leaves his guild (since he's the only member and leader, the guild should disband automatically).
        Debug.Log($"\n--- {PLAYER_BOB} (Leader) leaves {bobGuildName} (guild will disband as he is the only member) ---");
        GuildManager.Instance.LeaveGuild(PLAYER_BOB);

        // Final check of all active guilds.
        Debug.Log("\n--- All guilds after scenario ---");
        if (GuildManager.Instance.GetAllGuilds().Any())
        {
            foreach (var guild in GuildManager.Instance.GetAllGuilds())
            {
                Debug.Log($"Active Guild: {guild.Name} (ID: {guild.Id}, Members: {guild.GetMemberCount()})");
            }
        }
        else
        {
            Debug.Log("No active guilds remaining.");
        }
    }

    // --- Event Handlers (for demonstration purposes, these would update UI or other game state) ---

    void OnGuildCreated(Guild guild)
    {
        Debug.Log($"[EVENT] Guild Created: '{guild.Name}' (ID: {guild.Id}) by '{guild.LeaderPlayerId}'.");
    }

    void OnGuildDisbanded(Guild guild)
    {
        Debug.Log($"[EVENT] Guild Disbanded: '{guild.Name}' (ID: {guild.Id}).");
    }

    void OnMemberJoinedGuild(string playerId, GuildMember member)
    {
        Debug.Log($"[EVENT] Player '{playerId}' joined guild '{member.GuildId}' as '{member.Role}'.");
    }

    void OnMemberLeftGuild(string playerId, string guildId)
    {
        Debug.Log($"[EVENT] Player '{playerId}' left guild '{guildId}'.");
    }

    void OnMemberRoleChanged(string playerId, GuildMember member)
    {
        Debug.Log($"[EVENT] Player '{playerId}' role in guild '{member.GuildId}' changed to '{member.Role}'.");
    }

    void OnGuildInfoUpdated(string guildId, string newName, string newDescription)
    {
        Debug.Log($"[EVENT] Guild '{guildId}' info updated. New Name: '{newName}', New Description: '{newDescription}'.");
    }
}
```