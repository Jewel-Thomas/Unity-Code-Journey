// Unity Design Pattern Example: ParrySystem
// This script demonstrates the ParrySystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ParrySystem' design pattern in games allows a player to negate an enemy's attack, often by performing a specific action within a small, precise timeframe. This usually involves an "attack wind-up" phase where the parry window opens, followed by the "active attack" phase where the player can parry. A successful parry often leads to a counter-attack opportunity, stunning the enemy, or negating damage, while a failed parry might leave the player vulnerable.

This example provides a complete, practical C# Unity script for implementing such a system.

---

### Key Concepts of the ParrySystem Pattern:

1.  **Parry Initiator (Player):** The entity attempting to parry.
2.  **Attacker (Enemy):** The entity performing an attack that can be parried.
3.  **Parry Window:** A specific, often short, time frame during an attack's wind-up or early active phase where a parry attempt will succeed.
4.  **Parry Action:** The specific input or action the Parry Initiator performs (e.g., pressing a block button, using a shield).
5.  **Success Outcome:** What happens when a parry succeeds (e.g., enemy stunned, damage negated, counter-attack opportunity, visual/audio feedback).
6.  **Failure Outcome:** What happens when a parry fails (e.g., player takes damage, no effect, visual/audio feedback).
7.  **Cooldown:** A period after a parry attempt (successful or not) before another parry can be attempted, preventing spamming.

---

### `ParrySystem.cs` Script

This script manages the core logic of the parry system:
*   It exposes methods for other scripts (like an `EnemyAttack` script) to initiate a parry window.
*   It exposes methods for a `PlayerInput` script to attempt a parry.
*   It uses Coroutines to manage the timing of the parry window and cooldowns.
*   It uses `UnityEvent`s for other systems to subscribe to parry outcomes.
*   It includes optional visual feedback for demonstration purposes.

