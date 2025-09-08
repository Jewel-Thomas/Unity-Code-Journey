// Unity Design Pattern Example: BattleSystem
// This script demonstrates the BattleSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

To demonstrate the 'BattleSystem' design pattern in Unity, we'll create a single, comprehensive C# script. This script will include all necessary classes and components, making it ready to drop into a Unity project.

The design pattern focuses on:
1.  **Singleton `BattleSystem`:** A central manager for battle logic.
2.  **State Machine:** Manages the different phases of a battle (Player Turn, Enemy Turn, etc.).
3.  **Command Pattern:** Encapsulates player/enemy actions (Attack, Heal) for execution.
4.  **Data-Driven Abilities (`ScriptableObject`):** Allows easy creation and modification of abilities.
5.  **Event-Driven Communication:** Notifies other game systems (like UI) about battle events.

---

### **`BattleSystemExample.cs`**

```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions like .Where(), .OrderByDescending(), .All()

// This script contains all components for the BattleSystem pattern demonstration.
// For a large, real-world project, these classes would typically be split into separate files
// to follow the Single Responsibility Principle more strictly.
// However, for an educational example designed to be dropped in, a single file is easier to manage.

/// <summary>
/// Represents the fundamental stats of a character.
/// Marked as System.Serializable to be visible in the Inspector.
/// </summary>
[System.Serializable]
public class CharacterStats
{
    public int MaxHealth;
    public int Attack;
    public int Defense;
    public int Speed; // Determines turn order, higher speed acts earlier
}

/// <summary>
/// Abstract base class for any participant in a battle (Player, Enemy).
/// Handles core battle-related properties like health and stats, and provides
/// common methods for taking damage and restoring health.
/// </summary>
public abstract class BattleParticipant : MonoBehaviour
{
    [Tooltip("The name of this battle participant.")]
    public string Name = "Unnamed";

    [Tooltip("The base statistics for this participant.")]
    public CharacterStats BaseStats;

    // Current health, separate from MaxHealth in BaseStats, making it dynamic during battle.
    [SerializeField] // Keeps this field visible in the Inspector for debugging purposes.
    protected int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    [Tooltip("List of abilities this participant can use in battle.")]
    public List<AbilitySO> Abilities;

    protected virtual void Awake()
    {
        // Initialize current health to max health at the start.
        _currentHealth = BaseStats.MaxHealth;
    }

    /// <summary>
    /// Reduces the participant's health based on incoming damage and their defense.
    /// </summary>
    /// <param name="amount">The base amount of damage to inflict.</param>
    /// <returns>The actual damage taken after defense calculation.</returns>
    public virtual int TakeDamage(int amount)
    {
        // Simple defense calculation: damage is reduced by defense, minimum 0.
        int damageTaken = Math.Max(0, amount - BaseStats.Defense);
        _currentHealth -= damageTaken;
        _currentHealth = Mathf.Max(0, _currentHealth); // Health cannot go below 0.
        Debug.Log($"{Name} took {damageTaken} damage. Current Health: {_currentHealth}");
        return damageTaken;
    }

    /// <summary>
    /// Restores the participant's health up to their maximum health.
    /// </summary>
    /// <param name="amount">The amount of health to restore.</param>
    /// <returns>The actual health healed.</returns>
    public virtual int RestoreHealth(int amount)
    {
        int healthBefore = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Min(BaseStats.MaxHealth, _currentHealth); // Health cannot exceed MaxHealth.
        int healedAmount = _currentHealth - healthBefore;
        Debug.Log($"{Name} healed {healedAmount} health. Current Health: {_currentHealth}");
        return healedAmount;
    }

    /// <summary>
    /// Abstract method for choosing an action. Concrete Player and Enemy classes
    /// will implement their specific logic here (e.g., player input, AI decision).
    /// </summary>
    /// <param name="battleSystem">Reference to the BattleSystem for context.</param>
    /// <returns>An IBattleCommand to be executed. Returns null for players as their input
    /// is handled externally via UI, triggering commands directly on BattleSystem.</returns>
    public abstract IBattleCommand ChooseAction(BattleSystem battleSystem);
}

/// <summary>
/// Represents a generic ability or attack that characters can perform.
/// This uses the ScriptableObject pattern for data-driven abilities, allowing designers
/// to create and configure abilities directly in the Unity Editor without coding.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Battle System/Ability", order = 1)]
public class AbilitySO : ScriptableObject
{
    [Tooltip("The display name of the ability.")]
    public string AbilityName = "New Ability";

    [Tooltip("A brief description of what the ability does.")]
    [TextArea]
    public string Description = "A generic ability.";

    [Tooltip("The base power/effectiveness of the ability (e.g., base damage, base heal amount).")]
    public int BasePower = 10;

    [Tooltip("Defines who the ability can target.")]
    public AbilityTargetType TargetType;

    [Tooltip("Defines the primary effect of the ability.")]
    public AbilityEffectType EffectType;

    // Enum for different types of targets an ability can have.
    public enum AbilityTargetType { Single, All, Self }

    // Enum for different effects an ability can produce. Can be extended for buffs, debuffs, etc.
    public enum AbilityEffectType { Damage, Heal, Buff, Debuff }
}

/// <summary>
/// Defines the interface for all battle commands.
/// This is a core component of the Command pattern, which decouples the request (choosing an action)
/// from its execution (performing the action). This makes the system flexible and extensible.
/// </summary>
public interface IBattleCommand
{
    /// <summary>
    /// Executes the specific action defined by the command.
    /// </summary>
    /// <param name="battleSystem">Reference to the BattleSystem, allowing commands to
    /// interact with the overall battle state or notify events.</param>
    void Execute(BattleSystem battleSystem);
}

/// <summary>
/// Concrete command for dealing damage from a source to a target.
/// Implements the IBattleCommand interface.
/// </summary>
public class AttackCommand : IBattleCommand
{
    private BattleParticipant _source;
    private BattleParticipant _target;
    private AbilitySO _ability;

    /// <summary>
    /// Initializes a new instance of the AttackCommand.
    /// </summary>
    /// <param name="source">The participant performing the attack.</param>
    /// <param name="target">The participant being attacked.</param>
    /// <param name="ability">The ability used for the attack, providing base power.</param>
    public AttackCommand(BattleParticipant source, BattleParticipant target, AbilitySO ability)
    {
        _source = source;
        _target = target;
        _ability = ability;
    }

    /// <summary>
    /// Executes the attack, calculating and applying damage to the target.
    /// </summary>
    public void Execute(BattleSystem battleSystem)
    {
        // Pre-check if participants are still valid for the action.
        if (!_source.IsAlive || !_target.IsAlive)
        {
            Debug.Log($"Attack from {_source.Name} to {_target.Name} failed: One or both participants not alive.");
            return;
        }

        // Simple damage calculation: Source Attack + Ability Power.
        int totalDamage = Mathf.Max(0, _source.BaseStats.Attack + _ability.BasePower);
        int actualDamageDealt = _target.TakeDamage(totalDamage);

        // Notify the BattleSystem (and any subscribers) that damage was dealt.
        battleSystem.NotifyDamageDealt(_source, _target, actualDamageDealt);

        Debug.Log($"{_source.Name} used {_ability.AbilityName} on {_target.Name}, dealing {actualDamageDealt} damage.");
    }
}

/// <summary>
/// Concrete command for restoring health to a target.
/// Implements the IBattleCommand interface.
/// </summary>
public class HealCommand : IBattleCommand
{
    private BattleParticipant _source;
    private BattleParticipant _target; // The participant to be healed (can be self or another).
    private AbilitySO _ability;        // The ability used for healing, providing base heal power.

    /// <summary>
    /// Initializes a new instance of the HealCommand.
    /// </summary>
    /// <param name="source">The participant performing the heal.</param>
    /// <param name="target">The participant receiving the heal.</param>
    /// <param name="ability">The ability used for the heal, providing base power.</param>
    public HealCommand(BattleParticipant source, BattleParticipant target, AbilitySO ability)
    {
        _source = source;
        _target = target;
        _ability = ability;
    }

    /// <summary>
    /// Executes the heal, restoring health to the target.
    /// </summary>
    public void Execute(BattleSystem battleSystem)
    {
        // Pre-check if source is alive. Target can be dead but still potentially revivable,
        // though for this simple example, we prevent healing dead targets.
        if (!_source.IsAlive)
        {
            Debug.Log($"Heal from {_source.Name} failed: Source not alive.");
            return;
        }
        if (!_target.IsAlive)
        {
            Debug.Log($"Heal from {_source.Name} on {_target.Name} failed: Target is not alive.");
            return;
        }

        // Simple heal amount from the ability's base power.
        int healAmount = _ability.BasePower;
        int actualHealAmount = _target.RestoreHealth(healAmount);

        // Notify the BattleSystem (and any subscribers) that healing occurred.
        battleSystem.NotifyHealPerformed(_source, _target, actualHealAmount);

        Debug.Log($"{_source.Name} used {_ability.AbilityName} to heal {_target.Name} for {actualHealAmount} health.");
    }
}

/// <summary>
/// The central manager for all battle logic.
/// Implements a state machine to control battle flow (turns, phases).
/// Acts as a Singleton for easy access throughout the game.
/// </summary>
public class BattleSystem : MonoBehaviour
{
    // Singleton instance for easy global access.
    public static BattleSystem Instance { get; private set; }

    /// <summary>
    /// Defines the current state or phase of the battle.
    /// This forms the basis of the battle's state machine.
    /// </summary>
    public enum BattleState
    {
        StartBattle,          // Initial state, setting up battle environment and turn order.
        PlayerTurn,           // Player is actively choosing an action.
        EnemyTurn,            // AI is actively choosing and executing an action.
        ExecutingAction,      // An ability/command is being performed, awaiting its completion.
        CheckingBattleEnd,    // Checking win/loss conditions after an action or turn.
        BattleWon,            // Players have defeated all enemies.
        BattleLost,           // All players have been defeated.
        BattleEnded           // Battle is completely over and results are displayed/processed.
    }

    [Header("Battle Settings")]
    [Tooltip("Delay between action execution for visual clarity.")]
    [SerializeField] private float _actionExecutionDelay = 1.0f; 

    // --- Internal State Variables ---
    private BattleState _currentBattleState;
    public BattleState CurrentBattleState => _currentBattleState;

    private List<BattleParticipant> _allParticipants = new List<BattleParticipant>();
    private List<BattleParticipant> _playerParticipants = new List<BattleParticipant>();
    private List<BattleParticipant> _enemyParticipants = new List<BattleParticipant>();

    private Queue<BattleParticipant> _turnOrderQueue = new Queue<BattleParticipant>();
    private BattleParticipant _currentTurnParticipant;
    public BattleParticipant CurrentTurnParticipant => _currentTurnParticipant;

    // --- Events (for UI and other systems to subscribe to) ---
    // These events allow other parts of the game to react to battle events
    // without the BattleSystem needing to know about those specific systems (Observer Pattern).
    public event Action<BattleState> OnBattleStateChanged;
    public event Action<BattleParticipant> OnTurnStarted;
    public event Action<BattleParticipant, BattleParticipant, int> OnDamageDealt;
    public event Action<BattleParticipant, BattleParticipant, int> OnHealPerformed;
    public event Action<BattleParticipant> OnCharacterDied;
    public event Action<bool> OnBattleEnded; // 'true' for won, 'false' for lost.

    // Specific event to notify when player input is expected (e.g., UI should activate).
    public event Action OnPlayerInputExpected;

    // --- Singleton Setup ---
    private void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances.
        }
        else
        {
            Instance = this; // Set this as the singleton instance.
            // Optional: DontDestroyOnLoad(gameObject); if the battle system persists across scenes.
        }
    }

    // --- Public Initialization Method ---

    /// <summary>
    /// Initializes and starts a new battle with the given player and enemy participants.
    /// </summary>
    /// <param name="players">List of player-controlled participants.</param>
    /// <param name="enemies">List of enemy-controlled participants.</param>
    public void StartBattle(List<BattleParticipant> players, List<BattleParticipant> enemies)
    {
        _playerParticipants = players;
        _enemyParticipants = enemies;

        // Combine all participants into a single list for turn management.
        _allParticipants.Clear();
        _allParticipants.AddRange(_playerParticipants);
        _allParticipants.AddRange(_enemyParticipants);

        Debug.Log("Battle Initialized with " + _playerParticipants.Count + " players and " + _enemyParticipants.Count + " enemies.");
        ChangeState(BattleState.StartBattle); // Begin the battle state machine.
    }

    // --- State Machine Logic ---

    /// <summary>
    /// Changes the current battle state and triggers actions associated with the new state.
    /// This is the core of the battle's state machine.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    private void ChangeState(BattleState newState)
    {
        _currentBattleState = newState;
        Debug.Log($"Battle State Changed: {newState}");
        OnBattleStateChanged?.Invoke(newState); // Notify subscribers of the state change.

        switch (newState)
        {
            case BattleState.StartBattle:
                InitializeTurnOrder(); // Determine who goes first.
                StartCoroutine(BattleFlowCoroutine()); // Start the main battle loop.
                break;
            case BattleState.PlayerTurn:
                OnPlayerInputExpected?.Invoke(); // Notify UI to prompt player for input.
                // The system will wait for ProcessCommand to be called externally (e.g., from UI).
                break;
            case BattleState.EnemyTurn:
                StartCoroutine(PerformEnemyAction()); // AI takes its turn.
                break;
            case BattleState.ExecutingAction:
                // This state is entered when a command is processed. The BattleFlowCoroutine
                // will wait for the command execution to complete before proceeding.
                break;
            case BattleState.CheckingBattleEnd:
                CheckBattleEndConditions(); // Determine if battle is won/lost.
                break;
            case BattleState.BattleWon:
                OnBattleEnded?.Invoke(true); // Notify win condition.
                break;
            case BattleState.BattleLost:
                OnBattleEnded?.Invoke(false); // Notify loss condition.
                break;
            case BattleState.BattleEnded:
                // Final state, useful for cleanup or transitioning out of battle.
                Debug.Log("Battle is fully concluded.");
                break;
        }
    }

    /// <summary>
    /// The main coroutine that orchestrates the battle flow, cycling through turns
    /// and states until the battle ends.
    /// </summary>
    private IEnumerator BattleFlowCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // Small delay to visually start the battle.

        // Loop as long as the battle is ongoing (not won or lost).
        while (_currentBattleState != BattleState.BattleWon && _currentBattleState != BattleState.BattleLost)
        {
            AdvanceTurn(); // Determine whose turn it is next.
            yield return new WaitForSeconds(0.2f); // Small pause between turns.

            // If no valid participant can take a turn (e.g., all dead), check battle end.
            if (_currentTurnParticipant == null || !_currentTurnParticipant.IsAlive)
            {
                Debug.Log("Current turn participant is null or dead, checking battle end.");
                ChangeState(BattleState.CheckingBattleEnd);
                continue; // Skip to next iteration to re-check conditions.
            }

            OnTurnStarted?.Invoke(_currentTurnParticipant); // Notify subscribers of the new turn.
            Debug.Log($"It's {_currentTurnParticipant.Name}'s turn!");

            // Branch based on participant type (Player or Enemy).
            if (_playerParticipants.Contains(_currentTurnParticipant))
            {
                ChangeState(BattleState.PlayerTurn);
                // Wait for an external command (from player input/UI) to change state.
                yield return new WaitUntil(() => _currentBattleState != BattleState.PlayerTurn);
            }
            else if (_enemyParticipants.Contains(_currentTurnParticipant))
            {
                ChangeState(BattleState.EnemyTurn);
                // Wait for the enemy AI to perform its action and change state.
                yield return new WaitUntil(() => _currentBattleState != BattleState.EnemyTurn);
            }
            
            // After any action is executed, or if a turn participant was invalid, check battle end conditions.
            ChangeState(BattleState.CheckingBattleEnd);
            yield return new WaitUntil(() => _currentBattleState != BattleState.CheckingBattleEnd); // Wait until check is done.

            // Small delay after action/check for better visual pacing.
            yield return new WaitForSeconds(_actionExecutionDelay);
        }

        Debug.Log("Battle flow coroutine ended.");
    }

    /// <summary>
    /// Establishes the initial turn order based on participants' speed, highest speed first.
    /// </summary>
    private void InitializeTurnOrder()
    {
        _turnOrderQueue.Clear();

        // Filter out any participants that might already be dead and sort by speed.
        List<BattleParticipant> activeParticipants = _allParticipants
            .Where(p => p.IsAlive)
            .OrderByDescending(p => p.BaseStats.Speed)
            .ToList();

        foreach (var p in activeParticipants)
        {
            _turnOrderQueue.Enqueue(p);
        }

        Debug.Log("Initial Turn Order: " + string.Join(", ", activeParticipants.Select(p => p.Name)));
    }

    /// <summary>
    /// Advances to the next participant in the turn order.
    /// Re-initializes the turn queue if all participants have taken a turn in the current round.
    /// Skips over any defeated participants.
    /// </summary>
    private void AdvanceTurn()
    {
        // If the queue is empty, a round has completed, so re-initialize.
        if (_turnOrderQueue.Count == 0)
        {
            InitializeTurnOrder();
            if (_turnOrderQueue.Count == 0) // If still empty, it means all participants are dead.
            {
                _currentTurnParticipant = null;
                return;
            }
        }

        BattleParticipant nextParticipant = null;
        int initialQueueCount = _turnOrderQueue.Count; // To prevent infinite loops if everyone is dead.

        // Loop through the queue to find the next *alive* participant.
        for (int i = 0; i < initialQueueCount; i++)
        {
            BattleParticipant participant = _turnOrderQueue.Dequeue();
            if (participant.IsAlive)
            {
                nextParticipant = participant;
                _turnOrderQueue.Enqueue(participant); // Place the participant at the end for the next round.
                break; // Found an alive participant, break.
            }
            // If dead, don't re-enqueue; effectively removes them from turn order for this round.
            // They will be re-evaluated if InitializeTurnOrder is called again next round.
        }

        _currentTurnParticipant = nextParticipant;

        // If after trying all in queue, no one is alive, set to null.
        if (_currentTurnParticipant == null || !_currentTurnParticipant.IsAlive)
        {
            _currentTurnParticipant = null;
            Debug.Log("No active participants left in turn queue.");
        }
    }

    /// <summary>
    /// Processes a battle command issued by a participant (player or enemy AI).
    /// This is the entry point for all actions within the battle.
    /// </summary>
    /// <param name="command">The IBattleCommand instance to execute.</param>
    public void ProcessCommand(IBattleCommand command)
    {
        // Ensure commands are only processed during appropriate states.
        if (_currentBattleState != BattleState.PlayerTurn && _currentBattleState != BattleState.EnemyTurn)
        {
            Debug.LogWarning($"Cannot process command in current state: {_currentBattleState}");
            return;
        }

        ChangeState(BattleState.ExecutingAction); // Transition to action execution state.
        StartCoroutine(ExecuteCommandCoroutine(command));
    }

    /// <summary>
    /// Coroutine to execute a given battle command, allowing for delays/animations.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    private IEnumerator ExecuteCommandCoroutine(IBattleCommand command)
    {
        command.Execute(this); // Perform the action defined by the command.
        yield return new WaitForSeconds(_actionExecutionDelay); // Pause for visual effect.

        // After the command completes, transition to checking battle end conditions.
        ChangeState(BattleState.CheckingBattleEnd);
    }

    /// <summary>
    /// Simple AI logic for enemy turns. Chooses a random ability and a random alive player target.
    /// </summary>
    private IEnumerator PerformEnemyAction()
    {
        yield return new WaitForSeconds(_actionExecutionDelay); // Simulate AI thinking time.

        if (_currentTurnParticipant == null || !_currentTurnParticipant.IsAlive)
        {
            Debug.Log("Enemy turn skipped: Participant is null or dead.");
            ChangeState(BattleState.CheckingBattleEnd);
            yield break; // Exit coroutine.
        }

        // Get all currently alive player targets.
        List<BattleParticipant> alivePlayerTargets = _playerParticipants.Where(p => p.IsAlive).ToList();

        if (alivePlayerTargets.Count == 0)
        {
            Debug.Log("No player targets for enemy to attack. Enemy skips turn.");
            ChangeState(BattleState.CheckingBattleEnd);
            yield break;
        }

        // Enemy chooses a random ability from its list.
        AbilitySO chosenAbility = _currentTurnParticipant.Abilities[UnityEngine.Random.Range(0, _currentTurnParticipant.Abilities.Count)];
        if (chosenAbility == null)
        {
            Debug.LogWarning($"{_currentTurnParticipant.Name} has no abilities! Skipping turn.");
            ChangeState(BattleState.CheckingBattleEnd);
            yield break;
        }

        IBattleCommand enemyCommand = null;
        switch (chosenAbility.EffectType)
        {
            case AbilitySO.AbilityEffectType.Damage:
                // Choose a random alive player to attack.
                BattleParticipant target = alivePlayerTargets[UnityEngine.Random.Range(0, alivePlayerTargets.Count)];
                enemyCommand = new AttackCommand(_currentTurnParticipant, target, chosenAbility);
                break;
            case AbilitySO.AbilityEffectType.Heal:
                // For simplicity, enemy always heals itself if it has a heal ability.
                // Could be extended to heal other low-health enemies.
                enemyCommand = new HealCommand(_currentTurnParticipant, _currentTurnParticipant, chosenAbility);
                break;
            default:
                Debug.LogWarning($"Unsupported ability effect type for enemy AI: {chosenAbility.EffectType}. Skipping turn.");
                break;
        }

        if (enemyCommand != null)
        {
            ProcessCommand(enemyCommand); // Process the chosen command.
        }
        else
        {
            // If no valid command was created, directly transition to checking battle end.
            ChangeState(BattleState.CheckingBattleEnd);
        }
        // The ExecuteCommandCoroutine (triggered by ProcessCommand) will handle the state transition after execution.
    }

    /// <summary>
    /// Checks if all players or all enemies have been defeated to determine battle outcome.
    /// </summary>
    private void CheckBattleEndConditions()
    {
        bool playersAllDead = _playerParticipants.All(p => !p.IsAlive);
        bool enemiesAllDead = _enemyParticipants.All(p => !p.IsAlive);

        if (playersAllDead)
        {
            Debug.Log("All players defeated! Battle Lost.");
            ChangeState(BattleState.BattleLost);
        }
        else if (enemiesAllDead)
        {
            Debug.Log("All enemies defeated! Battle Won.");
            ChangeState(BattleState.BattleWon);
        }
        else
        {
            // If battle not ended, continue the battle loop by returning to the 'StartBattle' logic
            // (which effectively means preparing for the next turn sequence).
            ChangeState(BattleState.StartBattle);
        }
    }

    // --- Event Notifiers (Internal use, called by Commands) ---
    // These methods provide a single point for commands to notify the BattleSystem
    // about specific events, which then can propagate to any external subscribers.

    public void NotifyDamageDealt(BattleParticipant source, BattleParticipant target, int amount)
    {
        Debug.Log($"EVENT: {source.Name} dealt {amount} damage to {target.Name}. {target.Name} HP: {target.CurrentHealth}");
        OnDamageDealt?.Invoke(source, target, amount); // Invoke event for subscribers.

        if (!target.IsAlive)
        {
            Debug.Log($"EVENT: {target.Name} has been defeated!");
            OnCharacterDied?.Invoke(target); // Invoke character died event.
        }
    }

    public void NotifyHealPerformed(BattleParticipant source, BattleParticipant target, int amount)
    {
        Debug.Log($"EVENT: {source.Name} healed {target.Name} for {amount} HP. {target.Name} HP: {target.CurrentHealth}");
        OnHealPerformed?.Invoke(source, target, amount); // Invoke event for subscribers.
    }
}

/// <summary>
/// Represents a player-controlled character in the battle.
/// In a real game, this would interface with UI for input.
/// For this example, we'll expose public methods that a simulated input handler calls.
/// </summary>
public class PlayerCharacter : BattleParticipant
{
    // These methods are publicly exposed for external systems (like UI controllers
    // or, in this example, BattleStarter's simulation) to call when a player chooses an action.

    public void SimulatePlayerAttack(BattleParticipant target, AbilitySO ability)
    {
        // Basic validation: ensure it's this player's turn and the battle is in the correct state.
        if (BattleSystem.Instance == null || BattleSystem.Instance.CurrentTurnParticipant != this ||
            BattleSystem.Instance.CurrentBattleState != BattleSystem.BattleState.PlayerTurn)
        {
            Debug.LogWarning($"It's not {Name}'s turn or battle is not in PlayerTurn state to perform action.");
            return;
        }

        if (!IsAlive)
        {
            Debug.LogWarning($"{Name} is defeated and cannot perform actions.");
            return;
        }

        Debug.Log($"{Name} simulates choosing to attack {target.Name} with {ability.AbilityName}.");
        IBattleCommand attack = new AttackCommand(this, target, ability);
        BattleSystem.Instance.ProcessCommand(attack); // Send the command to the BattleSystem for execution.
    }

    public void SimulatePlayerHealSelf(AbilitySO ability)
    {
        if (BattleSystem.Instance == null || BattleSystem.Instance.CurrentTurnParticipant != this ||
            BattleSystem.Instance.CurrentBattleState != BattleSystem.BattleState.PlayerTurn)
        {
            Debug.LogWarning($"It's not {Name}'s turn or battle is not in PlayerTurn state to perform action.");
            return;
        }

        if (!IsAlive)
        {
            Debug.LogWarning($"{Name} is defeated and cannot perform actions.");
            return;
        }

        Debug.Log($"{Name} simulates choosing to heal self with {ability.AbilityName}.");
        IBattleCommand heal = new HealCommand(this, this, ability); // Target is self.
        BattleSystem.Instance.ProcessCommand(heal); // Send the command to the BattleSystem.
    }

    /// <summary>
    /// For player characters, this method signifies that the BattleSystem is waiting for
    /// player input. It doesn't return a command directly, as the command is generated
    /// by external input (e.g., UI clicks calling SimulatePlayerAttack/Heal).
    /// </summary>
    public override IBattleCommand ChooseAction(BattleSystem battleSystem)
    {
        Debug.Log($"It's {Name}'s turn. Waiting for player input...");
        return null; // Player input will trigger a command externally via `BattleSystem.ProcessCommand`.
    }
}

/// <summary>
/// Represents an AI-controlled enemy character in the battle.
/// The `BattleSystem` itself will handle the AI decision-making for enemies
/// in its `PerformEnemyAction` coroutine, so this class primarily serves as a marker
/// and holds enemy-specific properties.
/// </summary>
public class EnemyCharacter : BattleParticipant
{
    /// <summary>
    /// For enemy characters, the actual AI decision and command creation
    /// are handled by the BattleSystem's `PerformEnemyAction` method.
    /// This method can return null here as it's not directly responsible
    /// for generating the command in this specific BattleSystem implementation.
    /// </summary>
    public override IBattleCommand ChooseAction(BattleSystem battleSystem)
    {
        return null; // BattleSystem's PerformEnemyAction handles enemy AI decisions.
    }
}

/// <summary>
/// An example MonoBehaviour to set up and start a battle in the Unity scene.
/// This script would be attached to an empty GameObject in the scene to demonstrate
/// the BattleSystem by instantiating participants and simulating player input.
/// </summary>
public class BattleStarter : MonoBehaviour
{
    [Header("Player Setup")]
    [Tooltip("Prefabs of player characters to instantiate.")]
    public List<PlayerCharacter> PlayerPrefabs; 
    [Tooltip("Transforms indicating where players will be spawned.")]
    public List<Transform> PlayerSpawnPoints;

    [Header("Enemy Setup")]
    [Tooltip("Prefabs of enemy characters to instantiate.")]
    public List<EnemyCharacter> EnemyPrefabs;
    [Tooltip("Transforms indicating where enemies will be spawned.")]
    public List<Transform> EnemySpawnPoints;

    [Header("Abilities (Assigned to characters dynamically)")]
    [Tooltip("The default attack ability for players.")]
    public AbilitySO PlayerAttackAbility;
    [Tooltip("The default heal ability for players.")]
    public AbilitySO PlayerHealAbility;
    [Tooltip("The default attack ability for enemies.")]
    public AbilitySO EnemyAttackAbility;
    [Tooltip("Optional: The default heal ability for enemies.")]
    public AbilitySO EnemyHealAbility; 

    private List<PlayerCharacter> _activePlayers = new List<PlayerCharacter>();
    private List<EnemyCharacter> _activeEnemies = new List<EnemyCharacter>();

    // Used to manage the coroutine for simulating player input.
    private Coroutine _playerInputCoroutine;

    void Start()
    {
        // Subscribe to BattleSystem events for logging and demonstrating event-driven design.
        if (BattleSystem.Instance != null)
        {
            BattleSystem.Instance.OnBattleStateChanged += HandleBattleStateChange;
            BattleSystem.Instance.OnTurnStarted += HandleTurnStarted;
            BattleSystem.Instance.OnDamageDealt += HandleDamageDealt;
            BattleSystem.Instance.OnHealPerformed += HandleHealPerformed;
            BattleSystem.Instance.OnCharacterDied += HandleCharacterDied;
            BattleSystem.Instance.OnBattleEnded += HandleBattleEnded;
            BattleSystem.Instance.OnPlayerInputExpected += HandlePlayerInputExpected;
        }
        else
        {
            Debug.LogError("BattleSystem not found! Make sure an GameObject with the BattleSystem script is in the scene.");
            enabled = false; // Disable this script if BattleSystem is missing.
            return;
        }

        SetupBattleParticipants(); // Instantiate characters.

        // Start the battle after a small delay to allow everything to initialize.
        Invoke("DelayedStartBattle", 1f);
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks and null reference exceptions.
        if (BattleSystem.Instance != null)
        {
            BattleSystem.Instance.OnBattleStateChanged -= HandleBattleStateChange;
            BattleSystem.Instance.OnTurnStarted -= HandleTurnStarted;
            BattleSystem.Instance.OnDamageDealt -= HandleDamageDealt;
            BattleSystem.Instance.OnHealPerformed -= HandleHealPerformed;
            BattleSystem.Instance.OnCharacterDied -= HandleCharacterDied;
            BattleSystem.Instance.OnBattleEnded -= HandleBattleEnded;
            BattleSystem.Instance.OnPlayerInputExpected -= HandlePlayerInputExpected;
        }
    }

    /// <summary>
    /// Instantiates player and enemy prefabs at their designated spawn points
    /// and assigns their initial abilities.
    /// </summary>
    private void SetupBattleParticipants()
    {
        // Instantiate Players
        for (int i = 0; i < PlayerPrefabs.Count && i < PlayerSpawnPoints.Count; i++)
        {
            PlayerCharacter player = Instantiate(PlayerPrefabs[i], PlayerSpawnPoints[i].position, Quaternion.identity);
            player.Name = PlayerPrefabs[i].Name + " " + (i + 1); // Customize instantiated name.
            player.Abilities.Clear(); // Clear any abilities on prefab, then add specific ones.
            player.Abilities.Add(PlayerAttackAbility);
            player.Abilities.Add(PlayerHealAbility);
            _activePlayers.Add(player);
        }

        // Instantiate Enemies
        for (int i = 0; i < EnemyPrefabs.Count && i < EnemySpawnPoints.Count; i++)
        {
            EnemyCharacter enemy = Instantiate(EnemyPrefabs[i], EnemySpawnPoints[i].position, Quaternion.identity);
            enemy.Name = EnemyPrefabs[i].Name + " " + (i + 1); // Customize instantiated name.
            enemy.Abilities.Clear();
            enemy.Abilities.Add(EnemyAttackAbility);
            if(EnemyHealAbility != null) enemy.Abilities.Add(EnemyHealAbility); // Optionally add heal for enemies.
            _activeEnemies.Add(enemy);
        }

        Debug.Log("Participants instantiated.");
    }

    /// <summary>
    /// Calls the BattleSystem to officially start the battle.
    /// Used with Invoke to ensure all Start methods have completed.
    /// </summary>
    private void DelayedStartBattle()
    {
        BattleSystem.Instance.StartBattle(_activePlayers.Cast<BattleParticipant>().ToList(), _activeEnemies.Cast<BattleParticipant>().ToList());
    }

    // --- Event Handlers for Demonstration (Simulate UI/Logging) ---
    // These methods would typically update UI elements, play sounds, or trigger animations
    // in a real game. Here, they just log to the console.

    private void HandleBattleStateChange(BattleSystem.BattleState state)
    {
        Debug.Log($"[UI/LOG] Battle state changed to: {state}");
        // If battle ends, stop any ongoing player input simulation.
        if (state == BattleSystem.BattleState.BattleWon || state == BattleSystem.BattleState.BattleLost)
        {
            if (_playerInputCoroutine != null)
            {
                StopCoroutine(_playerInputCoroutine);
                _playerInputCoroutine = null;
            }
        }
    }

    private void HandleTurnStarted(BattleParticipant participant)
    {
        Debug.Log($"[UI/LOG] It's {participant.Name}'s turn!");
    }

    private void HandleDamageDealt(BattleParticipant source, BattleParticipant target, int amount)
    {
        Debug.Log($"[UI/LOG] {target.Name} took {amount} damage from {source.Name}. HP: {target.CurrentHealth}/{target.BaseStats.MaxHealth}");
    }

    private void HandleHealPerformed(BattleParticipant source, BattleParticipant target, int amount)
    {
        Debug.Log($"[UI/LOG] {target.Name} healed {amount} HP from {source.Name}. HP: {target.CurrentHealth}/{target.BaseStats.MaxHealth}");
    }

    private void HandleCharacterDied(BattleParticipant character)
    {
        Debug.Log($"[UI/LOG] {character.Name} has been defeated!");
    }

    private void HandleBattleEnded(bool won)
    {
        Debug.Log($"[UI/LOG] Battle Concluded! You {(won ? "Won!" : "Lost!")}");
        // Example: Transition to results screen, play victory/defeat music.
    }

    /// <summary>
    /// Handles the event when BattleSystem expects player input.
    /// This method simulates a player choosing an action after a delay.
    /// </summary>
    private void HandlePlayerInputExpected()
    {
        Debug.Log("[UI/LOG] Player input is expected. Simulating a random player action...");
        if (_playerInputCoroutine != null)
        {
            StopCoroutine(_playerInputCoroutine); // Stop any previous simulation if it's still running.
        }
        _playerInputCoroutine = StartCoroutine(SimulatePlayerActionAfterDelay(2f)); // Simulate player 'thinking' for 2 seconds.
    }

    /// <summary>
    /// Coroutine to simulate player decision-making and action execution.
    /// </summary>
    private IEnumerator SimulatePlayerActionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Double-check state before acting, as battle state might have changed during the delay.
        if (BattleSystem.Instance.CurrentBattleState != BattleSystem.BattleState.PlayerTurn)
        {
            Debug.LogWarning("Skipping simulated player action: Not player turn anymore.");
            _playerInputCoroutine = null;
            yield break;
        }

        PlayerCharacter currentPlayer = BattleSystem.Instance.CurrentTurnParticipant as PlayerCharacter;
        if (currentPlayer == null || !currentPlayer.IsAlive)
        {
            Debug.LogWarning("Skipping simulated player action: Current turn participant is not a player or is dead.");
            _playerInputCoroutine = null;
            yield break;
        }

        // --- Simulated Player Action Logic ---
        List<BattleParticipant> aliveEnemies = _activeEnemies.Where(e => e.IsAlive).ToList();
        
        // Simple AI for player: 70% chance to attack if enemies exist, otherwise heal self if needed.
        bool hasEnemies = aliveEnemies.Count > 0;
        bool shouldHealSelf = (currentPlayer.CurrentHealth < currentPlayer.BaseStats.MaxHealth * 0.7f); // Heal if below 70% HP.

        if (hasEnemies && (UnityEngine.Random.value < 0.7f || !shouldHealSelf))
        {
            // Attack an enemy.
            BattleParticipant targetEnemy = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
            currentPlayer.SimulatePlayerAttack(targetEnemy, PlayerAttackAbility);
        }
        else if (shouldHealSelf && PlayerHealAbility != null)
        {
            // Heal self.
            currentPlayer.SimulatePlayerHealSelf(PlayerHealAbility);
        }
        else if (hasEnemies) // Fallback: if no heal or heal not needed, and enemies exist, attack.
        {
            BattleParticipant targetEnemy = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
            currentPlayer.SimulatePlayerAttack(targetEnemy, PlayerAttackAbility);
        }
        else
        {
            // If no enemies and no healing needed (or no heal ability), player effectively "skips" turn.
            // In a real game, a "Pass Turn" button would explicitly process a NoOpCommand or similar.
            // For this example, if no valid action is taken, the BattleSystem will eventually detect
            // that PlayerTurn state isn't changing and proceed.
            Debug.Log($"{currentPlayer.Name} could not find a valid action (no enemies, no healing needed/available). Effectively skips turn.");
            BattleSystem.Instance.ChangeState(BattleSystem.BattleState.CheckingBattleEnd); // Force state transition.
        }
        _playerInputCoroutine = null; // Mark coroutine as finished.
    }
}
```

