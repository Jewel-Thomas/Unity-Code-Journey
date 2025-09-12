// Unity Design Pattern Example: DynamicAnimationSystem
// This script demonstrates the DynamicAnimationSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'DynamicAnimationSystem' design pattern in Unity aims to provide a flexible way to play `AnimationClips` directly, blend them, and control their properties at runtime, *without needing to pre-configure them as states within an `AnimatorController`*. This is particularly useful for:

*   **Reactive animations:** Hit reactions, flinches, quick gestures.
*   **Procedural or context-sensitive animations:** Blending different walk/run cycles based on speed, surface, or mood.
*   **Layered effects:** Adding partial-body animations (e.g., hand gestures, head turns) on top of a base locomotion.
*   **Special abilities or spell effects:** Where creating a full `Animator` state machine for every single short-lived effect would be cumbersome.

This system leverages Unity's `Animator.PlayInFixedTime` or `Animator.CrossFadeInFixedTime` (though we'll use `PlayInFixedTime` for fine-grained control over layer weights) combined with `Animator.SetLayerWeight` to dynamically control the contribution of various `AnimationClips` on different `Animator` layers.

---

### **Setup in Unity Editor:**

Before using the script, you need:

1.  **A GameObject** with an `Animator` component.
2.  **An `AnimatorController`** assigned to the `Animator`.
3.  **Define at least one "Dynamic" Layer** in your `AnimatorController` (in addition to the Base Layer).
    *   For this example, let's assume you've created layers like:
        *   `Layer 0: Base Layer` (e.g., for locomotion, idle states)
        *   `Layer 1: Reaction Layer` (for hit reactions, flinches)
        *   `Layer 2: Gesture Layer` (for hand gestures, head movements)
    *   Set the `Weight` of these dynamic layers to `1` in the `AnimatorController` by default. This script will override it dynamically.
    *   Set the `Blending` mode for `Reaction Layer` to **Additive** if you want it to layer on top without fully overriding.
    *   Set the `Blending` mode for `Gesture Layer` to **Override** or **Additive** based on your needs.
4.  **Assign `AnimationClips`** to the example fields in the Inspector (e.g., for `ReactionClip`, `GestureClip`).

---

### **`DynamicAnimationSystem.cs` Script:**

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations if needed, currently not heavily used.

/// <summary>
/// DynamicAnimationHandle is a lightweight struct used to uniquely identify and control
/// an active dynamic animation instance.
/// </summary>
public struct DynamicAnimationHandle
{
    public int Id; // A unique identifier for this animation instance.
    public bool IsValid { get { return Id != 0; } } // Checks if the handle refers to a valid animation.

    public DynamicAnimationHandle(int id) { this.Id = id; }

    public static DynamicAnimationHandle Invalid { get { return new DynamicAnimationHandle(0); } }
}

/// <summary>
/// ActiveDynamicAnimation is an internal class that holds all runtime data for
/// a single dynamic animation instance currently playing through the system.
/// </summary>
[System.Serializable] // Make it visible in Inspector for debugging if needed.
internal class ActiveDynamicAnimation
{
    public int Id; // Unique ID, matching the DynamicAnimationHandle.
    public AnimationClip Clip; // The actual AnimationClip asset being played.
    public float StartTime; // Time.time when this animation instance started.
    public float Duration; // How long this animation should play (excluding blend-in/out). 0 for natural clip length.
    public float TargetWeight; // The maximum weight this animation should reach (0-1).
    public float CurrentWeight; // The actual current weight, managed by the system for blending.
    public float BlendInDuration; // Time in seconds to blend in to TargetWeight.
    public float BlendOutDuration; // Time in seconds to blend out to 0 weight.
    public int LayerIndex; // The Animator layer index this animation is playing on.
    public float Speed; // Playback speed of the clip (currently only affects calculated Duration if not looping).
    public bool IsLooping; // True if the clip should loop indefinitely until stopped.

    // Runtime state variables:
    public float ElapsedTime; // Total time this animation has been active.
    public bool IsBlendingOut; // True if a blend-out has been requested.
    private float _blendOutTimer; // Internal timer for blend-out phase.
    public bool IsFinished; // True if the animation has fully blended out and can be removed.

