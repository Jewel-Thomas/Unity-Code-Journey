// Unity Design Pattern Example: StealthKillSystem
// This script demonstrates the StealthKillSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the 'StealthKillSystem' design pattern in Unity, focusing on the **Strategy pattern** to manage different stealth kill criteria and execution methods. This makes the system highly flexible and extensible.

The core idea is to decouple:
1.  **What conditions make a stealth kill possible?** (Criteria Strategies)
2.  **How is a stealth kill performed?** (Performer Strategies)
3.  **The system that orchestrates these parts.** (Context - `StealthKillSystem`)

---

### **Unity Project Setup Steps:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a C# Script:** Name it `StealthKillSystem.cs` and replace its content with the code below.
3.  **Create a Player GameObject:**
    *   Create a 3D Object (e.g., `Capsule`). Name it `Player`.
    *   Add a `CharacterController` component to it.
    *   Add the `PlayerController` script to it.
    *   (Optional but Recommended) Set its Tag to "Player".
4.  **Create an Enemy GameObject:**
    *   Create a 3D Object (e.g., `Capsule`). Name it `Enemy_01`.
    *   Add a `CapsuleCollider` component (make sure `Is Trigger` is **unchecked** for proper CharacterController interaction, or use a separate physics layer).
    *   Add the `EnemyController` script to it.
    *   In the Inspector for `Enemy_01`, drag your `Player` GameObject into the `Player Transform` slot.
    *   Duplicate `Enemy_01` a few times (e.g., `Enemy_02`, `Enemy_03`) and place them around the scene.
5.  **Create a StealthKillSystem Manager:**
    *   Create an Empty GameObject. Name it `StealthKillManager`.
    *   Add the `StealthKillSystem` script to it.
