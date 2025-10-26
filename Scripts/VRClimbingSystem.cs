// Unity Design Pattern Example: VRClimbingSystem
// This script demonstrates the VRClimbingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example implements a 'VRClimbingSystem' design pattern using Unity's component-based architecture, a state machine, and a publisher-subscriber model for communication. It focuses on clarity, extensibility, and practical application.

The core idea of the `VRClimbingSystem` pattern here is to break down the climbing functionality into three distinct, interacting components:
1.  **`GrabbableClimbPoint`**: Marks world objects as climbable, acting as a data component.
2.  **`VRClimbingHand`**: Handles controller input and detects climbable objects, acting as a publisher of grab/release events.
3.  **`ClimberPlayer`**: Manages the player's overall climbing state and movement logic, acting as the central orchestrator (controller) and subscriber to hand events, incorporating a state machine for player behavior.

This modular approach ensures that each component has a single responsibility, making the system easy to understand, modify, and extend.

---

### C# Unity Scripts

Below are the three C# scripts required for the VRClimbingSystem. Save each script in your Unity project with its respective name.

---

#### 1. `GrabbableClimbPoint.cs`

This script simply marks a GameObject as a point that can be grabbed for climbing. It can optionally define a more precise grab location than the object's root transform.

```csharp
using UnityEngine;

/// <summary>
/// GrabbableClimbPoint.cs
/// Marks an object as a climbable point in the VRClimbingSystem.
/// Design Pattern: This acts as a simple Data Component.
/// Its primary role is to provide information about a climbable spot.
/// </summary>
public class GrabbableClimbPoint : MonoBehaviour
{
    [Tooltip("Optional: If assigned, this Transform will be used as the actual grab point. " +
             "Otherwise, the object's own Transform will be used.")]
    public Transform grabReferencePoint;

    /// <summary>
    /// Gets the world position where a hand should visually attach or where the grab logic should refer to.
    /// </summary>
    public Vector3 GetGrabPointWorldPosition()
    {
        return grabReferencePoint != null ? grabReferencePoint.position : transform.position;
    }

    /// <summary>
    /// Gets the Transform that represents the actual grab point.
    /// </summary>
    public Transform GetGrabTransform()
    {
        return grabReferencePoint != null ? grabReferencePoint : transform;
    }

    // Visual aid in the editor to show where the grab point is.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(GetGrabPointWorldPosition(), 0.05f);
        Gizmos.color = Color.white;
    }
}
```

---

#### 2. `VRClimbingHand.cs`

This script is attached to your VR controller (hand) objects. It detects `GrabbableClimbPoint` objects within its trigger collider and handles the input for grabbing and releasing. It then notifies the `ClimberPlayer` of these events.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines, though not used in this simplified version, good practice.
using System.Collections.Generic; // Required for List, Dictionary, etc.

/// <summary>
/// VRClimbingHand.cs
/// Represents a VR hand (controller) that can grab climb points.
/// It detects GrabbableClimbPoint objects in its vicinity and processes grab/release input.
/// Design Pattern: This acts as a Publisher. It publishes (notifies) grab/release events
/// to the ClimberPlayer, which acts as the Subscriber.
/// </summary>
[RequireComponent(typeof(Collider))] // Needs a trigger collider to detect climb points
[RequireComponent(typeof(Rigidbody))] // Needs a Rigidbody to work with triggers, should be kinematic
public class VRClimbingHand : MonoBehaviour
{
    [Tooltip("Reference to the main ClimberPlayer script in the scene.")]
    public ClimberPlayer climberPlayer;

    [Tooltip("The input button name for grabbing (e.g., 'GrabLeft', 'GrabRight'). " +
             "These need to be configured in Unity's Input Manager.")]
    public string grabInputButton = "GrabLeft"; 

    [Tooltip("If true, this hand will only respond to its assigned grabInputButton. " +
             "If false, it will use a generic 'Fire1' button for testing.")]
    public bool useSpecificGrabButton = true;

    private GrabbableClimbPoint _hoveringClimbPoint;        // The climb point currently under the hand's collider
    private GrabbableClimbPoint _currentlyGrabbedClimbPoint; // The climb point currently held by this hand
    private Transform _currentGrabbedReferenceTransform;     // The actual transform of the grabbed point (from GrabbableClimbPoint)
    private bool _isGrabbing = false;