---

### **How to Use in Unity:**

1.  **Create a C# Script:** In your Unity project, right-click in the Project window, select `Create -> C# Script`, and name it `BattleSystemExample`. Copy and paste the entire code above into this new script.
2.  **Create Abilities (ScriptableObjects):**
    *   Right-click in your Project window -> `Create -> Battle System -> Ability`.
    *   Create **three** (or four) instances:
        *   **`PlayerAttack`**: Set `AbilityName` to "Sword Slash", `BasePower` to 20, `EffectType` to `Damage`, `TargetType` to `Single`.
        *   **`PlayerHeal`**: Set `AbilityName` to "Minor Heal", `BasePower` to 15, `EffectType` to `Heal`, `TargetType` to `Self`.
        *   **`EnemyAttack`**: Set `AbilityName` to "Goblin Strike", `BasePower` to 15, `EffectType` to `Damage`, `TargetType` to `Single`.
        *   (Optional) **`EnemyHeal`**: Set `AbilityName` to "Goblin Heal", `BasePower` to 10, `EffectType` to `Heal`, `TargetType` to `Self`.
3.  **Create Player and Enemy Prefabs:**
    *   Create an empty GameObject in your Hierarchy. Rename it to `Player_Hero`.
    *   Add the `PlayerCharacter` component to it.
    *   In the Inspector for `PlayerCharacter`:
        *   Set `Name` to "Hero".
        *   Expand `BaseStats` and set: `MaxHealth: 100`, `Attack: 25`, `Defense: 5`, `Speed: 10`.
        *   Leave `Abilities` empty (they will be assigned by `BattleStarter`).
    *   Drag this `Player_Hero` GameObject from the Hierarchy into your Project window to create a prefab. Delete the instance from the Hierarchy.
    *   Repeat this process to create another player prefab, e.g., `Player_Mage` (`MaxHealth: 90`, `Attack: 20`, `Defense: 3`, `Speed: 8`).
    *   Repeat for enemies: Create `Enemy_Goblin` (`EnemyCharacter` component, `MaxHealth: 80`, `Attack: 18`, `Defense: 2`, `Speed: 7`).
    *   Create another enemy prefab, e.g., `Enemy_Orc` (`EnemyCharacter` component, `MaxHealth: 120`, `Attack: 22`, `Defense: 4`, `Speed: 6`).