6.  **Configure in the Inspector:**
    *   **StealthKillManager (StealthKillSystem script):**
        *   **Detection Range:** e.g., `2` (Player must be within this distance).
        *   **Detection Angle:** e.g., `90` (Player must be within this angle behind the enemy, e.g., 45 degrees left/right of enemy's backward vector).
        *   **Stealth Kill Criteria:**
            *   Click the '+' to add two criteria.
            *   For the first slot, select `EnemyUnawareCriteria`.
            *   For the second slot, select `BehindEnemyCriteria`.
            *   (Optional: Try `TargetVulnerableCriteria` or `CompositeStealthKillCriteria` for more advanced setups).
        *   **Stealth Kill Performer:**
            *   Select `DefaultStealthKillPerformer`.
            *   (Optional: Try `ChokeOutPerformer` for a non-lethal kill).
    *   **Player (PlayerController script):**
        *   Drag your `StealthKillManager` GameObject into the `Stealth Kill System` slot.
        *   (Optional) Set `Stealth Kill Key` to 'E' (or your preferred key).
7.  **Run the Scene:**
    *   Use WASD to move the player.
    *   Approach an enemy from behind while it's **unaware** (watch its debug logs or its green gizmo cone).
    *   Press the `Stealth Kill Key` (default 'E').
    *   Observe the debug logs and the enemy disappearing.

---

### **C# Unity Script: `StealthKillSystem.cs`**

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List<T> or array of strategies

// --- Core Pattern: StealthKillSystem ---

/// <summary>
/// The main StealthKillSystem component. This acts as the **Context** for the Strategy pattern,
/// managing the criteria for a stealth kill and executing it if conditions are met.
/// It orchestrates the checking of conditions (via IStealthKillCriteria) and
/// the execution of the kill (via IStealthKillPerformer).
/// </summary>
public class StealthKillSystem : MonoBehaviour
{
    [Header("Stealth Kill Setup")]
    [Tooltip("The maximum distance from the player to the enemy for a stealth kill to be possible.")]
    [SerializeField] private float detectionRange = 2.0f;
    [Tooltip("The total field of view angle *behind* the enemy. A stealth kill is only possible " +
             "if the player is within this angle relative to the enemy's backward direction. " +
             "e.g., 90 means 45 degrees left and 45 degrees right of the enemy's back.")]
    [Range(0, 180)]
    [SerializeField] private float detectionAngle = 90f;

    [Header("Strategy Implementations")]
    // [SerializeReference] is ideal for serializing interfaces/abstract classes directly in the Inspector (Unity 2021.2+).
    // For broader compatibility or simpler setup, we use [System.Serializable] on the abstract base classes
    // and rely on Unity's polymorphic serialization for the array.
    [Tooltip("The concrete criteria strategies used to determine if a stealth kill is possible. " +
             "All criteria in this list must pass for a stealth kill to be valid (AND logic).")]
    [SerializeReference] // Allows selection of derived types in Inspector without custom editor
    private List<StealthKillCriteriaBase> stealthKillCriteria = new List<StealthKillCriteriaBase>();

    [Tooltip("The concrete performer strategy used to execute the stealth kill.")]
    [SerializeReference] // Allows selection of derived types in Inspector
    private StealthKillPerformerBase stealthKillPerformer;

    private bool isPerformingKill = false;

    // A getter for the detection range, useful for player to know how close to check
    public float DetectionRange => detectionRange;

    /// <summary>
    /// Ensures that default strategies are assigned if none are set in the Inspector.
    /// This makes the system more robust for out-of-the-box usage.
    /// </summary>
    void OnValidate()
    {
        if (stealthKillPerformer == null)
        {
            stealthKillPerformer = new DefaultStealthKillPerformer();
            Debug.LogWarning("StealthKillPerformer not assigned. A 'DefaultStealthKillPerformer' has been assigned.", this);
        }

        if (stealthKillCriteria == null || stealthKillCriteria.Count == 0)
        {
            stealthKillCriteria = new List<StealthKillCriteriaBase>
            {
                new EnemyUnawareCriteria(),
                new BehindEnemyCriteria()
            };
            Debug.LogWarning("StealthKillCriteria not assigned. Default criteria (EnemyUnawareCriteria, BehindEnemyCriteria) have been assigned.", this);
        }
    }

    /// <summary>
    /// Attempts to perform a stealth kill. This is the main entry point for a player controller
    /// or AI to initiate a stealth kill attempt. It first checks general spatial conditions,
    /// then delegates to the configured criteria strategies, and finally to the performer strategy.
    /// </summary>
    /// <param name="initiator">The GameObject performing the kill (e.g., the player).</param>
    /// <param name="target">The GameObject being targeted for the kill (e.g., an enemy).</param>
    /// <returns>True if the kill was initiated successfully, false otherwise.</returns>
    public bool TryPerformStealthKill(GameObject initiator, GameObject target)
    {
        if (isPerformingKill)
        {
            Debug.Log("StealthKillSystem: Already performing a stealth kill. Please wait.", this);
            return false;
        }

        if (initiator == null || target == null)
        {
            Debug.LogError("StealthKillSystem: Initiator or target is null. Cannot perform stealth kill.", this);
            return false;
        }

        // --- 1. Check if the target is a valid killable enemy ---
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsKillable)
        {
            // Debug.Log($"StealthKillSystem: Target {target.name} is not a valid killable enemy.", this);
            return false;
        }

        // --- 2. Check general spatial/directional criteria ---
        // These are often common to all stealth kills and can be managed by the system.
        // More complex spatial checks (e.g., line of sight, collider checks) could be moved
        // into a dedicated criteria strategy if needed.
        float distance = Vector3.Distance(initiator.transform.position, target.transform.position);
        if (distance > detectionRange)
        {
            // Debug.Log($"StealthKillSystem: Player too far from {target.name}. Distance: {distance}, Required: <{detectionRange}", this);
            return false;
        }

        // Check if player is behind the enemy within the detection angle
        Vector3 directionFromEnemyToPlayer = (initiator.transform.position - target.transform.position).normalized;
        // Angle from enemy's *back* vector to the direction vector pointing at player
        float angleToEnemyBack = Vector3.Angle(directionFromEnemyToPlayer, -target.transform.forward);
        if (angleToEnemyBack > detectionAngle / 2f) // Half angle because detectionAngle is total spread
        {
            // Debug.Log($"StealthKillSystem: Player not behind {target.name} enough. Angle: {angleToEnemyBack}, Required: <{detectionAngle / 2f}", this);
            return false;
        }

        // --- 3. Apply the Strategy Pattern: Check specific criteria ---
        // Iterate through all assigned criteria. If any one fails, the kill is not possible.
        foreach (var criteria in stealthKillCriteria)
        {
            if (criteria == null)
            {
                Debug.LogWarning("StealthKillSystem: A StealthKillCriteria slot is null. Skipping.", this);
                continue;
            }
            if (!criteria.CheckCriteria(initiator, target))
            {
                Debug.Log($"StealthKillSystem: Criteria '{criteria.GetType().Name}' failed for {target.name}.", this);
                return false;
            }
        }

        // --- 4. All criteria met, perform the kill using the IStealthKillPerformer strategy ---
        Debug.Log($"StealthKillSystem: All criteria met. Initiating stealth kill on {target.name} by {initiator.name}!", this);
        StartCoroutine(PerformKillSequence(initiator, enemy));
        return true;
    }

    /// <summary>
    /// Coroutine to manage the kill sequence, including potential animation delays.
    /// It delegates the actual kill execution to the chosen `StealthKillPerformerBase`.
    /// </summary>
    /// <param name="initiator">The GameObject performing the kill.</param>
    /// <param name="enemy">The EnemyController being killed.</param>
    private IEnumerator PerformKillSequence(GameObject initiator, EnemyController enemy)
    {
        isPerformingKill = true;

        // Temporarily disable input for player or trigger kill animation state
        PlayerController player = initiator.GetComponent<PlayerController>();
        if (player != null) player.DisableInput();

        // Inform the enemy it's being killed, allowing it to react (e.g., freeze AI, play victim animation)
        enemy.OnKillStarted();

        // Execute the actual kill logic via the chosen strategy
        // This might involve playing animations, waiting for them to finish, applying damage, etc.
        yield return stealthKillPerformer.ExecuteKill(initiator, enemy);

        // Kill sequence finished
        if (player != null) player.EnableInput();
        isPerformingKill = false;
        Debug.Log($"StealthKillSystem: Stealth kill sequence finished for {enemy.gameObject.name}.", this);
    }

    /// <summary>
    /// Visualizes the detection range for the stealth kill in the editor.
    /// This gizmo shows the sphere around the StealthKillSystem's location (e.g., player or manager).
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Visualize the detection range around the object this script is attached to (e.g., the Player or Manager)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

// --- Strategy Interface/Abstract Base for Stealth Kill Criteria ---

/// <summary>
/// Abstract base class for defining various criteria that must be met for a stealth kill to be possible.
/// This allows for different conditions to be plugged in without modifying the core StealthKillSystem.
/// This acts as the **Strategy** interface/abstract class for criteria.
/// </summary>
[System.Serializable] // Make it serializable so it can be exposed in the Inspector
public abstract class StealthKillCriteriaBase
{
    /// <summary>
    /// Checks if the specific criteria are met for a stealth kill.
    /// </summary>
    /// <param name="initiator">The GameObject attempting the kill (e.g., player).</param>
    /// <param name="target">The GameObject being targeted (e.g., enemy).</param>
    /// <returns>True if the criteria are met, false otherwise.</returns>
    public abstract bool CheckCriteria(GameObject initiator, GameObject target);
}

// --- Concrete Strategies for Stealth Kill Criteria ---

/// <summary>
/// Concrete Strategy: Checks if the target is unaware of the initiator.
/// Requires the target to have an EnemyController component.
/// </summary>
[System.Serializable]
public class EnemyUnawareCriteria : StealthKillCriteriaBase
{
    public override bool CheckCriteria(GameObject initiator, GameObject target)
    {
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy == null)
        {
            Debug.LogWarning($"EnemyUnawareCriteria: Target {target.name} does not have an EnemyController. Cannot check awareness.");
            return false;
        }
        return !enemy.IsAwareOf(initiator);
    }
}

