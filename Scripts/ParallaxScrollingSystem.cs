// Unity Design Pattern Example: ParallaxScrollingSystem
// This script demonstrates the ParallaxScrollingSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'Parallax Scrolling System' is a design pattern used in game development to create an illusion of depth by having background and foreground layers move at different speeds relative to the camera. Layers further away from the camera move slower, while layers closer to the camera move faster, mimicking how our eyes perceive objects at varying distances.

This C# Unity script provides a practical and educational implementation of the Parallax Scrolling System pattern.

---

### **`ParallaxScrollingSystem.cs`**

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a single layer within the Parallax Scrolling System.
/// </summary>
[System.Serializable] // Makes this struct visible and editable in the Unity Inspector
public struct ParallaxLayer
{
    [Tooltip("The Transform of the GameObject that represents this parallax layer.")]
    public Transform layerTransform;

    [Tooltip("How much this layer moves relative to the camera's movement. " +
             "0.0 = completely stationary (infinite distance). " +
             "0.5 = moves at half camera speed (far distance). " +
             "1.0 = moves at camera speed (no parallax). " +
             "Values > 1.0 can be used for foreground elements that move faster than the camera.")]
    [Range(0f, 2f)] // A reasonable range for parallax factors
    public float parallaxFactor;

    [Tooltip("The width of the sprite/texture in world units for horizontal tiling. " +
             "Set to 0 if this layer should not tile horizontally or if it's a fixed background.")]
    public float tilingFactorX;

    [Tooltip("The height of the sprite/texture in world units for vertical tiling. " +
             "Set to 0 if this layer should not tile vertically or if it's a fixed background.")]
    public float tilingFactorY;

    [Tooltip("If true, this layer will scroll horizontally with camera movement.")]
    public bool horizontalScrolling;

    [Tooltip("If true, this layer will scroll vertically with camera movement.")]
    public bool verticalScrolling;
}

/// <summary>
/// Implements the Parallax Scrolling System design pattern in Unity.
/// Manages multiple parallax layers, making them move at different speeds relative to the camera,
/// creating an illusion of depth. It also supports infinite scrolling (tiling) for background elements.
/// </summary>
public class ParallaxScrollingSystem : MonoBehaviour
{
    [Tooltip("The camera that the parallax layers will follow. If null, Camera.main will be used.")]
    public Camera targetCamera;

    [Tooltip("A list of all parallax layers to be managed by this system.")]
    public List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

