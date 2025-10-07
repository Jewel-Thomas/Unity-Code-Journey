// Unity Design Pattern Example: InputLatencyCompensation
// This script demonstrates the InputLatencyCompensation pattern in Unity
// Generated automatically - ready to use in your Unity project

The Input Latency Compensation design pattern aims to reduce the perceived delay between a player's input and the visual feedback in the game. This is particularly crucial in games where the authoritative game state (e.g., physics simulation, server state) updates at a different rate or with a delay compared to the rendering frame rate.

In Unity, a common scenario for this pattern involves player-controlled `Rigidbody` objects. Physics updates happen in `FixedUpdate` on a fixed timestep, while rendering occurs in `Update` which runs at a variable frame rate. If `Update` runs multiple times before `FixedUpdate` processes the latest input, the player's visual feedback might lag behind their input.

This example demonstrates a practical form of Input Latency Compensation using **visual extrapolation/prediction**. Instead of simply displaying the `Rigidbody`'s current position (which might be slightly out of date), the visual representation of the player character will slightly "predict" its movement based on the latest input and its current velocity. This creates the illusion of immediate responsiveness.

---

### Key Concepts Illustrated:

1.  **Input Gathering (`Update`):** Player input is read every frame.
2.  **Authoritative Physics (`FixedUpdate`):** The `Rigidbody`'s movement is handled on a fixed timestep, representing the "true" game state.
3.  **State History (Implicit):** The `Rigidbody`'s position and velocity are stored after each `FixedUpdate` as the "last known authoritative state."
4.  **Visual Compensation (`Update`):**
    *   The player's visual model (a child `GameObject`) is moved independently of the root `Rigidbody` `GameObject`.
    *   It interpolates between the last known physics position and the current `Rigidbody` position for smooth visual movement between `FixedUpdate` steps.
    *   **The Compensation:** It extrapolates its visual position slightly ahead based on the `lastKnownPhysicsVelocity` and, crucially, the `currentInput` received *in the same `Update` frame*. This makes the character appear to react instantly, even before `FixedUpdate` processes the input.
5.  **Artificial Latency:** An optional delay in `FixedUpdate` is included to make the benefits of compensation more apparent.

---

### Setup Instructions in Unity:

To use this script and observe the Input Latency Compensation pattern:

1.  **Create a New Unity Project** (or open an existing one).
2.  **Create a Folder** named `Scripts` (or similar) in your `Assets` folder.
3.  **Create the `InputLatencyCompensatedPlayer.cs` script:**
    *   Right-click in the `Scripts` folder -> `Create` -> `C# Script`.
    *   Name it `InputLatencyCompensatedPlayer`.
    *   Copy and paste the code below into this new script file.
4.  **Create the `NonCompensatedPlayer.cs` script (for comparison):**
    *   Right-click in the `Scripts` folder -> `Create` -> `C# Script`.
    *   Name it `NonCompensatedPlayer`.
    *   Copy and paste the code for `NonCompensatedPlayer.cs` (provided below in comments) into this new script file.
