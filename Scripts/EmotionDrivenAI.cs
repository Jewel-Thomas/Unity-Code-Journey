// Unity Design Pattern Example: EmotionDrivenAI
// This script demonstrates the EmotionDrivenAI pattern in Unity
// Generated automatically - ready to use in your Unity project

This example demonstrates the **Emotion-Driven AI** design pattern in Unity. This pattern involves an AI agent whose behavior is primarily influenced by an internal 'emotional state'. This state is dynamic, changing based on events, decaying over time, and directly mapping to observable actions and characteristics of the AI.

**Key Components of Emotion-Driven AI:**

1.  **Emotion Types:** A defined set of possible emotions (e.g., Happy, Angry, Scared).
2.  **Emotional State:** The AI's current dominant emotion and its intensity.
3.  **Emotion Triggers:** External or internal events that cause a change in the emotional state.
4.  **Emotion Dynamics:** Rules for how emotions change (e.g., decay over time, how new emotions override or blend with existing ones).
5.  **Behavior Mapping:** A clear link between the emotional state and specific AI behaviors (e.g., movement speed, visual appearance, attack patterns, dialogue).

---

## C# Unity Script: `EmotionDrivenAIController.cs`

This script is designed to be attached to any GameObject representing an AI agent. It provides a flexible system for defining emotions, how they affect the AI, and how external events can trigger emotional responses.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For using .FirstOrDefault()

/// <summary>
/// Defines the possible types of emotions our AI can experience.
/// Extend this enum with more emotions as needed for your game.
/// </summary>
public enum EmotionType { Neutral, Happy, Angry, Scared, Sad, Confused, Excited }

/// <summary>
/// Represents an active emotion, combining its type and its current intensity.
/// Intensity typically ranges from 0 (no emotion) to 1 (full intensity).
/// </summary>
[System.Serializable] // Allows this struct to be seen/edited in the Unity Inspector
public struct Emotion
{
    public EmotionType type;
    [Range(0f, 1f)] // Restricts intensity input in Inspector for easy balancing
    public float intensity;

    public Emotion(EmotionType type, float intensity)
    {
        this.type = type;
        this.intensity = intensity;
    }

    public static Emotion Neutral => new Emotion(EmotionType.Neutral, 0f);
}

/// <summary>
/// Defines how the AI's behavior changes when it's experiencing a specific emotion.
/// This allows for highly customizable and reusable emotional responses.
/// </summary>
[System.Serializable]
public class EmotionBehaviorProfile
{
    public EmotionType emotionType;

    [Header("Behavior Modifiers")]
    [Tooltip("Multiplier for the AI's base movement speed.")]
    public float movementSpeedMultiplier = 1f;

    [Tooltip("Color tint applied to the AI's renderer to visually indicate mood.")]
    public Color moodColor = Color.white;

    [Tooltip("A short text prefix that might be added to dialogue to reflect mood.")]
    [TextArea] public string dialoguePrefix = "";

    [Tooltip("Modifier for attack speed. Positive values mean faster attacks, negative means slower.")]
    public float attackSpeedModifier = 0f; // 0 = normal, +ve = faster, -ve = slower

    [Tooltip("An optional audio clip to play when this emotion is dominant.")]
    public AudioClip voiceClip;

    [Tooltip("An optional particle system to activate/deactivate for this emotion.")]
    public GameObject moodParticleEffectPrefab; // Could instantiate/activate a child object

    // Add more behavior parameters here as your AI needs them (e.g., defense, accuracy, item usage probability)
}

/// <summary>
/// The core controller for Emotion-Driven AI. Manages the AI's emotional state,
/// handles triggers, decays emotions over time, and maps emotions to concrete behaviors.
/// </summary>
[RequireComponent(typeof(AudioSource))] // Ensure an AudioSource is present for mood sounds
public class EmotionDrivenAIController : MonoBehaviour
{
    // --- Public Read-Only Properties ---
    // These allow other scripts to query the AI's current emotional state without modifying it directly.
    public EmotionType CurrentEmotionType => _currentEmotion.type;
    public float CurrentEmotionIntensity => _currentEmotion.intensity;

