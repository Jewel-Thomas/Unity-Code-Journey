// Unity Design Pattern Example: MotionMatching
// This script demonstrates the MotionMatching pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates a simplified version of the **Motion Matching** pattern in Unity. While true Motion Matching involves complex real-time trajectory prediction and comparison against a vast database of pre-analyzed motion fragments, this C# Unity implementation focuses on the core design principles:

1.  **Data-Driven Motion Assets:** Defining animation clips with associated metadata (speed, angular speed) using `ScriptableObject`.
2.  **Motion Database:** Centralizing all available motion assets in a database (`ScriptableObject`).
3.  **Matching Algorithm:** A component (`MotionMatcher`) that continuously evaluates desired movement parameters against the motion database to find the *best-matching* animation.
4.  **Smooth Blending:** Transitioning between selected animations fluidly.

This setup allows you to create highly responsive character controllers by dynamically selecting the most appropriate animation based on current input, rather than relying on complex state machines with hard-coded transitions.

---

### **MotionMatching Example Code**

This solution comprises three C# scripts and detailed usage instructions.

**1. `MotionData.cs`**
*(Represents a single animation clip and its matching parameters)*

```csharp
using UnityEngine;

namespace MotionMatching
{
    /// <summary>
    /// Represents a single animation clip and the data required to match it to desired movement.
    /// This is a ScriptableObject, allowing us to create reusable motion assets in the Project.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotionData", menuName = "MotionMatching/Motion Data")]
    public class MotionData : ScriptableObject
    {
        [Tooltip("The animation clip associated with this motion.")]
        public AnimationClip motionClip;

        [Tooltip("The approximate linear speed (units/second) this animation is designed for.")]
        public float targetSpeed = 0f;

        [Tooltip("The approximate angular speed (degrees/second) this animation is designed for. " +
                 "Positive for turning right, negative for turning left. 0 for straight movement/idle.")]
        public float targetAngularSpeed = 0f;

        [Tooltip("An optional tag to categorize motions (e.g., 'Idle', 'Walk', 'Run', 'Turn').")]
        public string motionTag = "Locomotion";

        // In a full Motion Matching system, this would also include:
        // - Future trajectory points (position, rotation) sampled at fixed intervals.
        // - Root motion deltas (position/rotation changes per frame) for the clip.
        // - Pose data (joint positions/rotations) at various frames.
        // - Any other contextual information needed for robust matching (e.g., foot contacts).

        /// <summary>
        /// Calculates a 'score' representing how well this motion matches the desired parameters.
        /// Lower scores indicate a better match. This is a simplified matching function.
        /// </summary>
        /// <param name="desiredSpeed">The speed we want the character to move at.</param>
        /// <param name="desiredAngularSpeed">The angular speed we want the character to rotate at.</param>
        /// <param name="speedWeight">The importance of speed matching.</param>
        /// <param name="angularSpeedWeight">The importance of angular speed matching.</param>
        /// <returns>A float score, where lower is better.</returns>
        public float CalculateMatchScore(float desiredSpeed, float desiredAngularSpeed, float speedWeight, float angularSpeedWeight)
        {
            // We use a squared difference (error) to amplify larger discrepancies,
            // making motions that are significantly off less likely to be chosen.
            float speedDifference = Mathf.Pow(targetSpeed - desiredSpeed, 2);
            float angularSpeedDifference = Mathf.Pow(targetAngularSpeed - desiredAngularSpeed, 2);

            float score = (speedDifference * speedWeight) + (angularSpeedDifference * angularSpeedWeight);

            // In a real Motion Matching system, the score would also consider:
            // - Pose difference between the character's current pose and the start of the motion clip.
            // - Trajectory difference (future path prediction vs. clip's pre-recorded path).
            // - Continuity with the currently playing animation.
            // - Any constraints or goals (e.g., reaching a target).

            return score;
        }
    }
}
```

**2. `MotionDatabase.cs`**
*(Holds a collection of `MotionData` assets)*

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ extensions if used, e.g., .Any()

namespace MotionMatching
{
    /// <summary>
    /// A database (library) of all available MotionData assets for the MotionMatcher to choose from.
    /// This is a ScriptableObject, allowing us to define and manage our motion library in the Project.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotionDatabase", menuName = "MotionMatching/Motion Database")]
    public class MotionDatabase : ScriptableObject
    {
        [Tooltip("A list of all MotionData assets available for matching.")]
        public List<MotionData> motions = new List<MotionData>();

