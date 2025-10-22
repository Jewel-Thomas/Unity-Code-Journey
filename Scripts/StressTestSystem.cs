// Unity Design Pattern Example: StressTestSystem
// This script demonstrates the StressTestSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The "StressTestSystem" design pattern, while not one of the canonical Gang of Four patterns, represents a practical and invaluable approach in game development, particularly with Unity. It focuses on creating a flexible, configurable system to simulate various performance bottlenecks or challenging conditions within your game environment. This allows developers to proactively identify performance issues, memory leaks, and stability problems before they impact the player experience.

**Key Principles of the Stress Test System:**

1.  **Centralized Management:** A single manager oversees all stress tests, providing methods to start, stop, and configure them.
2.  **Scenario-Based Testing:** Different types of stress tests are encapsulated as distinct "scenarios" (e.g., heavy object spawning, complex computation, memory allocation, network latency simulation).
3.  **Configurable Parameters:** Each scenario has its own set of parameters (e.g., spawn rate, max objects, computation intensity) that can be easily adjusted, often through the Unity Editor.
4.  **Extensibility:** The system is designed so that new types of stress tests can be added without modifying existing code, following the Open/Closed Principle.
5.  **Isolation:** Stress tests should ideally be able to run independently or in combination, and their impact should be easily measurable.

This example demonstrates a practical Stress Test System in Unity, focusing on a common bottleneck: **object spawning and destruction**.

---

## Complete C# Unity StressTestSystem Example

This script is designed to be a single, drop-in file for your Unity project.