5.  **Setup the Scene:**
    *   **Add a Floor:** Right-click in `Hierarchy` -> `3D Object` -> `Plane`. Position it at `(0,0,0)`.
    *   **Create the Compensated Player:**
        *   Right-click in `Hierarchy` -> `Create Empty`. Name it `CompensatedPlayer`.
        *   Add a `Rigidbody` component to `CompensatedPlayer` (select `CompensatedPlayer` -> `Add Component` -> `Rigidbody`).
        *   Set its `Drag` to `5` and `Angular Drag` to `5` for better control.
        *   Add a visual model: Right-click on `CompensatedPlayer` -> `3D Object` -> `Cube`. Name it `VisualCube`.
        *   Position `VisualCube` at `(0,0,0)` relative to `CompensatedPlayer`. Scale it if desired (e.g., `(1,1,1)`).
        *   Select `CompensatedPlayer` and drag the `InputLatencyCompensatedPlayer.cs` script onto it.
        *   In the Inspector for `CompensatedPlayer`, drag the `VisualCube` `GameObject` into the `Visual Transform` field of the `InputLatencyCompensatedPlayer` component.
        *   Set its position, e.g., `(2, 0.5, 0)`.
    *   **Create the Non-Compensated Player (for comparison):**
        *   Right-click in `Hierarchy` -> `Create Empty`. Name it `NonCompensatedPlayer`.
        *   Add a `Rigidbody` component to `NonCompensatedPlayer`.
        *   Set its `Drag` to `5` and `Angular Drag` to `5`.
        *   Add a visual model: Right-click on `NonCompensatedPlayer` -> `3D Object` -> `Sphere`. Name it `VisualSphere`.
        *   Position `VisualSphere` at `(0,0,0)` relative to `NonCompensatedPlayer`. Scale it if desired.
        *   Select `NonCompensatedPlayer` and drag the `NonCompensatedPlayer.cs` script onto it.
        *   Set its position, e.g., `(-2, 0.5, 0)`.
6.  **Run the Scene:** Press `Play`. Use the `A` and `D` keys (or Left/Right arrow keys) to move the players. You can also press `Space` to jump.

### Observing the Effect:

*   **Increase `FixedUpdate Lag Ms`:** In the Inspector for both player GameObjects, set `FixedUpdate Lag Ms` to a value like `100` (100 milliseconds). This will exaggerate the physics delay.
*   **Compare Movement:**
    *   The **Compensated Player** (Cube) should feel more responsive. Its visual model will appear to start moving almost instantly when you press a key.
    *   The **Non-Compensated Player** (Sphere) will exhibit a noticeable delay. You'll press a key, and there will be a brief moment before its visual model starts moving, especially with high `FixedUpdate Lag Ms`.
*   **Gizmos:** In the Scene view, enable Gizmos. Red spheres indicate the `Rigidbody`'s actual position, while green spheres show the `visualTransform`'s predicted position. You'll see the green sphere of the compensated player leading the red sphere slightly when moving.

---

### `InputLatencyCompensatedPlayer.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Not strictly needed for this simplified version, but common for pattern implementations

