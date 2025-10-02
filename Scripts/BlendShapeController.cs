// Unity Design Pattern Example: BlendShapeController
// This script demonstrates the BlendShapeController pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the **BlendShapeController design pattern**. This pattern centralizes and abstracts the management of blend shapes on a `SkinnedMeshRenderer`, making your code cleaner, more robust, and easier to maintain.

## BlendShapeController Design Pattern

**Goal:** To provide a single, high-level interface for controlling a character's facial expressions or other blend shape animations, rather than scattering low-level `SkinnedMeshRenderer.SetBlendShapeWeight` calls throughout your codebase.

**Key Benefits:**
1.  **Encapsulation:** All blend shape management logic (mapping names to indices, handling animation, validation) is contained within one class.
2.  **Abstraction:** Provides a more semantic API (e.g., `AnimateBlendShapeTo("MouthSmile", 100f, 0.5f)`) instead of direct index manipulation (`skinnedMeshRenderer.SetBlendShapeWeight(2, 100f)`).
3.  **Maintainability:** If your 3D model's blend shapes change names or order, you only need to update the controller's internal logic, not every script that uses blend shapes.
4.  **Robustness:** Handles cases where a requested blend shape might not exist, preventing runtime errors with clear warnings.
5.  **Readability:** Other scripts interact with blend shapes using clear, descriptive method calls.
6.  **Performance:** Caches blend shape names and their corresponding indices for fast lookups after initialization.

---

### `BlendShapeController.cs`

This script is designed to be dropped onto a GameObject that has a `SkinnedMeshRenderer` component.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The BlendShapeController design pattern centralizes and abstracts the management of blend shapes
/// on a SkinnedMeshRenderer. Instead of directly manipulating blend shape weights via raw indices
/// or hardcoded strings scattered across multiple scripts, this controller provides a single, robust,
/// and user-friendly interface.
/// </summary>
/// <remarks>
/// Attach this script to a GameObject that contains a SkinnedMeshRenderer.
/// It will automatically find and cache all blend shapes available on the renderer's mesh.
/// </remarks>
[RequireComponent(typeof(SkinnedMeshRenderer))] // Ensures a SkinnedMeshRenderer is present on the GameObject
public class BlendShapeController : MonoBehaviour
{
    // --- Public Fields ---
    // The SkinnedMeshRenderer component that this controller will manage.
    // [SerializeField] makes this private field visible and editable in the Unity Inspector,
    // allowing you to assign it manually if needed, or let the script find it automatically.
    [SerializeField]
    private SkinnedMeshRenderer targetRenderer;

    // --- Private Fields ---
    // A dictionary to store the mapping from blend shape names (strings) to their internal indices.
    // This allows for quick lookups by name, which is more readable and less error-prone than using raw indices.
    private Dictionary<string, int> blendShapeNameToIndex = new Dictionary<string, int>();

    // A dictionary to store the current weight of each blend shape.
    // This allows other scripts to query the current state without directly accessing the renderer,
    // and helps in animating from the current state (e.g., from current weight to a target weight).
    private Dictionary<string, float> currentBlendShapeWeights = new Dictionary<string, float>();

    // --- MonoBehaviour Lifecycle Methods ---

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// This is where we perform initial setup and caching of blend shape data.
    /// </summary>
    void Awake()
    {
        InitializeController();
    }

    // --- Initialization ---