    private Vector3 _cameraInitialPosition;
    private List<Vector3> _initialLayerPositions; // Stores the starting world position of each layer

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize the system by storing initial positions.
    /// </summary>
    void Awake()
    {
        // If no camera is explicitly assigned, try to find the main camera.
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("ParallaxScrollingSystem: No target camera assigned and Camera.main not found. " +
                               "Please assign a camera in the Inspector or ensure one is tagged 'MainCamera'.");
                enabled = false; // Disable the script if no camera is found
                return;
            }
        }

        // Store the camera's initial position. This is our reference point.
        _cameraInitialPosition = targetCamera.transform.position;

        // Initialize the list to store each layer's initial position.
        _initialLayerPositions = new List<Vector3>();
        foreach (ParallaxLayer layer in parallaxLayers)
        {
            if (layer.layerTransform != null)
            {
                _initialLayerPositions.Add(layer.layerTransform.position);
            }
            else
            {
                Debug.LogWarning($"ParallaxScrollingSystem: Layer transform is null for a layer in {gameObject.name}. " +
                                 "This layer will be skipped.");
                _initialLayerPositions.Add(Vector3.zero); // Add a placeholder to maintain list indexing
            }
        }
    }

    /// <summary>
    /// LateUpdate is called once per frame, after all Update functions have been called.
    /// This is ideal for camera-related movements to ensure the camera has already moved for the current frame.
    /// </summary>
    void LateUpdate()
    {
        // Calculate how much the camera has moved relative to its starting position.
        // This is the "camera delta" or "camera offset".
        Vector3 cameraDelta = targetCamera.transform.position - _cameraInitialPosition;

        // Iterate through each parallax layer and update its position.
        for (int i = 0; i < parallaxLayers.Count; i++)
        {
            ParallaxLayer layer = parallaxLayers[i];

            // Skip if the layer's transform is not assigned.
            if (layer.layerTransform == null) continue;

            // Get the stored initial position for this specific layer.
            // We use this as a base, which gets updated for infinite tiling.
            Vector3 currentLayerEffectiveStartPos = _initialLayerPositions[i];
            Vector3 newLayerPosition = currentLayerEffectiveStartPos;

            // --- Horizontal Scrolling Logic ---
            if (layer.horizontalScrolling)
            {
                // Calculate the layer's raw horizontal offset based on camera movement and parallax factor.
                float xOffset = cameraDelta.x * layer.parallaxFactor;
                newLayerPosition.x = currentLayerEffectiveStartPos.x + xOffset;

                // Handle infinite horizontal scrolling (tiling)
                if (layer.tilingFactorX > 0)
                {
                    // Calculate how far the layer has theoretically moved from its original start point.
                    float distanceFromLayerOriginX = newLayerPosition.x - currentLayerEffectiveStartPos.x;

                    // If the layer has moved a distance greater than its tiling factor in either direction,
                    // adjust its effective start position to "wrap" the background.
                    // This makes the background appear continuous.
                    if (distanceFromLayerOriginX > layer.tilingFactorX)
                    {
                        currentLayerEffectiveStartPos.x += layer.tilingFactorX;
                        // Update the stored effective start position for the next frame.
                        _initialLayerPositions[i] = currentLayerEffectiveStartPos;
                        // Recalculate new position based on the adjusted effective start.
                        newLayerPosition.x = currentLayerEffectiveStartPos.x + xOffset;
                    }
                    else if (distanceFromLayerOriginX < -layer.tilingFactorX)
                    {
                        currentLayerEffectiveStartPos.x -= layer.tilingFactorX;
                        _initialLayerPositions[i] = currentLayerEffectiveStartPos;
                        newLayerPosition.x = currentLayerEffectiveStartPos.x + xOffset;
                    }
                }
            }

            // --- Vertical Scrolling Logic ---
            if (layer.verticalScrolling)
            {
                // Calculate the layer's raw vertical offset based on camera movement and parallax factor.
                float yOffset = cameraDelta.y * layer.parallaxFactor;
                newLayerPosition.y = currentLayerEffectiveStartPos.y + yOffset;

                // Handle infinite vertical scrolling (tiling)
                if (layer.tilingFactorY > 0)
                {
                    float distanceFromLayerOriginY = newLayerPosition.y - currentLayerEffectiveStartPos.y;

                    if (distanceFromLayerOriginY > layer.tilingFactorY)
                    {
                        currentLayerEffectiveStartPos.y += layer.tilingFactorY;
                        _initialLayerPositions[i] = currentLayerEffectiveStartPos;
                        newLayerPosition.y = currentLayerEffectiveStartPos.y + yOffset;
                    }
                    else if (distanceFromLayerOriginY < -layer.tilingFactorY)
                    {
                        currentLayerEffectiveStartPos.y -= layer.tilingFactorY;
                        _initialLayerPositions[i] = currentLayerEffectiveStartPos;
                        newLayerPosition.y = currentLayerEffectiveStartPos.y + yOffset;
                    }
                }
            }
            
            // Set the layer's position, preserving Z-axis if not specifically scrolled.
            // The Z position of parallax layers should typically be set manually in the editor
            // to define their visual depth ordering.
            layer.layerTransform.position = new Vector3(newLayerPosition.x, newLayerPosition.y, layer.layerTransform.position.z);
        }
    }

    // Optional: If you want to reset the parallax system (e.g., after a scene transition)
    public void ResetParallaxSystem()
    {
        Awake(); // Re-initialize all positions
    }
}
```

---

### **How the Parallax Scrolling System Pattern Works (Detailed Explanation):**

1.  **Core Idea: Relative Movement:** The fundamental principle is that objects at different depths move at different speeds relative to the camera. Closer objects move more, further objects move less.
    *   **Camera Reference Point:** The system first records the camera's initial position (`_cameraInitialPosition`). This acts as the anchor point for all parallax calculations.
    *   **Layer Reference Point:** Similarly, the initial position of each parallax layer (`_initialLayerPositions`) is recorded. This is crucial for both basic parallax and infinite scrolling.

2.  **Parallax Factor (`parallaxFactor`):**
    *   This is the heart of the depth illusion. It's a multiplier applied to the camera's movement.
    *   **`0.0` (Stationary):** A layer with `parallaxFactor = 0.0` will not move at all, effectively being "infinitely" far away.
    *   **`0.5` (Half Speed):** A layer with `parallaxFactor = 0.5` will move at half the speed of the camera. This creates the illusion of being far away.
    *   **`1.0` (Same Speed):** A layer with `parallaxFactor = 1.0` will move exactly with the camera. This typically means there's no parallax effect relative to the camera for this layer (e.g., UI elements that stick to the camera, or objects at the "player's depth").
    *   **`> 1.0` (Faster Speed):** Values greater than `1.0` can be used for "foreground parallax," where elements appear to move *faster* than the camera, simulating a very close depth, often used for stylistic effects or extreme foreground elements.

3.  **Calculating Layer Movement (`LateUpdate`):**
    *   **Camera Delta:** In `LateUpdate`, the script first calculates `cameraDelta = targetCamera.transform.position - _cameraInitialPosition`. This tells us precisely how much the camera has moved from its starting point in world space.
    *   **Layer Offset:** For each layer, the `xOffset` (or `yOffset`) is calculated as `cameraDelta.x * layer.parallaxFactor`. This scaled offset is then added to the layer's *current effective start position* to get its new desired position.
    *   **`LateUpdate` Choice:** Using `LateUpdate` is a best practice for camera-related logic. `Update` runs before the camera has necessarily finished its movement for the current frame, which could lead to stuttering or lag. `LateUpdate` ensures the camera has settled, providing a smoother parallax effect.

4.  **Infinite Scrolling (Tiling - `tilingFactorX`/`tilingFactorY`):**
    *   **Problem:** If you have a background sprite that's just a single image, it will eventually move off-screen, revealing an empty space.
    *   **Solution:** Infinite scrolling wraps the background around.
    *   **`tilingFactor`:** This property defines the width (or height) of a single "tile" of the background image in world units.
    *   **Wrapping Logic:**
        *   The script tracks the `distanceFromLayerOriginX` (or Y), which is how far the layer's calculated position is from its *current effective starting position*.
        *   If this distance exceeds `tilingFactorX` (or Y) in either positive or negative direction, it means the camera has moved past one full "tile" of the background.
        *   When this happens, the `_initialLayerPositions` for that layer is adjusted (e.g., `_initialLayerPositions[i].x += layer.tilingFactorX`). This effectively "resets" the layer's origin, making it appear as if a new identical tile has seamlessly entered the screen, maintaining a continuous background.
        *   The position is then recalculated based on this *new* effective start position.

5.  **Axis Control (`horizontalScrolling`, `verticalScrolling`):**
    *   These boolean flags allow individual layers to scroll only on the X-axis, only on the Y-axis, or both, providing flexibility for different types of backgrounds (e.g., a static floor texture vs. a horizontally scrolling sky).

### **Example Usage in Unity Project:**

1.  **Create the Script:**
    *   In your Unity project, create a new C# script named `ParallaxScrollingSystem.cs` and copy the code above into it.

2.  **Setup the Parallax Manager:**
    *   Create an empty GameObject in your scene (e.g., rename it to `ParallaxManager`).
    *   Drag and drop the `ParallaxScrollingSystem.cs` script onto this new `ParallaxManager` GameObject.

3.  **Prepare Parallax Layers:**
    *   **Import Sprites:** Import several background/foreground sprites into your Unity project (e.g., `sky.png`, `mountains.png`, `trees.png`, `bushes.png`). Ensure their "Texture Type" is `Sprite (2D and UI)` in the Inspector.
    *   **Create Layer GameObjects:** For each sprite you want to use as a parallax layer:
        *   Create an empty GameObject as a child of `ParallaxManager` (e.g., `Background_Sky`, `Background_Mountains`, `Midground_Trees`, `Foreground_Bushes`).
        *   Add a `SpriteRenderer` component to each of these child GameObjects.
        *   Drag the corresponding sprite from your project into the `Sprite` slot of the `SpriteRenderer`.
        *   **Position and Z-Ordering:** Carefully position these layers on the Y-axis (and X if needed) to create your scene. Most importantly, adjust their **Z-axis** position (e.g., `Sky` at `Z=10`, `Mountains` at `Z=5`, `Trees` at `Z=0`, `Bushes` at `Z=-5`). Layers with higher Z values will appear "behind" layers with lower Z values.
        *   **Tiling (if applicable):** If a sprite is meant to tile (like a continuous ground or sky), make sure its sprite import settings allow tiling (e.g., "Wrap Mode" to `Repeat` if using a `RawImage` or other custom texture setup; for `SpriteRenderer`, you'd typically have multiple identical sprites side-by-side and let the script manage their position). The `tilingFactorX/Y` parameter is crucial here. To get the world unit size of a sprite, you can usually look at its `SpriteRenderer`'s `sprite.bounds.size.x` or `sprite.bounds.size.y`.

4.  **Configure `ParallaxManager` in the Inspector:**
    *   Select your `ParallaxManager` GameObject in the Hierarchy.
    *   In the Inspector, find the `Parallax Scrolling System` component.
    *   **Target Camera:** Drag your `Main Camera` (or the specific camera you want to use) into the `Target Camera` slot.
    *   **Parallax Layers:**
        *   Expand the `Parallax Layers` list.
        *   Set the `Size` to the number of layers you created (e.g., 4).
        *   For each element in the list:
            *   **Layer Transform:** Drag the corresponding child GameObject's `Transform` (e.g., `Background_Sky`'s Transform) from the Hierarchy into this slot.
            *   **Parallax Factor:** Assign a value:
                *   `Background_Sky`: `0.1` (moves very slowly)
                *   `Background_Mountains`: `0.3`
                *   `Midground_Trees`: `0.6`
                *   `Foreground_Bushes`: `0.9` (moves almost with the camera, feels close)
            *   **Tiling Factor X/Y:** If a layer should tile (e.g., the sky, ground), input its width/height in world units. If using a `SpriteRenderer` with a single sprite:
                *   Select the layer GameObject (e.g., `Background_Sky`).
                *   In its Inspector, find the `Sprite Renderer` component.
                *   Note down `Sprite > Bounds > Size > X` for `tilingFactorX` and `Y` for `tilingFactorY`.
                *   Set to `0` if it's a fixed background that doesn't need to tile.
            *   **Horizontal/Vertical Scrolling:** Check `Horizontal Scrolling` and/or `Vertical Scrolling` based on how you want the layer to move. For most side-scrollers, you'd check `Horizontal Scrolling` for backgrounds.

5.  **Run Your Game:**
    *   Move your `Main Camera` (or the target camera you assigned) in the scene, and you'll observe the parallax effect! The layers will scroll at different speeds, creating a convincing illusion of depth.