    // Optional: Unity events for other systems to subscribe if needed (e.g., for visual feedback).
    // Not directly part of the core VRClimbingSystem pattern but good for extensibility.
    public delegate void OnGrabEvent(GrabbableClimbPoint grabbedPoint);
    public event OnGrabEvent OnHandGrabbedPoint;

    public delegate void OnReleaseEvent();
    public event OnReleaseEvent OnHandReleasedPoint;

    void Awake()
    {
        // Ensure the attached collider is a trigger to detect overlaps without physical collision.
        Collider handCollider = GetComponent<Collider>();
        if (handCollider != null)
        {
            handCollider.isTrigger = true;
        }

        // Ensure Rigidbody is kinematic to prevent physics interactions but allow trigger events.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false; // Hands should not be affected by gravity.
        }

        // Essential safety check: Ensure the ClimberPlayer reference is set.
        if (climberPlayer == null)
        {
            Debug.LogError("VRClimbingHand: 'climberPlayer' not assigned! Please assign the ClimberPlayer script in the Inspector.", this);
            enabled = false; // Disable script if essential reference is missing
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Checks for input to grab or release.
    /// </summary>
    private void HandleInput()
    {
        // Determine if the grab button is currently pressed.
        // Use Input.GetButton for VR controllers or custom input maps.
        // Use a generic 'Fire1' (left mouse button) for easy testing in editor without specific VR input setup.
        bool grabButtonPressed = useSpecificGrabButton ? Input.GetButton(grabInputButton) : Input.GetButton("Fire1");

        // If button pressed AND not currently grabbing AND hovering over a climb point, try to grab.
        if (grabButtonPressed && !_isGrabbing)
        {
            TryGrab();
        }
        // If button released AND currently grabbing, release.
        else if (!grabButtonPressed && _isGrabbing)
        {
            Release();
        }
    }

    /// <summary>
    /// Attempts to grab the currently hovered climb point.
    /// </summary>
    private void TryGrab()
    {
        if (_hoveringClimbPoint != null)
        {
            _currentlyGrabbedClimbPoint = _hoveringClimbPoint;
            _currentGrabbedReferenceTransform = _currentlyGrabbedClimbPoint.GetGrabTransform();
            _isGrabbing = true;

            // Notify the ClimberPlayer that this hand has grabbed something.
            climberPlayer.OnHandGrabbed(this, _currentlyGrabbedClimbPoint);

            // Invoke local event for any subscribers (e.g., hand animation, haptic feedback).
            OnHandGrabbedPoint?.Invoke(_currentlyGrabbedClimbPoint); 
            Debug.Log($"{name} grabbed {_currentlyGrabbedClimbPoint.name}");
        }
    }

    /// <summary>
    /// Releases the currently grabbed climb point.
    /// </summary>
    private void Release()
    {
        if (_isGrabbing)
        {
            _isGrabbing = false;
            _currentlyGrabbedClimbPoint = null;
            _currentGrabbedReferenceTransform = null;

            // Notify the ClimberPlayer that this hand has released its grab.
            climberPlayer.OnHandReleased(this);

            // Invoke local event for any subscribers.
            OnHandReleasedPoint?.Invoke(); 
            Debug.Log($"{name} released.");
        }
    }

    /// <summary>
    /// Called when another collider enters this hand's trigger volume.
    /// </summary>
    /// <param name="other">The collider that entered.</param>
    void OnTriggerEnter(Collider other)
    {
        GrabbableClimbPoint climbPoint = other.GetComponent<GrabbableClimbPoint>();
        if (climbPoint != null)
        {
            _hoveringClimbPoint = climbPoint;
            // Debug.Log($"{name} hovering over {climbPoint.name}");
        }
    }

    /// <summary>
    /// Called when another collider exits this hand's trigger volume.
    /// </summary>
    /// <param name="other">The collider that exited.</param>
    void OnTriggerExit(Collider other)
    {
        GrabbableClimbPoint climbPoint = other.GetComponent<GrabbableClimbPoint>();
        // Only clear _hoveringClimbPoint if the exiting object is indeed the one we were hovering over.
        if (climbPoint != null && climbPoint == _hoveringClimbPoint)
        {
            _hoveringClimbPoint = null;
            // Debug.Log($"{name} no longer hovering over {climbPoint.name}");
        }
    }

    /// <summary>
    /// Returns the GrabbableClimbPoint currently being held by this hand.
    /// </summary>
    public GrabbableClimbPoint GetCurrentlyGrabbedClimbPoint()
    {
        return _currentlyGrabbedClimbPoint;
    }

    /// <summary>
    /// Returns the actual world space transform of the grabbed point (could be grabReferencePoint or the object itself).
    /// </summary>
    public Transform GetCurrentGrabbedReferenceTransform()
    {
        return _currentGrabbedReferenceTransform;
    }

    /// <summary>
    /// Checks if this hand is currently actively grabbing something.
    /// </summary>
    public bool IsGrabbing()
    {
        return _isGrabbing;
    }
}
```

---

#### 3. `ClimberPlayer.cs`

This is the central script responsible for managing the player's climbing state, responding to hand events, and applying movement. It effectively orchestrates the climbing experience.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List and Dictionary

/// <summary>
/// ClimberPlayer.cs
/// The core component that manages the player's climbing state and movement.
/// Design Pattern: This acts as the central orchestrator (Controller) and also implements
/// an internal State Machine for the player's climbing behavior. It subscribes to events
/// (method calls) from the VRClimbingHand components.
/// </summary>
[RequireComponent(typeof(Rigidbody))] // Player needs a Rigidbody for physics interaction (gravity, movement)
public class ClimberPlayer : MonoBehaviour
{
    [Tooltip("The left VR hand controller script.")]
    public VRClimbingHand leftHand;
    [Tooltip("The right VR hand controller script.")]
    public VRClimbingHand rightHand;

    [Tooltip("Optional: An offset transform relative to the player's root (e.g., camera/head). " +
             "Used to adjust the player's root position so the camera/head lands correctly " +
             "at the calculated climb position.")]
    public Transform cameraOffsetTransform; // e.g., VR camera rig or head transform

    [Tooltip("How much gravity affects the player when not climbing.")]
    public float gravityMultiplier = 1.0f;

    [Tooltip("The maximum speed the player can fall when not climbing. Prevents excessive acceleration.")]
    public float maxFallSpeed = 10f;

    private Rigidbody _rigidbody;
    private List<VRClimbingHand> _activeGrabs = new List<VRClimbingHand>(); // List of hands currently grabbing.

    // A struct to store the necessary data for each active grab.
    // This allows us to track the initial relationship between the player and hand when a grab occurs.
    private struct GrabData
    {
        public VRClimbingHand hand;
        public GrabbableClimbPoint climbPoint;
        // This offset stores the vector from the hand's world position to the player's world position AT THE MOMENT OF GRAB.
        // When the hand moves, we want the player to move such that this offset is maintained, effectively pulling the player.
        public Vector3 playerToHandWorldOffset; 
    }
    private List<GrabData> _grabDataList = new List<GrabData>();

    // Internal state machine for the player's climbing behavior.
    private enum ClimberState
    {
        Idle,       // Not grabbing, free to move (subject to gravity), on ground.
        Climbing,   // At least one hand grabbing, gravity disabled, movement based on hand input.
        Falling     // No hands grabbing, player is in mid-air and subject to gravity.
    }
    private ClimberState _currentState = ClimberState.Idle;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        // Ensure Rigidbody starts with physics enabled, ClimberPlayer will manage kinematics for climbing.
        _rigidbody.isKinematic = false; 
        _rigidbody.useGravity = true;

        if (leftHand == null || rightHand == null)
        {
            Debug.LogError("ClimberPlayer: Left or Right hand not assigned! Please assign both VRClimbingHand scripts in the Inspector.", this);
            enabled = false; // Disable script if essential references are missing.
            return;
        }

        // Ensure both hands have a reference back to this ClimberPlayer.
        // This allows hands to notify the player about grab/release events.
        leftHand.climberPlayer = this;
        rightHand.climberPlayer = this;
    }

    void FixedUpdate()
    {
        // Physics updates should happen in FixedUpdate for consistency.
        HandlePlayerStatePhysics();
    }

    /// <summary>
    /// Called by VRClimbingHand when it successfully grabs a GrabbableClimbPoint.
    /// Design Pattern: This is a Subscriber method, reacting to the VRClimbingHand's "publication".
    /// </summary>
    /// <param name="hand">The VRClimbingHand component that initiated the grab.</param>
    /// <param name="climbPoint">The GrabbableClimbPoint that was grabbed.</param>
    public void OnHandGrabbed(VRClimbingHand hand, GrabbableClimbPoint climbPoint)
    {
        if (!_activeGrabs.Contains(hand))
        {
            _activeGrabs.Add(hand); // Track which hand is active.

            // Store crucial data for calculating player movement.
            GrabData data = new GrabData();
            data.hand = hand;
            data.climbPoint = climbPoint;
            // Calculate the offset from the hand's current world position to the player's current world position.
            // This offset needs to be maintained when the hand moves.
            data.playerToHandWorldOffset = transform.position - hand.transform.position;
            _grabDataList.Add(data);
            
            Debug.Log($"Player received grab from {hand.name}. Total active grabs: {_activeGrabs.Count}");

            UpdateClimberState(); // Re-evaluate player's state.
        }
    }

    /// <summary>
    /// Called by VRClimbingHand when it releases its GrabbableClimbPoint.
    /// Design Pattern: Another Subscriber method.
    /// </summary>
    /// <param name="hand">The VRClimbingHand component that initiated the release.</param>
    public void OnHandReleased(VRClimbingHand hand)
    {
        if (_activeGrabs.Contains(hand))
        {
            _activeGrabs.Remove(hand); // Remove from active grabs list.
            _grabDataList.RemoveAll(g => g.hand == hand); // Remove corresponding grab data.
            
            Debug.Log($"Player received release from {hand.name}. Total active grabs: {_activeGrabs.Count}");

            UpdateClimberState(); // Re-evaluate player's state.
        }
    }

    /// <summary>
    /// Determines and updates the player's current climbing state based on active grabs.
    /// </summary>
    private void UpdateClimberState()
    {
        if (_activeGrabs.Count > 0)
        {
            TransitionToState(ClimberState.Climbing);
        }
        else // No hands are currently grabbing.
        {
            // If the player was previously climbing, they should now fall.
            if (_currentState == ClimberState.Climbing)
            {
                TransitionToState(ClimberState.Falling);
            }
            // If already falling or idle, stay in that state (Idle will check if grounded).
        }
    }

    /// <summary>
    /// Handles the transition between different ClimberStates, adjusting Rigidbody properties.
    /// Design Pattern: Part of the State Machine implementation.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void TransitionToState(ClimberState newState)
    {
        if (_currentState == newState) return; // No state change needed.

        _currentState = newState;
        Debug.Log($"Player State Transition: {_currentState}");

        switch (_currentState)
        {
            case ClimberState.Idle:
                _rigidbody.isKinematic = false; // Allow physics to control the player.
                _rigidbody.useGravity = true;   // Apply gravity.
                _rigidbody.velocity = Vector3.zero; // Stop any residual movement when becoming idle.
                break;
            case ClimberState.Climbing:
                _rigidbody.isKinematic = true;  // Player's movement is now manually controlled by this script.
                _rigidbody.useGravity = false;  // Disable gravity while climbing.
                _rigidbody.velocity = Vector3.zero; // Stop any previous velocity.
                _rigidbody.angularVelocity = Vector3.zero; // Stop any previous rotation.
                break;
            case ClimberState.Falling:
                _rigidbody.isKinematic = false; // Re-enable physics for falling.
                _rigidbody.useGravity = true;   // Apply gravity.
                // Velocity is preserved from when they released, allowing for momentum-based throws.
                break;
        }
    }

    /// <summary>
    /// Applies physics and movement logic based on the current player state.
    /// </summary>
    private void HandlePlayerStatePhysics()
    {
        switch (_currentState)
        {
            case ClimberState.Idle:
            case ClimberState.Falling:
                // Apply gravity and clamp fall speed when not climbing.
                Vector3 currentVelocity = _rigidbody.velocity;
                currentVelocity += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
                currentVelocity.y = Mathf.Max(currentVelocity.y, -maxFallSpeed); // Clamp max fall speed.
                _rigidbody.velocity = currentVelocity;

                // If falling and suddenly grounded, transition to idle.
                if (_currentState == ClimberState.Falling && IsGrounded())
                {
                    TransitionToState(ClimberState.Idle);
                }
                break;

            case ClimberState.Climbing:
                // If somehow no hands are grabbing but we are in Climbing state, transition to falling as a fallback.
                if (_grabDataList.Count == 0)
                {
                    TransitionToState(ClimberState.Falling);
                    return;
                }
                
                // Calculate the target position for the player based on all active grabs.
                // We average the desired player positions from each hand.
                Vector3 combinedTargetPlayerPosition = Vector3.zero;
                foreach (var grabData in _grabDataList)
                {
                    // For each grabbed hand, calculate where the player's root *should be*
                    // if it were to maintain its initial relative position to that hand.
                    // This creates the "pull" effect.
                    combinedTargetPlayerPosition += grabData.hand.transform.position + grabData.playerToHandWorldOffset;
                }
                combinedTargetPlayerPosition /= _grabDataList.Count; // Average the target positions.

                // Adjust the target position to account for the camera/head offset if provided.
                // This ensures that when the player moves, their camera/head stays in the correct
                // relative position to the climbing surface, not just the player's root.
                if (cameraOffsetTransform != null)
                {
                    // Calculate the current offset from the player's root to the camera/head.
                    Vector3 currentCameraToPlayerRootOffset = transform.position - cameraOffsetTransform.position;
                    // Apply this inverse offset to the calculated target position, so that the
                    // camera/head ends up where `combinedTargetPlayerPosition` would normally be.
                    combinedTargetPlayerPosition += currentCameraToPlayerRootOffset;
                }

                // Smoothly interpolate towards the target position.
                // Using Rigidbody.position for physics-related movement.
                _rigidbody.position = Vector3.Lerp(_rigidbody.position, combinedTargetPlayerPosition, Time.fixedDeltaTime * 20f); // 20f is a smoothing factor.
                break;
        }
    }

    /// <summary>
    /// A simple check to determine if the player is grounded.
    /// For robust games, this should be improved with more detailed raycasts, spherecasts,
    /// or checking collision flags from a CharacterController.
    /// </summary>
    /// <returns>True if the player is close to the ground, false otherwise.</returns>
    private bool IsGrounded()
    {
        // Adjust the raycast distance to be slightly more than the collider half-extents.
        // Assumes player has a collider at its root.
        float groundCheckDistance = 0.2f; 
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    // Visual aid in the editor to show the calculated target positions during climbing.
    void OnDrawGizmos()
    {
        if (Application.isPlaying && _currentState == ClimberState.Climbing)
        {
            Gizmos.color = Color.magenta;
            Vector3 avgTargetPos = Vector3.zero;
            foreach (var grabData in _grabDataList)
            {
                // Draw a line from the hand to its calculated ideal player position
                Vector3 targetForThisHand = grabData.hand.transform.position + grabData.playerToHandWorldOffset;
                Gizmos.DrawLine(grabData.hand.transform.position, targetForThisHand);
                Gizmos.DrawSphere(targetForThisHand, 0.08f);
                avgTargetPos += targetForThisHand;
            }
            if (_grabDataList.Count > 0)
            {
                avgTargetPos /= _grabDataList.Count;

                // Adjust for camera offset for the final visualized target
                if (cameraOffsetTransform != null)
                {
                     Vector3 currentCameraToPlayerRootOffset = transform.position - cameraOffsetTransform.position;
                     avgTargetPos += currentCameraToPlayerRootOffset;
                }
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(avgTargetPos, 0.15f); // Final calculated player position
                Gizmos.DrawLine(transform.position, avgTargetPos); // From current player position to target
            }
        }
    }
}
```

---

### Example Usage and Setup in Unity

Follow these steps to set up the VRClimbingSystem in a Unity project.

#### 1. Project Setup (Input Manager)

This example uses Unity's default Input Manager for generic `GrabLeft` and `GrabRight` buttons, making it testable without a specific VR SDK. For a real VR project, you would replace `Input.GetButton` with your VR SDK's input calls (e.g., `OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch)` for Oculus, or similar for OpenXR/SteamVR).

1.  Go to `Edit -> Project Settings -> Input Manager`.
2.  Expand `Axes`.
3.  Increase the `Size` by at least 2.
4.  Configure two new axes:

