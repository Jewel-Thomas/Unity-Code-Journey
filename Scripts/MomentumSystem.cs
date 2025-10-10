// Unity Design Pattern Example: MomentumSystem
// This script demonstrates the MomentumSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'MomentumSystem' design pattern in Unity allows you to create systems where an entity's performance, abilities, or state build up over time or through specific actions, and then gradually decay when those conditions are no longer met. This creates a dynamic gameplay loop where players are rewarded for sustained performance or tactical decisions.

A common real-world use case is a player character's movement speed that accelerates as they run and slowly decelerates when they stop, rather than instantly changing speed. Other examples include combo meters, rage systems, or temporary buffs that require continuous action to maintain.

This example provides a generic `MomentumSystem` script and a `MomentumControlledMover` script that demonstrates how to integrate it into player movement.

---

### **1. `MomentumSystem.cs`**

This script defines the core logic for managing momentum: how it builds, decays, and notifies other systems of its state changes.

```csharp
using UnityEngine;
using System; // Required for Action events

/// <summary>
/// The MomentumSystem design pattern provides a robust and reusable way to manage
/// a dynamic value (momentum) that builds up based on specific inputs/actions
/// and decays naturally over time.
///
/// Use Cases:
/// - Player movement acceleration/deceleration.
/// - Combo meters that decay if no actions are performed.
/// - "Rage" or "Adrenaline" systems that build with combat and decay out of it.
/// - Charge-up abilities or temporary power-ups.
///
/// This script manages the state of momentum (current value, max, min, build/decay rates).
/// It uses events to notify other components when momentum changes, promoting
/// loose coupling between systems.
/// </summary>
[DisallowMultipleComponent] // Ensures only one MomentumSystem can exist on a GameObject
public class MomentumSystem : MonoBehaviour
{
    [Header("Momentum Settings")]
    [Tooltip("The current momentum value. Read-only in Inspector during play.")]
    [SerializeField]
    private float _currentMomentum = 0f;

    [Tooltip("The maximum momentum value this system can accumulate.")]
    [SerializeField]
    private float _maxMomentum = 100f;

    [Tooltip("How quickly momentum builds up when 'AddMomentum' is called by sources.")]
    [SerializeField]
    private float _buildRate = 10f; // Momentum units per second of input/action contribution

    [Tooltip("How quickly momentum naturally decays over time when conditions for decay are met.")]
    [SerializeField]
    private float _decayRate = 5f; // Momentum units per second

    [Tooltip("The minimum momentum value. Often 0, but could be negative for 'debt' systems.")]
    [SerializeField]
    private float _minMomentum = 0f;

    [Tooltip("Momentum value must be above this threshold to be considered 'active' or to have an effect (e.g., enable a special ability).")]
    [SerializeField]
    private float _activeThreshold = 10f;

    [Tooltip("If true, momentum only decays when no build sources are active and the decay delay has passed. If false, it decays constantly (even while building) but build rate might offset it.")]
    [SerializeField]
    private bool _decayOnlyWhenInactive = true;

    [Tooltip("A small delay before momentum starts decaying after active building stops. Improves feel.")]
    [SerializeField] private float _decayDelay = 0.5f;

    // Internal tracking for when momentum is actively being built this frame
    private bool _isBuildingMomentumThisFrame = false;
    private float _currentDecayTimer = 0f; // Counts down the decay delay

    // --- Public Properties (Read-Only Access) ---
    public float CurrentMomentum => _currentMomentum;
    public float MaxMomentum => _maxMomentum;
    public float MinMomentum => _minMomentum;
    public float BuildRate => _buildRate;
    public float DecayRate => _decayRate;
    public float ActiveThreshold => _activeThreshold;
    public bool IsMomentumActive => _currentMomentum >= _activeThreshold;
    public float MomentumNormalized => Mathf.InverseLerp(_minMomentum, _maxMomentum, _currentMomentum); // 0-1 range

    // --- Events ---
    // Events allow other scripts to react to momentum changes without direct coupling.
    // They are a core part of making the MomentumSystem flexible and reusable.

    /// <summary>
    /// Event fired whenever the momentum value changes.
    /// Provides the current momentum and the max momentum.
    /// </summary>
    public event Action<float, float> OnMomentumChanged;

    /// <summary>
    /// Event fired when momentum crosses the active threshold from below to above.
    /// </summary>
    public event Action OnMomentumThresholdReached;

    /// <summary>
    /// Event fired when momentum drops below the active threshold from above.
    /// </summary>
    public event Action OnMomentumThresholdLost;

    private bool _wasMomentumActiveLastFrame = false; // Internal state to detect threshold crossing

    void Start()
    {
        // Ensure momentum starts within valid bounds.
        _currentMomentum = Mathf.Clamp(_currentMomentum, _minMomentum, _maxMomentum);
        _wasMomentumActiveLastFrame = IsMomentumActive; // Initialize threshold state
    }

    void Update()
    {
        HandleMomentumDecay();
        CheckThresholdEvents();

        // Reset the build flag at the end of the frame.
        // If AddMomentum is called next frame, it will be set to true again.
        _isBuildingMomentumThisFrame = false;
    }

    /// <summary>
    /// Checks if momentum has crossed the active threshold and fires the appropriate events.
    /// </summary>
    private void CheckThresholdEvents()
    {
        bool currentMomentumActive = IsMomentumActive;

        if (currentMomentumActive && !_wasMomentumActiveLastFrame)
        {
            OnMomentumThresholdReached?.Invoke();
            // Debug.Log($"{gameObject.name}: Momentum threshold reached at {_currentMomentum:F0}!");
        }
        else if (!currentMomentumActive && _wasMomentumActiveLastFrame)
        {
            OnMomentumThresholdLost?.Invoke();
            // Debug.Log($"{gameObject.name}: Momentum threshold lost at {_currentMomentum:F0}.");
        }

        _wasMomentumActiveLastFrame = currentMomentumActive;
    }

    /// <summary>
    /// Manages the natural decay of momentum based on configured rules.
    /// </summary>
    private void HandleMomentumDecay()
    {
        // If momentum was actively built this frame, reset the decay timer.
        if (_isBuildingMomentumThisFrame)
        {
            _currentDecayTimer = _decayDelay;
        }
        else
        {
            // If not building, count down the decay delay.
            if (_currentDecayTimer > 0)
            {
                _currentDecayTimer -= Time.deltaTime;
            }

            // Once the delay is over, apply decay if conditions allow.
            if (_currentDecayTimer <= 0)
            {
                // Decay conditions:
                // 1. If _decayOnlyWhenInactive is false (always decay)
                // OR
                // 2. If _decayOnlyWhenInactive is true AND momentum is above min AND it's NOT considered 'active' (e.g., below threshold).
                //    This allows active momentum to persist longer, only decaying when below active threshold.
                if (!_decayOnlyWhenInactive || (_currentMomentum > _minMomentum && !IsMomentumActive))
                {
                    float previousMomentum = _currentMomentum;
                    _currentMomentum -= _decayRate * Time.deltaTime;
                    _currentMomentum = Mathf.Clamp(_currentMomentum, _minMomentum, _maxMomentum);

                    // Only invoke event if the value actually changed to prevent unnecessary calls
                    if (!Mathf.Approximately(previousMomentum, _currentMomentum))
                    {
                        OnMomentumChanged?.Invoke(_currentMomentum, _maxMomentum);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds momentum to the system. This method is called by 'Momentum Sources'
    /// (e.g., player input, attacking, collecting items).
    /// </summary>
    /// <param name="baseAmount">The raw amount of momentum to contribute. This is scaled by buildRate and Time.deltaTime.</param>
    public void AddMomentum(float baseAmount)
    {
        float actualAmount = baseAmount * _buildRate * Time.deltaTime;
        float previousMomentum = _currentMomentum;
        _currentMomentum += actualAmount;
        _currentMomentum = Mathf.Clamp(_currentMomentum, _minMomentum, _maxMomentum);

        if (!Mathf.Approximately(previousMomentum, _currentMomentum))
        {
            OnMomentumChanged?.Invoke(_currentMomentum, _maxMomentum);
        }

        // Mark that momentum was actively built this frame, which will reset the decay timer.
        _isBuildingMomentumThisFrame = true;
    }

    /// <summary>
    /// Removes momentum from the system, potentially due to negative actions or specific costs.
    /// (e.g., taking damage, using a special ability, failing an action).
    /// </summary>
    /// <param name="amount">The absolute amount of momentum to remove.</param>
    public void RemoveMomentum(float amount)
    {
        float previousMomentum = _currentMomentum;
        _currentMomentum -= amount;
        _currentMomentum = Mathf.Clamp(_currentMomentum, _minMomentum, _maxMomentum);

        if (!Mathf.Approximately(previousMomentum, _currentMomentum))
        {
            OnMomentumChanged?.Invoke(_currentMomentum, _maxMomentum);
        }
    }

    /// <summary>
    /// Sets the momentum directly to a specific value.
    /// Useful for instant changes, cheat codes, or specific game state transitions.
    /// </summary>
    /// <param name="value">The desired momentum value.</param>
    public void SetMomentum(float value)
    {
        float previousMomentum = _currentMomentum;
        _currentMomentum = Mathf.Clamp(value, _minMomentum, _maxMomentum);

        if (!Mathf.Approximately(previousMomentum, _currentMomentum))
        {
            OnMomentumChanged?.Invoke(_currentMomentum, _maxMomentum);
        }
    }

    /// <summary>
    /// Resets momentum to its minimum value (usually 0).
    /// </summary>
    public void ResetMomentum()
    {
        SetMomentum(_minMomentum);
    }

    // --- Example Usage in another script (MomentumControlledMover.cs below) ---
    /*
    // To get momentum value:
    float currentMomentum = myMomentumSystem.CurrentMomentum;
    bool isActive = myMomentumSystem.IsMomentumActive;

    // To add momentum:
    myMomentumSystem.AddMomentum(1f); // 1f is a base contribution factor

    // To remove momentum:
    myMomentumSystem.RemoveMomentum(20f); // Remove 20 momentum units

    // To subscribe to changes (e.g., for UI or effects):
    void OnEnable() {
        if (myMomentumSystem != null) {
            myMomentumSystem.OnMomentumChanged += HandleMomentumUpdate;
            myMomentumSystem.OnMomentumThresholdReached += OnMomentumActive;
        }
    }

    void OnDisable() {
        if (myMomentumSystem != null) {
            myMomentumSystem.OnMomentumChanged -= HandleMomentumUpdate;
            myMomentumSystem.OnMomentumThresholdReached -= OnMomentumActive;
        }
    }

    void HandleMomentumUpdate(float current, float max) {
        Debug.Log($"Momentum updated to {current}/{max}");
    }

    void OnMomentumActive() {
        Debug.Log("Momentum is now active!");
    }
    */
}
```