/// <summary>
/// Concrete Strategy: Ensures the target has an EnemyController. The main StealthKillSystem
/// typically handles the precise angle/distance checks, but this can ensure the target is of a type
/// that can be "behind" or add more complex positional checks if needed.
/// </summary>
[System.Serializable]
public class BehindEnemyCriteria : StealthKillCriteriaBase
{
    public override bool CheckCriteria(GameObject initiator, GameObject target)
    {
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy == null)
        {
            Debug.LogWarning($"BehindEnemyCriteria: Target {target.name} does not have an EnemyController. Cannot check 'behindness'.");
            return false;
        }
        // The StealthKillSystem already performs the general angle check.
        // This criteria could add more specific 'behind' logic, e.g., line of sight check from enemy's 'eyes'.
        return true; // Assume the main system handles the spatial aspect if no specific logic here
    }
}

/// <summary>
/// Concrete Strategy: Checks if the target is currently in a vulnerable state (e.g., sleeping, stunned).
/// Requires the target to have an EnemyController component.
/// </summary>
[System.Serializable]
public class TargetVulnerableCriteria : StealthKillCriteriaBase
{
    public override bool CheckCriteria(GameObject initiator, GameObject target)
    {
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy == null)
        {
            Debug.LogWarning($"TargetVulnerableCriteria: Target {target.name} does not have an EnemyController. Cannot check vulnerability.");
            return false;
        }
        return enemy.IsVulnerable; // Assume EnemyController has this property
    }
}