    // --- Configuration Parameters (Editable in Inspector) ---
    [Header("Emotion Dynamics Settings")]
    [Tooltip("How much emotion intensity decays per second when no new triggers occur.")]
    [SerializeField] private float _emotionDecayRate = 0.1f; // e.g., 0.1 means 10% intensity loss per second

    [Tooltip("Maximum intensity an emotion can reach.")]
    [SerializeField] private float _maxEmotionIntensity = 1f;

    [Tooltip("Minimum intensity an emotion needs to be above to be considered 'active'. Below this, it reverts to Neutral.")]
    [SerializeField] private float _minActiveEmotionIntensity = 0.05f;

    [Tooltip("Time in seconds after a strong emotion trigger before the emotion starts stabilizing (decaying more rapidly if still high).")]
    [SerializeField] private float _emotionStabilizationDelay = 2f;
    [Tooltip("How quickly an emotion stabilizes after its delay period, blending towards a lower intensity.")]
    [SerializeField] private float _emotionStabilizationRate = 0.5f;

    [Header("Behavior Profiles")]
    [Tooltip("A list of behavior definitions for each emotion type. Configure these in the Inspector.")]
    [SerializeField] private List<EmotionBehaviorProfile> _behaviorProfiles;

    [Header("AI Component References")]
    [Tooltip("The Renderer component whose material color will be tinted by mood.")]
    [SerializeField] private Renderer _moodRenderer; // e.g., MeshRenderer, SpriteRenderer

    [Tooltip("The base movement speed of the AI in its neutral state.")]
    [SerializeField] private float _baseMovementSpeed = 5f;

    // --- Internal State ---
    private Emotion _currentEmotion;
    private EmotionBehaviorProfile _activeBehaviorProfile;
    private float _timeSinceLastEmotionChange; // Tracks when a significant emotion change last occurred

    // --- Cached AI Component References ---
    private AudioSource _audioSource;
    private GameObject _activeParticleEffect; // To manage particle systems

    // --- AI's Actual Behavior Variables (these are what other AI systems would use) ---
    public float ActualMovementSpeed { get; private set; }
    public float ActualAttackDelay { get; private set; } // The time to wait between attacks

    void Awake()
    {
        // Initialize the AI with a Neutral emotional state
        _currentEmotion = Emotion.Neutral;
        _activeBehaviorProfile = GetProfile(EmotionType.Neutral);

        // Get component references if not assigned in Inspector
        if (_moodRenderer == null) _moodRenderer = GetComponentInChildren<Renderer>();
        _audioSource = GetComponent<AudioSource>(); // Required by [RequireComponent]

        // Apply initial neutral behaviors
        ApplyBehaviorBasedOnEmotion();
    }

    void Update()
    {
        // 1. Manage emotion dynamics (decay and stabilization)
        DecayEmotion();
        StabilizeEmotion();

        // 2. Apply the current emotional state to the AI's behavior and visuals
        ApplyBehaviorBasedOnEmotion();

        // Optional: Debug log for current emotion
        // Debug.Log($"{gameObject.name} - Emotion: {_currentEmotion.type} (Intensity: {_currentEmotion.intensity:F2})");
    }