---

### **2. `MomentumControlledMover.cs`**

This script demonstrates how to *use* the `MomentumSystem` to control a player character's movement speed. It acts as both a **Momentum Source** (adding momentum when the player moves) and a **Momentum Consumer** (using the momentum value to calculate speed).

```csharp
using UnityEngine;
using TMPro; // For TextMeshPro UI, if used for display

/// <summary>
/// This script demonstrates a practical use case for the MomentumSystem: a character mover
/// whose speed is dynamically influenced by accumulated momentum.
///
/// It exemplifies how to:
/// 1. Get a reference to the MomentumSystem.
/// 2. Act as a 'Momentum Source' by calling AddMomentum based on player input.
/// 3. Act as a 'Momentum Consumer' by reading CurrentMomentum to calculate movement speed.
/// 4. Subscribe to MomentumSystem events for UI updates or triggering effects.
/// </summary>
[RequireComponent(typeof(MomentumSystem))] // Ensures a MomentumSystem exists on the same GameObject
[RequireComponent(typeof(Rigidbody))]      // Required for physics-based movement
public class MomentumControlledMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The base movement speed when momentum is at its minimum.")]
    [SerializeField]
    private float _baseSpeed = 5f;

    [Tooltip("The maximum additional speed gained when momentum is at its maximum.")]
    [SerializeField]
    private float _maxMomentumSpeedBonus = 10f; // E.g., if base is 5 and bonus is 10, max speed is 15.

    [Tooltip("How smoothly the character rotates to face the movement direction.")]
    [SerializeField]
    private float _turnSpeed = 10f;

    [Tooltip("How quickly the character's current speed adjusts to the target speed (based on momentum).")]
    [SerializeField]
    private float _speedLerpRate = 5f;

    [Header("Momentum Contribution")]
    [Tooltip("The base factor for how much momentum is added per frame when the character is moving.")]
    [SerializeField]
    private float _movementMomentumContribution = 1f; // This value is multiplied by MomentumSystem's Build Rate

    // Private references to components
    private MomentumSystem _momentumSystem;
    private Rigidbody _rb;
    private Vector3 _movementInput;
    private float _currentCalculatedSpeed; // The actual speed currently applied to the Rigidbody

    [Header("UI Feedback (Optional)")]
    [Tooltip("Assign a TextMeshProUGUI component here to display momentum text.")]
    [SerializeField]
    private TextMeshProUGUI _momentumText;
    [Tooltip("Assign a UI Image component here to use as a momentum fill bar.")]
    [SerializeField]
    private UnityEngine.UI.Image _momentumFillBar;

    void Awake()
    {
        // Get references to required components
        _momentumSystem = GetComponent<MomentumSystem>();
        _rb = GetComponent<Rigidbody>();

        if (_rb == null)
        {
            Debug.LogError("MomentumControlledMover requires a Rigidbody component.", this);
            enabled = false; // Disable script if Rigidbody is missing
            return;
        }

        // Freeze rotation to prevent unwanted tilting due to physics or movement
        _rb.freezeRotation = true;
    }

    void OnEnable()
    {
        // Subscribe to momentum changes to update UI or trigger effects.
        // This makes the mover react to momentum changes from ANY source, not just its own input.
        if (_momentumSystem != null)
        {
            _momentumSystem.OnMomentumChanged += UpdateMomentumUI;
            _momentumSystem.OnMomentumThresholdReached += HandleMomentumActive;
            _momentumSystem.OnMomentumThresholdLost += HandleMomentumInactive;
        }
    }

    void OnDisable()
    {
        // Always unsubscribe from events to prevent memory leaks or issues when the GameObject is disabled/destroyed.
        if (_momentumSystem != null)
        {
            _momentumSystem.OnMomentumChanged -= UpdateMomentumUI;
            _momentumSystem.OnMomentumThresholdReached -= HandleMomentumActive;
            _momentumSystem.OnMomentumThresholdLost -= HandleMomentumInactive;
        }
    }

    void Update()
    {
        HandleInput();
        CalculateCurrentSpeed(); // Calculate target speed based on momentum
    }

    void FixedUpdate()
    {
        // Physics-related movement should happen in FixedUpdate for consistency.
        ApplyMovement();
    }

    /// <summary>
    /// Reads player input and adds momentum if moving.
    /// </summary>
    private void HandleInput()
    {
        _movementInput.x = Input.GetAxisRaw("Horizontal");
        _movementInput.z = Input.GetAxisRaw("Vertical");
        _movementInput.y = 0; // Ensure no vertical input for ground movement

        // Normalize input to prevent faster diagonal movement
        if (_movementInput.magnitude > 1f)
        {
            _movementInput.Normalize();
        }

        // If there's significant movement input, contribute to momentum.
        // A small threshold (0.1f) prevents momentum build-up from tiny joystick drifts.
        if (_movementInput.magnitude > 0.1f)
        {
            _momentumSystem.AddMomentum(_movementMomentumContribution);
        }
        // The MomentumSystem handles natural decay, so no explicit RemoveMomentum call here for stopping.
        // However, other systems (e.g., combat) could call _momentumSystem.RemoveMomentum() for penalties.
    }

    /// <summary>
    /// Calculates the target speed based on the current momentum value.
    /// </summary>
    private void CalculateCurrentSpeed()
    {
        // The speed scales from _baseSpeed up to (_baseSpeed + _maxMomentumSpeedBonus)
        // based on the normalized momentum (0 to 1).
        float targetSpeed = _baseSpeed + (_momentumSystem.MomentumNormalized * _maxMomentumSpeedBonus);

        // Smoothly interpolate the actual applied speed towards the target speed.
        _currentCalculatedSpeed = Mathf.Lerp(_currentCalculatedSpeed, targetSpeed, Time.deltaTime * _speedLerpRate);
    }

    /// <summary>
    /// Applies movement and rotation to the Rigidbody.
    /// </summary>
    private void ApplyMovement()
    {
        if (_movementInput.magnitude > 0)
        {
            // Determine the target movement direction, relative to the camera for intuitive controls.
            Vector3 targetDirection = Camera.main.transform.TransformDirection(_movementInput);
            targetDirection.y = 0; // Keep movement strictly on the horizontal plane
            targetDirection.Normalize();

            // Rotate the character to face the movement direction.
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, _turnSpeed * Time.fixedDeltaTime);

            // Apply velocity, preserving any vertical velocity (e.g., from jumping/falling).
            _rb.velocity = targetDirection * _currentCalculatedSpeed + Vector3.up * _rb.velocity.y;
        }
        else
        {
            // If there's no input, smoothly decelerate the horizontal velocity.
            Vector3 horizontalVelocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
            if (horizontalVelocity.magnitude > 0.1f) // Stop only if still moving significantly
            {
                // Decelerate faster than acceleration for a snappier feel
                _rb.velocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.fixedDeltaTime * _speedLerpRate * 2f) + Vector3.up * _rb.velocity.y;
            }
            else
            {
                // Snap to zero horizontal velocity to prevent perpetual tiny movements
                _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
            }
        }
    }

    /// <summary>
    /// Updates UI elements to reflect the current momentum.
    /// This method is subscribed to the MomentumSystem's OnMomentumChanged event.
    /// </summary>
    /// <param name="currentMomentum">The new current momentum value.</param>
    /// <param name="maxMomentum">The maximum momentum value.</param>
    private void UpdateMomentumUI(float currentMomentum, float maxMomentum)
    {
        if (_momentumText != null)
        {
            _momentumText.text = $"Momentum: {currentMomentum:F0}/{maxMomentum:F0}";
            // Change text color based on whether momentum is active
            _momentumText.color = _momentumSystem.IsMomentumActive ? Color.cyan : Color.white;
        }

        if (_momentumFillBar != null)
        {
            _momentumFillBar.fillAmount = _momentumSystem.MomentumNormalized;
            // Lerp fill bar color from blue to red as momentum increases
            _momentumFillBar.color = Color.Lerp(Color.blue, Color.red, _momentumSystem.MomentumNormalized);
        }
    }

    /// <summary>
    /// Handler for when momentum crosses the active threshold upwards.
    /// Trigger special effects, sounds, or enable abilities here.
    /// </summary>
    private void HandleMomentumActive()
    {
        Debug.Log("Momentum is now ACTIVE! Special effects, sounds, or abilities could trigger here.");
        // Example: Play a 'power-up' sound, activate a particle effect, enable an 'Ultimate' button.
    }

    /// <summary>
    /// Handler for when momentum drops below the active threshold.
    /// Revert special effects, sounds, or disable abilities here.
    /// </summary>
    private void HandleMomentumInactive()
    {
        Debug.Log("Momentum is no longer active. Special effects revert.");
        // Example: Stop 'power-up' sound, disable particle effect, disable 'Ultimate' button.
    }

    // --- Example of another external system influencing momentum ---
    /// <summary>
    /// Public method to simulate an external event (e.g., taking damage) that penalizes momentum.
    /// </summary>
    /// <param name="penaltyAmount">The amount of momentum to lose.</param>
    public void TakeDamageMomentumPenalty(float penaltyAmount)
    {
        _momentumSystem.RemoveMomentum(penaltyAmount);
        Debug.Log($"Took damage, lost {penaltyAmount} momentum. Current: {_momentumSystem.CurrentMomentum:F0}");
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create a new Unity project** or open an existing one.
2.  **Create C# Scripts:**
    *   In your Project window, create two new C# scripts named `MomentumSystem` and `MomentumControlledMover`.
    *   Copy and paste the code above into their respective files.
3.  **Create a Player GameObject:**
    *   In the Hierarchy, right-click -> `3D Object` -> `Capsule`. Name it "Player".
    *   Position it at `(0, 1, 0)` so it's above the ground.
4.  **Add Components to Player:**
    *   Select the "Player" GameObject.
    *   In the Inspector, click `Add Component`.
    *   Search for `Rigidbody` and add it.
        *   For the Rigidbody, set `Drag` to `5` and `Angular Drag` to `5` for a smoother feel.
        *   Check the `Freeze Rotation` checkboxes for X, Y, and Z axes (or just X and Z, as `MomentumControlledMover` handles Y rotation).
    *   Click `Add Component` again and search for `MomentumSystem`. Add it.
    *   Click `Add Component` again and search for `MomentumControlledMover`. Add it.
5.  **Configure Components in Inspector (on "Player"):**
    *   **MomentumSystem:**
        *   `Max Momentum`: e.g., `100`
        *   `Build Rate`: e.g., `20` (builds 20 momentum units per second of active input)
        *   `Decay Rate`: e.g., `10` (decays 10 momentum units per second when inactive)
        *   `Active Threshold`: e.g., `50` (momentum needs to be 50 or more to be 'active')
        *   `Decay Only When Inactive`: `true` (recommended for movement to prevent decay while running)
        *   `Decay Delay`: e.g., `0.5` (a small pause before decay starts after stopping movement)
    *   **MomentumControlledMover:**
        *   `Base Speed`: e.g., `5`
        *   `Max Momentum Speed Bonus`: e.g., `10` (max speed will be 5 + 10 = 15)
        *   `Turn Speed`: e.g., `10`
        *   `Speed Lerp Rate`: e.g., `5`
        *   `Movement Momentum Contribution`: e.g., `1` (this is a multiplier for the MomentumSystem's Build Rate)
6.  **Add a Ground Plane:**
    *   In the Hierarchy, right-click -> `3D Object` -> `Plane`. Position it at `(0, 0, 0)`.
7.  **Set up the Camera:**
    *   For a simple setup, drag your `Main Camera` onto the "Player" GameObject in the Hierarchy to make it a child.
    *   Adjust the camera's position (e.g., `X:0, Y:3, Z:-5`) and rotation (`X:20, Y:0, Z:0`) relative to the Player so you can see it from behind.
8.  **Optional: Add UI for Momentum Display (Recommended):**
    *   In the Hierarchy, right-click -> `UI` -> `Canvas`.
    *   Right-click on the `Canvas` -> `UI` -> `Text - TextMeshPro`. Import TMP Essentials if prompted.
        *   Name this "MomentumText". Position it (e.g., `X:0, Y:100`).
    *   Right-click on the `Canvas` -> `UI` -> `Image`.
        *   Name this "MomentumFillBar". Position it (e.g., `X:0, Y:70`), set `Width: 200`, `Height: 20`.
        *   In the Image component, change `Image Type` to `Filled`. Set `Fill Method` to `Horizontal`, `Fill Origin` to `Left`.
    *   Select your "Player" GameObject again.
    *   Drag "MomentumText" from the Hierarchy into the `Momentum Text` slot on the `MomentumControlledMover` component.
    *   Drag "MomentumFillBar" from the Hierarchy into the `Momentum Fill Bar` slot on the `MomentumControlledMover` component.
9.  **Run the Game!**
    *   Press Play. Use WASD or Arrow keys to move your player.
    *   Observe how the player accelerates and decelerates, and how the momentum UI updates. Notice the speed boost when momentum is high and the subtle delay before momentum decays.