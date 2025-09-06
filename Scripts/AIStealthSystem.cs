// Unity Design Pattern Example: AIStealthSystem
// This script demonstrates the AIStealthSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

This C# Unity example demonstrates the 'AIStealthSystem' design pattern. The pattern focuses on a clear separation of concerns:

1.  **StealthTarget**: Represents an entity that can be stealthy (e.g., player). It provides its stealth-related properties and status.
2.  **AIObserver**: Represents an entity that perceives targets (e.g., an enemy AI). It defines its perception capabilities and manages its detection state.
3.  **AIStealthSystem (Static Helper)**: This is the core of the pattern. It's a static utility class that encapsulates the *rules* and *calculations* for how an `AIObserver` detects a `StealthTarget`. It centralizes the detection logic, making it reusable and easy to modify without changing the individual `StealthTarget` or `AIObserver` components.

By separating these responsibilities, we achieve a flexible, maintainable, and scalable system for AI stealth mechanics.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// =========================================================================================================
// AIStealthSystem: The Core Detection Logic (Static Helper Class)
//
// This static class encapsulates the rules and calculations for how an AIObserver detects a StealthTarget.
// It acts as the central brain for all detection-related computations, making the system flexible
// and easy to modify. Any AIObserver can query this system to determine detection.
// =========================================================================================================

/// <summary>
/// Static helper class containing the core logic for AI stealth detection.
/// This centralizes detection rules, making them reusable and easily modifiable.
/// </summary>
public static class AIStealthSystem
{
    /// <summary>
    /// Calculates a raw detection factor from an observer to a target.
    /// This factor is a normalized value (0.0 to 1.0) indicating how "detectable" the target is
    /// to this specific observer, taking into account various environmental and entity properties.
    /// </summary>
    /// <param name="target">The StealthTarget being observed.</param>
    /// <param name="observer">The AIObserver doing the observing.</param>
    /// <param name="obstructionLayer">LayerMask for environment obstructions to check Line of Sight.</param>
    /// <returns>A float representing the raw detection factor (0.0 = completely undetectable, 1.0 = fully detectable).</returns>
    public static float CalculateRawDetectionFactor(StealthTarget target, AIObserver observer, LayerMask obstructionLayer)
    {
        if (target == null || observer == null)
        {
            return 0f;
        }

        // --- 1. Distance Factor ---
        // Shorter distance increases detection.
        float distance = Vector3.Distance(observer.transform.position, target.transform.position);
        float distanceFactor = 1.0f - Mathf.Clamp01(distance / observer.PerceptionRange);
        if (distance > observer.PerceptionRange)
        {
            return 0f; // Target is out of range, cannot be detected
        }

        // --- 2. Field of View Factor ---
        // Target must be within the AI's frontal cone of vision.
        float fovFactor = IsInFieldOfView(observer.transform, target.transform, observer.FieldOfViewAngle) ? 1.0f : 0.0f;
        if (fovFactor == 0f)
        {
            // If target is not in FoV, detection is very low, but not necessarily zero
            // unless the AI has no "peripheral" or "sound" perception.
            // For simplicity, we'll make it very difficult but allow for some passive detection
            // even if not directly seen if other factors are high.
            fovFactor = 0.1f; // A small baseline for "out of direct view" detection
        }


        // --- 3. Line of Sight (LoS) Factor ---
        // Obstructions reduce detection significantly.
        float losFactor = HasLineOfSight(observer.transform, target.transform, obstructionLayer) ? 1.0f : 0.1f; // Small residual if not direct LoS (e.g., hearing through wall)

        // --- 4. Target's Stealth Level Factor ---
        // A higher stealth level from the target reduces detection.
        // We invert this to make it a "detectability" factor.
        float targetStealthFactor = Mathf.Clamp01(1.0f - target.GetEffectiveStealthLevel());

        // --- 5. Target's Visibility Factor (e.g., light level, cover) ---
        // How visible the target is in its current environment.
        float targetVisibilityFactor = target.GetVisibilityFactor();

        // --- 6. Target's Noise Factor (e.g., movement speed, actions) ---
        // How much noise the target is making.
        float targetNoiseFactor = target.GetNoiseFactor();

        // --- 7. Observer's Perception Sensitivity ---
        // How good the observer is at perceiving.
        float observerPerceptionFactor = observer.BasePerceptionSensitivity;

        // --- Combine Factors (Weighted) ---
        // This is where the "art" of game design comes in.
        // Adjust weights based on what's most important in your game.
        float combinedFactor =
            (distanceFactor * 0.3f) +            // Distance is important
            (fovFactor * 0.2f) +                 // FoV is important
            (losFactor * 0.2f) +                 // LoS is very important
            (targetStealthFactor * 0.15f) +      // Target's base stealth
            (targetVisibilityFactor * 0.1f) +    // Environmental visibility
            (targetNoiseFactor * 0.05f);         // Noise is often a smaller contributor, but can spike

        // Apply observer's perception sensitivity and clamp final value
        return Mathf.Clamp01(combinedFactor * observerPerceptionFactor);
    }

