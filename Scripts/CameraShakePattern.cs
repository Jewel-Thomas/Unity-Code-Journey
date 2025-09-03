// Unity Design Pattern Example: CameraShakePattern
// This script demonstrates the CameraShakePattern pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'CameraShakePattern' isn't a formally recognized Gang of Four design pattern, but rather a common game development *strategy* for creating flexible and extensible camera shake systems. This solution leverages the **Strategy Pattern** to define different types of camera shake behaviors, and a **Singleton-like Manager** (the `CameraShakeController`) to apply and manage these effects.

Here's how the pattern is implemented:

1.  **`ICameraShakeEffect` (Strategy Interface):** Defines the common interface for all camera shake types. Each concrete shake effect must implement this, providing its own logic for calculating position and rotation offsets.
2.  **Concrete Shake Effects (Concrete Strategies):**
    *   `ExplosionShake`: A short, high-intensity shake that quickly decays, ideal for impacts.
    *   `ConstantRumbleShake`: A sustained, lower-intensity shake suitable for ongoing events like engine vibrations or earthquakes.
    *   `DirectionalPushShake`: A quick jolt in a specific direction, useful for recoil or pushes.
    Each of these classes encapsulates a specific shake algorithm.
3.  **`CameraShakeController` (Context/Manager):**
    *   This is a `MonoBehaviour` that manages a list of active `ICameraShakeEffect` instances.
    *   It's responsible for combining the offsets and rotations from all active shakes.
    *   It applies the final calculated transform to the camera in `LateUpdate` (ensuring camera movements happen after all other object movements).
    *   It provides a public `StartShake` method to add new shake effects.
    *   It also handles smoothly returning the camera to its original position when no shakes are active or when they fade out.
    *   It uses a `static Instance` for easy global access (a common pattern in Unity for managers).
4.  **`ShakeTester` (Example Usage):** A simple script to demonstrate how to trigger various shake effects using the `CameraShakeController`.

---

## 1. Project Setup and Code

You will need five C# script files for this example.

### 1.1. `ICameraShakeEffect.cs`

This interface defines the contract for any camera shake effect.

```csharp
// File: ICameraShakeEffect.cs
using UnityEngine;

/// <summary>
/// Interface for a camera shake effect strategy.
/// This defines the contract for any specific type of camera shake.
/// </summary>
public interface ICameraShakeEffect
{
    /// <summary>
    /// Updates the shake effect's internal state and calculates the current offset and rotation.
    /// This method is called every frame by the CameraShakeController.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <returns>A tuple containing the calculated position offset (Vector3) and Euler angle rotation (Vector3) for the camera.</returns>
    (Vector3 offset, Vector3 rotationEuler) UpdateShake(float deltaTime);

    /// <summary>
    /// Gets a value indicating whether the shake effect has completed its duration or faded out.
    /// The CameraShakeController will remove finished shakes from its active list.
    /// </summary>
    bool IsFinished { get; }
}
```

### 1.2. `ExplosionShake.cs`

A concrete shake strategy for a quick, impactful shake.

