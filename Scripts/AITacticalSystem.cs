// Unity Design Pattern Example: AITacticalSystem
// This script demonstrates the AITacticalSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity example demonstrates the **AITacticalSystem** design pattern. This pattern is particularly useful in game AI for managing groups of units (like a squad) by centralizing strategic decision-making while delegating tactical execution to individual agents.

---

### AITacticalSystem Design Pattern Explained

**Core Idea:** The AITacticalSystem pattern involves a central "Tactical Manager" that acts as the brain for a group of AI units. This manager perceives the overall battlefield state, determines high-level strategic goals, and then assigns specific roles and orders to individual AI agents to achieve those goals. Individual agents, in turn, are responsible for executing their assigned roles.

**Components:**

1.  **Tactical Manager (`AITacticalManager`):**
    *   **Perception:** Gathers information about friendly units, enemy units, objectives, terrain, and other relevant game state.
    *   **Strategic Goal Setting:** Based on its perception, it determines the most appropriate overall `TacticalGoal` (e.g., `EngageAllEnemies`, `DefendPosition`, `FlankEnemy`).
    *   **Role Assignment:** Once a strategic goal is chosen, it assigns specific `UnitRole`s (e.g., `Attacker`, `Defender`, `Flanker`, `Support`) to its individual AI agents. It also provides context, such as a specific `target` (enemy) or `destination` (position), required for that role.
    *   **Iteration:** Continuously re-evaluates the situation and updates goals and unit roles as the battlefield changes.

2.  **AI Agent (`AIAgent`):**
    *   **Role Execution:** Each agent receives a `UnitRole` and associated parameters (target, destination) from the Tactical Manager. It then independently executes the behavior associated with that role.
    *   **State:** Maintains its own combat state (health, position, etc.).
    *   **No Strategic Decision:** An individual agent does *not* decide the overall strategy; it only follows the orders given by the Manager.

3.  **Tactical Goals (`TacticalGoal` enum):**
    *   High-level objectives for the entire group (e.g., "Eliminate all enemies," "Secure the flag," "Regroup").

4.  **Unit Roles (`UnitRole` enum):**
    *   Specific tasks assigned to individual agents within the context of the overall goal (e.g., "Attack the closest enemy," "Move to cover point A," "Heal Unit B").

**Benefits:**

*   **Centralized Control:** Simplifies managing complex group behaviors from a single point.
*   **Adaptability:** The system can quickly adapt to changing situations by reassigning roles and goals.
*   **Scalability:** New unit roles or tactical goals can be added without significant changes to individual AI agents, as long as the manager knows how to assign them.
*   **Modularity:** Clearly separates strategic decision-making (Manager) from tactical execution (Agents).
*   **Debugging:** Easier to understand why units are doing what they are doing, as decisions flow from a single source.

---

### Complete C# Unity Example

This example simulates a squad of friendly AI units (AIAgents) engaging enemy units (EnemyAgents) under the command of an `AITacticalManager`. The manager will dynamically assess the situation and assign roles like Attacker, Defender, Flanker, or Support.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like OrderBy, Where etc.

// --- 1. Enums for Tactical Decisions ---
// These enums define the strategic goals for the AI Tactical Manager
// and the specific roles individual AI agents can take.

/// <summary>
/// Defines the overall strategic goals that the AITacticalManager can pursue.
/// </summary>
public enum TacticalGoal
{
    EngageAllEnemies,   // Focus on attacking all visible enemies.
    DefendPosition,     // Focus on defending a specific location, perhaps seeking cover.
    FlankEnemy,         // Focus on maneuvering around an enemy to attack from a less defended side.
    Retreat,            // Withdraw to a safer position. (Not fully implemented in demo, but a valid goal)
    HealFriendly        // Focus on healing a low-health friendly unit (if a healer unit exists).
}

/// <summary>
/// Defines the specific role an individual AI unit is assigned by the AITacticalManager.
/// </summary>
public enum UnitRole
{
    None,       // No specific role assigned yet (e.g., idling, waiting for orders).
    Attacker,   // Focuses on direct combat, pushing towards the enemy.
    Defender,   // Focuses on holding a position, engaging enemies that approach.
    Flanker,    // Attempts to move to a tactical position to attack enemies from the side/rear.
    Support,    // Provides support (e.g., healing, buffs, suppressing fire).
    Scout       // Gathers information, avoids direct engagement. (Not fully implemented in demo)
}

// --- 2. Base Classes for AI Agents ---
// These classes provide common functionality for both friendly and enemy units.

/// <summary>
/// Base class for all combatants (friendly and enemy).
/// Manages basic attributes like health and provides a way to take damage and attack.
/// </summary>
public abstract class Combatant : MonoBehaviour
{
    [Header("Combatant Stats")]
    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackRange = 5f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;

    protected float lastAttackTime;

