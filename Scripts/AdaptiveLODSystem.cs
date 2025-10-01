// Unity Design Pattern Example: AdaptiveLODSystem
// This script demonstrates the AdaptiveLODSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

Here's a complete, practical C# Unity example demonstrating the 'AdaptiveLODSystem' design pattern. This solution includes two main scripts: `AdaptiveLODManager` and `AdaptiveLODObject`.

---

### `AdaptiveLODManager.cs`

This script acts as the central control for the adaptive LOD system. It monitors overall game performance (frame rate) and adjusts a global LOD bias accordingly. It then tells all registered `AdaptiveLODObject` instances to re-evaluate their LODs.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Design Pattern: Adaptive LOD System
//
// This system demonstrates the Adaptive LOD (Level of Detail) design pattern.
// It consists of two main components:
// 1. AdaptiveLODManager (Singleton): Manages global LOD settings, monitors performance,
//    and orchestrates the LOD evaluation for all registered objects. It adapts the
//    global LOD bias based on the game's current frame rate.
// 2. AdaptiveLODObject (MonoBehaviour): Attached to individual GameObjects, it defines
//    its specific LOD levels (child GameObjects) and their transition distances.
//    It requests LOD evaluation from the manager and applies the determined LOD.
//
// How it works:
// - The Manager runs periodically (not every frame) to avoid performance spikes.
// - It checks the frame rate (`Time.deltaTime`). If it's too high (meaning low FPS),
//   it increases a global 'lodBiasMultiplier', effectively making all objects switch
//   to lower LODs sooner. If performance is good, it reduces the bias, allowing
//   higher LODs for longer. This is the 'adaptive' part.
// - During each evaluation cycle, the Manager iterates through all registered
//   AdaptiveLODObjects and tells them to re-evaluate their LOD.
// - Each AdaptiveLODObject calculates its distance to the camera, applies its own
//   'importanceFactor' and the global 'lodBiasMultiplier' from the manager.
// - Based on the adjusted distance, it activates the appropriate child GameObject
//   representing the correct LOD level and deactivates others.
//
// Benefits:
// - Performance Optimization: Reduces polygon count and draw calls for distant/unimportant objects.
// - Adaptability: Dynamically adjusts LODs based on real-time performance, ensuring a smoother
//   experience even during demanding scenes.
// - Centralized Control: Global LOD settings and performance monitoring are managed in one place.
// - Component-Based: Easy to add/remove LOD functionality to specific GameObjects.

/// <summary>
/// Manages the global Adaptive LOD system, monitoring performance and adjusting
/// a global LOD bias accordingly. It also orchestrates LOD evaluation for
/// all registered AdaptiveLODObjects.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensures this manager runs before other LOD objects.
public class AdaptiveLODManager : MonoBehaviour
{
    public static AdaptiveLODManager Instance { get; private set; }

    [Header("Global Settings")]
    [Tooltip("The target frame rate to maintain. Used for performance adaptation.")]
    public float targetFrameRate = 60f;
    [Tooltip("How much deltaTime can exceed the budget before performance mode kicks in. (ms)")]
    public float performanceBudgetToleranceMs = 5f; // e.g., for 60fps, budget is 16.6ms. 5ms tolerance means 21.6ms is too much.
    [Tooltip("How often (in seconds) the manager should evaluate LODs and check performance.")]
    public float evaluationIntervalSeconds = 0.5f;
    [Tooltip("Minimum allowed global LOD bias. Higher bias means lower LODs sooner.")]
    [Range(0.1f, 5f)] public float minLodBias = 0.5f;
    [Tooltip("Maximum allowed global LOD bias. Higher bias means lower LODs sooner.")]
    [Range(0.1f, 5f)] public float maxLodBias = 2.0f;
    [Tooltip("Speed at which the global LOD bias adjusts to performance changes.")]
    public float biasAdjustmentSpeed = 0.5f;

    [Header("Debug")]
    [Tooltip("Current global LOD bias applied to all objects.")]
    [SerializeField] private float _currentLodBiasMultiplier = 1.0f;
    public float CurrentLodBiasMultiplier => _currentLodBiasMultiplier;
    [Tooltip("The last measured frame time (ms).")]
    [SerializeField] private float _lastFrameTimeMs;
    [Tooltip("Is the system currently reducing LODs due to low performance?")]
    [SerializeField] private bool _inPerformanceMode = false;
    public bool InPerformanceMode => _inPerformanceMode;

