// Unity Design Pattern Example: SpectatorModeSystem
// This script demonstrates the SpectatorModeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Spectator Mode System** design pattern in Unity. This pattern involves a central system that passively observes the state of various entities within the game world without directly participating in or modifying their behavior. It then aggregates and presents this observed information, typically through a UI, for debugging, analytics, or a literal "spectator" view.

**Key Concepts of the Spectator Mode System Pattern:**

1.  **Passive Observation:** The system only *reads* data from other entities; it does not issue commands or change their state.
2.  **Centralized Monitoring:** It acts as a single point of collection for data from multiple, disparate sources.
3.  **Data Aggregation & Presentation:** It processes the raw data into a digestible format (e.g., a leaderboard, a debug overlay, a minimap).
4.  **Decoupling:** The observed entities do not need to know that they are being observed, or by whom. They simply expose their relevant state. The Spectator Mode System is responsible for finding and registering these entities.

---

### **Unity Project Setup Instructions:**

To make this example work in your Unity project:

1.  **Create a C# Script for `SpectatorModeSystem`:**
    *   In your Unity Project window, right-click -> Create -> C# Script.
    *   Name it `SpectatorModeSystem`.
    *   Replace its content with the `SpectatorModeSystem.cs` code block below.
2.  **Create a C# Script for `Player`:**
    *   Right-click -> Create -> C# Script.
    *   Name it `Player`.
    *   Replace its content with the `Player.cs` code block below.
3.  **Create the Spectator System GameObject:**
    *   In your Hierarchy window, right-click -> Create Empty.
    *   Rename it `SpectatorModeSystem`.
    *   Drag the `SpectatorModeSystem.cs` script onto this new GameObject in the Hierarchy.
4.  **Create a UI Text Element:**
    *   In your Hierarchy window, right-click -> UI -> Text (Legacy) or TextMeshPro - Text (if you have TextMeshPro imported).
    *   If using TextMeshPro, make sure to replace `UnityEngine.UI.Text` with `TMPro.TextMeshProUGUI` in `SpectatorModeSystem.cs` and add `using TMPro;`.
    *   Adjust the Text element's Rect Transform (size, position) to be visible on your screen. You might need to adjust the Canvas settings too.
    *   Rename this UI Text GameObject to something like `SpectatorDisplay`.
5.  **Assign the UI Text:**
    *   Select the `SpectatorModeSystem` GameObject in the Hierarchy.
    *   In the Inspector, you'll see a field called `Spectator Info Text`.
    *   Drag your `SpectatorDisplay` UI Text GameObject from the Hierarchy into this field.
6.  **Create Player GameObjects:**
    *   In your Hierarchy window, right-click -> 3D Object -> Cube (or any other GameObject).
    *   Rename it `Player1`.
    *   Drag the `Player.cs` script onto `Player1`.
    *   Duplicate `Player1` a few times (e.g., `Player2`, `Player3`).
    *   *Optional:* Select each Player GameObject and change its `Player Name` in the Inspector to differentiate them.
7.  **Run the Scene:**
    *   Press Play in Unity.
    *   You should now see the `SpectatorDisplay` UI element showing information about all active players (their names, health, and scores).
8.  **Test State Changes:**
    *   While in Play mode, select one of your `Player` GameObjects in the Hierarchy.
    *   In the Inspector, manually change its `Current Health` or `Score`.
    *   Observe how the `SpectatorDisplay` UI updates automatically (every `Refresh Interval` seconds, as set in the `SpectatorModeSystem` Inspector).
    *   You can also temporarily disable/enable a Player GameObject to see it register/deregister.

---

### 1. `SpectatorModeSystem.cs`

This script is the core of the Spectator Mode System. It's a `MonoBehaviour` singleton that manages the collection and display of information from observed entities.

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // For UI Text elements
using System.Text;   // For efficient string building