    // Public properties to access combatant state
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float AttackRange => attackRange;
    public float MoveSpeed => moveSpeed;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces the combatant's health by the specified amount.
    /// If damage is negative, it acts as healing.
    /// </summary>
    /// <param name="damage">The amount of damage to take (positive for damage, negative for healing).</param>
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive && damage > 0) return; // Cannot take damage if already dead

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Clamp health between 0 and max

        Debug.Log($"{name} took {damage} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateVisuals(); // Update any health bar or color changes
    }

    /// <summary>
    /// Abstract method for dying, to be implemented by derived classes.
    /// </summary>
    protected abstract void Die();

    /// <summary>
    /// Abstract method to update visual representations based on health/state.
    /// </summary>
    protected abstract void UpdateVisuals();

    /// <summary>
    /// Simple attack logic. Tries to damage the target if within range and cooldown is ready.
    /// </summary>
    /// <param name="target">The target combatant to attack.</param>
    protected void Attack(Combatant target)
    {
        if (target == null || !target.IsAlive || !IsAlive) return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
            {
                Debug.Log($"<color=orange>{name} attacking {target.name} for {attackDamage} damage!</color>");
                target.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
            }
        }
    }
}

/// <summary>
/// Represents an enemy unit. Simple for this example, primarily for identification
/// by the AITacticalManager and basic combat.
/// </summary>
public class EnemyAgent : Combatant
{
    [Header("Enemy Specific")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Color aliveColor = Color.red;
    [SerializeField] private Color deadColor = new Color(0.5f, 0, 0, 0.5f); // Dark red, semi-transparent

    protected override void Awake()
    {
        base.Awake();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) Debug.LogError($"{name}: MeshRenderer not found!");
        }
        UpdateVisuals();
    }

    protected override void Die()
    {
        Debug.Log($"<color=red>{name} has been defeated!</color>");
        // Disable component/gameObject instead of destroying to keep reference for manager temporarily.
        // This allows the TacticalManager to re-evaluate without immediate null references.
        // In a real game, you might pool or destroy it after a delay.
        if (meshRenderer != null) meshRenderer.material.color = deadColor;
        this.enabled = false; // Disable script logic
    }

    protected override void UpdateVisuals()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = IsAlive ? aliveColor : deadColor;
        }
    }

    // Enemy agents can have simple independent behavior if not being managed
    // For this example, they just stand there and take damage, but you could add
    // a basic "attack nearest friendly" logic here.
    void Update()
    {
        if (!IsAlive) return;

        // Example: Simple retaliatory attack for enemies (not managed by AITacticalManager)
        AIAgent[] friendlies = FindObjectsOfType<AIAgent>().Where(f => f.IsAlive).ToArray();
        if (friendlies.Length > 0)
        {
            AIAgent nearestFriendly = friendlies.OrderBy(f => Vector3.Distance(transform.position, f.transform.position)).FirstOrDefault();
            if (nearestFriendly != null)
            {
                // Simple movement towards target if not in range
                if (Vector3.Distance(transform.position, nearestFriendly.transform.position) > attackRange * 0.9f)
                {
                    Vector3 direction = (nearestFriendly.transform.position - transform.position).normalized;
                    transform.position += direction * moveSpeed * Time.deltaTime;
                }
                LookAt(nearestFriendly.transform.position);
                Attack(nearestFriendly);
            }
        }
    }

    /// <summary>
    /// Makes the agent look at a target position.
    /// </summary>
    /// <param name="targetPos">The position to look at.</param>
    private void LookAt(Vector3 targetPos)
    {
        Vector3 lookDirection = targetPos - transform.position;
        lookDirection.y = 0; // Keep agent upright
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
        }
    }
}


// --- 3. The Core AI Agent ---

/// <summary>
/// Represents an individual AI unit that receives orders from the AITacticalManager.
/// It interprets and executes its assigned role.
/// </summary>
public class AIAgent : Combatant
{
    [Header("AI Agent Specific")]
    [SerializeField] private UnitRole currentRole = UnitRole.None;
    [SerializeField] private Transform currentTarget; // Enemy target for attack roles
    [SerializeField] private Vector3 currentDestination; // Positional target for movement roles
    [SerializeField] private MeshRenderer meshRenderer;

    // Visual indicators for roles (optional, for debugging/clarity)
    [SerializeField] private Color defaultColor = Color.blue;
    [SerializeField] private Color attackerColor = Color.yellow;
    [SerializeField] private Color defenderColor = Color.green;
    [SerializeField] private Color flankerColor = Color.magenta;
    [SerializeField] private Color supportColor = Color.cyan;
    [SerializeField] private Color deadFriendlyColor = new Color(0, 0, 0.5f, 0.5f); // Dark blue, semi-transparent


    public UnitRole CurrentRole => currentRole;