/// <summary>
/// Demonstrates the Input Latency Compensation design pattern using visual extrapolation/prediction.
/// This script controls a Rigidbody-based player character and aims to reduce
/// perceived input latency by visually predicting movement.
/// </summary>
/// <remarks>
/// This pattern is useful when the authoritative game state (e.g., physics engine,
/// network server) updates on a fixed timestep or is otherwise delayed, but the
/// player expects an immediate visual response to their input.
///
/// **How this specific implementation works (Simplified Client-Side Prediction / Visual Extrapolation):**
/// 1.  **Input Gathering (`Update`):** Player input (e.g., horizontal movement) is read
///     every frame.
/// 2.  **Authoritative Physics (`FixedUpdate`):** The Rigidbody's movement is handled
///     in `FixedUpdate()` on a fixed timestep. This ensures stable and deterministic
///     physics simulation.
/// 3.  **State History (Implicit):** We explicitly store the Rigidbody's position
///     and velocity (`lastPhysicsPosition`, `lastPhysicsVelocity`) immediately after
///     `FixedUpdate()` finishes. This represents the last known "true" state from the physics engine.
/// 4.  **Visual Compensation (`Update`):**
///     a.  Instead of directly setting the player's `transform.position` to the `Rigidbody`'s
///         position (which would lag behind if `Update` runs more frequently or if `FixedUpdate`
///         is delayed), we manipulate a separate `visualTransform` (a child object).
///     b.  The `visualPosition` for this frame is calculated by:
///         i.   **Interpolation:** Smoothly moving the visual transform between
///              `lastPhysicsPosition` and the `Rigidbody`'s *current* position. This helps
///              bridge the visual gap between discrete `FixedUpdate` steps.
///         ii.  **Extrapolation/Prediction (The Compensation):** Further moving the visual
///              transform slightly *ahead* of the interpolated position. This extrapolation
///              is based on the `lastPhysicsVelocity` and, crucially, the `currentHorizontalInput`
///              received in the *current `Update` frame*. This makes the visual model appear
///              to react instantly to the player's input, predicting where the physics body
///              *will be* in the near future.
///     c.  After each `FixedUpdate`, the `visualTransform`'s local position is reset to `Vector3.zero`,
///         snapping it back to perfectly align with the `Rigidbody`'s authoritative position.
///         It then starts predicting again in the subsequent `Update` calls.
///
/// **Practical Use Cases:**
/// *   **Local Player Movement:** Reducing perceived latency for player-controlled characters,
///     especially with physics-based movement.
/// *   **Networked Games (Client-Side Prediction):** A more advanced form where the client
///     immediately simulates its own actions, then reconciles with authoritative server updates.
///     This example provides a foundation for the client-side visual prediction component.
/// </remarks>
public class InputLatencyCompensatedPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the player moves horizontally.")]
    [SerializeField] private float moveSpeed = 10f;
    [Tooltip("Force applied when the player jumps.")]
    [SerializeField] private float jumpForce = 5f;
    [Tooltip("The Rigidbody component for physics-based movement.")]
    [SerializeField] private Rigidbody rb;

    [Header("Visual Settings")]
    [Tooltip("The visual representation of the player (a child GameObject). This is what gets visually manipulated.")]
    [SerializeField] private Transform visualTransform;
    [Tooltip("Multiplier for how aggressively the visual model predicts movement based on input. Tune this for desired feel.")]
    [Range(0f, 2f)]
    [SerializeField] private float predictionStrength = 1.0f;


    [Header("Latency Simulation (for demonstration)")]
    [Tooltip("Artificially delays FixedUpdate by this many milliseconds. Higher values make compensation more apparent.")]
    [Range(0, 200)]
    [SerializeField] private int fixedUpdateLagMs = 50;

    // --- Private members for compensation logic ---
    private float currentHorizontalInput;
    private Vector3 lastPhysicsPosition;
    private Vector3 lastPhysicsVelocity;
    private float lastFixedUpdateTime;

    void Awake()
    {
        // Get Rigidbody if not assigned
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("InputLatencyCompensatedPlayer requires a Rigidbody component!", this);
                enabled = false;
                return;
            }
        }

        // Get visualTransform if not assigned (assumes first child)
        if (visualTransform == null)
        {
            if (transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            else
            {
                Debug.LogWarning("Visual Transform not assigned and no child found. Using the root GameObject's transform for visuals, which is less ideal for compensation.", this);
                visualTransform = transform; // Fallback, though not recommended for this pattern
            }
        }

        // Initialize last known authoritative states
        lastPhysicsPosition = rb.position;
        lastPhysicsVelocity = rb.velocity;
        lastFixedUpdateTime = Time.fixedTime;
    }

    void Update()
    {
        // 1. Input Gathering: Read input every frame.
        currentHorizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            // For simple impulses like jump, applying directly might feel fine,
            // but for full compensation, even impulses could be buffered and
            // processed within the compensated system. For this example, we keep
            // jump simple and focus on continuous movement.
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // 2. Visual Compensation (Extrapolation/Prediction)
        // This calculates where the visual model should be rendered *now* to feel responsive.

        // Time since the last physics update (FixedUpdate)
        float timeSinceFixedUpdate = Time.time - lastFixedUpdateTime;

        // Calculate the interpolation factor (0 to 1) between physics steps.
        // This ensures the visual model smoothly catches up to the Rigidbody's true position.
        float fixedUpdateDeltaTime = Time.fixedDeltaTime;
        float t = Mathf.Clamp01(timeSinceFixedUpdate / fixedUpdateDeltaTime);

        // Interpolate between the last known authoritative physics position and the
        // Rigidbody's current (potentially slightly outdated) position.
        Vector3 interpolatedPhysicsPosition = Vector3.Lerp(lastPhysicsPosition, rb.position, t);

        // --- THE LATENCY COMPENSATION / VISUAL PREDICTION PART ---
        // Extrapolate the visual position further based on the current velocity
        // and, importantly, the *current input* received in this Update frame.
        // This creates the illusion of immediate response.

        Vector3 predictedVelocity = lastPhysicsVelocity;

        // If there's active horizontal input, adjust the predicted velocity
        // to anticipate the Rigidbody's next movement before FixedUpdate actually processes it.
        if (Mathf.Abs(currentHorizontalInput) > 0.01f)
        {
            // We're adding a component to the predicted velocity based on current input.
            // `predictionStrength` allows tuning how aggressively the visual model predicts.
            predictedVelocity += Vector3.right * currentHorizontalInput * moveSpeed * predictionStrength;
        }

        // Apply a small extrapolation using the predicted velocity over the elapsed time
        Vector3 visualPredictionOffset = predictedVelocity * timeSinceFixedUpdate;

        // Combine the interpolated position with the prediction offset to get the final visual position.
        Vector3 visualPosition = interpolatedPhysicsPosition + visualPredictionOffset;

        // Apply the calculated visual position to the visual transform.
        if (visualTransform != null)
        {
            visualTransform.position = visualPosition;
        }
    }

    void FixedUpdate()
    {
        // --- Artificial Latency Simulation ---
        // This coroutine introduces a delay *before* the physics calculations for this FixedUpdate,
        // making the compensation more evident by exaggerating the lag between input and physics processing.
        if (fixedUpdateLagMs > 0)
        {
            StartCoroutine(SimulateFixedUpdateLatency(() =>
            {
                ApplyPhysicsMovement();
            }));
        }
        else
        {
            ApplyPhysicsMovement();
        }
    }

    /// <summary>
    /// Applies physics forces to the Rigidbody and records the new authoritative state.
    /// This is the "true" state update that happens after a fixed timestep.
    /// </summary>
    private void ApplyPhysicsMovement()
    {
        // Apply force based on the latest input read in Update().
        // The Rigidbody's true position is updated here by the physics engine.
        Vector3 movement = new Vector3(currentHorizontalInput, 0, 0);
        rb.AddForce(movement * moveSpeed, ForceMode.Force);

        // Store the authoritative physics state after this FixedUpdate has completed.
        lastPhysicsPosition = rb.position;
        lastPhysicsVelocity = rb.velocity;
        lastFixedUpdateTime = Time.fixedTime;

        // Reset the visual transform's local position relative to the Rigidbody's root.
        // This is crucial: after a physics step, the visual model snaps back to perfectly
        // align with the authoritative Rigidbody position. It then starts predicting again
        // in subsequent Update calls until the next FixedUpdate.
        if (visualTransform != null && visualTransform != transform) // Ensure visualTransform is a child
        {
            visualTransform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Coroutine to simulate latency for FixedUpdate.
    /// </summary>
    /// <param name="callback">The action to perform after the delay.</param>
    private IEnumerator SimulateFixedUpdateLatency(System.Action callback)
    {
        yield return new WaitForSecondsRealtime(fixedUpdateLagMs / 1000f);
        callback?.Invoke();
    }

    /// <summary>
    /// Called after all Update and FixedUpdate calls for the current frame.
    /// Good for final adjustments like clamping velocity.
    /// </summary>
    void LateUpdate()
    {
        // Clamp horizontal velocity to prevent runaway acceleration due to continuous force.
        if (rb != null)
        {
            Vector3 currentVel = rb.velocity;
            currentVel.x = Mathf.Clamp(currentVel.x, -moveSpeed, moveSpeed);
            rb.velocity = currentVel;
        }
    }

    /// <summary>
    /// Draws debug lines and spheres in the Scene view to visualize the Rigidbody's
    /// actual position (red) versus the visual object's compensated position (green).
    /// </summary>
    void OnDrawGizmos()
    {
        if (rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rb.position, 0.2f); // Rigidbody's actual authoritative position

            if (visualTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(visualTransform.position, 0.15f); // Visual object's compensated position
                Gizmos.DrawLine(rb.position, visualTransform.position); // Line connecting them
            }
        }
    }
}
```

### `NonCompensatedPlayer.cs` (For Comparison)

Create a separate C# script file named `NonCompensatedPlayer.cs` and paste the following code into it. This script demonstrates player movement without any input latency compensation, relying directly on the `Rigidbody`'s position for rendering.

```csharp
using UnityEngine;
using System.Collections;