```csharp
// StressTestSystem.cs
// This file contains a complete implementation of the StressTestSystem design pattern for Unity.
// It includes interfaces, base classes, concrete scenarios (object spawning), and a central manager.

using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List<T>
using System; // For Action, Func, Serializable

namespace UnityStressTestSystem
{
    // --- 1. IStressTestScenario Interface ---
    /// <summary>
    /// Defines the contract for any stress test scenario.
    /// This interface allows different types of stress tests (e.g., object spawning, heavy computation,
    /// memory allocation) to be managed uniformly by the StressTestManager.
    /// New stress test types can be created by implementing this interface.
    /// </summary>
    public interface IStressTestScenario
    {
        /// <summary> The unique name of this stress test scenario. </summary>
        string TestName { get; }

        /// <summary> Indicates if the stress test is currently running. </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Initializes the scenario, performing any one-time setup (e.g., pre-allocating lists,
        /// gathering references). This is called once when the StressTestManager initializes.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Starts the execution of the stress test. If the test is already running, it does nothing.
        /// </summary>
        void StartTest();

        /// <summary>
        /// Updates the state of the stress test during its execution.
        /// This method is typically called every frame by the StressTestManager's Update loop.
        /// It contains the core logic for the stress test's operations (e.g., spawning objects).
        /// </summary>
        void UpdateTest();

        /// <summary>
        /// Stops the execution of the stress test. The current state might be preserved until ResetTest is called.
        /// </summary>
        void StopTest();

        /// <summary>
        /// Resets the scenario to its initial state, cleaning up any generated objects or resources.
        /// This method should also implicitly stop the test if it's running.
        /// </summary>
        void ResetTest();
    }

    // --- 2. BaseStressTestConfig (ScriptableObject) ---
    /// <summary>
    /// An abstract base class for all stress test configurations.
    /// Using ScriptableObjects is a Unity best practice for separating configuration data from runtime logic.
    /// This allows developers to define and store various stress test parameters directly in the
    /// Unity Editor, promoting reusability and clean data management across different scenes or projects.
    /// </summary>
    public abstract class BaseStressTestConfig : ScriptableObject
    {
        [Tooltip("A unique name for this stress test configuration. Used for starting/stopping tests.")]
        public string testName = "New Stress Test";

        [Tooltip("If true, this test will automatically start when the StressTestManager initializes.")]
        public bool startOnAwake = false;

        /// <summary>
        /// Factory method: Creates and returns a runtime instance of the stress test scenario
        /// based on this configuration. This method acts as a bridge between the configuration
        /// data (ScriptableObject) and the executable logic (IStressTestScenario implementation).
        /// </summary>
        /// <returns>An instance of IStressTestScenario.</returns>
        public abstract IStressTestScenario CreateScenario();
    }

    // --- 3. Concrete StressTestConfig Example: ObjectSpawningStressTestConfig ---
    /// <summary>
    /// Configuration for a specific stress test that continuously spawns and destroys GameObjects.
    /// This scenario targets a common performance bottleneck in games: object instantiation,
    /// management, and garbage collection from destruction.
    /// </summary>
    [CreateAssetMenu(fileName = "ObjectSpawningStressTest", menuName = "Stress Tests/Object Spawning Test")]
    public class ObjectSpawningStressTestConfig : BaseStressTestConfig
    {
        [Header("Object Spawning Settings")]
        [Tooltip("The GameObject prefab to be spawned repeatedly by this test.")]
        public GameObject prefabToSpawn;

        [Range(1, 2000)]
        [Tooltip("How many objects to attempt to spawn per second. High values can quickly impact performance.")]
        public int spawnRatePerSecond = 100;

        [Range(1, 20000)]
        [Tooltip("The maximum number of active objects allowed at any given time for this test.")]
        public int maxActiveObjects = 1000;

        [Range(0.1f, 120f)]
        [Tooltip("The lifetime (in seconds) of each spawned object before it's destroyed. Affects GC stress.")]
        public float objectLifetime = 5f;

        [Header("Spawn Area & Movement")]
        [Tooltip("The size of the box volume within which objects will be spawned (centered at the spawner's position).")]
        public Vector3 spawnAreaSize = new Vector3(10, 10, 10);

        [Tooltip("If true, spawned objects will move in a random direction after being spawned.")]
        public bool enableMovement = true;

        [Range(0f, 10f)]
        [Tooltip("The speed at which objects move, if movement is enabled. Adds CPU load.")]
        public float movementSpeed = 1f;

        /// <summary>
        /// Overrides the factory method to create an <see cref="ObjectSpawningStressTestScenario"/>
        /// instance, passing this configuration to its constructor.
        /// </summary>
        public override IStressTestScenario CreateScenario()
        {
            return new ObjectSpawningStressTestScenario(this);
        }
    }

    // --- 4. Concrete StressTestScenario Example: ObjectSpawningStressTestScenario ---
    /// <summary>
    /// The runtime logic for the object spawning stress test. This class implements
    /// the IStressTestScenario interface and manages the continuous spawning,
    /// movement, and destruction of objects based on its associated configuration.
    /// </summary>
    public class ObjectSpawningStressTestScenario : IStressTestScenario
    {
        private ObjectSpawningStressTestConfig _config;
        private List<GameObject> _activeObjects; // Tracks all currently active spawned objects
        private float _timeSinceLastSpawn; // Used to control spawn rate
        private bool _isRunning;

        // Public properties to expose scenario state for debugging or UI
        public string TestName => _config.testName;
        public bool IsRunning => _isRunning;
        public int ActiveObjectCount => _activeObjects.Count;

        // A reference to the manager's transform, used as the origin for spawning.
        // This is passed during specific initialization because scenarios don't have a direct GameObject context.
        private Transform _spawnOrigin;

        /// <summary>
        /// Constructor for the ObjectSpawningStressTestScenario.
        /// Takes its configuration as input to initialize its behavior.
        /// </summary>
        /// <param name="config">The configuration ScriptableObject for this scenario.</param>
        public ObjectSpawningStressTestScenario(ObjectSpawningStressTestConfig config)
        {
            _config = config;
            // Pre-allocate capacity for the list to reduce reallocations under stress
            _activeObjects = new List<GameObject>(_config.maxActiveObjects);
            _timeSinceLastSpawn = 0f;
            _isRunning = false;
        }

        /// <summary>
        /// Initializes the scenario, providing the transform of the StressTestManager
        /// as the central point for object spawning. This specific initialization
        /// is called by the StressTestManager after creating the scenario.
        /// </summary>
        /// <param name="spawnOrigin">The transform to use as the origin for spawning objects.</param>
        public void Initialize(Transform spawnOrigin)
        {
            _spawnOrigin = spawnOrigin;
            // Additional initialization for this scenario could go here if needed.
        }

        // The parameterless Initialize required by IStressTestScenario.
        // It relies on the specific Initialize(Transform) being called for context.
        public void Initialize() { /* No-op, relies on Initialize(Transform) */ }


        /// <summary>
        /// Starts the object spawning test.
        /// </summary>
        public void StartTest()
        {
            if (_isRunning) return; // Prevent starting if already running
            if (_config.prefabToSpawn == null)
            {
                Debug.LogError($"[StressTestSystem] '{TestName}': Prefab to spawn is null! Cannot start test.");
                return;
            }

            Debug.Log($"[StressTestSystem] Starting '{TestName}'...");
            _isRunning = true;
            _timeSinceLastSpawn = 0f; // Reset spawn timer
        }

        /// <summary>
        /// Updates the state of the test, handling object destruction, spawning, and movement.
        /// This is called every frame by the StressTestManager.
        /// </summary>
        public void UpdateTest()
        {
            if (!_isRunning) return;

            // 1. Handle object destruction based on lifetime
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeObjects[i];
                if (obj == null) // Object might have been destroyed externally or by a scene unload
                {
                    _activeObjects.RemoveAt(i);
                    continue;
                }

                StressTestObject objComponent = obj.GetComponent<StressTestObject>();
                // If component is missing or object lifetime exceeded, destroy the object
                if (objComponent == null || Time.time - objComponent.spawnTime > _config.objectLifetime)
                {
                    GameObject.Destroy(obj); // Destroys the GameObject
                    _activeObjects.RemoveAt(i); // Removes from our tracking list
                }
            }

            // 2. Handle object spawning to maintain desired rate and max count
            if (_activeObjects.Count < _config.maxActiveObjects)
            {
                _timeSinceLastSpawn += Time.deltaTime;
                float timeBetweenSpawns = 1f / _config.spawnRatePerSecond;

                // Spawn multiple objects if enough time has passed to catch up
                while (_timeSinceLastSpawn >= timeBetweenSpawns && _activeObjects.Count < _config.maxActiveObjects)
                {
                    SpawnObject();
                    _timeSinceLastSpawn -= timeBetweenSpawns; // Decrement by time for one spawn
                }
            }

            // 3. Handle object movement
            if (_config.enableMovement && _config.movementSpeed > 0f)
            {
                foreach (GameObject obj in _activeObjects)
                {
                    if (obj != null)
                    {
                        StressTestObject objComponent = obj.GetComponent<StressTestObject>();
                        if (objComponent != null)
                        {
                            obj.transform.position += objComponent.movementDirection * _config.movementSpeed * Time.deltaTime;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a new object at a random position within the defined spawn area.
        /// Attaches a helper component to track its spawn time and movement direction.
        /// </summary>
        private void SpawnObject()
        {
            if (_config.prefabToSpawn == null)
            {
                Debug.LogError($"[StressTestSystem] '{TestName}': Prefab to spawn is null during SpawnObject call! Stopping test.");
                StopTest();
                return;
            }
            if (_spawnOrigin == null)
            {
                 Debug.LogError($"[StressTestSystem] '{TestName}': Spawn origin is null! Cannot spawn objects. Ensure Initialize(Transform) was called.");
                 StopTest();
                 return;
            }

            // Calculate a random spawn position within the configured area, relative to the manager's position
            Vector3 spawnOffset = new Vector3(
                UnityEngine.Random.Range(-_config.spawnAreaSize.x / 2, _config.spawnAreaSize.x / 2),
                UnityEngine.Random.Range(-_config.spawnAreaSize.y / 2, _config.spawnAreaSize.y / 2),
                UnityEngine.Random.Range(-_config.spawnAreaSize.z / 2, _config.spawnAreaSize.z / 2)
            );
            Vector3 spawnPosition = _spawnOrigin.position + spawnOffset;

            // Instantiate the prefab, parent it to the StressTestManager for cleaner Hierarchy
            GameObject newObj = GameObject.Instantiate(_config.prefabToSpawn, spawnPosition, Quaternion.identity, _spawnOrigin);

            // Add our helper component to track specific data for the stress test logic
            StressTestObject objComponent = newObj.AddComponent<StressTestObject>();
            objComponent.spawnTime = Time.time;
            objComponent.movementDirection = UnityEngine.Random.onUnitSphere; // Random direction for simple movement

            _activeObjects.Add(newObj); // Add to our list of tracked objects
        }

        /// <summary>
        /// Stops the object spawning test. Objects currently in the scene remain until ResetTest is called.
        /// </summary>
        public void StopTest()
        {
            if (!_isRunning) return;
            Debug.Log($"[StressTestSystem] Stopping '{TestName}'. Active objects: {_activeObjects.Count}");
            _isRunning = false;
        }

        /// <summary>
        /// Resets the scenario, stopping it and destroying all currently active spawned objects.
        /// This cleans up the scene and prepares the test for another run.
        /// </summary>
        public void ResetTest()
        {
            StopTest(); // Ensure test is stopped before cleanup
            Debug.Log($"[StressTestSystem] Resetting '{TestName}'. Destroying {_activeObjects.Count} objects.");
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeObjects[i];
                if (obj != null)
                {
                    GameObject.Destroy(obj); // Destroy the GameObject
                }
            }
            _activeObjects.Clear(); // Clear our tracking list
            _timeSinceLastSpawn = 0f; // Reset spawn timer
        }
    }

    // --- Helper Component for Spawned Objects ---
    /// <summary>
    /// A simple MonoBehaviour component attached to each spawned object during a stress test.
    /// It stores metadata like the object's spawn time and its movement direction,
    /// allowing the stress test scenario to manage and update these objects.
    /// </summary>
    public class StressTestObject : MonoBehaviour
    {
        public float spawnTime; // Time.time when this object was spawned
        public Vector3 movementDirection; // Random direction for simple movement
    }

    // --- 5. StressTestManager (MonoBehaviour, Singleton) ---
    /// <summary>
    /// The central manager for all stress tests in the application.
    /// It's implemented as a MonoBehaviour singleton, allowing easy global access
    /// and enabling it to exist in the scene. It orchestrates the initialization,
    /// starting, stopping, and updating of various stress test scenarios
    /// defined via ScriptableObjects.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Ensures this manager initializes before most other scripts
    public class StressTestManager : MonoBehaviour
    {
        // Singleton pattern for easy global access
        public static StressTestManager Instance { get; private set; }

        [Header("Stress Test Configurations")]
        [Tooltip("Drag your 'BaseStressTestConfig' ScriptableObjects here to register them. " +
                 "These define what tests are available and their parameters.")]
        [SerializeField]
        private List<BaseStressTestConfig> _stressTestConfigs = new List<BaseStressTestConfig>();

        // Internal list of all active runtime scenario instances
        private List<IStressTestScenario> _allScenarios = new List<IStressTestScenario>();
        // A dictionary for quick lookup of scenarios by their unique name
        private Dictionary<string, IStressTestScenario> _scenarioMap = new Dictionary<string, IStressTestScenario>();

        [Header("Runtime Info")]
        [Tooltip("Displays the currently running stress tests and their active object counts in the Inspector.")]
        public List<RunningTestInfo> runningTestsInfo = new List<RunningTestInfo>();

        /// <summary>
        /// Helper class for displaying runtime information in the Inspector.
        /// </summary>
        [Serializable]
        public class RunningTestInfo
        {
            public string Name;
            public bool IsRunning;
            public int ActiveObjects; // Specific to object spawning tests
        }

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the singleton and all registered stress test scenarios.
        /// </summary>
        private void Awake()
        {
            // Enforce singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StressTestSystem] Duplicate StressTestManager found, destroying the new one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optionally: Make the manager persist across scene loads.
            // Consider if your stress tests should persist or be scene-specific.
            DontDestroyOnLoad(gameObject);

            InitializeAllScenarios();

            // Automatically start any tests marked with 'startOnAwake' in their config
            foreach (var config in _stressTestConfigs)
            {
                if (config != null && config.startOnAwake)
                {
                    StartTest(config.testName);
                }
            }
        }

        /// <summary>
        /// Called when the GameObject is destroyed.
        /// Cleans up the singleton reference and resets all active tests.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ResetAllTests(); // Ensure all spawned objects are cleaned up
            }
        }

        /// <summary>
        /// Initializes all stress test scenarios from their ScriptableObject configurations.
        /// Creates runtime instances and stores them for management.
        /// </summary>
        private void InitializeAllScenarios()
        {
            _allScenarios.Clear();
            _scenarioMap.Clear();

            foreach (var config in _stressTestConfigs)
            {
                if (config == null)
                {
                    Debug.LogWarning("[StressTestSystem] A null config was found in the StressTestConfigs list. Skipping.");
                    continue;
                }

                // Check for duplicate test names to prevent conflicts
                if (_scenarioMap.ContainsKey(config.testName))
                {
                    Debug.LogError($"[StressTestSystem] Duplicate stress test name '{config.testName}' detected! " +
                                   "Please ensure all configs have unique names. Skipping duplicate.");
                    continue;
                }

                // Create the runtime scenario instance using the factory method
                IStressTestScenario scenario = config.CreateScenario();

                // Perform specific initialization for scenarios that require additional context (like a Transform)
                if (scenario is ObjectSpawningStressTestScenario spawningScenario)
                {
                    spawningScenario.Initialize(this.transform); // Provide manager's transform as spawn origin
                }
                else
                {
                    scenario.Initialize(); // Call general initialization for other types of scenarios
                }

                _allScenarios.Add(scenario);
                _scenarioMap.Add(config.testName, scenario);
                Debug.Log($"[StressTestSystem] Initialized scenario: '{scenario.TestName}'");
            }
        }

        /// <summary>
        /// Called every frame. Updates all currently running stress test scenarios.
        /// </summary>
        private void Update()
        {
            // Iterate through all scenarios and update only those that are running
            foreach (var scenario in _allScenarios)
            {
                if (scenario.IsRunning)
                {
                    scenario.UpdateTest();
                }
            }

            // Update the Inspector display info
            UpdateRunningTestsInfo();
        }

        /// <summary>
        /// Populates the 'runningTestsInfo' list for display in the Unity Inspector,
        /// providing live feedback on active tests.
        /// </summary>
        private void UpdateRunningTestsInfo()
        {
            runningTestsInfo.Clear();
            foreach (var scenario in _allScenarios)
            {
                // Retrieve specific info for object spawning tests, generic info for others
                int activeObjects = (scenario is ObjectSpawningStressTestScenario objSpawner) ? objSpawner.ActiveObjectCount : 0;
                runningTestsInfo.Add(new RunningTestInfo
                {
                    Name = scenario.TestName,
                    IsRunning = scenario.IsRunning,
                    ActiveObjects = activeObjects
                });
            }
        }

        // --- Public API for Controlling Stress Tests ---

        /// <summary>
        /// Starts all registered stress tests. Can be triggered from the Inspector Context Menu.
        /// </summary>
        [ContextMenu("Start All Stress Tests")]
        public void StartAllTests()
        {
            Debug.Log("[StressTestSystem] Starting all registered stress tests.");
            foreach (var scenario in _allScenarios)
            {
                scenario.StartTest();
            }
        }

        /// <summary>
        /// Stops all currently running stress tests. Can be triggered from the Inspector Context Menu.
        /// </summary>
        [ContextMenu("Stop All Stress Tests")]
        public void StopAllTests()
        {
            Debug.Log("[StressTestSystem] Stopping all registered stress tests.");
            foreach (var scenario in _allScenarios)
            {
                scenario.StopTest();
            }
        }

        /// <summary>
        /// Resets (stops and cleans up) all registered stress tests, destroying any generated objects.
        /// Can be triggered from the Inspector Context Menu.
        /// </summary>
        [ContextMenu("Reset All Stress Tests")]
        public void ResetAllTests()
        {
            Debug.Log("[StressTestSystem] Resetting all registered stress tests.");
            foreach (var scenario in _allScenarios)
            {
                scenario.ResetTest();
            }
        }

        /// <summary>
        /// Starts a specific stress test by its unique name.
        /// </summary>
        /// <param name="testName">The unique name of the stress test to start.</param>
        public void StartTest(string testName)
        {
            if (_scenarioMap.TryGetValue(testName, out var scenario))
            {
                scenario.StartTest();
            }
            else
            {
                Debug.LogWarning($"[StressTestSystem] Test '{testName}' not found. Cannot start.");
            }
        }

        /// <summary>
        /// Stops a specific stress test by its unique name.
        /// </summary>
        /// <param name="testName">The unique name of the stress test to stop.</param>
        public void StopTest(string testName)
        {
            if (_scenarioMap.TryGetValue(testName, out var scenario))
            {
                scenario.StopTest();
            }
            else
            {
                Debug.LogWarning($"[StressTestSystem] Test '{testName}' not found. Cannot stop.");
            }
        }

        /// <summary>
        /// Resets (stops and cleans up) a specific stress test by its unique name.
        /// </summary>
        /// <param name="testName">The unique name of the stress test to reset.</param>
        public void ResetTest(string testName)
        {
            if (_scenarioMap.TryGetValue(testName, out var scenario))
            {
                scenario.ResetTest();
            }
            else
            {
                Debug.LogWarning($"[StressTestSystem] Test '{testName}' not found. Cannot reset.");
            }
        }

        // --- Editor Gizmos for Visualization ---
        /// <summary>
        /// Draws visual aids in the scene view for active stress tests.
        /// This helps visualize spawn areas and object locations during testing.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Only draw gizmos in editor play mode and if manager instance is valid
            if (!Application.isPlaying || Instance == null) return;

            foreach (var config in _stressTestConfigs)
            {
                if (config == null || !_scenarioMap.TryGetValue(config.testName, out var scenario))
                {
                    continue;
                }

                // Example: Draw gizmos for ObjectSpawningStressTestScenario
                if (scenario is ObjectSpawningStressTestScenario objSpawner && config is ObjectSpawningStressTestConfig objConfig)
                {
                    // Draw spawn area for current or scheduled object spawning tests
                    bool isRunningOrWillStart = objSpawner.IsRunning || objConfig.startOnAwake;
                    if (isRunningOrWillStart)
                    {
                        // Semi-transparent cube for the spawn area
                        Gizmos.color = objSpawner.IsRunning ? new Color(1, 0.5f, 0, 0.3f) : new Color(0, 1, 1, 0.1f);
                        Gizmos.DrawCube(transform.position, objConfig.spawnAreaSize);

                        // Wireframe for the spawn area
                        Gizmos.color = objSpawner.IsRunning ? Color.red : Color.cyan;
                        Gizmos.DrawWireCube(transform.position, objConfig.spawnAreaSize);

                        // Draw lines to a few spawned objects to visualize their presence/movement
                        if (objSpawner.IsRunning)
                        {
                            Gizmos.color = Color.yellow;
                            for (int i = 0; i < Mathf.Min(objSpawner.ActiveObjectCount, 50); i++) // Limit lines for performance
                            {
                                GameObject obj = objSpawner._activeObjects[i];
                                if (obj != null)
                                {
                                    Gizmos.DrawLine(transform.position, obj.transform.position);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

/*
--- EXAMPLE USAGE IN UNITY PROJECT ---

To use this StressTestSystem in your Unity project, follow these steps:

1.  **Create the Script File:**
    *   In your Unity Project window, navigate to an appropriate folder (e.g., `Assets/Scripts/StressTest`).
    *   Right-click -> `Create` -> `C# Script`.
    *   Name it `StressTestSystem.cs`.
    *   Copy and paste the entire code block above into this new script, overwriting its default content.