    protected override void Awake()
    {
        base.Awake();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) Debug.LogError($"{name}: MeshRenderer not found!");
        }
        UpdateVisuals();
    }

    /// <summary>
    /// Sets the role, target, and destination for this AI agent.
    /// This is the primary way the AITacticalManager commands an individual unit.
    /// </summary>
    /// <param name="newRole">The new role to assign.</param>
    /// <param name="target">The primary enemy target for this role (can be null).</param>
    /// <param name="destination">The primary positional destination for this role (can be Vector3.zero).</param>
    public void SetRole(UnitRole newRole, Transform target = null, Vector3 destination = default)
    {
        if (!IsAlive) return;

        currentRole = newRole;
        currentTarget = target;
        currentDestination = destination;
        Debug.Log($"<color=lightblue>{name} assigned role: {newRole}, Target: {(target != null ? target.name : "None")}, Dest: {destination}</color>");
        UpdateVisuals();
    }

    /// <summary>
    /// Updates the agent's behavior based on its currently assigned role.
    /// This method is called by the agent itself in its Update loop.
    /// </summary>
    void Update()
    {
        if (!IsAlive) return;

        ExecuteRole();
    }

    /// <summary>
    /// Executes the behavior associated with the agent's current role.
    /// This is where the individual unit's AI logic resides for each role.
    /// </summary>
    private void ExecuteRole()
    {
        // If the agent has a target, and it's dead or invalid, clear the target
        if (currentTarget != null && (!currentTarget.gameObject.activeInHierarchy || !currentTarget.GetComponent<Combatant>().IsAlive))
        {
            currentTarget = null;
        }

        switch (currentRole)
        {
            case UnitRole.Attacker:
                HandleAttackerRole();
                break;
            case UnitRole.Defender:
                HandleDefenderRole();
                break;
            case UnitRole.Flanker:
                HandleFlankerRole();
                break;
            case UnitRole.Support:
                HandleSupportRole();
                break;
            case UnitRole.Scout:
                HandleScoutRole();
                break;
            case UnitRole.None:
            default:
                // Do nothing, or patrol / idle. For now, just idle.
                break;
        }
    }

    /// <summary>
    /// Logic for the Attacker role: move towards and attack the current target.
    /// </summary>
    private void HandleAttackerRole()
    {
        if (currentTarget != null)
        {
            MoveTowards(currentTarget.position);
            LookAt(currentTarget.position);
            Attack(currentTarget.GetComponent<Combatant>());
        }
        else
        {
            // If no specific target, perhaps acquire a new one (e.g., nearest enemy) or await new orders.
            // For this demo, we'll rely on the manager to assign a target.
            // Debug.Log($"{name} (Attacker): No current target. Idling.");
        }
    }

    /// <summary>
    /// Logic for the Defender role: move to and hold a specified position, engaging nearby enemies.
    /// </summary>
    private void HandleDefenderRole()
    {
        // If a specific defense position is set and not yet reached
        if (currentDestination != Vector3.zero && Vector3.Distance(transform.position, currentDestination) > 0.5f)
        {
            MoveTowards(currentDestination);
            LookAt(currentDestination); // Look at the target point while moving
        }
        else // Once at position, defend.
        {
            // Look for nearest enemy within a slightly extended awareness range.
            EnemyAgent nearestEnemy = FindNearestEnemy(); // Note: This is an expensive call for every agent in Update. For demo, it's fine.
            if (nearestEnemy != null && Vector3.Distance(transform.position, nearestEnemy.transform.position) <= attackRange * 1.5f)
            {
                currentTarget = nearestEnemy.transform; // Engage this enemy
                LookAt(nearestEnemy.transform.position);
                Attack(nearestEnemy);
            }
            else
            {
                // No immediate threat, hold position and look around or face a default direction.
                // Debug.Log($"{name} (Defender): Holding position at {currentDestination}.");
            }
        }
    }

    /// <summary>
    /// Logic for the Flanker role: first move to a flanking position, then engage the target.
    /// </summary>
    private void HandleFlankerRole()
    {
        // First, move to the flanking destination
        if (currentDestination != Vector3.zero && Vector3.Distance(transform.position, currentDestination) > 0.5f)
        {
            MoveTowards(currentDestination);
            LookAt(currentDestination); // Look towards the flanking point
        }
        else if (currentTarget != null) // Once at flanking position, engage the target
        {
            MoveTowards(currentTarget.position); // Could be a more complex "peek and shoot" behavior
            LookAt(currentTarget.position);
            Attack(currentTarget.GetComponent<Combatant>());
        }
        else
        {
            // Debug.Log($"{name} (Flanker): At flanking position but no target. Idling.");
        }
    }

    /// <summary>
    /// Logic for the Support role: e.g., heal the lowest health friendly unit.
    /// </summary>
    private void HandleSupportRole()
    {
        // Find the friendly unit with the lowest health that needs healing
        AIAgent lowestHealthFriendly = FindLowestHealthFriendly();
        // Heal if below 75% health threshold
        if (lowestHealthFriendly != null && lowestHealthFriendly.CurrentHealth < lowestHealthFriendly.MaxHealth * 0.75f)
        {
            MoveTowards(lowestHealthFriendly.transform.position);
            LookAt(lowestHealthFriendly.transform.position);
            // Simulate healing if within range and cooldown is ready
            if (Vector3.Distance(transform.position, lowestHealthFriendly.transform.position) <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                float healAmount = 20f;
                Debug.Log($"<color=cyan>{name} (Support) healing {lowestHealthFriendly.name} for {healAmount} HP.</color>");
                lowestHealthFriendly.TakeDamage(-healAmount); // Negative damage to increase health
                lastAttackTime = Time.time;
            }
        }
        else
        {
            // If no one needs healing, perhaps follow the main attackers or defend itself.
            // For simplicity, fall back to attacker behavior if no healing is needed.
            HandleAttackerRole();
        }
    }
    
    /// <summary>
    /// Logic for the Scout role: move to a designated point to gather information.
    /// </summary>
    private void HandleScoutRole()
    {
        // Example: Move to a far point to "scout" then return or patrol
        if (currentDestination != Vector3.zero && Vector3.Distance(transform.position, currentDestination) > 0.5f)
        {
            MoveTowards(currentDestination);
            LookAt(currentDestination);
        }
        else
        {
            // Scout reached destination, perhaps find new patrol point or report back.
            // For simplicity, just idle.
            // Debug.Log($"{name} (Scout): Reached scout point. Idling.");
        }
    }


    /// <summary>
    /// Basic movement towards a target position. In a real game, this would typically
    /// use a `NavMeshAgent` for sophisticated pathfinding around obstacles.
    /// </summary>
    /// <param name="targetPos">The target world position.</param>
    private void MoveTowards(Vector3 targetPos)
    {
        if (Vector3.Distance(transform.position, targetPos) > 0.1f) // Small threshold to avoid jitter
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Makes the agent look at a target position.
    /// </summary>
    /// <param name="targetPos">The position to look at.</param>
    private void LookAt(Vector3 targetPos)
    {
        Vector3 lookDirection = targetPos - transform.position;
        lookDirection.y = 0; // Keep agent upright
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
        }
    }


    /// <summary>
    /// Finds the nearest active enemy agent in the scene.
    /// (Note: In a full game, the TacticalManager or a dedicated sensor component
    /// would provide this information to avoid expensive `FindObjectsOfType` calls in `Update`.)
    /// </summary>
    private EnemyAgent FindNearestEnemy()
    {
        EnemyAgent[] enemies = FindObjectsOfType<EnemyAgent>().Where(e => e.IsAlive).ToArray();
        if (enemies.Length == 0) return null;

        EnemyAgent nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Finds the friendly agent with the lowest health (excluding itself).
    /// (Similar performance note as `FindNearestEnemy`).
    /// </summary>
    private AIAgent FindLowestHealthFriendly()
    {
        AIAgent[] friendlies = FindObjectsOfType<AIAgent>().Where(a => a.IsAlive && a != this).ToArray();
        if (friendlies.Length == 0) return null;

        AIAgent lowest = null;
        float minHealthRatio = float.MaxValue;

        foreach (var friendly in friendlies)
        {
            float healthRatio = friendly.CurrentHealth / friendly.MaxHealth;
            if (healthRatio < minHealthRatio)
            {
                minHealthRatio = healthRatio;
                lowest = friendly;
            }
        }
        return lowest;
    }


    protected override void Die()
    {
        Debug.Log($"<color=blue>{name} (Friendly) has fallen!</color>");
        // Disable component/gameObject instead of destroying to keep reference for manager temporarily.
        // In a real game, you might notify the TacticalManager explicitly or remove from its lists.
        if (meshRenderer != null) meshRenderer.material.color = deadFriendlyColor;
        this.enabled = false; // Disable script logic
        currentRole = UnitRole.None; // Clear role
        // Optionally, disable collider, stop animations, etc.
    }

    /// <summary>
    /// Updates the visual representation of the agent based on its current role and health.
    /// </summary>
    protected override void UpdateVisuals()
    {
        if (meshRenderer == null) return;

        if (!IsAlive)
        {
            meshRenderer.material.color = deadFriendlyColor;
            return;
        }

        switch (currentRole)
        {
            case UnitRole.Attacker: meshRenderer.material.color = attackerColor; break;
            case UnitRole.Defender: meshRenderer.material.color = defenderColor; break;
            case UnitRole.Flanker: meshRenderer.material.color = flankerColor; break;
            case UnitRole.Support: meshRenderer.material.color = supportColor; break;
            case UnitRole.Scout: meshRenderer.material.color = defaultColor; break; // Scouts can use default or a specific color
            case UnitRole.None:
            default: meshRenderer.material.color = defaultColor; break;
        }
    }
}


