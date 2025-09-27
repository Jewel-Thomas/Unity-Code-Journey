// Unity Design Pattern Example: SwimmingSystem
// This script demonstrates the SwimmingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

It seems there might be a slight misunderstanding regarding the term "SwimmingSystem design pattern." In software development, especially within the realm of game development and general programming, "SwimmingSystem" isn't a recognized or standardized design pattern like Strategy, State, Observer, Singleton, etc.

However, the request for a "SwimmingSystem" implies a need for a robust, modular, and extensible *system* to manage character swimming mechanics in Unity. This is an excellent opportunity to demonstrate how established design patterns, particularly the **State Pattern**, can be applied to build such a system effectively.

The **State Pattern** is ideal here because a character's behavior (movement, input handling, physics) changes significantly depending on whether they are on the ground, treading water, or actively swimming.

This example will:
1.  **Define an `ICharacterState` interface:** The blueprint for all character states.
2.  **Implement concrete states:** `CharacterOnGroundState`, `CharacterInWaterState`, `CharacterSwimmingState`.
3.  **Create a `CharacterMovement` script:** This acts as the "Context" that holds the current state and manages transitions between states. It also encapsulates character-specific settings and components (Rigidbody).
4.  **Create a `WaterVolume` script:** A simple trigger to detect entry into and exit from water, informing the `CharacterMovement` script.

---

### **Design Pattern Used: State Pattern**

The core of this "SwimmingSystem" example is built around the **State Pattern**.

*   **Context:** `CharacterMovement.cs` (The character that can be in various states).
*   **State Interface:** `ICharacterState.cs` (Defines methods common to all states).
*   **Concrete States:**
    *   `CharacterOnGroundState.cs` (Handles movement and actions when the character is on solid ground).
    *   `CharacterInWaterState.cs` (Handles floating, treading water, and simple buoyancy when the character is in water but not actively swimming).
    *   `CharacterSwimmingState.cs` (Handles active swimming movement, including diving and surfacing).

This approach makes the character's behavior highly modular. To add new behaviors (e.g., `DivingState`, `WallClimbingState`), you just create a new state class and define its transitions within `CharacterMovement` or other relevant states.

---

### **C# Unity Example: SwimmingSystem using the State Pattern**

To use this, create a new Unity project, then create the following C# script files and copy the code into them.

**1. `ICharacterState.cs`**
```csharp
using UnityEngine;

/// <summary>
/// Defines the interface for all character states.
/// This is the 'State' part of the State Pattern.
/// Each method represents a behavior or lifecycle event for a state.
/// </summary>
public interface ICharacterState
{
    /// <summary>
    /// Called when the character enters this state.
    /// Used for initial setup, like changing physics properties or animation.
    /// </summary>
    /// <param name="player">The CharacterMovement context that this state belongs to.</param>
    void EnterState(CharacterMovement player);

    /// <summary>
    /// Called when the character exits this state.
    /// Used for cleanup or resetting properties.
    /// </summary>
    void ExitState();

    /// <summary>
    /// Handles player input specific to this state.
    /// Called in Update().
    /// </summary>
    void HandleInput();

    /// <summary>
    /// Updates the state's logic per frame.
    /// Called in Update().
    /// </summary>
    void UpdateState();

    /// <summary>
    /// Updates the state's physics logic.
    /// Called in FixedUpdate().
    /// </summary>
    void FixedUpdateState();
}
```