```csharp
// File: ExplosionShake.cs
using UnityEngine;

/// <summary>
/// A concrete implementation of ICameraShakeEffect for an explosive, short-duration shake.
/// Characterized by high initial intensity that quickly decays.
/// Uses Perlin noise for organic, non-repeating motion.
/// </summary>
public class ExplosionShake : ICameraShakeEffect
{
    private float _duration;
    private float _intensity;
    private float _roughness; // Controls the frequency/speed of the Perlin noise
    private float _elapsedTime;
    private float _perlinOffsetX; // Used to seed Perlin noise for x, y, z axes
    private float _perlinOffsetY;
    private float _perlinOffsetZ;

    /// <summary>
    /// Initializes a new instance of the ExplosionShake.
    /// </summary>
    /// <param name="duration">The total duration of the shake effect in seconds.</param>
    /// <param name="intensity">The maximum intensity (amplitude) of the shake effect.</param>
    /// <param name="roughness">How "jagged" or "smooth" the shake motion is. Higher values mean faster, more chaotic motion.</param>
    public ExplosionShake(float duration, float intensity, float roughness)
    {
        _duration = duration;
        _intensity = intensity;
        _roughness = roughness;
        _elapsedTime = 0f;

        // Initialize Perlin noise offsets for different axes to make motion less predictable
        _perlinOffsetX = Random.Range(0f, 1000f);
        _perlinOffsetY = Random.Range(0f, 1000f);
        _perlinOffsetZ = Random.Range(0f, 1000f);
    }

    /// <inheritdoc/>
    public (Vector3 offset, Vector3 rotationEuler) UpdateShake(float deltaTime)
    {
        _elapsedTime += deltaTime;

        // Calculate a normalized value from 0 to 1 based on elapsed time
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);

        // Apply an ease-out or exponential decay to the intensity over time
        // Example: starts at full intensity, rapidly decays to zero using a quadratic decay
        float currentIntensity = _intensity * (1f - normalizedTime * normalizedTime);

        // If shake has finished or intensity is too low, return zero to stop contributing
        if (IsFinished || currentIntensity <= 0.001f)
        {
            return (Vector3.zero, Vector3.zero);
        }

        // Use Perlin noise to get smooth, pseudo-random values for position offset
        // Scale roughness by current time to make the noise 'move'
        float x = (Mathf.PerlinNoise(_perlinOffsetX + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;
        float y = (Mathf.PerlinNoise(_perlinOffsetY + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;
        float z = (Mathf.PerlinNoise(_perlinOffsetZ + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;

        // For rotation, use similar noise values, but usually with lower intensity
        // Using different seed offsets (e.g., 100f, 200f, 300f) for distinct rotation noise
        float rotX = (Mathf.PerlinNoise(_perlinOffsetX + _elapsedTime * _roughness * 1.5f, 100f) * 2f - 1f) * currentIntensity * 0.5f;
        float rotY = (Mathf.PerlinNoise(_perlinOffsetY + _elapsedTime * _roughness * 1.5f, 200f) * 2f - 1f) * currentIntensity * 0.5f;
        float rotZ = (Mathf.PerlinNoise(_perlinOffsetZ + _elapsedTime * _roughness * 1.5f, 300f) * 2f - 1f) * currentIntensity * 0.5f;

        return (new Vector3(x, y, z), new Vector3(rotX, rotY, rotZ));
    }

    /// <inheritdoc/>
    public bool IsFinished => _elapsedTime >= _duration;
}
```

### 1.3. `ConstantRumbleShake.cs`

A concrete shake strategy for a continuous, sustained rumble.