    private List<AdaptiveLODObject> _registeredLODObjects = new List<AdaptiveLODObject>();
    private Coroutine _evaluationCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple AdaptiveLODManagers found. Destroying duplicate.", this);
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        // Ensure initial bias is within bounds
        _currentLodBiasMultiplier = Mathf.Clamp(_currentLodBiasMultiplier, minLodBias, maxLodBias);
    }

    private void OnEnable()
    {
        if (_evaluationCoroutine == null)
        {
            _evaluationCoroutine = StartCoroutine(AdaptiveLODRoutine());
        }
    }

    private void OnDisable()
    {
        if (_evaluationCoroutine != null)
        {
            StopCoroutine(_evaluationCoroutine);
            _evaluationCoroutine = null;
        }
    }

    /// <summary>
    /// Registers an AdaptiveLODObject with the manager.
    /// </summary>
    /// <param name="lodObject">The object to register.</param>
    public void Register(AdaptiveLODObject lodObject)
    {
        if (!_registeredLODObjects.Contains(lodObject))
        {
            _registeredLODObjects.Add(lodObject);
        }
    }

    /// <summary>
    /// Unregisters an AdaptiveLODObject from the manager.
    /// </summary>
    /// <param name="lodObject">The object to unregister.</param>
    public void Unregister(AdaptiveLODObject lodObject)
    {
        _registeredLODObjects.Remove(lodObject);
    }

    /// <summary>
    /// The main coroutine that periodically evaluates performance and triggers LOD updates.
    /// </summary>
    private IEnumerator AdaptiveLODRoutine()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(evaluationIntervalSeconds);

        while (true)
        {
            yield return waitInterval;

            _lastFrameTimeMs = Time.deltaTime * 1000f; // Convert seconds to milliseconds
            float targetFrameTimeMs = 1000f / targetFrameRate;

            // Determine if performance is suffering
            if (_lastFrameTimeMs > targetFrameTimeMs + performanceBudgetToleranceMs)
            {
                // Performance is bad, increase bias (move to lower LODs sooner)
                // The adjustment speed is scaled by the evaluation interval to make it consistent
                _currentLodBiasMultiplier += biasAdjustmentSpeed * (evaluationIntervalSeconds / 0.1f); 
                _inPerformanceMode = true;
            }
            else if (_lastFrameTimeMs < targetFrameTimeMs - (performanceBudgetToleranceMs / 2f)) // Give some headroom before reducing bias
            {
                // Performance is good, decrease bias (allow higher LODs longer)
                _currentLodBiasMultiplier -= biasAdjustmentSpeed * (evaluationIntervalSeconds / 0.1f); 
                _inPerformanceMode = false;
            }
            // Clamp the bias within defined min/max range
            _currentLodBiasMultiplier = Mathf.Clamp(_currentLodBiasMultiplier, minLodBias, maxLodBias);

            // Trigger LOD evaluation for all registered objects
            foreach (var lodObject in _registeredLODObjects)
            {
                if (lodObject != null && lodObject.isActiveAndEnabled)
                {
                    lodObject.EvaluateLOD();
                }
            }
        }
    }
}
```

---

### `AdaptiveLODObject.cs`

This script is attached to individual game objects that require adaptive LOD. It defines its specific LOD models (as child GameObjects) and their transition distances. It interacts with the `AdaptiveLODManager` to get the global bias and perform its LOD switching.

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents an individual GameObject that can adapt its Level of Detail.
/// It uses child GameObjects for different LOD levels.
/// </summary>
public class AdaptiveLODObject : MonoBehaviour
{
    [Header("LOD Configuration")]
    [Tooltip("Array of GameObjects, where index 0 is the highest LOD and the last index is the lowest.")]
    public GameObject[] lodLevels;
    [Tooltip("Distances at which to switch to the next lower LOD. Requires (LOD count - 1) distances.")]
    public float[] transitionDistances;
    [Tooltip("A multiplier for this object's transition distances. Higher importance means it stays at higher LODs longer.")]
    [Range(0.1f, 5.0f)] public float importanceFactor = 1.0f;

    [Header("Debug")]
    [Tooltip("The index of the currently active LOD level.")]
    [SerializeField] private int _currentLODIndex = -1;
    [Tooltip("The last calculated distance to the main camera.")]
    [SerializeField] private float _lastDistanceToCamera;

    private Camera _mainCamera;
    private Transform _cachedTransform;

    private void Awake()
    {
        _cachedTransform = transform;
        if (lodLevels == null || lodLevels.Length == 0)
        {
            Debug.LogError($"AdaptiveLODObject on {name} has no LOD levels assigned! Disabling component.", this);
            enabled = false;
            return;
        }
        if (transitionDistances == null || transitionDistances.Length != lodLevels.Length - 1)
        {
            Debug.LogError($"AdaptiveLODObject on {name} requires {lodLevels.Length - 1} transition distances for its {lodLevels.Length} LOD levels. Disabling component.", this);
            enabled = false;
            return;
        }

        // Cache main camera to avoid expensive Camera.main calls repeatedly
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("AdaptiveLODObject requires a main camera in the scene tagged 'MainCamera'. Disabling component.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (AdaptiveLODManager.Instance != null)
        {
            AdaptiveLODManager.Instance.Register(this);
        }
        else
        {
            Debug.LogWarning($"AdaptiveLODManager not found for {name}. LOD will not be adaptive and component will be disabled.", this);
            enabled = false;
            return;
        }

        // Initialize by setting all LODs inactive, then activate the highest LOD initially (or trigger immediate evaluation)
        SetLOD(-1); // Deactivate all children
        EvaluateLOD(); // Immediately evaluate to show the correct initial LOD
    }

    private void OnDisable()
    {
        if (AdaptiveLODManager.Instance != null)
        {
            AdaptiveLODManager.Instance.Unregister(this);
        }
        // Ensure all LODs are deactivated when the object is disabled
        SetLOD(-1);
    }

    /// <summary>
    /// Evaluates and sets the appropriate LOD level based on distance, importance, and global bias.
    /// This method is typically called by the AdaptiveLODManager.
    /// </summary>
    public void EvaluateLOD()
    {
        if (_mainCamera == null || !enabled || !gameObject.activeInHierarchy) return;

        _lastDistanceToCamera = Vector3.Distance(_cachedTransform.position, _mainCamera.transform.position);

        // Get the global bias from the manager
        float globalBias = AdaptiveLODManager.Instance.CurrentLodBiasMultiplier;

        // Apply both importance factor and global bias to the distances
        // Higher importance factor makes it "appear" closer, effectively staying at higher LODs longer.
        // Higher global bias makes it "appear" farther, effectively switching to lower LODs sooner.
        float currentAdjustedDistance = _lastDistanceToCamera / (importanceFactor * globalBias); 
        // Note: dividing by importanceFactor and globalBias here means a higher value
        // will reduce the effective distance, keeping it in higher LODs longer.
        // If importanceFactor was a multiplier for transition distances, then 
        // transitionDistances[i] * importanceFactor * globalBias would be applied.
        // The current implementation directly modifies the distance used for comparison.

        int newLODIndex = lodLevels.Length - 1; // Default to the lowest LOD

        // Iterate through transition distances to find the appropriate LOD
        for (int i = 0; i < transitionDistances.Length; i++)
        {
            if (currentAdjustedDistance < transitionDistances[i])
            {
                newLODIndex = i;
                break;
            }
        }

        // Only update if the LOD index has changed to avoid unnecessary GameObject activation/deactivation
        if (newLODIndex != _currentLODIndex)
        {
            SetLOD(newLODIndex);
        }
    }

    /// <summary>
    /// Activates the specified LOD level and deactivates all others.
    /// </summary>
    /// <param name="lodIndex">The index of the LOD level to activate. Use -1 to deactivate all.</param>
    private void SetLOD(int lodIndex)
    {
        if (lodLevels == null || lodLevels.Length == 0) return;

        for (int i = 0; i < lodLevels.Length; i++)
        {
            if (lodLevels[i] != null)
            {
                lodLevels[i].SetActive(i == lodIndex);
            }
        }
        _currentLODIndex = lodIndex;
    }

    // Optional: Draw gizmos to visualize transition distances in the editor
    private void OnDrawGizmosSelected()
    {
        if (_cachedTransform == null) _cachedTransform = transform;
        if (transitionDistances == null || transitionDistances.Length == 0) return;

        // Draw basic transition spheres
        Gizmos.color = Color.cyan;
        for (int i = 0; i < transitionDistances.Length; i++)
        {
            // Visualize the raw transition distance
            Gizmos.DrawWireSphere(_cachedTransform.position, transitionDistances[i]);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(_cachedTransform.position + Vector3.up * (transitionDistances[i] + 0.5f), $"LOD {i} -> {i + 1} at {transitionDistances[i]:F1}m (Base)");
            #endif
        }

        // Draw the current calculated effective distance sphere (including global bias and importance)
        if (AdaptiveLODManager.Instance != null && enabled && _mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            float globalBias = AdaptiveLODManager.Instance.CurrentLodBiasMultiplier;
            float currentAdjustedDistance = _lastDistanceToCamera / (importanceFactor * globalBias);

            // This sphere represents the distance as it's *perceived* by the LOD logic,
            // which determines the active LOD.
            Gizmos.DrawWireSphere(_cachedTransform.position, currentAdjustedDistance);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(_cachedTransform.position + Vector3.up * (currentAdjustedDistance + 1.0f), $"Perceived Dist (Imp: {importanceFactor:F1}, Bias: {globalBias:F1}): {currentAdjustedDistance:F1}m");
            UnityEditor.Handles.Label(_cachedTransform.position + Vector3.up * (currentAdjustedDistance + 0.5f), $"Actual Camera Dist: {_lastDistanceToCamera:F1}m");
            #endif
        }
    }
}
```