2.  **Create a Simple Prefab for Stress Testing (e.g., "StressCube"):**
    *   In your Hierarchy, `Right-click` -> `3D Object` -> `Cube`.
    *   Rename it to `StressCube`.
    *   Adjust its scale (e.g., to `0.2, 0.2, 0.2`) to prevent it from being too large.
    *   (Optional) Create a simple material (e.g., a bright, distinct color) and assign it to `StressCube` for easy visibility.
    *   Drag `StressCube` from the Hierarchy into a folder in your Project window (e.g., `Assets/Prefabs`) to create a prefab.
    *   Delete the `StressCube` from your Hierarchy (the prefab is what we need).

3.  **Create Stress Test Configurations (ScriptableObjects):**
    *   In your Project window, `Right-click` -> `Create` -> `Stress Tests` -> `Object Spawning Test`.
    *   Name this new ScriptableObject asset, for example, `HeavyObjectSpawnerConfig`.
    *   Select `HeavyObjectSpawnerConfig` in the Project window to view its settings in the Inspector:
        *   `Test Name`: Enter a unique name, e.g., `HeavySpawner`. This name will be used to control the test.
        *   `Start On Awake`: Set to `false` initially, so we can start it manually.
        *   `Prefab To Spawn`: Drag your `StressCube` prefab from your Project window into this slot.
        *   `Spawn Rate Per Second`: Experiment with values like `200` to `500`.
        *   `Max Active Objects`: Try `2000` to `5000`.
        *   `Object Lifetime`: Set to `5` to `10` seconds.
        *   `Spawn Area Size`: A `Vector3` like `20, 20, 20` works well.
        *   `Enable Movement`: Check this box.
        *   `Movement Speed`: Set to `2` to `5`.

    *   (Optional) Create another `Object Spawning Test` ScriptableObject, e.g., `LightObjectSpawnerConfig`, with lower settings (e.g., `Test Name: LightSpawner`, `Spawn Rate: 50`, `Max Active: 500`). This demonstrates having multiple distinct tests.

