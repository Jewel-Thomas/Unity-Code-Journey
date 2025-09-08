// Unity Design Pattern Example: BillboardSystem
// This script demonstrates the BillboardSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The Billboard System is a common design pattern in 3D graphics and game development, particularly useful in Unity. Its core purpose is to ensure that a 3D object always "faces" the camera, regardless of the camera's position or rotation. This is frequently used for:

*   **UI Elements in 3D Space:** Health bars, name tags, warning signs that should always be readable.
*   **Sprite-based Assets:** Trees, bushes, smoke, fire, or other particle effects that are rendered as flat 2D sprites but need to appear volumetric by always facing the viewer.
*   **Impostors:** Low-detail representations of complex objects used at a distance to save performance, which are essentially billboards.

This example provides a flexible and practical `BillboardSystem` component for Unity, allowing you to choose between fully aligning with the camera or just rotating around the Y-axis.

---

```csharp
using UnityEngine; // Required for all Unity-specific classes like MonoBehaviour, Transform, Camera, etc.
using System.Collections; // Not strictly needed for this specific script, but often useful.

/// <summary>
///     The BillboardSystem design pattern ensures a 3D object always faces the camera.
///     This script provides a practical Unity implementation, allowing objects to
///     either fully align with the camera's orientation or only rotate around their Y-axis
///     to face the camera horizontally.
/// </summary>
/// <remarks>
///     This pattern is crucial for:
///     -   **UI in 3D space:** Health bars, name tags, indicators that must always be readable.
///     -   **Sprite-based effects:** Fire, smoke, trees (impostors), or other particle effects
///         that are 2D but need to appear volumetric by orienting towards the viewer.
///     -   **Performance optimization:** Using low-poly/sprite impostors for distant objects.
/// </remarks>
[AddComponentMenu("Rendering/Billboard System")] // Adds this script to the "Rendering" submenu in the Add Component menu
public class BillboardSystem : MonoBehaviour
{
    // =====================================================================================
    // ENUMS
    // =====================================================================================

    /// <summary>
    ///     Defines the different modes for how the billboard should face the camera.
    /// </summary>
    public enum BillboardMode
    {
        /// <summary>
        ///     The object will fully align its rotation with the camera, making it perfectly
        ///     parallel to the screen plane. This is ideal for UI elements like health bars.
        ///     Its local 'up' vector will match the camera's 'up' vector, and its 'forward'
        ///     will point directly at (or away from) the camera's forward.
        /// </summary>
        FullRotation,

        /// <summary>
        ///     The object will only rotate around its Y-axis (vertical axis) to face the camera.
        ///     Its X and Z axes will remain level with the world's horizontal plane. This is
        ///     commonly used for sprite-based trees or foliage where you don't want the object
        ///     to tilt up or down with the camera's pitch.
        /// </summary>
        YAxisOnly
    }

    // =====================================================================================
    // SERIALIZED FIELDS
    // =====================================================================================

    [Header("Billboard Settings")]
    [Tooltip("The camera this object should always face. If left null, Camera.main will be used.")]
    [SerializeField]
    private Camera targetCamera;

    [Tooltip("Determines how the billboard should orient itself relative to the camera.")]
    [SerializeField]
    private BillboardMode mode = BillboardMode.FullRotation;

    // =====================================================================================
    // PRIVATE VARIABLES
    // =====================================================================================

    private Transform _cameraTransform; // Cached reference to the target camera's transform for performance.

    // =====================================================================================
    // UNITY LIFECYCLE METHODS
    // =====================================================================================

    /// <summary>
    ///     Called when the script instance is being loaded.
    ///     Used to initialize references and ensure a camera is assigned.
    /// </summary>
    private void Awake()
    {
        // If targetCamera is not explicitly set in the Inspector, try to find the main camera.
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            // If still null, log a warning, as the billboard won't function without a camera.
            if (targetCamera == null)
            {
                Debug.LogWarning($"BillboardSystem on {gameObject.name}: No target camera assigned and Camera.main not found. " +
                                 "Please assign a camera in the Inspector or ensure a camera is tagged as 'MainCamera'. " +
                                 "This billboard will not function.", this);
                enabled = false; // Disable the script if no camera is found.
                return;
            }
        }

        // Cache the camera's transform for direct access, avoiding repeated .transform calls.
        _cameraTransform = targetCamera.transform;
    }

    /// <summary>
    ///     LateUpdate is called after all Update functions have been called.
    ///     This is ideal for billboard systems because it ensures the camera has finished
    ///     its movement and rotation for the current frame before the billboard attempts
    ///     to face it. This prevents "jittering" or one-frame lag.
    /// </summary>
    private void LateUpdate()
    {
        // If for any reason the camera reference becomes null (e.g., camera destroyed),
        // stop trying to billboard and disable the component.
        if (_cameraTransform == null)
        {
            Debug.LogWarning($"BillboardSystem on {gameObject.name}: Target camera transform is null. Disabling billboard.", this);
            enabled = false;
            return;
        }

        switch (mode)
        {
            case BillboardMode.FullRotation:
                // FullRotation mode: Make the object's local 'forward' (Z-axis) point away from the camera's 'forward'.
                // This effectively makes the object's X-Y plane (the visual "face" of a sprite) parallel
                // to the camera's X-Y plane (the screen).
                // The 'up' vector for the object will align with the camera's 'up' vector.
                // This is ideal for UI elements that should always be perfectly flat to the screen.
                transform.rotation = Quaternion.LookRotation(-_cameraTransform.forward, _cameraTransform.up);
                break;

            case BillboardMode.YAxisOnly:
                // YAxisOnly mode: Calculate the direction from the object to the camera.
                // Then, project this direction onto the horizontal plane by zeroing out the Y-component.
                // Finally, create a rotation that makes the object's 'forward' (Z-axis) point
                // along this horizontal direction. The object's 'up' (Y-axis) remains globally up.
                // This is ideal for sprite-based trees or other objects that should not pitch up or down.
                Vector3 directionToCamera = _cameraTransform.position - transform.position;
                directionToCamera.y = 0f; // Ignore vertical difference, only rotate horizontally.

                // If the camera and object are at the exact same horizontal position, 'directionToCamera' might be zero.
                // Avoid creating a zero-length rotation, which can cause errors.
                if (directionToCamera == Vector3.zero)
                {
                    return;
                }

                transform.rotation = Quaternion.LookRotation(directionToCamera.normalized);
                break;
        }
    }
}

/*
// =====================================================================================
// EXAMPLE USAGE IN UNITY PROJECT
// =====================================================================================

// To use the BillboardSystem:

// 1. Create a new C# script named 'BillboardSystem.cs' (or copy the above code into it).
// 2. Save the script in your Unity project (e.g., in an 'Assets/Scripts/Patterns' folder).

// --- Example 1: Health Bar (Full Rotation) ---
// 1. Create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
// 2. Rename it to "PlayerHealthBar".
// 3. Add a 3D TextMeshPro or a Plane with a UI Texture as a child of "PlayerHealthBar".
//    (If using a Plane, ensure its Z-axis is its normal, and its texture is visible on one side).
// 4. Position this child object slightly above where the player would be.
// 5. Select the "PlayerHealthBar" parent GameObject.
// 6. Click "Add Component" in the Inspector and search for "Billboard System" (or find it under Rendering -> Billboard System).
// 7. In the Inspector for the BillboardSystem component:
//    -  Leave "Target Camera" empty to use Camera.main, or drag your specific camera (e.g., Main Camera) into the slot.
//    -  Set "Mode" to "Full Rotation".
// 8. Run the game. The health bar will always face your camera, no matter where you move or rotate it.

// --- Example 2: Sprite-based Tree (Y-Axis Only) ---
// 1. Create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
// 2. Rename it to "ForestTree".
// 3. Add a Quad or a Plane as a child of "ForestTree".
// 4. Apply a tree sprite texture to a material and assign it to the Quad/Plane.
//    (Ensure the sprite is oriented correctly, typically standing upright along the Y-axis).
// 5. Select the "ForestTree" parent GameObject.
// 6. Click "Add Component" in the Inspector and search for "Billboard System".
// 7. In the Inspector for the BillboardSystem component:
//    -  Leave "Target Camera" empty or assign your camera.
//    -  Set "Mode" to "Y Axis Only".
// 8. Run the game. The tree sprite will always face your camera horizontally, but it won't tilt
//    up or down if you look from above or below, maintaining its upright appearance like a real tree.

// --- Best Practices ---
// -   **Placement:** Attach the BillboardSystem script to the parent GameObject that you want to rotate.
//     Its children can then define the actual visual elements (e.g., a TextMeshPro component, a SpriteRenderer, a Plane).
// -   **Performance:** The calculations are lightweight and occur in LateUpdate, which is optimal.
// -   **Camera:** Ensure your main camera is tagged "MainCamera" in the Inspector if you're not assigning it manually.
// -   **Editor Use:** The script is designed to be easily configurable directly in the Unity Inspector.
*/
```