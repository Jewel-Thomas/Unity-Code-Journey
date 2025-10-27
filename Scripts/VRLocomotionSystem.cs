// Unity Design Pattern Example: VRLocomotionSystem
// This script demonstrates the VRLocomotionSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This comprehensive C# Unity script demonstrates the **VR Locomotion System** design pattern, primarily leveraging the **Strategy Pattern**. It allows you to define multiple locomotion behaviors (smooth movement, teleportation, snap turning) and dynamically switch between them at runtime.

### Design Pattern Explanation: VR Locomotion System (Strategy Pattern)

The 'VR Locomotion System' described here is an application of the well-known **Strategy Design Pattern**.

**1. Context (`VRLocomotionSystem`):**
*   This is the main MonoBehaviour that sits on your VR player's root (e.g., XR Origin).
*   It holds references to the currently active locomotion strategies (e.g., one for movement, one for turning).
*   It does *not* implement the locomotion logic itself. Instead, it delegates the actual movement and turning tasks to its active strategies.
*   It provides a public API (`SetMovementStrategy`, `SetTurnStrategy`) to switch between different locomotion strategies at runtime.

**2. Strategy Interface (`ILocomotionStrategy`):**
*   This interface defines a common contract for all concrete locomotion algorithms.
*   It specifies methods that all locomotion strategies must implement:
    *   `Initialize()`: To provide the strategy with necessary Unity components (XR Origin, Character Controller, etc.).
    *   `Activate()`: Called when a strategy becomes the active one. Useful for enabling input listeners, visual cues (like teleport arcs), etc.
    *   `Deactivate()`: Called when a strategy is replaced or turned off. Useful for cleaning up, hiding visuals, etc.
    *   `UpdateStrategy()`: Contains the core, frame-by-frame locomotion logic for that specific strategy.

**3. Concrete Strategies (`SmoothLocomotionStrategy`, `TeleportLocomotionStrategy`, `SnapTurnStrategy`):**
*   These are classes that implement the `ILocomotionStrategy` interface.
*   Each class encapsulates a specific locomotion algorithm:
    *   **`SmoothLocomotionStrategy`**: Handles continuous, joystick-based movement.
    *   **`TeleportLocomotionStrategy`**: Manages point-and-click teleportation.
    *   **`SnapTurnStrategy`**: Implements discrete, rotational turning.
*   They don't know about `VRLocomotionSystem`, only the `ILocomotionStrategy` interface.

**Advantages of this pattern for VR Locomotion:**
*   **Flexibility**: Easily swap different locomotion methods at runtime based on user preference, game state, or accessibility needs.
*   **Modularity**: Each locomotion method is self-contained in its own class, making the code cleaner, easier to understand, test, and debug.
*   **Extensibility**: Adding new locomotion strategies (e.g., dash, climbing, flying) simply involves creating a new class that implements `ILocomotionStrategy` and registering it with the `VRLocomotionSystem`, without modifying existing code.
*   **Decoupling**: The `VRLocomotionSystem` (context) is decoupled from the specific implementation details of any locomotion method. It only cares that the method conforms to the `ILocomotionStrategy` interface.

---

### `VRLocomotionSystem.cs`

To use this script:
1.  **Copy the entire code block** below into a new C# script file named `VRLocomotionSystem.cs` in your Unity project.
2.  Follow the **"Example Unity Scene Setup"** instructions provided in the comments at the end of the script to configure your scene and the `VRLocomotionSystem` component.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For potential strategy management
using UnityEngine.XR.Management; // To remind about XR setup (optional, not used in script logic)

// Important Note for Setup:
// This script assumes you have a basic Unity XR Project set up.
// Specifically, it expects:
// 1. An 'XR Origin' GameObject in your scene (from XR Interaction Toolkit or similar).
// 2. A 'Main Camera' child of the XR Origin, representing the player's head.
// 3. A 'Character Controller' component attached to the XR Origin (or its parent).
// 4. Input Axes configured in 'Edit -> Project Settings -> Input Manager' for:
//    - "LeftThumbstickX", "LeftThumbstickY" (for smooth movement)
//    - "RightThumbstickX" (for snap turn)
//    - "Fire1" (for teleport activation, usually a trigger button)