/// <summary>
/// This script demonstrates the Spectator Mode System design pattern.
/// It acts as a central observer that monitors the state of various
/// 'Spectatable' entities (e.g., Players) without directly interacting
/// or modifying them. It then presents this observed information,
/// typically via a UI, for a "spectator" or debug view.
/// </summary>
/// <remarks>
/// Key characteristics of the Spectator Mode System:
/// 1.  **Passive Observation:** It only reads data; it doesn't change it.
/// 2.  **Centralized Monitoring:** Gathers information from multiple sources.
/// 3.  **Data Aggregation/Presentation:** Processes and displays the observed data.
/// 4.  **Decoupling:** The observed entities don't need to know *how* or *if* they are being spectated.
///     They simply expose their data. The SpectatorModeSystem is responsible for finding and observing them.
///
/// In this example, the SpectatorModeSystem monitors multiple 'Player' entities
/// and displays their names, health, and scores on a UI Text element.
/// </remarks>
public class SpectatorModeSystem : MonoBehaviour
{
    // Singleton instance for easy global access from other scripts.
    // This ensures there's only one SpectatorModeSystem managing observations.
    public static SpectatorModeSystem Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Assign a UI Text (or TextMeshProUGUI if using TMPro) element here to display spectator information.")]
    [SerializeField]
    private Text spectatorInfoText; // Use TextMeshProUGUI if using TMPro and add 'using TMPro;'

    [Header("Spectator Settings")]
    [Tooltip("How often, in seconds, the spectator display should refresh.")]
    [SerializeField]
    private float refreshInterval = 0.5f;

    // A list to hold all entities that want to be observed.
    // They must implement the ISpectatable interface.
    private List<ISpectatable> spectatableEntities = new List<ISpectatable>();
    
    // Timer to control the refresh rate of the UI display.
    private float timeSinceLastRefresh = 0f;

    /// <summary>
    /// Ensures only one instance of the SpectatorModeSystem exists (Singleton pattern)
    /// and initializes it. Also performs initial setup checks.
    /// </summary>
    private void Awake()
    {
        // Enforce singleton pattern: if another instance exists, destroy this one.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SpectatorModeSystem: Found multiple instances, destroying this one. Only one SpectatorModeSystem should exist per scene.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optionally, uncomment the line below if you want the spectator system
            // to persist across scene loads (e.g., for a global debug overlay).
            // DontDestroyOnLoad(gameObject); 
        }

        // Check if the UI Text component is assigned.
        if (spectatorInfoText == null)
        {
            Debug.LogError("SpectatorModeSystem: 'Spectator Info Text' is not assigned! Please assign a UI Text component in the Inspector.");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Good place to ensure updates start running and perform an initial display refresh.
    /// </summary>
    private void OnEnable()
    {
        RefreshSpectatorDisplay();
    }

    /// <summary>
    /// Update is called once per frame.
    /// Used here to periodically refresh the displayed information based on `refreshInterval`.
    /// </summary>
    private void Update()
    {
        timeSinceLastRefresh += Time.deltaTime;
        if (timeSinceLastRefresh >= refreshInterval)
        {
            RefreshSpectatorDisplay();
            timeSinceLastRefresh = 0f;
        }
    }

    /// <summary>
    /// Registers an entity to be observed by the SpectatorModeSystem.
    /// Entities must implement the <see cref="ISpectatable"/> interface to provide their data.
    /// </summary>
    /// <param name="entity">The ISpectatable entity to register.</param>
    public void RegisterSpectatable(ISpectatable entity)
    {
        if (entity == null)
        {
            Debug.LogWarning("SpectatorModeSystem: Attempted to register a null ISpectatable entity.");
            return;
        }

        if (!spectatableEntities.Contains(entity))
        {
            spectatableEntities.Add(entity);
            Debug.Log($"SpectatorModeSystem: Registered entity: {entity.SpectatorName}");
            // Refresh display immediately when a new entity is registered to show it right away.
            RefreshSpectatorDisplay();
        }
        else
        {
            Debug.LogWarning($"SpectatorModeSystem: Entity '{entity.SpectatorName}' is already registered.");
        }
    }

    /// <summary>
    /// Deregisters an entity, removing it from the observation list.
    /// This should be called when an entity is destroyed or no longer needs to be spectated.
    /// </summary>
    /// <param name="entity">The ISpectatable entity to deregister.</param>
    public void DeregisterSpectatable(ISpectatable entity)
    {
        if (entity == null)
        {
            // This can happen if the entity was destroyed before it could deregister itself
            // or if it was never properly registered in the first place.
            // It's a benign warning if the entity was already destroyed.
            Debug.LogWarning("SpectatorModeSystem: Attempted to deregister a null ISpectatable entity.");
            return;
        }

        if (spectatableEntities.Remove(entity))
        {
            Debug.Log($"SpectatorModeSystem: Deregistered entity: {entity.SpectatorName}");
            // Refresh display immediately when an entity is removed.
            RefreshSpectatorDisplay();
        }
        else
        {
            Debug.LogWarning($"SpectatorModeSystem: Entity '{entity.SpectatorName}' was not found in the registered list. Already deregistered or never registered?");
        }
    }

    /// <summary>
    /// Gathers information from all registered spectatable entities and updates the UI display.
    /// This is the core logic for aggregating and presenting observed data.
    /// </summary>
    private void RefreshSpectatorDisplay()
    {
        if (spectatorInfoText == null)
        {
            return; // Cannot update if UI text is not assigned.
        }

        // Use StringBuilder for efficient string concatenation, especially when updating frequently.
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("--- SPECTATOR OVERVIEW ---");
        sb.AppendLine($"Active Entities: {spectatableEntities.Count}");
        sb.AppendLine("--------------------------");

        if (spectatableEntities.Count == 0)
        {
            sb.AppendLine("No spectatable entities currently active.");
        }
        else
        {
            // Iterate through all registered entities and get their spectator info.
            foreach (ISpectatable entity in spectatableEntities)
            {
                // Each entity provides its own spectator display string via the interface.
                sb.AppendLine(entity.GetSpectatorDisplayInfo());
            }
        }

        spectatorInfoText.text = sb.ToString();
    }
}

/// <summary>
/// Interface for entities that can be observed by the SpectatorModeSystem.
/// This interface is crucial for decoupling: the SpectatorModeSystem doesn't need
/// to know the concrete type of an entity, only that it can provide spectatable data.
/// </summary>
public interface ISpectatable
{
    /// <summary>
    /// A unique or descriptive name for the spectator system to identify this entity.
    /// </summary>
    string SpectatorName { get; }

