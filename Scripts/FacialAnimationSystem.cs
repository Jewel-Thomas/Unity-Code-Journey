// Unity Design Pattern Example: FacialAnimationSystem
// This script demonstrates the FacialAnimationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates a robust and practical **FacialAnimationSystem** design pattern. It's structured to be modular, data-driven, and easy to use, separating concerns between expression definition, low-level animation control, and high-level system management.

The pattern consists of three main parts:

1.  **`FacialExpression` (ScriptableObject):** Defines what an expression *is* (data).
2.  **`FacialAnimationController` (MonoBehaviour):** Manages *how* expressions are applied to a `SkinnedMeshRenderer` (low-level driver).
3.  **`FacialAnimationSystem` (MonoBehaviour):** The central manager that orchestrates the `FacialAnimationController` and provides a high-level API for playing expressions (the 'Facade' or 'Manager' part of the pattern).

---

### **1. `FacialExpression.cs`**
*This ScriptableObject defines a single facial expression as a reusable asset. It decouples the 'what' (the desired blend shape weights and timing) from the 'how' (its application to a mesh).*

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that defines a single facial expression.
/// This acts as the data-driven component of the Facial Animation System pattern.
/// Each instance of this asset represents a specific facial pose (e.g., "Happy", "Sad", "Angry").
/// </summary>
[CreateAssetMenu(fileName = "NewFacialExpression", menuName = "Facial Animation/Facial Expression")]
public class FacialExpression : ScriptableObject
{
    /// <summary>
    /// Defines a single blend shape and its target weight for this expression.
    /// </summary>
    [System.Serializable]
    public struct BlendShapeWeight
    {
        [Tooltip("The exact name of the blend shape as it appears in the SkinnedMeshRenderer's mesh.")]
        public string blendShapeName; 
        [Range(0, 100)]
        [Tooltip("The target weight (0-100) for this blend shape when the expression is active.")]
        public float weight;         
    }

    [Header("Expression Definition")]
    [Tooltip("A descriptive name for this expression (e.g., 'Happy', 'Surprise').")]
    public string expressionName;
    [Tooltip("The list of blend shapes and their target weights that constitute this expression.")]
    public List<BlendShapeWeight> blendShapeWeights = new List<BlendShapeWeight>();

    [Header("Animation Properties (Defaults)")]
    [Tooltip("The default duration for this expression to be held active before blending out.")]
    public float defaultDuration = 1.0f;
    [Tooltip("The default duration for blending into this expression from the previous state.")]
    public float defaultBlendInDuration = 0.2f;
    [Tooltip("The default duration for blending out of this expression back to neutral.")]
    public float defaultBlendOutDuration = 0.2f;
    [Tooltip("The animation curve used for blending in and out, providing smooth transitions.")]
    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // --- Future Expansion Ideas ---
    // public List<BoneRotation> boneRotations; // If your system also uses bone-based facial animation
    // public AudioClip associatedDialogueLine; // For linking expressions to specific dialogue
}
```

---

### **2. `FacialAnimationController.cs`**
*This MonoBehaviour sits on the character's face GameObject and is the low-level driver. It directly manipulates the `SkinnedMeshRenderer`'s blend shapes and handles the blending transitions. It abstracts away the direct Unity API calls from the higher-level `FacialAnimationSystem`.*

```csharp
using UnityEngine;
using System.Collections.Generic; // For Dictionaries

