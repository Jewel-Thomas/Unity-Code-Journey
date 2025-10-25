// Unity Design Pattern Example: TurnBasedCombatSystem
// This script demonstrates the TurnBasedCombatSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **Turn-Based Combat System** design pattern. It provides a complete, working framework for managing combat between multiple participants (players and enemies) in a turn-based fashion.

The core idea is to have a central `CombatManager` that orchestrates the battle flow, while individual `Combatant` entities define their behavior for their turn.

**Key Components of the Pattern:**

1.  **`Combatant` (Abstract Base Class):** Defines the common properties (HP, Attack, Name) and actions (`TakeDamage`, `Heal`, `Attack`) for any entity that can participate in combat. It includes an abstract `PerformTurn` method that concrete combatants must implement.
2.  **`PlayerCombatant` (Concrete Combatant):** Extends `Combatant` and implements turn logic that waits for player input.
3.  **`EnemyCombatant` (Concrete Combatant):** Extends `Combatant` and implements simple AI for its turn logic (e.g., attack a random player).
4.  **`CombatManager` (The Orchestrator):** The central hub of the system. It manages:
    *   The list of all combatants.
    *   The current state of the battle (`BattleState` enum).
    *   Turn order and progression.
    *   Processing of actions (including handling player input).
    *   Checking for battle over conditions.
    *   Provides logging for battle events.

This setup ensures that the core battle logic is centralized in the `CombatManager`, while the specific behaviors of each combatant are encapsulated within their respective classes, making the system flexible and extensible.

---

**Complete C# Unity Script (`TurnBasedCombatSystem.cs`):**

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For ordering and filtering collections

// --- 1. Combatant Base Class ---
/// <summary>
/// Abstract base class for any entity participating in combat (Player or Enemy).
/// Implements common properties and methods like health, damage, and turn execution.
/// This acts as the 'Participant' or 'Unit' in the Turn-Based Combat System pattern.
/// </summary>
public abstract class Combatant : MonoBehaviour
{
    [Header("Combatant Stats")]
    [SerializeField] private string combatantName = "Combatant";
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int attackPower = 10;

    private int currentHP;

    public string Name => combatantName;
    public int MaxHP => maxHP;
    public int AttackPower => attackPower;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    /// <summary>
    /// Abstract property to distinguish between player and enemy combatants.
    /// Used by the CombatManager to determine turn logic and targeting.
    /// </summary>
    public abstract bool IsPlayer { get; }

    protected virtual void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// Reduces the combatant's current health by a specified amount.
    /// Triggers a death message if HP drops to zero or below.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public virtual void TakeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP); // Ensure HP doesn't go below zero
        CombatManager.Instance.LogMessage($"{Name} took {amount} damage. HP: {CurrentHP}/{MaxHP}");

        if (IsDead)
        {
            CombatManager.Instance.LogMessage($"{Name} has been defeated!");
            // In a real game, you would trigger death animations, sound effects,
            // or visual effects here. For this example, we simply deactivate.
            gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Increases the combatant's current health by a specified amount, up to MaxHP.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    public virtual void Heal(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(maxHP, currentHP); // Ensure HP doesn't exceed maxHP
        CombatManager.Instance.LogMessage($"{Name} healed for {amount} HP. HP: {CurrentHP}/{MaxHP}");
    }

    /// <summary>
    /// Executes an attack on a target combatant.
    /// This is a common action available to all combatants.
    /// </summary>
    /// <param name="target">The combatant to attack.</param>
    public virtual void Attack(Combatant target)
    {
        if (target == null || target.IsDead)
        {
            CombatManager.Instance.LogMessage($"{Name} tried to attack a dead or non-existent target.");
            return;
        }

        CombatManager.Instance.LogMessage($"{Name} attacks {target.Name} for {AttackPower} damage!");
        target.TakeDamage(AttackPower);
    }

