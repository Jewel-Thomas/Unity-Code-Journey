// Unity Design Pattern Example: MentorAICompanion
// This script demonstrates the MentorAICompanion pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MentorAICompanion' design pattern describes an AI entity that observes the player's actions, performance, or the game state, and provides contextual guidance, advice, or demonstrations. It acts as an in-game tutor or assistant, aiming to improve player experience, aid in onboarding, or help overcome challenges.

**Key Principles of the MentorAICompanion Pattern:**

1.  **Observation:** The companion constantly monitors relevant aspects of the game world and the player's behavior (e.g., health, progress, skills learned, current challenge).
2.  **Analysis & Decision:** Based on the observed data and its internal "knowledge base" or "ruleset," the companion decides if advice is needed and what kind of advice would be most beneficial. This is where the "AI" aspect comes in.
3.  **Delivery:** The companion communicates the advice to the player through appropriate channels (UI text, voice lines, visual cues, or even direct demonstrations).
4.  **Contextual & Adaptive:** The advice given should be relevant to the player's current situation and ideally adapt over time as the player learns or the game state changes.

---

### Example: Combat Mentor AI Companion in Unity

This example demonstrates a 'Combat Mentor AI Companion' that observes a player's combat situation (health, enemy types, learned skills) and offers real-time tactical advice.

**To set this up in Unity:**

1.  **Create a new C# Script** named `PlayerCharacter.cs` and paste the first code block into it.
2.  **Create a new C# Script** named `MentorAICompanion.cs` and paste the second code block into it.
3.  **Ensure you have TextMeshPro imported:** Go to `Window -> TextMeshPro -> Import TMPro Essential Resources`.

---

### 1. `PlayerCharacter.cs` (Mock Player for Demonstration)

This script simulates a player's behavior, health, combat state, and learned skills. In a real project, this would be your actual player controller.

```csharp
using UnityEngine;
using System.Collections.Generic; // For List<T> if needed elsewhere

/// <summary>
/// This mock PlayerCharacter script simulates a player's state for the MentorAICompanion.
/// In a real game, this would be your actual player controller and related systems.
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("Player Stats")]
    public int currentHealth = 100;
    public int maxHealth = 100;

    [Header("Combat State")]
    public bool isFightingEnemy = false;
    public string currentEnemyType = ""; // e.g., "Goblin", "Slime", "Archer", "Warrior"

    [Header("Learned Skills")]
    public bool hasLearnedDodge = false;
    public bool hasLearnedBlock = false;

    private float _timeSinceLastDamage = 0f;

    void Update()
    {
        // --- Simulate Health Changes ---
        _timeSinceLastDamage += Time.deltaTime;

        if (isFightingEnemy)
        {
            // Simulate taking damage every few seconds during combat
            if (_timeSinceLastDamage > 3f)
            {
                TakeDamage(Random.Range(5, 15));
                _timeSinceLastDamage = 0f;
            }
        }
        else
        {
            // Regenerate health slowly out of combat
            if (currentHealth < maxHealth)
            {
                currentHealth += 1;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }

        // --- Simulate Player Actions (for Mentor to react to) ---

        // Toggle combat state and enemy type by pressing 'E'
        if (Input.GetKeyDown(KeyCode.E))
        {
            isFightingEnemy = !isFightingEnemy;
            if (isFightingEnemy)
            {
                string[] enemyTypes = { "Goblin", "Slime", "Archer", "Warrior" };
                currentEnemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
                Debug.Log($"<color=cyan>Player entered combat with a {currentEnemyType}!</color>");
            }
            else
            {
                currentEnemyType = "";
                Debug.Log("<color=cyan>Player exited combat.</color>");
            }
        }

        // "Learn" Dodge skill by pressing 'D'
        if (Input.GetKeyDown(KeyCode.D) && !hasLearnedDodge)
        {
            hasLearnedDodge = true;
            Debug.Log("<color=lime>Player learned Dodge!</color>");
        }

        // "Learn" Block skill by pressing 'B'
        if (Input.GetKeyDown(KeyCode.B) && !hasLearnedBlock)
        {
            hasLearnedBlock = true;
            Debug.Log("<color=lime>Player learned Block!</color>");
        }
    }

    /// <summary>
    /// Reduces player health.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"<color=orange>Player took {amount} damage. Health: {currentHealth}</color>");
        if (currentHealth <= 0)
        {
            Debug.Log("<color=red>Player defeated!</color>");
            isFightingEnemy = false; // End combat
            // In a real game, this would trigger a game over or respawn
            currentHealth = maxHealth; // For demo, reset health
        }
    }
}

/// <summary>
/// Data structure to pass relevant player information to the MentorAICompanion for analysis.
/// This decouples the mentor from directly accessing all player internals.
/// </summary>
public class PlayerCombatStats
{
    public int currentHealth;
    public int maxHealth;
    public float healthPercentage;
    public bool isInCombat;
    public string currentEnemyType;
    public bool hasLearnedDodge;
    public bool hasLearnedBlock;
}
```