/// <summary>
/// Controls the low-level application of blend shapes to a SkinnedMeshRenderer.
/// This component sits on the character's face GameObject and is responsible for:
/// - Storing current and target blend shape weights.
/// - Handling the smooth blending between different facial states over time.
/// It acts as the 'Strategy' or 'Concrete Component' in our Facial Animation System pattern,
/// providing the specific implementation for facial mesh manipulation.
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class FacialAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SkinnedMeshRenderer component that displays the character's face and has blend shapes.")]
    [SerializeField] private SkinnedMeshRenderer faceMeshRenderer;

    // Internal state for managing blend shape weights during transitions.
    private Dictionary<int, float> currentBlendWeights = new Dictionary<int, float>(); // Actual weights currently set on the mesh
    private Dictionary<int, float> targetBlendWeights = new Dictionary<int, float>();  // The desired weights for the current expression
    private Dictionary<int, float> startBlendWeights = new Dictionary<int, float>();   // Weights at the beginning of the current blend operation

    // Blending parameters
    private float blendTimer = 0f;
    private float blendDuration = 0f;
    private AnimationCurve activeBlendCurve;
    private bool isBlending = false;

    private void Awake()
    {
        // Auto-assign SkinnedMeshRenderer if not already set.
        if (faceMeshRenderer == null)
        {
            faceMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        if (faceMeshRenderer == null)
        {
            Debug.LogError("FacialAnimationController requires a SkinnedMeshRenderer component on the same GameObject.", this);
            enabled = false; // Disable component if essential reference is missing
            return;
        }

        // Initialize internal weight dictionaries with current blend shape states (usually 0 at start).
        // This ensures a smooth start if blend shapes have initial values.
        for (int i = 0; i < faceMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            float initialWeight = faceMeshRenderer.GetBlendShapeWeight(i);
            currentBlendWeights[i] = initialWeight;
            targetBlendWeights[i] = initialWeight;
            startBlendWeights[i] = initialWeight;
        }
    }

    private void Update()
    {
        // Only perform blending calculations if a blend operation is active.
        if (isBlending)
        {
            blendTimer += Time.deltaTime;
            // Calculate normalized time (0 to 1) for the blend, clamping it.
            float t = (blendDuration > 0) ? Mathf.Clamp01(blendTimer / blendDuration) : 1f;
            // Apply the animation curve to 't' for non-linear blending (e.g., ease in/out).
            float curvedT = activeBlendCurve.Evaluate(t);

            bool anyWeightChanged = false; // Flag to track if any blend shape weight actually changed

            // Iterate through all target blend shapes and interpolate their weights.
            foreach (var kvp in targetBlendWeights)
            {
                int blendShapeIndex = kvp.Key;
                float targetWeight = kvp.Value;
                // Get the starting weight for this blend shape; default to 0 if not present (shouldn't happen after initialization).
                float startWeight = startBlendWeights.ContainsKey(blendShapeIndex) ? startBlendWeights[blendShapeIndex] : 0f;

                // Linearly interpolate between the start and target weight using the curved time.
                float blendedWeight = Mathf.Lerp(startWeight, targetWeight, curvedT);

                // Only update the blend shape if there's a significant difference to avoid redundant API calls.
                if (Mathf.Abs(currentBlendWeights[blendShapeIndex] - blendedWeight) > 0.01f)
                {
                    currentBlendWeights[blendShapeIndex] = blendedWeight;
                    faceMeshRenderer.SetBlendShapeWeight(blendShapeIndex, blendedWeight);
                    anyWeightChanged = true;
                }
            }

            // If blending is complete (or duration was 0), finalize weights and stop blending.
            if (t >= 1.0f)
            {
                isBlending = false;
                // Ensure final weights are exactly the target weights to prevent floating-point inaccuracies.
                foreach (var kvp in targetBlendWeights)
                {
                    currentBlendWeights[kvp.Key] = kvp.Value;
                    faceMeshRenderer.SetBlendShapeWeight(kvp.Key, kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Sets the target blend shape weights for a facial expression and starts the blending process.
    /// This method is called by the higher-level FacialAnimationSystem.
    /// </summary>
    /// <param name="expression">The FacialExpression ScriptableObject containing the target weights.</param>
    /// <param name="blendDurationOverride">Optional: Override the blend duration defined in the expression.</param>
    /// <param name="blendCurveOverride">Optional: Override the blend curve defined in the expression.</param>
    public void SetExpressionTarget(FacialExpression expression, float blendDurationOverride = -1f, AnimationCurve blendCurveOverride = null)
    {
        if (expression == null)
        {
            Debug.LogWarning("Attempted to set null expression.", this);
            return;
        }

        // 1. Snapshot current blend weights as the 'start' weights for the upcoming blend.
        foreach (var kvp in currentBlendWeights)
        {
            startBlendWeights[kvp.Key] = kvp.Value;
        }

        // 2. Reset target weights to match current weights before applying the new expression.
        // This ensures blend shapes *not* specified in the new expression remain at their current value
        // or slowly blend back to what they were, rather than instantly snapping to 0.
        foreach (var kvp in currentBlendWeights)
        {
            targetBlendWeights[kvp.Key] = kvp.Value;
        }

        // 3. Apply the blend shape weights defined in the FacialExpression to the target weights.
        foreach (var bw in expression.blendShapeWeights)
        {
            int blendShapeIndex = faceMeshRenderer.sharedMesh.GetBlendShapeIndex(bw.blendShapeName);
            if (blendShapeIndex != -1)
            {
                targetBlendWeights[blendShapeIndex] = bw.weight;
            }
            else
            {
                Debug.LogWarning($"Blend shape '{bw.blendShapeName}' not found on mesh '{faceMeshRenderer.name}'. Please check the name in the FacialExpression asset.", this);
            }
        }

        // Set up blending parameters.
        this.blendDuration = (blendDurationOverride >= 0) ? blendDurationOverride : expression.defaultBlendInDuration;
        this.activeBlendCurve = blendCurveOverride ?? expression.blendCurve;
        this.blendTimer = 0f;
        this.isBlending = true; // Start the blending process in Update()
    }

    /// <summary>
    /// Resets all blend shapes to a neutral state (0 weight) over a specified duration.
    /// </summary>
    /// <param name="duration">The time to blend to neutral.</param>
    /// <param name="curve">The animation curve to use for the blend.</param>
    public void ResetToNeutral(float duration = 0.2f, AnimationCurve curve = null)
    {
        // Snapshot current weights as start weights.
        foreach (var kvp in currentBlendWeights)
        {
            startBlendWeights[kvp.Key] = kvp.Value;
        }

        // Set all target weights to 0.
        foreach (var kvp in currentBlendWeights)
        {
            targetBlendWeights[kvp.Key] = 0f;
        }

        // Set up blending parameters for the reset.
        this.blendDuration = duration;
        this.activeBlendCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        this.blendTimer = 0f;
        this.isBlending = true;
    }

    /// <summary>
    /// Directly sets the weight of a single blend shape over a short duration.
    /// Useful for granular control like lip sync, where specific blend shapes need rapid, independent adjustments.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape to adjust.</param>
    /// <param name="weight">The target weight (0-100).</param>
    /// <param name="blendDuration">The duration for this specific blend.</param>
    public void SetSingleBlendShapeWeight(string blendShapeName, float weight, float blendDuration = 0.05f)
    {
        int blendShapeIndex = faceMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
        if (blendShapeIndex != -1)
        {
            // Snapshot current weight for this specific blend shape.
            startBlendWeights[blendShapeIndex] = currentBlendWeights[blendShapeIndex];
            // Set new target weight for this specific blend shape.
            targetBlendWeights[blendShapeIndex] = weight;

            // Start a quick blend for just this blend shape.
            this.blendDuration = blendDuration;
            this.activeBlendCurve = AnimationCurve.Linear(0, 0, 1, 1); // Often a linear blend is preferred for direct control.
            this.blendTimer = 0f;
            this.isBlending = true; // Ensure the Update loop is active for blending.
        }
        else
        {
            Debug.LogWarning($"Blend shape '{blendShapeName}' not found for direct setting. Check name.", this);
        }
    }
}
```

---

### **3. `FacialAnimationSystem.cs`**
*This is the central manager (or 'Facade') of the pattern. It provides a high-level API for other game systems to control facial animations without needing to know the low-level details of blend shapes or `SkinnedMeshRenderer`s. It orchestrates the `FacialAnimationController` to play expressions.*

```csharp
using UnityEngine;
using System.Collections; // For Coroutines
using System.Collections.Generic; // Not strictly needed here, but good practice for other managers

/// <summary>
/// The core component of the 'FacialAnimationSystem' pattern.
/// This acts as a Facade or Manager, providing a high-level, easy-to-use interface
/// for other game systems to control facial animations. It orchestrates the
/// FacialAnimationController to play expressions, handling the blend-in, hold, and blend-out phases.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensures this script runs very early, before other scripts that might want to use it.
public class FacialAnimationSystem : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("The FacialAnimationController on the character's face GameObject. " +
             "If not assigned, it will try to find one in children during Awake.")]
    [SerializeField] private FacialAnimationController facialAnimationController;

    [Header("Current State")]
    [Tooltip("The currently active facial expression being played by the system.")]
    public FacialExpression currentExpression;
    private Coroutine activeExpressionCoroutine; // Reference to the currently running expression animation routine.

    private void Awake()
    {
        // Attempt to find the FacialAnimationController in children if not explicitly assigned.
        if (facialAnimationController == null)
        {
            facialAnimationController = GetComponentInChildren<FacialAnimationController>();
            if (facialAnimationController == null)
            {
                Debug.LogError("FacialAnimationSystem: No FacialAnimationController found in children or assigned. " +
                               "Please ensure a FacialAnimationController is on the character's face GameObject.", this);
                enabled = false; // Disable this component if its dependency is missing.
                return;
            }
        }
    }

    /// <summary>
    /// Plays a facial expression, managing its blend-in, hold duration, and blend-out to neutral.
    /// This is the primary public method for triggering emotional states or reactions.
    /// </summary>
    /// <param name="expression">The FacialExpression ScriptableObject to play.</param>
    /// <param name="intensity">Overall intensity of the expression (0-1). Currently, this parameter is a placeholder
    ///                          for future scaling of blend shape weights, but not actively used to scale weights in this example.</param>
    /// <param name="blendInDuration">Optional: Duration to blend into the expression. Uses expression's default if negative.</param>
    /// <param name="holdDuration">Optional: Duration to hold the expression. Uses expression's default if negative.</param>
    /// <param name="blendOutDuration">Optional: Duration to blend out of the expression to neutral. Uses expression's default if negative.</param>
    public void PlayExpression(
        FacialExpression expression,
        float intensity = 1.0f, // Future expansion: scale blend shape weights by this intensity
        float blendInDuration = -1f,
        float holdDuration = -1f,
        float blendOutDuration = -1f)
    {
        if (expression == null)
        {
            Debug.LogWarning("Attempted to play a null FacialExpression.", this);
            return;
        }

        // Stop any currently running expression animation to prevent conflicts.
        if (activeExpressionCoroutine != null)
        {
            StopCoroutine(activeExpressionCoroutine);
            // Optional: Immediately reset the face or blend out quickly before starting the new expression.
            // facialAnimationController.ResetToNeutral(0.1f);
        }

        currentExpression = expression; // Update the current active expression.
        activeExpressionCoroutine = StartCoroutine(AnimateExpressionRoutine(
            expression, intensity, blendInDuration, holdDuration, blendOutDuration));
    }

    /// <summary>
    /// Coroutine that handles the full lifecycle of an expression: blend in, hold, and blend out.
    /// </summary>
    private IEnumerator AnimateExpressionRoutine(
        FacialExpression expression,
        float intensity, // Placeholder for future use
        float blendInDuration,
        float holdDuration,
        float blendOutDuration)
    {
        // Determine the actual durations, using overrides if provided, otherwise defaults from the ScriptableObject.
        float actualBlendInDuration = (blendInDuration >= 0) ? blendInDuration : expression.defaultBlendInDuration;
        float actualHoldDuration = (holdDuration >= 0) ? holdDuration : expression.defaultDuration;
        float actualBlendOutDuration = (blendOutDuration >= 0) ? blendOutDuration : expression.defaultBlendOutDuration;

        // 1. Blend In: Instruct the controller to smoothly transition to the new expression.
        facialAnimationController.SetExpressionTarget(expression, actualBlendInDuration, expression.blendCurve);
        yield return new WaitForSeconds(actualBlendInDuration); // Wait for the blend-in to complete.

        // 2. Hold: Maintain the expression for the specified duration.
        yield return new WaitForSeconds(actualHoldDuration);

        // 3. Blend Out: Instruct the controller to smoothly transition back to a neutral face.
        facialAnimationController.ResetToNeutral(actualBlendOutDuration, expression.blendCurve);
        yield return new WaitForSeconds(actualBlendOutDuration); // Wait for the blend-out to complete.

        // Reset state after the expression cycle is complete.
        currentExpression = null;
        activeExpressionCoroutine = null;
    }

    /// <summary>
    /// Sets a specific blend shape weight directly. Useful for continuous effects like lip-sync,
    /// blinking, or fine-tuning without playing a full expression.
    /// Note: This direct control might conflict with a currently playing full expression if
    /// they attempt to control the same blend shapes. A more advanced system might implement
    /// blend shape "layers" for better arbitration.
    /// </summary>
    /// <param name="blendShapeName">The name of the blend shape to adjust.</param>
    /// <param name="weight">The target weight (0-100).</param>
    /// <param name="blendDuration">The duration over which to transition to the new weight.</param>
    public void SetDirectBlendShapeWeight(string blendShapeName, float weight, float blendDuration = 0.05f)
    {
        facialAnimationController.SetSingleBlendShapeWeight(blendShapeName, weight, blendDuration);
    }

    /// <summary>
    /// Immediately stops any ongoing facial animation and resets all blend shapes to their neutral (0) state.
    /// Useful for abrupt scene changes or character state resets.
    /// </summary>
    public void ResetFaceImmediately()
    {
        if (activeExpressionCoroutine != null)
        {
            StopCoroutine(activeExpressionCoroutine);
            activeExpressionCoroutine = null;
            currentExpression = null;
        }
        facialAnimationController.ResetToNeutral(0f); // 0 duration for instant reset.
    }


    // ====================================================================================
    // --- EXAMPLE USAGE IN COMMENTS ---
    // ====================================================================================

    /*
    // To implement the FacialAnimationSystem in your Unity project:

    // 1. Prepare your 3D Character:
    //    - Ensure your character model has a `SkinnedMeshRenderer` component for its face.
    //    - The `SkinnedMeshRenderer`'s mesh must contain blend shapes (e.g., "Mouth_Smile", "Brow_Raise", "Eye_Blink").
    //      These blend shape names are crucial as they will be referenced by your FacialExpression assets.

    // 2. Add the `FacialAnimationController`:
    //    - Create an empty GameObject as a child of your character's root (e.g., "Face_Rig")
    //      OR place it directly on the GameObject containing the SkinnedMeshRenderer for the face.
    //    - Add the `FacialAnimationController` component to this GameObject.
    //    - Drag your character's `SkinnedMeshRenderer` (the face mesh) into the `Face Mesh Renderer` slot if it's not on the same GameObject.

    // 3. Add the `FacialAnimationSystem` (The Manager):
    //    - Create another empty GameObject in your scene (e.g., "FacialAnimationManager").
    //    - Add the `FacialAnimationSystem` component to this new GameObject.
    //    - In the Inspector for `FacialAnimationSystem`, drag the GameObject that has your
    //      `FacialAnimationController` (from step 2) into the `Facial Animation Controller` slot.

    // 4. Create `FacialExpression` ScriptableObject Assets:
    //    - In your Project window, right-click -> Create -> Facial Animation -> Facial Expression.
    //    - Create several of these (e.g., "HappyExpression", "SadExpression", "AngryExpression", "SurpriseExpression").
    //    - For each `FacialExpression` asset, in the Inspector:
    //        - Give it a descriptive `Expression Name`.
    //        - In the `Blend Shape Weights` list, add elements:
    //            - For each element, type the **exact name** of a blend shape from your `SkinnedMeshRenderer`'s mesh.
    //            - Set the `Weight` (0-100) for that blend shape when this expression is active.
    //        - Adjust `Default Duration`, `Default Blend In Duration`, `Default Blend Out Duration`, and `Blend Curve` as desired.

    // 5. Example Usage from another script (e.g., `CharacterActionController.cs`):

    // public class CharacterActionController : MonoBehaviour
    // {
    //     [Header("Facial System References")]
    //     [Tooltip("Reference to the FacialAnimationSystem in the scene.")]
    //     [SerializeField] private FacialAnimationSystem facialSystem;
    //
    //     [Header("Facial Expression Assets")]
    //     [Tooltip("Assign your created FacialExpression ScriptableObjects here.")]
    //     [SerializeField] private FacialExpression happyExpression;
    //     [SerializeField] private FacialExpression sadExpression;
    //     [SerializeField] private FacialExpression angryExpression;
    //     [SerializeField] private FacialExpression surpriseExpression;
    //
    //     void Start()
    //     {
    //         // Start a sequence of expressions after a short delay.
    //         StartCoroutine(PlayExpressionsSequence());
    //     }
    //
    //     IEnumerator PlayExpressionsSequence()
    //     {
    //         if (facialSystem == null)
    //         {
    //             Debug.LogError("FacialAnimationSystem reference is missing!", this);
    //             yield break;
    //         }
    //
    //         Debug.Log("Starting facial expression demonstration...");
    //         yield return new WaitForSeconds(1.0f);
    //
    //         // --- Playing a basic expression ---
    //         Debug.Log("Playing Happy expression (using custom blend-in, hold, and blend-out durations)...");
    //         facialSystem.PlayExpression(happyExpression, blendInDuration: 0.3f, holdDuration: 1.5f, blendOutDuration: 0.5f);
    //         // Wait for the full duration of the happy expression before proceeding.
    //         yield return new WaitForSeconds(0.3f + 1.5f + 0.5f + 1.0f); // blendIn + hold + blendOut + extra pause
    //
    //         // --- Playing an expression using its default durations ---
    //         Debug.Log("Playing Sad expression (using default durations from the ScriptableObject)...");
    //         facialSystem.PlayExpression(sadExpression);
    //         yield return new WaitForSeconds(sadExpression.defaultBlendInDuration + sadExpression.defaultDuration + sadExpression.defaultBlendOutDuration + 1.0f);
    //
    //         // --- Playing an expression with an overridden hold duration ---
    //         Debug.Log("Playing Angry expression (with a shorter hold duration)...");
    //         facialSystem.PlayExpression(angryExpression, holdDuration: 0.8f);
    //         yield return new WaitForSeconds(angryExpression.defaultBlendInDuration + 0.8f + angryExpression.defaultBlendOutDuration + 1.0f);
    //
    //         // --- Immediately playing another expression (it will interrupt the previous one) ---
    //         Debug.Log("Playing Surprise expression (overriding previous expression)...");
    //         facialSystem.PlayExpression(surpriseExpression, blendInDuration: 0.1f, holdDuration: 1.0f);
    //         yield return new WaitForSeconds(0.1f + 1.0f + surpriseExpression.defaultBlendOutDuration + 1.0f);
    //
    //         // --- Demonstrating immediate face reset ---
    //         Debug.Log("Resetting face immediately (e.g., if dialogue ends abruptly or character dies).");
    //         facialSystem.ResetFaceImmediately();
    //         yield return new WaitForSeconds(1.5f);
    //
    //         // --- Demonstrating direct blend shape control (e.g., for simple lip sync or blinking) ---
    //         Debug.Log("Demonstrating direct blend shape control for a 'Mouth_Open' blend shape...");
    //         facialSystem.SetDirectBlendShapeWeight("Mouth_Open", 50, 0.1f); // Open mouth
    //         yield return new WaitForSeconds(0.5f);
    //         facialSystem.SetDirectBlendShapeWeight("Mouth_Open", 0, 0.2f);  // Close mouth
    //         facialSystem.SetDirectBlendShapeWeight("Mouth_Smile", 70, 0.2f); // Smile
    //         yield return new WaitForSeconds(1.0f);
    //         facialSystem.SetDirectBlendShapeWeight("Mouth_Smile", 0, 0.3f); // Stop smiling
    //         yield return new WaitForSeconds(1.0f);
    //
    //         Debug.Log("Facial expression demonstration complete!");
    //     }
    //
    //     // You can also trigger expressions from UI buttons, animation events, AI scripts, etc.
    //     public void OnHappyButtonClicked()
    //     {
    //         if (facialSystem != null && happyExpression != null)
    //         {
    //             facialSystem.PlayExpression(happyExpression);
    //         }
    //     }
    // }
    */
}
```