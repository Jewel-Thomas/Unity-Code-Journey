// Unity Design Pattern Example: RecoilSystem
// This script demonstrates the RecoilSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'RecoilSystem' design pattern in Unity (while not a formal GoF pattern, it's a common architectural approach) focuses on centralizing and managing the visual and mechanical effects of recoil, typically from a weapon. It decouples the act of *firing* a weapon from the complex logic of *applying and recovering from* recoil on various game objects like the camera and the weapon model itself.

**Key Principles of the RecoilSystem Pattern:**

1.  **Centralization:** All recoil-related logic (applying kick, handling recovery, managing parameters) resides in one dedicated system component.
2.  **Decoupling:** Weapon scripts simply "tell" the RecoilSystem to apply recoil, without needing to know *how* it's done or on which objects.
3.  **Configurability:** Recoil parameters (strength, speed, recovery) are exposed in the Inspector, allowing designers to easily tune weapon feel without code changes.
4.  **Smooth Transitions:** Recoil effects are typically applied and recovered using interpolation (Lerp, Slerp) for a natural, smooth feel.
5.  **Additive Effects:** Recoil is usually applied as an *offset* or *addition* to existing rotations/positions, ensuring it doesn't conflict with other systems like mouse look or weapon animations.

---

## `RecoilSystem.cs`

This script demonstrates a practical `RecoilSystem`. It manages recoil on both a camera (for view kick) and a weapon model (for visual weapon movement), applying them as additive offsets.

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator if you use coroutines, but not directly in this example

/// <summary>
/// RecoilSystem: A design pattern component for managing weapon recoil effects.
/// This system centralizes the logic for applying and recovering from recoil
/// on specified target transforms (e.g., camera, weapon model).
/// </summary>
/// <remarks>
/// This script should be attached to a GameObject in your scene,
/// typically a player controller or a dedicated 'Recoil Manager' object.
///
/// For best results, the 'Camera Recoil Target' and 'Weapon Recoil Target'
/// transforms should be empty GameObjects that are children of your
/// actual camera and weapon model respectively. This allows the RecoilSystem
/// to apply additive offsets without directly conflicting with other scripts
/// (e.g., a MouseLook script on the main camera or animation on the weapon model).
/// </remarks>
public class RecoilSystem : MonoBehaviour
{
    [Header("Recoil Targets")]
    [Tooltip("The transform to apply camera-specific recoil to (e.g., a Camera Pivot).")]
    [SerializeField] private Transform cameraRecoilTarget;
    [Tooltip("The transform to apply weapon-specific recoil to (e.g., the Weapon Model itself).")]
    [SerializeField] private Transform weaponRecoilTarget;

    [Header("Recoil Parameters")]
    [Tooltip("Maximum rotational recoil values (X: vertical, Y: horizontal, Z: roll).")]
    [SerializeField] private Vector3 maxRecoilRotation = new Vector3(-10f, 5f, 5f);
    [Tooltip("Maximum positional recoil offset (e.g., weapon moving back).")]
    [SerializeField] private Vector3 maxRecoilPositionOffset = new Vector3(0f, 0f, -0.1f);

    [Tooltip("How quickly the recoil 'kicks in' (higher value = snappier kick).")]
    [SerializeField] private float snappiness = 15f;
    [Tooltip("How quickly the recoil 'recovers' back to zero (higher value = faster recovery).")]
    [SerializeField] private float recoverySpeed = 8f;
    [Tooltip("The duration of the initial kick phase before recovery fully takes over.")]
    [SerializeField] private float recoilKickDuration = 0.1f;

    [Header("Recoil Modifiers (per shot)")]
    [Tooltip("Multiplier for X (vertical) recoil applied per shot.")]
    [SerializeField] private float verticalRecoilMultiplier = 1f;
    [Tooltip("Multiplier for Y (horizontal) recoil applied per shot (can be negative for left kick).")]
    [SerializeField] private float horizontalRecoilMultiplier = 1f;
    [Tooltip("Multiplier for Z (roll) recoil applied per shot.")]
    [SerializeField] private float rollRecoilMultiplier = 1f;
    [Tooltip("Multiplier for positional (Z) recoil applied per shot.")]
    [SerializeField] private float positionalRecoilMultiplier = 1f;