    /// <summary>
    /// Checks if the target is within the observer's field of view angle.
    /// </summary>
    /// <param name="observerTransform">Transform of the observer.</param>
    /// <param name="targetTransform">Transform of the target.</param>
    /// <param name="fovAngle">The observer's field of view angle in degrees.</param>
    /// <returns>True if the target is within the FOV, false otherwise.</returns>
    public static bool IsInFieldOfView(Transform observerTransform, Transform targetTransform, float fovAngle)
    {
        Vector3 directionToTarget = (targetTransform.position - observerTransform.position).normalized;
        float angle = Vector3.Angle(observerTransform.forward, directionToTarget);
        return angle < fovAngle * 0.5f; // FOV is usually total angle, so we compare to half
    }

    /// <summary>
    /// Checks for a clear line of sight between the observer and the target.
    /// </summary>
    /// <param name="observerTransform">Transform of the observer (raycast origin).</param>
    /// <param name="targetTransform">Transform of the target (raycast destination).</param>
    /// <param name="obstructionLayer">LayerMask for objects that should block line of sight.</param>
    /// <returns>True if there's an unobstructed line of sight, false otherwise.</returns>
    public static bool HasLineOfSight(Transform observerTransform, Transform targetTransform, LayerMask obstructionLayer)
    {
        Vector3 origin = observerTransform.position + Vector3.up * 0.5f; // Ray from slightly above ground
        Vector3 targetPos = targetTransform.position + Vector3.up * 0.5f; // To center of target
        Vector3 direction = (targetPos - origin).normalized;
        float distance = Vector3.Distance(origin, targetPos);

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, obstructionLayer))
        {
            // Something blocked the view before reaching the target.
            // Ensure the hit object isn't the target itself.
            if (hit.collider.transform != targetTransform && hit.collider.transform.root != targetTransform.root)
            {
                return false;
            }
        }
        return true; // No obstruction or hit the target itself
    }
}

// =========================================================================================================
// StealthTarget: Represents an entity that can be stealthy
//
// Attach this component to a GameObject (e.g., your Player) that needs to have stealth mechanics.
// It exposes properties that influence how easily it can be detected by AIObservers.
// =========================================================================================================

/// <summary>
/// Component attached to game objects that can be stealthy (e.g., the player).
/// It provides properties and methods that AIObservers use to calculate detection.
/// </summary>
public class StealthTarget : MonoBehaviour
{
    [Header("Stealth Properties")]
    [Tooltip("Base stealth modifier. Higher value means more stealthy (0.0 = invisible, 1.0 = normal).")]
    [SerializeField] private float baseStealthModifier = 0.5f; // 0.0 (very stealthy) to 1.0 (not stealthy)

    [Tooltip("Multiplier for how much noise movement makes. Higher value means more noise.")]
    [SerializeField] private float movementNoiseMultiplier = 1.0f;

    [Tooltip("Multiplier for visibility based on environmental factors (e.g., light, cover).")]
    [SerializeField] private float baseVisibilityMultiplier = 1.0f; // 0.0 (fully hidden) to 1.0 (fully exposed)

    private Vector3 _previousPosition;
    private float _currentMovementSpeed;