// --- VR Locomotion System Design Pattern ---
//
// This example demonstrates the Strategy Design Pattern applied to a VR Locomotion System.
//
// Pattern Components:
// 1.  Context: The 'VRLocomotionSystem' class. This is the main MonoBehaviour that
//     manages and orchestrates different locomotion behaviors. It holds a reference
//     to the currently active locomotion strategy and delegates locomotion tasks to it.
//     It also provides methods to switch between strategies.
//
// 2.  Strategy Interface: The 'ILocomotionStrategy' interface. This defines a common
//     interface for all concrete locomotion algorithms. It ensures that any locomotion
//     method can be used interchangeably by the context.
//     - Initialize(): Called once to provide necessary references (XR Origin, etc.).
//     - Activate(): Called when a strategy becomes the active one. Useful for setting up
//                   input listeners, visual aids (e.g., teleport arc), etc.
//     - Deactivate(): Called when a strategy is replaced. Useful for cleaning up.
//     - UpdateStrategy(): The core locomotion logic, called every frame by the context.
//
// 3.  Concrete Strategies: Classes that implement the 'ILocomotionStrategy' interface.
//     Each concrete strategy provides a specific locomotion algorithm.
//     - 'SmoothLocomotionStrategy': Implements continuous, smooth movement.
//     - 'TeleportLocomotionStrategy': Implements point-and-click teleportation.
//     - 'SnapTurnStrategy': Implements discrete, rotational turning.
//
// Advantages of this pattern:
// -   Flexibility: Easily swap different locomotion methods at runtime.
// -   Modularity: Each locomotion method is encapsulated in its own class, making
//     code easier to understand, test, and maintain.
// -   Extensibility: New locomotion strategies can be added without modifying the
//     core 'VRLocomotionSystem' context.
// -   Decoupling: The 'VRLocomotionSystem' doesn't need to know the specifics of
//     how each locomotion method works, only that it conforms to the 'ILocomotionStrategy'
//     interface.

/// <summary>
/// Interface for all VR locomotion strategies.
/// Defines the contract for different movement and turning behaviors.
/// </summary>
public interface ILocomotionStrategy
{
    /// <summary>
    /// Initializes the strategy with necessary Unity components.
    /// This is called once when the VRLocomotionSystem awakens.
    /// </summary>
    /// <param name="xrOrigin">The root GameObject of the VR player (e.g., XR Origin).</param>
    /// <param name="playerHead">The GameObject representing the player's head (camera).</param>
    /// <param name="characterController">The CharacterController component for collision-based movement.</param>
    /// <param name="debugLineRenderer">Optional LineRenderer for debug visualizations (e.g., teleport arc).</param>
    void Initialize(GameObject xrOrigin, GameObject playerHead, CharacterController characterController, LineRenderer debugLineRenderer);

    /// <summary>
    /// Called when this strategy becomes the active locomotion method.
    /// Use this to enable input listeners, visual cues, etc.
    /// </summary>
    void Activate();

    /// <summary>
    /// Called when this strategy is no longer the active locomotion method.
    /// Use this to disable input listeners, hide visual cues, clean up resources, etc.
    /// </summary>
    void Deactivate();

    /// <summary>
    /// Contains the core logic for the locomotion strategy, called every frame.
    /// </summary>
    void UpdateStrategy();
}

/// <summary>
/// The Context class for the VR Locomotion System.
/// This MonoBehaviour manages and delegates to different locomotion strategies.
/// It acts as the central hub for handling VR player movement and turning.
/// </summary>
[DisallowMultipleComponent] // Ensures only one locomotion system exists on the XR Origin.
public class VRLocomotionSystem : MonoBehaviour
{
    [Header("VR Rig References")]
    [Tooltip("The root GameObject of the XR rig (e.g., 'XR Origin').")]
    [SerializeField] private GameObject xrOrigin;
    [Tooltip("The GameObject representing the player's head (usually the Main Camera).")]
    [SerializeField] private GameObject playerHead;
    [Tooltip("The CharacterController component attached to the XR Origin for collision.")]
    [SerializeField] private CharacterController characterController;

    [Header("Debug & Visualization")]
    [Tooltip("Optional Line Renderer for visualizing things like teleport arcs.")]
    [SerializeField] private LineRenderer debugLineRenderer;

