// Unity Design Pattern Example: ProceduralAnimation
// This script demonstrates the ProceduralAnimation pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'ProceduralAnimation' design pattern involves generating animation at runtime through code, rather than relying on pre-baked keyframes or sprite sheets. This allows for dynamic, context-aware, and often more natural-looking movements that can react to game events, physics, or player input.

This example demonstrates a common use case: making an object float, wobble, and subtly react to its parent's movement, giving it a more "alive" feel without complex keyframe setup.

---

### `WobbleAndFloat.cs` Script

This script will make an object:
1.  Float up and down smoothly using a sine wave.
2.  Wobble subtly on its X and Z axes (rotation) using sine/cosine waves.
3.  Optionally react with a slight "lag" or "trail" effect when its parent object moves, simulating inertia.

```csharp
using UnityEngine;

/// <summary>
/// ProceduralAnimation Design Pattern Example: WobbleAndFloat
///
/// This script demonstrates the ProceduralAnimation pattern by
/// generating an object's floating, wobbling, and reactive movements
/// entirely through code at runtime, rather than using pre-baked animations.
///
/// Key characteristics of Procedural Animation:
/// 1.  **Code-Driven:** All animation data (position, rotation) is calculated
///     mathematically in real-time.
/// 2.  **Dynamic & Reactive:** Can easily adapt to game state, physics, or
///     external forces (e.g., reacting to parent movement as shown here).
/// 3.  **Variability:** Simple randomization (like time offset) can make
///     multiple instances of the same animation look unique.
/// 4.  **Efficiency:** Can be more memory-efficient than storing large
///     animation clips for simple, repetitive motions.
/// </summary>
[DisallowMultipleComponent] // Prevents adding multiple instances of this script to the same GameObject
public class WobbleAndFloat : MonoBehaviour
{
    // --- Core Procedural Animation Parameters ---

    [Header("Floating Parameters")]
    [Tooltip("How high the object will float up and down from its initial position.")]
    [Range(0f, 2f)]
    [SerializeField] private float floatAmplitude = 0.1f;

    [Tooltip("How fast the object will float up and down.")]
    [Range(0f, 5f)]
    [SerializeField] private float floatSpeed = 1.5f;

    [Header("Wobble (Rotation) Parameters")]
    [Tooltip("Maximum rotation angle around the X-axis.")]
    [Range(0f, 45f)]
    [SerializeField] private float wobbleAmplitudeX = 5f;

    [Tooltip("Speed of the wobble around the X-axis.")]
    [Range(0f, 5f)]
    [SerializeField] private float wobbleSpeedX = 2f;

    [Tooltip("Maximum rotation angle around the Z-axis.")]
    [Range(0f, 45f)]
    [SerializeField] private float wobbleAmplitudeZ = 5f;

    [Tooltip("Speed of the wobble around the Z-axis.")]
    [Range(0f, 5f)]
    [SerializeField] private float wobbleSpeedZ = 2.5f;

    // --- Optional: Reactive Animation Parameters ---

    [Header("Parent Movement Reaction")]
    [Tooltip("If true, the object will subtly lag/trail its parent's movement.")]
    [SerializeField] private bool reactToParentMovement = true;

    [Tooltip("How much the object lags behind its parent. Higher values mean more lag.")]
    [Range(0f, 1f)]
    [SerializeField] private float lagStrength = 0.5f;

    [Tooltip("How smoothly the object catches up to its parent-induced offset. Lower values mean snappier.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float lagSmoothTime = 0.1f;

    // --- Internal State Variables ---

    private Vector3 _initialLocalPosition;      // Stores the object's starting local position.
    private Quaternion _initialLocalRotation;   // Stores the object's starting local rotation.
    private float _timeOffset;                  // A random offset for sine waves to desynchronize multiple instances.

    private Vector3 _previousParentWorldPosition; // Stores parent's position from previous frame for reaction calculation.
    private Vector3 _currentLagOffset;            // Current calculated offset due to parent movement.
    private Vector3 _lagVelocity;                 // Used by SmoothDamp for the lag effect.

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the initial position, rotation, and time offset.
    /// </summary>
    private void Awake()
    {
        // Store the object's initial local position and rotation.
        // All procedural animations will be applied relative to these values.
        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;

        // Give each instance a random time offset so they don't all animate in sync.
        _timeOffset = Random.Range(0f, 100f);

        // If reacting to parent movement, store the parent's initial world position.
        if (reactToParentMovement && transform.parent != null)
        {
            _previousParentWorldPosition = transform.parent.position;
        }
    }

    /// <summary>
    /// Called once per frame. This is where all the procedural animation calculations happen.
    /// </summary>
    private void Update()
    {
        // Calculate the current time for sine wave functions, adjusted by a unique offset.
        float time = Time.time + _timeOffset;

        // --- 1. Procedural Floating (Vertical Movement) ---
        // Uses a sine wave to create a smooth, oscillating vertical movement.
        // Mathf.Sin returns values between -1 and 1.
        float floatY = Mathf.Sin(time * floatSpeed) * floatAmplitude;
        Vector3 newLocalPosition = _initialLocalPosition + new Vector3(0, floatY, 0);

        // --- 2. Procedural Wobbling (Rotation) ---
        // Uses sine and cosine waves for distinct X and Z axis wobbles.
        // This creates a more organic, less predictable movement.
        float wobbleX = Mathf.Sin(time * wobbleSpeedX) * wobbleAmplitudeX;
        float wobbleZ = Mathf.Cos(time * wobbleSpeedZ) * wobbleAmplitudeZ; // Using Cos for phase difference
        Quaternion newLocalRotation = _initialLocalRotation * Quaternion.Euler(wobbleX, 0, wobbleZ);

        // --- 3. Optional: Procedural Reaction to Parent Movement (Lag/Trail Effect) ---
        if (reactToParentMovement && transform.parent != null)
        {
            // Calculate how much the parent has moved since the last frame.
            Vector3 parentWorldDelta = transform.parent.position - _previousParentWorldPosition;

            // Determine the target local offset. If parent moves right, the object should
            // lag slightly left relative to the parent's local space.
            Vector3 targetLagOffset = -parentWorldDelta * lagStrength;

            // Smoothly interpolate the current lag offset towards the target lag offset.
            // This creates the dampening/springy effect.
            _currentLagOffset = Vector3.SmoothDamp(
                _currentLagOffset,
                targetLagOffset,
                ref _lagVelocity,
                lagSmoothTime
            );

            // Apply the lag offset to the object's local position.
            newLocalPosition += _currentLagOffset;

            // Update the previous parent position for the next frame's calculation.
            _previousParentWorldPosition = transform.parent.position;
        }

        // Apply the calculated procedural animations to the transform.
        // We use localPosition and localRotation so the object behaves correctly
        // even if it's a child of another moving/rotating object.
        transform.localPosition = newLocalPosition;
        transform.localRotation = newLocalRotation;
    }

    /// <summary>
    /// Visualize the initial position in the editor for debugging.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.parent != null ? transform.parent.TransformPoint(_initialLocalPosition) : _initialLocalPosition, 0.05f);
            Gizmos.color = new Color(0, 1, 1, 0.2f); // Transparent cyan
            Gizmos.DrawSphere(transform.parent != null ? transform.parent.TransformPoint(_initialLocalPosition) : _initialLocalPosition, floatAmplitude);
        }
    }
}
```