    /// <summary>
    /// Triggers an emotion in the AI. This is the primary method for external systems
    /// (e.g., player actions, environmental events) to influence the AI's mood.
    /// </summary>
    /// <param name="triggeredEmotionType">The type of emotion to introduce or intensify.</param>
    /// <param name="intensityImpact">The amount this trigger increases the emotion's intensity.
    /// If negative, it decreases the intensity. A strong positive value might override weaker existing emotions.</param>
    /// <param name="forceOverride">If true, this emotion will always become dominant, regardless of current emotion's intensity.</param>
    public void TriggerEmotion(EmotionType triggeredEmotionType, float intensityImpact, bool forceOverride = false)
    {
        // If the new emotion is the same type as the current one, just adjust intensity
        if (_currentEmotion.type == triggeredEmotionType)
        {
            _currentEmotion.intensity += intensityImpact;
            Debug.Log($"Emotion '{triggeredEmotionType}' intensified by {intensityImpact}.");
        }
        else // Different emotion type
        {
            // Only change to the new emotion if it's stronger, or if explicitly forced to override
            if (intensityImpact > _currentEmotion.intensity || forceOverride)
            {
                _currentEmotion.type = triggeredEmotionType;
                _currentEmotion.intensity = Mathf.Max(_currentEmotion.intensity, intensityImpact); // Take the higher intensity if it's overriding
                Debug.Log($"Emotion changed to '{triggeredEmotionType}' with intensity {intensityImpact}.");
            }
            else // If the new emotion is weaker and not forced, it has no effect
            {
                Debug.Log($"Emotion '{triggeredEmotionType}' (intensity {intensityImpact}) was too weak to override '{_currentEmotion.type}' (intensity {_currentEmotion.intensity:F2}).");
                return; // Do not proceed if emotion wasn't changed
            }
        }

        // Clamp intensity to ensure it stays within valid bounds [0, _maxEmotionIntensity]
        _currentEmotion.intensity = Mathf.Clamp(_currentEmotion.intensity, 0f, _maxEmotionIntensity);

        // Reset the stabilization timer whenever a significant emotion change occurs, to allow new emotions to "settle"
        if (intensityImpact > 0.05f || forceOverride)
        {
             _timeSinceLastEmotionChange = 0f;
        }

        // Immediately update the behavior profile for responsiveness
        _activeBehaviorProfile = GetProfile(_currentEmotion.type);
    }

    /// <summary>
    /// Reduces the intensity of the current emotion over time, simulating emotions fading.
    /// </summary>
    private void DecayEmotion()
    {
        // Neutral emotion doesn't naturally decay as it's the base state
        if (_currentEmotion.type == EmotionType.Neutral)
        {
            _currentEmotion.intensity = 0f;
            return;
        }

        _currentEmotion.intensity -= _emotionDecayRate * Time.deltaTime;
        _currentEmotion.intensity = Mathf.Max(_currentEmotion.intensity, 0f); // Ensure intensity doesn't go below zero

        // If intensity drops below a threshold, revert to Neutral
        if (_currentEmotion.intensity < _minActiveEmotionIntensity)
        {
            _currentEmotion = Emotion.Neutral;
            _activeBehaviorProfile = GetProfile(EmotionType.Neutral);
            _timeSinceLastEmotionChange = 0f; // Reset timer upon returning to neutral
        }
    }

    /// <summary>
    /// Prevents emotions from staying at peak intensity indefinitely after a single strong trigger.
    /// It gently pushes high-intensity emotions towards a more stable state after a delay.
    /// </summary>
    private void StabilizeEmotion()
    {
        // Only stabilize if there's an active non-neutral emotion above minimum intensity
        if (_currentEmotion.type == EmotionType.Neutral || _currentEmotion.intensity <= _minActiveEmotionIntensity)
        {
            return;
        }

        _timeSinceLastEmotionChange += Time.deltaTime;

        // After the stabilization delay, start gently reducing intensity if it's still high
        if (_timeSinceLastEmotionChange > _emotionStabilizationDelay && _currentEmotion.intensity > _minActiveEmotionIntensity)
        {
            _currentEmotion.intensity = Mathf.Lerp(_currentEmotion.intensity, _minActiveEmotionIntensity, Time.deltaTime * _emotionStabilizationRate);
            // Ensure intensity doesn't dip below the minimum active threshold during stabilization
            _currentEmotion.intensity = Mathf.Max(_currentEmotion.intensity, _minActiveEmotionIntensity);
        }
    }