    [Header("Default Strategies")]
    [Tooltip("The default movement strategy to use on Awake.")]
    [SerializeField] private LocomotionType defaultMovementStrategy = LocomotionType.Smooth;
    [Tooltip("The default turn strategy to use on Awake.")]
    [SerializeField] private LocomotionType defaultTurnStrategy = LocomotionType.SnapTurn;

    // --- Private Members ---
    private ILocomotionStrategy _currentMovementStrategy;
    private ILocomotionStrategy _currentTurnStrategy;

    // A dictionary to hold instances of all potential strategies.
    // This prevents re-instantiating them and allows for easy switching.
    private Dictionary<LocomotionType, ILocomotionStrategy> _availableStrategies = new Dictionary<LocomotionType, ILocomotionStrategy>();

    /// <summary>
    /// Defines the different types of locomotion available.
    /// </summary>
    public enum LocomotionType
    {
        None,       // No movement or turn strategy active
        Smooth,     // Continuous joystick movement
        Teleport,   // Point-and-click teleportation
        SnapTurn    // Discrete rotational turning
    }

    void Awake()
    {
        // --- 1. Validate required references ---
        if (xrOrigin == null) { Debug.LogError("VRLocomotionSystem: XR Origin not assigned! Please assign it in the Inspector.", this); enabled = false; return; }
        if (playerHead == null) { Debug.LogError("VRLocomotionSystem: Player Head (Camera) not assigned! Please assign it in the Inspector.", this); enabled = false; return; }
        if (characterController == null) { Debug.LogError("VRLocomotionSystem: Character Controller not assigned! Please add a CharacterController to your XR Origin and assign it.", this); enabled = false; return; }
        if (debugLineRenderer == null) { Debug.LogWarning("VRLocomotionSystem: Debug Line Renderer not assigned. Teleport arc will not be visible.", this); }


        // --- 2. Initialize all concrete strategies ---
        // Create an instance of each strategy and store it in the dictionary.
        // This ensures they are ready to be activated when needed and prevents re-instantiation.
        _availableStrategies.Add(LocomotionType.Smooth, new SmoothLocomotionStrategy());
        _availableStrategies.Add(LocomotionType.Teleport, new TeleportLocomotionStrategy());
        _availableStrategies.Add(LocomotionType.SnapTurn, new SnapTurnStrategy());

        // --- 3. Pass essential components to each strategy ---
        // Initialize each strategy once with the necessary components (XR Origin, CharacterController, etc.).
        foreach (var entry in _availableStrategies)
        {
            entry.Value.Initialize(xrOrigin, playerHead, characterController, debugLineRenderer);
        }

        // --- 4. Set initial strategies based on Inspector defaults ---
        SetMovementStrategy(defaultMovementStrategy);
        SetTurnStrategy(defaultTurnStrategy);
    }

    void Update()
    {
        // --- 5. Delegate locomotion updates to the currently active strategies ---
        // The VRLocomotionSystem (Context) simply calls UpdateStrategy() on its current strategies,
        // without knowing their internal implementation details.
        _currentMovementStrategy?.UpdateStrategy();
        _currentTurnStrategy?.UpdateStrategy();

        // --- Example: Dynamically switch strategies at runtime (for testing) ---
        // This demonstrates the flexibility of the Strategy pattern.
        if (Input.GetKeyDown(KeyCode.M)) // Press 'M' to toggle movement strategies
        {
            if (_currentMovementStrategy is SmoothLocomotionStrategy)
            {
                SetMovementStrategy(LocomotionType.Teleport);
                Debug.Log("Switched to Teleport Locomotion!");
            }
            else if (_currentMovementStrategy is TeleportLocomotionStrategy)
            {
                SetMovementStrategy(LocomotionType.Smooth);
                Debug.Log("Switched to Smooth Locomotion!");
            }
            else // If no strategy or an unknown one is active, default to Smooth
            {
                SetMovementStrategy(LocomotionType.Smooth);
                Debug.Log("Switched to Smooth Locomotion (default fallback)!");
            }
        }
        // Example: Dynamically switch turn strategy based on input
        if (Input.GetKeyDown(KeyCode.T)) // Press 'T' to toggle turn strategies
        {
            if (_currentTurnStrategy is SnapTurnStrategy)
            {
                SetTurnStrategy(LocomotionType.None); // Example: No turn strategy (free-turning only)
                Debug.Log("Switched to No Turn Strategy!");
            }
            else
            {
                SetTurnStrategy(LocomotionType.SnapTurn);
                Debug.Log("Switched to Snap Turn Strategy!");
            }
        }
    }