    /// <summary>
    /// Initializes the BlendShapeController by finding the SkinnedMeshRenderer
    /// and populating the blend shape name-to-index mapping and current weights.
    /// </summary>
    private void InitializeController()
    {
        // If targetRenderer is not already assigned (e.g., manually in the Inspector),
        // try to get it from the current GameObject.
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SkinnedMeshRenderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("BlendShapeController: No SkinnedMeshRenderer found on this GameObject.", this);
                enabled = false; // Disable the script if no renderer is found to prevent further errors.
                return;
            }
        }

        // Clear previous mappings. This is important if Awake is called multiple times
        // (e.g., in editor resets or hot-reloads) to prevent duplicate entries.
        blendShapeNameToIndex.Clear();
        currentBlendShapeWeights.Clear();

        // Get the mesh associated with the SkinnedMeshRenderer.
        // The blend shape information (names, count) is stored in the sharedMesh.
        Mesh mesh = targetRenderer.sharedMesh;

        if (mesh == null)
        {
            Debug.LogError("BlendShapeController: SkinnedMeshRenderer has no sharedMesh assigned.", this);
            enabled = false;
            return;
        }

        // Iterate through all blend shapes available on the mesh.
        // For each blend shape, store its name and index for quick lookup.
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string blendShapeName = mesh.GetBlendShapeName(i);
            blendShapeNameToIndex.Add(blendShapeName, i);
            // Initialize current weight by reading the actual weight from the renderer.
            // This ensures the controller starts with the current state of the model.
            currentBlendShapeWeights.Add(blendShapeName, targetRenderer.GetBlendShapeWeight(i));
        }

        Debug.Log($"BlendShapeController initialized for '{gameObject.name}' with {mesh.blendShapeCount} blend shapes.");
    }

    // --- Public API for Blend Shape Control ---

    /// <summary>
    /// Sets the weight of a specific blend shape. This is the core method for direct control.
    /// Weights are clamped between 0 and 100, as per Unity's SkinnedMeshRenderer requirements.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape to set (e.g., "MouthSmile", "EyeBlink").</param>
    /// <param name="weight">The target weight for the blend shape, clamped between 0 and 100.</param>
    public void SetBlendShapeWeight(string blendShapeName, float weight)
    {
        // Clamp the weight to ensure it stays within the valid range [0, 100].
        weight = Mathf.Clamp(weight, 0f, 100f);

        // Try to get the internal index of the blend shape using our cached dictionary.
        if (TryGetBlendShapeIndex(blendShapeName, out int index))
        {
            // Set the blend shape weight on the SkinnedMeshRenderer.
            targetRenderer.SetBlendShapeWeight(index, weight);
            // Update the controller's internal record of the current weight.
            currentBlendShapeWeights[blendShapeName] = weight;
        }
    }

    /// <summary>
    /// Gets the current weight of a specific blend shape as managed by this controller.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape to query.</param>
    /// <returns>The current weight (0-100) or 0 if the blend shape is not found.</returns>
    public float GetBlendShapeWeight(string blendShapeName)
    {
        if (currentBlendShapeWeights.TryGetValue(blendShapeName, out float weight))
        {
            return weight;
        }

        // If the blend shape is not in our dictionary, it doesn't exist on the mesh.
        Debug.LogWarning($"BlendShapeController: Blend shape '{blendShapeName}' not found in current weights.");
        return 0f; // Return 0 if the blend shape doesn't exist.
    }

    /// <summary>
    /// Resets all blend shapes managed by this controller to a weight of 0.
    /// Useful for returning a character to a neutral expression.
    /// </summary>
    public void ResetAllBlendShapes()
    {
        // Iterate through all known blend shapes and set their weights to 0.
        foreach (var entry in blendShapeNameToIndex)
        {
            targetRenderer.SetBlendShapeWeight(entry.Value, 0f);
            currentBlendShapeWeights[entry.Key] = 0f; // Update internal state
        }
        Debug.Log("BlendShapeController: All blend shapes reset to 0.");
    }

    /// <summary>
    /// Animates a specific blend shape from its current weight to a target weight over a given duration.
    /// This uses a Coroutine for smooth, frame-rate independent animation.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape to animate.</param>
    /// <param name="targetWeight">The final weight (0-100) for the blend shape.</param>
    /// <param name="duration">The time in seconds over which the animation should occur.</param>
    /// <returns>The Coroutine object, allowing you to stop it if needed, or null if the blend shape was not found.</returns>
    public Coroutine AnimateBlendShapeTo(string blendShapeName, float targetWeight, float duration)
    {
        if (TryGetBlendShapeIndex(blendShapeName, out int index))
        {
            // It's often good practice to stop any existing animation on this *specific* blend shape
            // to avoid conflicts. For simplicity, this example stops ALL coroutines on this component.
            // For more complex systems, you might store references to active coroutines in a dictionary
            // keyed by blendShapeName and stop only the relevant one.
            StopAllCoroutines(); 
            
            // Start the animation coroutine from the blend shape's current weight.
            return StartCoroutine(AnimateBlendShapeCoroutine(blendShapeName, GetBlendShapeWeight(blendShapeName), targetWeight, duration));
        }
        return null; // Return null if the blend shape doesn't exist.
    }

    // --- Private Helper Methods ---

    /// <summary>
    /// Internal helper method to safely get the blend shape index from its name.
    /// Provides error logging if the blend shape is not found.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape.</param>
    /// <param name="index">Output parameter for the blend shape index.</param>
    /// <returns>True if the blend shape was found, false otherwise.</returns>
    private bool TryGetBlendShapeIndex(string blendShapeName, out int index)
    {
        if (blendShapeNameToIndex.TryGetValue(blendShapeName, out index))
        {
            return true;
        }

        // Log a warning if the blend shape name is not found, helping to debug typos or missing blend shapes.
        Debug.LogWarning($"BlendShapeController: Blend shape '{blendShapeName}' not found on mesh '{targetRenderer.sharedMesh.name}'. " +
                         "Please check the blend shape name for typos and ensure it exists in your 3D model.", this);
        return false;
    }

    /// <summary>
    /// Coroutine to smoothly interpolate a blend shape's weight over time.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape being animated.</param>
    /// <param name="startWeight">The initial weight of the blend shape.</param>
    /// <param name="targetWeight">The final desired weight of the blend shape.</param>
    /// <param name="duration">The total time in seconds for the animation.</param>
    private IEnumerator AnimateBlendShapeCoroutine(string blendShapeName, float startWeight, float targetWeight, float duration)
    {
        float timer = 0f;
        // Clamp target weight to ensure it stays within the valid range [0, 100].
        targetWeight = Mathf.Clamp(targetWeight, 0f, 100f); 

        // Continue animating as long as the timer is less than the duration.
        while (timer < duration)
        {
            timer += Time.deltaTime; // Increment timer by the time passed since last frame.
            float progress = timer / duration; // Calculate animation progress (0 to 1).
            
            // Use Mathf.Lerp for smooth interpolation between the start and target weights.
            float currentWeight = Mathf.Lerp(startWeight, targetWeight, progress);
            SetBlendShapeWeight(blendShapeName, currentWeight); // Apply the interpolated weight.
            yield return null; // Wait for the next frame before continuing the loop.
        }

        // Ensure the final weight is set precisely to the target weight to avoid floating-point inaccuracies.
        SetBlendShapeWeight(blendShapeName, targetWeight);
    }
}
```

---

### Example Usage Script (`BlendShapeTester.cs`)

This script demonstrates how another script would interact with the `BlendShapeController`. Create a new C# script named `BlendShapeTester` and add the following code.

```csharp
using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// This script demonstrates how to use the BlendShapeController to manipulate
/// blend shapes on a character. It provides examples for direct setting,
/// animating, and resetting blend shapes, with Inspector controls for testing.
/// </summary>
public class BlendShapeTester : MonoBehaviour
{
    // A reference to the BlendShapeController on our character.
    // Assign this in the Unity Inspector.
    [SerializeField]
    private BlendShapeController blendShapeController;