**2. `CharacterMovement.cs`**
```csharp
using UnityEngine;

/// <summary>
/// The main character controller that acts as the 'Context' in the State Pattern.
/// It holds the current state and manages state transitions based on environmental factors
/// (like entering/exiting water) and input.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    // The currently active state of the character.
    public ICharacterState CurrentState { get; private set; }

    // Instances of all possible states. These are created once and reused.
    // This reduces garbage collection overhead compared to creating new states on each transition.
    public CharacterOnGroundState OnGroundState { get; private set; }
    public CharacterInWaterState InWaterState { get; private set; }
    public CharacterSwimmingState SwimmingState { get; private set; }

    [Header("Movement Settings")]
    [Tooltip("Base movement speed on ground.")]
    public float moveSpeed = 5f;
    [Tooltip("Movement speed when actively swimming.")]
    public float swimSpeed = 3f;
    [Tooltip("Force applied when jumping.")]
    public float jumpForce = 5f;
    [Tooltip("Speed at which the character rotates to face movement direction.")]
    public float rotationSpeed = 10f;

    [Header("Ground Check Settings")]
    [Tooltip("Layer Mask for what is considered ground.")]
    public LayerMask groundLayer;
    [Tooltip("Transform representing the position to check for ground.")]
    public Transform groundCheck;
    [Tooltip("Radius of the sphere used for ground checking.")]
    public float groundCheckRadius = 0.2f;
    // Read-only property indicating if the character is currently grounded.
    public bool IsGrounded { get; private set; }

    [Header("Water Detection Settings")]
    [Tooltip("How far above the water surface the player can be and still be considered 'in water'.")]
    public float waterEntryTolerance = 0.5f;
    [Tooltip("How far below the water surface the player can be and still be considered 'out of water'.")]
    public float waterExitTolerance = 0.5f;
    // The Y-level of the current water surface. Updated by WaterVolume.
    public float WaterLevel { get; private set; } = float.MinValue; // Sentinel value when not in water.
    // Read-only property indicating if the character is currently in water.
    public bool IsInWater { get; private set set; }

    // Reference to the character's Rigidbody component.
    public Rigidbody Rb { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        // Prevent the Rigidbody from rotating due to collisions, character will rotate manually.
        Rb.freezeRotation = true;

        // Initialize all state objects, passing 'this' (the CharacterMovement) as context.
        OnGroundState = new CharacterOnGroundState(this);
        InWaterState = new CharacterInWaterState(this);
        SwimmingState = new CharacterSwimmingState(this);

        // Set the initial state of the character.
        SetState(OnGroundState);
    }

    private void Update()
    {
        // Perform ground check for the OnGroundState.
        IsGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Continuously check and manage water-related state transitions.
        CheckWaterStatus();

        // Delegate input handling and state updates to the current state.
        CurrentState?.HandleInput();
        CurrentState?.UpdateState();
    }

    private void FixedUpdate()
    {
        // Delegate physics updates to the current state.
        CurrentState?.FixedUpdateState();
    }

    /// <summary>
    /// Transitions the character to a new state.
    /// This is the core state change mechanism.
    /// </summary>
    /// <param name="newState">The new ICharacterState to transition to.</param>
    public void SetState(ICharacterState newState)
    {
        // If there's a current state, call its ExitState method for cleanup.
        CurrentState?.ExitState();
        // Set the new state.
        CurrentState = newState;
        // Call the new state's EnterState method for initialization.
        CurrentState.EnterState(this);
        Debug.Log($"State Changed to: {newState.GetType().Name}");
    }

    /// <summary>
    /// Checks the character's position relative to the known WaterLevel
    /// and triggers appropriate state transitions.
    /// This allows CharacterMovement to manage its own water status internally.
    /// </summary>
    private void CheckWaterStatus()
    {
        // Only proceed if a water level has been set (meaning we are inside a WaterVolume).
        if (WaterLevel == float.MinValue)
        {
            if (IsInWater) // If we were in water but now no volume is detected
            {
                IsInWater = false;
                // If exiting water, immediately transition to OnGroundState.
                if (CurrentState == InWaterState || CurrentState == SwimmingState)
                {
                    SetState(OnGroundState);
                }
            }
            return;
        }

        bool wasInWater = IsInWater;
        float currentY = transform.position.y;

        // Determine if player is in water based on water level and tolerance.
        if (currentY <= WaterLevel + waterEntryTolerance)
        {
            IsInWater = true;
        }
        else if (currentY > WaterLevel + waterExitTolerance)
        {
            IsInWater = false;
        }

        // Handle state transitions based on changes in water status.
        if (IsInWater && !wasInWater)
        {
            // Entered water. Transition to InWaterState if not already in a water state.
            if (CurrentState != InWaterState && CurrentState != SwimmingState)
            {
                SetState(InWaterState);
            }
        }
        else if (!IsInWater && wasInWater)
        {
            // Exited water. Transition to OnGroundState if previously in a water state.
            if (CurrentState == InWaterState || CurrentState == SwimmingState)
            {
                SetState(OnGroundState);
            }
        }
    }

    /// <summary>
    /// Called by a WaterVolume when the character enters its trigger.
    /// Provides the current water surface Y-level to the character.
    /// </summary>
    /// <param name="level">The Y-coordinate of the water surface.</param>
    public void SetWaterLevel(float level)
    {
        WaterLevel = level;
    }

    // --- Gizmos for Editor Visualization ---
    private void OnDrawGizmos()
    {
        // Visualize ground check sphere.
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawSphere(groundCheck.position, groundCheckRadius);
        }

        // Visualize the detected water level.
        if (WaterLevel != float.MinValue)
        {
            Gizmos.color = Color.blue;
            // Draw a horizontal line at the water level around the character.
            Vector3 waterStart = new Vector3(transform.position.x - 5, WaterLevel, transform.position.z);
            Vector3 waterEnd = new Vector3(transform.position.x + 5, WaterLevel, transform.position.z);
            Gizmos.DrawLine(waterStart, waterEnd);
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, WaterLevel, transform.position.z), 0.3f);
        }
    }
}
```