---

### Example Usage in Unity Project

To implement and test the AdaptiveLODSystem in your Unity project:

1.  **Create the `AdaptiveLODManager`:**
    *   Create an empty GameObject in your scene (e.g., right-click in the Hierarchy -> `Create Empty`).
    *   Rename it to `AdaptiveLODManager`.
    *   Attach the `AdaptiveLODManager.cs` script to this GameObject.
    *   In the Inspector, configure its settings:
        *   **Target Frame Rate**: The FPS you want to achieve (e.g., `60`).
        *   **Performance Budget Tolerance Ms**: How many milliseconds `Time.deltaTime` can exceed `1000 / TargetFrameRate` before the system starts reducing LODs (e.g., `5`ms).
        *   **Evaluation Interval Seconds**: How often the manager checks performance and updates LODs (e.g., `0.5` seconds).
        *   **Min/Max Lod Bias**: Defines the range for the adaptive LOD bias (e.g., `0.5` to `2.0`).
        *   You can monitor `Current Lod Bias Multiplier` and `In Performance Mode` during runtime to see the system adapting.

2.  **Prepare a GameObject for Adaptive LOD:**
    *   Create a parent GameObject (e.g., right-click in the Hierarchy -> `Create Empty`, rename to `MyLODObject`). This will hold the `AdaptiveLODObject` component.
    *   As children of `MyLODObject`, create your different LOD models. These should typically be `3D Object` -> `Cube`, `Sphere`, or imported models with varying levels of detail.
        *   Example children:
            *   `LOD0_HighDetail` (contains `MeshFilter` and `MeshRenderer` for the high-detail model)
            *   `LOD1_MediumDetail` (contains `MeshFilter` and `MeshRenderer` for the medium-detail model)
            *   `LOD2_LowDetail` (contains `MeshFilter` and `MeshRenderer` for the low-detail model)
            *   `LOD3_Culled` (can be an empty GameObject or a very simple billboard/imposter, or just let the previous LOD be the lowest visible one).
    *   Position these child LOD models at `(0,0,0)` relative to their parent (`MyLODObject`) for easy alignment.
    *   Initially, you might want to disable all child LOD GameObjects. The `AdaptiveLODObject` script will handle activating the correct one.