    /// <summary>
    /// Abstract method to define the specific logic for a combatant's turn.
    /// This method will be implemented by concrete classes (PlayerCombatant, EnemyCombatant).
    /// It returns an IEnumerator to allow for asynchronous operations (e.g., waiting for input, animations, delays),
    /// which is crucial for turn-based systems in Unity using Coroutines.
    /// </summary>
    /// <param name="manager">Reference to the CombatManager to interact with the battle system.</param>
    /// <returns>An IEnumerator for a Coroutine that represents the combatant's turn.</returns>
    public abstract IEnumerator PerformTurn(CombatManager manager);
}

// --- 2. Player Combatant Concrete Class ---
/// <summary>
/// Represents a player-controlled combatant.
/// Its turn logic involves waiting for player input via the CombatManager.
/// </summary>
public class PlayerCombatant : Combatant
{
    public override bool IsPlayer => true;

    /// <summary>
    /// Player's turn implementation: Prompts the player for action and waits for input.
    /// The actual action execution will happen after input is received by the CombatManager.
    /// </summary>
    public override IEnumerator PerformTurn(CombatManager manager)
    {
        manager.LogMessage($"--- {Name}'s Turn (Player) ---");
        manager.LogMessage($"Please select a target to attack for {Name}.");

        // The CombatManager will yield until the player makes a selection
        // This method effectively pauses the PlayerCombatant's turn until player input is processed.
        yield return manager.GetPlayerAction(this);

        // After the player action is processed, the CombatManager will resume the turn progression.
    }
}

// --- 3. Enemy Combatant Concrete Class ---
/// <summary>
/// Represents an AI-controlled enemy combatant.
/// Its turn logic involves simple AI to select a target and attack.
/// </summary>
public class EnemyCombatant : Combatant
{
    public override bool IsPlayer => false;

    /// <summary>
    /// Enemy's turn implementation: Implements simple AI to attack a random living player.
    /// </summary>
    public override IEnumerator PerformTurn(CombatManager manager)
    {
        manager.LogMessage($"--- {Name}'s Turn (Enemy) ---");

        // Simple AI: Find a random living player to attack
        List<Combatant> livingPlayers = manager.GetLivingPlayers();
        if (livingPlayers.Count > 0)
        {
            Combatant target = livingPlayers[Random.Range(0, livingPlayers.Count)];
            Attack(target); // Perform the attack using the base Combatant's Attack method
        }
        else
        {
            manager.LogMessage($"{Name} couldn't find any players to attack!");
        }

        // Simulate action time with a delay to make it visually discernible
        yield return new WaitForSeconds(manager.ActionDelay);
    }
}

// --- 4. CombatManager (The Core Turn-Based Combat System) ---
/// <summary>
/// The central orchestrator for the turn-based combat system.
/// This class embodies the 'TurnBasedCombatSystem' pattern.
/// It manages combatants, turn order, battle states, and action execution.
/// </summary>
public class CombatManager : MonoBehaviour
{
    // Singleton pattern for easy access from other scripts (e.g., UI, Combatants).
    // In a larger game, consider a more robust service locator or dependency injection.
    public static CombatManager Instance { get; private set; }

    /// <summary>
    /// Defines the various states of the battle.
    /// A simple state machine helps manage the flow of turns and actions.
    /// </summary>
    public enum BattleState
    {
        WaitingToStart,     // Initial state before battle begins
        PlayerTurn,         // Current turn belongs to a player combatant
        EnemyTurn,          // Current turn belongs to an enemy combatant
        ProcessingAction,   // An action (player or enemy) is currently being executed/animated
        BattleOver          // Battle has concluded (win/loss)
    }

    [Header("Combat Settings")]
    [Tooltip("List of all combatants participating in the battle. If empty, will search children.")]
    [SerializeField] private List<Combatant> allCombatants = new List<Combatant>();
    [Tooltip("Delay in seconds between actions for visual effect and readability.")]
    [SerializeField] private float actionDelay = 1.0f; 

    private BattleState currentState = BattleState.WaitingToStart;
    private int currentCombatantIndex = -1; // -1 indicates no turn started yet
    private Combatant currentActiveCombatant;

    // Fields for managing player input
    private bool _playerActionPending;      // True when the player is expected to make a choice
    private Combatant _playerSelectedTarget; // The target chosen by the player