    /// <summary>
    /// Applies the current emotional state to the AI's various components and behaviors.
    /// This is where the emotional state directly influences game mechanics.
    /// </summary>
    private void ApplyBehaviorBasedOnEmotion()
    {
        // Ensure we always have an active behavior profile, defaulting to Neutral if none found
        if (_activeBehaviorProfile == null || _activeBehaviorProfile.emotionType != _currentEmotion.type)
        {
            _activeBehaviorProfile = GetProfile(_currentEmotion.type);
            if (_activeBehaviorProfile == null)
            {
                Debug.LogWarning($"No behavior profile found for emotion type: {_currentEmotion.type}. Using Neutral defaults.");
                _activeBehaviorProfile = GetProfile(EmotionType.Neutral); // Fallback
            }
        }

        // Calculate a normalized intensity for blending behaviors, 0 for Neutral, 1 for full emotion impact
        float normalizedIntensity = (_currentEmotion.type == EmotionType.Neutral) ? 0f :
                                    Mathf.Clamp01(_currentEmotion.intensity / _maxEmotionIntensity);

        // --- 1. Movement Behavior ---
        // Interpolate movement speed from base (Neutral) to the profile's modified speed based on emotion intensity.
        float targetSpeed = _baseMovementSpeed * _activeBehaviorProfile.movementSpeedMultiplier;
        ActualMovementSpeed = Mathf.Lerp(_baseMovementSpeed, targetSpeed, normalizedIntensity);
        // In a real AI, this `ActualMovementSpeed` would be used by a movement component (e.g., NavMeshAgent, Rigidbody).
        // Example: myNavMeshAgent.speed = ActualMovementSpeed;

        // --- 2. Visual Feedback (e.g., character color tint) ---
        if (_moodRenderer != null && _moodRenderer.material != null)
        {
            Color neutralColor = GetProfile(EmotionType.Neutral)?.moodColor ?? Color.white;
            _moodRenderer.material.color = Color.Lerp(neutralColor, _activeBehaviorProfile.moodColor, normalizedIntensity);
        }

        // --- 3. Audio Feedback (e.g., vocalizations, mood music) ---
        if (_audioSource != null)
        {
            // Only play/change clip if it's different and a clip is assigned
            if (_activeBehaviorProfile.voiceClip != null && _audioSource.clip != _activeBehaviorProfile.voiceClip)
            {
                _audioSource.clip = _activeBehaviorProfile.voiceClip;
                _audioSource.Play();
            }
            else if (_activeBehaviorProfile.voiceClip == null && _audioSource.isPlaying)
            {
                _audioSource.Stop(); // Stop playing if no clip for current emotion
            }
        }

        // --- 4. Dialogue/Text Feedback ---
        // The `dialoguePrefix` could be prepended to any generated dialogue or text messages from the AI.
        // Example: myAIDialogueSystem.SetDialoguePrefix(_activeBehaviorProfile.dialoguePrefix);

        // --- 5. Combat/Interaction Behavior ---
        // Calculate actual attack delay. A higher `attackSpeedModifier` means faster attacks (lower delay).
        float baseAttackDelay = 1f; // Example: 1 second between attacks in neutral state
        float targetAttackDelay = baseAttackDelay / (1f + _activeBehaviorProfile.attackSpeedModifier); // E.g., if modifier is 0.5, delay becomes 1/1.5 = 0.66s
        ActualAttackDelay = Mathf.Lerp(baseAttackDelay, targetAttackDelay, normalizedIntensity);
        // This `ActualAttackDelay` would then be used by an AICombatSystem component for timing attacks.
        // Example: myAICombatSystem.SetAttackCooldown(ActualAttackDelay);

        // --- 6. Particle Effects ---
        if (_activeBehaviorProfile.moodParticleEffectPrefab != null)
        {
            if (_activeParticleEffect == null)
            {
                // Instantiate and parent the particle effect
                _activeParticleEffect = Instantiate(_activeBehaviorProfile.moodParticleEffectPrefab, transform);
            }
            // Ensure only the current emotion's particle effect is active
            if (!_activeParticleEffect.activeSelf) _activeParticleEffect.SetActive(true);
        }
        else if (_activeParticleEffect != null)
        {
            // If no particle effect for this emotion, deactivate the existing one
            _activeParticleEffect.SetActive(false);
        }
    }