**3. `CharacterOnGroundState.cs`**
```csharp
using UnityEngine;

/// <summary>
/// Concrete State: Handles character behavior when on solid ground.
/// </summary>
public class CharacterOnGroundState : ICharacterState
{
    private CharacterMovement _player; // Reference to the context (CharacterMovement).
    private Vector3 _moveDirection;    // Stores input-based movement direction.

    /// <summary>
    /// Constructor: Initializes the state with a reference to the CharacterMovement context.
    /// </summary>
    /// <param name="player">The CharacterMovement instance.</param>
    public CharacterOnGroundState(CharacterMovement player)
    {
        _player = player;
    }

    /// <summary>
    /// Called when entering the OnGroundState.
    /// Ensures gravity is active for ground movement.
    /// </summary>
    /// <param name="player">The CharacterMovement context.</param>
    public void EnterState(CharacterMovement player)
    {
        _player = player; // Re-assign in case state instance is reused across different players.
        _player.Rb.useGravity = true; // Gravity must be active on ground.
        _player.Rb.drag = 0f; // Reset drag from potential water states.
        _player.Rb.angularDrag = 0.05f; // Unity default angular drag.
        Debug.Log("Entered OnGroundState");
    }

    /// <summary>
    /// Called when exiting the OnGroundState.
    /// Currently, no specific cleanup is needed.
    /// </summary>
    public void ExitState()
    {
        Debug.Log("Exited OnGroundState");
    }

    /// <summary>
    /// Handles movement and jump input for ground state.
    /// </summary>
    public void HandleInput()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D keys or Left/Right arrow
        float moveZ = Input.GetAxis("Vertical");   // W/S keys or Up/Down arrow

        // Calculate movement direction relative to the player's forward/right vectors.
        _moveDirection = (_player.transform.right * moveX + _player.transform.forward * moveZ).normalized;

        // Jump input: only allow jump if grounded.
        if (Input.GetButtonDown("Jump") && _player.IsGrounded) // Spacebar by default
        {
            // Apply an instantaneous force upwards.
            _player.Rb.AddForce(Vector3.up * _player.jumpForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Updates character rotation based on movement direction.
    /// </summary>
    public void UpdateState()
    {
        // Rotate the character to face the direction of movement.
        if (_moveDirection.magnitude >= 0.1f)
        {
            // Create a rotation looking towards the move direction.
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            // Smoothly interpolate to the target rotation.
            _player.transform.rotation = Quaternion.Slerp(_player.transform.rotation, targetRotation, _player.rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Applies movement force in FixedUpdate for consistent physics.
    /// </summary>
    public void FixedUpdateState()
    {
        // Move the Rigidbody directly for character movement.
        // Using MovePosition ensures physics engine stability.
        Vector3 targetPosition = _player.Rb.position + _moveDirection * _player.moveSpeed * Time.fixedDeltaTime;
        _player.Rb.MovePosition(targetPosition);
    }
}
```

