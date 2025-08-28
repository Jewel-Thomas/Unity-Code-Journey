// Unity Design Pattern Example: FiniteStateMachine
// This script demonstrates the FiniteStateMachine pattern in Unity
// Generated automatically - ready to use in your Unity project

This complete C# Unity example demonstrates the **Finite State Machine (FSM)** design pattern. It's designed to be educational, practical, and ready to use in a Unity project.

The example simulates a `PlayerCharacter` that can be in different states: `Idle`, `Moving`, `Attacking`, and `Dying`. The FSM manages the transitions between these states and executes the logic for the current state.

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator if you were using coroutines within states

// This namespace helps organize your FSM related code.
namespace FSMExample
{
    /// <summary>
    /// IState Interface:
    /// Defines the common interface for all states in the Finite State Machine.
    /// Each concrete state must implement these three methods.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called once when entering the state. Use this for setup (e.g., starting animations, initializing timers).
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while the state is active. Use this for continuous logic (e.g., movement, input checking, updating timers).
        /// </summary>
        void Execute();

        /// <summary>
        /// Called once when exiting the state. Use this for cleanup (e.g., stopping animations, resetting values).
        /// </summary>
        void Exit();
    }

    /// <summary>
    /// StateMachine Class:
    /// Manages the current state and handles transitions between states.
    /// It's a generic class that can be used with any context (e.g., Player, AI, UI).
    /// </summary>
    public class StateMachine
    {
        // The current active state.
        private IState _currentState;

        /// <summary>
        /// Gets the currently active state. Useful for debugging or external queries.
        /// </summary>
        public IState CurrentState => _currentState;

        /// <summary>
        /// Initializes the state machine with a starting state.
        /// This should be called once, typically in Awake or Start of the owning component.
        /// </summary>
        /// <param name="startingState">The state to begin in.</param>
        public void Initialize(IState startingState)
        {
            _currentState = startingState;
            // Immediately call Enter for the initial state.
            startingState.Enter();
        }

        /// <summary>
        /// Changes the current state of the machine.
        /// It calls Exit on the old state, sets the new state, and then calls Enter on the new state.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void ChangeState(IState newState)
        {
            // If there's an existing state, call its Exit method for cleanup.
            if (_currentState != null)
            {
                _currentState.Exit();
            }

            // Set the new state.
            _currentState = newState;
            // Call Enter for the new state to initialize it.
            _currentState.Enter();
        }

        /// <summary>
        /// Executes the logic of the current state.
        /// This should be called every frame (e.g., in MonoBehaviour's Update method).
        /// </summary>
        public void ExecuteState()
        {
            // Only execute if a state is currently set.
            _currentState?.Execute();
        }
    }

    // --- Concrete State Implementations ---
    // These classes implement the IState interface for specific behaviors.
    // Each state typically needs a reference to the 'context' (e.g., PlayerCharacter)
    // to perform actions or request state changes.

    /// <summary>
    /// IdleState: The character is doing nothing, waiting for input or events.
    /// </summary>
    public class IdleState : IState
    {
        private PlayerCharacter _player;

        public IdleState(PlayerCharacter player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("<color=cyan>Entering Idle State.</color> Player is standing still.");
            _player.SetAnimation("Idle"); // Set animation
            // Example: Stop any ongoing movement, reset timers.
        }

        public void Execute()
        {
            // Logic to check for transitions to other states.
            // For demo: Check for movement input or a random chance to attack/move.
            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
            {
                _player.RequestStateChange(_player.MovingState);
            }
            // Simulate random event: Small chance to attack
            else if (Random.Range(0, 1000) < 5)
            {
                _player.RequestStateChange(_player.AttackingState);
            }
        }

        public void Exit()
        {
            Debug.Log("<color=cyan>Exiting Idle State.</color>");
            // Cleanup: e.g., clear any idle-specific effects.
        }
    }

    /// <summary>
    /// MovingState: The character is actively moving towards a destination.
    /// </summary>
    public class MovingState : IState
    {
        private PlayerCharacter _player;
        private Vector3 _targetPosition;

        public MovingState(PlayerCharacter player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("<color=green>Entering Moving State.</color> Player is moving.");
            _player.SetAnimation("Run"); // Set animation
            // For demo: Set a random target position nearby. In a real game, this would come from input or AI.
            _targetPosition = _player.transform.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        }

        public void Execute()
        {
            // Move the character towards the target.
            _player.transform.position = Vector3.MoveTowards(_player.transform.position, _targetPosition, _player.MoveSpeed * Time.deltaTime);
            Debug.Log($"Moving towards {_targetPosition}. Current pos: {_player.transform.position}");

            // Check for conditions to transition out of this state.
            // If arrived at target, go back to Idle.
            if (Vector3.Distance(_player.transform.position, _targetPosition) < 0.1f)
            {
                Debug.Log("Arrived at target position.");
                _player.RequestStateChange(_player.IdleState);
            }
            // If player stops input, go back to Idle.
            else if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                _player.RequestStateChange(_player.IdleState);
            }
            // Simulate random event: Small chance to attack while moving
            else if (Random.Range(0, 1000) < 3)
            {
                _player.RequestStateChange(_player.AttackingState);
            }
        }

        public void Exit()
        {
            Debug.Log("<color=green>Exiting Moving State.</color>");
            // Cleanup: e.g., stop movement sound, clear pathfinding data.
        }
    }

    /// <summary>
    /// AttackingState: The character is performing an attack.
    /// </summary>
    public class AttackingState : IState
    {
        private PlayerCharacter _player;
        private float _attackDuration = 1.0f; // How long the attack animation/action lasts
        private float _attackTimer;

        public AttackingState(PlayerCharacter player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("<color=red>Entering Attacking State.</color> Player is attacking!");
            _player.SetAnimation("Attack"); // Set animation
            _attackTimer = _attackDuration;
            // Trigger actual attack logic (e.g., deal damage, play sound effects).
        }

        public void Execute()
        {
            _attackTimer -= Time.deltaTime;
            Debug.Log($"Attacking... {_attackTimer:F2} seconds left.");

            // After the attack duration, transition back to Idle.
            if (_attackTimer <= 0)
            {
                Debug.Log("Attack finished.");
                _player.RequestStateChange(_player.IdleState);
            }
        }

        public void Exit()
        {
            Debug.Log("<color=red>Exiting Attacking State.</color>");
            // Cleanup: e.g., stop attack effects, reset attack cooldowns.
        }
    }

    /// <summary>
    /// DyingState: The character has died and is playing a death animation/effect.
    /// This is often a terminal state, meaning no transitions out of it.
    /// </summary>
    public class DyingState : IState
    {
        private PlayerCharacter _player;
        private float _deathAnimationDuration = 2.0f;
        private float _deathTimer;

        public DyingState(PlayerCharacter player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("<color=purple>Entering Dying State.</color> Player has died!");
            _player.SetAnimation("Die"); // Set animation
            _deathTimer = _deathAnimationDuration;
            // Disable player input, disable physics, play death sound, etc.
            // For demo: Disable collider and make Rigidbody kinematic if present.
            if (_player.TryGetComponent<Collider>(out var collider)) collider.enabled = false;
            if (_player.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        }

        public void Execute()
        {
            _deathTimer -= Time.deltaTime;
            Debug.Log($"Dying... {_deathTimer:F2} seconds left until fully gone.");
            // After the death animation, the character might be removed, game over screen shown, etc.
            if (_deathTimer <= 0)
            {
                Debug.Log("Player is fully dead. (Game Over logic would go here)");
                // In a real game, you might set the GameObject inactive or destroy it here.
                // _player.gameObject.SetActive(false);
            }
        }

        public void Exit()
        {
            // DyingState is often a terminal state, so Exit might not be called,
            // or it might trigger game over logic if the character is revived.
            Debug.Log("<color=purple>Exiting Dying State (unlikely for a terminal state).</color>");
        }
    }

    /// <summary>
    /// PlayerCharacter (Context/Agent) MonoBehaviour:
    /// This is the Unity component that utilizes the Finite State Machine.
    /// It holds an instance of the StateMachine and references to all possible states.
    /// It updates the FSM in its Update method and provides public methods for states
    /// to request transitions.
    /// </summary>
    [RequireComponent(typeof(CharacterController))] // Ensure player has a CharacterController for movement or adjust movement logic
    [RequireComponent(typeof(Animator))] // Ensure player has an Animator for animation example
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("FSM Configuration")]
        [Tooltip("The speed at which the character moves.")]
        [SerializeField] private float _moveSpeed = 3.0f;
        public float MoveSpeed => _moveSpeed;

        [Tooltip("Reference to the Animator component for controlling animations.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Displays the current FSM state name for debugging in the Inspector.")]
        [SerializeField] private string _currentStateName; // For debugging in Inspector

        // The core Finite State Machine instance.
        private StateMachine _stateMachine;

        // References to all possible state instances. These are instantiated once.
        public IState IdleState { get; private set; }
        public IState MovingState { get; private set; }
        public IState AttackingState { get; private set; }
        public IState DyingState { get; private set; }

        private CharacterController _characterController; // For actual Unity movement

        void Awake()
        {
            // Get component references (Unity Best Practice).
            if (_animator == null) _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();

            // 1. Instantiate all concrete states, passing 'this' (the PlayerCharacter)
            // as the context so states can interact with the character's properties and methods.
            IdleState = new IdleState(this);
            MovingState = new MovingState(this);
            AttackingState = new AttackingState(this);
            DyingState = new DyingState(this);

            // 2. Initialize the State Machine.
            _stateMachine = new StateMachine();
        }

        void Start()
        {
            // 3. Set the initial state of the FSM.
            _stateMachine.Initialize(IdleState);
            UpdateDebugStateName();
        }

        void Update()
        {
            // 4. In every frame, tell the State Machine to execute the current state's logic.
            _stateMachine.ExecuteState();
            UpdateDebugStateName(); // Keep the debug name updated.

            // Example: Trigger dying state manually (e.g., press 'K' for 'Kill')
            if (Input.GetKeyDown(KeyCode.K))
            {
                RequestStateChange(DyingState);
            }
        }

        /// <summary>
        /// Public method for states (or external systems) to request a state transition.
        /// The FSM itself handles the transition logic (Exit old, Enter new).
        /// </summary>
        /// <param name="newState">The target state to transition to.</param>
        public void RequestStateChange(IState newState)
        {
            // Prevent changing state if we are already in a terminal state like Dying,
            // unless the new state is also DyingState (redundant but safe).
            if (_stateMachine.CurrentState == DyingState && newState != DyingState)
            {
                Debug.LogWarning($"<color=orange>Player is in Dying State. Cannot transition to {newState.GetType().Name}.</color>");
                return;
            }

            // Only change state if the requested state is different from the current one.
            if (_stateMachine.CurrentState != newState)
            {
                _stateMachine.ChangeState(newState);
            }
        }

        /// <summary>
        /// Helper method to set animations. This is called by the individual states.
        /// </summary>
        /// <param name="animationTrigger">The name of the animation trigger to set.</param>
        public void SetAnimation(string animationTrigger)
        {
            if (_animator != null)
            {
                // For a more robust animation system, you might use SetBool, SetFloat, or play specific states.
                // This example uses SetTrigger for simplicity, assuming triggers like "Idle", "Run", "Attack", "Die" exist.
                Debug.Log($"Setting animation trigger: '{animationTrigger}'");
                _animator.SetTrigger(animationTrigger);
            }
            else
            {
                Debug.LogWarning("Animator not assigned or found on PlayerCharacter.");
            }
        }

        /// <summary>
        /// Internal method to update the debug string for the Inspector.
        /// </summary>
        private void UpdateDebugStateName()
        {
            if (_stateMachine != null && _stateMachine.CurrentState != null)
            {
                _currentStateName = _stateMachine.CurrentState.GetType().Name;
            }
            else
            {
                _currentStateName = "No State";
            }
        }

        // Example Public API for the PlayerCharacter that external scripts might call
        // These methods would internally request state changes.
        public void StartMoving(Vector3 direction)
        {
            // In a real game, MovingState would need a target,
            // so you might pass it to the state or have the state query it.
            // For this example, MovingState picks a random target.
            RequestStateChange(MovingState);
            // You'd also update the target or direction in MovingState here.
        }

        public void PerformAttack()
        {
            RequestStateChange(AttackingState);
        }
    }
}

