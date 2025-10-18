// Unity Design Pattern Example: ScoreStreakSystem
// This script demonstrates the ScoreStreakSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ScoreStreakSystem' design pattern is a common and effective way to reward players for sustained good performance in games. It tracks a series of consecutive successful actions (a "streak") and typically grants increasing bonuses (like more points) as the streak grows. Conversely, a single failed action or a period of inactivity will "break" the streak, resetting it and often applying a penalty.

This system encourages players to focus, maintain precision, and engage continuously, providing a clear incentive for mastering game mechanics.

Here's a complete C# Unity implementation demonstrating the ScoreStreakSystem pattern.

---

### `ScoreStreakSystem.cs`

```csharp
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent
using System.Collections.Generic; // For List<int> streakMilestones

/// <summary>
/// The ScoreStreakSystem design pattern rewards players for consecutive successful actions.
/// It tracks a "streak" count, applying bonuses to scores as the streak grows,
/// and resets the streak upon a failed action or inactivity.
///
/// This pattern encourages players to maintain focus and skill, providing a sense of
/// progression and rewarding sustained good performance.
///
/// Key Components of the ScoreStreakSystem Pattern:
/// 1.  Streak Tracker: Manages the current count of consecutive successful actions.
/// 2.  Score Calculator: Applies a bonus multiplier to points based on the current streak,
///     making successful actions more rewarding as the streak lengthens.
/// 3.  Streak Breaker: Defines conditions under which the streak is reset (e.g., player failure, inactivity).
///     It can also apply penalties.
/// 4.  Event Notifier: Communicates changes in streak count and total score to other systems
///     (e.g., UI, audio, visual effects) using UnityEvents for loose coupling.
/// 5.  Milestones: Allows triggering specific events when the streak reaches predefined levels.
/// </summary>
public class ScoreStreakSystem : MonoBehaviour
{
    [Header("Core Configuration")]
    [Tooltip("The base score awarded for a single successful action before any streak bonus is applied.")]
    [SerializeField] private int baseScorePerHit = 100;

    [Tooltip("The multiplier applied to the base score for each level of streak. " +
             "e.g., 1.0 (no bonus), 1.1 (10% bonus per streak level). The bonus is applied exponentially " +
             "like baseScore * (streakBonusMultiplierPerLevel ^ (streak - 1)).")]
    [SerializeField] [Range(1.0f, 5.0f)] private float streakBonusMultiplierPerLevel = 1.1f;

    [Tooltip("The maximum multiplier that can be applied to the score, regardless of how long the streak becomes. " +
             "Prevents scores from escalating infinitely.")]
    [SerializeField] [Range(1.0f, 10.0f)] private float maxStreakBonusCap = 3.0f;

    [Tooltip("Time in seconds after a successful action without another action before the streak automatically " +
             "breaks due to inactivity. Set to 0 for no inactivity break.")]
    [SerializeField] private float streakResetDelay = 3.0f;

    [Tooltip("Optional: Points deducted from the total score when a streak is broken by a failed action.")]
    [SerializeField] private int streakBreakPenalty = 0;

    [Header("Streak Milestones")]
    [Tooltip("A list of specific streak counts that will trigger the OnNewStreakMilestone event " +
             "(e.g., enter 5, 10, 25 to get notified when streak reaches these values).")]
    [SerializeField] private List<int> streakMilestones = new List<int> { 5, 10, 20, 50, 100 };

    [Header("Current State (Read Only)")]
    [Tooltip("The current number of consecutive successful actions.")]
    [SerializeField] private int _currentStreak = 0;
    /// <summary>Gets the current streak count.</summary>
    public int CurrentStreak => _currentStreak;

    [Tooltip("The player's total accumulated score.")]
    [SerializeField] private int _totalScore = 0;
    /// <summary>Gets the player's total accumulated score.</summary>
    public int TotalScore => _totalScore;

    private float _lastSuccessfulActionTime; // Timestamp of the last successful action for inactivity check

    [Header("Events")]
    [Tooltip("UnityEvent invoked when the streak count increases. Provides the new streak value.")]
    public UnityEvent<int> OnStreakIncreased;

    [Tooltip("UnityEvent invoked when the streak is broken (resets to 0). " +
             "Provides the streak count before it was broken.")]
    public UnityEvent<int> OnStreakBroken;

    [Tooltip("UnityEvent invoked when the total score changes. Provides the new total score.")]
    public UnityEvent<int> OnScoreChanged;

    [Tooltip("UnityEvent invoked when a configured streak milestone is reached. " +
             "Provides the milestone value that was reached.")]
    public UnityEvent<int> OnNewStreakMilestone;

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Initializes the system state and ensures UnityEvents are ready.
    /// </summary>
    private void Awake()
    {
        // Initialize internal state variables
        _currentStreak = 0;
        _totalScore = 0;
        _lastSuccessfulActionTime = 0; // Initialize to 0 or Time.time if a streak starts immediately on game start

        // Ensure UnityEvents are initialized to prevent NullReferenceExceptions when invoked
        OnStreakIncreased ??= new UnityEvent<int>();
        OnStreakBroken ??= new UnityEvent<int>();
        OnScoreChanged ??= new UnityEvent<int>();
        OnNewStreakMilestone ??= new UnityEvent<int>();
    }

    /// <summary>
    /// Broadcasts initial state for UI or other systems to set up.
    /// </summary>
    private void Start()
    {
        OnScoreChanged.Invoke(_totalScore);     // Broadcast initial score (0)
        OnStreakIncreased.Invoke(_currentStreak); // Broadcast initial streak (0)
    }

    /// <summary>
    /// Checks for streak reset due to inactivity if `streakResetDelay` is configured.
    /// </summary>
    private void Update()
    {
        // Only check for inactivity if a delay is set and a streak is currently active
        if (streakResetDelay > 0 && _currentStreak > 0)
        {
            // If the time since the last successful action exceeds the reset delay, break the streak
            if (Time.time - _lastSuccessfulActionTime >= streakResetDelay)
            {
                Debug.Log($"<color=orange>Streak of {_currentStreak} broken due to inactivity!</color>");
                BreakStreak(); // Break without penalty for inactivity
            }
        }
    }

    // --- Public Methods to Interact with the System ---

    /// <summary>
    /// Call this method when the player performs a successful action (e.g., hits a target, correctly answers a question).
    /// It increments the streak, calculates and adds score, and notifies listeners of changes.
    /// </summary>
    public void OnSuccessfulAction()
    {
        _currentStreak++;
        _lastSuccessfulActionTime = Time.time; // Update timestamp for the inactivity check

        // Calculate score for this action with streak bonus
        int scoreEarned = CalculateScoreForAction(baseScorePerHit);
        _totalScore += scoreEarned; // Add to total score

        Debug.Log($"<color=green>Successful Action!</color> Streak: {_currentStreak}, Score Earned: {scoreEarned}, Total Score: {_totalScore}");

        // Notify listeners about streak and score changes
        OnStreakIncreased.Invoke(_currentStreak);
        OnScoreChanged.Invoke(_totalScore);

        // Check if the new streak count has reached any configured milestones
        CheckForMilestone(_currentStreak);
    }

    /// <summary>
    /// Call this method when the player performs a failed action (e.g., misses a target, takes damage, makes a mistake).
    /// It resets the streak, applies an optional penalty, and notifies listeners.
    /// </summary>
    public void OnFailedAction()
    {
        Debug.Log($"<color=red>Failed Action!</color> Streak of {_currentStreak} broken.");
        BreakStreak(applyPenalty: true); // Break streak and apply penalty
    }

    /// <summary>
    /// Resets the current streak to zero and notifies listeners.
    /// Optionally applies a penalty to the total score if `applyPenalty` is true and `streakBreakPenalty` > 0.
    /// </summary>
    /// <param name="applyPenalty">Whether to apply the configured streak break penalty.</param>
    private void BreakStreak(bool applyPenalty = false)
    {
        if (_currentStreak == 0) return; // No streak to break if already 0

        int oldStreak = _currentStreak; // Store old streak for notification
        _currentStreak = 0; // Reset streak

        // Apply penalty if specified and configured
        if (applyPenalty && streakBreakPenalty > 0)
        {
            _totalScore = Mathf.Max(0, _totalScore - streakBreakPenalty); // Ensure score doesn't go below 0
            Debug.Log($"<color=red>Streak penalty of {streakBreakPenalty} applied.</color> New Total Score: {_totalScore}");
            OnScoreChanged.Invoke(_totalScore); // Notify score change due to penalty
        }

        // Notify listeners that the streak was broken
        OnStreakBroken.Invoke(oldStreak);
        OnStreakIncreased.Invoke(_currentStreak); // Also update UI components displaying current streak (now 0)
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Calculates the score for a single action, applying the current streak bonus.
    /// The bonus grows exponentially based on `streakBonusMultiplierPerLevel`.
    /// </summary>
    /// <param name="baseScore">The base points for the action before any streak bonus.</param>
    /// <returns>The total points awarded for this action, including streak bonus.</returns>
    private int CalculateScoreForAction(int baseScore)
    {
        float currentMultiplier = GetCurrentStreakMultiplier();
        int score = Mathf.RoundToInt(baseScore * currentMultiplier);
        return score;
    }

    /// <summary>
    /// Determines the current score multiplier based on the streak count and configuration.
    /// The multiplier increases with streak length but is capped by `maxStreakBonusCap`.
    /// </summary>
    /// <returns>The multiplier to apply to the base score (1.0 for no bonus).</returns>
    private float GetCurrentStreakMultiplier()
    {
        if (_currentStreak <= 1)
        {
            return 1.0f; // No bonus for streak of 0 or 1
        }

        // Calculate exponential bonus: multiplier = (multiplier_per_level) ^ (streak - 1)
        // Subtract 1 from streak because the first action (streak=1) has no bonus.
        // The bonus starts from streak=2.
        float calculatedMultiplier = Mathf.Pow(streakBonusMultiplierPerLevel, _currentStreak - 1);

        // Return the calculated multiplier, ensuring it doesn't exceed the defined cap
        return Mathf.Min(calculatedMultiplier, maxStreakBonusCap);
    }

    /// <summary>
    /// Checks if the current streak has reached any predefined milestones and invokes the `OnNewStreakMilestone` event.
    /// </summary>
    /// <param name="streak">The current streak value to check against milestones.</param>
    private void CheckForMilestone(int streak)
    {
        if (streakMilestones == null || streakMilestones.Count == 0) return;

        // Iterate through the milestones list
        foreach (int milestone in streakMilestones)
        {
            // If the current streak exactly matches a milestone
            if (streak == milestone)
            {
                Debug.Log($"<color=purple>Milestone Reached! Streak: {milestone}</color>");
                OnNewStreakMilestone.Invoke(milestone); // Notify listeners
                // We can remove it from the list here if we only want it to trigger once ever,
                // or keep it if it's fine for it to re-trigger if streak drops below and comes back.
                // For simplicity, we'll keep it in the list to trigger every time it's hit.
                break; // Assuming milestones are unique and sorted, no need to check further after a match
            }
        }
    }

    // --- Example Usage / Debugging Context Menu Items ---
    // These methods can be called directly from the Inspector's context menu (right-click on the script).
    // Useful for quick testing during development.

    [ContextMenu("Simulate Successful Action")]
    private void SimulateSuccessfulAction()
    {
        OnSuccessfulAction();
    }

    [ContextMenu("Simulate Failed Action")]
    private void SimulateFailedAction()
    {
        OnFailedAction();
    }

    [ContextMenu("Reset System (Score & Streak)")]
    private void ResetSystem()
    {
        _totalScore = 0;
        BreakStreak(); // Will reset streak to 0 and invoke relevant events
        OnScoreChanged.Invoke(_totalScore); // Make sure total score update is broadcasted
        _lastSuccessfulActionTime = 0; // Reset inactivity timer
        Debug.Log("<color=blue>Score Streak System Reset!</color>");
    }
}

/*
/// --- HOW TO USE THIS SCORESTREAKSYSTEM IN YOUR UNITY PROJECT ---
///
/// 1.  Create an Empty GameObject in your Unity scene (e.g., named "GameManager" or "ScoreSystem").
/// 2.  Attach this 'ScoreStreakSystem' script to the newly created GameObject.
/// 3.  **Configure Parameters in the Inspector:**
///     -   **Base Score Per Hit:** Set the base points for a successful action (e.g., 100).
///     -   **Streak Bonus Multiplier Per Level:** Adjust how much the score bonus increases per streak level (e.g., 1.1 for 10% increase per level).
///     -   **Max Streak Bonus Cap:** Define the maximum multiplier to prevent excessively high scores (e.g., 3.0 means max 3x base score).
///     -   **Streak Reset Delay:** Specify how long the player can be inactive after a successful action before the streak breaks (e.g., 3.0 seconds). Set to 0 for no timeout.
///     -   **Streak Break Penalty:** Set points to deduct if a streak is broken by a failed action (e.g., 50).
///     -   **Streak Milestones:** Add integer values (e.g., 5, 10, 25) to trigger the `OnNewStreakMilestone` event when these streak counts are reached.
///
/// 4.  **Integrate with UI, Audio, and VFX (using UnityEvents):**
///     -   In the Inspector, expand the "Events" section of the `ScoreStreakSystem` component.
///     -   For each UnityEvent (e.g., `OnScoreChanged`, `OnStreakIncreased`, `OnStreakBroken`, `OnNewStreakMilestone`), click the `+` button to add a new listener.
///     -   Drag and drop the GameObject containing the script that will handle the event (e.g., your "UIManager" GameObject) into the 'Runtime Only' object slot.
///     -   From the dropdown menu, select the script component and then choose the public method you want to call when the event fires.
///
///     Example of a `UIManager` script that could listen to these events:
///     ```csharp
///     using UnityEngine;
///     using TMPro; // Assuming TextMeshPro for UI text elements
///
///     public class UIManager : MonoBehaviour
///     {
///         public TextMeshProUGUI scoreText;
///         public TextMeshProUGUI streakText;
///         public GameObject streakIncreaseEffectPrefab; // Particle system or sound for streak increase
///         public GameObject streakBreakEffectPrefab;    // Particle system or sound for streak break
///         public GameObject milestoneEffectPrefab;      // Particle system or sound for milestones
///
///         // --- Methods to be hooked up to ScoreStreakSystem's UnityEvents ---
///
///         public void UpdateScoreText(int newScore)
///         {
///             if (scoreText != null)
///             {
///                 scoreText.text = $"SCORE: {newScore:N0}"; // Format with thousands separator
///             }
///         }
///
///         public void UpdateStreakText(int newStreak)
///         {
///             if (streakText != null)
///             {
///                 streakText.text = (newStreak > 1) ? $"STREAK x{newStreak}" : ""; // Only show streak if > 1
///                 if (newStreak > 1 && streakIncreaseEffectPrefab != null)
///                 {
///                     // Play a subtle effect or sound for streak increase
///                     Instantiate(streakIncreaseEffectPrefab, transform.position, Quaternion.identity);
///                 }
///             }
///         }
///
///         public void OnStreakWasBroken(int oldStreak)
///         {
///             Debug.Log($"UI Manager: Visually indicating streak of {oldStreak} was broken!");
///             if (streakBreakEffectPrefab != null)
///             {
///                 // Play a visual effect (e.g., "Streak Broken!" text, particle burst) or sound
///                 Instantiate(streakBreakEffectPrefab, transform.position, Quaternion.identity);
///             }
///         }
///
///         public void OnMilestoneReached(int milestone)
///         {
///             Debug.Log($"UI Manager: Displaying special notification for milestone {milestone}!");
///             if (milestoneEffectPrefab != null)
///             {
///                 // Play a grander effect or sound for reaching a significant milestone
///                 Instantiate(milestoneEffectPrefab, transform.position, Quaternion.identity);
///             }
///         }
///     }
///     ```
///
/// 5.  **Triggering Actions from other Game Logic (e.g., Player, Enemy, Game Mechanics):**
///     Other scripts in your game will need to inform the `ScoreStreakSystem` about successful or failed actions.
///
///     ```csharp
///     using UnityEngine;
///
///     public class PlayerActionTrigger : MonoBehaviour
///     {
///         private ScoreStreakSystem scoreSystem;
///
///         void Start()
///         {
///             // Find the ScoreStreakSystem in the scene. A more robust approach might be
///             // to pass it via a GameManager or dependency injection, but FindObjectOfType is quick.
///             scoreSystem = FindObjectOfType<ScoreStreakSystem>();
///             if (scoreSystem == null)
///             {
///                 Debug.LogError("ScoreStreakSystem not found in scene! Please add it to a GameObject.");
///             }
///         }
///
///         // Example: Called when the player successfully performs an action (e.g., hits an enemy, solves a puzzle)
///         public void OnPlayerSuccessfulInteraction()
///         {
///             if (scoreSystem != null)
///             {
///                 scoreSystem.OnSuccessfulAction();
///                 Debug.Log("Player scored a perfect hit!");
///             }
///         }
///
///         // Example: Called when the player fails an action (e.g., misses a shot, takes damage, falls off a platform)
///         public void OnPlayerFailedInteraction()
///         {
///             if (scoreSystem != null)
///             {
///                 scoreSystem.OnFailedAction();
///                 Debug.Log("Player made a mistake or got hit!");
///             }
///         }
///
///         // You might call these methods from input handlers, collision detection, game state logic, etc.
///         void OnTriggerEnter(Collider other)
///         {
///             if (other.CompareTag("Collectible"))
///             {
///                 OnPlayerSuccessfulInteraction();
///                 Destroy(other.gameObject); // Collect it
///             }
///             else if (other.CompareTag("DangerZone"))
///             {
///                 OnPlayerFailedInteraction();
///                 // Take damage, etc.
///             }
///         }
///     }
///     ```
///
/// By following these steps, the ScoreStreakSystem will be fully integrated and functional
/// in your Unity project, providing a clear separation of concerns and a flexible
/// way to manage player scoring based on consecutive actions.
*/
```