**4. `CharacterInWaterState.cs`**
```csharp
using UnityEngine;

/// <summary>
/// Concrete State: Handles character behavior when in water but not actively swimming (treading water).
/// Provides buoyancy and allows transition to active swimming.
/// </summary>
public class CharacterInWaterState : ICharacterState
{
    private CharacterMovement _player; // Reference to the context.

    /// <summary>
    /// Constructor: Initializes the state with a reference to the CharacterMovement context.
    /// </summary>
    /// <param name="player">The CharacterMovement instance.</param>
    public CharacterInWaterState(CharacterMovement player)
    {
        _player = player;
    }

    /// <summary>
    /// Called when entering the InWaterState.
    /// Adjusts physics properties for water environment (drag, gravity).
    /// </summary>
    /// <param name="player">The CharacterMovement context.</param>
    public void EnterState(CharacterMovement player)
    {
        _player = player;
        _player.Rb.useGravity = true; // Still affected by gravity, but buoyancy counteracts it.
        _player.Rb.drag = 5f; // Increased drag to simulate water resistance.
        _player.Rb.angularDrag = 5f; // Increased angular drag.
        Debug.Log("Entered InWaterState (Treading Water)");
    }

    /// <summary>
    /// Called when exiting the InWaterState.
    /// Resets physics properties to default or prepares for the next state.
    /// </summary>
    public void ExitState()
    {
        // Drag will be reset by the OnGroundState or SwimmingState depending on transition.
        Debug.Log("Exited InWaterState");
    }

    /// <summary>
    /// Checks for input to transition to active swimming.
    /// </summary>
    public void HandleInput()
    {
        // If 'Jump' button is held down, transition to active SwimmingState.
        if (Input.GetButton("Jump")) // Spacebar by default
        {
            _player.SetState(_player.SwimmingState);
        }
    }

    /// <summary>
    /// Applies buoyancy force to keep the character floating.
    /// </summary>
    public void UpdateState()
    {
        // Simple buoyancy calculation:
        // The deeper the character is below the water level, the more buoyancy force.
        float depth = _player.WaterLevel - _player.transform.position.y;
        if (depth > 0) // Only apply buoyancy if character is actually below the water surface.
        {
            // A simple approximation: buoyancy proportional to depth and character's mass.
            float buoyancyForce = Mathf.Abs(Physics.gravity.y) * _player.Rb.mass * (depth * 0.2f);
            _player.Rb.AddForce(Vector3.up * buoyancyForce, ForceMode.Acceleration);
        }

        // Keep character upright and facing forward slightly, even when treading water.
        Quaternion targetRotation = Quaternion.Euler(0, _player.transform.eulerAngles.y, 0);
        _player.transform.rotation = Quaternion.Slerp(_player.transform.rotation, targetRotation, _player.rotationSpeed * Time.deltaTime * 0.5f);
    }

    /// <summary>
    /// No active movement in this state, purely passive floating and buoyancy.
    /// </summary>
    public void FixedUpdateState()
    {
        // No direct movement forces in this state; buoyancy is applied in Update.
    }
}
```

