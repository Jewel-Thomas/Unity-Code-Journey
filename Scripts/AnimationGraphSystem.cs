// Unity Design Pattern Example: AnimationGraphSystem
// This script demonstrates the AnimationGraphSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'AnimationGraphSystem' design pattern provides a robust and flexible way to manage character animations programmatically, similar to Unity's built-in Animator Controller but with full C# control. It decouples animation logic from the character's movement or input logic, making it easier to manage complex animation states and transitions.

This example demonstrates a basic character with Idle, Walk, Run, Jump, and Fall animations. The system manages which animation plays based on character input, grounded status, and vertical velocity, all driven by a custom state machine.

---

**How to use this example in Unity:**

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a 3D Object:** Go to `GameObject > 3D Object > Capsule` (or any other primitive). This will serve as our character.
3.  **Add Components to the Capsule:**
    *   Select the Capsule in the Hierarchy.
    *   Go to `Add Component` in the Inspector.
    *   Add an `Animator` component.
    *   Add a `Character Controller` component.
4.  **Prepare an Animator Controller:**
    *   Create a new Animator Controller: Right-click in your Project window (`Assets` folder), `Create > Animator Controller`. Name it `CharacterAnimator`.
    *   Assign `CharacterAnimator` to the `Controller` field of the `Animator` component on your Capsule.
    *   **Crucially**, for this example to work, the Animator Controller *must contain empty states with the exact names* specified in the `AnimationGraphSystemController` script (`_idleClipName`, `_walkClipName`, etc.).
        *   In the Animator window, right-click, `Create State > Empty`. Rename it to `Idle`.
        *   Repeat for `Walk`, `Run`, `Jump`, `Fall`.
        *   **Important:** This example uses `Animator.CrossFadeInFixedTime` by string name, which expects these states to exist. You would normally link actual animation clips to these states in your Animator Controller. For a pure programmatic approach, you could use `Animator.Play("Base Layer.Idle")` to directly play *any clip* in the Animator, or simply ensure the state exists. For simplicity, we'll assume these states map to actual animation clips.
        *   You can also add a `Speed` float parameter, an `IsGrounded` bool, and a `VerticalVelocity` float parameter to your Animator, as the script *updates* these, even though the custom graph controls state changes directly. These parameters would typically be used in Blend Trees *within* those states if you needed more complex animation blending (e.g., a walk blend tree for different walk styles).
5.  **Create a C# Script:** Right-click in your Project window, `Create > C# Script`. Name it `AnimationGraphSystemController`.
6.  **Copy & Paste:** Replace the entire content of the `AnimationGraphSystemController.cs` file with the code provided below.
7.  **Attach Script:** Drag the `AnimationGraphSystemController.cs` script onto your Capsule GameObject.
8.  **Assign References:** In the Inspector, for your Capsule:
    *   Drag the `Animator` component (from the Capsule itself) to the `Animator` field in the `AnimationGraphSystemController`.
    *   Drag the `Character Controller` component (from the Capsule itself) to the `Character Controller` field.
    *   (Optional, but recommended) Ensure the `Animation Clip Names` match the *state names* you created in your Animator Controller.
9.  **Create a Plane:** Go to `GameObject > 3D Object > Plane` to provide ground for your character.
10. **Run the Scene!** Use WASD to move, Shift to run, and Space to jump. Observe how the animations transition smoothly based on the programmatic graph logic.

---

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

// --- 1. The Core AnimationGraphSystem Pattern Components ---

/// <summary>
/// Defines the interface for any animation state in our system.
/// This promotes polymorphism, allowing different types of states (e.g., simple clip, blend tree, sub-graph).
/// </summary>
public interface IAnimationState
{
    string Name { get; }
    void Enter(Animator animator);
    void Update(Animator animator);
    void Exit(Animator animator);
}

/// <summary>
/// Abstract base class for animation states.
/// Provides common functionality and stores the state's name.
/// </summary>
public abstract class AnimationState : IAnimationState
{
    public string Name { get; private set; }
    protected Animator Animator { get; private set; } // Reference to the Unity Animator, set on Enter

    public AnimationState(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Called when the state is entered.
    /// This is where you would typically start playing an animation, set initial parameters, etc.
    /// </summary>
    /// <param name="animator">The Unity Animator component.</param>
    public virtual void Enter(Animator animator)
    {
        Animator = animator;
        Debug.Log($"<color=lightblue>Entering State: {Name}</color>");
        // Common entry logic can go here (e.g., reset layer weights, set initial speed)
    }

    /// <summary>
    /// Called every frame while this state is active.
    /// Useful for updating blend tree parameters, checking for specific animation events, etc.
    /// </summary>
    /// <param name="animator">The Unity Animator component.</param>
    public abstract void Update(Animator animator);

    /// <summary>
    /// Called when the state is exited.
    /// This is where you would typically perform cleanup, reset parameters, etc.
    /// </summary>
    /// <param name="animator">The Unity Animator component.</param>
    public virtual void Exit(Animator animator)
    {
        Debug.Log($"<color=orange>Exiting State: {Name}</color>");
        // Common exit logic can go here (e.g., revert layer weights)
    }
}

/// <summary>
/// A concrete implementation of an animation state that plays a single animation clip
/// using Unity's Animator.CrossFadeInFixedTime for smooth transitions.
/// </summary>
public class AnimatorClipState : AnimationState
{
    private string _clipName;
    private float _transitionDuration;