    /// <summary>
    /// Helper method to find the behavior profile corresponding to a given emotion type.
    /// </summary>
    /// <param name="type">The EmotionType to look up.</param>
    /// <returns>The matching EmotionBehaviorProfile, or null if not found.</returns>
    private EmotionBehaviorProfile GetProfile(EmotionType type)
    {
        // Using LINQ's FirstOrDefault for a concise lookup. Consider a Dictionary for very large numbers of profiles for performance.
        return _behaviorProfiles.FirstOrDefault(p => p.emotionType == type);
    }

    // --- Example Usage Methods (for demonstration/testing) ---
    // These methods would typically be called by player interaction scripts, game event managers, etc.
    // They are marked with [ContextMenu] to allow easy testing directly from the Unity Inspector.

    [ContextMenu("Simulate - Player Praises AI (+Happy)")]
    public void SimulatePlayerPraise()
    {
        TriggerEmotion(EmotionType.Happy, 0.4f);
    }

    [ContextMenu("Simulate - Player Attacks AI (+Angry)")]
    public void SimulatePlayerAttack()
    {
        TriggerEmotion(EmotionType.Angry, 0.6f);
        // An attack might also cause fear, depending on the AI's personality/context:
        // TriggerEmotion(EmotionType.Scared, 0.3f);
    }

    [ContextMenu("Simulate - AI Sees Danger (+Scared, forced)")]
    public void SimulateDanger()
    {
        // Danger is often critical, so we might force it to override even if weaker
        TriggerEmotion(EmotionType.Scared, 0.7f, true);
    }

    [ContextMenu("Simulate - AI Experiences Loss (+Sad)")]
    public void SimulateLoss()
    {
        TriggerEmotion(EmotionType.Sad, 0.5f);
    }

    [ContextMenu("Simulate - AI Receives Bad News (+Confused & -Happy)")]
    public void SimulateBadNews()
    {
        TriggerEmotion(EmotionType.Confused, 0.3f);
        TriggerEmotion(EmotionType.Happy, -0.2f); // Reduce happiness
    }

    [ContextMenu("Simulate - AI Reverts to Neutral")]
    public void SimulateRevertToNeutral()
    {
        _currentEmotion = Emotion.Neutral;
        _activeBehaviorProfile = GetProfile(EmotionType.Neutral);
        _timeSinceLastEmotionChange = 0f;
        Debug.Log("Emotion reset to Neutral.");
    }
}
```

---

## How to Implement and Use in Unity

1.  **Create a New C# Script:**
    *   In your Unity project, go to `Assets/Create/C# Script` and name it `EmotionDrivenAIController`.
    *   Copy and paste the entire code above into this new script, replacing its default content.

2.  **Create an AI GameObject:**
    *   In your Unity scene, create an empty GameObject (`GameObject/Create Empty`). Name it something like "MyEmotionDrivenAI".
    *   Add a visual component to it. For example, right-click on "MyEmotionDrivenAI" in the Hierarchy, then `3D Object/Cube`. This will be the AI's visible body.
    *   Add an `AudioSource` component to "MyEmotionDrivenAI" (`Add Component/Audio Source`).

3.  **Attach the Script:**
    *   Select "MyEmotionDrivenAI" in the Hierarchy.
    *   Drag the `EmotionDrivenAIController.cs` script from your Project window onto the "MyEmotionDrivenAI" GameObject in the Inspector.

