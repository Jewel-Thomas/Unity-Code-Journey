// Unity Design Pattern Example: SwimmingStaminaSystem
// This script demonstrates the SwimmingStaminaSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **State Design Pattern** applied to a "Swimming Stamina System" in Unity. The `SwimmingStaminaSystem` acts as the **Context**, managing the current stamina and delegating behavior to its `IStaminaState` objects. Each concrete state (e.g., `IdleLandState`, `NormalSwimState`, `SprintSwimState`, `ExhaustedState`) encapsulates the behavior specific to that state, such as stamina regeneration or depletion rates.

This approach makes the system clean, extensible, and easy to understand. Adding new stamina-related states (e.g., "Underwater Diving State" with unique breath/stamina rules) would only require creating a new concrete state class, without modifying existing states or the core `SwimmingStaminaSystem`.

---

```csharp
using UnityEngine;
using System; // For Action events
using System.Collections; // Not strictly needed for this example but good practice for Unity scripts

/// <summary>
/// Defines the interface for all stamina states.
/// This is the 'State' part of the State Design Pattern.
/// </summary>
public interface IStaminaState
{
    /// <summary>
    /// Called when entering this state. Use for initial setup or effects.
    /// </summary>
    /// <param name="system">The context (SwimmingStaminaSystem) that owns this state.</param>
    void EnterState(SwimmingStaminaSystem system);

    /// <summary>
    /// Called every frame while this state is active. Use for continuous behavior like stamina changes.
    /// </summary>
    /// <param name="system">The context (SwimmingStaminaSystem) that owns this state.</param>
    void ExecuteState(SwimmingStaminaSystem system);

    /// <summary>
    /// Called when exiting this state. Use for cleanup or ending effects.
    /// </summary>
    /// <param name="system">The context (SwimmingStaminaSystem) that owns this state.</param>
    void ExitState(SwimmingStaminaSystem system);
}

/// <summary>
/// The main MonoBehaviour class that manages the swimming stamina system.
/// This acts as the 'Context' in the State Design Pattern.
/// It holds the current stamina, provides an API for other components to interact,
/// and delegates stamina logic to the current IStaminaState.
/// </summary>
[DisallowMultipleComponent] // Ensures only one Stamina System exists on a GameObject
public class SwimmingStaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [Tooltip("The maximum stamina capacity.")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("The current stamina level.")]
    [SerializeField] private float currentStamina;

    [Header("Stamina Rates")]
    [Tooltip("Stamina regenerated per second when idle on land.")]
    [SerializeField] private float idleRegenRate = 5f;
    [Tooltip("Stamina drained per second when swimming normally.")]
    [SerializeField] private float normalSwimDrainRate = 10f;
    [Tooltip("Stamina drained per second when sprinting while swimming.")]
    [SerializeField] private float sprintSwimDrainRate = 25f;

    [Header("Exhaustion Settings")]
    [Tooltip("Minimum stamina required to start or maintain sprinting.")]
    [SerializeField] private float minStaminaForSprint = 15f;
    [Tooltip("Movement speed multiplier when exhausted (e.g., 0.5 means 50% speed).")]
    [SerializeField] private float exhaustedPenaltyMoveSpeedMultiplier = 0.4f;
    [Tooltip("Amount of damage taken per second when exhausted in water.")]
    [SerializeField] private float exhaustedDamageRate = 10f;

    // Public properties for other scripts to read current state
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public IStaminaState CurrentState => _currentStaminaState;

    // Events for UI or other game systems to react to changes
    public event Action<float> OnStaminaChanged;
    public event Action<IStaminaState> OnStateChanged;

    // Internal state instances (Concrete States)
    private IdleLandState _idleLandState;
    private NormalSwimState _normalSwimState;
    private SprintSwimState _sprintSwimState;
    private ExhaustedState _exhaustedState;

    // The currently active stamina state
    private IStaminaState _currentStaminaState;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes stamina and creates state instances.
    /// </summary>
    void Awake()
    {
        currentStamina = maxStamina; // Start with full stamina

        // Instantiate all concrete state objects
        _idleLandState = new IdleLandState();
        _normalSwimState = new NormalSwimState();
        _sprintSwimState = new SprintSwimState();
        _exhaustedState = new ExhaustedState();

        // Set initial state
        TransitionToState(_idleLandState);
    }

    /// <summary>
    /// Called once per frame.
    /// Delegates the update logic to the current state's ExecuteState method.
    /// </summary>
    void Update()
    {
        // Ensure there's a current state before executing
        _currentStaminaState?.ExecuteState(this);

        // Notify listeners about stamina changes
        OnStaminaChanged?.Invoke(currentStamina);
    }

    /// <summary>
    /// Transitions the system to a new stamina state.
    /// This is the core state transition mechanism.
    /// </summary>
    /// <param name="newState">The new IStaminaState to transition to.</param>
    public void TransitionToState(IStaminaState newState)
    {
        if (_currentStaminaState == newState) return; // Avoid re-entering the same state

        _currentStaminaState?.ExitState(this); // Call Exit on the old state
        _currentStaminaState = newState;       // Set the new state
        _currentStaminaState.EnterState(this); // Call Enter on the new state

        OnStateChanged?.Invoke(newState); // Notify listeners about state change
        Debug.Log($"Stamina System State Changed to: <color=yellow>{newState.GetType().Name}</color>");
    }

    // --- Public API for external components (e.g., Player Controller) to interact with ---

    /// <summary>
    /// Call this when the player enters water.
    /// </summary>
    public void EnterWater()
    {
        TransitionToState(_normalSwimState);
    }

    /// <summary>
    /// Call this when the player exits water or lands on solid ground.
    /// </summary>
    public void ExitWater()
    {
        TransitionToState(_idleLandState);
    }

    /// <summary>
    /// Call this when the player attempts to sprint while swimming.
    /// </summary>
    public void StartSprinting()
    {
        // Only allow sprinting if enough stamina and not already sprinting
        if (currentStamina > minStaminaForSprint && !(_currentStaminaState is SprintSwimState))
        {
            TransitionToState(_sprintSwimState);
        }
    }

    /// <summary>
    /// Call this when the player stops sprinting while swimming.
    /// </summary>
    public void StopSprinting()
    {
        // Only transition if currently sprinting
        if (_currentStaminaState is SprintSwimState)
        {
            TransitionToState(_normalSwimState);
        }
    }

    /// <summary>
    /// Checks if the player is currently in any swimming state (normal or sprint).
    /// </summary>
    public bool IsSwimming => _currentStaminaState is NormalSwimState || _currentStaminaState is SprintSwimState;

    /// <summary>
    /// Checks if the player is currently exhausted.
    /// </summary>
    public bool IsExhausted => _currentStaminaState is ExhaustedState;

    /// <summary>
    /// Provides the current movement speed multiplier based on the stamina state.
    /// </summary>
    /// <returns>1.0f normally, less when exhausted.</returns>
    public float GetMovementSpeedMultiplier()
    {
        return IsExhausted ? exhaustedPenaltyMoveSpeedMultiplier : 1.0f;
    }

    // --- Internal methods for states to modify stamina and interact with other systems ---
    // These are 'internal' to restrict access to the assembly, specifically to the states.

    /// <summary>
    /// Increases current stamina, capped at maxStamina.
    /// Called by states that regenerate stamina.
    /// </summary>
    internal void RegenerateStamina(float amount)
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
            // Optionally, add a slight delay before full regen after exhausting, etc.
        }
    }

    /// <summary>
    /// Decreases current stamina, floored at 0.
    /// Automatically transitions to ExhaustedState if stamina hits zero and not already exhausted.
    /// Called by states that drain stamina.
    /// </summary>
    internal void DrainStamina(float amount)
    {
        if (currentStamina > 0)
        {
            currentStamina = Mathf.Max(currentStamina - amount, 0f);
            if (currentStamina <= 0f && !(_currentStaminaState is ExhaustedState))
            {
                TransitionToState(_exhaustedState);
            }
        }
    }

    /// <summary>
    /// Placeholder for applying damage to the player's health system.
    /// In a real game, this would call a player health script.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    internal void ApplyDamage(float amount)
    {
        Debug.Log($"<color=red>Player taking {amount:F1} damage from exhaustion!</color>");
        // Example: GetComponent<PlayerHealth>().TakeDamage(amount);
        // Ensure PlayerHealth exists or handle null reference
    }

    // --- Getters for configuration values, used by states ---
    // These methods provide the state objects access to the system's configuration.
    internal float GetIdleRegenRate() => idleRegenRate;
    internal float GetNormalSwimDrainRate() => normalSwimDrainRate;
    internal float GetSprintSwimDrainRate() => sprintSwimDrainRate;
    internal float GetMinStaminaForSprint() => minStaminaForSprint;
    internal float GetExhaustedPenaltyMoveSpeedMultiplier() => exhaustedPenaltyMoveSpeedMultiplier;
    internal float GetExhaustedDamageRate() => exhaustedDamageRate;

    // --- State Instances Getters (for states to request transitions to specific states) ---
    // These are internal to prevent external scripts from directly changing states without proper methods.
    internal IdleLandState GetIdleLandStateInstance() => _idleLandState;
    internal NormalSwimState GetNormalSwimStateInstance() => _normalSwimState;
    internal SprintSwimState GetSprintSwimStateInstance() => _sprintSwimState;
    internal ExhaustedState GetExhaustedStateInstance() => _exhaustedState;


    // --- CONCRETE STAMINA STATES ---
    // These nested classes implement the IStaminaState interface, defining behavior for each state.

    /// <summary>
    /// Concrete State: Player is on land, stamina regenerates.
    /// </summary>
    public class IdleLandState : IStaminaState
    {
        public void EnterState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Entered IdleLandState.");
            // Optionally, stop any swimming animations or effects here
        }

        public void ExecuteState(SwimmingStaminaSystem system)
        {
            // Regenerate stamina over time
            system.RegenerateStamina(system.GetIdleRegenRate() * Time.deltaTime);

            // Transition to swimming is triggered externally by system.EnterWater()
        }

        public void ExitState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Exited IdleLandState.");
            // Optionally, play a splashing sound when entering water
        }
    }

    /// <summary>
    /// Concrete State: Player is swimming normally, stamina drains.
    /// </summary>
    public class NormalSwimState : IStaminaState
    {
        public void EnterState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Entered NormalSwimState.");
            // Optionally, start normal swimming animations or effects
        }

        public void ExecuteState(SwimmingStaminaSystem system)
        {
            // Drain stamina over time
            system.DrainStamina(system.GetNormalSwimDrainRate() * Time.deltaTime);

            // Transitions:
            // - To SprintSwimState: Triggered externally by system.StartSprinting()
            // - To ExhaustedState: Handled automatically by system.DrainStamina() if stamina hits 0
            // - To IdleLandState: Triggered externally by system.ExitWater()
        }

        public void ExitState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Exited NormalSwimState.");
            // Optionally, stop normal swimming animations
        }
    }

    /// <summary>
    /// Concrete State: Player is sprinting while swimming, stamina drains faster.
    /// </summary>
    public class SprintSwimState : IStaminaState
    {
        public void EnterState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Entered SprintSwimState.");
            // Optionally, start sprint swimming animations or effects
        }

        public void ExecuteState(SwimmingStaminaSystem system)
        {
            // Drain stamina faster over time
            system.DrainStamina(system.GetSprintSwimDrainRate() * Time.deltaTime);

            // If stamina drops below the sprint threshold, automatically stop sprinting.
            // This is an internal state-driven transition.
            if (system.CurrentStamina <= system.GetMinStaminaForSprint())
            {
                system.TransitionToState(system.GetNormalSwimStateInstance());
            }

            // Other Transitions:
            // - To ExhaustedState: Handled automatically by system.DrainStamina() if stamina hits 0
            // - To IdleLandState: Triggered externally by system.ExitWater()
            // - To NormalSwimState: Triggered externally by system.StopSprinting()
        }

        public void ExitState(SwimmingStaminaSystem system)
        {
            // Debug.Log("Exited SprintSwimState.");
            // Optionally, stop sprint swimming animations
        }
    }

    /// <summary>
    /// Concrete State: Player is exhausted (stamina at or near zero) in water.
    /// Player moves slower and takes damage.
    /// </summary>
    public class ExhaustedState : IStaminaState
    {
        public void EnterState(SwimmingStaminaSystem system)
        {
            Debug.Log("<color=red>Entered Exhausted State! Player is out of stamina and suffering penalties.</color>");
            // Implement visual/audio cues for exhaustion (e.g., screen tint, heavy breathing sound)
            // The movement penalty is handled by GetMovementSpeedMultiplier() called by PlayerMovement.
        }

        public void ExecuteState(SwimmingStaminaSystem system)
        {
            // While exhausted in water, player takes continuous damage.
            if (system.IsSwimming)
            {
                system.ApplyDamage(system.GetExhaustedDamageRate() * Time.deltaTime);
            }
            else // If somehow exhausted on land, or reached land while exhausted
            {
                // Transition back to IdleLandState if they are out of water and stamina isn't zero
                if (system.CurrentStamina > 0)
                {
                    system.TransitionToState(system.GetIdleLandStateInstance());
                    return; // Exit early to prevent further execution in this state
                }
            }

            // A mechanism for recovery: if player is still in water but somehow regains enough stamina
            // (e.g., from an external buff, or very slow, hidden regeneration),
            // they can transition back to normal swimming.
            // This example doesn't have internal regen in exhausted state.
            // If they reach land, ExitWater() will transition them.
        }

        public void ExitState(SwimmingStaminaSystem system)
        {
            Debug.Log("<color=green>Exited Exhausted State. Penalties removed.</color>");
            // Optionally, remove visual/audio cues for exhaustion
        }
    }
}


/*
/// <summary>
/// EXAMPLE USAGE: Player Input/Movement Controller
/// This script demonstrates how another component would interact with the SwimmingStaminaSystem.
/// Attach this (or integrate its logic) to your Player GameObject alongside the SwimmingStaminaSystem.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    private SwimmingStaminaSystem _staminaSystem;
    private CharacterController _characterController; // Or your custom movement component

    [Header("Player Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float sprintSwimSpeed = 6f;

    void Awake()
    {
        _staminaSystem = GetComponent<SwimmingStaminaSystem>();
        if (_staminaSystem == null)
        {
            Debug.LogError("PlayerInputController requires a SwimmingStaminaSystem component on the same GameObject!");
            enabled = false; // Disable if no stamina system
            return;
        }

        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            Debug.LogWarning("PlayerInputController could benefit from a CharacterController for movement example.");
            // Not essential for stamina demo, but good for movement demo
        }

        // Subscribe to stamina system events (optional, for UI updates or debug)
        _staminaSystem.OnStaminaChanged += UpdateStaminaUI;
        _staminaSystem.OnStateChanged += HandleStaminaStateChange;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (_staminaSystem != null)
        {
            _staminaSystem.OnStaminaChanged -= UpdateStaminaUI;
            _staminaSystem.OnStateChanged -= HandleStaminaStateChange;
        }
    }

    void Update()
    {
        HandlePlayerInput();
        MovePlayer();
    }

    void HandlePlayerInput()
    {
        // Example: Detect entering/exiting water (e.g., via trigger colliders)
        // For simplicity, let's use key presses for this example:
        if (Input.GetKeyDown(KeyCode.W)) // W for Water, imagine player just entered water
        {
            _staminaSystem.EnterWater();
        }
        if (Input.GetKeyDown(KeyCode.L)) // L for Land, imagine player just exited water
        {
            _staminaSystem.ExitWater();
        }

        // Handle sprinting input only if currently swimming
        if (_staminaSystem.IsSwimming)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _staminaSystem.StartSprinting();
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                _staminaSystem.StopSprinting();
            }
        }
        else // If not swimming, ensure sprint state is reset (though stamina system should handle this)
        {
            _staminaSystem.StopSprinting(); // Redundant, but safe
        }
    }

    void MovePlayer()
    {
        // Basic movement example using CharacterController
        if (_characterController == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        float currentSpeed = 0f;

        if (_staminaSystem.IsSwimming)
        {
            if (_staminaSystem.CurrentState is SwimmingStaminaSystem.SprintSwimState)
            {
                currentSpeed = sprintSwimSpeed;
            }
            else // NormalSwimState or ExhaustedState in water
            {
                currentSpeed = swimSpeed;
            }
        }
        else // IdleLandState
        {
            currentSpeed = walkSpeed;
        }

        // Apply exhaustion penalty to speed
        currentSpeed *= _staminaSystem.GetMovementSpeedMultiplier();

        _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    // --- Event Handlers (for UI updates, logging, or other system interactions) ---

    void UpdateStaminaUI(float currentStamina)
    {
        // Example: Update a UI slider or text component
        // Debug.Log($"Stamina: {currentStamina:F1}/{_staminaSystem.MaxStamina:F1}");
        // UIManager.Instance.SetStaminaBar(currentStamina / _staminaSystem.MaxStamina);
    }

    void HandleStaminaStateChange(IStaminaState newState)
    {
        // Example: Change player animation, play sounds, or update status indicators
        // Debug.Log($"Player stamina state is now: {newState.GetType().Name}");
        // PlayerAnimationController.SetStaminaState(newState.GetType().Name);
    }
}

/// <summary>
/// EXAMPLE USAGE: Simple UI Display (optional)
/// Attach this to a UI Text or Slider GameObject to visualize stamina and state.
/// Requires a TextMeshProUGUI or Slider component.
/// </summary>
public class StaminaUIDisplay : MonoBehaviour
{
    public UnityEngine.UI.Text staminaText; // Assign a UI Text component here
    public UnityEngine.UI.Slider staminaSlider; // Assign a UI Slider component here
    public UnityEngine.UI.Text stateText; // Assign a UI Text component for state

    [SerializeField]
    private SwimmingStaminaSystem targetStaminaSystem; // Assign the Player's SwimmingStaminaSystem here

    void Start()
    {
        if (targetStaminaSystem == null)
        {
            Debug.LogError("StaminaUIDisplay: Target Stamina System not assigned!", this);
            enabled = false;
            return;
        }

        // Initial update
        UpdateStaminaUI(targetStaminaSystem.CurrentStamina);
        HandleStaminaStateChange(targetStaminaSystem.CurrentState);

        // Subscribe to events
        targetStaminaSystem.OnStaminaChanged += UpdateStaminaUI;
        targetStaminaSystem.OnStateChanged += HandleStaminaStateChange;
    }

    void OnDestroy()
    {
        if (targetStaminaSystem != null)
        {
            targetStaminaSystem.OnStaminaChanged -= UpdateStaminaUI;
            targetStaminaSystem.OnStateChanged -= HandleStaminaStateChange;
        }
    }

    void UpdateStaminaUI(float currentStamina)
    {
        if (staminaText != null)
        {
            staminaText.text = $"Stamina: {currentStamina:F0}/{targetStaminaSystem.MaxStamina:F0}";
        }
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = targetStaminaSystem.MaxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void HandleStaminaStateChange(IStaminaState newState)
    {
        if (stateText != null)
        {
            stateText.text = $"State: {newState.GetType().Name}";
        }
    }
}
*/
```