    /// <summary>
    /// Constructor for a new active dynamic animation.
    /// </summary>
    public ActiveDynamicAnimation(int id, AnimationClip clip, float duration, float targetWeight,
                                  float blendInDuration, float blendOutDuration, int layerIndex,
                                  float speed, bool isLooping)
    {
        Id = id;
        Clip = clip;
        StartTime = Time.time;
        // If duration is 0, use the clip's natural length scaled by speed.
        // If isLooping, duration only serves as a minimum time before an explicit stop can be requested.
        Duration = (duration > 0 || !clip) ? duration : (clip.length / speed);
        TargetWeight = Mathf.Clamp01(targetWeight);
        CurrentWeight = 0f; // Always start at 0 and blend in.
        BlendInDuration = Mathf.Max(0f, blendInDuration);
        BlendOutDuration = Mathf.Max(0f, blendOutDuration);
        LayerIndex = layerIndex;
        Speed = speed;
        IsLooping = isLooping;

        ElapsedTime = 0f;
        IsBlendingOut = false;
        _blendOutTimer = 0f;
        IsFinished = false;
    }

    /// <summary>
    /// Updates the animation's internal state, including weight and blend progression.
    /// </summary>
    public void Update(float deltaTime)
    {
        ElapsedTime += deltaTime;

        if (!IsBlendingOut)
        {
            // --- Blend In Phase ---
            if (ElapsedTime < BlendInDuration)
            {
                // Gradually increase weight from 0 to TargetWeight.
                CurrentWeight = Mathf.Lerp(0f, TargetWeight, ElapsedTime / BlendInDuration);
            }
            else
            {
                // Fully blended in.
                CurrentWeight = TargetWeight;

                // --- Check for natural end of non-looping animation ---
                // If not looping and the clip's duration has passed, request blend out.
                if (!IsLooping && Duration > 0 && ElapsedTime >= Duration)
                {
                    RequestBlendOut();
                }
            }
        }
        else // --- Blend Out Phase ---
        {
            _blendOutTimer += deltaTime;

            // Gradually decrease weight from its current value to 0 over BlendOutDuration.
            // Lerp from TargetWeight because that's the peak weight it reached.
            CurrentWeight = Mathf.Lerp(TargetWeight, 0f, _blendOutTimer / BlendOutDuration);

            // If blend out is complete (or nearly complete), mark as finished.
            if (CurrentWeight <= 0.01f || _blendOutTimer >= BlendOutDuration)
            {
                IsFinished = true;
                CurrentWeight = 0f; // Ensure weight is exactly 0.
            }
        }
    }

    /// <summary>
    /// Initiates the blend-out process for this animation.
    /// </summary>
    public void RequestBlendOut()
    {
        if (!IsBlendingOut)
        {
            IsBlendingOut = true;
            _blendOutTimer = 0f; // Reset blend out timer to start from now.
        }
    }
}

/// <summary>
/// The DynamicAnimationSystem provides a mechanism to play AnimationClips directly on an Animator
/// without requiring them to be part of an AnimatorController's state machine.
/// It enables dynamic blending, layering, and runtime control of these animations,
/// making it ideal for reactive animations, specific character abilities, or layered effects
/// that would be cumbersome to manage within a complex AnimatorController.
/// </summary>
[RequireComponent(typeof(Animator))] // Ensures an Animator component is present.
public class DynamicAnimationSystem : MonoBehaviour
{
    private Animator _animator; // Reference to the Animator component.

    // A counter for generating unique IDs for each dynamic animation instance.
    private int _nextAnimationId = 1;

    // Dictionary to manage currently active dynamic animations by their unique ID.
    private Dictionary<int, ActiveDynamicAnimation> _activeAnimations = new Dictionary<int, ActiveDynamicAnimation>();

    // A temporary list to store IDs of animations that need to be removed at the end of the frame,
    // preventing modification of _activeAnimations during iteration.
    private List<int> _animationsToRemove = new List<int>();

    /// <summary>
    /// Configuration class for defining how each Animator layer behaves when used by the DynamicAnimationSystem.
    /// These layers should be pre-configured in your Animator Controller.
    /// </summary>
    [System.Serializable]
    public class DynamicLayerConfig
    {
        public string Name; // A friendly name for identification in the Inspector.
        [Tooltip("The actual layer index in the Animator Controller (e.g., 0 for Base Layer, 1 for Reaction Layer).")]
        public int LayerIndex; // The Animator layer index.
        [Tooltip("If true, only one dynamic animation can play on this layer at a time. A new animation will " +
                 "automatically blend out and replace any existing one on this layer.")]
        public bool SingleAnimationPerLayer = true; // Does this layer support multiple concurrent animations?
        [Tooltip("The default blending mode for this Animator layer. This will be set by the system at Awake.")]
        public AnimatorLayerBlending DefaultLayerBlending = AnimatorLayerBlending.Override;
    }