    void Start()
    {
        _previousPosition = transform.position;
    }

    void Update()
    {
        // Calculate current movement speed for noise factor
        _currentMovementSpeed = (transform.position - _previousPosition).magnitude / Time.deltaTime;
        _previousPosition = transform.position;

        // Example: Update visibility based on light levels (simple mock)
        // In a real game, this would involve raycasts to light sources, checking cover, etc.
        // For this example, we'll assume a constant base visibility unless overridden.
        // currentVisibilityMultiplier = CalculateEnvironmentalVisibility();
    }

    /// <summary>
    /// Gets the target's effective stealth level.
    /// Higher values mean the target is more stealthy (less detectable).
    /// </summary>
    /// <returns>A float representing the current stealth level (0.0 to 1.0).</returns>
    public float GetEffectiveStealthLevel()
    {
        // This can be further modified by temporary buffs, debuffs, equipment, etc.
        // For now, it's just the base.
        return baseStealthModifier;
    }

    /// <summary>
    /// Gets a factor representing how visible the target is due to environmental conditions.
    /// Higher values mean more visible (more detectable).
    /// </summary>
    /// <returns>A float representing the current visibility factor (0.0 to 1.0).</returns>
    public float GetVisibilityFactor()
    {
        // In a real game, this would involve checking:
        // - Light level at target's position
        // - Is target behind cover?
        // - Specific character abilities
        // For this example, it's a base value.
        return baseVisibilityMultiplier;
    }

    /// <summary>
    /// Gets a factor representing how much noise the target is making.
    /// Higher values mean more noise (more detectable).
    /// </summary>
    /// <returns>A float representing the current noise factor (0.0 to 1.0).</returns>
    public float GetNoiseFactor()
    {
        // Example: Noise scales with movement speed
        // You can add more factors like shooting, opening doors, etc.
        float noiseFromMovement = Mathf.Clamp01(_currentMovementSpeed * movementNoiseMultiplier * 0.1f); // Adjust 0.1f for sensitivity
        return noiseFromMovement;
    }

    // You could add methods here to temporarily modify stealth, e.g.:
    // public void ApplyStealthBuff(float amount, float duration) { ... }
}

// =========================================================================================================
// AIObserver: Represents an AI character that perceives and reacts to StealthTargets
//
// Attach this component to any AI GameObject that needs to detect targets.
// It uses the AIStealthSystem to perform detection calculations and manages its
// own internal detection state.
// =========================================================================================================

/// <summary>
/// Represents an AI character that observes StealthTargets.
/// It uses the static AIStealthSystem to calculate detection and manages its
/// own internal state (Unaware, Suspicious, Detected).
/// </summary>
public class AIObserver : MonoBehaviour
{
    public enum AIDetectionState { Unaware, Suspicious, Detected }

    [Header("Perception Properties")]
    [Tooltip("Maximum range within which the AI can perceive targets.")]
    [SerializeField] private float perceptionRange = 20f;
    public float PerceptionRange => perceptionRange;

    [Tooltip("Total angle (in degrees) of the AI's field of view.")]
    [SerializeField] private float fieldOfViewAngle = 120f;
    public float FieldOfViewAngle => fieldOfViewAngle;

    [Tooltip("Base sensitivity of the AI's perception. Higher value makes detection easier.")]
    [SerializeField] private float basePerceptionSensitivity = 1.0f; // Higher = more sensitive
    public float BasePerceptionSensitivity => basePerceptionSensitivity;

    [Tooltip("LayerMask for environment objects that block line of sight (e.g., 'Default', 'Environment').")]
    [SerializeField] private LayerMask obstructionLayer;

    [Header("Detection Thresholds")]
    [Tooltip("Detection accumulation needed to transition from Suspicious to Detected.")]
    [SerializeField] private float detectedThreshold = 0.8f;

    [Tooltip("Detection accumulation needed to transition from Unaware to Suspicious.")]
    [SerializeField] private float suspiciousThreshold = 0.3f;

    [Tooltip("Rate at which detection accumulation decreases when the target is not being actively detected.")]
    [SerializeField] private float loseDetectionRate = 0.1f;