    /// <summary>
    /// Sets the active movement locomotion strategy for the VR player.
    /// Deactivates the old strategy and activates the new one.
    /// </summary>
    /// <param name="newStrategyType">The type of locomotion strategy to activate.</param>
    public void SetMovementStrategy(LocomotionType newStrategyType)
    {
        // If the new strategy type is 'None', we simply deactivate the current one.
        if (newStrategyType == LocomotionType.None)
        {
            if (_currentMovementStrategy != null)
            {
                _currentMovementStrategy.Deactivate();
                _currentMovementStrategy = null;
            }
            Debug.Log($"VRLocomotionSystem: Movement strategy set to None.");
            return;
        }

        // Ensure the requested strategy type exists in our dictionary.
        if (!_availableStrategies.ContainsKey(newStrategyType))
        {
            Debug.LogWarning($"VRLocomotionSystem: Attempted to set unknown movement strategy: {newStrategyType}. No change made.");
            return;
        }

        // Prevent redundant switching if the new strategy is already active.
        if (_currentMovementStrategy == _availableStrategies[newStrategyType])
        {
            Debug.Log($"VRLocomotionSystem: Movement strategy is already {newStrategyType}.");
            return;
        }

        // Deactivate the old strategy before activating the new one.
        _currentMovementStrategy?.Deactivate();

        // Set and activate the new strategy.
        _currentMovementStrategy = _availableStrategies[newStrategyType];
        _currentMovementStrategy.Activate();
        Debug.Log($"VRLocomotionSystem: Movement strategy switched to: {newStrategyType}");
    }

    /// <summary>
    /// Sets the active turn locomotion strategy for the VR player.
    /// Deactivates the old strategy and activates the new one.
    /// </summary>
    /// <param name="newStrategyType">The type of locomotion strategy to activate.</param>
    public void SetTurnStrategy(LocomotionType newStrategyType)
    {
        // If the new strategy type is 'None', we simply deactivate the current one.
        if (newStrategyType == LocomotionType.None)
        {
            if (_currentTurnStrategy != null)
            {
                _currentTurnStrategy.Deactivate();
                _currentTurnStrategy = null;
            }
            Debug.Log($"VRLocomotionSystem: Turn strategy set to None.");
            return;
        }

        // Ensure the requested strategy type exists in our dictionary.
        if (!_availableStrategies.ContainsKey(newStrategyType))
        {
            Debug.LogWarning($"VRLocomotionSystem: Attempted to set unknown turn strategy: {newStrategyType}. No change made.");
            return;
        }
        
        // Prevent redundant switching if the new strategy is already active.
        if (_currentTurnStrategy == _availableStrategies[newStrategyType])
        {
            Debug.Log($"VRLocomotionSystem: Turn strategy is already {newStrategyType}.");
            return;
        }

        // Deactivate the old strategy before activating the new one.
        _currentTurnStrategy?.Deactivate();

        // Set and activate the new strategy.
        _currentTurnStrategy = _availableStrategies[newStrategyType];
        _currentTurnStrategy.Activate();
        Debug.Log($"VRLocomotionSystem: Turn strategy switched to: {newStrategyType}");
    }

    // --- Concrete Strategy Implementations ---
    // These classes implement the ILocomotionStrategy interface, each providing a
    // distinct locomotion behavior. They are nested here for a single-file example,
    // but in larger projects, they might reside in separate files.

    /// <summary>
    /// Implements smooth, continuous movement for VR using joystick input and a CharacterController.
    /// </summary>
    private class SmoothLocomotionStrategy : ILocomotionStrategy
    {
        private GameObject _xrOrigin; // Reference to the XR Origin (for movement)
        private GameObject _playerHead; // Reference to the player's head/camera (for direction)
        private CharacterController _characterController; // For collision-based movement