4.  **Configure in the Inspector:**
    *   **Emotion Dynamics Settings:** Adjust `Emotion Decay Rate`, `Max Emotion Intensity`, `Min Active Emotion Intensity`, `Emotion Stabilization Delay`, and `Emotion Stabilization Rate` to control how quickly emotions change and fade.
    *   **AI Component References:**
        *   Drag the `Cube` (child of "MyEmotionDrivenAI") into the `Mood Renderer` slot.
        *   Leave `Base Movement Speed` at its default or adjust.
    *   **Behavior Profiles:** This is the core setup for your AI's personality and reactions.
        *   Click the `+` icon under `Behavior Profiles` to add new entries.
        *   **Crucially, add a `Neutral` emotion profile first!** This is the AI's default state.
            *   `Emotion Type`: Neutral
            *   `Movement Speed Multiplier`: 1 (base speed)
            *   `Mood Color`: White
            *   `Dialogue Prefix`: ""
            *   `Attack Speed Modifier`: 0
            *   `Voice Clip`: (none or a calm idle sound)
        *   **Add other Emotion Profiles (e.g., Happy, Angry, Scared, Sad):**
            *   For each emotion, choose an `Emotion Type` from the dropdown.
            *   **`Movement Speed Multiplier`:** e.g., 1.2 for Happy (a bit faster), 0.8 for Angry (slow, deliberate), 1.5 for Scared (running away), 0.5 for Sad (slow, lethargic).
            *   **`Mood Color`:** Choose a distinct color for each (e.g., Green for Happy, Red for Angry, Blue for Scared, Gray for Sad).
            *   **`Dialogue Prefix`:** Add short phrases (e.g., "Yay! " for Happy, "Grrr! " for Angry).
            *   **`Attack Speed Modifier`:** e.g., 0.5 for Angry (50% faster attacks), -0.5 for Scared (50% slower attacks).
            *   **`Voice Clip`:** Optionally, drag an `AudioClip` into this slot for specific vocalizations when this emotion is active.
            *   **`Mood Particle Effect Prefab`**: Create a simple particle system prefab (e.g. `GameObject/Effects/Particle System`) and drag it here. It will be instantiated and activated when this emotion is active.

5.  **Run the Scene:**
    *   Press Play in the Unity editor.
    *   Your cube will initially be white (Neutral).
    *   **Test Emotions:** Select "MyEmotionDrivenAI" in the Hierarchy. In the Inspector, right-click on the `Emotion Driven AI Controller` component. You'll see the "Simulate -" context menu items. Click them to instantly trigger different emotions and observe the cube's color change, the `Current Emotion Type` and `Intensity` values updating, and hear audio if assigned.

---

## How to Integrate with Other AI Systems:

You would typically call the `TriggerEmotion` method from other C# scripts based on game events:

```csharp
// Example: A Player interaction script
public class PlayerInteraction : MonoBehaviour
{
    public EmotionDrivenAIController targetAI;
    public float praiseAmount = 0.4f;
    public float attackAmount = 0.6f;

    void Update()
    {
        // Simulate player interaction
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (targetAI != null)
            {
                targetAI.TriggerEmotion(EmotionType.Happy, praiseAmount);
                Debug.Log("Player praised AI!");
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (targetAI != null)
            {
                targetAI.TriggerEmotion(EmotionType.Angry, attackAmount);
                Debug.Log("Player attacked AI!");
            }
        }
    }
}

// Example: An AI Combat System using the emotional state
public class AICombatSystem : MonoBehaviour
{
    public EmotionDrivenAIController emotionAI;
    private float _attackCooldownTimer;

    void Start()
    {
        _attackCooldownTimer = 0f;
    }

    void Update()
    {
        // Decrease cooldown timer
        _attackCooldownTimer -= Time.deltaTime;

        // Use the AI's actual attack delay influenced by emotion
        if (_attackCooldownTimer <= 0 && CanSeeEnemy())
        {
            AttackEnemy();
            _attackCooldownTimer = emotionAI.ActualAttackDelay; // Get delay from emotion controller
        }
    }

    private bool CanSeeEnemy() { /* Your line-of-sight/detection logic */ return true; }
    private void AttackEnemy() { /* Your attack logic */ Debug.Log($"AI attacks with {emotionAI.CurrentEmotionType} emotion!"); }
}
```

This `EmotionDrivenAIController` provides a robust, extensible, and practical foundation for adding emotional depth to your Unity AI agents. By simply configuring `EmotionBehaviorProfile`s in the Inspector, you can create a wide range of emotionally responsive characters.