    [Header("Current State (Read Only)")]
    [Tooltip("Current detection state of this AI towards its primary target.")]
    [SerializeField] private AIDetectionState currentDetectionState = AIDetectionState.Unaware;
    public AIDetectionState CurrentDetectionState => currentDetectionState;

    [Tooltip("Accumulated detection level for the primary target (0.0 to 1.0).")]
    [SerializeField] private float currentDetectionAccumulation = 0f;
    public float CurrentDetectionAccumulation => currentDetectionAccumulation;

    private StealthTarget _currentObservedTarget; // The target currently being observed/tracked

    void Start()
    {
        // In a real game, you might find targets dynamically or have a specific player target.
        // For this example, let's assume there's a single player target tagged "Player".
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _currentObservedTarget = playerObj.GetComponent<StealthTarget>();
            if (_currentObservedTarget == null)
            {
                Debug.LogWarning("Player GameObject found but does not have a StealthTarget component. AI will not detect it.", playerObj);
            }
        }
        else
        {
            Debug.LogWarning("No GameObject tagged 'Player' found in the scene. AIObserver will not function.");
        }
    }

    void Update()
    {
        if (_currentObservedTarget == null)
        {
            currentDetectionAccumulation = 0;
            currentDetectionState = AIDetectionState.Unaware;
            return;
        }

        // --- Step 1: Calculate raw detection factor using the AIStealthSystem ---
        float rawDetectionFactor = AIStealthSystem.CalculateRawDetectionFactor(
            _currentObservedTarget, this, obstructionLayer);

        // --- Step 2: Accumulate or decay detection based on the raw factor ---
        if (rawDetectionFactor > 0)
        {
            // If there's any detectability, increase accumulation
            currentDetectionAccumulation += rawDetectionFactor * Time.deltaTime;
        }
        else
        {
            // If not detectable (e.g., out of range, fully hidden), decrease accumulation
            currentDetectionAccumulation -= loseDetectionRate * Time.deltaTime;
        }

        // Clamp detection accumulation between 0 and 1
        currentDetectionAccumulation = Mathf.Clamp01(currentDetectionAccumulation);

        // --- Step 3: Update AI's internal detection state ---
        UpdateDetectionState();

        // --- Step 4: React based on the current state ---
        PerformActionBasedOnState();
    }

    /// <summary>
    /// Updates the AI's internal detection state based on current detection accumulation.
    /// </summary>
    private void UpdateDetectionState()
    {
        if (currentDetectionAccumulation >= detectedThreshold)
        {
            // Transition to Detected
            if (currentDetectionState != AIDetectionState.Detected)
            {
                Debug.Log($"{gameObject.name}: Target {_currentObservedTarget.name} DETECTED!");
            }
            currentDetectionState = AIDetectionState.Detected;
        }
        else if (currentDetectionAccumulation >= suspiciousThreshold)
        {
            // Transition to Suspicious
            if (currentDetectionState != AIDetectionState.Suspicious)
            {
                Debug.Log($"{gameObject.name}: Target {_currentObservedTarget.name} is SUSPICIOUS.");
            }
            currentDetectionState = AIDetectionState.Suspicious;
        }
        else
        {
            // Transition to Unaware
            if (currentDetectionState != AIDetectionState.Unaware)
            {
                Debug.Log($"{gameObject.name}: Target {_currentObservedTarget.name} lost detection, UNAWARE.");
            }
            currentDetectionState = AIDetectionState.Unaware;
        }
    }

    /// <summary>
    /// Performs AI actions based on the current detection state.
    /// This is where the AI's behavior tree or state machine would hook in.
    /// </summary>
    private void PerformActionBasedOnState()
    {
        switch (currentDetectionState)
        {
            case AIDetectionState.Unaware:
                // AI is unaware, continue patrolling or idle.
                // Debug.Log($"{gameObject.name} is Unaware.");
                break;
            case AIDetectionState.Suspicious:
                // AI is suspicious, investigate last known position, look around.
                // Debug.Log($"{gameObject.name} is Suspicious. Investigating...");
                break;
            case AIDetectionState.Detected:
                // AI has fully detected the target, pursue, attack, or call for backup.
                // Debug.Log($"{gameObject.name} is Detected! Engaging!");
                break;
        }
        // You could also trigger events here:
        // OnDetectionStateChanged?.Invoke(currentDetectionState, _currentObservedTarget);
    }

    /// <summary>
    /// Draws debug visualizations for the AI's perception range and field of view.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw perception range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perceptionRange);

        // Draw field of view
        Gizmos.color = Color.cyan;
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, transform.up) * transform.forward * perceptionRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, transform.up) * transform.forward * perceptionRange;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
        Gizmos.DrawLine(transform.position + fovLine1, transform.position + fovLine2); // Connecting the arc visually
        
        // Indicate current detection state
        switch (currentDetectionState)
        {
            case AIDetectionState.Unaware:
                Gizmos.color = Color.green;
                break;
            case AIDetectionState.Suspicious:
                Gizmos.color = Color.yellow;
                break;
            case AIDetectionState.Detected:
                Gizmos.color = Color.red;
                break;
        }
        // Draw a small sphere above the AI to indicate its state
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.2f);
    }
}