/// <summary>
/// Concrete Strategy: A composite criterion that combines multiple criteria.
/// All sub-criteria must pass for this composite criterion to pass (AND logic).
/// This demonstrates the Composite pattern combined with Strategy, adding more flexibility.
/// </summary>
[System.Serializable]
public class CompositeStealthKillCriteria : StealthKillCriteriaBase
{
    [SerializeReference] // Allows nested selection of derived types
    [Tooltip("List of sub-criteria that all must pass for this composite to pass.")]
    private List<StealthKillCriteriaBase> subCriteria = new List<StealthKillCriteriaBase>();

    public CompositeStealthKillCriteria(params StealthKillCriteriaBase[] criteria)
    {
        subCriteria = new List<StealthKillCriteriaBase>(criteria);
    }

    public override bool CheckCriteria(GameObject initiator, GameObject target)
    {
        if (subCriteria == null || subCriteria.Count == 0)
        {
            Debug.LogWarning("CompositeStealthKillCriteria has no sub-criteria assigned. Returning true by default.");
            return true;
        }

        foreach (var criteria in subCriteria)
        {
            if (criteria == null)
            {
                Debug.LogWarning("CompositeStealthKillCriteria contains a null sub-criterion. Skipping.");
                continue;
            }
            if (!criteria.CheckCriteria(initiator, target))
            {
                return false; // If any sub-criteria fails, the composite fails
            }
        }
        return true; // All sub-criteria passed
    }
}


// --- Strategy Interface/Abstract Base for Stealth Kill Performer ---

/// <summary>
/// Abstract base class for defining how a stealth kill is executed.
/// This allows for different kill animations, effects, or outcomes to be plugged in.
/// This acts as the **Strategy** interface/abstract class for the performer.
/// </summary>
[System.Serializable]
public abstract class StealthKillPerformerBase
{
    /// <summary>
    /// Executes the stealth kill sequence. This method is an IEnumerator to allow
    /// for asynchronous operations like waiting for animations to finish.
    /// </summary>
    /// <param name="initiator">The GameObject performing the kill (e.g., player).</param>
    /// <param name="target">The EnemyController being killed.</param>
    /// <returns>An IEnumerator for coroutine execution (e.g., for animation delays).</returns>
    public abstract IEnumerator ExecuteKill(GameObject initiator, EnemyController target);
}

// --- Concrete Strategies for Stealth Kill Performer ---

/// <summary>
/// Concrete Strategy: A basic stealth kill performer that simply disables the enemy
/// after a short simulated animation delay, effectively "killing" them.
/// </summary>
[System.Serializable]
public class DefaultStealthKillPerformer : StealthKillPerformerBase
{
    [Tooltip("Time in seconds to simulate the kill animation duration.")]
    [SerializeField] private float killAnimationDuration = 1.5f;

    public override IEnumerator ExecuteKill(GameObject initiator, EnemyController enemy)
    {
        Debug.Log($"DefaultStealthKillPerformer: Initiating lethal kill sequence on {enemy.gameObject.name}...", enemy.gameObject);
        // --- Here you would trigger actual animations on initiator and enemy ---
        // Example: initiator.GetComponent<Animator>().Play("StealthKillAnimation");
        // Example: enemy.GetComponent<Animator>().Play("StealthKillVictimAnimation");
        // Example: Hide specific parts of the enemy model for dismemberment

        yield return new WaitForSeconds(killAnimationDuration); // Simulate animation time

        // Apply damage/destroy enemy
        enemy.TakeDamage(1000); // Insta-kill
        Debug.Log($"DefaultStealthKillPerformer: {enemy.gameObject.name} has been killed.", enemy.gameObject);
    }
}

/// <summary>
/// Concrete Strategy: A stealth kill performer that might involve a specific "choke out" animation
/// and render the enemy unconscious rather than dead. This demonstrates a non-lethal outcome.
/// </summary>
[System.Serializable]
public class ChokeOutPerformer : StealthKillPerformerBase
{
    [Tooltip("Time in seconds to simulate the choke out animation duration.")]
    [SerializeField] private float chokeOutAnimationDuration = 3.0f;