// --- 4. The AITacticalManager (The Heart of the Pattern) ---

/// <summary>
/// <para>
/// **AITacticalSystem Design Pattern Example: Centralized AI Coordination**
/// </para>
/// <para>
/// This script represents the 'AITacticalManager' - the central brain
/// responsible for evaluating the overall battlefield situation, setting strategic
/// goals, and assigning specific roles and targets to individual AI units
/// (`AIAgent`s).
/// </para>
/// <para>
/// **How the AITacticalSystem Pattern Works:**
/// 1.  **Perception:** The Manager gathers information about the game world
///     (friendly unit status, enemy positions, objectives, cover points, etc.).
///     In this example, it directly observes `AIAgent` and `EnemyAgent` lists.
///     For performance, these should be cached and updated efficiently.
/// 2.  **Strategic Goal Setting:** Based on its perception, the Manager determines
///     the most appropriate overarching `TacticalGoal` (e.g., `EngageAllEnemies`,
///     `DefendPosition`, `FlankEnemy`). This is a high-level strategic decision.
/// 3.  **Role Assignment:** Once a strategic goal is chosen, the Manager
///     assigns specific `UnitRole`s (e.g., `Attacker`, `Defender`, `Flanker`, `Support`)
///     to its individual `AIAgent`s. It also provides context, such as a
///     specific `target` (enemy) or `destination` (position), required for that role.
///     This distributes the execution details to the units while maintaining
///     centralized control over the overall plan.
/// 4.  **Unit Execution:** Each `AIAgent` then independently executes the
///     behavior associated with its assigned `UnitRole`. The agent does not
///     decide the overall strategy; it only follows the orders given by the Manager.
/// 5.  **Iteration:** The Manager continuously re-evaluates the situation and
///     updates goals and unit roles as the battlefield changes.
/// </para>
/// <para>
/// **Benefits:**
/// -   **Centralized Control:** Easy to manage complex AI behaviors from a single point.
/// -   **Adaptability:** The system can quickly adapt to changing situations by
///     reassigning roles and goals.
/// -   **Scalability:** New unit roles or tactical goals can be added without
///     significant changes to individual AI agents, as long as the manager
///     knows how to assign them.
/// -   **Modularity:** Separates strategic decision-making (Manager) from
///     tactical execution (Agents).
/// -   **Debugging:** Easier to understand why units are doing what they are doing,
///     as decisions flow from a single source.
/// </para>
/// </summary>
public class AITacticalManager : MonoBehaviour
{
    [Header("Tactical System Setup")]
    [SerializeField] private List<AIAgent> friendlyUnits = new List<AIAgent>();
    [SerializeField] private List<EnemyAgent> enemyUnits = new List<EnemyAgent>();
    [SerializeField] private Transform defensePosition; // An optional point to defend
    [SerializeField] private float tacticalDecisionInterval = 2.0f; // How often the manager re-evaluates
    [SerializeField] private float flankDistance = 10f; // Distance an enemy needs to be from others to be considered flankable