    *   **Name:** `GrabLeft`
        *   **Positive Button:** `joystick button 4` (common for left VR trigger), or `mouse 0` (for testing with mouse)
        *   **Alt Positive Button:** `left shift` (for keyboard testing)
        *   **Type:** `Button`
        *   **Joy Num:** `Get Motion From All Joysticks`
    *   **Name:** `GrabRight`
        *   **Positive Button:** `joystick button 5` (common for right VR trigger), or `mouse 1` (for testing with mouse)
        *   **Alt Positive Button:** `right shift` (for keyboard testing)
        *   **Type:** `Button`
        *   **Joy Num:** `Get Motion From All Joysticks`

#### 2. Player GameObject (`VRPlayer`)

1.  Create an empty `GameObject` in your scene and name it `VRPlayer`. This will be the root of your player character.
2.  Add a `Rigidbody` component to `VRPlayer`.
    *   Set `Mass` to `70` (or a realistic player weight).
    *   `Drag`, `Angular Drag` can be default.
    *   `Is Kinematic` should be **unchecked** initially (the `ClimberPlayer` script will manage this).
    *   `Use Gravity` should be **checked** initially.
    *   `Collision Detection`: `Continuous` is often recommended for player characters.
    *   `Constraints`: Freeze Rotation X, Y, Z to prevent unwanted tipping.
3.  Add the `ClimberPlayer` script to `VRPlayer`.
4.  In the `ClimberPlayer` inspector:
    *   Leave `Left Hand` and `Right Hand` unassigned for now.
    *   `Camera Offset Transform`: Drag your main camera (or your VR headset tracking object) into this field. This is crucial for correctly positioning the player's root based on their head/camera, ensuring a comfortable VR experience during climbing. If your VR camera is a child of the `VRPlayer` root, this helps align the player's body beneath the head.

#### 3. Hand Controller GameObjects (`LeftHand`, `RightHand`)

1.  As children of your `VRPlayer` GameObject (or wherever your VR hand tracking objects are located in your hierarchy), create two empty `GameObject`s: `LeftHand` and `RightHand`. These represent your virtual hands.
2.  For each hand (`LeftHand` and `RightHand`):
    *   Add a `Sphere Collider` component (or `Capsule Collider`).
        *   Set `Is Trigger` to **true**.
        *   Adjust `Radius` (e.g., `0.1` to `0.2`) to define the grab detection range.
    *   Add a `Rigidbody` component.
        *   Set `Is Kinematic` to **true**.
        *   Set `Use Gravity` to **false**.
    *   Add the `VRClimbingHand` script.
    *   In the `VRClimbingHand` inspector:
        *   `Climber Player`: Drag the `VRPlayer` object (which has the `ClimberPlayer` script) into this field.
        *   `Grab Input Button`: For `LeftHand`, type `GrabLeft`. For `RightHand`, type `GrabRight`.
        *   `Use Specific Grab Button`: Keep this checked for normal VR use. Uncheck for simple mouse `Fire1` testing.

3.  Now, go back to the `VRPlayer` GameObject and assign the `LeftHand` and `RightHand` GameObjects to the corresponding fields in the `ClimberPlayer` script.

#### 4. Climbable Objects (`ClimbWall`, `ClimbHandle`)

1.  Create a `GameObject` in your scene that will serve as a climbable surface (e.g., a simple `Cube`, or a custom mesh). Name it `ClimbWall`.
2.  Add a `Box Collider` (or appropriate collider for its shape) to `ClimbWall`. Make sure `Is Trigger` is **false** for this collider; it represents the physical wall.
3.  As children of `ClimbWall`, create empty `GameObject`s for individual grab points (e.g., `ClimbHandle1`, `ClimbHandle2`). Position them where you want the player to be able to grab.
4.  For each `ClimbHandle` (or other desired climbable element):
    *   Add a `Sphere Collider` (or `Box Collider`) component.
        *   Set `Is Trigger` to **true**.
        *   Adjust `Radius` or `Size` to define the interactive grab area for the hand.
    *   Add the `GrabbableClimbPoint` script.
    *   In the `GrabbableClimbPoint` inspector:
        *   `Grab Reference Point`: You can optionally assign a child `Transform` here if you want a very specific point *within* the `ClimbHandle` to be the exact grab pivot (e.g., the center of a small visual knob). If left empty, the `ClimbHandle`'s own transform will be used.

#### 5. Testing in Editor (without VR Headset)

1.  Ensure your `VRPlayer` Rigidbody has `Freeze Rotation` enabled on all axes.
2.  Run the scene.
3.  In the Unity editor's Scene view, select your `LeftHand` or `RightHand` GameObject.
4.  Use the Transform tool (W) to move the hand GameObject. Position it so its collider overlaps with a `ClimbHandle`'s trigger collider.
5.  While the hand is hovering, press the corresponding grab button (e.g., `left shift` for `GrabLeft`, or `right shift` for `GrabRight`). You should see debug messages in the console.
6.  While holding the grab button, move the hand GameObject away from the `ClimbHandle`. The `VRPlayer` root (and its camera) should move, allowing you to simulate climbing by "pulling" the world towards your hand.
7.  Release the grab button, and the player should transition to `Falling` state and be affected by gravity.

This comprehensive setup provides a functional and extensible VR climbing system, demonstrating the described design pattern in a practical Unity context.