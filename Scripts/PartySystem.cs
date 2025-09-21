// Unity Design Pattern Example: PartySystem
// This script demonstrates the PartySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'PartySystem' design pattern centralizes the management of a group of characters or entities, often called a "party." This pattern is commonly found in RPGs, strategy games, or any game where multiple units act as a cohesive group. It encapsulates the logic for adding, removing, and performing actions on all members of the party, ensuring consistent behavior and simplifying interactions from other parts of the game.

**Benefits of the PartySystem Pattern:**

1.  **Centralized Management:** All party-related logic (adding, removing, iterating, group actions) is in one place, making it easy to understand and modify.
2.  **Decoupling:** Game logic (e.g., UI, combat system) doesn't need to know the specifics of *how* the party members are stored or managed; they just interact with the `PartySystem`.
3.  **Consistency:** Ensures that all operations on the party (like healing all members, applying buffs, or checking if the party is defeated) are handled uniformly.
4.  **Reusability:** The system can be easily reused across different parts of the game or even different projects.
5.  **Maintainability:** Changes to party mechanics only need to be made in the `PartySystem`, reducing the risk of introducing bugs elsewhere.

---

### PartySystem Example for Unity

This example will demonstrate:
1.  A `PartyMemberData` class to represent an individual character's stats.
2.  A `PartySystem` (Singleton `MonoBehaviour`) to manage a collection of `PartyMemberData` instances.
3.  Methods for adding, removing, and interacting with party members.
4.  Unity Events (`Action`) for notifying other systems about changes.
5.  Example usage within the `PartySystem` itself and comments showing how other scripts would interact.

**How to Use This Example:**

1.  Create an empty GameObject in your Unity scene (e.g., named `GameManager` or `PartyManager`).
2.  Attach the `PartySystem` script to this GameObject.
3.  Run the scene. The `Awake` method of `PartySystem` will initialize the singleton instance, and the `Start` method will run a demo of adding members and performing actions.
4.  Observe the `Debug.Log` output in the Unity Console.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic; // For List<T>
using System.Linq; // For LINQ operations like .Where(), .Any(), .All()

// PartyMemberData: Represents an individual character's data within the party.
// This is a plain C# class, not a MonoBehaviour, to keep character data
// independent of specific GameObjects, allowing flexibility.
[System.Serializable] // Allows instances of this class to be serialized in the Inspector if part of a MonoBehaviour or ScriptableObject.
public class PartyMemberData
{
    // Unique identifier for the party member. Useful for saving/loading or referencing.
    public string Id { get; private set; }
    public string Name { get; private set; }
    public int MaxHealth { get; private set; }
    private int _currentHealth;
    public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth); // Ensure health stays within bounds
            if (_currentHealth != oldHealth)
            {
                // Notify the PartySystem (or any listener) that this member's health changed.
                // This is crucial for UI updates, game logic, etc.
                PartySystem.Instance?.OnMemberHealthChanged?.Invoke(this, oldHealth, _currentHealth);

                if (_currentHealth <= 0 && oldHealth > 0)
                {
                    PartySystem.Instance?.OnMemberDied?.Invoke(this);
                }
                else if (_currentHealth > 0 && oldHealth <= 0)
                {
                    PartySystem.Instance?.OnMemberRevived?.Invoke(this);
                }
            }
        }
    }
    public bool IsAlive => CurrentHealth > 0;
    public int Level { get; private set; }
    public int Experience { get; private set; }

    // Constructor to initialize a new party member.
    public PartyMemberData(string name, int maxHealth, int level = 1, int experience = 0, string id = "")
    {
        Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id; // Generate a unique ID if not provided
        Name = name;
        MaxHealth = maxHealth;
        _currentHealth = maxHealth; // Start with full health
        Level = level;
        Experience = experience;
    }

    // --- Member-specific Actions ---
    public void TakeDamage(int amount)
    {
        if (amount < 0) return;
        Debug.Log($"{Name} takes {amount} damage.");
        CurrentHealth -= amount;
        Debug.Log($"{Name} health: {CurrentHealth}/{MaxHealth}. IsAlive: {IsAlive}");
    }

    public void Heal(int amount)
    {
        if (amount < 0) return;
        Debug.Log($"{Name} is healed by {amount}.");
        CurrentHealth += amount;
        Debug.Log($"{Name} health: {CurrentHealth}/{MaxHealth}.");
    }

    public void GainExperience(int amount)
    {
        if (amount < 0) return;
        Experience += amount;
        // Add level-up logic here if needed
        Debug.Log($"{Name} gained {amount} experience. Total: {Experience}");
    }

    public override string ToString()
    {
        return $"{Name} (Lv.{Level}) - HP: {CurrentHealth}/{MaxHealth} ({(IsAlive ? "Alive" : "Defeated")})";
    }
}