    private TacticalGoal currentOverallGoal = TacticalGoal.EngageAllEnemies;
    private float lastDecisionTime;

    // Properties to expose active units for decision making, filtering out dead units.
    private List<AIAgent> ActiveFriendlyUnits => friendlyUnits.Where(u => u != null && u.IsAlive).ToList();
    private List<EnemyAgent> ActiveEnemyUnits => enemyUnits.Where(e => e != null && e.IsAlive).ToList();

    void Start()
    {
        // Initial setup: If lists are empty, try to find them in the scene.
        // Manual assignment in Inspector is generally preferred for explicit control.
        if (friendlyUnits.Count == 0)
        {
            friendlyUnits = FindObjectsOfType<AIAgent>().ToList();
        }
        if (enemyUnits.Count == 0)
        {
            enemyUnits = FindObjectsOfType<EnemyAgent>().ToList();
        }

        Debug.Log($"<color=green>AITacticalManager Initialized. Friendly Units: {friendlyUnits.Count}, Enemy Units: {enemyUnits.Count}</color>");

        lastDecisionTime = -tacticalDecisionInterval; // Make it decide immediately on start
    }

    void Update()
    {
        // Only make decisions at a set interval to prevent excessive processing.
        // This is crucial for performance in complex scenarios.
        if (Time.time >= lastDecisionTime + tacticalDecisionInterval)
        {
            EvaluateAndDecide();
            lastDecisionTime = Time.time;
        }

        // Check for victory/defeat conditions.
        // These can trigger game state changes or scene transitions.
        if (ActiveEnemyUnits.Count == 0 && ActiveFriendlyUnits.Count > 0)
        {
            Debug.Log("<color=lime>Tactical Manager: All enemies defeated! Victory!</color>");
            this.enabled = false; // Stop managing AI
        }
        else if (ActiveFriendlyUnits.Count == 0 && ActiveEnemyUnits.Count > 0)
        {
            Debug.Log("<color=red>Tactical Manager: All friendly units defeated! Defeat!</color>");
            this.enabled = false; // Stop managing AI
        }
    }

    /// <summary>
    /// The main decision-making loop of the Tactical Manager.
    /// It perceives the battlefield, evaluates the situation, chooses a strategic goal,
    /// and then assigns appropriate roles to individual friendly units.
    /// </summary>
    private void EvaluateAndDecide()
    {
        Debug.Log("--- <color=yellow>Tactical Manager Re-evaluating Situation</color> ---");

        List<AIAgent> activeFriendlies = ActiveFriendlyUnits;
        List<EnemyAgent> activeEnemies = ActiveEnemyUnits;

        if (activeFriendlies.Count == 0 || activeEnemies.Count == 0)
        {
            // No need to decide if one side is wiped out, handled in Update.
            return;
        }

        // 1. Perception & Situation Assessment
        // This is where you would gather more sophisticated data:
        // - Unit positions, health, ammo, line of sight
        // - Cover points, objectives, chokepoints, danger zones
        // - Enemy formations, threat levels (e.g., strong vs. weak enemies)
        int numFriendlies = activeFriendlies.Count;
        int numEnemies = activeEnemies.Count;
        float friendlyAvgHealth = activeFriendlies.Average(u => u.CurrentHealth / u.MaxHealth);

        // Identify a potential isolated enemy for flanking
        EnemyAgent flankTarget = GetFlankableEnemy(activeEnemies);

        // Identify a friendly unit that needs healing
        // Order by health ratio to find the most critically wounded.
        AIAgent friendlyInNeedOfHealing = activeFriendlies.OrderBy(u => u.CurrentHealth / u.MaxHealth).FirstOrDefault();
        bool needsHealing = friendlyInNeedOfHealing != null && friendlyInNeedOfHealing.CurrentHealth < friendlyInNeedOfHealing.MaxHealth * 0.5f; // Below 50% health

        // Check if there is a 'Support' type unit available to heal
        bool hasSupportUnit = activeFriendlies.Any(u => u.CurrentRole == UnitRole.Support || u.CurrentRole == UnitRole.None); // Consider available units as potential support


        // 2. Choose Overall Tactical Goal
        ChooseOverallGoal(numFriendlies, numEnemies, friendlyAvgHealth, flankTarget, needsHealing, hasSupportUnit);

        // 3. Assign Unit Roles based on the chosen goal
        AssignUnitRoles(activeFriendlies, activeEnemies, flankTarget, friendlyInNeedOfHealing);

        Debug.Log($"<color=white>Current Overall Goal: {currentOverallGoal}</color>");
    }