    /// <summary>
    /// Initializes a new AnimatorClipState.
    /// </summary>
    /// <param name="name">The unique name of this state within the graph.</param>
    /// <param name="clipName">The name of the animation state/clip in the Unity Animator Controller.</param>
    /// <param name="transitionDuration">Duration for cross-fading into this clip.</param>
    public AnimatorClipState(string name, string clipName, float transitionDuration = 0.2f) : base(name)
    {
        _clipName = clipName;
        _transitionDuration = transitionDuration;
    }

    /// <summary>
    /// On entering this state, cross-fade to the specified animation clip.
    /// </summary>
    public override void Enter(Animator animator)
    {
        base.Enter(animator);
        // Using CrossFadeInFixedTime for smooth transitions.
        // The _clipName must correspond to an existing state name in the Animator Controller.
        animator.CrossFadeInFixedTime(_clipName, _transitionDuration);
    }

    /// <summary>
    /// No specific per-frame updates needed for a simple clip state,
    /// but could be used to set parameters relevant to this specific clip.
    /// </summary>
    public override void Update(Animator animator)
    {
        // Example: If this state had an internal timer or specific parameter to set:
        // animator.SetFloat("WalkCycleSpeed", currentWalkSpeed);
    }

    /// <summary>
    /// No specific exit logic needed for a simple clip state.
    /// </summary>
    public override void Exit(Animator animator)
    {
        base.Exit(animator);
    }
}

/// <summary>
/// Represents a transition between two animation states.
/// It holds a target state and a condition (a boolean function) that must be met for the transition to occur.
/// </summary>
public class AnimationTransition
{
    public IAnimationState TargetState { get; private set; }
    public Func<bool> Condition { get; private set; } // A delegate that evaluates to true when the transition should happen.

    /// <summary>
    /// Initializes a new AnimationTransition.
    /// </summary>
    /// <param name="targetState">The state to transition to.</param>
    /// <param name="condition">A function that returns true when the transition should occur.</param>
    public AnimationTransition(IAnimationState targetState, Func<bool> condition)
    {
        TargetState = targetState;
        Condition = condition;
    }
}

/// <summary>
/// The central manager for the animation graph.
/// It stores all states and their transitions, and manages the current active state.
/// </summary>
public class AnimationGraph
{
    private Dictionary<string, IAnimationState> _states = new Dictionary<string, IAnimationState>();
    private Dictionary<IAnimationState, List<AnimationTransition>> _transitions = new Dictionary<IAnimationState, List<AnimationTransition>>();

    public IAnimationState CurrentState { get; private set; }
    private Animator _animator; // The Unity Animator component this graph controls.

    /// <summary>
    /// Initializes the AnimationGraph with a reference to the Unity Animator.
    /// </summary>
    /// <param name="animator">The Animator component to control.</param>
    public AnimationGraph(Animator animator)
    {
        _animator = animator;
    }

    /// <summary>
    /// Adds a new animation state to the graph.
    /// </summary>
    /// <param name="state">The IAnimationState implementation.</param>
    public void AddState(IAnimationState state)
    {
        if (_states.ContainsKey(state.Name))
        {
            Debug.LogWarning($"AnimationGraph: State with name '{state.Name}' already exists. Skipping.");
            return;
        }
        _states.Add(state.Name, state);
        _transitions.Add(state, new List<AnimationTransition>()); // Initialize list of transitions for this state
    }

    /// <summary>
    /// Adds a transition from one state to another, triggered by a condition.
    /// </summary>
    /// <param name="from">The starting state.</param>
    /// <param name="to">The target state.</param>
    /// <param name="condition">A function that, when true, triggers this transition.</param>
    public void AddTransition(IAnimationState from, IAnimationState to, Func<bool> condition)
    {
        if (!_states.ContainsValue(from) || !_states.ContainsValue(to))
        {
            Debug.LogError("AnimationGraph: Attempted to add transition between non-existent states. Please add states first.");
            return;
        }
        _transitions[from].Add(new AnimationTransition(to, condition));
    }