    // --- Private Internal State ---
    private Vector3 _currentRecoilRotation = Vector3.zero; // The current accumulated recoil rotation
    private Vector3 _currentRecoilPositionOffset = Vector3.zero; // The current accumulated recoil position offset

    private Vector3 _targetRecoilRotation = Vector3.zero; // The rotation we're lerping towards during the kick phase
    private Vector3 _targetRecoilPositionOffset = Vector3.zero; // The position we're lerping towards during the kick phase

    private float _recoilTimer; // Timer to track the recoil kick/recovery phase

    // Cache initial transforms if you need to revert to them,
    // but for additive recoil on dedicated targets, it's less critical.
    // Here, we just set the localRotation/localPosition of the targets
    // as offsets, so they implicitly recover to (0,0,0) local values.

    /// <summary>
    /// Update is called once per frame. It handles the continuous
    /// application and recovery of recoil effects.
    /// </summary>
    void Update()
    {
        // --- Recoil Kick Phase ---
        // If the recoil timer is still within the kick duration, we are in the initial kick phase.
        if (_recoilTimer < recoilKickDuration)
        {
            // Smoothly interpolate the current recoil towards the target recoil values.
            // This creates the immediate 'kick' effect.
            _currentRecoilRotation = Vector3.Lerp(_currentRecoilRotation, _targetRecoilRotation, Time.deltaTime * snappiness);
            _currentRecoilPositionOffset = Vector3.Lerp(_currentRecoilPositionOffset, _targetRecoilPositionOffset, Time.deltaTime * snappiness);
        }
        // --- Recoil Recovery Phase ---
        // Once the kick duration is over, we transition to the recovery phase.
        else
        {
            // Smoothly interpolate the current recoil back to zero (the initial state).
            // This creates the 'recovery' effect.
            _currentRecoilRotation = Vector3.Lerp(_currentRecoilRotation, Vector3.zero, Time.deltaTime * recoverySpeed);
            _currentRecoilPositionOffset = Vector3.Lerp(_currentRecoilPositionOffset, Vector3.zero, Time.deltaTime * recoverySpeed);
        }

        // Increment the recoil timer.
        _recoilTimer += Time.deltaTime;

        // --- Apply Recoil to Target Transforms ---
        // The current accumulated recoil is applied as local offsets to the target transforms.
        // This makes the recoil effect additive to any parent transform's movement or rotation.

        // Apply rotational recoil to the camera target.
        if (cameraRecoilTarget != null)
        {
            // Quaternion.Euler converts our Vector3 rotation into a Quaternion.
            // We set the local rotation directly, assuming this target transform's
            // local rotation is solely managed by the RecoilSystem for recoil offsets.
            cameraRecoilTarget.localRotation = Quaternion.Euler(_currentRecoilRotation);
        }

        // Apply both rotational and positional recoil to the weapon model target.
        if (weaponRecoilTarget != null)
        {
            // Rotational recoil for the weapon.
            weaponRecoilTarget.localRotation = Quaternion.Euler(_currentRecoilRotation);
            // Positional recoil for the weapon (e.g., pushing back into the player's hands).
            weaponRecoilTarget.localPosition = _currentRecoilPositionOffset;
        }
    }