// PartySystem: A Singleton MonoBehaviour responsible for managing all party members.
// This centralizes party logic, allowing other systems to interact with the party
// without needing to know its internal structure.
public class PartySystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides a globally accessible single instance of PartySystem.
    public static PartySystem Instance { get; private set; }

    [Header("Party Settings")]
    [Tooltip("The maximum number of members allowed in the party.")]
    [SerializeField] private int _maxPartySize = 4;
    public int MaxPartySize => _maxPartySize;

    // The internal list of party members.
    // Kept private to enforce management through public methods.
    private readonly List<PartyMemberData> _members = new List<PartyMemberData>();

    // Public read-only access to the party members.
    // Use IReadOnlyList to prevent external modification of the list itself.
    public IReadOnlyList<PartyMemberData> Members => _members;

    // --- Events for Notifying Other Systems ---
    // These Actions allow other scripts (e.g., UI Manager, Combat Manager)
    // to subscribe and react to changes in the party without direct coupling.
    public event Action<PartyMemberData> OnMemberJoined;
    public event Action<PartyMemberData> OnMemberLeft;
    public event Action<PartyMemberData, int, int> OnMemberHealthChanged; // Member, OldHealth, NewHealth
    public event Action<PartyMemberData> OnMemberDied;
    public event Action<PartyMemberData> OnMemberRevived;
    public event Action OnPartyDefeated; // When all members are down
    public event Action OnPartyActive; // When at least one member is alive again after defeat

    // --- MonoBehaviour Lifecycle ---
    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PartySystem instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, make this object persist across scene loads.
            // DontDestroyOnLoad(gameObject);
            Debug.Log("PartySystem initialized.");
        }
    }

    private void Start()
    {
        // --- DEMO USAGE ---
        Debug.Log("\n--- PartySystem Demo Start ---");

        // 1. Add some initial members
        Debug.Log("Adding initial party members:");
        AddMember(new PartyMemberData("Heroic Adventurer", 100, 5));
        AddMember(new PartyMemberData("Wise Mage", 70, 4));
        AddMember(new PartyMemberData("Stalwart Knight", 120, 6));

        Debug.Log("\nCurrent Party Status:");
        PrintPartyStatus();

        // 2. Demonstrate party actions
        Debug.Log("\n--- Demonstrating Party Actions ---");
        Debug.Log("Healing all members...");
        HealAllMembers(20);
        PrintPartyStatus();

        Debug.Log("\nDamaging all members...");
        DamageAllMembers(30);
        PrintPartyStatus();

        Debug.Log("\nAttempting to add another member (should fail if max size reached):");
        AddMember(new PartyMemberData("Sneaky Rogue", 80, 3)); // This should fail if _maxPartySize is 3

        Debug.Log("\n--- Simulating Combat and Defeat ---");
        // Get the first member and have them take a lot of damage
        if (Members.Any())
        {
            PartyMemberData firstMember = Members[0];
            Debug.Log($"\n{firstMember.Name} takes a massive hit!");
            firstMember.TakeDamage(150); // Should defeat them
        }

        Debug.Log("\nMassive AoE attack on party!");
        DamageAllMembers(50); // Should defeat the remaining ones

        PrintPartyStatus();

        Debug.Log("\n--- Checking Party Status ---");
        Debug.Log($"Are all members alive? {AreAllMembersAlive()}");
        Debug.Log($"Is the party defeated? {IsPartyDefeated()}");

        Debug.Log("\n--- Reviving a member ---");
        PartyMemberData deadMember = _members.FirstOrDefault(m => !m.IsAlive);
        if (deadMember != null)
        {
            Debug.Log($"Reviving {deadMember.Name}...");
            deadMember.Heal(50); // Bring them back to life
            PrintPartyStatus();
            Debug.Log($"Is the party defeated now? {IsPartyDefeated()}");
        }

        // 3. Remove a member
        if (_members.Count > 1)
        {
            PartyMemberData memberToRemove = _members[1];
            Debug.Log($"\nRemoving {memberToRemove.Name} from the party.");
            RemoveMember(memberToRemove);
            PrintPartyStatus();
        }

        Debug.Log("\n--- PartySystem Demo End ---");
    }

    // --- Public Party Management Methods ---

    /// <summary>
    /// Adds a new member to the party.
    /// </summary>
    /// <param name="member">The PartyMemberData object to add.</param>
    /// <returns>True if the member was added successfully, false otherwise (e.g., party full).</returns>
    public bool AddMember(PartyMemberData member)
    {
        if (member == null)
        {
            Debug.LogError("Attempted to add a null party member.");
            return false;
        }

        if (_members.Count >= _maxPartySize)
        {
            Debug.LogWarning($"Cannot add {member.Name}: Party is full (Max: {_maxPartySize}).");
            return false;
        }

        if (_members.Any(m => m.Id == member.Id))
        {
            Debug.LogWarning($"Member with ID '{member.Id}' (Name: {member.Name}) is already in the party.");
            return false;
        }

        _members.Add(member);
        Debug.Log($"Added {member.Name} to the party.");
        OnMemberJoined?.Invoke(member); // Invoke event
        CheckPartyStatusAfterChange();
        return true;
    }

    /// <summary>
    /// Removes a member from the party.
    /// </summary>
    /// <param name="member">The PartyMemberData object to remove.</param>
    /// <returns>True if the member was removed successfully, false if not found.</returns>
    public bool RemoveMember(PartyMemberData member)
    {
        if (member == null)
        {
            Debug.LogError("Attempted to remove a null party member.");
            return false;
        }

        if (_members.Remove(member))
        {
            Debug.Log($"Removed {member.Name} from the party.");
            OnMemberLeft?.Invoke(member); // Invoke event
            CheckPartyStatusAfterChange();
            return true;
        }
        Debug.LogWarning($"Could not find {member.Name} in the party to remove.");
        return false;
    }

    /// <summary>
    /// Removes a member from the party by their unique ID.
    /// </summary>
    /// <param name="memberId">The ID of the member to remove.</param>
    /// <returns>True if the member was removed successfully, false if not found.</returns>
    public bool RemoveMemberById(string memberId)
    {
        PartyMemberData memberToRemove = _members.FirstOrDefault(m => m.Id == memberId);
        if (memberToRemove != null)
        {
            return RemoveMember(memberToRemove);
        }
        Debug.LogWarning($"Could not find member with ID '{memberId}' in the party to remove.");
        return false;
    }

    /// <summary>
    /// Gets a specific party member by their unique ID.
    /// </summary>
    /// <param name="memberId">The ID of the member to retrieve.</param>
    /// <returns>The PartyMemberData object if found, otherwise null.</returns>
    public PartyMemberData GetMemberById(string memberId)
    {
        return _members.FirstOrDefault(m => m.Id == memberId);
    }

    // --- Public Party-wide Actions ---

    /// <summary>
    /// Applies damage to all living members of the party.
    /// </summary>
    /// <param name="amount">The amount of damage to deal.</param>
    public void DamageAllMembers(int amount)
    {
        if (amount < 0) return;
        foreach (PartyMemberData member in _members.Where(m => m.IsAlive))
        {
            member.TakeDamage(amount);
        }
        CheckPartyStatusAfterChange();
    }

    /// <summary>
    /// Heals all members of the party.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void HealAllMembers(int amount)
    {
        if (amount < 0) return;
        foreach (PartyMemberData member in _members)
        {
            member.Heal(amount);
        }
        CheckPartyStatusAfterChange();
    }

    /// <summary>
    /// Grants experience to all members of the party.
    /// </summary>
    /// <param name="amount">The amount of experience to grant.</param>
    public void GrantExperienceToAllMembers(int amount)
    {
        if (amount < 0) return;
        foreach (PartyMemberData member in _members)
        {
            member.GainExperience(amount);
        }
    }

    // --- Public Party Status Checks ---

    /// <summary>
    /// Checks if all members in the party are currently alive.
    /// </summary>
    /// <returns>True if all members are alive, false otherwise.</returns>
    public bool AreAllMembersAlive()
    {
        return _members.Any() && _members.All(member => member.IsAlive);
    }

    /// <summary>
    /// Checks if the entire party is defeated (i.e., all members are not alive).
    /// </summary>
    /// <returns>True if all members are defeated, false otherwise.</returns>
    public bool IsPartyDefeated()
    {
        return _members.Any() && _members.All(member => !member.IsAlive);
    }

    /// <summary>
    /// Checks if there is at least one living member in the party.
    /// </summary>
    /// <returns>True if at least one member is alive, false otherwise (e.g., empty party or all defeated).</returns>
    public bool HasLivingMembers()
    {
        return _members.Any(member => member.IsAlive);
    }

    // --- Internal Helper for Event Dispatching ---

    private bool _wasPartyDefeatedLastCheck = false; // To track state changes

    private void CheckPartyStatusAfterChange()
    {
        bool currentPartyDefeated = IsPartyDefeated();

        if (currentPartyDefeated && !_wasPartyDefeatedLastCheck)
        {
            OnPartyDefeated?.Invoke();
            Debug.LogWarning("!!! PARTY DEFEATED !!!");
        }
        else if (!currentPartyDefeated && _wasPartyDefeatedLastCheck)
        {
            OnPartyActive?.Invoke(); // Party is no longer defeated
            Debug.Log("Party is no longer defeated (at least one member revived/joined).");
        }
        _wasPartyDefeatedLastCheck = currentPartyDefeated;
    }


    // --- Demo / Debugging Helper ---
    private void PrintPartyStatus()
    {
        if (_members.Count == 0)
        {
            Debug.Log("Party is empty.");
            return;
        }

        Debug.Log($"Party Members ({_members.Count}/{_maxPartySize}):");
        foreach (PartyMemberData member in _members)
        {
            Debug.Log($"  - {member}");
        }
    }

    // --- Example of how another script would listen to events ---
    /*
    // Example: UI Manager might subscribe to update health bars
    public class UIManager : MonoBehaviour
    {
        private void OnEnable()
        {
            if (PartySystem.Instance != null)
            {
                PartySystem.Instance.OnMemberHealthChanged += UpdateHealthBarUI;
                PartySystem.Instance.OnMemberDied += ShowDefeatedMessage;
                PartySystem.Instance.OnMemberJoined += AddMemberToUI;
                PartySystem.Instance.OnPartyDefeated += ShowGameOverScreen;
            }
        }

        private void OnDisable()
        {
            if (PartySystem.Instance != null)
            {
                PartySystem.Instance.OnMemberHealthChanged -= UpdateHealthBarUI;
                PartySystem.Instance.OnMemberDied -= ShowDefeatedMessage;
                PartySystem.Instance.OnMemberJoined -= AddMemberToUI;
                PartySystem.Instance.OnPartyDefeated -= ShowGameOverScreen;
            }
        }

        private void UpdateHealthBarUI(PartyMemberData member, int oldHealth, int newHealth)
        {
            Debug.Log($"UI: {member.Name}'s health changed from {oldHealth} to {newHealth}. Update UI bar for {member.Name}.");
            // Find and update the health bar for 'member'
        }

        private void ShowDefeatedMessage(PartyMemberData member)
        {
            Debug.Log($"UI: Display '{member.Name} has been defeated!' message.");
        }

        private void AddMemberToUI(PartyMemberData member)
        {
            Debug.Log($"UI: Add {member.Name} to party roster display.");
        }

        private void ShowGameOverScreen()
        {
            Debug.Log("UI: Display Game Over screen!");
            // Load Game Over scene, show retry options, etc.
        }
    }

    // Example: Combat Manager might check party status
    public class CombatManager : MonoBehaviour
    {
        private void Update()
        {
            // In a real game, this might be triggered by events or a combat phase
            if (PartySystem.Instance != null && PartySystem.Instance.IsPartyDefeated())
            {
                // Trigger game over logic, end combat round, etc.
                Debug.Log("CombatManager: Party is defeated. Ending combat.");
                // Potentially unsubscribe from events to prevent further calls
                // enabled = false;
            }
        }

        private void OnEnable()
        {
            if (PartySystem.Instance != null)
            {
                PartySystem.Instance.OnPartyDefeated += HandlePartyDefeatedInCombat;
            }
        }

        private void OnDisable()
        {
            if (PartySystem.Instance != null)
            {
                PartySystem.Instance.OnPartyDefeated -= HandlePartyDefeatedInCombat;
            }
        }

        private void HandlePartyDefeatedInCombat()
        {
            Debug.Log("CombatManager received PartyDefeated event! Triggering combat end sequence.");
            // Stop enemy AI, show defeat animation, etc.
        }
    }
    */
}
```