    [Header("Manual Blend Shape Control")]
    [Tooltip("Enter the exact name of a blend shape from your 3D model.")]
    [SerializeField] private string blendShapeToControl = "MouthSmile"; // Example: "MouthSmile", "EyeBlink", "BrowRaise"
    [Range(0, 100)]
    [Tooltip("Adjust this slider to manually set the weight of the blend shape.")]
    [SerializeField] private float manualWeight = 0f;

    [Header("Animation Test")]
    [Tooltip("Enter the exact name of a blend shape to animate.")]
    [SerializeField] private string animBlendShapeName = "MouthOpen";
    [Range(0, 100)]
    [Tooltip("The target weight for the animation.")]
    [SerializeField] private float animTargetWeight = 100f;
    [Tooltip("The duration of the animation in seconds.")]
    [SerializeField] private float animDuration = 0.5f;

    // --- MonoBehaviour Lifecycle ---

    void Start()
    {
        if (blendShapeController == null)
        {
            Debug.LogError("BlendShapeTester: BlendShapeController is not assigned!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// OnValidate is called in the editor when the script is loaded or a value is changed in the Inspector.
    /// This allows us to see immediate feedback when adjusting 'manualWeight' in play mode.
    /// </summary>
    void OnValidate()
    {
        if (blendShapeController != null && Application.isPlaying)
        {
            blendShapeController.SetBlendShapeWeight(blendShapeToControl, manualWeight);
        }
    }

    // --- Public Test Methods (Can be called from UI buttons, other scripts, or Context Menu) ---

    /// <summary>
    /// Sets a basic "Happy" expression using multiple blend shapes.
    /// </summary>
    [ContextMenu("Test: Set Happy Expression")]
    public void SetHappyExpression()
    {
        if (blendShapeController == null) return;

        Debug.Log("Setting Happy Expression...");
        blendShapeController.AnimateBlendShapeTo("MouthSmile", 100f, 0.3f); // Assuming "MouthSmile" blend shape exists
        blendShapeController.AnimateBlendShapeTo("EyeWide", 30f, 0.3f); // Example for slightly wider eyes
        blendShapeController.AnimateBlendShapeTo("CheekPuff", 20f, 0.3f); // Example for slightly raised cheeks
        // Add more blend shapes for a complex expression as needed
    }

    /// <summary>
    /// Triggers an animation for a single blend shape based on Inspector settings.
    /// </summary>
    [ContextMenu("Test: Animate Single Blend Shape")]
    public void AnimateSingleBlendShape()
    {
        if (blendShapeController == null) return;

        Debug.Log($"Animating '{animBlendShapeName}' to {animTargetWeight} over {animDuration}s.");
        blendShapeController.AnimateBlendShapeTo(animBlendShapeName, animTargetWeight, animDuration);
    }

    /// <summary>
    /// Initiates a continuous blinking animation for an eye blend shape.
    /// </summary>
    [ContextMenu("Test: Start Blinking")]
    public void StartBlinkingAnimation()
    {
        if (blendShapeController == null) return;
        Debug.Log("Starting blinking animation...");
        StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// Resets all blend shapes to 0.
    /// </summary>
    [ContextMenu("Test: Reset All Blend Shapes")]
    public void ResetCharacterFace()
    {
        if (blendShapeController == null) return;
        Debug.Log("Resetting all blend shapes.");
        blendShapeController.ResetAllBlendShapes();
    }

    // --- Private Coroutines ---

    /// <summary>
    /// A simple coroutine to simulate continuous blinking.
    /// </summary>
    private IEnumerator BlinkRoutine()
    {
        while (true) // Loop indefinitely for continuous blinking
        {
            // Wait for a random period before the next blink
            yield return new WaitForSeconds(Random.Range(2.0f, 5.0f));

            // Close eyes quickly
            blendShapeController.AnimateBlendShapeTo("EyeClosed", 100f, 0.1f); // Assuming "EyeClosed" blend shape
            yield return new WaitForSeconds(0.15f); // Keep eyes closed briefly

            // Open eyes quickly
            blendShapeController.AnimateBlendShapeTo("EyeClosed", 0f, 0.15f);
        }
    }
}
```

---

### How to Use in Unity:

1.  **Prepare your 3D Model:**
    *   Ensure your 3D model (e.g., character head) has `Blend Shapes` (also known as morph targets, shape keys) set up in your 3D software (Blender, Maya, 3ds Max).
    *   Give your blend shapes meaningful names (e.g., "MouthSmile", "EyeClosed", "BrowRaise", "MouthOpen"). These names are crucial as the C# script uses them to identify blend shapes.
    *   Export your model as an FBX file into your Unity project.

2.  **Import to Unity & Scene Setup:**
    *   Drag your FBX model into your Unity scene.
    *   Select the GameObject that contains the `SkinnedMeshRenderer` component of your model (this is usually the root of your imported character or a child object).

3.  **Attach `BlendShapeController.cs`:**
    *   Add the `BlendShapeController.cs` script to the GameObject you selected in step 2.
    *   The `RequireComponent(typeof(SkinnedMeshRenderer))` attribute will ensure a `SkinnedMeshRenderer` is present.
    *   The script's `Awake()` method will automatically try to find the `SkinnedMeshRenderer` on the same GameObject. If you have multiple `SkinnedMeshRenderer` components or it's on a child, you can manually drag the correct one to the `Target Renderer` field in the Inspector.

4.  **Attach `BlendShapeTester.cs` (for demonstration):**
    *   Create an empty GameObject in your scene (or attach it to your character's root).
    *   Add the `BlendShapeTester.cs` script to this GameObject.
    *   In the Inspector for `BlendShapeTester`, drag the `BlendShapeController` component (from your character GameObject) into the `Blend Shape Controller` field.

5.  **Run and Test:**
    *   Play the scene.
    *   In the Inspector for the `BlendShapeTester` GameObject:
        *   **Manual Control:** Type a blend shape name (e.g., "MouthSmile") into `Blend Shape To Control` and adjust the `Manual Weight` slider. You should see your character's blend shape change in real-time.
        *   **Animation Test:** Fill in `Anim Blend Shape Name`, `Anim Target Weight`, and `Anim Duration`. Right-click on the `BlendShapeTester` component in the Inspector and select "Test: Animate Single Blend Shape".
        *   **High-level functions:** Use the "Test: Set Happy Expression", "Test: Start Blinking", and "Test: Reset All Blend Shapes" options from the right-click Context Menu to see the controller manage multiple blend shapes or complex behaviors.

This setup provides a robust and educational example of the BlendShapeController pattern, ready for use and extension in your Unity projects.