/// <summary>
/// A simple player controller for comparison, demonstrating movement *without*
/// explicit input latency compensation. This player relies directly on
/// Rigidbody.position for its visual updates, which can introduce perceived
/// lag when FixedUpdate runs slower or is delayed.
/// </summary>
public class NonCompensatedPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the player moves horizontally.")]
    [SerializeField] private float moveSpeed = 10f;
    [Tooltip("Force applied when the player jumps.")]
    [SerializeField] private float jumpForce = 5f;
    [Tooltip("The Rigidbody component for physics-based movement.")]
    [SerializeField] private Rigidbody rb;

    [Header("Latency Simulation (for demonstration)")]
    [Tooltip("Artificially delays FixedUpdate by this many milliseconds. Higher values make lag more apparent.")]
    [Range(0, 200)]
    [SerializeField] private int fixedUpdateLagMs = 50;

    private float currentHorizontalInput;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("NonCompensatedPlayer requires a Rigidbody component!", this);
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        // Input Gathering: Read input every frame.
        currentHorizontalInput = Input.GetAxis("Horizontal");
        if (Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // For non-compensated, the visual transform (this.transform, or its child if it has one)
        // implicitly follows the rigidbody's position. Unity's default Rigidbody behavior
        // synchronizes the GameObject's transform with the Rigidbody's position
        // at the start of Update, so no explicit visual prediction is done here.
    }

    void FixedUpdate()
    {
        // --- Artificial Latency Simulation ---
        // This coroutine introduces a delay *before* the physics calculations for this FixedUpdate,
        // making the lag more evident.
        if (fixedUpdateLagMs > 0)
        {
            StartCoroutine(SimulateFixedUpdateLatency(() =>
            {
                ApplyPhysicsMovement();
            }));
        }
        else
        {
            ApplyPhysicsMovement();
        }
    }

    /// <summary>
    /// Applies physics forces to the Rigidbody.
    /// </summary>
    private void ApplyPhysicsMovement()
    {
        Vector3 movement = new Vector3(currentHorizontalInput, 0, 0);
        rb.AddForce(movement * moveSpeed, ForceMode.Force);
    }

    /// <summary>
    /// Coroutine to simulate latency for FixedUpdate.
    /// </summary>
    /// <param name="callback">The action to perform after the delay.</param>
    private IEnumerator SimulateFixedUpdateLatency(System.Action callback)
    {
        yield return new WaitForSecondsRealtime(fixedUpdateLagMs / 1000f);
        callback?.Invoke();
    }

    /// <summary>
    /// Called after all Update and FixedUpdate calls for the current frame.
    /// Good for final adjustments like clamping velocity.
    /// </summary>
    void LateUpdate()
    {
        // Clamp horizontal velocity to prevent runaway acceleration.
        if (rb != null)
        {
            Vector3 currentVel = rb.velocity;
            currentVel.x = Mathf.Clamp(currentVel.x, -moveSpeed, moveSpeed);
            rb.velocity = currentVel;
        }
    }

    /// <summary>
    /// Draws a debug sphere in the Scene view to visualize the Rigidbody's actual position.
    /// </summary>
    void OnDrawGizmos()
    {
        if (rb != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(rb.position, 0.2f); // Rigidbody's actual authoritative position
        }
    }
}
```