**5. `CharacterSwimmingState.cs`**
```csharp
using UnityEngine;

/// <summary>
/// Concrete State: Handles character behavior when actively swimming in water.
/// Allows for 3D movement (up, down, forward, sideways).
/// </summary>
public class CharacterSwimmingState : ICharacterState
{
    private CharacterMovement _player; // Reference to the context.
    private Vector3 _swimDirection;    // Stores the calculated 3D swim direction.
    private float _verticalInput;      // Stores input for vertical swimming (up/down).

    /// <summary>
    /// Constructor: Initializes the state with a reference to the CharacterMovement context.
    /// </summary>
    /// <param name="player">The CharacterMovement instance.</param>
    public CharacterSwimmingState(CharacterMovement player)
    {
        _player = player;
    }

    /// <summary>
    /// Called when entering the SwimmingState.
    /// Disables gravity for free 3D swimming and sets high drag.
    /// </summary>
    /// <param name="player">The CharacterMovement context.</param>
    public void EnterState(CharacterMovement player)
    {
        _player = player;
        _player.Rb.useGravity = false; // Disable gravity for full 3D swimming.
        _player.Rb.drag = 8f; // High drag for strong water resistance during active swimming.
        _player.Rb.angularDrag = 8f;
        Debug.Log("Entered SwimmingState");
    }

    /// <summary>
    /// Called when exiting the SwimmingState.
    /// Re-enables gravity and resets drag if exiting water completely.
    /// </summary>
    public void ExitState()
    {
        // If exiting water (not just transitioning to treading water), reset physics props.
        if (!_player.IsInWater)
        {
            _player.Rb.useGravity = true;
            _player.Rb.drag = 0f;
            _player.Rb.angularDrag = 0.05f;
        }
        Debug.Log("Exited SwimmingState");
    }

    /// <summary>
    /// Handles 3D movement input (horizontal, vertical, and depth).
    /// </summary>
    public void HandleInput()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        _verticalInput = 0;
        if (Input.GetKey(KeyCode.Space)) _verticalInput = 1;         // Swim up
        if (Input.GetKey(KeyCode.LeftControl)) _verticalInput = -1; // Swim down

        // Combine horizontal, vertical, and depth input.
        Vector3 desiredMoveLocal = new Vector3(moveX, _verticalInput, moveZ).normalized;

        // Convert local input to world space relative to the player's current orientation.
        _swimDirection = _player.transform.TransformDirection(desiredMoveLocal);

        // Transition back to InWaterState if the 'Jump' button (active swim trigger) is released
        // AND there is no vertical swimming input. This means the player wants to stop active swimming.
        if (!Input.GetButton("Jump") && _verticalInput == 0 && desiredMoveLocal.magnitude < 0.1f)
        {
            _player.SetState(_player.InWaterState);
        }
    }

    /// <summary>
    /// Rotates the character to face the direction of swimming.
    /// </summary>
    public void UpdateState()
    {
        // Rotate the character to align with the active swim direction.
        if (_swimDirection.magnitude >= 0.1f)
        {
            // Quaternion.LookRotation creates a rotation that looks along forward and aligns up.
            // We want to keep the player's "up" vector generally aligned with world up.
            Quaternion targetRotation = Quaternion.LookRotation(_swimDirection, Vector3.up);
            _player.transform.rotation = Quaternion.Slerp(_player.transform.rotation, targetRotation, _player.rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Applies swimming force to the Rigidbody.
    /// </summary>
    public void FixedUpdateState()
    {
        // Apply force in the swim direction.
        if (_swimDirection.magnitude >= 0.1f)
        {
            _player.Rb.AddForce(_swimDirection * _player.swimSpeed * _player.Rb.mass, ForceMode.Acceleration);
        }

        // Optionally, limit the maximum swimming speed to prevent excessive acceleration.
        _player.Rb.velocity = Vector3.ClampMagnitude(_player.Rb.velocity, _player.swimSpeed * 1.5f);
    }
}
```

**6. `WaterVolume.cs`**
```csharp
using UnityEngine;

/// <summary>
/// A simple component to define an area of water.
/// It uses a trigger collider to detect when the CharacterMovement enters or exits the water.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WaterVolume : MonoBehaviour
{
    [Tooltip("The Y-level that defines the exact water surface. Usually the top of the collider.")]
    public float waterSurfaceY;

    private void Awake()
    {
        // Ensure the collider is a trigger, so it doesn't block movement.
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} is not a trigger. Setting to true.", this);
            col.isTrigger = true;
        }

        // If waterSurfaceY isn't explicitly set in the inspector, default to the top bound of the collider.
        if (waterSurfaceY == 0f && col != null)
        {
            waterSurfaceY = col.bounds.max.y;
            Debug.Log($"Water surface Y not explicitly set, defaulting to collider top bound: {waterSurfaceY}", this);
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// </summary>
    /// <param name="other">The collider that entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Try to get CharacterMovement component from the entering collider's GameObject.
        CharacterMovement player = other.GetComponent<CharacterMovement>();
        if (player != null)
        {
            // Inform the player about the water level.
            player.SetWaterLevel(waterSurfaceY);
            // CharacterMovement itself will determine if it's "in water" based on its Y position
            // relative to this water level and manage state transitions.
        }
    }

    /// <summary>
    /// Called when another collider exits this trigger.
    /// </summary>
    /// <param name="other">The collider that exited.</param>
    private void OnTriggerExit(Collider other)
    {
        CharacterMovement player = other.GetComponent<CharacterMovement>();
        if (player != null)
        {
            // Inform the player that no water volume is currently affecting it by resetting the water level.
            // This allows the player to correctly transition out of water states.
            player.SetWaterLevel(float.MinValue); // Using float.MinValue as a sentinel value.
        }
    }

    /// <summary>
    /// Draws a transparent blue cube for the water volume and a cyan line at the surface.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.5f, 1f, 0.5f); // Transparent blue
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }

        // Draw the water surface level as a cyan line.
        if (waterSurfaceY != float.MinValue)
        {
            Gizmos.color = Color.cyan;
            // Draw a wider line across the scene to clearly show the water surface.
            Vector3 surfaceStart = new Vector3(transform.position.x - 100, waterSurfaceY, transform.position.z - 100);
            Vector3 surfaceEnd = new Vector3(transform.position.x + 100, waterSurfaceY, transform.position.z + 100);
            Gizmos.DrawLine(surfaceStart, surfaceEnd);
        }
    }
}
```

