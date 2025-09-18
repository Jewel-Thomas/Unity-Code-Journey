// Unity Design Pattern Example: LightProbeSystem
// This script demonstrates the LightProbeSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "LightProbeSystem" pattern, as a custom design pattern, draws inspiration from Unity's built-in Light Probe system. The core idea is to sample environmental data at discrete, static points (probes) and then allow dynamic objects to query and interpolate this data based on their current position. This avoids the need for complex trigger volumes or constant computations for every dynamic object.

This pattern is highly useful for:
*   **Environmental Context:** Providing dynamic objects with information about their surroundings (e.g., wind direction, danger level, resource density, "mood" of an area).
*   **Dynamic Visuals/Audio:** Changing post-processing effects, material properties, or audio reverb/mix based on location without complex collider setups.
*   **AI Behavior:** Guiding AI agents based on contextual information (e.g., "is this area safe?", "is there loot nearby?").

---

### **LightProbeSystem Pattern Components:**

1.  **`EnvironmentalContextData` (Data Structure):** Defines the type of environmental information each probe stores. This can be anything: colors, floats, vectors, enums, or references to ScriptableObjects.
2.  **`ContextProbe` (MonoBehaviour):**
    *   Represents a static point in the scene.
    *   Holds an instance of `EnvironmentalContextData`.
    *   Registers itself with the `ContextProbeSystem` when enabled and unregisters when disabled.
    *   Often visualizes its data in the Unity Editor using `Gizmos`.
3.  **`ContextProbeSystem` (MonoBehaviour, Singleton):**
    *   Manages all active `ContextProbe` instances in the scene.
    *   Provides methods for clients to query environmental data at a given position.
    *   Responsible for finding the nearest probes and interpolating their data (e.g., weighted average based on distance).
4.  **`ContextProbeClient` (MonoBehaviour):**
    *   A dynamic object that needs environmental data.
    *   Periodically queries the `ContextProbeSystem` for data relevant to its current position.
    *   Applies the received data to itself (e.g., changes its material color, modifies an audio source, updates AI state).

---

### **Complete C# Unity Example**

Below is a single C# script ready to be dropped into a Unity project. It demonstrates changing an object's material color and intensity based on its proximity to various "mood" probes.