4.  **Set up the StressTestManager in your Scene:**
    *   In your Hierarchy, `Right-click` -> `Create Empty`.
    *   Rename this new GameObject to `StressTestManager`.
    *   With `StressTestManager` selected, click `Add Component` in the Inspector and search for `StressTestManager`. Add the component.
    *   In the `StressTestManager` component's Inspector:
        *   Expand the `Stress Test Configurations` list.
        *   Set the `Size` property to the number of `ObjectSpawningStressTestConfig` assets you created (e.g., `2` for `HeavySpawnerConfig` and `LightSpawnerConfig`).
        *   Drag your `HeavyObjectSpawnerConfig` and `LightObjectSpawnerConfig` ScriptableObjects from your Project window into the respective `Element` slots.

5.  **Run and Test in the Editor:**
    *   **Play the scene.**
    *   Select the `StressTestManager` GameObject in the Hierarchy.
    *   In its Inspector, observe the `Running Tests Info` list. Initially, it should be empty.
    *   **Control via Context Menu:**
        *   `Right-click` on the `StressTestManager` component header in the Inspector.
        *   Select `Start All Stress Tests` to begin spawning objects.
        *   You should see your `StressCube` prefabs appearing around the `StressTestManager`'s position.
        *   Watch the `Running Tests Info` list update with the number of `Active Objects`.
        *   Open the Unity `Profiler` window (`Window` -> `Analysis` -> `Profiler`) to observe the CPU and GPU impact. Look for spikes in `GC.Alloc` (garbage collection) and CPU time in `Physics` or `Script` updates.
        *   Use `Stop All Stress Tests` or `Reset All Stress Tests` from the context menu to halt and clean up the tests. `Reset` will destroy all spawned objects.

    *   **Control via Code (Advanced):**
        You can programmatically control the stress tests from any other script in your scene.

        ```csharp
        using UnityEngine;
        using UnityStressTestSystem; // Important: Include the namespace!

        /// <summary>
        /// Example script to demonstrate controlling stress tests via input keys.
        /// Attach this to any GameObject in your scene.
        /// </summary>
        public class StressTestInputController : MonoBehaviour
        {
            [Tooltip("The name of the specific test to control with keys 1, 2, 3.")]
            public string specificTestName = "HeavySpawner"; // Match this to your config's Test Name

            void Update()
            {
                // Ensure the manager exists before trying to access it
                if (StressTestManager.Instance == null)
                {
                    Debug.LogWarning("StressTestManager.Instance is null. Is the manager GameObject in the scene?");
                    return;
                }

                // Start specific test (e.g., "HeavySpawner")
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Debug.Log($"Attempting to START test: {specificTestName}");
                    StressTestManager.Instance.StartTest(specificTestName);
                }
                // Stop specific test
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log($"Attempting to STOP test: {specificTestName}");
                    StressTestManager.Instance.StopTest(specificTestName);
                }
                // Reset specific test (stops and cleans up objects)
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log($"Attempting to RESET test: {specificTestName}");
                    StressTestManager.Instance.ResetTest(specificTestName);
                }

                // Start ALL registered tests
                if (Input.GetKeyDown(KeyCode.S)) // 'S' for Start All
                {
                    Debug.Log("Attempting to START ALL tests.");
                    StressTestManager.Instance.StartAllTests();
                }
                // Stop ALL registered tests
                if (Input.GetKeyDown(KeyCode.X)) // 'X' for Stop All
                {
                    Debug.Log("Attempting to STOP ALL tests.");
                    StressTestManager.Instance.StopAllTests();
                }
                // Reset ALL registered tests
                if (Input.GetKeyDown(KeyCode.R)) // 'R' for Reset All
                {
                    Debug.Log("Attempting to RESET ALL tests.");
                    StressTestManager.Instance.ResetAllTests();
                }
            }
        }
        ```
        *   Create an empty GameObject (e.g., `TestController`).
        *   Add the `StressTestInputController` component to it.
        *   Set the `Specific Test Name` field to "HeavySpawner" (or "LightSpawner").
        *   Run the scene and use keys `1`, `2`, `3`, `S`, `X`, `R` to control the tests.

This `StressTestSystem` provides a robust and extensible framework for systematically evaluating your Unity project's performance and stability under various demanding conditions. It's a crucial tool for optimizing your game and ensuring a smooth player experience.
*/
```