```csharp
// File: ConstantRumbleShake.cs
using UnityEngine;

/// <summary>
/// A concrete implementation of ICameraShakeEffect for a continuous, sustained rumble shake.
/// Characterized by a steady intensity over its duration, ideal for ongoing effects like engines or earthquakes.
/// </summary>
public class ConstantRumbleShake : ICameraShakeEffect
{
    private float _duration;
    private float _intensity;
    private float _roughness; // Controls the frequency/speed of the Perlin noise
    private float _elapsedTime;
    private float _perlinOffsetX;
    private float _perlinOffsetY;
    private float _perlinOffsetZ;

    /// <summary>
    /// Initializes a new instance of the ConstantRumbleShake.
    /// </summary>
    /// <param name="duration">The total duration of the shake effect in seconds.</param>
    /// <param name="intensity">The constant intensity (amplitude) of the shake effect.</param>
    /// <param name="roughness">How "jagged" or "smooth" the shake motion is. Higher values mean faster, more chaotic motion.</param>
    public ConstantRumbleShake(float duration, float intensity, float roughness)
    {
        _duration = duration;
        _intensity = intensity;
        _roughness = roughness;
        _elapsedTime = 0f;

        _perlinOffsetX = Random.Range(0f, 1000f);
        _perlinOffsetY = Random.Range(0f, 1000f);
        _perlinOffsetZ = Random.Range(0f, 1000f);
    }

    /// <inheritdoc/>
    public (Vector3 offset, Vector3 rotationEuler) UpdateShake(float deltaTime)
    {
        _elapsedTime += deltaTime;

        // For a constant rumble, the intensity might only slightly fade out at the very end
        // or remain constant until the duration is over.
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);
        float currentIntensity = _intensity; // Mostly constant

        // Optional: Fade out slightly at the end to avoid an abrupt stop
        if (normalizedTime > 0.8f) // Fade out over the last 20% of duration
        {
            currentIntensity *= (1f - (normalizedTime - 0.8f) / 0.2f);
        }

        if (IsFinished || currentIntensity <= 0.001f)
        {
            return (Vector3.zero, Vector3.zero);
        }

        // Use Perlin noise, similar to ExplosionShake, but with sustained intensity
        float x = (Mathf.PerlinNoise(_perlinOffsetX + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;
        float y = (Mathf.PerlinNoise(_perlinOffsetY + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;
        float z = (Mathf.PerlinNoise(_perlinOffsetZ + _elapsedTime * _roughness, 0f) * 2f - 1f) * currentIntensity;

        float rotX = (Mathf.PerlinNoise(_perlinOffsetX + _elapsedTime * _roughness * 1.2f, 100f) * 2f - 1f) * currentIntensity * 0.3f;
        float rotY = (Mathf.PerlinNoise(_perlinOffsetY + _elapsedTime * _roughness * 1.2f, 200f) * 2f - 1f) * currentIntensity * 0.3f;
        float rotZ = (Mathf.PerlinNoise(_perlinOffsetZ + _elapsedTime * _roughness * 1.2f, 300f) * 2f - 1f) * currentIntensity * 0.3f;

        return (new Vector3(x, y, z), new Vector3(rotX, rotY, rotZ));
    }

    /// <inheritdoc/>
    public bool IsFinished => _elapsedTime >= _duration;
}
```

### 1.4. `DirectionalPushShake.cs`

A concrete shake strategy for a quick, directional push.

```csharp
// File: DirectionalPushShake.cs
using UnityEngine;

/// <summary>
/// A concrete implementation of ICameraShakeEffect that provides a quick, directional "push" effect.
/// Useful for impacts, recoils, or a quick jolt in a specific direction.
/// The effect quickly reaches its peak then smoothly returns to zero.
/// </summary>
public class DirectionalPushShake : ICameraShakeEffect
{
    private float _duration;
    private float _intensity;
    private Vector3 _direction; // The direction of the push (e.g., Vector3.back for recoil)
    private float _elapsedTime;

    /// <summary>
    /// Initializes a new instance of the DirectionalPushShake.
    /// </summary>
    /// <param name="duration">The total duration of the shake effect in seconds (how long it takes to return to zero).</param>
    /// <param name="intensity">The maximum intensity (amplitude) of the push.</param>
    /// <param name="direction">The normalized direction of the push (e.g., Vector3.back for recoil).</param>
    public DirectionalPushShake(float duration, float intensity, Vector3 direction)
    {
        _duration = duration;
        _intensity = intensity;
        _direction = direction.normalized; // Ensure direction is normalized
        _elapsedTime = 0f;
    }

    /// <inheritdoc/>
    public (Vector3 offset, Vector3 rotationEuler) UpdateShake(float deltaTime)
    {
        _elapsedTime += deltaTime;

        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);

        // Use a bell curve or a quick ease-in/ease-out function for the intensity profile.
        // For example, starts at 0, peaks, then returns to 0.
        // A simple sinusoidal wave can work: sin(normalizedTime * PI)
        float currentIntensity = Mathf.Sin(normalizedTime * Mathf.PI) * _intensity;

        if (IsFinished || currentIntensity <= 0.001f)
        {
            return (Vector3.zero, Vector3.zero);
        }

        // The offset is directly proportional to the direction and current intensity
        Vector3 offset = _direction * currentIntensity;

        // Rotational shake for directional push might be minimal or derived from the direction.
        // Here, a slight "kick" rotation based on direction is added by crossing with Vector3.up.
        Vector3 rotationEuler = Vector3.Cross(_direction, Vector3.up) * currentIntensity * 0.2f;

        return (offset, rotationEuler);
    }

    /// <inheritdoc/>
    public bool IsFinished => _elapsedTime >= _duration;
}
```

