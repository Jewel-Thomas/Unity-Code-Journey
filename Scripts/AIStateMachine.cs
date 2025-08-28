// Unity Design Pattern Example: AIStateMachine
// This script demonstrates the AIStateMachine pattern in Unity
// Generated automatically - ready to use in your Unity project

The AI State Machine design pattern is a behavioral pattern that allows an object (often an AI agent) to change its behavior based on its internal state. It helps to organize complex AI logic by breaking it down into distinct, manageable states and defining clear transitions between them.

This example provides a complete, practical implementation of an AI State Machine in Unity. It features:

*   **`IState` Interface:** Defines the contract for all states.
*   **`AIStateMachine` Class:** Manages the current state and handles transitions.
*   **`AIAgent` MonoBehaviour:** The main AI entity that utilizes the state machine.
*   **Concrete States:** `IdleState`, `PatrolState`, `ChaseState`, and `AttackState` demonstrating common AI behaviors.
*   **Visual Feedback:** The agent's material color changes to reflect its current state.
*   **Unity Best Practices:** Proper `using` statements, `[SerializeField]`, `Debug.Log`, and clear separation of concerns.

---

### **AIStateMachine.cs**

This single script will contain all the necessary classes to demonstrate the AI State Machine pattern.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// =========================================================================
// AI State Machine Pattern Example
// Author: Your Name (or AI Assistant)
// Date: October 26, 2023
//
// This script demonstrates a practical implementation of the AI State Machine
// design pattern in Unity. It provides a flexible and organized way to manage
// complex AI behaviors by breaking them down into distinct, manageable states.
//
// How it works:
// 1. IState: An interface defining the common methods for all states (OnEnter, OnUpdate, OnExit).
// 2. AIStateMachine: Manages the current active state, handles state transitions,
//    and updates the current state.
// 3. AIAgent (MonoBehaviour): The AI entity that owns and utilizes the AIStateMachine.
//    It provides context (transform, target, speed, etc.) to the states.
// 4. Concrete States (IdleState, PatrolState, ChaseState, AttackState): Implement
//    the IState interface and define specific behaviors and transition logic
//    based on the AIAgent's context.
//
// To use this:
// 1. Create an empty GameObject in your scene (e.g., "AIAgent").
// 2. Add a Cube (or any 3D object) as a child to "AIAgent" and name it "Body".
// 3. Attach this script (AIStateMachine.cs) to the "AIAgent" GameObject.
// 4. Assign the "Body" GameObject's MeshRenderer to the _meshRenderer field in the Inspector.
// 5. Create another GameObject (e.g., a Sphere or another Cube) and name it "Target".
// 6. Assign the "Target" GameObject's Transform to the _target field in the Inspector.
// 7. Adjust _moveSpeed, _patrolRadius, and _detectionRange as needed.
// 8. Run the scene! The AIAgent will demonstrate state-based behavior.
//
// =========================================================================


/// <summary>
/// Interface for all AI states.
/// Defines the contract that every state must adhere to.
/// </summary>
public interface IState
{
    /// <summary>
    /// Called once when the state is entered.
    /// Used for initialization, setting up behaviors, etc.
    /// </summary>
    void OnEnter();

    /// <summary>
    /// Called every frame while the state is active.
    /// Contains the main logic for the state, including checks for transitions.
    /// Returns the next state, or 'this' if no transition should occur.
    /// </summary>
    IState OnUpdate();

    /// <summary>
    /// Called once when the state is exited.
    /// Used for cleanup, resetting behaviors, etc.
    /// </summary>
    void OnExit();
}

/// <summary>
/// The core AI State Machine.
/// Manages the current active state and facilitates transitions between states.
/// </summary>
public class AIStateMachine
{
    private AIAgent _owner;          // Reference to the AI agent that owns this state machine.
    private IState _currentState;    // The currently active state.

    /// <summary>
    /// Initializes the state machine with its owner and a starting state.
    /// </summary>
    /// <param name="owner">The AIAgent MonoBehaviour that uses this state machine.</param>
    /// <param name="startingState">The state to begin with.</param>
    public void Initialize(AIAgent owner, IState startingState)
    {
        _owner = owner;
        _currentState = startingState;
        _currentState?.OnEnter(); // Call OnEnter for the initial state.
    }