    /// <summary>
    /// Public method to trigger a recoil event.
    /// This is called by other scripts (e.g., a Weapon script) when a shot is fired.
    /// </summary>
    public void ApplyRecoil()
    {
        // Reset the recoil timer to restart the kick and recovery process.
        _recoilTimer = 0f;

        // Calculate the actual recoil values for this shot, potentially with randomization.
        // For vertical (X), it's typically always positive (kick up).
        // For horizontal (Y), it can be positive (right) or negative (left) for randomness.
        // For roll (Z), it can also be random.
        
        float recoilX = maxRecoilRotation.x * verticalRecoilMultiplier;
        float recoilY = Random.Range(-maxRecoilRotation.y, maxRecoilRotation.y) * horizontalRecoilMultiplier;
        float recoilZ = Random.Range(-maxRecoilRotation.z, maxRecoilRotation.z) * rollRecoilMultiplier;

        // Update the target recoil rotations and positions for the lerp.
        _targetRecoilRotation += new Vector3(recoilX, recoilY, recoilZ);
        _targetRecoilPositionOffset += maxRecoilPositionOffset * positionalRecoilMultiplier;

        // Clamp the target recoil to prevent excessive accumulation.
        // This ensures recoil doesn't go infinitely high if firing very fast.
        _targetRecoilRotation.x = Mathf.Clamp(_targetRecoilRotation.x, -maxRecoilRotation.x * 2f, 0); // Can only go up (negative X for Unity's common camera setup)
        _targetRecoilRotation.y = Mathf.Clamp(_targetRecoilRotation.y, -maxRecoilRotation.y * 2f, maxRecoilRotation.y * 2f);
        _targetRecoilRotation.z = Mathf.Clamp(_targetRecoilRotation.z, -maxRecoilRotation.z * 2f, maxRecoilRotation.z * 2f);

        _targetRecoilPositionOffset.x = Mathf.Clamp(_targetRecoilPositionOffset.x, -maxRecoilPositionOffset.x * 2f, maxRecoilPositionOffset.x * 2f);
        _targetRecoilPositionOffset.y = Mathf.Clamp(_targetRecoilPositionOffset.y, -maxRecoilPositionOffset.y * 2f, maxRecoilPositionOffset.y * 2f);
        _targetRecoilPositionOffset.z = Mathf.Clamp(_targetRecoilPositionOffset.z, maxRecoilPositionOffset.z * 2f, 0); // Can only go back (negative Z for weapon)
    }

    /// <summary>
    /// Optional: Resets all recoil immediately. Useful for weapon swapping or game state changes.
    /// </summary>
    public void ResetRecoil()
    {
        _currentRecoilRotation = Vector3.zero;
        _currentRecoilPositionOffset = Vector3.zero;
        _targetRecoilRotation = Vector3.zero;
        _targetRecoilPositionOffset = Vector3.zero;
        _recoilTimer = 0f;

        if (cameraRecoilTarget != null)
        {
            cameraRecoilTarget.localRotation = Quaternion.identity;
        }
        if (weaponRecoilTarget != null)
        {
            weaponRecoilTarget.localRotation = Quaternion.identity;
            weaponRecoilTarget.localPosition = Vector3.zero;
        }
    }
}
```

---

## How to Set Up in Unity

1.  **Create an Empty GameObject:** In your scene, create an empty GameObject (e.g., `RecoilManager`). Attach the `RecoilSystem.cs` script to it.
2.  **Player Hierarchy Example:**
    *   **Player Character** (Root for character, handles movement)
        *   **Camera Holder** (Handles overall camera rotation, e.g., Mouse Look X-axis)
            *   **Camera Pivot** (Handles camera vertical rotation, e.g., Mouse Look Y-axis)
                *   **Recoil Camera Target** (EMPTY GameObject, child of `Camera Pivot`)
                    *   **Main Camera** (Actual Camera component)
        *   **Weapon Holder** (Holds the currently equipped weapon)
            *   **Recoil Weapon Target** (EMPTY GameObject, child of `Weapon Holder`)
                *   **Weapon Model** (Your 3D weapon mesh, animations, etc.)

3.  **Assign Targets in Inspector:**
    *   Drag your `Recoil Camera Target` GameObject into the `Camera Recoil Target` slot on the `RecoilSystem` component.
    *   Drag your `Recoil Weapon Target` GameObject into the `Weapon Recoil Target` slot on the `RecoilSystem` component.
    *   Adjust the `Recoil Parameters` to your liking.

## Example Usage: `Weapon.cs`

This is a simplified `Weapon` script demonstrating how it interacts with the `RecoilSystem`.

```csharp
using UnityEngine;