        // --- Settings (could be passed from VRLocomotionSystem for configurability) ---
        private float _moveSpeed = 2.5f;
        private float _gravity = -9.8f;
        private Vector3 _currentVerticalVelocity; // Tracks gravity effect

        // --- Input configuration (using old Unity Input Manager for simplicity) ---
        private const string _moveXAxis = "LeftThumbstickX"; // Configure in Project Settings -> Input Manager
        private const string _moveYAxis = "LeftThumbstickY"; // Configure in Project Settings -> Input Manager

        public void Initialize(GameObject xrOrigin, GameObject playerHead, CharacterController characterController, LineRenderer debugLineRenderer)
        {
            _xrOrigin = xrOrigin;
            _playerHead = playerHead;
            _characterController = characterController;
            // debugLineRenderer is not used by this strategy, but passed for consistency.
        }

        public void Activate()
        {
            Debug.Log("SmoothLocomotionStrategy Activated.");
            _currentVerticalVelocity = Vector3.zero; // Reset vertical velocity on activate
        }

        public void Deactivate()
        {
            Debug.Log("SmoothLocomotionStrategy Deactivated.");
        }

        public void UpdateStrategy()
        {
            // --- 1. Apply Gravity ---
            // If grounded, reset vertical velocity to a small downward force.
            if (_characterController.isGrounded && _currentVerticalVelocity.y < 0)
            {
                _currentVerticalVelocity.y = -2f; 
            }
            // Apply gravity over time.
            _currentVerticalVelocity.y += _gravity * Time.deltaTime;

            // --- 2. Get Horizontal Movement Input ---
            float moveInputX = Input.GetAxis(_moveXAxis);
            float moveInputY = Input.GetAxis(_moveYAxis);

            // Calculate movement direction relative to the player's head (camera).
            // This ensures movement is intuitive (forward relative to where player is looking).
            // We ignore the Y-axis rotation of the head to prevent unintended "flying" or "crouching"
            // based on head tilt, making movement strictly horizontal.
            Vector3 forward = _playerHead.transform.forward;
            forward.y = 0; // Flatten the vector
            forward.Normalize(); // Ensure unit length

            Vector3 right = _playerHead.transform.right;
            right.y = 0; // Flatten the vector
            right.Normalize(); // Ensure unit length

            // Combine forward and right inputs to get the desired horizontal move direction.
            Vector3 horizontalMoveDirection = forward * moveInputY + right * moveInputX;
            horizontalMoveDirection.Normalize(); // Normalize to prevent faster diagonal movement.

            // --- 3. Apply Movement ---
            // Combine horizontal movement with vertical (gravity) velocity.
            Vector3 totalMovement = (horizontalMoveDirection * _moveSpeed) + _currentVerticalVelocity;
            _characterController.Move(totalMovement * Time.deltaTime);
        }
    }

    /// <summary>
    /// Implements teleportation locomotion for VR.
    /// This uses a simple raycast from the player's head to determine teleport target.
    /// </summary>
    private class TeleportLocomotionStrategy : ILocomotionStrategy
    {
        private GameObject _xrOrigin; // Reference to the XR Origin (to move the player)
        private GameObject _playerHead; // Reference to the player's head/camera (for raycast origin)
        private LineRenderer _debugLineRenderer; // For showing the teleport arc/line

        // --- Settings (could be passed from VRLocomotionSystem for configurability) ---
        private float _teleportRayLength = 20f; // Max distance for teleport raycast
        private Color _validTeleportColor = Color.green; // Color for valid target
        private Color _invalidTeleportColor = Color.red; // Color for invalid target
        private LayerMask _teleportLayerMask = ~0; // Layers to consider for teleport (all layers by default)

        // --- Input configuration (using old Unity Input Manager for simplicity) ---
        private const string _teleportButton = "Fire1"; // Example: Right Controller Trigger (configure in Input Manager)

        private Vector3 _teleportTargetPosition; // The calculated position to teleport to
        private bool _isValidTeleportTarget; // True if current raycast hits a valid surface
        private bool _isTeleporting; // Flag to prevent input during a teleport fade (simulated here)

