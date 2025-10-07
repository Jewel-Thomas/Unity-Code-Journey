// Unity Design Pattern Example: JetpackController
// This script demonstrates the JetpackController pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'JetpackController' design pattern, while not one of the classic Gang of Four patterns, represents a common and highly practical architectural approach in Unity game development. It's about encapsulating all the logic for a specific game mechanic (the jetpack) into a single, cohesive component.

This implementation leverages several core design principles:

1.  **State Machine:** The most crucial aspect. A jetpack isn't just "on" or "off"; it has states like Boosting, Cooling Down, Out Of Fuel, and Idle. A state machine makes transitions between these states robust and manageable.
2.  **Encapsulation:** All jetpack-related data (fuel, force, rates) and logic are contained within this script, making it a self-contained unit.
3.  **Event-Driven Communication:** Uses C# events to notify other systems (like UI for fuel bars, or VFX/SFX managers for effects) about changes in fuel or state. This promotes loose coupling, meaning those other systems don't need direct references to the `JetpackController` but can simply subscribe to its events.
4.  **Component-Based Design:** Naturally fits Unity's component-based architecture, allowing you to easily add this functionality to any `GameObject` with a `Rigidbody`.

---

### How to Use This Script in Unity:

1.  **Create Player GameObject:** In your Unity scene, create a 3D Object (e.g., a "Cube" or "Capsule") that will serve as your player.
2.  **Add Rigidbody:** Select your player GameObject and add a `Rigidbody` component (`Add Component -> Physics -> Rigidbody`). Ensure "Use Gravity" is checked.
3.  **Attach JetpackController:** Create a new C# script named `JetpackController` and copy the code below into it. Attach this script to your player GameObject.
4.  **Configure Inspector:** Select your player GameObject. In the Inspector, you'll see the `JetpackController` component. Adjust the `Jetpack Settings` (Max Fuel, Consumption Rate, Jetpack Force, etc.) to your liking.
5.  **Add PlayerInputExample (Optional but Recommended):** Create another C# script named `PlayerInputExample` and copy the *example usage* code (provided after the main script) into it. Attach this script to your player GameObject.
6.  **Run:** Play the scene. When you press and hold the "Jump" button (Spacebar by default), your player should activate the jetpack and fly upwards, consuming fuel. Releasing the button will stop boosting, and fuel will recharge after a brief cooldown.

---

