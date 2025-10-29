// Unity Design Pattern Example: WeaponJamSystem
// This script demonstrates the WeaponJamSystem pattern in Unity
// Generated automatically - ready to use in your Unity project

The 'WeaponJamSystem' is a practical design pattern in game development, particularly for shooters, where weapons can temporarily become inoperable due to various conditions. This system aims to decouple the jamming logic from the weapon's core firing mechanism, making weapons more robust and easier to manage.

**Pattern Overview:**

At its core, the Weapon Jam System involves:

1.  **Jam Controller (`WeaponJamController`):** A dedicated component responsible for managing the jamming state of a weapon. It holds parameters like jam chance, unjamming duration, and provides methods to `TryJam()` and `AttemptUnjam()`. Crucially, it uses **events** to notify other parts of the system when the weapon jams or unjams.
2.  **Weapon Component (`DemoWeapon`):** This is the actual weapon script. Instead of handling jamming logic itself, it interacts with the `WeaponJamController`. It calls `TryJam()` after a firing attempt and subscribes to the `OnWeaponJammed` and `OnWeaponUnjammed` events to adjust its own behavior (e.g., prevent firing, change visuals).

**Benefits of this approach:**

*   **Decoupling:** The jam logic is separate from the weapon's firing logic. This means you can reuse the `WeaponJamController` on different types of weapons without modifying their core firing scripts.
*   **Modularity:** You can easily add new types of jam conditions or unjamming mechanics by extending the `WeaponJamController` or creating new specialized controllers.
*   **Flexibility:** Weapons can react differently to being jammed (e.g., a pistol might simply stop firing, a heavy machine gun might have a visible jam animation).
*   **Scalability:** Managing multiple weapon types with diverse jamming behaviors becomes much simpler.

---

Here's a complete C# Unity script demonstrating the WeaponJamSystem pattern:

```csharp
using UnityEngine;
using System.Collections; // Required for IEnumerator
using UnityEngine.Events;   // Required for UnityEvent

namespace WeaponSystem
{
    /// <summary>
    /// This script demonstrates the 'WeaponJamSystem' design pattern in Unity.
    /// It consists of two main parts:
    /// 1. WeaponJamController: Manages the jamming state and logic for a weapon.
    /// 2. DemoWeapon: An example weapon that uses the WeaponJamController.
    ///
    /// The core idea is to decouple the "jamming" mechanic from the "weapon firing" mechanic.
    /// The WeaponJamController is responsible for determining if a jam occurs and managing
    /// the unjamming process. It then notifies the weapon (or any other interested components)
    /// via UnityEvents when the jam state changes.
    /// </summary>
    public class WeaponJamSystemExample : MonoBehaviour
    {
        // --- WEAPON JAM CONTROLLER ---
        // This class represents the core of the WeaponJamSystem pattern.
        // It manages the jamming state and logic independently of any specific weapon type.
        [System.Serializable] // Makes this class visible in the Inspector for `DemoWeapon` to reference
        public class WeaponJamController
        {
            [Header("Jamming Configuration")]
            [Tooltip("The probability (0-1) that the weapon will jam after a fire attempt.")]
            [Range(0f, 1f)]
            [SerializeField] private float jamChance = 0.1f; // 10% chance to jam

            [Tooltip("The time (in seconds) it takes to unjam the weapon.")]
            [SerializeField] private float unjamTime = 2.0f;

            // Internal state variables
            private bool _isJammed = false;
            private bool _isUnjamming = false;
            private MonoBehaviour _coroutineHost; // Used to start coroutines from this non-MonoBehaviour class

            // Public properties to check the weapon's current jam state
            public bool IsJammed => _isJammed;
            public bool IsUnjamming => _isUnjamming;

            [Header("Jamming Events")]
            // UnityEvents allow other components (like the DemoWeapon) to subscribe
            // and react to changes in the jamming state.
            public UnityEvent OnWeaponJammed = new UnityEvent();
            public UnityEvent OnWeaponUnjammed = new UnityEvent();
            public UnityEvent OnUnjammingStarted = new UnityEvent();
            // This event provides progress for UI feedback or animations
            public UnityEvent<float> OnUnjammingProgress = new UnityEvent<float>();


            /// <summary>
            /// Initializes the controller. This method should be called once,
            /// typically by the MonoBehaviour that owns this controller (e.g., the DemoWeapon).
            /// </summary>
            /// <param name="host">A MonoBehaviour instance to run coroutines from.</param>
            public void Initialize(MonoBehaviour host)
            {
                _coroutineHost = host;
                if (OnWeaponJammed == null) OnWeaponJammed = new UnityEvent();
                if (OnWeaponUnjammed == null) OnWeaponUnjammed = new UnityEvent();
                if (OnUnjammingStarted == null) OnUnjammingStarted = new UnityEvent();
                if (OnUnjammingProgress == null) OnUnjammingProgress = new UnityEvent<float>();

                Debug.Log($"<color=cyan>[JamController]</color> Initialized with Jam Chance: {jamChance * 100}% and Unjam Time: {unjamTime}s.");
            }

            /// <summary>
            /// Attempts to jam the weapon based on the configured jam chance.
            /// This method is typically called by the weapon's firing logic after each shot.
            /// </summary>
            /// <returns>True if the weapon jammed, false otherwise.</returns>
            public bool TryJam()
            {
                if (_isJammed)
                {
                    Debug.Log("<color=yellow>[JamController]</color> Weapon is already jammed.");
                    return true;
                }

                // Randomly determine if the weapon jams
                if (Random.value < jamChance)
                {
                    _isJammed = true;
                    OnWeaponJammed.Invoke(); // Notify subscribers that the weapon is jammed
                    Debug.Log("<color=red>[JamController]</color> WEAPON JAMMED!");
                    return true;
                }

                Debug.Log("<color=green>[JamController]</color> Weapon did not jam.");
                return false;
            }

            /// <summary>
            /// Attempts to unjam the weapon. This method is typically called by player input.
            /// </summary>
            public void AttemptUnjam()
            {
                if (!_isJammed)
                {
                    Debug.Log("<color=green>[JamController]</color> Weapon is not jammed.");
                    return;
                }
                if (_isUnjamming)
                {
                    Debug.Log("<color=yellow>[JamController]</color> Already unjamming...");
                    return;
                }

                // Start the unjamming process if currently jammed and not already unjamming
                _coroutineHost.StartCoroutine(UnjamCoroutine());
            }

            /// <summary>
            /// Coroutine to handle the timed unjamming process.
            /// </summary>
            private IEnumerator UnjamCoroutine()
            {
                _isUnjamming = true;
                OnUnjammingStarted.Invoke(); // Notify subscribers that unjamming has begun
                Debug.Log($"<color=orange>[JamController]</color> Starting unjam process... (Takes {unjamTime}s)");

                float timer = 0f;
                while (timer < unjamTime)
                {
                    timer += Time.deltaTime;
                    float progress = Mathf.Clamp01(timer / unjamTime);
                    OnUnjammingProgress.Invoke(progress); // Provide progress update
                    // Debug.Log($"<color=orange>[JamController]</color> Unjamming Progress: {progress:P0}"); // Optional verbose logging
                    yield return null; // Wait for the next frame
                }

                _isJammed = false;
                _isUnjamming = false;
                OnWeaponUnjammed.Invoke(); // Notify subscribers that the weapon is unjammed
                Debug.Log("<color=green>[JamController]</color> WEAPON UNJAMMED!");
            }

            /// <summary>
            /// Resets the jam controller to its initial state.
            /// </summary>
            public void ResetController()
            {
                _isJammed = false;
                _isUnjamming = false;
                // Note: Don't invoke events here if it's just for internal state reset,
                // only when actual state change happens from operation.
            }
        }

        // --- DEMO WEAPON (Client of the WeaponJamSystem) ---
        // This MonoBehaviour represents a typical weapon script that utilizes the
        // WeaponJamController to manage its jamming behavior.
        [Header("Demo Weapon Configuration")]
        [Tooltip("Reference to the WeaponJamController instance for this weapon.")]
        [SerializeField] private WeaponJamController jamController = new WeaponJamController();

        [Tooltip("The rate of fire in shots per second.")]
        [SerializeField] private float fireRate = 5.0f;

        private float _nextFireTime = 0f;
        private bool _canFire = true; // Tracks if the weapon is currently able to fire

        // UI related (for basic demonstration feedback)
        [Tooltip("Optional: A TextMeshProUGUI or UI.Text component to display weapon status.")]
        [SerializeField] private TMPro.TextMeshProUGUI weaponStatusText;

        void Awake()
        {
            // Initialize the jam controller, passing 'this' (the DemoWeapon MonoBehaviour)
            // as the host for coroutines.
            jamController.Initialize(this);
        }

        void OnEnable()
        {
            // Subscribe to the jam controller's events.
            // This is how the DemoWeapon reacts to jamming events without knowing
            // the internal logic of the jam controller.
            jamController.OnWeaponJammed.AddListener(HandleWeaponJammed);
            jamController.OnWeaponUnjammed.AddListener(HandleWeaponUnjammed);
            jamController.OnUnjammingStarted.AddListener(HandleUnjammingStarted);
            jamController.OnUnjammingProgress.AddListener(HandleUnjammingProgress);

            // Ensure initial state is not jammed
            _canFire = !jamController.IsJammed;
            UpdateWeaponStatusUI();
        }

        void OnDisable()
        {
            // Always unsubscribe from events to prevent memory leaks or errors
            // when the GameObject is disabled or destroyed.
            jamController.OnWeaponJammed.RemoveListener(HandleWeaponJammed);
            jamController.OnWeaponUnjammed.RemoveListener(HandleWeaponUnjammed);
            jamController.OnUnjammingStarted.RemoveListener(HandleUnjammingStarted);
            jamController.OnUnjammingProgress.RemoveListener(HandleUnjammingProgress);
        }

        void Update()
        {
            // Simulate firing input
            if (Input.GetMouseButtonDown(0)) // Left mouse button click
            {
                TryFireWeapon();
            }

            // Simulate unjamming input
            if (Input.GetKeyDown(KeyCode.R)) // 'R' key for reload/unjam
            {
                Debug.Log("<color=magenta>[DemoWeapon]</color> Player attempted to unjam.");
                jamController.AttemptUnjam();
                UpdateWeaponStatusUI();
            }
        }

        /// <summary>
        /// Attempts to fire the weapon. Checks fire rate and jam status.
        /// </summary>
        private void TryFireWeapon()
        {
            // Basic fire rate check
            if (Time.time < _nextFireTime)
            {
                Debug.Log("<color=white>[DemoWeapon]</color> Firing too fast!");
                UpdateWeaponStatusUI("Firing too fast!");
                return;
            }

            if (!_canFire)
            {
                Debug.Log("<color=red>[DemoWeapon]</color> Cannot fire: " + (jamController.IsJammed ? "Jammed!" : "Unjamming..."));
                UpdateWeaponStatusUI("Cannot fire: " + (jamController.IsJammed ? "Jammed!" : "Unjamming..."));
                return;
            }

            // If not jammed and ready to fire, proceed with firing logic
            PerformFire();
            _nextFireTime = Time.time + 1f / fireRate; // Update next fire time
        }

        /// <summary>
        /// Contains the actual firing logic of the weapon (e.g., shooting a raycast, instantiating a projectile).
        /// </summary>
        private void PerformFire()
        {
            Debug.Log("<color=blue>[DemoWeapon]</color> BANG! Weapon fired.");
            UpdateWeaponStatusUI("Fired!");

            // After firing, there's a chance the weapon might jam.
            // We delegate this check entirely to the WeaponJamController.
            jamController.TryJam();
            
            // If it jammed, the HandleWeaponJammed method will be called via the event,
            // which will set _canFire to false.
        }

        /// <summary>
        /// Callback method invoked when the jam controller notifies that the weapon has jammed.
        /// </summary>
        private void HandleWeaponJammed()
        {
            _canFire = false; // Prevent further firing
            Debug.Log("<color=red>[DemoWeapon]</color> Received notification: Weapon is now J A M M E D!");
            UpdateWeaponStatusUI("JAMMED!");
        }

        /// <summary>
        /// Callback method invoked when the jam controller notifies that the weapon has unjammed.
        /// </summary>
        private void HandleWeaponUnjammed()
        {
            _canFire = true; // Allow firing again
            Debug.Log("<color=green>[DemoWeapon]</color> Received notification: Weapon is UNJAMMED!");
            UpdateWeaponStatusUI("Ready to Fire!");
        }

        /// <summary>
        /// Callback method invoked when the jam controller notifies that unjamming has started.
        /// </summary>
        private void HandleUnjammingStarted()
        {
            _canFire = false; // Ensure no firing during unjamming
            Debug.Log("<color=orange>[DemoWeapon]</color> Received notification: Unjamming started...");
            UpdateWeaponStatusUI("Unjamming...");
        }

        /// <summary>
        /// Callback method invoked when the jam controller notifies about unjamming progress.
        /// </summary>
        /// <param name="progress">The unjamming progress from 0.0 to 1.0.</param>
        private void HandleUnjammingProgress(float progress)
        {
            // This can be used to update a UI progress bar, play an animation, etc.
            // Debug.Log($"<color=orange>[DemoWeapon]</color> Unjamming Progress: {progress:P0}"); // Optional verbose logging
            UpdateWeaponStatusUI($"Unjamming... ({progress * 100:F0}%)");
        }


        /// <summary>
        /// Updates an optional UI text element with the current weapon status.
        /// Requires TextMeshPro if used, otherwise comment out.
        /// </summary>
        /// <param name="message">An optional custom message to display.</param>
        private void UpdateWeaponStatusUI(string message = null)
        {
            if (weaponStatusText == null) return;

            string status = "";
            if (jamController.IsJammed)
            {
                status = "STATUS: JAMMED!";
                weaponStatusText.color = Color.red;
            }
            else if (jamController.IsUnjamming)
            {
                status = "STATUS: UNJAMMING...";
                weaponStatusText.color = Color.yellow;
            }
            else
            {
                status = "STATUS: Ready";
                weaponStatusText.color = Color.green;
            }

            if (!string.IsNullOrEmpty(message))
            {
                status += $" ({message})";
            }

            weaponStatusText.text = status;
        }
    }
}

/*
--- EXAMPLE USAGE IN UNITY PROJECT ---

1.  **Create a C# Script:**
    *   In your Unity project, right-click in the Project window -> Create -> C# Script.
    *   Name it `WeaponJamSystemExample`.
    *   Copy and paste the entire code above into this new script, overwriting its contents.
    *   Save the script.

2.  **Create a GameObject for the Weapon:**
    *   In your Unity scene, right-click in the Hierarchy window -> Create Empty.
    *   Name this GameObject `PlayerWeapon` (or anything descriptive).

3.  **Attach the Script:**
    *   Select the `PlayerWeapon` GameObject in the Hierarchy.
    *   Drag the `WeaponJamSystemExample` script from your Project window onto the `PlayerWeapon` in the Inspector.

4.  **Configure in Inspector:**
    *   With `PlayerWeapon` selected, look at the `Weapon Jam System Example` component in the Inspector.
    *   **Weapon Jam Controller:**
        *   `Jam Chance`: Adjust this value (e.g., 0.1 for 10% chance) to control how often the weapon jams.
        *   `Unjam Time`: Set how long it takes to clear a jam (e.g., 2 seconds).
    *   **Demo Weapon Configuration:**
        *   `Fire Rate`: Set how many shots per second (e.g., 5).
        *   `Weapon Status Text`: (Optional but recommended for visual feedback)
            *   Create a UI Text element: Right-click in Hierarchy -> UI -> Text - TextMeshPro. (Requires importing TMP Essentials if you haven't).
            *   Adjust its position and size to be visible on screen.
            *   Drag this new UI Text GameObject from the Hierarchy onto the `Weapon Status Text` field in the `PlayerWeapon`'s Inspector.

5.  **Run the Scene:**
    *   Press the Play button in the Unity Editor.
    *   **To Fire:** Click the Left Mouse Button. Watch the Console for debug messages and the UI Text for status updates.
    *   **To Unjam:** If the weapon jams (you'll see "WEAPON JAMMED!" in the console and UI), press the 'R' key. You'll see the unjamming progress and eventually "WEAPON UNJAMMED!".

This setup provides a complete and interactive demonstration of the WeaponJamSystem pattern, highlighting the separation of concerns and event-driven communication.
*/
```