4.  **Create Spawn Points (Optional, but Recommended):**
    *   In your Hierarchy, create several empty GameObjects (e.g., `PlayerSpawnPoint1`, `PlayerSpawnPoint2`, `EnemySpawnPoint1`, `EnemySpawnPoint2`). Position them where you want your characters to appear in the scene.
5.  **Create the `BattleSystem` GameObject:**
    *   Create an empty GameObject in your Hierarchy. Name it `BattleSystem`.
    *   Add the `BattleSystem` component to it. This will make it accessible as a Singleton.
6.  **Create the `BattleStarter` GameObject:**
    *   Create an empty GameObject in your Hierarchy. Name it `BattleStarter`.
    *   Add the `BattleStarter` component to it.
    *   **Configure `BattleStarter` in the Inspector:**
        *   **Player Setup:**
            *   Set `Player Prefabs` size to 2. Drag `Player_Hero` and `Player_Mage` into the slots.
            *   Set `Player Spawn Points` size to 2. Drag `PlayerSpawnPoint1` and `PlayerSpawnPoint2` Transforms into the slots.
        *   **Enemy Setup:**
            *   Set `Enemy Prefabs` size to 2. Drag `Enemy_Goblin` and `Enemy_Orc` into the slots.
            *   Set `Enemy Spawn Points` size to 2. Drag `EnemySpawnPoint1` and `EnemySpawnPoint2` Transforms into the slots.
        *   **Abilities:**
            *   Drag `PlayerAttack`, `PlayerHeal`, `EnemyAttack`, and `EnemyHeal` (if you created it) ScriptableObjects into their respective slots.
7.  **Run the Scene:**
    *   Press Play in the Unity Editor.
    *   Observe the Console window. You will see logs detailing the battle's progression: state changes, turn starts, actions being performed, damage/healing, and battle conclusions. The `BattleStarter` script will simulate player input after a short delay.

This setup provides a complete and practical example of the BattleSystem pattern, demonstrating its modularity, extensibility, and event-driven nature for managing complex combat in Unity.