    // Public getter for action delay, useful for combatants to synchronize animations/delays
    public float ActionDelay => actionDelay; 
    public BattleState CurrentState => currentState; // Public getter for current battle state

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the singleton and gathers combatants for the battle.
    /// </summary>
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure only one CombatManager exists
            return;
        }

        // If 'allCombatants' list is empty in the Inspector, try to find them as children.
        // This provides flexibility in how combatants are set up in the scene.
        if (allCombatants.Count == 0)
        {
            // GetComponentsInChildren(true) includes inactive GameObjects,
            // which is useful if combatants are initially disabled and activated by the manager.
            allCombatants = GetComponentsInChildren<Combatant>(true).ToList(); 
        }

        // Ensure all combatants are active at the start of battle logic.
        // This allows them to be disabled by default in the editor.
        foreach (var combatant in allCombatants)
        {
            combatant.gameObject.SetActive(true);
        }

        // Randomize the initial turn order to add variety to battles.
        ShuffleCombatants(allCombatants);
    }

    /// <summary>
    /// Shuffles the list of combatants using the Fisher-Yates algorithm
    /// to randomize the initial turn order.
    /// </summary>
    private void ShuffleCombatants(List<Combatant> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Combatant temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Called on the first frame a script is enabled.
    /// Starts the battle sequence.
    /// </summary>
    private void Start()
    {
        StartBattle();
    }

    /// <summary>
    /// Initiates the battle sequence. This can be called from UI or other game logic.
    /// </summary>
    public void StartBattle()
    {
        LogMessage("---------------- Battle Starts! ----------------");
        currentCombatantIndex = -1; // Reset turn index to ensure the first combatant starts at 0
        currentState = BattleState.WaitingToStart;
        StartCoroutine(NextTurnRoutine()); // Start the main turn-based loop coroutine
    }

    /// <summary>
    /// The main coroutine that manages the sequence of turns for all combatants.
    /// This is the heart of the TurnBasedCombatSystem pattern, orchestrating the entire battle flow.
    /// It cycles through combatants, handles turn execution, and checks for battle end conditions.
    /// </summary>
    private IEnumerator NextTurnRoutine()
    {
        // Continue the battle loop until a win/loss condition is met
        while (currentState != BattleState.BattleOver)
        {
            // Advance to the next combatant in the turn order (circularly)
            currentCombatantIndex = (currentCombatantIndex + 1) % allCombatants.Count;
            currentActiveCombatant = allCombatants[currentCombatantIndex];

            // If the current combatant is dead, skip their turn and immediately proceed to the next one.
            if (currentActiveCombatant.IsDead)
            {
                LogMessage($"{currentActiveCombatant.Name} is dead, skipping turn.");
                yield return null; // Wait one frame to avoid potential infinite loops in edge cases
                continue; // Move to the next combatant in the list
            }

            // Before starting a new turn, check if the battle has already concluded.
            if (CheckBattleOver())
            {
                currentState = BattleState.BattleOver;
                LogMessage("---------------- Battle Over! ----------------");
                yield break; // Exit the coroutine, ending the battle loop
            }

            // Set the current battle state based on who's turn it is.
            if (currentActiveCombatant.IsPlayer)
            {
                currentState = BattleState.PlayerTurn;
                LogMessage($"It's {currentActiveCombatant.Name}'s turn.");
            }
            else
            {
                currentState = BattleState.EnemyTurn;
                LogMessage($"It's {currentActiveCombatant.Name}'s turn.");
            }
            
            // Crucial step: Delegate the turn logic to the current combatant.
            // The 'yield return' here ensures that the CombatManager pauses
            // until the combatant's 'PerformTurn' coroutine completes.
            yield return currentActiveCombatant.PerformTurn(this);

            // After a combatant's turn is completed, introduce a short delay
            // before the next turn begins, enhancing readability and visual pacing.
            yield return new WaitForSeconds(actionDelay);
        }
    }

    /// <summary>
    /// This coroutine is specifically called by a PlayerCombatant during its `PerformTurn` method.
    /// It effectively pauses the player's turn until player input is received via `PlayerAttack`.
    /// </summary>
    /// <param name="player">The player combatant whose turn it is.</param>
    /// <returns>An IEnumerator that yields until player input is received.</returns>
    public IEnumerator GetPlayerAction(PlayerCombatant player)
    {
        _playerActionPending = true;
        _playerSelectedTarget = null; // Reset target selection

        // This loop will continue to yield every frame until _playerActionPending becomes false,
        // which happens when PlayerAttack() is called with a valid target.
        while (_playerActionPending)
        {
            yield return null; // Wait for the next frame
        }

        // Once player input is received and processed by PlayerAttack(),
        // the selected action (e.g., attacking the chosen target) is executed.
        if (_playerSelectedTarget != null && !_playerSelectedTarget.IsDead)
        {
            player.Attack(_playerSelectedTarget); // Player executes their chosen attack
            yield return new WaitForSeconds(actionDelay); // Short delay after player action for effect
        }
        else
        {
            LogMessage("Player did not select a valid target or action was cancelled.");
            // In a real game, you might re-prompt the player or handle a skipped turn.
        }
    }

    /// <summary>
    /// Public method called by a PlayerInput/UI script when the player selects a target to attack.
    /// This method resolves the 'yield return GetPlayerAction()' in the PlayerCombatant's PerformTurn.
    /// </summary>
    /// <param name="target">The combatant chosen as the target by the player.</param>
    public void PlayerAttack(Combatant target)
    {
        // Only allow player action if it's currently the player's turn, an action is pending,
        // and the current active combatant is indeed a player.
        if (currentState == BattleState.PlayerTurn && currentActiveCombatant.IsPlayer && _playerActionPending)
        {
            if (target != null && !target.IsDead)
            {
                _playerSelectedTarget = target;
                _playerActionPending = false; // Signal that player action has been received, unblocking GetPlayerAction
                LogMessage($"{currentActiveCombatant.Name} selected {target.Name} as target.");
                currentState = BattleState.ProcessingAction; // Temporarily update state while action executes
            }
            else
            {
                LogMessage("Invalid target selected. Please choose a living enemy.");
            }
        }
        else
        {
            LogMessage("Cannot attack now. It's not the player's turn, or an action is not pending.");
        }
    }

    /// <summary>
    /// Checks if the battle has concluded (all players or all enemies are defeated).
    /// </summary>
    /// <returns>True if the battle is over, false otherwise.</returns>
    private bool CheckBattleOver()
    {
        List<Combatant> livingPlayers = GetLivingPlayers();
        List<Combatant> livingEnemies = GetLivingEnemies();

        if (livingPlayers.Count == 0)
        {
            LogMessage("All players defeated! Enemies win!");
            return true;
        }

        if (livingEnemies.Count == 0)
        {
            LogMessage("All enemies defeated! Players win!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a list of all currently living player combatants.
    /// Useful for targeting or UI display.
    /// </summary>
    public List<Combatant> GetLivingPlayers()
    {
        // Use LINQ to filter combatants that are players and are not dead.
        return allCombatants.Where(c => c.IsPlayer && !c.IsDead).ToList();
    }

    /// <summary>
    /// Returns a list of all currently living enemy combatants.
    /// Useful for targeting or UI display.
    /// </summary>
    public List<Combatant> GetLivingEnemies()
    {
        // Use LINQ to filter combatants that are NOT players (i.e., enemies) and are not dead.
        return allCombatants.Where(c => !c.IsPlayer && !c.IsDead).ToList();
    }

    /// <summary>
    /// Simple logging utility for battle events.
    /// In a real project, this would be hooked up to a UI Text element or a dedicated logger.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogMessage(string message)
    {
        Debug.Log($"[Combat Log] {message}");
        // Example: If you had a UI Text element to display battle messages:
        // if (uiBattleLogText != null) uiBattleLogText.text += message + "\n";
    }
}


/*
--- EXAMPLE USAGE AND SETUP IN UNITY ---

1.  **Create the 'TurnBasedCombatSystem.cs' Script:**
    *   Create a new C# script in your Unity project, name it `TurnBasedCombatSystem`.
    *   Copy and paste all the code above into this script file.

2.  **Set up the Combat Manager:**
    *   In your Unity scene, create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
    *   Name this GameObject "CombatManager".
    *   Drag and drop the `TurnBasedCombatSystem.cs` script onto this "CombatManager" GameObject in the Inspector.
    *   You can adjust the `Action Delay` in the Inspector to control the pause between actions.

3.  **Create Player Combatants:**
    *   Create another empty GameObject (e.g., "Player1").
    *   Drag and drop the `TurnBasedCombatSystem.cs` script onto "Player1".
    *   In the Inspector for "Player1", you'll see a dropdown for "Script". Since `PlayerCombatant` and `EnemyCombatant` are defined within the same file, you'll select `PlayerCombatant` from the dropdown.
    *   Adjust `Combatant Name`, `Max HP`, `Attack Power` as desired.
    *   Make "Player1" a child of the "CombatManager" GameObject (drag "Player1" onto "CombatManager" in the Hierarchy). This allows the CombatManager to automatically find it.

4.  **Create Enemy Combatants:**
    *   Repeat the process for enemies: Create an empty GameObject (e.g., "EnemyGrunt1").
    *   Drag and drop the `TurnBasedCombatSystem.cs` script onto "EnemyGrunt1".
    *   Select `EnemyCombatant` from the "Script" dropdown.
    *   Adjust its stats.
    *   Make "EnemyGrunt1" a child of the "CombatManager" GameObject.
    *   Create another enemy (e.g., "EnemyGrunt2") and set it up similarly.

    *Alternative Combatant Setup:* Instead of making combatants children, you can manually drag their GameObjects into the `All Combatants` list in the `CombatManager`'s Inspector. This gives more control over scene structure.

5.  **Create a Player Input Handler (Simulated UI):**
    *   Create another empty GameObject in your scene (e.g., "PlayerInputHandler").
    *   Create a new C# script named `PlayerInputHandler.cs`.
    *   Paste the following code into `PlayerInputHandler.cs`:

    ```csharp
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Example script to simulate player input for the turn-based combat system.
    /// In a real game, this would typically involve UI buttons or mouse clicks on targets.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        void Update()
        {
            // Ensure the CombatManager exists and it's currently a player's turn
            if (CombatManager.Instance == null || CombatManager.Instance.CurrentState != CombatManager.BattleState.PlayerTurn)
            {
                return; // Not our turn, or battle not active
            }

            // Get a list of living enemies for dynamic targeting
            List<Combatant> livingEnemies = CombatManager.Instance.GetLivingEnemies();

            // Example: Press '1' to target the first living enemy
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (livingEnemies.Count > 0)
                {
                    CombatManager.Instance.PlayerAttack(livingEnemies[0]);
                }
                else
                {
                    Debug.LogWarning("No enemies to target with '1'!");
                }
            }
            // Example: Press '2' to target the second living enemy
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (livingEnemies.Count > 1) // Make sure there's a second enemy
                {
                    CombatManager.Instance.PlayerAttack(livingEnemies[1]);
                }
                else
                {
                    Debug.LogWarning("No second enemy to target with '2'!");
                }
            }
            // You can extend this for more enemies or other actions.
            // For a real UI, you'd have buttons for each enemy, and their OnClick event
            // would call CombatManager.Instance.PlayerAttack(thisEnemyReference).
        }
    }
    ```
    *   Drag and drop `PlayerInputHandler.cs` onto the "PlayerInputHandler" GameObject.

6.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe the `Debug.Log` messages in the Console window. They will indicate turn progression, attacks, and damage.
    *   When it's a Player Combatant's turn, the console will prompt you. Press '1' or '2' on your keyboard to have the player attack the corresponding enemy.
    *   The battle will continue until all players or all enemies are defeated.

This comprehensive example provides a robust foundation for building more complex turn-based combat systems in Unity, adhering to best practices and the design pattern principles.
*/
```