    [Header("Dynamic Animator Layer Configuration")]
    [Tooltip("Define which Animator layers your dynamic animations will use. These layers should exist in your Animator Controller.")]
    public DynamicLayerConfig[] dynamicLayers;

    // A dictionary for quick lookup of layer configurations by their index.
    private Dictionary<int, DynamicLayerConfig> _layerConfigsMap = new Dictionary<int, DynamicLayerConfig>();


    void Awake()
    {
        _animator = GetComponent<Animator>();

        // Initialize the layer configurations map for efficient lookups.
        foreach (var config in dynamicLayers)
        {
            if (!_layerConfigsMap.ContainsKey(config.LayerIndex))
            {
                _layerConfigsMap.Add(config.LayerIndex, config);
            }
            else
            {
                Debug.LogWarning($"DynamicAnimationSystem: Duplicate layer index {config.LayerIndex} found in dynamicLayers. Ignoring duplicate.", this);
            }

            // Set the Animator layer's blending mode as defined in the configuration.
            // This ensures consistency and can override Animator Controller settings if needed.
            _animator.SetLayerBlending(config.LayerIndex, config.DefaultLayerBlending);
        }
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // Clear the list of animations to remove from the previous frame.
        _animationsToRemove.Clear();

        // Iterate through all currently active dynamic animations.
        foreach (var kvp in _activeAnimations)
        {
            ActiveDynamicAnimation anim = kvp.Value;
            anim.Update(deltaTime); // Update its blend and time state.

            // Apply the animation's calculated current weight to its corresponding Animator layer.
            // This is how the system dynamically controls the layer's influence.
            _animator.SetLayerWeight(anim.LayerIndex, anim.CurrentWeight);

            // If the animation has finished its blend-out, mark it for removal.
            if (anim.IsFinished)
            {
                _animationsToRemove.Add(anim.Id);
            }
        }

        // Remove all animations that have finished their lifecycle.
        foreach (int id in _animationsToRemove)
        {
            _activeAnimations.Remove(id);
            // After removal, the layer weight for this animation instance will naturally go to 0
            // because the next frame it won't be processed, and its CurrentWeight was already 0.
            // If the layer should instantly return to 0 and no other animations are on it,
            // we could explicitly set _animator.SetLayerWeight(layerIndex, 0f) here,
            // but the current system's blend-out ensures a smooth transition.
        }
    }

    /// <summary>
    /// Plays an AnimationClip dynamically on a specified Animator layer.
    /// This system manages blending and weight without needing Animator Controller states.
    /// </summary>
    /// <param name="clip">The AnimationClip to play.</param>
    /// <param name="layerIndex">The Animator layer index to play the clip on. Must be configured in dynamicLayers.</param>
    /// <param name="blendInDuration">How long to blend in from current animation state on the layer.</param>
    /// <param name="duration">How long the animation should play after blending in. 0 uses clip's natural length. Negative value for infinite (only if isLooping is true).</param>
    /// <param name="targetWeight">The target weight for the animation (0-1) once blended in.</param>
    /// <param name="speed">Playback speed of the clip. (Note: Only affects calculated 'duration' if not looping, as Animator layer speed is global unless controller parameters are used).</param>
    /// <param name="isLooping">If true, the clip will loop indefinitely until explicitly stopped or blended out.</param>
    /// <returns>A <see cref="DynamicAnimationHandle"/> to control the played animation, or an invalid handle if play failed.</returns>
    public DynamicAnimationHandle PlayAnimation(
        AnimationClip clip,
        int layerIndex,
        float blendInDuration = 0.2f,
        float duration = 0f, // 0 for natural clip length
        float targetWeight = 1.0f,
        float speed = 1.0f,
        bool isLooping = false,
        float blendOutDuration = -1f) // -1 to match blendInDuration
    {
        if (clip == null)
        {
            Debug.LogWarning("DynamicAnimationSystem: Attempted to play a null AnimationClip.", this);
            return DynamicAnimationHandle.Invalid;
        }

        // Validate that the requested layer index is configured in our system.
        if (!_layerConfigsMap.TryGetValue(layerIndex, out DynamicLayerConfig layerConfig))
        {
            Debug.LogError($"DynamicAnimationSystem: Layer index {layerIndex} not configured in dynamicLayers. " +
                           $"Please add it in the Inspector.", this);
            return DynamicAnimationHandle.Invalid;
        }

        // If the layer is configured for single animation at a time, find and request blend-out for any existing ones.
        if (layerConfig.SingleAnimationPerLayer)
        {
            foreach (var kvp in _activeAnimations.Where(kvp => kvp.Value.LayerIndex == layerIndex && !kvp.Value.IsBlendingOut))
            {
                kvp.Value.RequestBlendOut();
            }
        }

        // Generate a unique ID for this new animation instance.
        int currentId = _nextAnimationId++;

        // If blendOutDuration is not explicitly set, use blendInDuration.
        if (blendOutDuration < 0) blendOutDuration = blendInDuration;

        // Create the internal representation of the animation.
        ActiveDynamicAnimation newAnim = new ActiveDynamicAnimation(
            currentId, clip, duration, targetWeight, blendInDuration, blendOutDuration,
            layerIndex, speed, isLooping
        );

        _activeAnimations.Add(currentId, newAnim);

        // Instruct the Unity Animator to play the clip directly on the specified layer.
        // We use PlayInFixedTime with an initial weight of 0 and normalized time 0.
        // Our system will then manage the layer's weight (blending it in) over time.
        // This gives us fine-grained control over how the layer contributes.
        _animator.PlayInFixedTime(clip.name, layerIndex, 0f, 0f);

        return new DynamicAnimationHandle(currentId);
    }