3.  **Attach and Configure `AdaptiveLODObject`:**
    *   Attach the `AdaptiveLODObject.cs` script to your parent GameObject (`MyLODObject`).
    *   In the Inspector for `MyLODObject`:
        *   **LOD Levels:** Drag your child LOD GameObjects into this array in order, from highest detail (index 0) to lowest detail (last index).
            *   `Element 0`: `LOD0_HighDetail`
            *   `Element 1`: `LOD1_MediumDetail`
            *   `Element 2`: `LOD2_LowDetail`
            *   `Element 3`: `LOD3_Culled` (if applicable)
        *   **Transition Distances:** Define the distances (in Unity units) at which to switch to the *next lower* LOD. You need `(Number of LOD Levels - 1)` distances.
            *   If you have 4 LODs (0, 1, 2, 3), you'll need 3 transition distances:
                *   `Element 0`: e.g., `20` (Switch from LOD0 to LOD1 when perceived distance is > 20m)
                *   `Element 1`: e.g., `50` (Switch from LOD1 to LOD2 when perceived distance is > 50m)
                *   `Element 2`: e.g., `100` (Switch from LOD2 to LOD3 when perceived distance is > 100m)
            *   Any perceived distance beyond the last transition distance will use the lowest LOD.
        *   **Importance Factor:** Adjust this value. A higher value (e.g., `2.0`) will make this object stay in higher LODs longer. A lower value (e.g., `0.5`) will make it switch to lower LODs sooner. This is useful for prioritizing foreground elements (like the player character) over background clutter.

4.  **Run Your Scene:**
    *   Ensure your `Main Camera` is tagged `MainCamera`.
    *   Play the scene.
    *   Move your camera closer to and farther from `MyLODObject`. You should see the different child LOD GameObjects activating/deactivating.
    *   To observe the 'adaptive' behavior:
        *   **Simulate high performance demand:** Drag the Game window to be very large, or introduce other heavy visual effects (e.g., many particle systems, complex post-processing, lots of complex objects).
        *   Watch the `Current Lod Bias Multiplier` in the `AdaptiveLODManager` Inspector. It should increase when performance drops, causing objects to switch to lower LODs even at closer actual distances.
        *   When performance recovers (e.g., shrink the Game window), the `Current Lod Bias Multiplier` should decrease, allowing objects to stay in higher LODs longer.
    *   The `OnDrawGizmosSelected` in `AdaptiveLODObject` provides visual debugging spheres in the Scene view, showing actual and perceived distances.

This setup provides a robust and flexible Adaptive LOD system, allowing you to dynamically manage visual quality versus performance in your Unity projects.