        /// <summary>
        /// Retrieves all motions in the database.
        /// </summary>
        public IEnumerable<MotionData> GetAllMotions()
        {
            return motions;
        }

        // In a more advanced system, this database might optimize motion retrieval,
        // e.g., using spatial partitioning (like a k-d tree) or hash tables for faster lookups
        // based on motion properties and trajectory data, instead of iterating through all.
    }
}
```

**3. `MotionMatcher.cs`**
*(The core component that manages animation playback)*

```csharp
using UnityEngine;
using System.Linq; // Required for LINQ operations like .Any()

namespace MotionMatching
{
    /// <summary>
    /// The core Motion Matching component. It dynamically selects and plays character animations
    /// by taking desired movement input, querying a MotionDatabase for the best matching animation,
    /// and smoothly blending to it using the Animator component.
    ///
    /// This script exemplifies the Motion Matching *design pattern* by abstracting animation
    /// selection into a data-driven matching process, rather than a rigid state machine.
    /// </summary>
    [RequireComponent(typeof(Animator))] // Motion Matching requires an Animator to play clips.
    public class MotionMatcher : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Animator component on this GameObject. It must have 'Apply Root Motion' checked.")]
        [SerializeField] private Animator characterAnimator;

        [Tooltip("The database containing all available motion clips and their matching data.")]
        [SerializeField] private MotionDatabase motionDatabase;

        [Header("Matching Parameters")]
        [Tooltip("The duration over which to crossfade between animations, creating smooth transitions.")]
        [SerializeField] private float crossfadeDuration = 0.2f;

        [Tooltip("The weight applied to linear speed differences when calculating motion scores. Higher value means speed match is more important.")]
        [SerializeField] private float speedMatchWeight = 1.0f;

        [Tooltip("The weight applied to angular speed differences when calculating motion scores. Higher value means angular speed match is more important.")]
        [SerializeField] private float angularSpeedMatchWeight = 0.5f;

        [Header("Movement Settings")]
        [Tooltip("Maximum linear speed the character can reach based on player input (units/second).")]
        [SerializeField] private float maxSpeed = 5f;

        [Tooltip("Maximum angular speed the character can reach based on player input (degrees/second).")]
        [SerializeField] private float maxAngularSpeed = 180f; // degrees per second

        // Internal state variables for desired movement and current animation.
        private float desiredSpeed;
        private float desiredAngularSpeed;
        private MotionData currentMotion; // The animation clip currently being played.

        /// <summary>
        /// Initializes the Animator reference and performs validation.
        /// Ensures root motion is enabled, as this system relies on animations driving movement.
        /// </summary>
        private void Awake()
        {
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<Animator>();
            }
            if (motionDatabase == null)
            {
                Debug.LogError("MotionDatabase not assigned to MotionMatcher! Please assign one in the Inspector.", this);
                enabled = false; // Disable the script if critical dependency is missing.
                return;
            }