```csharp
using UnityEngine;
using System.Collections;
using UnityEngine.Events; // Required for UnityEvent

/// <summary>
/// The ParrySystem class manages the core logic for player parries against enemy attacks.
/// It defines a 'parry window' during which a player's parry attempt will succeed.
/// This system typically interacts with an 'EnemyAttack' script (to open the window)
/// and a 'PlayerInput' script (to attempt a parry).
/// </summary>
public class ParrySystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Provides an easy way for other scripts to access this single ParrySystem instance.
    public static ParrySystem Instance { get; private set; }

    private void Awake()
    {
        // Enforce singleton pattern: only one instance of ParrySystem can exist.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ParrySystem: Multiple ParrySystem instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this; // Set this instance as the singleton.
        InitializeSystem();
    }

    // --- Configuration Parameters ---
    [Header("Parry Configuration")]
    [Tooltip("The duration (in seconds) that the parry window is open after an attack is initiated.")]
    [SerializeField] private float _parryWindowDuration = 0.3f; // Example: 300ms window
    [Tooltip("The cooldown duration (in seconds) after any parry attempt (successful or not) before another can be made.")]
    [SerializeField] private float _parryAttemptCooldown = 1.0f; // Example: 1 second cooldown

    // --- Optional Visual Feedback ---
    // Useful for debugging and demonstrating the parry window and outcomes.
    [Header("Visual Feedback (Optional)")]
    [Tooltip("Renderer component to change color for visual feedback. Leave null if not using graphical feedback.")]
    [SerializeField] private Renderer _feedbackRenderer;
    [SerializeField] private Color _defaultFeedbackColor = Color.white;
    [SerializeField] private Color _parryWindowColor = Color.yellow;
    [SerializeField] private Color _successFeedbackColor = Color.green;
    [SerializeField] private Color _failFeedbackColor = Color.red;
    [Tooltip("How long the success/fail feedback color is displayed before returning to default.")]
    [SerializeField] private float _feedbackDisplayDuration = 0.5f;

    // --- Internal State Variables ---
    private bool _isParryWindowOpen = false; // Is an enemy attack currently in its parry-able phase?
    private bool _canAttemptParry = true;    // Is the player currently off cooldown and allowed to attempt a parry?

    // Coroutine references to manage timing, allowing them to be stopped if necessary.
    private Coroutine _parryWindowCoroutine;
    private Coroutine _parryCooldownCoroutine;
    private Coroutine _visualFeedbackCoroutine; // For temporary color changes

    // --- Events (for other systems to subscribe to specific outcomes) ---
    [Header("Parry Events")]
    public UnityEvent OnParrySuccess;       // Invoked when a parry is successfully performed.
    public UnityEvent OnParryFail;          // Invoked when a parry attempt fails.
    public UnityEvent OnParryWindowOpened;  // Invoked when an enemy attack opens the parry window.
    public UnityEvent OnParryWindowClosed;  // Invoked when the parry window closes (either by timeout or successful parry).

    /// <summary>
    /// Initializes the system's state when the script starts or is enabled.
    /// </summary>
    private void InitializeSystem()
    {
        _canAttemptParry = true; // Player can attempt a parry initially.
        _isParryWindowOpen = false; // No parry window is open initially.
        SetFeedbackColor(_defaultFeedbackColor); // Set default visual state.
        Debug.Log("ParrySystem: Initialized. Ready for parry attempts.");
    }

    /// <summary>
    /// <para>Call this method from an 'EnemyAttack' script when an attack is about to become parry-able.</para>
    /// <para>This opens the parry window for a short, predefined duration.</para>
    /// </summary>
    public void InitiateParryWindow()
    {
        // Prevent opening multiple parry windows simultaneously.
        // Game design choice: either ignore, reset the current one, or queue. Ignoring for simplicity.
        if (_isParryWindowOpen)
        {
            Debug.LogWarning("ParrySystem: Attempted to open a new parry window while one is already active. Ignoring current request.");
            return;
        }

        Debug.Log($"ParrySystem: Parry window opened! Active for {_parryWindowDuration:F2} seconds.");
        _isParryWindowOpen = true; // Mark the parry window as open.
        OnParryWindowOpened?.Invoke(); // Notify subscribers.
        SetFeedbackColor(_parryWindowColor); // Provide visual feedback.

        // Start a coroutine to automatically close the parry window after its duration.
        if (_parryWindowCoroutine != null) StopCoroutine(_parryWindowCoroutine); // Stop any existing timer.
        _parryWindowCoroutine = StartCoroutine(ParryWindowTimer());
    }

    /// <summary>
    /// <para>Call this method from a 'PlayerInput' script when the player performs the parry action.</para>
    /// <para>It checks if the attempt was successful based on whether the parry window is currently open and if player is off cooldown.</para>
    /// </summary>
    public void AttemptParry()
    {
        // First, check if the player is on cooldown from a previous parry attempt.
        if (!_canAttemptParry)
        {
            Debug.Log("ParrySystem: Parry attempt failed - player is on cooldown.");
            TriggerParryFail(); // A failed attempt, even due to cooldown, is still a 'fail'.
            return;
        }

        // Immediately put the player on cooldown regardless of success or failure.
        // This prevents spamming the parry button.
        StartParryCooldown();

        // Now, check if the parry window is open to determine success.
        if (_isParryWindowOpen)
        {
            HandleParrySuccess();
        }
        else
        {
            HandleParryFail();
        }
    }

    // --- Internal Coroutines and Logic ---

    /// <summary>
    /// Manages the duration of the parry window. If the timer expires and no parry occurred,
    /// the window is closed, signifying a missed parry opportunity.
    /// </summary>
    private IEnumerator ParryWindowTimer()
    {
        yield return new WaitForSeconds(_parryWindowDuration);

        // If the window is still open after the timer, it means no successful parry was performed.
        if (_isParryWindowOpen)
        {
            _isParryWindowOpen = false; // Close the window.
            OnParryWindowClosed?.Invoke(); // Notify subscribers.
            Debug.Log("ParrySystem: Parry window closed automatically (no parry detected).");
            SetFeedbackColor(_defaultFeedbackColor); // Reset visual feedback.
        }
    }

    /// <summary>
    /// Initiates the cooldown period after a parry attempt.
    /// </summary>
    private void StartParryCooldown()
    {
        _canAttemptParry = false; // Player cannot attempt another parry during cooldown.
        // Stop any existing cooldown timer before starting a new one.
        if (_parryCooldownCoroutine != null) StopCoroutine(_parryCooldownCoroutine);
        _parryCooldownCoroutine = StartCoroutine(ParryCooldownTimer());
    }

    /// <summary>
    /// Manages the duration of the parry cooldown. After the cooldown, the player can attempt parries again.
    /// </summary>
    private IEnumerator ParryCooldownTimer()
    {
        yield return new WaitForSeconds(_parryAttemptCooldown);
        _canAttemptParry = true; // Allow new parry attempts.
        Debug.Log("ParrySystem: Parry cooldown ended. Can attempt parry again.");
    }

    /// <summary>
    /// Handles the logic for a successful parry.
    /// </summary>
    private void HandleParrySuccess()
    {
        // A parry was successful, so immediately close the window regardless of its timer.
        if (_parryWindowCoroutine != null) StopCoroutine(_parryWindowCoroutine);
        _isParryWindowOpen = false; // Mark window as closed.
        OnParryWindowClosed?.Invoke(); // Notify subscribers.

        Debug.Log("<color=green>ParrySystem: PARRY SUCCESS!</color> Enemy is vulnerable!");
        OnParrySuccess?.Invoke(); // Trigger success event.
        SetFeedbackColor(_successFeedbackColor, _feedbackDisplayDuration); // Visual feedback.
    }

    /// <summary>
    /// Handles the logic for a failed parry (e.g., wrong timing, or on cooldown).
    /// </summary>
    private void HandleParryFail()
    {
        Debug.Log("<color=red>ParrySystem: Parry attempt failed.</color> Bad timing!");
        TriggerParryFail(); // Use helper to trigger fail event and feedback.
    }

    /// <summary>
    /// Helper method to consistently trigger the fail event and visual feedback.
    /// </summary>
    private void TriggerParryFail()
    {
        OnParryFail?.Invoke(); // Trigger fail event.
        SetFeedbackColor(_failFeedbackColor, _feedbackDisplayDuration); // Visual feedback.
    }

    /// <summary>
    /// Sets the color of the feedback renderer.
    /// </summary>
    /// <param name="color">The color to set.</param>
    /// <param name="duration">How long the color should be displayed before returning to default. If 0, stays until next call.</param>
    private void SetFeedbackColor(Color color, float duration = 0f)
    {
        if (_feedbackRenderer == null) return; // Only apply if a renderer is assigned.

        if (_visualFeedbackCoroutine != null) StopCoroutine(_visualFeedbackCoroutine); // Stop any ongoing flash.

        if (duration > 0)
        {
            _visualFeedbackCoroutine = StartCoroutine(FlashColor(color, duration)); // Flash for a duration.
        }
        else
        {
            _feedbackRenderer.material.color = color; // Set color permanently (until next call).
        }
    }

    /// <summary>
    /// Coroutine to briefly flash a color on the feedback renderer and then return to the default color.
    /// </summary>
    private IEnumerator FlashColor(Color color, float duration)
    {
        if (_feedbackRenderer != null)
        {
            _feedbackRenderer.material.color = color; // Set the flash color.
            yield return new WaitForSeconds(duration); // Wait for the specified duration.
            _feedbackRenderer.material.color = _defaultFeedbackColor; // Return to default.
        }
        _visualFeedbackCoroutine = null; // Clear coroutine reference when done.
    }

    // --- Editor-only setup for demonstration convenience ---
    void OnValidate()
    {
        // This method is called in the editor when script is loaded or a value is changed in Inspector.
        // It's used here to auto-populate the _feedbackRenderer if a Renderer component exists
        // on the same GameObject, making initial setup quicker for the example.
        if (_feedbackRenderer == null)
        {
            _feedbackRenderer = GetComponent<Renderer>();
        }
    }

    /// <summary>
    /// <para>To set up and test the ParrySystem:</para>
    /// <para>1. Create an empty GameObject in your scene, name it "ParrySystem".</para>
    /// <para>2. Attach this `ParrySystem.cs` script to it.</para>
    /// <para>3. (Optional, but recommended for visual feedback) Add a visible component like a `MeshRenderer`
    ///    (e.g., a 3D Cube component) to the "ParrySystem" GameObject. The `_feedbackRenderer` field
    ///    in the Inspector should auto-populate with this renderer.</para>
    /// <para>4. Create another empty GameObject, name it "Enemy".</para>
    /// <para>5. Create a new C# script named `EnemyAttack.cs` (see below for content) and attach it to "Enemy".</para>
    /// <para>6. Create another empty GameObject, name it "Player".</para>
    /// <para>7. Create a new C# script named `PlayerInput.cs` (see below for content) and attach it to "Player".</para>
    /// <para>8. Run the scene.</para>
    ///
    /// <para>Interactions:</para>
    /// <para>- Press 'A' (Attack) to make the enemy start an attack and open the parry window.</para>
    /// <para>- Press 'Q' (Parry) to attempt a parry during the enemy's attack.</para>
    /// <para>Observe Debug.Log messages in the console and the color changes on the "ParrySystem" GameObject (if using MeshRenderer).</para>
    /// </summary>
    
    // --- Example Usage Script: PlayerInput.cs ---
    // Attach this to a "Player" GameObject in your scene.
    /*
    using UnityEngine;

    public class PlayerInput : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q)) // Player presses 'Q' to attempt a parry
            {
                // Access the ParrySystem via its static Instance.
                if (ParrySystem.Instance != null)
                {
                    ParrySystem.Instance.AttemptParry();
                }
                else
                {
                    Debug.LogError("PlayerInput: ParrySystem instance not found! Make sure it's in the scene.");
                }
            }
        }
    }
    */

    // --- Example Usage Script: EnemyAttack.cs ---
    // Attach this to an "Enemy" GameObject in your scene.
    /*
    using UnityEngine;
    using System.Collections; // Required for Coroutines

    public class EnemyAttack : MonoBehaviour
    {
        [Header("Enemy Attack Settings")]
        [Tooltip("Time from attack initiation until the parry window opens.")]
        [SerializeField] private float _attackWindUpTime = 1.0f; 
        [Tooltip("Duration the enemy attack remains active after wind-up (e.g., dealing damage).")]
        [SerializeField] private float _attackDamageDuration = 0.5f;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A)) // Simulate enemy deciding to attack (e.g., AI decision, player proximity)
            {
                PerformAttack();
            }
        }

        /// <summary>
        /// Initiates the enemy's attack sequence.
        /// </summary>
        public void PerformAttack()
        {
            Debug.Log("Enemy: Starting attack wind-up...");
            StartCoroutine(AttackRoutine());
        }

        /// <summary>
        /// Coroutine to simulate the enemy's attack phases.
        /// </summary>
        IEnumerator AttackRoutine()
        {
            // --- Phase 1: Attack Wind-up ---
            // Simulate enemy animation wind-up (e.g., raising a sword).
            yield return new WaitForSeconds(_attackWindUpTime);

            // --- Phase 2: Parry Window & Active Attack ---
            // At this point, the attack is 'active', and the parry window opens.
            if (ParrySystem.Instance != null)
            {
                Debug.Log("Enemy: Attack is now active. Opening parry window!");
                ParrySystem.Instance.InitiateParryWindow();
                // The ParrySystem itself manages its timer for closing the window.
                // We don't need to wait for the parry window to close here.
            }
            else
            {
                Debug.LogError("EnemyAttack: ParrySystem instance not found! Make sure it's in the scene.");
            }

            // Simulate the active damage phase of the attack.
            // During this time, if the player *doesn't* parry, they would typically take damage.
            yield return new WaitForSeconds(_attackDamageDuration);

            Debug.Log("Enemy: Attack active phase ended.");
        }

        /// <summary>
        /// Subscribes to ParrySystem events when this script is enabled.
        /// This allows the enemy to react to the player's parry outcomes.
        /// </summary>
        void OnEnable()
        {
            if (ParrySystem.Instance != null)
            {
                ParrySystem.Instance.OnParrySuccess.AddListener(OnParrySuccessfulByPlayer);
                ParrySystem.Instance.OnParryFail.AddListener(OnParryFailedByPlayer);
            }
            else
            {
                Debug.LogWarning("EnemyAttack: ParrySystem.Instance is null on OnEnable. Events will not be subscribed.");
            }
        }

        /// <summary>
        /// Unsubscribes from ParrySystem events when this script is disabled to prevent memory leaks.
        /// </summary>
        void OnDisable()
        {
            // Check if Instance is still valid before removing listener, as it might have been destroyed.
            if (ParrySystem.Instance != null)
            {
                ParrySystem.Instance.OnParrySuccess.RemoveListener(OnParrySuccessfulByPlayer);
                ParrySystem.Instance.OnParryFail.RemoveListener(OnParryFailedByPlayer);
            }
        }

        /// <summary>
        /// Called when the player successfully parries an attack.
        /// The enemy can react by getting stunned, staggered, or becoming vulnerable.
        /// </summary>
        private void OnParrySuccessfulByPlayer()
        {
            Debug.Log("<color=cyan>Enemy: My attack was PARRIED! I should be stunned or staggered!</color>");
            // TODO: Implement enemy specific logic here, e.g.,
            // StartCoroutine(StunEnemy(_stunDuration));
            // Apply a 'vulnerable' status.
        }

        /// <summary>
        /// Called when the player fails to parry an attack (or is on cooldown).
        /// The enemy can proceed with its attack, deal damage, or perform a follow-up.
        /// </summary>
        private void OnParryFailedByPlayer()
        {
            Debug.Log("<color=cyan>Enemy: Player failed to parry my attack!</color> Proceeding as planned or dealing damage.");
            // TODO: Implement enemy specific logic here, e.g.,
            // playerHealth.TakeDamage(_attackDamage);
            // Initiate a follow-up attack.
        }
    }
    */
}
```