### 1.5. `CameraShakeController.cs`

This is the central manager (Context) that applies all active shake effects to the camera.

```csharp
// File: CameraShakeController.cs
using UnityEngine;
using System.Collections.Generic; // For List<T>
using System.Linq; // For .ToList() extension method for safe iteration

/// <summary>
/// The CameraShakeController is a MonoBehaviour that manages and applies various camera shake effects.
/// It acts as the "Context" in the Strategy Pattern, consuming ICameraShakeEffect strategies.
/// Attach this script to your main camera or its parent rig.
/// </summary>
public class CameraShakeController : MonoBehaviour
{
    /// <summary>
    /// Static instance to allow easy access from anywhere without needing to find the object.
    /// This follows a Singleton-like pattern for convenience in a typical game setup.
    /// </summary>
    public static CameraShakeController Instance { get; private set; }

    [Header("Camera Rig Settings")]
    [Tooltip("The actual transform that will be moved and rotated. If null, this GameObject's transform is used.")]
    [SerializeField] private Transform _targetCameraTransform;

    [Tooltip("How smoothly the camera returns to its original position after shakes. Higher values mean faster return.")]
    [SerializeField] private float _returnToOriginSpeed = 5f;

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private List<ICameraShakeEffect> _activeShakes = new List<ICameraShakeEffect>();

    private void Awake()
    {
        // Singleton enforcement: Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // If no target transform is assigned in the Inspector, use this GameObject's transform.
        // This is useful if the controller is directly on the camera.
        if (_targetCameraTransform == null)
        {
            _targetCameraTransform = transform;
        }

        // Store the initial local position and rotation of the camera rig.
        // This is crucial for returning the camera to its base state after shakes.
        _initialLocalPosition = _targetCameraTransform.localPosition;
        _initialLocalRotation = _targetCameraTransform.localRotation;
    }

    /// <summary>
    /// LateUpdate is called once per frame, after all Update functions have been called.
    /// This is ideal for camera movements to ensure they react to all object movements
    /// and avoid visual jitter caused by other updates.
    /// </summary>
    private void LateUpdate()
    {
        // Calculate the combined offset and rotation from all active shakes
        Vector3 totalShakeOffset = Vector3.zero;
        Vector3 totalShakeRotationEuler = Vector3.zero;

        // Iterate through a copy of the list to allow safe modification (removal of finished shakes)
        // during iteration. Using .ToList() creates a shallow copy.
        foreach (ICameraShakeEffect shake in _activeShakes.ToList())
        {
            if (shake.IsFinished)
            {
                _activeShakes.Remove(shake); // Remove finished shakes from the original list
                continue;
            }

            // Get the current offset and rotation from the shake effect
            (Vector3 offset, Vector3 rotationEuler) = shake.UpdateShake(Time.deltaTime);
            totalShakeOffset += offset;
            totalShakeRotationEuler += rotationEuler;
        }

        // Apply the combined shake to the camera's local position and rotation.
        // The camera always moves relative to its initial position/rotation,
        // so we add the shake to the initial state.

        // Determine the target position based on initial position plus accumulated shakes
        Vector3 targetPosition = _initialLocalPosition + totalShakeOffset;
        // Smoothly interpolate the camera's current position towards the target shake position.
        // When totalShakeOffset is zero (no active shakes), it smoothly returns to _initialLocalPosition.
        _targetCameraTransform.localPosition = Vector3.Lerp(_targetCameraTransform.localPosition, targetPosition, Time.deltaTime * _returnToOriginSpeed);

        // Determine the target rotation based on initial rotation plus accumulated shakes (Euler angles)
        Quaternion targetRotation = _initialLocalRotation * Quaternion.Euler(totalShakeRotationEuler);
        // Smoothly spherical interpolate (Slerp) the camera's current rotation towards the target shake rotation.
        _targetCameraTransform.localRotation = Quaternion.Slerp(_targetCameraTransform.localRotation, targetRotation, Time.deltaTime * _returnToOriginSpeed);
    }

    /// <summary>
    /// Starts a new camera shake effect.
    /// Multiple shake effects can be active simultaneously, and their outputs will be combined.
    /// </summary>
    /// <param name="shakeEffect">The ICameraShakeEffect instance to start.</param>
    public void StartShake(ICameraShakeEffect shakeEffect)
    {
        if (shakeEffect == null)
        {
            Debug.LogWarning("Attempted to start a null camera shake effect. Please provide a valid ICameraShakeEffect instance.");
            return;
        }
        _activeShakes.Add(shakeEffect);
    }

    /// <summary>
    /// Clears all currently active camera shake effects immediately, causing the camera to return
    /// to its initial position and rotation smoothly based on _returnToOriginSpeed.
    /// </summary>
    public void StopAllShakes()
    {
        _activeShakes.Clear();
    }

    /// <summary>
    /// Stops camera shakes of a specific type. Useful for stopping a continuous rumble, for example.
    /// </summary>
    /// <typeparam name="T">The type of ICameraShakeEffect to stop (e.g., ExplosionShake, ConstantRumbleShake).</typeparam>
    public void StopShakesOfType<T>() where T : ICameraShakeEffect
    {
        // Remove all shakes from the list that are of the specified type T
        _activeShakes.RemoveAll(shake => shake is T);
    }

    private void OnDestroy()
    {
        // Clean up static instance reference when the GameObject is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

### 1.6. `ShakeTester.cs`

This script provides example usage by triggering different shakes.

```csharp
// File: ShakeTester.cs
using UnityEngine;
using UnityEngine.UI; // For UI elements if using buttons
using System.Collections; // For Coroutines