    /// <summary>
    /// Updates the current state. This method should be called every frame
    /// (e.g., from MonoBehaviour's Update or FixedUpdate).
    /// </summary>
    public void Update()
    {
        // If there's no current state, nothing to do.
        if (_currentState == null) return;

        // The current state's OnUpdate method determines if a transition should occur.
        // It returns the new state if a transition is needed, or 'this' state otherwise.
        IState nextState = _currentState.OnUpdate();

        // If the state returned is different from the current state, perform a transition.
        if (nextState != null && nextState != _currentState)
        {
            ChangeState(nextState);
        }
    }

    /// <summary>
    /// Changes the current state of the AI agent.
    /// Handles calling OnExit on the old state and OnEnter on the new state.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    public void ChangeState(IState newState)
    {
        if (newState == null)
        {
            Debug.LogError("Attempted to change to a null state.");
            return;
        }

        // Exit the current state if one exists.
        _currentState?.OnExit();

        // Set the new state.
        _currentState = newState;

        // Enter the new state.
        _currentState.OnEnter();
    }

    /// <summary>
    /// Gets the current active state.
    /// </summary>
    public IState CurrentState => _currentState;
}


/// <summary>
/// The main AI Agent MonoBehaviour.
/// This component will be attached to the GameObject in Unity.
/// It owns the AIStateMachine and provides the context (data) for the states.
/// </summary>
public class AIAgent : MonoBehaviour
{
    [Header("AI Agent Settings")]
    [SerializeField] private Transform _target;             // The target for the AI to chase/attack.
    [SerializeField] private float _moveSpeed = 3f;         // Speed of the AI agent.
    [SerializeField] private float _patrolRadius = 10f;     // Radius for finding patrol points.
    [SerializeField] private float _detectionRange = 8f;    // How far the AI can detect the target.
    [SerializeField] private float _attackRange = 1.5f;     // How close to be to attack.
    [SerializeField] private float _attackCooldown = 1.5f;  // Time between attacks.
    [SerializeField] private MeshRenderer _meshRenderer;    // For changing material color based on state.

    private AIStateMachine _stateMachine;                   // The state machine instance.
    private Vector3 _currentPatrolPoint;                    // Current destination for patrolling.
    private Coroutine _attackCoroutine;                     // Reference to the attack coroutine.

    // Public properties to allow states to access agent data
    public Transform AgentTransform => transform;
    public Transform Target => _target;
    public float MoveSpeed => _moveSpeed;
    public float PatrolRadius => _patrolRadius;
    public float DetectionRange => _detectionRange;
    public float AttackRange => _attackRange;
    public float AttackCooldown => _attackCooldown;
    public MeshRenderer AgentMeshRenderer => _meshRenderer;
    public Vector3 CurrentPatrolPoint { get => _currentPatrolPoint; set => _currentPatrolPoint = value; }

    public Coroutine AttackCoroutine { get => _attackCoroutine; set => _attackCoroutine = value; }

    void Start()
    {
        // Initialize the state machine with this agent and an initial state (IdleState).
        _stateMachine = new AIStateMachine();
        _stateMachine.Initialize(this, new IdleState(this));
    }

    void Update()
    {
        // Update the state machine every frame.
        _stateMachine.Update();
    }

    /// <summary>
    /// Helper method for states to check if the target is within a certain range.
    /// </summary>
    public bool IsTargetInRange(float range)
    {
        if (_target == null) return false;
        return Vector3.Distance(transform.position, _target.position) <= range;
    }

    /// <summary>
    /// Helper method for states to move the agent towards a specified destination.
    /// </summary>
    public void MoveTowards(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        transform.position += direction * _moveSpeed * Time.deltaTime;

        // Optional: Make the agent look at the direction it's moving
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * _moveSpeed);
        }
    }

    /// <summary>
    /// Stops any active coroutines on this agent. Useful when changing states.
    /// </summary>
    public void StopAllAgentCoroutines()
    {
        StopAllCoroutines();
        _attackCoroutine = null; // Clear attack coroutine reference
    }
}


// =========================================================================
// CONCRETE STATES
// These classes implement the IState interface and define specific behaviors
// for the AI Agent. Each state holds a reference to the AIAgent (its context)
// to access its properties and methods.
// =========================================================================