    public override IEnumerator ExecuteKill(GameObject initiator, EnemyController enemy)
    {
        Debug.Log($"ChokeOutPerformer: Initiating choke out sequence on {enemy.gameObject.name}...", enemy.gameObject);
        // --- Trigger non-lethal animations ---
        // Example: initiator.GetComponent<Animator>().Play("ChokeOutAnimation");
        // Example: enemy.GetComponent<Animator>().Play("ChokeOutVictimAnimation");

        yield return new WaitForSeconds(chokeOutAnimationDuration); // Simulate animation time

        enemy.BecomeUnconscious(); // Assume EnemyController has this method for non-lethal takedowns
        Debug.Log($"ChokeOutPerformer: {enemy.gameObject.name} has been choked out and is unconscious.", enemy.gameObject);
    }
}


// --- Supporting Components (Player and Enemy) ---
// These are simplified for demonstration purposes to show how they interact with the StealthKillSystem.

/// <summary>
/// Simple Player Controller to simulate movement and initiate stealth kills.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private KeyCode stealthKillKey = KeyCode.E;
    [Tooltip("Reference to the StealthKillSystem in the scene. Assign in Inspector.")]
    [SerializeField] private StealthKillSystem stealthKillSystem;

    private CharacterController characterController;
    private bool inputEnabled = true;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (stealthKillSystem == null)
        {
            stealthKillSystem = FindObjectOfType<StealthKillSystem>();
            if (stealthKillSystem == null)
            {
                Debug.LogError("PlayerController: StealthKillSystem not found in scene! Please assign it or add it to a GameObject.", this);
                enabled = false; // Disable player if system not found
            }
        }
    }

    void Update()
    {
        if (!inputEnabled) return;

        HandleMovement();
        HandleStealthKillInput();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        // Apply gravity if CharacterController is not already handling it
        if (!characterController.isGrounded)
        {
            moveDirection += Physics.gravity;
        }
        characterController.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);

        // Simple rotation with horizontal input (can be swapped for mouse input if preferred)
        transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.deltaTime);
    }

    private void HandleStealthKillInput()
    {
        if (Input.GetKeyDown(stealthKillKey))
        {
            // Find closest potential enemy target within the system's detection range
            EnemyController closestEnemy = FindClosestEnemyPotentialTarget();
            if (closestEnemy != null)
            {
                stealthKillSystem.TryPerformStealthKill(gameObject, closestEnemy.gameObject);
            }
            else
            {
                Debug.Log("PlayerController: No valid enemy target found in range for stealth kill.", this);
            }
        }
    }

    /// <summary>
    /// Finds the closest *potential* enemy target within the StealthKillSystem's detection range.
    /// This is a simplified detection; a real game might use raycasts, trigger colliders, or AI perception.
    /// </summary>
    /// <returns>The closest EnemyController or null if none found.</returns>
    private EnemyController FindClosestEnemyPotentialTarget()
    {
        if (stealthKillSystem == null) return null;

        // Use a slightly larger sphere than the actual kill range to find candidates more easily.
        float searchRadius = stealthKillSystem.DetectionRange * 1.5f; 
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius);
        
        EnemyController closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            EnemyController enemy = hitCollider.GetComponent<EnemyController>();
            if (enemy != null && enemy.IsKillable)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        return closestEnemy;
    }

    public void DisableInput()
    {
        inputEnabled = false;
        Debug.Log("PlayerController: Player input disabled during kill sequence.", this);
    }

    public void EnableInput()
    {
        inputEnabled = true;
        Debug.Log("PlayerController: Player input enabled.", this);
    }
}

