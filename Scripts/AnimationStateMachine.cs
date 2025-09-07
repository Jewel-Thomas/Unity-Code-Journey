// Unity Design Pattern Example: AnimationStateMachine
// This script demonstrates the AnimationStateMachine pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity script demonstrates the **Animation State Machine** design pattern. It provides a structured way to manage character animations based on game logic, player input, or AI decisions, making your animation system robust, scalable, and easy to maintain.

The core idea is to define a set of distinct animation states and then manage transitions between these states programmatically. Instead of directly calling `Animator.Play()`, this pattern typically sets Animator parameters (booleans, floats, triggers) which then drive the state changes within Unity's Animator Controller, allowing for smooth transitions, blend trees, and layered animations.

```csharp
using UnityEngine;
using System.Collections.Generic; // Required for Dictionary
using System;                   // Required for Action

/// <summary>
/// Defines the possible animation states for our player character.
/// Using an enum makes the states strongly typed and readable.
/// </summary>
public enum PlayerAnimationState
{
    Idle,
    Walk,
    Run,
    Jump,
    Attack
}

/// <summary>
/// The AnimationStateMachineExample class demonstrates the Animation State Machine pattern in Unity.
/// It manages the character's animations by transitioning between defined states
/// and setting appropriate parameters on the Unity Animator component.
/// </summary>
/// <remarks>
/// This script requires an Animator component on the same GameObject.
/// It also expects specific parameters to be set up in the Animator Controller.
/// </remarks>
[RequireComponent(typeof(Animator))]
public class AnimationStateMachineExample : MonoBehaviour
{
    [Header("Animator Setup")]
    [Tooltip("Reference to the Animator component on this GameObject.")]
    [SerializeField] private Animator animator;

    [Header("Animation Parameter Names")]
    [Tooltip("The name of the boolean parameter for walking in the Animator Controller.")]
    [SerializeField] private string isWalkingParam = "IsWalking";
    [Tooltip("The name of the boolean parameter for running in the Animator Controller.")]
    [SerializeField] private string isRunningParam = "IsRunning";
    [Tooltip("The name of the trigger parameter for jumping in the Animator Controller.")]
    [SerializeField] private string jumpTriggerParam = "JumpTrigger";
    [Tooltip("The name of the trigger parameter for attacking in the Animator Controller.")]
    [SerializeField] private string attackTriggerParam = "AttackTrigger";

    // The private field to hold the current active animation state.
    // This is the heart of our state machine.
    private PlayerAnimationState _currentState;

    // A dictionary to map each PlayerAnimationState to a specific action (a method)
    // that interacts with the Animator to set the correct parameters for that state.
    // This makes the state execution logic clean and easily extensible.
    private Dictionary<PlayerAnimationState, Action> _stateActions;

    // Public property to allow other scripts to read the current state without directly modifying it.
    public PlayerAnimationState CurrentState => _currentState;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used for initial setup and ensuring dependencies are met.
    /// </summary>
    private void Awake()
    {
        // Automatically get the Animator component if it's not assigned in the Inspector.
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on this GameObject. Disabling script.", this);
                enabled = false; // Disable the script if no Animator is present.
                return;
            }
        }

        // Initialize the dictionary that defines what each state "does"
        // by setting the appropriate Animator parameters.
        _stateActions = new Dictionary<PlayerAnimationState, Action>
        {
            // For Idle, Walking, and Running, we set boolean parameters.
            // These usually control blend trees or direct state transitions.
            { PlayerAnimationState.Idle, () => SetMovementAnimatorParameters(false, false) },
            { PlayerAnimationState.Walk, () => SetMovementAnimatorParameters(true, false) },
            { PlayerAnimationState.Run, () => SetMovementAnimatorParameters(false, true) },

            // For Jump and Attack, we use triggers. Triggers are one-shot events
            // that cause an immediate transition and then automatically reset.
            // The Animator Controller is responsible for transitioning out of these
            // temporary states (e.g., back to Idle or Walk once the animation finishes).
            { PlayerAnimationState.Jump, () => animator.SetTrigger(jumpTriggerParam) },
            { PlayerAnimationState.Attack, () => animator.SetTrigger(attackTriggerParam) }
        };

        // Set the initial state of the character.
        TransitionToState(PlayerAnimationState.Idle);
    }

    /// <summary>
    /// Called once per frame. This is where input is checked and state transitions are initiated.
    /// </summary>
    private void Update()
    {
        // --- Input Handling and State Transition Logic ---
        // This section determines the *desired* state based on player input or other game conditions.
        // The order of checks often implies priority (e.g., Attack overrides movement).

        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        bool isRunningInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Prioritize actions: Attack > Jump > Run > Walk > Idle
        if (Input.GetMouseButtonDown(0)) // Left mouse click for attack
        {
            TransitionToState(PlayerAnimationState.Attack);
        }
        else if (Input.GetKeyDown(KeyCode.Space)) // Space bar for jump
        {
            TransitionToState(PlayerAnimationState.Jump);
        }
        else if (isMoving && isRunningInput)
        {
            TransitionToState(PlayerAnimationState.Run);
        }
        else if (isMoving)
        {
            TransitionToState(PlayerAnimationState.Walk);
        }
        else
        {
            // If none of the above conditions are met, default to Idle.
            TransitionToState(PlayerAnimationState.Idle);
        }
        // --- End Input Handling ---

        // After determining and potentially transitioning to a new state,
        // execute the action associated with the *current* state.
        // This ensures the Animator parameters are updated every frame,
        // even if the state didn't change (important for blend trees, etc.).
        ExecuteCurrentStateAction();
    }

    /// <summary>
    /// Attempts to transition the state machine to a new state.
    /// This is the central method for changing the character's animation state.
    /// </summary>
    /// <param name="newState">The <see cref="PlayerAnimationState"/> to transition to.</param>
    private void TransitionToState(PlayerAnimationState newState)
    {
        // Only perform the transition if the new state is different from the current state.
        // This prevents redundant state changes and re-triggering of animations/logic.
        if (_currentState == newState)
        {
            return;
        }

        // Optional: Call an 'OnStateExit' method for _currentState if you have specific
        // logic that needs to run when leaving a state (e.g., disabling an effect).
        // OnStateExit(_currentState);

        // Update the current state to the new state.
        _currentState = newState;
        Debug.Log($"[Animation State Machine] Transitioned to: {_currentState}");

        // Optional: Call an 'OnStateEnter' method for newState if you have specific
        // logic that needs to run when entering a state (e.g., playing a sound,
        // enabling a collider for an attack).
        // OnStateEnter(_currentState);
    }

    /// <summary>
    /// Executes the action associated with the <see cref="CurrentState"/>.
    /// This typically involves setting Animator parameters to drive the animations.
    /// </summary>
    private void ExecuteCurrentStateAction()
    {
        // Look up the action for the current state in our dictionary.
        if (_stateActions.TryGetValue(_currentState, out Action action))
        {
            // If an action is found, invoke it.
            action?.Invoke();
        }
        else
        {
            // Log a warning if no action is defined for a given state (should not happen with complete setup).
            Debug.LogWarning($"[Animation State Machine] No action defined for state: {_currentState}", this);
        }
    }

    /// <summary>
    /// Helper method to consistently set the Animator's boolean parameters for walking and running.
    /// This method is called by the actions defined in the _stateActions dictionary.
    /// </summary>
    /// <param name="isWalking">True to set the 'IsWalking' parameter to true, false otherwise.</param>
    /// <param name="isRunning">True to set the 'IsRunning' parameter to true, false otherwise.</param>
    private void SetMovementAnimatorParameters(bool isWalking, bool isRunning)
    {
        // Set the boolean parameters on the Animator.
        // These parameters will drive transitions or blend trees within the Animator Controller.
        animator.SetBool(isWalkingParam, isWalking);
        animator.SetBool(isRunningParam, isRunning);
    }

    /*
    // --- Optional State Entry/Exit Logic ---
    // These methods can be uncommented and extended if you need to perform
    // additional game logic specifically when entering or exiting a state.

    /// <summary>
    /// Placeholder for logic to execute when entering a specific animation state.
    /// </summary>
    /// <param name="state">The state being entered.</param>
    private void OnStateEnter(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.Jump:
                Debug.Log($"[Animation State Machine] Entering Jump state. Initiating physics for jump.");
                // Example: Add an upward force to a Rigidbody component here.
                break;
            case PlayerAnimationState.Attack:
                Debug.Log($"[Animation State Machine] Entering Attack state. Activating attack hitbox.");
                // Example: Enable a collider for a short duration to detect hits.
                break;
            // Add more cases for other states as needed.
        }
    }

    /// <summary>
    /// Placeholder for logic to execute when exiting a specific animation state.
    /// </summary>
    /// <param name="state">The state being exited.</param>
    private void OnStateExit(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.Jump:
                Debug.Log($"[Animation State Machine] Exiting Jump state. Re-enabling ground checks.");
                // Example: Reset jump-related flags or enable gravity.
                break;
            case PlayerAnimationState.Attack:
                Debug.Log($"[Animation State Machine] Exiting Attack state. Deactivating attack hitbox.");
                // Example: Disable the attack collider.
                break;
            // Add more cases for other states as needed.
        }
    }
    */

    // ====================================================================================
    // EXAMPLE USAGE GUIDE: How to implement this in your Unity project
    // ====================================================================================

    /// <summary>
    /// To make this `AnimationStateMachineExample` script fully functional in Unity,
    /// you need to set up an `Animator Controller` and connect it to your character.
    ///
    /// **STEPS:**
    ///
    /// 1.  **Create a New C# Script:**
    ///     -   In your Unity project, right-click in the `Assets` folder -> `Create` -> `C# Script`.
    ///     -   Name it `AnimationStateMachineExample.cs` and copy this entire code into it.
    ///
    /// 2.  **Prepare Your Character GameObject:**
    ///     -   Create a 3D character (e.g., `GameObject -> 3D Object -> Capsule` for a placeholder, or import your custom model).
    ///     -   Rename it to `Player`.
    ///     -   Select the `Player` GameObject in the Hierarchy.
    ///     -   In the Inspector, click `Add Component` and search for `Animator`. Add it.
    ///
    /// 3.  **Create an Animator Controller Asset:**
    ///     -   In your `Project` window, right-click `Assets` -> `Create` -> `Animator Controller`.
    ///     -   Name it `PlayerAnimatorController`.
    ///
    /// 4.  **Assign the Animator Controller:**
    ///     -   Select your `Player` GameObject.
    ///     -   Drag `PlayerAnimatorController` from the `Project` window into the `Controller` slot of the `Animator` component in the Inspector.
    ///
    /// 5.  **Add Animation Clips to the Controller:**
    ///     -   Double-click `PlayerAnimatorController` in the `Project` window to open the `Animator` window.
    ///     -   You'll need actual animation clips (e.g., "Idle", "Walk", "Run", "Jump", "Attack"). These can come from Mixamo, the Unity Asset Store, or be custom-made.
    ///     -   Right-click in the `Animator` window -> `Create State` -> `Empty`. Name this state `Idle`.
    ///     -   Drag your actual "Idle" animation clip from your `Project` window into the `Motion` slot of the `Idle` state in the Inspector.
    ///     -   Repeat this process for `Walk`, `Run`, `Jump`, and `Attack` states, assigning their respective animation clips.
    ///     -   *(For a quick test without real animations, you can leave the Motion slots empty; the state changes will still register in the console, but you won't see visual movement).*
    ///
    /// 6.  **Add Animator Parameters:**
    ///     -   In the `Animator` window, go to the `Parameters` tab (usually on the left side).
    ///     -   Click the `+` button and add the following parameters. Their names **must** match the `[SerializeField]` strings at the top of this script (or you can change the script's `[SerializeField]` values to match your preferred parameter names):
    ///         -   `Bool`: `IsWalking` (default unchecked)
    ///         -   `Bool`: `IsRunning` (default unchecked)
    ///         -   `Trigger`: `JumpTrigger`
    ///         -   `Trigger`: `AttackTrigger`
    ///
    /// 7.  **Create Animator Transitions:**
    ///     -   **Idle to Walk:**
    ///         -   Right-click `Idle` -> `Make Transition` -> `Walk`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `IsWalking` -> `true`.
    ///     -   **Walk to Idle:**
    ///         -   Right-click `Walk` -> `Make Transition` -> `Idle`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `IsWalking` -> `false`.
    ///     -   **Walk to Run:**
    ///         -   Right-click `Walk` -> `Make Transition` -> `Run`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `IsRunning` -> `true`.
    ///     -   **Run to Walk:**
    ///         -   Right-click `Run` -> `Make Transition` -> `Walk`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `IsRunning` -> `false`.
    ///     -   **Any State to Jump:**
    ///         -   Right-click `Any State` (a grey box, usually at the top) -> `Make Transition` -> `Jump`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `JumpTrigger`.
    ///     -   **Jump to Idle:** (Once jump animation finishes)
    ///         -   Right-click `Jump` -> `Make Transition` -> `Idle`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   **Check `Has Exit Time`** (so the full jump animation plays).
    ///             -   You might add `IsWalking` -> `false` if you want it to go to walk if moving after landing.
    ///     -   **Any State to Attack:**
    ///         -   Right-click `Any State` -> `Make Transition` -> `Attack`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   Uncheck `Has Exit Time`.
    ///             -   Under `Conditions`, click `+` and select `AttackTrigger`.
    ///     -   **Attack to Idle:** (Once attack animation finishes)
    ///         -   Right-click `Attack` -> `Make Transition` -> `Idle`.
    ///         -   Select the transition arrow. In the Inspector:
    ///             -   **Check `Has Exit Time`**.
    ///             -   (Similar to Jump, you might add conditions to transition to Walk if moving).
    ///
    /// 8.  **Attach the Script to Your Player:**
    ///     -   Select your `Player` GameObject in the Hierarchy.
    ///     -   Drag the `AnimationStateMachineExample` script from your `Project` window onto the `Player` GameObject in the Inspector.
    ///
    /// 9.  **Run the Game:**
    ///     -   Press Play in the Unity editor.
    ///     -   Observe the `Debug.Log` messages in the Console showing state transitions.
    ///     -   Press 'W' to walk, 'Shift + W' to run, 'Space' to jump, and 'Left Mouse Button' to attack.
    ///     -   The character's animations should change accordingly.
    ///
    /// This setup provides a powerful and organized way to manage complex animation behavior!
    /// </summary>
}
```