    /// <summary>
    /// Determines the overarching strategic goal based on battlefield conditions.
    /// This logic can be as simple or as complex as needed (e.g., using utility AI or behavior trees).
    /// </summary>
    private void ChooseOverallGoal(int numFriendlies, int numEnemies, float friendlyAvgHealth, EnemyAgent flankTarget, bool needsHealing, bool hasSupportUnit)
    {
        // Priority-based decision logic (higher priority checks first)
        if (needsHealing && hasSupportUnit && friendlyUnits.Any(u => u.CurrentRole == UnitRole.Support))
        {
            // Only prioritize healing if a dedicated support unit exists AND healing is needed.
            currentOverallGoal = TacticalGoal.HealFriendly;
        }
        else if (flankTarget != null && numFriendlies >= 2 && numEnemies > 1) // Need at least two units to flank effectively (one to flank, one to engage)
        {
            currentOverallGoal = TacticalGoal.FlankEnemy;
        }
        else if (friendlyAvgHealth < 0.4f && numEnemies >= numFriendlies && defensePosition != null) // If friendlies are low on health and outnumbered
        {
            currentOverallGoal = TacticalGoal.DefendPosition;
        }
        else // Default or fallback to engaging all enemies
        {
            currentOverallGoal = TacticalGoal.EngageAllEnemies;
        }
    }