---

### 2. `MentorAICompanion.cs` (The Core Pattern Implementation)

This script embodies the MentorAICompanion pattern, observing the `PlayerCharacter`, analyzing its state, and delivering advice via UI.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List<T>
using TMPro; // Required for TextMeshProUGUI

/// <summary>
/// The MentorAICompanion is an AI entity designed to observe the player's actions,
/// analyze their performance or current game state, and provide contextual advice,
/// guidance, or demonstrations. It acts as an in-game tutor or assistant.
/// </summary>
/// <remarks>
/// This pattern is highly practical for Unity development, enabling features like:
/// 1.  **Dynamic Onboarding:** Guiding new players through game mechanics as they encounter them.
/// 2.  **Skill Improvement:** Offering real-time tips to help players overcome challenges or refine their strategies.
/// 3.  **Adaptive Tutoring:** Adjusting advice frequency and type based on player progress, skill level, or observed difficulties.
/// 4.  **Enhanced Immersion:** Making the game world feel more interactive and alive with an active, helpful companion.
/// 5.  **Debugging & Analytics (indirectly):** By observing player behavior, a robust mentor system can also inform developers about common player pain points.
/// </remarks>
public class MentorAICompanion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlayerCharacter script the companion will observe.")]
    [SerializeField] private PlayerCharacter targetPlayer;
    [Tooltip("UI Text component (TextMeshProUGUI) to display the mentor's advice.")]
    [SerializeField] private TextMeshProUGUI adviceTextUI;

    [Header("Behavior Settings")]
    [Tooltip("How long advice messages remain on screen (in seconds).")]
    [SerializeField] private float adviceDisplayDuration = 5f;
    [Tooltip("Minimum time between consecutive advice messages (in seconds). This prevents message spam.")]
    [SerializeField] private float adviceCooldown = 10f;
    [Tooltip("How often the mentor checks the player's state (in seconds).")]
    [SerializeField] private float observationInterval = 1f;

    [Header("Knowledge Base")]
    [Tooltip("A list of general tips the mentor can provide when no specific urgent advice is needed.")]
    [SerializeField] private List<string> generalTips = new List<string>
    {
        "Remember to explore your surroundings!",
        "Check your inventory regularly for useful items.",
        "Don't forget to save your progress!",
        "Talk to NPCs, they might have quests or valuable information.",
        "Some enemies have elemental weaknesses. Try different spells!",
        "Look for hidden paths, they often lead to treasures."
    };

    // --- Private State Variables ---
    private float _lastAdviceTime; // Tracks when the last advice was given to enforce cooldown
    private float _lastObservationTime; // Tracks when the last observation occurred
    private Coroutine _currentAdviceCoroutine; // Reference to the active coroutine for displaying advice
    private bool _isGivingAdvice = false; // Flag to prevent multiple advice displays at once

    // --- Core Pattern Methods ---

    void Awake()
    {
        // --- 1. Initialization and Reference Validation ---
        // Ensure essential references are set in the Inspector.
        // If not set, try to find them or log an error and disable the script.
        if (targetPlayer == null)
        {
            Debug.LogError("MentorAICompanion: 'targetPlayer' not assigned! Attempting to find one in the scene.", this);
            targetPlayer = FindObjectOfType<PlayerCharacter>();
            if (targetPlayer == null)
            {
                Debug.LogError("MentorAICompanion: No PlayerCharacter found in the scene. Companion cannot function without a player.", this);
                enabled = false; // Disable script if no player is found
                return;
            }
        }
        if (adviceTextUI == null)
        {
            Debug.LogError("MentorAICompanion: 'adviceTextUI' not assigned! Please assign a TextMeshProUGUI component in the Inspector.", this);
            enabled = false; // Disable script if no UI for advice
            return;
        }

        adviceTextUI.text = ""; // Clear any initial text from the UI component
        _lastAdviceTime = -adviceCooldown; // Initialize to allow advice immediately at start
        _lastObservationTime = Time.time; // Set initial observation time
    }

    void Update()
    {
        // --- 2. Observation Trigger ---
        // Periodically check the player's state based on observationInterval.
        if (Time.time >= _lastObservationTime + observationInterval)
        {
            ObserveAnalyzeAndAdvise();
            _lastObservationTime = Time.time;
        }
    }

    /// <summary>
    /// Combines the Observation, Analysis, and Decision-making steps of the pattern.
    /// This method is called periodically.
    /// </summary>
    private void ObserveAnalyzeAndAdvise()
    {
        // Only proceed if cooldown is over and we are not currently displaying advice.
        // This prevents the mentor from spamming messages or interrupting itself.
        if (Time.time < _lastAdviceTime + adviceCooldown && !_isGivingAdvice)
        {
            return; // Still on cooldown, or advice is currently visible
        }

        // --- Step 1: Observe ---
        // Gather the current state of the player and relevant game data.
        PlayerCombatStats currentStats = ObservePlayerState();
        if (currentStats == null) return; // Should not happen if targetPlayer is valid

        // --- Step 2: Analyze & Decide ---
        // Determine if advice is needed and what specific advice to give based on observed stats.
        string advice = DecideOnAdvice(currentStats);

        // --- Step 3: Deliver ---
        // If advice is generated, deliver it to the player.
        if (!string.IsNullOrEmpty(advice))
        {
            DeliverAdvice(advice);
        }
    }

    /// <summary>
    /// **OBSERVATION STEP:** Gathers the current state of the player and relevant game context.
    /// In a real game, this would query various game systems (PlayerController, CombatManager, QuestManager, etc.).
    /// </summary>
    /// <returns>A PlayerCombatStats object encapsulating the observed data.</returns>
    private PlayerCombatStats ObservePlayerState()
    {
        if (targetPlayer == null) return null; // Safety check

        // Populate the PlayerCombatStats object with data from the targetPlayer.
        return new PlayerCombatStats
        {
            currentHealth = targetPlayer.currentHealth,
            maxHealth = targetPlayer.maxHealth,
            healthPercentage = (float)targetPlayer.currentHealth / targetPlayer.maxHealth,
            isInCombat = targetPlayer.isFightingEnemy,
            currentEnemyType = targetPlayer.currentEnemyType,
            hasLearnedDodge = targetPlayer.hasLearnedDodge,
            hasLearnedBlock = targetPlayer.hasLearnedBlock
        };
    }

    /// <summary>
    /// **ANALYSIS & DECISION STEP (The 'AI' Logic):** Determines if advice is needed and what advice to give.
    /// This method contains the core logic for the mentor, applying its "knowledge" to the observed player state.
    /// </summary>
    /// <param name="stats">The observed player combat statistics.</param>
    /// <returns>A relevant advice message, or null if no advice is needed currently.</returns>
    private string DecideOnAdvice(PlayerCombatStats stats)
    {
        if (stats == null) return null;

        // --- Prioritized Advice Logic ---
        // Advice is prioritized from most critical/urgent to least critical/general.

        // 1. Critical Health Advice (Highest Priority)
        if (stats.isInCombat && stats.healthPercentage < 0.25f)
        {
            return "Your health is dangerously low! Consider disengaging or using a healing potion immediately.";
        }
        else if (stats.healthPercentage < 0.5f && stats.isInCombat)
        {
            return "Watch your health! Don't let it drop too low.";
        }

        // 2. Combat Specific Advice (based on enemy type and unlearned skills)
        if (stats.isInCombat)
        {
            switch (stats.currentEnemyType)
            {
                case "Goblin":
                    if (!stats.hasLearnedBlock) return "Goblins often attack directly. Try blocking to mitigate their damage!";
                    if (!stats.hasLearnedDodge) return "A quick dodge can evade many Goblin attacks.";
                    break;
                case "Archer":
                    if (!stats.hasLearnedDodge) return "Archers are vulnerable at close range. Dodge their arrows and close the distance!";
                    if (!stats.hasLearnedBlock) return "Blocking an arrow might not be as effective as dodging it, but it helps!";
                    break;
                case "Warrior":
                    if (!stats.hasLearnedBlock) return "Warriors have strong, slower attacks. Blocking is key to defending against them.";
                    if (!stats.hasLearnedDodge) return "A well-timed dodge can put you behind a Warrior for a counter-attack.";
                    break;
                case "Slime":
                    return "Slimes are usually weak to elemental attacks. Check your spells or consider fire damage!";
            }
        }

        // 3. General Skill Acquisition Advice (if player hasn't learned key skills yet, appears periodically)
        // Using a modulus operator with Time.time to make these appear intermittently if not learned.
        // This makes the advice feel less spammy but still ensures it eventually appears.
        if (!stats.hasLearnedDodge && Time.time % 25 < observationInterval * 2) // Suggest every 25 seconds roughly if not learned
        {
            return "Have you learned how to dodge yet? It's crucial for avoiding damage!";
        }
        if (!stats.hasLearnedBlock && !stats.hasLearnedDodge && Time.time % 35 < observationInterval * 2) // Less frequent, or if dodge isn't learned yet
        {
            return "Blocking can save you from a lot of damage, especially from heavy attacks!";
        }

        // 4. Random General Tips (Lowest Priority)
        // Only give general tips when not in combat, or very rarely during calm combat.
        // This ensures the mentor stays active but doesn't distract during critical moments.
        if (!stats.isInCombat && Random.value < 0.15f) // 15% chance to give a general tip when out of combat
        {
            return generalTips[Random.Range(0, generalTips.Count)];
        }

        // If no specific or general advice conditions are met, return null.
        return null;
    }

    /// <summary>
    /// **DELIVERY STEP:** Delivers the advice message to the player via the UI.
    /// Manages the display duration and updates the cooldown.
    /// </summary>
    /// <param name="adviceMessage">The message string to display to the player.</param>
    private void DeliverAdvice(string adviceMessage)
    {
        // If there's an ongoing advice display, stop it to show the new, potentially more urgent one.
        if (_isGivingAdvice && _currentAdviceCoroutine != null)
        {
            StopCoroutine(_currentAdviceCoroutine);
        }

        Debug.Log($"<color=yellow>[Mentor AI]: {adviceMessage}</color>");
        _currentAdviceCoroutine = StartCoroutine(DisplayAdviceCoroutine(adviceMessage));
        _lastAdviceTime = Time.time; // Reset cooldown timer
    }

    /// <summary>
    /// Coroutine to handle the timed display of an advice message on the UI.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private IEnumerator DisplayAdviceCoroutine(string message)
    {
        _isGivingAdvice = true;
        adviceTextUI.text = message; // Set the UI text
        yield return new WaitForSeconds(adviceDisplayDuration); // Wait for the specified duration
        adviceTextUI.text = ""; // Clear the text after the duration
        _isGivingAdvice = false;
    }

    /// <summary>
    /// **Public API Example:** Allows other game systems to request the mentor to give a specific, urgent tip.
    /// This can be used for tutorials, quest hints, or specific event triggers.
    /// </summary>
    /// <param name="specificTip">The specific tip string to display.</param>
    /// <param name="overrideCooldown">If true, this tip will ignore the advice cooldown, allowing immediate display.</param>
    public void GiveSpecificTip(string specificTip, bool overrideCooldown = false)
    {
        if (!overrideCooldown && Time.time < _lastAdviceTime + adviceCooldown)
        {
            Debug.Log($"Mentor AI: Cannot give specific tip '{specificTip}' yet due to cooldown.");
            return;
        }
        DeliverAdvice(specificTip);
    }
}
```

---

### How to Set Up in Unity Editor:

1.  **Create UI Text (TextMeshPro):**
    *   In the Hierarchy, right-click -> `UI -> Text - TextMeshPro`.
    *   If prompted, click "Import TMPro Essentials".
    *   Rename this new UI object to something like "MentorAdviceText".
    *   Position it somewhere visible on your screen (e.g., top-center).
    *   Adjust its Rect Transform, Font Size, Color, and Alignment in the Inspector to make it readable.

2.  **Create a Player GameObject:**
    *   In the Hierarchy, right-click -> `Create Empty`.
    *   Rename it to "Player".
    *   Attach the `PlayerCharacter.cs` script to this "Player" GameObject.

3.  **Create the MentorAICompanion GameObject:**
    *   In the Hierarchy, right-click -> `Create Empty`.
    *   Rename it to "MentorAI".
    *   Attach the `MentorAICompanion.cs` script to this "MentorAI" GameObject.

4.  **Configure the MentorAICompanion script in the Inspector:**
    *   Select your "MentorAI" GameObject.
    *   **Target Player:** Drag your "Player" GameObject (from step 2) into the "Target Player" slot.
    *   **Advice Text UI:** Drag your "MentorAdviceText" UI element (from step 1) into the "Advice Text UI" slot.
    *   **Behavior Settings:** Adjust `Advice Display Duration`, `Advice Cooldown`, and `Observation Interval` to your preference.
    *   **Knowledge Base:** Populate the `General Tips` list with various advice strings. You can add more specific, contextual advice directly within the `DecideOnAdvice` method.

5.  **Run the Scene:**
    *   Observe the "MentorAdviceText" UI in your Game window.
    *   The mentor will start providing general tips when the player is idle.
    *   **Press 'E'** to toggle combat mode. Each time you enter combat, a random `currentEnemyType` is assigned.
    *   The mentor will then give combat-specific advice (e.g., about low health, enemy weaknesses, or suggesting skills).
    *   **Press 'D'** to "learn" the dodge skill.
    *   **Press 'B'** to "learn" the block skill. Notice how the mentor's advice adapts once these skills are learned.
    *   The console will also show debug messages from both the player and the mentor.

This setup creates a fully functional, educational demonstration of the MentorAICompanion pattern, ready to be dropped into a Unity project.