/// <summary>
/// Simple Enemy Controller to manage awareness, health, and state.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private int maxHealth = 100;
    [Tooltip("Is the enemy currently aware of the player or any threats?")]
    [SerializeField] private bool isAware = false;
    [Tooltip("Is the enemy in a vulnerable state (e.g., sleeping, stunned)?")]
    [SerializeField] private bool isVulnerable = false;
    [Tooltip("Reference to the player for simple awareness checks. Assign in Inspector or ensure player has 'Player' tag.")]
    [SerializeField] private Transform playerTransform;

    private int currentHealth;
    private bool isDead = false;
    private bool isUnconscious = false;

    // Public properties for criteria to check
    public bool IsKillable => !isDead && !isUnconscious;
    public bool IsVulnerable => isVulnerable;

    void Awake()
    {
        currentHealth = maxHealth;
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player"); 
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        // Simple AI simulation: Enemy becomes aware if player is close and in front.
        // This is for demonstration of the `EnemyUnawareCriteria`.
        if (playerTransform != null && IsKillable)
        {
            float awarenessRange = 5f;
            float awarenessAngle = 70f; // Half angle, so 140 degree FOV

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(directionToPlayer, transform.forward);

            bool playerDetected = (distanceToPlayer < awarenessRange && angleToPlayer < awarenessAngle);

            if (playerDetected && !isAware)
            {
                isAware = true;
                Debug.Log($"{gameObject.name}: became AWARE of {playerTransform.name}!", this);
            }
            else if (!playerDetected && isAware)
            {
                isAware = false;
                Debug.Log($"{gameObject.name}: became UNAWARE of {playerTransform.name}.", this);
            }
        }
    }

    /// <summary>
    /// Checks if the enemy is aware of a specific GameObject.
    /// Used by `EnemyUnawareCriteria`.
    /// </summary>
    /// <param name="initiator">The GameObject to check awareness against.</param>
    /// <returns>True if aware, false otherwise.</returns>
    public bool IsAwareOf(GameObject initiator)
    {
        // For this example, we'll just check the general 'isAware' flag.
        // In a real game, this might check specific line-of-sight to the initiator.
        return isAware;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name}: took {amount} damage. Health: {currentHealth}", this);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name}: has died.", this);
        // Play death animation, disable renderer, etc.
        gameObject.SetActive(false); // Simple way to 'kill' for this example
    }

    public void BecomeUnconscious()
    {
        if (isDead || isUnconscious) return;

        isUnconscious = true;
        isAware = false; // Unconscious enemies are not aware
        isVulnerable = true; // Might become vulnerable after being knocked out (e.g., for follow-up actions)
        Debug.Log($"{gameObject.name}: has been rendered unconscious.", this);
        // Play unconscious animation, fall to ground, etc.
        // For simplicity, we'll just disable the game object like a death.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when a stealth kill sequence is initiated on this enemy.
    /// This allows the enemy to freeze its AI, play a victim animation, etc.
    /// </summary>
    public void OnKillStarted()
    {
        Debug.Log($"{gameObject.name}: is being targeted for a stealth kill. Freezing actions.", this);
        isAware = false; // Enemy cannot be aware if being stealth killed
        // Potentially disable AI/movement, set animation states, etc.
    }

    /// <summary>
    /// Visualizes the enemy's awareness cone (blue) and the stealth kill back cone (green) in the editor.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Visualize enemy's forward awareness cone
        Gizmos.color = Color.blue;
        float awarenessRange = 5f;
        float awarenessAngle = 70f; // Matches Update() logic
        DrawCone(transform.position, transform.forward, awarenessRange, awarenessAngle, 32);

        // Visualize enemy's backward cone for stealth kill potential
        Gizmos.color = Color.green;
        float stealthKillRange = FindObjectOfType<StealthKillSystem>()?.detectionRange ?? 2f;
        float stealthKillAngle = FindObjectOfType<StealthKillSystem>()?.detectionAngle ?? 90f;
        DrawCone(transform.position, -transform.forward, stealthKillRange, stealthKillAngle, 32);
    }

    // Helper method to draw a cone Gizmo
    private void DrawCone(Vector3 origin, Vector3 direction, float range, float angle, int segments)
    {
        float halfAngleRad = angle * 0.5f * Mathf.Deg2Rad;
        Vector3 endpoint = origin + direction * range;
        
        Vector3 startDirection = Quaternion.Euler(0, -angle / 2, 0) * direction;
        
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = i * (angle / segments);
            Vector3 from = Quaternion.Euler(0, currentAngle, 0) * startDirection;
            Vector3 to = Quaternion.Euler(0, currentAngle + (angle / segments), 0) * startDirection;
            
            Gizmos.DrawLine(origin, origin + from.normalized * range);
            Gizmos.DrawLine(origin + from.normalized * range, origin + to.normalized * range);
        }
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0, angle / 2, 0) * direction).normalized * range);
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0, -angle / 2, 0) * direction).normalized * range);
    }
}
```