---

### **How to Set Up in Unity:**

1.  **Create a Player GameObject:**
    *   Create an empty GameObject (e.g., "Player").
    *   Add a `Capsule Collider` or `Box Collider` (adjust size as needed, e.g., radius 0.5, height 2).
    *   Add a `Rigidbody` component. Make sure `Use Gravity` is checked, and `Freeze Rotation` (X, Y, Z) is checked to prevent tumbling.
    *   Add the `CharacterMovement` script to this GameObject.
    *   Create an empty child GameObject named "GroundCheck" and position it at the bottom of your character collider (e.g., Y position -0.9 for a capsule collider of height 2). Assign this to the `Ground Check` field in the `CharacterMovement` script.
    *   **Layer:** Create a new Layer called "Player" and assign your Player GameObject to it. Set `groundLayer` in `CharacterMovement` to exclude the "Player" layer (so the character doesn't ground-check itself).
    *   **Input:** Ensure Unity's default input axes ("Horizontal", "Vertical", "Jump") are configured (they usually are by default in `Edit > Project Settings > Input Manager`).

2.  **Create a Ground GameObject:**
    *   Create a 3D object (e.g., a `Cube`) to serve as the ground. Scale it up (e.g., X:100, Y:0.1, Z:100).
    *   Ensure it has a collider.
    *   **Layer:** Set its layer to "Default" or a custom "Ground" layer, and ensure this layer is included in `groundLayer` in your `CharacterMovement` script.

3.  **Create a Water GameObject:**
    *   Create a 3D object (e.g., a `Cube`) to represent your water body.
    *   Position it so its top surface is where you want the water level to be (e.g., Y=0).
    *   Scale it (e.g., X:50, Y:2, Z:50).
    *   Add the `WaterVolume` script to it.
    *   **Collider:** Ensure its `Box Collider` (or whatever collider type) has `Is Trigger` checked.
    *   **Appearance (Optional):** Create a new `Material` (e.g., blue, transparent) and assign it to the water cube to make it look like water.

4.  **Test:**
    *   Run the scene.
    *   Use W/A/S/D to move the character on the ground.
    *   Press Space to jump.
    *   Walk into the water. The character should transition to `InWaterState`, float, and automatically disable gravity if it falls too deep.
    *   Hold Space in water to enter `SwimmingState`. Use W/A/S/D to swim horizontally, Space to swim up, and Left Control to swim down.
    *   Release Space (and vertical input) to return to `InWaterState`.
    *   Swim out of the water, and the character should transition back to `OnGroundState`.

---

### **Further Enhancements & Other Patterns:**

This basic SwimmingSystem can be greatly expanded. Here are ideas and how other patterns could be used:

*   **Strategy Pattern:**
    *   Instead of hardcoding `_player.swimSpeed`, you could have different `ISwimmingStyle` strategies (e.g., `FastSwimStrategy`, `StealthSwimStrategy`) that implement different movement calculations, speeds, or even animations, which the `CharacterSwimmingState` then uses. This allows for dynamic changes in swimming behavior without modifying the state itself.
*   **Observer Pattern:**
    *   Have a `SwimmingSystemEvents` class with `OnEnteredWater`, `OnExitedWater`, `OnStartedSwimming`, etc., events. Other systems (UI, VFX, audio) can subscribe to these events. For example, a UI script could display a "Diving" indicator, an audio script could play water sounds, or a VFX script could emit bubbles.
*   **Command Pattern:**
    *   Player input could be abstracted into `SwimUpCommand`, `SwimForwardCommand`, etc. This allows for easier remapping, macro creation, or recording/playback of actions.
*   **Composite Pattern:**
    *   If you have complex character abilities that are made up of simpler ones, you could use Composite. For example, a "SpecialDive" ability might combine "SwimDown" with a "ForwardDash."
*   **Service Locator / Dependency Injection:**
    *   For larger projects, managing dependencies (like the Camera for relative movement, or a separate AnimationController) could be done via a Service Locator or a proper Dependency Injection framework instead of direct `GetComponent` calls or constructor injection.

This example provides a strong foundation for a flexible and maintainable character swimming system, demonstrating the practical application of the State design pattern in Unity.