```csharp
using UnityEngine;
using System; // Required for Action events

/// <summary>
/// The JetpackController pattern encapsulates all the logic related to a player's jetpack,
/// including fuel management, physics application, and state transitions.
///
/// It uses a State Machine pattern to manage different operational states (Idle, Boosting, CoolingDown, OutOfFuel),
/// ensuring robust and predictable behavior. It also uses C# Events for loose coupling, allowing
/// UI, SFX, and VFX systems to react to jetpack changes without direct dependencies.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Ensures a Rigidbody is present on the GameObject
public class JetpackController : MonoBehaviour
{
    // --- DESIGN PATTERN EXPLANATION ---
    // The 'JetpackController' isn't a classical GoF design pattern, but rather an architectural pattern
    // for managing a specific game mechanic. It encapsulates all the logic related to the jetpack,
    // including its state, resource management (fuel), physics interaction, and provides a clear API
    // for other parts of the game (like player input) to interact with it.
    //
    // This implementation leverages:
    // 1.  **State Machine:** To manage the different operational phases of the jetpack (Idle, Boosting, CoolingDown, OutOfFuel).
    //     This ensures robust behavior and correct transitions between modes.
    // 2.  **Encapsulation:** All jetpack-specific logic and data are contained within this class.
    // 3.  **Event-Driven Communication:** Uses C# events (OnFuelChanged, OnJetpackStateChanged) to notify other
    //     systems (e.g., UI, SFX, VFX managers) about changes, promoting loose coupling.
    // 4.  **Component-Based Design:** Fits perfectly into Unity's component-based architecture.
    // ------------------------------------

    #region Configuration Parameters

    [Header("Jetpack Settings")]
    [Tooltip("The maximum amount of fuel the jetpack can hold.")]
    [SerializeField] private float maxFuel = 100f;
    [Tooltip("How much fuel is consumed per second when the jetpack is active.")]
    [SerializeField] private float fuelConsumptionRate = 30f;
    [Tooltip("How much fuel is recharged per second when the jetpack is idle and not cooling down.")]
    [SerializeField] private float fuelRechargeRate = 15f;
    [Tooltip("The upward force applied to the Rigidbody when the jetpack is active.")]
    [SerializeField] private float jetpackForce = 1500f;
    [Tooltip("Duration after jetpack use before fuel starts recharging.")]
    [SerializeField] private float cooldownDuration = 1f;

    #endregion

    #region Internal State Variables

    private Rigidbody _rigidbody;          // Reference to the Rigidbody component for physics
    private float _currentFuel;            // Current amount of fuel remaining
    private bool _isInputActive;           // True if the player is currently holding the jetpack button
    private float _cooldownTimer;          // Timer to track the cooldown period before recharge
    private JetpackState _currentJetpackState; // The current operational state of the jetpack

    /// <summary>
    /// Defines the various operational states of the jetpack.
    /// </summary>
    public enum JetpackState
    {
        Idle,           // Not active, possibly recharging fuel
        Boosting,       // Actively applying force and consuming fuel
        CoolingDown,    // Recently used, waiting for recharge to begin
        OutOfFuel       // No fuel, cannot boost, prioritizing recharge
    }

    #endregion

    #region Public Properties & Events

    /// <summary>
    /// Event fired when the jetpack's current fuel amount changes.
    /// Provides the new normalized fuel amount (0-1).
    /// Other scripts (e.g., UI scripts) can subscribe to this.
    /// </summary>
    public event Action<float> OnFuelChanged;

    /// <summary>
    /// Event fired when the jetpack's operational state changes.
    /// Provides the new JetpackState.
    /// Other scripts (e.g., VFX, SFX managers) can subscribe to this.
    /// </summary>
    public event Action<JetpackState> OnJetpackStateChanged;

    /// <summary>
    /// The current fuel amount, normalized between 0 and 1.
    /// Useful for UI elements like fuel bars or visual indicators.
    /// </summary>
    public float CurrentFuelNormalized => _currentFuel / maxFuel;

    /// <summary>
    /// The current operational state of the jetpack.
    /// </summary>
    public JetpackState State => _currentJetpackState;

    /// <summary>
    /// Returns true if the jetpack is currently in the Boosting state.
    /// </summary>
    public bool IsBoosting => _currentJetpackState == JetpackState.Boosting;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to get initial component references and set initial state.
    /// </summary>
    private void Awake()
    {
        // Get the Rigidbody component. Using GetComponent<Rigidbody>() assumes it's on the same GameObject.
        // If your Rigidbody is on a parent, use GetComponentInParent<Rigidbody>().
        _rigidbody = GetComponent<Rigidbody>(); 
        if (_rigidbody == null)
        {
            Debug.LogError("JetpackController requires a Rigidbody component on its GameObject!", this);
            enabled = false; // Disable the script if no Rigidbody is found
            return;
        }

        _currentFuel = maxFuel; // Start with full fuel
        SetJetpackState(JetpackState.Idle); // Initialize to Idle state
    }

    /// <summary>
    /// Called once per frame. Handles state transitions and fuel management based on input and timers.
    /// </summary>
    private void Update()
    {
        // Decrement cooldown timer if it's active
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        // --- State Machine Logic ---
        // This switch statement orchestrates the transitions and actions for each jetpack state.
        switch (_currentJetpackState)
        {
            case JetpackState.Idle:
                // If player inputs activation, has fuel, and not cooling down, transition to Boosting.
                if (_isInputActive && _currentFuel > 0 && _cooldownTimer <= 0)
                {
                    SetJetpackState(JetpackState.Boosting);
                }
                // If not boosting and not cooling down, recharge fuel.
                else if (_cooldownTimer <= 0)
                {
                    RechargeFuel(Time.deltaTime);
                }
                break;

            case JetpackState.Boosting:
                // While boosting, continuously consume fuel.
                ConsumeFuel(fuelConsumptionRate * Time.deltaTime);

                // Transition conditions from Boosting:
                if (!_isInputActive) // Input button released
                {
                    // If still has fuel, enter cooldown. Otherwise, out of fuel.
                    if (_currentFuel > 0)
                    {
                        _cooldownTimer = cooldownDuration; // Start cooldown timer
                        SetJetpackState(JetpackState.CoolingDown);
                    }
                    else
                    {
                        SetJetpackState(JetpackState.OutOfFuel);
                    }
                }
                else if (_currentFuel <= 0) // Ran out of fuel while input was held
                {
                    SetJetpackState(JetpackState.OutOfFuel);
                }
                break;

            case JetpackState.CoolingDown:
                // If input is reactivated AND there's fuel, can immediately go back to Boosting,
                // effectively canceling the rest of the cooldown.
                if (_isInputActive && _currentFuel > 0)
                {
                    SetJetpackState(JetpackState.Boosting);
                }
                // If cooldown timer finishes without re-activation, return to Idle state.
                else if (_cooldownTimer <= 0)
                {
                    SetJetpackState(JetpackState.Idle);
                }
                break;

            case JetpackState.OutOfFuel:
                // Always recharge fuel when in OutOfFuel state.
                RechargeFuel(Time.deltaTime);

                // Transition conditions from OutOfFuel:
                // Once enough fuel is available, transition to Idle.
                // It will then require new input to transition to Boosting.
                if (_currentFuel > 0)
                {
                    SetJetpackState(JetpackState.Idle);
                }
                break;
        }
    }

    /// <summary>
    /// Called at a fixed framerate interval. Used for physics calculations.
    /// </summary>
    private void FixedUpdate()
    {
        // Apply upward force to the Rigidbody only when in the Boosting state.
        if (_currentJetpackState == JetpackState.Boosting)
        {
            _rigidbody.AddForce(Vector3.up * jetpackForce, ForceMode.Force);
        }
    }

    #endregion

    #region Public API for Player Input

    /// <summary>
    /// Called by the player input system when the jetpack activation button is pressed or held down.
    /// This sets an internal flag; the actual state transition is handled by the state machine in Update().
    /// </summary>
    public void ActivateJetpackInput()
    {
        _isInputActive = true;
    }

    /// <summary>
    /// Called by the player input system when the jetpack activation button is released.
    /// This clears an internal flag; the actual state transition is handled by the state machine in Update().
    /// </summary>
    public void DeactivateJetpackInput()
    {
        _isInputActive = false;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Changes the current jetpack state and fires the OnJetpackStateChanged event.
    /// Ensures listeners are notified of state changes.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    private void SetJetpackState(JetpackState newState)
    {
        // Only change state if it's actually different from the current one.
        if (_currentJetpackState == newState) return;

        _currentJetpackState = newState;
        // The '?.Invoke' is a null-conditional operator, safely invoking the event only if it has subscribers.
        OnJetpackStateChanged?.Invoke(_currentJetpackState); 
    }

    /// <summary>
    /// Consumes a specified amount of fuel from the jetpack.
    /// Notifies listeners if fuel amount changes.
    /// </summary>
    /// <param name="amount">The amount of fuel to consume.</param>
    private void ConsumeFuel(float amount)
    {
        _currentFuel -= amount;
        _currentFuel = Mathf.Max(_currentFuel, 0f); // Clamp fuel so it doesn't go below zero
        OnFuelChanged?.Invoke(CurrentFuelNormalized); // Notify listeners about fuel change
    }

    /// <summary>
    /// Recharges a specified amount of fuel to the jetpack.
    /// Notifies listeners if fuel amount changes.
    /// </summary>
    /// <param name="amount">The amount of fuel to recharge.</param>
    private void RechargeFuel(float amount)
    {
        // Only recharge if not already at maximum fuel capacity.
        if (_currentFuel < maxFuel)
        {
            _currentFuel += amount;
            _currentFuel = Mathf.Min(_currentFuel, maxFuel); // Clamp fuel so it doesn't exceed max
            OnFuelChanged?.Invoke(CurrentFuelNormalized); // Notify listeners about fuel change
        }
    }

    #endregion
}

/*
 * --- EXAMPLE USAGE IN A PLAYER INPUT SCRIPT ---
 *
 * This section demonstrates how a typical PlayerInput script would interact
 * with the JetpackController.
 *
 * To set up in Unity:
 * 1. Create a new C# Script named "PlayerInputExample".
 * 2. Copy the content below into it.
 * 3. Attach this "PlayerInputExample" script to your Player GameObject
 *    (the one that also has the Rigidbody and JetpackController).
 * 4. Run the scene and press the Spacebar (mapped to "Jump" in Unity's default input)
 *    to activate the jetpack.
 */

// #region Example Player Input Script
//
// using UnityEngine;
//
// public class PlayerInputExample : MonoBehaviour
// {
//     [Tooltip("Reference to the JetpackController component.")]
//     [SerializeField] private JetpackController jetpackController;
//
//     private void Awake()
//     {
//         // Attempt to get the JetpackController component if not assigned in the Inspector.
//         if (jetpackController == null)
//         {
//             jetpackController = GetComponent<JetpackController>();
//             if (jetpackController == null)
//             {
//                 Debug.LogError("PlayerInputExample requires a JetpackController component on this GameObject!", this);
//                 enabled = false; // Disable this script if no JetpackController is found
//             }
//         }
//
//         // Optional: Subscribe to events for UI updates, debug logging, or reacting to state changes
//         if (jetpackController != null)
//         {
//             jetpackController.OnFuelChanged += UpdateFuelUI;
//             jetpackController.OnJetpackStateChanged += UpdateJetpackVisualsAndSFX;
//         }
//     }
//
//     private void OnDestroy()
//     {
//         // Always unsubscribe from events to prevent potential memory leaks or null reference exceptions
//         // if the event publisher (JetpackController) outlives the subscriber (this script).
//         if (jetpackController != null)
//         {
//             jetpackController.OnFuelChanged -= UpdateFuelUI;
//             jetpackController.OnJetpackStateChanged -= UpdateJetpackVisualsAndSFX;
//         }
//     }
//
//     private void Update()
//     {
//         // --- Jetpack Input Handling ---
//         // Use Unity's Input.GetButtonDown/Up for a general "Jump" input, which is
//         // typically mapped to the Spacebar, gamepad buttons, etc., in Project Settings -> Input Manager.
//
//         if (jetpackController == null) return; // Prevent errors if controller is missing
//
//         if (Input.GetButtonDown("Jump"))
//         {
//             jetpackController.ActivateJetpackInput();
//         }
//         if (Input.GetButtonUp("Jump"))
//         {
//             jetpackController.DeactivateJetpackInput();
//         }
//
//         // Example of checking jetpack state for other game logic (e.g., displaying a "low fuel" warning)
//         if (jetpackController.State == JetpackController.JetpackState.OutOfFuel)
//         {
//             // Debug.Log("Can't boost, out of fuel! Recharging...");
//             // You could trigger a specific UI element or sound effect here.
//         }
//     }
//
//     // --- Example Event Handlers ---
//     // These methods demonstrate how other systems would react to JetpackController events.
//
//     /// <summary>
//     /// Example method to update a UI element (like a fuel bar) when fuel changes.
//     /// </summary>
//     /// <param name="normalizedFuel">The current fuel amount normalized between 0 and 1.</param>
//     private void UpdateFuelUI(float normalizedFuel)
//     {
//         // In a real project, you would update a UI Slider or Image fill amount here.
//         // For example: myFuelBarSlider.value = normalizedFuel;
//         Debug.Log($"[UI] Fuel: {normalizedFuel:P0}"); // Example debug output for demonstration
//     }
//
//     /// <summary>
//     /// Example method to control visual effects (VFX) and sound effects (SFX) based on jetpack state.
//     /// </summary>
//     /// <param name="newState">The new state of the jetpack.</param>
//     private void UpdateJetpackVisualsAndSFX(JetpackController.JetpackState newState)
//     {
//         switch (newState)
//         {
//             case JetpackController.JetpackState.Boosting:
//                 Debug.Log("[VFX/SFX] Jetpack ON! Play boost SFX and particle effects.");
//                 // Example: jetpackParticles.Play();
//                 // Example: jetpackAudioSource.Play();
//                 // Example: playerAnimator.SetBool("IsJetpacking", true);
//                 break;
//             case JetpackController.JetpackState.Idle:
//             case JetpackController.JetpackState.CoolingDown:
//             case JetpackController.JetpackState.OutOfFuel:
//                 Debug.Log("[VFX/SFX] Jetpack OFF. Stop boost SFX and particle effects.");
//                 // Example: jetpackParticles.Stop();
//                 // Example: jetpackAudioSource.Stop();
//                 // Example: playerAnimator.SetBool("IsJetpacking", false);
//                 break;
//         }
//     }
// }
//
// #endregion
```