**File:** `LightProbeSystemExample.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // For LINQ operations, though optimized manual sorting is used.

namespace LightProbeSystem
{
    // --- 1. EnvironmentalContextData (Data Structure) ---
    /// <summary>
    /// Defines the type of environmental data that each probe will store and clients will receive.
    /// This can be customized to include any kind of data relevant to your game.
    /// </summary>
    public struct EnvironmentalContextData
    {
        public Color AmbientColor;
        public float Intensity;
        // Add more context data as needed, e.g.:
        // public float WindSpeed;
        // public Vector3 WindDirection;
        // public AudioReverbPreset ReverbPreset; // Requires an AudioSource component

        /// <summary>
        /// Linearly interpolates between two EnvironmentalContextData structures.
        /// Useful if a simpler interpolation method is desired over weighted averaging.
        /// </summary>
        public static EnvironmentalContextData Lerp(EnvironmentalContextData a, EnvironmentalContextData b, float t)
        {
            return new EnvironmentalContextData
            {
                AmbientColor = Color.Lerp(a.AmbientColor, b.AmbientColor, t),
                Intensity = Mathf.Lerp(a.Intensity, b.Intensity, t)
                // Lerp other fields here
            };
        }

        public override string ToString()
        {
            return $"Color: {AmbientColor}, Intensity: {Intensity:F2}";
        }

        /// <summary>
        /// Returns a default, empty EnvironmentalContextData instance.
        /// </summary>
        public static EnvironmentalContextData Default => new EnvironmentalContextData { AmbientColor = Color.gray, Intensity = 0.5f };
    }

    // --- 2. ContextProbe (MonoBehaviour) ---
    /// <summary>
    /// Represents a static point in the scene that provides environmental context data.
    /// Acts like a 'light probe' but for custom data.
    /// </summary>
    [ExecuteInEditMode] // Allows gizmos and updates in editor without playing
    public class ContextProbe : MonoBehaviour
    {
        [Header("Probe Configuration")]
        [Tooltip("The environmental data this probe provides.")]
        [SerializeField] private EnvironmentalContextData _data = EnvironmentalContextData.Default;

        [Header("Editor Visualization")]
        [Tooltip("Color of the probe gizmo in the editor.")]
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [Tooltip("Radius of the probe gizmo.")]
        [SerializeField] private float _gizmoRadius = 0.5f;

        /// <summary>
        /// Public accessor for the probe's data.
        /// </summary>
        public EnvironmentalContextData Data => _data;

        /// <summary>
        /// Registers this probe with the ContextProbeSystem when it becomes active.
        /// </summary>
        private void OnEnable()
        {
            if (ContextProbeSystem.Instance != null)
            {
                ContextProbeSystem.Instance.RegisterProbe(this);
            }
        }

        /// <summary>
        /// Unregisters this probe from the ContextProbeSystem when it becomes inactive or destroyed.
        /// </summary>
        private void OnDisable()
        {
            if (ContextProbeSystem.Instance != null)
            {
                ContextProbeSystem.Instance.UnregisterProbe(this);
            }
        }

        /// <summary>
        /// Draws a gizmo in the editor to visualize the probe's position and data.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw a wire sphere representing the probe's position
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);

            // Draw a solid sphere to represent the probe's data visually
            Gizmos.color = new Color(_data.AmbientColor.r, _data.AmbientColor.g, _data.AmbientColor.b, 0.4f); // Semi-transparent
            Gizmos.DrawSphere(transform.position, _gizmoRadius);

            // Optional: Draw text to show data (requires UnityEditor namespace, so keep simple for runtime script)
            // Handles.Label(transform.position + Vector3.up * (gizmoRadius + 0.1f), $"Color: {_data.AmbientColor}\nIntensity: {_data.Intensity}");
        }
    }

    // --- 3. ContextProbeSystem (MonoBehaviour, Singleton) ---
    /// <summary>
    /// A singleton manager that keeps track of all active ContextProbes
    /// and provides methods for clients to query environmental data.
    /// This is analogous to Unity's Light Probe Group.
    /// </summary>
    [DisallowMultipleComponent]
    public class ContextProbeSystem : MonoBehaviour
    {
        public static ContextProbeSystem Instance { get; private set; }

        [Header("System Configuration")]
        [Tooltip("The maximum number of nearest probes to consider for interpolation.")]
        [SerializeField] private int _maxProbesToConsider = 4;
        [Tooltip("If true, data will be interpolated from multiple nearest probes. If false, only the closest probe's data is returned.")]
        [SerializeField] private bool _interpolateData = true;
        [Tooltip("Minimum distance threshold to prevent extreme weighting when a client is exactly at a probe's position.")]
        [SerializeField] private float _minDistanceThreshold = 0.01f;

        private List<ContextProbe> _registeredProbes = new List<ContextProbe>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple ContextProbeSystem instances found! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Registers a ContextProbe with the system. Called by ContextProbe.OnEnable().
        /// </summary>
        /// <param name="probe">The probe to register.</param>
        public void RegisterProbe(ContextProbe probe)
        {
            if (!_registeredProbes.Contains(probe))
            {
                _registeredProbes.Add(probe);
                // Debug.Log($"Probe registered: {probe.name}. Total probes: {_registeredProbes.Count}");
            }
        }

        /// <summary>
        /// Unregisters a ContextProbe from the system. Called by ContextProbe.OnDisable().
        /// </summary>
        /// <param name="probe">The probe to unregister.</param>
        public void UnregisterProbe(ContextProbe probe)
        {
            if (_registeredProbes.Remove(probe))
            {
                // Debug.Log($"Probe unregistered: {probe.name}. Total probes: {_registeredProbes.Count}");
            }
        }

        /// <summary>
        /// Queries the system for environmental data at a given world position.
        /// It finds the nearest probes and interpolates their data.
        /// </summary>
        /// <param name="queryPosition">The world position to query from.</param>
        /// <returns>The interpolated (or closest) EnvironmentalContextData.</returns>
        public EnvironmentalContextData GetProbeData(Vector3 queryPosition)
        {
            if (_registeredProbes == null || _registeredProbes.Count == 0)
            {
                return EnvironmentalContextData.Default; // No probes, return default data.
            }

            // --- Step 1: Find the N nearest probes ---
            // Using a simple list and sorting for demonstration. For very large numbers of probes (>1000s),
            // consider spatial partitioning (e.g., octree) for faster nearest neighbor queries.
            var nearestProbes = new List<(ContextProbe probe, float sqrDistance)>();

            foreach (var probe in _registeredProbes)
            {
                float sqrDist = (queryPosition - probe.transform.position).sqrMagnitude;
                nearestProbes.Add((probe, sqrDist));
            }

            // Sort by squared distance to find the closest ones
            nearestProbes.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

            // Take only the top N probes to consider
            List<(ContextProbe probe, float sqrDistance)> relevantProbes = nearestProbes
                .Take(Mathf.Min(_maxProbesToConsider, nearestProbes.Count))
                .ToList();

            if (relevantProbes.Count == 0)
            {
                return EnvironmentalContextData.Default;
            }

            // If interpolation is off or only one relevant probe, return the closest one's data directly
            if (!_interpolateData || relevantProbes.Count == 1)
            {
                return relevantProbes[0].probe.Data;
            }

            // --- Step 2: Interpolate data from the nearest probes ---
            EnvironmentalContextData interpolatedData = new EnvironmentalContextData();
            float totalWeight = 0f;

            foreach (var (probe, sqrDistance) in relevantProbes)
            {
                // Clamp squared distance to prevent division by zero or extremely large weights
                // when the client is very close or exactly at a probe's position.
                float actualSqrDistance = Mathf.Max(sqrDistance, _minDistanceThreshold * _minDistanceThreshold);

                // Use inverse square distance for weighting (closer probes have more influence)
                float weight = 1f / actualSqrDistance;

                // Accumulate weighted data
                interpolatedData.AmbientColor += probe.Data.AmbientColor * weight;
                interpolatedData.Intensity += probe.Data.Intensity * weight;
                // Add other data fields here for weighted accumulation
                // e.g., interpolatedData.WindSpeed += probe.Data.WindSpeed * weight;

                totalWeight += weight;
            }

            // Normalize the accumulated data by the total weight
            if (totalWeight > 0)
            {
                interpolatedData.AmbientColor /= totalWeight;
                interpolatedData.Intensity /= totalWeight;
                // Divide other data fields here
            }

            return interpolatedData;
        }
    }

    // --- 4. ContextProbeClient (MonoBehaviour) ---
    /// <summary>
    /// An example client that queries the ContextProbeSystem and applies the received data.
    /// This specific client changes the object's material color and intensity.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ContextProbeClient : MonoBehaviour
    {
        [Header("Client Configuration")]
        [Tooltip("How often the client updates its context data (in seconds).")]
        [SerializeField] private float _updateInterval = 0.2f;
        [Tooltip("How quickly the client's material color adapts to the new context.")]
        [SerializeField] private float _colorLerpSpeed = 5f;
        [Tooltip("How quickly the client's material intensity adapts to the new context.")]
        [SerializeField] private float _intensityLerpSpeed = 5f;

        private MeshRenderer _meshRenderer;
        private Coroutine _updateCoroutine;
        private EnvironmentalContextData _currentContextData; // The data we are currently moving towards

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                Debug.LogError("ContextProbeClient requires a MeshRenderer component!", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_meshRenderer != null)
            {
                // Ensure initial context is set
                _currentContextData = ContextProbeSystem.Instance?.GetProbeData(transform.position) ?? EnvironmentalContextData.Default;
                ApplyContextDataInstant(_currentContextData); // Apply immediately on enable

                // Start periodic updates
                _updateCoroutine = StartCoroutine(UpdateContextRoutine());
            }
        }

        private void OnDisable()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }

        private IEnumerator UpdateContextRoutine()
        {
            while (true)
            {
                // Wait for the next update interval
                yield return new WaitForSeconds(_updateInterval);

                // Query the ContextProbeSystem for the current position's data
                if (ContextProbeSystem.Instance != null)
                {
                    _currentContextData = ContextProbeSystem.Instance.GetProbeData(transform.position);
                }
                else
                {
                    _currentContextData = EnvironmentalContextData.Default;
                }
            }
        }

        private void Update()
        {
            // Smoothly interpolate towards the _currentContextData received from the system
            ApplyContextDataSmooth(_currentContextData);
        }

        /// <summary>
        /// Applies the context data instantly to the MeshRenderer.
        /// </summary>
        /// <param name="data">The context data to apply.</param>
        private void ApplyContextDataInstant(EnvironmentalContextData data)
        {
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.color = data.AmbientColor;
                _meshRenderer.material.SetFloat("_EmissionIntensity", data.Intensity); // Assumes a material with _EmissionIntensity
                _meshRenderer.material.EnableKeyword("_EMISSION"); // Ensure emission is enabled
            }
        }

        /// <summary>
        /// Smoothly interpolates and applies the context data to the MeshRenderer over time.
        /// </summary>
        /// <param name="targetData">The target context data to interpolate towards.</param>
        private void ApplyContextDataSmooth(EnvironmentalContextData targetData)
        {
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                Color currentColor = _meshRenderer.material.color;
                float currentIntensity = _meshRenderer.material.HasProperty("_EmissionIntensity") ? _meshRenderer.material.GetFloat("_EmissionIntensity") : 0f;

                Color newColor = Color.Lerp(currentColor, targetData.AmbientColor, Time.deltaTime * _colorLerpSpeed);
                float newIntensity = Mathf.Lerp(currentIntensity, targetData.Intensity, Time.deltaTime * _intensityLerpSpeed);

                _meshRenderer.material.color = newColor;
                _meshRenderer.material.SetFloat("_EmissionIntensity", newIntensity);
                _meshRenderer.material.EnableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// Optional: Visualize the client's current target data in the editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(_currentContextData.AmbientColor.r, _currentContextData.AmbientData.g, _currentContextData.AmbientColor.b, 0.7f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
        }
    }
}
```