            // Crucial for Motion Matching: animations should drive the character's movement.
            // If animations don't have root motion, the character will play animations in place.
            characterAnimator.applyRootMotion = true;
        }

        /// <summary>
        /// Continuously updates the desired movement and finds/plays the best matching animation.
        /// This is the core loop of the Motion Matching system.
        /// </summary>
        private void Update()
        {
            // 1. Get player input and determine desired movement parameters.
            // For this example, we use simple keyboard input (W/S for vertical, A/D for horizontal).
            float verticalInput = Input.GetAxis("Vertical");   // typically W (+1) / S (-1)
            float horizontalInput = Input.GetAxis("Horizontal"); // typically A (-1) / D (+1)

            // Map input to desired speed and angular speed.
            // desiredSpeed: Forward/Backward movement speed.
            desiredSpeed = verticalInput * maxSpeed;
            // desiredAngularSpeed: Rotational speed (positive for right turn, negative for left turn).
            desiredAngularSpeed = horizontalInput * maxAngularSpeed;

            // 2. Find the best matching motion from our database based on desired parameters.
            MotionData bestMatch = FindBestMatch(desiredSpeed, desiredAngularSpeed);

            // 3. If a new best match is found, or if no motion is currently playing, transition to it.
            if (bestMatch != null && bestMatch != currentMotion)
            {
                PlayMotion(bestMatch);
            }
            // Additional check: If the current motion somehow stopped playing (e.g., a one-shot animation completed)
            // but the system still thinks it's the "currentMotion", re-evaluate and play.
            // This ensures continuous animation playback for locomotion.
            else if (currentMotion != null && !characterAnimator.GetCurrentAnimatorStateInfo(0).IsName(currentMotion.motionClip.name))
            {
                // This condition helps re-trigger the animation if it somehow stopped.
                // For continuously looping locomotion animations, CrossFadeInFixedTime implicitly handles looping.
                // This branch is more for robustness or specific one-shot scenarios.
                PlayMotion(bestMatch); // Play the (still) best match
            }
            // Handle initial state or recovery if currentMotion is unexpectedly null but a match exists.
            else if (currentMotion == null && bestMatch != null)
            {
                PlayMotion(bestMatch);
            }
        }

        /// <summary>
        /// Iterates through the MotionDatabase to find the animation clip that best matches
        /// the desired speed and angular speed based on their calculated scores.
        /// </summary>
        /// <param name="desiredSpeed">The target linear speed (units/second).</param>
        /// <param name="desiredAngularSpeed">The target angular speed (degrees/second).</param>
        /// <returns>The MotionData asset with the lowest (best) match score, or null if the database is empty.</returns>
        private MotionData FindBestMatch(float desiredSpeed, float desiredAngularSpeed)
        {
            if (motionDatabase == null || !motionDatabase.GetAllMotions().Any())
            {
                Debug.LogWarning("MotionDatabase is empty or not assigned to MotionMatcher. Cannot find best match.", this);
                return null;
            }

            MotionData bestMatch = null;
            float lowestScore = float.MaxValue; // Initialize with a very high score.

            // Iterate through all motions in the database to find the best one.
            foreach (MotionData motion in motionDatabase.GetAllMotions())
            {
                if (motion.motionClip == null)
                {
                    Debug.LogWarning($"MotionData '{motion.name}' has no AnimationClip assigned and will be skipped during matching.", motion);
                    continue;
                }

                // Calculate the match score for this motion.
                float score = motion.CalculateMatchScore(desiredSpeed, desiredAngularSpeed, speedMatchWeight, angularSpeedWeight);

                // Add hysteresis: Give a small bonus to the currently playing animation.
                // This prevents rapid, unnecessary switching between two very similar animations,
                // making transitions smoother and less 'flickery'.
                if (motion == currentMotion)
                {
                    score -= 0.1f; // A small score reduction to favor the current motion. Adjust this value as needed.
                }

                // If this motion has a better (lower) score, it becomes the new best match.
                if (score < lowestScore)
                {
                    lowestScore = score;
                    bestMatch = motion;
                }
            }
            return bestMatch;
        }

        /// <summary>
        /// Plays the given MotionData's animation clip on the character's Animator,
        /// using a smooth crossfade to transition from the current animation.
        /// </summary>
        /// <param name="newMotion">The MotionData containing the animation clip to play.</param>
        private void PlayMotion(MotionData newMotion)
        {
            if (newMotion == null || newMotion.motionClip == null)
            {
                Debug.LogError("Attempted to play a null motion or a MotionData with no AnimationClip assigned.", this);
                return;
            }

            // Play the animation clip directly by its name, crossfading from the current state.
            // When using direct clip playback (without an Animator Controller), Unity typically
            // uses the clip's name as the state name for CrossFade.
            characterAnimator.CrossFadeInFixedTime(newMotion.motionClip.name, crossfadeDuration);

            // Update the record of the currently playing motion.
            currentMotion = newMotion;

            // Optional: Uncomment for debug output to see what's being played and why.
            // Debug.Log($"Playing: {newMotion.name} (Target Speed: {newMotion.targetSpeed}, Target Angular: {newMotion.targetAngularSpeed}) " +
            //           $"Desired (Speed: {desiredSpeed:F2}, Angular: {desiredAngularSpeed:F2}) Score: {newMotion.CalculateMatchScore(desiredSpeed, desiredAngularSpeed, speedMatchWeight, angularSpeedWeight):F2}");
        }

        // -------------------------------------------------------------------------------------------------------------------------------------
        // Example Usage Instructions (for Unity Editor Setup)
        // -------------------------------------------------------------------------------------------------------------------------------------
        /*
        How to use this Motion Matching system in Unity:

        1.  **Create a Character GameObject:**
            *   Create an empty GameObject in your scene (e.g., rename it "PlayerCharacter").
            *   Add an `Animator` component to this GameObject (Component -> Animation -> Animator).
            *   Assign an **Avatar** to the Animator (this usually comes from your rigged 3D model, e.g., a humanoid character). If you don't have one, import a character model from the Asset Store or Mixamo.
            *   **Crucially:** In the Animator component, make sure the **"Apply Root Motion"** checkbox is **checked**. This script relies on animations moving the character's transform directly.

        2.  **Prepare Animation Clips:**
            *   You'll need several animation clips (e.g., "Idle", "Walk", "Run", "TurnLeft", "TurnRight").
            *   Ensure these clips are imported into your project. For locomotion, they should ideally contain root motion.
            *   Set their **wrap mode** to "Loop" in the Inspector for continuous actions (like walk/run/idle), or "Default" if they are one-shot actions.

        3.  **Create MotionData Assets:**
            *   In your Project window, navigate to a desired folder (e.g., "Assets/Motions").
            *   Right-click -> Create -> **MotionMatching -> Motion Data**.
            *   Create one `MotionData` asset for **each** animation clip you have (e.g., "IdleMotionData", "WalkMotionData", "RunMotionData", "TurnLeftMotionData", "TurnRightMotionData").
            *   Select each `MotionData` asset in the Project window and fill out its Inspector fields:
                *   **Motion Clip:** Drag your corresponding `AnimationClip` from your Project into this field.
                *   **Target Speed:** Enter the approximate linear speed (units/second) this animation is designed for.
                    *   `IdleMotionData`: 0
                    *   `WalkMotionData`: ~1.5 - 2.5 (e.g., 2.0)
                    *   `RunMotionData`: ~4.0 - 6.0 (e.g., 5.0)
                    *   `TurnLeftMotionData`/`TurnRightMotionData`: 0 (or slight if the character moves while turning in place)
                *   **Target Angular Speed:** Enter the approximate rotational speed (degrees/second) this animation is designed for.
                    *   `IdleMotionData`, `WalkMotionData`, `RunMotionData`: 0 (unless it's a curved motion)
                    *   `TurnLeftMotionData`: A negative value (e.g., -90 or -120, for a turn-in-place left animation)
                    *   `TurnRightMotionData`: A positive value (e.g., 90 or 120, for a turn-in-place right animation)
                *   **Motion Tag (Optional):** Categorize your motions (e.g., "Idle", "Locomotion", "Turn"). This script doesn't use this directly but it's useful for organization or future extensions.

        4.  **Create MotionDatabase Asset:**
            *   In your Project window, right-click -> Create -> **MotionMatching -> Motion Database**.
            *   Select this new `MotionDatabase` asset.
            *   In its Inspector, expand the "Motions" list.
            *   Increase its "Size" to match the number of `MotionData` assets you created.
            *   Drag all your created `MotionData` assets into the empty slots of the "Motions" list.

        5.  **Add MotionMatcher Component to your Character:**
            *   Select your "PlayerCharacter" GameObject in the Hierarchy.
            *   Add the `MotionMatcher` component to it (either drag the script onto the GameObject or use "Add Component" and search for "MotionMatcher").
            *   In the `MotionMatcher` component's Inspector:
                *   Drag your `MotionDatabase` asset into the **"Motion Database"** field.
                *   Adjust **"Crossfade Duration"**, **"Speed Match Weight"**, **"Angular Speed Match Weight"**, **"Max Speed"**, and **"Max Angular Speed"** as needed to fine-tune the character's responsiveness and animation blending. These values will greatly affect how your character feels and reacts.

        6.  **Run the Scene:**
            *   Press the Play button in the Unity Editor.
            *   Use the **W/S keys** for forward/backward movement (controlling desired linear speed).
            *   Use the **A/D keys** for turning (controlling desired angular speed).
            *   Observe how the character dynamically switches between Idle, Walk, Run, and Turn animations based on your input, smoothly blending between them without a traditional Animator State Machine.
        */
    }
}
```