    /// <summary>
    /// Returns a formatted string containing relevant information for the spectator display.
    /// This allows each entity to decide what specific information it exposes
    /// and how that information is formatted.
    /// </summary>
    /// <returns>A string representation of the entity's state for spectator mode.</returns>
    string GetSpectatorDisplayInfo();
}
```

---

### 2. `Player.cs`

This script represents an entity (`Player`) that exposes its state to the `SpectatorModeSystem` by implementing the `ISpectatable` interface. It handles its own lifecycle (health, score, status) and registers/deregisters itself with the system.

```csharp
using UnityEngine;
using System.Text; // For StringBuilder (optional, but good practice for frequent string manipulation)

/// <summary>
/// An example of an entity that can be observed by the SpectatorModeSystem.
/// This script implements the <see cref="ISpectatable"/> interface to expose its state.
/// </summary>
/// <remarks>
/// This script simulates a player with health, score, and a status.
/// It automatically registers itself with the SpectatorModeSystem upon activation
/// and deregisters upon deactivation or destruction, ensuring it's always tracked
/// correctly while active in the scene.
/// </remarks>
public class Player : MonoBehaviour, ISpectatable
{
    [Header("Player Settings")]
    [Tooltip("The unique name of this player for identification.")]
    [SerializeField]
    private string playerName = "Player";

    [Tooltip("Initial and maximum health of the player.")]
    [SerializeField]
    private int maxHealth = 100;

    [Tooltip("Current health of the player.")]
    [SerializeField]
    private int currentHealth;

    [Tooltip("Current score of the player.")]
    [SerializeField]
    private int score;

    public enum PlayerStatus { Alive, Dead, Active, Inactive }
    [Tooltip("Current status of the player.")]
    [SerializeField]
    private PlayerStatus status;

    // Public properties for external systems to read (though SpectatorSystem uses the interface).
    // These are good practice for read-only access to internal state.
    public string PlayerName => playerName;
    public int CurrentHealth => currentHealth;
    public int Score => score;
    public PlayerStatus Status => status;

    // --- ISpectatable Interface Implementation ---
    /// <summary>
    /// Implements <see cref="ISpectatable.SpectatorName"/>.
    /// Provides the name of this player for the spectator system.
    /// </summary>
    public string SpectatorName => playerName;