---

### **How to Use in Unity:**

1.  **Create the Script:**
    *   In your Unity project, create a new C# script named `LightProbeSystemExample.cs`.
    *   Copy and paste the entire code block above into this script.

2.  **Set Up the System Manager:**
    *   Create an empty GameObject in your scene (e.g., `GameObject -> Create Empty`).
    *   Rename it to `ContextProbeSystem`.
    *   Add the `ContextProbeSystem` component to this GameObject (`Add Component -> Context Probe System`).
    *   Configure its settings in the Inspector (e.g., `Max Probes To Consider`, `Interpolate Data`).

3.  **Place Context Probes:**
    *   Create several empty GameObjects.
    *   Rename them descriptively (e.g., `MoodProbe_Red`, `MoodProbe_Blue`, `MoodProbe_Green`).
    *   Add the `ContextProbe` component to each of them.
    *   **Position them** around your scene.
    *   **Configure their `Data`:**
        *   For `MoodProbe_Red`, set `Ambient Color` to Red, `Intensity` to 1.0.
        *   For `MoodProbe_Blue`, set `Ambient Color` to Blue, `Intensity` to 1.0.
        *   For `MoodProbe_Green`, set `Ambient Color` to Green, `Intensity` to 1.0.
        *   You can also create areas with lower intensity or different colors.

4.  **Create a Dynamic Client Object:**
    *   Create a 3D object in your scene (e.g., `GameObject -> 3D Object -> Cube`).
    *   Rename it to `DynamicContextObject`.
    *   Ensure it has a `MeshRenderer` component.
    *   Assign a simple material to it (e.g., a standard `Lit` or `Universal/Lit` material that supports emission for the intensity effect).
    *   Add the `ContextProbeClient` component to this object.
    *   Configure its `Update Interval`, `Color Lerp Speed`, and `Intensity Lerp Speed`.

5.  **Run the Scene:**
    *   Move the `DynamicContextObject` around in the scene (either manually in the editor or with a simple movement script).
    *   Observe how its material color and intensity smoothly change based on its proximity to the `ContextProbe` objects, interpolating between their data.

This setup provides a clear, practical demonstration of the "LightProbeSystem" pattern, allowing dynamic objects to react to environmental data defined by static probes.