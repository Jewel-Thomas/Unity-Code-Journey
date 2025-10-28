// Unity Design Pattern Example: WallRunSystem
// This script demonstrates the WallRunSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **'WallRunSystem' design pattern** in Unity, which refers to creating a dedicated, modular system to manage a specific game mechanic – in this case, wall running. This approach promotes the Single Responsibility Principle, making your code cleaner, more maintainable, and easier to extend.

The `WallRunSystem` script handles all aspects of wall detection, initiating the wall run, managing movement and gravity during the run, and handling a wall jump. Other player movement systems can then integrate with it by querying its state (`IsWallRunning()`) and deferring control when necessary.

---

```csharp
using UnityEngine;
using System.Collections; // Included for completeness, though not strictly required for this specific script.

// Enum to define which side the player is currently wall running on.
// This helps to differentiate behavior or visual feedback based on the wall's location.
public enum WallRunSide
{
    None,  // Not wall running
    Left,  // Wall running on the left side
    Right  // Wall running on the right side
}

/// <summary>
/// WallRunSystem: A practical C# Unity example demonstrating a modular Wall Run mechanic.
/// This system encapsulates all logic related to wall running, adhering to the Single Responsibility Principle.
/// It works by detecting walls, applying custom gravity and forces, and handling wall-specific jumps.
/// Other player movement scripts can query its state and integrate with it, ensuring smooth transitions.
/// </summary>
[RequireComponent(typeof(CharacterController))] // Ensures the player GameObject has a CharacterController.
public class WallRunSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CharacterController component attached to this GameObject. Used for player movement.")]
    [SerializeField] private CharacterController characterController;
    [Tooltip("The main camera transform. Used for determining forward movement relative to player view and raycast directions.")]
    [SerializeField] private Transform playerCameraTransform; // Assign Main Camera's transform in Inspector

    [Header("Wall Run Settings")]
    [Tooltip("The speed at which the player moves along the wall.")]
    [SerializeField] private float wallRunSpeed = 7f;
    [Tooltip("The force applied when the player jumps off the wall (magnitude).")]
    [SerializeField] private float wallRunJumpForce = 12f;
    [Tooltip("The upward force applied immediately when starting a wall run to prevent falling instantly.")]
    [SerializeField] private float wallRunVerticalStartBoost = 3f;
    [Tooltip("Multiplier for gravity during wall run. A value < 1 reduces gravity, 0 removes it.")]
    [SerializeField] private float wallRunGravityMultiplier = 0.2f;
    [Tooltip("How long the player can wall run before automatically detaching (set to 0 for infinite).")]
    [SerializeField] private float wallRunMaxDuration = 3f;
    [Tooltip("A small force that continuously pushes the player towards the wall to ensure they stick.")]
    [SerializeField] private float wallStickForce = 5f;

    [Header("Detection Settings")]
    [Tooltip("Distance for raycasts to detect walls to the left and right of the player.")]
    [SerializeField] private float wallCheckDistance = 0.8f;
    [Tooltip("Minimum height from the ground the player must be to initiate a wall run. Prevents wall running on ground.")]
    [SerializeField] private float minWallRunHeight = 1.5f;
    [Tooltip("Maximum angle (in degrees) of a surface's normal from Vector3.up to be considered a valid wall.")]
    [SerializeField] private float maxWallAngle = 70f;
    [Tooltip("LayerMask for what objects are considered walls for wall running (e.g., set your walls to a 'Wall' layer).")]
    [SerializeField] private LayerMask wallRunLayer;

    // Internal State Variables
    private WallRunSide currentWallRunSide = WallRunSide.None; // Tracks which side the wall run is happening on.
    private bool isWallRunning = false;                       // True if the player is currently wall running.
    private float wallRunTimer;                               // Counts down the remaining wall run duration.
    private Vector3 wallNormal;                               // The normal vector of the detected wall surface.
    private Vector3 wallForward;                              // The calculated forward direction along the wall.
    private Vector3 playerVelocity;                           // The current velocity applied by this system.

    // Cached Input Values (assumes standard Unity input setup: "Horizontal", "Vertical", "Jump")
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;

    /// <summary>
    /// Public property to check if the player is currently wall running.
    /// This is crucial for other movement systems (e.g., PlayerMovement) to adapt their behavior.
    /// </summary>
    public bool IsWallRunning() => isWallRunning;

    /// <summary>
    /// Public property to get the current side of the wall run (Left, Right, or None).
    /// </summary>
    public WallRunSide GetCurrentWallRunSide() => currentWallRunSide;

    private void Awake()
    {
        // Ensure CharacterController is assigned, or try to get it.
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
        // Ensure camera transform is assigned, or try to find the Main Camera.
        if (playerCameraTransform == null)
        {
            Debug.LogWarning("WallRunSystem: Player Camera Transform not assigned. Attempting to find Main Camera.");
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCameraTransform = mainCam.transform;
            }
            else
            {
                Debug.LogError("WallRunSystem: No Main Camera found! Wall run forward direction will not work correctly without a camera reference.");
            }
        }
    }

    private void Update()
    {
        // --- Input Collection ---
        // Collect input every frame. In a larger project, this might come from a dedicated Input Manager.
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetButtonDown("Jump");

        // --- Main Wall Run Logic Flow ---
        if (isWallRunning)
        {
            HandleWallRunning(); // If already wall running, manage the run.
        }
        else
        {
            DetectAndAttemptWallRun(); // If not wall running, look for walls to start a run.
        }

        // --- Gravity Application (for non-wall-run state in this self-contained example) ---
        // This part would typically be handled by a main PlayerMovement script.
        // For this self-contained example, we apply basic gravity when not wall running and not grounded.
        if (!isWallRunning && !characterController.isGrounded)
        {
            playerVelocity.y += Physics.gravity.y * Time.deltaTime;
        }
        // If not wall running and not moving, gradually reduce horizontal velocity to zero
        // (again, this is simplified for a self-contained example, a PlayerMovement script would manage this).
        if (!isWallRunning && Mathf.Approximately(horizontalInput, 0) && Mathf.Approximately(verticalInput, 0))
        {
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 5f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 5f);
        }

        // Apply the calculated velocity to the CharacterController.
        // CharacterController.Move expects a displacement vector, so multiply by Time.deltaTime.
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Handles the continuous logic when the player is actively wall running.
    /// This includes maintaining speed, applying reduced gravity, sticking to the wall,
    /// checking for termination conditions, and handling wall jumps.
    /// </summary>
    private void HandleWallRunning()
    {
        // 1. Check if the player is still near a wall and if conditions are met.
        if (!IsWallDetected(out RaycastHit hitLeft, out RaycastHit hitRight, out WallRunSide side, out Vector3 normal))
        {
            StopWallRun(); // No wall detected, stop wall running.
            return;
        }

        // 2. Ensure the player is still on the same side of the wall.
        if (side != currentWallRunSide)
        {
            StopWallRun(); // Switched sides or wall disappeared unexpectedly, stop.
            return;
        }

        // Update wall normal for accurate calculations.
        wallNormal = normal;

        // 3. Determine the forward direction along the wall based on the wall normal and player's orientation.
        // This calculates a vector parallel to the ground plane and along the wall.
        if (currentWallRunSide == WallRunSide.Right)
        {
            // For a right wall, the wall normal points right. Cross (Up, Normal) gives forward.
            wallForward = Vector3.Cross(Vector3.up, wallNormal);
        }
        else // WallRunSide.Left
        {
            // For a left wall, the wall normal points left. Cross (Normal, Up) gives forward.
            wallForward = Vector3.Cross(wallNormal, Vector3.up);
        }

        // 4. Check player input: Player must be pressing forward (or slightly into the wall) to continue wall running.
        // If not pressing forward, or if trying to move away from the wall, stop the wall run.
        // We only allow wall running if verticalInput is positive (moving forward relative to camera).
        if (verticalInput <= 0 || characterController.isGrounded)
        {
            StopWallRun();
            return;
        }

        // Optional: Check if player is actively trying to move away from the wall.
        // Vector from player camera's right/left, projected onto the horizontal plane.
        Vector3 playerHorizontalInputDir = playerCameraTransform.right * horizontalInput + playerCameraTransform.forward * verticalInput;
        playerHorizontalInputDir = Vector3.ProjectOnPlane(playerHorizontalInputDir, Vector3.up).normalized;

        // If dot product with negative wall normal is positive, player is pushing away from the wall.
        if (Vector3.Dot(playerHorizontalInputDir, -wallNormal) > 0.1f) // 0.1f tolerance
        {
            // Stop wall run if player is explicitly trying to move away from the wall.
            StopWallRun();
            return;
        }

        // 5. Apply Wall Run Movement and Forces
        // Set horizontal velocity to run along the wall.
        playerVelocity = wallForward * wallRunSpeed;

        // Apply reduced gravity for the wall run effect.
        playerVelocity.y += Physics.gravity.y * wallRunGravityMultiplier * Time.deltaTime;

        // Apply a continuous force pushing the player towards the wall to prevent them from falling off.
        playerVelocity += -wallNormal * wallStickForce;

        // 6. Handle Wall Run Duration (if maxDuration is set)
        if (wallRunMaxDuration > 0)
        {
            wallRunTimer -= Time.deltaTime;
            if (wallRunTimer <= 0)
            {
                StopWallRun(); // Wall run duration expired.
                return;
            }
        }

        // 7. Handle Wall Jump Input
        if (jumpInput)
        {
            WallJump(); // Perform a wall jump.
        }
    }

    /// <summary>
    /// Attempts to detect a wall and start a wall run if all conditions are met.
    /// Conditions include: player is in the air, pressing forward, and a valid wall is found.
    /// </summary>
    private void DetectAndAttemptWallRun()
    {
        // 1. Pre-conditions for starting a wall run:
        //    - Player must not be grounded.
        //    - Player must be pressing "forward" (verticalInput > 0).
        if (characterController.isGrounded || verticalInput <= 0)
        {
            return;
        }

        // 2. Check if player is high enough from the ground.
        // This prevents wall running immediately after jumping off the ground or on very low walls.
        if (Physics.Raycast(transform.position, Vector3.down, minWallRunHeight + 0.1f, wallRunLayer))
        {
            // Debug.DrawRay(transform.position, Vector3.down * (minWallRunHeight + 0.1f), Color.yellow, 0.1f);
            return; // Too close to ground to start a wall run.
        }

        // 3. Perform raycasts to detect walls on either side.
        if (IsWallDetected(out RaycastHit hitLeft, out RaycastHit hitRight, out WallRunSide side, out Vector3 normal))
        {
            // A valid wall has been found, so initiate the wall run.
            currentWallRunSide = side;
            wallNormal = normal;
            StartWallRun();
        }
    }

    /// <summary>
    /// Performs raycasts to detect walls on either side of the player's camera forward direction.
    /// Checks for valid wall angles and layers.
    /// </summary>
    /// <param name="hitLeft">Out parameter for RaycastHit info if a left wall is detected.</param>
    /// <param name="hitRight">Out parameter for RaycastHit info if a right wall is detected.</param>
    /// <param name="side">Out parameter for the detected WallRunSide (Left/Right).</param>
    /// <param name="normal">Out parameter for the normal vector of the detected wall.</param>
    /// <returns>True if a valid wall is detected within range, false otherwise.</returns>
    private bool IsWallDetected(out RaycastHit hitLeft, out RaycastHit hitRight, out WallRunSide side, out Vector3 normal)
    {
        // Initialize out parameters
        hitLeft = new RaycastHit();
        hitRight = new RaycastHit();
        side = WallRunSide.None;
        normal = Vector3.zero;

        // Raycast origins are slightly offset from the player's center for better detection.
        Vector3 rayOrigin = transform.position + characterController.center;

        // Calculate raycast directions relative to the player's camera/view.
        // This makes wall running intuitive based on where the player is looking.
        Vector3 rayDirectionLeft = -playerCameraTransform.right;
        Vector3 rayDirectionRight = playerCameraTransform.right;

        // Perform the raycasts.
        bool leftWall = Physics.Raycast(rayOrigin, rayDirectionLeft, out hitLeft, wallCheckDistance, wallRunLayer);
        bool rightWall = Physics.Raycast(rayOrigin, rayDirectionRight, out hitRight, wallCheckDistance, wallRunLayer);

        // Debug visualization of the raycasts.
        Debug.DrawRay(rayOrigin, rayDirectionLeft * wallCheckDistance, leftWall ? Color.green : Color.red);
        Debug.DrawRay(rayOrigin, rayDirectionRight * wallCheckDistance, rightWall ? Color.green : Color.red);

        // Process detection results. Prioritize one side if both are hit (e.g., in a narrow corner).
        // This example prioritizes left if both are hit, but a more complex system might pick the 'most prominent' wall.
        if (leftWall)
        {
            // Check wall angle: ensure it's a relatively vertical wall, not a floor or ceiling.
            if (Vector3.Angle(hitLeft.normal, Vector3.up) > maxWallAngle) return false;

            side = WallRunSide.Left;
            normal = hitLeft.normal;
            return true; // Wall detected on the left.
        }
        else if (rightWall)
        {
            // Check wall angle.
            if (Vector3.Angle(hitRight.normal, Vector3.up) > maxWallAngle) return false;

            side = WallRunSide.Right;
            normal = hitRight.normal;
            return true; // Wall detected on the right.
        }

        return false; // No valid wall detected.
    }

    /// <summary>
    /// Initiates a wall run, setting up initial conditions and state.
    /// This includes resetting the timer and applying an initial vertical boost.
    /// </summary>
    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunMaxDuration;

        // Apply an initial vertical boost to counteract gravity and give a 'lift' feeling.
        // We only modify the Y velocity, keeping horizontal velocity from previous state if any.
        playerVelocity = new Vector3(playerVelocity.x, wallRunVerticalStartBoost, playerVelocity.z);

        // Optional: Implement camera effects, sound effects, or character animations here.
        // For instance, slightly tilting the camera or playing a whoosh sound.
    }

    /// <summary>
    /// Terminates the wall run state, resetting variables and preparing for normal movement.
    /// This is called when duration expires, wall is lost, or player jumps off.
    /// </summary>
    private void StopWallRun()
    {
        if (!isWallRunning) return; // Only stop if currently running.

        isWallRunning = false;
        currentWallRunSide = WallRunSide.None;
        wallNormal = Vector3.zero;
        wallForward = Vector3.zero;

        // Reset vertical velocity if it was positive from the wall run, to prevent "floating".
        // A main PlayerMovement script would then take over gravity application.
        if (playerVelocity.y > 0)
        {
            playerVelocity.y = 0;
        }

        // Optional: Revert camera effects, stop sounds, or transition animations.
    }

    /// <summary>
    /// Handles the jump action specifically when wall running.
    /// Applies a force away from the wall normal and upwards, then stops the wall run.
    /// This method is public so it can be called by other scripts if needed (e.g., from a more complex input system).
    /// </summary>
    public void WallJump()
    {
        if (!isWallRunning) return; // Can only wall jump if currently wall running.

        // Calculate jump direction: away from the wall normal and upwards.
        // The multipliers can be adjusted for different jump feels.
        Vector3 jumpDirection = (wallNormal * wallRunJumpForce * 0.7f) + (Vector3.up * wallRunJumpForce * 1.0f);
        playerVelocity = jumpDirection; // Set the player's new velocity for the jump.

        StopWallRun(); // End the wall run after performing the jump.
    }

    // --- Example Usage Comments and Design Pattern Explanation ---
    /*
     * How to use the WallRunSystem in a Unity Project:
     *
     * 1. Create a Player GameObject:
     *    - Create an empty GameObject (e.g., 'GameObject -> Create Empty'), name it "Player".
     *    - Add a `CharacterController` component to it (select "Player", then 'Add Component -> Physics -> Character Controller').
     *    - Add this `WallRunSystem.cs` script to the "Player" GameObject ('Add Component -> Scripts -> Wall Run System').
     *
     * 2. Configure the WallRunSystem in the Inspector:
     *    - Drag your Main Camera (or its child that represents the player's view) into the 'Player Camera Transform' field.
     *    - Set the 'Wall Run Layer' to a LayerMask that includes your wall GameObjects (see step 5).
     *    - Adjust 'Wall Run Speed', 'Wall Run Jump Force', 'Wall Run Max Duration', 'Wall Check Distance', etc., to fine-tune the mechanic.
     *
     * 3. Create Walls in your Scene:
     *    - Create 3D Cube GameObjects (e.g., 'GameObject -> 3D Object -> Cube') and position them to act as walls.
     *
     * 4. Setup Layers for Walls:
     *    - Go to `Edit -> Project Settings -> Tags and Layers`.
     *    - Add a new Layer (e.g., at an empty slot, name it "Wall").
     *    - Select your wall GameObjects in the Hierarchy and, in the Inspector, change their Layer dropdown to "Wall".
     *    - Back on your "Player" GameObject, in the `WallRunSystem` component, set the 'Wall Run Layer' field to "Wall".
     *
     * 5. Integrate with your PlayerMovement script (RECOMMENDED for robust player control):
     *    - If you have a separate `PlayerMovement` script, it should integrate with `WallRunSystem`
     *      to avoid conflicting movement logic. The `WallRunSystem` takes over completely during a wall run.
     *
     *    Example `PlayerMovement.cs` Integration:
     *    ```csharp
     *    using UnityEngine;
     *
     *    [RequireComponent(typeof(CharacterController))]
     *    public class PlayerMovement : MonoBehaviour
     *    {
     *        [Header("References")]
     *        [SerializeField] private CharacterController characterController;
     *        [SerializeField] private WallRunSystem wallRunSystem; // <--- Assign this in the Inspector!
     *        [SerializeField] private Transform playerCameraTransform; // Your camera for input direction
     *
     *        [Header("Movement Settings")]
     *        [SerializeField] private float moveSpeed = 5f;
     *        [SerializeField] private float runSpeedMultiplier = 1.5f;
     *        [SerializeField] private float jumpForce = 10f;
     *        [SerializeField] private float gravity = -9.81f; // Standard Earth gravity
     *        [SerializeField] private float groundCheckDistance = 0.2f;
     *        [SerializeField] private LayerMask groundLayer; // Set this layer for your ground objects
     *
     *        private Vector3 currentVelocity;
     *        private bool isGrounded;
     *
     *        void Awake()
     *        {
     *            if (characterController == null) characterController = GetComponent<CharacterController>();
     *            // Crucial: Get a reference to the WallRunSystem
     *            if (wallRunSystem == null) wallRunSystem = GetComponent<WallRunSystem>();
     *            if (playerCameraTransform == null) playerCameraTransform = Camera.main.transform;
     *        }
     *
     *        void Update()
     *        {
     *            // Perform ground check
     *            // Physics.CheckSphere creates a sphere and returns true if it overlaps with anything on `groundLayer`.
     *            isGrounded = Physics.CheckSphere(transform.position - new Vector3(0, characterController.height / 2 - characterController.radius, 0),
     *                                             groundCheckDistance, groundLayer);
     *            if (isGrounded && currentVelocity.y < 0)
     *            {
     *                currentVelocity.y = -2f; // Small downward force to keep player firmly grounded
     *            }
     *
     *            // --- Wall Run System Integration POINT ---
     *            // This is the core of the 'System' pattern: check if the system is active.
     *            if (wallRunSystem.IsWallRunning())
     *            {
     *                // If WallRunSystem is active, it completely takes over the player's movement.
     *                // The PlayerMovement script should *not* apply its own horizontal movement,
     *                // vertical movement, or gravity during this time.
     *                // WallRunSystem will handle its own velocity.
     *
     *                // We let WallRunSystem handle jump input too.
     *                return; // IMPORTANT: Exit Update() early if wall running, let WallRunSystem manage everything.
     *            }
     *            // --- End Wall Run System Integration ---
     *
     *            // --- Normal Ground/Air Movement (only if NOT wall running) ---
     *            float horizontal = Input.GetAxis("Horizontal");
     *            float vertical = Input.GetAxis("Vertical");
     *
     *            // Calculate movement direction relative to the camera's forward.
     *            Vector3 moveDirection = playerCameraTransform.right * horizontal + playerCameraTransform.forward * vertical;
     *            moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized; // Flatten to ground plane
     *
     *            float currentSpeed = moveSpeed;
     *            if (Input.GetKey(KeyCode.LeftShift)) // Example run input (holding Left Shift)
     *            {
     *                currentSpeed *= runSpeedMultiplier;
     *            }
     *
     *            // Apply horizontal movement
     *            currentVelocity.x = moveDirection.x * currentSpeed;
     *            currentVelocity.z = moveDirection.z * currentSpeed;
     *
     *            // Jump logic
     *            if (Input.GetButtonDown("Jump") && isGrounded)
     *            {
     *                currentVelocity.y = jumpForce;
     *            }
     *
     *            // Apply gravity
     *            currentVelocity.y += gravity * Time.deltaTime;
     *
     *            // Apply the calculated movement using CharacterController.
     *            characterController.Move(currentVelocity * Time.deltaTime);
     *        }
     *    }
     *    ```
     *
     * Design Pattern Explanation: The 'WallRunSystem' Pattern
     * The 'WallRunSystem' here embodies the "System" design pattern common in game development,
     * particularly related to component-based architectures.
     *
     * 1.  Single Responsibility Principle (SRP): The `WallRunSystem` is solely responsible for wall running.
     *     It doesn't handle general player movement, input collection for non-wall-run actions, or health management.
     *     This keeps the code focused and manageable.
     *
     * 2.  Decoupling: It's decoupled from the main `PlayerMovement` script. The `PlayerMovement` doesn't
     *     need to know *how* wall running works internally; it only needs to know *if* the player is wall running
     *     (`IsWallRunning()`) and then hands off control. This allows both systems to be developed and modified
     *     independently.
     *
     * 3.  Composability: It's designed as a MonoBehaviour component that can be added to any GameObject with a
     *     `CharacterController`. This makes it reusable across different player characters or even for AI if needed.
     *     It "composes" functionality onto an existing entity.
     *
     * 4.  Maintainability and Extensibility: All wall run logic (detection, movement, exit conditions, jump) is
     *     centralized. If you want to change how wall running feels or add new features (e.g., wall run stamina,
     *     different wall run types), you only need to modify this one script.
     *
     * 5.  Clarity: The presence of `WallRunSystem` clearly signals what that component does, improving code readability
     *     and understanding for anyone working on the project.
     *
     * This 'System' pattern is a practical and educational way to structure complex game mechanics, making them robust and flexible.
     */
}
```