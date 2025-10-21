// Unity Design Pattern Example: SprintStaminaSystem
// This script demonstrates the SprintStaminaSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script demonstrates the **SprintStaminaSystem** design pattern. This pattern focuses on encapsulating all related stamina logic into a single, modular, and reusable component, making it easy to integrate into various player characters or entities.

## SprintStaminaSystem: A Component Design Pattern for Game Mechanics

The `SprintStaminaSystem` component provides a self-contained solution for managing a character's ability to sprint based on a stamina resource.

**Key Principles Demonstrated:**

1.  **Encapsulation**: All stamina-related data (current, max, rates) and behavior (consumption, regeneration, depletion cooldown) are managed internally by this single script. External systems don't need to know the implementation details.
2.  **Modularity & Reusability**: The script is designed to be a standalone Unity component. You can attach it to any player or AI character that needs a sprint-stamina mechanic without modification.
3.  **Clear API**: It provides simple public methods (`StartSprint()`, `StopSprint()`) and properties (`CanSprint`, `CurrentStamina`) for other scripts (like a `PlayerController`) to interact with it.
4.  **Decoupling via Events**: It uses C# events (`OnStaminaChanged`, `OnStaminaDepleted`, `OnStaminaRecovered`, `OnSprintStatusChanged`) to notify other systems (like UI, sound, or visual effects) about state changes. This prevents direct dependencies and makes the system flexible.
5.  **Configurability**: All important parameters (stamina amount, rates, delays) are exposed in the Unity Inspector, allowing designers to easily tune the game feel.

---

### SprintStaminaSystem.cs