        public void Initialize(GameObject xrOrigin, GameObject playerHead, CharacterController characterController, LineRenderer debugLineRenderer)
        {
            _xrOrigin = xrOrigin;
            _playerHead = playerHead;
            _debugLineRenderer = debugLineRenderer; // CharacterController not directly used by teleport, but useful for other checks.

            // Configure the LineRenderer for a basic teleport arc/line.
            if (_debugLineRenderer != null)
            {
                _debugLineRenderer.positionCount = 2; // Simple line from head to target
                _debugLineRenderer.startWidth = 0.05f;
                _debugLineRenderer.endWidth = 0.01f;
                _debugLineRenderer.useWorldSpace = true;
                _debugLineRenderer.enabled = false; // Start disabled
                // Ensure a material is assigned to the LineRenderer in the Inspector.
            }
        }

        public void Activate()
        {
            Debug.Log("TeleportLocomotionStrategy Activated.");
            if (_debugLineRenderer != null) _debugLineRenderer.enabled = true; // Show arc when active
            _isValidTeleportTarget = false;
            _isTeleporting = false;
        }

        public void Deactivate()
        {
            Debug.Log("TeleportLocomotionStrategy Deactivated.");
            if (_debugLineRenderer != null) _debugLineRenderer.enabled = false; // Hide arc when deactivated
        }

        public void UpdateStrategy()
        {
            if (_isTeleporting) return; // Ignore input if a teleport sequence is ongoing

            // --- 1. Detect Teleport Input (Button Held) ---
            if (Input.GetButton(_teleportButton))
            {
                RaycastHit hit;
                // Raycast from the player's head (camera) forward.
                // In a real VR app, this would typically originate from a hand controller.
                if (Physics.Raycast(_playerHead.transform.position, _playerHead.transform.forward, out hit, _teleportRayLength, _teleportLayerMask))
                {
                    // For a robust system, add checks here for valid teleport surfaces (e.g.,
                    // not too steep, not obstructed by objects, on a NavMesh).
                    _teleportTargetPosition = hit.point;
                    _isValidTeleportTarget = true;
                }
                else
                {
                    _isValidTeleportTarget = false;
                }
                // Draw the teleport visualization (line).
                DrawTeleportArc(_isValidTeleportTarget ? _teleportTargetPosition : _playerHead.transform.position + _playerHead.transform.forward * _teleportRayLength, _isValidTeleportTarget);
            }
            // --- 2. Detect Teleport Input (Button Released) ---
            else if (Input.GetButtonUp(_teleportButton))
            {
                if (_isValidTeleportTarget)
                {
                    // A proper VR teleport usually involves a screen fade-out, moving the player,
                    // and then fading back in for comfort.
                    // To achieve this here, the strategy would need a way to communicate back to the
                    // `VRLocomotionSystem` (which is a MonoBehaviour) to start a Coroutine for the fade effect.
                    // For simplicity in this example, we perform an instant teleport.
                    // Example for fade (would require a delegate/callback from VRLocomotionSystem):
                    // _isTeleporting = true;
                    // _teleportEffectDelegate?.Invoke(_teleportTargetPosition, _teleportFadeDuration);

                    DoTeleport(_teleportTargetPosition);
                    _isTeleporting = false; // Reset for next input cycle
                }
                HideTeleportArc(); // Hide the visual after release
            }
            // --- 3. No Teleport Input ---
            else
            {
                HideTeleportArc(); // Hide arc if button is not pressed
            }
        }

        /// <summary>
        /// Moves the XR Origin to the target position, accounting for player head offset.
        /// </summary>
        private void DoTeleport(Vector3 targetPosition)
        {
            // Calculate the horizontal offset from the XR Origin's pivot to the player's head.
            // This ensures that when we move the XR Origin, the *player's head* lands exactly
            // at the `targetPosition`, preventing them from being shifted off-center from the target.
            Vector3 headOffset = _playerHead.transform.position - _xrOrigin.transform.position;
            headOffset.y = 0; // Only consider horizontal offset for ground placement.

            // Move the XR Origin (the root of the VR player) to the new position.
            _xrOrigin.transform.position = targetPosition - headOffset;
            Debug.Log($"Teleported to: {targetPosition}");
        }