/*
=========================================================================================================
EXAMPLE USAGE IN A UNITY PROJECT:
=========================================================================================================

1.  Create a new C# script named "AIStealthSystemDemo" (or whatever you prefer) and copy
    all the code above into it.

2.  **Create your Player GameObject:**
    *   Create an empty GameObject (e.g., right-click in Hierarchy -> Create Empty).
    *   Rename it to "Player".
    *   Add a `Capsule Collider` and `Rigidbody` (set `isKinematic` to true if you don't want physics movement, or attach a simple player movement script).
    *   **Add the `StealthTarget` component to this "Player" GameObject.**
    *   Ensure the "Player" GameObject has the tag "Player" (select GameObject, in Inspector click Tag dropdown -> Add Tag..., create "Player" tag, then assign it).
    *   Adjust `StealthTarget` properties in the Inspector if desired (e.g., `Base Stealth Modifier`).

3.  **Create your AI Enemy GameObject:**
    *   Create another empty GameObject.
    *   Rename it to "EnemyAI".
    *   Add a `Capsule Collider` and `Rigidbody` (optional, for visual representation).
    *   **Add the `AIObserver` component to this "EnemyAI" GameObject.**
    *   Adjust `AIObserver` properties in the Inspector:
        *   `Perception Range`: How far the AI can see.
        *   `Field Of View Angle`: The AI's vision cone (e.g., 120 degrees).
        *   `Base Perception Sensitivity`: How keen its senses are.
        *   `Obstruction Layer`: Crucial! Create a new Layer (e.g., "Environment") in the Layers dropdown (top right of Unity Editor). Assign your ground, walls, and any other objects that should block vision to this layer. Then, in the `AIObserver` Inspector, select this new layer for the `Obstruction Layer` field.
        *   `Detected Threshold`, `Suspicious Threshold`: Fine-tune how quickly the AI reacts.

4.  **Create an Environment:**
    *   Create some 3D objects (e.g., Cube for ground, more Cubes for walls).
    *   Ensure your walls/ground are on the `Obstruction Layer` you defined in step 3.

5.  **Run the Scene:**
    *   Move the "Player" GameObject around.
    *   Observe the "EnemyAI" in the scene view while running. You'll see:
        *   Yellow wire sphere for `Perception Range`.
        *   Cyan cone for `Field Of View`.
        *   A small sphere above the AI changing color (Green = Unaware, Yellow = Suspicious, Red = Detected).
    *   Check the Console for debug messages about detection state changes.

**Experiment:**
*   Move the Player slowly vs. quickly to see `Noise Factor` impact.
*   Hide the Player behind a wall (on the obstruction layer) to see `Line Of Sight` impact.
*   Move the Player out of the AI's FOV but within range to see it become "Suspicious" slowly.
*   Adjust the `StealthTarget`'s `Base Stealth Modifier` to make the player more or less detectable.
*   Adjust `AIObserver`'s `Perception Range` or `Base Perception Sensitivity`.
*/
```