/// <summary>
/// This script demonstrates how to use the CameraShakeController
/// by triggering different types of camera shakes via key presses or UI buttons.
/// Attach this script to any GameObject in your scene (e.g., a "GameManager" or an empty object).
/// </summary>
public class ShakeTester : MonoBehaviour
{
    [Header("Shake Parameters: Explosion")]
    [SerializeField] private float _explosionDuration = 0.4f;
    [SerializeField] private float _explosionIntensity = 0.3f;
    [SerializeField] private float _explosionRoughness = 20f;

    [Header("Shake Parameters: Rumble")]
    [SerializeField] private float _rumbleDuration = 2.0f;
    [SerializeField] private float _rumbleIntensity = 0.1f;
    [SerializeField] private float _rumbleRoughness = 10f;

    [Header("Shake Parameters: Directional Push")]
    [SerializeField] private float _pushDuration = 0.3f;
    [SerializeField] private float _pushIntensity = 0.5f;
    [SerializeField] private Vector3 _pushDirection = Vector3.back; // Recoil effect

    [Header("UI References (Optional)")]
    [Tooltip("Assign UI Buttons here if you want to trigger shakes from the UI.")]
    [SerializeField] private Button _explosionButton;
    [SerializeField] private Button _rumbleButton;
    [SerializeField] private Button _pushButton;
    [SerializeField] private Button _stopAllButton;
    [SerializeField] private Button _combinedShakeButton;
    [SerializeField] private Button _sequentialShakeButton;

    private void Start()
    {
        // Add listeners to UI buttons if they are assigned in the Inspector
        _explosionButton?.onClick.AddListener(TriggerExplosionShake);
        _rumbleButton?.onClick.AddListener(TriggerRumbleShake);
        _pushButton?.onClick.AddListener(TriggerDirectionalPushShake);
        _stopAllButton?.onClick.AddListener(StopAllShakes);
        _combinedShakeButton?.onClick.AddListener(TriggerCombinedShake);
        _sequentialShakeButton?.onClick.AddListener(TriggerSequentialShakes);

        Debug.Log("Camera Shake Tester Ready! Press '1' for Explosion, '2' for Rumble, '3' for Directional Push, '4' to Stop All.");
        Debug.Log("Also try '5' for combined shake, '6' for sequential shakes, or use the UI buttons if set up.");
    }