    /// <summary>
    /// Stops a currently playing dynamic animation identified by its handle.
    /// The animation will blend out gracefully over its configured blend-out duration.
    /// </summary>
    /// <param name="handle">The handle of the animation to stop.</param>
    public void StopAnimation(DynamicAnimationHandle handle)
    {
        if (handle.IsValid && _activeAnimations.TryGetValue(handle.Id, out ActiveDynamicAnimation anim))
        {
            anim.RequestBlendOut();
        }
    }

    /// <summary>
    /// Force-stops an animation immediately without blending.
    /// This will set its layer weight to 0 instantly and remove it.
    /// </summary>
    /// <param name="handle">The handle of the animation to stop.</param>
    public void StopAnimationImmediately(DynamicAnimationHandle handle)
    {
        if (handle.IsValid && _activeAnimations.TryGetValue(handle.Id, out ActiveDynamicAnimation anim))
        {
            // Set layer weight to 0 immediately.
            _animator.SetLayerWeight(anim.LayerIndex, 0f);
            anim.IsFinished = true; // Mark for immediate removal in the next Update.
            anim.CurrentWeight = 0f;
        }
    }

    /// <summary>
    /// Retrieves a currently active animation's properties by its handle.
    /// </summary>
    /// <param name="handle">The handle of the animation.</param>
    /// <param name="animationData">Output parameter for the animation data.</param>
    /// <returns>True if the animation is found and active, false otherwise.</returns>
    public bool TryGetActiveAnimation(DynamicAnimationHandle handle, out ActiveDynamicAnimation animationData)
    {
        return _activeAnimations.TryGetValue(handle.Id, out animationData);
    }

    // --- Example Fields for Demonstration ---
    [Header("Example Usage Fields")]
    public AnimationClip reactionClip;
    public AnimationClip gestureClip;
    public AnimationClip fullBodyActionClip;

    private DynamicAnimationHandle _currentGestureHandle = DynamicAnimationHandle.Invalid;

    /// <summary>
    /// Example usage: Plays a one-shot reaction animation (e.g., flinch, quick nod).
    /// Assumes 'Layer 1: Reaction Layer' is configured in dynamicLayers and is set to Additive blending.
    /// </summary>
    public void PlayReactionExample()
    {
        if (reactionClip == null)
        {
            Debug.LogWarning("Reaction Clip not assigned for example usage!", this);
            return;
        }
        // Play reaction on layer 1. Blend in 0.1s, play for 0.5s, target weight 1.0.
        // It's not looping.
        PlayAnimation(reactionClip, 1, 0.1f, 0.5f, 1.0f, 1.0f, false);
        Debug.Log("Playing Reaction Animation!");
    }