/*
/// --- HOW TO USE THIS FSM EXAMPLE IN UNITY ---
///
/// 1. Create a new C# script in your Unity project, name it "FiniteStateMachineExample".
/// 2. Copy and paste ALL the code above into this new script, replacing its default content.
///    (Ensure the file name matches the main class name: 'FiniteStateMachineExample' if you
///    don't want to change the class name 'PlayerCharacter' to 'FiniteStateMachineExample').
///    For this example, it's better if you name the file 'PlayerCharacter.cs' as it contains
///    the main MonoBehaviour. If you paste it all into one file named PlayerCharacter.cs,
///    it will work fine.
///
/// 3. Create an Empty GameObject in your scene (e.g., right-click in Hierarchy -> Create Empty).
/// 4. Rename this GameObject to "Player".
/// 5. Add a CharacterController component to the "Player" GameObject (Component -> Physics -> Character Controller).
///    - You can leave default settings for this example.
/// 6. Add an Animator component to the "Player" GameObject (Component -> Animation -> Animator).
///    - For animations to work: You'll need an Animator Controller. Create one (right-click in Project window -> Create -> Animator Controller),
///      name it "PlayerAnimatorController", and drag it into the 'Controller' slot of the Animator component on your "Player" GameObject.
///    - Inside the "PlayerAnimatorController":
///      - Create four new Trigger parameters: "Idle", "Run", "Attack", "Die" (Window -> Animation -> Animator).
///      - Add some basic animation states (e.g., right-click -> Create State -> Empty). You can rename them to "Idle", "Run", "Attack", "Die".
///      - For this example, you don't *need* actual animation clips, but the `SetAnimation` calls will trigger if they exist.
///      - Create transitions from 'Any State' to each of your new states (right-click 'Any State' -> Make Transition),
///        and on each transition, set its Condition to the corresponding Trigger parameter (e.g., for transition to "Run", set condition to "Run" trigger).
///      - Create transitions from your "Attack" and "Die" states back to "Idle" (or another appropriate state) once they finish,
///        or simply allow `Has Exit Time` to be true on their transitions back to idle.
/// 7. Add the `PlayerCharacter` script to the "Player" GameObject (Drag the script from your Project window onto the "Player" GameObject in the Hierarchy).
/// 8. In the Inspector for the "Player" GameObject:
///    - Ensure the `Animator` field in the `PlayerCharacter` script component is linked to the Animator component on the same GameObject (it should auto-populate).
///    - You can adjust the `Move Speed` if desired.
///
/// 9. Run the scene!
///    - Observe the Console window. You'll see detailed log messages about state entries, executions, and exits.
///    - The "Player" GameObject's `_currentStateName` field in the Inspector will update in real-time.
///    - The character will randomly transition between Idle, Moving, and Attacking states.
///    - Press the 'K' key to force the character into the Dying state.
///
/// This setup provides a clear, practical demonstration of the FSM pattern in a Unity context.
*/
```