```csharp
using UnityEngine;
using System; // Required for Action delegate (used for events)

// Add a menu item to make it easier to add this component in the Editor
[AddComponentMenu("Game Systems/Sprint Stamina System")]

/// <summary>
///     The 'SprintStaminaSystem' design pattern encapsulates all logic related to a player's
///     sprinting stamina management into a single, reusable component.
///
///     This pattern promotes:
///     1.  **Encapsulation**: All stamina-related state (current stamina, max stamina, rates)
///         and behavior (consumption, regeneration, depletion cooldown) are contained within this class.
///         Other scripts (e.g., PlayerController, UI) don't need to know *how* stamina works,
///         only *what* they can do with it (check if can sprint, start sprint, stop sprint).
///     2.  **Modularity/Reusability**: This component can be dropped onto any GameObject (e.g., Player)
///         that needs a sprint stamina mechanic. It's independent of the player's movement
///         or input system, communicating via a clean API and events.
///     3.  **Maintainability**: Changes to the stamina rules (e.g., new depletion mechanics,
///         different regeneration curves) only affect this one script.
///     4.  **Decoupling**: Events (`OnStaminaChanged`, `OnStaminaDepleted`, etc.) allow
///         UI elements, sound systems, or other game logic to react to stamina changes
///         without having direct references to this system.
///
///     **How to use this pattern:**
///     1.  Attach this script to your player GameObject.
///     2.  Configure the stamina parameters in the Inspector (Max Stamina, Consumption/Regeneration Rates).
///     3.  Your PlayerController script will interact with this system via its public methods
///         (e.g., `StartSprint()`, `StopSprint()`) and properties (`CanSprint`).
///     4.  Your UI script can subscribe to the `OnStaminaChanged` event to update a stamina bar.
///
///     This is a common approach for building robust, modular game mechanics in Unity.
/// </summary>
public class SprintStaminaSystem : MonoBehaviour
{
    [Header("Stamina Configuration")]
    [Tooltip("The maximum amount of stamina this character can have.")]
    [SerializeField]
    private float _maxStamina = 100f;
    public float MaxStamina => _maxStamina; // Public read-only access to MaxStamina

    [Tooltip("The rate at which stamina is consumed per second while sprinting.")]
    [SerializeField]
    private float _sprintStaminaConsumptionRate = 15f; // Stamina per second

    [Tooltip("The rate at which stamina regenerates per second when not sprinting and not depleted.")]
    [SerializeField]
    private float _staminaRegenerationRate = 10f; // Stamina per second

    [Tooltip("The minimum amount of stamina required to initiate or continue sprinting.")]
    [SerializeField]
    private float _minimumStaminaToSprint = 5f;

    [Tooltip("The delay (in seconds) before stamina starts regenerating after being completely depleted (hits 0).")]
    [SerializeField]
    private float _depletionRechargeDelay = 2f;

    [Header("Current Stamina State (Read Only)")]
    [Tooltip("The current amount of stamina.")]
    [SerializeField]
    private float _currentStamina; // Backing field for CurrentStamina property

    [Tooltip("Is the character currently attempting to sprint?")]
    [SerializeField]
    private bool _isSprinting = false; // Internal flag for sprint attempt

    [Tooltip("Has stamina been fully depleted and is currently waiting for recharge delay?")]
    [SerializeField]
    private bool _isStaminaDepleted = false; // True if stamina hit 0 and is in cooldown

    private float _rechargeCooldownTimer = 0f; // Timer for depletion delay

    // =====================================================================================
    // PUBLIC PROPERTIES (Read-Only Access to State)
    // =====================================================================================

    /// <summary>
    /// Gets the current stamina value.
    /// </summary>
    public float CurrentStamina => _currentStamina;

    /// <summary>
    /// Gets a value indicating whether the character is currently trying to sprint.
    /// This is set by calling StartSprint() / StopSprint().
    /// </summary>
    public bool IsSprinting => _isSprinting;

    /// <summary>
    /// Gets a value indicating whether stamina is currently depleted and undergoing a recharge delay.
    /// </summary>
    public bool IsStaminaDepleted => _isStaminaDepleted;

    /// <summary>
    /// Gets a value indicating whether the character *can* currently sprint based on
    /// stamina levels and depletion status. This is the primary check for a PlayerController.
    /// </summary>
    public bool CanSprint => _currentStamina > _minimumStaminaToSprint && !_isStaminaDepleted;


    // =====================================================================================
    // EVENTS (For Decoupling and UI Updates)
    // =====================================================================================

    /// <summary>
    /// Event fired whenever the current stamina value changes.
    /// Subscribers (e.g., UI scripts) can use this to update stamina bars.
    /// Parameters: (currentStamina, maxStamina)
    /// </summary>
    public event Action<float, float> OnStaminaChanged;

    /// <summary>
    /// Event fired when stamina is completely depleted (hits 0).
    /// </summary>
    public event Action OnStaminaDepleted;

    /// <summary>
    /// Event fired when stamina is no longer depleted (after the recharge delay has passed).
    /// </summary>
    public event Action OnStaminaRecovered;

    /// <summary>
    /// Event fired when the sprint status (attempting to sprint or not) changes.
    /// Parameters: (isSprinting)
    /// </summary>
    public event Action<bool> OnSprintStatusChanged;

    // =====================================================================================
    // UNITY LIFECYCLE METHODS
    // =====================================================================================

    private void Awake()
    {
        // Initialize stamina to full on start
        _currentStamina = _maxStamina;
        // Notify any listeners of the initial state
        OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
    }

    private void Update()
    {
        HandleStaminaLogic();
    }

    // =====================================================================================
    // PUBLIC API (For PlayerController or other systems to interact)
    // =====================================================================================

    /// <summary>
    /// Instructs the system that the character is attempting to sprint.
    /// The system will then consume stamina if possible.
    /// </summary>
    public void StartSprint()
    {
        if (!_isSprinting)
        {
            _isSprinting = true;
            OnSprintStatusChanged?.Invoke(true);
            // Debug.Log("Sprint initiated.");
        }
    }

    /// <summary>
    /// Instructs the system that the character is no longer attempting to sprint.
    /// The system will then regenerate stamina if not depleted.
    /// </summary>
    public void StopSprint()
    {
        if (_isSprinting)
        {
            _isSprinting = false;
            OnSprintStatusChanged?.Invoke(false);
            // Debug.Log("Sprint stopped.");
        }
    }

    // =====================================================================================
    // PRIVATE HELPER METHODS (Internal Logic)
    // =====================================================================================

    /// <summary>
    /// Centralized method to handle all stamina consumption and regeneration logic.
    /// Called every frame by Update().
    /// </summary>
    private void HandleStaminaLogic()
    {
        // 1. Handle depletion cooldown if stamina hit zero
        if (_isStaminaDepleted)
        {
            _rechargeCooldownTimer -= Time.deltaTime;
            if (_rechargeCooldownTimer <= 0f)
            {
                // Cooldown finished, stamina can now regenerate
                _isStaminaDepleted = false;
                OnStaminaRecovered?.Invoke();
                Debug.Log("<color=green>Stamina recovered from depletion cooldown!</color>");
            }
            // While in cooldown, no stamina changes (neither consume nor regenerate)
            return;
        }

        // 2. Decide whether to consume or regenerate stamina
        if (_isSprinting && CanSprint)
        {
            // Sprinting and has enough stamina (and not depleted)
            SetStamina(_currentStamina - _sprintStaminaConsumptionRate * Time.deltaTime);
        }
        else
        {
            // Not sprinting, or tried to sprint but couldn't (not enough stamina / depleted)
            // Regenerate stamina only if not fully depleted (handled by the 'if (_isStaminaDepleted)' block)
            SetStamina(_currentStamina + _staminaRegenerationRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// Sets the current stamina value, clamps it within [0, MaxStamina],
    /// and triggers the OnStaminaChanged event if the value actually changed.
    /// Also handles `_isStaminaDepleted` state transitions.
    /// </summary>
    /// <param name="newValue">The new desired stamina value.</param>
    private void SetStamina(float newValue)
    {
        float oldValue = _currentStamina;
        _currentStamina = Mathf.Clamp(newValue, 0f, _maxStamina);

        // Only invoke event if stamina value actually changed (to avoid spamming for tiny float changes, or when clamped)
        // Using Mathf.Approximately for robust float comparison.
        if (!Mathf.Approximately(oldValue, _currentStamina))
        {
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            // Debug.Log($"Stamina: {_currentStamina:F1}/{_maxStamina:F1}"); // Uncomment for detailed stamina logging
        }

        // Check for stamina depletion state
        if (_currentStamina <= 0.001f && !_isStaminaDepleted) // Use small epsilon for float comparison to catch near-zero
        {
            _isStaminaDepleted = true;
            _rechargeCooldownTimer = _depletionRechargeDelay;
            OnStaminaDepleted?.Invoke();
            Debug.Log("<color=red>Stamina completely depleted! Starting recharge delay.</color>");
            // Automatically stop sprinting if stamina runs out, ensures other systems are notified.
            StopSprint();
        }
        // Note: OnStaminaRecovered is fired when _rechargeCooldownTimer finishes in HandleStaminaLogic().
    }

    // =====================================================================================
    // EXAMPLE USAGE IN COMMENTS
    // =====================================================================================

    /*
    // --- EXAMPLE PLAYER CONTROLLER USAGE ---
    // (Attach this script to your Player GameObject, then add a PlayerController component)

    using UnityEngine;

    public class PlayerController : MonoBehaviour
    {
        private SprintStaminaSystem _staminaSystem;
        private CharacterController _characterController; // Assuming you have a character controller for movement

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 6f;

        void Awake()
        {
            // Get reference to the SprintStaminaSystem component on the same GameObject
            _staminaSystem = GetComponent<SprintStaminaSystem>();
            _characterController = GetComponent<CharacterController>();

            if (_staminaSystem == null)
            {
                Debug.LogError("PlayerController requires a SprintStaminaSystem component!", this);
                enabled = false; // Disable this script if the required component is missing
                return;
            }
            if (_characterController == null)
            {
                Debug.LogError("PlayerController requires a CharacterController component!", this);
                enabled = false;
                return;
            }

            // Optional: Subscribe to sprint status changes for player-specific effects (e.g., animation)
            _staminaSystem.OnSprintStatusChanged += OnSprintStatusChanged;
        }

        void OnDestroy()
        {
            // Always unsubscribe from events to prevent memory leaks, especially when objects are destroyed
            if (_staminaSystem != null)
            {
                _staminaSystem.OnSprintStatusChanged -= OnSprintStatusChanged;
            }
        }

        void Update()
        {
            // Get input for movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calculate movement direction relative to the player's forward/right vectors
            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            if (moveDirection.magnitude > 1f) moveDirection.Normalize(); // Prevent faster diagonal movement

            float currentSpeed = _walkSpeed;

            // Determine if the player is attempting to sprint and if the stamina system allows it
            bool wantsToSprint = Input.GetKey(KeyCode.LeftShift);

            if (wantsToSprint && _staminaSystem.CanSprint)
            {
                _staminaSystem.StartSprint(); // Tell the stamina system we are trying to sprint
                currentSpeed = _sprintSpeed;
            }
            else
            {
                _staminaSystem.StopSprint(); // Tell the stamina system we are not sprinting
                // Note: If player was sprinting and _staminaSystem.CanSprint became false (due to depletion),
                // the SprintStaminaSystem itself will call StopSprint(), ensuring consistency.
                // This `else` block also catches when the player simply releases LeftShift.
            }

            // Apply movement (adjust for CharacterController's Move method requirements)
            // Assuming gravity and vertical movement are handled elsewhere or not critical for this example
            Vector3 finalMovement = moveDirection * currentSpeed * Time.deltaTime;
            _characterController.Move(finalMovement); // CharacterController.Move expects world space delta
        }

        private void OnSprintStatusChanged(bool isSprinting)
        {
            // Example: Trigger sprint/idle animations, play footstep sounds, etc.
            // Debug.Log($"PlayerController: Sprint status changed to {isSprinting}");
        }
    }

    // --- EXAMPLE UI STAMINA BAR USAGE ---
    // (Attach this script to a UI Canvas GameObject, add a Slider component named 'StaminaBarSlider')

    using UnityEngine.UI; // Required for UI elements like Slider
    using UnityEngine; // Also for Debug.Log

    public class UIStaminaBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider _staminaBarSlider;
        [SerializeField] private Image _fillImage; // Optional: For changing color
        [SerializeField] private Color _normalColor = Color.green;
        [SerializeField] private Color _depletedColor = Color.red;

        [Header("System Reference")]
        [Tooltip("Drag your Player's SprintStaminaSystem component here in the Inspector.")]
        [SerializeField] private SprintStaminaSystem _playerStaminaSystem; 

        void Awake()
        {
            // Basic validation for UI references
            if (_staminaBarSlider == null)
            {
                _staminaBarSlider = GetComponent<Slider>();
                if (_staminaBarSlider == null)
                {
                    Debug.LogError("UIStaminaBar requires a Slider component or a reference to one!", this);
                    enabled = false;
                    return;
                }
            }
            if (_fillImage == null && _staminaBarSlider.fillRect != null)
            {
                 _fillImage = _staminaBarSlider.fillRect.GetComponent<Image>();
            }

            // Basic validation for the stamina system reference
            if (_playerStaminaSystem == null)
            {
                Debug.LogError("UIStaminaBar requires a reference to the Player's SprintStaminaSystem!", this);
                enabled = false;
                return;
            }

            // Subscribe to the stamina events
            _playerStaminaSystem.OnStaminaChanged += UpdateStaminaUI;
            _playerStaminaSystem.OnStaminaDepleted += OnStaminaDepletedUI;
            _playerStaminaSystem.OnStaminaRecovered += OnStaminaRecoveredUI;

            // Initialize UI with the current stamina state
            UpdateStaminaUI(_playerStaminaSystem.CurrentStamina, _playerStaminaSystem.MaxStamina);
        }

        void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks when the UI object is destroyed
            if (_playerStaminaSystem != null)
            {
                _playerStaminaSystem.OnStaminaChanged -= UpdateStaminaUI;
                _playerStaminaSystem.OnStaminaDepleted -= OnStaminaDepletedUI;
                _playerStaminaSystem.OnStaminaRecovered -= OnStaminaRecoveredUI;
            }
        }

        /// <summary>
        /// Updates the UI slider whenever stamina changes.
        /// This method is an event handler for OnStaminaChanged.
        /// </summary>
        private void UpdateStaminaUI(float currentStamina, float maxStamina)
        {
            _staminaBarSlider.maxValue = maxStamina;
            _staminaBarSlider.value = currentStamina;
            // Optionally change color based on sprint ability, not just depletion state
            if (_fillImage != null)
            {
                 _fillImage.color = _playerStaminaSystem.CanSprint ? _normalColor : Color.Lerp(_depletedColor, _normalColor, currentStamina / maxStamina);
            }
        }

        /// <summary>
        /// Handles UI reactions when stamina is completely depleted.
        /// This method is an event handler for OnStaminaDepleted.
        /// </summary>
        private void OnStaminaDepletedUI()
        {
            Debug.Log("UI: Stamina bar should flash red or play a depletion sound!");
            if (_fillImage != null)
            {
                _fillImage.color = _depletedColor; // Set to red when depleted
            }
            // Example: Play a 'stamina out' sound effect
        }

        /// <summary>
        /// Handles UI reactions when stamina recovers from the depleted state.
        /// This method is an event handler for OnStaminaRecovered.
        /// </summary>
        private void OnStaminaRecoveredUI()
        {
            Debug.Log("UI: Stamina bar back to normal, maybe a sound effect for recovery!");
            if (_fillImage != null)
            {
                _fillImage.color = _normalColor; // Restore to normal color
            }
            // Example: Play a 'stamina recovered' sound effect
        }
    }
    */
}
```