    /// <summary>
    /// Sets the initial state of the animation graph. This is the first state entered when the graph starts.
    /// </summary>
    /// <param name="stateName">The name of the initial state.</param>
    public void SetInitialState(string stateName)
    {
        if (_states.TryGetValue(stateName, out IAnimationState initialState))
        {
            CurrentState = initialState;
            CurrentState.Enter(_animator);
        }
        else
        {
            Debug.LogError($"AnimationGraph: Initial state '{stateName}' not found.");
        }
    }

    /// <summary>
    /// Called every frame to update the animation graph.
    /// It checks for transitions from the current state and, if none occur, updates the current state.
    /// </summary>
    public void Update()
    {
        if (CurrentState == null) return;

        // 1. Evaluate Transitions:
        // Iterate through all transitions defined for the CurrentState.
        foreach (var transition in _transitions[CurrentState])
        {
            if (transition.Condition())
            {
                // Transition triggered!
                CurrentState.Exit(_animator); // Exit the old state
                CurrentState = transition.TargetState; // Set the new state
                CurrentState.Enter(_animator); // Enter the new state
                return; // Only one transition can occur per frame for simplicity.
            }
        }

        // 2. If no transition occurred, update the current state:
        CurrentState.Update(_animator);
    }

    /// <summary>
    /// Helper method to retrieve a state by its name.
    /// </summary>
    /// <param name="stateName">The name of the state.</param>
    /// <returns>The IAnimationState if found, otherwise null.</returns>
    public IAnimationState GetState(string stateName)
    {
        _states.TryGetValue(stateName, out IAnimationState state);
        return state;
    }
}

// --- 2. Unity Integration (MonoBehaviour) ---

/// <summary>
/// This MonoBehaviour acts as the entry point for our AnimationGraphSystem in Unity.
/// It initializes the graph, defines states and transitions, and handles character input/movement.
/// </summary>
[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class AnimationGraphSystemController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterController _characterController;

    [Header("Animation Clip Names (Must match Animator Controller states)")]
    [SerializeField] private string _idleClipName = "Idle";
    [SerializeField] private string _walkClipName = "Walk";
    [SerializeField] private string _runClipName = "Run";
    [SerializeField] private string _jumpClipName = "Jump";
    [SerializeField] private string _fallClipName = "Fall";

    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _gravity = -15f; // Standard gravity, applied downwards

    private AnimationGraph _animationGraph;
    private Vector3 _velocity; // Current character velocity, primarily for Y-axis (jump/fall)
    private bool _isGrounded;
    private float _currentSpeed;

    // Input flags, used as conditions for animation transitions
    private bool _wantsToMove;
    private bool _wantsToRun;
    private bool _wantsToJump;

    void Awake()
    {
        // Get component references if not assigned in Inspector
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_characterController == null) _characterController = GetComponent<CharacterController>();

        if (_animator == null || _characterController == null)
        {
            Debug.LogError("AnimationGraphSystemController: Animator or CharacterController not found! Disabling script.", this);
            enabled = false;
            return;
        }

        // Initialize our custom animation graph
        _animationGraph = new AnimationGraph(_animator);

        // Setup all states and transitions
        SetupAnimationGraph();
    }

    void Update()
    {
        // 1. Gather Input
        HandleInput();

        // 2. Process Character Movement (updates _isGrounded, _velocity, _currentSpeed)
        HandleMovement();

        // 3. Update the Animation Graph (checks conditions, transitions states, plays animations)
        _animationGraph.Update();
    }

    /// <summary>
    /// Gathers player input to determine movement and action intent.
    /// </summary>
    private void HandleInput()
    {
        _wantsToMove = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
        _wantsToRun = Input.GetKey(KeyCode.LeftShift);
        _wantsToJump = Input.GetButtonDown("Jump"); // Spacebar by default
    }

    /// <summary>
    /// Manages character movement using CharacterController.
    /// Updates internal state variables like _isGrounded, _velocity, _currentSpeed.
    /// </summary>
    private void HandleMovement()
    {
        // Check if character is grounded
        _isGrounded = _characterController.isGrounded;

        // Reset vertical velocity when grounded to prevent accumulation
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -0.5f; // Small constant downward force to ensure _isGrounded is consistent
        }

        // Calculate horizontal movement
        Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        // Transform input to world space relative to character's forward direction
        moveInput = transform.TransformDirection(moveInput);
        moveInput.Normalize(); // Ensure consistent speed in all directions

        if (_wantsToMove)
        {
            _currentSpeed = _wantsToRun ? _runSpeed : _walkSpeed;
            _characterController.Move(moveInput * _currentSpeed * Time.deltaTime);
        }
        else
        {
            _currentSpeed = 0; // Standing still
        }

        // Handle jumping
        if (_wantsToJump && _isGrounded)
        {
            _velocity.y = _jumpForce;
        }