    private void Update()
    {
        // Keyboard input for quick testing in the editor
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TriggerExplosionShake();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerRumbleShake();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TriggerDirectionalPushShake();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StopAllShakes();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TriggerCombinedShake();
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TriggerSequentialShakes();
        }
    }

    /// <summary>
    /// Triggers an explosion-like camera shake using configured parameters.
    /// </summary>
    public void TriggerExplosionShake()
    {
        if (CameraShakeController.Instance != null)
        {
            Debug.Log("Triggering Explosion Shake!");
            // Create a new ExplosionShake strategy instance and start it
            CameraShakeController.Instance.StartShake(new ExplosionShake(_explosionDuration, _explosionIntensity, _explosionRoughness));
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    /// <summary>
    /// Triggers a constant rumble camera shake using configured parameters.
    /// </summary>
    public void TriggerRumbleShake()
    {
        if (CameraShakeController.Instance != null)
        {
            Debug.Log("Triggering Rumble Shake!");
            // Create a new ConstantRumbleShake strategy instance and start it
            CameraShakeController.Instance.StartShake(new ConstantRumbleShake(_rumbleDuration, _rumbleIntensity, _rumbleRoughness));
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    /// <summary>
    /// Triggers a directional push camera shake using configured parameters.
    /// </summary>
    public void TriggerDirectionalPushShake()
    {
        if (CameraShakeController.Instance != null)
        {
            Debug.Log("Triggering Directional Push Shake!");
            // Create a new DirectionalPushShake strategy instance and start it
            CameraShakeController.Instance.StartShake(new DirectionalPushShake(_pushDuration, _pushIntensity, _pushDirection));
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    /// <summary>
    /// Stops all currently active camera shakes immediately.
    /// The camera will smoothly return to its initial position.
    /// </summary>
    public void StopAllShakes()
    {
        if (CameraShakeController.Instance != null)
        {
            Debug.Log("Stopping All Shakes!");
            CameraShakeController.Instance.StopAllShakes();
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    /// <summary>
    /// Demonstrates triggering multiple different shake effects simultaneously.
    /// Their effects will combine.
    /// </summary>
    public void TriggerCombinedShake()
    {
        if (CameraShakeController.Instance != null)
        {
            Debug.Log("Triggering Combined Shake (Explosion + Rumble)!");
            // Start an explosion shake
            CameraShakeController.Instance.StartShake(new ExplosionShake(0.6f, 0.2f, 25f));
            // Start a longer, lower intensity rumble shake at the same time
            CameraShakeController.Instance.StartShake(new ConstantRumbleShake(3f, 0.05f, 8f));
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    /// <summary>
    /// Demonstrates triggering multiple different shake effects sequentially using a Coroutine.
    /// </summary>
    public void TriggerSequentialShakes()
    {
        if (CameraShakeController.Instance != null)
        {
            StartCoroutine(DoSequentialShakes());
        }
        else
        {
            Debug.LogError("CameraShakeController.Instance is not found. Make sure it's attached to your camera (or camera rig) and active in the scene!");
        }
    }

    private IEnumerator DoSequentialShakes()
    {
        Debug.Log("Starting sequential shakes...");
        // Push forward
        CameraShakeController.Instance.StartShake(new DirectionalPushShake(0.2f, 0.4f, Vector3.forward));
        yield return new WaitForSeconds(0.3f); // Wait a bit
        // Explosion
        CameraShakeController.Instance.StartShake(new ExplosionShake(0.5f, 0.25f, 20f));
        yield return new WaitForSeconds(0.6f); // Wait for explosion to mostly finish
        // Long rumble
        CameraShakeController.Instance.StartShake(new ConstantRumbleShake(1.5f, 0.08f, 12f));
        Debug.Log("Sequential shakes finished.");
    }
}
```

---

## 2. How to Use in Unity

1.  **Create C# Scripts:**
    *   In your Unity Project window, create five new C# scripts named `ICameraShakeEffect`, `ExplosionShake`, `ConstantRumbleShake`, `DirectionalPushShake`, `CameraShakeController`, and `ShakeTester`.
    *   Copy and paste the code for each respective file into its new script.

2.  **Setup `CameraShakeController`:**
    *   Select your `Main Camera` GameObject (or the parent GameObject that acts as your camera rig).
    *   Add the `CameraShakeController` component to it.
    *   **Crucial:** If the `CameraShakeController` is *not* directly on the `Main Camera` (e.g., it's on a parent "CameraRig" GameObject), drag your actual `Main Camera` GameObject into the `_targetCameraTransform` slot in the Inspector of the `CameraShakeController`. If `CameraShakeController` *is* on the `Main Camera`, you can leave this field empty, and it will automatically use its own `transform`.
    *   Adjust `_returnToOriginSpeed` in the Inspector to control how quickly the camera snaps to the current shake or returns to its initial state.

3.  **Setup `ShakeTester`:**
    *   Create an empty GameObject in your scene and name it something like `GameManager` or `ShakeTesterObject`.
    *   Add the `ShakeTester` component to this new GameObject.
    *   You can adjust the shake parameters for Explosion, Rumble, and Directional Push directly in the Inspector of the `ShakeTester`.

4.  **Optional: Setup UI Buttons (Recommended for easy testing):**
    *   Create a UI Canvas (GameObject -> UI -> Canvas).
    *   Inside the Canvas, create several Buttons (e.g., 6 buttons).
    *   For each Button:
        *   In the Button's Inspector, find the `OnClick()` event list.
        *   Click the `+` button to add a new event.
        *   Drag your `ShakeTesterObject` (from step 3) into the "None (Object)" slot.
        *   From the "No Function" dropdown, select `ShakeTester` and then the corresponding public method (e.g., `TriggerExplosionShake`, `TriggerRumbleShake`, `TriggerDirectionalPushShake`, `StopAllShakes`, `TriggerCombinedShake`, `TriggerSequentialShakes`).
        *   Change the button text to match its function.

5.  **Run the Scene:**
    *   Press Play in Unity.
    *   You can now trigger camera shakes using:
        *   Keyboard: Press `1` for Explosion, `2` for Rumble, `3` for Directional Push, `4` to Stop All, `5` for Combined, `6` for Sequential.
        *   UI Buttons: Click the buttons you set up.

---

## 3. Explanation of the Camera Shake Pattern

This implementation showcases the **Strategy Design Pattern**.

*   **Strategy Interface (`ICameraShakeEffect`):** This defines the common operations that all camera shake algorithms must implement (`UpdateShake` and `IsFinished`). It ensures that different shake types can be used interchangeably.
*   **Concrete Strategies (`ExplosionShake`, `ConstantRumbleShake`, `DirectionalPushShake`):** These are the specific algorithms (strategies) for different types of shakes. Each class encapsulates its unique logic, parameters, and decay curves. They are self-contained and don't depend on each other or the `CameraShakeController`'s internal workings.
*   **Context (`CameraShakeController`):** This is the component that holds a reference to one or more `ICameraShakeEffect` strategies. It delegates the task of calculating the individual shake components to the concrete strategies. It then combines these results and applies them to the camera. The `CameraShakeController` itself doesn't know *how* each shake is calculated, only *that* it can ask them for their current effect.

**Benefits of this Pattern:**

*   **Extensibility:** Easily add new types of camera shakes (e.g., `HeartbeatShake`, `EarthquakeShake`, `VehicleBumpShake`) by simply creating a new class that implements `ICameraShakeEffect`. You don't need to modify the `CameraShakeController`.
*   **Flexibility:** Multiple shake effects can be active simultaneously, and the `CameraShakeController` gracefully combines their outputs. This allows for complex, layered shake effects (e.g., a constant rumble from an engine combined with a sudden jolt from hitting a bump).
*   **Modularity:** Each shake type is self-contained and focuses on its specific logic, making the code easier to understand, test, and maintain.
*   **Reusability:** Shake effect implementations can be reused across different parts of your game or even in different projects.
*   **Runtime Behavior Change:** Shake effects can be dynamically added or removed at runtime, allowing for highly reactive and adaptive camera behavior.

This pattern makes your camera shake system robust, maintainable, and highly adaptable to diverse gameplay scenarios.