/// <summary>
/// Example Weapon script demonstrating how to trigger the RecoilSystem.
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.1f; // Seconds between shots
    [SerializeField] private int ammoCount = 30;
    [SerializeField] private GameObject projectilePrefab; // Example for shooting
    [SerializeField] private Transform firePoint; // Where projectiles spawn

    private float _nextFireTime;
    private RecoilSystem _recoilSystem; // Reference to the RecoilSystem

    void Awake()
    {
        // Find the RecoilSystem in the scene.
        // It's common for there to be only one RecoilSystem per player.
        _recoilSystem = FindObjectOfType<RecoilSystem>();
        if (_recoilSystem == null)
        {
            Debug.LogError("RecoilSystem not found in the scene! Please ensure one is present.", this);
        }
    }

    void Update()
    {
        // Example of firing the weapon
        if (Input.GetButton("Fire1") && Time.time >= _nextFireTime)
        {
            Fire();
        }
    }

    void Fire()
    {
        if (ammoCount <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        // --- Core interaction with RecoilSystem ---
        // 1. Tell the RecoilSystem to apply recoil.
        if (_recoilSystem != null)
        {
            _recoilSystem.ApplyRecoil();
        }
        // ----------------------------------------

        // Handle weapon specific logic (e.g., spawning a projectile)
        ammoCount--;
        _nextFireTime = Time.time + fireRate;
        Debug.Log("Shot fired! Ammo left: " + ammoCount);

        if (projectilePrefab != null && firePoint != null)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        }

        // Play sound, animation, particle effects etc.
    }

    public void Reload()
    {
        // Example reload logic
        ammoCount = 30;
        Debug.Log("Reloaded!");
        // If necessary, also reset recoil here (e.g., if reloading should calm the weapon)
        if (_recoilSystem != null)
        {
            _recoilSystem.ResetRecoil();
        }
    }
}
```

---

## Explanation of the Pattern and Best Practices:

1.  **Separation of Concerns:** The `RecoilSystem` doesn't care *who* tells it to recoil, only *that* it needs to recoil. The `Weapon` script doesn't care *how* recoil is applied, only *that* it should happen when fired. This makes both modules more independent and easier to manage or swap out.
2.  **Additive Recoil Targets:** The most robust way to implement recoil is to apply it to dedicated "recoil targets" (empty GameObjects) that are children of your main camera and weapon model.
    *   **Camera Recoil:** Your mouse look script might directly rotate the main camera or its parent. If `RecoilSystem` also tries to directly rotate the same camera, they'll fight. By using a `Recoil Camera Target` (child of the mouse-controlled pivot), the `RecoilSystem` only applies local offsets, which are then added to the parent's base rotation.
    *   **Weapon Recoil:** Similar to the camera, weapon animations or other scripts might manipulate the weapon model's transform. Applying recoil to a `Recoil Weapon Target` (child of the main weapon holder/model) ensures it acts as an additive "kick" on top of existing animations or positions.
3.  **Lerp for Smoothness:** Using `Vector3.Lerp` and `Quaternion.Lerp` (implicitly used by `Quaternion.Euler` on the `_currentRecoilRotation`) is crucial for creating smooth, natural-looking recoil and recovery. Hard snaps would feel unnatural.
4.  **Configurable Parameters:** Exposing `snappiness`, `recoverySpeed`, `maxRecoilRotation`, etc., as `[SerializeField]` variables allows game designers to easily tweak the feel of each weapon without touching code.
5.  **Accumulation & Clamping:** The `_targetRecoilRotation` and `_targetRecoilPositionOffset` accumulate over multiple shots (especially with automatic weapons). Clamping these values (`Mathf.Clamp`) prevents the recoil from becoming excessively high and uncontrollable if the player fires too rapidly.
6.  **FindObjectOfType (for simplicity):** In this example, `Weapon.cs` uses `FindObjectOfType<RecoilSystem>()`. For more complex projects, consider:
    *   **Dependency Injection:** Pass the `RecoilSystem` reference to the `Weapon` script through a manager or a dedicated setup method.
    *   **Singleton Pattern:** If you're certain there will only ever be one `RecoilSystem` in the entire game (e.g., only one player), you could make it a singleton, but be cautious with singletons as they can introduce tight coupling.
    *   **Event System:** The `RecoilSystem` could subscribe to a "OnWeaponFired" event, and weapons would simply publish this event.

This `RecoilSystem` provides a robust, flexible, and educational example of managing weapon recoil in Unity, adhering to good design principles.