    /// <summary>
    /// Assigns specific roles and targets/destinations to each active friendly unit
    /// based on the current overall tactical goal.
    /// </summary>
    private void AssignUnitRoles(List<AIAgent> activeFriendlies, List<EnemyAgent> activeEnemies, EnemyAgent flankTarget, AIAgent friendlyInNeedOfHealing)
    {
        // First, reset all units to 'None' or default behavior before reassigning.
        // This ensures units don't stick to old orders if roles change.
        foreach (var unit in activeFriendlies)
        {
            unit.SetRole(UnitRole.None);
        }

        switch (currentOverallGoal)
        {
            case TacticalGoal.EngageAllEnemies:
                // Assign all available units as attackers, targeting the closest enemy or cycling through.
                for (int i = 0; i < activeFriendlies.Count; i++)
                {
                    AIAgent unit = activeFriendlies[i];
                    if (unit.IsAlive)
                    {
                        // Assign closest active enemy or cycle through them
                        EnemyAgent targetEnemy = activeEnemies.OrderBy(e => Vector3.Distance(unit.transform.position, e.transform.position)).FirstOrDefault();
                        unit.SetRole(UnitRole.Attacker, targetEnemy?.transform);
                    }
                }
                break;

            case TacticalGoal.DefendPosition:
                if (defensePosition == null)
                {
                    Debug.LogWarning("Tactical Goal is DefendPosition, but no defensePosition is set! Falling back to EngageAllEnemies.");
                    goto case TacticalGoal.EngageAllEnemies; // Fallback to engaging enemies
                }
                // Assign all units to defend the specified position, possibly spreading out.
                for (int i = 0; i < activeFriendlies.Count; i++)
                {
                    AIAgent unit = activeFriendlies[i];
                    if (unit.IsAlive)
                    {
                        // Calculate a spread-out position around the defense point.
                        Vector3 specificDefensePoint = defensePosition.position + (Vector3)(Quaternion.Euler(0, i * (360f / activeFriendlies.Count), 0) * Vector3.forward * 2f);
                        unit.SetRole(UnitRole.Defender, null, specificDefensePoint);
                    }
                }
                break;

            case TacticalGoal.FlankEnemy:
                // Ensure there's a valid flank target and enough units to execute.
                if (flankTarget == null || activeFriendlies.Count < 2)
                {
                    Debug.LogWarning("Tactical Goal is FlankEnemy, but no valid flank target or not enough units! Falling back to EngageAllEnemies.");
                    goto case TacticalGoal.EngageAllEnemies; // Fallback
                }

                // Assign one unit to flank, others to engage the target directly.
                AIAgent flanker = activeFriendlies.OrderBy(u => Random.value).FirstOrDefault(); // Pick a random available unit for flanking
                if (flanker != null)
                {
                    // Calculate a flanking position relative to the target and a friendly unit.
                    Vector3 friendlyOrigin = activeFriendlies.First(u => u != flanker && u.IsAlive).transform.position; // Use another friendly's position as a reference
                    Vector3 flankPosition = GetFlankPosition(flankTarget.transform.position, friendlyOrigin);
                    flanker.SetRole(UnitRole.Flanker, flankTarget.transform, flankPosition);

                    // Other units engage the flank target directly or the nearest enemy.
                    foreach (AIAgent unit in activeFriendlies)
                    {
                        if (unit.IsAlive && unit != flanker)
                        {
                            unit.SetRole(UnitRole.Attacker, flankTarget.transform);
                        }
                    }
                }
                break;

            case TacticalGoal.HealFriendly:
                // Ensure there's a friendly needing healing and a unit capable of supporting.
                if (friendlyInNeedOfHealing == null)
                {
                    Debug.LogWarning("Tactical Goal is HealFriendly, but no friendly unit needs healing! Falling back to EngageAllEnemies.");
                    goto case TacticalGoal.EngageAllEnemies; // Fallback
                }

                // Try to find a dedicated support unit, or assign any available unit if no specialized one.
                AIAgent healer = activeFriendlies.FirstOrDefault(u => u.CurrentRole == UnitRole.Support) ??
                                 activeFriendlies.FirstOrDefault(u => u.CurrentRole == UnitRole.None); // If no dedicated support, pick an idle one
                if (healer == null) healer = activeFriendlies.OrderBy(u => Random.value).FirstOrDefault(); // If still no one, just pick a random unit

                if (healer != null)
                {
                    healer.SetRole(UnitRole.Support, friendlyInNeedOfHealing.transform);
                    // Other units continue attacking enemies.
                    foreach (AIAgent unit in activeFriendlies)
                    {
                        if (unit.IsAlive && unit != healer)
                        {
                            EnemyAgent targetEnemy = activeEnemies.OrderBy(e => Vector3.Distance(unit.transform.position, e.transform.position)).FirstOrDefault();
                            unit.SetRole(UnitRole.Attacker, targetEnemy?.transform);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Tactical Goal is HealFriendly, but no units available to heal! Falling back to EngageAllEnemies.");
                    goto case TacticalGoal.EngageAllEnemies; // Fallback
                }
                break;

            case TacticalGoal.Retreat:
                // For a simple retreat, units would move to a predefined safe point.
                // Not fully implemented for demo, but would involve setting a destination far away.
                // For example:
                // foreach (AIAgent unit in activeFriendlies)
                // {
                //     unit.SetRole(UnitRole.Retreat, null, safeRetreatPoint.position);
                // }
                break;
        }
    }

    /// <summary>
    /// Tries to find an enemy that is relatively isolated and can be flanked.
    /// An enemy is considered "flankable" if no other enemy is within `flankDistance`.
    /// </summary>
    private EnemyAgent GetFlankableEnemy(List<EnemyAgent> currentEnemies)
    {
        if (currentEnemies.Count < 2) return null; // Need at least two enemies to determine isolation effectively

        foreach (var potentialFlankTarget in currentEnemies)
        {
            bool isIsolated = true;
            foreach (var otherEnemy in currentEnemies)
            {
                if (potentialFlankTarget != otherEnemy && otherEnemy.IsAlive)
                {
                    // If another active enemy is too close, the target is not isolated.
                    if (Vector3.Distance(potentialFlankTarget.transform.position, otherEnemy.transform.position) < flankDistance)
                    {
                        isIsolated = false;
                        break;
                    }
                }
            }

            if (isIsolated)
            {
                return potentialFlankTarget; // Found an isolated enemy.
            }
        }
        return null; // No flankable enemy found.
    }

    /// <summary>
    /// Calculates a simple flanking position relative to the target and a friendly unit.
    /// This creates a position to the side of the direct line of attack.
    /// </summary>
    /// <param name="targetPos">The position of the enemy to flank.</param>
    /// <param name="friendlyOrigin">A friendly unit's position, used as a reference to define "front".</param>
    /// <returns>A world position suitable for flanking.</returns>
    private Vector3 GetFlankPosition(Vector3 targetPos, Vector3 friendlyOrigin)
    {
        Vector3 toTarget = (targetPos - friendlyOrigin).normalized;
        // Get a vector perpendicular to the forward direction on the ground plane.
        Vector3 perpendicular = Vector3.Cross(toTarget, Vector3.up); 
        // Move to the side of the target.
        return targetPos + perpendicular * (flankDistance / 2f); 
    }

    /// <summary>
    /// Gizmos for visualizing the defense position and flankable enemies in the editor.
    /// These are helpful for setting up and debugging the tactical logic.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw defense position
        if (defensePosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(defensePosition.position, 3f);
            // Gizmos.DrawIcon(defensePosition.position + Vector3.up * 3f, "Assets/Gizmos/icon_defense.png", true); // Requires an icon if uncommented
            Gizmos.DrawLine(defensePosition.position, defensePosition.position + Vector3.up * 5f);
        }

        // Draw flankable enemy and flank position
        if (Application.isPlaying && currentOverallGoal == TacticalGoal.FlankEnemy)
        {
            EnemyAgent flankTarget = GetFlankableEnemy(ActiveEnemyUnits);
            if (flankTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(flankTarget.transform.position, flankDistance / 2f);
                // Gizmos.DrawIcon(flankTarget.transform.position + Vector3.up * 3f, "Assets/Gizmos/icon_flank.png", true); // Requires an icon if uncommented
                Gizmos.DrawLine(flankTarget.transform.position, flankTarget.transform.position + Vector3.up * 5f);

                // Show the calculated flank position
                if (ActiveFriendlyUnits.Count > 1)
                {
                    // Pick one friendly as reference (could be the assigned flanker, or just the closest friendly)
                    Vector3 friendlyOrigin = ActiveFriendlyUnits.First().transform.position; 
                    Vector3 flankPos = GetFlankPosition(flankTarget.transform.position, friendlyOrigin);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(flankPos, 0.5f);
                    Gizmos.DrawLine(flankTarget.transform.position, flankPos);
                }
            }
        }
    }
}
```

---

### How to Use in Unity (Example Setup)

To get this example running in your Unity project:

1.  **Create New C# Scripts:**
    *   Create a new C# script named `Combatant.cs` and paste the `Combatant` abstract class code into it.
    *   Create `EnemyAgent.cs` and paste the `EnemyAgent` class code.
    *   Create `AIAgent.cs` and paste the `AIAgent` class code.
    *   Create `AITacticalManager.cs` and paste the `AITacticalManager` class code.
    *   *(Note: The enums `TacticalGoal` and `UnitRole` can reside in any of these files or their own dedicated file, but placing them before `Combatant` in `AITacticalManager.cs` as shown simplifies the structure for this single-file demonstration.)*

2.  **Scene Setup:**

    *   **AITacticalManager GameObject:**
        *   Create an empty GameObject in your scene (e.g., right-click in Hierarchy -> `Create Empty`). Name it `AITacticalSystemManager`.
        *   Drag and drop the `AITacticalManager.cs` script onto this GameObject in the Inspector.

    *   **Friendly Units:**
        *   Create several 3D objects (e.g., `3D Object -> Cube` or `Capsule`). Name them like `Friendly_01`, `Friendly_02`, `Friendly_03`.
        *   Position them relatively close together.
        *   Drag and drop the `AIAgent.cs` script onto *each* of these friendly units.
        *   (Optional but Recommended): On each `AIAgent`, assign a distinct material/color to its `Mesh Renderer` (e.g., blue) to differentiate it from enemies. The script also changes colors based on role.

    *   **Enemy Units:**
        *   Create several more 3D objects (e.g., Cubes/Capsules). Name them like `Enemy_01`, `Enemy_02`, `Enemy_03`.
        *   Position them at a distance from your friendly units, simulating an encounter.
        *   Drag and drop the `EnemyAgent.cs` script onto *each* of these enemy units.
        *   (Optional but Recommended): On each `EnemyAgent`, assign a distinct material/color (e.g., red).

    *   **Defense Position (Optional):**
        *   Create an empty GameObject (e.g., `Create Empty`). Name it `DefensePoint`.
        *   Position it somewhere on your map where it makes sense for units to defend (e.g., behind some cover).
        *   Select your `AITacticalSystemManager` GameObject. In its Inspector, drag your `DefensePoint` GameObject into the `Defense Position` slot.

3.  **Assign Units to Manager:**

    *   Select the `AITacticalSystemManager` GameObject.
    *   In its Inspector, find the `Friendly Units` list. Drag all your `Friendly_XX` GameObjects from the Hierarchy into this list.
    *   Find the `Enemy Units` list. Drag all your `Enemy_XX` GameObjects from the Hierarchy into this list.
    *   *(Note: If you leave these lists empty, the `AITacticalManager` will attempt to find all `AIAgent` and `EnemyAgent` components in the scene automatically on `Start()`. Manually assigning is generally more reliable and explicit for larger projects.)*

4.  **Adjust Settings (Optional):**

    *   On the `AITacticalSystemManager`, you can adjust `Tactical Decision Interval` (e.g., `1.0` to `3.0` seconds) to control how often the manager makes new decisions.
    *   On `AIAgent` and `EnemyAgent` scripts, adjust `Move Speed`, `Attack Range`, `Attack Damage`, `Max Health`, `Attack Cooldown` as needed to balance the combat.

5.  **Run the Scene:**

    *   Press the Play button in the Unity editor.
    *   Observe the units moving, attacking, and changing colors (indicating their assigned roles).
    *   Watch the Console for detailed logs from the `AITacticalManager` about its strategic decisions and unit assignments.
    *   **Experiment:** Try deleting some enemy units at runtime, or manually reducing friendly unit health to see how the manager adapts its strategy (e.g., switching to `FlankEnemy`, `DefendPosition`, or `HealFriendly`).

This setup provides a solid foundation for understanding and expanding upon the AITacticalSystem design pattern in your Unity games.