    /// <summary>
    /// Example usage: Toggles a looping gesture animation (e.g., waving, thinking pose).
    /// Assumes 'Layer 2: Gesture Layer' is configured in dynamicLayers. This might be Additive for partial body.
    /// </summary>
    public void ToggleGestureExample()
    {
        if (gestureClip == null)
        {
            Debug.LogWarning("Gesture Clip not assigned for example usage!", this);
            return;
        }

        if (_currentGestureHandle.IsValid)
        {
            StopAnimation(_currentGestureHandle); // Blend out the current gesture.
            _currentGestureHandle = DynamicAnimationHandle.Invalid;
            Debug.Log("Stopping Gesture Animation!");
        }
        else
        {
            // Play gesture on layer 2. Blend in 0.3s, loop indefinitely, target weight 0.7 (less dominant).
            _currentGestureHandle = PlayAnimation(gestureClip, 2, 0.3f, -1f, 0.7f, 1.0f, true);
            Debug.Log("Playing Looping Gesture Animation!");
        }
    }

    /// <summary>
    /// Example usage: Plays a full-body action that overrides other animations (e.g., a kick, special attack).
    /// Assumes 'Layer 1: Reaction Layer' could be used, but since it's an override, ensure the layer blending is correct.
    /// Or use a separate "Action Layer" with Override blending. For simplicity, let's use layer 1 for override.
    /// </summary>
    public void PlayFullBodyActionExample()
    {
        if (fullBodyActionClip == null)
        {
            Debug.LogWarning("Full Body Action Clip not assigned for example usage!", this);
            return;
        }

        // Play full body action on layer 1. Blend in 0.2s, use natural clip length, target weight 1.0 (full override).
        // Since layer 1 is set to SingleAnimationPerLayer, it will blend out any active reaction.
        // It's crucial that Layer 1's DefaultLayerBlending is set to AnimatorLayerBlending.Override
        // in the DynamicLayerConfig for this example to work as a full body override.
        PlayAnimation(fullBodyActionClip, 1, 0.2f, 0f, 1.0f, 1.0f, false);
        Debug.Log("Playing Full Body Action!");

        // If a gesture is playing on Layer 2, it will continue to play unless explicitly stopped or its layer is an Additive one.
        // If Layer 2 has its own SingleAnimationPerLayer set to false, it can run concurrently.
        // If Layer 2 is Additive, the Full Body Action (on an Override layer) would visually dominate.
    }
}
```

### **How to Use (Example Implementation):**

1.  **Create a new C# script** named `DynamicAnimationSystem.cs` and copy the code above into it.
2.  **Attach this script** to your character GameObject (the one with the `Animator`).
3.  **In the Inspector**, configure the `Dynamic Animator Layer Configuration`:
    *   **Size**: Set to `3` (for Base, Reaction, Gesture layers).
    *   **Element 0**: `Name: Base Layer`, `Layer Index: 0`, `Single Animation Per Layer: true` (or false if you intend to layer on base), `Default Layer Blending: Override`.
    *   **Element 1**: `Name: Reaction Layer`, `Layer Index: 1`, `Single Animation Per Layer: true`, `Default Layer Blending: Additive` (for layering reactions) or `Override` (for full body reactions). For the example, let's use `Additive`.
    *   **Element 2**: `Name: Gesture Layer`, `Layer Index: 2`, `Single Animation Per Layer: true`, `Default Layer Blending: Additive` (for layering gestures).
4.  **Drag and drop your `AnimationClip` assets** into the `Reaction Clip`, `Gesture Clip`, and `Full Body Action Clip` fields in the Inspector.
5.  **Call the methods** from other scripts, UI events, or input systems:

    ```csharp
    // Example of another script (e.g., PlayerController.cs) interacting with the system.
    using UnityEngine;

    public class PlayerController : MonoBehaviour
    {
        public DynamicAnimationSystem dynamicAnimSystem;

        void Start()
        {
            if (dynamicAnimSystem == null)
            {
                dynamicAnimSystem = GetComponent<DynamicAnimationSystem>();
                if (dynamicAnimSystem == null)
                {
                    Debug.LogError("DynamicAnimationSystem not found on this GameObject.", this);
                    enabled = false;
                }
            }
        }

        void Update()
        {
            // Trigger a reaction animation on pressing 'R'
            if (Input.GetKeyDown(KeyCode.R))
            {
                dynamicAnimSystem.PlayReactionExample();
            }

            // Toggle a looping gesture animation on pressing 'G'
            if (Input.GetKeyDown(KeyCode.G))
            {
                dynamicAnimSystem.ToggleGestureExample();
            }

            // Play a full body action on pressing 'F'
            if (Input.GetKeyDown(KeyCode.F))
            {
                dynamicAnimSystem.PlayFullBodyActionExample();
            }
        }
    }
    ```

### **How the DynamicAnimationSystem Pattern Works:**

1.  **`DynamicAnimationHandle`:** A simple `struct` providing a unique ID. When you play an animation, you get a handle back, allowing you to later stop or query that *specific instance* of the animation.

2.  **`ActiveDynamicAnimation` (Internal State):**
    *   This class encapsulates all the runtime information for an animation currently managed by the system (the `AnimationClip`, its target weight, current blend, duration, layer, etc.).
    *   It contains the logic for **blending in** (gradually increasing `CurrentWeight` to `TargetWeight`), **playing** (maintaining `TargetWeight`), and **blending out** (gradually decreasing `CurrentWeight` to 0).
    *   It tracks its own `ElapsedTime` and determines when it should naturally end (for non-looping clips) or when to initiate a blend-out request.

3.  **`DynamicAnimationSystem` (The Core Component):**
    *   **`Animator` Interaction:** It requires an `Animator` component and directly interacts with `_animator.PlayInFixedTime()` to initiate the playing of an `AnimationClip` on a specific layer, and `_animator.SetLayerWeight()` in its `Update()` loop to control the real-time contribution of that layer.
    *   **Layer Configuration (`DynamicLayerConfig`):** This is crucial. It allows you to define which Animator layers will be used for dynamic animations. You can specify:
        *   `LayerIndex`: The actual index in your `AnimatorController`.
        *   `SingleAnimationPerLayer`: Whether only one dynamic animation can play on this layer at a time (new ones override old ones).
        *   `DefaultLayerBlending`: Ensures the `Animator` layer's blending mode (e.g., `Override`, `Additive`) is correctly set at startup.
    *   **`PlayAnimation` Method:**
        *   Takes an `AnimationClip` and parameters like `layerIndex`, `blendInDuration`, `duration`, `targetWeight`, `isLooping`, etc.
        *   It first checks the `DynamicLayerConfig` for the specified `layerIndex`. If `SingleAnimationPerLayer` is true, it finds and requests a blend-out for any existing animations on that layer.
        *   It creates a new `ActiveDynamicAnimation` instance, assigns it a unique ID, and adds it to the `_activeAnimations` dictionary.
        *   It calls `_animator.PlayInFixedTime(clip.name, layerIndex, 0f, 0f)` to start the clip on the Animator layer. Importantly, the `initialWeight` is `0`, because *our system* will manage the layer's weight dynamically via `SetLayerWeight`.
        *   Returns a `DynamicAnimationHandle` for control.
    *   **`Update` Method:**
        *   Iterates through all `_activeAnimations`.
        *   Calls `anim.Update(deltaTime)` on each to progress their blending and time.
        *   Sets `_animator.SetLayerWeight(anim.LayerIndex, anim.CurrentWeight)` for each active animation. This is where the dynamic control happens – the system effectively "dials up" and "dials down" the influence of each Animator layer based on the animation's current blend state.
        *   Identifies and removes `IsFinished` animations to keep the system clean.
    *   **`StopAnimation` and `StopAnimationImmediately`:** Allow external control over active animations, either blending them out gracefully or instantly stopping them.

### **Benefits of this Pattern:**

*   **Runtime Flexibility:** Play any `AnimationClip` asset without needing to pre-bake it into an `AnimatorController` state machine.
*   **Reduced AnimatorController Complexity:** Avoids creating dozens or hundreds of states and transitions for one-shot or reactive animations. Your Animator Controller can focus on core locomotion and major states.
*   **Layered Animations:** Easily blend multiple dynamic animations (e.g., a "hit reaction" on Layer 1 + a "waving gesture" on Layer 2 + "base locomotion" on Layer 0).
*   **Dynamic Blending:** Fine-grained control over blend-in and blend-out durations and target weights, even while animations are active.
*   **Reusability:** The system is generic; you pass it any `AnimationClip`.
*   **Clean API:** `PlayAnimation` returns a handle, providing a clear way to manage specific instances.

This `DynamicAnimationSystem` provides a powerful tool for extending Unity's animation capabilities, enabling more complex and fluid character behaviors without sacrificing performance or maintainability.