        // Apply gravity
        _velocity.y += _gravity * Time.deltaTime;

        // Apply vertical movement
        _characterController.Move(_velocity * Time.deltaTime);

        // Optional: Update Animator parameters for blend trees or other effects not directly handled by our graph.
        // Even if our custom graph manages state transitions, blend trees within those states might use these.
        _animator.SetFloat("Speed", _currentSpeed);
        _animator.SetBool("IsGrounded", _isGrounded);
        _animator.SetFloat("VerticalVelocity", _velocity.y);
    }


    /// <summary>
    /// This is where the AnimationGraph is populated with states and transitions.
    /// This method demonstrates how to define a complex animation logic programmatically.
    /// </summary>
    private void SetupAnimationGraph()
    {
        // 1. Define all Animation States
        // Each state is an instance of a class implementing IAnimationState.
        // We use AnimatorClipState here, which maps directly to Animator Controller states.
        var idleState = new AnimatorClipState("Idle", _idleClipName, 0.2f);
        var walkState = new AnimatorClipState("Walk", _walkClipName, 0.2f);
        var runState = new AnimatorClipState("Run", _runClipName, 0.2f);
        var jumpState = new AnimatorClipState("Jump", _jumpClipName, 0.1f); // Shorter transition for jump
        var fallState = new AnimatorClipState("Fall", _fallClipName, 0.1f); // Shorter transition for fall

        // Add states to the graph
        _animationGraph.AddState(idleState);
        _animationGraph.AddState(walkState);
        _animationGraph.AddState(runState);
        _animationGraph.AddState(jumpState);
        _animationGraph.AddState(fallState);

        // 2. Define Transitions between states

        // --- Global Transitions (can happen from almost any state) ---
        // A common pattern is to check for falling/landing from many states.
        // Note: The order of AddTransition matters if conditions can overlap from a single state.
        // The first matching condition will trigger the transition.

        // Any state -> Fall: If not grounded and moving downwards (e.g., walked off a ledge, or peaked a jump)
        _animationGraph.AddTransition(idleState, fallState, () => !_isGrounded && _velocity.y < 0);
        _animationGraph.AddTransition(walkState, fallState, () => !_isGrounded && _velocity.y < 0);
        _animationGraph.AddTransition(runState, fallState, () => !_isGrounded && _velocity.y < 0);
        // Jump state will naturally transition to Fall if it reaches apex and starts descending
        _animationGraph.AddTransition(jumpState, fallState, () => !_isGrounded && _velocity.y < 0);


        // --- Idle State Transitions ---
        _animationGraph.AddTransition(idleState, walkState, () => _wantsToMove && !_wantsToRun && _isGrounded);
        _animationGraph.AddTransition(idleState, runState, () => _wantsToMove && _wantsToRun && _isGrounded);
        _animationGraph.AddTransition(idleState, jumpState, () => _wantsToJump && _isGrounded);

        // --- Walk State Transitions ---
        _animationGraph.AddTransition(walkState, idleState, () => !_wantsToMove && _isGrounded);
        _animationGraph.AddTransition(walkState, runState, () => _wantsToMove && _wantsToRun && _isGrounded);
        _animationGraph.AddTransition(walkState, jumpState, () => _wantsToJump && _isGrounded);

        // --- Run State Transitions ---
        _animationGraph.AddTransition(runState, idleState, () => !_wantsToMove && _isGrounded);
        _animationGraph.AddTransition(runState, walkState, () => _wantsToMove && !_wantsToRun && _isGrounded);
        _animationGraph.AddTransition(runState, jumpState, () => _wantsToJump && _isGrounded);

        // --- Jump State Transitions ---
        // Conditions for landing from a jump (velocity.y <= 0.1f to account for slight float errors)
        _animationGraph.AddTransition(jumpState, idleState, () => _isGrounded && _velocity.y <= 0.1f && !_wantsToMove);
        _animationGraph.AddTransition(jumpState, walkState, () => _isGrounded && _velocity.y <= 0.1f && _wantsToMove && !_wantsToRun);
        _animationGraph.AddTransition(jumpState, runState, () => _isGrounded && _velocity.y <= 0.1f && _wantsToMove && _wantsToRun);

        // --- Fall State Transitions ---
        // Conditions for landing from a fall
        _animationGraph.AddTransition(fallState, idleState, () => _isGrounded && _velocity.y <= 0.1f && !_wantsToMove);
        _animationGraph.AddTransition(fallState, walkState, () => _isGrounded && _velocity.y <= 0.1f && _wantsToMove && !_wantsToRun);
        _animationGraph.AddTransition(fallState, runState, () => _isGrounded && _velocity.y <= 0.1f && _wantsToMove && _wantsToRun);


        // 3. Set the initial state for the animation graph
        _animationGraph.SetInitialState("Idle");
    }
}
```