/// <summary>
/// State where the AI agent is idle, doing nothing specific but potentially
/// looking for a target or transitioning to patrol.
/// </summary>
public class IdleState : IState
{
    private AIAgent _agent;
    private Color _stateColor = Color.green; // Visual representation for this state.
    private float _idleTimer;
    private float _maxIdleTime = 3f;

    public IdleState(AIAgent agent)
    {
        _agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entering Idle State.");
        _agent.AgentMeshRenderer.material.color = _stateColor;
        _agent.StopAllAgentCoroutines(); // Stop any previous actions.
        _idleTimer = _maxIdleTime; // Reset idle timer.
    }

    public IState OnUpdate()
    {
        // Check for target in range (transition to ChaseState).
        if (_agent.IsTargetInRange(_agent.DetectionRange))
        {
            return new ChaseState(_agent);
        }

        // If target not found, countdown to patrol.
        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0)
        {
            return new PatrolState(_agent); // Transition to PatrolState after idling.
        }

        // Stay in Idle state if no conditions are met for transition.
        return this;
    }

    public void OnExit()
    {
        Debug.Log("Exiting Idle State.");
    }
}

/// <summary>
/// State where the AI agent moves randomly within a defined patrol radius.
/// </summary>
public class PatrolState : IState
{
    private AIAgent _agent;
    private Color _stateColor = Color.yellow; // Visual representation for this state.
    private float _minDistanceToPatrolPoint = 0.5f; // How close to consider target reached.

    public PatrolState(AIAgent agent)
    {
        _agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entering Patrol State.");
        _agent.AgentMeshRenderer.material.color = _stateColor;
        _agent.StopAllAgentCoroutines();
        // Generate a new random patrol point when entering this state.
        SetNewPatrolPoint();
    }

    public IState OnUpdate()
    {
        // Check for target in range (transition to ChaseState).
        if (_agent.IsTargetInRange(_agent.DetectionRange))
        {
            return new ChaseState(_agent);
        }

        // Move towards the current patrol point.
        _agent.MoveTowards(_agent.CurrentPatrolPoint);

        // If the agent has reached the patrol point, find a new one.
        if (Vector3.Distance(_agent.AgentTransform.position, _agent.CurrentPatrolPoint) <= _minDistanceToPatrolPoint)
        {
            SetNewPatrolPoint();
        }

        // Stay in Patrol state if no conditions are met for transition.
        return this;
    }

    public void OnExit()
    {
        Debug.Log("Exiting Patrol State.");
    }

    /// <summary>
    /// Generates a new random point within the patrol radius around the agent's starting position.
    /// </summary>
    private void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _agent.PatrolRadius;
        randomDirection += _agent.AgentTransform.position;
        // Keep the y-coordinate fixed to avoid flying/diving for simple movement.
        _agent.CurrentPatrolPoint = new Vector3(randomDirection.x, _agent.AgentTransform.position.y, randomDirection.z);
        Debug.Log($"New patrol point: {_agent.CurrentPatrolPoint}");
    }
}

/// <summary>
/// State where the AI agent actively pursues its target.
/// </summary>
public class ChaseState : IState
{
    private AIAgent _agent;
    private Color _stateColor = Color.red; // Visual representation for this state.

    public ChaseState(AIAgent agent)
    {
        _agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entering Chase State.");
        _agent.AgentMeshRenderer.material.color = _stateColor;
        _agent.StopAllAgentCoroutines();
    }

    public IState OnUpdate()
    {
        // If target is null or out of detection range, transition back to Idle/Patrol.
        if (_agent.Target == null || !_agent.IsTargetInRange(_agent.DetectionRange * 1.5f)) // Use a slightly larger range to prevent rapid flickers
        {
            return new IdleState(_agent); // Or new PatrolState(_agent);
        }

        // If target is within attack range, transition to AttackState.
        if (_agent.IsTargetInRange(_agent.AttackRange))
        {
            return new AttackState(_agent);
        }

        // Move towards the target.
        _agent.MoveTowards(_agent.Target.position);

        // Stay in Chase state if no conditions are met for transition.
        return this;
    }

    public void OnExit()
    {
        Debug.Log("Exiting Chase State.");
    }
}

/// <summary>
/// State where the AI agent is close enough to its target to perform an attack.
/// </summary>
public class AttackState : IState
{
    private AIAgent _agent;
    private Color _stateColor = Color.magenta; // Visual representation for this state.
    private float _lastAttackTime = -Mathf.Infinity; // Time of the last attack.