---

### How to Use This Script in Unity:

1.  **Create a C# Script:** In your Unity project, go to `Assets -> Create -> C# Script` and name it `WobbleAndFloat`.
2.  **Copy and Paste:** Open the newly created script and replace its entire content with the code provided above. Save the script.
3.  **Create a GameObject:** In your Unity scene, create an empty GameObject (`GameObject -> Create Empty`). This will be your animated object. You can add a 3D Mesh (e.g., a Cube or Sphere) as a child to visualize it better, or attach the script directly to an existing model.
4.  **Attach the Script:** Drag and drop the `WobbleAndFloat` script from your Project window onto your created GameObject in the Hierarchy.
5.  **Configure in Inspector:**
    *   Select the GameObject with the script attached.
    *   In the Inspector, you'll see the script's parameters.
    *   **Floating Parameters:** Adjust `Float Amplitude` (how high it floats) and `Float Speed` (how fast it floats).
    *   **Wobble (Rotation) Parameters:** Adjust `Wobble Amplitude X/Z` (max rotation angle) and `Wobble Speed X/Z` (speed of rotation).
    *   **Parent Movement Reaction:**
        *   Check `React To Parent Movement` if you want it to lag behind its parent.
        *   `Lag Strength`: Controls how pronounced the lag effect is.
        *   `Lag Smooth Time`: Controls how quickly it dampens and catches up.
    *   **To see the parent reaction:** Create another Empty GameObject (e.g., "Mover"). Make your `WobbleAndFloat` GameObject a child of "Mover". Then, either manually move the "Mover" in the scene view during Play Mode, or attach a simple `Rigidbody` and apply forces, or a simple `MoveScript` to "Mover" to make it move around.

6.  **Run the Scene:** Press Play. You should see your GameObject floating and wobbling smoothly, adapting to the parameters you set. If it's a child of a moving parent and `reactToParentMovement` is enabled, you'll observe a subtle trailing effect.

---

### Educational Takeaways:

*   **Procedural Animation Core:** This script fundamentally generates animation frames on the fly using `Mathf.Sin` and `Mathf.Cos` functions tied to `Time.time`. This is the essence of procedural animation.
*   **Modularity:** Each aspect (float, wobble, lag) is calculated independently and then combined, making the script easy to extend or modify.
*   **Runtime Control:** All parameters are exposed in the Inspector, allowing designers to tweak the animation's feel in real-time without recompiling or re-importing animation assets.
*   **Local vs. World Space:** Using `localPosition` and `localRotation` ensures the procedural movements are relative to the object's parent, making it behave correctly within hierarchies.
*   **`Time.deltaTime` (Implicit):** While not explicitly called in the `Mathf.Sin` arguments (as `Time.time` is already time-based), the `Vector3.SmoothDamp` function internally handles `Time.deltaTime` to ensure frame-rate independent smoothing for the lag effect.
*   **Randomization:** The `_timeOffset` is a simple yet effective way to ensure multiple instances of the same procedural animation don't look identical.