    /// <summary>
    /// Implements <see cref="ISpectatable.GetSpectatorDisplayInfo"/>.
    /// Returns a formatted string of the player's current state for the spectator display.
    /// This allows each entity to define its own display format.
    /// </summary>
    /// <returns>A string with player name, health, score, and status.</returns>
    public string GetSpectatorDisplayInfo()
    {
        // Using StringBuilder is generally more performant for complex or frequent string building
        // than repeated string concatenation with '+'.
        StringBuilder sb = new StringBuilder();
        sb.Append($"  {playerName,-12}"); // Pad name for alignment
        sb.Append($"HP: {currentHealth,3}/{maxHealth,-3} "); // Pad health values
        sb.Append($"Score: {score,-5} "); // Pad score
        sb.Append($"Status: {status}");
        return sb.ToString();
    }

    // --- MonoBehaviour Lifecycle ---
    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes player health and status.
    /// </summary>
    private void Awake()
    {
        currentHealth = maxHealth;
        status = PlayerStatus.Active;
        // Ensure player name is unique if not set in inspector.
        // Good for differentiating players if multiple instances are created without unique names.
        if (string.IsNullOrEmpty(playerName) || playerName == "Player")
        {
            playerName = "Player_" + GetInstanceID();
        }
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// This is where the player registers itself with the SpectatorModeSystem.
    /// </summary>
    private void OnEnable()
    {
        if (SpectatorModeSystem.Instance != null)
        {
            SpectatorModeSystem.Instance.RegisterSpectatable(this);
        }
        else
        {
            Debug.LogWarning($"Player '{playerName}': SpectatorModeSystem.Instance not found. Cannot register.");
        }
    }

    /// <summary>
    /// Called when the behaviour becomes disabled or inactive.
    /// This is where the player deregisters itself from the SpectatorModeSystem.
    /// This is important for preventing NullReferenceExceptions if the SpectatorModeSystem
    /// tries to access a destroyed or inactive player.
    /// </summary>
    private void OnDisable()
    {
        // Important: Check if SpectatorModeSystem.Instance is not null AND not a destroyed object.
        // The `!Equals(null)` check handles cases where the static instance reference points
        // to a GameObject that has already been destroyed (e.g., during scene unload).
        if (SpectatorModeSystem.Instance != null && !SpectatorModeSystem.Instance.Equals(null))
        {
            SpectatorModeSystem.Instance.DeregisterSpectatable(this);
        }
    }

    /// <summary>
    /// Called when the GameObject is being destroyed.
    /// Ensures deregistration even if OnDisable wasn't called (e.g., immediate destruction).
    /// </summary>
    private void OnDestroy()
    {
        if (SpectatorModeSystem.Instance != null && !SpectatorModeSystem.Instance.Equals(null))
        {
            SpectatorModeSystem.Instance.DeregisterSpectatable(this);
        }
    }

    // --- Example Player Actions (to demonstrate observable state changes) ---

    /// <summary>
    /// Simulates this player taking damage.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        if (status == PlayerStatus.Dead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth); // Health cannot go below 0.
        Debug.Log($"{playerName} took {amount} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        // The SpectatorModeSystem will automatically pick up this change on its next refresh cycle.
    }

    /// <summary>
    /// Simulates this player healing.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        if (status == PlayerStatus.Dead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Health cannot exceed max.
        Debug.Log($"{playerName} healed for {amount}. Health: {currentHealth}");
    }

    /// <summary>
    /// Simulates this player gaining score.
    /// </summary>
    /// <param name="points">The points to add.</param>
    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"{playerName} gained {points} points. Total Score: {score}");
    }

    /// <summary>
    /// Sets the player's status to Dead.
    /// </summary>
    public void Die()
    {
        currentHealth = 0;
        status = PlayerStatus.Dead;
        Debug.Log($"{playerName} has died!");
        // The SpectatorModeSystem will update this status.
    }

    /// <summary>
    /// Respawns the player, restoring health and setting status to Active.
    /// </summary>
    public void Respawn()
    {
        currentHealth = maxHealth;
        score = 0; // Or keep existing score based on game design.
        status = PlayerStatus.Active;
        Debug.Log($"{playerName} has respawned!");
    }
}
```