    public AttackState(AIAgent agent)
    {
        _agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entering Attack State.");
        _agent.AgentMeshRenderer.material.color = _stateColor;
        _agent.StopAllAgentCoroutines(); // Ensure no movement coroutines are running.
    }

    public IState OnUpdate()
    {
        // If target is null or out of attack range, transition back to ChaseState.
        if (_agent.Target == null || !_agent.IsTargetInRange(_agent.AttackRange * 1.2f)) // A bit of buffer
        {
            return new ChaseState(_agent);
        }
        
        // Ensure agent is facing the target while attacking.
        if (_agent.Target != null)
        {
            Vector3 directionToTarget = (_agent.Target.position - _agent.AgentTransform.position).normalized;
            directionToTarget.y = 0; // Keep agent upright
            if (directionToTarget != Vector3.zero)
            {
                _agent.AgentTransform.rotation = Quaternion.Slerp(_agent.AgentTransform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * _agent.MoveSpeed * 2);
            }
        }

        // Perform attack if cooldown allows.
        if (Time.time >= _lastAttackTime + _agent.AttackCooldown)
        {
            PerformAttack();
            _lastAttackTime = Time.time;
        }

        // Stay in Attack state if no conditions are met for transition.
        return this;
    }

    public void OnExit()
    {
        Debug.Log("Exiting Attack State.");
        // If there was an ongoing attack coroutine (e.g., for animations), stop it.
        if (_agent.AttackCoroutine != null)
        {
            _agent.StopCoroutine(_agent.AttackCoroutine);
            _agent.AttackCoroutine = null;
        }
    }

    /// <summary>
    /// Simulates an attack action.
    /// </summary>
    private void PerformAttack()
    {
        Debug.Log($"AI Agent is Attacking {_agent.Target.name}!");
        // Here you would trigger attack animations, deal damage, play sounds, etc.
        // For demonstration, we'll just log it.
        // If attack involves a delay/animation, you might start a coroutine here.
        // Example: _agent.AttackCoroutine = _agent.StartCoroutine(AttackAnimationAndDamage());
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create a C# Script:** In your Unity project, create a new C# script named `AIStateMachine.cs` (or copy the content into an existing script).
2.  **Create the AI Agent:**
    *   In the Unity editor, create an empty GameObject: `GameObject -> Create Empty`. Name it `AIAgent`.
    *   As a child of `AIAgent`, create a 3D Cube: `GameObject -> 3D Object -> Cube`. Name it `Body`.
    *   Select the `AIAgent` GameObject.
    *   Drag and drop the `AIStateMachine.cs` script onto the `AIAgent` GameObject in the Inspector, or click "Add Component" and search for `AIStateMachine`.
3.  **Assign References in Inspector:**
    *   With `AIAgent` selected, look at its Inspector.
    *   **Mesh Renderer:** Drag the `Body` child GameObject's `MeshRenderer` component into the `_meshRenderer` field. (The `MeshRenderer` is usually on the `Cube` child itself).
    *   **Target:** Create another GameObject in your scene (e.g., `GameObject -> 3D Object -> Sphere`). Name it `Target`. Drag this `Target` GameObject from the Hierarchy into the `_target` field on the `AIAgent` in the Inspector.
4.  **Adjust Settings:**
    *   You can modify `_moveSpeed`, `_patrolRadius`, `_detectionRange`, `_attackRange`, and `_attackCooldown` directly in the `AIAgent`'s Inspector to experiment with different behaviors.
5.  **Run the Scene:**
    *   Press the Play button in Unity.
    *   Observe the `AIAgent` (the cube). It will start by idling (green), then patrol (yellow).
    *   Move the `Target` (the sphere) closer to the `AIAgent`.
    *   When the `Target` enters the `AIAgent`'s `_detectionRange`, the `AIAgent` will start chasing it (red).
    *   When the `Target` is within `_attackRange`, the `AIAgent` will stop and "attack" (magenta), logging messages to the console.
    *   Move the `Target` away, and the `AIAgent` will revert to chasing, then patrolling/idling.

This example provides a clear and functional demonstration of the AI State Machine pattern, making it easy to understand, extend, and integrate into your own Unity projects.