        /// <summary>
        /// Draws a simple line from the player's head to the target point.
        /// </summary>
        private void DrawTeleportArc(Vector3 endPoint, bool isValid)
        {
            if (_debugLineRenderer == null) return;

            _debugLineRenderer.enabled = true;
            _debugLineRenderer.startColor = isValid ? _validTeleportColor : _invalidTeleportColor;
            _debugLineRenderer.endColor = isValid ? _validTeleportColor : _invalidTeleportColor;

            _debugLineRenderer.SetPosition(0, _playerHead.transform.position);
            _debugLineRenderer.SetPosition(1, endPoint);
        }

        /// <summary>
        /// Hides the teleport arc.
        /// </summary>
        private void HideTeleportArc()
        {
            if (_debugLineRenderer != null)
            {
                _debugLineRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// Implements snap turning for VR, rotating the player by discrete angles.
    /// </summary>
    private class SnapTurnStrategy : ILocomotionStrategy
    {
        private GameObject _xrOrigin; // Reference to the XR Origin (to rotate the player)

        // --- Settings (could be passed from VRLocomotionSystem for configurability) ---
        private float _snapAngle = 30f; // Degrees to turn per snap (e.g., 30 degrees)
        private float _snapThreshold = 0.7f; // Joystick input threshold to trigger a snap (0.0 to 1.0)
        private float _cooldownTime = 0.3f; // Time in seconds between snaps to prevent rapid turning

        // --- Input configuration (using old Unity Input Manager for simplicity) ---
        private const string _turnXAxis = "RightThumbstickX"; // Configure in Project Settings -> Input Manager

        private float _lastTurnTime = 0f; // Timer to manage cooldown

        public void Initialize(GameObject xrOrigin, GameObject playerHead, CharacterController characterController, LineRenderer debugLineRenderer)
        {
            _xrOrigin = xrOrigin;
            // playerHead, characterController, debugLineRenderer are not used by this strategy, but passed for consistency.
        }

        public void Activate()
        {
            Debug.Log("SnapTurnStrategy Activated.");
            _lastTurnTime = Time.time; // Reset cooldown when activated
        }

        public void Deactivate()
        {
            Debug.Log("SnapTurnStrategy Deactivated.");
        }

        public void UpdateStrategy()
        {
            // Only allow turning if the cooldown has passed.
            if (Time.time < _lastTurnTime + _cooldownTime)
            {
                return;
            }

            float turnInput = Input.GetAxis(_turnXAxis);

            // Check for right snap turn.
            if (turnInput > _snapThreshold) 
            {
                _xrOrigin.transform.Rotate(Vector3.up, _snapAngle); // Rotate XR Origin around its Y-axis.
                _lastTurnTime = Time.time; // Reset cooldown timer.
                Debug.Log($"Snap turned right by {_snapAngle} degrees.");
            }
            // Check for left snap turn.
            else if (turnInput < -_snapThreshold) 
            {
                _xrOrigin.transform.Rotate(Vector3.up, -_snapAngle); // Rotate XR Origin around its Y-axis.
                _lastTurnTime = Time.time; // Reset cooldown timer.
                Debug.Log($"Snap turned left by {_snapAngle} degrees.");
            }
        }
    }
}

/*
/// --- Example Unity Scene Setup ---
///
/// This section describes how to set up your Unity scene to use the VRLocomotionSystem script.
///
/// 1.  Create an Empty GameObject in your scene, name it "XR Origin".
///     -   This GameObject will be the root of your VR player rig.
///     -   Add a 'Character Controller' component to "XR Origin".
///         -   Set its `Center` to (0, 0.9, 0) and `Height` to 1.8 (approx player height).
///         -   (Optional) Adjust Radius (e.g., 0.25).
///     -   Add a 'Line Renderer' component to "XR Origin".
///         -   This will be used for visualizing teleport arcs.
///         -   Configure its `Material` (e.g., Default-Line, or a custom unlit material).
///         -   Adjust `Width` (e.g., Start: 0.05, End: 0.01) and `Colors` as desired.
///
/// 2.  Create a Camera as a child of "XR Origin", name it "Main Camera".
///     -   Position it at (0, 0, 0) relative to XR Origin.
///     -   Ensure "Main Camera" has the 'MainCamera' tag (Unity's default camera tag).
///     -   (Optional, but common for XR Interaction Toolkit): Add an 'XR Camera' component.
///
/// 3.  (Optional but good practice): Create an Empty GameObject as a child of "Main Camera",
///     name it "Player Head Position".
///     -   Position it at (0, 0, 0) relative to Main Camera.
///     -   This acts as a clear reference for the player's head, which the locomotion
///         system uses for direction and teleport raycasting. If not used, you can
///         directly reference "Main Camera" for the 'Player Head' field in the script.
///
/// 4.  Attach the 'VRLocomotionSystem.cs' script to the "XR Origin" GameObject.
///
/// 5.  In the Inspector for "XR Origin" (with VRLocomotionSystem component selected):
///     -   **XR Origin**: Drag the "XR Origin" GameObject itself to this field.
///     -   **Player Head**: Drag the "Main Camera" GameObject (or "Player Head Position" if you created it) to this field.
///     -   **Character Controller**: Drag the 'Character Controller' component from "XR Origin" to this field.
///     -   **Debug Line Renderer**: Drag the 'Line Renderer' component from "XR Origin" to this field.
///     -   **Default Movement Strategy**: Set this to 'Smooth' (or 'Teleport').
///     -   **Default Turn Strategy**: Set this to 'SnapTurn' (or 'None' for free turning).
///
/// 6.  Ensure your Unity project has XR enabled.
///     -   Go to 'Edit -> Project Settings -> XR Plugin Management'.
///     -   Install appropriate XR Plug-in Providers (e.g., OpenXR for most headsets).
///     -   Enable it for the platforms you are targeting (e.g., Standalone, Android).
///
/// 7.  (Crucial for old Input Manager): Configure Input Axes:
///     -   Go to 'Edit -> Project Settings -> Input Manager'.
///     -   Expand 'Axes'. You'll need to ensure entries exist or create new ones for:
///         -   **"LeftThumbstickX" and "LeftThumbstickY"**: For smooth movement.
///             -   Example for Oculus Quest Left Controller:
///                 -   Name: LeftThumbstickX, Type: Joystick Axis, Axis: 4th Axis (Joy 1), Joystick: Joystick 1
///                 -   Name: LeftThumbstickY, Type: Joystick Axis, Axis: 5th Axis (Joy 1), Joystick: Joystick 1
///         -   **"RightThumbstickX"**: For snap turn.
///             -   Example for Oculus Quest Right Controller:
///                 -   Name: RightThumbstickX, Type: Joystick Axis, Axis: 3rd Axis (Joy 1), Joystick: Joystick 1
///         -   **"Fire1"**: For teleport trigger.
///             -   This often maps to the primary trigger on the right controller.
///             -   Example for Oculus Quest Right Trigger:
///                 -   Name: Fire1, Type: Joystick Button, Joystick Button: Joystick 1 Button 14 (or similar for your controller)
///             -   Alternatively, for PC testing without VR, you can set "Fire1" to "Mouse 0" (Left Click).
///     -   **NOTE**: For a modern Unity VR project, the `Unity Input System` package and `XR Interaction Toolkit`
///         are the recommended approach. This example uses the legacy `Input` for simplicity in a single-file
///         drop-in solution without requiring specific Input System asset setup. To adapt to Input System:
///         -   Replace `Input.GetAxis("AxisName")` with `inputAction.ReadValue<Vector2>().x/y` or similar.
///         -   Replace `Input.GetButton("ButtonName")` with `inputAction.IsPressed()` or `inputAction.WasPressedThisFrame()`.
///         -   InputActions would be defined in an Input Action Asset and referenced in your strategies.
///
/// 8.  Create a simple scene with a ground plane and some objects (e.g., cubes) to test movement and teleportation.
///
/// 9.  Run the scene!
///     -   You should be able to move with the left thumbstick (Smooth Locomotion).
///     -   You should be able to snap turn with the right thumbstick (Snap Turn).
///     -   Pressing "Fire1" (e.g., Right Trigger or Mouse Left Click for desktop test) will show a teleport arc/line.
///         Releasing the button will teleport you to the target.
///     -   Press the **'M' key** on your keyboard to toggle between Smooth and Teleport movement.
///     -   Press the **'T' key** on your keyboard to toggle Snap Turn on/off.
///
/// This setup provides a functional VR locomotion system using the Strategy pattern,
/// allowing easy swapping and extension of locomotion behaviors.
*/
```